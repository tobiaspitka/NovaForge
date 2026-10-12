using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using NovaForge.Automation;
using NovaForge.Core;
using System.Xml.Linq;
internal static class Program
{
 private static int passed;
 private static void Assert(bool value,string name){if(!value)throw new Exception("FAIL "+name);Console.WriteLine("PASS "+name);passed++;}
 private static async Task Throws(Func<Task> action,string name){try{await action();}catch(Exception e) when(e is InvalidDataException or TimeoutException){Assert(true,name);return;}throw new Exception("Expected rejection: "+name);}
 [STAThread] public static int Main(string[] args)=>Run(args).GetAwaiter().GetResult();
 private static async Task<int> Run(string[] args)
 {
  var exe=args[0];var directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaForge","Beta","Test-"+Guid.NewGuid());Directory.CreateDirectory(directory);var marker=Path.Combine(directory,"proof.txt");Process? process=null;
  try{
   process=UiAutomationEngine.StartInstaller(exe,"--fixture "+IsoBuilder.Quote(marker));
   var allowed=UiAutomationEngine.ProcessFilter(process.Id,false);AutomationElement? window=null;var until=DateTime.UtcNow.AddSeconds(20);
   while(DateTime.UtcNow<until){window=AutomationElement.RootElement.FindAll(TreeScope.Children,new PropertyCondition(AutomationElement.NameProperty,"NovaForge UIA Test")).Cast<AutomationElement>().FirstOrDefault(x=>allowed(x.Current.ProcessId));if(window!=null)break;await Task.Delay(200);}
   if(window==null)throw new Exception("Real fixture window failed to open.");
   AutomationElement Control(string id)=>window.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,id));
   var toggle=UiAutomationEngine.Capture(Control("component"))!;var text=UiAutomationEngine.Capture(Control("target"),true)!;var finish=UiAutomationEngine.Capture(Control("finish"))!;
   Assert(toggle.Action=="Toggle"&&toggle.Value=="On","Capture real checkbox selector and desired state");Assert(text.Action=="SetValue","Capture real edit control by explicit request");Assert(UiAutomationEngine.Capture(Control("secret"),true)==null,"Password control never recorded");Assert(finish.Action=="Invoke"&&finish.AutomationId=="finish","Capture real button semantic ID");
   text.Value="recorded-value";UiAutomationEngine.Apply(Control("component"),toggle);UiAutomationEngine.Apply(Control("target"),text);UiAutomationEngine.Apply(Control("finish"),finish);await process.WaitForExitAsync();Assert(File.ReadAllText(marker)=="enabled|recorded-value","UIA actions produce actual expected fixture result");File.Delete(marker);
   var profile=new RecordedProfile{InstallerSha256=await Validation.Hash(exe),InstallerArguments="--fixture "+IsoBuilder.Quote(marker),VerificationFile=UiAutomationEngine.Normalize(marker),Steps=[toggle,text,finish]};profile.Validate();
   Assert(!profile.Tested,"Recording starts untested");await Throws(()=>Task.Run(()=>profile.Validate(true)),"Untested profile rejected for ISO");
   using(var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30))){var code=await UiAutomationEngine.Replay(profile,exe,Console.WriteLine,timeout.Token);Assert(code==0&&File.ReadAllText(marker)=="enabled|recorded-value","Fresh real process replays recorded selections and verifies new file");}
   profile.Tested=true;profile.TestedPlanSha256=profile.PlanHash();profile.Validate(true);Assert(true,"Unchanged tested plan accepted");text.Value="modified";await Throws(()=>Task.Run(()=>profile.Validate(true)),"Changed tested plan invalidates approval");text.Value="recorded-value";
   await Throws(()=>UiAutomationEngine.Replay(profile,exe,_=>{},CancellationToken.None),"Existing proof file cannot falsely confirm a fresh installation");File.Delete(marker);
   var hash=profile.InstallerSha256;profile.InstallerSha256=new string('0',64);await Throws(()=>UiAutomationEngine.Replay(profile,exe,_=>{},CancellationToken.None),"Mismatched installer rejected before start");profile.InstallerSha256=hash;
   var old=text.AutomationId;text.AutomationId="does-not-exist";text.TimeoutSeconds=1;await Throws(()=>UiAutomationEngine.Replay(profile,exe,_=>{},CancellationToken.None),"Missing UI target times out instead of blind clicking");text.AutomationId=old;
   var config=XDocument.Parse(SandboxSessions.Config(@"C:\Input Ä & files",@"C:\Output",false));Assert(config.Descendants("ReadOnly").Select(x=>x.Value).SequenceEqual(new[]{"true","false"}),"Sandbox input read-only and isolated output writable");Assert(config.Root!.Element("Networking")!.Value=="Disable"&&config.Root.Element("ClipboardRedirection")!.Value=="Disable","Sandbox network and clipboard disabled by default");
   Console.WriteLine($"{passed} automation checks passed. Only the own harmless test window was executed.");return 0;
  }finally{if(process is {HasExited:false})process.Kill(true);process?.Dispose();Directory.Delete(directory,true);}
 }
}

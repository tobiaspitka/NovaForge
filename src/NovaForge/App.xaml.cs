using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using NovaForge.Automation;
using NovaForge.Core;
namespace NovaForge;
public partial class App : Application
{
 protected override async void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);DispatcherUnhandledException+=(_,a)=>{var path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaForge","Beta");Directory.CreateDirectory(path);File.AppendAllText(System.IO.Path.Combine(path,"application-error.log"),DateTimeOffset.Now+"\n"+a.Exception+"\n");MessageBox.Show(a.Exception.GetBaseException().Message,"NovaForge Beta – Fehler");a.Handled=true;};
  try{
   string Arg(string key){var index=Array.IndexOf(e.Args,key);if(index<0||index+1>=e.Args.Length)throw new InvalidDataException("Argument fehlt: "+key);return e.Args[index+1];}
   if(e.Args.Contains("--fixture")){new AutomationFixtureWindow(Arg("--fixture")).Show();return;}
   if(e.Args.Contains("--recorder")){new RecorderWindow(Arg("--session")).Show();return;}
   if(e.Args.Contains("--replay")){
    ShutdownMode=ShutdownMode.OnExplicitShutdown;var profileFile=Arg("--profile");var installer=Arg("--installer");var logFile=Arg("--log");var profile=await RecordedProfile.Load(profileFile);profile.Validate(true);
    if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)){
     var arguments=string.Join(" ",e.Args.Select(IsoBuilder.Quote));using var child=Process.Start(new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=true,Verb="runas",Arguments=arguments})??throw new IOException("Erhöhung fehlgeschlagen.");await child.WaitForExitAsync();Shutdown(child.ExitCode);return;
    }
    using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(30));Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(logFile))!);
    var code=await UiAutomationEngine.Replay(profile,installer,s=>File.AppendAllText(logFile,DateTimeOffset.Now.ToString("o")+" "+s+Environment.NewLine),timeout.Token);Shutdown(code);return;
   }
   new MainWindow().Show();
  }catch(Exception ex){if(e.Args.Contains("--replay")){try{var i=Array.IndexOf(e.Args,"--log");if(i>=0)File.AppendAllText(e.Args[i+1],ex.ToString());}catch{}Shutdown(1);}else{MessageBox.Show(ex.Message,"NovaForge Beta – Vorgang nicht möglich");Shutdown(1);}}
 }
}

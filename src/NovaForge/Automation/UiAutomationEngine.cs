using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using NovaForge.Core;
using Condition = System.Windows.Automation.Condition;
namespace NovaForge.Automation;
public static class UiAutomationEngine
{
 public static readonly Dictionary<string,Environment.SpecialFolder> Folders=new(){["{ProgramFilesX86}"]=Environment.SpecialFolder.ProgramFilesX86,["{ProgramFiles}"]=Environment.SpecialFolder.ProgramFiles,["{LocalAppData}"]=Environment.SpecialFolder.LocalApplicationData,["{ProgramData}"]=Environment.SpecialFolder.CommonApplicationData,["{UserProfile}"]=Environment.SpecialFolder.UserProfile};
 public static string Normalize(string value){foreach(var f in Folders.OrderByDescending(x=>Environment.GetFolderPath(x.Value).Length)){var path=Environment.GetFolderPath(f.Value);if(path.Length>0)value=value.Replace(path,f.Key,StringComparison.OrdinalIgnoreCase);}return value;}
 public static string Expand(string value){foreach(var f in Folders)value=value.Replace(f.Key,Environment.GetFolderPath(f.Value),StringComparison.Ordinal);return value;}
 public static AutomationElement? TopWindow(AutomationElement element){var walker=TreeWalker.ControlViewWalker;for(int i=0;i<30&&element!=null;i++){var parent=walker.GetParent(element);if(parent==null||parent.Equals(AutomationElement.RootElement))return element;element=parent;}return null;}
 public static UiStep? Capture(AutomationElement element,bool captureText=false)
 {
  var walker=TreeWalker.ControlViewWalker;
  for(int i=0;i<8&&element!=null;i++,element=walker.GetParent(element)){
   if(element.Current.IsPassword)return null;
   string action;string value="";
   if(captureText&&element.TryGetCurrentPattern(ValuePattern.Pattern,out var v)){if(System.Text.RegularExpressions.Regex.IsMatch(element.Current.Name,"password|kennwort|license|lizenz|serial|schlüssel",System.Text.RegularExpressions.RegexOptions.IgnoreCase))return null;action="SetValue";value=Normalize(((ValuePattern)v).Current.Value);}
   else if(element.TryGetCurrentPattern(TogglePattern.Pattern,out var t)){action="Toggle";value=((TogglePattern)t).Current.ToggleState==ToggleState.On?"Off":"On";}
   else if(element.TryGetCurrentPattern(SelectionItemPattern.Pattern,out _))action="Select";
   else if(element.TryGetCurrentPattern(InvokePattern.Pattern,out _))action="Invoke";
   else continue;
   var window=TopWindow(element);if(window==null)return null;
   return new(){Action=action,Value=value,WindowTitle=window.Current.Name,Name=element.Current.Name,AutomationId=element.Current.AutomationId,ClassName=element.Current.ClassName,ControlTypeId=element.Current.ControlType.Id};
  }
  return null;
 }
 public static UiStep CaptureSecret(AutomationElement element,string value){var window=TopWindow(element)??throw new InvalidDataException("Kein Fenster gefunden.");if(element.Current.ControlType!=ControlType.Edit)throw new InvalidDataException("Bitte ein Text- oder Kennwortfeld fokussieren.");return new(){Action="SetSecretValue",Value=value,WindowTitle=window.Current.Name,Name=element.Current.Name,AutomationId=element.Current.AutomationId,ClassName=element.Current.ClassName,ControlTypeId=element.Current.ControlType.Id};}
 public static void BindExternal(UiStep step,int pid){using var process=Process.GetProcessById(pid);var path=process.MainModule?.FileName??throw new IOException("Zusatzprozess kann nicht geprüft werden.");step.ProcessName=process.ProcessName;step.ProcessImageSha256=Validation.Hash(path).GetAwaiter().GetResult();}
 public static bool MatchesExternal(UiStep step,int pid){if(step.ProcessImageSha256.Length==0)return false;try{using var p=Process.GetProcessById(pid);return p.ProcessName.Equals(step.ProcessName,StringComparison.OrdinalIgnoreCase)&&Validation.Hash(p.MainModule!.FileName).GetAwaiter().GetResult()==step.ProcessImageSha256;}catch{return false;}}
 public static AutomationElement? Find(UiStep step,Func<int,bool> allowed)
 {
  var windows=AutomationElement.RootElement.FindAll(TreeScope.Children,new PropertyCondition(AutomationElement.NameProperty,step.WindowTitle));var matches=new List<AutomationElement>();
  foreach(AutomationElement window in windows){if(!allowed(window.Current.ProcessId))continue;
   var conditions=new List<Condition>{new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.LookupById(step.ControlTypeId))};
   if(step.AutomationId.Length>0)conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty,step.AutomationId));else conditions.Add(new PropertyCondition(AutomationElement.NameProperty,step.Name));
   if(step.ClassName.Length>0)conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty,step.ClassName));
   foreach(AutomationElement element in window.FindAll(TreeScope.Descendants,new AndCondition(conditions.ToArray())))if(element.Current.IsEnabled&&!element.Current.IsOffscreen)matches.Add(element);
  }
  if(matches.Count>1)throw new InvalidDataException("Steuerelement ist nicht eindeutig: "+step.Name);return matches.SingleOrDefault();
 }
 public static void Apply(AutomationElement element,UiStep step)
 {
  if(step.Action=="SetSecretValue"){if(element.TryGetCurrentPattern(ValuePattern.Pattern,out var pattern)){((ValuePattern)pattern).SetValue(step.Value);return;}element.SetFocus();if(!element.Equals(AutomationElement.FocusedElement))throw new InvalidDataException("Vertrauliches Zielfeld konnte nicht fokussiert werden.");var keys=new List<Input>();Input Key(ushort key,uint flags)=>new(){Type=1,Data=new InputUnion{Keyboard=new KeyboardInput{VirtualKey=key,Flags=flags}}};keys.Add(Key(0x11,0));keys.Add(Key(0x41,0));keys.Add(Key(0x41,2));keys.Add(Key(0x11,2));keys.Add(Key(0x08,0));keys.Add(Key(0x08,2));foreach(var c in step.Value){keys.Add(new(){Type=1,Data=new InputUnion{Keyboard=new KeyboardInput{Scan=c,Flags=4}}});keys.Add(new(){Type=1,Data=new InputUnion{Keyboard=new KeyboardInput{Scan=c,Flags=6}}});}if(SendInput((uint)keys.Count,keys.ToArray(),Marshal.SizeOf<Input>())!=(uint)keys.Count)throw new IOException("Vertrauliches Feld konnte nicht vollständig eingegeben werden.");return;}
  if(element.Current.IsPassword)throw new InvalidDataException("Kennwortfelder erfordern einen ausdrücklich aufgenommenen vertraulichen Schritt.");
  switch(step.Action){case "Invoke":((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();break;case "Select":((SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();break;case "SetValue":((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).SetValue(Expand(step.Value));break;case "Toggle":var toggle=(TogglePattern)element.GetCurrentPattern(TogglePattern.Pattern);var desired=step.Value=="On"?ToggleState.On:ToggleState.Off;if(toggle.Current.ToggleState!=desired)toggle.Toggle();if(toggle.Current.ToggleState!=desired)throw new InvalidDataException("Kontrollkästchen hat nicht den erwarteten Zustand.");break;default:throw new InvalidDataException("Nicht unterstützte Aktion.");}
 }
 public static Func<int,bool> ProcessFilter(int root,bool msi,HashSet<int>? extra=null)=>pid=>pid==root||(extra?.Contains(pid)??false)||IsDescendant(pid,root)||(msi&&SafeProcessName(pid)=="msiexec");
 private static string SafeProcessName(int id){try{return Process.GetProcessById(id).ProcessName.ToLowerInvariant();}catch{return "";}}
 private static bool IsDescendant(int id,int root){var parents=ParentMap();for(int i=0;i<30&&parents.TryGetValue(id,out var parent);i++){if(parent==root)return true;if(parent==id)break;id=parent;}return false;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct Entry {public uint Size,Usage,Pid;public IntPtr Heap;public uint Module,Threads,Parent;public int Priority;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Name;}
 [DllImport("kernel32.dll")]private static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint pid);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern bool Process32FirstW(IntPtr snapshot,ref Entry entry);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern bool Process32NextW(IntPtr snapshot,ref Entry entry);
 [DllImport("kernel32.dll")]private static extern bool CloseHandle(IntPtr handle);
 private static Dictionary<int,int> ParentMap(){var result=new Dictionary<int,int>();var handle=CreateToolhelp32Snapshot(2,0);if(handle==new IntPtr(-1))return result;try{var e=new Entry{Size=(uint)Marshal.SizeOf<Entry>(),Name=""};if(Process32FirstW(handle,ref e))do{result[(int)e.Pid]=(int)e.Parent;}while(Process32NextW(handle,ref e));}finally{CloseHandle(handle);}return result;}
 [StructLayout(LayoutKind.Sequential)]private struct Input{public uint Type;public InputUnion Data;}
 [StructLayout(LayoutKind.Explicit)]private struct InputUnion{[FieldOffset(0)]public KeyboardInput Keyboard;[FieldOffset(0)]public NativeMouseInput Mouse;}
 [StructLayout(LayoutKind.Sequential)]private struct KeyboardInput{public ushort VirtualKey,Scan;public uint Flags,Time;public IntPtr Extra;}
 [StructLayout(LayoutKind.Sequential)]private struct NativeMouseInput{public int X,Y;public uint Data,Flags,Time;public IntPtr Extra;}
 [DllImport("user32.dll",SetLastError=true)]private static extern uint SendInput(uint count,Input[] input,int size);
 public static Process StartInstaller(string path,string arguments)
 {
  var msi=System.IO.Path.GetExtension(path).Equals(".msi",StringComparison.OrdinalIgnoreCase);var info=new ProcessStartInfo(msi?System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"msiexec.exe"):path){UseShellExecute=false,Arguments=msi?"/i "+IsoBuilder.Quote(path)+" "+arguments:arguments,WorkingDirectory=System.IO.Path.GetDirectoryName(path)!};return Process.Start(info)??throw new IOException("Installer konnte nicht gestartet werden.");
 }
 public static async Task<int> Replay(RecordedProfile profile,string installer,Action<string> log,CancellationToken ct)
 {
  profile.Validate();if(await Validation.Hash(installer,ct)!=profile.InstallerSha256)throw new InvalidDataException("Installer stimmt nicht mit der Aufzeichnung überein.");
  var verification=Expand(profile.VerificationFile);if(File.Exists(verification))throw new InvalidDataException("Prüfdatei existiert bereits. Für den Profiltest eine frische Umgebung verwenden.");
  using var process=StartInstaller(installer,profile.InstallerArguments);var allowed=ProcessFilter(process.Id,System.IO.Path.GetExtension(installer).Equals(".msi",StringComparison.OrdinalIgnoreCase));
  try{foreach(var step in profile.Steps){log("Schritt: "+step.Action+" · "+(step.Action=="SetSecretValue"?"Vertrauliches Feld":step.Name));var until=DateTime.UtcNow.AddSeconds(step.TimeoutSeconds);AutomationElement? element=null;while(DateTime.UtcNow<until){ct.ThrowIfCancellationRequested();try{element=Find(step,pid=>allowed(pid)||MatchesExternal(step,pid));}catch(ElementNotAvailableException){}if(element!=null)break;await Task.Delay(200,ct);}if(element==null)throw new TimeoutException("Fenster/Steuerelement nicht gefunden: "+step.WindowTitle+" / "+step.Name);Apply(element,step);await Task.Delay(300,ct);}
   await process.WaitForExitAsync(ct);var code=process.ExitCode;if(code is not (0 or 3010))throw new IOException("Installer-Exitcode: "+code);
   if(!File.Exists(verification))throw new IOException("Installation nicht bestätigt: neue Prüfdatei fehlt.");log("Installation anhand neuer Prüfdatei bestätigt.");return code;
  }catch{try{if(!process.HasExited)process.Kill(true);}catch{}throw;}
 }
}

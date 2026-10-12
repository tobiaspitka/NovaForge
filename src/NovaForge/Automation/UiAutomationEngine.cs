using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using NovaForge.Core;
using Condition = System.Windows.Automation.Condition;
namespace NovaForge.Automation;
public static class UiAutomationEngine
{
 public static readonly Dictionary<string,Environment.SpecialFolder> Folders=new(){["{ProgramFilesX86}"]=Environment.SpecialFolder.ProgramFilesX86,["{ProgramFiles}"]=Environment.SpecialFolder.ProgramFiles,["{LocalAppData}"]=Environment.SpecialFolder.LocalApplicationData,["{Windows}"]=Environment.SpecialFolder.Windows,["{ProgramData}"]=Environment.SpecialFolder.CommonApplicationData,["{UserProfile}"]=Environment.SpecialFolder.UserProfile};
 public static string InstallerDirectory {get;set;}="";
 public static string Normalize(string value){if(InstallerDirectory.Length>0)value=value.Replace(InstallerDirectory,"{InstallerDirectory}",StringComparison.OrdinalIgnoreCase);foreach(var f in Folders.OrderByDescending(x=>Environment.GetFolderPath(x.Value).Length)){var path=Environment.GetFolderPath(f.Value);if(path.Length>0)value=value.Replace(path,f.Key,StringComparison.OrdinalIgnoreCase);}return value;}
 public static string Expand(string value){if(InstallerDirectory.Length>0)value=value.Replace("{InstallerDirectory}",InstallerDirectory,StringComparison.Ordinal);foreach(var f in Folders)value=value.Replace(f.Key,Environment.GetFolderPath(f.Value),StringComparison.Ordinal);return value;}
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
 public static string ProcessImagePath(int pid){var handle=OpenProcess(0x1000,false,(uint)pid);if(handle==IntPtr.Zero)throw new IOException("Zusatzfenster-Prozess kann nicht abgefragt werden (Win32 "+Marshal.GetLastWin32Error()+").");try{var path=new System.Text.StringBuilder(32768);var size=(uint)path.Capacity;if(!QueryFullProcessImageName(handle,0,path,ref size))throw new IOException("Zusatzfenster-Prozesspfad fehlt (Win32 "+Marshal.GetLastWin32Error()+").");return path.ToString();}finally{CloseHandle(handle);}}
 public static void BindExternal(UiStep step,int pid){var path=ProcessImagePath(pid);step.ProcessName=Path.GetFileNameWithoutExtension(path);step.ProcessImageSha256=Validation.Hash(path).GetAwaiter().GetResult();}
 public static bool MatchesExternal(UiStep step,int pid){if(step.ProcessImageSha256.Length==0)return false;try{var path=ProcessImagePath(pid);return Path.GetFileNameWithoutExtension(path).Equals(step.ProcessName,StringComparison.OrdinalIgnoreCase)&&Validation.Hash(path).GetAwaiter().GetResult()==step.ProcessImageSha256;}catch{return false;}}
 public sealed record RepairTarget(UiStep Selector,string Label){public override string ToString()=>Label;}
 private static string LabelKey(string value)=>System.Text.RegularExpressions.Regex.Replace(value.Replace("&","").Replace("…","...").Trim(),@"\s+"," ").TrimEnd('.').ToUpperInvariant();
 private static string TitleKey(string value)=>LabelKey(System.Text.RegularExpressions.Regex.Replace(Expand(value),@"\b(?:v(?:ersion)?\s*)?\d+(?:\.\d+){1,4}(?:[-\s]*(?:beta|rc)\s*\d+)?\b","",System.Text.RegularExpressions.RegexOptions.IgnoreCase));
 private static bool Supports(AutomationElement element,string action)=>action switch{"Invoke"=>element.TryGetCurrentPattern(InvokePattern.Pattern,out _),"Select"=>element.TryGetCurrentPattern(SelectionItemPattern.Pattern,out _),"Toggle"=>element.TryGetCurrentPattern(TogglePattern.Pattern,out _),"SetValue"=>!element.Current.IsPassword&&element.TryGetCurrentPattern(ValuePattern.Pattern,out _),"SetSecretValue"=>element.Current.ControlType==ControlType.Edit,_=>false};
 private static List<(AutomationElement Element,string Title)> Targets(UiStep step,Func<int,bool> allowed)
 {
  var result=new List<(AutomationElement,string)>();foreach(AutomationElement top in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){
   try{if(!allowed(top.Current.ProcessId))continue;foreach(AutomationElement element in top.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.LookupById(step.ControlTypeId)))){
    try{if(!allowed(element.Current.ProcessId)||!element.Current.IsEnabled||element.Current.IsOffscreen||(!Supports(element,step.Action))||(element.Current.IsPassword&&step.Action!="SetSecretValue"))continue;
     result.Add((element,TopWindow(element)?.Current.Name??top.Current.Name));}catch(ElementNotAvailableException){}
   }}catch(ElementNotAvailableException){}
  }return result;
 }
 public static AutomationElement? Find(UiStep step,Func<int,bool> allowed)
 {
  var ranked=new List<(AutomationElement Element,int Rank)>();foreach(var target in Targets(step,allowed)){
   var current=target.Element.Current;var id=step.AutomationId.Length>0&&current.AutomationId==step.AutomationId;var name=step.Name.Length>0&&LabelKey(current.Name)==LabelKey(step.Name);var title=TitleKey(target.Title)==TitleKey(step.WindowTitle);var classMatch=step.ClassName.Length==0||current.ClassName==step.ClassName;
   var rank=id&&name?100+(title?20:0)+(classMatch?5:0):id&&title&&(step.Name.Length==0||current.Name.Length==0)?90+(classMatch?5:0):name&&title&&classMatch?80:0;
   if(rank>0)ranked.Add((target.Element,rank));
  }
  if(ranked.Count==0)return null;var best=ranked.Max(x=>x.Rank);var matches=ranked.Where(x=>x.Rank==best).Select(x=>x.Element).Distinct().ToList();if(matches.Count>1)throw new InvalidDataException("Mehrere passende Elemente gefunden: "+(step.Action=="SetSecretValue"?"Vertrauliches Feld":step.Name)+". Das richtige Ziel muss neu zugeordnet werden.");return matches.Single();
 }
 public static List<RepairTarget> RepairTargets(UiStep step,Func<int,bool> allowed)
 {
  var result=new List<RepairTarget>();var identities=new Dictionary<int,(string Name,string Hash)>();foreach(var target in Targets(step,allowed).Take(80)){
   var current=target.Element.Current;var selector=new UiStep{Action=step.Action,WindowTitle=target.Title,AutomationId=current.AutomationId,Name=current.Name,ClassName=current.ClassName,ControlTypeId=current.ControlType.Id};if(selector.AutomationId.Length==0&&selector.Name.Length==0)continue;try{if(Find(selector,allowed) is not {} resolved||!resolved.Equals(target.Element))continue;}catch(InvalidDataException){continue;}catch(ElementNotAvailableException){continue;}
   if(!identities.TryGetValue(current.ProcessId,out var identity)){BindExternal(selector,current.ProcessId);identity=(selector.ProcessName,selector.ProcessImageSha256);identities[current.ProcessId]=identity;}selector.ProcessName=identity.Name;selector.ProcessImageSha256=identity.Hash;
   result.Add(new(selector,(step.Action=="SetSecretValue"?"Vertrauliches Eingabefeld":current.Name.Length>0?current.Name:"Unbenanntes Element")+" · "+target.Title+" · Kennung: "+current.AutomationId));
  }return result;
 }
 public static string TargetDiagnosis(UiStep step,Func<int,bool> allowed){var windows=new List<string>();foreach(AutomationElement window in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){try{if(allowed(window.Current.ProcessId)&&window.Current.IsEnabled)windows.Add(window.Current.Name);}catch(ElementNotAvailableException){}}return windows.Count==0?"Kein zugängliches Installerfenster gefunden. Prüfe, ob der Installer gestartet ist oder eine Administratorbestätigung offen ist.":"Sichtbare zugeordnete Fenster: "+string.Join(" · ",windows.Distinct().Take(6))+". Das gespeicherte Ziel ist möglicherweise geändert, verdeckt oder noch nicht geöffnet.";}
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
 [DllImport("kernel32.dll",SetLastError=true)]private static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true,EntryPoint="QueryFullProcessImageNameW")]private static extern bool QueryFullProcessImageName(IntPtr process,uint flags,System.Text.StringBuilder path,ref uint size);
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
 public static Task<int> Replay(RecordedProfile profile,string installer,Action<string> log,CancellationToken ct,Func<UiStep,IReadOnlyList<RepairTarget>,CancellationToken,Task<bool>>? repair=null)=>Task.Run(()=>ReplayCore(profile,installer,log,ct,repair),ct);
 private static Task InvokeOnWorker(UiStep step,Func<int,bool> allowed,CancellationToken ct)
 {
  var completed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var worker=new Thread(()=>{try{ct.ThrowIfCancellationRequested();var target=Find(step,allowed)??throw new InvalidDataException("Schaltfläche ist vor dem Auslösen verschwunden: "+step.Name);Apply(target,step);completed.TrySetResult();}catch(Exception e){completed.TrySetException(e);}}){IsBackground=true,Name="NovaForge UIA invocation"};worker.SetApartmentState(ApartmentState.MTA);worker.Start();return completed.Task;
 }
 private static async Task<int> ReplayCore(RecordedProfile profile,string installer,Action<string> log,CancellationToken ct,Func<UiStep,IReadOnlyList<RepairTarget>,CancellationToken,Task<bool>>? repair)
 {
  profile.Validate();if(await Validation.Hash(installer,ct)!=profile.InstallerSha256)throw new InvalidDataException("Installer stimmt nicht mit der Aufzeichnung überein.");
  string? ownedPackage=null;if(profile.PackageSha256.Length>0){if(await FolderMedia.Digest(Path.GetDirectoryName(installer)!,ct)!=profile.PackageSha256)throw new InvalidDataException("Installer-Begleitdateien stimmen nicht mit der Aufnahme überein.");ownedPackage=Path.Combine(Path.GetTempPath(),"NovaForge-Replay-"+Guid.NewGuid().ToString("N"));await FolderMedia.Copy(Path.GetDirectoryName(installer)!,ownedPackage,new Progress<BuildProgress>(),ct);installer=Path.Combine(ownedPackage,Path.GetFileName(installer));}InstallerDirectory=Path.GetDirectoryName(installer)!;
  try {
  var verification=Expand(profile.VerificationFile);if(File.Exists(verification))throw new InvalidDataException("Prüfdatei existiert bereits. Für den Profiltest eine frische Umgebung verwenden.");
  using var process=StartInstaller(installer,profile.InstallerArguments);var allowed=ProcessFilter(process.Id,System.IO.Path.GetExtension(installer).Equals(".msi",StringComparison.OrdinalIgnoreCase));
  var pendingInvokes=new List<Task>();
  try{for(var index=0;index<profile.Steps.Count;index++){var step=profile.Steps[index];var description="Schritt "+(index+1)+" von "+profile.Steps.Count+": "+step.ActionLabel+" · "+(step.Action=="SetSecretValue"?"Vertrauliches Feld":step.Name);log(description);var until=DateTime.UtcNow.AddSeconds(step.TimeoutSeconds);var nextNotice=DateTime.UtcNow.AddSeconds(3);AutomationElement? element=null;while(DateTime.UtcNow<until){ct.ThrowIfCancellationRequested();foreach(var pending in pendingInvokes.Where(x=>x.IsCompleted).ToList()){await pending;pendingInvokes.Remove(pending);}try{element=Find(step,pid=>allowed(pid)||MatchesExternal(step,pid));}catch(ElementNotAvailableException){}catch(InvalidDataException) when(repair!=null){break;}if(element!=null)break;if(DateTime.UtcNow>=nextNotice){log(description+" · Warte auf „"+step.WindowTitle+"“ (noch "+Math.Max(0,(int)(until-DateTime.UtcNow).TotalSeconds)+" s).");nextNotice=DateTime.UtcNow.AddSeconds(5);}await Task.Delay(200,ct);}if(element==null){var diagnosis=TargetDiagnosis(step,pid=>allowed(pid)||MatchesExternal(step,pid));log(description+" · "+diagnosis);if(repair!=null){var candidates=RepairTargets(step,pid=>allowed(pid)||MatchesExternal(step,pid));if(await repair(step,candidates,ct)){profile.Tested=false;profile.TestedPlanSha256="";profile.Validate();index--;continue;}throw new OperationCanceledException("Neu zuordnen abgebrochen. Aufnahme bleibt ungeprüft.");}throw new TimeoutException(description+" · Fenster/Steuerelement nicht gefunden: "+step.WindowTitle+" / "+(step.Action=="SetSecretValue"?"Vertrauliches Feld":step.Name)+". "+diagnosis);}
   if(step.Action=="Invoke"){var invocation=InvokeOnWorker(step,pid=>allowed(pid)||MatchesExternal(step,pid),ct);if(await Task.WhenAny(invocation,Task.Delay(750,ct))==invocation)await invocation;else{pendingInvokes.Add(invocation);log(description+" · Auslösung läuft; nächster Dialog wird erwartet.");}}else Apply(element,step);await Task.Delay(300,ct);}
   await process.WaitForExitAsync(ct);var code=process.ExitCode;if(code is not (0 or 3010))throw new IOException("Installer-Exitcode: "+code);
   await Task.WhenAll(pendingInvokes).WaitAsync(ct);if(!File.Exists(verification))throw new IOException("Installation nicht bestätigt: neue Prüfdatei fehlt.");log("Installation anhand neuer Prüfdatei bestätigt.");return code;
  }catch{try{if(!process.HasExited)process.Kill(true);}catch{}throw;}
  }finally{if(ownedPackage!=null&&FolderMedia.Within(ownedPackage,Path.GetTempPath())){try{Directory.Delete(ownedPackage,true);}catch{}}}
 }
}

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using NovaForge.Core;
namespace NovaForge.Automation;
public static class SandboxSessions
{
 public static string Root=>System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaForge","Beta","RecorderSessions");
 public static bool Available=>File.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsSandbox.exe"));
 public static string Config(string input,string output,bool network)=>new XDocument(new XElement("Configuration",new XElement("Networking",network?"Enable":"Disable"),new XElement("vGPU","Disable"),new XElement("ClipboardRedirection","Disable"),new XElement("AudioInput","Disable"),new XElement("VideoInput","Disable"),new XElement("PrinterRedirection","Disable"),new XElement("MappedFolders",Map(input,@"C:\NovaForgeInput",true),Map(output,@"C:\NovaForgeOutput",false)),new XElement("LogonCommand",new XElement("Command",@"powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\NovaForgeInput\start.ps1")))).ToString();
 private static XElement Map(string host,string guest,bool readOnly)=>new("MappedFolder",new XElement("HostFolder",host),new XElement("SandboxFolder",guest),new XElement("ReadOnly",readOnly));
 public static async Task<string> Prepare(Installer app,string mode,bool elevation,string? recordingName=null)
 {
  if(!Available)throw new InvalidOperationException("Windows Sandbox ist nicht verfügbar. In den Windows-Features aktivieren (Pro/Enterprise). Es wird nichts auf dem Host installiert.");
  var exe=Environment.ProcessPath??"";if(typeof(App).Assembly.GetName().Name!="NovaForge.Beta"||typeof(App).Assembly.Location.Length>0)throw new InvalidOperationException("Recorder-Sitzungen aus der veröffentlichten portablen NovaForge.Beta.exe starten.");
  var dir=System.IO.Path.Combine(Root,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));var input=System.IO.Path.Combine(dir,"Input");var output=System.IO.Path.Combine(dir,"Output");Directory.CreateDirectory(input);Directory.CreateDirectory(output);
  var hash=await Validation.Hash(app.Path);if(app.Sha256.Length>0&&app.Sha256!=hash)throw new InvalidDataException("Installer verändert. Neu zuordnen.");app.Sha256=hash;
  var package=Path.Combine(input,"Package");var installerCopy=await InstallerPackage.Copy(app,package);var file=Path.GetRelativePath(input,installerCopy);await Task.Run(()=>File.Copy(exe,Path.Combine(input,"NovaForge.Beta.exe")));var packageHash=await FolderMedia.Digest(package);
  var session=new RecorderSession{ProgramName=app.Name,Mode=mode,PackageSha256=packageHash,InstallerFileName=file,InstallerSha256=hash,InstallerArguments=app.AutomationMode=="Recorded"?"":app.Arguments,RequiresElevation=true,DisplayName=string.IsNullOrWhiteSpace(recordingName)?app.Name:recordingName.Trim()};
  // Recording uses interactive parameters; silent flags must not be reused automatically.
  if(mode=="Record")session.InstallerArguments="";
  if(mode=="Test"){var profile=await RecordedProfile.Load(app.RecordedProfilePath);profile.Validate();if(profile.PackageSha256.Length>0&&profile.PackageSha256!=packageHash)throw new InvalidDataException("Begleitdateien wurden verändert. Aufnahme mit diesem Paket erneut erstellen.");if(profile.InstallerSha256!=hash)throw new InvalidDataException("Profil gehört zu anderem Installer.");if(string.IsNullOrWhiteSpace(profile.DisplayName))profile.DisplayName=session.DisplayName;if(profile.PackageSha256.Length==0)profile.PackageSha256=packageHash;await profile.Save(System.IO.Path.Combine(input,"profile.json"));session.RequiresElevation=true;}
  await File.WriteAllTextAsync(System.IO.Path.Combine(input,"session.json"),JsonSerializer.Serialize(session,Project.JsonOptions),new UTF8Encoding(true));
  var startup="$ErrorActionPreference='Stop'\n$exe='C:\\NovaForgeInput\\NovaForge.Beta.exe'\n$args='--recorder --session C:\\NovaForgeInput\\session.json'\nStart-Process -FilePath $exe -ArgumentList $args"+(session.RequiresElevation?" -Verb RunAs":"")+"\n";
  await File.WriteAllTextAsync(System.IO.Path.Combine(input,"start.ps1"),startup,new UTF8Encoding(true));await File.WriteAllTextAsync(System.IO.Path.Combine(dir,"NovaForge-Recorder.wsb"),Config(input,output,app.WebInstaller),new UTF8Encoding(true));return dir;
 }
 public static void Launch(string directory)=>Process.Start(new ProcessStartInfo(System.IO.Path.Combine(directory,"NovaForge-Recorder.wsb")){UseShellExecute=true});
 public static bool GuestRunning=>new[]{"WindowsSandbox","WindowsSandboxClient","vmmemWindowsSandbox"}.Any(name=>{var processes=Process.GetProcessesByName(name);try{return processes.Length>0;}finally{foreach(var process in processes)process.Dispose();}});
 public static bool Cleanup(string directory)=>Cleanup(directory,out _);
 public static bool Cleanup(string directory,out string pending)
 {
  pending="";if(GuestRunning){pending="Die Testumgebung wird noch verwendet.";return false;}
  var path=Path.GetFullPath(directory);var root=Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
  if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!string.Equals(Path.GetDirectoryName(path),Path.GetFullPath(Root),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Ungültiger Sitzungsordner.");
  if(!Directory.Exists(path))return true;
  try{
   var files=new List<string>();var folders=new List<string>();
   void Scan(string folder){if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Sitzungsordner enthält Verknüpfungen.");folders.Add(folder);foreach(var item in Directory.EnumerateFileSystemEntries(folder)){var attributes=File.GetAttributes(item);if((attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Sitzungsordner enthält Verknüpfungen.");if((attributes&FileAttributes.Directory)!=0)Scan(item);else files.Add(item);}}
   Scan(path);
   // Keep transfer metadata until binary/package copies have been removed. A locked executable must not destroy recovery data.
   var profile=Path.Combine(path,"Output","profile.json");var session=Path.Combine(path,"Input","session.json");
   var ordered=files.OrderBy(file=>file.Equals(session,StringComparison.OrdinalIgnoreCase)?3:file.Equals(profile,StringComparison.OrdinalIgnoreCase)?2:Path.GetFileName(file).Equals("NovaForge.Beta.exe",StringComparison.OrdinalIgnoreCase)?0:1).ToList();
   var locks=new List<FileStream>();try{foreach(var file in ordered)locks.Add(new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.None));}finally{foreach(var handle in locks)handle.Dispose();}
   foreach(var file in ordered){var attributes=File.GetAttributes(file);if((attributes&FileAttributes.ReadOnly)!=0)File.SetAttributes(file,attributes&~FileAttributes.ReadOnly);File.Delete(file);}
   foreach(var folder in folders.OrderByDescending(x=>x.Length))Directory.Delete(folder,false);
   return true;
  }catch(Exception e) when(e is IOException or UnauthorizedAccessException){pending="Temporäre Sitzungsdateien sind noch gesperrt. Bereinigung wird automatisch wiederholt; gespeicherte Aufnahmen bleiben erhalten.";return false;}
 }
 public static async Task Activate(){var command="Enable-WindowsOptionalFeature -Online -FeatureName 'Containers-DisposableClientVM' -All -NoRestart -ErrorAction Stop";var encoded=Convert.ToBase64String(Encoding.Unicode.GetBytes("try { "+command+"; exit 0 } catch { exit 1 }"));using var process=Process.Start(new ProcessStartInfo(IsoBuilder.PowerShell){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden,Arguments="-NoProfile -NonInteractive -EncodedCommand "+encoded})??throw new IOException("Aktivierung konnte nicht gestartet werden.");await process.WaitForExitAsync();if(process.ExitCode!=0)throw new InvalidOperationException("Windows Sandbox konnte nicht aktiviert werden. Edition, Virtualisierung und Windows-Feature-Verfügbarkeit prüfen.");}
}

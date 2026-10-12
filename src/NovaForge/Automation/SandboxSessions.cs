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
 public static async Task<string> Prepare(Installer app,string mode,bool elevation)
 {
  if(!Available)throw new InvalidOperationException("Windows Sandbox ist nicht verfügbar. In den Windows-Features aktivieren (Pro/Enterprise). Es wird nichts auf dem Host installiert.");
  var exe=Environment.ProcessPath??"";if(typeof(App).Assembly.GetName().Name!="NovaForge.Beta"||typeof(App).Assembly.Location.Length>0)throw new InvalidOperationException("Recorder-Sitzungen aus der veröffentlichten portablen NovaForge.Beta.exe starten.");
  var dir=System.IO.Path.Combine(Root,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));var input=System.IO.Path.Combine(dir,"Input");var output=System.IO.Path.Combine(dir,"Output");Directory.CreateDirectory(input);Directory.CreateDirectory(output);
  var hash=await Validation.Hash(app.Path);if(app.Sha256.Length>0&&app.Sha256!=hash)throw new InvalidDataException("Installer verändert. Neu zuordnen.");app.Sha256=hash;
  var file="installer"+System.IO.Path.GetExtension(app.Path);await Task.Run(()=>{File.Copy(exe,System.IO.Path.Combine(input,"NovaForge.Beta.exe"));File.Copy(app.Path,System.IO.Path.Combine(input,file));});
  var session=new RecorderSession{Mode=mode,InstallerFileName=file,InstallerSha256=hash,InstallerArguments=app.AutomationMode=="Recorded"?"":app.Arguments,RequiresElevation=elevation,DisplayName=app.Name};
  // Recording uses interactive parameters; silent flags must not be reused automatically.
  if(mode=="Record")session.InstallerArguments="";
  if(mode=="Test"){var profile=await RecordedProfile.Load(app.RecordedProfilePath);profile.Validate();if(profile.InstallerSha256!=hash)throw new InvalidDataException("Profil gehört zu anderem Installer.");await profile.Save(System.IO.Path.Combine(input,"profile.json"));session.RequiresElevation=profile.RequiresElevation;}
  await File.WriteAllTextAsync(System.IO.Path.Combine(input,"session.json"),JsonSerializer.Serialize(session,Project.JsonOptions),new UTF8Encoding(true));
  var startup="$ErrorActionPreference='Stop'\n$exe='C:\\NovaForgeInput\\NovaForge.Beta.exe'\n$args='--recorder --session C:\\NovaForgeInput\\session.json'\nStart-Process -FilePath $exe -ArgumentList $args"+(session.RequiresElevation?" -Verb RunAs":"")+"\n";
  await File.WriteAllTextAsync(System.IO.Path.Combine(input,"start.ps1"),startup,new UTF8Encoding(true));await File.WriteAllTextAsync(System.IO.Path.Combine(dir,"NovaForge-Recorder.wsb"),Config(input,output,app.WebInstaller),new UTF8Encoding(true));return dir;
 }
 public static void Launch(string directory)=>Process.Start(new ProcessStartInfo(System.IO.Path.Combine(directory,"NovaForge-Recorder.wsb")){UseShellExecute=true});
}

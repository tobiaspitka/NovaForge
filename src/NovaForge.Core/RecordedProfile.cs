using System.Text.Json;
using System.Text.Json.Serialization;
namespace NovaForge.Core;
public sealed class UiStep : Observable
{
 public string Action {get;set;}="Invoke";
 public string WindowTitle {get;set;}="";
 public string AutomationId {get;set;}="";
 public string Name {get;set;}="";
 public string ClassName {get;set;}="";
 public int ControlTypeId {get;set;}
 public string Value {get;set;}="";
 public int TimeoutSeconds {get;set;}=45;
 public string ProcessName {get;set;}="";
 public string ProcessImageSha256 {get;set;}="";
 [JsonIgnore] public string ActionLabel=>Action switch{"Invoke"=>"Schaltfläche betätigen","Select"=>"Eintrag auswählen","Toggle"=>Value=="On"?"Häkchen setzen":"Häkchen entfernen","SetValue"=>"Text eingeben","SetSecretValue"=>"Vertraulichen Wert eingeben",_=>Action};
 public override string ToString()=>$"{ActionLabel} · {(Action=="SetSecretValue"?"Vertrauliches Feld":Name)} · {WindowTitle}";
}
public sealed class RecordedProfile
{
 public int Version {get;set;}=1;
 public string DisplayName {get;set;}="";
 public string InstallerSha256 {get;set;}="";
 public string InstallerArguments {get;set;}="";
 public string PackageSha256 {get;set;}="";
 public bool RequiresElevation {get;set;}
 public List<UiStep> Steps {get;set;}=[];
 public string VerificationFile {get;set;}="";
 public bool Tested {get;set;}
 public string TestedPlanSha256 {get;set;}="";
 public DateTimeOffset? TestedAt {get;set;}
 public string PlanHash()
 {
  var bytes=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{Version,InstallerSha256,InstallerArguments,PackageSha256,RequiresElevation,Steps,VerificationFile},Project.JsonOptions));
  return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
 }
 public void Validate(bool requireTest=false,bool requireVerification=true)
 {
  if(Version!=1||InstallerSha256.Length!=64||!InstallerSha256.All(Uri.IsHexDigit))throw new InvalidDataException("Ungültige Recorder-Version oder Installer-Prüfsumme.");
  if(PackageSha256.Length>0&&(PackageSha256.Length!=64||!PackageSha256.All(Uri.IsHexDigit)))throw new InvalidDataException("Installer-Paket-Prüfsumme ungültig.");
  if(Steps.Count>300||((requireTest||requireVerification)&&Steps.Count<1))throw new InvalidDataException("Noch keine Schritte aufgenommen. Im Recorder die Aufnahme starten und den Installer bedienen.");
  foreach(var s in Steps){if(s.Action is not ("Invoke" or "Toggle" or "Select" or "SetValue" or "SetSecretValue"))throw new InvalidDataException("Unbekannte Recorder-Aktion.");if(s.Action=="Toggle"&&s.Value is not ("On" or "Off"))throw new InvalidDataException("Ungültiger Checkbox-Zustand.");if(string.IsNullOrWhiteSpace(s.WindowTitle)||(string.IsNullOrWhiteSpace(s.AutomationId)&&string.IsNullOrWhiteSpace(s.Name))||s.ControlTypeId is <50000 or >50050||s.TimeoutSeconds is <1 or >600)throw new InvalidDataException("Unvollständiger Steuerelement-Selektor.");if(s.Value.Length>4096||s.Value.Contains('\0'))throw new InvalidDataException("Recorder-Wert ungültig.");if(s.ProcessImageSha256.Length>0&&(s.ProcessImageSha256.Length!=64||!s.ProcessImageSha256.All(Uri.IsHexDigit)||string.IsNullOrWhiteSpace(s.ProcessName)))throw new InvalidDataException("Zusatzfenster-Prozesskennung ungültig.");}
  if((requireTest||requireVerification)&&string.IsNullOrWhiteSpace(VerificationFile))throw new InvalidDataException("Aufnahme ist ein Entwurf. In der Sandbox eine neu installierte Datei als Prüfnachweis auswählen.");
  if(VerificationFile.Length>0&&(!new[]{"{ProgramFiles}","{ProgramFilesX86}","{LocalAppData}","{UserProfile}","{ProgramData}","{Windows}"}.Any(VerificationFile.StartsWith)||VerificationFile.Contains("..")))throw new InvalidDataException("Eine installierte Prüfdatei unter einem unterstützten Zielordner auswählen.");
  if(requireTest&&(!Tested||TestedPlanSha256!=PlanHash()))throw new InvalidDataException("Dieses Recorder-Profil muss unverändert in einer frischen Sandbox getestet werden.");
 }
 public static async Task<RecordedProfile> Load(string path){if(new FileInfo(path).Length>2_000_000)throw new InvalidDataException("Recorder-Profil zu groß.");return JsonSerializer.Deserialize<RecordedProfile>(await File.ReadAllTextAsync(path),Project.JsonOptions)??throw new InvalidDataException("Recorder-Profil leer.");}
 public async Task Save(string path)=>await File.WriteAllTextAsync(path,JsonSerializer.Serialize(this,Project.JsonOptions));
}
public sealed class RecorderSession
{
 public string ProgramName {get;set;}="";
 public string Mode {get;set;}="Record";
 public string InstallerFileName {get;set;}="";
 public string InstallerSha256 {get;set;}="";
 public string InstallerArguments {get;set;}="";
 public bool RequiresElevation {get;set;}
 public string DisplayName {get;set;}="";
 public string PackageSha256 {get;set;}="";
}

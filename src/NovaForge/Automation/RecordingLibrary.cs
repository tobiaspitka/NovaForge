using NovaForge.Core;
namespace NovaForge.Automation;
public sealed record RecordingEntry(string Name,string Path,string InstallerSha256,bool Tested,bool HasSecrets){public override string ToString()=>Name+(Tested?" · geprüft":" · Test erforderlich")+(HasSecrets?" · vertraulich":"");}
public static class RecordingLibrary
{
 public static string Root=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaForge","Beta","Recordings");
 public static async Task<RecordingEntry> Store(string source,string name,string? root=null)
 {
  var profile=await RecordedProfile.Load(source);profile.Validate(false,false);var tested=profile.Tested&&profile.TestedPlanSha256==profile.PlanHash();if(tested)profile.Validate(true);
  name=string.IsNullOrWhiteSpace(profile.DisplayName)?name:profile.DisplayName.Trim();if(string.IsNullOrWhiteSpace(name)||name.Length>120)throw new InvalidDataException("Aufnahmename muss 1–120 Zeichen lang sein.");profile.DisplayName=name;var directory=Path.Combine(root??Root,profile.InstallerSha256);Directory.CreateDirectory(directory);var path=Path.Combine(directory,profile.PlanHash()+(tested?"-tested":"-recorded")+".json");
  var pending=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{await profile.Save(pending);File.Move(pending,path,true);}finally{if(File.Exists(pending))File.Delete(pending);}
  await File.WriteAllTextAsync(Path.ChangeExtension(path,"name"),name);return new(name,path,profile.InstallerSha256,tested,profile.Steps.Any(x=>x.Action=="SetSecretValue"));
 }
 public static async Task<List<RecordingEntry>> List(string? root=null){var dir=root??Root;var result=new List<RecordingEntry>();if(!Directory.Exists(dir))return result;foreach(var path in Directory.EnumerateFiles(dir,"*.json",SearchOption.AllDirectories)){try{var p=await RecordedProfile.Load(path);p.Validate(false,false);var nameFile=Path.ChangeExtension(path,"name");result.Add(new(File.Exists(nameFile)?await File.ReadAllTextAsync(nameFile):Path.GetFileNameWithoutExtension(path),path,p.InstallerSha256,p.Tested&&p.TestedPlanSha256==p.PlanHash(),p.Steps.Any(x=>x.Action=="SetSecretValue")));}catch(IOException){}catch(System.Text.Json.JsonException){}catch(InvalidDataException){}}return result.OrderByDescending(x=>x.Tested).ThenBy(x=>x.Name).ToList();}
 public static async Task Attach(Installer app,RecordingEntry entry){var hash=await Validation.Hash(app.Path);if(hash!=entry.InstallerSha256)throw new InvalidDataException("Aufzeichnung gehört zu einer anderen Installer-Version.");var profile=await RecordedProfile.Load(entry.Path);profile.Validate(false,false);if(profile.InstallerSha256!=hash)throw new InvalidDataException("Aufzeichnung verändert.");app.Sha256=hash;app.RecordedProfilePath=entry.Path;app.RecordedProfileName=entry.Name;app.RecordedProfileSha256=await Validation.Hash(entry.Path);app.AutomationMode="Recorded";app.Context="User";app.Verified=false;app.ApplyRecordingState(profile);}
 public static void Delete(RecordingEntry entry,string? root=null)
 {
  var library=Path.GetFullPath(root??Root);var path=Path.GetFullPath(entry.Path);var parent=Path.GetDirectoryName(path)!;
  if(entry.InstallerSha256.Length!=64||!entry.InstallerSha256.All(Uri.IsHexDigit)||!parent.Equals(Path.Combine(library,entry.InstallerSha256),StringComparison.OrdinalIgnoreCase)||!Path.GetExtension(path).Equals(".json",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Nur Aufnahmen innerhalb der Bibliothek können gelöscht werden.");
  foreach(var item in new[]{library,parent,path,Path.ChangeExtension(path,"name")})if((File.Exists(item)||Directory.Exists(item))&&(File.GetAttributes(item)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Verknüpfte Aufnahmen können nicht gelöscht werden.");
  File.Delete(path);File.Delete(Path.ChangeExtension(path,"name"));
 }
}

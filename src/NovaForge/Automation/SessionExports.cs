using NovaForge.Core;
namespace NovaForge.Automation;
public static class SessionExports
{
 public static async Task<string> Save(RecordedProfile profile,string output){profile.DisplayName=profile.DisplayName.Trim();if(profile.DisplayName.Length is <1 or >120)throw new InvalidDataException("Aufnahmename muss 1–120 Zeichen lang sein.");profile.Validate(false,false);Directory.CreateDirectory(output);var final=Path.Combine(output,"profile.json");var pending=Path.Combine(output,"profile-"+Guid.NewGuid().ToString("N")+".tmp");try{await profile.Save(pending);File.Move(pending,final,true);}finally{if(File.Exists(pending))File.Delete(pending);}return await NovaForge.Core.Validation.Hash(final);}
 public static async Task<bool> Accepted(string output,string hash){var ack=Path.Combine(output,"accepted.txt");return File.Exists(ack)&&(await File.ReadAllTextAsync(ack)).Trim()==hash;}
}

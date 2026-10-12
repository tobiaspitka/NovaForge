namespace NovaForge.Core;
public static class InstallerPackage
{
 public static IEnumerable<string> Files(Installer app)=>string.IsNullOrWhiteSpace(app.CompanionDirectory)?new[]{app.Path}:FolderMedia.Files(app.CompanionDirectory);
 public static async Task<string> Copy(Installer app,string destination,CancellationToken ct=default){Directory.CreateDirectory(destination);if(app.CompanionDirectory.Length>0){if(!Path.GetDirectoryName(Path.GetFullPath(app.Path))!.TrimEnd('\\').Equals(Path.GetFullPath(app.CompanionDirectory).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Der Installer muss direkt im Begleitordner liegen.");await FolderMedia.Copy(app.CompanionDirectory,destination,new Progress<BuildProgress>(),ct);}else{var target=Path.Combine(destination,Path.GetFileName(app.Path));await using(var input=File.OpenRead(app.Path))await using(var output=new FileStream(target,FileMode.CreateNew)){await input.CopyToAsync(output,ct);}if(await Validation.Hash(app.Path,ct)!=await Validation.Hash(target,ct))throw new IOException("Installer-Kopie fehlerhaft.");}return Path.Combine(destination,Path.GetFileName(app.Path));}
}

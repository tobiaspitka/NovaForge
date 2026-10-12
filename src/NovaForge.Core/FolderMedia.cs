using System.Text;
using System.Security.Cryptography;
namespace NovaForge.Core;
public static class FolderMedia
{
 public static bool Within(string path,string root){var p=Path.GetFullPath(path);var r=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;return p.Equals(r.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)||p.StartsWith(r,StringComparison.OrdinalIgnoreCase);}
 public static List<string> Files(string root)
 {
  root=Path.GetFullPath(root);if(!Directory.Exists(root))throw new DirectoryNotFoundException("USB-/Medienordner fehlt.");var files=new List<string>();
  void Walk(string dir){if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Verknüpfte Ordner sind als Medienquelle nicht unterstützt: "+dir);foreach(var entry in Directory.EnumerateFileSystemEntries(dir)){var name=Path.GetFileName(entry);if(dir==root&&new[]{"System Volume Information","$RECYCLE.BIN"}.Contains(name,StringComparer.OrdinalIgnoreCase))continue;var attr=File.GetAttributes(entry);if((attr&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Verknüpfte Dateien sind als Medienquelle nicht unterstützt: "+entry);if((attr&FileAttributes.Directory)!=0)Walk(entry);else files.Add(entry);}}
  Walk(root);return files.OrderBy(x=>Path.GetRelativePath(root,x),StringComparer.OrdinalIgnoreCase).ToList();
 }
 public static async Task<string> Digest(string root,CancellationToken ct=default){var records=new StringBuilder();foreach(var file in Files(root)){ct.ThrowIfCancellationRequested();records.Append(Path.GetRelativePath(root,file).Replace('\\','/')).Append(':').Append(await Validation.Hash(file,ct)).Append('\n');}return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(records.ToString())));}
 public static async Task Copy(string source,string destination,IProgress<BuildProgress> progress,CancellationToken ct)
 {
  source=Path.GetFullPath(source);destination=Path.GetFullPath(destination);if(Within(destination,source)||Within(source,destination))throw new InvalidDataException("Arbeitskopie und Medienquelle dürfen sich nicht überlappen.");var files=Files(source);long total=files.Sum(x=>new FileInfo(x).Length),done=0;Directory.CreateDirectory(destination);
  foreach(var file in files){ct.ThrowIfCancellationRequested();var target=Path.Combine(destination,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);await using(var input=File.OpenRead(file))await using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write)){var buffer=new byte[1048576];int n;while((n=await input.ReadAsync(buffer,ct))>0){await output.WriteAsync(buffer.AsMemory(0,n),ct);done+=n;progress.Report(new("USB-/Medienordner kopieren",total==0?1:(double)done/total));}}if(await Validation.Hash(file,ct)!=await Validation.Hash(target,ct))throw new IOException("Quelldatei verändert oder Kopie fehlerhaft: "+file);}
 }
}

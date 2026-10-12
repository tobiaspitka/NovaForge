using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace NovaForge.Core;
public record BuildProgress(string Activity, double? Fraction=null);
public record BuildResult(string Path,long Size,string Sha256,string LogPath,List<Installer> Programs);
public sealed class IsoBuilder
{
    public static string PowerShell => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","WindowsPowerShell","v1.0","powershell.exe");
    public static string FindOscdimg()
    {
        foreach(var root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)})
        {var path=System.IO.Path.Combine(root,"Windows Kits","10","Assessment and Deployment Kit","Deployment Tools","amd64","Oscdimg","oscdimg.exe");if(File.Exists(path))return path;}
        foreach(var dir in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')) {try{var path=System.IO.Path.Combine(dir,"oscdimg.exe");if(File.Exists(path))return path;}catch{}}
        return "";
    }
    public static string Resource(string name)
    {
        var asm=typeof(IsoBuilder).Assembly;var resource=asm.GetManifestResourceNames().Single(x=>x.EndsWith("."+name,StringComparison.Ordinal));
        using var s=asm.GetManifestResourceStream(resource)!;using var r=new StreamReader(s);return r.ReadToEnd();
    }
    // Windows command line quoting for the ShellExecute elevation boundary.
    public static string Quote(string value)
    {
        var b=new StringBuilder("\"");int slashes=0;
        foreach(var c in value){if(c=='\\'){slashes++;continue;}if(c=='\"'){b.Append('\\',slashes*2+1);b.Append(c);}else{b.Append('\\',slashes);b.Append(c);}slashes=0;}
        b.Append('\\',slashes*2);return b.Append('"').ToString();
    }
    private static async Task Run(string exe,IEnumerable<string> args,Action<string> log,CancellationToken ct)
    {
        var info=new ProcessStartInfo(exe){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        foreach(var arg in args)info.ArgumentList.Add(arg);
        using var p=Process.Start(info)??throw new IOException("Prozess konnte nicht gestartet werden.");
        async Task Read(StreamReader reader) {while(await reader.ReadLineAsync() is {} line)log(line);}
        var reads=Task.WhenAll(Read(p.StandardOutput),Read(p.StandardError));
        try {await p.WaitForExitAsync(ct);await reads;ct.ThrowIfCancellationRequested();if(p.ExitCode!=0)throw new IOException($"{System.IO.Path.GetFileName(exe)}: Exitcode {p.ExitCode}. Siehe Protokoll.");}
        catch {if(!p.HasExited){p.Kill(true);await p.WaitForExitAsync();}await reads;throw;}
    }
    private static async Task ElevatedHelper(string scriptName,string activity,IEnumerable<string> inputArgs,string workspace,IProgress<BuildProgress> progress,Action<string> log,CancellationToken ct)
    {
        var script=System.IO.Path.Combine(workspace,scriptName);ct.ThrowIfCancellationRequested();
        var status=script+".progress";var cancel=script+".cancel";var result=script+".result";
        var args=inputArgs.Concat(new[]{"-ProgressFile",status,"-CancelFile",cancel,"-ResultFile",result}).ToArray();
        string Literal(string s)=>"'"+s.Replace("'","''")+"'";
        var command="& {\n"+Resource(scriptName)+"\n} "+string.Join(" ",args.Select((x,i)=>i%2==0?x:Literal(x)));
        var encoded=Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
        if(encoded.Length>30000)throw new IOException("Die Pfade sind für den Windows-Mount-Hilfsprozess zu lang.");
        // Embedded script travels inside the process command line, not through an editable elevated script file.
        var info=new ProcessStartInfo(PowerShell){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden,Arguments="-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand "+encoded};
        log("Windows-Mount/Kopie: Administratorfreigabe nur für diesen Hilfsprozess.");
        using var p=Process.Start(info)??throw new IOException("Mount-Hilfsprozess konnte nicht gestartet werden.");
        while(!p.HasExited)
        {
            if(ct.IsCancellationRequested)await File.WriteAllTextAsync(cancel,"cancel");
            try{if(File.Exists(status)){var parts=(await File.ReadAllTextAsync(status)).Split('/');if(parts.Length==2&&double.TryParse(parts[0],out var bytes)&&double.TryParse(parts[1],out var total)&&total>0)progress.Report(new(activity,bytes/total));}}catch(IOException){}
            await Task.Delay(250);
        }
        ct.ThrowIfCancellationRequested();
        var message=File.Exists(result)?await File.ReadAllTextAsync(result):"Der Hilfsprozess hat kein Ergebnis geliefert.";
        if(p.ExitCode!=0||message!="OK")throw new IOException(message);
    }
    public static void CheckMedia(string media,bool allowExistingCustomization=false)
    {
        foreach(var file in new[]{"setup.exe","sources/boot.wim","efi/boot/bootx64.efi"})if(!File.Exists(System.IO.Path.Combine(media,file)))throw new InvalidDataException("Windows-x64-Setup-Datei fehlt: "+file);
        if(!new[]{"install.wim","install.esd","install.swm"}.Any(x=>File.Exists(System.IO.Path.Combine(media,"sources",x))))throw new InvalidDataException("Windows-Installationsabbild fehlt.");
        CheckWimSet(media);
        var conflicts=Directory.EnumerateFiles(media,"*",SearchOption.AllDirectories).Where(x=>new[]{"autounattend.xml","unattend.xml","setupcomplete.cmd","errorhandler.cmd"}.Contains(System.IO.Path.GetFileName(x).ToLowerInvariant())).ToList();
        if(Directory.Exists(System.IO.Path.Combine(media,"sources","$OEM$")))conflicts.Add("sources/$OEM$");
        if(conflicts.Count>0&&!allowExistingCustomization)throw new InvalidDataException("Vorhandene Anpassung erkannt. Automatische Zusammenführung wird nicht durchgeführt: "+string.Join(", ",conflicts.Select(x=>System.IO.Path.GetRelativePath(media,x))));
        if(!File.Exists(System.IO.Path.Combine(media,"boot","etfsboot.com"))&&!File.Exists(System.IO.Path.Combine(media,"efi","microsoft","boot","efisys.bin")))throw new InvalidDataException("Keine unterstützten BIOS-/UEFI-Bootdaten vorhanden.");
    }
    public static void CheckWimSet(string media)
    {
        (Guid Id,ushort Part,ushort Total,uint Images) Header(string path)
        {
            using var stream=File.OpenRead(path);if(stream.Length<208)throw new InvalidDataException("Abbild ist zu klein / beschädigt: "+path);var bytes=new byte[208];stream.ReadExactly(bytes);
            if(!bytes.Take(8).SequenceEqual(new byte[]{77,83,87,73,77,0,0,0})||BitConverter.ToUInt32(bytes,8)<208)throw new InvalidDataException("Ungültiges WIM/ESD/SWM-Abbild: "+path);
            return (new Guid(bytes.AsSpan(24,16)),BitConverter.ToUInt16(bytes,40),BitConverter.ToUInt16(bytes,42),BitConverter.ToUInt32(bytes,44));
        }
        var boot=Header(System.IO.Path.Combine(media,"sources","boot.wim"));if(boot.Images==0)throw new InvalidDataException("boot.wim enthält keine Abbilder.");
        var sources=System.IO.Path.Combine(media,"sources");
        foreach(var name in new[]{"install.wim","install.esd","install.swm"})
        {
            var file=System.IO.Path.Combine(sources,name);if(!File.Exists(file))continue;var first=Header(file);
            if(first.Images==0||first.Part!=1||first.Total<1)throw new InvalidDataException("Ungültiges Installationsabbild: "+name);
            if(name!="install.swm")continue;
            var parts=Directory.GetFiles(sources,"install*.swm").Select(Header).OrderBy(x=>x.Part).ToArray();
            if(parts.Length!=first.Total||parts.Where((x,i)=>x.Id!=first.Id||x.Total!=first.Total||x.Part!=i+1).Any())throw new InvalidDataException("Geteiltes install.swm ist unvollständig oder seine Teile passen nicht zusammen.");
        }
    }
    public static string AnswerFile(SetupOptions? options=null)=>UnattendBuilder.Build(options??new());
    public static string BootArgument(string media)
    {
        var bios=System.IO.Path.Combine(media,"boot","etfsboot.com");var uefi=System.IO.Path.Combine(media,"efi","microsoft","boot","efisys.bin");
        var entries=new List<string>();if(File.Exists(bios))entries.Add("p0,e,b"+bios);if(File.Exists(uefi))entries.Add("pEF,e,b"+uefi);
        if(entries.Count==0)throw new InvalidDataException("Bootdaten fehlen.");return "-bootdata:"+entries.Count+"#"+string.Join("#",entries);
    }
    public async Task<BuildResult> Build(Project p,IProgress<BuildProgress> progress,CancellationToken ct)
    {
        var ordered=Validation.Ordered(p);var iso=System.IO.Path.GetFullPath(p.IsoPath);var output=System.IO.Path.GetFullPath(p.OutputPath);
        var folder=p.SourceKind=="Folder";var customize=ordered.Count>0||p.Setup.HasCustomization;
        if(folder){if(!Directory.Exists(iso))throw new InvalidDataException("Vorhandenen USB-/Medienordner auswählen.");CheckMedia(iso,!customize);if(FolderMedia.Within(output,iso)||FolderMedia.Within(p.WorkDirectory,iso))throw new InvalidDataException("Ausgabe und Arbeitsverzeichnis außerhalb der USB-/Medienquelle wählen.");}
        else if(!File.Exists(iso)||!iso.EndsWith(".iso",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Eine vorhandene Windows-ISO auswählen.");
        if(string.Equals(iso,output,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Quelle und Ausgabe müssen unterschiedlich sein.");
        if(File.Exists(output))throw new IOException("Ausgabedatei existiert bereits. Bitte einen neuen Namen wählen.");
        if(!output.EndsWith(".iso",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Ausgabedatei muss die Endung .iso besitzen.");
        if(!File.Exists(p.OscdimgPath)||!System.IO.Path.GetFileName(p.OscdimgPath).Equals("oscdimg.exe",StringComparison.OrdinalIgnoreCase))throw new FileNotFoundException("Microsoft oscdimg.exe aus den ADK Deployment Tools auswählen.");
        var workRoot=System.IO.Path.GetFullPath(p.WorkDirectory);Directory.CreateDirectory(workRoot);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
        var payloadSize=ordered.Sum(x=>new FileInfo(x.Path).Length);var sourceSize=folder?FolderMedia.Files(iso).Sum(x=>new FileInfo(x).Length):new FileInfo(iso).Length;
        var workDrive=new DriveInfo(System.IO.Path.GetPathRoot(workRoot)!);var outputDrive=new DriveInfo(System.IO.Path.GetPathRoot(output)!);
        var sameDrive=workDrive.Name.Equals(outputDrive.Name,StringComparison.OrdinalIgnoreCase);
        if(workDrive.AvailableFreeSpace<(sourceSize+payloadSize+536870912)*(sameDrive?2:1)||outputDrive.AvailableFreeSpace<sourceSize+payloadSize+536870912)throw new IOException("Nicht genügend freier Speicher. Benötigt: ISO-Kopie, Installer und neue ISO plus Reserve.");
        var workspace=System.IO.Path.Combine(workRoot,"NovaForge-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(workspace);
        var media=System.IO.Path.Combine(workspace,"media");Directory.CreateDirectory(media);
        var logPath=output+".log";using var logWriter=new StreamWriter(logPath,false,new UTF8Encoding(true)){AutoFlush=true};var sync=new object();
        void Log(string line){lock(sync)logWriter.WriteLine(DateTimeOffset.Now.ToString("o")+" "+line);progress.Report(new(line));}
        var partial=output+".partial-"+Guid.NewGuid().ToString("N")+".iso";bool ownsOutput=false;
        try
        {
            Log("Quelle: "+iso);progress.Report(new("Quell-ISO prüfen / SHA-256",null));
            var sourceHash=folder?await FolderMedia.Digest(iso,ct):await Validation.Hash(iso,ct);if(p.IsoSha256.Length>0&&!p.IsoSha256.Equals(sourceHash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Medienquelle wurde seit dem Speichern verändert. Quelle neu zuordnen.");p.IsoSha256=sourceHash;
            if(folder){await FolderMedia.Copy(iso,media,progress,ct);if(await FolderMedia.Digest(iso,ct)!=sourceHash||await FolderMedia.Digest(media,ct)!=sourceHash)throw new IOException("Medienquelle während der Kopie verändert.");var dismInfo=new StringBuilder();await Run(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"dism.exe"),new[]{"/English","/Get-WimInfo","/WimFile:"+System.IO.Path.Combine(media,"sources","boot.wim"),"/Index:1","/LogPath:"+System.IO.Path.Combine(workspace,"media-dism.log")},line=>{Log(line);dismInfo.AppendLine(line);},ct);if(!System.Text.RegularExpressions.Regex.IsMatch(dismInfo.ToString(),@"Architecture\s*:\s*x64")||!System.Text.RegularExpressions.Regex.IsMatch(dismInfo.ToString(),@"Version\s*:\s*10\."))throw new InvalidDataException("Das Boot-Abbild ist kein Windows-10/11-x64-Abbild.");}
            else await ElevatedHelper("MountCopy.ps1","ISO-Inhalt kopieren",new[]{"-Iso",iso,"-Destination",media},workspace,progress,Log,ct);
            CheckMedia(media,!customize);Log("Windows-Setup und Anpassungskonflikte geprüft.");
            if(customize){
            var payload=System.IO.Path.Combine(media,"sources","$OEM$","$$","NovaForge");Directory.CreateDirectory(System.IO.Path.Combine(payload,"Installers"));
            if(ordered.Any(x=>x.AutomationMode=="Recorded")){
                if(!File.Exists(p.ReplayHostPath)||!p.ReplayHostPortable)throw new InvalidDataException("Recorder-ISOs aus der portablen NovaForge.Beta.exe erstellen.");
                File.Copy(p.ReplayHostPath,System.IO.Path.Combine(payload,"NovaForge.Beta.exe"));Directory.CreateDirectory(System.IO.Path.Combine(payload,"Profiles"));
                foreach(var app in ordered.Where(x=>x.AutomationMode=="Recorded")){var hash=await Validation.Hash(app.RecordedProfilePath,ct);if(app.RecordedProfileSha256.Length>0&&app.RecordedProfileSha256!=hash)throw new InvalidDataException("Recorder-Profil verändert: "+app.Name);app.RecordedProfileSha256=hash;File.Copy(app.RecordedProfilePath,System.IO.Path.Combine(payload,"Profiles",app.Id+".json"));}
            }
            for(var i=0;i<ordered.Count;i++)
            {
                var app=ordered[i];progress.Report(new("Installer integrieren: "+app.Name,(double)i/ordered.Count));
                var hash=await Validation.Hash(app.Path,ct);if(app.Sha256.Length>0&&!app.Sha256.Equals(hash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Installer verändert: "+app.Name+". Datei neu zuordnen.");app.Sha256=hash;
                var dest=System.IO.Path.Combine(payload,"Installers",app.Id+System.IO.Path.GetExtension(app.Path));
                await using(var input=File.OpenRead(app.Path))await using(var target=File.Create(dest))await input.CopyToAsync(target,ct);
                if(await Validation.Hash(dest,ct)!=hash)throw new IOException("Kopierprüfung fehlgeschlagen: "+app.Name);
                Log("Integriert (nicht ausgeführt): "+app.Name+" SHA256="+hash);
            }
            var manifest=new{Version=1,BuildId=Guid.NewGuid().ToString("N"),SourceSha256=sourceHash,Programs=ordered.Select(x=>new{x.Id,x.Name,Path=System.IO.Path.GetFileName(x.Path),x.Type,x.Arguments,x.Context,x.TimeoutSeconds,x.Sha256,x.RebootPolicy,x.AutomationMode,x.RecordedProfileSha256,DependencyIds=x.DependencyIds.ToArray(),AllowedCodes=Validation.ParseCodes(x.AllowedExitCodes)})};
            await File.WriteAllTextAsync(System.IO.Path.Combine(payload,"manifest.json"),JsonSerializer.Serialize(manifest,Project.JsonOptions),new UTF8Encoding(true),ct);
            foreach(var script in new[]{"Bootstrap.ps1","Runner.ps1"})await File.WriteAllTextAsync(System.IO.Path.Combine(payload,script),Resource(script),new UTF8Encoding(true),ct);
            await File.WriteAllTextAsync(System.IO.Path.Combine(media,"autounattend.xml"),AnswerFile(p.Setup),new UTF8Encoding(true),ct);
            Log("Setup-Optionen integriert. Kennwörter sind nicht Bestandteil des Buildprotokolls.");
            }
            var boot=BootArgument(media);Log("oscdimg Bootkonfiguration: "+boot);progress.Report(new("Bootfähige ISO erstellen",null));
            await Run(p.OscdimgPath,new[]{"-m","-o","-u2","-udfver102",boot,media,partial},Log,ct);
            if(!File.Exists(partial)||new FileInfo(partial).Length<sourceSize/2)throw new IOException("oscdimg-Ausgabe fehlt oder ist ungewöhnlich klein.");
            progress.Report(new("Ausgabe prüfen / SHA-256",null));var checksum=await Validation.Hash(partial,ct);
            // Verify readability and injected payload through Windows' ISO filesystem, without copying again.
            await ElevatedHelper("VerifyIso.ps1","ISO-Dateien prüfen",new[]{"-Iso",partial,"-Reference",media},workspace,progress,Log,ct);
            Log("ISO lesbar; alle Dateien bytegetreu und El-Torito-Bootkatalog geprüft.");
            ct.ThrowIfCancellationRequested();File.Move(partial,output,false);ownsOutput=true;
            await File.WriteAllTextAsync(output+".sha256",checksum+"  "+System.IO.Path.GetFileName(output)+Environment.NewLine,ct);
            Log("Fertig: "+output+" SHA256="+checksum);
            return new(output,new FileInfo(output).Length,checksum,logPath,ordered);
        }
        catch{if(ownsOutput){File.Delete(output);if(File.Exists(output+".sha256"))File.Delete(output+".sha256");}throw;}
        finally
        {
            if(File.Exists(partial))File.Delete(partial);
            try{Directory.Delete(workspace,true);Log("Arbeitsdateien entfernt.");}catch(Exception e){Log("Bereinigung nicht vollständig: "+workspace+" – "+e.Message);}
        }
    }
}

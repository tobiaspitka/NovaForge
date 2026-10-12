using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace NovaForge.Core;
public class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field,value)) return false; field=value; Notify(name); return true; }
}
public sealed class Installer : Observable
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    private string name="", path="", arguments="", dependencies="", context="System", profile="Unbekannt / ungeprüft", codes="0,3010", hash="";
    private bool enabled=true, verified, webInstaller; private int timeout=1800;
    public string Name { get=>name; set=>Set(ref name,value); }
    public string Path { get=>path; set { if(Set(ref path,value))ResetRecordingState();Notify(nameof(Type)); } }
    public string Type => System.IO.Path.GetExtension(Path).Equals(".msi",StringComparison.OrdinalIgnoreCase)?"MSI":"EXE";
    public string Arguments { get=>arguments; set=>Set(ref arguments,value); }
    public string Dependencies { get=>dependencies; set=>Set(ref dependencies,value); }
    public string Context { get=>context; set{Set(ref context,value);Notify(nameof(ContextLabel));NotifyWorkflow();} }
    public string ContextLabel=>Context=="System"?"Systemweit":"Benutzerbezogen";
    public string Profile { get=>profile; set=>Set(ref profile,value); }
    public string AllowedExitCodes { get=>codes; set=>Set(ref codes,value); }
    public int TimeoutSeconds { get=>timeout; set=>Set(ref timeout,value); }
    public bool Enabled { get=>enabled; set{Set(ref enabled,value);NotifyWorkflow();} }
    public bool Verified { get=>verified; set=>Set(ref verified,value); }
    public bool WebInstaller { get=>webInstaller; set=>Set(ref webInstaller,value); }
    public string Sha256 { get=>hash; set{if(Set(ref hash,value))ResetRecordingState();} }
    public string RebootPolicy { get; set; }="PauseUntilReboot";
    private string automationMode="Silent",recordedProfilePath="";
    public string AutomationMode {get=>automationMode;set{Set(ref automationMode,value);NotifyWorkflow();}}
    public string RecordedProfilePath {get=>recordedProfilePath;set{if(Set(ref recordedProfilePath,value)){RecordedProfileName="";ResetRecordingState();Notify(nameof(RecordingLabel));}}}
    private string recordedProfileName="";
    [System.Text.Json.Serialization.JsonIgnore] public string RecordedProfileName {get=>recordedProfileName;set{if(Set(ref recordedProfileName,value))Notify(nameof(RecordingLabel));}}
    [System.Text.Json.Serialization.JsonIgnore] public string RecordingLabel=>RecordedProfilePath.Length==0?"Keine Aufnahme zugeordnet":RecordedProfileName.Length>0?RecordedProfileName:"Aufnahme ohne Anzeigenamen";
    public string RecordedProfileSha256 {get;set;}="";
    private string companions="";public string CompanionDirectory {get=>companions;set{if(Set(ref companions,value))ResetRecordingState();}}
    private int recordedSteps;private bool recordingComplete,recordingTested;
    [System.Text.Json.Serialization.JsonIgnore] public int RecordedSteps=>recordedSteps;
    [System.Text.Json.Serialization.JsonIgnore] public bool RecordingComplete=>recordingComplete;
    [System.Text.Json.Serialization.JsonIgnore] public bool RecordingTested=>recordingTested;
    [System.Text.Json.Serialization.JsonIgnore] public bool ReadyForIso=>Context=="User"&&AutomationMode=="Recorded"&&RecordedProfilePath.Length>0&&recordingComplete&&recordingTested;
    [System.Text.Json.Serialization.JsonIgnore] public int WorkflowStep=>ReadyForIso?4:RecordedSteps==0?1:!RecordingComplete?2:3;
    [System.Text.Json.Serialization.JsonIgnore] public string WorkflowStatus=>!Enabled?"Nicht in der ISO":WorkflowStep switch{1=>"Installation aufnehmen",2=>"Aufnahme ergänzen",3=>"Frisch testen",_=>"Für ISO geprüft"};
    public void ApplyRecordingState(RecordedProfile value){recordedSteps=value.Steps.Count;recordingComplete=false;try{value.Validate();recordingComplete=true;}catch(InvalidDataException){}recordingTested=recordingComplete&&value.InstallerSha256.Equals(Sha256,StringComparison.OrdinalIgnoreCase)&&value.Tested&&value.TestedPlanSha256==value.PlanHash();NotifyWorkflow();}
    public void ResetRecordingState(){recordedSteps=0;recordingComplete=recordingTested=false;NotifyWorkflow();}
    private void NotifyWorkflow(){foreach(var name in new[]{nameof(RecordedSteps),nameof(RecordingComplete),nameof(RecordingTested),nameof(ReadyForIso),nameof(WorkflowStep),nameof(WorkflowStatus)})Notify(name);}
    public IEnumerable<string> DependencyIds => Dependencies.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
    public override string ToString()=>Name;
}
public sealed class Project
{
    public int Version { get; set; }=3;
    [System.Text.Json.Serialization.JsonIgnore] public bool ReplayHostPortable {get;set;}
    [System.Text.Json.Serialization.JsonIgnore] public string ReplayHostPath {get;set;}="";
    public string IsoPath { get; set; }="";
    public string SourceKind {get;set;}="Iso";
    public bool RequireRecordings {get;set;}
    public string IsoSha256 { get; set; }="";
    public string OutputPath { get; set; }="";
    public string WorkDirectory { get; set; }=System.IO.Path.GetTempPath();
    public string OscdimgPath { get; set; }="";
    public List<Installer> Programs { get; set; }=[];
    public SetupOptions Setup {get;set;}=new();
    public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    public static async Task<Project> Load(string path)
    {
        var p=JsonSerializer.Deserialize<Project>(await File.ReadAllTextAsync(path),JsonOptions)??throw new InvalidDataException("Leere Projektdatei.");
        if(p.Version is not (1 or 2 or 3)) throw new InvalidDataException("Nicht unterstützte Projektversion.");
        p.Version=3;p.Setup??=new();
        if(p.Programs==null)throw new InvalidDataException("Programmliste fehlt im Projekt.");
        var directory=System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!;
        string Resolve(string value)=>string.IsNullOrWhiteSpace(value)?value:System.IO.Path.GetFullPath(value,directory);
        p.IsoPath=Resolve(p.IsoPath);p.OutputPath=Resolve(p.OutputPath);p.OscdimgPath=Resolve(p.OscdimgPath);p.WorkDirectory=Resolve(p.WorkDirectory);
        foreach(var app in p.Programs){app.Path=Resolve(app.Path);app.RecordedProfilePath=Resolve(app.RecordedProfilePath);app.CompanionDirectory=Resolve(app.CompanionDirectory);if(File.Exists(app.RecordedProfilePath)){try{var profile=await RecordedProfile.Load(app.RecordedProfilePath);app.RecordedProfileName=profile.DisplayName.Trim();app.ApplyRecordingState(profile);if(app.RecordedProfileName.Length==0&&File.Exists(System.IO.Path.ChangeExtension(app.RecordedProfilePath,"name")))app.RecordedProfileName=(await File.ReadAllTextAsync(System.IO.Path.ChangeExtension(app.RecordedProfilePath,"name"))).Trim();}catch(IOException){}catch(System.Text.Json.JsonException){}}}
        return p;
    }
    public async Task Save(string path) { var tmp=path+".tmp"; await File.WriteAllTextAsync(tmp,JsonSerializer.Serialize(this,JsonOptions)); File.Move(tmp,path,true); }
}
public static class Validation
{
    public static List<Installer> Ordered(Project p, bool files=true)
    {
        var active=p.Programs.Where(x=>x.Enabled).ToList();
        p.Setup.Validate();
        if(p.SourceKind is not ("Iso" or "Folder"))throw new InvalidDataException("Unbekannter Quelltyp.");
        if(active.Count==0&&!p.Setup.HasCustomization&&p.SourceKind!="Folder") throw new InvalidDataException("Mindestens ein Programm oder eine Setup-Option aktivieren.");
        if(p.Programs.Select(x=>x.Id).Distinct().Count()!=p.Programs.Count || p.Programs.Any(x=>!Guid.TryParseExact(x.Id,"N",out _))) throw new InvalidDataException("Ungültige oder doppelte Programm-IDs.");
        if(p.Programs.Any(x=>string.IsNullOrWhiteSpace(x.Name)||string.IsNullOrWhiteSpace(x.Path)))throw new InvalidDataException("Name und Dateipfad sind Pflichtangaben.");
        if(p.Programs.Select(x=>System.IO.Path.GetFullPath(x.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=p.Programs.Count) throw new InvalidDataException("Doppelte Installationsdateien.");
        foreach(var a in active)
        {
            if(p.RequireRecordings&&a.AutomationMode!="Recorded")throw new InvalidDataException("Jedes aktive Programm muss zuerst in der Sandbox aufgenommen und frisch getestet werden: "+a.Name);
            if(string.IsNullOrWhiteSpace(a.Name)||string.IsNullOrWhiteSpace(a.Path)) throw new InvalidDataException("Name und Dateipfad sind Pflichtangaben.");
            if(files&&!File.Exists(a.Path)) throw new FileNotFoundException("Installer fehlt: "+a.Name,a.Path);
            if(files&&a.CompanionDirectory.Length>0&&(!Directory.Exists(a.CompanionDirectory)||!FolderMedia.Within(a.Path,a.CompanionDirectory)))throw new InvalidDataException("Begleitordner muss den ausgewählten Installer enthalten: "+a.Name);
            if(!new[]{".msi",".exe"}.Contains(System.IO.Path.GetExtension(a.Path).ToLowerInvariant())) throw new InvalidDataException("Nur MSI und EXE: "+a.Name);
            if(a.Context is not ("System" or "User")) throw new InvalidDataException("Ungültiger Installationskontext.");
            if(a.TimeoutSeconds<10||a.TimeoutSeconds>86400) throw new InvalidDataException("Zeitlimit muss zwischen 10 und 86400 Sekunden liegen.");
            if(a.RebootPolicy is not ("PauseUntilReboot" or "Continue")) throw new InvalidDataException("Ungültige Neustartbehandlung.");
            if(a.Arguments.Contains('\r')||a.Arguments.Contains('\n')||a.Arguments.Contains('\0')) throw new InvalidDataException("Parameter dürfen keine Zeilenumbrüche oder NUL enthalten.");
            if(a.AutomationMode is not ("Silent" or "Recorded"))throw new InvalidDataException("Unbekannter Installationsmodus.");
            if(a.AutomationMode=="Recorded"){
                if(a.Context!="User")throw new InvalidDataException("Recorder-Profile laufen nur nach Anmeldung im interaktiven Benutzerkontext.");
                if(files&&!File.Exists(a.RecordedProfilePath))throw new InvalidDataException("Recorder-Profil fehlt: "+a.Name);
                if(files){var profile=RecordedProfile.Load(a.RecordedProfilePath).GetAwaiter().GetResult();profile.Validate(true);if(profile.InstallerSha256!=a.Sha256)throw new InvalidDataException("Recorder-Profil gehört zu einem anderen Installer.");}
            }else{
                if(!a.Verified) throw new InvalidDataException("Silent-Konfiguration bestätigen: "+a.Name);
                if(a.Type=="EXE"&&string.IsNullOrWhiteSpace(a.Arguments))throw new InvalidDataException("Silent-Parameter für EXE fehlen: "+a.Name);
            }
            ParseCodes(a.AllowedExitCodes);
            foreach(var id in a.DependencyIds)
            {
                var dep=active.SingleOrDefault(x=>x.Id==id)??throw new InvalidDataException("Fehlende/deaktivierte Abhängigkeit: "+a.Name+" → "+id);
                if(a.Context=="System"&&dep.Context=="User") throw new InvalidDataException("Systemprogramme können nicht von Benutzerprogrammen abhängen.");
            }
        }
        var result=new List<Installer>(); var visiting=new HashSet<string>(); var done=new HashSet<string>();
        void Visit(Installer a) { if(done.Contains(a.Id))return; if(!visiting.Add(a.Id))throw new InvalidDataException("Zyklische Abhängigkeiten."); foreach(var id in a.DependencyIds)Visit(active.Single(x=>x.Id==id)); visiting.Remove(a.Id);done.Add(a.Id);result.Add(a); }
        foreach(var a in active.Where(x=>x.Context=="System").Concat(active.Where(x=>x.Context=="User")))Visit(a);
        return result;
    }
    public static int[] ParseCodes(string text) { var a=text.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries); if(a.Length==0||a.Any(x=>!int.TryParse(x,out _)))throw new InvalidDataException("Exitcodes als kommaseparierte Ganzzahlen eingeben."); return a.Select(int.Parse).ToArray(); }
    public static async Task<string> Hash(string path,CancellationToken ct=default) { await using var s=File.OpenRead(path); return Convert.ToHexString(await SHA256.HashDataAsync(s,ct)); }
    public static string Outcome(int code,int[] allowed)=>code==1641?"UnexpectedReboot":!allowed.Contains(code)?"Failed":code==3010?"RebootRequired":"Success";
    public static bool ShouldRun(string? state)=>state is null or "Pending";
}

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NovaForge.Core;
using Validation = NovaForge.Core.Validation;
using NovaForge.Automation;

namespace NovaForge;
public sealed class RelayCommand(Func<Task> action,Func<bool>? can=null) : ICommand
{
 public event EventHandler? CanExecuteChanged {add=>CommandManager.RequerySuggested+=value;remove=>CommandManager.RequerySuggested-=value;}
 public bool CanExecute(object? p)=>can?.Invoke()??true;
 public async void Execute(object? p){try{await action();}catch(Exception e){Dialog.Show("Vorgang nicht möglich",e.Message);}}
}
public static class Dialog
{
 public static void Show(string title,string message)
 {
  var w=new Window{Title=title,Width=620,Height=350,MinWidth=450,MinHeight=250,Owner=Application.Current.MainWindow,WindowStartupLocation=WindowStartupLocation.CenterOwner};
  var panel=new DockPanel{Margin=new Thickness(24)};
  var button=new Button{Content="Verstanden",HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};button.Click+=(_,_)=>w.Close();DockPanel.SetDock(button,Dock.Bottom);panel.Children.Add(button);
  panel.Children.Add(new ScrollViewer{Content=new TextBlock{Text=message,TextWrapping=TextWrapping.Wrap}});w.Content=panel;w.ShowDialog();
 }
}
public sealed class MainViewModel : Observable
{
 private string iso="",output="",tool=IsoBuilder.FindOscdimg(),work=System.IO.Path.GetTempPath(),status="Bereit · Original-ISO auswählen und Programme hinzufügen.",log="",activity="Bereit",isoHash="";
 private bool busy,indeterminate;private double fraction;private int tab;private Installer? selected;private CancellationTokenSource? cancellation;private BuildResult? result;
 public ObservableCollection<Installer> Programs {get;}=[];
 private SetupOptions setup=new();
 public SetupOptions Setup {get=>setup;private set=>Set(ref setup,value);}
 public string VersionText=>"BETA · "+(typeof(MainViewModel).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute),false).OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion??"1.2.0-beta.1");
 private string sourceKind="Iso";public string SourceKind{get=>sourceKind;set{if(Set(ref sourceKind,value)){isoHash="";Notify(nameof(IsoHint));}}}
 public ObservableCollection<RecordingEntry> Recordings {get;}=[];public RecordingEntry? SelectedRecording {get;set;}
 private readonly System.Windows.Threading.DispatcherTimer sessionTimer=new(){Interval=TimeSpan.FromSeconds(3)};private readonly HashSet<string> importedSessions=[];private bool polling;
 private readonly CancellationTokenSource lifetime=new();public void Close(){sessionTimer.Stop();lifetime.Cancel();}
 private string recorderStatus="Installer auswählen. Aufnahme und Wiederholung finden ausschließlich in Windows Sandbox statt.",lastSession="";private bool recorderElevation=true;
 public string RecorderStatus {get=>recorderStatus;set=>Set(ref recorderStatus,value);}
 public bool RecorderElevation {get=>true;set=>Set(ref recorderElevation,true);}
 private string sandboxStatus="Windows-Sandbox-Status wird geprüft …";public string SandboxStatus=>sandboxStatus; private async Task RefreshSandboxStatus(){try{sandboxStatus=(await SandboxFeature.Read(lifetime.Token)).Status;}catch(Exception e){sandboxStatus="Feature-Status konnte nicht gelesen werden: "+e.Message;}Notify(nameof(SandboxStatus));}
 public Func<bool> EditorValid {get;set;}=()=>true;
 public string[] Profiles {get;}=["Unbekannt / ungeprüft","MSI · Windows Installer","Inno Setup · Vorschlag","NSIS · Vorschlag","InstallShield · MSI-Wrapper Vorschlag"];
 public string IsoPath{get=>iso;set{if(Set(ref iso,value))isoHash="";Notify(nameof(IsoHint));}}
 public string OutputPath{get=>output;set{Set(ref output,value);Notify(nameof(OutputHint));}}
 public string OscdimgPath{get=>tool;set{Set(ref tool,value);Notify(nameof(ToolHint));}}
 public string IsoHint=>SourceKind=="Folder"?(System.IO.Directory.Exists(IsoPath)?"USB-/Medienordner vorhanden. Quelle wird nur gelesen; Bootdateien und Abbilder werden geprüft.":"Den Stammordner eines entpackten Windows-Setup-Mediums auswählen."):string.IsNullOrWhiteSpace(IsoPath)?"Bitte eine originale Windows-ISO auswählen.":!System.IO.File.Exists(IsoPath)?"Datei nicht gefunden. Bitte die ISO neu zuordnen.":!IsoPath.EndsWith(".iso",StringComparison.OrdinalIgnoreCase)?"Die Quelldatei muss die Endung .iso besitzen.":"Quelldatei vorhanden. Architektur und Setup-Inhalt werden bei Erstellung geprüft.";
 public string ToolHint=>!System.IO.File.Exists(OscdimgPath)?"oscdimg fehlt. Deployment Tools installieren oder den Werkzeugpfad auswählen.":System.IO.Path.GetFileName(OscdimgPath).Equals("oscdimg.exe",StringComparison.OrdinalIgnoreCase)?"oscdimg gefunden.":"Bitte Microsoft oscdimg.exe auswählen.";
 public string OutputHint=>string.IsNullOrWhiteSpace(OutputPath)?"Einen neuen Dateinamen für die Ausgabe wählen.":System.IO.File.Exists(OutputPath)?"Dieser Dateiname ist bereits belegt. Ein neuer Ausgabename ist erforderlich.":OutputPath.Equals(IsoPath,StringComparison.OrdinalIgnoreCase)?"Quelle und Ausgabe müssen unterschiedlich sein.":!OutputPath.EndsWith(".iso",StringComparison.OrdinalIgnoreCase)?"Der Ausgabename muss auf .iso enden.":"Neue Ausgabe wird nach vollständiger Prüfung übernommen.";
 public string WorkDirectory{get=>work;set=>Set(ref work,value);}
 public string Status{get=>status;set=>Set(ref status,value);}
 public string Activity{get=>activity;set=>Set(ref activity,value);}
 public string Log{get=>log;set=>Set(ref log,value);}
 public bool Busy{get=>busy;set{Set(ref busy,value);Notify(nameof(Editing));CommandManager.InvalidateRequerySuggested();}}
 public bool Editing=>!Busy;
 public bool Indeterminate{get=>indeterminate;set=>Set(ref indeterminate,value);}
 public double Fraction{get=>fraction;set=>Set(ref fraction,value);}
 public int Tab{get=>tab;set=>Set(ref tab,value);}
 public Installer? Selected{get=>selected;set{Set(ref selected,value);Notify(nameof(HasSelection));CommandManager.InvalidateRequerySuggested();}}
 public bool HasSelection=>Selected!=null;
 public ICommand RecordCommand {get;}public ICommand TestRecordingCommand {get;}public ICommand ImportRecordingCommand {get;}public ICommand SessionFolderCommand {get;}
 public ICommand MediaFolderCommand {get;}public ICommand UseRecordingCommand {get;}public ICommand LibraryFolderCommand {get;}public ICommand ActivateSandboxCommand {get;}
 public string ResultTitle=>result==null?"Noch keine ISO erstellt":"✓  ISO erfolgreich erstellt";
 public string ResultHash=>result?.Sha256??"";
 public string ResultText=>result==null?"Das Ergebnis erscheint nach einer vollständig abgeschlossenen Erstellung.":result.Path+"\n\nGröße: "+(result.Size/1073741824d).ToString("N2")+" GiB\n\nEnthaltene Programme (effektive Reihenfolge):\n"+string.Join("\n",result.Programs.Select((a,i)=>$"{i+1}. {a.Name} · {(a.Context=="System"?"specialize / SYSTEM; Fortsetzung nach Neustart":"erste Anmeldung / Benutzerkonto")}"));
 public ICommand IsoCommand{get;}public ICommand ToolCommand{get;}public ICommand OutputCommand{get;}public ICommand WorkCommand{get;}public ICommand AdkCommand{get;}public ICommand AddCommand{get;}public ICommand RemoveCommand{get;}public ICommand UpCommand{get;}public ICommand DownCommand{get;}public ICommand RelinkCommand{get;}public ICommand DependenciesCommand{get;}public ICommand SaveCommand{get;}public ICommand OpenCommand{get;}public ICommand ValidateCommand{get;}public ICommand BuildCommand{get;}public ICommand CancelCommand{get;}public ICommand FolderCommand{get;}public ICommand LogCommand{get;}public ICommand ReleasesCommand{get;}public ICommand HistoryCommand{get;}
 public MainViewModel()
 {
  ICommand Command(Action action,Func<bool>? can=null)=>new RelayCommand(()=>{action();return Task.CompletedTask;},can);
  bool Edit()=>Editing;bool Selection()=>Editing&&HasSelection;
  IsoCommand=Command(()=>{var d=new OpenFileDialog{Filter="Windows-ISO|*.iso"};if(d.ShowDialog()==true){SourceKind="Iso";IsoPath=d.FileName;isoHash="";Status="ISO ausgewählt. Die Medienprüfung erfolgt beim Erstellen.";}},Edit);
  MediaFolderCommand=Command(()=>{var d=new OpenFolderDialog{Title="USB-Stick oder entpacktes Windows-Medium: Stammordner auswählen"};if(d.ShowDialog()==true){SourceKind="Folder";IsoPath=d.FolderName;isoHash="";Status="Medienordner ausgewählt. USB-Stick wird nur gelesen; Ausgabe auf einem anderen Laufwerk wählen.";}},Edit);
  ToolCommand=Command(()=>{var d=new OpenFileDialog{Filter="Microsoft oscdimg|oscdimg.exe"};if(d.ShowDialog()==true)OscdimgPath=d.FileName;},Edit);
  OutputCommand=Command(()=>{var d=new SaveFileDialog{Filter="ISO-Datei|*.iso",FileName="Windows-NovaForge.iso",OverwritePrompt=true};if(d.ShowDialog()==true)OutputPath=d.FileName;},Edit);
  WorkCommand=Command(()=>{var d=new OpenFolderDialog{Title="Arbeitsverzeichnis auswählen"};if(d.ShowDialog()==true)WorkDirectory=d.FolderName;},Edit);
  AdkCommand=Command(()=>Launch("https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install"));
  ReleasesCommand=Command(()=>Launch("https://github.com/tobiaspitka/NovaForge/releases"));
  HistoryCommand=Command(()=>{var path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaForge","Versions");System.IO.Directory.CreateDirectory(path);Launch(path);});
  RecordCommand=new RelayCommand(()=>StartRecorder("Record"),Selection);
  TestRecordingCommand=new RelayCommand(()=>StartRecorder("Test"),()=>Selection()&&!string.IsNullOrEmpty(Selected!.RecordedProfilePath));
  ImportRecordingCommand=new RelayCommand(ImportRecording,Selection);
  UseRecordingCommand=new RelayCommand(async()=>{if(Selected==null||SelectedRecording==null)throw new InvalidOperationException("Programm und gespeicherte Aufnahme auswählen.");await RecordingLibrary.Attach(Selected,SelectedRecording);RecorderStatus=SelectedRecording.Tested?"Geprüfte Aufnahme zugeordnet. Bereit für ISO-Erstellung.":"Aufnahme zugeordnet. Jetzt frisch wiederholen.";},Selection);
  LibraryFolderCommand=Command(()=>{Directory.CreateDirectory(RecordingLibrary.Root);Launch(RecordingLibrary.Root);});
  ActivateSandboxCommand=new RelayCommand(async()=>{Busy=true;try{RecorderStatus="Windows Sandbox aktivieren … Administratorfreigabe bestätigen.";(await SandboxFeature.Read()).ValidateActivation();await SandboxSessions.Activate();await RefreshSandboxStatus();RecorderStatus="Aktivierung abgeschlossen. Windows bei Bedarf manuell neu starten; danach NovaForge erneut öffnen.";Notify(nameof(SandboxStatus));}finally{Busy=false;}},Edit);
  sessionTimer.Tick+=async(_,_)=>await PollSessions();sessionTimer.Start();_ = RefreshLibrary();_ = RefreshSandboxStatus();
  SessionFolderCommand=Command(()=>Launch(lastSession),()=>!string.IsNullOrEmpty(lastSession));
  AddCommand=Command(()=>{var d=new OpenFileDialog{Filter="Installer|*.msi;*.exe",Multiselect=true};if(d.ShowDialog()==true)AddFiles(d.FileNames);},Edit);
  RemoveCommand=Command(()=>{if(Selected is {} a){Programs.Remove(a);Selected=Programs.FirstOrDefault();Status="Programm entfernt. Abhängigkeiten werden beim Prüfen validiert.";}},Selection);
  UpCommand=Command(()=>Move(-1),Selection);DownCommand=Command(()=>Move(1),Selection);
  RelinkCommand=Command(()=>{if(Selected is {} a){var d=new OpenFileDialog{Filter="Installer|*.msi;*.exe"};if(d.ShowDialog()==true){a.Path=d.FileName;a.Sha256="";a.Verified=false;Status="Datei neu zugeordnet. Silent-Konfiguration erneut bestätigen.";}}},Selection);
  DependenciesCommand=Command(EditDependencies,Selection);
  SaveCommand=new RelayCommand(Save,Edit);OpenCommand=new RelayCommand(Open,Edit);
  ValidateCommand=Command(()=>{var ordered=Validation.Ordered(GetProject());if(SourceKind=="Folder")IsoBuilder.CheckMedia(IsoPath,ordered.Count==0&&!Setup.HasCustomization);else if(!System.IO.File.Exists(IsoPath))throw new InvalidOperationException("Quell-ISO fehlt.");if(!System.IO.File.Exists(OscdimgPath))throw new InvalidOperationException("oscdimg fehlt. ADK Deployment Tools installieren oder Werkzeug zuordnen.");Status="Konfiguration gültig: "+ordered.Count+" Programme. Medieninhalt wird beim Erstellen geprüft.";},Edit);
  BuildCommand=new RelayCommand(Build,Edit);CancelCommand=Command(()=>{cancellation?.Cancel();Status="Abbruch angefordert. Ressourcen werden kontrolliert freigegeben.";},()=>Busy&&cancellation!=null);
  FolderCommand=Command(()=>Launch(System.IO.Path.GetDirectoryName(result!.Path)!),()=>result!=null);LogCommand=Command(()=>Launch(result!.LogPath),()=>result!=null);
 }
 private static void Launch(string path)=>Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
 public void AddFiles(IEnumerable<string> files)
 {
  if(Busy)return;
  foreach(var file in files)
  {
   if(Directory.Exists(file)){SourceKind="Folder";IsoPath=file;isoHash="";Tab=0;continue;}
   if(System.IO.Path.GetExtension(file).Equals(".iso",StringComparison.OrdinalIgnoreCase)){SourceKind="Iso";IsoPath=file;isoHash="";Tab=0;continue;}
   if(!new[]{".exe",".msi"}.Contains(System.IO.Path.GetExtension(file).ToLowerInvariant())){Status="Nur ISO-, MSI- und EXE-Dateien werden angenommen.";continue;}
   if(Programs.Any(a=>a.Path.Equals(file,StringComparison.OrdinalIgnoreCase))){Status="Datei bereits vorhanden: "+file;continue;}
   var msi=System.IO.Path.GetExtension(file).Equals(".msi",StringComparison.OrdinalIgnoreCase);
   var app=new Installer{Name=System.IO.Path.GetFileNameWithoutExtension(file),Path=file,Profile=msi?Profiles[1]:Profiles[0],Verified=msi};Programs.Add(app);Selected=app;Tab=1;
  }
 }
 public void ApplyProfile(Installer a)
 {
  if(a.Type=="MSI") {a.Arguments="";a.Verified=true;return;}
  a.Arguments=a.Profile switch{"Inno Setup · Vorschlag"=>"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-","NSIS · Vorschlag"=>"/S","InstallShield · MSI-Wrapper Vorschlag"=>"/s /v\"/qn /norestart REBOOT=ReallySuppress\"",_=>""};a.Verified=false;
 }
 private void Move(int delta){if(Selected is {} a){var index=Programs.IndexOf(a);var next=index+delta;if(next>=0&&next<Programs.Count)Programs.Move(index,next);}}
 private Project GetProject()=>new(){SourceKind=SourceKind,IsoPath=IsoPath,IsoSha256=isoHash,OutputPath=OutputPath,OscdimgPath=OscdimgPath,WorkDirectory=WorkDirectory,Programs=Programs.ToList(),Setup=Setup,ReplayHostPortable=typeof(App).Assembly.Location.Length==0,ReplayHostPath=Environment.ProcessPath??""};
 private async Task StartRecorder(string mode){if(Selected is not {} app)return;Busy=true;try{RecorderStatus="Isolierte Sitzung vorbereiten …";lastSession=await SandboxSessions.Prepare(app,mode,RecorderElevation);SandboxSessions.Launch(lastSession);RecorderStatus="Sandbox gestartet. Dort Installation vormachen beziehungsweise testen. Speichern übernimmt das Profil automatisch. Sandbox schließt anschließend; vor Wiederholung auf das Schließen warten.";}finally{Busy=false;}}
 private async Task ImportRecording(){if(Selected is not {} app)return;var dialog=new OpenFileDialog{Filter="Recorder-Profil|profile.json;*.json",InitialDirectory=RecordingLibrary.Root};if(dialog.ShowDialog()!=true)return;var entry=await RecordingLibrary.Store(dialog.FileName,app.Name);await RecordingLibrary.Attach(app,entry);await RefreshLibrary();RecorderStatus=entry.Tested?"Geprüfte Aufnahme gespeichert und zugeordnet. Bereit für ISO.":"Aufnahme gespeichert und zugeordnet. Jetzt frisch wiederholen.";}
 private async Task RefreshLibrary(){try{var items=await RecordingLibrary.List();Recordings.Clear();foreach(var item in items)Recordings.Add(item);}catch(Exception e){RecorderStatus="Bibliothek konnte nicht gelesen werden: "+e.Message;}}
 private async Task PollSessions(){if(polling||Busy||!Directory.Exists(SandboxSessions.Root))return;polling=true;try{foreach(var dir in Directory.GetDirectories(SandboxSessions.Root)){try{var output=Path.Combine(dir,"Output","profile.json");if(File.Exists(output)&&!importedSessions.Contains(dir)){var plan=await RecordedProfile.Load(output);var app=Programs.FirstOrDefault(a=>a.Sha256==plan.InstallerSha256);if(app==null){foreach(var candidate in Programs.Where(x=>File.Exists(x.Path))){if(await Validation.Hash(candidate.Path)==plan.InstallerSha256){app=candidate;break;}}}var session=System.Text.Json.JsonSerializer.Deserialize<RecorderSession>(await File.ReadAllTextAsync(Path.Combine(dir,"Input","session.json")),Project.JsonOptions);var entry=await RecordingLibrary.Store(output,app?.Name??session?.DisplayName??"Aufnahme");if(app!=null)await RecordingLibrary.Attach(app,entry);importedSessions.Add(dir);await RefreshLibrary();RecorderStatus=entry.Tested?"Geprüfte Aufnahme automatisch gespeichert und zugeordnet. Bereit für ISO.":"Aufnahme automatisch gespeichert und zugeordnet. Sandbox schließen lassen, dann frisch wiederholen.";}
 if(!SandboxSessions.GuestRunning&&(importedSessions.Contains(dir)||(!File.Exists(output)&&Directory.GetCreationTimeUtc(dir)<DateTime.UtcNow.AddMinutes(-2)))){var error=Path.Combine(dir,"Output","test-error.txt");if(File.Exists(error)){var diagnostics=Path.Combine(RecordingLibrary.Root,"Diagnostics");Directory.CreateDirectory(diagnostics);File.Copy(error,Path.Combine(diagnostics,Path.GetFileName(dir)+".log"),true);RecorderStatus="Wiederholung fehlgeschlagen. Diagnose in der Aufnahmebibliothek gespeichert.";}if(SandboxSessions.Cleanup(dir)){if(lastSession==dir)lastSession="";CommandManager.InvalidateRequerySuggested();}}}catch(IOException){}catch(System.Text.Json.JsonException){}catch(Exception e){RecorderStatus="Sitzungsübernahme: "+e.Message;}}}finally{polling=false;}}
 private void EditDependencies()
 {
  if(Selected is not {} a)return;
  var window=new Window{Title="Abhängigkeiten · "+a.Name,Width=550,Height=450,Owner=Application.Current.MainWindow,WindowStartupLocation=WindowStartupLocation.CenterOwner};var panel=new DockPanel{Margin=new Thickness(24)};
  var button=new Button{Content="Übernehmen",Margin=new Thickness(0,14,0,0)};DockPanel.SetDock(button,Dock.Bottom);panel.Children.Add(button);
  var list=new ListBox{SelectionMode=SelectionMode.Multiple,DisplayMemberPath="Name",ItemsSource=Programs.Where(x=>x!=a).ToList()};panel.Children.Add(list);foreach(var x in Programs.Where(x=>a.DependencyIds.Contains(x.Id)))list.SelectedItems.Add(x);
  button.Click+=(_,_)=>{a.Dependencies=string.Join(",",list.SelectedItems.Cast<Installer>().Select(x=>x.Id));window.Close();};window.Content=panel;window.ShowDialog();
 }
 private async Task Save()
 {
  if(!EditorValid())throw new InvalidOperationException("Bitte ungültige Eingaben korrigieren, insbesondere das Zeitlimit.");
  var d=new SaveFileDialog{Filter="NovaForge-Projekt|*.novaforge.json",FileName="Mein-Windows.novaforge.json"};if(d.ShowDialog()!=true)return;
  Busy=true;
  try{Status="Projektdateien und Prüfsummen prüfen …";if(SourceKind=="Folder"?Directory.Exists(IsoPath):File.Exists(IsoPath)){var hash=SourceKind=="Folder"?await FolderMedia.Digest(IsoPath):await Validation.Hash(IsoPath);if(isoHash.Length>0&&hash!=isoHash)throw new InvalidOperationException("ISO verändert. Bitte erneut auswählen.");isoHash=hash;}
   foreach(var a in Programs.Where(a=>System.IO.File.Exists(a.Path))){var hash=await Validation.Hash(a.Path);if(a.Sha256.Length>0&&a.Sha256!=hash)throw new InvalidOperationException("Datei verändert: "+a.Name+". Bitte neu zuordnen.");a.Sha256=hash;}
   await GetProject().Save(d.FileName);Status="Projekt gespeichert: "+d.FileName;
  }finally{Busy=false;}
 }
 private async Task Open()
 {
  var d=new OpenFileDialog{Filter="NovaForge-Projekt|*.json"};if(d.ShowDialog()!=true)return;Busy=true;
  try{
   var p=await Project.Load(d.FileName);var issues=new List<string>();Status="Referenzierte Dateien prüfen …";
   if(p.SourceKind=="Folder"?!Directory.Exists(p.IsoPath):!File.Exists(p.IsoPath))issues.Add("Medienquelle fehlt: "+p.IsoPath);else if(p.IsoSha256.Length>0&&(p.SourceKind=="Folder"?await FolderMedia.Digest(p.IsoPath):await Validation.Hash(p.IsoPath))!=p.IsoSha256)issues.Add("Medienquelle wurde verändert.");
   foreach(var a in p.Programs){if(!System.IO.File.Exists(a.Path))issues.Add(a.Name+": Datei fehlt.");else if(a.Sha256.Length>0&&await Validation.Hash(a.Path)!=a.Sha256)issues.Add(a.Name+": Datei wurde verändert.");}
   SourceKind=p.SourceKind;IsoPath=p.IsoPath;isoHash=p.IsoSha256;OutputPath=p.OutputPath;WorkDirectory=p.WorkDirectory;OscdimgPath=p.OscdimgPath;Setup=p.Setup;Programs.Clear();foreach(var a in p.Programs)Programs.Add(a);Selected=Programs.FirstOrDefault();
   if(Setup.CreateLocalUser)issues.Add("Kennwort des lokalen Kontos erneut eingeben; es wird nicht im Projekt gespeichert.");
   Status=issues.Count==0?"Projekt geladen; gespeicherte Prüfsummen geprüft.":"Projekt geladen: "+issues.Count+" Dateiprobleme. ISO auswählen / Installer neu zuordnen.";
   if(issues.Count>0)Dialog.Show("Dateizuordnung prüfen",string.Join("\n",issues)+"\n\nVerschobene oder absichtlich aktualisierte Dateien über die Auswahl neu zuordnen.");
  }finally{Busy=false;}
 }
 private async Task Build()
 {
  if(!EditorValid())throw new InvalidOperationException("Bitte ungültige Eingaben korrigieren, insbesondere das Zeitlimit.");
  Busy=true;Tab=4;Log="";result=null;Notify(nameof(ResultTitle));Notify(nameof(ResultText));Notify(nameof(ResultHash));cancellation=new();Indeterminate=true;
  try {
   var p=GetProject();var reporter=new Progress<BuildProgress>(v=>{Activity=v.Activity;Indeterminate=v.Fraction==null;Fraction=v.Fraction??0;if(v.Fraction==null){Log+=(DateTime.Now.ToString("HH:mm:ss")+"  "+v.Activity+"\n");if(Log.Length>60000)Log=Log[^50000..];}});
   result=await Task.Run(()=>new IsoBuilder().Build(p,reporter,cancellation.Token));isoHash=p.IsoSha256;Status="ISO erfolgreich erstellt und auf Lesbarkeit sowie Installer-Prüfsummen geprüft.";Activity="Abgeschlossen";Fraction=1;Tab=5;
  } catch(OperationCanceledException){Status="Erstellung abgebrochen. Details stehen im Ausgabeprotokoll.";Activity="Abgebrochen";}
  catch(Exception e){Status=e.Message;Activity="Erstellung fehlgeschlagen";Dialog.Show("Erstellung fehlgeschlagen",e.Message+"\n\nDas Protokoll liegt beim gewählten Ausgabepfad (.log).");}
  finally{Busy=false;Indeterminate=false;cancellation.Dispose();cancellation=null;Notify(nameof(ResultTitle));Notify(nameof(ResultText));Notify(nameof(ResultHash));}
 }
}

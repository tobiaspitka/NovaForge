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
 public ObservableCollection<RecordingEntry> Recordings {get;}=[];private RecordingEntry? selectedRecording;public RecordingEntry? SelectedRecording{get=>selectedRecording;set{Set(ref selectedRecording,value);CommandManager.InvalidateRequerySuggested();}}
 private readonly System.Windows.Threading.DispatcherTimer sessionTimer=new(){Interval=TimeSpan.FromSeconds(2)};private readonly HashSet<string> importedSessions=[];private readonly Dictionary<string,string> sessionHashes=[];private bool polling;
 private string recordingName="Meine Installation";public string RecordingName{get=>recordingName;set=>Set(ref recordingName,value);}
 private readonly CancellationTokenSource lifetime=new();public void Close(){sessionTimer.Stop();lifetime.Cancel();}
 private string recorderStatus="Installer auswählen. Aufnahme und Wiederholung finden ausschließlich in Windows Sandbox statt.",lastSession="";private bool recorderElevation=true;
 public string RecorderStatus {get=>recorderStatus;set=>Set(ref recorderStatus,value);}
 public bool RecorderElevation {get=>true;set=>Set(ref recorderElevation,true);}
 private string sandboxStatus="Windows-Sandbox-Status wird geprüft …";public string SandboxStatus=>sandboxStatus; private async Task RefreshSandboxStatus(){try{var info=await SandboxFeature.Read(lifetime.Token);sandboxStatus=info.Status;sandboxUsable=info.Supported&&info.State==1&&SandboxSessions.Available&&(info.Hypervisor||info.FirmwareVirtualization!=false);}catch(Exception e){sandboxStatus="Feature-Status konnte nicht gelesen werden: "+e.Message;}sandboxChecked=true;Notify(nameof(SandboxStatus));NotifyWizard();}
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
 public bool Editing=>!Busy&&!sessionActive;
 public bool Indeterminate{get=>indeterminate;set=>Set(ref indeterminate,value);}
 public double Fraction{get=>fraction;set=>Set(ref fraction,value);}
 public int Tab{get=>tab;set{if(Set(ref tab,value))NotifyGuide();}}
 public ICommand BackCommand {get;}public ICommand ContinueCommand {get;}
 public string PageTitle=>new[]{"Dein Windows, mit deinen Programmen","Welche Programme möchtest du mitnehmen?","Einmal zeigen. Danach automatisch installieren.","Wie soll Windows eingerichtet werden?","Alles bereit für dein neues Windows?","Dein Ergebnis"}[Math.Clamp(Tab,0,5)];
 public string PageHelp=>new[]{"Wähle zuerst deine Windows-Datei oder einen Windows-USB-Stick. Wir führen dich durch den Rest.","Füge die Installationsdateien deiner Programme hinzu. Du kannst auch mehrere Dateien gleichzeitig auswählen.","Du installierst ein Programm in einem getrennten Test-Windows. NovaForge merkt sich deine Schritte und prüft sie danach in einer neuen Sitzung.","Dieser Schritt ist freiwillig. Lass die Einstellungen unverändert, wenn du nicht sicher bist.","Wähle, wo deine neue ISO-Datei gespeichert werden soll. Die Checkliste zeigt dir, was noch fehlt.","Hier findest du deine fertige Datei. Teste sie vor der Verwendung auf einem anderen Computer."}[Math.Clamp(Tab,0,5)];
 public string PageNumber=>"SCHRITT "+(Tab+1)+" VON 6";
 public string ContinueLabel=>Tab switch{0=>"Weiter: Programme →",1=>Programs.Any(x=>x.Enabled)?"Weiter: Installation zeigen →":"Ohne Programme weiter →",2=>"Weiter: Windows einrichten →",3=>"Weiter: Zusammenfassung →",4=>"ISO erstellen",_=>"Zur Zusammenfassung"};
 public bool SourceReady=>SourceKind=="Folder"?Directory.Exists(IsoPath):File.Exists(IsoPath)&&IsoPath.EndsWith(".iso",StringComparison.OrdinalIgnoreCase);
 public bool ToolReady=>File.Exists(OscdimgPath)&&Path.GetFileName(OscdimgPath).Equals("oscdimg.exe",StringComparison.OrdinalIgnoreCase);
 public bool OutputReady=>!string.IsNullOrWhiteSpace(OutputPath)&&OutputPath.EndsWith(".iso",StringComparison.OrdinalIgnoreCase)&&!File.Exists(OutputPath)&&!OutputPath.Equals(IsoPath,StringComparison.OrdinalIgnoreCase);
 public string SourceCheck=>(SourceReady?"✓  ":"○  ")+"Windows-Quelle "+(SourceReady?"ausgewählt":"fehlt noch");
 public string SoftwareCheck=>(AllProgramsReady?"✓  ":"○  ")+(Programs.Any(x=>x.Enabled)?ReadySummary:"Keine zusätzlichen Programme gewählt");
 public string ToolCheck=>(ToolReady?"✓  ":"○  ")+"Werkzeug zur ISO-Erstellung "+(ToolReady?"gefunden":"fehlt noch · in Schritt 1 einrichten");
 public string OutputCheck=>(OutputReady?"✓  ":"○  ")+"Speicherort "+(OutputReady?"gewählt":"fehlt oder ist bereits belegt");
 public string NavigationHint=>sessionActive?"Beende zuerst die offene Testumgebung mit „Speichern und schließen“.":Tab==0&&!SourceReady?"Wähle eine Windows-Quelle, um fortzufahren.":Tab==2&&!AllProgramsReady?"Nimm jedes ausgewählte Programm auf und teste es einmal.":Tab==4?(!SourceReady?"Die Windows-Quelle fehlt.":!ToolReady?"Richte zuerst das ISO-Werkzeug in Schritt 1 ein.":!AllProgramsReady?"Einige Programme müssen noch getestet werden.":!OutputReady?"Wähle einen neuen Dateinamen für deine ISO.":"Alles ausgewählt. Die vollständige Prüfung erfolgt beim Erstellen."):"Du kannst jederzeit zu einem vorherigen Schritt zurückgehen.";
 private void NotifyGuide(){foreach(var name in new[]{nameof(PageTitle),nameof(PageHelp),nameof(PageNumber),nameof(ContinueLabel),nameof(SourceReady),nameof(ToolReady),nameof(OutputReady),nameof(SourceCheck),nameof(SoftwareCheck),nameof(ToolCheck),nameof(OutputCheck),nameof(NavigationHint)})Notify(name);CommandManager.InvalidateRequerySuggested();}
 public Installer? Selected{get=>selected;set{if(Set(ref selected,value)){RecordingName=value?.RecordedProfileName.Length>0?value.RecordedProfileName:value?.Name??"Meine Installation";}Notify(nameof(HasSelection));NotifyWizard();CommandManager.InvalidateRequerySuggested();}}
 private bool sandboxChecked,sandboxUsable;private bool sessionActive;private DateTime sessionStarted;private string sessionProgram="",sessionMode="";
 public bool SessionActive=>sessionActive;
 public string CurrentProgramName=>Selected?.Name??"Wähle ein Programm";
 public string CurrentProgramInfo=>Selected==null?"Software hinzufügen, dann Schritt für Schritt vorbereiten.":"Programm "+(Programs.IndexOf(Selected)+1)+" von "+Programs.Count+" · "+Selected.Type+" · "+Selected.WorkflowStatus;
 public string WizardAction=>sessionActive?"Sandbox-Sitzung läuft":Selected==null?"Software hinzufügen":(Selected.WorkflowStep is 1 or 3)&&!sandboxUsable?"Windows Sandbox vorbereiten":Selected.WorkflowStep switch{1=>"1  Installation in Sandbox vormachen",2=>"2  Aufnahme / Prüfdatei ergänzen",3=>"3  In frischer Sandbox testen",_=>"4  Nächstes Programm vorbereiten"};
 public string WizardHint=>sessionActive?"In der Sandbox weiterarbeiten. Speichern und schließen übernimmt die Aufnahme automatisch.":Selected==null?"Beginne mit einer MSI- oder EXE-Datei.":(Selected.WorkflowStep is 1 or 3)&&!sandboxUsable?SandboxStatus:Selected.WorkflowStep switch{1=>"Installiere dieses Programm einmal in der Sandbox. Deine Auswahl und zusätzliche Treiberfenster werden aufgenommen.",2=>"Die Aufnahme ist gespeichert. Ergänze eine neu installierte Datei als Erfolgskriterium.",3=>"Die erste Sandbox muss geschlossen sein. NovaForge öffnet für den Test eine neue, saubere Sitzung.",_=>"Aufnahme und frischer Test abgeschlossen. Dieses Programm ist für die ISO vorbereitet."};
 public string ReadySummary=>Programs.Count(x=>x.Enabled&&x.ReadyForIso)+" von "+Programs.Count(x=>x.Enabled)+" Programmen geprüft";
 public bool AllProgramsReady=>Programs.Where(x=>x.Enabled).All(x=>x.ReadyForIso);
 public bool CanRecord=>Editing&&HasSelection&&!sessionActive&&sandboxUsable;
 public bool CanTest=>CanRecord&&Selected!.RecordingComplete;
 public string InstallStepText=>Selected?.RecordedSteps>0?"✓ Installation aufgenommen":"Installation vormachen";
 public string SaveStepText=>Selected?.RecordingComplete==true?"✓ Aufnahme gespeichert":"Aufnahme abschließen";
 public string TestStepText=>Selected?.ReadyForIso==true?"✓ Frischer Test bestanden":"Neue Sandbox testen";
 public string ReleaseStepText=>Selected?.ReadyForIso==true?"✓ Bereit für ISO":"Freigabe nach Test";
 private void NotifyWizard(){foreach(var name in new[]{nameof(CurrentProgramName),nameof(CurrentProgramInfo),nameof(WizardAction),nameof(WizardHint),nameof(ReadySummary),nameof(AllProgramsReady),nameof(CanRecord),nameof(CanTest),nameof(InstallStepText),nameof(SaveStepText),nameof(TestStepText),nameof(ReleaseStepText),nameof(SessionActive),nameof(Editing)})Notify(name);CommandManager.InvalidateRequerySuggested();}
 private void ProgramChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs args){if(args.PropertyName is nameof(Installer.Name) or nameof(Installer.WorkflowStatus) or nameof(Installer.Enabled) or nameof(Installer.RecordedProfileName))NotifyWizard();}
 public bool HasSelection=>Selected!=null;
 public ICommand RecordCommand {get;}public ICommand TestRecordingCommand {get;}public ICommand ImportRecordingCommand {get;}public ICommand SessionFolderCommand {get;}
 public ICommand MediaFolderCommand {get;}public ICommand UseRecordingCommand {get;}public ICommand LibraryFolderCommand {get;}public ICommand ActivateSandboxCommand {get;}
 public ICommand CompanionFolderCommand {get;}
 public ICommand EditRecordingCommand {get;}
 public ICommand ExportRecordingCommand {get;}public ICommand DeleteRecordingCommand {get;}
 public ICommand WizardCommand {get;}public ICommand NextProgramCommand {get;}public ICommand StudioCommand {get;}
 public string ResultTitle=>result==null?"Noch keine ISO erstellt":"✓  ISO erfolgreich erstellt";
 public string ResultHash=>result?.Sha256??"";
 public string ResultText=>result==null?"Das Ergebnis erscheint nach einer vollständig abgeschlossenen Erstellung.":result.Path+"\n\nGröße: "+(result.Size/1073741824d).ToString("N2")+" GiB\n\nEnthaltene Programme (effektive Reihenfolge):\n"+string.Join("\n",result.Programs.Select((a,i)=>$"{i+1}. {a.Name} · {(a.Context=="System"?"specialize / SYSTEM; Fortsetzung nach Neustart":"erste Anmeldung / Benutzerkonto")}"));
 public ICommand IsoCommand{get;}public ICommand ToolCommand{get;}public ICommand OutputCommand{get;}public ICommand WorkCommand{get;}public ICommand AdkCommand{get;}public ICommand AddCommand{get;}public ICommand RemoveCommand{get;}public ICommand UpCommand{get;}public ICommand DownCommand{get;}public ICommand RelinkCommand{get;}public ICommand DependenciesCommand{get;}public ICommand SaveCommand{get;}public ICommand OpenCommand{get;}public ICommand ValidateCommand{get;}public ICommand BuildCommand{get;}public ICommand CancelCommand{get;}public ICommand FolderCommand{get;}public ICommand LogCommand{get;}public ICommand ReleasesCommand{get;}public ICommand HistoryCommand{get;}
 public MainViewModel()
 {
  ICommand Command(Action action,Func<bool>? can=null)=>new RelayCommand(()=>{action();return Task.CompletedTask;},can);
  bool Edit()=>Editing;bool Selection()=>Editing&&HasSelection;
  PropertyChanged+=(_,e)=>{if(e.PropertyName is nameof(IsoPath) or nameof(SourceKind) or nameof(OscdimgPath) or nameof(OutputPath) or nameof(AllProgramsReady) or nameof(ReadySummary) or nameof(SessionActive))NotifyGuide();};
  BackCommand=Command(()=>Tab=Math.Max(0,Tab-1),()=>Editing&&Tab>0);
  ContinueCommand=new RelayCommand(async()=>{if(Tab==4){await Build();return;}if(Tab==5){Tab=4;return;}if(Tab==1&&!Programs.Any(x=>x.Enabled)){Tab=3;return;}if(Tab==1)Selected=Programs.FirstOrDefault(x=>x.Enabled&&!x.ReadyForIso)??Programs.FirstOrDefault(x=>x.Enabled);Tab++;},()=>Editing&&(Tab switch{0=>SourceReady,2=>AllProgramsReady,4=>SourceReady&&ToolReady&&OutputReady&&AllProgramsReady,_=>true}));
  Programs.CollectionChanged+=(_,e)=>{if(e.OldItems!=null)foreach(Installer app in e.OldItems)app.PropertyChanged-=ProgramChanged;if(e.NewItems!=null)foreach(Installer app in e.NewItems)app.PropertyChanged+=ProgramChanged;NotifyWizard();};
  StudioCommand=Command(()=>Tab=2,Selection);
  NextProgramCommand=Command(()=>{var next=Programs.FirstOrDefault(x=>x.Enabled&&!x.ReadyForIso&&x!=Selected);if(next!=null){Selected=next;Tab=2;}else if(AllProgramsReady){Tab=4;Status="Alle aktiven Programme wurden aufgenommen und frisch getestet.";}else RecorderStatus="Für dieses Programm fehlen noch Aufnahme oder Test.";},()=>Editing&&!sessionActive);
  WizardCommand=new RelayCommand(async()=>{if(Selected==null){AddCommand?.Execute(null);return;}if((Selected.WorkflowStep is 1 or 3)&&!sandboxUsable){ActivateSandboxCommand?.Execute(null);return;}if(Selected.WorkflowStep==1)await StartRecorder("Record");else if(Selected.WorkflowStep==2){SelectedRecording=Recordings.FirstOrDefault(x=>x.Path==Selected.RecordedProfilePath);await EditRecording();}else if(Selected.WorkflowStep==3)await StartRecorder("Test");else NextProgramCommand.Execute(null);},()=>Editing&&!sessionActive&&(sandboxChecked||Selected==null||Selected.WorkflowStep is 2 or 4));
  IsoCommand=Command(()=>{var d=new OpenFileDialog{Filter="Windows-ISO|*.iso"};if(d.ShowDialog()==true){SourceKind="Iso";IsoPath=d.FileName;isoHash="";Status="ISO ausgewählt. Die Medienprüfung erfolgt beim Erstellen.";}},Edit);
  MediaFolderCommand=Command(()=>{var d=new OpenFolderDialog{Title="USB-Stick oder entpacktes Windows-Medium: Stammordner auswählen"};if(d.ShowDialog()==true){SourceKind="Folder";IsoPath=d.FolderName;isoHash="";Status="Medienordner ausgewählt. USB-Stick wird nur gelesen; Ausgabe auf einem anderen Laufwerk wählen.";}},Edit);
  ToolCommand=Command(()=>{var d=new OpenFileDialog{Filter="Microsoft oscdimg|oscdimg.exe"};if(d.ShowDialog()==true)OscdimgPath=d.FileName;},Edit);
  OutputCommand=Command(()=>{var d=new SaveFileDialog{Filter="ISO-Datei|*.iso",FileName="Windows-NovaForge.iso",OverwritePrompt=true};if(d.ShowDialog()==true)OutputPath=d.FileName;},Edit);
  WorkCommand=Command(()=>{var d=new OpenFolderDialog{Title="Arbeitsverzeichnis auswählen"};if(d.ShowDialog()==true)WorkDirectory=d.FolderName;},Edit);
  AdkCommand=Command(()=>Launch("https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install"));
  ReleasesCommand=Command(()=>Launch("https://github.com/tobiaspitka/NovaForge/releases"));
  HistoryCommand=Command(()=>{var path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaForge","Versions");System.IO.Directory.CreateDirectory(path);Launch(path);});
  RecordCommand=new RelayCommand(()=>StartRecorder("Record"),()=>CanRecord);
  TestRecordingCommand=new RelayCommand(()=>StartRecorder("Test"),()=>CanTest);
  ImportRecordingCommand=new RelayCommand(ImportRecording,Selection);
  CompanionFolderCommand=Command(()=>{if(Selected==null)return;var dialog=new OpenFolderDialog{Title="Ordner mit Installer und Begleitdateien (Treiber/Daten) auswählen",InitialDirectory=Path.GetDirectoryName(Selected.Path)};if(dialog.ShowDialog()==true){if(!Path.GetDirectoryName(Path.GetFullPath(Selected.Path))!.TrimEnd('\\').Equals(Path.GetFullPath(dialog.FolderName).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Den Ordner auswählen, in dem die Installerdatei direkt liegt.");Selected.CompanionDirectory=dialog.FolderName;Status="Begleitordner wird vollständig in Sandbox und ISO übernommen.";}},Selection);
  UseRecordingCommand=new RelayCommand(async()=>{if(Selected==null||SelectedRecording==null)throw new InvalidOperationException("Programm und gespeicherte Aufnahme auswählen.");await RecordingLibrary.Attach(Selected,SelectedRecording);RecorderStatus=SelectedRecording.Tested?"Geprüfte Aufnahme zugeordnet. Bereit für ISO-Erstellung.":"Aufnahme zugeordnet. Jetzt frisch wiederholen.";},Selection);
  LibraryFolderCommand=Command(()=>{Directory.CreateDirectory(RecordingLibrary.Root);Launch(RecordingLibrary.Root);});
  DeleteRecordingCommand=new RelayCommand(DeleteRecording,()=>Editing&&!SandboxSessions.GuestRunning&&SelectedRecording!=null);
  EditRecordingCommand=new RelayCommand(EditRecording,()=>Editing&&SelectedRecording!=null);
  ExportRecordingCommand=Command(()=>{if(SelectedRecording is not {} entry)return;var safe=new string(entry.Name.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c).ToArray());var dialog=new SaveFileDialog{Filter="Installationsaufnahme|*.json",FileName=safe+".json",OverwritePrompt=true};if(dialog.ShowDialog()==true){File.Copy(entry.Path,dialog.FileName,true);RecorderStatus="Aufnahme exportiert: "+dialog.FileName;}},()=>Editing&&SelectedRecording!=null);
  ActivateSandboxCommand=new RelayCommand(async()=>{Busy=true;try{RecorderStatus="Windows Sandbox aktivieren … Administratorfreigabe bestätigen.";(await SandboxFeature.Read()).ValidateActivation();await SandboxSessions.Activate();await RefreshSandboxStatus();RecorderStatus="Aktivierung abgeschlossen. Windows bei Bedarf manuell neu starten; danach NovaForge erneut öffnen.";sandboxChecked=true;Notify(nameof(SandboxStatus));NotifyWizard();}finally{Busy=false;}},Edit);
  sessionTimer.Tick+=async(_,_)=>await PollSessions();sessionTimer.Start();_ = RefreshLibrary();_ = RefreshSandboxStatus();
  SessionFolderCommand=Command(()=>Launch(lastSession),()=>!string.IsNullOrEmpty(lastSession));
  AddCommand=Command(()=>{var d=new OpenFileDialog{Filter="Installer|*.msi;*.exe",Multiselect=true};if(d.ShowDialog()==true)AddFiles(d.FileNames);},Edit);
  RemoveCommand=Command(()=>{if(Selected is {} a){Programs.Remove(a);Selected=Programs.FirstOrDefault();Status="Programm entfernt. Abhängigkeiten werden beim Prüfen validiert.";}},Selection);
  UpCommand=Command(()=>Move(-1),Selection);DownCommand=Command(()=>Move(1),Selection);
  RelinkCommand=Command(()=>{if(Selected is {} a){var d=new OpenFileDialog{Filter="Installer|*.msi;*.exe"};if(d.ShowDialog()==true){a.Path=d.FileName;a.Sha256="";a.Verified=false;Status="Datei neu zugeordnet. Silent-Konfiguration erneut bestätigen.";}}},Selection);
  DependenciesCommand=Command(EditDependencies,Selection);
  SaveCommand=new RelayCommand(Save,Edit);OpenCommand=new RelayCommand(Open,Edit);
  ValidateCommand=Command(()=>{var ordered=Validation.Ordered(GetProject());if(SourceKind=="Folder")IsoBuilder.CheckMedia(IsoPath,ordered.Count==0&&!Setup.HasCustomization);else if(!System.IO.File.Exists(IsoPath))throw new InvalidOperationException("Quell-ISO fehlt.");if(!System.IO.File.Exists(OscdimgPath))throw new InvalidOperationException("oscdimg fehlt. ADK Deployment Tools installieren oder Werkzeug zuordnen.");Status="Konfiguration gültig: "+ordered.Count+" Programme. Medieninhalt wird beim Erstellen geprüft.";},Edit);
  BuildCommand=new RelayCommand(Build,()=>Editing&&!sessionActive&&SourceReady&&ToolReady&&OutputReady&&AllProgramsReady);CancelCommand=Command(()=>{cancellation?.Cancel();Status="Abbruch angefordert. Ressourcen werden kontrolliert freigegeben.";},()=>Busy&&cancellation!=null);
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
   var displayName=Path.GetFileNameWithoutExtension(file);if(!msi){try{var product=FileVersionInfo.GetVersionInfo(file).ProductName;if(!string.IsNullOrWhiteSpace(product)&&!new[]{"Setup","Installer","Inno Setup"}.Contains(product,StringComparer.OrdinalIgnoreCase))displayName=product.Trim();}catch{}}
   var app=new Installer{Name=displayName,Path=file,Profile=msi?Profiles[1]:Profiles[0],Verified=msi,AutomationMode="Recorded",Context="User"};Programs.Add(app);Selected=app;Tab=1;
  }
 }
 public void ApplyProfile(Installer a)
 {
  if(a.Type=="MSI") {a.Arguments="";a.Verified=true;return;}
  a.Arguments=a.Profile switch{"Inno Setup · Vorschlag"=>"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-","NSIS · Vorschlag"=>"/S","InstallShield · MSI-Wrapper Vorschlag"=>"/s /v\"/qn /norestart REBOOT=ReallySuppress\"",_=>""};a.Verified=false;
 }
 private void Move(int delta){if(Selected is {} a){var index=Programs.IndexOf(a);var next=index+delta;if(next>=0&&next<Programs.Count)Programs.Move(index,next);}}
 private Project GetProject()=>new(){RequireRecordings=true,SourceKind=SourceKind,IsoPath=IsoPath,IsoSha256=isoHash,OutputPath=OutputPath,OscdimgPath=OscdimgPath,WorkDirectory=WorkDirectory,Programs=Programs.ToList(),Setup=Setup,ReplayHostPortable=typeof(App).Assembly.Location.Length==0,ReplayHostPath=Environment.ProcessPath??""};
 private async Task StartRecorder(string mode){if(Selected is not {} app)return;if(sessionActive||SandboxSessions.GuestRunning)throw new InvalidOperationException("Zuerst die geöffnete Sandbox schließen. Aufnahme und Test brauchen getrennte, frische Sitzungen.");if(mode=="Test"&&!app.RecordingComplete)throw new InvalidOperationException("Zuerst Aufnahme und Prüfdatei vervollständigen.");Busy=true;NotifyWizard();try{RecorderStatus="Neue Sandbox für "+app.Name+" vorbereiten …";lastSession=await SandboxSessions.Prepare(app,mode,RecorderElevation,RecordingName);SandboxSessions.Launch(lastSession);sessionActive=true;sessionStarted=DateTime.UtcNow;sessionProgram=app.Id;sessionMode=mode;app.AutomationMode="Recorded";app.Context="User";RecorderStatus=(mode=="Record"?"Installation vormachen: ":"Frischer Test: ")+app.Name+". Danach Speichern und schließen. Übernahme erfolgt automatisch.";}finally{Busy=false;NotifyWizard();}}
 private async Task ImportRecording(){if(Selected is not {} app)return;var dialog=new OpenFileDialog{Filter="Recorder-Profil|profile.json;*.json",InitialDirectory=RecordingLibrary.Root};if(dialog.ShowDialog()!=true)return;var entry=await RecordingLibrary.Store(dialog.FileName,app.Name);await RecordingLibrary.Attach(app,entry);await RefreshLibrary();RecorderStatus=entry.Tested?"Geprüfte Aufnahme gespeichert und zugeordnet. Bereit für ISO.":"Aufnahme gespeichert und zugeordnet. Jetzt frisch wiederholen.";}
 private async Task RefreshLibrary(){try{var items=await RecordingLibrary.List();Recordings.Clear();foreach(var item in items)Recordings.Add(item);}catch(Exception e){RecorderStatus="Bibliothek konnte nicht gelesen werden: "+e.Message;}}
 private async Task DeleteRecording()
 {
  if(SelectedRecording is not {} entry)return;
  var linked=Programs.Where(x=>x.RecordedProfilePath.Equals(entry.Path,StringComparison.OrdinalIgnoreCase)).ToList();
  var window=new Window{Title="Aufnahme löschen",Owner=Application.Current.MainWindow,Width=600,Height=300,WindowStartupLocation=WindowStartupLocation.CenterOwner};
  var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(new TextBlock{Text="„"+entry.Name+"“ löschen?",FontSize=22,FontWeight=FontWeights.SemiBold});panel.Children.Add(new TextBlock{Text="Diese Aufnahme wird aus deiner Bibliothek entfernt. "+(linked.Count>0?linked.Count+" zugeordnete Programme benötigen danach eine andere Aufnahme oder eine neue Installation.":"Andere Aufnahmen bleiben erhalten.")+" Exportierte Dateien und lokale Versionssicherungen bleiben erhalten.",Margin=new Thickness(0,16,0,20),TextWrapping=TextWrapping.Wrap});
  var buttons=new WrapPanel();var cancel=new Button{Content="Behalten"};var remove=new Button{Content="Aufnahme löschen",Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107,52,68))};cancel.Click+=(_,_)=>window.DialogResult=false;remove.Click+=(_,_)=>window.DialogResult=true;buttons.Children.Add(cancel);buttons.Children.Add(remove);panel.Children.Add(buttons);window.Content=panel;
  if(window.ShowDialog()!=true)return;Busy=true;
  try{RecordingLibrary.Delete(entry);foreach(var app in linked){app.RecordedProfilePath="";app.RecordedProfileSha256="";app.Verified=false;}SelectedRecording=null;await RefreshLibrary();RecorderStatus="Aufnahme gelöscht: "+entry.Name;NotifyWizard();}finally{Busy=false;}
 }
 private async Task EditRecording(){if(SelectedRecording is not {} entry)return;var profile=await RecordedProfile.Load(entry.Path);var oldHash=profile.PlanHash();var window=new Window{Title="Aufnahme benennen / Prüfdatei ergänzen",Owner=Application.Current.MainWindow,Width=680,Height=420,WindowStartupLocation=WindowStartupLocation.CenterOwner};var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(new TextBlock{Text="Name meiner Aufnahme"});var label=new TextBox{Text=entry.Name};panel.Children.Add(label);panel.Children.Add(new TextBlock{Text="Installierte Prüfdatei (Zielpfad aus der Sandbox)"});var proof=new TextBox{Text=profile.VerificationFile};panel.Children.Add(proof);panel.Children.Add(new TextBlock{Text="Beispiel: {ProgramFiles}\\MeinProgramm\\app.exe oder {Windows}\\System32\\drivers\\treiber.sys. Die Datei muss im frischen Test neu entstehen. Änderungen am Zielpfad erfordern einen neuen Test.",TextWrapping=TextWrapping.Wrap});var error=new TextBlock{TextWrapping=TextWrapping.Wrap};panel.Children.Add(error);var save=new Button{Content="Aufnahme speichern",Margin=new Thickness(0,16,0,0)};save.Click+=async(_,_)=>{save.IsEnabled=false;var temporary=Path.Combine(Path.GetTempPath(),"NovaForge-Profile-"+Guid.NewGuid().ToString("N")+".json");try{profile.DisplayName=label.Text;profile.VerificationFile=UiAutomationEngine.Normalize(proof.Text.Trim());if(profile.PlanHash()!=oldHash){profile.Tested=false;profile.TestedPlanSha256="";}profile.Validate(false,false);await profile.Save(temporary);var updated=await RecordingLibrary.Store(temporary,label.Text);if(Selected!=null&&await Validation.Hash(Selected.Path)==updated.InstallerSha256)await RecordingLibrary.Attach(Selected,updated);await RefreshLibrary();RecorderStatus="Aufnahme gespeichert: "+updated.Name+(updated.Tested?" · geprüft":" · frisch testen");window.Close();}catch(Exception e){error.Text=e.Message;}finally{if(File.Exists(temporary))File.Delete(temporary);save.IsEnabled=true;}};panel.Children.Add(save);window.Content=panel;window.ShowDialog();}
 private async Task PollSessions(){if(polling||Busy)return;if(sessionActive&&DateTime.UtcNow-sessionStarted>TimeSpan.FromSeconds(8)&&!SandboxSessions.GuestRunning){sessionActive=false;RecorderStatus=Selected?.ReadyForIso==true?"Programm geprüft. Jetzt das nächste Programm vorbereiten oder die ISO erstellen.":Selected?.RecordingComplete==true?"Aufnahme gespeichert. Jetzt in einer neuen Sandbox testen.":"Sandbox geschlossen. Aufnahme bei Bedarf ergänzen oder erneut starten.";NotifyWizard();}if(!Directory.Exists(SandboxSessions.Root))return;polling=true;try{foreach(var dir in Directory.GetDirectories(SandboxSessions.Root)){try{var output=Path.Combine(dir,"Output","profile.json");if(File.Exists(output)&&!sessionHashes.ContainsKey(dir)){var acknowledgedHash=await Validation.Hash(output);if(await SessionExports.Accepted(Path.GetDirectoryName(output)!,acknowledgedHash)){importedSessions.Add(dir);sessionHashes[dir]=acknowledgedHash;}}if(File.Exists(output)&&(!sessionHashes.TryGetValue(dir,out var priorHash)||priorHash!=await Validation.Hash(output))){var transferHash=await Validation.Hash(output);var plan=await RecordedProfile.Load(output);var app=Programs.FirstOrDefault(a=>a.Sha256==plan.InstallerSha256);if(app==null){foreach(var candidate in Programs.Where(x=>File.Exists(x.Path))){if(await Validation.Hash(candidate.Path)==plan.InstallerSha256){app=candidate;break;}}}var session=System.Text.Json.JsonSerializer.Deserialize<RecorderSession>(await File.ReadAllTextAsync(Path.Combine(dir,"Input","session.json")),Project.JsonOptions);var entry=await RecordingLibrary.Store(output,app?.Name??session?.DisplayName??"Aufnahme");if(await Validation.Hash(output)!=transferHash)continue;if(app!=null)await RecordingLibrary.Attach(app,entry);await File.WriteAllTextAsync(Path.Combine(dir,"Output","accepted.txt"),transferHash);importedSessions.Add(dir);sessionHashes[dir]=transferHash;await RefreshLibrary();RecorderStatus=entry.Tested?"Geprüfte Aufnahme automatisch gespeichert und zugeordnet. Bereit für ISO.":"Aufnahme automatisch gespeichert und zugeordnet. Sandbox schließen lassen, dann frisch wiederholen.";}
 if(!SandboxSessions.GuestRunning&&(importedSessions.Contains(dir)||(!File.Exists(output)&&Directory.GetCreationTimeUtc(dir)<DateTime.UtcNow.AddMinutes(-2)))){var replayLog=Path.Combine(dir,"Output","replay.log");if(File.Exists(replayLog)){var diagnostics=Path.Combine(RecordingLibrary.Root,"Diagnostics");Directory.CreateDirectory(diagnostics);File.Copy(replayLog,Path.Combine(diagnostics,Path.GetFileName(dir)+"-replay.log"),true);}var error=Path.Combine(dir,"Output","test-error.txt");if(File.Exists(error)){var diagnostics=Path.Combine(RecordingLibrary.Root,"Diagnostics");Directory.CreateDirectory(diagnostics);File.Copy(error,Path.Combine(diagnostics,Path.GetFileName(dir)+".log"),true);RecorderStatus="Test fehlgeschlagen. Aufnahme bleibt ungeprüft. Diagnose in der Aufnahmebibliothek; Test nach Korrektur erneut starten.";}if(SandboxSessions.Cleanup(dir,out var cleanupPending)){if(Status.StartsWith("Temporäre Sitzungsdateien sind noch gesperrt.",StringComparison.Ordinal))Status="Temporäre Sitzungsdateien entfernt. Deine gespeicherten Aufnahmen bleiben in der Bibliothek.";if(lastSession==dir)lastSession="";CommandManager.InvalidateRequerySuggested();}else Status=cleanupPending;}}catch(IOException e){RecorderStatus="Speicherung wird erneut versucht: "+e.Message;}catch(System.Text.Json.JsonException){RecorderStatus="Aufnahme wird noch geschrieben; Übernahme wird wiederholt.";}catch(Exception e){RecorderStatus="Sitzungsübernahme: "+e.Message;}}}finally{polling=false;}}
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
   foreach(var a in p.Programs){if(!System.IO.File.Exists(a.Path))issues.Add(a.Name+": Datei fehlt.");else if(a.Sha256.Length>0&&await Validation.Hash(a.Path)!=a.Sha256){a.ResetRecordingState();issues.Add(a.Name+": Datei wurde verändert; Aufnahme erneut erstellen.");}}
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

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NovaForge.Core;
using Condition = System.Windows.Automation.Condition;
using Validation = NovaForge.Core.Validation;
namespace NovaForge.Automation;
public sealed class RecorderWindow : Window
{
 private readonly RecorderSession session;private readonly ObservableCollection<UiStep> steps=[];private readonly ListBox list=new();private readonly TextBlock status=new();private MouseRecorder? recorder;private Process? installer;private readonly HashSet<int> extra=[];private readonly RecordedProfile profile;private string verification="";private bool testing;
 public RecorderWindow(string sessionPath)
 {
  if(!Environment.UserName.Equals("WDAGUtilityAccount",StringComparison.OrdinalIgnoreCase)||!System.IO.Path.GetFullPath(sessionPath).Equals(@"C:\NovaForgeInput\session.json",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Aufzeichnung wird ausschließlich innerhalb der vorbereiteten Windows Sandbox gestartet.");
  session=System.Text.Json.JsonSerializer.Deserialize<RecorderSession>(File.ReadAllText(sessionPath),Project.JsonOptions)??throw new InvalidDataException("Sitzung fehlt.");
  if(session.InstallerFileName!=System.IO.Path.GetFileName(session.InstallerFileName)||session.Mode is not ("Record" or "Test"))throw new InvalidDataException("Sitzung ungültig.");
  profile=new(){InstallerSha256=session.InstallerSha256,InstallerArguments=session.InstallerArguments,RequiresElevation=session.RequiresElevation};
  Style=(Style)FindResource(typeof(Window));Title="NovaForge Beta · Installationsaufnahme";Width=900;Height=700;MinWidth=700;MinHeight=550;var panel=new DockPanel{Margin=new Thickness(24)};Content=panel;
  var head=new StackPanel();DockPanel.SetDock(head,Dock.Top);panel.Children.Add(head);head.Children.Add(new TextBlock{Text="BETA · "+session.DisplayName,FontSize=24,FontWeight=FontWeights.Bold});head.Children.Add(new TextBlock{Text="Nur in dieser Sandbox: Installation vormachen. Klicks werden als Steuerelemente aufgenommen. Textfelder werden nur ausdrücklich über den Aufnahme-Knopf gespeichert; Kennwort-/Lizenzfelder sind ausgeschlossen.",Margin=new Thickness(0,12,0,16)});
  var buttons=new WrapPanel();head.Children.Add(buttons);Button Add(string label,RoutedEventHandler handler){var button=new Button{Content=label,Margin=new Thickness(0,0,8,8)};button.Click+=handler;buttons.Children.Add(button);return button;}
  Add("Installer starten",async(_,_)=>await Start());Add("Textfeld: F8 im Installer",(_,_)=>status.Text="Textfeld im Installer fokussieren und F8 drücken. Inhalt vorher auf vertrauliche Angaben prüfen.");Add("Letzten Schritt entfernen",(_,_)=>{if(steps.Count>0)steps.RemoveAt(steps.Count-1);});Add("Prüfdatei auswählen",(_,_)=>{var d=new OpenFileDialog{Title="Neu installiertes Programm als Prüfdatei auswählen"};if(d.ShowDialog(this)==true){verification=UiAutomationEngine.Normalize(d.FileName);status.Text="Prüfdatei: "+verification;}});Add("Aufzeichnung speichern",async(_,_)=>await Save());Add("Installerfenster verbinden",(_,_)=>ConnectWindow());
  DockPanel.SetDock(status,Dock.Bottom);status.Margin=new Thickness(0,14,0,0);panel.Children.Add(status);list.ItemsSource=steps;panel.Children.Add(list);Closing+=(_,e)=>{if(testing||installer is {HasExited:false}){e.Cancel=true;status.Text="Installer zuerst abschließen oder abbrechen.";}else recorder?.Dispose();};
  SourceInitialized+=(_,_)=>{var source=(HwndSource)PresentationSource.FromVisual(this);source.AddHook((IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled)=>{if(msg==0x0312){try{recorder?.CaptureFocusedText();}catch(Exception e){Error(e);}handled=true;}return IntPtr.Zero;});RegisterHotKey(new WindowInteropHelper(this).Handle,781,0,0x77);};Closed+=(_,_)=>UnregisterHotKey(new WindowInteropHelper(this).Handle,781);
  if(session.Mode=="Test")Loaded+=async(_,_)=>await Test();
 }
 private void Error(Exception e){status.Text=e.Message;}
 private async Task Start(){try{if(session.Mode=="Test"||installer!=null)throw new InvalidOperationException("Diese Sitzung startet keinen weiteren Installer.");var path=System.IO.Path.Combine(@"C:\NovaForgeInput",session.InstallerFileName);if(await Validation.Hash(path)!=session.InstallerSha256)throw new InvalidDataException("Installer-Prüfsumme stimmt nicht.");installer=UiAutomationEngine.StartInstaller(path,session.InstallerArguments);recorder=new(UiAutomationEngine.ProcessFilter(installer.Id,path.EndsWith(".msi",StringComparison.OrdinalIgnoreCase),extra),step=>Dispatcher.BeginInvoke(()=>{steps.Add(step);list.ScrollIntoView(step);}));status.Text="Aufnahme läuft. Installer normal bedienen. Noch nicht als getestet freigegeben.";}catch(Exception e){Error(e);}}
 private void ConnectWindow(){var pick=new Window{Title="Installerfenster verbinden",Owner=this,Width=600,Height=400};var box=new ListBox();var candidates=new List<(int Pid,string Title)>();foreach(AutomationElement e in AutomationElement.RootElement.FindAll(TreeScope.Children,Condition.TrueCondition)){try{if(e.Current.ProcessId!=Environment.ProcessId&&e.Current.Name.Length>0)candidates.Add((e.Current.ProcessId,e.Current.Name));}catch{}}box.ItemsSource=candidates.Select(x=>x.Title).ToList();box.MouseDoubleClick+=(_,_)=>{if(box.SelectedIndex>=0){extra.Add(candidates[box.SelectedIndex].Pid);status.Text="Zusätzliches Installerfenster verbunden.";pick.Close();}};pick.Content=box;pick.ShowDialog();}
 private async Task Save(){try{if(installer==null||!installer.HasExited)throw new InvalidOperationException("Installer zuerst vollständig beenden.");if(installer.ExitCode is not (0 or 3010))throw new InvalidDataException("Installer meldet Fehler: "+installer.ExitCode);recorder?.Dispose();profile.Steps=steps.ToList();profile.VerificationFile=verification;profile.Validate();await profile.Save(@"C:\NovaForgeOutput\profile.json");status.Text="Aufzeichnung gespeichert. Sandbox schließen, auf dem Host importieren und in einer neuen Sandbox testen.";}catch(Exception e){Error(e);}}
 private async Task Test(){testing=true;try{var plan=await RecordedProfile.Load(@"C:\NovaForgeInput\profile.json");using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(30));await UiAutomationEngine.Replay(plan,System.IO.Path.Combine(@"C:\NovaForgeInput",session.InstallerFileName),s=>Dispatcher.Invoke(()=>status.Text=s),timeout.Token);plan.Tested=true;plan.TestedPlanSha256=plan.PlanHash();plan.TestedAt=DateTimeOffset.UtcNow;await plan.Save(@"C:\NovaForgeOutput\profile.json");status.Text="Wiederholung erfolgreich; neue Prüfdatei vorhanden. Getestetes Profil auf dem Host übernehmen.";}catch(Exception e){await File.WriteAllTextAsync(@"C:\NovaForgeOutput\test-error.txt",e.ToString());Error(e);}finally{testing=false;}}
 [DllImport("user32.dll")]private static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")]private static extern bool UnregisterHotKey(IntPtr h,int id);
}

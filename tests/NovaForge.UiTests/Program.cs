using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NovaForge;
using NovaForge.Core;
internal static class Program
{
 [STAThread] public static int Main(string[] args)
 {
  RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
  var directory=args.Length>0?args[0]:Path.Combine(Environment.CurrentDirectory,"artifacts","ui");Directory.CreateDirectory(directory);
  var app=new App();app.InitializeComponent();
  var errors=new List<string>();app.DispatcherUnhandledException+=(_,e)=>{errors.Add(e.Exception.ToString());e.Handled=true;};
  var window=new MainWindow();window.Show();
  if(window.Model.ContinueCommand.CanExecute(null))errors.Add("Beginner guide allows missing Windows source.");
  var media=Path.Combine(directory,"guide-media");Directory.CreateDirectory(media);
  window.Model.SourceKind="Folder";window.Model.IsoPath=media;
  if(!window.Model.ContinueCommand.CanExecute(null))errors.Add("Selected source does not enable guide continuation.");
  window.Model.ContinueCommand.Execute(null);
  if(window.Model.Tab!=1)errors.Add("Guide did not proceed to software.");
  window.Model.ContinueCommand.Execute(null);
  if(window.Model.Tab!=3)errors.Add("Guide without software did not skip recording.");
  window.Model.Tab=4;if(window.Model.ContinueCommand.CanExecute(null))errors.Add("Guide allows build without tool/output.");
  window.Model.Tab=0;
  var recorderSetup=NovaForge.Automation.MouseRecorder.Create(_=>false,_=>{});
  var a=new Installer{Name="Beispiel MSI",Path="C:\\Installationsdateien\\Beispiel.msi",Profile="MSI · Windows Installer",Verified=true};
  var b=new Installer{Name="Beispiel EXE",Path="C:\\Installationsdateien\\Programm.exe",Profile="Inno Setup · Vorschlag",Arguments="/custom-preserve",Verified=true};
  b.RecordedProfilePath="C:\\Aufnahmen\\123456789-recorded.json";b.RecordedProfileName="VNC mit Bildschirmtreiber";
  window.Model.Programs.Add(a);window.Model.Programs.Add(b);window.Model.Selected=b;
  window.Model.Tab=2;if(window.Model.ContinueCommand.CanExecute(null))errors.Add("Guide skips untested software.");window.Model.Tab=0;
  if(window.Model.BuildCommand.CanExecute(null))errors.Add("ISO creation enabled before software recording and fresh test.");
  if(window.Model.CurrentProgramName!=b.Name||!window.Title.Contains(b.Name))errors.Add("Program name is not reflected in guided header and title bar.");
  int page=0;bool? bitmapAvailable=null;var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(600)};
  timer.Tick+=(_,_)=>{
   window.UpdateLayout();
   var visual=(FrameworkElement)window.Content;var size=new Size(window.Width-32,window.Height-80);
   visual.Measure(size);visual.Arrange(new Rect(size));visual.UpdateLayout();
   if(!visual.IsLoaded||!visual.IsVisible||VisualTreeHelper.GetDescendantBounds(visual).Width<100)errors.Add($"Page {page} has no loaded layout.");
   if(bitmapAvailable==null){
    var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen())dc.DrawRectangle(Brushes.Red,null,new Rect(0,0,100,100));
    var probe=new RenderTargetBitmap(100,100,96,96,PixelFormats.Pbgra32);probe.Render(drawing);var pixels=new byte[40000];probe.CopyPixels(pixels,400,0);
    bitmapAvailable=pixels[2]==255&&pixels[3]==255;
   }
   var png=Path.Combine(directory,$"page-{page}.png");
   if(bitmapAvailable==true){
    var image=new RenderTargetBitmap((int)size.Width,(int)size.Height,96,96,PixelFormats.Pbgra32);image.Render(visual);
    var pixels=new byte[(int)size.Width*(int)size.Height*4];image.CopyPixels(pixels,(int)size.Width*4,0);
    var colors=new HashSet<int>();for(int i=0;i<pixels.Length;i+=64)colors.Add(BitConverter.ToInt32(pixels,i));
    if(colors.Count<20)errors.Add($"Page {page} rendered no usable content ({colors.Count} colors).");
    var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var s=File.Create(png))encoder.Save(s);
   }else if(File.Exists(png))File.Delete(png);
   if(page==1 && b.Arguments!="/custom-preserve")errors.Add("Profile binding changed custom parameters while switching selection.");
   if(page==1){System.Windows.Controls.TextBox? FindName(DependencyObject node){if(node is System.Windows.Controls.TextBox box&&System.Windows.Automation.AutomationProperties.GetAutomationId(box)=="AssignedRecordingName")return box;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var found=FindName(VisualTreeHelper.GetChild(node,i));if(found!=null)return found;}return null;}if(FindName(window)?.Text!="VNC mit Bildschirmtreiber")errors.Add("Assigned recording shows internal filename instead of user-defined name.");}
   page++;if(page==6){timer.Stop();window.Close();frame.Continue=false;}else{window.Model.Tab=page;if(page==1){window.Model.Selected=a;window.Model.Selected=b;}}
  };
  timer.Start();Dispatcher.PushFrame(frame);
  if(!recorderSetup.IsCompletedSuccessfully)errors.Add("Recorder setup on MTA worker did not complete while real WPF dispatcher remained responsive.");else recorderSetup.Result.Dispose();
  foreach(var e in errors)Console.Error.WriteLine(e);
  var status=errors.Count==0?"PASS: six real WPF pages loaded and laid out; no dispatcher exceptions; parameter selection preserved.":"FAIL: WPF layout/binding exceptions.";
  status+="\n"+(bitmapAvailable==true?"PASS: bitmap capture verified against red control drawing and content color count.":"SKIP: bitmap capture unavailable in current desktop session; even independent red drawing produced empty pixels. No blank screenshots delivered.");
  Console.WriteLine(status);File.WriteAllText(Path.Combine(directory,"render-status.txt"),status);
  return errors.Count==0?0:1;
 }
}

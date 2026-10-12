using System.Windows;
namespace NovaForge;
public partial class App : Application
{
 protected override void OnStartup(StartupEventArgs e) { base.OnStartup(e);DispatcherUnhandledException+=(s,a)=>{var path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NovaForge");System.IO.Directory.CreateDirectory(path);System.IO.File.AppendAllText(System.IO.Path.Combine(path,"application-error.log"),DateTimeOffset.Now+"\n"+a.Exception+"\n");MessageBox.Show(a.Exception.GetBaseException().Message,"NovaForge – Fehler",MessageBoxButton.OK,MessageBoxImage.Error);a.Handled=true;}; }
}

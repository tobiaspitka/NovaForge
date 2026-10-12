using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovaForge.Core;
namespace NovaForge;
public partial class MainWindow : Window
{
 public MainViewModel Model{get;}=new();
 public MainWindow(){InitializeComponent();DataContext=Model;Closing+=OnClosing;Closed+=(_,_)=>Model.Close();Model.EditorValid=()=>!HasValidationError(this);Model.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(MainViewModel.CurrentProgramName))Title=Model.Selected==null?"NovaForge Beta · Installationsstudio":"NovaForge Beta · "+Model.CurrentProgramName;if(e.PropertyName==nameof(MainViewModel.Setup))ClearPasswordFields(this);};}
 private static void ClearPasswordFields(DependencyObject node){if(node is PasswordBox box)box.Clear();for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)ClearPasswordFields(VisualTreeHelper.GetChild(node,i));}
 private static bool HasValidationError(DependencyObject node){if(System.Windows.Controls.Validation.GetHasError(node))return true;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)if(HasValidationError(VisualTreeHelper.GetChild(node,i)))return true;return false;}
 private void OnClosing(object? sender,CancelEventArgs e){if(Model.Busy){e.Cancel=true;Model.Status="Bitte den laufenden Vorgang erst abbrechen oder abschließen lassen.";}}
 private void FilesDragOver(object sender,DragEventArgs e){e.Effects=!Model.Busy&&e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
 private void FilesDropped(object sender,DragEventArgs e){if(e.Data.GetData(DataFormats.FileDrop) is string[] paths)Model.AddFiles(paths);}
 private void ProfileChanged(object sender,SelectionChangedEventArgs e){if(sender is ComboBox{DataContext:Installer a} box && (box.IsKeyboardFocusWithin||box.IsDropDownOpen) && e.RemovedItems.Count>0 && Model.Editing)Model.ApplyProfile(a);}
 private void LocalPasswordChanged(object sender,RoutedEventArgs e){if(sender is PasswordBox box && Model.Editing)Model.Setup.LocalUserPassword=box.Password;}
}

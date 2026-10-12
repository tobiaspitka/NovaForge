using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
namespace NovaForge.Automation;
// Harmless UI test fixture. Only creates the explicitly supplied temporary proof file.
public sealed class AutomationFixtureWindow : Window
{
 public AutomationFixtureWindow(string marker)
 {
  Title="NovaForge UIA Test";Width=450;Height=360;var panel=new StackPanel{Margin=new Thickness(20)};Content=panel;
  var check=new CheckBox{Content="Testkomponente"};AutomationProperties.SetAutomationId(check,"component");panel.Children.Add(check);
  var field=new TextBox{Text="initial"};AutomationProperties.SetAutomationId(field,"target");panel.Children.Add(field);
  var password=new PasswordBox();AutomationProperties.SetAutomationId(password,"secret");panel.Children.Add(password);
  var choice=new RadioButton{Content="Testauswahl"};AutomationProperties.SetAutomationId(choice,"choice");panel.Children.Add(choice);
  var open=new Button{Content="Testdialog öffnen"};AutomationProperties.SetAutomationId(open,"modal");panel.Children.Add(open);open.Click+=(_,_)=>{File.WriteAllText(marker+".modal-opened","opened");var dialog=new Window{Title="NovaForge UIA Dialog",Owner=this,Width=350,Height=160};var confirm=new Button{Content="Bestätigen",Margin=new Thickness(20)};AutomationProperties.SetAutomationId(confirm,"confirm");confirm.Click+=(_,_)=>{field.Text=choice.IsChecked==true?"selection-and-dialog-confirmed":"dialog-only";dialog.Close();};dialog.Content=confirm;dialog.ShowDialog();};
  var button=new Button{Content="Test abschließen"};AutomationProperties.SetAutomationId(button,"finish");panel.Children.Add(button);button.Click+=async(_,_)=>{Directory.CreateDirectory(System.IO.Path.GetDirectoryName(marker)!);File.WriteAllText(marker,(check.IsChecked==true?"enabled":"disabled")+"|"+field.Text+(password.Password.Length>0?"|"+password.Password:""));await Task.Delay(400);Close();};
 }
}

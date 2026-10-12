using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
namespace NovaForge.Automation;
// Harmless UI test fixture. Only creates the explicitly supplied temporary proof file.
public sealed class AutomationFixtureWindow : Window
{
 public AutomationFixtureWindow(string marker)
 {
  Title="NovaForge UIA Test";Width=450;Height=260;var panel=new StackPanel{Margin=new Thickness(20)};Content=panel;
  var check=new CheckBox{Content="Testkomponente"};AutomationProperties.SetAutomationId(check,"component");panel.Children.Add(check);
  var field=new TextBox{Text="initial"};AutomationProperties.SetAutomationId(field,"target");panel.Children.Add(field);
  var password=new PasswordBox();AutomationProperties.SetAutomationId(password,"secret");panel.Children.Add(password);
  var button=new Button{Content="Test abschließen"};AutomationProperties.SetAutomationId(button,"finish");panel.Children.Add(button);button.Click+=(_,_)=>{Directory.CreateDirectory(System.IO.Path.GetDirectoryName(marker)!);File.WriteAllText(marker,(check.IsChecked==true?"enabled":"disabled")+"|"+field.Text+(password.Password.Length>0?"|"+password.Password:""));Close();};
 }
}

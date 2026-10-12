using System.Text;
using System.Xml.Linq;

namespace NovaForge.Core;
public static class UnattendBuilder
{
    public static readonly XNamespace Namespace="urn:schemas-microsoft-com:unattend";
    private static readonly XNamespace Wcm="http://schemas.microsoft.com/WMIConfig/2002/State";
    private static XElement Component(string name)=>new(Namespace+"component",new XAttribute("name",name),new XAttribute("processorArchitecture","amd64"),new XAttribute("publicKeyToken","31bf3856ad364e35"),new XAttribute("language","neutral"),new XAttribute("versionScope","nonSxS"));
    public static string Build(SetupOptions options)
    {
        options.Validate();var u=Namespace;
        var root=new XElement(u+"unattend",new XAttribute(XNamespace.Xmlns+"wcm",Wcm));
        var pe=Component("Microsoft-Windows-Setup");var hardware=new List<string>();
        if(options.BypassTpmCheck)hardware.Add("BypassTPMCheck");
        if(options.BypassSecureBootCheck)hardware.Add("BypassSecureBootCheck");
        if(options.BypassRamCheck)hardware.Add("BypassRAMCheck");
        if(hardware.Count>0)pe.Add(new XElement(u+"RunSynchronous",hardware.Select((key,i)=>new XElement(u+"RunSynchronousCommand",new XAttribute(Wcm+"action","add"),new XElement(u+"Order",i+1),new XElement(u+"Path",$"reg.exe add HKLM\\SYSTEM\\Setup\\LabConfig /v {key} /t REG_DWORD /d 1 /f")))));
        if(pe.HasElements)root.Add(new XElement(u+"settings",new XAttribute("pass","windowsPE"),pe));
        var deployment=Component("Microsoft-Windows-Deployment");var commands=new List<string>();
        if(options.EnableOfflineButton)commands.Add("reg.exe add HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\OOBE /v BypassNRO /t REG_DWORD /d 1 /f");
        if(options.EnableLongPaths)commands.Add("reg.exe add HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem /v LongPathsEnabled /t REG_DWORD /d 1 /f");
        commands.Add("powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"%WINDIR%\\NovaForge\\Bootstrap.ps1\"");
        deployment.Add(new XElement(u+"RunSynchronous",commands.Select((command,i)=>new XElement(u+"RunSynchronousCommand",new XAttribute(Wcm+"action","add"),new XElement(u+"Order",i+1),new XElement(u+"Description","NovaForge Setup-Konfiguration"),new XElement(u+"Path",command),new XElement(u+"WillReboot","Never")))));
        var specialize=new XElement(u+"settings",new XAttribute("pass","specialize"),deployment);
        if(!string.IsNullOrEmpty(options.ComputerName)){var shell=Component("Microsoft-Windows-Shell-Setup");shell.Add(new XElement(u+"ComputerName",options.ComputerName));specialize.Add(shell);}
        root.Add(specialize);
        var oobeSettings=new XElement(u+"settings",new XAttribute("pass","oobeSystem"));var oobeShell=Component("Microsoft-Windows-Shell-Setup");var oobe=new XElement(u+"OOBE");
        if(options.CreateLocalUser)
        {
            oobe.Add(new XElement(u+"HideOnlineAccountScreens",true));
            // Windows SIM's documented password encoding is obfuscation, not encryption.
            var encoded=Convert.ToBase64String(Encoding.Unicode.GetBytes(options.LocalUserPassword+"Password"));
            oobeShell.Add(new XElement(u+"UserAccounts",new XElement(u+"LocalAccounts",new XElement(u+"LocalAccount",new XAttribute(Wcm+"action","add"),new XElement(u+"Name",options.LocalUserName),new XElement(u+"DisplayName",options.LocalUserName),new XElement(u+"Group",options.LocalUserAdministrator?"Administrators":"Users"),new XElement(u+"Password",new XElement(u+"Value",encoded),new XElement(u+"PlainText",false))))));
        }
        if(options.HideOemRegistration)oobe.Add(new XElement(u+"HideOEMRegistrationScreen",true));
        if(oobe.HasElements)oobeShell.Add(oobe);
        if(options.ConfigureTimeZone)oobeShell.Add(new XElement(u+"TimeZone",options.TimeZone));
        if(oobeShell.HasElements)oobeSettings.Add(oobeShell);
        if(options.ConfigureRegion){var intl=Component("Microsoft-Windows-International-Core");intl.Add(new XElement(u+"InputLocale",options.Region=="en-US"?"0409:00000409":options.Region=="de-AT"?"0c07:00000407":"0407:00000407"),new XElement(u+"SystemLocale",options.Region),new XElement(u+"UserLocale",options.Region));oobeSettings.Add(intl);}
        if(oobeSettings.HasElements)root.Add(oobeSettings);
        return new XDocument(new XDeclaration("1.0","utf-8",null),root).ToString();
    }
}

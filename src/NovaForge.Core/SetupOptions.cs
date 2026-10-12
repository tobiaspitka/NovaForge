using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NovaForge.Core;
public sealed class SetupOptions : Observable
{
    private bool createUser, administrator=true, offlineButton, bypassTpm, bypassSecureBoot, bypassRam, regionEnabled, timeZoneEnabled, hideOem, longPaths;
    private string userName="Benutzer", password="", computerName="", region="de-DE", timeZone="W. Europe Standard Time";
    public bool CreateLocalUser {get=>createUser;set=>Set(ref createUser,value);}
    public bool LocalUserAdministrator {get=>administrator;set=>Set(ref administrator,value);}
    public string LocalUserName {get=>userName;set=>Set(ref userName,value);}
    [JsonIgnore] public string LocalUserPassword {get=>password;set=>Set(ref password,value);}
    public bool EnableOfflineButton {get=>offlineButton;set=>Set(ref offlineButton,value);}
    public bool BypassTpmCheck {get=>bypassTpm;set=>Set(ref bypassTpm,value);}
    public bool BypassSecureBootCheck {get=>bypassSecureBoot;set=>Set(ref bypassSecureBoot,value);}
    public bool BypassRamCheck {get=>bypassRam;set=>Set(ref bypassRam,value);}
    public string ComputerName {get=>computerName;set=>Set(ref computerName,value);}
    public bool ConfigureRegion {get=>regionEnabled;set=>Set(ref regionEnabled,value);}
    public string Region {get=>region;set=>Set(ref region,value);}
    public bool ConfigureTimeZone {get=>timeZoneEnabled;set=>Set(ref timeZoneEnabled,value);}
    public string TimeZone {get=>timeZone;set=>Set(ref timeZone,value);}
    public bool HideOemRegistration {get=>hideOem;set=>Set(ref hideOem,value);}
    public bool EnableLongPaths {get=>longPaths;set=>Set(ref longPaths,value);}
    [JsonIgnore] public bool HasCustomization=>CreateLocalUser||EnableOfflineButton||BypassTpmCheck||BypassSecureBootCheck||BypassRamCheck||!string.IsNullOrWhiteSpace(ComputerName)||ConfigureRegion||ConfigureTimeZone||HideOemRegistration||EnableLongPaths;
    public void Validate()
    {
        if(CreateLocalUser)
        {
            if(string.IsNullOrWhiteSpace(LocalUserName)||LocalUserName.Length>20||LocalUserName.EndsWith('.')||LocalUserName.Any(c=>char.IsControl(c)||"\"/\\[]:;|=,+*?<>@".Contains(c))||LocalUserName is "." or "..")
                throw new InvalidDataException("Lokaler Benutzer: gültigen Kontonamen mit höchstens 20 Zeichen eingeben.");
            if(new[]{"Administrator","Guest","Gast","DefaultAccount","WDAGUtilityAccount"}.Contains(LocalUserName,StringComparer.OrdinalIgnoreCase))throw new InvalidDataException("Dieser Benutzername ist für ein Windows-Systemkonto reserviert.");
            if(LocalUserPassword.Length<8||LocalUserPassword.Length>127||LocalUserPassword.Any(char.IsControl))throw new InvalidDataException("Lokaler Benutzer: Kennwort mit 8–127 Zeichen eingeben. Es wird nicht in Projektdateien gespeichert.");
        }
        if(!string.IsNullOrEmpty(ComputerName)&&(!Regex.IsMatch(ComputerName,@"\A[A-Za-z0-9](?:[A-Za-z0-9-]{0,13}[A-Za-z0-9])?\z")||ComputerName.All(char.IsDigit)))throw new InvalidDataException("Computername: 1–15 Buchstaben, Zahlen oder Bindestriche; nicht ausschließlich Zahlen.");
        if(ConfigureRegion&&!new[]{"de-DE","de-AT","en-US"}.Contains(Region))throw new InvalidDataException("Nicht unterstützte Region.");
        if(ConfigureTimeZone&&!new[]{"W. Europe Standard Time","GMT Standard Time","UTC","Eastern Standard Time"}.Contains(TimeZone))throw new InvalidDataException("Nicht unterstützte Zeitzone.");
    }
}

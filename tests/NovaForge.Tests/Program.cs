using NovaForge.Core;
using System.Xml.Linq;
int passed=0;
void Test(string name,Action test){test();Console.WriteLine("PASS "+name);passed++;}
void Assert(bool value){if(!value)throw new Exception("Assertion failed");}
void Throws(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("Expected validation error");}
Installer App(string name)=>new(){Name=name,Path=System.IO.Path.GetFullPath(name+".exe"),Arguments="/silent",Verified=true};
var a=App("Alpha");var b=App("Beta");var c=App("Gamma");var p=new Project{Programs=[c,b,a]};
b.Dependencies=a.Id;c.Dependencies=b.Id;
Test("Stable topological dependency ordering",()=>Assert(Validation.Ordered(p,false).SequenceEqual(new[]{a,b,c})));
Test("System phase precedes user phase",()=>{var user=App("User");user.Context="User";var project=new Project{Programs=[user,a]};Assert(Validation.Ordered(project,false).SequenceEqual(new[]{a,user}));});
Test("Cycle rejected",()=>{a.Dependencies=c.Id;Throws(()=>Validation.Ordered(p,false));a.Dependencies="";});
Test("Disabled dependency rejected",()=>{a.Enabled=false;Throws(()=>Validation.Ordered(p,false));a.Enabled=true;});
Test("System -> user dependency rejected",()=>{a.Context="User";Throws(()=>Validation.Ordered(p,false));a.Context="System";});
Test("Duplicate paths rejected",()=>{var old=b.Path;b.Path=a.Path;Throws(()=>Validation.Ordered(p,false));b.Path=old;});
Test("Unverified EXE rejected",()=>{a.Verified=false;Throws(()=>Validation.Ordered(p,false));a.Verified=true;});
Test("Missing EXE silent arguments rejected",()=>{a.Arguments="";Throws(()=>Validation.Ordered(p,false));a.Arguments="/silent";});
Test("Timeout bounds",()=>{a.TimeoutSeconds=1;Throws(()=>Validation.Ordered(p,false));a.TimeoutSeconds=1800;});
Test("Malformed exitcodes rejected",()=>Throws(()=>Validation.ParseCodes("0,no")));
Test("Exitcode interpretation",()=>{Assert(Validation.Outcome(0,[0,3010])=="Success");Assert(Validation.Outcome(3010,[0,3010])=="RebootRequired");Assert(Validation.Outcome(5,[0])=="Failed");Assert(Validation.Outcome(1641,[0,1641])=="UnexpectedReboot");});
Test("Resume never repeats completed/running/failed",()=>{Assert(Validation.ShouldRun(null));Assert(Validation.ShouldRun("Pending"));foreach(var s in new[]{"Success","Running","Failed","Timeout","Interrupted","RebootRequired"})Assert(!Validation.ShouldRun(s));});
Test("Quotes preserve spaces / umlauts / trailing slash",()=>{Assert(IsoBuilder.Quote("C:\\Ördner mit Leerzeichen\\")=="\"C:\\Ördner mit Leerzeichen\\\\\"");Assert(IsoBuilder.Quote("a\"b")=="\"a\\\"b\"");});
Test("Answer file specialize / no reboot",()=>{var xml=XDocument.Parse(IsoBuilder.AnswerFile());XNamespace u="urn:schemas-microsoft-com:unattend";Assert(xml.Descendants(u+"settings").Single().Attribute("pass")!.Value=="specialize");Assert(xml.Descendants(u+"WillReboot").Single().Value=="Never");});
Test("Scripts embedded",()=>{foreach(var name in new[]{"Runner.ps1","Bootstrap.ps1","MountCopy.ps1"})Assert(IsoBuilder.Resource(name).Length>500);});
Test("Setup defaults preserve Windows setup",()=>{var xml=XDocument.Parse(UnattendBuilder.Build(new()));XNamespace u=UnattendBuilder.Namespace;Assert(xml.Descendants(u+"settings").Count()==1);Assert(!xml.ToString().Contains("LabConfig"));Assert(!xml.ToString().Contains("BypassNRO"));});
Test("Hardware flags are independent and run in WinPE",()=>{var xml=XDocument.Parse(UnattendBuilder.Build(new(){BypassTpmCheck=true}));XNamespace u=UnattendBuilder.Namespace;var pe=xml.Descendants(u+"settings").Single(x=>x.Attribute("pass")!.Value=="windowsPE");Assert(pe.ToString().Contains("BypassTPMCheck"));Assert(!pe.ToString().Contains("BypassSecureBootCheck"));Assert(!pe.ToString().Contains("BypassRAMCheck"));});
Test("Offline button registry precedes bootstrap",()=>{var xml=XDocument.Parse(UnattendBuilder.Build(new(){EnableOfflineButton=true}));XNamespace u=UnattendBuilder.Namespace;var paths=xml.Descendants(u+"Path").Select(x=>x.Value).ToArray();Assert(paths[0].Contains("BypassNRO"));Assert(paths[^1].Contains("Bootstrap.ps1"));Assert(xml.Descendants(u+"WillReboot").All(x=>x.Value=="Never"));});
Test("Local account password is correctly encoded and XML escaped",()=>{var options=new SetupOptions{CreateLocalUser=true,LocalUserName="Test&Name",LocalUserPassword="Secret<&123",LocalUserAdministrator=false};var xml=XDocument.Parse(UnattendBuilder.Build(options));XNamespace u=UnattendBuilder.Namespace;Assert(xml.Descendants(u+"Name").Single().Value=="Test&Name");Assert(xml.Descendants(u+"Group").Single().Value=="Users");var encoded=xml.Descendants(u+"Password").Single().Element(u+"Value")!.Value;Assert(System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(encoded))=="Secret<&123Password");Assert(!xml.ToString().Contains("Secret"));});
Test("Local-account password validation",()=>{Throws(()=>new SetupOptions{CreateLocalUser=true,LocalUserPassword="short"}.Validate());Throws(()=>new SetupOptions{CreateLocalUser=true,LocalUserName="Bad\\Name",LocalUserPassword="Example123"}.Validate());});
Test("Computer-name validation rejects command characters",()=>{Throws(()=>new SetupOptions{ComputerName="pc & whoami"}.Validate());Throws(()=>new SetupOptions{ComputerName="12345"}.Validate());new SetupOptions{ComputerName="NOVA-PC"}.Validate();});
Test("Region/timezone and long paths generated without UI language override",()=>{var xml=XDocument.Parse(UnattendBuilder.Build(new(){ConfigureRegion=true,Region="de-AT",ConfigureTimeZone=true,TimeZone="W. Europe Standard Time",ComputerName="NOVA-PC",EnableLongPaths=true,HideOemRegistration=true}));XNamespace u=UnattendBuilder.Namespace;Assert(xml.Descendants(u+"UserLocale").Single().Value=="de-AT");Assert(xml.Descendants(u+"InputLocale").Single().Value=="0c07:00000407");Assert(!xml.Descendants(u+"UILanguage").Any());Assert(xml.ToString().Contains("LongPathsEnabled"));Assert(xml.Descendants(u+"HideOEMRegistrationScreen").Single().Value=="true");});
Test("Setup-only project allowed",()=>Assert(Validation.Ordered(new Project{Setup=new(){BypassRamCheck=true}},false).Count==0));
var temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"NovaForge-Test-"+Guid.NewGuid());Directory.CreateDirectory(temp);
try{
 Test("Split SWM media validation and conflict detection",()=>{
  foreach(var file in new[]{"setup.exe","sources/boot.wim","sources/install.swm","sources/install2.swm","efi/boot/bootx64.efi","boot/etfsboot.com","efi/microsoft/boot/efisys.bin"}){var path=System.IO.Path.Combine(temp,file);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);File.WriteAllText(path,"fixture");}
  var guid=Guid.NewGuid();
  void Wim(string name,ushort part,ushort total){var bytes=new byte[208];new byte[]{77,83,87,73,77,0,0,0}.CopyTo(bytes,0);BitConverter.GetBytes(208u).CopyTo(bytes,8);guid.ToByteArray().CopyTo(bytes,24);BitConverter.GetBytes(part).CopyTo(bytes,40);BitConverter.GetBytes(total).CopyTo(bytes,42);BitConverter.GetBytes(1u).CopyTo(bytes,44);File.WriteAllBytes(System.IO.Path.Combine(temp,"sources",name),bytes);}
  Wim("boot.wim",1,1);Wim("install.swm",1,2);Wim("install2.swm",2,2);
  IsoBuilder.CheckMedia(temp);Assert(IsoBuilder.BootArgument(temp).StartsWith("-bootdata:2#p0,e,b"));File.WriteAllText(System.IO.Path.Combine(temp,"autounattend.xml"),"conflict");Throws(()=>IsoBuilder.CheckMedia(temp));File.Delete(System.IO.Path.Combine(temp,"autounattend.xml"));
  File.Delete(System.IO.Path.Combine(temp,"sources","install2.swm"));Throws(()=>IsoBuilder.CheckMedia(temp));
 });
 var projectPath=System.IO.Path.Combine(temp,"test.json");await p.Save(projectPath);var loaded=await Project.Load(projectPath);Test("JSON roundtrip preserves IDs/arguments/dependencies",()=>Assert(loaded.Programs[1].Dependencies==a.Id&&loaded.Programs[0].Id==c.Id));
 p.Setup=new(){CreateLocalUser=true,LocalUserName="Testkonto",LocalUserPassword="DoNotPersist123",EnableOfflineButton=true};await p.Save(projectPath);loaded=await Project.Load(projectPath);var saved=await File.ReadAllTextAsync(projectPath);Test("Project persists options but never password",()=>Assert(!saved.Contains("DoNotPersist")&&!saved.Contains("LocalUserPassword")&&loaded.Setup.LocalUserPassword==""&&loaded.Setup.EnableOfflineButton));
 await File.WriteAllTextAsync(projectPath,"{\"Version\":1,\"Programs\":[]}");loaded=await Project.Load(projectPath);Test("Version 1 project migrates to version 3",()=>Assert(loaded.Version==3&&!loaded.Setup.HasCustomization));
 var hashFile=System.IO.Path.Combine(temp,"hash");await File.WriteAllTextAsync(hashFile,"abc");var hash=await Validation.Hash(hashFile);Test("SHA256 known vector",()=>Assert(hash=="BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"));
}finally{Directory.Delete(temp,true);}
Console.WriteLine($"{passed} tests passed.");

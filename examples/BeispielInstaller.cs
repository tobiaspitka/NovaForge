using System;
using System.IO;
// Own, harmless offline example. Does not start other programs or download anything.
public static class BeispielInstaller
{
 public static int Main(string[] args)
 {
  if(args.Length!=1 || args[0]!="/silent")return 2;
  try{
   string target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"NovaForge-Beispiel");
   Directory.CreateDirectory(target);
   File.WriteAllText(Path.Combine(target,"Installationsnachweis.txt"),"NovaForge-Beispiel 1.0 wurde installiert.\r\n"+DateTimeOffset.Now.ToString("o")+"\r\nKonto: "+Environment.UserName);
   return 0;
  }catch{return 5;}
 }
}

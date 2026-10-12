using System;
using System.IO;
using System.Threading;
// Harmless installer substitute: records its arguments, optionally sleeps, returns requested code.
public class Fixture
{
 public static int Main(string[] args)
 {
  string marker=Environment.GetEnvironmentVariable("NOVAFORGE_TEST_MARKER");
  if(!String.IsNullOrEmpty(marker))File.AppendAllText(marker,String.Join("|",args)+Environment.NewLine);
  if(args.Length>1&&args[0]!="/i")Thread.Sleep(Int32.Parse(args[1]));
  return args.Length>0&&args[0]!="/i"?Int32.Parse(args[0]):0;
 }
}

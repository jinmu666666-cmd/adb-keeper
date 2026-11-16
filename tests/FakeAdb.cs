using System;
using System.IO;
using System.Linq;
class FakeAdb {
 static int Main(string[] args){string home=Environment.GetEnvironmentVariable("ADBKEEPER_FAKE_HOME");if(String.IsNullOrEmpty(home))return 2;Directory.CreateDirectory(home);File.AppendAllText(Path.Combine(home,"calls.log"),String.Join(" ",args)+"\n");
 string[] a=args;if(a.Length>=2&&a[0]=="-s")a=a.Skip(2).ToArray();string cmd=String.Join(" ",a);bool offline=File.Exists(Path.Combine(home,"offline"));
 if(cmd.StartsWith("devices")){Console.WriteLine("List of devices attached\nfixture-usb\t"+(offline?"offline":"device product:test model:Test_Phone"));return 0;}
 if(cmd=="reconnect"||cmd.StartsWith("connect ")){Console.WriteLine("reconnecting fixture");return 0;}
 if(offline){Console.Error.WriteLine("device offline");return 1;}
 if(cmd=="get-state"){Console.WriteLine("device");return 0;}
 if(cmd.StartsWith("shell getprop ")){Console.WriteLine("Fixture Phone");return 0;}
 if(cmd=="shell settings get secure android_id"){Console.WriteLine("0123456789abcdef");return 0;}
 if(a.Length>=5&&a[0]=="shell"&&a[1]=="settings"){
 string file=Path.Combine(home,a[4]+".txt");if(a[2]=="get"){Console.WriteLine(File.Exists(file)?File.ReadAllText(file):"null");return 0;}
 if(a[2]=="put"&&a.Length==6){File.WriteAllText(file,a[5]);return 0;}
 if(a[2]=="delete"){File.Delete(file);return 0;}
 }
 if(cmd.StartsWith("shell input keyevent ")||cmd.StartsWith("shell echo "))return 0;
 Console.Error.WriteLine("Unexpected fake command: "+cmd);return 2;}
}

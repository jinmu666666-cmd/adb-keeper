using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
class LifecycleChecks {
 static void Must(bool condition,string name){if(!condition)throw new Exception(name);}
 static object Field(object o,string name){return o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static async Task Call(object form,string name){await (Task)form.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);}
 static string home,data;
 static async Task<AdbKeeper> Open(){var f=new AdbKeeper();((TextBox)Field(f,"adb")).Text=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"FakeAdb.exe");f.Show();var deadline=DateTime.UtcNow.AddSeconds(15);do{await Task.Delay(10);if(DateTime.UtcNow>deadline)throw new Exception("Initial refresh timeout");}while((bool)Field(f,"busy"));return f;}
 static async Task RunScenarios(){
 string temp=Path.Combine(Path.GetTempPath(),"AdbKeeper-tests-"+Guid.NewGuid().ToString("N"));home=Path.Combine(temp,"phone");data=Path.Combine(temp,"app");Directory.CreateDirectory(home);Directory.CreateDirectory(data);Environment.SetEnvironmentVariable("ADBKEEPER_DATA_DIR",data);Environment.SetEnvironmentVariable("ADBKEEPER_FAKE_HOME",home);
 File.WriteAllText(Path.Combine(home,"stay_on_while_plugged_in.txt"),"0");File.WriteAllText(Path.Combine(home,"screen_off_timeout.txt"),"60000");
 using(var f=await Open()){
 Must(((ComboBox)Field(f,"devices")).Items.Count==1,"fixture discovery");await Call(f,"Start");
 Must(File.Exists(Path.Combine(data,"restore.json")),"write snapshot before phone changes");Must(File.ReadAllText(Path.Combine(home,"stay_on_while_plugged_in.txt"))=="7","charging awake");Must(File.ReadAllText(Path.Combine(home,"screen_off_timeout.txt"))=="1800000","screen timeout");
 var status=File.ReadAllText(Path.Combine(data,"status.json"));Must(status.Contains("Fixture Phone")&&status.Contains("0123456789abcdef"),"AI status identity");
 File.WriteAllText(Path.Combine(home,"offline"),"");await Call(f,"Tick");Must(File.ReadAllText(Path.Combine(data,"status.json")).Contains("连接异常"),"offline status");Must(File.ReadAllText(Path.Combine(home,"calls.log")).Contains("-s fixture-usb reconnect"),"device scoped retry");
 File.Delete(Path.Combine(home,"offline"));await Call(f,"Tick");await Call(f,"Stop");Must(!File.Exists(Path.Combine(data,"restore.json")),"recovery cleared");Must(File.ReadAllText(Path.Combine(home,"screen_off_timeout.txt"))=="60000","timeout restored");Must(File.ReadAllText(Path.Combine(home,"stay_on_while_plugged_in.txt"))=="0","charging restored");
 f.Close();Must(!f.Visible&&!f.IsDisposed,"X hides without disposing");Must(((NotifyIcon)Field(f,"tray")).Visible,"own tray remains available");await Call(f,"ExitApplication");Must(f.IsDisposed,"tray exit closes app");
 }
 File.WriteAllText(Path.Combine(data,"restore.json"),"{\"Serial\":\"fixture-usb\",\"Awake\":\"null\",\"Timeout\":\"45000\"}");
 using(var f=await Open()){await Call(f,"Stop");Must(!File.Exists(Path.Combine(home,"stay_on_while_plugged_in.txt")),"restore absent original setting");Must(File.ReadAllText(Path.Combine(home,"screen_off_timeout.txt"))=="45000","crash recovery");await Call(f,"ExitApplication");}
 File.WriteAllText(Path.Combine(data,"restore.json"),"invalid-json");
 using(var f=await Open()){Must((bool)Field(f,"recoveryInvalid"),"invalid snapshot detected");await Call(f,"Start");Must(!(bool)Field(f,"active"),"invalid snapshot blocks changes");Must(File.ReadAllText(Path.Combine(data,"restore.json"))=="invalid-json","invalid snapshot preserved");await Call(f,"ExitApplication");}
 Must(!File.ReadAllText(Path.Combine(home,"calls.log")).Contains("kill-server"),"no global server restart");
 Console.WriteLine("PASS: isolated fake-ADB lifecycle, offline retry, setting restore, crash recovery, corrupt record, own tray/X/exit. Fixtures: "+temp);}
 [STAThread]static int Main(){int result=1;Application.EnableVisualStyles();using(var host=new Form()){host.ShowInTaskbar=false;host.Opacity=0;host.Shown+=async delegate{try{await RunScenarios();result=0;}catch(Exception e){Console.Error.WriteLine(e);}finally{host.Close();}};Application.Run(host);}return result;}
}

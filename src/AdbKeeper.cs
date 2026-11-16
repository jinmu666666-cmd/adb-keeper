using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
class Snapshot { public string Serial, Awake, Timeout; }
class ConnectionNoticeState {
 bool online;
 public bool Observe(bool connected){bool disconnected=online&&!connected;online=connected;return disconnected;}
 public void Reset(){online=false;}
}
class CornerNotice : Form {
 Label message=new Label(),heading=new Label();System.Windows.Forms.Timer hide=new System.Windows.Forms.Timer();
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
 public CornerNotice(string text){Text="ADB 断连提示";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(350,158);BackColor=Color.FromArgb(29,65,94);Font=new Font("Microsoft YaHei UI",10);
 heading=new Label{Text="手机 ADB 连接已断开",ForeColor=Color.FromArgb(169,221,255),AutoSize=true,Location=new Point(14,12)};
 var dismiss=new Button{Text="×",AccessibleName="关闭断连提示",Size=new Size(28,28),Location=new Point(310,6),FlatStyle=FlatStyle.Flat,ForeColor=Color.White};dismiss.FlatAppearance.BorderSize=0;dismiss.Click+=delegate{Close();};
 message.Text=text;message.ForeColor=Color.FromArgb(239,248,255);message.Location=new Point(14,43);message.Size=new Size(320,105);Controls.Add(heading);Controls.Add(message);Controls.Add(dismiss);
 StartPosition=FormStartPosition.Manual;Shown+=delegate{var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-12,area.Bottom-Height-12);};hide.Interval=8000;hide.Tick+=delegate{Close();};hide.Start();FormClosed+=delegate{hide.Dispose();};}
 public void Recovered(){Text="ADB 连接已恢复";heading.Text="手机 ADB 连接已恢复";message.Text="手机连接已恢复，保持连接服务继续运行。";}
}
class DeviceItem {
 public string Serial, State, Model, DeviceId;
 public override string ToString(){return (String.IsNullOrEmpty(Model)?"型号待授权读取":Model)+" · 设备 ID："+(String.IsNullOrEmpty(DeviceId)?Serial:DeviceId)+" · "+(State=="device"?"已连接":State=="unauthorized"?"待手机授权":State=="offline"?"离线":State);}
 public static DeviceItem Parse(string line){var parts=line.Trim().Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(parts.Length<2||!System.Text.RegularExpressions.Regex.IsMatch(parts[0],@"^[a-zA-Z0-9._:\-]+$")||!new[]{"device","offline","unauthorized","recovery","sideload","bootloader"}.Contains(parts[1]))return null;return new DeviceItem{Serial=parts[0],State=parts[1],Model=parts.Where(p=>p.StartsWith("model:")).Select(p=>p.Substring(6).Replace('_',' ')).FirstOrDefault()};}
}
class AdbKeeper : Form {
 static string Root=AppDomain.CurrentDomain.BaseDirectory;
 static string Data=Path.GetFullPath(Environment.GetEnvironmentVariable("ADBKEEPER_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AdbKeeper"));
 static JavaScriptSerializer Json=new JavaScriptSerializer();
 TextBox adb=new TextBox(), log=new TextBox(); ComboBox devices=new ComboBox(); Label state=new Label();
 Button start=new Button(),refresh=new Button(),mirrorStart=new Button(); Process mirror; System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
 bool active,busy,closing,settingsApplied;string serial;DeviceItem selectedDevice;Snapshot original;DateTime retry=DateTime.MinValue;int ticks;
 System.Threading.EventWaitHandle showWindow=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,@"Local\AdbKeeper.Show"),exitTool=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,@"Local\AdbKeeper.Exit");
 NotifyIcon tray=new NotifyIcon(); ToolStripMenuItem keepaliveMenu=new ToolStripMenuItem(),mirrorMenu=new ToolStripMenuItem();
 System.Windows.Forms.Timer framework=new System.Windows.Forms.Timer();volatile bool exitRequested;bool exitInProgress,recoveryInvalid;Process adbCommand;readonly object commandLock=new object();
 ConnectionNoticeState connectionNotice=new ConnectionNoticeState();CornerNotice notice;
 [DllImport("kernel32.dll")]static extern uint SetThreadExecutionState(uint flags);
 string Recovery {get{return Path.Combine(Data,"restore.json");}}
 public AdbKeeper(){Text="ADB 连接助手";Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Size=new Size(840,620);MinimumSize=new Size(700,500);Font=new Font("Microsoft YaHei UI",10);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(242,248,254);ForeColor=Color.FromArgb(35,66,91);foreach(var button in new[]{start,refresh,mirrorStart}){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(221,239,253);button.FlatAppearance.BorderColor=Color.FromArgb(165,207,237);button.FlatAppearance.MouseOverBackColor=Color.FromArgb(199,228,249);}adb.BackColor=log.BackColor=devices.BackColor=Color.FromArgb(251,253,255);
 var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=7};Controls.Add(panel);
 panel.RowStyles.Add(new RowStyle(SizeType.Absolute,45));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,38));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,45));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,45));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,55));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
 panel.Controls.Add(new Label{Text="ADB 真机连接助手",Font=new Font(Font.FontFamily,20,FontStyle.Bold),AutoSize=true});
 adb.Dock=DockStyle.Fill;adb.Text=FindAdb();adb.AccessibleName="ADB 程序路径";panel.Controls.Add(adb);
 var devicebar=new FlowLayoutPanel{Dock=DockStyle.Fill};devices.Width=470;devices.DropDownWidth=850;devices.AccessibleName="手机型号和设备 ID";devices.DropDownStyle=ComboBoxStyle.DropDownList;refresh.Text="刷新设备";refresh.AutoSize=true;devicebar.Controls.Add(devices);devicebar.Controls.Add(refresh);panel.Controls.Add(devicebar);
 var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};start.Text="开始保持连接";start.AutoSize=true;buttons.Controls.Add(start);mirrorStart.Text="打开投屏";mirrorStart.AutoSize=true;buttons.Controls.Add(mirrorStart);panel.Controls.Add(buttons);
 state.Dock=DockStyle.Fill;state.Text="请选择手机。保持期间手机亮屏，电脑不自动休眠。";panel.Controls.Add(state);
 log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.Dock=DockStyle.Fill;panel.Controls.Add(log);
 panel.Controls.Add(new Label{Text="每 5 秒监测 · 断线重试间隔 15 秒 · 不重启全局 ADB 服务",AutoSize=true});
 refresh.Click+=async delegate{await RefreshDevices();};start.Click+=async delegate{if(active||original!=null)await Stop();else await Start();UpdateButtons();};
 mirrorStart.Click+=delegate{if(mirror!=null&&!mirror.HasExited)CloseMirror();else OpenMirror();UpdateButtons();};
 timer.Interval=5000;timer.Tick+=async delegate{await Tick();};
 var menu=new ContextMenuStrip();menu.Items.Add("打开窗口",null,delegate{Show();WindowState=FormWindowState.Normal;Activate();});
 keepaliveMenu.Click+=async delegate{if(busy||recoveryInvalid)return;if(active||original!=null)await Stop();else await Start();UpdateButtons();};
 mirrorMenu.Click+=delegate{if(mirror!=null&&!mirror.HasExited)CloseMirror();else OpenMirror();UpdateButtons();};
 menu.Items.Add(keepaliveMenu);menu.Items.Add(mirrorMenu);menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出",null,delegate{exitRequested=true;timer.Stop();CancelPendingCommand();});
 tray.Icon=Icon;tray.Text="ADB 连接助手";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=delegate{Show();WindowState=FormWindowState.Normal;Activate();};UpdateButtons();
 Shown+=async delegate {if(File.Exists(Recovery)){try{original=Json.Deserialize<Snapshot>(File.ReadAllText(Recovery));ValidateSnapshot(original);serial=original.Serial;state.Text="检测到上次未恢复的设置，请连接原手机后点击“重试恢复设置”。";}catch(Exception e){original=null;recoveryInvalid=true;state.Text="恢复记录无效，已禁止修改手机设置。请先备份并检查 restore.json。";Log("读取恢复记录失败："+e.Message);}UpdateButtons();}await RefreshDevices();};
 framework.Interval=100;framework.Tick+=async delegate{if(showWindow.WaitOne(0)){Show();WindowState=FormWindowState.Normal;Activate();}if(exitTool.WaitOne(0)){exitRequested=true;timer.Stop();if(!exitInProgress)CancelPendingCommand();}if(exitRequested&&!busy&&!exitInProgress){exitRequested=false;await ExitApplication();}};framework.Start();
 FormClosing+=delegate(object sender,FormClosingEventArgs e){if(closing)return;e.Cancel=true;if(e.CloseReason!=CloseReason.WindowsShutDown){Hide();return;}exitRequested=true;};
 FormClosed+=delegate{tray.Visible=false;tray.Dispose();menu.Dispose();timer.Dispose();framework.Stop();framework.Dispose();showWindow.Dispose();exitTool.Dispose();};}
 static void ValidateSnapshot(Snapshot value){long n;if(value==null||String.IsNullOrEmpty(value.Serial)||!System.Text.RegularExpressions.Regex.IsMatch(value.Serial,@"^[a-zA-Z0-9._:\-]+$")||!(value.Awake=="null"||long.TryParse(value.Awake,out n))||!(value.Timeout=="null"||long.TryParse(value.Timeout,out n)))throw new InvalidDataException("Invalid recovery snapshot");}
 async Task ExitApplication(){exitInProgress=true;try{await Stop();if(original!=null){Log("手机设置待恢复，恢复记录已保留。");Show();if(MessageBox.Show(this,"手机未连接，无法恢复原亮屏设置。退出会保留恢复记录，下次启动请连接原手机重试。仍要退出吗？","设置尚未恢复",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;}CloseMirror();if(notice!=null&&!notice.IsDisposed)notice.Close();closing=true;Close();}finally{exitInProgress=false;}}
 void ObserveConnection(bool connected){bool alert=connectionNotice.Observe(connected);if(connected&&notice!=null&&!notice.IsDisposed)notice.Recovered();if(!alert)return;if(notice!=null&&!notice.IsDisposed)notice.Close();string model=selectedDevice==null?serial:selectedDevice.Model;string id=selectedDevice==null?serial:selectedDevice.DeviceId??serial;notice=new CornerNotice(model+"\r\n设备 ID："+id+"\r\n"+DateTime.Now.ToString("HH:mm:ss")+" · 正在自动重连，请检查连接。");notice.Show();}
 void UpdateButtons(){UpdateTrayMenu();start.Text=active?"停止保持连接":original!=null?"重试恢复设置":"开始保持连接";start.Enabled=!busy&&!recoveryInvalid;mirrorStart.Text=mirror!=null&&!mirror.HasExited?"关闭投屏":"打开投屏";}
 void UpdateTrayMenu(){keepaliveMenu.Text=active?"停止保持连接":original!=null?"重试恢复设置":"开始保持连接";keepaliveMenu.Enabled=!busy&&!recoveryInvalid;mirrorMenu.Text=mirror!=null&&!mirror.HasExited?"关闭投屏":"打开投屏";}
 void MirrorLog(string text){if(String.IsNullOrEmpty(text)||IsDisposed||!IsHandleCreated)return;try{BeginInvoke((Action)delegate{if(!IsDisposed){Log("投屏："+text);UpdateButtons();}});}catch(InvalidOperationException){}}
 void OpenMirror(){try{
 if(mirror!=null&&!mirror.HasExited){Log("投屏窗口已打开");return;}
 var item=active?selectedDevice:devices.SelectedItem as DeviceItem;
 if(item==null||item.State!="device")throw new Exception("请选择已授权且在线的手机，再打开投屏。");
 string exe=Path.Combine(Root,"scrcpy","scrcpy.exe");if(!File.Exists(exe))throw new Exception("投屏组件缺失，请先运行 Install-Dependencies.cmd 下载官方组件。");
 var info=new ProcessStartInfo(exe,"--serial="+item.Serial+" --no-audio --max-size=1600 --window-title=\""+(item.Model??"Android").Replace("\"","")+" - "+(item.DeviceId??item.Serial)+"\""){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(exe),RedirectStandardOutput=true,RedirectStandardError=true};
 info.EnvironmentVariables["ADB"]=adb.Text;
 mirror=new Process{StartInfo=info,EnableRaisingEvents=true};mirror.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){MirrorLog(e.Data);};mirror.ErrorDataReceived+=delegate(object s,DataReceivedEventArgs e){MirrorLog(e.Data);};mirror.Exited+=delegate{MirrorLog("进程已退出；需要时点击“打开投屏”重新连接。");};
 mirror.Start();mirror.BeginOutputReadLine();mirror.BeginErrorReadLine();Log("正在投屏："+item);
 }catch(Exception e){Log("投屏失败："+e.Message);MessageBox.Show(e.Message,"投屏失败");}}
 void CloseMirror(){try{if(mirror!=null&&!mirror.HasExited){mirror.CloseMainWindow();if(!mirror.WaitForExit(2000))mirror.Kill();Log("投屏已关闭");}}catch(Exception e){Log("关闭投屏失败："+e.Message);}}
 static string FindAdb(){string bundled=Path.Combine(Root,"scrcpy","adb.exe");if(File.Exists(bundled))return bundled;string local=Path.Combine(Root,"platform-tools","adb.exe");if(File.Exists(local))return local;foreach(var p in new[]{Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),Environment.GetEnvironmentVariable("ANDROID_HOME"),@"C:\android-sdk",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Android","Sdk")}){if(String.IsNullOrEmpty(p))continue;string f=Path.Combine(p,"platform-tools","adb.exe");if(File.Exists(f))return f;}return "adb.exe";}
 void CancelPendingCommand(){lock(commandLock){if(adbCommand!=null)try{if(!adbCommand.HasExited)adbCommand.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}}
 async Task<string> Run(string args){if(exitRequested&&!exitInProgress)throw new OperationCanceledException("正在退出");string executable=adb.Text;return await Task.Run(delegate{using(var p=new Process{StartInfo=new ProcessStartInfo(executable,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}}){try{lock(commandLock){if(exitRequested&&!exitInProgress)throw new OperationCanceledException("正在退出");p.Start();adbCommand=p;}var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(7000)){try{p.Kill();}catch(InvalidOperationException){}throw new Exception("ADB 命令超时");}Task.WaitAll(output,error);if(exitRequested&&!exitInProgress)throw new OperationCanceledException("正在退出");if(p.ExitCode!=0)throw new Exception(error.Result.Trim()+" "+output.Result.Trim());return output.Result.Trim();}finally{lock(commandLock){if(adbCommand==p)adbCommand=null;}}}});}
 string ForDevice(string args){if(String.IsNullOrEmpty(serial)||!System.Text.RegularExpressions.Regex.IsMatch(serial,@"^[a-zA-Z0-9._:\-]+$"))throw new Exception("设备序列号无效");return "-s "+serial+" "+args;}
 void Log(string text){string line=DateTime.Now.ToString("HH:mm:ss")+"  "+text;log.AppendText(line+Environment.NewLine);try{File.AppendAllText(Path.Combine(Data,"connection.log"),line+Environment.NewLine);}catch{} }
 void Status(string text){state.Text=text;File.WriteAllText(Path.Combine(Data,"status.json"),Json.Serialize(new{device=serial,model=selectedDevice==null?null:selectedDevice.Model,deviceId=selectedDevice==null?null:selectedDevice.DeviceId,active=active,state=text,updatedAt=DateTimeOffset.Now.ToString("o"),adb=adb.Text}));}
 async Task RefreshDevices(){if(busy||active)return;busy=true;try {
 string previous=devices.SelectedItem is DeviceItem?((DeviceItem)devices.SelectedItem).Serial:null;
 string output=await Run("devices -l");devices.Items.Clear();foreach(var line in output.Split('\n')){
 var item=DeviceItem.Parse(line);if(item==null)continue;
 if(item.State=="device"){try {string prefix="-s "+item.Serial+" shell ";string market=await Run(prefix+"getprop ro.product.marketname");if(!String.IsNullOrWhiteSpace(market))item.Model=market;
 if(String.IsNullOrWhiteSpace(item.Model))item.Model=await Run(prefix+"getprop ro.product.model");
 string id=await Run(prefix+"settings get secure android_id");if(System.Text.RegularExpressions.Regex.IsMatch(id,@"^[0-9a-fA-F]{16}$"))item.DeviceId=id;
 }catch(Exception e){Log("设备信息读取失败（"+item.Serial+"）："+e.Message);}}
 devices.Items.Add(item);if(item.Serial==previous)devices.SelectedItem=item;
 }if(devices.SelectedIndex<0&&devices.Items.Count>0)devices.SelectedIndex=0;Log("设备列表已刷新："+devices.Items.Count+" 台（型号 + 设备 ID；内部按 ADB 序列号发送命令）");
 }catch(Exception e){Log(e.Message);}finally{busy=false;UpdateButtons();}}
 async Task Start(){if(busy||active||original!=null||recoveryInvalid)return;selectedDevice=devices.SelectedItem as DeviceItem;if(selectedDevice==null){MessageBox.Show("请先连接手机并刷新设备。");return;}serial=selectedDevice.Serial;connectionNotice.Reset();active=true;refresh.Enabled=adb.Enabled=devices.Enabled=false;SetThreadExecutionState(0x80000003);Log("开始监测 "+selectedDevice+" · ADB 序列号："+serial);timer.Start();UpdateButtons();await Tick();}
 async Task Tick(){if(busy||!active)return;busy=true;Exception failure=null;bool checkingConnection=true;try {string online=await Run(ForDevice("get-state"));if(online!="device")throw new Exception("设备状态："+online);checkingConnection=false;
 if(original==null){string awake=await Run(ForDevice("shell settings get global stay_on_while_plugged_in"));string timeout=await Run(ForDevice("shell settings get system screen_off_timeout"));long n;if(!(awake=="null"||long.TryParse(awake,out n))||!(timeout=="null"||long.TryParse(timeout,out n)))throw new Exception("无法读取原亮屏设置");original=new Snapshot{Serial=serial,Awake=awake,Timeout=timeout};File.WriteAllText(Recovery,Json.Serialize(original));} if(!settingsApplied){await Run(ForDevice("shell settings put global stay_on_while_plugged_in 7"));await Run(ForDevice("shell settings put system screen_off_timeout 1800000"));settingsApplied=true;Log("原设置已保存；已启用充电常亮和 30 分钟熄屏超时");}
 checkingConnection=true; if(ticks++%6==0)await Run(ForDevice("shell input keyevent KEYCODE_WAKEUP"));else await Run(ForDevice("shell echo adb-keeper-heartbeat"));ObserveConnection(true);Status("已连接 · "+(selectedDevice==null?serial:selectedDevice.ToString())+" · "+DateTime.Now.ToString("HH:mm:ss"));
 }catch(Exception e){failure=e;} if(exitRequested){busy=false;UpdateButtons();return;} if(failure!=null){if(checkingConnection)ObserveConnection(false);Status("连接异常 · "+failure.Message);Log(failure.Message);if(DateTime.UtcNow>=retry){retry=DateTime.UtcNow.AddSeconds(15);try{string listing=await Run("devices");if(listing.Contains(serial+"\tunauthorized")){Log("请在手机上确认 USB 调试授权");}else if(System.Text.RegularExpressions.Regex.IsMatch(serial,@"^\d+\.\d+\.\d+\.\d+:\d+$")){Log(await Run("connect "+serial));}else{Log(await Run(ForDevice("reconnect")));}}catch(Exception retryError){Log("重连："+retryError.Message);}}}busy=false;UpdateButtons();}
 async Task Stop(){if(busy){state.Text="命令执行中，请稍后再次停止。";return;}timer.Stop();connectionNotice.Reset();if(notice!=null&&!notice.IsDisposed)notice.Close();active=false;SetThreadExecutionState(0x80000000);busy=true;try{if(original!=null){serial=original.Serial;await Restore("global","stay_on_while_plugged_in",original.Awake);await Restore("system","screen_off_timeout",original.Timeout);File.Delete(Recovery);original=null;settingsApplied=false;Log("原始亮屏设置已恢复");}Status("已停止 · 电脑休眠策略已恢复");start.Enabled=true;refresh.Enabled=adb.Enabled=devices.Enabled=true;}catch(Exception e){Status("已停止监测，但手机设置待恢复："+e.Message);Log(state.Text);}finally{busy=false;UpdateButtons();}}
 Task<string> Restore(string ns,string key,string value){return Run(ForDevice("shell settings "+(value=="null"?"delete "+ns+" "+key:"put "+ns+" "+key+" "+value)));}
 [STAThread]static void Main(){Directory.CreateDirectory(Data);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);using(var mutex=new System.Threading.Mutex(false,"AdbKeeper.Standalone")){if(!mutex.WaitOne(0)){try{using(var signal=System.Threading.EventWaitHandle.OpenExisting(@"Local\AdbKeeper.Show"))signal.Set();}catch(System.Threading.WaitHandleCannotBeOpenedException){}return;}try{Application.Run(new AdbKeeper());}finally{mutex.ReleaseMutex();}}}
}

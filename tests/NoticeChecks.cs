using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
class NoticeChecks {
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr hwnd,int index);
 [STAThread]static int Main(){try{Application.EnableVisualStyles();var before=GetForegroundWindow();using(var toast=new CornerNotice("功能验证\r\n不影响真实 ADB 连接")){toast.Show();Application.DoEvents();if(GetForegroundWindow()!=before)throw new Exception("Notification stole focus");if((GetWindowLong(toast.Handle,-20)&0x08000000)==0)throw new Exception("NOACTIVATE flag missing");var area=Screen.PrimaryScreen.WorkingArea;if(toast.Right>area.Right||toast.Bottom>area.Bottom||toast.Left<area.Left||toast.Top<area.Top)throw new Exception("Notification outside work area");toast.Close();}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"notice-check.txt"),"PASS: no focus stolen, NOACTIVATE flag, notification inside screen work area.");return 0;}catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"notice-check.txt"),e.ToString());return 1;}}
}

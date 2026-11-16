using System;
class DeviceChecks {
 static void Must(bool value,string name){if(!value)throw new Exception(name);}
 [STAThread] static void Main(){
 var notices=new ConnectionNoticeState();Must(!notices.Observe(false),"initial offline must not claim a disconnection");Must(!notices.Observe(true),"connected state");Must(notices.Observe(false),"disconnect must notify");Must(!notices.Observe(false),"persistent offline must not repeat");notices.Observe(true);Must(notices.Observe(false),"second disconnection must notify");notices.Reset();Must(!notices.Observe(false),"manual stop must not notify");
 Must(DeviceItem.Parse("List of devices attached")==null,"header");
 Must(DeviceItem.Parse("* daemon started successfully *")==null,"daemon message");
 Must(DeviceItem.Parse("bad\"serial device")==null,"unsafe serial");
 var first=DeviceItem.Parse("usb-first device product:fuxi model:2211133C device:fuxi transport_id:36");
 var second=DeviceItem.Parse("192.168.1.5:42345 device model:2211133C");
 first.Model=second.Model="Xiaomi 13";first.DeviceId="1111111111111111";second.DeviceId="2222222222222222";
 Must(first.ToString()!=second.ToString(),"same model must show distinct device IDs");
 Must(first.Serial=="usb-first"&&second.Serial=="192.168.1.5:42345","display must preserve transport serial");
 Must(DeviceItem.Parse("usb-first unauthorized").ToString().Contains("待手机授权"),"authorization state");
 Must(DeviceItem.Parse("usb-first offline").ToString().Contains("离线"),"offline state");
 Console.WriteLine("PASS: header, daemon, serial validation, same-model IDs, transport binding, authorization and offline state.");
 }
}

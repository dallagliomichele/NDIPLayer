using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using ImageNdi;
class ModuleIntegrationHost {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(object o,string n){return typeof(MainWindow).GetField(n,F).GetValue(o);}static void Put(object o,string n,object v){typeof(MainWindow).GetField(n,F).SetValue(o,v);}
 sealed class Sender:IVideoSender {public void Send(byte[] p,int w,int h,int fps){Thread.Sleep(10);}public void Dispose(){}}
 [STAThread] static void Main(){string dir=Path.Combine(Path.GetTempPath(),"ndiplayer-module-"+Guid.NewGuid());Directory.CreateDirectory(dir);try{
  string a=Path.Combine(dir,"A.png"),b=Path.Combine(dir,"B.png");foreach(string path in new[]{a,b})using(var image=new Bitmap(8,8)){using(var g=Graphics.FromImage(image))g.Clear(Color.Coral);image.Save(path);}
  var data=new Settings{Root=dir,OutputWidth=16,OutputHeight=16,OutputFps=50,EscClearMs=2000};var first=ProjectStore.AddBlock(data,"Apertura");var second=ProjectStore.AddBlock(data,"Intervallo");ProjectStore.AddFiles(first,new[]{a,b});ProjectStore.AddFiles(second,new[]{a,b});first.Numbers[a]="1";first.Numbers[b]="12";second.Numbers[a]="12";second.Numbers[b]="1";data.ActiveBlockId=first.Id;
  var reserved=new TcpListener(IPAddress.Loopback,0);reserved.Start();int port=((IPEndPoint)reserved.LocalEndpoint).Port;reserved.Stop();data.Companion.ControlEnabled=true;data.Companion.ControlAddress="127.0.0.1";data.Companion.ControlPort=port;string prefs=Path.Combine(dir,"prefs.json");File.WriteAllText(prefs,new JavaScriptSerializer().Serialize(data));
  var messages=new ConcurrentQueue<string>();new Thread(delegate(){string line;while((line=Console.ReadLine())!=null){messages.Enqueue(line);if(line=="QUIT")return;}messages.Enqueue("QUIT");}){IsBackground=true}.Start();
  using(var window=new MainWindow(prefs)){window.ShowInTaskbar=false;window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-3000,-3000);window.Show();var program=new Playout(new Sender(),16,16,50);Put(window,"output",program);Put(window,"outW",16);Put(window,"outH",16);Put(window,"activeProgramFps",50);program.Cue(Renderer.Render(a,16,16,new Placement()),0,a);Console.WriteLine("READY "+port);bool done=false;while(!done){Application.DoEvents();string message;while(messages.TryDequeue(out message)){if(message=="QUIT"){done=true;break;}if(message=="STATE"){var current=(Settings)Get(window,"settings");string selected=(string)Get(window,"selected");Console.WriteLine("STATE "+new JavaScriptSerializer().Serialize(new{selected=selected==null?null:Path.GetFileName(selected),program=program.ReadOnAirStatus().Paths.Select(Path.GetFileName).ToArray(),clearing=program.Clearing,enabled=Get(window,"output")!=null,block=current.Blocks.Find(x=>x.Id==current.ActiveBlockId).Name}));}}Thread.Sleep(2);}}
 }catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}finally{Directory.Delete(dir,true);}}
}

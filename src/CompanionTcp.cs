using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ImageNdi {
public class CompanionSettings {
 public bool Enabled {get;set;} public string Address {get;set;} public int Port {get;set;} public string OnAirCommand {get;set;} public string EmptyCommand {get;set;}
 public CompanionSettings(){Address="127.0.0.1";Port=51234;OnAirCommand="";EmptyCommand="";}
 public CompanionSettings Copy(){return new CompanionSettings{Enabled=Enabled,Address=Address,Port=Port,OnAirCommand=OnAirCommand,EmptyCommand=EmptyCommand};}
 public void Validate(){IPAddress parsed;if(!IPAddress.TryParse(Address,out parsed))throw new ArgumentException("Inserisci un indirizzo IP valido per Companion.");if(Port<1||Port>65535)throw new ArgumentException("La porta TCP deve essere compresa tra 1 e 65535.");Line(OnAirCommand);Line(EmptyCommand);}
 public static string Line(string command){string value=(command??"").TrimEnd('\r','\n');if(value.Length>4096||value.IndexOfAny(new[]{'\r','\n','\0'})>=0)throw new ArgumentException("Ogni stringa Companion deve contenere un solo comando, massimo 4096 caratteri. Il ritorno a capo viene aggiunto automaticamente.");return value;}
}
// Only queues on the video thread. Socket I/O happens on this separate bounded worker.
public sealed class CompanionTcp:IDisposable {
 sealed class Work {public CompanionSettings Config;public string Command;public bool Test;public long Generation;}
 readonly object gate=new object();readonly BlockingCollection<Work> queue=new BlockingCollection<Work>(32);readonly Thread worker;
 CompanionSettings config=new CompanionSettings();bool stopping;long generation;TcpClient activeClient;int pending;
 public volatile string Status="Companion TCP: disabilitato";
 public CompanionTcp(){worker=new Thread(Run){IsBackground=true,Name="Companion TCP"};worker.Start();}
 public void Configure(CompanionSettings value){value.Validate();lock(gate){if(stopping)return;config=value.Copy();generation++;Work discarded;while(queue.TryTake(out discarded))Interlocked.Decrement(ref pending);if(activeClient!=null)activeClient.Close();Status=config.Enabled?"Companion TCP: abilitato, in attesa di eventi":"Companion TCP: disabilitato";}}
 public void Presence(bool hasImage){lock(gate){if(stopping||!config.Enabled)return;string command=CompanionSettings.Line(hasImage?config.OnAirCommand:config.EmptyCommand);if(command.Length==0)return;Interlocked.Increment(ref pending);if(!queue.TryAdd(new Work{Config=config.Copy(),Command=command,Generation=generation})){Interlocked.Decrement(ref pending);Status="Companion TCP: coda piena, evento non inviato";}}}
 public void Test(CompanionSettings value){value.Validate();lock(gate){if(stopping)return;Status="Companion TCP: test connessione in corso…";Interlocked.Increment(ref pending);if(!queue.TryAdd(new Work{Config=value.Copy(),Test=true,Generation=generation})){Interlocked.Decrement(ref pending);Status="Companion TCP: coda piena";}}}
 void Run(){foreach(var work in queue.GetConsumingEnumerable()){
  lock(gate){if(stopping){Interlocked.Decrement(ref pending);break;}if(work.Generation!=generation){Interlocked.Decrement(ref pending);continue;}}
  try{IPAddress address=IPAddress.Parse(work.Config.Address);using(var client=new TcpClient(address.AddressFamily)){client.NoDelay=true;client.SendTimeout=1000;lock(gate){if(stopping||work.Generation!=generation)continue;activeClient=client;}
   var connect=client.BeginConnect(address,work.Config.Port,null,null);try{if(!connect.AsyncWaitHandle.WaitOne(1000))throw new TimeoutException("Connessione TCP scaduta");client.EndConnect(connect);}finally{connect.AsyncWaitHandle.Close();}
   if(!work.Test){byte[] data=Encoding.UTF8.GetBytes(work.Command+"\n");using(var stream=client.GetStream()){stream.Write(data,0,data.Length);stream.Flush();client.Client.Shutdown(SocketShutdown.Send);}}
   lock(gate)if(work.Generation==generation&&!stopping)Status=work.Test?"Companion TCP: porta raggiungibile (nessun comando inviato)":"Companion TCP: stringa inviata "+DateTime.Now.ToString("HH:mm:ss");
  }}catch(Exception e){lock(gate)if(!stopping&&work.Generation==generation)Status="Companion TCP: ERRORE · "+e.Message;}
  finally{lock(gate){activeClient=null;}Interlocked.Decrement(ref pending);}
 }}
 public bool Flush(int timeoutMilliseconds){return SpinWait.SpinUntil(()=>Volatile.Read(ref pending)==0,timeoutMilliseconds);}
 public void Dispose(){lock(gate){if(stopping)return;stopping=true;generation++;if(activeClient!=null)activeClient.Close();queue.CompleteAdding();}worker.Join();queue.Dispose();}
}
public sealed class ProgramPresenceEventArgs:EventArgs {public readonly bool HasImage;public ProgramPresenceEventArgs(bool hasImage){HasImage=hasImage;}}
}

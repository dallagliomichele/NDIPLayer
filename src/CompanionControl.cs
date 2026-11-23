using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace ImageNdi {
// Four bounded clients; each connection preserves command order and uses LF/CRLF framing.
public sealed class CompanionControlServer:IDisposable {
 readonly TcpListener listener;readonly Thread acceptor;readonly Func<string,string> execute;readonly object gate=new object();readonly HashSet<TcpClient> clients=new HashSet<TcpClient>();bool stopped;
 public int Port {get{return ((IPEndPoint)listener.LocalEndpoint).Port;}}
 public CompanionControlServer(IPAddress address,int port,Func<string,string> handler){execute=handler;listener=new TcpListener(address,port);listener.Start(4);acceptor=new Thread(Accept){IsBackground=true,Name="Companion TCP input"};acceptor.Start();}
 void Accept(){try{while(true){var client=listener.AcceptTcpClient();lock(gate){if(stopped){client.Close();break;}if(clients.Count>=4){client.Close();continue;}clients.Add(client);}ThreadPool.QueueUserWorkItem(ReadClient,client);}}catch(SocketException){}catch(ObjectDisposedException){}}
 void ReadClient(object state){var client=(TcpClient)state;try{using(client){client.NoDelay=true;client.SendTimeout=2000;using(var stream=client.GetStream()){var line=new StringBuilder();bool invalid=false;while(true){int value=stream.ReadByte();if(value<0)break;if(value==10){string reply;if(invalid)reply="ERR Comando troppo lungo o non ASCII";else{try{reply=execute(line.ToString().TrimEnd('\r'));}catch(Exception){reply="ERR Comando non eseguito";}}byte[] result=Encoding.UTF8.GetBytes(reply.Replace("\r"," ").Replace("\n"," ")+"\n");stream.Write(result,0,result.Length);line.Clear();invalid=false;}else if(line.Length>=128||value>127||value==0)invalid=true;else if(!invalid)line.Append((char)value);}}}}catch(IOException){}catch(SocketException){}catch(ObjectDisposedException){}finally{lock(gate){clients.Remove(client);Monitor.PulseAll(gate);}}}
 public void Dispose(){lock(gate){if(stopped)return;stopped=true;listener.Stop();foreach(var client in clients)client.Close();}acceptor.Join();}
}
public sealed class CompanionCommand {
 public string Name,Value;
 public static bool TryParse(string line,out CompanionCommand command){command=null;string[] fields=(line??"").Trim().Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(fields.Length==0)return false;string name=fields[0].ToUpperInvariant();if(name=="SELECT"||name=="DIGIT"){if(fields.Length!=2)return false;string value=fields[1];if(value.Length<1||value.Length>9)return false;foreach(char c in value)if(c<'0'||c>'9')return false;if(name=="DIGIT"&&value.Length!=1)return false;command=new CompanionCommand{Name=name,Value=value};return true;}if(fields.Length!=1||Array.IndexOf(new[]{"NEXT","PREV","NEXT_BLOCK","PREV_BLOCK","GO","CLEAR_FADE","CLEAR_CUT","PLAY","PAUSE","RESTART","VIDEO_STATUS","ASSET_STATUS","BACKSPACE","CANCEL","ENTER"},name)<0)return false;command=new CompanionCommand{Name=name};return true;}
}
}

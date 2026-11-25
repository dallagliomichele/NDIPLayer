using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageNdi {
internal static class ThumbnailCache {
 internal static string ProjectDirectory(string appData,string project,string root){
  string identity=project??root??"";
  string name=project==null?"Progetto locale":Path.GetFileNameWithoutExtension(project);
  if(name.Length>48)name=name.Substring(0,48);
  if(!String.IsNullOrWhiteSpace(identity)){
   try{identity=Path.GetFullPath(identity).TrimEnd('\\');}
   catch(ArgumentException){}
   catch(NotSupportedException){}
   catch(PathTooLongException){}
  }else identity="";
  string key=Hash(identity.ToUpperInvariant());
  return Path.Combine(appData,"NDIPlayer",name+"-"+key.Substring(0,16),"Thumbnails");
 }

 static string Hash(string value){
  using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");
 }

 internal static string Key(string source,int edge){
  try{
   var info=new FileInfo(source);
   if(!info.Exists)return null;
   return Hash(String.Format(CultureInfo.InvariantCulture,"1\n{0}\n{1}\n{2}\n{3}\n{4}",
    info.FullName.ToUpperInvariant(),info.Length,info.LastWriteTimeUtc.Ticks,info.CreationTimeUtc.Ticks,edge));
  }catch(IOException){return null;}
  catch(UnauthorizedAccessException){return null;}
  catch(ArgumentException){return null;}
 }

 internal static Bitmap Load(string directory,string source,int edge,string key,bool prefetch){
  if(key==null)return null;
  string file=directory==null?null:Path.Combine(directory,key+".png");
  if(file!=null&&File.Exists(file)){
   if(prefetch)return null;
   try{
    using(var image=Image.FromFile(file))
     if(image.Width==edge&&image.Height==edge)return new Bitmap(image);
   }catch(IOException){}
   catch(UnauthorizedAccessException){}
   catch(ArgumentException){}
   catch(OutOfMemoryException){} // GDI+ also reports invalid image data this way.
   catch(System.Runtime.InteropServices.ExternalException){}
  }

  Bitmap thumbnail;
  try{
   using(var image=VideoTools.IsVideo(source)?VideoTools.Poster(source,true):Renderer.LoadBitmap(source))
    thumbnail=Renderer.RenderCpu(image,edge,edge,new Placement());
  }catch(Exception){return null;}
  if(file!=null&&key==Key(source,edge))Write(file,thumbnail);
  return thumbnail;
 }

 static void Write(string file,Bitmap thumbnail){
  string directory=Path.GetDirectoryName(file),temp=Path.Combine(directory,Guid.NewGuid().ToString("N")+".tmp");
  try{
   Directory.CreateDirectory(directory);
   thumbnail.Save(temp,ImageFormat.Png);
   if(File.Exists(file))File.Replace(temp,file,null);
   else File.Move(temp,file);
  }catch(IOException){}
  catch(UnauthorizedAccessException){}
  catch(System.Runtime.InteropServices.ExternalException){}
  finally{
   try{File.Delete(temp);}
   catch(IOException){}
   catch(UnauthorizedAccessException){}
  }
 }

 internal static void Clear(string directory){
  if(directory==null)return;
  try{
   foreach(string file in Directory.EnumerateFiles(directory)){
    string extension=Path.GetExtension(file);
    int length=extension==".png"?64:extension==".tmp"?32:0;
    string name=Path.GetFileNameWithoutExtension(file);
    if(length>0&&name.Length==length&&Array.TrueForAll(name.ToCharArray(),Uri.IsHexDigit))File.Delete(file);
   }
  }catch(DirectoryNotFoundException){}
 }
}

internal sealed class ThumbnailWorker : IDisposable {
 internal sealed class Result : IDisposable {
  internal string Source,Key;
  internal Bitmap Image;
  internal bool Unchanged;
  public void Dispose(){if(Image!=null)Image.Dispose();}
 }

 readonly object gate=new object();
 readonly Queue<Tuple<string,string>> visible=new Queue<Tuple<string,string>>();
 readonly Queue<Action> maintenance=new Queue<Action>();
 readonly Action notify;
 Thread thread;
 Result result;
 bool stopped,allowPrefetch;
 string directory;
 int edge,revision,next;
 string[] library=new string[0];
 DateTime prefetchAfter;

 internal ThumbnailWorker(Action notify){this.notify=notify;}

 internal void Configure(string directory,int edge,string[] library){
  lock(gate){
   this.directory=directory;
   this.edge=edge;
   this.library=library;
   next=0;
   ResetVisible();
   Wake();
  }
 }

 internal void ResetVisible(){
  lock(gate){
   revision++;
   visible.Clear();
   DiscardResult();
   prefetchAfter=DateTime.UtcNow.AddMilliseconds(200);
   Monitor.PulseAll(gate);
  }
 }

 internal void Request(string source,string knownKey){
  lock(gate){
   visible.Enqueue(Tuple.Create(source,knownKey));
   Wake();
  }
 }

 internal void SetPrefetchAllowed(bool allowed){
  lock(gate){
   if(allowPrefetch==allowed)return;
   allowPrefetch=allowed;
   Wake();
  }
 }

 internal Task Clear(){
  var completion=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
  lock(gate){
   string target=directory;
   // Serialize deletion with writes and leave the rest of the library cold until the next refresh.
   next=library.Length;
   ResetVisible();
   maintenance.Enqueue(delegate{
    try{ThumbnailCache.Clear(target);completion.SetResult(true);}
    catch(Exception error){completion.SetException(error);}
   });
   Wake();
  }
  return completion.Task;
 }

 void Wake(){
  if(stopped)return;
  if(thread==null&&(visible.Count>0||maintenance.Count>0||CanPrefetch)){
   thread=new Thread(Run){Name="NDIPlayer thumbnails",IsBackground=true,Priority=ThreadPriority.BelowNormal};
   thread.Start();
  }
  Monitor.PulseAll(gate);
 }

 bool CanPrefetch {get{return allowPrefetch&&directory!=null&&next<library.Length;}}

 void Run(){
  while(true){
   Action action=null;
   Tuple<string,string> request=null;
   string cacheDirectory=null,source=null;
   int version=0,pixels=0;
   lock(gate){
    while(true){
     if(maintenance.Count>0){action=maintenance.Dequeue();break;}
     if(stopped)return;
     if(result==null&&visible.Count>0){request=visible.Dequeue();source=request.Item1;break;}
     if(result==null&&CanPrefetch){
      double delay=(prefetchAfter-DateTime.UtcNow).TotalMilliseconds;
      if(delay<=0){source=library[next++];break;}
      Monitor.Wait(gate,(int)Math.Ceiling(delay));
     }else Monitor.Wait(gate);
    }
    cacheDirectory=directory;
    version=revision;
    pixels=edge;
   }
   if(action!=null){action();continue;}
   string key=ThumbnailCache.Key(source,pixels);
   bool prefetch=request==null;
   bool unchanged=!prefetch&&key!=null&&key==request.Item2;
   var image=unchanged?null:ThumbnailCache.Load(cacheDirectory,source,pixels,key,prefetch);
   if(prefetch){if(image!=null)image.Dispose();continue;}
   lock(gate){
    if(stopped||version!=revision){if(image!=null)image.Dispose();continue;}
    result=new Result{Source=source,Key=image==null&&!unchanged?null:key,Image=image,Unchanged=unchanged};
   }
   try{notify();}
   catch(InvalidOperationException){lock(gate)DiscardResult();}
  }
 }

 internal Result TakeResult(){
  lock(gate){
   var ready=result;
   result=null;
   Monitor.PulseAll(gate);
   return ready;
  }
 }

 void DiscardResult(){
  if(result==null)return;
  result.Dispose();
  result=null;
 }

 public void Dispose(){
  lock(gate){
   stopped=true;
   DiscardResult();
   Monitor.PulseAll(gate);
  }
 }
}
}

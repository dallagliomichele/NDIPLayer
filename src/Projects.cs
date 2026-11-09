using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace ImageNdi {
public class PlaylistBlock {
 public string Id {get;set;} public string Name {get;set;} public List<string> Files {get;set;} public Dictionary<string,string> Numbers {get;set;}
 public PlaylistBlock(){Id=Guid.NewGuid().ToString("N");Name="Nuovo blocco";Files=new List<string>();Numbers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);}
 public override string ToString(){return Name+" ("+Files.Count+")";}
}
public class ProjectDocument {public int Version {get;set;} public Settings Data {get;set;} public ProjectDocument(){Version=2;}}
public static class ProjectStore {
 public static void Normalize(Settings data){
  if(data.PreviewChannelName==null)data.PreviewChannelName=NdiChannel.PreviewName(data.ChannelName ?? NdiChannel.DefaultName);
  if(data.Companion==null)data.Companion=new CompanionSettings();
  if(data.ImageNumbers==null)data.ImageNumbers=new Dictionary<string,string>();
  data.ImageNumbers=data.ImageNumbers.Where(p=>!String.IsNullOrWhiteSpace(p.Key)&&ImageNumbers.Normalize(p.Value).Length>0).ToDictionary(p=>p.Key,p=>ImageNumbers.Normalize(p.Value),StringComparer.OrdinalIgnoreCase);
  if(data.Images==null)data.Images=new Dictionary<string,Placement>(StringComparer.OrdinalIgnoreCase);
  data.Images=new Dictionary<string,Placement>(data.Images,StringComparer.OrdinalIgnoreCase);
  if(data.Blocks==null)data.Blocks=new List<PlaylistBlock>();
  var ids=new HashSet<string>();
  foreach(var b in data.Blocks){if(b==null)throw new InvalidDataException("Blocco non valido.");if(String.IsNullOrWhiteSpace(b.Id)||!ids.Add(b.Id)){b.Id=Guid.NewGuid().ToString("N");ids.Add(b.Id);}if(String.IsNullOrWhiteSpace(b.Name))b.Name="Blocco";b.Files=(b.Files??new List<string>()).Where(p=>!String.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();if(b.Numbers==null)b.Numbers=new Dictionary<string,string>();b.Numbers=b.Numbers.Where(p=>b.Files.Contains(p.Key,StringComparer.OrdinalIgnoreCase)&&ImageNumbers.Normalize(p.Value).Length>0).ToDictionary(p=>p.Key,p=>ImageNumbers.Normalize(p.Value),StringComparer.OrdinalIgnoreCase);if(data.NumberingVersion==null)foreach(var path in b.Files){string old;if(!b.Numbers.ContainsKey(path)&&data.ImageNumbers.TryGetValue(path,out old))b.Numbers[path]=old;}}
  data.NumberingVersion=1;
  if(!data.Blocks.Exists(b=>b.Id==data.ActiveBlockId))data.ActiveBlockId=null;
 }
 public static PlaylistBlock AddBlock(Settings data,string name){name=(name??"").Trim();if(name.Length==0)throw new ArgumentException("Inserisci il nome del blocco.");var b=new PlaylistBlock{Name=name};data.Blocks.Add(b);return b;}
 public static void AddFiles(PlaylistBlock block,IEnumerable<string> files){foreach(var file in files)if(!block.Files.Contains(file,StringComparer.OrdinalIgnoreCase))block.Files.Add(file);}
 public static void MoveFiles(PlaylistBlock block,IEnumerable<string> moving,string before){var set=new HashSet<string>(moving,StringComparer.OrdinalIgnoreCase);var ordered=block.Files.Where(set.Contains).ToList();if(before!=null&&set.Contains(before))return;block.Files.RemoveAll(set.Contains);int index=before==null?block.Files.Count:block.Files.FindIndex(p=>String.Equals(p,before,StringComparison.OrdinalIgnoreCase));if(index<0)index=block.Files.Count;block.Files.InsertRange(index,ordered);}
 public static List<string> Visible(Settings data,IEnumerable<string> catalog,string blockId,string search){var block=data.Blocks.Find(b=>b.Id==blockId);IEnumerable<string> source=block==null?catalog:block.Files;var numbers=block==null?data.ImageNumbers:block.Numbers;string query=(search??"").Trim();return source.Where(p=>query.Length==0||p.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||(numbers.ContainsKey(p)&&numbers[p].IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToList();}
 static JavaScriptSerializer Serializer(){return new JavaScriptSerializer{MaxJsonLength=16*1024*1024};}
 public static Settings Clone(Settings data){return Serializer().Deserialize<Settings>(Serializer().Serialize(data));}
 public static string Fingerprint(Settings data){return Serializer().Serialize(data);}
 static string Relative(string path,string directory){if(String.IsNullOrWhiteSpace(path))return path;if(!String.Equals(Path.GetPathRoot(Path.GetFullPath(path)),Path.GetPathRoot(Path.GetFullPath(directory)),StringComparison.OrdinalIgnoreCase))return path;var root=new Uri(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar);var target=new Uri(Path.GetFullPath(path));if(root.Scheme!=target.Scheme||root.Host!=target.Host)return path;return Uri.UnescapeDataString(root.MakeRelativeUri(target).ToString()).Replace('/',Path.DirectorySeparatorChar);}
 static string Absolute(string path,string directory){return String.IsNullOrWhiteSpace(path)?path:Path.GetFullPath(Path.IsPathRooted(path)?path:Path.Combine(directory,path));}
 static void MapPaths(Settings data,Func<string,string> map){data.Root=map(data.Root);var placements=new Dictionary<string,Placement>(StringComparer.OrdinalIgnoreCase);foreach(var p in data.Images)placements[map(p.Key)]=p.Value;data.Images=placements;var numbers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var n in data.ImageNumbers)numbers[map(n.Key)]=n.Value;data.ImageNumbers=numbers;foreach(var b in data.Blocks){b.Files=b.Files.Select(map).ToList();var blockNumbers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var n in b.Numbers)blockNumbers[map(n.Key)]=n.Value;b.Numbers=blockNumbers;}}
 public static void Write(string path,Settings settings){var data=Clone(settings);Normalize(data);if(data.EscClearMs<1||data.EscClearMs>60000)throw new InvalidDataException("Durata clear Esc non valida.");data.Companion.Validate();NdiChannel.ValidatePair(data.ChannelName,data.PreviewChannelName);string folder=Path.GetDirectoryName(Path.GetFullPath(path));MapPaths(data,p=>Relative(p,folder));var document=new ProjectDocument{Data=data};string temp=path+".tmp";File.WriteAllText(temp,Serializer().Serialize(document));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
 public static Settings Read(string path){var document=Serializer().Deserialize<ProjectDocument>(File.ReadAllText(path));if(document==null||(document.Version!=1&&document.Version!=2)||document.Data==null)throw new InvalidDataException("Formato progetto non supportato.");var data=document.Data;Normalize(data);string folder=Path.GetDirectoryName(Path.GetFullPath(path));MapPaths(data,p=>Absolute(p,folder));if(data.OutputWidth<16||data.OutputWidth>3840||data.OutputWidth%2!=0||data.OutputHeight<16||data.OutputHeight>2160||data.OutputFps<1||data.OutputFps>60)throw new InvalidDataException("Formato video del progetto non valido.");if(data.EscClearMs<1||data.EscClearMs>60000)throw new InvalidDataException("Durata clear Esc non valida.");data.Companion.Validate();data.ChannelName=NdiChannel.Validate(data.ChannelName);data.PreviewChannelName=NdiChannel.Validate(data.PreviewChannelName);NdiChannel.ValidatePair(data.ChannelName,data.PreviewChannelName);foreach(var p in data.Images.Values)if(p==null||Double.IsNaN(p.Scale)||Double.IsInfinity(p.Scale)||p.Scale<.01||p.Scale>4||p.X < -32768||p.X>32768||p.Y < -32768||p.Y>32768)throw new InvalidDataException("Posizione immagine non valida.");return data;}
 public static void Relocate(Settings data,string folder){string previous=Path.GetFullPath(data.Root??folder).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;string next=Path.GetFullPath(folder);MapPaths(data,p=>!String.IsNullOrWhiteSpace(p)&&Path.GetFullPath(p).StartsWith(previous,StringComparison.OrdinalIgnoreCase)?Path.Combine(next,p.Substring(previous.Length)):p);data.Root=next;}
}
}

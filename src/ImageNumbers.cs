using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace ImageNdi {
public static class ImageNumbers {
 public static string Normalize(string value){value=(value??"").Trim();if(value.Length==0)return "";if(value.Length>9||value.Any(c=>c<'0'||c>'9'))throw new ArgumentException("Il numero deve contenere da 1 a 9 cifre. Lascia vuoto per rimuoverlo.");return value;}
 public static string Key(string value){value=Normalize(value);if(value.Length==0)return "";string key=value.TrimStart('0');return key.Length==0?"0":key;}
 public static Dictionary<string,string> Scope(Settings settings){var block=settings.Blocks.Find(b=>b.Id==settings.ActiveBlockId);return block==null?settings.ImageNumbers:block.Numbers;}
 public static void Set(Settings settings,string path,string value){value=Normalize(value);var block=settings.Blocks.Find(b=>b.Id==settings.ActiveBlockId);if(block!=null&&!block.Files.Contains(path,StringComparer.OrdinalIgnoreCase))throw new ArgumentException("La foto non appartiene al blocco corrente.");var numbers=Scope(settings);if(value.Length==0){numbers.Remove(path);return;}string key=Key(value);if(numbers.Any(p=>!String.Equals(p.Key,path,StringComparison.OrdinalIgnoreCase)&&Key(p.Value)==key))throw new ArgumentException("Numero già assegnato a un'altra foto in questo blocco. Scegli un numero diverso.");numbers[path]=value;}
 public static List<string> Find(Settings settings,IEnumerable<string> paths,string number){string key=Key(number);if(key.Length==0)return new List<string>();var numbers=Scope(settings);return paths.Distinct(StringComparer.OrdinalIgnoreCase).Where(p=>numbers.ContainsKey(p)&&Key(numbers[p])==key).ToList();}
 public static string Label(Settings settings,string path){string value;return Scope(settings).TryGetValue(path,out value)?"#"+value+" · "+Path.GetFileName(path):Path.GetFileName(path);}
}
}

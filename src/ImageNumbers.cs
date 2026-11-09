using System;
using System.IO;
using System.Linq;
using System.Globalization;

namespace ImageNdi {
public static class ImageNumbers {
 public static string Normalize(string value){value=(value??"").Trim();if(value.Length==0)return "";if(value.Length>9||value.Any(c=>c<'0'||c>'9'))throw new ArgumentException("Il numero deve contenere da 1 a 9 cifre. Lascia vuoto per rimuoverlo.");return value;}
 public static void Set(Settings settings,string path,string value){value=Normalize(value);if(value.Length==0)settings.ImageNumbers.Remove(path);else settings.ImageNumbers[path]=value;}
 public static string Label(Settings settings,string path){string value;return settings.ImageNumbers.TryGetValue(path,out value)?"#"+value+" · "+Path.GetFileName(path):Path.GetFileName(path);}
}
}

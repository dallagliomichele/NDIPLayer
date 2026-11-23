using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Web.Script.Serialization;
namespace ImageNdi {
public static class WindowPlacement {
 public static string MonitorLayout(){return String.Join("|",Screen.AllScreens.OrderBy(s=>s.DeviceName,StringComparer.Ordinal).Select(s=>s.DeviceName+":"+s.Bounds.X+","+s.Bounds.Y+","+s.Bounds.Width+","+s.Bounds.Height+":"+s.WorkingArea.X+","+s.WorkingArea.Y+","+s.WorkingArea.Width+","+s.WorkingArea.Height+":"+s.Primary).ToArray());}
 public static bool CanRestore(WindowPreferences saved,string layout){return saved!=null&&saved.MonitorLayout==layout&&saved.Bounds.Width>=200&&saved.Bounds.Height>=150;}
 public static Rectangle VisibleBounds(Rectangle bounds,Rectangle area){int w=Math.Min(bounds.Width,area.Width),h=Math.Min(bounds.Height,area.Height);return new Rectangle(Math.Max(area.Left,Math.Min(bounds.X,area.Right-w)),Math.Max(area.Top,Math.Min(bounds.Y,area.Bottom-h)),w,h);}
}
public partial class MainWindow {
 SplitContainer librarySplit,monitorSplit;WindowPreferences windowPreferences;FormWindowState lastWindowState=FormWindowState.Normal;
 void RestoreWindowPreferences(){if(!WindowPlacement.CanRestore(windowPreferences,WindowPlacement.MonitorLayout()))return;var bounds=windowPreferences.Bounds;var area=Screen.FromRectangle(bounds).WorkingArea;StartPosition=FormStartPosition.Manual;Bounds=WindowPlacement.VisibleBounds(bounds,area);if(windowPreferences.Maximized)WindowState=FormWindowState.Maximized;PerformLayout();if(windowPreferences.LibrarySplitterDistance>0){int max=Math.Max(librarySplit.Panel1MinSize,librarySplit.ClientSize.Width-librarySplit.SplitterWidth-librarySplit.Panel2MinSize);librarySplit.SplitterDistance=Math.Max(librarySplit.Panel1MinSize,Math.Min(windowPreferences.LibrarySplitterDistance,max));}if(windowPreferences.MonitorSplitterDistance>0){int max=Math.Max(monitorSplit.Panel1MinSize,monitorSplit.ClientSize.Height-monitorSplit.SplitterWidth-monitorSplit.Panel2MinSize);monitorSplit.SplitterDistance=Math.Max(monitorSplit.Panel1MinSize,Math.Min(windowPreferences.MonitorSplitterDistance,max));}}
 void SaveWindowPreferences(){try{var value=new WindowPreferences{AlwaysOnTop=TopMost,DarkMode=darkMode!=null?darkMode.Checked:windowPreferences!=null&&windowPreferences.DarkMode,LastProjectPath=projectPath};if(Visible){value.LibrarySplitterDistance=librarySplit.SplitterDistance;value.MonitorSplitterDistance=monitorSplit.SplitterDistance;value.MonitorLayout=WindowPlacement.MonitorLayout();value.Bounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;value.Maximized=WindowState==FormWindowState.Maximized||(WindowState==FormWindowState.Minimized&&lastWindowState==FormWindowState.Maximized);}else if(windowPreferences!=null){value.LibrarySplitterDistance=windowPreferences.LibrarySplitterDistance;value.MonitorSplitterDistance=windowPreferences.MonitorSplitterDistance;value.MonitorLayout=windowPreferences.MonitorLayout;value.Bounds=windowPreferences.Bounds;value.Maximized=windowPreferences.Maximized;}Directory.CreateDirectory(Path.GetDirectoryName(state));string path=state+".window.json",temp=path+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(value));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);windowPreferences=value;}catch(Exception e){status.Text="Preferenza finestra non salvata: "+e.Message;}}
}
}

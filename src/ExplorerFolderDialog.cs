using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageNdi {
internal static class ExplorerFolderDialog {
 const uint PickFolders = 0x20, ForceFileSystem = 0x40, FileSystemPath = 0x80058000;
 const int Cancelled = unchecked((int)0x800704C7);

 internal static string Pick(IntPtr owner, string title, string initialFolder) {
  var dialog = (IFileDialog)new FileOpenDialog();
  try {
   uint options;
   dialog.GetOptions(out options);
   dialog.SetOptions(options | PickFolders | ForceFileSystem);
   dialog.SetTitle(title);
   if (Directory.Exists(initialFolder)) {
    var itemId = typeof(IShellItem).GUID;
    IShellItem folder;
    if (SHCreateItemFromParsingName(initialFolder, IntPtr.Zero, ref itemId, out folder) >= 0) {
     try { dialog.SetFolder(folder); }
     finally { Marshal.ReleaseComObject(folder); }
    }
   }
   int result = dialog.Show(owner);
   if (result == Cancelled) return null;
   Marshal.ThrowExceptionForHR(result);
   IShellItem selection;
   dialog.GetResult(out selection);
   try {
    string path;
    selection.GetDisplayName(FileSystemPath, out path);
    return path;
   } finally { Marshal.ReleaseComObject(selection); }
  } finally { Marshal.ReleaseComObject(dialog); }
 }

 [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
 static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid interfaceId,
  [MarshalAs(UnmanagedType.Interface)] out IShellItem item);

 [ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
 class FileOpenDialog {}

 // Methods appear in native vtable order through GetResult.
 [ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IFileDialog {
  [PreserveSig] int Show(IntPtr owner);
  void SetFileTypes(uint count, IntPtr filters);
  void SetFileTypeIndex(uint index);
  void GetFileTypeIndex(out uint index);
  void Advise(IntPtr events, out uint cookie);
  void Unadvise(uint cookie);
  void SetOptions(uint options);
  void GetOptions(out uint options);
  void SetDefaultFolder(IShellItem folder);
  void SetFolder(IShellItem folder);
  void GetFolder(out IShellItem folder);
  void GetCurrentSelection(out IShellItem item);
  void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
  void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
  void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
  void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
  void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
  void GetResult(out IShellItem item);
 }

 [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IShellItem {
  void BindToHandler(IntPtr bindContext, ref Guid handlerId, ref Guid interfaceId, out IntPtr result);
  void GetParent(out IShellItem parent);
  void GetDisplayName(uint displayName, [MarshalAs(UnmanagedType.LPWStr)] out string name);
 }
}
}

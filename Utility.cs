using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImgComparer
{
    public static class Utility
    {
        private static Random rnd = new Random();
        private static List<string> toDelete = new List<string>();

        [DllImport("shell32.dll")]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll")]
        private static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);

        public static int Rand => rnd.Next();

        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        public static void MarkToDelete(string path)
        {
            toDelete.Add(path);
        }

        public static bool DeleteFiles(IWin32Window owner)
        {
            while (toDelete.Count > 0)
            {
                string path = toDelete[0];
                try
                {
                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    toDelete.RemoveAt(0);
                }
                catch (Exception ex)
                {
                    DialogResult result = MessageBox.Show(owner, $"Failed to delete file {path}.\n{ex.Message}", "Retry?", MessageBoxButtons.RetryCancel);
                    if (result == DialogResult.Cancel)
                        return false;
                }
            }

            return true;
        }

        public static void OpenInExplorer(string path)
        {
            if (!File.Exists(path))
            {
                if (Directory.Exists(path))
                {
                    if (!path.EndsWith("\\"))
                        path += "\\";
                    Process.Start(path);
                }
                return;
            }

            string dir = Path.GetDirectoryName(path);
            SHParseDisplayName(dir, IntPtr.Zero, out IntPtr folder, 0, out uint _);
            if (folder == IntPtr.Zero)
                return;

            SHParseDisplayName(path, IntPtr.Zero, out IntPtr file, 0, out uint _);

            if (file != IntPtr.Zero)
            {
                IntPtr[] files = { file };
                SHOpenFolderAndSelectItems(folder, (uint)files.Length, files, 0);
                Marshal.FreeCoTaskMem(file);
            }

            Marshal.FreeCoTaskMem(folder);
        }
    }
}

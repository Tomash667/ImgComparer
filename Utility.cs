using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImgComparer
{
    public static class Utility
    {
        private static Random rnd = new Random();

        [DllImport("shell32.dll")]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll")]
        private static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);

        public static int Rand => rnd.Next();

        public static int NextPow2(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            return value;
        }

        // 2->1
        // 4->2
        // 8->3
        // 16->4
        // etc
        public static int Pow2Exponent(int value)
        {
            int exp = 0;
            while (value > 1)
            {
                ++exp;
                value /= 2;
            }
            return exp;
        }

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

        public static void SafeDelete(IWin32Window owner, string path)
        {
            while (File.Exists(path))
            {
                try
                {
                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    return;
                }
                catch (Exception ex)
                {
                    DialogResult result = MessageBox.Show(owner, $"Failed to delete file {path}.\n{ex.Message}", "Retry?", MessageBoxButtons.RetryCancel);
                    if (result == DialogResult.Cancel)
                        return;
                }
            }
        }

        public static void OpenInExplorer(string path)
        {
            if (!File.Exists(path))
                return;

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

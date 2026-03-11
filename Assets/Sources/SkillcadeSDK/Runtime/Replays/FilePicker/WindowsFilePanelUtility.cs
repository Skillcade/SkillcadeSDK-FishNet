
using System;
using System.Runtime.InteropServices;

namespace SkillcadeSDK.Replays
{
    public static class WindowsFilePanelUtility
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);

        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_NOCHANGEDIR = 0x00000008;
        private const int MAX_PATH = 260;

        public static string OpenFilePanel(string extension, string filterDescription)
        {
            var ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.lpstrFilter = $"{filterDescription} (*.{extension})\0*.{extension}\0All Files (*.*)\0*.*\0";
            ofn.lpstrFile = new string('\0', MAX_PATH);
            ofn.nMaxFile = MAX_PATH;
            ofn.lpstrTitle = "Choose replay file";
            ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
            ofn.lpstrDefExt = extension;

            return GetOpenFileName(ref ofn) ? ofn.lpstrFile : null;
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BeiShuiCS2
{
    public static class ShortcutHelper
    {
        [ComImport, Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")]
        private class IWshLocator { }

        [ComImport, Guid("F935DC21-1CF0-11D0-ADB9-00C04FD58A0B"),
         InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IWshShortcut
        {
            string TargetPath { get; set; }
            string Arguments { get; set; }
            string WorkingDirectory { get; set; }
            string Description { get; set; }
            string IconLocation { get; set; }
            void Save();
        }

        public static void CreateShortcut(string shortcutPath, string targetPath,
            string workingDir, string description, string iconLocation)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) throw new Exception("WScript.Shell 不可用");
                dynamic shell = Activator.CreateInstance(shellType)!;
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Description = description;
                shortcut.IconLocation = iconLocation;
                shortcut.Save();
                Marshal.ReleaseComObject(shell);
            }
            catch (Exception ex)
            {
                throw new Exception($"创建快捷方式失败: {ex.Message}", ex);
            }
        }

        public static void EnsureDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktopPath, "背水平台.lnk");

                if (File.Exists(shortcutPath))
                    return;

                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    return;

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return;

                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return;

                dynamic? shortcut = shell.CreateShortcut(shortcutPath);
                if (shortcut == null) return;

                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description = "背水对战平台";
                shortcut.Save();

                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建桌面快捷方式失败: {ex.Message}");
            }
        }
    }
}
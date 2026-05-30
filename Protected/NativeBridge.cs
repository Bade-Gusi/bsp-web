// ===================================================================
// NativeBridge.cs — C# P/Invoke 桥接层
// WPF 通过此类调用 BSPCore.dll 的所有核心功能
// 此文件只做调用转发，不含任何核心业务逻辑
// ===================================================================

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BeiShuiCS2.Protected
{
    public enum BspViolationType
    {
        None = 0,
        Debugger = 1,
        BlacklistedProcess = 2,
        BlacklistedWindow = 3,
        Injection = 4,
        Macro = 5,
        PacketSniffer = 6
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BspCheckResult
    {
        [MarshalAs(UnmanagedType.I1)] public bool Ok;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)] public string Reason;
        public BspViolationType Type;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BspHeartbeatInfo
    {
        [MarshalAs(UnmanagedType.I1)] public bool IsActive;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string ServerIP;
        public int Port;
        public long LastHeartbeatTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string SteamID;
        public int PacketCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct BspAntiCheatToken
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string SteamID;
        [MarshalAs(UnmanagedType.I1)] public bool Status;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Version;
        public long Timestamp;
        public int Seq;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Signature;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public delegate void BspLaunchCallback(int state, string message);

    public static class NativeBridge
    {
        private const string DLL_NAME = "BSPCore.dll";
        private static IntPtr _dllHandle = IntPtr.Zero;
        private static readonly object _lock = new object();

        public static void EnsureLoaded()
        {
            if (_dllHandle != IntPtr.Zero) return;
            lock (_lock)
            {
                if (_dllHandle != IntPtr.Zero) return;
                string dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DLL_NAME);
                _dllHandle = LoadLibrary(dllPath);
                if (_dllHandle == IntPtr.Zero)
                    throw new DllNotFoundException($"无法加载 {DLL_NAME}，请确保文件位于程序目录下。");
            }
        }

        public static void Unload()
        {
            lock (_lock)
            {
                if (_dllHandle != IntPtr.Zero)
                {
                    FreeLibrary(_dllHandle);
                    _dllHandle = IntPtr.Zero;
                }
            }
        }

        // ---- 加载 DLL ----
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // ---- 反作弊 ----
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern BspCheckResult BspAntiCheat_PerformFullCheck();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr BspAntiCheat_GetVersion();

        public static BspCheckResult PerformAntiCheatCheck()
        {
            EnsureLoaded();
            return BspAntiCheat_PerformFullCheck();
        }

        public static string GetAntiCheatVersion()
        {
            EnsureLoaded();
            IntPtr ptr = BspAntiCheat_GetVersion();
            return Marshal.PtrToStringAnsi(ptr) ?? "";
        }

        // ---- Token ----
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern BspAntiCheatToken BspToken_Create(string steamId, [MarshalAs(UnmanagedType.I1)] bool status, string version);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BspToken_ResetSequence();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void BspToken_ComputeLegacySignature(string steamId, [MarshalAs(UnmanagedType.I1)] bool status, string version, long timestamp, [MarshalAs(UnmanagedType.LPStr)] StringBuilder outSig, int outSize);

        public static BspAntiCheatToken CreateToken(string steamId, bool status, string version)
        {
            EnsureLoaded();
            return BspToken_Create(steamId, status, version);
        }

        public static void ResetTokenSequence() { EnsureLoaded(); BspToken_ResetSequence(); }

        public static string ComputeLegacySignature(string steamId, bool status, string version, long timestamp)
        {
            EnsureLoaded();
            var sb = new StringBuilder(128);
            BspToken_ComputeLegacySignature(steamId, status, version, timestamp, sb, 128);
            return sb.ToString();
        }

        // ---- 加密 ----
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspCrypto_InitCipher(byte[] seed, int seedLen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspCrypto_BeiShuiEncrypt(byte[] plain, int plainLen, byte[] outCipher, ref int outLen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspCrypto_BeiShuiDecrypt(byte[] cipher, int cipherLen, byte[] outPlain, ref int outLen);

        public static bool InitCipher(byte[] seed)
        {
            EnsureLoaded();
            return BspCrypto_InitCipher(seed, seed.Length);
        }

        public static byte[]? BeiShuiEncrypt(byte[] plain)
        {
            int outLen = plain.Length + 16;
            var outBuf = new byte[outLen];
            if (BspCrypto_BeiShuiEncrypt(plain, plain.Length, outBuf, ref outLen))
            {
                Array.Resize(ref outBuf, outLen);
                return outBuf;
            }
            return null;
        }

        public static byte[]? BeiShuiDecrypt(byte[] cipher)
        {
            int outLen = cipher.Length;
            var outBuf = new byte[outLen];
            if (BspCrypto_BeiShuiDecrypt(cipher, cipher.Length, outBuf, ref outLen))
            {
                Array.Resize(ref outBuf, outLen);
                return outBuf;
            }
            return null;
        }

        // ---- 密钥管理 ----
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspKeyManager_Init(string serverIP, int heartbeatPort, string steamId);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspKeyManager_IsKeyValid();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int BspKeyManager_GetNextSequence();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BspKeyManager_BuildEncryptedPacket(string steamId, [MarshalAs(UnmanagedType.I1)] bool status, string version, byte[] outPacket, ref int outLen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void BspKeyManager_BuildLegacyPacket(string steamId, [MarshalAs(UnmanagedType.I1)] bool status, string version, [MarshalAs(UnmanagedType.LPStr)] StringBuilder outJson, int outSize);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BspKeyManager_Shutdown();

        public static bool InitKeyManager(string serverIP, int heartbeatPort, string steamId)
        {
            EnsureLoaded();
            return BspKeyManager_Init(serverIP, heartbeatPort, steamId);
        }

        public static bool IsKeyValid() { EnsureLoaded(); return BspKeyManager_IsKeyValid(); }
        public static int GetNextSequence() { EnsureLoaded(); return BspKeyManager_GetNextSequence(); }
        public static void ShutdownKeyManager() { BspKeyManager_Shutdown(); }

        public static byte[]? BuildEncryptedPacket(string steamId, bool status, string version)
        {
            int outLen = 2300;
            var buf = new byte[outLen];
            EnsureLoaded();
            BspKeyManager_BuildEncryptedPacket(steamId, status, version, buf, ref outLen);
            if (outLen <= 0) return null;
            Array.Resize(ref buf, outLen);
            return buf;
        }

        public static string BuildLegacyPacket(string steamId, bool status, string version)
        {
            var sb = new StringBuilder(2048);
            EnsureLoaded();
            BspKeyManager_BuildLegacyPacket(steamId, status, version, sb, 2048);
            return sb.ToString();
        }

        // ---- 游戏启动器 ----
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspLauncher_LaunchViaSteam(string command);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspLauncher_LaunchViaSteamConnect(string serverAddress);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspLauncher_LaunchWithAntiCheat(string serverAddress, string cs2Path, BspLaunchCallback callback);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BspLauncher_StopHeartbeat();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspLauncher_IsHeartbeatActive();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern BspHeartbeatInfo BspLauncher_GetHeartbeatInfo();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BspLauncher_KillCS2();

        public static bool LaunchViaSteam(string command)
        {
            EnsureLoaded();
            return BspLauncher_LaunchViaSteam(command);
        }

        public static bool LaunchViaSteamConnect(string serverAddress)
        {
            EnsureLoaded();
            return BspLauncher_LaunchViaSteamConnect(serverAddress);
        }

        public static bool LaunchWithAntiCheat(string serverAddress, string cs2Path, BspLaunchCallback callback)
        {
            EnsureLoaded();
            return BspLauncher_LaunchWithAntiCheat(serverAddress, cs2Path, callback);
        }

        public static void StopHeartbeat() { EnsureLoaded(); BspLauncher_StopHeartbeat(); }
        public static bool IsHeartbeatActive() { EnsureLoaded(); return BspLauncher_IsHeartbeatActive(); }
        public static BspHeartbeatInfo GetHeartbeatInfo() { EnsureLoaded(); return BspLauncher_GetHeartbeatInfo(); }
        public static void KillCS2() { EnsureLoaded(); BspLauncher_KillCS2(); }

        // ---- 进程守护 ----
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspGuardian_Start(int parentPid, string serverAddress);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BspGuardian_Stop();

        public static bool StartGuardian(int parentPid, string serverAddress)
        {
            EnsureLoaded();
            return BspGuardian_Start(parentPid, serverAddress);
        }

        public static void StopGuardian() { EnsureLoaded(); BspGuardian_Stop(); }

        // ---- AES/RSA ----
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspCrypto_AesEncrypt(byte[] plain, int plainLen, byte[] key, byte[] outCipher, ref int outLen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspCrypto_AesDecrypt(byte[] cipher, int cipherLen, byte[] key, byte[] outPlain, ref int outLen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspCrypto_RsaGenerateKeyPair(StringBuilder outPub, int pubSize, StringBuilder outPriv, int privSize);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int BspCrypto_RsaDecrypt(byte[] cipher, int cipherLen, string privateKey, byte[] outPlain);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool BspCrypto_IsCipherReady();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BspAntiCheat_ResetState();

        public static byte[]? AesEncrypt(byte[] plain, byte[] key)
        {
            EnsureLoaded();
            int outLen = plain.Length + 32;
            var outBuf = new byte[outLen];
            if (BspCrypto_AesEncrypt(plain, plain.Length, key, outBuf, ref outLen))
            {
                Array.Resize(ref outBuf, outLen);
                return outBuf;
            }
            return null;
        }

        public static byte[]? AesDecrypt(byte[] cipher, byte[] key)
        {
            EnsureLoaded();
            int outLen = cipher.Length;
            var outBuf = new byte[outLen];
            if (BspCrypto_AesDecrypt(cipher, cipher.Length, key, outBuf, ref outLen))
            {
                Array.Resize(ref outBuf, outLen);
                return outBuf;
            }
            return null;
        }

        public static (string publicKey, string privateKey)? GenerateRsaKeyPair()
        {
            EnsureLoaded();
            var pubSb = new StringBuilder(256);
            var privSb = new StringBuilder(512);
            if (BspCrypto_RsaGenerateKeyPair(pubSb, 256, privSb, 512))
            {
                return (pubSb.ToString(), privSb.ToString());
            }
            return null;
        }

        public static byte[]? RsaDecrypt(byte[] cipher, string privateKey)
        {
            EnsureLoaded();
            var outBuf = new byte[256];
            int len = BspCrypto_RsaDecrypt(cipher, cipher.Length, privateKey, outBuf);
            if (len > 0)
            {
                Array.Resize(ref outBuf, len);
                return outBuf;
            }
            return null;
        }

        public static bool IsCipherReady()
        {
            EnsureLoaded();
            return BspCrypto_IsCipherReady();
        }

        public static void ResetAntiCheatState()
        {
            EnsureLoaded();
            BspAntiCheat_ResetState();
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Editor.BuildTools
{
    public static class BuildSecretsStore
    {
        private const string SecretsFileRelativePath = "UserSettings/BuildAutomationSecrets.dat";
        private const string EntropyValue = "zoosort_build_automation_v1";

        public static bool HasStoredSecrets()
        {
            string filePath = GetSecretsFilePath();
            return File.Exists(filePath);
        }

        public static void SaveReleaseSigningPasswords(string keystorePassword, string keyAliasPassword)
        {
            if (string.IsNullOrWhiteSpace(keystorePassword))
            {
                throw new ArgumentException("Keystore password is empty.", nameof(keystorePassword));
            }

            if (string.IsNullOrWhiteSpace(keyAliasPassword))
            {
                throw new ArgumentException("Key alias password is empty.", nameof(keyAliasPassword));
            }

            BuildAutomationSecretsData data = new BuildAutomationSecretsData
            {
                keystorePassword = keystorePassword,
                keyAliasPassword = keyAliasPassword,
                createdUtc = DateTime.UtcNow.ToString("O")
            };

            string json = JsonUtility.ToJson(data);
            byte[] encryptedBytes = Protect(Encoding.UTF8.GetBytes(json));

            string filePath = GetSecretsFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            File.WriteAllBytes(filePath, encryptedBytes);
        }

        public static bool TryLoadReleaseSigningPasswords(out string keystorePassword, out string keyAliasPassword)
        {
            keystorePassword = string.Empty;
            keyAliasPassword = string.Empty;

            string filePath = GetSecretsFilePath();
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(filePath);
                byte[] jsonBytes = Unprotect(encryptedBytes);
                string json = Encoding.UTF8.GetString(jsonBytes);

                BuildAutomationSecretsData data = JsonUtility.FromJson<BuildAutomationSecretsData>(json);
                if (data == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(data.keystorePassword) || string.IsNullOrWhiteSpace(data.keyAliasPassword))
                {
                    return false;
                }

                keystorePassword = data.keystorePassword;
                keyAliasPassword = data.keyAliasPassword;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ClearStoredSecrets()
        {
            string filePath = GetSecretsFilePath();
            if (!File.Exists(filePath))
            {
                return;
            }

            File.Delete(filePath);
        }

        private static string GetSecretsFilePath()
        {
            // Application.dataPath = <project>/Assets
            string projectRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRootPath, SecretsFileRelativePath);
        }

        private static byte[] Protect(byte[] data)
        {
#if UNITY_EDITOR_WIN
            byte[] entropyBytes = Encoding.UTF8.GetBytes(EntropyValue);
            return WindowsDpapi.ProtectCurrentUser(data, entropyBytes);
#else
            throw new PlatformNotSupportedException("Local encrypted secrets are supported only on Windows Editor currently.");
#endif
        }

        private static byte[] Unprotect(byte[] protectedData)
        {
#if UNITY_EDITOR_WIN
            byte[] entropyBytes = Encoding.UTF8.GetBytes(EntropyValue);
            return WindowsDpapi.UnprotectCurrentUser(protectedData, entropyBytes);
#else
            throw new PlatformNotSupportedException("Local encrypted secrets are supported only on Windows Editor currently.");
#endif
        }

        [Serializable]
        private sealed class BuildAutomationSecretsData
        {
            public string keystorePassword;
            public string keyAliasPassword;
            public string createdUtc;
        }

#if UNITY_EDITOR_WIN
        private static class WindowsDpapi
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct DataBlob
            {
                public int cbData;
                public IntPtr pbData;
            }

            [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool CryptProtectData(
                ref DataBlob pDataIn,
                string szDataDescr,
                ref DataBlob pOptionalEntropy,
                IntPtr pvReserved,
                IntPtr pPromptStruct,
                int dwFlags,
                ref DataBlob pDataOut);

            [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool CryptUnprotectData(
                ref DataBlob pDataIn,
                StringBuilder pszDataDescr,
                ref DataBlob pOptionalEntropy,
                IntPtr pvReserved,
                IntPtr pPromptStruct,
                int dwFlags,
                ref DataBlob pDataOut);

            [DllImport("Kernel32.dll", SetLastError = true)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            private const int CryptProtectUiForbidden = 0x1;

            public static byte[] ProtectCurrentUser(byte[] data, byte[] optionalEntropy)
            {
                return ProtectInternal(data, optionalEntropy);
            }

            public static byte[] UnprotectCurrentUser(byte[] protectedData, byte[] optionalEntropy)
            {
                return UnprotectInternal(protectedData, optionalEntropy);
            }

            private static byte[] ProtectInternal(byte[] data, byte[] optionalEntropy)
            {
                DataBlob dataIn = ToBlob(data);
                DataBlob entropyBlob = ToBlob(optionalEntropy);
                DataBlob dataOut = new DataBlob();

                try
                {
                    bool result = CryptProtectData(
                        ref dataIn,
                        "BuildAutomationSecrets",
                        ref entropyBlob,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        CryptProtectUiForbidden,
                        ref dataOut);

                    if (!result)
                    {
                        throw new InvalidOperationException($"CryptProtectData failed. Win32Error={Marshal.GetLastWin32Error()}");
                    }

                    return FromBlob(dataOut);
                }
                finally
                {
                    FreeBlob(ref dataIn);
                    FreeBlob(ref entropyBlob);
                    FreeBlobOut(ref dataOut);
                }
            }

            private static byte[] UnprotectInternal(byte[] protectedData, byte[] optionalEntropy)
            {
                DataBlob dataIn = ToBlob(protectedData);
                DataBlob entropyBlob = ToBlob(optionalEntropy);
                DataBlob dataOut = new DataBlob();

                try
                {
                    StringBuilder description = new StringBuilder();
                    bool result = CryptUnprotectData(
                        ref dataIn,
                        description,
                        ref entropyBlob,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        CryptProtectUiForbidden,
                        ref dataOut);

                    if (!result)
                    {
                        throw new InvalidOperationException($"CryptUnprotectData failed. Win32Error={Marshal.GetLastWin32Error()}");
                    }

                    return FromBlob(dataOut);
                }
                finally
                {
                    FreeBlob(ref dataIn);
                    FreeBlob(ref entropyBlob);
                    FreeBlobOut(ref dataOut);
                }
            }

            private static DataBlob ToBlob(byte[] data)
            {
                if (data == null || data.Length == 0)
                {
                    return new DataBlob { cbData = 0, pbData = IntPtr.Zero };
                }

                IntPtr pointer = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, pointer, data.Length);
                return new DataBlob { cbData = data.Length, pbData = pointer };
            }

            private static byte[] FromBlob(DataBlob blob)
            {
                if (blob.cbData <= 0 || blob.pbData == IntPtr.Zero)
                {
                    return Array.Empty<byte>();
                }

                byte[] data = new byte[blob.cbData];
                Marshal.Copy(blob.pbData, data, 0, blob.cbData);
                return data;
            }

            private static void FreeBlob(ref DataBlob blob)
            {
                if (blob.pbData == IntPtr.Zero)
                {
                    return;
                }

                Marshal.FreeHGlobal(blob.pbData);
                blob.pbData = IntPtr.Zero;
                blob.cbData = 0;
            }

            private static void FreeBlobOut(ref DataBlob blob)
            {
                if (blob.pbData == IntPtr.Zero)
                {
                    return;
                }

                LocalFree(blob.pbData);
                blob.pbData = IntPtr.Zero;
                blob.cbData = 0;
            }
        }
#endif
    }
}



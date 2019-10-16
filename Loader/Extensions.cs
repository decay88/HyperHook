﻿using antinet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Loader {
    internal class Extensions {

        #region Authentication

        internal class Authentication {

            public static string Login(string Username, string Password, string HWID) {
                using (System.Net.WebClient WebClient = new System.Net.WebClient()) {
                    WebClient.Proxy = null;

                    WebClient.Headers.Add("user-agent", Settings.Useragent_string);

                    var data = new System.Collections.Specialized.NameValueCollection {
                        ["Username"] = Username,
                        ["Password"] = Password,
                        ["HWID"] = HWID
                    };

                    byte[] response = WebClient.UploadValues(Settings.Website + "Loader/php/Login.php", data);

                    return Encoding.UTF8.GetString(response);
                }
            }

            public static string Register(string Username, string Password, string HWID) {
                using (System.Net.WebClient WebClient = new System.Net.WebClient()) {
                    WebClient.Proxy = null;

                    WebClient.Headers.Add("user-agent", Settings.Useragent_string);

                    var data = new System.Collections.Specialized.NameValueCollection {
                        ["Username"] = Username,
                        ["Password"] = Password,
                        ["HWID"] = HWID
                    };

                    byte[] Response = WebClient.UploadValues(Settings.Website + "Loader/php/Register.php", data);

                    return Encoding.UTF8.GetString(Response);
                }
            }

            public static string Activate(string Username, string License, string HWID) {
                using (System.Net.WebClient WebClient = new System.Net.WebClient()) {
                    WebClient.Proxy = null;

                    WebClient.Headers.Add("user-agent", Settings.Useragent_string);

                    var data = new System.Collections.Specialized.NameValueCollection {
                        ["Username"] = Username,
                        ["License"] = License,
                        ["HWID"] = HWID
                    };

                    byte[] Response = WebClient.UploadValues(Settings.Website + "Loader/php/Activate.php", data);

                    return Encoding.UTF8.GetString(Response);
                }
            }

            public static string Alert(string Message) {
                using (System.Net.WebClient WebClient = new System.Net.WebClient()) {
                    WebClient.Proxy = null;

                    WebClient.Headers.Add("user-agent", Settings.Useragent_string);

                    var data = new System.Collections.Specialized.NameValueCollection {
                        ["Message"] = Message
                    };

                    byte[] Response = WebClient.UploadValues(Settings.Website + "Loader/php/Alert.php", data);

                    return Encoding.UTF8.GetString(Response);
                }
            }
        }

        #endregion Authentication

        #region HWID

        internal class HWID {
            static string hardware_ID = string.Empty;

            public static string UserID() {
                if (string.IsNullOrEmpty(hardware_ID)) {
                    hardware_ID = GetHash("CPU >> " + CpuID() + "\nBIOS >> " + BiosID() + "\nBASE >> " + BaseID());
                }

                return hardware_ID;
            }

            static string GetHash(string s) {
                MD5 sec = new MD5CryptoServiceProvider();
                byte[] bt = Encoding.ASCII.GetBytes(s);

                return GetHexString(sec.ComputeHash(bt));
            }

            static string GetHexString(IList<byte> bt) {
                var s = string.Empty;

                for (int i = 0; i < bt.Count; i++) {
                    var b = bt[i];
                    int n = b;
                    var n1 = n & 15;
                    var n2 = (n >> 4) & 15;

                    if (n2 > 9)
                        s += ((char) (n2 - 10 + 'A')).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    else
                        s += n2.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    if (n1 > 9)
                        s += ((char) (n1 - 10 + 'A')).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    else
                        s += n1.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    if ((i + 1) != bt.Count && (i + 1) % 2 == 0) s += "-";
                }

                return s;
            }

            static string Identifier(string wmiClass, string wmiProperty, string wmiMustBeTrue) {
                var result = "";
                var mc = new System.Management.ManagementClass(wmiClass);
                var moc = mc.GetInstances();

                foreach (System.Management.ManagementBaseObject mo in moc) {
                    if (mo[wmiMustBeTrue].ToString() != "True") continue;

                    if (result != "") continue;

                    try {
                        result = mo[wmiProperty].ToString();

                        break;
                    }
                    catch { }
                }

                return result;
            }

            static string Identifier(string wmiClass, string wmiProperty) {
                var result = "";
                var mc = new System.Management.ManagementClass(wmiClass);
                var moc = mc.GetInstances();

                foreach (System.Management.ManagementBaseObject mo in moc) {
                    if (result != "") continue;

                    try {
                        result = mo[wmiProperty].ToString();

                        break;
                    }
                    catch { }
                }

                return result;
            }

            static string CpuID() {
                var retVal = Identifier("Win32_Processor", "UniqueId");

                if (retVal != "") return retVal;
                retVal = Identifier("Win32_Processor", "ProcessorId");

                if (retVal != "") return retVal;
                retVal = Identifier("Win32_Processor", "Name");

                if (retVal == "") {
                    retVal = Identifier("Win32_Processor", "Manufacturer");
                }

                retVal += Identifier("Win32_Processor", "MaxClockSpeed");

                return retVal;
            }

            // BIOS Identifier
            static string BiosID() {
                return Identifier("Win32_BIOS", "Manufacturer") + Identifier("Win32_BIOS", "SMBIOSBIOSVersion") +
                       Identifier("Win32_BIOS", "IdentificationCode") + Identifier("Win32_BIOS", "SerialNumber") +
                       Identifier("Win32_BIOS", "ReleaseDate") + Identifier("Win32_BIOS", "Version");
            }

            // Motherboard ID
            static string BaseID() {
                return Identifier("Win32_BaseBoard", "Model") + Identifier("Win32_BaseBoard", "Manufacturer") +
                       Identifier("Win32_BaseBoard", "Name") + Identifier("Win32_BaseBoard", "SerialNumber");
            }
        }

        #endregion HWID

        #region Other

        internal class Other {
            public static byte[] DecryptModule(byte[] File, string Password) {
                byte[] PasswordBytes = Encoding.UTF8.GetBytes(Password);
                PasswordBytes = SHA256.Create().ComputeHash(PasswordBytes);
                byte[] DecryptedBytes = null;
                byte[] SaltBytes = PasswordBytes;

                using (MemoryStream ms = new MemoryStream()) {
                    using (RijndaelManaged AES = new RijndaelManaged()) {
                        AES.KeySize = 256;
                        AES.BlockSize = 128;

                        var key = new Rfc2898DeriveBytes(PasswordBytes, SaltBytes, 1000);
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);

                        AES.Mode = CipherMode.CBC;

                        using (CryptoStream cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write)) {
                            cs.Write(File, 0, File.Length);
                            cs.Close();
                        }

                        DecryptedBytes = ms.ToArray();
                    }
                }

                return DecryptedBytes;
            }

            public static string MD5Hexstring(string Input) {
                MD5 sec = new MD5CryptoServiceProvider();
                byte[] Bytes = Encoding.ASCII.GetBytes(Input);

                IList<byte> Byte2 = sec.ComputeHash(Bytes);

                var temp = string.Empty;

                for (int i = 0; i < Byte2.Count; i++) {
                    var b = Byte2[i];
                    int n = b;
                    var n1 = n & 15;
                    var n2 = (n >> 4) & 15;

                    if (n2 > 9)
                        temp += ((char) (n2 - 10 + 'A')).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    else
                        temp += n2.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    if (n1 > 9)
                        temp += ((char) (n1 - 10 + 'A')).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    else
                        temp += n1.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    if ((i + 1) != Byte2.Count && (i + 1) % 2 == 0) temp += "-";
                }

                return temp;
            }

            public static string Base64Encode(string Input) {
                var plainTextBytes = Encoding.UTF8.GetBytes(Input);

                return Convert.ToBase64String(plainTextBytes);
            }

            public static string Base64Decode(string Input) {
                var base64EncodedBytes = Convert.FromBase64String(Input);

                return Encoding.UTF8.GetString(base64EncodedBytes);
            }

            public static string RandomString(int Length) {
                Random Rand = new Random();

                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";

                return new string(Enumerable.Repeat(chars, Length)
                    .Select(s => s[Rand.Next(s.Length)]).ToArray());
            }

            static List<string> ProcList = new List<string>() {
                "dnSpy",
                "dnSpy-x86",
                "Fiddler",
                "ida",
                "ollydbg",
                "MegaDumper",
                "X64NetDumper",
                "x64dbg",
                "cheatengine-x86_64",
                "cheatengine-i386",
                "Extreme Injector v3",
                "Extreme Injector v2",
                "Extreme Injector v1"
            };

            public static bool IsDebugging() {
                /* Antinet by 0xd4d - https://github.com/0xd4d/antinet/ */

                if (Debugger.IsLogging()) return true;
                if (Debugger.IsAttached) return true;

                if (AntiManagedProfiler.IsProfilerAttached) return true;

                foreach (string Proc in ProcList) {
                    if (Process.GetProcessesByName(Proc).Length > 0) return true;
                }

                return false;
            }

            public static void StartProfilerSpam() {
                Random Rand = new Random();

                while (true) {

                    Thread.Sleep(50);

                    int Num = Rand.Next(0, 6);

                    switch (Num) {
                        case 1:
                            Jnk1.Execute();

                            break;

                        case 2:
                            Jnk2.Execute();

                            break;

                        case 3:
                            Jnk3.Execute();

                            break;

                        case 4:
                            Jnk4.Execute();

                            break;

                        case 5:
                            Jnk5.Execute();

                            break;

                        default:

                            break;
                    }
                }
            }

            public static void StartAntiDebug() {
                if (!AntiManagedProfiler.Initialize()) Environment.Exit(0);
                if (!AntiManagedDebugger.Initialize()) Environment.Exit(0);

                while (true) {
                    if (IsDebugging()) Environment.Exit(0);
                    Thread.Sleep(250);
                }
            }

            public static unsafe void StartAntiDump() {
                uint old;
                var module = typeof(Other).Module;
                var bas = (byte*) Marshal.GetHINSTANCE(module);
                byte* ptr = bas + 0x3c;
                byte* ptr2;
                ptr = ptr2 = bas + *(uint*) ptr;
                ptr += 0x6;
                var sectNum = *(ushort*) ptr;
                ptr += 14;
                var optSize = *(ushort*) ptr;
                ptr = ptr2 = ptr + 0x4 + optSize;

                byte* @new = stackalloc byte[11];

                if (module.FullyQualifiedName[0] != '<') {
                    byte* mdDir = bas + *(uint*) (ptr - 16);

                    if (*(uint*) (ptr - 0x78) != 0) {
                        byte* importDir = bas + *(uint*) (ptr - 0x78);
                        byte* oftMod = bas + *(uint*) importDir;
                        byte* modName = bas + *(uint*) (importDir + 12);
                        byte* funcName = bas + *(uint*) oftMod + 2;
                        Imports.VirtualProtect(modName, 11, 0x40, out old);

                        *(uint*) @new = 0x6c64746e;
                        *((uint*) @new + 1) = 0x6c642e6c;
                        *((ushort*) @new + 4) = 0x006c;
                        *(@new + 10) = 0;

                        for (int i = 0; i < 11; i++)
                            *(modName + i) = *(@new + i);

                        Imports.VirtualProtect(funcName, 11, 0x40, out old);

                        *(uint*) @new = 0x6f43744e;
                        *((uint*) @new + 1) = 0x6e69746e;
                        *((ushort*) @new + 4) = 0x6575;
                        *(@new + 10) = 0;

                        for (int i = 0; i < 11; i++)
                            *(funcName + i) = *(@new + i);
                    }

                    for (int i = 0; i < sectNum; i++) {
                        Imports.VirtualProtect(ptr, 8, 0x40, out old);
                        Marshal.Copy(new byte[8], 0, (IntPtr) ptr, 8);
                        ptr += 0x28;
                    }

                    Imports.VirtualProtect(mdDir, 0x48, 0x40, out old);
                    byte* mdHdr = bas + *(uint*) (mdDir + 8);
                    *(uint*) mdDir = 0;
                    *((uint*) mdDir + 1) = 0;
                    *((uint*) mdDir + 2) = 0;
                    *((uint*) mdDir + 3) = 0;

                    Imports.VirtualProtect(mdHdr, 4, 0x40, out old);
                    *(uint*) mdHdr = 0;
                    mdHdr += 12;
                    mdHdr += *(uint*) mdHdr;
                    mdHdr = (byte*) (((ulong) mdHdr + 7) & ~3UL);
                    mdHdr += 2;
                    ushort numOfStream = *mdHdr;
                    mdHdr += 2;

                    for (int i = 0; i < numOfStream; i++) {
                        Imports.VirtualProtect(mdHdr, 8, 0x40, out old);
                        mdHdr += 4;
                        mdHdr += 4;

                        for (int ii = 0; ii < 8; ii++) {
                            Imports.VirtualProtect(mdHdr, 4, 0x40, out old);
                            *mdHdr = 0;
                            mdHdr++;

                            if (*mdHdr == 0) {
                                mdHdr += 3;

                                break;
                            }

                            *mdHdr = 0;
                            mdHdr++;

                            if (*mdHdr == 0) {
                                mdHdr += 2;

                                break;
                            }

                            *mdHdr = 0;
                            mdHdr++;

                            if (*mdHdr == 0) {
                                mdHdr += 1;

                                break;
                            }

                            *mdHdr = 0;
                            mdHdr++;
                        }
                    }
                }
                else {
                    var mdDir = *(uint*) (ptr - 16);
                    var importDir = *(uint*) (ptr - 0x78);

                    var vAdrs = new uint[sectNum];
                    var vSizes = new uint[sectNum];
                    var rAdrs = new uint[sectNum];

                    for (int i = 0; i < sectNum; i++) {
                        Imports.VirtualProtect(ptr, 8, 0x40, out old);
                        Marshal.Copy(new byte[8], 0, (IntPtr) ptr, 8);
                        vAdrs[i] = *(uint*) (ptr + 12);
                        vSizes[i] = *(uint*) (ptr + 8);
                        rAdrs[i] = *(uint*) (ptr + 20);
                        ptr += 0x28;
                    }

                    if (importDir != 0) {
                        for (int i = 0; i < sectNum; i++)
                            if (vAdrs[i] <= importDir && importDir < vAdrs[i] + vSizes[i]) {
                                importDir = importDir - vAdrs[i] + rAdrs[i];

                                break;
                            }

                        byte* importDirPtr = bas + importDir;
                        var oftMod = *(uint*) importDirPtr;

                        for (int i = 0; i < sectNum; i++)
                            if (vAdrs[i] <= oftMod && oftMod < vAdrs[i] + vSizes[i]) {
                                oftMod = oftMod - vAdrs[i] + rAdrs[i];

                                break;
                            }

                        byte* oftModPtr = bas + oftMod;
                        var modName = *(uint*) (importDirPtr + 12);

                        for (int i = 0; i < sectNum; i++)
                            if (vAdrs[i] <= modName && modName < vAdrs[i] + vSizes[i]) {
                                modName = modName - vAdrs[i] + rAdrs[i];

                                break;
                            }

                        var funcName = *(uint*) oftModPtr + 2;

                        for (int i = 0; i < sectNum; i++)
                            if (vAdrs[i] <= funcName && funcName < vAdrs[i] + vSizes[i]) {
                                funcName = funcName - vAdrs[i] + rAdrs[i];

                                break;
                            }

                        Imports.VirtualProtect(bas + modName, 11, 0x40, out old);

                        *(uint*) @new = 0x6c64746e;
                        *((uint*) @new + 1) = 0x6c642e6c;
                        *((ushort*) @new + 4) = 0x006c;
                        *(@new + 10) = 0;

                        for (int i = 0; i < 11; i++)
                            *(bas + modName + i) = *(@new + i);

                        Imports.VirtualProtect(bas + funcName, 11, 0x40, out old);

                        *(uint*) @new = 0x6f43744e;
                        *((uint*) @new + 1) = 0x6e69746e;
                        *((ushort*) @new + 4) = 0x6575;
                        *(@new + 10) = 0;

                        for (int i = 0; i < 11; i++)
                            *(bas + funcName + i) = *(@new + i);
                    }

                    for (int i = 0; i < sectNum; i++)
                        if (vAdrs[i] <= mdDir && mdDir < vAdrs[i] + vSizes[i]) {
                            mdDir = mdDir - vAdrs[i] + rAdrs[i];

                            break;
                        }

                    byte* mdDirPtr = bas + mdDir;
                    Imports.VirtualProtect(mdDirPtr, 0x48, 0x40, out old);
                    var mdHdr = *(uint*) (mdDirPtr + 8);

                    for (int i = 0; i < sectNum; i++)
                        if (vAdrs[i] <= mdHdr && mdHdr < vAdrs[i] + vSizes[i]) {
                            mdHdr = mdHdr - vAdrs[i] + rAdrs[i];

                            break;
                        }

                    *(uint*) mdDirPtr = 0;
                    *((uint*) mdDirPtr + 1) = 0;
                    *((uint*) mdDirPtr + 2) = 0;
                    *((uint*) mdDirPtr + 3) = 0;

                    byte* mdHdrPtr = bas + mdHdr;
                    Imports.VirtualProtect(mdHdrPtr, 4, 0x40, out old);
                    *(uint*) mdHdrPtr = 0;
                    mdHdrPtr += 12;
                    mdHdrPtr += *(uint*) mdHdrPtr;
                    mdHdrPtr = (byte*) (((ulong) mdHdrPtr + 7) & ~3UL);
                    mdHdrPtr += 2;
                    ushort numOfStream = *mdHdrPtr;
                    mdHdrPtr += 2;

                    for (int i = 0; i < numOfStream; i++) {
                        Imports.VirtualProtect(mdHdrPtr, 8, 0x40, out old);
                        mdHdrPtr += 4;
                        mdHdrPtr += 4;

                        for (int ii = 0; ii < 8; ii++) {
                            Imports.VirtualProtect(mdHdrPtr, 4, 0x40, out old);
                            *mdHdrPtr = 0;
                            mdHdrPtr++;

                            if (*mdHdrPtr == 0) {
                                mdHdrPtr += 3;

                                break;
                            }

                            *mdHdrPtr = 0;
                            mdHdrPtr++;

                            if (*mdHdrPtr == 0) {
                                mdHdrPtr += 2;

                                break;
                            }

                            *mdHdrPtr = 0;
                            mdHdrPtr++;

                            if (*mdHdrPtr == 0) {
                                mdHdrPtr += 1;

                                break;
                            }

                            *mdHdrPtr = 0;
                            mdHdrPtr++;
                        }
                    }
                }
            }
        }

        #endregion Other

        #region Imports

        internal class Imports {
            [DllImport("kernel32.dll")]
            public static extern unsafe bool VirtualProtect(byte* lpAddress, int dwSize, uint flNewProtect,
                out uint lpflOldProtect);
        }

        #endregion Imports
    }
}

/* Made by Roshly */
// Phase 3, Prototype 13b: launching a process INTO an AppContainer.
//
// Prototype 13 validated the enforcement half of ADR-0004 (job object memory
// caps are real, per-job accounting is readable) and confirmed an AppContainer
// profile can be created. It explicitly did NOT test the harder half: whether a
// process can actually be launched inside a container, and whether the sandbox
// then denies what it should. That was the remaining Phase 5 blocker.
//
// Design: rather than sandbox a custom .NET child (which would need the shared
// framework directory ACL'd for the container SID — invasive, and it would
// muddy the result), this launches cmd.exe. System32 already grants
// ALL APPLICATION PACKAGES read+execute, and every AppContainer is a member of
// that group, so no ACL anywhere needs modifying.
//
// Three tests, in the order that makes the result meaningful:
//   A (control)     — run "exit 42" inside the container. Proves process
//                     creation into an AppContainer works at all.
//   B (baseline)    — read a probe file OUTSIDE the container. Proves the file
//                     exists and is readable normally.
//   C (isolation)   — read the same file INSIDE the container. Must fail.
//
// B is what makes C meaningful: without it, C failing could just mean a bad path.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
static class NativeMethods
{
    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    public static extern int CreateAppContainerProfile(
        string pszAppContainerName, string pszDisplayName, string pszDescription,
        IntPtr pCapabilities, int dwCapabilityCount, out IntPtr ppSidAppContainerSid);

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    public static extern int DeriveAppContainerSidFromAppContainerName(
        string pszAppContainerName, out IntPtr ppsidAppContainerSid);

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    public static extern int DeleteAppContainerProfile(string pszAppContainerName);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool ConvertSidToStringSidW(IntPtr sid, out IntPtr stringSid);

    [DllImport("kernel32.dll")]
    public static extern IntPtr LocalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool InitializeProcThreadAttributeList(
        IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool UpdateProcThreadAttribute(
        IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize,
        IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll")]
    public static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateProcessW(
        string? lpApplicationName, string lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
        uint dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory,
        ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    public const uint CREATE_NO_WINDOW = 0x08000000;
    public static readonly IntPtr PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES = new(0x00020009);

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_CAPABILITIES
    {
        public IntPtr AppContainerSid;
        public IntPtr Capabilities;
        public uint CapabilityCount;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFOW
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars;
        public int dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFOEX
    {
        public STARTUPINFOW StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }
}

[SupportedOSPlatform("windows")]
static class Program
{
    const string ContainerName = "DesktopRuntime.Prototype13b";

    static int Main()
    {
        Console.WriteLine("Phase 3 Prototype 13b: launching a process into an AppContainer");
        Console.WriteLine($"OS: {Environment.OSVersion.VersionString}");
        Console.WriteLine();

        string probeFile = Path.Combine(Path.GetTempPath(), "desktop-runtime-13b-secret.txt");
        File.WriteAllText(probeFile, "this file must not be readable from inside the container");
        Console.WriteLine($"Probe file: {probeFile}");

        IntPtr sid = IntPtr.Zero;

        try
        {
            if (!TryGetContainerSid(out sid))
            {
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("=== A. Control: process creation into the container ===");
            var control = RunInContainer(sid, "cmd.exe /c exit 42");
            bool launchWorks = control.Launched && control.ExitCode == 42;
            Console.WriteLine(launchWorks
                ? "  RESULT: PASS — process ran inside the AppContainer and returned its exit code."
                : $"  RESULT: FAIL — launched={control.Launched}, exit={control.ExitCode}, err={control.Error}");

            Console.WriteLine();
            Console.WriteLine("=== B. Baseline: read the probe file OUTSIDE the container ===");
            int baselineExit = RunNormally($"cmd.exe /c type \"{probeFile}\" >nul 2>&1");
            bool readableNormally = baselineExit == 0;
            Console.WriteLine(readableNormally
                ? "  RESULT: PASS — the file is readable normally, so a failure in C means the sandbox."
                : $"  RESULT: INCONCLUSIVE — the file was not readable even outside the container (exit {baselineExit}).");

            Console.WriteLine();
            Console.WriteLine("=== C. Isolation: read the same file INSIDE the container ===");
            var isolation = RunInContainer(sid, $"cmd.exe /c type \"{probeFile}\" >nul 2>&1");
            bool denied = isolation.Launched && isolation.ExitCode != 0;
            Console.WriteLine($"  launched={isolation.Launched}, exit code={isolation.ExitCode}");
            Console.WriteLine(denied
                ? "  RESULT: PASS — the sandboxed process could NOT read the file."
                : "  RESULT: FAIL — the sandboxed process READ the file. Isolation is not working.");

            Console.WriteLine();
            Console.WriteLine("=== SUMMARY ===");
            Console.WriteLine($"  A. Launch into AppContainer works : {launchWorks}");
            Console.WriteLine($"  B. File readable outside container: {readableNormally}");
            Console.WriteLine($"  C. File denied inside container   : {denied}");

            bool conclusive = launchWorks && readableNormally && denied;
            Console.WriteLine();
            Console.WriteLine(conclusive
                ? "OVERALL: AppContainer isolation is demonstrably working (differential test)."
                : "OVERALL: inconclusive or failing — see individual results above.");

            return conclusive ? 0 : 1;
        }
        finally
        {
            if (sid != IntPtr.Zero) NativeMethods.LocalFree(sid);
            if (File.Exists(probeFile)) File.Delete(probeFile);
            int hr = NativeMethods.DeleteAppContainerProfile(ContainerName);
            Console.WriteLine();
            Console.WriteLine($"Cleanup: probe file deleted, DeleteAppContainerProfile hr=0x{hr:X8}");
        }
    }

    static bool TryGetContainerSid(out IntPtr sid)
    {
        int hr = NativeMethods.CreateAppContainerProfile(
            ContainerName, "Desktop Runtime probe 13b",
            "Temporary AppContainer; deleted at the end of this run.",
            IntPtr.Zero, 0, out sid);

        // 0x800700B7 = ERROR_ALREADY_EXISTS — a leftover profile, so derive its SID instead.
        if ((uint)hr == 0x800700B7)
        {
            hr = NativeMethods.DeriveAppContainerSidFromAppContainerName(ContainerName, out sid);
        }

        if (hr != 0 || sid == IntPtr.Zero)
        {
            Console.WriteLine($"Could not obtain an AppContainer SID, hr=0x{hr:X8}");
            return false;
        }

        if (NativeMethods.ConvertSidToStringSidW(sid, out IntPtr sidString))
        {
            Console.WriteLine($"AppContainer SID: {Marshal.PtrToStringUni(sidString)}");
            NativeMethods.LocalFree(sidString);
        }

        // No capabilities are granted, deliberately: this is the default-deny baseline a
        // package with an empty permission set would run under.
        Console.WriteLine("Capabilities granted: none (default-deny baseline)");
        return true;
    }

    static int RunNormally(string commandLine)
    {
        var psi = new ProcessStartInfo("cmd.exe", commandLine[("cmd.exe ".Length)..])
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process!.WaitForExit(10_000);
        return process.ExitCode;
    }

    static (bool Launched, uint ExitCode, string? Error) RunInContainer(IntPtr sid, string commandLine)
    {
        IntPtr attributeList = IntPtr.Zero;
        IntPtr capabilitiesPtr = IntPtr.Zero;

        try
        {
            IntPtr size = IntPtr.Zero;
            NativeMethods.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
            attributeList = Marshal.AllocHGlobal(size);

            if (!NativeMethods.InitializeProcThreadAttributeList(attributeList, 1, 0, ref size))
            {
                return (false, 0, $"InitializeProcThreadAttributeList win32={Marshal.GetLastWin32Error()}");
            }

            var capabilities = new NativeMethods.SECURITY_CAPABILITIES
            {
                AppContainerSid = sid,
                Capabilities = IntPtr.Zero,
                CapabilityCount = 0,
                Reserved = 0
            };

            capabilitiesPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.SECURITY_CAPABILITIES>());
            Marshal.StructureToPtr(capabilities, capabilitiesPtr, false);

            if (!NativeMethods.UpdateProcThreadAttribute(
                    attributeList, 0, NativeMethods.PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES,
                    capabilitiesPtr, Marshal.SizeOf<NativeMethods.SECURITY_CAPABILITIES>(),
                    IntPtr.Zero, IntPtr.Zero))
            {
                return (false, 0, $"UpdateProcThreadAttribute win32={Marshal.GetLastWin32Error()}");
            }

            var startupInfo = new NativeMethods.STARTUPINFOEX
            {
                StartupInfo = new NativeMethods.STARTUPINFOW
                {
                    cb = Marshal.SizeOf<NativeMethods.STARTUPINFOEX>()
                },
                lpAttributeList = attributeList
            };

            // A mutable copy: CreateProcessW may write to the command line buffer.
            string mutableCommandLine = commandLine;

            if (!NativeMethods.CreateProcessW(
                    null, mutableCommandLine, IntPtr.Zero, IntPtr.Zero, false,
                    NativeMethods.EXTENDED_STARTUPINFO_PRESENT | NativeMethods.CREATE_NO_WINDOW,
                    IntPtr.Zero, null, ref startupInfo, out var processInfo))
            {
                return (false, 0, $"CreateProcess win32={Marshal.GetLastWin32Error()}");
            }

            try
            {
                NativeMethods.WaitForSingleObject(processInfo.hProcess, 15_000);
                NativeMethods.GetExitCodeProcess(processInfo.hProcess, out uint exitCode);
                return (true, exitCode, null);
            }
            finally
            {
                NativeMethods.CloseHandle(processInfo.hThread);
                NativeMethods.CloseHandle(processInfo.hProcess);
            }
        }
        finally
        {
            if (attributeList != IntPtr.Zero)
            {
                NativeMethods.DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
            }

            if (capabilitiesPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(capabilitiesPtr);
            }
        }
    }
}

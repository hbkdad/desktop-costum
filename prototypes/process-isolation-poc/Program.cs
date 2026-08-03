// Phase 3, Prototype 13: process isolation mechanisms.
//
// ADR-0004 commits to sandboxing package code in a per-package process with an
// AppContainer, a restricted token, and a job object whose memory/CPU caps come
// from the manifest's declared budget. None of that was validated when the ADR
// was written, which is why Prototype 13 was promoted to a Phase 5 blocker.
//
// This probe tests the three assumptions that matter most, on real hardware:
//
//   1. Job object memory caps are actually ENFORCED — not merely settable.
//      This is what turns ResourceLedger's measurement into enforcement.
//   2. Job accounting can be READ back — this is where ResourceLedger's samples
//      would come from in production.
//   3. An AppContainer profile can be created without administrator rights.
//
// It changes no system state permanently: the job object is process-scoped and
// any AppContainer profile created is deleted again before exit.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
static class NativeMethods
{
    // --- Job objects ---

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetInformationJobObject(
        IntPtr hJob, int JobObjectInformationClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInformation, int cbJobObjectInformationLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool QueryInformationJobObject(
        IntPtr hJob, int JobObjectInformationClass, out JOBOBJECT_BASIC_ACCOUNTING_INFORMATION lpJobObjectInformation,
        int cbJobObjectInformationLength, IntPtr lpReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();

    // --- AppContainer ---

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    public static extern int CreateAppContainerProfile(
        string pszAppContainerName, string pszDisplayName, string pszDescription,
        IntPtr pCapabilities, int dwCapabilityCount, out IntPtr ppSidAppContainerSid);

    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    public static extern int DeleteAppContainerProfile(string pszAppContainerName);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool ConvertSidToStringSidW(IntPtr sid, out IntPtr stringSid);

    [DllImport("kernel32.dll")]
    public static extern IntPtr LocalFree(IntPtr hMem);

    public const int JobObjectBasicAccountingInformation = 1;
    public const int JobObjectExtendedLimitInformation = 9;

    public const uint JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100;
    public const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x00000200;

    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
    {
        public long TotalUserTime;
        public long TotalKernelTime;
        public long ThisPeriodTotalUserTime;
        public long ThisPeriodTotalKernelTime;
        public uint TotalPageFaultCount;
        public uint TotalProcesses;
        public uint ActiveProcesses;
        public uint TotalTerminatedProcesses;
    }
}

[SupportedOSPlatform("windows")]
static class Program
{
    static int Main()
    {
        Console.WriteLine("Phase 3 Prototype 13: process isolation mechanisms");
        Console.WriteLine($"OS: {Environment.OSVersion.VersionString}");
        Console.WriteLine($"Elevated: {IsElevated()}");
        Console.WriteLine();

        var (memoryCapEnforced, accountingReadable) = TestJobObjectLimitsAndAccounting();
        Console.WriteLine();
        bool appContainerAvailable = TestAppContainerProfile();

        Console.WriteLine();
        Console.WriteLine("=== SUMMARY ===");
        Console.WriteLine($"  Job memory cap enforced      : {memoryCapEnforced}");
        Console.WriteLine($"  Job accounting readable      : {accountingReadable}");
        Console.WriteLine($"  AppContainer profile creatable: {appContainerAvailable}");

        return memoryCapEnforced && accountingReadable ? 0 : 1;
    }

    static bool IsElevated()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Exercises one job through its whole lifecycle, so accounting is read from a job
    /// that actually contains a process. Querying an empty job returns all zeros, which
    /// would prove the call compiles and nothing more.
    /// </summary>
    static (bool MemoryCapEnforced, bool AccountingReadable) TestJobObjectLimitsAndAccounting()
    {
        Console.WriteLine("=== 1. Job object memory cap + accounting ===");

        IntPtr job = NativeMethods.CreateJobObjectW(IntPtr.Zero, null);
        if (job == IntPtr.Zero)
        {
            Console.WriteLine($"  CreateJobObject FAILED, win32={Marshal.GetLastWin32Error()}");
            return (false, false);
        }

        try
        {
            // Headroom above what this process already uses, so normal operation
            // continues and only a deliberately oversized allocation crosses the line.
            long current = Environment.WorkingSet;
            nuint limit = (nuint)(current + 128L * 1024 * 1024);

            Console.WriteLine($"  Current working set: {current / (1024 * 1024)} MB");
            Console.WriteLine($"  Setting process memory limit: {(ulong)limit / (1024 * 1024)} MB");

            var info = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_PROCESS_MEMORY |
                                 NativeMethods.JOB_OBJECT_LIMIT_JOB_MEMORY
                },
                ProcessMemoryLimit = limit,
                JobMemoryLimit = limit
            };

            int size = Marshal.SizeOf<NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            if (!NativeMethods.SetInformationJobObject(
                    job, NativeMethods.JobObjectExtendedLimitInformation, ref info, size))
            {
                Console.WriteLine($"  SetInformationJobObject FAILED, win32={Marshal.GetLastWin32Error()}");
                return (false, false);
            }

            if (!NativeMethods.AssignProcessToJobObject(job, NativeMethods.GetCurrentProcess()))
            {
                int err = Marshal.GetLastWin32Error();
                Console.WriteLine($"  AssignProcessToJobObject FAILED, win32={err}");
                Console.WriteLine("  (ERROR_ACCESS_DENIED=5 here usually means this process is already in a");
                Console.WriteLine("   job that forbids nesting — e.g. a CI container or debugger host.)");
                return (false, false);
            }

            Console.WriteLine("  Assigned current process to the job.");

            bool accountingReadable = ReportAccounting(job);

            Console.WriteLine();
            Console.WriteLine("  Attempting a 512 MB allocation, which must fail...");

            try
            {
                byte[] hog = new byte[512L * 1024 * 1024];
                hog[0] = 1;
                hog[^1] = 1;
                GC.KeepAlive(hog);

                Console.WriteLine("  RESULT: FAIL — the allocation SUCCEEDED. The cap was not enforced.");
                return (false, accountingReadable);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("  RESULT: PASS — allocation refused with OutOfMemoryException; the cap is real.");
                return (true, accountingReadable);
            }
        }
        finally
        {
            NativeMethods.CloseHandle(job);
        }
    }

    /// <summary>
    /// Where ResourceLedger's samples would come from: per-job accounting read from the
    /// trusted side, rather than asking the sandboxed process to self-report.
    /// </summary>
    static bool ReportAccounting(IntPtr job)
    {
        Console.WriteLine();
        Console.WriteLine("  --- Accounting (ResourceLedger sample source) ---");

        int size = Marshal.SizeOf<NativeMethods.JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>();
        var sw = Stopwatch.StartNew();
        bool ok = NativeMethods.QueryInformationJobObject(
            job, NativeMethods.JobObjectBasicAccountingInformation, out var accounting, size, IntPtr.Zero);
        sw.Stop();

        if (!ok)
        {
            Console.WriteLine($"  QueryInformationJobObject FAILED, win32={Marshal.GetLastWin32Error()}");
            return false;
        }

        Console.WriteLine($"  Active processes          : {accounting.ActiveProcesses}");
        Console.WriteLine($"  Total processes ever in job: {accounting.TotalProcesses}");
        Console.WriteLine($"  Total user time            : {accounting.TotalUserTime / 10_000.0:0.0} ms");
        Console.WriteLine($"  Total kernel time          : {accounting.TotalKernelTime / 10_000.0:0.0} ms");
        Console.WriteLine($"  Page faults                : {accounting.TotalPageFaultCount}");
        Console.WriteLine($"  Query cost                 : {sw.Elapsed.TotalMilliseconds:0.000} ms");

        // All-zero counters would mean the job is empty and the numbers say nothing.
        bool meaningful = accounting.ActiveProcesses > 0 && accounting.TotalUserTime + accounting.TotalKernelTime > 0;
        Console.WriteLine(meaningful
            ? "  RESULT: PASS — real CPU accounting readable from the trusted side."
            : "  RESULT: INCONCLUSIVE — counters are zero; the job may be empty.");

        return meaningful;
    }

    /// <summary>
    /// AppContainer profiles are the OS-level default-deny boundary ADR-0004 relies on.
    /// This checks only that a profile can be created without elevation; it does not yet
    /// launch a process into one.
    /// </summary>
    static bool TestAppContainerProfile()
    {
        Console.WriteLine("=== 2. AppContainer profile ===");

        const string containerName = "DesktopRuntime.Prototype13.Probe";
        IntPtr sid = IntPtr.Zero;

        try
        {
            int hr = NativeMethods.CreateAppContainerProfile(
                containerName,
                "Desktop Runtime probe",
                "Temporary AppContainer created by Prototype 13; deleted immediately.",
                IntPtr.Zero, 0, out sid);

            // 0x800700B7 = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS): a leftover from an
            // earlier run, which still tells us creation is permitted.
            if (hr != 0 && (uint)hr != 0x800700B7)
            {
                Console.WriteLine($"  CreateAppContainerProfile FAILED, hr=0x{hr:X8}");
                Console.WriteLine("  RESULT: AppContainer profile creation is NOT available in this context.");
                return false;
            }

            if (sid != IntPtr.Zero && NativeMethods.ConvertSidToStringSidW(sid, out IntPtr sidString))
            {
                Console.WriteLine($"  AppContainer SID: {Marshal.PtrToStringUni(sidString)}");
                NativeMethods.LocalFree(sidString);
            }

            Console.WriteLine("  RESULT: PASS — an AppContainer profile can be created without elevation.");
            Console.WriteLine("  NOTE: launching a process INTO the container is not covered by this probe.");
            return true;
        }
        catch (DllNotFoundException ex)
        {
            Console.WriteLine($"  userenv.dll unavailable: {ex.Message}");
            return false;
        }
        finally
        {
            if (sid != IntPtr.Zero)
            {
                NativeMethods.LocalFree(sid);
            }

            int deleteResult = NativeMethods.DeleteAppContainerProfile(containerName);
            Console.WriteLine($"  Cleanup: DeleteAppContainerProfile hr=0x{deleteResult:X8}");
        }
    }
}

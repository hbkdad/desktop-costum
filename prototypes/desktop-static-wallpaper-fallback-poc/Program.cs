// Phase 3, Prototype 2 follow-up (Spike B): guaranteed-available static
// wallpaper fallback.
//
// Purpose: Prototypes 1 and 2 both found that rendering *behind desktop
// icons* via WorkerW attachment or z-order tricks is unreliable/impossible
// on this build. This probe tests the one remaining path that sidesteps
// z-order entirely: SystemParametersInfo(SPI_SETDESKWALLPAPER), the actual
// supported Win32 API for setting the desktop background image. This is the
// tier-of-last-resort fallback: static/slow-changing content only, but it
// cannot fail the way both attachment strategies have shown fragility,
// because it doesn't compete for window Z-order at all.
//
// Safety: this probe reads the user's CURRENT wallpaper path before doing
// anything, sets a generated placeholder image, waits briefly, then
// restores the original wallpaper path exactly — in a finally block, so a
// crash mid-probe still restores it. A temp bitmap file is written and
// deleted; nothing else on the system is touched.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

[SupportedOSPlatform("windows")]
static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SystemParametersInfoW")]
    public static extern bool SystemParametersInfoGet(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWinIni);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SystemParametersInfoW")]
    public static extern bool SystemParametersInfoSet(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

    public const uint SPI_GETDESKWALLPAPER = 0x0073;
    public const uint SPI_SETDESKWALLPAPER = 0x0014;
    public const uint SPIF_UPDATEINIFILE = 0x01;
    public const uint SPIF_SENDCHANGE = 0x02;
}

[SupportedOSPlatform("windows")]
static class Program
{
    static int Main()
    {
        Console.WriteLine("Phase 3 Prototype 2, Spike B: static wallpaper fallback (SPI_SETDESKWALLPAPER)");

        string originalWallpaper = GetCurrentWallpaper();
        Console.WriteLine($"Original wallpaper path: \"{originalWallpaper}\"");

        string tempPath = Path.Combine(Path.GetTempPath(), "desktop-runtime-static-fallback-poc.bmp");
        bool setSucceeded = false;

        try
        {
            GeneratePlaceholderBitmap(tempPath);
            Console.WriteLine($"Generated placeholder bitmap: {tempPath}");

            setSucceeded = NativeMethods.SystemParametersInfoSet(
                NativeMethods.SPI_SETDESKWALLPAPER, 0, tempPath,
                NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);

            Console.WriteLine($"SystemParametersInfo(SPI_SETDESKWALLPAPER) returned: {setSucceeded}");

            Thread.Sleep(1500);

            string confirmedWallpaper = GetCurrentWallpaper();
            Console.WriteLine($"Wallpaper path read back after set: \"{confirmedWallpaper}\"");

            bool matches = string.Equals(confirmedWallpaper, tempPath, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine(matches
                ? "RESULT: PASS — desktop wallpaper was set to the generated placeholder image and confirmed via read-back."
                : "RESULT: PARTIAL — API call succeeded but read-back path did not exactly match (may be a path-normalization difference, not necessarily a failure).");
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine("Restoring original wallpaper...");
            bool restored = NativeMethods.SystemParametersInfoSet(
                NativeMethods.SPI_SETDESKWALLPAPER, 0, originalWallpaper,
                NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
            Console.WriteLine($"Restore call returned: {restored}");

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
                Console.WriteLine("Deleted temporary placeholder bitmap.");
            }
        }

        return setSucceeded ? 0 : 1;
    }

    static string GetCurrentWallpaper()
    {
        var sb = new StringBuilder(260);
        NativeMethods.SystemParametersInfoGet(NativeMethods.SPI_GETDESKWALLPAPER, (uint)sb.Capacity, sb, 0);
        return sb.ToString();
    }

    static void GeneratePlaceholderBitmap(string path)
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(18, 32, 58));
            using var font = new Font("Segoe UI", 28f, FontStyle.Bold);
            g.DrawString("Desktop Runtime — static fallback probe (temporary)", font, Brushes.White, 60, 60);
        }
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
    }
}

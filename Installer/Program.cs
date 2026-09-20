// Silent-capable installer for Partner Center / enterprise deploy.
//
// Exit codes (documented at):
// https://downloads.linespotting.com/opening-times-stores/docs/installer-return-codes.html
//
// Switches: /S /s /silent /quiet /q /VERYSILENT /qn
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

static class ExitCodes
{
    public const int Success = 0;
    public const int CancelledByUser = 1602;
    public const int AlreadyExists = 1638;
    public const int AlreadyInProgress = 1618;
    public const int DiskFull = 112;
    public const int RebootRequired = 3010;
    public const int NetworkFailure = 1619;
    public const int PackageRejected = 1625;
    public const int GenericFailure = 1603;
}

static class Program
{
    static readonly string MutexName = "Global\\Linespotting_OpeningTimesStores_Setup_1_0_2";
    static readonly string TargetDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "OpeningTimesStores");
    static readonly string TargetExe = Path.Combine(TargetDir, "OpeningTimesWidget.exe");

    static int Main(string[] args)
    {
        var silent = IsSilent(args);
        var force = args.Any(a =>
        {
            var x = a.Trim().TrimStart('-', '/');
            return x.Equals("force", StringComparison.OrdinalIgnoreCase)
                || x.Equals("repair", StringComparison.OrdinalIgnoreCase)
                || x.Equals("reinstall", StringComparison.OrdinalIgnoreCase);
        });

        // Mutex: another setup already running
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
            return ExitCodes.AlreadyInProgress;

        try
        {
            // Already installed?
            if (!force && File.Exists(TargetExe))
            {
                if (!silent)
                    Info("Opening Times EU is already installed.", silent);
                return ExitCodes.AlreadyExists;
            }

            // Rough free-space check (need ~200 MB free)
            var drive = Path.GetPathRoot(TargetDir) ?? "C:\\";
            if (!HasEnoughDisk(drive, 200L * 1024 * 1024))
            {
                if (!silent)
                    Error("Not enough disk space to install Opening Times EU.", silent);
                return ExitCodes.DiskFull;
            }

            Directory.CreateDirectory(TargetDir);
            ExtractPayload(TargetDir);
            CreateStartMenuShortcut(TargetExe);

            if (!File.Exists(TargetExe))
            {
                if (!silent)
                    Error("Install failed: application files were not written.", silent);
                return ExitCodes.GenericFailure;
            }

            if (!silent)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = TargetExe,
                    UseShellExecute = true,
                    WorkingDirectory = TargetDir
                });
            }

            // No reboot required for this per-user copy install
            return ExitCodes.Success;
        }
        catch (UnauthorizedAccessException ex)
        {
            if (!silent) Error("Package rejected or access denied:\n" + ex.Message, silent);
            return ExitCodes.PackageRejected;
        }
        catch (IOException ex) when (IsDiskFull(ex))
        {
            if (!silent) Error("Disk is full.", silent);
            return ExitCodes.DiskFull;
        }
        catch (IOException ex)
        {
            if (!silent) Error("Install I/O error:\n" + ex.Message, silent);
            return ExitCodes.GenericFailure;
        }
        catch (Exception ex)
        {
            if (!silent) Error("Install failed:\n" + ex.Message, silent);
            // Network-ish message heuristic (payload is embedded; rare)
            if (ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("remote", StringComparison.OrdinalIgnoreCase))
                return ExitCodes.NetworkFailure;
            return ExitCodes.GenericFailure;
        }
    }

    static bool IsSilent(string[] args) => args.Any(a =>
    {
        var x = a.Trim().TrimStart('-', '/');
        return x.Equals("S", StringComparison.OrdinalIgnoreCase)
            || x.Equals("s", StringComparison.OrdinalIgnoreCase)
            || x.Equals("silent", StringComparison.OrdinalIgnoreCase)
            || x.Equals("quiet", StringComparison.OrdinalIgnoreCase)
            || x.Equals("q", StringComparison.OrdinalIgnoreCase)
            || x.Equals("VERYSILENT", StringComparison.OrdinalIgnoreCase)
            || x.Equals("qn", StringComparison.OrdinalIgnoreCase);
    });

    static bool HasEnoughDisk(string root, long bytesNeeded)
    {
        try
        {
            var di = new DriveInfo(root);
            return di.IsReady && di.AvailableFreeSpace >= bytesNeeded;
        }
        catch
        {
            return true; // don't block if unknown
        }
    }

    static bool IsDiskFull(IOException ex)
    {
        const int ERROR_DISK_FULL = unchecked((int)0x80070070);
        const int ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);
        return ex.HResult == ERROR_DISK_FULL || ex.HResult == ERROR_HANDLE_DISK_FULL
            || ex.Message.Contains("disk", StringComparison.OrdinalIgnoreCase);
    }

    static void ExtractPayload(string targetDir)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Embedded payload.zip missing.");

        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Cannot open payload.zip stream.");
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            var dest = Path.Combine(targetDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);
            entry.ExtractToFile(dest, overwrite: true);
        }
    }

    static void CreateStartMenuShortcut(string exePath)
    {
        try
        {
            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var names = new[] { "Opening Times EU.lnk", "Opening Times - Stores.lnk" };
            var vbs = Path.Combine(Path.GetTempPath(), "ots-shortcut-" + Guid.NewGuid().ToString("N") + ".vbs");
            var workDir = Path.GetDirectoryName(exePath) ?? "";
            var exeEsc = exePath.Replace("\"", "\"\"");
            var workEsc = workDir.Replace("\"", "\"\"");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Set o = CreateObject(\"WScript.Shell\")");
            foreach (var name in names)
            {
                var linkPath = Path.Combine(programs, name).Replace("\"", "\"\"");
                sb.AppendLine($@"
Set s = o.CreateShortcut(""{linkPath}"")
s.TargetPath = ""{exeEsc}""
s.WorkingDirectory = ""{workEsc}""
s.Description = ""Opening Times EU""
s.IconLocation = ""{exeEsc},0""
s.Save
");
            }
            File.WriteAllText(vbs, sb.ToString());
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "wscript.exe",
                Arguments = $"//B \"{vbs}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            p?.WaitForExit(15000);
            try { File.Delete(vbs); } catch { /* ignore */ }
        }
        catch { /* optional */ }
    }

    static void Info(string msg, bool silent)
    {
        if (silent) return;
        try
        {
            System.Windows.Forms.MessageBox.Show(msg, "Opening Times EU Setup",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }
        catch { }
    }

    static void Error(string msg, bool silent)
    {
        if (silent) return;
        try
        {
            System.Windows.Forms.MessageBox.Show(msg, "Opening Times EU Setup",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
        catch { }
    }
}

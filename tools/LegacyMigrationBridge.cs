#:property TargetFramework=net461
#:property LangVersion=latest
#:property OutputType=WinExe
#:property AssemblyName=Mapping Tools
#:property PublishAot=false
#:property DebugType=embedded

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

// Published by release.yml without a project file. Uses Windows' existing .NET Framework,
// just like the legacy client, rather than bundling another runtime into the update ZIP.
internal static class LegacyMigrationBridge
{
    private const string setup_name = "MappingTools-Setup.exe";
    private const string uninstall_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{50E2DF2A-2D83-4958-B5A6-EBFD651B9CA7}_is1";
    private static string? logPath;

    private static int Main(string[] args)
    {
        try
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) throw new PlatformNotSupportedException("Migration requires Windows.");

            if (args.Length == 3 && args[0] == "--migrate")
                Migrate(Path.GetFullPath(args[1]), int.Parse(args[2]));
            else
                Stage();

            return 0;
        }
        catch (Exception exception)
        {
            Log(exception.ToString());
            MessageBoxW(0, $"Mapping Tools migration could not finish.\n\n{exception.Message}\n\n"
                + "The new application, if installed, is available from the Start menu."
                + (logPath is null ? "" : $"\nLog: {logPath}"), "Mapping Tools migration", 0x10);
            return 1;
        }
    }

    private static void Stage()
    {
        string executable = CurrentExecutable();
        string legacyDirectory = Path.GetDirectoryName(executable)!;
        string setup = Path.Combine(legacyDirectory, setup_name);
        if (!File.Exists(setup)) throw new FileNotFoundException("The new installer must be beside Mapping Tools.exe.", setup);

        using var user = new DesktopUser();
        string stagingDirectory = Path.Combine(user.LocalAppData, "Temp", $"mapping-tools-migration-{Guid.NewGuid():N}");
        logPath = stagingDirectory + ".log";
        Directory.CreateDirectory(stagingDirectory);
        File.Copy(executable, Path.Combine(stagingDirectory, "Mapping Tools.exe"));
        File.Copy(setup, Path.Combine(stagingDirectory, setup_name));
        Log($"Migrating {legacyDirectory}; staged at {stagingDirectory}.");

        // Retain inherited elevation here. Only installer/app processes use the desktop user's token.
        using var worker = Process.Start(new ProcessStartInfo(Path.Combine(stagingDirectory, "Mapping Tools.exe"))
        {
            UseShellExecute = false,
            WorkingDirectory = stagingDirectory,
            Arguments = "--migrate " + QuoteArgument(legacyDirectory) + " " + CurrentProcessId()
        }) ?? throw new InvalidOperationException("Could not start the temporary migration bridge.");
    }

    private static void Migrate(string legacyDirectory, int parentId)
    {
        string stagingDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        logPath = stagingDirectory + ".log";
        using var user = new DesktopUser();
        string newDirectory = Path.Combine(user.LocalAppData, "MappingTools");
        ValidateCleanupPaths(legacyDirectory, stagingDirectory, user.LocalAppData, user.RoamingAppData);
        WaitForOriginalBridge(parentId, Path.Combine(legacyDirectory, "Mapping Tools.exe"));

        using var hash = SHA256.Create();
        string mutexName = "Local\\MappingToolsMigration-" + BitConverter.ToString(
            hash.ComputeHash(Encoding.UTF8.GetBytes(legacyDirectory.ToUpperInvariant()))).Replace("-", "");
        using var mutex = new Mutex(false, mutexName);
        bool acquired;
        try { acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) throw new InvalidOperationException("Another migration is already running.");

        try
        {
            string? uninstaller = FindUninstaller(legacyDirectory);
            string newExecutable = Path.Combine(newDirectory, "current", "Mapping Tools.exe");
            Version requiredVersion = Assembly.GetExecutingAssembly().GetName().Version!;
            if (!IsInstalled(newDirectory, requiredVersion))
            {
                Log("Installing the new Mapping Tools for the desktop user.");
                int exitCode = user.Run(Path.Combine(stagingDirectory, setup_name),
                    ["--silent", "--installto", newDirectory, "--log", logPath + ".setup"], true);
                if (exitCode != 0 || !IsInstalled(newDirectory, requiredVersion))
                    throw new InvalidOperationException($"The new installer did not complete successfully (exit code {exitCode}). The legacy installation has been retained.");
            }

            if (!user.IsElevated && uninstaller is not null)
            {
                // One UAC prompt covers both uninstall and final cleanup. Installation has
                // already completed for the original user before requesting elevation.
                try
                {
                    using var elevated = Process.Start(new ProcessStartInfo(CurrentExecutable())
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = stagingDirectory,
                        Arguments = "--migrate " + QuoteArgument(legacyDirectory) + " " + CurrentProcessId()
                    }) ?? throw new InvalidOperationException("Could not obtain permission to uninstall the legacy version.");
                }
                catch
                {
                    user.Run(newExecutable, [], false);
                    throw;
                }
                return;
            }

            Exception? uninstallFailure = null;
            try
            {
                if (uninstaller is null)
                    throw new InvalidOperationException("No registered legacy uninstaller was found in the old installation folder. The folder has been retained.");

                Log($"Running legacy uninstaller {uninstaller}.");
                using var uninstall = Process.Start(new ProcessStartInfo(uninstaller)
                {
                    UseShellExecute = true,
                    Verb = user.IsElevated ? "" : "runas",
                    WorkingDirectory = stagingDirectory,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG=" + QuoteArgument(logPath + ".uninstall")
                }) ?? throw new InvalidOperationException("Could not start the legacy uninstaller.");
                uninstall.WaitForExit();
                if (uninstall.ExitCode != 0)
                    throw new InvalidOperationException($"The legacy uninstaller failed (exit code {uninstall.ExitCode}). Its remaining files have been retained.");
            }
            catch (Exception exception)
            {
                uninstallFailure = exception;
            }

            Log($"Launching {newExecutable} for the desktop user.");
            user.Run(newExecutable, [], false);
            if (uninstallFailure is not null) throw uninstallFailure;

            // The helper runs outside both directories and waits until this executable is unlocked.
            string command = BuildCleanupCommand(legacyDirectory, stagingDirectory, CurrentProcessId(), logPath);
            using var cleanup = Process.Start(new ProcessStartInfo(PowerShellPath())
            {
                UseShellExecute = true,
                Verb = user.IsElevated ? "" : "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand "
                    + Convert.ToBase64String(Encoding.Unicode.GetBytes(command))
            }) ?? throw new InvalidOperationException("Could not start final cleanup.");
            Log("Uninstall completed; final cleanup scheduled.");
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static void WaitForOriginalBridge(int parentId, string originalExecutable)
    {
        try
        {
            using var parent = Process.GetProcessById(parentId);
            if ((string.Equals(parent.MainModule?.FileName, originalExecutable, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(parent.MainModule?.FileName, CurrentExecutable(), StringComparison.OrdinalIgnoreCase))
                && !parent.WaitForExit(30000))
                throw new TimeoutException("The original migration bridge did not exit.");
        }
        catch (ArgumentException) { } // The original bridge normally exits before the worker starts.
        catch (InvalidOperationException) { } // It exited while its module was being inspected.
    }

    private static bool IsInstalled(string directory, Version requiredVersion)
    {
        string executable = Path.Combine(directory, "current", "Mapping Tools.exe");
        if (!File.Exists(executable) || !File.Exists(Path.Combine(directory, "Update.exe"))) return false;
        FileVersionInfo info = FileVersionInfo.GetVersionInfo(executable);
        return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart)
               >= new Version(requiredVersion.Major, requiredVersion.Minor, requiredVersion.Build);
    }

    private static string? FindUninstaller(string legacyDirectory)
    {
        foreach (RegistryHive hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
        {
            using var registry = RegistryKey.OpenBaseKey(hive, view);
            using var key = registry.OpenSubKey(uninstall_key);
            if (key?.GetValue("UninstallString") is not string command) continue;
            string executable = GetUninstallExecutable(command);
            if (!string.Equals(Path.GetDirectoryName(executable), legacyDirectory, StringComparison.OrdinalIgnoreCase)) continue;
            if (!Path.GetFileName(executable).StartsWith("unins", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetExtension(executable), ".exe", StringComparison.OrdinalIgnoreCase)) continue;
            if (File.Exists(executable) && File.Exists(Path.ChangeExtension(executable, ".dat"))) return executable;
        }

        return null;
    }

    private static string GetUninstallExecutable(string command)
    {
        command = command.Trim();
        if (command.StartsWith("\"", StringComparison.Ordinal))
        {
            int end = command.IndexOf('"', 1);
            return end > 1 ? command.Substring(1, end - 1) : "";
        }

        int extension = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return extension >= 0 ? command.Substring(0, extension + 4) : "";
    }

    private static void ValidateCleanupPaths(string legacyDirectory, string stagingDirectory, string localAppData, string roamingAppData)
    {
        string[] protectedPaths =
        [
            localAppData, Path.Combine(localAppData, "MappingTools"), Path.Combine(localAppData, "Mapping Tools"),
            roamingAppData, Path.Combine(roamingAppData, "Mapping Tools"),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            stagingDirectory
        ];
        if (!Path.IsPathRooted(legacyDirectory)
            || !string.Equals(Path.GetFullPath(legacyDirectory), legacyDirectory, StringComparison.OrdinalIgnoreCase)
            || protectedPaths.Where(path => !string.IsNullOrEmpty(path)).Any(path => IsWithin(path, legacyDirectory)))
            throw new InvalidOperationException("The legacy installation folder overlaps a protected folder; it cannot be removed automatically.");

        string temporaryRoot = Path.Combine(localAppData, "Temp");
        if (!string.Equals(Path.GetDirectoryName(stagingDirectory), temporaryRoot, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(stagingDirectory).StartsWith("mapping-tools-migration-", StringComparison.Ordinal)
            || IsWithin(legacyDirectory, stagingDirectory))
            throw new InvalidOperationException("The temporary migration folder is invalid.");

        RejectReparseParents(legacyDirectory);
        RejectReparseParents(stagingDirectory);
    }

    private static bool IsWithin(string path, string directory)
    {
        path = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        directory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
        return string.Equals(path, directory, StringComparison.OrdinalIgnoreCase)
               || path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void RejectReparseParents(string path)
    {
        for (var directory = new DirectoryInfo(path); directory is not null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException($"Automatic cleanup cannot traverse a redirected folder: {directory.FullName}");
    }

    private static string BuildCleanupCommand(string legacyDirectory, string stagingDirectory, int processId, string log)
    {
        static string literal(string value) => "'" + value.Replace("'", "''") + "'";

        return $$"""
            $ErrorActionPreference = 'Stop'
            $log = {{literal(log)}}
            function Remove-MigrationFolder([string]$path) {
                if (-not [IO.Directory]::Exists($path)) { return }
                $directory = [IO.DirectoryInfo]::new($path)
                for ($parent = $directory; $null -ne $parent; $parent = $parent.Parent) {
                    if ($parent.Exists -and ($parent.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                        throw "Refusing to traverse redirected folder: $($parent.FullName)"
                    }
                }
                foreach ($entry in $directory.EnumerateFileSystemInfos()) {
                    if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { $entry.Delete() }
                    elseif ($entry.Attributes -band [IO.FileAttributes]::Directory) { Remove-MigrationFolder $entry.FullName }
                    else { $entry.Attributes = [IO.FileAttributes]::Normal; $entry.Delete() }
                }
                $directory.Delete()
            }
            try {
                Add-Content -LiteralPath $log -Value 'Waiting for the bridge to exit before cleanup.'
                Get-Process -Id {{processId}} -ErrorAction SilentlyContinue | Wait-Process -Timeout 60
                foreach ($path in @({{literal(legacyDirectory)}}, {{literal(stagingDirectory)}})) {
                    for ($attempt = 0; ; $attempt++) {
                        try { Remove-MigrationFolder $path; break }
                        catch { if ($attempt -ge 9) { throw }; Start-Sleep -Milliseconds 500 }
                    }
                }
                Add-Content -LiteralPath $log -Value 'Final cleanup completed.'
            }
            catch {
                Add-Content -LiteralPath $log -Value $_.Exception.ToString()
                Add-Type -AssemblyName System.Windows.Forms
                [Windows.Forms.MessageBox]::Show("Mapping Tools was installed, but cleanup could not finish. See $log", 'Mapping Tools migration') | Out-Null
                exit 1
            }
            """;
    }

    private static string PowerShellPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");

    private static string CurrentExecutable()
    {
        using var process = Process.GetCurrentProcess();
        return process.MainModule!.FileName;
    }

    private static int CurrentProcessId()
    {
        using var process = Process.GetCurrentProcess();
        return process.Id;
    }

    private static string QuoteArgument(string value)
    {
        var result = new StringBuilder("\"");
        int backslashes = 0;
        foreach (char character in value)
        {
            if (character == '\\') { backslashes++; continue; }
            result.Append('\\', character == '"' ? backslashes * 2 + 1 : backslashes);
            result.Append(character);
            backslashes = 0;
        }
        return result.Append('\\', backslashes * 2).Append('"').ToString();
    }

    private static void Log(string message)
    {
        if (logPath is null) return;
        try { File.AppendAllText(logPath, $"{DateTimeOffset.Now:O} {message}\n"); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class DesktopUser : IDisposable
    {
        private readonly SafeAccessTokenHandle token;
        internal bool IsElevated { get; }
        internal string LocalAppData { get; }
        internal string RoamingAppData { get; }

        internal DesktopUser()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            IsElevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            if (IsElevated)
            {
                nint shell = GetShellWindow();
                if (shell == 0) throw new InvalidOperationException("No desktop user could be found. Run the bridge from your Windows desktop.");
                GetWindowThreadProcessId(shell, out uint shellPid);
                using var process = Process.GetProcessById((int)shellPid);
                if (!OpenProcessToken(process.Handle, 0x000A, out var shellToken)) throw new Win32Exception();
                using (shellToken)
                    if (!DuplicateTokenEx(shellToken, 0x02000000, 0, 2, 1, out token)) throw new Win32Exception();
            }
            else
            {
                using var process = Process.GetCurrentProcess();
                if (!OpenProcessToken(process.Handle, 0x0008, out token)) throw new Win32Exception();
            }

            LocalAppData = GetKnownFolder(new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091"));
            RoamingAppData = GetKnownFolder(new Guid("3EB685DB-65F9-4CF6-A03A-E3EF65729F3D"));
        }

        private string GetKnownFolder(Guid folder)
        {
            int result = SHGetKnownFolderPath(ref folder, 0, token, out nint path);
            try
            {
                Marshal.ThrowExceptionForHR(result);
                return Marshal.PtrToStringUni(path)!;
            }
            finally { Marshal.FreeCoTaskMem(path); }
        }

        internal int Run(string executable, string[] arguments, bool wait)
        {
            if (!IsElevated)
            {
                var start = new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(executable)! };
                start.Arguments = string.Join(" ", arguments.Select(QuoteArgument));
                // Do not forward any Velopack hook flags inherited from a previous installer.
                foreach (string key in start.EnvironmentVariables.Keys.Cast<string>().Where(key => key.StartsWith("VELOPACK_", StringComparison.OrdinalIgnoreCase)).ToArray())
                    start.EnvironmentVariables.Remove(key);
                using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");
                if (!wait) return 0;
                process.WaitForExit();
                return process.ExitCode;
            }

            if (!CreateEnvironmentBlock(out nint environment, token, false)) throw new Win32Exception();
            try
            {
                var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>() };
                var command = new StringBuilder(string.Join(" ", new[] { executable }.Concat(arguments).Select(QuoteArgument)));
                if (!CreateProcessWithTokenW(token, 1, executable, command, 0x08000400, environment,
                        Path.GetDirectoryName(executable)!, ref startup, out var process)) throw new Win32Exception();
                using var handle = new SafeProcessHandle(process.Process, true);
                CloseHandle(process.Thread);
                if (!wait) return 0;
                if (WaitForSingleObject(handle, uint.MaxValue) != 0 || !GetExitCodeProcess(handle, out uint exitCode)) throw new Win32Exception();
                return (int)exitCode;
            }
            finally { DestroyEnvironmentBlock(environment); }
        }

        /// <inheritdoc />
        public void Dispose() => token.Dispose();
    }

    // These layouts mirror STARTUPINFOW and PROCESS_INFORMATION on both x86 and x64.
#pragma warning disable CS0649
    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfo
    {
        internal int Size;
        internal nint Reserved, Desktop, Title;
        internal int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        internal short ShowWindow, ReservedSize;
        internal nint ReservedBytes, StandardInput, StandardOutput, StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        internal nint Process, Thread;
        internal uint ProcessId, ThreadId;
    }
#pragma warning restore CS0649

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(nint window, string text, string caption, uint type);
    [DllImport("user32.dll")]
    private static extern nint GetShellWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(nint process, uint access, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(SafeAccessTokenHandle existing, uint access, nint attributes, int level, int type, out SafeAccessTokenHandle token);
    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(ref Guid folder, uint flags, SafeAccessTokenHandle token, out nint path);
    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out nint environment, SafeAccessTokenHandle token, bool inherit);
    [DllImport("userenv.dll")]
    private static extern bool DestroyEnvironmentBlock(nint environment);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessWithTokenW(SafeAccessTokenHandle token, uint flags, string application, StringBuilder command,
        uint creationFlags, nint environment, string directory, ref StartupInfo startup, out ProcessInformation process);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(SafeProcessHandle handle, out uint exitCode);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);
}

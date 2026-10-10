using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

namespace Mapping_Tools.Infrastructure.Editor.Memory;

/// <summary>
/// Reads Windows stable or its Wine process on Linux. Target pointers remain 32-bit;
/// native handles, addresses, and Linux iovec lengths use the host's pointer width.
/// </summary>
internal sealed class EditorProcessMemory(Process process) : IEditorProcessMemory, IDisposable
{
    internal bool HasExited => process.HasExited;

    internal static EditorProcessMemory? Open()
    {
        if (OperatingSystem.IsWindows())
        {
            var windowsProcess = OsuProcessDiscovery.FindStableProcess();
            return windowsProcess is null ? null : new EditorProcessMemory(windowsProcess);
        }

        if (!OperatingSystem.IsLinux()) return null;

        var processes = Process.GetProcessesByName("osu!.exe");
        var selectedProcess = processes.FirstOrDefault();
        foreach (var candidate in processes.Skip(1)) candidate.Dispose();
        return selectedProcess is null ? null : new EditorProcessMemory(selectedProcess);
    }

    public bool TryRead(nint address, Span<byte> destination)
    {
        if (destination.IsEmpty) return true;
        if (address == 0) return false;

        // Arrays avoid requiring unsafe code in Infrastructure. Remote reads must
        // fill the whole buffer; a partial read must never become a valid snapshot.
        byte[] buffer = new byte[destination.Length];
        bool complete;
        if (OperatingSystem.IsWindows())
        {
            complete = ReadProcessMemory(process.Handle, address, buffer, (nuint)buffer.Length, out nuint bytesRead)
                       && bytesRead == (nuint)buffer.Length;
        }
        else
        {
            var localHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var local = new IoVector(localHandle.AddrOfPinnedObject(), (nuint)buffer.Length);
                var remote = new IoVector(address, (nuint)buffer.Length);
                complete = ProcessVmRead(process.Id, ref local, 1, ref remote, 1, 0) == buffer.Length;
            }
            finally
            {
                localHandle.Free();
            }
        }

        if (complete) buffer.CopyTo(destination);
        return complete;
    }

    public IEnumerable<EditorMemoryRegion> EnumerateWritableRegions()
    {
        return OperatingSystem.IsWindows() ? ReadWindowsRegions() : ReadLinuxRegions();
    }

    private IEnumerable<EditorMemoryRegion> ReadWindowsRegions()
    {
        long address = 0;
        while (address < uint.MaxValue)
        {
            if (VirtualQueryEx(process.Handle, (nint)address, out var region, (nuint)Marshal.SizeOf<WindowsMemoryRegion>()) == 0)
                yield break;

            long length = checked((long)region.RegionSize);
            long end = region.BaseAddress + length;
            if (end <= address) yield break;

            // The original signature describes a managed editor object in a
            // committed, private, read/write heap allocation, rather than code.
            if (region.State == 0x1000 && region.Protect == 0x04 && region.Type == 0x20000)
                yield return new EditorMemoryRegion(region.BaseAddress, Math.Min(end, (long)uint.MaxValue + 1) - region.BaseAddress);

            address = end;
        }
    }

    private IEnumerable<EditorMemoryRegion> ReadLinuxRegions() =>
        ParseLinuxRegions(File.ReadLines($"/proc/{process.Id}/maps"));

    internal static IEnumerable<EditorMemoryRegion> ParseLinuxRegions(IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 2 || !fields[1].StartsWith("rw", StringComparison.Ordinal)) continue;

            string[] addresses = fields[0].Split('-');
            long start = long.Parse(addresses[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            long end = long.Parse(addresses[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            // Wine also maps host libraries above 4 GiB. Stable's managed editor
            // graph is addressed by 32-bit pointers, so those regions cannot match.
            if (start > uint.MaxValue) continue;
            yield return new EditorMemoryRegion((nint)start, Math.Min(end, (long)uint.MaxValue + 1) - start);
        }
    }

    public void Dispose() => process.Dispose();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(nint processHandle, nint address, [Out] byte[] buffer, nuint size, out nuint bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualQueryEx(nint processHandle, nint address, out WindowsMemoryRegion information, nuint size);

    [DllImport("libc", EntryPoint = "process_vm_readv", SetLastError = true)]
    private static extern nint ProcessVmRead(int processId, ref IoVector local, nuint localCount, ref IoVector remote, nuint remoteCount, nuint flags);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct IoVector(nint address, nuint length)
    {
        private readonly nint address = address;
        private readonly nuint length = length;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsMemoryRegion
    {
        internal nint BaseAddress;
        private nint allocationBase;
        private uint allocationProtect;
        internal nuint RegionSize;
        internal uint State;
        internal uint Protect;
        internal uint Type;
    }
}

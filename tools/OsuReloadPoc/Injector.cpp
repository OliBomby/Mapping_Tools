#include <windows.h>
#include <string>
#include <cstdio>

int wmain(int argc, wchar_t** argv)
{
    if (argc != 2) { fwprintf(stderr, L"Usage: Injector.exe <osu! process id>\n"); return 1; }
    wchar_t* end = nullptr;
    DWORD pid = wcstoul(argv[1], &end, 10);
    if (!pid || *end) return 1;
    HANDLE process = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION |
        PROCESS_VM_WRITE | PROCESS_VM_READ, FALSE, pid);
    if (!process) { fprintf(stderr, "OpenProcess failed: %lu\n", GetLastError()); return 2; }
    wchar_t executable[MAX_PATH];
    DWORD length = MAX_PATH;
    BOOL targetWow64 = FALSE, injectorWow64 = FALSE;
    if (!QueryFullProcessImageNameW(process, 0, executable, &length) ||
        _wcsicmp(wcsrchr(executable, L'\\') + 1, L"osu!.exe") != 0 ||
        !IsWow64Process(process, &targetWow64) || !IsWow64Process(GetCurrentProcess(), &injectorWow64) ||
        targetWow64 != injectorWow64)
    {
        fprintf(stderr, "Target must be osu!.exe with the same architecture as this injector.\n");
        CloseHandle(process);
        return 3;
    }
    wchar_t ownPath[MAX_PATH];
    GetModuleFileNameW(nullptr, ownPath, MAX_PATH);
    std::wstring dll(ownPath);
    dll.resize(dll.find_last_of(L"\\") + 1);
    dll += L"Bootstrap.dll";
    SIZE_T size = (dll.size() + 1) * sizeof(wchar_t);
    void* remote = VirtualAllocEx(process, nullptr, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    SIZE_T written = 0;
    HANDLE thread = nullptr;
    if (remote && WriteProcessMemory(process, remote, dll.c_str(), size, &written) && written == size)
    {
        auto loadLibrary = reinterpret_cast<LPTHREAD_START_ROUTINE>(
            GetProcAddress(GetModuleHandleW(L"kernel32.dll"), "LoadLibraryW"));
        thread = CreateRemoteThread(process, nullptr, 0, loadLibrary, remote, 0, nullptr);
    }
    DWORD loaded = 0;
    bool complete = thread && WaitForSingleObject(thread, 15000) == WAIT_OBJECT_0;
    if (complete) GetExitCodeThread(thread, &loaded);
    if (remote && (!thread || complete)) VirtualFreeEx(process, remote, 0, MEM_RELEASE);
    if (thread) CloseHandle(thread);
    CloseHandle(process);
    if (!loaded) { fprintf(stderr, "Bootstrap load failed or timed out: %lu\n", GetLastError()); return 4; }
    printf("Loaded reload bootstrap into osu! PID %lu (module 0x%08lx).\n", pid, loaded);
    return 0;
}

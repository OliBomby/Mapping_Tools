#include <windows.h>
#include <metahost.h>
#include <string>
#include <cstdio>
#pragma comment(lib, "mscoree.lib")

static HMODULE moduleHandle;

static DWORD WINAPI LoadBridge(void*)
{
    wchar_t path[MAX_PATH];
    GetModuleFileNameW(moduleHandle, path, MAX_PATH);
    std::wstring directory(path);
    directory.resize(directory.find_last_of(L"\\") + 1);
    FILE* log = nullptr;
    _wfopen_s(&log, (directory + L"bootstrap.log").c_str(), L"a");
    FILE* manifest = nullptr;
    wchar_t assemblyName[128] = {};
    _wfopen_s(&manifest, (directory + L"bridge-assembly.txt").c_str(), L"r");
    if (manifest)
    {
        fgetws(assemblyName, 128, manifest);
        fclose(manifest);
    }
    std::wstring bridgeAssembly(assemblyName);
    size_t newline = bridgeAssembly.find_first_of(L"\r\n");
    if (newline != std::wstring::npos) bridgeAssembly.resize(newline);
    if (bridgeAssembly.find(L"EditorBridge-") != 0 || bridgeAssembly.find_first_of(L"/\\") != std::wstring::npos)
    {
        if (log) { fprintf(log, "Invalid bridge assembly manifest.\n"); fclose(log); }
        FreeLibraryAndExitThread(moduleHandle, ERROR_INVALID_DATA);
    }
    ICLRMetaHost* meta = nullptr;
    ICLRRuntimeInfo* runtime = nullptr;
    ICLRRuntimeHost* host = nullptr;
    HRESULT result = CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, reinterpret_cast<void**>(&meta));
    if (SUCCEEDED(result)) result = meta->GetRuntime(L"v4.0.30319", IID_ICLRRuntimeInfo, reinterpret_cast<void**>(&runtime));
    BOOL started = FALSE;
    DWORD flags = 0;
    if (SUCCEEDED(result)) result = runtime->IsStarted(&started, &flags);
    if (SUCCEEDED(result) && !started) result = E_FAIL;
    if (SUCCEEDED(result)) result = runtime->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, reinterpret_cast<void**>(&host));
    DWORD managedResult = 0;
    if (SUCCEEDED(result))
        result = host->ExecuteInDefaultAppDomain((directory + bridgeAssembly).c_str(),
            L"OsuReloadPoc.Bridge", L"Start", directory.c_str(), &managedResult);
    if (log)
    {
        fprintf(log, "CLR bridge start: HRESULT=0x%08lx result=%lu\n", result, managedResult);
        fclose(log);
    }
    if (host) host->Release();
    if (runtime) runtime->Release();
    if (meta) meta->Release();
    FreeLibraryAndExitThread(moduleHandle, static_cast<DWORD>(result));
}

BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        moduleHandle = instance;
        DisableThreadLibraryCalls(instance);
        HANDLE thread = CreateThread(nullptr, 0, LoadBridge, nullptr, 0, nullptr);
        if (!thread) return FALSE;
        CloseHandle(thread);
    }
    return TRUE;
}

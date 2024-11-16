// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"
#include <iostream>
#include "memory.h"
#include "hid_dll.hpp"
#include "version_dll.hpp"
#include "plugin.hpp"

enum class DllType {
    Unknown,
    Version,
    Hid,
};

static DllType dllType = DllType::Unknown;
static HMODULE g_hModule = 0;

struct MonoAssemblyOpenRequest
{
    uint8_t raw[40];
};

typedef enum {
    MONO_IMAGE_OK,
    MONO_IMAGE_ERROR_ERRNO,
    MONO_IMAGE_MISSING_ASSEMBLYREF,
    MONO_IMAGE_IMAGE_INVALID
} MonoImageOpenStatus;

// MonoThread * mono_thread_internal_attach (MonoDomain *domain)
typedef void* (*mono_thread_internal_attach_t)(void* domain);
// MonoAssembly * mono_assembly_request_open (const char *filename, const MonoAssemblyOpenRequest *open_req, MonoImageOpenStatus *status)
typedef void* (*mono_assembly_request_open_t)(const char* filename, const MonoAssemblyOpenRequest* open_req, MonoImageOpenStatus* status);

/*
gint32 ves_icall_System_AppDomain_ExecuteAssembly (
    MonoAppDomainHandle ad, MonoReflectionAssemblyHandle refass, MonoArrayHandle args, MonoError *error)
struct _MonoAppDomain { // size: 32
    MonoMarshalByRefObject mbr; // 0
    MonoDomain *data; // 24
};
struct _MonoReflectionAssembly { // size: 32
    MonoObject object; // 0
    MonoAssembly *assembly; // 16
    MonoObject *evidence; // 24
};
*/
struct _MonoAppDomain { // size: 32
    uint8_t mbr[24]; // 0
    void* data; // 24
};
struct _MonoReflectionAssembly { // size: 32
    uint8_t object[16]; // 0
    void* assembly; // 16
    void* evidence; // 24
};
struct MonoError {
    uint8_t raw[104];
};
typedef int (*ves_icall_System_AppDomain_ExecuteAssembly_t)(_MonoAppDomain** ad, _MonoReflectionAssembly** refass, void** args, MonoError* error);

typedef void* (*writeDomainPtr_t)();

auto writeDomainPtr = (writeDomainPtr_t)0;

DWORD WINAPI MainThread(LPVOID dwModule);

void* domain = nullptr;

static void* WriteDomainPtrHook()
{
    if (writeDomainPtr)
    {
        domain = writeDomainPtr();
        wprintf_s(L"domain: 0x%p\n", domain);
        CreateThread(nullptr, 0, MainThread, g_hModule, 0, nullptr);
        //MainThread(0);
    }
    return domain;
}

DWORD WINAPI MainThread(LPVOID dwModule)
{
    loadPluginDlls();
    std::cout << "CSharpLoader wait for init." << std::endl;

    auto mono_thread_internal_attach = (mono_thread_internal_attach_t)signature(
        "40 57 48 83 EC 30 8B 15 ? ? ? ? 48 8B F9 65 48 8B 04 25 58 00 00 00 B9 A8 02 00 00 48 8B 04 D0 48 83 3C 01 00").GetPointer();
    if (mono_thread_internal_attach == nullptr) {
        std::cout << "mono_thread_internal_attach not found." << std::endl;
        return EXIT_FAILURE;
    }
    auto mono_assembly_request_open = (mono_assembly_request_open_t)signature(
        "40 55 41 56 41 57 48 8D AC 24 40 FF FF FF 48 81 EC C0 01 00 00 48 8B 05 ? ? ? ? 48 33 C4 48 89 85 90 00 00 00 48 89 4D 90").GetPointer();
    if (mono_assembly_request_open == nullptr) {
        std::cout << "mono_assembly_request_open not found." << std::endl;
        return EXIT_FAILURE;
    }
    auto ves_icall_System_AppDomain_ExecuteAssembly = (ves_icall_System_AppDomain_ExecuteAssembly_t)signature(
        "48 89 5C 24 08 55 56 57 41 56 41 57 48 83 EC 50 48 89 94 24 88 00 00 00").GetPointer();
    if (ves_icall_System_AppDomain_ExecuteAssembly == nullptr) {
        std::cout << "ves_icall_System_AppDomain_ExecuteAssembly not found." << std::endl;
        return EXIT_FAILURE;
    }

    if (domain == nullptr) {
        std::cout << "domain is null." << std::endl;
        return EXIT_FAILURE;
    }

    void* mono_thread = mono_thread_internal_attach(domain);
    if (mono_thread == nullptr) {
        std::cout << "mono_thread_internal_attach failed." << std::endl;
        return EXIT_FAILURE;
    }

    MonoAssemblyOpenRequest open_request{};
    MonoImageOpenStatus status = MonoImageOpenStatus::MONO_IMAGE_OK;

    wchar_t fullFilename[MAX_PATH]{};
    GetFullPathName(L"CSharpLoader\\CSharpManager.bin", MAX_PATH, fullFilename, nullptr);
    char fullFilenameA[MAX_PATH]{};
    // convert to utf-8 to support Chinese path
    WideCharToMultiByte(CP_UTF8, 0, fullFilename, MAX_PATH, fullFilenameA, MAX_PATH, NULL, NULL);
    void* assembly = mono_assembly_request_open(fullFilenameA, &open_request, &status);
    if (assembly == nullptr) {
        std::cout << "mono_assembly_request_open failed." << std::endl;
        return EXIT_FAILURE;
    }

    _MonoAppDomain ad{};
    ad.data = domain;
    _MonoReflectionAssembly refass{};
    refass.assembly = assembly;
    MonoError mono_error{};
    _MonoAppDomain* p_ad = &ad;
    _MonoReflectionAssembly* p_refass = &refass;
    void* args = nullptr;
    int res = ves_icall_System_AppDomain_ExecuteAssembly(&p_ad, &p_refass, &args, &mono_error);
    if (res != 0) {
        std::cout << "ves_icall_System_AppDomain_ExecuteAssembly failed: " << res << std::endl;
        return EXIT_FAILURE;
    }
    std::cout << "CSharpLoader init success." << std::endl;
    return EXIT_SUCCESS;
}

namespace Memory
{
    bool CallFunction32(void* src, void* dst, int len)
    {
        if (!src || !dst || len < 5)
        {
            return false;
        }
        DWORD curProtection;
        VirtualProtect(src, len, PAGE_EXECUTE_READWRITE, &curProtection);

        memset(src, 0x90, len);

        uintptr_t relativeAddress = ((uintptr_t)dst - (uintptr_t)src) - 5;

        *(BYTE*)src = 0xE8;
        *(uint32_t*)((uintptr_t)src + 1) = relativeAddress;

        DWORD temp;
        VirtualProtect(src, len, curProtection, &temp);

        return true;
    }

    void* DetourFunction64(void* pSource, void* pDestination, DWORD dwLen)
    {
        constexpr DWORD MinLen = 14;

        if (dwLen < MinLen)
        {
            return nullptr;
        }

        BYTE stub[] = {
        0xFF, 0x25, 0x00, 0x00, 0x00, 0x00, // jmp qword ptr [$+6]
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 // ptr
        };

        DWORD dwOld = 0;
        VirtualProtect(pSource, dwLen, PAGE_EXECUTE_READWRITE, &dwOld);

        // orig
        memcpy(stub + 6, &pDestination, 8);
        memcpy(pSource, stub, sizeof(stub));

        for (DWORD i = MinLen; i < dwLen; i++)
        {
            *(BYTE*)((DWORD_PTR)pSource + i) = 0x90;
        }

        VirtualProtect(pSource, dwLen, dwOld, &dwOld);
        return pDestination;
    }
};

static void Make32to64Call(uintptr_t source_target, uintptr_t second_jmp, uintptr_t target_jmp, uint32_t source_size, const wchar_t* source_name = L"", const wchar_t* second_jmp_name = L"", const wchar_t* target_jmp_name = L"")
{
    if (!source_target || !second_jmp || !target_jmp || source_size < 5)
    {
        wprintf_s(L"Canoot create jump '%s' from '%s' to '%s'\n", source_name, second_jmp_name, target_jmp_name);
        wprintf_s(L"source_target: 0x%llx\n", source_target);
        wprintf_s(L"source_size: %u bytes\n", source_size);
        wprintf_s(L"second_jmp: 0x%llx\n", second_jmp);
        wprintf_s(L"target_jmp: 0x%llx\n", target_jmp);
        return;
    }
    Memory::CallFunction32((void*)source_target, (void*)second_jmp, source_size);
    wprintf_s(L"Created jump %s (0x%016llx) to %s (0x%016llx)\n", source_name, (uintptr_t)source_target, second_jmp_name, (uintptr_t)second_jmp);
    Memory::DetourFunction64((void*)second_jmp, (void*)target_jmp, 14);
    wprintf_s(L"Created jump %s (0x%016llx) to %s (0x%016llx)\n", second_jmp_name, (uintptr_t)second_jmp, target_jmp_name, (uintptr_t)target_jmp);
}

static void enableJitPatch()
{
    // if (enableJit) 
    {
        uint64_t memory_fuction_ptr = signature("83 3D ? ? ? ? 00 0F 84 ? ? ? ? C7 84 24 ? ? 00 00 01 00 00 00").GetPointer();
        if (memory_fuction_ptr == 0) {
            std::cout << "memory function signature not found." << std::endl;
        }
        else {
            DWORD old_protect;
            if (VirtualProtect((void*)(memory_fuction_ptr + 7), 2, PAGE_EXECUTE_READWRITE, &old_protect)) {
                *(uint16_t*)(memory_fuction_ptr + 7) = 0xE990;  // nop; jmp
                VirtualProtect((void*)(memory_fuction_ptr + 7), 2, old_protect, &old_protect);
                uint64_t mono_mode_ptr = signature("48 8D 0D ? ? ? ? E8 ? ? ? ? 89 44 24 ? 83 7C 24 ? 00").GetPointer();
                if (mono_mode_ptr == 0) {
                    std::cout << "mono_mode signature not found." << std::endl;
                }
                else {
                    if (VirtualProtect((void*)(mono_mode_ptr + 7), 5, PAGE_EXECUTE_READWRITE, &old_protect)) {
                        *(uint8_t*)(mono_mode_ptr + 7) = 0xB8;
                        *(uint32_t*)(mono_mode_ptr + 8) = 1;
                        VirtualProtect((void*)(mono_mode_ptr + 7), 5, old_protect, &old_protect);
                    }
                }
            }
        }
    }
}

static void StartupPatch()
{
    const char* configFile = "./CSharpLoader/b1cs.ini";
    BOOL enableConsole{}, enableJit{};
    enableConsole = GetPrivateProfileIntA("Settings", "Console", 0, configFile);
    enableJit = GetPrivateProfileIntA("Settings", "EnableJit", 1, configFile);
    if (enableConsole)
    {
        AllocConsole();
        FILE* fDummy;
        freopen_s(&fDummy, "CONIN$", "r", stdin);
        freopen_s(&fDummy, "CONOUT$", "w", stdout);
        freopen_s(&fDummy, "CONOUT$", "w", stderr);
        std::cout << "CSharpLoader enableConsole: " << enableConsole << std::endl;
    }
    // required for harmony to work?
    // without it patches applied don't seem to work
    if (enableJit)
    {
        std::cout << "CSharpLoader enableJit: " << enableJit << std::endl;
        enableJitPatch();
    }
    auto WriteMonoDomainPtrFuncAddr = signature("E8 ? ? ? ? 8B 15 ? ? ? ? 65 48 8B 0C 25 58 00 00 00 41 B8 98 02 00 00 48 89 05 ? ? ? ? 48 89 05 ? ? ? ?");
    const uintptr_t WriteMonoDomainPtrFuncAddr2 = WriteMonoDomainPtrFuncAddr.GetPointer();
    const uintptr_t Int3Jmp = signature("CC CC CC CC CC CC CC CC CC CC CC CC CC CC").GetPointer();
    const uintptr_t WriteDomainHook = (uintptr_t)&WriteDomainPtrHook;
    wprintf_s(L"WriteMonoDomainPtrFuncAddr: 0x%llx\n", WriteMonoDomainPtrFuncAddr.GetPointer());
    if (WriteMonoDomainPtrFuncAddr2 && Int3Jmp && WriteDomainHook)
    {
        writeDomainPtr = (writeDomainPtr_t)WriteMonoDomainPtrFuncAddr.instruction(1).add(5).GetPointer();
        if (writeDomainPtr)
        {
            Make32to64Call(WriteMonoDomainPtrFuncAddr2, Int3Jmp, WriteDomainHook, 5);
        }
    }
}

void init_dll(HMODULE hModule) {
    DisableThreadLibraryCalls(hModule);
    wchar_t moduleFullpathFilename[MAX_PATH + 1]{};
    GetModuleFileNameW(hModule, moduleFullpathFilename, static_cast<UINT>(std::size(moduleFullpathFilename)));
    wchar_t fname[_MAX_FNAME + 1]{};
    {
        wchar_t drive[_MAX_DRIVE + 1]{};
        wchar_t dir[_MAX_DIR + 1]{};
        wchar_t ext[_MAX_EXT + 1]{};
        _wsplitpath_s(moduleFullpathFilename, drive, dir, fname, ext);
    }
    if (_wcsicmp(fname, L"version") == 0) {
        dllType = DllType::Version;
        init_version_dll();
    } else if (_wcsicmp(fname, L"hid") == 0) {
        dllType = DllType::Hid;
        init_hid_dll();
    }
    StartupPatch();
}

void deinit_dll() {
    if (dllType == DllType::Version) {
        deinit_version_dll();
    } else if (dllType == DllType::Hid) {
        deinit_hid_dll();
    }
    // unloadPluginDlls();
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    static std::once_flag initFlag;
    static std::once_flag cleanupFlag;
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        g_hModule = hModule;
        std::call_once(initFlag, [&]() { init_dll(hModule); });
        break;
    case DLL_PROCESS_DETACH:
        std::call_once(cleanupFlag, [&]() { deinit_dll(); });
        break;
    }
    return TRUE;
}


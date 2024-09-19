// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"
#include <iostream>
#include "memory.h"
#include "hid_dll.hpp"

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

DWORD WINAPI MainThread(LPVOID dwModule)
{
    UINT enableConsole = GetPrivateProfileIntA("Settings", "Console", 0, "./CSharpLoader/b1cs.ini");
    if (enableConsole == 1) {
        AllocConsole();
        FILE* fDummy;
        freopen_s(&fDummy, "CONIN$", "r", stdin);
        freopen_s(&fDummy, "CONOUT$", "w", stdout);
        freopen_s(&fDummy, "CONOUT$", "w", stderr);
    }

    signature domain_s("F0 FF 88 B0 00 00 00 48 8B 05 ? ? ? ? 48 3B D8 49 0F 44 C4");
    if (domain_s.GetPointer() == 0) {
        std::cout << "domainPtr pattern not found." << std::endl;
        return EXIT_FAILURE;
    }
    void **domainPtr = (void**)domain_s.instruction(10).add(14).GetPointer();
    if (domainPtr == nullptr) {
        std::cout << "domainPtr not found." << std::endl;
        return EXIT_FAILURE;
    }
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
    Sleep(30000); // 30s
    void* domain = nullptr;
    for (size_t i = 0; i < 12; i++) {
        domain = *domainPtr;
        if (domain != nullptr) {
            break;
        }
        Sleep(10000); // 10s
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

    void* assembly = mono_assembly_request_open("CSharpLoader\\CSharpManager.bin", &open_request, &status);
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
        std::call_once(initFlag, [&]() { init_hid_dll(); });
        DisableThreadLibraryCalls(hModule);
        CreateThread(nullptr, 0, MainThread, hModule, 0, nullptr);
        break;
    case DLL_PROCESS_DETACH:
        std::call_once(cleanupFlag, [&]() { deinit_hid_dll(); });
        break;
    }
    return TRUE;
}


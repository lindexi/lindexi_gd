#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include "ProcessHelper.h"

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved){
	if (fdwReason == DLL_PROCESS_ATTACH){
		DisableThreadLibraryCalls(hinstDLL);
	}
	return TRUE;
}

bool __stdcall GetProcCmdLine(DWORD dwProcId, WCHAR* buf, DWORD dwSizeBuf){
	LPWSTR sz = 0;
	if (CProcessHelper::GetProcessCommandLine(dwProcId, sz) && sz){
		wcscpy_s(buf, dwSizeBuf, sz);
		delete sz;
		return true;
	}
	return false;
}

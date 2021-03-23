#ifndef __PROCESS_HELPER_H__
#define __PROCESS_HELPER_H__
#pragma once

#include <Windows.h>
#include <winternl.h>

class CProcessHelper
{
public:
	static bool GetProcessCommandLine(DWORD dwProcId, LPWSTR& szCmdLine);

private:
	static PVOID GetPebAddress(HANDLE ProcessHandle);

	typedef NTSTATUS(NTAPI* _NtQueryInformationProcess)(
					HANDLE ProcessHandle,
					DWORD ProcessInformationClass,
					PVOID ProcessInformation,
					DWORD ProcessInformationLength,
					PDWORD ReturnLength);
};

#endif /** !__PROCESS_HELPER_H__ **/
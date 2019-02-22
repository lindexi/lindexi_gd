#include "ProcessHelper.h"


PVOID CProcessHelper::GetPebAddress(HANDLE ProcessHandle){
	_NtQueryInformationProcess NtQueryInformationProcess = (_NtQueryInformationProcess)GetProcAddress(GetModuleHandleA("ntdll.dll"), "NtQueryInformationProcess");
	PROCESS_BASIC_INFORMATION pbi = { 0 };
	NtQueryInformationProcess(ProcessHandle, 0, &pbi, sizeof(pbi), NULL);
	return pbi.PebBaseAddress;
}

bool CProcessHelper::GetProcessCommandLine(DWORD dwProcId, LPWSTR& szCmdLine){
	szCmdLine = NULL;

	HANDLE processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | /* required for NtQueryInformationProcess */
		PROCESS_VM_READ, /* required for ReadProcessMemory */
		FALSE, dwProcId);
	if (!processHandle){ return false; }

	PVOID pebAddress = GetPebAddress(processHandle);

	PVOID rtlUserProcParamsAddress;
	if (!ReadProcessMemory(processHandle, &(((_PEB*)pebAddress)->ProcessParameters), &rtlUserProcParamsAddress, sizeof(PVOID), NULL)){
		CloseHandle(processHandle);
		return false;
	}

	UNICODE_STRING commandLine;
	if (!ReadProcessMemory(processHandle, &(((_RTL_USER_PROCESS_PARAMETERS*)rtlUserProcParamsAddress)->CommandLine), &commandLine, sizeof(commandLine), NULL)){
		CloseHandle(processHandle);
		return false;
	}

	szCmdLine = new WCHAR[commandLine.MaximumLength];
	memset(szCmdLine, 0, commandLine.MaximumLength);

	if (!ReadProcessMemory(processHandle, commandLine.Buffer, szCmdLine, commandLine.Length, NULL)){
		delete szCmdLine;
		CloseHandle(processHandle);
		return false;
	}

	CloseHandle(processHandle);
	return true;
}

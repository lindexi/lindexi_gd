#include <stdio.h>
#include "mscoree.h"	// Generated from mscoree.idl


// The host must be able to find CoreCLR.dll to start the runtime.
// This string is used as a common, known location for a centrally installed CoreCLR.dll on Windows.
// If your customers will have the CoreCLR.dll installed elsewhere, this will, of course, need modified.
// Some hosts will carry the runtime and Framework with them locally so that necessary files like CoreCLR.dll
// are easy to find and known to be good versions. Other hosts may expect that the apps they run will include
// CoreCLR.dll next to them. Still others may rely on an environment variable being set to indicate where
// the library can be found. In the end, every host will have its own heuristics for finding the core runtime
// library, but it must always be findable in order to start the CLR.
static const wchar_t* coreCLRInstallDirectory = L"%programfiles%\\dotnet\\shared\\Microsoft.NETCore.App\\5.0.0";

// Main clr library to load
// Note that on Linux and Mac platforms, this library will
// be called libcoreclr.so or libcoreclr.dylib, respectively
static const wchar_t* coreCLRDll = L"coreclr.dll";

// Helper method to check for CoreCLR.dll in a given path and load it, if possible
HMODULE LoadCoreCLR(const wchar_t* directoryPath);

// Function pointer type to be used if this sample is modified to use CreateDelegate to
// call into a static managed method (with signature void (string[])). This would be
// an alternative to using the ICLRRuntimeHost4::ExecuteAssembly helper function which
// loads and executes a managed assembly's entry point directly.
typedef void (STDMETHODCALLTYPE MainMethodFp)(LPWSTR* args);

// One large main method to keep this sample streamlined.
//
// This function demonstrates how to start the .NET Core runtime,
// create an AppDomain, and execute managed code.
//
// It is meant as an educational sample, so not all error paths are checked,
// cross-platform functionality is not yet implemented, and some design
// decisions have been made to emphasize readability over efficiency.
int wmain(int argc, wchar_t* argv[])
{
	//
    // STEP 1: Get the app from app.txt
    //
	
	// 从 App.txt 配置文件里面读取应用和框架的路径
	TCHAR exeFullPath[MAX_PATH];
	memset(exeFullPath, 0, MAX_PATH);
	// 获取当前路径
	GetModuleFileName(nullptr, exeFullPath, MAX_PATH);
	// 获取当前 Exe 所在文件夹
	char drive[4];
	char currentDirectory[MAX_PATH];
	char fileName[16];
	char fileExtension[8];
	_splitpath_s(exeFullPath, drive, currentDirectory, fileName, fileExtension);
	printf("Application path=%s%s\n", drive, currentDirectory);

	// 配置文件路径
	const char* configFileName = "app";
	const char* configFileExtension = ".txt";
	// 拼接配置文件路径
	TCHAR configFileFullPath[MAX_PATH];
	memset(configFileFullPath, 0, MAX_PATH);
	_makepath_s(configFileFullPath, drive, currentDirectory, configFileName, configFileExtension);
	printf("Application config path=%s\n", configFileFullPath);

	// 读取文件内容
	// 约定第一行就是应用路径，第二行就是 CLR 框架路径
	FILE* file;
	fopen_s(&file, configFileFullPath, "r");
	// 应用配置文件路径，这也许是相对路径
	char applicationConfigPath[MAX_PATH];
	memset(applicationConfigPath, 0, MAX_PATH);
	fgets(applicationConfigPath, MAX_PATH, file);
	// 读取文件的时候，会将 \n 读取，因此需要删除最后一个字符
	size_t stringLength = strlen(applicationConfigPath);

	applicationConfigPath[stringLength - 1] = 0;
	stringLength--;
	// 应用的绝对路径
	char applicationFullPath[MAX_PATH];
	memset(applicationFullPath, 0, MAX_PATH);
	// 判断是否相对路径，暂时没有找到啥库函数，只通过判断第二个字符是否冒号
	if (stringLength < 2 || applicationConfigPath[1] != ':')
	{
		if (strstr(applicationConfigPath, "%") != nullptr)
		{
			ExpandEnvironmentStrings(applicationConfigPath, applicationFullPath, MAX_PATH);
		}
		else
		{
			// 拼接回绝对路径
			_makepath_s(applicationFullPath, drive, currentDirectory, applicationConfigPath, nullptr);
		}
	}
	else
	{
		strcpy_s(applicationFullPath, applicationConfigPath);
	}
	printf("dotnet application path=%s\n", applicationFullPath);

	// 读取框架 CLR 所在文件夹
	char frameworkConfigPath[MAX_PATH];
	memset(frameworkConfigPath, 0, MAX_PATH);
	fgets(frameworkConfigPath, MAX_PATH, file);
	// 没啥需要读的了，可以释放文件了
	if (file != nullptr)
	{
		fclose(file);
	}
	stringLength = strlen(frameworkConfigPath);
	// 框架绝对路径
	char frameworkFullPath[MAX_PATH];
	memset(frameworkFullPath, 0, MAX_PATH);
	// 判断是否相对路径，暂时没有找到啥库函数，只通过判断第二个字符是否冒号
	if (stringLength < 2 || frameworkConfigPath[1] != ':')
	{
		if(strstr(frameworkConfigPath,"%"))
		{
			ExpandEnvironmentStrings(frameworkConfigPath, frameworkFullPath, MAX_PATH);
		}
		else
		{
			// 拼接回绝对路径
			_makepath_s(frameworkFullPath, drive, currentDirectory, frameworkConfigPath, nullptr);
		}
	}
	else
	{
		strcpy_s(frameworkFullPath, frameworkConfigPath);
	}
	printf("dotnet framework path=%s\n", frameworkFullPath);

	wchar_t applicationFullPathW[MAX_PATH];
	MultiByteToWideChar(CP_ACP, 0, applicationFullPath, strlen(applicationFullPath) + 1, applicationFullPathW, MAX_PATH);
	wchar_t frameworkFullPathW[MAX_PATH];
	MultiByteToWideChar(CP_ACP, 0, frameworkFullPath, strlen(frameworkFullPath) + 1, frameworkFullPathW, MAX_PATH);

	wchar_t* application = applicationFullPathW;
	const wchar_t* coreCLRDirectory = frameworkFullPathW;

	// <Snippet1>
	// The managed application to run should be the first command-line parameter.
	// Subsequent command line parameters will be passed to the managed app later in this host.
	wchar_t targetApp[MAX_PATH];
	GetFullPathNameW(application, MAX_PATH, targetApp, nullptr);
	// </Snippet1>

	// Also note the directory the target app is in, as it will be referenced later.
	// The directory is determined by simply truncating the target app's full path
	// at the last path delimiter (\)
	wchar_t targetAppPath[MAX_PATH];
	wcscpy_s(targetAppPath, targetApp);

	// Walk the string backwards looking for '\'
	size_t i = wcsnlen(targetAppPath, MAX_PATH);
	while (i > 0 && targetAppPath[i - 1] != L'\\') i--;

	// Replace the first '\' found (if any) with \0
	if (i > 0)
	{
		targetAppPath[i - 1] = L'\0';
	}

	//
	// STEP 2: Find and load CoreCLR.dll
	//
	HMODULE coreCLRModule;

	// 先加载环境变量 CORE_ROOT 的路径
	// Look in %CORE_ROOT%
	wchar_t coreRoot[MAX_PATH];
	size_t outSize;
	_wgetenv_s(&outSize, coreRoot, MAX_PATH, L"CORE_ROOT");
	// 如果找不到路径，那么再加自己的私有的路径
	coreCLRModule = LoadCoreCLR(coreRoot);

	if (!coreCLRModule)
	{
		// 加载自己的私有的路径

		wcscpy_s(coreRoot, MAX_PATH, coreCLRDirectory);
		coreCLRModule = LoadCoreCLR(coreRoot);

		// 如果使用 %programfiles% 环境变量的路径写法，那么请使用 ExpandEnvironmentStringsW 的方式，如下面代码
		//::ExpandEnvironmentStringsW(coreCLRDirectory, coreRoot, MAX_PATH);
		//coreCLRModule = LoadCoreCLR(coreRoot);
	}

	// If CoreCLR.dll wasn't in %CORE_ROOT%, look next to the target app
	if (!coreCLRModule)
	{
		wcscpy_s(coreRoot, MAX_PATH, targetAppPath);
		coreCLRModule = LoadCoreCLR(coreRoot);
	}

	// If CoreCLR.dll wasn't in %CORE_ROOT% or next to the app,
	// look in the common 1.1.0 install directory
	if (!coreCLRModule)
	{
		::ExpandEnvironmentStringsW(coreCLRInstallDirectory, coreRoot, MAX_PATH);
		coreCLRModule = LoadCoreCLR(coreRoot);
	}

	if (!coreCLRModule)
	{
		printf("ERROR - CoreCLR.dll could not be found");
		return -1;
	}
	else
	{
		wprintf(L"CoreCLR loaded from %s\n", coreRoot);
	}



	//
	// STEP 3: Get ICLRRuntimeHost4 instance
	//

	// <Snippet3>
	ICLRRuntimeHost4* runtimeHost;

	FnGetCLRRuntimeHost pfnGetCLRRuntimeHost =
		(FnGetCLRRuntimeHost)::GetProcAddress(coreCLRModule, "GetCLRRuntimeHost");

	if (!pfnGetCLRRuntimeHost)
	{
		printf("ERROR - GetCLRRuntimeHost not found");
		return -1;
	}

	// Get the hosting interface
	HRESULT hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost4, (IUnknown**)&runtimeHost);
	// </Snippet3>

	if (FAILED(hr))
	{
		printf("ERROR - Failed to get ICLRRuntimeHost4 instance.\nError code:%x\n", hr);
		return -1;
	}

	//
	// STEP 4: Set desired startup flags and start the CLR
	//

	// <Snippet4>
	hr = runtimeHost->SetStartupFlags(
		// These startup flags control runtime-wide behaviors.
		// A complete list of STARTUP_FLAGS can be found in mscoree.h,
		// but some of the more common ones are listed below.
		static_cast<STARTUP_FLAGS>(
			// STARTUP_FLAGS::STARTUP_SERVER_GC |								// Use server GC
			// STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN |		// Maximize domain-neutral loading
			// STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST |	// Domain-neutral loading for strongly-named assemblies
			STARTUP_FLAGS::STARTUP_CONCURRENT_GC |						// Use concurrent GC
			STARTUP_FLAGS::STARTUP_SINGLE_APPDOMAIN |					// All code executes in the default AppDomain
																		// (required to use the runtimeHost->ExecuteAssembly helper function)
			STARTUP_FLAGS::STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN	// Prevents domain-neutral loading
			)
	);
	// </Snippet4>

	if (FAILED(hr))
	{
		printf("ERROR - Failed to set startup flags.\nError code:%x\n", hr);
		return -1;
	}

	// Starting the runtime will initialize the JIT, GC, loader, etc.
	hr = runtimeHost->Start();
	if (FAILED(hr))
	{
		printf("ERROR - Failed to start the runtime.\nError code:%x\n", hr);
		return -1;
	}
	else
	{
		printf("Runtime started\n");
	}



	//
	// STEP 5: Prepare properties for the AppDomain we will create
	//

	// Flags
	// <Snippet5>
	int appDomainFlags =
		// APPDOMAIN_FORCE_TRIVIAL_WAIT_OPERATIONS |		// Do not pump messages during wait
		// APPDOMAIN_SECURITY_SANDBOXED |					// Causes assemblies not from the TPA list to be loaded as partially trusted
		APPDOMAIN_ENABLE_PLATFORM_SPECIFIC_APPS |			// Enable platform-specific assemblies to run
		APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP |	// Allow PInvoking from non-TPA assemblies
		APPDOMAIN_DISABLE_TRANSPARENCY_ENFORCEMENT;			// Entirely disables transparency checks
	// </Snippet5>

	// <Snippet6>
	// TRUSTED_PLATFORM_ASSEMBLIES
	// "Trusted Platform Assemblies" are prioritized by the loader and always loaded with full trust.
	// A common pattern is to include any assemblies next to CoreCLR.dll as platform assemblies.
	// More sophisticated hosts may also include their own Framework extensions (such as AppDomain managers)
	// in this list.
	size_t tpaSize = 100 * MAX_PATH; // Starting size for our TPA (Trusted Platform Assemblies) list
	wchar_t* trustedPlatformAssemblies = new wchar_t[tpaSize];
	trustedPlatformAssemblies[0] = L'\0';

	// Extensions to probe for when finding TPA list files
	const wchar_t* tpaExtensions[] = 
	{
		L"*.dll",
		L"*.exe",
		L"*.winmd"
	};

	// Probe next to CoreCLR.dll for any files matching the extensions from tpaExtensions and
	// add them to the TPA list. In a real host, this would likely be extracted into a separate function
	// and perhaps also run on other directories of interest.
	for (int i = 0; i < _countof(tpaExtensions); i++)
	{
		// Construct the file name search pattern
		wchar_t searchPath[MAX_PATH];
		wcscpy_s(searchPath, MAX_PATH, coreRoot);
		wcscat_s(searchPath, MAX_PATH, L"\\");
		wcscat_s(searchPath, MAX_PATH, tpaExtensions[i]);

		// Find files matching the search pattern
		WIN32_FIND_DATAW findData;
		HANDLE fileHandle = FindFirstFileW(searchPath, &findData);

		if (fileHandle != INVALID_HANDLE_VALUE)
		{
			do
			{
				// Construct the full path of the trusted assembly
				wchar_t pathToAdd[MAX_PATH];
				wcscpy_s(pathToAdd, MAX_PATH, coreRoot);
				wcscat_s(pathToAdd, MAX_PATH, L"\\");
				wcscat_s(pathToAdd, MAX_PATH, findData.cFileName);

				// Check to see if TPA list needs expanded
				if (wcsnlen(pathToAdd, MAX_PATH) + (3) + wcsnlen(trustedPlatformAssemblies, tpaSize) >= tpaSize)
				{
					// Expand, if needed
					tpaSize *= 2;
					wchar_t* newTPAList = new wchar_t[tpaSize];
					wcscpy_s(newTPAList, tpaSize, trustedPlatformAssemblies);
					trustedPlatformAssemblies = newTPAList;
				}

				// Add the assembly to the list and delimited with a semi-colon
				wcscat_s(trustedPlatformAssemblies, tpaSize, pathToAdd);
				wcscat_s(trustedPlatformAssemblies, tpaSize, L";");

				// Note that the CLR does not guarantee which assembly will be loaded if an assembly
				// is in the TPA list multiple times (perhaps from different paths or perhaps with different NI/NI.dll
				// extensions. Therefore, a real host should probably add items to the list in priority order and only
				// add a file if it's not already present on the list.
				//
				// For this simple sample, though, and because we're only loading TPA assemblies from a single path,
				// we can ignore that complication.
			}             while (FindNextFileW(fileHandle, &findData));
			FindClose(fileHandle);
		}
	}


	// APP_PATHS
	// App paths are directories to probe in for assemblies which are not one of the well-known Framework assemblies
	// included in the TPA list.
	//
	// For this simple sample, we just include the directory the target application is in.
	// More complex hosts may want to also check the current working directory or other
	// locations known to contain application assets.
	wchar_t appPaths[MAX_PATH * 50];

	// Just use the targetApp provided by the user and remove the file name
	wcscpy_s(appPaths, targetAppPath);


	// APP_NI_PATHS
	// App (NI) paths are the paths that will be probed for native images not found on the TPA list.
	// It will typically be similar to the app paths.
	// For this sample, we probe next to the app and in a hypothetical directory of the same name with 'NI' suffixed to the end.
	wchar_t appNiPaths[MAX_PATH * 50];
	wcscpy_s(appNiPaths, targetAppPath);
	wcscat_s(appNiPaths, MAX_PATH * 50, L";");
	wcscat_s(appNiPaths, MAX_PATH * 50, targetAppPath);
	wcscat_s(appNiPaths, MAX_PATH * 50, L"NI");


	// NATIVE_DLL_SEARCH_DIRECTORIES
	// Native dll search directories are paths that the runtime will probe for native DLLs called via PInvoke
	wchar_t nativeDllSearchDirectories[MAX_PATH * 50];
	wcscpy_s(nativeDllSearchDirectories, appPaths);
	wcscat_s(nativeDllSearchDirectories, MAX_PATH * 50, L";");
	wcscat_s(nativeDllSearchDirectories, MAX_PATH * 50, coreRoot);


	// PLATFORM_RESOURCE_ROOTS
	// Platform resource roots are paths to probe in for resource assemblies (in culture-specific sub-directories)
	wchar_t platformResourceRoots[MAX_PATH * 50];
	wcscpy_s(platformResourceRoots, appPaths);
	// </Snippet6>


	//
	// STEP 6: Create the AppDomain
	//

	// <Snippet7>
	DWORD domainId;

	// Setup key/value pairs for AppDomain  properties
	const wchar_t* propertyKeys[] = 
	{
		L"TRUSTED_PLATFORM_ASSEMBLIES",
		L"APP_PATHS",
		L"APP_NI_PATHS",
		L"NATIVE_DLL_SEARCH_DIRECTORIES",
		L"PLATFORM_RESOURCE_ROOTS"
	};

	// Property values which were constructed in step 5
	const wchar_t* propertyValues[] = 
	{
		trustedPlatformAssemblies,
		appPaths,
		appNiPaths,
		nativeDllSearchDirectories,
		platformResourceRoots
	};

	// Create the AppDomain
	hr = runtimeHost->CreateAppDomainWithManager(
		L"Sample Host AppDomain",		// Friendly AD name
		appDomainFlags,
		NULL,							// Optional AppDomain manager assembly name
		NULL,							// Optional AppDomain manager type (including namespace)
		sizeof(propertyKeys) / sizeof(wchar_t*),
		propertyKeys,
		propertyValues,
		&domainId);
	// </Snippet7>

	if (FAILED(hr))
	{
		printf("ERROR - Failed to create AppDomain.\nError code:%x\n", hr);
		return -1;
	}
	else
	{
		printf("AppDomain %d created\n\n", domainId);
	}



	//
	// STEP 7: Run managed code
	//

	// ExecuteAssembly will load a managed assembly and execute its entry point.
	wprintf(L"Executing %s...\n\n", targetApp);

	// <Snippet8>
	DWORD exitCode = -1;
	hr = runtimeHost->ExecuteAssembly(domainId, targetApp, argc - 1, (LPCWSTR*)(/*argc > 1 ? &argv[1] :*/ NULL), &exitCode);
	// </Snippet8>

	if (FAILED(hr))
	{
		wprintf(L"ERROR - Failed to execute %s.\nError code:%x\n", targetApp, hr);
		return -1;
	}

	// Alternatively, to start managed code execution with a method other than an assembly's entrypoint,
	// runtimeHost->CreateDelegate can be used to create a pointer to an arbitrary static managed method
	// which can then be executed directly or via runtimeHost->ExecuteInAppDomain.
	//
	//  void *pfnDelegate = NULL;
	//  hr = runtimeHost->CreateDelegate(
	//	  domainId,
	//	  L"HW, Version=1.0.0.0, Culture=neutral",	// Target managed assembly name (https://docs.microsoft.com/dotnet/framework/app-domains/assembly-names)
	//	  L"ConsoleApplication.Program",				// Target managed type
	//	  L"Main",									// Target entry point (static method)
	//	  (INT_PTR*)&pfnDelegate);
	//  if (FAILED(hr))
	//  {
	//	  printf("ERROR - Failed to create delegate.\nError code:%x\n", hr);
	//	  return -1;
	//  }
	//  ((MainMethodFp*)pfnDelegate)(NULL);



	//
	// STEP 8: Clean up
	//

	// <Snippet9>
	runtimeHost->UnloadAppDomain(domainId, true /* Wait until unload complete */);
	runtimeHost->Stop();
	runtimeHost->Release();
	// </Snippet9>

	printf("\nDone\n");

	return exitCode;
}


// Helper methods

// Check for CoreCLR.dll in a given path and load it, if possible
HMODULE LoadCoreCLR(const wchar_t* directoryPath)
{
	wchar_t coreDllPath[MAX_PATH];
	wcscpy_s(coreDllPath, MAX_PATH, directoryPath);
	wcscat_s(coreDllPath, MAX_PATH, L"\\");
	wcscat_s(coreDllPath, MAX_PATH, coreCLRDll);

	// <Snippet2>
	HMODULE ret = LoadLibraryExW(coreDllPath, NULL, 0);
	// </Snippet2>

	if (!ret)
	{
		// This logging is likely too verbose for many scenarios, but is useful
		// when getting started with the hosting APIs.
		DWORD errorCode = GetLastError();
		wprintf(L"CoreCLR not loaded from %s. LoadLibrary error code: %d\n", coreDllPath, errorCode);
	}

	return ret;
}

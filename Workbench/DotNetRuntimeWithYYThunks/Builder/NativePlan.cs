using System.Text;

namespace Builder;

internal sealed record NativePlan(string Directory, string Injection, Command[] Commands, string[] Blockers, string[] OfficialScripts);

internal static class NativePlanning
{
    internal static string? FindTool(string name) => (Environment.GetEnvironmentVariable("PATH") ?? "")
        .Split(Path.PathSeparator).Select(p => Path.Combine(p.Trim('"'), name)).FirstOrDefault(File.Exists);

    internal static NativePlan Create(Options o, string directory)
    {
        string injection = Path.Combine(directory, "YYThunks.cmake");
        var blockers = new List<string>();
        if (!OperatingSystem.IsWindows()) blockers.Add(Text.Get("Windows"));
        foreach (string file in new[] { "eng/build.ps1", "src/coreclr/runtime.proj", "src/native/libs/build-native.proj",
                     "src/native/corehost/corehost.proj", "eng/native/init-vs-env.cmd" })
            if (!File.Exists(Path.Combine(o.Runtime, file))) blockers.Add(Text.Missing(Path.Combine(o.Runtime, file)));
        if (o.Command != "baseline")
        {
            if (!File.Exists(o.Obj)) blockers.Add(string.Format(Text.Get("ObjectMissing"), o.Obj));
            else
            {
                try
                {
                    BinaryInspection.ValidateObject(o.Obj, o.Arch);
                }
                catch (InvalidDataException ex) { blockers.Add(ex.Message); }
                catch (EndOfStreamException) { blockers.Add(Text.Get("Object")); }
            }
        }
        string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(powershell)) powershell = FindTool("powershell.exe") ?? powershell;
        if (!File.Exists(powershell)) blockers.Add(Text.Missing(powershell));
        string Literal(string value) => "'" + value.Replace("'", "''") + "'";
        var commands = new List<Command>();
        var scripts = new List<string>();
        foreach (string subset in o.FullRuntime
                     ? new[] { "clr.runtime+clr.corelib+libs.native+libs.sfx+host.native+host.tools" }
                     : new[] { "clr.native", "libs.native", "host.native" })
        {
            string config = subset == "libs.native" && o.Configuration == "Checked" ? "Debug" : o.Configuration;
            // Only fixed tokens cross the PS -> MSBuild -> cmd -> CMake argument boundary.
            // Windows corehost does not forward CMakeArgs, so the toolchain environment is essential.
            string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
                "$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'; " +
                "if(Test-Path -LiteralPath $vswhere) { $vs=& $vswhere -latest -prerelease -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath; " +
                "if($vs) { foreach($relative in @('Common7/IDE/CommonExtensions/Microsoft/CMake/CMake/bin','Common7/IDE/CommonExtensions/Microsoft/CMake/Ninja')) { " +
                "$tool=Join-Path $vs $relative; if(Test-Path -LiteralPath $tool) { $env:PATH=$tool+';'+$env:PATH } } } }; " +
                "$env:CMAKE_TOOLCHAIN_FILE=" + Literal(Path.Combine(directory, "Toolchain.cmake")) + "; " +
                "& " + Literal(Path.Combine(o.Runtime, "eng", "build.ps1")) +
                " -subset " + subset + " -arch " + o.Arch + " -os windows -configuration " + config +
                " -runtimeConfiguration " + o.Configuration +
                (o.ConfigureOnly ? " /p:ConfigureOnly=true" : "") + "; exit $LASTEXITCODE";
            scripts.Add(script);
            commands.Add(new(powershell, ["-OutputFormat", "Text", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand",
                Convert.ToBase64String(Encoding.Unicode.GetBytes(script))], o.Runtime));
        }
        if (o.FullRuntime && !o.ConfigureOnly)
        {
            string sdk = Path.Combine(o.Runtime, ".dotnet", "dotnet.exe");
            string common = " /restore /p:TargetOS=windows /p:TargetArchitecture=" + o.Arch +
                " /p:Configuration=" + o.Configuration + " /p:RuntimeConfiguration=" + o.Configuration +
                " /p:PublishReadyToRun=false /p:GenerateInstallers=false /p:GeneratePackageOnBuild=false" +
                " /p:RuntimeFlavor=CoreCLR /p:PrimaryRuntimeFlavor=CoreCLR " +
                Literal("/p:DirectoryBuildTargetsPath=" + Path.Combine(o.Root, "Builder", "RuntimeLayout.targets"));
            string component = Path.Combine(o.Runtime, "src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.Runtime.CoreCLR.sfxproj");
            string bundle = Path.Combine(o.Runtime, "src/installer/pkg/sfx/bundle/Microsoft.NETCore.App.Bundle.bundleproj");
            foreach (string script in new[]
            {
                "& " + Literal(sdk) + " msbuild " + Literal(component) + common + " /t:GetFilesToPublish; exit $LASTEXITCODE",
                "& " + Literal(sdk) + " msbuild " + Literal(bundle) + common + " /t:PublishToDisk " +
                    Literal("/p:OutputPath=" + Path.Combine(directory, "runtime") + Path.DirectorySeparatorChar) + "; exit $LASTEXITCODE"
            })
            {
                scripts.Add(script);
                commands.Add(new(powershell, ["-NoProfile", "-NonInteractive", "-EncodedCommand",
                    Convert.ToBase64String(Encoding.Unicode.GetBytes(script))], o.Runtime));
            }
        }
        return new(directory, injection, commands.ToArray(), blockers.ToArray(), scripts.ToArray());
    }

    internal static string Toolchain(string injection) =>
        "set(CMAKE_PROJECT_INCLUDE [==[" + injection.Replace('\\', '/') + "]==])\n";

    internal static string Injection(Options o) => """
        cmake_minimum_required(VERSION 3.21)
        if(NOT CMAKE_CURRENT_SOURCE_DIR STREQUAL CMAKE_SOURCE_DIR)
          return()
        endif()
        if(NOT PROJECT_NAME MATCHES "^(CoreCLR|LibsNative|corehost)$")
          return()
        endif()
        get_property(scheduled GLOBAL PROPERTY BUILDER_INJECTION_SCHEDULED)
        if(scheduled)
          return()
        endif()
        set_property(GLOBAL PROPERTY BUILDER_INJECTION_SCHEDULED TRUE)
        function(builder_apply directory)
          if(NOT WIN32 OR NOT MSVC OR CMAKE_CROSSCOMPILING)
            return()
          endif()
          get_property(targets DIRECTORY "${directory}" PROPERTY BUILDSYSTEM_TARGETS)
          foreach(target IN LISTS targets)
            get_target_property(kind "${target}" TYPE)
            get_target_property(imported "${target}" IMPORTED)
            get_target_property(managed "${target}" COMMON_LANGUAGE_RUNTIME)
            if(NOT imported AND (managed STREQUAL "managed-NOTFOUND") AND
               (kind STREQUAL "EXECUTABLE" OR kind STREQUAL "SHARED_LIBRARY" OR kind STREQUAL "MODULE_LIBRARY"))
        """ + $"\n      if(target STREQUAL \"clrinterpreter\")\n        target_sources(\"${{target}}\" PRIVATE [==[{Path.Combine(o.Root, "Builder", "clrinterpreter.rc").Replace('\\', '/')}]==])\n      endif()\n" +
        (o.Command == "baseline" ? "\n" : $"\n      target_sources(\"${{target}}\" PRIVATE [==[{o.Obj.Replace('\\', '/')}]==])\n") + """
              if(kind STREQUAL "EXECUTABLE")
                get_target_property(gui "${target}" WIN32_EXECUTABLE)
                if(gui)
                  target_link_options("${target}" PRIVATE "/SUBSYSTEM:WINDOWS,6.01")
                else()
                  target_link_options("${target}" PRIVATE "/SUBSYSTEM:CONSOLE,6.01")
                endif()
              endif()
              message(STATUS "Builder audited native target: ${target} (${kind})")
            endif()
          endforeach()
          get_property(children DIRECTORY "${directory}" PROPERTY SUBDIRECTORIES)
          foreach(child IN LISTS children)
            builder_apply("${child}")
          endforeach()
        endfunction()
        cmake_language(DEFER DIRECTORY "${CMAKE_SOURCE_DIR}" CALL builder_apply "${CMAKE_SOURCE_DIR}")
        """;
}

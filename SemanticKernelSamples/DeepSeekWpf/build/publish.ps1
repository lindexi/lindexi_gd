param(
	[string]$Version = "1.0.0",
	[string]$OutputPath = "artifacts/publish/win-x64"
)

$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $projectDirectory "DeepSeekWpf.csproj"
$testProjectPath = Join-Path $projectDirectory "../DeepSeekWpf.Tests/DeepSeekWpf.Tests.csproj"
$resolvedOutputPath = Join-Path $projectDirectory $OutputPath

Push-Location $projectDirectory
try {
	dotnet restore $testProjectPath
	dotnet restore $projectPath -r win-x64
	dotnet test $testProjectPath -c Release --no-restore -p:Version=$Version
	dotnet publish $projectPath -c Release --no-restore -p:PublishProfile=win-x64 -p:Version=$Version -p:InformationalVersion=$Version -o $resolvedOutputPath
}
finally {
	Pop-Location
}

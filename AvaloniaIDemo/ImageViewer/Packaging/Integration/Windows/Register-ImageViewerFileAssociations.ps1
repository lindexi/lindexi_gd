<#
.SYNOPSIS
Registers or unregisters per-user Windows file associations for the Avalonia Image Viewer.

.DESCRIPTION
This script is intentionally safe by default. Without -Apply it only prints the
registry operations it would perform. It writes only under HKCU:\Software\Classes
when -Apply is specified, so it does not require elevation and does not change
machine-wide defaults.

Windows 10/11 protects default-app choices. This script registers ImageViewer as
an available handler and adds OpenWith/right-click verbs. Users or installers
should still ask Windows to choose the default app through Settings, or rely on
Open with -> Image Viewer.

.EXAMPLE
.\Register-ImageViewerFileAssociations.ps1 -ExecutablePath "$env:LOCALAPPDATA\ImageViewer\ImageViewer.exe"

.EXAMPLE
.\Register-ImageViewerFileAssociations.ps1 -ExecutablePath "$env:LOCALAPPDATA\ImageViewer\ImageViewer.exe" -Apply

.EXAMPLE
.\Register-ImageViewerFileAssociations.ps1 -Uninstall -Apply
#>
[CmdletBinding(DefaultParameterSetName = 'Register')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Register')]
    [ValidateNotNullOrEmpty()]
    [string]$ExecutablePath,

    [Parameter()]
    [switch]$Apply,

    [Parameter(Mandatory = $true, ParameterSetName = 'Uninstall')]
    [switch]$Uninstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProgId = 'ImageViewer.ImageFile'
$FriendlyName = '图片查看器'
$DefaultApplicationName = 'ImageViewer.exe'
$SupportedExtensions = @('.jpg', '.jpeg', '.png', '.bmp', '.gif', '.webp', '.tif', '.tiff')
$MimeTypesByExtension = @{
    '.jpg' = 'image/jpeg'
    '.jpeg' = 'image/jpeg'
    '.png' = 'image/png'
    '.bmp' = 'image/bmp'
    '.gif' = 'image/gif'
    '.webp' = 'image/webp'
    '.tif' = 'image/tiff'
    '.tiff' = 'image/tiff'
}

function Write-Plan {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Output "[plan] $Message"
}

function Set-RegistryValueIfMissingSafe {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value
    )

    Write-Plan "Set if missing $Path :: $Name = $Value"
    if ($Apply) {
        if (-not (Test-Path -LiteralPath $Path)) {
            New-Item -Path $Path -Force | Out-Null
        }

        $currentValue = Get-ItemPropertyValue -LiteralPath $Path -Name $Name -ErrorAction SilentlyContinue
        if ($null -eq $currentValue) {
            New-ItemProperty -LiteralPath $Path -Name $Name -Value $Value -PropertyType String -Force | Out-Null
        }
    }
}

function Set-RegistryValueSafe {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value,
        [Parameter()][ValidateSet('String', 'None')][string]$ValueKind = 'String'
    )

    $displayName = if ($Name -eq '(default)') { '(default)' } else { $Name }
    Write-Plan "Set $Path :: $displayName = $Value"
    if ($Apply) {
        if (-not (Test-Path -LiteralPath $Path)) {
            New-Item -Path $Path -Force | Out-Null
        }

        if ($Name -eq '(default)') {
            Set-Item -LiteralPath $Path -Value $Value
        }
        elseif ($ValueKind -eq 'None') {
            New-ItemProperty -LiteralPath $Path -Name $Name -Value ([byte[]]::Empty) -PropertyType None -Force | Out-Null
        }
        else {
            New-ItemProperty -LiteralPath $Path -Name $Name -Value $Value -PropertyType String -Force | Out-Null
        }
    }
}

function Remove-RegistryTreeSafe {
    param([Parameter(Mandatory = $true)][string]$Path)

    Write-Plan "Remove $Path"
    if ($Apply -and (Test-Path -LiteralPath $Path)) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Register-ImageViewerAssociations {
    $resolvedExecutable = [System.IO.Path]::GetFullPath($ExecutablePath)
    if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
        throw "ExecutablePath does not exist: $resolvedExecutable"
    }

    $applicationName = [System.IO.Path]::GetFileName($resolvedExecutable)
    $command = '"{0}" "%1"' -f $resolvedExecutable
    $classesRoot = 'HKCU:\Software\Classes'
    $progIdRoot = Join-Path $classesRoot $ProgId
    $applicationRoot = Join-Path (Join-Path $classesRoot 'Applications') $applicationName

    Set-RegistryValueSafe -Path $progIdRoot -Name '(default)' -Value $FriendlyName
    Set-RegistryValueSafe -Path $progIdRoot -Name 'FriendlyTypeName' -Value $FriendlyName
    Set-RegistryValueSafe -Path (Join-Path $progIdRoot 'DefaultIcon') -Name '(default)' -Value ('"{0}",0' -f $resolvedExecutable)
    Set-RegistryValueSafe -Path (Join-Path $progIdRoot 'shell\open') -Name 'MUIVerb' -Value '使用图片查看器打开'
    Set-RegistryValueSafe -Path (Join-Path $progIdRoot 'shell\open\command') -Name '(default)' -Value $command

    Set-RegistryValueSafe -Path $applicationRoot -Name 'FriendlyAppName' -Value $FriendlyName
    Set-RegistryValueSafe -Path (Join-Path $applicationRoot 'DefaultIcon') -Name '(default)' -Value ('"{0}",0' -f $resolvedExecutable)
    Set-RegistryValueSafe -Path (Join-Path $applicationRoot 'shell\open\command') -Name '(default)' -Value $command

    foreach ($extension in $SupportedExtensions) {
        $extensionRoot = Join-Path $classesRoot $extension
        Set-RegistryValueSafe -Path (Join-Path $extensionRoot 'OpenWithProgids') -Name $ProgId -Value '' -ValueKind None
        Set-RegistryValueSafe -Path (Join-Path $extensionRoot "OpenWithList\$applicationName") -Name '(default)' -Value ''
        Set-RegistryValueSafe -Path (Join-Path $extensionRoot 'shell\ImageViewer.open') -Name 'MUIVerb' -Value '使用图片查看器打开'
        Set-RegistryValueSafe -Path (Join-Path $extensionRoot 'shell\ImageViewer.open\command') -Name '(default)' -Value $command

        if ($MimeTypesByExtension.ContainsKey($extension)) {
            Set-RegistryValueIfMissingSafe -Path $extensionRoot -Name 'Content Type' -Value $MimeTypesByExtension[$extension]
        }
    }
}

function Unregister-ImageViewerAssociations {
    $classesRoot = 'HKCU:\Software\Classes'
    Remove-RegistryTreeSafe -Path (Join-Path $classesRoot $ProgId)
    Remove-RegistryTreeSafe -Path (Join-Path (Join-Path $classesRoot 'Applications') $DefaultApplicationName)

    foreach ($extension in $SupportedExtensions) {
        $extensionRoot = Join-Path $classesRoot $extension
        Remove-RegistryTreeSafe -Path (Join-Path $extensionRoot 'shell\ImageViewer.open')
        Remove-RegistryTreeSafe -Path (Join-Path $extensionRoot "OpenWithList\$DefaultApplicationName")
        Write-Plan "Remove value $extensionRoot\OpenWithProgids :: $ProgId"
        if ($Apply) {
            $openWithProgIds = Join-Path $extensionRoot 'OpenWithProgids'
            if (Test-Path -LiteralPath $openWithProgIds) {
                Remove-ItemProperty -LiteralPath $openWithProgIds -Name $ProgId -ErrorAction SilentlyContinue
            }
        }
    }
}

if (-not $Apply) {
    Write-Warning 'Dry run only. Re-run with -Apply to change HKCU registry entries.'
}

if ($Uninstall) {
    Unregister-ImageViewerAssociations
}
else {
    Register-ImageViewerAssociations
}

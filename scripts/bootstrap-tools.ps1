param(
    [switch]$WithExportTemplates
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$toolRoot = Join-Path $workspace '.tools'
$godotRoot = Join-Path $toolRoot 'godot'
$dotnetRoot = Join-Path $toolRoot 'dotnet'
$godotArchive = Join-Path $toolRoot 'godot-4.7.1-mono.zip'
$dotnetInstall = Join-Path $toolRoot 'dotnet-install.ps1'

New-Item -ItemType Directory -Force -Path $toolRoot, $godotRoot, $dotnetRoot | Out-Null

if (-not (Get-ChildItem -LiteralPath $godotRoot -Filter 'Godot*_console.exe' -Recurse -ErrorAction SilentlyContinue)) {
    if (-not (Test-Path -LiteralPath $godotArchive)) {
        Invoke-WebRequest `
            -Uri 'https://github.com/godotengine/godot/releases/download/4.7.1-stable/Godot_v4.7.1-stable_mono_win64.zip' `
            -OutFile $godotArchive
    }

    Expand-Archive -LiteralPath $godotArchive -DestinationPath $godotRoot -Force
}

if (-not (Test-Path -LiteralPath (Join-Path $dotnetRoot 'dotnet.exe'))) {
    if (-not (Test-Path -LiteralPath $dotnetInstall)) {
        Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $dotnetInstall
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File $dotnetInstall `
        -Channel 8.0 `
        -InstallDir $dotnetRoot `
        -NoPath
}

if ($WithExportTemplates) {
    $templateArchive = Join-Path $toolRoot 'Godot_v4.7.1-stable_mono_export_templates.tpz'
    $templateRoot = Join-Path $env:APPDATA 'Godot\export_templates\4.7.1.stable.mono'

    if (-not (Test-Path -LiteralPath $templateArchive) -or
        (Get-Item -LiteralPath $templateArchive).Length -ne 1201759011) {
        Invoke-WebRequest `
            -Uri 'https://github.com/godotengine/godot/releases/download/4.7.1-stable/Godot_v4.7.1-stable_mono_export_templates.tpz' `
            -OutFile $templateArchive
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($templateArchive)
    try {
        New-Item -ItemType Directory -Force -Path $templateRoot | Out-Null
        foreach ($entry in $archive.Entries) {
            if (-not $entry.FullName.StartsWith('templates/')) { continue }
            $relative = $entry.FullName.Substring('templates/'.Length)
            if ([string]::IsNullOrWhiteSpace($relative)) { continue }
            $destination = Join-Path $templateRoot $relative
            if ($entry.FullName.EndsWith('/')) {
                New-Item -ItemType Directory -Force -Path $destination | Out-Null
                continue
            }

            $directory = Split-Path -Parent $destination
            New-Item -ItemType Directory -Force -Path $directory | Out-Null
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $true)
        }
    }
    finally {
        $archive.Dispose()
    }
}

$godotExe = Get-ChildItem -LiteralPath $godotRoot -Filter 'Godot*_console.exe' -Recurse |
    Select-Object -First 1 -ExpandProperty FullName
$dotnetExe = Join-Path $dotnetRoot 'dotnet.exe'

& $godotExe --version
& $dotnetExe --version

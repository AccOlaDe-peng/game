param(
    [switch]$SkipGodot,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$dotnetRoot = Join-Path $workspace '.tools\dotnet'
$godotRoot = Join-Path $workspace '.tools\godot'
$dotnetExe = Join-Path $dotnetRoot 'dotnet.exe'
$godotExe = Get-ChildItem -LiteralPath $godotRoot -Filter 'Godot*_console.exe' -Recurse -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName

if (-not (Test-Path -LiteralPath $dotnetExe)) {
    $dotnetExe = (Get-Command dotnet -ErrorAction Stop).Source
}

$env:DOTNET_ROOT = Split-Path -Parent $dotnetExe
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Push-Location $workspace
try {
    & $dotnetExe build 'Catalyst.csproj' -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Game assembly build failed.' }

    if (-not $SkipTests) {
        & $dotnetExe test 'tests\Catalyst.Tests.csproj' -v:minimal
        if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed.' }
    }

    if (-not $SkipGodot) {
        if (-not $godotExe) {
            throw 'Godot console executable was not found.'
        }

        & $godotExe --headless --path . --import
        if ($LASTEXITCODE -ne 0) { throw 'Godot resource import failed.' }

        & $godotExe --headless --path . 'res://scenes/tests/smoke_runner.tscn'
        if ($LASTEXITCODE -ne 0) { throw 'Godot smoke test failed.' }

        & $godotExe --headless --path . 'res://scenes/tests/element_reaction_runner.tscn'
        if ($LASTEXITCODE -ne 0) { throw 'Element reaction integration test failed.' }

        & $godotExe --headless --path . 'res://scenes/tests/m3_content_runner.tscn'
        if ($LASTEXITCODE -ne 0) { throw 'M3 content integration test failed.' }

        & $godotExe --headless --path . 'res://scenes/tests/m4_completion_runner.tscn'
        if ($LASTEXITCODE -ne 0) { throw 'M4 completion integration test failed.' }
    }
}
finally {
    Pop-Location
}

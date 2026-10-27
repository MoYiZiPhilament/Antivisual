[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$output = Join-Path $root "artifacts\publish\win-x64"
$archive = Join-Path $root "artifacts\Antivisual-win-x64.zip"

New-Item -ItemType Directory -Force $output | Out-Null

dotnet publish (Join-Path $root "Antivisual.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $output `
    --nologo `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$executable = Join-Path $output "Antivisual.exe"
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Published executable was not created: $executable"
}

Compress-Archive -Path (Join-Path $output "*") -DestinationPath $archive -Force

[PSCustomObject]@{
    Application = $executable
    Archive = $archive
    SizeMB = [Math]::Round((Get-Item -LiteralPath $archive).Length / 1MB, 2)
}

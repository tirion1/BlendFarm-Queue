# Builds ready-to-run Windows packages (no .NET install needed on the target machines).
#   release\BlendFarm-Queue\          GUI, for B and C (and for A if A also renders)
#   release\BlendFarm-Queue-Server\   headless server, for A when it only serves
#   release\*.zip                     the same, zipped for a GitHub release
# Needs the .NET SDK (6 or newer) on THIS machine only.
# Output goes to release\ (ignored by git), not to wherever the program is run from.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Remove-Item release -Recurse -Force -ErrorAction SilentlyContinue

$common = @('-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
            '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true')

dotnet publish LogicReinc.BlendFarm        @common -o release\BlendFarm-Queue
if ($LASTEXITCODE -ne 0) { throw 'Publishing the GUI failed' }
dotnet publish LogicReinc.BlendFarm.Server @common -o release\BlendFarm-Queue-Server
if ($LASTEXITCODE -ne 0) { throw 'Publishing the server failed' }

Get-ChildItem release -Recurse -Include *.pdb | Remove-Item

Compress-Archive -Path release\BlendFarm-Queue\*        -DestinationPath release\BlendFarm-Queue-win-x64.zip
Compress-Archive -Path release\BlendFarm-Queue-Server\* -DestinationPath release\BlendFarm-Queue-Server-win-x64.zip

Write-Host "`nDone:"
Get-ChildItem release | ForEach-Object { Write-Host "  $($_.FullName)" }

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

dotnet publish MystIVAssetExplorer.Desktop -c Release -o artifacts/Bin /bl:artifacts/Logs/publish.binlog

Move-Item artifacts/Bin/MystIVAssetExplorer.Desktop.exe artifacts/Bin/MystIVAssetExplorer.exe -Force

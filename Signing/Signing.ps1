param(
  [string]$rootDirectory,
  [string]$pfxPath = "$rootDirectory/Signing/extension.pfx",
  [string]$certPassword,
  [string]$extension = "../discordmessageapiservice_x64.dll"
)

# Correct directory
Set-Location "$rootDirectory\Signing"

# Using SignTool 
$signTool = (Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe" | Select-Object -First 1).FullName
if (-not (Test-Path $signTool)) {
  Write-Error "SignTool.exe not found, please check your Windows SDK path!"
  return
}

$extension = (Resolve-Path $extension).Path
Write-Host "Signing $extension with digital signature and Sectigo timestamp..." -ForegroundColor Yellow
& $signTool sign /f $pfxPath /p $certPassword /fd SHA256 /tr http://timestamp.sectigo.com /td sha256 $extension

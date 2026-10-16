param(
  [string]$pfxPath,
  [string]$certPassword,
  [string]$extension
)

# Get Password (for local development)
$certPasswordFile = ".\.cert"
if (Test-Path $certPasswordFile) {
  $certPassword = (Get-Content -Path $certPasswordFile -Raw).Trim()
  Write-Host "Detected cached password file, loaded automatically." -ForegroundColor Green
}

# Using SignTool 
$signTool = (Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe" | Select-Object -First 1).FullName
if (-not (Test-Path $signTool)) {
  Write-Error "SignTool.exe not found, please check your Windows SDK path!"
  return
}

$dllFiles = Get-ChildItem $extension -Filter "*.dll"
foreach ($file in $dllFiles) {
  $currentDllPath = $file.FullName
    
  Write-Host "Signing $currentDllPath with digital signature and Sectigo timestamp..." -ForegroundColor Yellow
  & $signTool sign /f $pfxPath /p $certPassword /fd SHA256 /tr http://timestamp.sectigo.com /td sha256 $currentDllPath
}

<# 
  This Script is used for creating local certificates for signing .dll on local/github
#>

param (
  [string]$pfxPath = ".\extension.pfx",
  [SecureString]$certPassword
)

# Correct directory
if ((Split-Path (Get-Location).Path -Leaf) -ne "Signing") {
  if (Test-Path ".\Signing") {
    Set-Location ".\Signing"
    Write-Host "Automatically switched working directory to: $(Get-Location)" -ForegroundColor Yellow
  }
  else {
    Write-Error ".\Signing folder not found, please check your file structure!"
    return
  }
}

# Get Password (for local development)
$certPasswordFile = ".\.cert"
if (Test-Path $certPasswordFile) {
  $certPassword = ConvertTo-SecureString ((Get-Content -Path $certPasswordFile -Raw).Trim()) -AsPlainText -Force 
  Write-Host "Detected stored password file, automatically loaded." -ForegroundColor Green
}

if ([string]::IsNullOrEmpty($certPassword)) {
  Write-Error "Cannot find existing certificate password! Please include -certPassword in the command or create the .cert password file."
  return
}

# Create a self-signed code signing certificate
Write-Host "=== Creating Self-Signed Certificate... === " -ForegroundColor Yellow
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=TWAaren" -KeyLength 2048 -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(5)
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $certPassword

# Get absolute path
$pfxPath = (Resolve-Path $pfxPath).Path

# Write certificate into base64.txt
$certBase64 = ".\base64.txt"
Write-Host "=== Writing '$pfxPath' as Base64 into '$certBase64'... ===" -ForegroundColor Yellow
$base64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($pfxPath))
Set-Content -Path $certBase64 -Value $base64

Write-Host "Base64 written into '$((Resolve-Path $certBase64).Path)'..." -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green

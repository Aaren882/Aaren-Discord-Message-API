param (
  [string]$pfxPath,
  [SecureString]$certPassword
)

# Using .NET to load existing PFX file to read public key information
  Write-Host "=== Loading existing certificate to parse public key... ===" -ForegroundColor Yellow
try {
  $absolutePfxPath = (Resolve-Path $pfxPath).Path
  $storageFlags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable

  # Initialize and unlock the certificate in a single line
  $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($absolutePfxPath, $certPassword, $storageFlags)
}
catch {
  Write-Error "Certificate loading failed! Please check the PFX password. Error message: $_"
  return
}

# Using SignTool 
<# $signTool = (Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe" | Select-Object -First 1).FullName
if (-not (Test-Path $signTool)) {
  Write-Error "SignTool.exe not found, please check your Windows SDK path!"
  return
} #>

# Write-Host "Signing $extension with digital signature and Sectigo timestamp..." -ForegroundColor Yellow
# & $signTool sign /f $pfxPath /p $certPassword /fd SHA256 /tr http://timestamp.sectigo.com /td sha256 $extension

$publicKeyBytes = $cert.PublicKey.EncodedKeyValue.RawData

# Convert binary to uppercase hex string (matching 308201... format)
$hexPublicKey = [System.BitConverter]::ToString($publicKeyBytes).Replace("-", "").ToUpper()

Write-Host "`n[Check] Public key string character length is: $($hexPublicKey.Length) (must be exactly 540)" -ForegroundColor Cyan
Write-Host "`n=== Public Key ===" -ForegroundColor Green
Write-Host $hexPublicKey
Write-Host "==============================================" -ForegroundColor Green

# Write Public Key into ".\include\z\publicKey.hpp"
Set-Content -Path "..\include\z\publicKey.hpp" -Value "publicKey = QUOTE($hexPublicKey);"

# $publicKey = "../include/z/publicKey.hpp"
# Move-Item -Path $publicKey -Destination "../dist/publicKey.hpp"

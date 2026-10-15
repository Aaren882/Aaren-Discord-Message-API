param (
  [string]$pfxName = "extension.pfx",
  [SecureString]$certPassword,
  [string]$certBase64 = ""
)

# Correct directory
if ((Split-Path (Get-Location).Path -Leaf) -ne "Signing") {
  if (Test-Path ".\Signing") {
    Set-Location ".\Signing"
    Write-Host "Working directory switched automatically to: $(Get-Location)" -ForegroundColor Yellow
  }
  else {
    Write-Error "Cannot find .\Signing folder, please confirm your file structure!"
    return
  }
}

# Get Password (for local development)
$certPasswordFile = ".\.cert"
if (Test-Path $certPasswordFile) {
  $certPassword = ConvertTo-SecureString ((Get-Content -Path $certPasswordFile -Raw).Trim()) -AsPlainText -Force 
  Write-Host "Detected cached password file, loaded automatically." -ForegroundColor Green
}

if ([string]::IsNullOrEmpty($certPassword)) {
  Write-Error "Cannot find the password for the existing certificate! Please provide -certPassword in the command or create a .cert password file."
  return
}

# Check certificate .pfx
$pfxPath = ".\$pfxName"
if (-not ([string]::IsNullOrEmpty($certBase64))) {
  # Parse Base64 to .pfx
  $bytes = [System.Convert]::FromBase64String($certBase64)
  [System.IO.File]::WriteAllBytes($pfxPath, $bytes)
}
elseif (-not (Test-Path $pfxPath)) {
  # Create certificate (For local development only)
  & "./CreateCertificate.ps1" -pfxPath $pfxPath -certPassword $certPassword
}

# Check if .pfx file exists
if (-not (Test-Path $pfxPath)) {
  Write-Error "Cannot find the specified PFX certificate file: $pfxPath"
  return
}

& ".\PublicKey.ps1" -pfxPath $pfxPath -certPassword $certPassword

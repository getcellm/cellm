<#
.SYNOPSIS
    Creates a new self-signed code signing certificate and exports it to a 
    password-protected .pfx file.

.DESCRIPTION
    This script performs two main actions:
    1. Generates a new self-signed certificate suitable for code signing and installs
       it into the current user's personal certificate store.
    2. Exports the newly created certificate to a .pfx file, prompting the user
       for a password to secure the file.

    This script must be run with Administrator privileges to create the certificate.

.PARAMETER CertificateName
    The friendly name (Subject/DnsName) for the certificate. This is how you will
    identify it in the certificate store.

.PARAMETER PfxExportPath
    The full path, including the filename, where the .pfx file will be saved.
    The script will automatically create the directory if it does not exist.

.EXAMPLE
    .\MakeTestCert.ps1
    (Uses the default values defined in the script)

.EXAMPLE
    .\MakeTestCert.ps1 -CertificateName "My Awesome App Cert" -PfxExportPath "C:\MyProject\Certs\TestCert.pfx"
    (Creates a certificate with a custom name and saves it to a custom location)
#>

[CmdletBinding()]
param (
    [string]$CertificateName = "Test Certificate",
    [string]$PfxExportPath = (Join-Path -Path $PSScriptRoot -ChildPath "TestCertificateContainer.pfx")
)

#======================================================================
# SCRIPT BODY
#======================================================================

# Step 0: Verify that the script is running as an Administrator
Write-Host "Checking for Administrator privileges..."
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning "This script must be run as an Administrator. Please re-run from an elevated PowerShell prompt."
    exit
}
Write-Host "Administrator privileges detected." -ForegroundColor Green
Write-Host ""


# Step 1: Create the self-signed certificate
Write-Host "Creating a new self-signed certificate named '$CertificateName'..."
try {
    $newCert = New-SelfSignedCertificate -DnsName $CertificateName -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeyUsage DigitalSignature -Type CodeSigningCert -ErrorAction Stop

    Write-Host "Successfully created certificate." -ForegroundColor Green
    Write-Host "  Subject: $($newCert.Subject)"
    Write-Host "  Thumbprint: $($newCert.Thumbprint)"
    Write-Host ""
}
catch {
    Write-Error "Failed to create the self-signed certificate. Error: $_"
    exit
}


# Step 2: Prompt for a password
Write-Host "You will now be prompted for a password to protect the .pfx file."
$password = Read-Host -AsSecureString -Prompt "Enter a password for the PFX file"


# Step 3: Ensure the export directory exists
$pfxDir = Split-Path $PfxExportPath -Parent
if (-not (Test-Path $pfxDir)) {
    Write-Host "Export directory '$pfxDir' does not exist. Creating it now..."
    try {
        New-Item -ItemType Directory -Path $pfxDir -ErrorAction Stop | Out-Null
        Write-Host "Directory created." -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to create directory '$pfxDir'. Please check permissions. Error: $_"
        exit
    }
}


# Step 4: Export the certificate to the PFX file
Write-Host "Exporting certificate to '$PfxExportPath'..."
try {
    Export-PfxCertificate -Cert $newCert -FilePath $PfxExportPath -Password $password -ErrorAction Stop
    
    Write-Host ""
    Write-Host "======================================================" -ForegroundColor Cyan
    Write-Host "  SUCCESS!" -ForegroundColor Green
    Write-Host "  Your test certificate has been exported to:" -ForegroundColor Green
    Write-Host "  $PfxExportPath" -ForegroundColor Yellow
    Write-Host "======================================================" -ForegroundColor Cyan
}
catch {
    Write-Error "Failed to export the PFX file. Error: $_"
    exit
}
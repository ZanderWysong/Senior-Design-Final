# PowerShell Deployment Script for Zita Data System

# Set the target deployment directory
$targetDirectory = "C:\srv\zita-testing"

# Ensure the target directory exists
if (!(Test-Path -Path $targetDirectory)) {
    Write-Host "Creating target directory: $targetDirectory"
    New-Item -ItemType Directory -Path $targetDirectory
}

# Copy the necessary files to the target directory
Write-Host "Copying files to target directory..."
Copy-Item -Path ".\bin\Release\net7.0\*" -Destination $targetDirectory -Recurse -Force

# Navigate to the target directory
Set-Location -Path $targetDirectory

# Remove old dependencies (if any)
Write-Host "Cleaning up old dependencies..."
if (Test-Path -Path ".\node_modules") {
    Remove-Item -Recurse -Force -Path ".\node_modules"
}

# Confirm deployment
Write-Host "Deployment completed successfully."

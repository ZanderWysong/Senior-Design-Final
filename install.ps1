# Define the deployment directory
$deployDirectory = "C:\srv\zita"

# Ensure the target directory exists
if (!(Test-Path -Path $deployDirectory)) {
    Write-Host "Creating deployment directory: $deployDirectory"
    New-Item -ItemType Directory -Path $deployDirectory
}

# Copy required files to the deployment directory
Write-Host "Copying project files to $deployDirectory..."
Copy-Item -Path .\bin\Release\net7.0\* -Destination $deployDirectory -Recurse -Force

# Navigate to the deployment directory
Set-Location -Path $deployDirectory

# Remove any old dependencies or artifacts
Write-Host "Cleaning up old dependencies..."
if (Test-Path -Path ".\node_modules") {
    Remove-Item -Recurse -Force -Path ".\node_modules"
}

Write-Host "Deployment completed successfully."

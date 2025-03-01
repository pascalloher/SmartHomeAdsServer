# dotnet publish --configuration Release
# Define the service directory
$ServiceDir = "C:\Program Files\L1\L1AdsServer"

# Check if the service already exists
$service = Get-Service -Name "L1AdsServer" -ErrorAction SilentlyContinue
if ($service) {
    Write-Output "Stopping existing service 'L1AdsServer'..."
    Stop-Service -Name "L1AdsServer" -Force
    Start-Sleep -Seconds 5
    Write-Output "Updating service files..."
    # Copy all files from the parent directory of the script to the service directory
    Copy-Item -Path "$PSScriptRoot\..\*" -Destination $ServiceDir -Recurse -Force
    Write-Output "Starting service 'L1AdsServer'..."
    Start-Service -Name "L1AdsServer"
} else {
    # Copy all files from the parent directory of the script to the service directory
    Copy-Item -Path "$PSScriptRoot\..\*" -Destination $ServiceDir -Recurse -Force
    Write-Output "Creating new service 'L1AdsServer'..."
    New-Service -Name "L1AdsServer" -BinaryPathName "$ServiceDir\L1AdsServer.exe" -StartupType Automatic -Description "L1AdsServer Windows Service"
    # Set the service to restart on failure
    sc.exe failure "L1AdsServer" reset= 86400 actions= restart/60000/restart/60000/restart/60000
    Start-Service -Name "L1AdsServer"
}

Pause
[CmdletBinding()]

param (
    [Parameter(Mandatory)][string]$Name,
    [Parameter(Mandatory)][string]$Dll,
    [Parameter(Mandatory)][string]$Game,
    [string]$Config
)

# Set dist folder.
$dist = ".\dist"

# Clean dist folder.
Remove-Item "$dist\$Name" -Recurse

# Create final module folder in the `dist` directory.
Copy-Item ".\Module" -Destination "$dist\$Name" -Recurse -Exclude SubModule.xml, .gitkeep

# Copy the built assembly in place.
Copy-Item "$Dll" -Destination "$dist\$Name\bin\Win64_Shipping_Client"

# Evaluate macros and copy SubModule.xml.
(Get-Content ".\Module\SubModule.xml" -Raw) `
    -Replace '\$\(Version\)', (Get-ChildItem "$Dll").VersionInfo.FileVersion `
    -Replace '\$\(Name\)', "$Name" | Set-Content -Path "$dist\$Name\SubModule.xml"

# Install the module into the game directory.
Remove-Item "$Game\Modules\$Name" -Recurse
Copy-Item "$dist\$Name" -Destination "$Game\Modules" -Recurse

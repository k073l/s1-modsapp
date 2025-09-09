$ModName = "ModsApp"

# Define paths
$AssetDir = $PSScriptRoot
$ProjectRoot = Resolve-Path "$AssetDir\.."
$MonoAssembly = Resolve-Path "$ProjectRoot\bin\Release Mono\netstandard2.1\$ModName.dll"
$TSZip = Join-Path $AssetDir "$ModName-TS.zip"
$NexusMonoZip = Join-Path $AssetDir "$ModName-Mono.zip"

# Clean up any existing zips
Remove-Item -Path $TSZip, $NexusMonoZip -ErrorAction SilentlyContinue

# --- Package TS ---
$TSFiles = @(
    "$AssetDir\icon.png",
    "$ProjectRoot\README.md",
    "$ProjectRoot\CHANGELOG.md",
    "$AssetDir\manifest.json",
    $MonoAssembly
)
Compress-Archive -Path $TSFiles -DestinationPath $TSZip
Write-Host "Created Thunderstore package: $TSZip"

# --- Package Nexus ---
Compress-Archive -Path $MonoAssembly -DestinationPath $NexusMonoZip
Write-Host "Created Nexus zips: $NexusMonoZip"

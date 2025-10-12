param(
	[Parameter(Mandatory=$true)]
	[string]$BinDirectory,

	[Parameter(Mandatory=$true)]
	[string]$DependencyVersion,

	[Parameter(Mandatory=$true)]
	[string]$SolutionName,

	[Parameter(Mandatory=$true)]
	[string]$ReleaseVersion
)

# Get all the framework-specific subdirectories in the bin folder
$frameworkDirs = Get-ChildItem -Path $BinDirectory -Directory

foreach ($dir in $frameworkDirs) {
	$frameworkName = $dir.Name
	Write-Host "----- Processing framework: $frameworkName -----"

	# --- Part 1: Organize original build artifacts into a 'Plugin' subfolder ---
	Write-Host "Organizing built artifacts into 'Plugin' subfolder..."
	$pluginFolderPath = Join-Path -Path $dir.FullName -ChildPath "Plugin"
	New-Item -ItemType Directory -Path $pluginFolderPath -Force | Out-Null
	
	# Get all items in the directory to move them
	$itemsToMove = Get-ChildItem -Path $dir.FullName -Force
	foreach ($item in $itemsToMove) {
		# Make sure not to move the newly created 'Plugin' folder into itself
		if ($item.Name -ne "Plugin") {
			Move-Item -Path $item.FullName -Destination $pluginFolderPath
		}
	}

	# --- Part 2: Extract dependency assets into the root of the framework folder ---
	$assetFilename = "Flatbed.Dialog.Lite_${DependencyVersion}_${frameworkName}.zip"
	$assetFilePath = Join-Path -Path $BinDirectory -ChildPath $assetFilename

	Write-Host "Looking for dependency asset: $assetFilePath"
	if (Test-Path -Path $assetFilePath -PathType Leaf) {
		Write-Host "Asset found. Extracting to '$($dir.FullName)'..."
		Expand-Archive -Path $assetFilePath -DestinationPath $dir.FullName -Force

		Write-Host "Cleaning up downloaded dependency zip..."
		Remove-Item -Path $assetFilePath
	}
	else {
		Write-Error "Required dependency asset '$assetFilename' not found."
		exit 1
	}

	# --- Part 3: Create the final zipped release archive ---
	Write-Host "Creating final release archive for $frameworkName..."
	$zipFileName = "${SolutionName}_v${ReleaseVersion}_${frameworkName}.zip"
	$destinationPath = Join-Path -Path $dir.Parent.FullName -ChildPath $zipFileName
	$sourceContentPath = Join-Path -Path $dir.FullName -ChildPath "*"

	Compress-Archive -Path $sourceContentPath -DestinationPath $destinationPath -Force
	Write-Host "Successfully created release archive: $zipFileName"
}
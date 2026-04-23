param(
    [string]$UnityPath = "C:\Program Files\Unity\6000.4.0f1\Editor\Unity.exe",
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$OutputRoot = "",
    [switch]$SkipUnityPackage
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable was not found: $UnityPath"
}

$projectPath = (Resolve-Path $ProjectPath).Path
$packageRoot = Join-Path $projectPath "Packages\com.sunmax.trusedison"
$packageManifestPath = Join-Path $packageRoot "package.json"

if (-not (Test-Path -LiteralPath $packageManifestPath)) {
    throw "package.json was not found: $packageManifestPath"
}

$packageManifest = Get-Content -Raw -Encoding utf8 $packageManifestPath | ConvertFrom-Json
$version = $packageManifest.version
$toolName = "TorusEdison"

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Package version could not be read from package.json."
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectPath "ReleaseBuilds"
}

$outputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$stageRoot = Join-Path $outputRoot "$toolName-$version"
$samplesRoot = Join-Path $stageRoot "Samples"
$unityPackagePath = Join-Path $stageRoot "$toolName-$version.unitypackage"
$zipPath = Join-Path $outputRoot "$toolName-$version-release.zip"
$manifestPath = Join-Path $stageRoot "release-manifest.txt"
$packageSamplesRoot = Join-Path $packageRoot "Samples~"

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stageRoot | Out-Null
New-Item -ItemType Directory -Path $samplesRoot | Out-Null

if (-not $SkipUnityPackage) {
    $env:TORUS_EDISON_UNITYPACKAGE_OUTPUT = $unityPackagePath
    try {
        $logPath = Join-Path $outputRoot "$toolName-$version-unitypackage.log"
        if (Test-Path -LiteralPath $logPath) {
            Remove-Item -LiteralPath $logPath -Force
        }

        $process = Start-Process -FilePath $UnityPath `
            -WorkingDirectory $projectPath `
            -ArgumentList @(
                "-batchmode",
                "-quit",
                "-nographics",
                "-projectPath", $projectPath,
                "-executeMethod", "TorusEdison.Editor.Release.TorusEdisonReleaseBuilder.BuildUnityPackageBatch",
                "-logFile", $logPath
            ) `
            -Wait `
            -PassThru

        if ($process.ExitCode -ne 0) {
            throw "Unity batch build failed with exit code $($process.ExitCode). See $logPath"
        }
    }
    finally {
        Remove-Item Env:TORUS_EDISON_UNITYPACKAGE_OUTPUT -ErrorAction SilentlyContinue
    }
}

Copy-Item -LiteralPath (Join-Path $projectPath "README.md") -Destination (Join-Path $stageRoot "README.md")
Copy-Item -LiteralPath (Join-Path $packageRoot "Documentation~\Manual.ja.md") -Destination (Join-Path $stageRoot "Manual.ja.md")
Copy-Item -LiteralPath (Join-Path $packageRoot "Documentation~\Manual.md") -Destination (Join-Path $stageRoot "Manual.md")
Copy-Item -LiteralPath (Join-Path $packageRoot "Documentation~\TermsOfUse.md") -Destination (Join-Path $stageRoot "TermsOfUse.md")
Copy-Item -LiteralPath (Join-Path $packageRoot "Documentation~\ReleaseNotes.md") -Destination (Join-Path $stageRoot "ReleaseNotes.md")
Copy-Item -LiteralPath (Join-Path $packageRoot "Documentation~\ValidationChecklist.md") -Destination (Join-Path $stageRoot "ValidationChecklist.md")
Copy-Item -LiteralPath (Join-Path $packageRoot "CHANGELOG.md") -Destination (Join-Path $stageRoot "CHANGELOG.md")
Copy-Item -LiteralPath (Join-Path $packageRoot "LICENSE.md") -Destination (Join-Path $stageRoot "LICENSE.md")

$sampleFiles = Get-ChildItem -Path $packageSamplesRoot -Recurse -Filter *.gats.json | Sort-Object FullName
if ($sampleFiles.Count -eq 0) {
    throw "No bundled sample projects were found under $packageSamplesRoot"
}

$sampleManifestEntries = @()
foreach ($sampleFile in $sampleFiles) {
    $relativePath = $sampleFile.FullName.Substring($packageSamplesRoot.Length).TrimStart('\', '/')
    $sampleDestination = Join-Path $samplesRoot $relativePath
    $sampleDestinationDirectory = Split-Path -Parent $sampleDestination
    if (-not (Test-Path -LiteralPath $sampleDestinationDirectory)) {
        New-Item -ItemType Directory -Path $sampleDestinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $sampleFile.FullName -Destination $sampleDestination
    $sampleManifestEntries += "- Samples/$($relativePath -replace '\\', '/')"
}

$manifestLines = @(
    "Tool: $toolName",
    "Version: $version",
    "UnityPackage: $(Split-Path -Leaf $unityPackagePath)",
    "Zip: $(Split-Path -Leaf $zipPath)",
    "Contents:",
    "- README.md",
    "- Manual.ja.md",
    "- Manual.md",
    "- TermsOfUse.md",
    "- ReleaseNotes.md",
    "- ValidationChecklist.md",
    "- CHANGELOG.md",
    "- LICENSE.md"
)

$manifestLines += $sampleManifestEntries

if (-not $SkipUnityPackage) {
    $manifestLines += "- $(Split-Path -Leaf $unityPackagePath)"
}

Set-Content -LiteralPath $manifestPath -Encoding utf8 -Value $manifestLines

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $stageRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Output "Release stage: $stageRoot"
Write-Output "Release zip: $zipPath"
if (-not $SkipUnityPackage) {
    Write-Output "UnityPackage: $unityPackagePath"
}

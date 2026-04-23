param(
    [string]$UnityPath = "C:\Program Files\Unity\6000.4.0f1\Editor\Unity.exe",
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$ResultsPath = "",
    [string]$LogPath = "",
    [string[]]$AssemblyNames = @("TorusEdison.Editor.Tests")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable was not found: $UnityPath"
}

$projectPath = (Resolve-Path $ProjectPath).Path

if ([string]::IsNullOrWhiteSpace($ResultsPath)) {
    $ResultsPath = Join-Path $projectPath "Validation\editmode-results.xml"
}

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogPath = Join-Path $projectPath "Logs\editmode-tests.log"
}

$resultsPath = [System.IO.Path]::GetFullPath($ResultsPath)
$logPath = [System.IO.Path]::GetFullPath($LogPath)
$resultsDirectory = Split-Path -Parent $resultsPath
$logDirectory = Split-Path -Parent $logPath

if (-not [string]::IsNullOrWhiteSpace($resultsDirectory)) {
    New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
}

if (-not [string]::IsNullOrWhiteSpace($logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

if (Test-Path -LiteralPath $resultsPath) {
    Remove-Item -LiteralPath $resultsPath -Force
}

if (Test-Path -LiteralPath $logPath) {
    Remove-Item -LiteralPath $logPath -Force
}

$argumentList = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $projectPath,
    "-runTests",
    "-testPlatform", "editmode",
    "-testResults", $resultsPath,
    "-logFile", $logPath
)

$normalizedAssemblyNames = @($AssemblyNames) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
if ($normalizedAssemblyNames.Count -gt 0) {
    $argumentList += @("-assemblyNames", ($normalizedAssemblyNames -join ";"))
}

Write-Output "Running Unity EditMode tests..."
Write-Output "Unity: $UnityPath"
Write-Output "Project: $projectPath"
Write-Output "Results: $resultsPath"
Write-Output "Log: $logPath"

$process = Start-Process -FilePath $UnityPath `
    -WorkingDirectory $projectPath `
    -ArgumentList $argumentList `
    -Wait `
    -PassThru

if ($process.ExitCode -ne 0) {
    throw "Unity EditMode tests failed with exit code $($process.ExitCode). See $logPath"
}

if (-not (Test-Path -LiteralPath $resultsPath)) {
    throw "Unity EditMode test results were not generated: $resultsPath. See $logPath"
}

if ((Get-Item -LiteralPath $resultsPath).Length -le 0) {
    throw "Unity EditMode test results were empty: $resultsPath. See $logPath"
}

try {
    [xml]$resultsXml = Get-Content -LiteralPath $resultsPath -Raw -Encoding utf8
}
catch {
    throw "Unity EditMode test results could not be parsed as XML: $resultsPath. $($_.Exception.Message)"
}

$testRun = $resultsXml.SelectSingleNode("/test-run")
if ($null -eq $testRun) {
    throw "Unity EditMode test results did not contain a test-run root node: $resultsPath"
}

function Get-TestRunIntAttribute {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$Name
    )

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute -or [string]::IsNullOrWhiteSpace($attribute.Value)) {
        return 0
    }

    return [int]$attribute.Value
}

function Get-TestRunStringAttribute {
    param(
        [System.Xml.XmlNode]$Node,
        [string]$Name
    )

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) {
        return ""
    }

    return [string]$attribute.Value
}

$total = Get-TestRunIntAttribute -Node $testRun -Name "total"
$passed = Get-TestRunIntAttribute -Node $testRun -Name "passed"
$failed = Get-TestRunIntAttribute -Node $testRun -Name "failed"
$skipped = Get-TestRunIntAttribute -Node $testRun -Name "skipped"
$result = Get-TestRunStringAttribute -Node $testRun -Name "result"

if ($total -le 0) {
    throw "Unity EditMode tests completed without executing any tests. See $resultsPath and $logPath"
}

if ($failed -gt 0 -or -not [string]::Equals($result, "Passed", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unity EditMode tests did not pass. Result=$result Total=$total Passed=$passed Failed=$failed Skipped=$skipped. See $resultsPath and $logPath"
}

Write-Output "Unity EditMode tests passed. Total=$total Passed=$passed Failed=$failed Skipped=$skipped"

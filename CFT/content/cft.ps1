param(
    [string]$Configuration = "",
    [string]$Path = "~",
    [string]$ConfigurationNameDefault = "",
    [string]$ConfigurationNameFile = "",
    [string]$ConfigurationNameEnv = ""
)

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

Function FindPackageTool([string]$packageName, [string]$tool) {
    $container = $scriptPath
    
    if (Test-Path(Join-Path $container $tool)) {
        # tool found next to the script file
        return Join-Path $container $tool
    }
    if (Test-Path(Join-Path (Join-Path $container bin) $tool)) {
        # tool found on bin folder (useful for websites)
        return Join-Path (Join-Path $container bin) $tool
    }

    # find in packages folder
    while (!(Test-Path(Join-Path $container "packages"))) {
        $containerParent = Join-Path $container ".." -resolve
        if ($container -eq $containerParent) {
            Throw "packages folder not found"
        }
        $container = $containerParent
    }
    $container = Join-Path $container "packages"
    $packagePath = (Get-ChildItem -Path $container -Filter "$packageName.*" | Select-Object -First 1).FullName
    if (!$packagePath) {
        Throw "Package not found: $packageName"
    }
    $packageToolsPath = Join-Path $packagePath "tools"
    $toolPath = Join-Path $packageToolsPath $tool -resolve
    if (!(Test-Path $toolPath)) {
        Throw "Tool not found. tool: $tool, package: $packageName"
    }
    return $toolPath
}

if ($Path.StartsWith("~")) {
    $Path = Join-Path $scriptPath $Path.SubString(1)
}

if (!$Configuration) {
    if ($ConfigurationNameFile -and (Test-Path(Join-Path $Path $ConfigurationNameFile))) {
        $Configuration = Get-Content(Join-Path $Path $ConfigurationNameFile)
        if ($Configuration) {
            Write-Host "Using configuration $Configuration obtained from $ConfigurationNameFile"
        }
    }
    if (!$Configuration -and $ConfigurationNameEnv) {
        $configuration = [environment]::GetEnvironmentVariable($ConfigurationNameEnv)
        if ($Configuration) {
            Write-Host "Using configuration $Configuration obtained from env:$ConfigurationNameEnv"
        }
    }
    if (!$Configuration) {
        $Configuration = $ConfigurationNameDefault
        if ($Configuration) {
            Write-Host "Using default configuration $Configuration"
        }
    }
    if (!$Configuration) {
        Write-Host "No configuration name specified"
    }
} else {
    Write-Host "Using configuration $Configuration"
}

$cftPath = FindPackageTool "cft" "cft.exe"

$command = ((FindPackageTool "cft" "cft.exe") + " $Path $Configuration")
Invoke-Expression $command

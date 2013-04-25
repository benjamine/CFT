param($installPath, $toolsPath, $package)

$global:cftPath = Join-Path $toolsPath "cft"

function global:ConfigTransform {
    [CmdletBinding()]
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$Configuration
    )
    Process {
        $project = Get-Project
        $projectPath = $project.Properties.Item("LocalPath").Value
        $command = $cftPath + " $projectPath $Configuration"
        echo $command
        Invoke-Expression $command
    }
}

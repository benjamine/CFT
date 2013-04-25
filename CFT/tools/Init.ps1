param($installPath, $toolsPath, $package)

$global:cftPath = Join-Path $toolsPath "cft"

function global:CFT {
    [CmdletBinding()]
    param(
        [parameter(ValueFromPipelineByPropertyName = $true)]
        [string]$configurationName
    )
    Process {
        $project = Get-Project
        $outputPath = Join-Path $project.Properties.Item("LocalPath").Value $project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value
        $command = $cftPath + " $outputPath $configurationName"
        echo $command
        Invoke-Expression $command
    }
}

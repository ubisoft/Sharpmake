<#
.SYNOPSIS
Runs the sample with the given name

.DESCRIPTION
Runs all commands for the given sample name, as defined in the SamplesDef.json file. 

.PARAMETER sampleName
name of the sample to run. If there is no sample with than name in the SamplesDef.json, an error is returned.

.PARAMETER configuration
valued used as configuration in the sample. All commands in the selected sample will see their "{configuration}" string replaced with this value.

.PARAMETER framework
valued used as framework in the sample. All commands in the selected sample will see their "{framework}" string replaced with this value.

.PARAMETER os
valued used as os in the sample. All commands in the selected sample will see their "{os}" string replaced with this value.

.PARAMETER vsVersionSuffix
valued used as Vs version suffix in the sample. All commands in the selected sample will see their "{vsVersionSuffix}" string replaced with this value.

#>
param ([string] $sampleName, [string] $configuration, [string] $framework, [string]$os = "", [string] $VsVersionSuffix = "")

# description of a sample
class SampleDef
{
    [string] $Name
    [string[]] $CIs
    [string[]] $OSs
    [string[]] $Frameworks
    [string[]] $Configurations
    [string] $TestFolder = ""
    [string[]] $Commands
}

# json file that contains all samples
class SamplesDefJson
{
    [SampleDef[]] $Samples  
}

# read json file
$testDefs = [SamplesDefJson] (Get-Content -Raw -Path "SamplesDef.json" | Out-String | ConvertFrom-Json)
# run all samples with given name and parameters
foreach ($sample in ($testDefs.Samples | Where-Object { $_.Name -eq $sampleName})) 
{
    $found = $true;
    Write-Host "running sample $($sample.Name)"
    # run all commands registered in sample
    foreach ($command in $sample.Commands) 
    {
        # apply parameters to sample command line
        $resolvedCommand = $command.replace('{testFolder}',$sample.testFolder).replace('{configuration}',$configuration).replace('{framework}',$framework).replace('{os}',$os).replace('{VsVersionSuffix}',$vsVersionSuffix)
        Write-Host "running : $resolvedCommand"
        # add error detection at the end of the command
        $resolvedCommand = $resolvedCommand + '; if ( -not $? ) { throw "command returned error $LASTEXITCODE" }'
        # run command line
        try
        {
            Invoke-Expression "$resolvedCommand" 
        }
        catch  
        {
            Write-Host "error detected in invoked command"
            Write-Error $PSItem.Exception
            exit 1
        }
    }
}
# error if given sample does not exist in json file
if ($found -ne $true)
{
    Write-Error "sample $sampleName was not found"
    exit 1
}

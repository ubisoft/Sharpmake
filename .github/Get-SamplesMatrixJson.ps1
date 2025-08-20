<#
.SYNOPSIS
Gets the matrix strategy used to generate samples jobs on GitHub Actions.

.DESCRIPTION
Transform the content of SamplesDef.json into a GitHub Actions matrix strategy representation that can be used to generate samples job.

.OUTPUTS
System.String
    Get-SamplesMatrixJson returns the samples job matrix strategy formatted as JSON.
#>

# Load samples definitions.
$samplesDef = Get-Content -Raw -Path "SamplesDef.json" | ConvertFrom-Json

# Transform into a list of samples matrix configurations.
$matrixInclude = foreach ($sample in $samplesDef.Samples)
{
    if ($sample.CIs.Contains("github"))
    {
        foreach ($os in $sample.OSs)
        {
            # Skip if windows-2019. This image is no longer available.
            if ($os -eq 'windows-2019') { continue }

            foreach ($framework in $sample.Frameworks)
            {
                # Map os to GitLab runner label
                $runsOn = switch ( $os )
                {
                    'linux' { 'ubuntu-latest' }
                    'macos' { 'macos-14' }
                    default { $os }
                }

                [pscustomobject]@{
                    name = $sample.Name
                    os = $runsOn
                    framework = $framework
                    configurations = $sample.Configurations -join ','
                }
            }
        }
    }
}

# Explicit matrix configurations with include only matrix strategy.
# See https://docs.github.com/en/actions/using-jobs/using-a-matrix-for-your-jobs#example-adding-configurations.
$samplesMatrix = [pscustomobject]@{
    include = $matrixInclude
}

# Output matrix object as Json.
# Enable dynamically specifying samples jobs by passing the produced Json as the matrix strategy.
$samplesMatrix | ConvertTo-Json

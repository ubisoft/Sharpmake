<#
.SYNOPSIS
Gets the child pipeline definition used to execute samples jobs on GitLab pipeline.

.DESCRIPTION
Transform the content of SamplesDef.json into a GitLab pipeline Yaml representation that can be used to execute samples job.

.OUTPUTS
System.String
    Get-SamplesPipeline returns the definition of the samples pipeline formatted as Yaml.
#>

# Load samples definitions.
$samplesDef = Get-Content -Raw -Path 'SamplesDef.json' | ConvertFrom-Json

# Transform into a hash table with an entry for each samples.
$samplesPipeline = @{
    include = @{
        project = "Square/Runners/ci/templates/square-runners-mac-ci-template"
        ref = "6.0.0"
        file = "square_mac_runner.yml"
    }
}

foreach ($sample in $samplesDef.Samples)
{
    if ($sample.CIs.Contains('gitlab'))
    {
        foreach ($os in $sample.OSs)
        {
            foreach ($framework in $sample.Frameworks)
            {
                foreach ($configuration in $sample.Configurations)
                {
                    # Array of commands to execute for a sample.
                    $script = @()

                    # Compose properties specific to runner os.
                    switch -Wildcard ( $os )
                    {
                        'linux'
                        {
                            $osProperties = @{
                                tags = @( 'square_linux_dind' )
                                services = @( 'docker:20.10.10-dind' )
                                variables = @{
                                    DOCKER_HOST = 'tcp://localhost:2376'
                                    DOCKER_TLS_VERIFY = 1
                                    DOCKER_CERT_PATH = '/certs/client'
                                }
                            }
                            $osCompilationName = 'linux'
                            
                            # Install Powershell on Alpine (https://learn.microsoft.com/en-us/powershell/scripting/install/install-alpine?view=powershell-7.2)
                            # Required to run RunSample.ps1.
                            $script += @(
                                'apk add --no-cache ca-certificates less ncurses-terminfo-base krb5-libs libgcc libintl libssl3 libstdc++ tzdata userspace-rcu zlib icu-libs curl'
                                'apk -X https://dl-cdn.alpinelinux.org/alpine/edge/main add --no-cache lttng-ust'
                                'curl -L https://github.com/PowerShell/PowerShell/releases/download/v7.2.11/powershell-7.2.11-linux-alpine-x64.tar.gz -o /tmp/powershell.tar.gz'
                                'mkdir -p /opt/microsoft/powershell/7'
                                'tar zxf /tmp/powershell.tar.gz -C /opt/microsoft/powershell/7'
                                'chmod +x /opt/microsoft/powershell/7/pwsh'
                                'ln -s /opt/microsoft/powershell/7/pwsh /usr/bin/pwsh'
                            )
                        }
                        'macos'
                        {
                            $osProperties = @{
                                extends = @( '.square_mac_arm_xcode16' )
                            }
                            $osCompilationName = 'mac'
                        }
                        'windows*'
                        {
                            $osProperties = @{
                                tags = @( 'square_windows' )
                            }
                            $osCompilationName = 'windows'
                            $vsVersionSuffix = switch ($os)
                            {
                                'windows-2019' { 'Vs2019' }
                                'windows-2022' { 'Vs2022' }
                            }
                        }
                    }


                    # Compose properties specific to a sample.
                    # These should be exceptions since idealy these commands should be in SamplesDef.json.
                    switch ($sample.Name)
                    {
                        'QTFileCustomBuild'
                        {    
                            $script += 'choco install python3 --version 3.10.6 --side-by-side -y --no-progress'
                        }
                    }

                    
                    $script += "pwsh ./RunSample.ps1 -sampleName ""$($sample.Name)"" -configuration $configuration -framework $framework -os $os -vsVersionSuffix $vsVersionSuffix"

                    # Merge sample properties into a single hash table.
                    $sampleJob = $osProperties + @{
                        artifacts = [PSCustomObject]@{
                            when = 'on_failure'
                            untracked = $true
                            expire_in = '1 day'
                        }
                        needs = [PSCustomObject]@{
                            pipeline = '$PARENT_PIPELINE_ID'
                            job = "compilation:${osCompilationName}: [release]"
                        }
                        script = $script
                    }

                    # Add sample to pipeline hash table.
                    $samplesPipeline.Add("$($sample.Name): [$os, $framework, $configuration]", $sampleJob)
                }
            }
        }
    }
}

# Output samples pipeline as Yaml.
# Enable dynamically specifying samples jobs as a child pipeline.
$samplesPipeline | ConvertTo-Yaml -Options DisableAliases

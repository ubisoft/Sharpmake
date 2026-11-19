<#
.SYNOPSIS
Returns the UDID of an iPhone simulator that can run the HelloIOS sample tests.

.DESCRIPTION
Selects an available iPhone simulator compatible with the iphonesimulator SDK of the active Xcode, and outputs its UDID.

The iOS runtime is chosen as the closest one that is not older than the iphonesimulator SDK:
- 'xcodebuild' rejects simulators from runtimes older than the SDK the sample was built with
  (e.g. an iPhone 16e of iOS 26.2 when building with the iOS 26.5 SDK), even if 'simctl' lists them.
- A runtime newer than the SDK is accepted (e.g. SDK 18.5 with only an iOS 18.6 runtime installed).
'xcodebuild -showdestinations' is not used because it only lists placeholder destinations on some runners.

If no suitable simulator is found, a warning is logged and nothing is output. This is not an error.

Use -Verbose to display the SDK version and the device lists returned by 'xcrun simctl'.

.EXAMPLE
$udid = ./GetSimulatorUdid.ps1 -Verbose
#>
[CmdletBinding()]
param ()

# Native command output is captured and sent to the verbose stream, so that only the UDID is written to the output stream.
$sdkVersionText = (xcrun --sdk iphonesimulator --show-sdk-version | Out-String).Trim()
$sdkVersion = $null
if (-not [version]::TryParse($sdkVersionText, [ref] $sdkVersion))
{
    Write-Warning "Unable to get the iphonesimulator SDK version (got '$sdkVersionText'), no simulator selected."
    return
}
Write-Verbose "Simulator SDK version: $sdkVersion"

xcrun simctl list devices | Out-String | Write-Verbose

$simDevices = xcrun simctl list devices available iPhone -j | Out-String | ConvertFrom-Json
$simDevices | ConvertTo-Json -Depth 10 | Write-Verbose

# closest iOS runtime that has devices and is not older than the SDK
$runtime = $simDevices.devices.PSObject.Properties |
    Where-Object { $_.Name -like '*SimRuntime.iOS-*' -and $_.Value.Count -gt 0 } |
    ForEach-Object { [pscustomobject]@{ Version = [version]($_.Name -replace '^.*iOS-', '' -replace '-', '.'); Devices = $_.Value } } |
    Where-Object { $_.Version -ge $sdkVersion } |
    Sort-Object Version |
    Select-Object -First 1

$device = $null
if ($runtime)
{
    $device = $runtime.Devices | Where-Object { $_.name -like 'iPhone*' -and $_.isAvailable } | Select-Object -First 1
}

if (-not $device)
{
    Write-Warning "No iPhone simulator found for iphonesimulator SDK $sdkVersion."
    return
}

Write-Host "Selected iPhone simulator: $($device.name) (iOS $($runtime.Version)) $($device.udid)"
$device.udid

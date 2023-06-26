<#
.SYNOPSIS
Gets the changelog based on a commit reference.

.DESCRIPTION
Generate a changelog that contains an entry for each commits reachable from the provided commit reference until the previously tagged commit is reached.

.PARAMETER commitish
Commit-ish object name from which commits will be enumerated.

.OUTPUTS
System.String
    Get-Changelog returns the changelog content as strings.
#>
param (
    $commitish = "HEAD"
)

$rangeFirst = (git describe --abbrev=0 --tags $commitish^)
$rangeLast = $commitish

if ($LASTEXITCODE -ne 0)
{
    Write-Error "No previous tag found. Error $LASTEXITCODE returned by git describe."
    return $LASTEXITCODE
}

Write-Host "Revision range: $rangeFirst..$rangeLast"

Write-Output "## What's Changed"
git log --no-merges --format=format:"- %s (%an) %h" "$rangeFirst..$rangeLast"

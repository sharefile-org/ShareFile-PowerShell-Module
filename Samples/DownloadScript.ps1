# PowerShell script that will download file(s) based on one or more ItemID (called by different script)
# PLEASE NOTE: This script has a dependency on another script posted to https://github.com/sharefile-org/ShareFile-PowerShell-Module
# 
# This script was originally written by Joel Stocker (Twitter: @sharefilejoel)
# This sample code is provided as-is and can be used freely by others
# Last Revised: March 26, 2016

Param ( [string[]]$itemids, [string]$destDir )

# Load ShareFile PowerSHell Snap-in. The Snap-in can be found on GitHub https://github.com/sharefile-org/ShareFile-PowerShell-Module
Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

# Load credentials from disk. If not there, challenge for credentials and store for future use
# Modify the path to match your desired location
If (test-path "$env:USERPROFILE\Documents\myauth.sfps")
{
  $sfclient=Get-sfclient "$env:USERPROFILE\Documents\myauth.sfps"
}
Else
{

New-sfclient "$env:USERPROFILE\Documents\myauth.sfps"
$sfclient=get-sfclient "$env:USERPROFILE\Documents\myauth.sfps"
} 

# Create empty array
$filelocations = @()

# Go through list of ItemIDs
foreach ($itemid in $itemids) {


# Get Download URL from API
$downloadlink = Send-SfRequest -Client $sfclient -Method GET -Uri "https://techteam.sf-api.com/sf/v3/Items($itemid)/Download?includeallversions=false&redirect=false" -Expand DownloadUrl
$downloadurl = $downloadlink.DownloadUrl.AbsoluteUri


# Check if the destinaton folder exist 
# If it does exist, just download there
If (Test-Path $destDir) {

# Get Timestamp in epoch seconds. This will be used as part of the filename to avoid overwriting files
$timestamp = Get-Date -UFormat "%s"
# Get filename for file being downloaded
$sfItem = Send-SfRequest -Client $sfClient -Method GET -Entity Items -Id $itemid
$filename = $sfItem.FileName

$localdest = $destDir + "\" + $timestamp + "_" + $filename

# Download & Save
$start_time = Get-Date
Import-Module BitsTransfer  
$job = Start-BitsTransfer -Asynchronous -Source $downloadurl -Destination $localdest

while (($Job.JobState -eq "Transferring") -or ($Job.JobState -eq "Connecting")) `
       { sleep 5;} # Poll for status, sleep for 5 seconds, or perform an action.

Switch($Job.JobState)
{
	"Transferred" {Complete-BitsTransfer -BitsJob $Job}
	"Error" {$Job | Format-List } # List the errors.
	default {"Other action"} #  Perform corrective action.
}

$filelocations += "Original Filename: " + $filename + " - Download Location: " + $localdest

# "Time taken: $((Get-Date).Subtract($start_time).Seconds) second(s)" # Not being used in script

}

# If the subfolder doesn't exist, create it and start download
Else {

New-Item -Path $destDir -ItemType Directory | Out-Null

# Get Timestamp in epoch seconds. This will be used as part of the filename to avoid overwriting files
$timestamp = Get-Date -UFormat "%s"
# Get filename for file being downloaded
$sfItem = Send-SfRequest -Client $sfClient -Method GET -Entity Items -Id $itemid
$filename = $sfItem.FileName

$localdest = $destDir + "\" + $timestamp + "_" + $filename

# Download & Save
$start_time = Get-Date
Import-Module BitsTransfer  
$job = Start-BitsTransfer -Asynchronous -Source $downloadurl -Destination $localdest

while (($Job.JobState -eq "Transferring") -or ($Job.JobState -eq "Connecting")) `
       { sleep 5;} # Poll for status, sleep for 5 seconds, or perform an action.

Switch($Job.JobState)
{
	"Transferred" {Complete-BitsTransfer -BitsJob $Job}
	"Error" {$Job | Format-List } # List the errors.
	default {"Other action"} #  Perform corrective action.
}

$filelocations += "Original Filename: " + $filename + " - Download Location: " + $localdest

# "Time taken: $((Get-Date).Subtract($start_time).Seconds) second(s)" # Not being used in script
}
}

"`n"
"Files Downloaded:"
$filelocations

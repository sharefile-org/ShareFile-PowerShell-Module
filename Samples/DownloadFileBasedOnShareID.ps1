# PowerShell script that will download a file to the local filesystem based on the Share URL
# This script currently only works with Share URLs that are send to users already on the ShareFile system. Share URLs that include the activation ID (&a=xxxx) will not work without removal of that part
# PLEASE NOTE: This script has a dependency on another script posted to https://github.com/sharefile-org/ShareFile-PowerShell-Module
# 
# This script was originally written by Joel Stocker (Twitter: @sharefilejoel)
# This sample code is provided as-is and can be used freely by others
# Last Revised: March 26, 2016

Param ( [string]$sharelinkurl ) 
#Param ( [string]$linkshareid ) #Instead of using the Share URL, you could also use the the ShareID. If so, please remove or comment out the line above and comment out the section below the authentication part
$DownloadScript = "C:\Scripts\DownloadScript.ps1" #This PS script calls another script, please ensure the correct path to the Download Script

# Load ShareFile PowerSHell Module. The Module can be found on GitHub https://github.com/sharefile-org/ShareFile-PowerShell-Module
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

# This code is only relevant if using the full Share URL, see comments above
# It determines if the Share URL is formatted as /d/ or /d- and based on that extracts the last 16 or 17 chars respectively 
# Please comment out this section when using Share ID
if ($sharelinkurl -match '\/d\/') 

{$linkshareid = $sharelinkurl.substring($sharelinkurl.length - 16, 16)}

Else {

if ($sharelinkurl -match '\/d\-') 
{$linkshareid = $sharelinkurl.substring($sharelinkurl.length - 17, 17)}

Else {"Not a Share URL or incorrect formatting" 
break} 

}
# End of code for using the Share URL

# Get a list of Item IDs contained in the Share
$shareid = Send-SfRequest -Client $sfclient -Entity Shares -Id $linkshareid
$shareitems = Send-SfRequest -Client $sfclient -Entity Shares -Id $shareid.Id -Expand Items
$itemids = $shareitems.Items.Id

# Create an empty array to store the filesystem destination location
$filelocations = @() #(not being used in this scipt)

$numberofitems = $shareitems.items.count #count the number of items in the download (not being used in this script)

# Set the root folder to download to
$rootfolder = "C:\Archive\" #Modify this based on what local root folder you want to download the files to. This folder needs to exist
# Set the subfolder value to be the ShareID
$subfolder = $linkshareid
# Set the rootfolder and subfolder path
$destDir = $rootfolder + $subfolder

"Download Folder Location: " + $destdir #Return the download path

# Start the Download Script
start-job -Name "Download" -FilePath $DownloadScript -ArgumentList $itemids,$destDir | wait-job
Receive-job -Name "Download"
#start-job -Name "Copy" -FilePath $DownloadScript -ArgumentList $itemids,$destDir | Out-Null # Use if you don't want to see job status on screen. Comment out the 2 lines above
 

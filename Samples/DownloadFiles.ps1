Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

################################################################################
# DownloadFiles.ps1
#
# This script will download all files from the My Files and Folders area of 
# ShareFile to the local Documents directory.
#

#Run the following interactively to create a login token that can be used by Get-SfClient in unattended scripts
#$sfClient = New-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps") -Account YourSubdomain
$sfClient = Get-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps")

#ShareFile directory is relative to the root of the account
#get the current user's home folder to use as the starting point
$ShareFileHomeFolder = "sfdrive:/" + (Send-SfRequest $sfClient -Entity Items).Name

#use the local My Documents folder
$LocalPath = (Join-Path $env:USERPROFILE "Documents")

#Create a PowerShell provider for ShareFile at the location specified
New-PSDrive -Name sfDrive -PSProvider ShareFile -Client $sfClient -Root "/"

#download files from a sub-folder in ShareFile to a local folder
#path must be specified (can't use root) so make sure to map the provider at a level higher than you want to copy from
Copy-SfItem -Path $ShareFileHomeFolder -Destination $LocalPath

#Remove the PSProvider when we are done
Remove-PSDrive sfdrive

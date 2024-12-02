Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

################################################################################
# UploadLocalFiles.ps1
#
# This script will upload all files in the local Documents directory to the 
# My Files and Folders area of ShareFile.
#
# Note: The ShareFile location needs to be specified as files cannot be written
#       to the root of the account. This script gets the user's home folder and
#       uses that, but shared folders could also be used.
#

#Run the following interactively to create a login token that can be used by Get-SfClient in unattended scripts
#$sfClient = New-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps") -Account YourSubdomain
$sfClient = Get-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps")


#upload directory is relative to the root of the account
#get the current user's home folder to use as the starting point
$ShareFileHomeFolder = (Send-SfRequest $sfClient -Entity Items).Url

#use the local My Documents folder as source
$LocalPath = (Join-Path $env:USERPROFILE "Documents")

#Create a PowerShell provider for ShareFile at the location specified
New-PSDrive -Name sfDrive -PSProvider ShareFile -Client $sfClient -Root "\" -RootUri $ShareFilePath

#upload all the files (recursively) in the local folder to the specified folder in ShareFile
Copy-SfItem -Path $LocalPath -Destination "sfDrive:"

#Remove the PSProvider when we are done
Remove-PSDrive sfdrive

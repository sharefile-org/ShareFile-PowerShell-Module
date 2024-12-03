################################################
# DeleteMostRecent.ps1
# 
# This script will delete the most recent version of a file in a ShareFile user's account.
# Initial use case is to overcome Cryptolock Malware.
# ADDITIONAL DETAILS: http://itsme2e.com/2015/11/30/recover-from-cryptolocker-like-malware-with-sharefile/
# 
# This script was originally written by Thuy Nguyen (Twitter: @itsme2e)
# This sample code is provided as-is and can be used freely by others
# Last Revised: March 26, 2016

Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+
 
# Load credentials from disk. If not there, challenge for credentials and store for future use
# Modify the path to match your desired location
If (test-path "$env:USERPROFILE\Documents\myauth.sfps”)
{
  $client=Get-sfclient –Name "$env:USERPROFILE\Documents\myauth.sfps”
}
Else
{

New-sfclient –Name "$env:USERPROFILE\Documents\myauth.sfps”
$client=get-sfclient –Name "$env:USERPROFILE\Documents\myauth.sfps”
} 

 
Function GetUser{
        $EmailAddress = Read-host "Email Address of User"
        # $DateOfAttack = Read-Host "Date of infection"
 
        $DateofAttack = GetDate;
 
        # Pulls the user object based on email address
        $sfUser = Send-SfRequest -Client $client -Entity Users -Parameters @{"emailaddress" = $EmailAddress}
 
        # Transform
        $HomeFolderID =  "Users(" + $sfuser.Id + ")/HomeFolder"
 
        # Pull Children
        $sfHomeFolder = Send-SfRequest -client $client -entity $HomeFolderID -expand "Children"
 
        CheckVersions $sfHomeFolder $DateOfAttack
         
}
 
Function GetDate{
        $date = Read-Host "Date of infection/cryptolock"
        if (($date -as [DateTime]) -ne $null) {
            $date = [DateTime]::Parse($date)
            Write-Host $date
            Return $date;
 
        } else {
          Write-Host "Invalid date format"
          GetDate;
        }
         
}
 
Function CheckVersions($FolderRoot, $Dateofincident){
 
        # loop through children objects
        foreach($item in $FolderRoot.children){
 
            $time = New-TimeSpan -Start $Dateofincident -End $item.creationdate
             
            # Write-Host "date of upload " $item.creationdate
            # Check if Item is a file
            if ($item.__type -eq "ShareFile.Api.Models.File"){
 
                # If there are multiple versions and the date of upload was within the 1 day span of infection
                if (($item.HasMultipleVersions -eq "true") -and ($time.days -le 1)) {
 
                    # Delete File 
                    Send-SfRequest -client $client -entity Items -Method Delete -Id $item.id -Parameters @{"singleversion" = "true"}
                }
 
                else {
                    # Write-Host $item.name " is the only version"
                }
            } 
 
            # Item is a Folder; send back to CheckVersions
            if ($item.__type -eq "ShareFile.Api.Models.Folder"){
                $newRoot = Send-SfRequest -client $client -Entity Items -Id $item.id -expand "Children"
                CheckVersions $newRoot $Dateofincident
            }
 
        }
}
 
GetUser;

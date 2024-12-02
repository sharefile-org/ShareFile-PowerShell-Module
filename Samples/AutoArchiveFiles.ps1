Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

################################################################################
# AutoArchiveFiles.ps1
#
# This script will move all files in the specified Parent folder to 
# a sub folder named the current date
#
#

#Set this to the Id of the folder all the files are uploaded to
$parentID = "Enter Parent Folder ID here"
#Set this to the number of files you want to process at a time (recommended 1000 or less)
$filesAtOnce = 1000;

#Run the following interactively to create a login token that can be used by Get-SfClient in unattended scripts
#$sfClient = New-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps") -Account YourSubdomain
$sfClient = Get-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps")


#Create a new folder named with today's date
$day = Get-Date
$folderName = $day.Year.ToString() + "_" + $day.Month.ToString() + "_" + $day.Day.ToString()
$folderInfo ='{
    "Name":"'+$folderName+'", 
    "Description":"Auto-Generated Folder"
    }'
$folder = Send-SfRequest -Client $sfClient -Entity Items -Method POST -Id $parentID -Navigation Folder -BodyText $folderInfo


#We want to know how many files we'll be moving
$pFolder = Send-SfRequest -Client $sfClient -Entity Items -Id $parentID

#We don't want to grab too many files at once so I limited it to 1000 at a time
for($x=0; $x -le $pFolder.FileCount; $x+=$filesAtOnce){

    #We only need the file ID and the type in order to move so we make a minimal query adding in the top to grab only 1000 files at a time
    $files += Send-SfRequest -Client $sfClient -Entity Items -Id $parentID -Navigation Children -Select Id -Parameters @{'$top' = "$filesAtOnce"; '$skip' = "$x"}
}

$fileParent = '{
    "Parent": {"Id": "'+ $folder.Id +'"}
}'

#Once we have the files we move them
foreach($file in $files){
    if ($file.GetType().ToString() -eq "ShareFile.Api.Models.File"){
        Send-SfRequest -Client $sfClient -Entity Items -Method PATCH -Id $file.Id.ToString() -BodyText $fileParent
    }
}

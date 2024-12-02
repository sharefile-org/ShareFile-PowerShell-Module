# Monitor_Uploads_From_ShareFile_Queue_SYMC_DLP.ps1
# Script written by Cameron Erens & Brian Mathews
# This example code is provided as-is.
# Last Revised May 7, 2015
# For full instructions, including configuration & dependencies please see
# 'Setup Instructions to Monitor ShareFile Uploads to on-prem with Symantec DLP (v1.1).docx'
# available at https://github.com/sharefile-org/ShareFile-PowerShell-Module/blob/master/Samples/Monitor_Uploads_From_ShareFile_Queue_SYMC_DLP_INSTRUCTIONS_1.1.docx
# and also read through the comments in this code.


Add-PSSnapin Microsoft.Exchange.Management.Powershell.Admin -erroraction silentlyContinue
Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

Import-Module 'C:\SymantecDLP\Protect\plugins\Disaster Recovery\Recovery.psm1'
Import-Module 'C:\SymantecDLP\Protect\plugins\Disaster Recovery\LitJson.dll'

$StoragePath = "\\StorageZone\ShareFileData\persistentstorage\sf-us-1\a0aa4f04-d303-45fa-aa66-6b958b44ec1e"
$DropLocation = "\\detectionserver\C$\drop"

# Get objects (qitems) from the Storage Zone's upload / av queue, and make an EML file out of each corresponding persistentstorage file
# this qid is constant--always the same for the upload / av queue
# please note that the storagezone url must be specified in the Recovery.psm file in the Disaster Recovery folder (Citrix ShareFile StorageZones libraries)
$qid = "914DF171-825A-4E0A-B622-384C0778386F"
$qitems = Get-SCQueueItem -qid $qid

foreach ($qitem in $qitems)
{
	$sfFilePath =  $qitem.DataBlob.filepath
	# the $substr will be the file guid in persistentstorage
	$storageGuid = $sfFilePath.SubString($sfFilePath.indexOf("\") )

	# ShareFile V3 api and StorageZones upload / av queue use dashes, but persistentstoage filenames 
	# use underscores, so replace any dashes with underscores
	$storageGuid = $storageGuid.Replace("-","_")
	$storagefile = "$StoragePath$storageGuid"


	# look up additional file attributes from ShareFile regarding upload
	#$storageGuid = $storageGuid.Replace("-","_")
				 
	# ***
	$FileID = $qitem.DataBlob.fileid
	Write-Host "File ID=$FileID"
	# ***
   
	# Now we need to log into ShareFile / use sfclient to pull the ShareFile item to get more metadata about the file
  
	$login = "C:\SymantecDLP\Protect\plugins\SFPS-login.sfps"
	# if the $login file does not exist or is invalid, then the lookup script will return only the
	# ShareFile file id, but no additional metadata
	

	$sfClient = get-SfClient -Name $login
	# could add some null check logic here to check whether a valid $sfClient was gotten successfully

	# Get the ShareFile file item by its ShareFile fileid
	$item = Send-SfRequest -Client $sfClient -Entity Items -id $qitem.DataBlob.fileid
	# could add some null check logic here to check and handle the case if specified ShareFile fileid was not found

	$name = $item.Name
	Write-Host "File Name=$name"

	$creationDate = $item.CreationDate
	Write-Host "Creation Date=$CreationDate"

	$CreatorName = $item.CreatorFirstName + " " + $item.CreatorLastName
	Write-Host "Creator=$CreatorName"

	#t his command returns a path of folder Ids which we need to parse to string names
	$folderPath = $item.Path.ToString()
	# remove first slash before split so it doesn't create empty string
	$fullPath = $folderPath.Substring(1).Split("/")
	$returnPath = "/"
	$count = 0
	# the returned path contains root and the account folder, which we do not need
	for ($x=2;$x-lt$fullPath.Count; $x++ ){
		$pathItem = $fullPath[$x]
		#we want to grab the name of the folder in the path
		$tmpItem = Send-SfRequest -Client $sfClient -Entity Items -Id $pathItem
		$returnPath += $tmpItem.Name + "/"
		$count++
	}
	$returnPath += $ParentItem.Name
	$ParentName = $returnPath
	Write-Host "Parent Folder=$ParentName"		
	
	# need an 'Expand' call to get Creator email address:
	$itemWithCreator = Send-SfRequest -Client $sfClient -Entity Items -id $item.id -Expand Creator
	$CreatorEmail = $itemWithCreator.Creator.Email
	Write-Host "Creator Email=$CreatorEmail"

	# get direct folder link to parent folder
	$parentId = $item.Parent.Id
	$sfUrl = $item.url.ToString()
	$subdomain = $sfUrl.SubString(0,$sfUrl.IndexOf("."))
	$link = "$subdomain.sharefile.com/f/$parentId"

	Write-Host "Direct Link to Parent Folder=$link"

	$FileSizeinKB = $item.FileSizeinKB
	$unit = "KB"
	if($fileSizeinKB -gt 1024*1024)
	{
		$unit = "GB"
	}
	elseif($fileSizeinKB -gt 1024)
	{
		$unit = "MB"
	}
	Write-Host "File Size=$FileSizeinKB $unit"

	# Creates EML message including this metadata, with file as attachment
	# this email message will be placed in drop folder so that Symantec DLP
	# scans the file and generates an DLP incident if a policy is violated
	$smtpClient = New-Object System.Net.Mail.SmtpClient
	$smtpClient.PickupDirectoryLocation = $DropLocation
	$smtpClient.DeliveryMethod = "SpecifiedPickupDirectory"
	$mail = New-Object System.Net.Mail.MailMessage

	$mail.From = $CreatorEmail
	$mail.To.Add("DLP@citrix.com")

	$mail.Subject = $FileID

	$mail.Attachments.Add($storagefile)

	# Populates message body with meaningful information about the file
	$mail.Body = "This was automatically generated.`
	
	File ID=$FileID `
	File Name=$name `
	Creation Date=$CreationDate `
	Creator=$CreatorName `
	Creator Email=$CreatorEmail `
	Parent Folder=$ParentName `
	File Size=$FileSizeinKB $unit `
	Direct Link to Parent Folder=$link"

				 
	$mail.IsBodyHtml = $true

	# Note that we might want to figure out how to give the .EML file a name of our choice
	$smtpClient.Send($mail)

	# Please note that if SFAntiVirus.exe is not run at this point in the script, then the AV / upload queue 
	# will continue to grow, never get cleared out, and so re-running this script would create duplicate DLP incidents.
	# If you:
	# a) believe this script is configured and working properly (generating Symantec incidents for violations)
	# b) do not wish to scan uploaded files with an AV solution
	# c) wish to manually discard the current upload / AV queue items,
	# you could uncomment add this line (uncommented) at the end of the above for loop:
	# $Remove-SCQueueItem -qid $qid -id $qitem.id
	# However if AV is desired, then instead follow the instructions below:

 }
 # At this point, if all is configured correctly, all of the files in the
 # upload / AV queue have been processed and placed as email message files into the drop folder
 # so that Symantec DLP will have scanned them and created incidents if any violations
 # Else, if AV scanning is configured and desired
 # (see documentation on Anti-Virus scanning of StorageZones files
 # available here: http://support.citrix.com/proddocs/topic/sharefile-storagezones-31/sf-cfg-antivirus-scans.html)
 # then do NOT create a separate scheduled task for AV scanning--this script will serve as the one scheduled task.
 # --if you have the script at this point SFAntiVirus.exe tool.
 # This will cause the same items in the upload / AV queue to be processed, scanned,
 # and then discarded, ensuring no dupe incidents when this script is re-run
 # --to do that, uncomment the following lines:

 # cd  c:\inetpub\wwwroot\Citrix\StorageCenter\Tools\SFAntiVirus
 # C:\inetpub\wwwroot\Citrix\StorageCenter\Tools\SFAntiVirus\SFAntiVirus.exe


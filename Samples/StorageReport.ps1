Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

################################################################################
# StorageReport.ps1
#
# This script will list all files that are in any shared folder or a user home
# folder in a custom .csv file.
#
# Notes:
#   - Script should be run as a super user or user who has access to all shared
#     folders and all home folders

#Run the following interactively to create a login token that can be used by Get-SfClient in unattended scripts
#$sfClient = New-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps") -Account YourSubdomain
$sfClient = Get-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps")

#create script variable to store items and IDs that for cache lookup purposes
#this is only needed for the full path details
$script:StorageItemArray = @()
$script:ItemIdArray = @{}
$script:ItemIdArray.Add("", $null)
$script:ItemIdArray.Add("root", $null)

#Get shared folder root
#note that this does not include size of versions that are stored but does list that there are versions
function GetFileSize($ItemID, $ItemType)
{
    #get the children of the passed in folder
    $Items = Send-SfRequest $sfClient -Method GET -Entity Items -Id $ItemID -Expand "Children,Owner"
    foreach ($item in $Items.Children)
    {
        #since folders return a 'file size' of all the children ignore unless it is a file
        if ($item.__type -eq "ShareFile.Api.Models.File")
        {
            #CREATE A HASTHATBLE WITH ID/NAME
            #LOOKUP WITH PATH AND ADD FROM GET IF NOT THERE SO WE ONLY GET ONCE
            $FormattedPath = ""

            #pull apart the path and replace IDs with names
            foreach ($el in $Item.Path.Split("/"))
            {
                #check if we have this in cache
                if ($script:ItemIdArray.ContainsKey($el) -eq $true)
                {
                    #make sure it isn't a value we want to ignore
                    if ($script:ItemIdArray[$el] -ne $null) { $FormattedPath += ($script:ItemIdArray[$el] + "\") }
                }
                else
                {
                    #get the name of the folder from the ID and add to cache
                    $FolderName = Send-SfRequest $sfClient -Method GET -Entity Items -Id $el -Select "Name,Path"
                    
                    #if this is the root of the account we ignore it
                    if ($FolderName.Path -eq "/root")
                    {
                        $script:ItemIdArray.Add($el, $null)
                    }
                    else
                    {
                        $script:ItemIdArray.Add($el, $FolderName.Name)
                        $FormattedPath += ($FolderName.Name + "\")
                    }
                }
            }

            #output an object to use in the report
            #could use the raw $item or could format to be 'pretty'
            $script:StorageItemArray += [PSCustomObject] @{Type=$ItemType; Path=$FormattedPath; Name=$item.Name; FileSizeBytes=$Item.FileSizeBytes; FileCount=$item.FileCount; DateModified=$item.CreationDate; DateAccessed=$item.ClientModifiedDate; DateCreated=$item.ClientCreatedDate; Creator=$item.CreatorNameShort; Owner=$items.Owner.Email; HasVersions=$item.HasMultipleVersions}
        }

        #determine type of item and recurse if a folder
        if ($item.__type -eq "ShareFile.Api.Models.Folder") { GetFileSize $item.Id $ItemType}
    }
}

#Get the top-level shared folders and recurse each one
$Items = Send-SfRequest $sfClient -Method GET -Entity Items -Id "allshared" -Expand "Children"
foreach ($item in $Items.Children)
{
    Write-Host ("Processing Shared Folder: {0}" -f $item.Name)
    GetFileSize $item.Id "Shared Folders"
}


$employees = Send-SfRequest $sfClient -Method GET -Entity Accounts/Employees -select "Id,Email"
foreach ($employee in $employees)
{
    Write-Host ("Processing Home Folder: {0}" -f $employee.Email)
    $HomeFolderID =  "Users(" + $employee.Id + ")/HomeFolder"
    $EmployeeHomeFolder = Send-SfRequest $sfClient -Method GET -Entity $HomeFolderID -Select "Id,Name"
    $script:FullPath = ($EmployeeHomeFolder.Name + "\")
    GetFileSize $EmployeeHomeFolder.Id "Home Folder"
}

#output the report to CSV
$script:StorageItemArray | Export-Csv -NoTypeInformation -Path ((Join-Path $env:USERPROFILE "Documents") + "\Storage." + (Get-Date -Format yyyy-MM-dd) + ".csv")

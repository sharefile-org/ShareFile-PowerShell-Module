Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

################################################################################
# ExpireFileBox.ps1
#
# This script will list all files that are in any user's filebox and are 
# over 7 days old. By default the file box holds files for 30 days but 
# with this script you could identify and delete items earlier. Just uncomment
# the line that processes the delete once you have verified the files.
#
# Notes:
#   - The Outlook plug-in also uses file box to store files shared. Deleting
#     these files will expire the shared links.
#   - This script should be run as an administrator who has the ability to view 
#     other user's filebox
#   - 
#     

#Run the following interactively to create a login token that can be used by Get-SfClient in unattended scripts
#$sfClient = New-SfClient -Name "C:\Users\Peter\Documents\YourSubdomain.sfps" -Account YourSubdomain
$sfClient = Get-SfClient -Name "C:\Users\Peter\Documents\YourSubdomain.sfps"

#Get all the employee users on the account
$employees = Send-SfRequest $sfClient -Method GET -Entity Accounts/Employees -select "Id,Name,Email"
foreach ($employee in $employees)
{
    $FileBoxID = "Users(" + $employee.Id + ")/FileBox"
    $FileBoxFolder = Send-SfRequest $sfClient -Method GET -Entity $FileBoxID -Expand Children
    
    #list the employee name
    Write-Host ("Employee:`t{0} ({1})" -f $employee.Name, $employee.Email)
    foreach ($file in $FileBoxFolder.Children)
    {
        #Check if the date the file was more than xx days ago
        if ( ($file.CreationDate.AddDays($FileBoxExpiration)) -le (Get-Date) )
        {
            Write-Host ("Deleting File:`t{0}`t`t{1}" -f $file.Name, $file.CreationDate)
            #BELOW LINE IS COMMENTED OUT FOR SAFETY - THIS DOES THE ACTUAL DELETE
            #Send-SfRequest $sfClient -Method DELETE -Entity Items -Id $file.Id
        }
    }
}

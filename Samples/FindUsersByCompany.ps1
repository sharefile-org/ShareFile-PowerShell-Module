Param ( [string]$companyName )

#Get all users by companyName
Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+
$sfClient = New-SfClient

#Pull all of the Account Employees or Clients
$sfUsers = Send-SfRequest -Client $sfClient -Entity Accounts\Employees

$fileOutput = @()
#Loop through each of the users
foreach($sfUserId in $sfUsers) {
    #Get full user information including security 
    $sfUser = Send-SfRequest -Client $sfClient -Entity Users -Id $sfUserId.Id
    if ($sfUser.Company -eq $companyName) {
        $fileOutput += New-Object PSObject -Property @{'UserId'=$sfUserId.Id;'FullName'=$sfUser.FullName;'Email'=$sfUser.Email;'Company'=$sfUser.Company}
    }
}
#output the results
$fileOutput

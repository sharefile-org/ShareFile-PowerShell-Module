Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+

################################################################################
# RemoveRole.ps1
#
# This script will update a user's roles to remove a specified list of roles.
#

#Run the following interactively to create a login token that can be used by Get-SfClient in unattended scripts
#$sfClient = New-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps") -Account YourSubdomain
$sfClient = Get-SfClient -Name ((Join-Path $env:USERPROFILE "Documents") + "\YourSubdomain.sfps")

#get a user by email address
$user = Send-SfRequest -Client $sfClient -Method GET -Entity Users -Parameters @{"emailaddress" = "sample@email.com"} 

#Build up a JSON string with all roles we want
$JSONBody = '{"Roles" : ['

#list of roles we want to KEEP
$RolesToKeep = @("CanChangePassword", "CanUseFileBox")

#save which roles the user currently belongs to except for the one we want to remove
$index=0
foreach ($role in $user.roles)
{
    #don't capture the role if we want to remove it
    if ($role.Value -notin $RolesToKeep)
    {
        #need to append a comma if more than 1 item in list and not last item
        if ($index -eq ($user.Roles.Count - 1))
            { $JSONBody += ('"' + $role.Value + '"') }
        else
            { $JSONBody += ('"' + $role.Value + '",') }
    }
    $index++
}

#close the JSON
$JSONBody += ']}'

#Overwrite the roles with this list
Send-SfRequest -Client $sfClient -Method PUT -Entity Users -Navigation Roles -Id $user.Id -BodyText $JSONBody

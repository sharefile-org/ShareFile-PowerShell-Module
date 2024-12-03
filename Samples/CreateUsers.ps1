Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+
$sfClient = New-SfClient

#The import file would typically be an exported contact list from Outlook or equivalent
$contacts = Import-Csv .\Contacts.CSV
foreach ($contact in $contacts)
{
    #make sure we have an email, name, and company
    if ($contact.'E-mail Address' -and $contact.'First Name' -and $contact.'Last Name')
    {
        #create contact in ShareFile
        $user = New-Object ShareFile.Api.Models.User

        #required fields
        $user.FirstName = $contact.'First Name'
        $user.LastName = $contact.'Last Name'
        $user.Email = $contact.'E-mail Address'

        #optional fields
        $user.Company = $contact.Company

        #create client user
        Send-SfRequest $sfClient -Method POST -Entity Users -Body $user -Parameters @{"addshared" = "true"}
    }
}

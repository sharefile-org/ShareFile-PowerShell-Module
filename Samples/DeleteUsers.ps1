#Script to delete users found by the Disabled Users script


Function DeleteUsers{
Param ([Parameter(HelpMessage="Hi Keith")] [string]$UserType="employee")
      $client = New-SfClient -Name "c:\tmp\sfclient.sfps"
      $admin_userid = "ADMIN_USERID"

      $sfUserObjects = Import-Csv ("C:\tmp\" + $UserType + ".csv")

      foreach($sfUser in $sfUserObjects){
           Send-SfRequest -Client $client -Method Delete -Entity Users -Id $sfUser.UserId -Parameters @{"completely" = "true"; "itemsReassignTo" = $admin_userid; "groupsReassignTo" = $admin_userid}
      }

}

#New-SfClient -Name "c:\tmp\sfclient.sfps"
Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+
DeleteUsers -UserType "employee";
DeleteUsers -UserType "client";

<#
Title: ShareFile Prerequisite Check + Installer
Description: This script will check that the appropriate roles, features and applications are installed in order to run the ShareFile StorageZone Controller software on the machine. 
Should the roles/features not be installed, this will give the user the option to install necessary roles/features.
Creators: Marc A. Kuh, Michael Dombroski
Company: Citrix Systems, Inc.

Disclaimer...

The sample scripts are provided AS IS without warranty of any kind. Citrix further disclaims all implied warranties including,  without limitation, any implied warranties of 
merchantability or of fitness for a particular purpose. The entire risk arising out of the use or performance of the sample scripts and documentation remains with you. 
In no event shall Citrix, its authors, or anyone else involved in the creation, production, or delivery of the scripts be liable for any damages whatsoever (including, without 
limitation, damages for loss of business profits, business interruption, loss of business information, or other pecuniary loss) arising out of the use of or inability to use 
the sample scripts or documentation, even if Citrix has been advised of the possibility of such damages.

This script will automatically install .NET Framework 4.5.2 (minimum requirement for StorageCenter to run) if the .NET Framework version is less than 4.5.2.

#>
## Gather .NET version information and update to at least the minimum version of .NET 4.5.2, if applicable, in order to run the StorageCenter software. Restart may be required

$dotnetver = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
Write-Host "Checking the version of .NET Framework.."
If ($dotnetver.Release -eq '378389') {Write-Host ".NET Framework 4.5 is installed - Please upgrade to the latest before installation!" -ForegroundColor Red }
ElseIf ($dotnetver.Release -eq '378675') {Write-Host ".NET Framework 4.5.1 is installed - Please upgrade to the latest before installation!" -ForegroundColor Red  }
ElseIf ($dotnetver.Release -eq '379893') {Write-Host ".NET Framework 4.5.2 is installed" -ForegroundColor Green }
ElseIf ($dotnetver.Release -eq '393297') {Write-Host ".NET Framework 4.6 is installed" -ForegroundColor Green }
ElseIf ($dotnetver.Release -eq '394271') {Write-Host ".NET Framework 4.6.1 is installed" -ForegroundColor Green }
ElseIf ($dotnetver.Release -eq '394748') {Write-Host ".NET Framework 4.6.2 is installed" -ForegroundColor Green }
Else {Write-Host "Please install the latest version of .NET Framework before beginning." -ForegroundColor Red }

If ($dotnetver.Release -ge ’378389’ -and $dotnetver.Release -lt '379893') {
Write-Host "Downloading .Net 4.5.2, Please wait..."
$SourceURI = "https://download.microsoft.com/download/B/4/1/B4119C11-0423-477B-80EE-7A474314B347/NDP452-KB2901954-Web.exe"
$FileName = $SourceURI.Split('/')[-1]
$BinPath = Join-Path $env:SystemRoot -ChildPath "Temp\$FileName"

if (!(Test-Path $BinPath))
 {
 Invoke-Webrequest -Uri $SourceURI -OutFile $BinPath
 }
Write-Host "Installing .NET 4.5.2, Please wait... Restart may be required..."
Start-Process -FilePath $BinPath -ArgumentList "/q /norestart" -Wait -NoNewWindow
    If ($dotnetver.Release -lt '379389') {
    Restart-Computer -Confirm
    }
}
ElseIf ($dotnetver.Release -lt ’379893’) {
Write-Host "Downloading .Net 4.5.2, Please wait..."
$SourceURI = "https://download.microsoft.com/download/B/4/1/B4119C11-0423-477B-80EE-7A474314B347/NDP452-KB2901954-Web.exe"
$FileName = $SourceURI.Split('/')[-1]
$BinPath = Join-Path $env:SystemRoot -ChildPath "Temp\$FileName"

if (!(Test-Path $BinPath))
 {
 (New-Object System.Net.WebClient).DownloadFile($SourceURI, $BinPath)
 }
Write-Host "Installing .NET 4.5.2, Please wait... Restart may be required..."
Start-Process -FilePath $BinPath -ArgumentList "/q /norestart" -Wait -NoNewWindow
    If ($dotnetver.Release -lt '379893') {
    Restart-Computer -Confirm
    }
} else {

##This function tests connectivity from the StorageZone Controller to the ShareFile control plane.
function TestPort
{
    Param(
        [parameter(ParameterSetName='ComputerName', Position=0)]
        [string]$ComputerName,
 
        [parameter(ParameterSetName='IP', Position=0)]
        [System.Net.IPAddress]
        $IPAddress,
 
        [parameter(Mandatory=$true , Position=1)]
        [int]
        $Port,
 
        [parameter(Mandatory=$true, Position=2)]
        [ValidateSet("TCP", "UDP")]
        [string]
        $Protocol
        )
 
    $RemoteServer = If ([string]::IsNullOrEmpty($ComputerName)) {$IPAddress} Else {$ComputerName};
 
    If ($Protocol -eq 'TCP')
    {
        $test = New-Object System.Net.Sockets.TcpClient;
        Try
        {
            Write-Host "Connecting to "$RemoteServer":"$Port" (TCP)..";
            $test.Connect($RemoteServer, $Port);
            Write-Host "Connection successful" -ForegroundColor Green ;
        }
        Catch
        {
            Write-Host "Connection failed" -ForegroundColor Red;
        }
        Finally
        {
            $test.Dispose();
        }
    }
       
}
 
$subdomain = Read-Host "Please type your ShareFile subdomain"
$fqdn = Read-Host "Please type your StorageZones Controller's FQDN"
 
$endpoints = @()
$endpoints += @("$subdomain.sf-api.com","$subdomain.sharefile.com")
$endpoints | ForEach {TestPort -ComputerName "$_" -Port 443 -Protocol TCP}
 
$extIP=Resolve-DNSName -Name $fqdn -type A -Server 8.8.8.8| Select IPAddress -ExpandProperty IPAddress
$extIP | ForEach {TestPort -ComputerName "$_" -Port 443 -Protocol TCP}

## Gathers server version information and continues to appropriate line of script. 
$Winserv2012="6.2.0"    
$Version=(Get-WmiObject win32_operatingsystem).version    
Import-Module ServerManager

## For Windows Server 2012 R2
if($version -ge "6.2.0"){
## Check the local server for necessary roles/features - if not currently installed, gives the user the option to install missing roles/features.
$features = @()
$features += @("FileAndStorage-Services","Storage-Services","Web-Server","Web-WebServer","Web-Common-Http","Web-Default-Doc","Web-Dir-Browsing","Web-Http-Errors","Web-Static-Content","Web-Health","Web-Http-Logging","Web-Performance","Web-Stat-Compression","Web-Security","Web-Filtering","Web-Basic-Auth","Web-Windows-Auth","Web-App-Dev","Web-Net-Ext45","Web-Asp-Net45","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Mgmt-Tools","Web-Mgmt-Console","Web-Scripting-Tools","NET-Framework-45-Features","NET-Framework-45-Core","NET-Framework-45-ASPNET","NET-WCF-Services45","NET-WCF-TCP-PortSharing45","FS-SMB1","User-Interfaces-Infra","Server-Gui-Mgmt-Infra","Server-Gui-Shell","PowerShellRoot","
PowerShell","PowerShell-ISE","WoW64-Support")
 
## Check the local server for necessary roles/features - if not currently installed, gives the user the option to install missing roles/features.
$localfeatures = @()
$localfeatures=Get-WindowsFeature | where-object {$_.Installed -eq $True} | Select-Object DisplayName | Out-String
 
 
Write-Host "Checking Roles/Features..."

$features | ForEach {Get-WindowsFeature -Name $_ }

function Show-Menu
{
     param (
           [string]$Title = 'Would you like to install the missing pre-requisites?'
     )
 
     Write-Host "$title"
     
     Write-Host "1: Press 'Y' to install missing roles/features."
     Write-Host "2: Press 'N' to skip this step."
     Write-Host "Q: Press 'Q' to quit."
}
do
{
     Show-Menu
     $input = Read-Host "Please make a selection"
     
     switch ($input)
     {
             'Y' {
                'Installing roles/features, please wait...'
			$features = @()
            $features += @("FileAndStorage-Services","Storage-Services","Web-Server","Web-WebServer","Web-Common-Http","Web-Default-Doc","Web-Dir-Browsing","Web-Http-Errors","Web-Static-Content","Web-Health","Web-Http-Logging","Web-Performance","Web-Stat-Compression","Web-Security","Web-Filtering","Web-Basic-Auth","Web-Windows-Auth","Web-App-Dev","Web-Net-Ext45","Web-Asp-Net45","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Mgmt-Tools","Web-Mgmt-Console","Web-Scripting-Tools","NET-Framework-45-Features","NET-Framework-45-Core","NET-Framework-45-ASPNET","NET-WCF-Services45","NET-WCF-TCP-PortSharing45","FS-SMB1","User-Interfaces-Infra","Server-Gui-Mgmt-Infra","Server-Gui-Shell","PowerShellRoot","
PowerShell","PowerShell-ISE","WoW64-Support")
            $features=Install-WindowsFeature -Name FileAndStorage-Services,Storage-Services,Web-Server,Web-WebServer,Web-Common-Http,Web-Default-Doc,Web-Dir-Browsing,Web-Http-Errors,Web-Static-Content,Web-Health,Web-Http-Logging,Web-Performance,Web-Stat-Compression,Web-Security,Web-Filtering,Web-Basic-Auth,Web-Windows-Auth,Web-App-Dev,Web-Net-Ext45,Web-Asp-Net45,Web-ISAPI-Ext,Web-ISAPI-Filter,Web-Mgmt-Tools,Web-Mgmt-Console,Web-Scripting-Tools,NET-Framework-45-Features,NET-Framework-45-Core,NET-Framework-45-ASPNET,NET-WCF-Services45,NET-WCF-TCP-PortSharing45,FS-SMB1,User-Interfaces-Infra,Server-Gui-Mgmt-Infra,Server-Gui-Shell,PowerShellRoot,PowerShell,PowerShell-ISE,WoW64-Support
            exit
		   } 'N' {
                'Please install missing roles/features before continuing.'
                exit
           } 'Q' {
                exit
           }
     }
     pause
}
until ($localfeatures=Get-WindowsFeature | where-object {$_.Installed -eq $True})

} else {

## For Windows Server 2008 R2
$features = @()
$features += @("File-Services","FS-FileServer","Web-Server","Web-WebServer","Web-Common-Http","Web-Default-Doc","Web-Dir-Browsing","Web-Http-Errors","Web-Static-Content","Web-Health","Web-Http-Logging","Web-Performance","Web-Stat-Compression","Web-Security","Web-Filtering","Web-Basic-Auth","Web-Windows-Auth","Web-App-Dev","Web-Net-Ext","Web-Asp-Net","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Mgmt-Tools","Web-Mgmt-Console","Web-Scripting-Tools","Application-Server","AS-NET-Framework","AS-Web-Support","AS-TCP-Port-Sharing","PowerShell-ISE")
 
## Check the local server for necessary roles/features - if not currently installed, gives the user the option to install missing roles/features.
$localfeatures = @()
$localfeatures=Get-WindowsFeature | where-object {$_.Installed -eq $True} | Select-Object DisplayName | Out-String
 
Write-Host "Checking Roles/Features..."

$features | ForEach {Get-WindowsFeature -Name $_ }

function Show-Menu
{
     param (
           [string]$Title = 'Would you like to install the missing pre-requisites?'
     )
 
     Write-Host "$title"
     
     Write-Host "1: Press 'Y' to install missing roles/features."
     Write-Host "2: Press 'N' to skip this step."
     Write-Host "Q: Press 'Q' to quit."
}
do
{
     Show-Menu
     $input = Read-Host "Please make a selection"
     switch ($input)
     {
             'Y' {
                'Installing roles/features, please wait...'
            $features = @()
            $features += @("File-Services","FS-FileServer","Web-Server","Web-WebServer","Web-Common-Http","Web-Default-Doc","Web-Dir-Browsing","Web-Http-Errors","Web-Static-Content","Web-Health","Web-Http-Logging","Web-Performance","Web-Stat-Compression","Web-Security","Web-Filtering","Web-Basic-Auth","Web-Windows-Auth","Web-App-Dev","Web-Net-Ext","Web-Asp-Net","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Mgmt-Tools","Web-Mgmt-Console","Web-Scripting-Tools","Application-Server","AS-NET-Framework","AS-Web-Support","AS-TCP-Port-Sharing","PowerShell-ISE")
            $features=Get-WindowsFeature | Where-Object {$features.Installed -eq $False} | Select-Object | Add-WindowsFeature -Name File-Services,FS-FileServer,Web-Server,Web-WebServer,Web-Common-Http,Web-Default-Doc,Web-Dir-Browsing,Web-Http-Errors,Web-Static-Content,Web-Health,Web-Http-Logging,Web-Performance,Web-Stat-Compression,Web-Security,Web-Filtering,Web-Basic-Auth,Web-Windows-Auth,Web-App-Dev,Web-Net-Ext,Web-Asp-Net,Web-ISAPI-Ext,Web-ISAPI-Filter,Web-Mgmt-Tools,Web-Mgmt-Console,Web-Scripting-Tools,Application-Server,AS-NET-Framework,AS-Web-Support,AS-TCP-Port-Sharing,PowerShell-ISE
            exit
           } 'N' {
                'Please install missing roles/features before continuing.'
                exit
           } 'Q' {
                exit
           }
     }
     pause
}
until ($localfeatures=Get-WindowsFeature | where-object {$_.Installed -eq $True})
}

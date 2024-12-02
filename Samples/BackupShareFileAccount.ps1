################################################################################
# BackupShareFileAccount.ps1
#
# Download an entire ShareFile account to a local directory.
#
#
# Local Directory Structure:
#
# Local Directory
# │   log-<date>.txt
# │   account-<date>.csv
# │   employees-<date>.csv
# │   sharedfolders-<date>.csv
# │     
# └───Users
# │   │
# │   └───user@email.com
# │       │   'Home Folder Name'
# │       └─   ...
# │   
# └───Shared Folders
#     │   folder
#     │   ...
#
# 
# Requirements:
# PowerShell 5
# The latest PowerShell SDK for ShareFile: https://github.com/citrix/ShareFile-PowerShell
# A Superuser for the ShareFile account: https://support.citrix.com/article/CTX208527
#
# Usage: 
# .\BackupShareFileAccount.ps1; Backup-ShareFileAccount -Type Shared -Path <LOCAL BACKUP PATH> -Email <ADMIN EMAIL> -Subdomain <ACCOUNT SUBDOMAIN>
#
# Type: Full, Users
# Path: The local path to backup to
# Email: A superuser account email
# Subdomain: The account subdomain
#

Import-Module ShareFile
#Import-Module ShareFile-Core # For PowerShell 7+
$global:BackupPath = $null
$global:SFClient = $null

function Backup-ShareFileAccount {
    [CmdletBinding(DefaultParameterSetName = "RequiredSet")]
    param(
        [Parameter(Mandatory,ParameterSetName = "RequiredSet")]
        [ValidateSet("Shared","Users","Full")]
        [string]$Type,
        [Parameter(Mandatory,ParameterSetName = "RequiredSet")]
        [string]$Path,
        [Parameter(Mandatory,ParameterSetName = "RequiredSet")]
        [string]$Email,
        [Parameter(Mandatory,ParameterSetName = "RequiredSet")]
        [string]$Subdomain
    )
    
    $global:BackupPath = $Path
    $SFClientPath = $Path + "\" + $Email + ".sfps"

    if ([System.IO.File]::Exists($SFClientPath)) {
        Write-Host $SFClientPath " was found." -BackgroundColor Green -ForegroundColor DarkBlue
        $global:SFClient = Get-SFClient -Name $SFClientPath
        # Check validity
        if($?){
            $(Send-SfRequest $SFClient -Entity Accounts) 2>&1 | out-null
        } else {
            Write-Host "Saved client file is invalid. Please sign in." -BackgroundColor Yellow -ForegroundColor DarkBlue
            New-SfClient -Name $SFClientPath -Account $Subdomain
            $global:SFClient = Get-SFClient -Name $SFClientPath
        }
    } else {
        Write-Host $SFClientPath " was NOT found. Please sign in." -BackgroundColor Yellow -ForegroundColor DarkBlue
        New-SfClient -Name $SFClientPath -Account $Subdomain
        $global:SFClient = Get-SFClient -Name $SFClientPath
    }

    Log "A $Type backup was requested by $Email."
    $AccountInfo = Send-SfRequest -Client $SFClient -Method GET -Entity Accounts -select "CompanyName,subdomain,Id" | Select-Object -Property CompanyName,subdomain,Id 
    $AccountInfo | Export-Csv -Path $BackupPath/account-$("{0:yyyyMMdd}" -f (Get-Date)).csv -NoTypeInformation

    switch ($Type) {
        "Shared"  { Backup-SharedFolders -SFClient $SFClient }
        "Users" { Backup-Users -SFClient $SFClient }
        "Full" { Backup-Full -SFClient $SFClient }
    }
}

function Backup-Full {
    param(
        [Parameter(Mandatory)]
        [ShareFile.Api.Powershell.PSShareFileClient]$SFClient
    )
    
    Log "Initiated Backup-Full function."
    Backup-Users -SFClient $SFClient
    Backup-SharedFolders -SFClient $SFClient

}

function Backup-SharedFolders {
    param(
        [Parameter(Mandatory)]
        [ShareFile.Api.Powershell.PSShareFileClient]$SFClient
    )

    Log "Initiated Backup-SharedFolders function."
    $sharedfolders = Send-SfRequest -Client $SFClient -Method GET -Entity Items -Id "allshared" -Expand "Children" | Select-object -ExpandProperty Children 
    $sharedfolders | Select-Object -Property Name,Id | Export-Csv -Path $BackupPath/sharedfolders-$("{0:yyyyMMdd}" -f (Get-Date)).csv -NoTypeInformation
    
    # Local Folder Structure and Download
    $SharedPath = "$BackupPath/Shared Folders"
    if (!(Test-Path $SharedPath)) { New-Item -Path $SharedPath -Type Directory }
    foreach ($sharedfolder in $sharedfolders) {
        $Root = "$($sharedfolder.Id):/" + "$($sharedfolder.Name)"
        $LocalPath = "$SharedPath/"
        Backup-ChildItems -Root $Root -LocalPath $LocalPath -DriveId $($sharedfolder.Id) -SFClient $SFClient
    }

    Log "Shared Folders backup completed."
}

function Backup-Users {
    param(
        [Parameter(Mandatory)]
        [ShareFile.Api.Powershell.PSShareFileClient]$SFClient
    )
    
    Log "Initiated Backup-Users function."
    $Employees = Send-SfRequest -Client $SFClient -Method GET -Entity Accounts/Employees -select "Id,Email"
    $Employees | Select-Object -Property Email,Id | Export-Csv -Path $BackupPath/employees-$("{0:yyyyMMdd}" -f (Get-Date)).csv -NoTypeInformation
    
    # Local Folder Structure & Download
    $UsersPath = "$BackupPath/Users"
    if (!(Test-Path $UsersPath)) { New-Item -Path $UsersPath -Type Directory } 
    foreach ($Employee in $Employees) {
        if (!(Test-Path "$UsersPath/$($Employee.Email)")) { New-Item -Path "$UsersPath/$($Employee.Email)" -Type Directory }
        $Root = "$($Employee.Id):/" + (Send-SfRequest $SFClient -Entity "Users($($Employee.Id))/HomeFolder").Name
        $LocalPath = "$UsersPath/$($Employee.Email)/"
        Backup-ChildItems -Root $Root -LocalPath $LocalPath -DriveId $($Employee.Id) -SFClient $SFClient
    }

    Log "User backup completed."
}

function Backup-ChildItems {
    param(
        [Parameter(Mandatory)]
        [ShareFile.Api.Powershell.PSShareFileClient]$SFClient,
        [Parameter(Mandatory)]
        [string]$Root,
        [Parameter(Mandatory)]
        [string]$LocalPath,
        [Parameter(Mandatory)]
        [string]$DriveId
    )
    New-PSDrive -Name $DriveId -PSProvider ShareFile -Client $SFClient -Root "/"
    Copy-SfItem -Path $Root -Destination $LocalPath
    Remove-PSDrive $DriveId
}

function Log {
    param(
        [Parameter(Mandatory=$true)][String]$msg
    )
    $LogFileDate = "{0:yyyyMMdd}" -f (Get-Date)
    $LogTime = "[{0:MM/dd/yy} {0:HH:mm:ss}] " -f (Get-Date)
    Add-Content $BackupPath/log-$LogFileDate.txt ($LogTime + $msg)
}

#
# Module manifest for module 'ShareFile-Core'
#
# Generated on: 11/26/2024
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'ShareFileModule.dll'

# Version number of this module.
ModuleVersion = '1.0'

# Supported PSEditions
CompatiblePSEditions = @('Core')

# ID used to uniquely identify this module
GUID = 'a3c12ea7-fa7c-4478-bb10-aae04e1b0a9a'

# Author of this module
Author = 'ShareFile'

# Company or vendor of this module
CompanyName = 'Progress ShareFile'

# Copyright statement for this module
Copyright = '(c) 2024 Progress. All rights reserved.'

# Description of the functionality provided by this module
Description = 'PowerShell Core Module for ShareFile API.'

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '5.0'

# Processor architecture (None, X86, Amd64) required by this module
ProcessorArchitecture = 'None'

# Assemblies that must be loaded prior to importing this module
RequiredAssemblies = @('ShareFile.Api.Client.dll', 'System.Management.Automation.dll', 'NLog.dll', 'Newtonsoft.Json.dll', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'Microsoft.Web.WebView2.Wpf.dll')

# Format files (.ps1xml) to be loaded when importing this module
FormatsToProcess = @('ShareFile.Format.ps1xml')

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = @()

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = @('Copy-SfItem', 'Get-SfClient', 'New-SfClient', 'Send-SfRequest', 'Sync-SfItem')

# Variables to export from this module
VariablesToExport = '*'

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @()

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = @('ShareFile', 'Windows', 'PSEdition_Core')

        # A URL to the license for this module.
        LicenseUri = 'https://github.com/sharefile-org/ShareFile-PowerShell/blob/master/ShareFileSnapIn/LICENSE.txt'

        # A URL to the main website for this project.
        ProjectUri = 'https://github.com/sharefile-org/ShareFile-PowerShell'

        # A URL to an icon representing this module.
        IconUri = 'https://github.com/sharefile-org/ShareFile-PowerShell/blob/master/ShareFileSnapIn/Assets/sf-icon.ico'

        # ReleaseNotes of this module
        # ReleaseNotes = ''

    } # End of PSData hashtable

} # End of PrivateData hashtable

}


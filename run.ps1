<#
.SYNOPSIS
Run a build and/or the service

.DESCRIPTION
Runs one or more Tasks in the psakefile.ps1

.PARAMETER Task
The task(s) to run

.PARAMETER ContainerName
Docker container if using DockerRun|Interactive|Stop, defaults to <servicename>

.PARAMETER DockerTag
Optional tag for the docker container, leave blank for ccc-cas-workflow4-api:MM.dd.yy

.PARAMETER DockerEnvFile
Env file for Docker for DockerRun and DockerInteractive

.PARAMETER NuGetUrl
Url to Nuget repository. Defaults to Nexus

.PARAMETER LocalPort
Local port to expose from container

.PARAMETER NexusUser
Nexus username for builds to get dependent packages, defaults to $env:nexusUser

.PARAMETER NexusPass
Nexus password for builds to get dependent packages, defaults to $env:nexusPass

.PARAMETER NoCache
Don't use Cache for DockerBuild

.PARAMETER PlainOutput
Set for getting plain docker build output

.PARAMETER HelmDryRun
For helm install, do dry run

.PARAMETER HelmNoWait
For helm install, wait until deployed

.PARAMETER KeepDockerEnv
Don't delete the generated docker env file on run. Useful debugging failed docker starts

.PARAMETER WantStdOut
For StartBack, show StdOut to the console

.EXAMPLE
.\run.ps1

Run a typical local build

.EXAMPLE
.\run.ps1 ci

Run the ci build

.EXAMPLE
.\run.ps1 DockerBuild,DockerInteractive -NexusUser $env:nexusUser -NexusPass $env:nexusPass

Run a Docker build and then run it in the current console (build requires user and pass)
#>
[CmdletBinding()]
param(
    [ValidateSet('default','ci','UpdateRunPs1','DumpVars','DotnetBuild','StartBack','StopBack','TestClient','GetReady','IntegrationTest','UnitTest','OpenSln','DockerBuild','HelmDependencyBuild','HelmInstall','HelmUninstall','DockerRun','DockerInteractive','DockerStop')]
    [string[]] $Task = 'Default',
    [string] $DockerEnvFile = (Join-Path $PSScriptRoot "env.list"),
    [string] $ContainerName = "workflow4-api",
    [string] $DockerTag = "ccc-cas-workflow4-api:$((Get-Date).ToString('MM.dd.yy'))",
    [string] $NuGetUrl = "https://artifacts.aisreview.com/repository/nuget-hosted/",
    [ValidateRange(32000,65535)]
    [int] $LocalPort = 58377,
    [string] $NexusUser = $env:nexusUser,
    [string] $NexusPass = $env:nexusPass,
    [switch] $NoCache,
    [switch] $PlainOutput,
    [switch] $HelmDryRun,
    [switch] $HelmNoWait,
    [switch] $KeepDockerEnv,
    [switch] $WantStdOut
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validateSet = "    [ValidateSet('$(((Get-Content (Join-Path $PSScriptRoot psakefile.ps1)) | Where-Object { $_ -match "^task ([\w+-]+)"} | ForEach-Object { $matches[1]}) -join "','")')]"
$content =  Get-Content $PSCommandPath
if (!($content | Where-Object { $_ -eq $validateSet})) {
    (Get-Content $PSCommandPath) -replace "^    \[ValidateSet.*'default'.*", $validateSet | Out-File $PSCommandPath -Encoding ascii
    Write-Verbose "Updated tasks"
} else {
    Write-Verbose "Tasks are current"
}

$psakeMaximumVersion = "4.9.0"

if ($PSVersionTable.PSVersion.Major -lt 6) {
    if (-not (Find-PackageProvider -Name "Nuget" -Verbose:$false)) {
        Write-Host "Installing Nuget provider"
        Install-PackageProvider -Name "Nuget" -Force -Verbose:$false
    }
    else {
        Write-Host "Nuget provider already installed"
    }
}

if (-not (Get-Module -ListAvailable -Name psake)) {
    Write-Host "Installing module psake"
    Find-Module -Name psake -MaximumVersion $psakeMaximumVersion -Verbose:$false | Install-Module -Scope CurrentUser -Force -Verbose:$false
}
else {
    Write-Host "psake already installed"
}

Import-Module psake -Verbose:$false
$psake.config_default.verboseError = $VerbosePreference -eq "Continue"
Write-Verbose "`$psake.config_default.verboseError = $($psake.config_default.verboseError)"

Invoke-Psake -buildFile '.\psakefile.ps1' -taskList @($Task) -parameters @{
    dockerEnvFile = $DockerEnvFile
    dockerTag = $DockerTag
    containerName = $ContainerName
    nugetUrl = $NugetUrl
    userName = $NexusUser
    pass = $NexusPass
    localPort = $LocalPort
    noCache = [bool]$NoCache
    plain = [bool] $PlainOutput
    overrideSourceDir = ""
    keepDockerEnv = [bool] $KeepDockerEnv
    wantStdOut = [bool]$WantStdOut
    runFolder = 'CCC.CAS.Api'
} -properties @{ helmDryRun = [bool]$HelmDryRun; helmWait = ![bool]$HelmNoWait } -Verbose:$VerbosePreference -nologo -notr

if ($psake.build_success) {
    exit 0
} else {
    exit 1
}

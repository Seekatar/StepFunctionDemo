<#
.SYNOPSIS
Start ActiveMQ in a docker container

.PARAMETER AdminUser
The admin user name for ActiveMQ, has a default

.PARAMETER AdminPw
The admin password for ActiveMQ defaults to $env:ActiveMq__AdminPassword

.PARAMETER user
The admin user name for ActiveMQ, has a default

.PARAMETER pw
The admin password for ActiveMQ, defaults to $env:ActiveMq__Password

.PARAMETER DataVolume
Optional data volume to set

.PARAMETER vromeroImage
Set to run the vromero/activemq-artemis image otherwise uses webcenter/activemq

.PARAMETER Rebuild
Set to delete and restart ActiveMq container
#>
param(
    [string] $AdminUser = "artemis_admin",
    [string] $AdminPw = $env:ActiveMq__AdminPassword,
    [string] $User = "service",
    [string] $Pw = $env:ActiveMq__Password,
    [ValidateScript({!$_ -or (Test-Path $_ -PathType Container)})]
    [string] $DataVolume,
    [switch] $vromeroImage,
    [switch] $Rebuild
)

Set-StrictMode -Version Latest
$prevErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = "Stop"

if (!$AdminPw -or !$Pw) {
    Write-Warning 'You must supply AdminPw and Pw, or set $env:ActiveMq__AdminPassword and $env:ActiveMq__Password)'
    return
}

try {

    if (![bool](docker network ls | Select-String \w+\s+services)) {
        Write-Information "Creating 'services' network in Docker" -InformationAction Continue
        docker network create services
    }

    if ([bool](docker ps --format '{{.Names}}' | Select-String activemq))  {
        if ($Rebuild) {
            Write-Information "Stopping activemq container" -InformationAction Continue
            docker stop activemq
        } else {
            Write-Information "activemq already running" -InformationAction Continue
            return
        }
    }

    $exists = [bool](docker ps -a --format '{{.Names}}' | Select-String activemq)
    if ($exists -and $Rebuild)  {
        Write-Information "Removing activemq container" -InformationAction Continue
        docker rm activemq
        $exists = $false
    } elseif ($exists) {
        Write-Information "Found activemq, starting instead of running it" -InformationAction Continue
        docker start activemq
    }

    if (!$exists) {

        # tried to splat, but couldn't get it to split correctly
        $volumes = @{}
        if ($DataVolume) {
            $volumes['v'] = "c:/temp/activemq-kahadb:/data"
        }

        if (!$vromeroImage) {
            # https://hub.docker.com/r/webcenter/activemq
            if ($DataVolume) {
                docker run -d `
                    --hostname activemq `
                    --name activemq `
                    -e "ACTIVEMQ_ADMIN_LOGIN=$AdminUser" -e "ACTIVEMQ_ADMIN_PASSWORD=$AdminPw" `
                    -e 'ACTIVEMQ_CONFIG_DEFAULTACCOUNT=false' `
                    -e 'ACTIVEMQ_NAME=localhost' `
                    -e "ACTIVEMQ_OWNER_PASSWORD=$Pw" `
                    -e "ACTIVEMQ_OWNER_LOGIN=$User" `
                    -e "ACTIVEMQ_ENABLED_AUTH=true" `
                    -p 8161:8161 `
                    -p 61616:61616 `
                    -p 61613:61613 `
                    -v "$($DataVolume -replace '\\','/'):/data" `
                    --network services `
                    webcenter/activemq:5.14.3
            } else {
                docker run -d `
                    --hostname activemq `
                    --name activemq `
                    -e "ACTIVEMQ_ADMIN_LOGIN=$AdminUser" -e "ACTIVEMQ_ADMIN_PASSWORD=$AdminPw" `
                    -e 'ACTIVEMQ_CONFIG_DEFAULTACCOUNT=false' `
                    -e 'ACTIVEMQ_NAME=localhost' `
                    -e "ACTIVEMQ_OWNER_PASSWORD=$Pw" `
                    -e "ACTIVEMQ_OWNER_LOGIN=$User" `
                    -e "ACTIVEMQ_ENABLED_AUTH=true" `
                    -p 8161:8161 `
                    -p 61616:61616 `
                    -p 61613:61613 `
                    --network services `
                    webcenter/activemq:5.14.3
            }
        } else {
            # This one different UI, a bit harder to see stuff.
            # https://github.com/vromero/activemq-artemis-docker/blob/master/README.md
            docker run --rm `
                    --hostname activemq `
                    --name activemq `
                    -e "ACTIVEMQ_ADMIN_LOGIN=$AdminUser" -e "ACTIVEMQ_ADMIN_PASSWORD=$AdminPw" `
                    -p 8161:8161 `
                    -p 61616:61616 `
                    -e ARTEMIS_USERNAME=$User `
                    -e ARTEMIS_PASSWORD=$Pw `
                    --network services `
                    vromero/activemq-artemis
        }
    }

    Write-Information "Console available at http://localhost:8161/console"  -InformationAction Continue

} finally {
    $ErrorActionPreference = $prevErrorActionPreference
}

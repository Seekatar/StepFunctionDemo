<#
psakefile for building and running dotnet app in docker

Invoke like this:

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
} -Verbose:$VerbosePreference
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSUseDeclaredVarsMoreThanAssignments", "", Justification = "Vars in properties block are actually used")]
param()

Set-StrictMode -Version latest

TaskSetup {
    Write-Host "##[section]Starting: Build task '$TaskName'"
}
TaskTearDown {
    Write-Host "##[section]Finishing: Build task '$TaskName'"
}

properties {
    $dockerPlain = $true
    if (Test-Path variable:plain) {
        $dockerPlain = $plain
    }
    if ($overrideSourceDir) {
        $sourceDir = $overrideSourceDir
    } else {
        $baseDir = $PSScriptRoot
        $sourceDir = (Join-Path $baseDir "src")
    }

    $aspNetPort = 8080
    $ASPNETCORE_URLS="http://+:$aspNetPort"

    $NUGET_SOURCE="https://api.nuget.org/v3/index.json"
    $dockerTag = $dockerTag.ToLowerInvariant()
    if ("ci" -in $taskList) {
        if ($env:sonarQubeToken) {
            $dockerfile = "SonarQube.dockerfile"
        } else {
            Write-Warning "CI Build doesn't have `$env:sonarQubeToken set."
        }
    } else {
        $dockerfile = "Dockerfile"
    }
    if (!(Test-Path (Join-Path $sourceDir $dockerfile))) {
        throw "Couldn't find dockerfile: $(Join-Path $sourceDir $dockerfile)"
    }

    $helmDryRun = $false
    $helmWait = $true
}

task default -depends DockerBuild, DockerInteractive
task ci -depends DockerBuild

task UpdateRunPs1 {
    "Does nothing, but run.ps1 ValidateSet should be updated"
}

task DumpVars {
    "##[group]==== Variables ===="
    Get-ChildItem variable: | Select-Object name, value
    "##[endgroup]"
    "##[group]==== Environment ===="
    Get-ChildItem env: | Where-Object name -NotLike *passw* | Select-Object name, value
    "##[endgroup]"
}

task DotnetBuild {
    exec -workingDirectory $sourceDir {
        dotnet build
    }
}

function makeProcessEnvName
{
    $PWD -replace '[:/\\]','-'
}

task StartBack {
    exec -workingDirectory (Join-Path $sourceDir $runFolder) {
        $psi = new-object 'System.Diagnostics.ProcessStartInfo' -arg 'dotnet','run'
        $psi.WorkingDirectory=$pwd
        if (!$wantStdout) {
            $psi.RedirectStandardError = $true
            $psi.RedirectStandardOutput = $true
        }
        $process = [System.Diagnostics.Process]::Start($psi)
        ">>>>>"
        ">>>>>"
        ">>>>> Started background process with id $($process.id)"
        ">>>>> Use ./run.ps1 StopBack to stop"
        ">>>>> Url should be http://localhost:$localPort/swagger/index.html"
        ">>>>>"
        if (!$wantStdout) {
            ">>>>> Stdout and stderr suppress. Use -WantStdOut if you want to see it."
        }
        ">>>>>"
        "Set $(makeProcessEnvName) to $($process.id)"
        [System.Environment]::SetEnvironmentVariable( (makeProcessEnvName), $process.id)
    }
}

task StopBack {
    exec -workingDirectory (Join-Path $sourceDir $runFolder) {
        $envName = (makeProcessEnvName)
        $processId = [System.Environment]::GetEnvironmentVariable($envName)
        if ($processId) {
            Remove-Item env:$envName
            $process = Get-Process -Id $processId -ErrorAction Ignore
            if ($process -and $process.Name -eq 'dotnet') {
                Stop-Process -Id $processId
            } else {
                "Found process with id $processId, but it's not dotnet"
            }
        } else {
            "Didn't find environment ($envName) variable indicating a .\run.ps1 StartBack was started in this prompt"
        }
    }
}

task TestClient {
    exec -workingDirectory $sourceDir/TestClient {
        dotnet run
    }
}

task GetReady {
    Write-Host "`n-----`nGetting http://localhost:$localport/health/ready"
    $result = Invoke-WebRequest "http://localhost:$localport/health/ready"
    Write-Host "-----`nStatus = $($result.StatusDescription) ($($result.StatusCode))"
    Write-Host ($result.Content | ConvertFrom-Json | Out-String)
}

task IntegrationTest {
}

task UnitTest {
    exec -workingDirectory $sourceDir/UnitTest {
        dotnet test
    }
}

task OpenSln {
    exec -workingDirectory $sourceDir {
        Start-Process (Get-Item *.sln)
    }
}


function Get-NugetSource
{
    Set-StrictMode -Version Latest
    $ErrorActionPreference = 'Stop'

    $sources = @()
    $inSource = $false
    $source = $null

    dotnet nuget list source | ForEach-Object {
        if ($_ -match "\s+\d+\.\s+([\w-\.]+) \[(.*)\]") {
            $inSource = $true
            $source = @{
                name = $Matches[1]
                enabled = $Matches[2] -eq 'Enabled'
                testedAndExists = $false
                path = ''
            }
        } elseif ($inSource) {
            $inSource = $false
            $source.path = $_.Trim()
            $sources += [PSCustomObject]$source
            $source = $null
        } else {
            $inSource = $false
        }
    }
    $sources
}

task DockerBuild {
    if ($userName -and $pass -and $nugetUrl) {
        $parms = @()
        if ($noCache) {
            $parms += "--no-cache"
        }
        if ($dockerPlain -or 'ci' -in $taskList) {
            $parms += "--progress"
            $parms += "plain"
        }
        if ($env:sonarQubeToken) {
            $parms += "--build-arg"
            $parms +=  "sonarQubeToken=`"$env:sonarQubeToken`"" `
        }
        $version = ($dockerTag -split ':')[1]
        $image = ($dockerTag -split ':')[0]
        "Dockerfile is $Dockerfile version is $version"
        exec -workingDirectory $sourceDir {
            if ('ci' -notin $taskList) {
                if (!(Test-Path 'packages/C*.nupkg')) {
                    # we don't have packages folder, but is there one?
                    if ((Get-Item *.sln).Name -match "(.*)(Api|Service)\.sln") {
                        Write-Verbose "Found SLN for $($Matches[1] | out-string)"
                        $source = Get-NugetSource | Where-Object { $_.name -eq "$($Matches[1])Messages" -and $_.enabled }
                        if ($source) {
                            Write-Verbose "Copying from $($source.path) to .\packages\"
                            # TODO not cross-platform
                            xcopy $source.path .\packages\ /Y
                        } else {
                             Write-Warning "Couldn't find NuGet source for $($Matches[1])Messages. Will try other nuget sources."
                        }
                    } else {
                        throw "No sln found in $sourceDir"
                    }
                }
            } else {
                Write-Warning "CI build skipping message build"
            }
            # always put a placeholder in packages for Docker CI or EasyButton
            if (!(Test-Path 'packages')) {
                $null = New-Item packages -ItemType Directory
            }
            "placeholder" | Out-File 'packages/placeholder.nupkg'

            docker build --rm `
                         --tag $dockerTag `
                         --tag "${image}:latest" `
                         --build-arg userName="$userName" `
                         --build-arg pass=$pass `
                         --build-arg nugetUrl=$nugetUrl `
                         --build-arg version=$version `
                         --file $dockerfile `
                         @parms `
                         .
            "Created image with tag: $dockerTag and ${image}:latest"
        }
    } else {
        Write-Error "Must supply username, pass, and nugetUrl to build Docker image"
    }
}

function GetSharedAppsettings
{
    [CmdletBinding()]
    param()

    Set-StrictMode -Version Latest

    $folder = Split-Path $PWD -Parent
    while ($folder -and !(Test-Path (Join-Path $folder shared_appsettings.json))) {
        $folder = Split-Path $folder -Parent
    }
    if ($folder -and (Test-Path (Join-Path $folder shared_appsettings.json))) {
        Write-Verbose "Found $folder/shared_appsettings.json"
        return Join-Path $folder shared_appsettings.json
    } else {
        Write-Warning ">>> Couldn't find shared_appsettings.json above $PWD"
        Write-Warning ">>> Active MQ etc. won't work."
        Write-Warning ">>> See README.md"
        throw "Couldn't find shared_appsettings.json above $PWD. See README.md"
    }
}

task HelmDependencyBuild {
    exec -workingdir $PSScriptRoot/build/helm {
        Remove-Item Chart.lock
        helm dependency build
    }
}

task HelmInstall {
    $parms = @()
    if ($helmWait) {
        $parms += "--wait" # default wait is 5m0s
    }
    if ($helmDryRun) {
        $parms += "--dry-run"
    }
    $sets = @(
        '--set', 'cas-service.image.tag=latest',
        '--set', 'cas-service.image.repository=""',
        '--set', 'cas-service.image.pullPolicy=Never',
        '--set', 'cas-service.deployFlow=false'
    )

    exec {
        $temp = New-TemporaryFile
        $vars = ConvertFrom-Json (Get-Content (GetSharedAppsettings) -Raw)
        $env:APPNAME="test"
        $env:BASEPATH="/basePath"
        $env:INGRESS_HOST_INT="ccc.int.com"
        $env:INGRESS_HOST_API="ccc.api.com"
        $vars | Get-Member -MemberType NoteProperty | ForEach-Object { Set-Item -Path "ENV:$($_.name)" -value $vars.($_.name) }
        $ExecutionContext.InvokeCommand.ExpandString((Get-Content .\values.yaml -Raw)) | Out-File $temp -encoding ascii
        try {
            "Helm temp file is $temp"
            helm upgrade --install --values $temp $containerName @sets . @parms
        } finally {
            if (!$keepDockerEnv) {
                Remove-Item $temp
            }
        }
    } -workingdir $PSScriptRoot/build/helm
}

task HelmUninstall {
    exec { helm uninstall $containerName }
}

function CreateEnvList {
    $dockerEnvList = "./env.list.tmp"
    $envList = @{}
    if (Test-Path ../env.list) {
        Get-Content ../env.list -ReadCount 0 | ForEach-Object {
            $equals = $_.IndexOf('=')
            if ($equals -gt 0 ) {
                $envList[$_.SubString(0,$equals)] = $_.SubString($equals+1)
            }
        }
    }

    $sharedSettings = GetSharedAppsettings
    if ($sharedSettings) {
        $config = Get-Content $sharedSettings -Raw | ConvertFrom-Json
        $config | Get-Member -MemberType NoteProperty |
        ForEach-Object {
            $value = $config."$($_.Name)" -replace '(localhost|127.0.0.1|::1)','host.docker.internal'
            $envList[$_.Name -replace ':','__']=$value
        }
    }

    $launchSettings = @(Get-ChildItem .\launchsettings.json -r | Where-Object FullName -notlike '*TestClient*')
    if ($launchSettings.Count -gt 1) {
        Write-Warning "Merging more than one non-test launchSettings.json into env.list"
        Write-Warning "   $($launchSettings -join ', ')"
    } elseif ($launchSettings) {
        foreach ($ls in $launchSettings) {
            $settings = Get-Content $launchSettings[0] | ConvertFrom-Json
            try {
                $config = $settings.profiles.Kestrel.environmentVariables
            } catch {
                throw "Error trying to process profiles.Kestrel.environmentVariables in $($launchSettings[0])"
            }
            $config | Get-Member -MemberType NoteProperty |
            ForEach-Object {
                $value = $config."$($_.Name)" -replace 'localhost','host.docker.internal'
                $envList[$_.Name -replace ':','__']=$value
            }
        }
    }
    $envList.Keys | ForEach-Object { "$_=$($envList[$_])" } | Out-File $dockerEnvList -Encoding ascii
    $dockerEnvList
}

function runDocker( $extraParams ) {
    $imageName = $dockerTag -replace ":.*",":latest"
    "Attempting to run image: $imageName named $containerName. Container id follows."
    ">>>>"
    ">>>>"
    ">>>> URL will be http://localhost:$localPort/swagger"
    ">>>>"
    ">>>>"
    exec -workingDirectory $sourceDir {
        $envList = CreateEnvList
        docker run --rm `
                   --publish "${localPort}:$aspNetPort" `
                   --env "ASPNETCORE_URLS=$ASPNETCORE_URLS" `
                   --env-file $envList `
                   --interactive `
                   --tty `
                   --name $containerName `
                   --network services `
                   @extraParams `
                   $imageName

        if (!$keepDockerEnv) {
            Remove-Item $envList
        } else {
            "Keeping env file '$envList'"
        }
    }
}

task DockerRun {
    runDocker @("--detach")
    ">>>>"
    ">>>> Container id should be above."
    ">>>> If the container isn't running, use .\run.ps1 DockerInteractive to get output"
    ">>>>"
}

task DockerInteractive {
    runDocker @("--interactive","--tty")
}

task DockerStop {
    docker stop $containerName
}

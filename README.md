//#if (ServiceType == 'REST')
# Workflow4 REST API Service
//#elseif (ServiceType == 'RESTStandAlone')
# Workflow4 Stand Alone REST API Service
//#else
# Workflow4 Backend Service
//#endif

[[_TOC_]]

> Note this sample is designed to get you up and running quickly with code to cover most scenarios. Before committing your repo, make sure you remove any code you do not intend to use such as sample controllers and consumers. Your reviewers will thank you.

//#if (ServiceType != 'Backend')
This is a REST API project created from the Casualty [dotnet template](https://dev.azure.com/CCC-Casualty/Reliance/_git/CAS-Template-Service).

This API is designed to call a backend service of the same name (Workflow4) and will use ActiveMQ to call it.

**IMPORTANT:** By default this project is paired with a `Workflow4` backend project. That project creates a local nuget source for the Messages both project use. If you don't need a backend service, use `--ServiceType RESTStandAlone` in dotnet new.

//#else
This is a backend service project project created from the Casualty [dotnet template](https://dev.azure.com/CCC-Casualty/Reliance/_git/CAS-Template-Service).

This service is consumes ActiveMQ messages for Workflow4 objects. The included TestClient project calls it, and if you create a matching Workflow4 API service, it will call it.

//#if (EnableMongo)
This app supports Mongo. Make sure you install [MongoDb](https://fastdl.mongodb.org/windows/mongodb-windows-x86_64-4.4.4-signed.msi) locally or edit the `appsettings.json`
//#endif

//#if (EnableSqlProxy)
This app supports CCC's SqlProxy to access SQL Server 2008 (MedFlow). Make sure you install [SqlProxy Example](https://dev.azure.com/CCC-Casualty/Reliance/_git/sql-proxy-sample) locally or edit the `appsettings.json`. You can avoid SqlProxy just for a test using json-server, see [below](#Using-json-server-instead-of-SqlProxy)
//#endif
//#endif

## Getting Started with Your Service

Make sure ActiveMQ is running with `./tools/Start-ActiveMqDocker.ps1`. From your root folder, use the `run.ps1` script to build and run in Docker (See [below](#using-podman-in-place-of-docker) for using [Podman](https://docs.podman.io/) instead of Docker)

``` PowerShell
# You can set $env:nexusUser and $env:nexusPass to avoid passing it in each time
.\run.ps1 -NexusUser myuser -NexusPass mypass
```

//#if (ServiceType != 'Backend')
Now you can hit the Swagger UI at [http://localhost:58377/swagger](http://localhost:58377/swagger/index.html). Woohoo! Depending on your selection of auth, you may have to provide headers or JWT to hit the Workflow4 endpoints on the API. Most of the calls send ActiveMQ messages to a Workflow4 service.
//#else
Now you can hit the Swagger UI at [http://localhost:58377/swagger](http://localhost:58377/swagger/index.html) for health checks. To exercise the service run the TestClient or create a matching API service with the template.
//#endif

### Using with Visual Studio

* Open the sln in VisualStudio and build it.
* Run the Unit tests
* Run ActiveMQ locally. `.\tools\Start-ActiveMqDocker.ps1` will start one (see below if you have your own).
//#if (ServiceType == 'Backend')
* It may be easiest to set the service and TestClient projects to start via the sln's `Set Startup Projects...` dialog. That way everything will start up for testing.
* If you enable SQL Proxy, it will have to be running for those features to work. You can get it from its [repo here](https://dev.azure.com/CCC-Casualty/Reliance/_git/sql-proxy-sample), which has directions for running it.
//#endif
* From Swagger, or the browser, make sure the site reports 200 from http://localhost:58377/health/ready with output like this:

``` json
{
"description": "CCC.CAS.*Service",
"version": "1",
"releaseId": "1.0.0.0",
"status": "pass",
...
```

## Using run.ps1

`.\run.ps1 <taskname>` has several task for building and running the project. You can run multiple tasks in order by passing them in on the command line.

To use DockerBuild, you must pass in your Nexus creds so the container access it as shown above.

| TaskName             | Description                                                                                  |
| -------------------- | -------------------------------------------------------------------------------------------- |
| default (or nothing) | Does default tasks for building and running locally in Docker                                |
| ci                   | Does ci build tasks, currently just DockerBuild                                              |
| DockerBuild          | Runs docker build                                                                            |
| DockerRun            | Runs docker run detached                                                                     |
| DockerInteractive    | Runs docker run with interactive flags, for debugging                                        |
| DockerStop           | Stops docker if started with DockerRun                                                       |
| DotnetBuild          | Runs dotnet build on the solution                                                            |
| DotnetTest           | Runs dotnet test on the solution                                                             |
| DumpVars             | Helper to dump out variables and environment. Useful for debugging                           |
| HelmInstall          | Run helm install locally on the service. Can dry run and wait                                |
| HelmUninstall        | Run helm uninstall locally on the service                                                    |
| BuildMessage         | Build the Message nuget package used by the service                                          |
| OpenSln              | Opens the sln in the associated app (VS)                                                     |
| UpdateRunPs1         | Helper for updating the run.ps1 if you add Tasks to psakefile.ps1                            |
| StartBack            | Starts the service in the background                                                         |
| StopBack             | Stops the service started with StartBack -- Must be in the same prompt that called StartBack |
//#if (ServiceType == 'Backend')
| TestClient           | Runs the TestClient                                                                          |
//#endif

## Tour of the Code

```text
├───build                            # files supporting the build and deploy
//#if (ServiceType != 'Backend')
├───OAS-API                          # OAS specification for generating API code
//#endif
├───src
//#if (ServiceType != 'Backend')
│   ├───CCC.CAS.Workflow4Api # REST API that calls the backend Workflow4Service via AMQ
│   │   ├───Controllers              # Initially generated controllers
│   │   ├───Installers               # DI Service installers
│   │   ├───Interfaces               # Services Interfaces
│   │   └───Services
//#endif
//#if (ServiceType == 'Backend')
│   ├───CCC.CAS.Workflow4Messages     # Shared Message and Models.
│   │   ├───build                    # files supporting the build and deploy for Messages
│   │   ├───Messages                 # Commands and Events
│   │   └───Models                   # Classes sent in Commands and Event
│   ├───CCC.CAS.Workflow4Service      # Backend service that talks AMQ and SQL Proxy
│   │   ├───Consumers                # Command and Event consumers
│   │   ├───Installers               # DI Service installers
│   │   ├───Interfaces               # Repository Interfaces
│   │   └───Repositories
│   ├───IntegrationTest              # Integration tests, code also used by TestClient
│   ├───TestClient                   # CLI app for triggering Commands and dumping Events
│   └───UnitTest
│       └───ServiceBus               # Unit tests for MassTransit since it can be tricky
│   └───packages                     # output folder for Messages nuget package
//#else
├───tests
│   └───integration                  # Pester tests scripts for REST service
//#endif
└───tools                            # Scripts for calling REST, starting ActiveMQ
```

//#if (ServiceType != 'Backend')

## Jump on the **API First** Bandwagon

Instead of just creating controllers to build your API, it is better to design it and its models first. The `OAS-API` folder has an [OAS standard](https://swagger.io/specification/) yaml file that defines the API used in the API project. View the [README](OAS-API/README.md) in that folder for details about editing the API and generating code from it. The model and controllers in the sample were generated using that method.

## Authentication and Authorization in the API Project

In addition to `none`, there are three supported authentication methods for ASP.NET. The recommended OktaGroupPolicyHandler and OktaScopeHandler methods use Okta JWT tokens. The ApigeeProxyAuthenticationHandler uses HTTP headers, and is not recommended.  Each requires different configuration and `[Authorize]` attributes all detailed in Common's [README.md](https://dev.azure.com/CCC-Casualty/Reliance/_git/CAS-Common).

`dotnet new` requires a value for the method you want to use and then configures the API settings and code for that method. The other methods will be disabled or commented out so you can see how to use them.

The sample Workflow4Api.cs file's GET and POST methods require Authorization. Swagger will have an `Authorize` button that will prompt to set a JWT. Also the integration tests will detect which one is active a set the headers.

//#if (AuthType == 'OktaGroupPolicyHandler')
### AuthType 'OktaGroupPolicyHandler'

This handler is for REST APIs that are called from a browser client with users logging in. It requires some setup in Okta, which is automated for RBR and DataCapture, and sample users are setup by default. To call the API in Swagger you will need JWT. You can use the [OktaPosh](https://www.powershellgallery.com/packages/OktaPosh/) PowerShell module to create one with a command like this.

```powershell
$userName = 'fflintstone@mailinator.com'
$pw = 'Test123!'
$scopes = 'openid','email','casualty.TestSPA.client.usaa'
$jwt = Get-OktaJwt -ClientId 0oaxotrcezjWplocQ0h7 -Issuer https://devauth.cccis.com/oauth2/ausxotrccziikg7uh0h7 -RedirectUri http://localhost:8008/fp-ui/implicit/callback -Username $userName -Pw $pw -Scopes $scopes
```
//#elseif (AuthType == 'OktaScopeHandler')
### OktaScopeHandler

This handler is for REST APIs that are called from another, internal service. Like above, it requires some setup in Okta, which is automated for RBR and OCR. You will have to get a JWT to pass to Swagger or set in the environment for integrated tests. To call the API in Swagger you will need JWT. You can use the [OktaPosh](https://www.powershellgallery.com/packages/OktaPosh/) PowerShell module to create one with a command like this.

```powershell
# this is the CCC-CASTestGateway-Writer
$jwt = Get-OktaAppJwt -ClientId 0oaxot5zg3YVNPZU90h7 -ClientSecret JNZMHMnzrbhYnxbtuyEB18FG9Ph00KMIjAA4PzRN -Issuer https://devauth.cccis.com/oauth2/ausxotepxz0xAOPE40h7 -Scopes get:item,access:token
```
//#elseif (AuthType == 'none')
### AuthType 'none'

If you create the API with `none` as the `AuthType` then there is no auth for any of the controllers and there will be no identity. Use this if you will roll your own.
//#else

### ApigeeProxyAuthenticationHandler

This is provided for older services that don't use Okta. For Authorization, only the existence of the RequiredHeaders is checked. Swagger will indicate which ones are required. In Swagger, you need only provide values for those to authenticate and authorize. You can use this if you don't use JWT and only use headers.
//#endif

To use other types of auth, see the service template's source, or create a sample service using another type.

## Troubleshooting the API

* If you have build issues related to a Messages dll or packages folder, it's probably not finding the CCC.CAS.Workflow4Messages source. That is created by a matching backend service. Or if you don't need messages in the API, you can `--ServiceType RESTStandAlone` in dotnet new.
* 401 Indicates you aren't authenticated. (The GET api/Workflow4 requires AUTHN)
  * Make sure you are passing the correct headers for the auth method you've configured
  * Make sure the values are correct. For JWT, you make sure it's valid and not expired. You can view a JWT on [JWT.io](http://jwt.io) Debugger tab
  * In Swagger pass in the JWT after 'Bearer' See above for using OktaPosh to get it it.
* 403 Indicates you aren't authorized. So the headers are ok, but for JWT types, you don't have the right scopes or groups needed.
  * Make sure you created the JWT correctly, as describe above using OktaPosh.
  * Use [jwt.io](https://jwt.io)'s Debugger to look at the JWT to make sure it's what you expect.
* Other auth errors can occur if/when you switch to your own Okta App and Auth server since you must update `issuer`, `authority`, and `audience` in the configuration.
* Exception about wrong auth handler at startup. You need to make sure the [Authorize] attribute matches what you've configured in `appsettings.json` `AuthHandlerName` value.
* ActiveMQ timeout
  * Make sure a Docker container called `activemq` is running and has ports 8161, 61616, and 61613 mapped locally.
  * If it's a login issue, make sure the credentials you created AMQ with match the service's.
  * Check the health/ready endpoints to make sure they return 200. If not check your logs and configuration.
  * Make sure the Service is running, since the API calls it via ActiveMQ

//#elseif

## The TestClient

* The `TestClient` sends AMQ messages to the service. It can send `SaveWorkflow4` and `GetWorkflow4` commands, and subscribes to the `Workflow4Saved` event and logs it to the screen on every save. For example, when you press `S` a command is sent to the service that fires a `Workflow4Saved` event, then the TestClient logs a message:

```PowerShell
This is a test client for the message-driven Workflow4 application.
Press a key: (S)aveWorkflow4 (G)etWorkflow4 (P)ublishWorkflow4Saved (E)choTest (Q)uit
Sent save command
Workflow4SavedConsumer - saved: Name = ABC123 Id: ABC123 Id: 62297002-db59-4f8b-9dd5-cf2f38c0fe7a
```

## Troubleshooting the Service

* Make sure a Docker container called `activemq` is running and has ports 8161, 61616, and 61613 mapped locally.
* Check the health/ready endpoints to make sure they return 200. If not check your logs and configuration.
* Make sure the Service is running, since the API calls it via ActiveMQ

//#endif

## Troubleshooting Builds

* `=> ERROR [internal] load metadata for mcr.microsoft.com/dotnet/sdk:5.0-focal` sometime this transient error will occur when building in Docker. Check your network and try again.

## The Messages Project and NuGet Package

The Workflow4Messages project builds a NuGet package of messages used by the backend service and other services that talk to the backend service (like the API). When you use `.\run.ps1 BuildMessage -Version 1.0.0.0` command will create a local NuGet source and build the NuGet package.

> You can build Messages in VisualStudio, but the script handles creating and updating a local NuGet source for the package, as well as updating the reference in the backend service.

Once you have it working locally fine, create an Azure DevOps pipeline with ./CCC.CAS.Workflow4Messages/build/build.yml. When built in Azure DevOps, you can then update packages from the Nexus repo.

### Updating Messages

As you make changes to messages, you'll have to update references to them. Getting the correct version number will make life easier. Use this process to determine the value.

1. If published, get version number from last AzDO build (e.g. 1.0.5)
2. If only localm get the version referenced by the csproj the uses message (Service or API)
3. Use a higher version than that, preferably using the forth level, e.g. 1.0.5.1

Then build the local nuget package with `.\run.ps1 BuildMessage -Version 1.0.5.1`. This will automatically update a service project in the same sln. You'll have to manually update any other services such as the API with one of these methods:

* VS GUI: Select the CAS-Workflow4-Messages source and update
* VS Package Manager console: `update-package -source CAS-Workflow4-Messages`
* dotnet cli from csproj folder: `dotnet add package CCC.CAS.Workflow4Messages --source CAS-Workflow4-Messages`

Once tested, commit and push the message changes and create a PR -- DO NOT push the service changes since they have a local nuget reference. When the PR's CI build is finished, update the service to use the remote nuget package, and commit.

## Configuration

Configuration settings are applied from multiple sources. Each of the following sections describe each source with the subsequent section overriding any previous ones.

### Shared Settings

A CCC-specific source used only for local development, the services use `shared_appsettings.json` to store passwords and secrets outside of any repo. See [this](https://dev.azure.com/CCC-Casualty/Reliance/_wiki/wikis/Reliance.wiki/307/Workflow4-based-Projects-Updates) wiki page for details about getting access to the default file. The code looks in parent folders until it finds a file. These settings should be in that file.

| Name               | Default   |
| ------------------ | --------- |
| ActiveMq: Host     | localhost |
| ActiveMq: Username | service   |
| ActiveMq: Password | secret    |

## AppSettings.json

Values in `appsettings.json` are ones that usually are set at deploy time since they are the same across all environments. In addition to these there are ones for auth. See the Workflow4's API settings files, and Common library's [README.md](https://dev.azure.com/CCC-Casualty/Reliance/_git/CAS-Common) for details.

| Name                       | Description                           | Default                 |
| -------------------------- | ------------------------------------- | ----------------------- |
| ActiveMq: Disabled         | Set to true to turn off core ActiveMQ | false                   |
| ActiveMq: RetryCount       | For retry of consumers                | 5                       |
| ActiveMq: RetryInterval    | For retry of consumers                | 2000                    |
| SqlProxy: Disabled         | Set to true if turn off core SqlProxy | true                    |
| SqlProxy: BaseUri          |                                       | [http://localhost:5000] |
| SqlProxy: HttpRetry        | Polly retry policy                    | 3                       |
| SqlProxy: HttpRetryDelayMs | Polly retry policy                    | 3000                    |

**IMPORTANT** if you disable ActiveMQ, be sure to remove the classes that inject `IBusControl` , otherwise you will DI errors when those classes are instantiated.

## AppSettings.Development.json

Only for development to override `appsettings.json`. The sample has overrides for logging to make it more human readable.

### Properties/LaunchSettings.json

Since we deploy to Kubernetes, all parameters that may be different in an environment (Dev, QA, Prod, etc.) are set via the environment variables. To make sure things work in VS, use the `environmentVariables` section of `launchsettings.json` instead of `appsettings.json`.

Any parameters which do not need to be different per environment, can be set in `appsettings.json` (and of course can be overridden by environment variables at runtime).

### Command Line

By default, the last config source is the command line, but since we use environment variables, this is not used.

## CI/CD

After you get the initial code running locally, you'll want to build and deploy it -- baby steps. I _highly_ recommend that you start CI immediately after your initial commit to the repo, and CD to Dev also.

## Building

1. Create a Git Repo in AzDO for your new project, e.g. `CAS-Workflow4-Api`
1. Commit the code locally, add the remote and push per the AzDO directions.
1. In AzDO, create a new pipeline
1. Select `Azure Repos Git` for your code source.
1. Select your new repo
1. Pick `Existing Azure Pipelines with YAML file`
1. Select `/build/build.yml`
1. Save the yml, then click Edit and then Validate to make sure it's ok
1. IMPORTANT - Rename the pipeline to CAS-Workflow4-Service-Build so the deploy will be triggered by it.
1. Click Run and enjoy!
1. After things building and deploying ok, you'll want to create a policy on your `main` (yes, main) branch to avoid commits directly to that branch.

The `/build/build.yml` should work as-is. As you update your project you may need to alter it. The yml uses DevOps templates that call same `.\run.ps1` script that you can run locally so you have higher confidence that it will work.

## Deploying in Azure DevOps to Kubernetes

This is very similar to the previous step. You will want to update the `deploy.yml` for your environment variables and secrets. By default this deploys to the RBR AWS Kubernetes Environments all the way through prod. The Environments in the deploy are configured in AzDO with approvers. Adjust the yaml as necessary.

1. In AzDO, create a new pipeline
1. Select `Azure Repos Git` for your code source.
1. Select your new repo
1. Pick `Existing Azure Pipelines with YAML file`
1. Select `/build/deploy.yml`
1. Save the yml, then click Edit and then Validate to make sure it's ok
1. Click Run and enjoy!
1. By default, this will also be triggered by the build pipeline above, which is why naming it is important.

### Creating a Variable Group in the Azure DevOps Library

Each environment you have will have different settings, such as connection strings, ports, passwords, etc. The Release pipeline will use one or more Libraries for each Stage that have those environment-specific values.

1. Click the Library icon in AzDO
2. Create your a Variable Group with your variables, such as connection strings, etc. For naming, it's best to use all caps with underscores so the name match generated environment variables. For values to override in `appSettings.json` use double underscore to separate levels in the JSON. E.g `{ActiveMq: {Host: "value"}}` would be `ACTIVEMQ__HOST = "value"`

These are the values you'll use in the `deploy.yml`, `configMapProperties` and `secretProperties`

## Running in Local Kubernetes

Although Docker usually suffices for validating containers, it's possible to run locally in Kubernetes. The `build/helm` folder has a chart for running the service locally in Kubernetes. You will need to also run ActiveMQ in Kubernetes and currently Docker and Kubernetes deployments don't play together due to separate network stacks. `run.ps1` has `helmInstall` and `helmUninstall` tasks. As with Docker, helm will look for `shared_appsettings.json` for secrets like the ActiveMQ password.

If you don't want to use helm, the deploy.yml uses the [DevOps-Templates](https://dev.azure.com/CCC-Casualty/Reliance/_git/DevOps-Workflow4s) repo to create the manifests that can be deployed.

## Etc

//#if (EnableSqlProxy)

### Using json-server instead of SqlProxy

If you want to avoid running SqlProxy (the [example](https://dev.azure.com/CCC-Casualty/Reliance/_git/sql-proxy-sample) is pretty easy to run), you can use [json-server](https://github.com/typicode/json-server). First create `jsondb.json` and `routes.json` files with the content below:

jsondb.json

```JSON
{
    "api": [
        {
            "id": "Echo",
            "parm": {
                "message": "hi there from json!",
                "name": "Fred",
                "Client": "Client"
            }
        }
    ],
    "health": [
        {
            "id": "ready",
            "message": "this is from json-server"
        }
    ]
}
```

routes.json

```JSON
{
    "/api/Echo/CONNA/:msg": "/api/Echo"
}
```

Then install and run json-server with these parameters

```PowerShell
npm install -g json-server
json-server --watch .\jsondb.json --port 5000 --routes .\routes.json
```

//#endif

### Disabling ActiveMQ

By default the sample uses ActiveMQ. To disable, turn if off in `appsettings.json` and remove the respective repositories files.

### Running an Existing ActiveMQ

`shared_appsettings.json` file will have ActiveMQ settings. If you have a different ActiveMQ, adjust those settings as needed

### Using Podman In Place of Docker

[Podman](https://docs.podman.io/) is a drop-in replacement for Docker that runs on WSL2. This project supports using podman, but you must create a PowerShell function to redirect `docker` to podman in WSL to docker as below. Then everything will work.

You can add that to you PowerShell Profile so it's always there. Edit that file via `notepad $Profile`.

```PowerShell
function docker { wsl podman @args }
```
# OAS API
`openapi.yaml` is the Open API Specification (OAS) for this application. Swagger uses the OAS specification, so you may find it familiar. Documentation for OAS is [here](https://swagger.io/specification/)

## Editing the YAML

To edit the OAS with Swagger preview you can can do a couple things:

1. Install and run the editor locally. See the github [editor](https://github.com/swagger-api/swagger-editor) for full directions.

   TLDR;

    ```PowerShell
    # in a new folder>
    npm install swagger-editor-dist
    start \node_modules\swagger-editor-dist\index.html
    # or
    docker run --detach --publish 80:8080 --name swagger swaggerapi/swagger-editor
    start "http://localhost"
    # then stop and start it, such as after a reboot or to save resources
    docker stop swagger
    docker start swagger
    ```

    In the editor you can use File->Import or paste in your yaml.

2. Install the VS Code [Swagger Viewer](https://github.com/arjun-g/vs-swagger-viewer) by Arjun G. Use `Shift+Alt+P` to open the preview while you edit the file to make sure it's always valid.

Option 2 works well with IntelliSense, etc., except in the case of errors. In that case using option 1 will show errors in the yaml at the top of the file. I use 2, and revert to 1 only when I can't figure out the error.

## Conventions
* Use descriptions to add clarity, but don't just reword the name
* Alphabetize the paths, if possible
* Alphabetize the schema items to make them easier to find on the Swagger page, and editing.
* Provide example values so you can copy from the Swagger page for mock data.
* Keep it DRY with `$ref`

## Generating Code
After you edit the yaml, you'll want to generate C# controller and model code. The [Tools](https://dev.azure.com/CCC-Casualty/Reliance/_git/Tools) project has a `SwaggerCodeGen\New-ServerApi.ps1` script to generate the .NET Core API code. You must have Java installed to run it. It takes parameters for the yaml file and output folder, defaulting `.\openapi.yaml` and `c:\temp\swagger-gen` folder. View the script of use -help for details.

```PowerShell
\code\Tools\SwaggerCodegen\New-ServerApi.ps1 -OASFile .\openapi.yaml -OutputFolder C:\temp\swaggerapi\ -namespace CCC.CAS.Workflow4Messages.Models

# launch compare tool of your choice to compare against repo models, or controllers
bc C:\Temp\swaggerapi\src\IO.Swagger\Models C:\code\CAS-Workflow4-Api\src\CCC.CAS.Workflow4Messages\Models
```
You can also use the local web site mentioned above to generate the code. To make diffing model code easier you can run  `.\Tools\SwaggerGen\Format-Model.ps1` to do some cleanup (it's called by New-Server).

After you generate it, for controllers, it may be easiest just to copy-and-paste the code as needed since only the method signature is useful. For models, they should be pretty close after `Format-Model.ps1` runs and usually can be taken as-is.


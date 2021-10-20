param(
[Parameter(Mandatory)]
[string] $name,
[string] $baseUri = "http://localhost:60781"
)

Invoke-RestMethod "$baseUri/api/workflow4s/$name" -Method get `
        -Headers @{"X-CCC-FP-Email" = "john@mailinator.com"; "X-CCC-FP-Roles" = "admin,adjuster" }

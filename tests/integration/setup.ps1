$env:baseUrl = "http://localhost:58377"

Import-Module Pester
$pester = Get-Module Pester
if (!$pester -or $pester.Version.Major -lt 5) {
    throw "Pester v5 or higher must be installed."
}

$handler = (ConvertFrom-Json (Get-Content (Join-Path $PSScriptRoot ../../src/CCC.CAS.Workflow4Api/appsettings.json) -raw)).AuthHandlerName

if ($handler -eq 'ApigeeProxyAuthenticationHandler') {
    # default required headers for ApigeeProxy
    $headers = @{
        "X-CCC-FP-Email" = "1"
        "X-CCC-FP-Roles" = "1"
    }
} elseif (Test-Path variable:jwt) {
    $headers = @{
        "Authorization" = "Bearer $jwt"
    }
} else {
    throw "Must set global `$jwt to test $handler auth"
}

BeforeAll {
    . (Join-Path $PSScriptRoot setup.ps1)
}

Describe 'Test get' {
    It 'Gets 123' {
        $result = Invoke-RestMethod "$env:baseUrl/api/workflow4s/123" -Headers $headers
        $result.name | Should -Be "123"
    }

    It 'Posts Fred' {
        $result = Invoke-WebRequest "$env:baseUrl/api/workflow4s" -Method Post -ContentType 'application/json' -Headers $headers -Body @"
        {
            "name": "Fred"
        }
"@
        $result.StatusCode | Should -Be 201
    }

}
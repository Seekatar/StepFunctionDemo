BeforeAll {
    . (Join-Path $PSScriptRoot setup.ps1)
}

Describe 'Test echoes' {
    It 'Says hi Fred' {
        $result = Invoke-RestMethod "$env:baseUrl/api/echo/Fred" -Headers $headers
        $result.parm.client | Should -Not -BeNullOrEmpty 
        $result.parm.message -Like "*Fred*" | Should -Be $true
    }
}


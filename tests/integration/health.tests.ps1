. (Join-Path $PSScriptRoot setup.ps1)

Describe 'Gets health' {

    It 'Gets live' {
        $result = Invoke-RestMethod "$env:baseUrl/health/live"
        $result.status | Should -Be 'pass'
    }

    It 'Gets ready' {
        $result = Invoke-RestMethod "$env:baseUrl/health/ready"
        $result.status | Should -Be 'pass'
    }
}

Write-Host "Current dir is $PWD"
Write-Host "Initializing git"
git init --initial-branch main
git add .
git commit -m 'Initial commit'
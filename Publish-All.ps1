$ErrorActionPreference = 'Stop'

$projects = @(
    'YouTubeWindows\YouTubeWindows.csproj',
    'YouTubeKids\YouTubeKids.csproj',
    'YouTubeTV\YouTubeTV.csproj',
    'YouTubeMusic\YouTubeMusic.csproj'
)

$architectures = @('x86', 'x64', 'arm64')

$dotnet = Join-Path $env:LocalAppData 'Programs\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $candidatePaths = @(
        (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'dotnet\dotnet.exe')
    )

    $dotnet = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $dotnet) {
    throw 'Could not find dotnet.exe. Install the .NET SDK or add dotnet to PATH.'
}

foreach ($architecture in $architectures) {
    $runtimeIdentifier = "win-$architecture"
    $architectureRoot = Join-Path $PSScriptRoot (Join-Path 'publish' $runtimeIdentifier)

    if (Test-Path $architectureRoot) {
        Remove-Item $architectureRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $architectureRoot | Out-Null

    foreach ($project in $projects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        $publishDir = Join-Path $architectureRoot (Join-Path '_staging' $projectName)

        New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

        Write-Host "Publishing $projectName for $runtimeIdentifier to $publishDir..."
        & $dotnet publish $project `
            -c Release `
            -r $runtimeIdentifier `
            --self-contained true `
            -o $publishDir `
            -p:NoWarn=CA1416

        if ($LASTEXITCODE -ne 0) {
            throw "Publishing $projectName for $runtimeIdentifier failed with exit code $LASTEXITCODE."
        }

        Get-ChildItem -Path $publishDir -Force | ForEach-Object {
            Copy-Item $_.FullName -Destination $architectureRoot -Recurse -Force
        }
    }

    Remove-Item (Join-Path $architectureRoot '_staging') -Recurse -Force
}
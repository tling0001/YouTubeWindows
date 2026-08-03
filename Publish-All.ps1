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
    $downloadRoot = Join-Path $env:USERPROFILE 'Downloads'

    switch ($architecture) {
        'x86' { $zipName = 'YouTubeLeanbackWindows-x86.zip' }
        'x64' { $zipName = 'YouTubeLeanbackWindows-x86-64.zip' }
        'arm64' { $zipName = 'YouTubeLeanbackWindows-arm64.zip' }
    }

    $zipPath = Join-Path $downloadRoot $zipName

    if (Test-Path $architectureRoot) {
        Remove-Item $architectureRoot -Recurse -Force
    }

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
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

    Compress-Archive -Path (Join-Path $architectureRoot '*') -DestinationPath $zipPath -Force
    Write-Host "Created $zipPath"
}
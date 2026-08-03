$ErrorActionPreference = 'Stop'

$solutionDir = $PSScriptRoot

$projects = @(
    'YouTubeWindows\YouTubeWindows.csproj',
    'YouTubeKids\YouTubeKids.csproj',
    'YouTubeTV\YouTubeTV.csproj',
    'YouTubeMusic\YouTubeMusic.csproj'
)

$architectures = @(
    @{ Name = 'x86';   RuntimeId = 'win-x86';   ZipName = 'YouTubeLeanbackWindows-x86' },
    @{ Name = 'x64';   RuntimeId = 'win-x64';   ZipName = 'YouTubeLeanbackWindows-x86-64' },
    @{ Name = 'arm64'; RuntimeId = 'win-arm64'; ZipName = 'YouTubeLeanbackWindows-arm64' }
)

$publishVariants = @(
    @{ Name = 'framework-dependent'; SelfContained = $false }
)

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

foreach ($variant in $publishVariants) {
    # Create zips for each architecture for the current variant
    foreach ($architecture in $architectures) {
        $runtimeIdentifier = $architecture.RuntimeId
        $architectureRoot = Join-Path $solutionDir (Join-Path 'publish' $runtimeIdentifier)
        $downloadRoot = Join-Path $env:USERPROFILE 'Downloads'
        $zipPath = Join-Path $downloadRoot ($architecture.ZipName + '.zip')

        # Clean bin and obj folders to prevent intermediate artifact contamination across architectures
        $binDir = Join-Path $solutionDir 'bin'
        if (Test-Path $binDir) {
            Remove-Item $binDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        Get-ChildItem -Path $solutionDir -Recurse -Directory -Filter 'obj' | ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }

        if (Test-Path $architectureRoot) {
            Remove-Item $architectureRoot -Recurse -Force
        }
        if (Test-Path $zipPath) {
            Remove-Item $zipPath -Force
        }

        New-Item -ItemType Directory -Force -Path $architectureRoot | Out-Null

        foreach ($project in $projects) {
            $projectName = [System.IO.Path]::GetFileNameWithoutExtension((Split-Path $project -Leaf))
            $stagingDir = Join-Path $architectureRoot (Join-Path '_staging' $projectName)

            New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null

            Write-Host "Publishing $projectName for $runtimeIdentifier ($($variant.Name)) to $stagingDir..."
            & $dotnet publish $project `
                -c Release `
                -r $runtimeIdentifier `
                --self-contained:$($variant.SelfContained) `
                -o $stagingDir `
                -p:NoWarn=CA1416

            if ($LASTEXITCODE -ne 0) {
                throw "Publishing $projectName for $runtimeIdentifier ($($variant.Name)) failed with exit code $LASTEXITCODE."
            }

            Get-ChildItem -Path $stagingDir -Force | ForEach-Object {
                Copy-Item $_.FullName -Destination $architectureRoot -Recurse -Force
            }
        }

        Remove-Item (Join-Path $architectureRoot '_staging') -Recurse -Force

        Compress-Archive -Path (Join-Path $architectureRoot '*') -DestinationPath $zipPath -Force
        Write-Host "Created $zipPath"
    }
}
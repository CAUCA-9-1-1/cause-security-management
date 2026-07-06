<#
.SYNOPSIS
    Release gate for Cause.SecurityManagement NuGet packages.

.DESCRIPTION
    Enforces coordinated versioning across all packable projects, then
    builds, tests, packs, and pushes them to the internal CaucaNuget feed.

    Note: Cause.SecurityManagement.Wolverine is intentionally excluded from the
    published set (incomplete/unfinished). It is still built and tested as part
    of the solution but is not packed or pushed.

    Steps performed in order:
      1. VERSION GATE  — reads <Version> from each packable .csproj; aborts if
                         any project differs from the others.
      2. BUILD         — dotnet build (Release).
      3. TEST          — dotnet test (Release, no-build). Skippable via -SkipTests.
      4. PACK          — dotnet pack each packable project into ./artifacts/nupkg.
      5. PUSH          — nuget push each produced .nupkg to CaucaNuget.

    Supports -WhatIf: the push step is skipped so the full gate can be previewed
    without actually publishing packages.

.PARAMETER SkipTests
    Skip the test step. Off by default.
    Use only when you have just run the full test suite locally in this session.

.PARAMETER ArtifactsDir
    Directory where .nupkg files are written. Defaults to ./artifacts/nupkg.
    The directory is cleaned at the start of each pack run.

.EXAMPLE
    .\release.ps1 -WhatIf
    Runs the version gate, build, and tests; shows what would be pushed — without publishing.

.EXAMPLE
    .\release.ps1
    Full release: gate, build, test, pack, push.

.EXAMPLE
    .\release.ps1 -SkipTests
    Skip the test step (escape hatch — use with care).
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [switch] $SkipTests,

    [string] $ArtifactsDir = (Join-Path $PSScriptRoot 'artifacts' 'nupkg')
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Packable projects (relative to repo root)
# ---------------------------------------------------------------------------
$RepoRoot = $PSScriptRoot

$PackableProjects = @(
    'Cause.SecurityManagement.Models\Cause.SecurityManagement.Models.csproj',
    'Cause.SecurityManagement.Core\Cause.SecurityManagement.Core.csproj',
    'Cause.SecurityManagement\Cause.SecurityManagement.csproj',
    'Cause.SecurityManagement.Wolverine.ExternalSystem\Cause.SecurityManagement.Wolverine.ExternalSystem.csproj'
)

$SolutionFile = Join-Path $RepoRoot 'Cause.SecurityManagement.sln'

# ---------------------------------------------------------------------------
# STEP 1 — VERSION GATE
# ---------------------------------------------------------------------------
Write-Host "`n=== Step 1: Version Gate ===" -ForegroundColor Cyan

function Get-ProjectVersion {
    [CmdletBinding()]
    param([string] $ProjectPath)

    [xml]$csproj = Get-Content $ProjectPath -Raw
    $versionNode = $csproj.Project.PropertyGroup |
        ForEach-Object { $_.Version } |
        Where-Object { $_ }

    if (-not $versionNode) {
        throw "No <Version> element found in '$ProjectPath'."
    }

    return $versionNode
}

$versionMap = [ordered]@{}

foreach ($relPath in $PackableProjects) {
    $fullPath = Join-Path $RepoRoot $relPath
    $version  = Get-ProjectVersion -ProjectPath $fullPath
    $versionMap[$relPath] = $version
    Write-Host "  $relPath  =>  $version"
}

$distinctVersions = @($versionMap.Values | Sort-Object -Unique)

if ($distinctVersions.Count -ne 1) {
    Write-Error @"

VERSION GATE FAILED — packable projects have divergent versions:

$(
    $versionMap.GetEnumerator() |
        ForEach-Object { "  $($_.Key)  =>  $($_.Value)" } |
        Out-String
)
All four projects must carry the same <Version> before a release can proceed.
"@
    exit 1
}

$CoordinatedVersion = $distinctVersions[0]
Write-Host "`n  Version gate passed: all packages at $CoordinatedVersion" -ForegroundColor Green

# ---------------------------------------------------------------------------
# STEP 2 — BUILD
# ---------------------------------------------------------------------------
Write-Host "`n=== Step 2: Build ===" -ForegroundColor Cyan

dotnet build $SolutionFile -c Release --nologo -p:GeneratePackageOnBuild=false
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed (exit $LASTEXITCODE). Aborting."
    exit $LASTEXITCODE
}

Write-Host "`n  Build passed." -ForegroundColor Green

# ---------------------------------------------------------------------------
# STEP 3 — TEST
# ---------------------------------------------------------------------------
if ($SkipTests) {
    Write-Warning "Skipping tests (-SkipTests switch is set). Use only when you have just run the full test suite locally in this session."
}
else {
    Write-Host "`n=== Step 3: Test ===" -ForegroundColor Cyan

    dotnet test $SolutionFile -c Release --no-build --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed (exit $LASTEXITCODE). Aborting."
        exit $LASTEXITCODE
    }

    Write-Host "`n  Tests passed." -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# STEP 4 — PACK
# ---------------------------------------------------------------------------
Write-Host "`n=== Step 4: Pack ===" -ForegroundColor Cyan

# Clean output directory before each run for idempotency.
# -Force keeps -WhatIf runs working: under -WhatIf the Remove-Item above is
# skipped (it honours ShouldProcess), so the directory may still exist here.
if (Test-Path $ArtifactsDir) {
    Remove-Item $ArtifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

foreach ($relPath in $PackableProjects) {
    $fullPath = Join-Path $RepoRoot $relPath
    Write-Host "`n  Packing $relPath ..."

    dotnet pack $fullPath -c Release --no-build --nologo -o $ArtifactsDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet pack failed for '$relPath' (exit $LASTEXITCODE). Aborting."
        exit $LASTEXITCODE
    }
}

$ProducedPackages = Get-ChildItem -Path $ArtifactsDir -Filter '*.nupkg'
Write-Host "`n  Packed $($ProducedPackages.Count) package(s):" -ForegroundColor Green
$ProducedPackages | ForEach-Object { Write-Host "    $($_.Name)" }

# ---------------------------------------------------------------------------
# STEP 5 — PUSH
# ---------------------------------------------------------------------------
Write-Host "`n=== Step 5: Push ===" -ForegroundColor Cyan

foreach ($pkg in $ProducedPackages) {
    if ($PSCmdlet.ShouldProcess($pkg.FullName, "nuget push -Source CaucaNuget")) {
        Write-Host "`n  Pushing $($pkg.Name) ..."
        nuget push -Source CaucaNuget $pkg.FullName
        if ($LASTEXITCODE -ne 0) {
            Write-Error "nuget push failed for '$($pkg.Name)' (exit $LASTEXITCODE). Aborting."
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Host "  [WhatIf] Would push: $($pkg.Name)" -ForegroundColor Yellow
    }
}

# ---------------------------------------------------------------------------
# SUMMARY
# ---------------------------------------------------------------------------
Write-Host "`n=== Release Summary ===" -ForegroundColor Cyan
Write-Host "  Coordinated version : $CoordinatedVersion"
Write-Host "  Projects packed     :"
$PackableProjects | ForEach-Object { Write-Host "    $_" }
Write-Host "  Packages pushed     :"
$ProducedPackages | ForEach-Object { Write-Host "    $($_.Name)" }

if ($WhatIfPreference) {
    Write-Host "`n  [WhatIf] No packages were actually pushed." -ForegroundColor Yellow
}
else {
    Write-Host "`n  Release complete." -ForegroundColor Green
}

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates the compiled EF Core model for TransmissionManager's AppDbContext.

.DESCRIPTION
    Runs `dotnet ef dbcontext optimize` with TransmissionManager.Api as the
    startup project and TransmissionManager.Database as the target project,
    writing the compiled model under TransmissionManager.Database/DbContextOptimized.

    The output directory is wiped between the build and the optimize step so
    that files orphaned by a removed entity (or renamed entity type) don't
    linger and silently slip past the CI freshness check. Wiping happens after
    the build (which still needs the previous generated files to compile) and
    `dotnet ef dbcontext optimize` is then invoked with `--no-build` so it
    works against the already-compiled assemblies.

    The auto-generated compiled model embeds a `modelId` GUID that dotnet-ef
    regenerates on every invocation. After generation, the script asks git whether
    the regenerated DbContextOptimized folder has any diff hunks beyond `modelId`
    lines (via `git diff -I`). If not, the script restores the folder from the
    index. This keeps the script idempotent for unchanged schemas while still
    propagating real schema changes (which carry their own fresh GUID).

    The `dotnet-ef` tool is restored from the tool manifest at
    src/.config/dotnet-tools.json on every invocation; no separate
    `dotnet tool restore` step is required.

.PARAMETER NoBuild
    Skip the internal `dotnet build` step. The caller is responsible for having
    already built TransmissionManager.Api in a configuration the EF tools can
    load. Intended for CI, where the workflow builds the solution before this
    script runs; the script always passes `--no-build` to
    `dotnet ef dbcontext optimize` regardless of this switch.
#>

[CmdletBinding()]
param(
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'

Push-Location (Join-Path $PSScriptRoot '..')
try {
    $optimizedDir = './TransmissionManager.Database/DbContextOptimized'

    dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to restore .NET tools from src/.config/dotnet-tools.json."
        exit $LASTEXITCODE
    }

    if (-not $NoBuild) {
        dotnet build ./TransmissionManager.Api/TransmissionManager.Api.csproj
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    if (Test-Path $optimizedDir) {
        Remove-Item -Recurse -Force -- (Join-Path $optimizedDir '*')
    }

    dotnet ef dbcontext optimize `
        -s ./TransmissionManager.Api `
        -p ./TransmissionManager.Database `
        -o DbContextOptimized `
        --no-build
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $untracked = git ls-files --others --exclude-standard -- $optimizedDir
    $filteredDiff = git diff -I 'modelId: new Guid' -- $optimizedDir
    if (-not $untracked -and -not $filteredDiff) {
        git restore -- $optimizedDir
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}
finally {
    Pop-Location
}

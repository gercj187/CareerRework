param (
    [switch]$NoArchive,
    [string]$OutputDirectory = $PSScriptRoot
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

Write-Host ""
Write-Host ""
Write-Host ""
Write-Host "=== Packe die Mod ===" -ForegroundColor Cyan
Write-Host ""

# -------------------------------------------------
# Checks
# -------------------------------------------------
if (!(Test-Path "info.json")) { throw "info.json fehlt!" }
if (!(Test-Path "build"))     { throw "build/-Ordner fehlt!" }

# -------------------------------------------------
# Mod-Infos
# -------------------------------------------------
$modInfo    = Get-Content -Raw "info.json" | ConvertFrom-Json
$modId      = $modInfo.Id
$modVersion = $modInfo.Version

Write-Host "Mod-ID:  $modId"
Write-Host "Version: $modVersion"

# -------------------------------------------------
# Zielverzeichnisse
# -------------------------------------------------
$DistDir   = Join-Path $OutputDirectory "dist"
$BackupDir = Join-Path $OutputDirectory "backup"

New-Item -Path $DistDir   -ItemType Directory -Force | Out-Null
New-Item -Path $BackupDir -ItemType Directory -Force | Out-Null

# -------------------------------------------------
# TEMP Ordner (ORIGINALVERHALTEN)
# -------------------------------------------------
$TmpDir    = Join-Path $DistDir "tmp"
$ZipOutDir = Join-Path $TmpDir $modId

if (Test-Path $ZipOutDir) {
    Remove-Item $ZipOutDir -Recurse -Force
}
New-Item -Path $ZipOutDir -ItemType Directory -Force | Out-Null

# -------------------------------------------------
# RELEASE Dateien sammeln (EXAKT WIE IM ORIGINAL)
# -------------------------------------------------
$FilesToInclude = @(
    "info.json",
    "build\*",
    "LICENSE"
)

foreach ($item in $FilesToInclude) {
    Copy-Item -Path $item -Destination $ZipOutDir -Recurse -Force
}

# -------------------------------------------------
# ZIP erstellen (dist)
# -------------------------------------------------
if (-not $NoArchive) {

    $fileName = "${modId}_v${modVersion}.zip"
    $zipPath  = Join-Path $DistDir $fileName

    Compress-Archive `
        -Path (Join-Path $ZipOutDir "*") `
        -DestinationPath $zipPath `
        -CompressionLevel Fastest `
        -Force

    Write-Host "ZIP erstellt: dist\$fileName"

    # =================================================
    # SOURCE BACKUP – BIN/OBJ ABSOLUT AUSGESCHLOSSEN
    # =================================================
    $timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
    $sourceBackupName = "${modId}_v${modVersion}_$timestamp.zip"
    $sourceBackupPath = Join-Path $BackupDir $sourceBackupName

    $sourceTmp = Join-Path $TmpDir "source"
    if (Test-Path $sourceTmp) {
        Remove-Item $sourceTmp -Recurse -Force
    }
    New-Item -Path $sourceTmp -ItemType Directory -Force | Out-Null

    $sourceFiles = Get-ChildItem -Path $PSScriptRoot -Recurse -File |
        Where-Object {
            $rel = $_.FullName.Substring($PSScriptRoot.Length).TrimStart('\','/')

            # HARTE Ausschlüsse (egal wo im Pfad)
            if ($rel -match '(^|[\\/])bin([\\/]|$)') { return $false }
            if ($rel -match '(^|[\\/])obj([\\/]|$)') { return $false }
            if ($rel -match '(^|[\\/])build([\\/]|$)') { return $false }
            if ($rel -match '(^|[\\/])dist([\\/]|$)')  { return $false }
            if ($rel -match '(^|[\\/])backup([\\/]|$)'){ return $false }

            # NUR Quellformate
            return $_.Extension -in ".cs", ".csproj", ".txt"
        }

    foreach ($f in $sourceFiles) {
        Copy-Item $f.FullName (Join-Path $sourceTmp $f.Name) -Force
    }

    Compress-Archive `
        -Path (Join-Path $sourceTmp "*") `
        -DestinationPath $sourceBackupPath `
        -CompressionLevel Optimal `
        -Force

    Remove-Item $sourceTmp -Recurse -Force

    Write-Host "Source-Backup erstellt: backup\$sourceBackupName"
}
else {
    Write-Host "Archivieren übersprungen (-NoArchive)"
}

Write-Host ""
Write-Host "=== FERTIG ===" -ForegroundColor Green
Write-Host ""
Write-Host ""
Write-Host ""

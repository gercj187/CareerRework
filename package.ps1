param (
    [switch]$NoArchive,
    [string]$OutputDirectory = $PSScriptRoot
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

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
# Mod-Infos (BOM-sicher lesen)
# -------------------------------------------------
$infoPath   = Join-Path $PSScriptRoot "info.json"
$infoRaw    = [System.IO.File]::ReadAllText($infoPath).TrimStart([char]0xFEFF)
$modInfo    = $infoRaw | ConvertFrom-Json
$modId      = $modInfo.Id
$modVersion = $modInfo.Version

# -------------------------------------------------
# Zielverzeichnisse
# -------------------------------------------------
$DistDir   = Join-Path $OutputDirectory "dist"
$BackupDir = Join-Path $OutputDirectory "backup"

New-Item -Path $DistDir   -ItemType Directory -Force | Out-Null
New-Item -Path $BackupDir -ItemType Directory -Force | Out-Null

# -------------------------------------------------
# VERSION: existiert das ZIP schon in dist -> Hotfix-Teil hochzaehlen
#   1.05.10 -> 1.05.11   |   2.00.0 -> 2.00.1   |   2.00.09 -> 2.00.10
# -------------------------------------------------
function Get-NextHotfixVersion([string]$version) {
    $parts = $version.Split('.')
    $last  = $parts[$parts.Length - 1]

    if ($last -notmatch '^\d+$') { throw "Version '$version' hat keinen numerischen Hotfix-Teil!" }

    $next = ([int]$last + 1).ToString().PadLeft($last.Length, '0')
    $parts[$parts.Length - 1] = $next
    return ($parts -join '.')
}

if (-not $NoArchive) {
    $originalVersion = $modVersion

    while (Test-Path (Join-Path $DistDir "${modId}_v${modVersion}.zip")) {
        $modVersion = Get-NextHotfixVersion $modVersion
    }

    if ($modVersion -ne $originalVersion) {
        Write-Host "dist\${modId}_v${originalVersion}.zip existiert bereits -> Version $originalVersion -> $modVersion" -ForegroundColor Yellow

        # Nur die Versionszeile ersetzen (Formatierung bleibt), UTF-8 OHNE BOM schreiben
        $pattern = '("Version"\s*:\s*")' + [regex]::Escape($originalVersion) + '(")'
        $infoRaw = [regex]::Replace($infoRaw, $pattern, "`${1}$modVersion`${2}")
        [System.IO.File]::WriteAllText($infoPath, $infoRaw, $Utf8NoBom)
    }
}

Write-Host "Mod-ID:  $modId"
Write-Host "Version: $modVersion"

# -------------------------------------------------
# TEMP Ordner
# -------------------------------------------------
$TmpDir    = Join-Path $DistDir "tmp"
$ZipOutDir = Join-Path $TmpDir $modId

if (Test-Path $ZipOutDir) {
    Remove-Item $ZipOutDir -Recurse -Force
}
New-Item -Path $ZipOutDir -ItemType Directory -Force | Out-Null

# -------------------------------------------------
# RELEASE Dateien sammeln
# -------------------------------------------------
# info.json immer frisch und OHNE BOM ins Paket (manche Mod-Manager lesen sonst nicht)
[System.IO.File]::WriteAllText((Join-Path $ZipOutDir "info.json"), $infoRaw, $Utf8NoBom)

$FilesToInclude = @(
    "build\*",
    "LICENSE"
)

# Optional: nur mitpacken, wenn vorhanden - ohne Warnung
$OptionalToInclude = @(
    "lang"          # eigene Sprachdateien (lang\<code>.json)
)

foreach ($item in $FilesToInclude) {
    if (-not (Test-Path $item)) {
        Write-Host "WARNUNG: '$item' fehlt - wird uebersprungen" -ForegroundColor Yellow
        continue
    }
    Copy-Item -Path $item -Destination $ZipOutDir -Recurse -Force
}

foreach ($item in $OptionalToInclude) {
    if (Test-Path $item) {
        Copy-Item -Path $item -Destination $ZipOutDir -Recurse -Force
    }
}

# Evtl. BOM aus mitkopierten JSON-Dateien entfernen
Get-ChildItem -Path $ZipOutDir -Recurse -Filter *.json | ForEach-Object {
    $text = [System.IO.File]::ReadAllText($_.FullName).TrimStart([char]0xFEFF)
    [System.IO.File]::WriteAllText($_.FullName, $text, $Utf8NoBom)
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
    # SOURCE BACKUP - BIN/OBJ ABSOLUT AUSGESCHLOSSEN
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

            # HARTE Ausschluesse (egal wo im Pfad)
            if ($rel -match '(^|[\\/])bin([\\/]|$)') { return $false }
            if ($rel -match '(^|[\\/])obj([\\/]|$)') { return $false }
            if ($rel -match '(^|[\\/])build([\\/]|$)') { return $false }
            if ($rel -match '(^|[\\/])dist([\\/]|$)')  { return $false }
            if ($rel -match '(^|[\\/])backup([\\/]|$)'){ return $false }

            # NUR Quellformate
            return $_.Extension -in ".cs", ".csproj", ".txt", ".json", ".ps1"
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
    Write-Host "Archivieren uebersprungen (-NoArchive)"
}

Write-Host ""
Write-Host "=== FERTIG ===" -ForegroundColor Green
Write-Host ""
Write-Host ""
Write-Host ""

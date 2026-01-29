param(
  [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

function Print-Hits([string]$filePath, [string[]]$patterns) {
  if (!(Test-Path -LiteralPath $filePath)) { return }
  foreach ($p in $patterns) {
    $hits = Select-String -LiteralPath $filePath -Pattern $p -SimpleMatch -ErrorAction SilentlyContinue
    foreach ($h in $hits) {
      "{0}:{1}: {2}" -f $filePath, $h.LineNumber, $h.Line.Trim()
    }
  }
}

$roots = @(
  (Join-Path $ProjectRoot "Library/Bee"),
  (Join-Path $ProjectRoot "Library"),
  (Join-Path $ProjectRoot "Temp")
)

$targets = New-Object System.Collections.Generic.List[string]

foreach ($r in $roots) {
  if (!(Test-Path -LiteralPath $r)) { continue }
  Get-ChildItem -LiteralPath $r -Recurse -ErrorAction SilentlyContinue -Include "AndroidManifest.xml","build.gradle","launcher.gradle","gradle.properties" |
    ForEach-Object { $targets.Add($_.FullName) | Out-Null }
}

if ($targets.Count -eq 0) {
  Write-Host "No Gradle/manifest files found in Library/Temp (build artifacts may be cleaned)."
  exit 0
}

# Prefer launcher module manifests (final app) if present
$launcherManifests = $targets | Where-Object { $_ -match '[\\/]launcher[\\/]src[\\/]main[\\/]AndroidManifest\.xml$' }
if ($launcherManifests.Count -gt 0) {
  Write-Host "=== Found launcher AndroidManifest.xml (final app manifest candidates) ==="
  $launcherManifests | Select-Object -Unique | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
  Write-Host ""
  Write-Host "=== Extracting package/label/provider authorities from launcher manifests ==="
  foreach ($m in ($launcherManifests | Select-Object -Unique | Select-Object -First 10)) {
    Print-Hits $m @('package="','android:label=','android:authorities=')
  }
  Write-Host ""
}

# Check any gradle files for applicationId
$gradles = $targets | Where-Object { $_ -match 'build\.gradle$|launcher\.gradle$' }
if ($gradles.Count -gt 0) {
  Write-Host "=== Extracting applicationId/namespace from gradle files ==="
  foreach ($g in ($gradles | Select-Object -Unique | Select-Object -First 30)) {
    Print-Hits $g @('applicationId','namespace')
  }
  Write-Host ""
}

# Check gradle.properties for applicationId related props
$props = $targets | Where-Object { $_ -match 'gradle\.properties$' }
if ($props.Count -gt 0) {
  Write-Host "=== Extracting any com.multicast.* occurrences from gradle.properties ==="
  foreach ($p in ($props | Select-Object -Unique | Select-Object -First 30)) {
    $hits = Select-String -LiteralPath $p -Pattern 'com.multicast.' -SimpleMatch -ErrorAction SilentlyContinue
    foreach ($h in $hits) {
      "{0}:{1}: {2}" -f $p, $h.LineNumber, $h.Line.Trim()
    }
  }
}



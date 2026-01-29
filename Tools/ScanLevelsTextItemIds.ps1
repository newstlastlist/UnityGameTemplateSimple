param(
  [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

$levelsRoot = Join-Path $ProjectRoot "Assets/Resources/LevelsData"
if (!(Test-Path -LiteralPath $levelsRoot)) {
  throw "LevelsData folder not found: $levelsRoot"
}

$files = Get-ChildItem -LiteralPath $levelsRoot -Filter "Level*.json" | Sort-Object Name
$rxCyrillic = [regex]::new('\p{IsCyrillic}')

$report = New-Object System.Collections.Generic.List[object]

foreach ($f in $files) {
  $json = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
  if ($null -eq $json.levels) { continue }

  $bad = New-Object System.Collections.Generic.List[string]

  foreach ($lvl in $json.levels) {
    foreach ($col in $lvl.columns) {
      foreach ($id in $col.itemIdsTopToBottom) {
        if ($null -eq $id) { continue }
        $s = [string]$id
        if ($s.Contains('/')) { continue } # картинки
        if ($rxCyrillic.IsMatch($s)) {
          $bad.Add($s) | Out-Null
        }
      }
    }
  }

  if ($bad.Count -gt 0) {
    $uniq = $bad | Select-Object -Unique
    $sample = ($uniq | Select-Object -First 10) -join ", "
    $report.Add([pscustomobject]@{
      File   = $f.Name
      Count  = $uniq.Count
      Sample = $sample
    }) | Out-Null
  }
}

if ($report.Count -eq 0) {
  Write-Host "OK: no Cyrillic in non-image itemIdsTopToBottom across LevelsData."
  exit 0
}

Write-Host ("Found files with Cyrillic non-image itemIdsTopToBottom: " + $report.Count)
$report | Sort-Object Count -Descending | Format-Table -AutoSize




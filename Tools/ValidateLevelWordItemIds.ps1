param(
  [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

function Load-WordsDictMap([string]$path) {
  if (!(Test-Path -LiteralPath $path)) { throw "Dictionary json not found: $path" }
  $json = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
  $map = @{}
  foreach ($e in $json) {
    if ($null -eq $e) { continue }
    if ([string]::IsNullOrWhiteSpace($e.categoryId)) { continue }
    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    foreach ($it in ($e.items | ForEach-Object { $_ })) {
      if ($null -eq $it) { continue }
      $s = [string]$it
      if ([string]::IsNullOrWhiteSpace($s)) { continue }
      $null = $set.Add($s)
    }
    $map[$e.categoryId] = $set
  }
  return $map
}

$levelsRoot = Join-Path $ProjectRoot "Assets/Resources/LevelsData"
$enPath = Join-Path $ProjectRoot "Assets/Resources/Dictionary/en.json"
$ruPath = Join-Path $ProjectRoot "Assets/Resources/Dictionary/ru.json"

if (!(Test-Path -LiteralPath $levelsRoot)) { throw "LevelsData folder not found: $levelsRoot" }

$enMap = Load-WordsDictMap $enPath
$ruMap = Load-WordsDictMap $ruPath

$files = Get-ChildItem -LiteralPath $levelsRoot -Filter "Level*.json" | Sort-Object Name
$rxCyr = [regex]::new('\p{IsCyrillic}')

$problems = New-Object System.Collections.Generic.List[object]

foreach ($f in $files) {
  $doc = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
  if ($null -eq $doc.levels) { continue }

  foreach ($lvl in $doc.levels) {
    # chipCategory(int) -> categoryId(string) for WORD archetype only
    $chipCategoryToCategoryId = @{}
    foreach ($b in ($lvl.categoryBindings | ForEach-Object { $_ })) {
      if ($null -eq $b) { continue }
      if ($b.archetype -ne 0) { continue } # Word only
      if ([string]::IsNullOrWhiteSpace($b.categoryId)) { continue }
      $chipCategoryToCategoryId[[int]$b.chipCategory] = [string]$b.categoryId
    }

    if ($chipCategoryToCategoryId.Count -eq 0) { continue }

    foreach ($col in ($lvl.columns | ForEach-Object { $_ })) {
      if ($null -eq $col) { continue }
      $chips = $col.chipsTopToBottom
      $items = $col.itemIdsTopToBottom
      if ($null -eq $chips -or $null -eq $items) { continue }

      $n = [Math]::Min($chips.Count, $items.Count)
      for ($i = 0; $i -lt $n; $i++) {
        $chipCat = [int]$chips[$i]
        $itemId = [string]$items[$i]
        if ([string]::IsNullOrWhiteSpace($itemId)) { continue }
        if ($itemId.Contains('/')) { continue } # images

        if (-not $chipCategoryToCategoryId.ContainsKey($chipCat)) { continue }
        $categoryId = $chipCategoryToCategoryId[$chipCat]

        $ok = $false
        if ($enMap.ContainsKey($categoryId) -and $enMap[$categoryId].Contains($itemId)) { $ok = $true }
        if ($ruMap.ContainsKey($categoryId) -and $ruMap[$categoryId].Contains($itemId)) { $ok = $true }

        if (-not $ok) {
          $problems.Add([pscustomobject]@{
            File       = $f.Name
            CategoryId = $categoryId
            ItemId     = $itemId
            IsCyrillic = $rxCyr.IsMatch($itemId)
          }) | Out-Null
        }
      }
    }
  }
}

if ($problems.Count -eq 0) {
  Write-Host "OK: all non-image itemIdsTopToBottom are present in either en.json or ru.json for their word category."
  exit 0
}

Write-Host ("FOUND INVALID WORD ITEMS: " + $problems.Count)
$problems |
  Sort-Object File, CategoryId, ItemId |
  Select-Object -First 200 |
  Format-Table -AutoSize

Write-Host ""
Write-Host "Tip: if level designer just pushed fixes, pull latest and rerun this script."




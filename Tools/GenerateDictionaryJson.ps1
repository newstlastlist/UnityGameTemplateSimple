param(
  [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

function Parse-DictionaryFile([string]$path) {
  if (!(Test-Path $path)) { throw "File not found: $path" }
  $lines = Get-Content -LiteralPath $path -Encoding UTF8

  $categories = @()
  $current = $null

  foreach ($raw in $lines) {
    $line = $raw.Trim()
    if ([string]::IsNullOrWhiteSpace($line)) { continue }

    if ($line.StartsWith("*")) {
      if ($null -ne $current) { $categories += $current }
      $current = [ordered]@{
        name  = $line.Substring(1).Trim()
        items = New-Object System.Collections.Generic.List[string]
      }
      continue
    }

    if ($null -eq $current) {
      throw "Invalid format: word before first category in $path. Line: $line"
    }

    $current.items.Add($line)
  }

  if ($null -ne $current) { $categories += $current }
  return ,$categories
}

$enPath = Join-Path $ProjectRoot "Assets/Resources/Dictionary/en.txt"
$ruPath = Join-Path $ProjectRoot "Assets/Resources/Dictionary/ru.txt"
$outEnJson = Join-Path $ProjectRoot "Assets/Resources/Dictionary/en.json"
$outRuJson = Join-Path $ProjectRoot "Assets/Resources/Dictionary/ru.json"

$en = Parse-DictionaryFile $enPath
$ru = Parse-DictionaryFile $ruPath

if ($en.Count -ne $ru.Count) {
  throw "en.txt and ru.txt category count mismatch: en=$($en.Count), ru=$($ru.Count)"
}

$enOut = New-Object System.Collections.Generic.List[object]
$ruOut = New-Object System.Collections.Generic.List[object]

for ($i = 0; $i -lt $en.Count; $i++) {
  $enCat = $en[$i]
  $ruCat = $ru[$i]

  $categoryId = [string]$enCat.name
  $enEntry = [ordered]@{
    categoryId  = $categoryId
    displayName = [string]$enCat.name
    items       = @($enCat.items)
  }
  $ruEntry = [ordered]@{
    categoryId  = $categoryId
    displayName = [string]$ruCat.name
    items       = @($ruCat.items)
  }

  $enOut.Add($enEntry) | Out-Null
  $ruOut.Add($ruEntry) | Out-Null
}

$enJsonText = ($enOut | ConvertTo-Json -Depth 6)
$ruJsonText = ($ruOut | ConvertTo-Json -Depth 6)

# Ensure UTF-8 without BOM for Unity TextAsset friendliness
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outEnJson, $enJsonText, $utf8NoBom)
[System.IO.File]::WriteAllText($outRuJson, $ruJsonText, $utf8NoBom)

Write-Host "Generated:"
Write-Host " - $outEnJson"
Write-Host " - $outRuJson"











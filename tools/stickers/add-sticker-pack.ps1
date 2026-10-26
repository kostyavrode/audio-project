<#
Добавляет стикеры из папки с .webp в каталог.

  powershell -ExecutionPolicy Bypass -File tools\stickers\add-sticker-pack.ps1 -Source D:\stickers\cats -PackId cats -Title "Коты"

Что делает:
  1. Каждому файлу (по алфавиту) выдаёт новый ID - больше всех, что уже есть в каталоге и в папке картинок.
     ID никогда не переиспользуются: по ним старые сообщения находят свои картинки.
  2. Копирует файлы в site\stickers\{id}.webp.
  3. Дописывает стикеры в пак PackId (создаёт пак, если его нет) и увеличивает version каталога.

После этого: закоммитить site (картинки) и audio-project (catalog.json), на сервере сделать git pull в обоих.
ChatService подхватит каталог сам в течение 30 секунд, перезапуск не нужен.
Картинки из site\stickers никогда не удаляйте - на них ссылаются старые сообщения.
#>
param(
    [Parameter(Mandatory = $true)] [string] $Source,
    [Parameter(Mandatory = $true)] [ValidatePattern('^[a-z0-9_-]+$')] [string] $PackId,
    [string] $Title = $PackId,
    [string] $CatalogPath,
    [string] $SiteDir
)

$ErrorActionPreference = 'Stop'

# В Windows PowerShell 5.1 $PSScriptRoot пуст в значениях параметров по умолчанию - считаем пути здесь
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $CatalogPath) { $CatalogPath = Join-Path $scriptDir '..\..\services\ChatService\ChatService.Api\Stickers\catalog.json' }
if (-not $SiteDir) { $SiteDir = Join-Path $scriptDir '..\..\..\site' }
$MaxRecommendedBytes = 512KB

$CatalogPath = [System.IO.Path]::GetFullPath($CatalogPath)
$stickersDir = [System.IO.Path]::GetFullPath((Join-Path $SiteDir 'stickers'))
if (-not (Test-Path $CatalogPath)) { throw "Не найден каталог: $CatalogPath" }
if (-not (Test-Path $SiteDir)) { throw "Не найдена папка сайта: $SiteDir" }
New-Item -ItemType Directory -Force $stickersDir | Out-Null

$files = @(Get-ChildItem -Path $Source -Filter '*.webp' -File | Sort-Object Name)
if ($files.Count -eq 0) { throw "В $Source нет файлов .webp" }

# Проверяем все файлы до того, как что-то менять
foreach ($file in $files) {
    $header = New-Object byte[] 12
    $stream = [System.IO.File]::OpenRead($file.FullName)
    try { [void]$stream.Read($header, 0, 12) } finally { $stream.Dispose() }
    $riff = [System.Text.Encoding]::ASCII.GetString($header, 0, 4)
    $webp = [System.Text.Encoding]::ASCII.GetString($header, 8, 4)
    if ($riff -ne 'RIFF' -or $webp -ne 'WEBP') { throw "$($file.Name) - не WebP (переименованный png/jpg?)" }
    if ($file.Length -gt $MaxRecommendedBytes) {
        Write-Warning "$($file.Name): $([math]::Round($file.Length / 1KB)) КБ - тяжеловато, рекомендуется до 512 КБ"
    }
}

$catalog = [System.IO.File]::ReadAllText($CatalogPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json

# Приводим каталог к простым структурам, чтобы ConvertTo-Json не терял массивы из одного элемента
$packs = New-Object System.Collections.ArrayList
foreach ($p in @($catalog.packs)) {
    if ($null -eq $p) { continue }
    $stickers = New-Object System.Collections.ArrayList
    foreach ($s in @($p.stickers)) {
        if ($null -eq $s) { continue }
        [void]$stickers.Add([ordered]@{ id = [int]$s.id; emoji = [string]$s.emoji })
    }
    [void]$packs.Add([ordered]@{ id = [string]$p.id; title = [string]$p.title; iconStickerId = [int]$p.iconStickerId; stickers = $stickers })
}

# Следующий ID: больше всех из каталога и всех уже лежащих картинок (даже если их убрали из каталога)
$maxId = 0
foreach ($p in $packs) { foreach ($s in $p.stickers) { if ($s.id -gt $maxId) { $maxId = $s.id } } }
foreach ($f in Get-ChildItem -Path $stickersDir -Filter '*.webp' -File) {
    $n = 0
    if ([int]::TryParse($f.BaseName, [ref]$n) -and $n -gt $maxId) { $maxId = $n }
}

$pack = $null
foreach ($p in $packs) { if ($p.id -eq $PackId) { $pack = $p } }
if ($null -eq $pack) {
    $pack = [ordered]@{ id = $PackId; title = $Title; iconStickerId = 0; stickers = (New-Object System.Collections.ArrayList) }
    [void]$packs.Add($pack)
}

$nextId = $maxId + 1
foreach ($file in $files) {
    $target = Join-Path $stickersDir "$nextId.webp"
    Copy-Item $file.FullName $target
    [void]$pack.stickers.Add([ordered]@{ id = $nextId; emoji = '' })
    Write-Host "  $($file.Name) -> stickers/$nextId.webp"
    $nextId++
}
if ($pack.iconStickerId -le 0) { $pack.iconStickerId = $pack.stickers[0].id }

$result = [ordered]@{ version = ([int]$catalog.version + 1); packs = $packs }
$json = ConvertTo-Json -InputObject $result -Depth 10
[System.IO.File]::WriteAllText($CatalogPath, $json, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "Пак '$PackId': добавлено $($files.Count) стикеров, каталог v$($result.version)."
Write-Host "Эмодзи для подсказок можно вписать вручную в поле emoji в $CatalogPath"

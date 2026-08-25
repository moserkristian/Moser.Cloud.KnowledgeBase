#Requires -Version 5.1
<#
.SYNOPSIS
  Pripravi native Windows Ollamu pre in-process policy RAG vo Web (Blazor BFF).

.DESCRIPTION
  Jediny runtime: native Ollama na http://localhost:11434.
  Nespusta Docker Ollamu, Aspire Ollama kontajner ani cloud OpenAI.

  Double-click: Ensure-NativeOllama.cmd
  (Ak Windows rezervuje 11434, vyskoci UAC - klikni Yes. Ziadne prikazy netreba.)

.PARAMETER FreePort
  Vyzaduje elevaciu. Zastavi WinNAT, aby Hyper-V uvolnil excluded range okolo 11434,
  spusti Ollamu a WinNAT znova nastartuje. Pri double-click sa zapne samo, ked treba.

.PARAMETER StopDockerOllama
  Ak stary kontajner ollama/ollama bezi, zastavi ho (docker stop). Default: len varovanie.

.PARAMETER SkipPull
  Nespusta ollama pull pre llama3.2 ani nomic-embed-text.
#>
[CmdletBinding()]
param(
    [switch]$FreePort,
    [switch]$StopDockerOllama,
    [switch]$SkipPull
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$OllamaUrl = "http://localhost:11434"
$ChatModel = "llama3.2"
$EmbedModel = "nomic-embed-text"
$OllamaApp = Join-Path $env:LOCALAPPDATA "Programs\Ollama\ollama app.exe"
$OllamaCli = Join-Path $env:LOCALAPPDATA "Programs\Ollama\ollama.exe"
$DockerFormat = '{{.Names}}|{{.Status}}|{{.Ports}}'

function Write-Step([string]$Message) { Write-Host "" ; Write-Host "==> $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "    OK  $Message" -ForegroundColor Green }
function Write-Warn([string]$Message) { Write-Host "    !!  $Message" -ForegroundColor Yellow }
function Write-Fail([string]$Message) { Write-Host "    XX  $Message" -ForegroundColor Red }

function Test-IsAdmin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return ([Security.Principal.WindowsPrincipal]$id).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Wait-ForEnter {
    if ($env:OLLAMA_ENSURE_NOPAUSE) { return }
    Write-Host ""
    Read-Host "Stlac Enter na zatvorenie"
}

function Request-Elevation {
    Write-Host "    Windows ziada admin prava (UAC) na uvolnenie portu 11434..."
    $argList = @(
        "-NoLogo", "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "`"$PSCommandPath`"",
        "-FreePort"
    )
    if ($StopDockerOllama) { $argList += "-StopDockerOllama" }
    if ($SkipPull) { $argList += "-SkipPull" }

    try {
        $proc = Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $argList -Wait -PassThru
        if ($null -eq $proc) {
            Write-Fail "UAC zrusene alebo sa elevated proces nespustil."
            Wait-ForEnter
            exit 1
        }
        exit $proc.ExitCode
    }
    catch {
        Write-Fail "UAC zrusene alebo elevation zlyhala: $($_.Exception.Message)"
        Wait-ForEnter
        exit 1
    }
}

function Test-PortExcluded([int]$Port) {
    $text = netsh interface ipv4 show excludedportrange protocol=tcp | Out-String
    foreach ($line in ($text -split "`r?`n")) {
        if ($line -match "^\s*(\d+)\s+(\d+)\s") {
            $start = [int]$Matches[1]
            $end = [int]$Matches[2]
            if ($Port -ge $start -and $Port -le $end) {
                return $true
            }
        }
    }
    return $false
}

function Test-OllamaReachable {
    try {
        $response = Invoke-WebRequest -Uri "$OllamaUrl/api/tags" -UseBasicParsing -TimeoutSec 3
        return ($response.StatusCode -eq 200)
    }
    catch {
        return $false
    }
}

function Get-InstalledModels {
    $names = New-Object System.Collections.Generic.List[string]
    try {
        $response = Invoke-WebRequest -Uri "$OllamaUrl/api/tags" -UseBasicParsing -TimeoutSec 10
        $json = $response.Content | ConvertFrom-Json
        foreach ($model in @($json.models)) {
            $name = [string]$model.name
            if (-not [string]::IsNullOrWhiteSpace($name)) {
                $names.Add($name)
            }
        }
    }
    catch {
        Write-Warn "Nepodarilo sa precitat /api/tags: $($_.Exception.Message)"
    }
    return $names
}

function Get-ModelNameList {
    $raw = Get-InstalledModels
    $flat = New-Object System.Collections.Generic.List[string]
    foreach ($item in @($raw)) {
        if ($item -is [System.Array]) {
            foreach ($inner in $item) { if ($inner) { $flat.Add([string]$inner) } }
        }
        elseif ($item) {
            $flat.Add([string]$item)
        }
    }
    return $flat
}

function Test-ModelPresent($Installed, [string]$Wanted) {
    foreach ($name in @($Installed)) {
        if ([string]::IsNullOrWhiteSpace($name)) { continue }
        if ($Wanted -eq $ChatModel -and $name -like "llama3.2:1b*") { continue }
        if ($name -eq $Wanted -or $name -like ($Wanted + ":*")) {
            return $true
        }
    }
    return $false
}

function Start-NativeOllama {
    $running = @(Get-Process | Where-Object { $_.ProcessName -like "ollama*" })
    if ($running.Count -gt 0) {
        Write-Ok "Ollama proces uz bezi."
        return
    }

    if (Test-Path -LiteralPath $OllamaApp) {
        Write-Host "    Spustam native Ollama app..."
        Start-Process -FilePath $OllamaApp
        return
    }

    if (Get-Command ollama -ErrorAction SilentlyContinue) {
        Write-Host "    Spustam ollama serve..."
        Start-Process -FilePath $OllamaCli -ArgumentList "serve" -WindowStyle Hidden
        return
    }

    throw "Native Ollama nie je nainstalovana. Stiahni installer z https://ollama.com/download (Windows), nie Docker image."
}

Write-Step "Docker Ollama (ma ostat vypnuta)"
$dockerOk = $false
try {
    & docker info --format "{{.ServerVersion}}" 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $dockerOk = $true }
}
catch { }

if (-not $dockerOk) {
    Write-Ok "Docker nie je dostupny - to je v poriadku, native Ollama ho nepotrebuje."
}
else {
    $rows = @(& docker ps -a --filter "ancestor=ollama/ollama" --format $DockerFormat 2>$null)
    $named = @(& docker ps -a --filter "name=ollama" --format $DockerFormat 2>$null)
    $all = @($rows + $named | Where-Object { $_ } | Select-Object -Unique)
    if ($all.Count -eq 0) {
        Write-Ok "Ziadny Ollama kontajner."
    }
    else {
        foreach ($row in $all) {
            $parts = @($row -split "\|")
            $name = $parts[0]
            $status = if ($parts.Count -gt 1) { $parts[1] } else { "?" }
            Write-Warn "Najdeny kontajner ${name}: $status - nespustaj ho (stary image, kolizia 11434)."
            if ($status -like "Up*" -and $StopDockerOllama) {
                & docker stop $name | Out-Null
                Write-Ok "Zastaveny $name."
            }
        }
        if (-not $StopDockerOllama) {
            Write-Host "    Odstranenie (volitelne): docker rm <name> ; docker rmi ollama/ollama:0.5.9"
        }
    }
}

Write-Step "Native Ollama CLI"
    if (-not (Test-Path -LiteralPath $OllamaCli) -and -not (Get-Command ollama -ErrorAction SilentlyContinue)) {
    Write-Fail "ollama.exe sa nenaslo."
    Write-Host "    Nainstaluj native Windows app z https://ollama.com/download"
    Wait-ForEnter
    exit 1
}
if (-not (Test-Path -LiteralPath $OllamaCli)) {
    $OllamaCli = (Get-Command ollama).Source
}
$ver = (& $OllamaCli --version 2>&1 | Out-String).Trim()
Write-Ok $ver
Write-Host "    Vahy: $env:USERPROFILE\.ollama   (nie git)"

Write-Step "Port 11434"
$excluded = Test-PortExcluded 11434
if ($excluded) {
    Write-Fail "11434 je v Hyper-V/WinNAT excluded range - native Ollama sa nevie naviazat."
    if (-not (Test-IsAdmin)) {
        Request-Elevation
    }

    Write-Host "    net stop winnat ..."
    net stop winnat | Out-Null
    Start-Sleep -Seconds 1
    if (Test-PortExcluded 11434) {
        Write-Warn "Rozsah stale obsahuje 11434. Nastavujem Windows dynamic TCP porty (49152+)..."
        netsh int ipv4 set dynamic tcp start=49152 num=16384 | Out-Null
        net stop winnat | Out-Null
        Start-Sleep -Seconds 1
    }
    if (Test-PortExcluded 11434) {
        Write-Fail "11434 je stale rezervovany. Reboot PC a znova double-clickni Ensure-NativeOllama.cmd."
        Wait-ForEnter
        exit 2
    }
    Write-Ok "11434 uz nie je excluded."
}
else {
    Write-Ok "11434 nie je v excluded range."
}

Write-Step "Spustenie native Ollamy"
if (Test-OllamaReachable) {
    Write-Ok "Uz pocuva $OllamaUrl"
}
else {
    Start-NativeOllama
    $deadline = (Get-Date).AddSeconds(40)
    do {
        Start-Sleep -Seconds 2
        if (Test-OllamaReachable) { break }
    } while ((Get-Date) -lt $deadline)

    if (Test-OllamaReachable) {
        Write-Ok "HTTP $OllamaUrl/api/tags = 200"
    }
    else {
        Write-Fail "Ollama nereaguje na $OllamaUrl/api/tags"
        Write-Host "    Pozri $env:LOCALAPPDATA\Ollama\server.log (hladaj bind / excluded port)."
        if ($excluded) {
            try { net start winnat | Out-Null } catch { }
        }
        Wait-ForEnter
        exit 3
    }
}

if ($excluded) {
    try {
        net start winnat | Out-Null
        Write-Ok "WinNAT znova bezi."
    }
    catch {
        Write-Warn "net start winnat zlyhal: $($_.Exception.Message)"
    }
}

Write-Step "Modely ($ChatModel chat, $EmbedModel embeddings)"
$installed = Get-ModelNameList
if ($installed.Count -gt 0) {
    Write-Host ("    Nainstalovane: " + [string]::Join(", ", $installed.ToArray()))
}

if ($SkipPull) {
    Write-Warn "SkipPull - nic nestahujem."
}
else {
    foreach ($model in @($ChatModel, $EmbedModel)) {
        if (Test-ModelPresent $installed $model) {
            Write-Ok "$model je na disku."
        }
        else {
            Write-Host "    Pull $model do $env:USERPROFILE\.ollama"
            Write-Host "    llama3.2 ~2 GB disk / ~4 GB RAM; nomic-embed-text ~274 MB."
            & $OllamaCli pull $model
            if ($LASTEXITCODE -ne 0) {
                Write-Fail "ollama pull $model zlyhal (exit $LASTEXITCODE)."
                Wait-ForEnter
                exit 4
            }
            Write-Ok "$model stiahnuty."
        }
    }
}

$installed = Get-ModelNameList
$chatOk = Test-ModelPresent $installed $ChatModel
$embedOk = Test-ModelPresent $installed $EmbedModel
if (-not $chatOk) { Write-Warn "Chyba $ChatModel (llama3.2:1b nestaci - app caka 3B latest)." }
if (-not $embedOk) { Write-Warn "Chyba $EmbedModel." }

Write-Step "Dalsi krok: AppHost"
Write-Host "    Native Ollama je jediny runtime. Docker Ollamu neries."
Write-Host "    Spusti:"
Write-Host "      dotnet run --project src\Orchestration\AppHost"
Write-Host "    Over /assistant/status:"
Write-Host "      provider = ollama (nie stub), chunk count > 0, oba modely v zozname."
Write-Host "    Ak Web nastartoval skor nez Ollama: v Aspire restartni webfrontend (AiStack je singleton)."
Write-Host "    Cloud OpenAI nespustaj, kym to vyslovne nepovies."

if ((Test-OllamaReachable) -and $chatOk -and $embedOk) {
    Write-Ok "Hotovo. Dalsie spustenia: znova double-click na Ensure-NativeOllama.cmd (pull sa preskoci, ked uz modely su)."
    Wait-ForEnter
    exit 0
}

Write-Warn "Ollama bezi, ale este chybaju modely. Znova double-clickni Ensure-NativeOllama.cmd, alebo pull z /assistant/status."
Wait-ForEnter
exit 5

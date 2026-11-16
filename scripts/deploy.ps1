[CmdletBinding(DefaultParameterSetName = "Deploy")]
param(
    [Parameter(Mandatory, ParameterSetName = "Deploy")]  [string] $Source,
    [Parameter(Mandatory, ParameterSetName = "Restore")] [string] $RestoreBackup,
    [string] $AppPool     = "TaskManagerPool",
    [string] $Destination = "C:\inetpub\wwwroot\TaskManager",
    [string] $BackupRoot  = "C:\deploy-backups\TaskManager",
    [string] $HealthUrl   = "http://localhost/TaskManager/health",
    [int]    $KeepBackups = 5
)

# Qualquer erro pára o script (e faz o job do GitHub falhar)
$ErrorActionPreference = "Stop"
Import-Module WebAdministration

# Pára o pool para libertar os ficheiros (um .dll em uso não pode ser substituído)
function Stop-Pool {
    Write-Host "A parar $AppPool..."
    if ((Get-WebAppPoolState $AppPool).Value -ne "Stopped") {
        Stop-WebAppPool $AppPool
        $timeout = (Get-Date).AddSeconds(30)
        while ((Get-WebAppPoolState $AppPool).Value -ne "Stopped") {
            if ((Get-Date) -gt $timeout) { throw "O pool $AppPool não parou em 30 segundos" }
            Start-Sleep 1
        }
    }
}

function Start-Pool {
    Write-Host "A arrancar $AppPool..."
    if ((Get-WebAppPoolState $AppPool).Value -ne "Started") { Start-WebAppPool $AppPool }
}

# Copia uma pasta para outra; /MIR apaga o que já não existe na origem, /XD logs preserva os logs
function Sync-Folder([string] $From, [string] $To) {
    robocopy $From $To /MIR /XD logs /NFL /NDL /NP
    # robocopy devolve 0-7 em sucesso (1 = copiou ficheiros); só 8 ou mais é erro
    if ($LASTEXITCODE -ge 8) { throw "robocopy falhou com código $LASTEXITCODE" }
}

# Smoke test: pergunta ao /health se a API arrancou e chega à base de dados (6 tentativas, até 5 s cada e 3 s de pausa: no pior caso cerca de 50 segundos)
function Test-Health {
    foreach ($i in 1..6) {
        try {
            $response = Invoke-WebRequest $HealthUrl -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) { return $true }
        } catch { }
        Start-Sleep 3
    }
    return $false
}

# 3. Arranca SEMPRE o pool, mesmo que algo tenha falhado, para não deixar a app parada
Start-Pool

# 4. Smoke test: só dá o deploy por bom se a API responder saudável
if (-not $failure -and (Test-Health)) {
    Write-Host "Health check OK."

    # 5. Mantém só os últimos backups
    Get-ChildItem $BackupRoot -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | Select-Object -Skip $KeepBackups |
        Remove-Item -Recurse -Force

    Write-Host "Deployment concluído."
    exit 0
}

Write-Host "A nova versão não ficou saudável."

# 6. Rollback automático: repõe a versão que estava antes
if ($backupReady) {
    Write-Host "A repor a versão anterior ($timestamp)..."
    Stop-Pool
    try { Sync-Folder $backup $Destination } finally { Start-Pool }

    if (Test-Health) { throw "Deploy falhou: a versão anterior foi reposta ($timestamp)." }
    throw "Deploy falhou e o rollback também: verifica o IIS manualmente."
}

throw "Deploy falhou e não há versão anterior para repor."

param(
    [Parameter(Mandatory)] [string] $Source,
    [string] $AppPool     = "TaskManagerPool",
    [string] $Destination = "C:\inetpub\wwwroot\TaskManager"
)

# Qualquer erro pára o script (e faz o job do GitHub falhar)
$ErrorActionPreference = "Stop"
Import-Module WebAdministration

if (-not (Test-Path $Source)) { throw "Pasta de origem não existe: $Source" }

# 1. Pára o pool para libertar os ficheiros (um .dll em uso não pode ser substituído)
Write-Host "A parar $AppPool..."
if ((Get-WebAppPoolState $AppPool).Value -ne "Stopped") {
    Stop-WebAppPool $AppPool
    $timeout = (Get-Date).AddSeconds(30)
    while ((Get-WebAppPoolState $AppPool).Value -ne "Stopped") {
        if ((Get-Date) -gt $timeout) { throw "O pool $AppPool não parou em 30 segundos" }
        Start-Sleep 1
    }
}

try {
    # 2. Copia o pacote novo; /MIR apaga o que já não existe na origem, /XD logs preserva os logs
    Write-Host "A copiar $Source para $Destination..."
    robocopy $Source $Destination /MIR /XD logs /NFL /NDL /NP

    # robocopy devolve 0-7 em sucesso (1 = copiou ficheiros); só 8 ou mais é erro
    if ($LASTEXITCODE -ge 8) { throw "robocopy falhou com código $LASTEXITCODE" }
}
finally {
    # 3. Arranca SEMPRE o pool, mesmo que a cópia tenha falhado, para não deixar a app parada
    Write-Host "A arrancar $AppPool..."
    Start-WebAppPool $AppPool
}

# Evita que o GitHub Actions leia o código 1 do robocopy como falha
Write-Host "Deployment concluído."
exit 0

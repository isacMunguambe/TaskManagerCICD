# Guia CI/CD — TaskManagerCICD

Percurso faseado para dominar CI/CD com **ASP.NET Core (.NET 10) + GitHub Actions + Windows/IIS**, aplicado a este repositório.

**Regras do jogo**

- Uma fase de cada vez. Só avanças quando todos os itens de **"Como sei que funcionou"** estiverem marcados.
- Cada fase termina com um commit (ou PR) próprio, para o histórico do Git contar a tua evolução.
- Antes de copiar um YAML, lê-o e responde às **perguntas de verificação**. Se não souberes responder, pára e pergunta.
- Usa o **Registo de progresso** no fim do documento para anotar datas, problemas e o que aprendeste.

---

## Mapa das fases

| Fase | Tema | Resultado |
| ---- | ---- | --------- |
| 1 | Primeiro CI verde | Push → build + testes automáticos no GitHub |
| 2 | Código e testes reais | O CI passa a proteger lógica de verdade |
| 3 | Branches, PRs e protecção | Ninguém faz merge sem CI verde |
| 4 | CI avançado | Cache, relatórios de testes, artefacto de publicação |
| 5 | IIS e publicação manual | API a correr no IIS, publicada à mão |
| 6 | Self-hosted runner | O GitHub consegue executar tarefas na tua máquina Windows |
| 7 | CD: deployment automático | Push em `master` → API actualizada no IIS |
| 8 | Configuração, segredos e base de dados | SQL Server + EF Core sem passwords no código |
| 9 | Health checks e rollback | Deployment verificado e reversível |
| 10 | Staging, Production e versões | Dois ambientes, aprovação manual e releases |

---

## Fase 1 — Primeiro CI verde

**Objectivo:** cada push e cada Pull Request executa restore → build → testes, e tu sabes ler o resultado.

### Conceitos
- **Workflow**: ficheiro YAML em `.github/workflows/`. **Trigger** (`on:`): o evento que o dispara.
- **Job**: conjunto de passos que corre numa máquina (**runner**). **Step**: um comando (`run:`) ou uma action reutilizável (`uses:`).
- **Runner hosted** (`windows-latest`): máquina temporária do GitHub, apagada no fim do job.

### Passos
- [x] **1.1** Substituir o conteúdo de `.github/workflows/ci.yaml`:
  ```yaml
  name: TaskManager CI

  on:
    push:
      branches: [ master, develop ]
    pull_request:
      branches: [ master, develop ]
    workflow_dispatch:          # permite correr à mão a partir do separador Actions

  jobs:
    build-and-test:
      runs-on: windows-latest

      steps:
        - name: Checkout source
          uses: actions/checkout@v4

        - name: Setup .NET
          uses: actions/setup-dotnet@v4
          with:
            dotnet-version: '10.0.x'

        - name: Restore dependencies
          run: dotnet restore TaskManagerCICD.slnx

        - name: Build application
          run: dotnet build TaskManagerCICD.slnx --no-restore --configuration Release

        - name: Run tests
          run: dotnet test TaskManagerCICD.slnx --no-build --configuration Release
  ```
  > Se o GitHub mostrar um aviso de versão de Node nas actions, sobe-as para a versão major mais recente (`@v5`, …). Ler e resolver avisos faz parte do trabalho.
- [x] **1.2** Commit e push das Fases 0 e 1:
  ```bash
  git add .
  git commit -m "Fix CI: target master, use .slnx, move xunit to test project"
  git push
  ```
- [x] **1.3** No GitHub → **Actions** → abre a execução e expande cada step. Repara no tempo de cada um.
- [x] **1.4** Experimenta o botão **Run workflow** (graças ao `workflow_dispatch`).
- [x] **1.5** Adiciona o badge de estado no topo de um `README.md`:
  ```markdown
  ![CI](https://github.com/isacMunguambe/TaskManagerCICD/actions/workflows/ci.yaml/badge.svg)
  ```

### Como sei que funcionou
- [x] Existe uma execução verde no separador Actions, disparada pelo teu push.
- [x] Consegues encontrar no log a linha com o número de testes executados.
- [x] O badge aparece verde no README.

### Perguntas de verificação
1. O que acontece se removeres `--no-build` do passo de testes? E se mantiveres `--no-build` mas removeres o passo de build?

   > **Resposta:** `--no-build` manda o `dotnet test` usar o que já está em `bin/`, sem compilar.
   > - **Sem `--no-build`:** o teste compila outra vez. Funciona, mas gasta tempo a repetir trabalho (e, com outra configuração, compilaria algo diferente do que o passo de build gerou).
   > - **Com `--no-build` mas sem o passo de build:** falha. O runner é uma máquina nova em cada job, por isso não existe `bin/Release` e não há nada para testar.
   >
   > Cada passo faz uma coisa e reaproveita o resultado do anterior (restore → build → test). Se falhar, sabes logo em que fase.

2. Porque usamos `--configuration Release` no CI e não `Debug`?

   > **Resposta:** para testar exactamente o que vai ser publicado. Em produção a aplicação corre em Release (código optimizado, sem `#if DEBUG` nem `Debug.Assert`), e pode comportar-se de forma diferente do Debug. O `dotnet publish` do deployment também usa Release. Debug traz ainda símbolos completos e binários maiores que o servidor não precisa.
   >
   > Frase para fixar: *"Compilo uma vez, em Release, e reutilizo esse resultado nos passos seguintes, para testar o que vou publicar."*

**Fase 1 concluída.**

---

## Fase 2 — Código e testes reais

**Objectivo:** um CI que só testa o `WeatherForecast` não protege nada. Vamos dar à API lógica real e fazer o pipeline **falhar de propósito**.

### Passos
- [x] **2.1** Remover `WeatherForecast.cs` e `WeatherForecastController.cs`.
- [x] **2.2** Criar o domínio (sem base de dados ainda — guardamos em memória):
  - `Models/TaskItem.cs` — `Id`, `Title`, `Description`, `IsCompleted`, `CreatedAt`, `DueDate?`.
  - `Services/ITaskService.cs` + `Services/InMemoryTaskService.cs` — `GetAll`, `GetById`, `Create`, `Complete`, `Delete`.
  - Regras de negócio: título obrigatório (máx. 100 caracteres); `DueDate` não pode estar no passado; não se pode completar uma tarefa já completa.
  - `Controllers/TasksController.cs` — endpoints REST (`GET /api/tasks`, `GET /api/tasks/{id}`, `POST`, `PUT /{id}/complete`, `DELETE /{id}`).
- [x] **2.3** Testes unitários em `Tests/` (apaga o `UnitTest1.cs`), um por regra de negócio, por exemplo:
  - `Create_WithEmptyTitle_Throws`
  - `Create_WithPastDueDate_Throws`
  - `Complete_AlreadyCompleted_Throws`
- [x] **2.4** Push → CI verde.
- [x] **2.5** **Partir de propósito:** altera uma regra (ex.: aceita títulos vazios), faz push e observa o CI vermelho. Lê o log até encontrares o teste que falhou e a mensagem. Depois corrige e volta a verde.
- [ ] **2.6** (Opcional) Testes de integração com `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) para chamar os endpoints HTTP de verdade.

### Como sei que funcionou
- [x] Tens pelo menos 6 testes com significado.
- [x] Viste o CI ficar vermelho por causa de um teste e sabes apontar a linha do log que o explica.

### Perguntas de verificação
1. Qual é a diferença entre um build que falha e um teste que falha, do ponto de vista do pipeline?

   > **Resposta:** os dois param o pipeline e deixam o run vermelho, mas dizem coisas diferentes.
   > - **Build falha:** o código nem compila (erro de sintaxe, tipo inexistente, referência em falta). Não existem binários, por isso os testes nem chegam a correr.
   > - **Teste falha:** o código compila, mas o comportamento não é o esperado. No nosso caso, o step *Build* ficou verde e só o *Run tests* ficou vermelho, com `Assert.Throws() Failure: No exception was thrown`.
   >
   > No pipeline, o step que falha indica logo onde procurar: build → erro no código; teste → erro na lógica. Em ambos os casos nada passa para o deployment.

2. Porque é que o serviço deve estar atrás de uma interface (`ITaskService`)? (Pista: Fase 8.)

   > **Resposta:** porque o controller e os testes dependem do **contrato** (`ITaskService`) e não da implementação concreta. Na Fase 8 trocamos o `InMemoryTaskService` por um `EfTaskService` (SQL Server) alterando apenas **uma linha** no `Program.cs` (o registo da injecção de dependências); o controller não muda. Os testes unitários continuam a usar a versão em memória, rápida e sem base de dados, o que mantém o CI rápido e sem dependências externas.

**Fase 2 concluída.**

---

## Fase 3 — Branches, Pull Requests e protecção

**Objectivo:** trabalhar como numa equipa: nada entra em `master` sem PR e sem CI verde.

### Passos
- [x] **3.1** Criar o branch `develop`:
  ```bash
  git checkout -b develop
  git push -u origin develop
  ```
- [x] **3.2** Fluxo de trabalho a partir de agora:
  `feature/xxx` → PR para `develop` → (quando estável) PR de `develop` para `master`.
- [x] **3.3** Repositório público (as regras de protecção são gratuitas em repositórios públicos).
  > ⚠️ Repositório público + self-hosted runner (Fase 6) exige cuidados: em *Settings → Actions → General → Fork pull request workflows*, exige aprovação para contribuidores externos, e o runner só será usado em `push` para `master`, nunca em `pull_request`.

  Em **Settings → Rules → Rulesets** (ou *Branches → Branch protection*), para `master` e `develop`:
  - Exigir Pull Request antes do merge.
  - Exigir que o status check **`build-and-test`** passe.
  - Bloquear force push.
- [x] **3.4** Criar `feature/task-priority` (adiciona prioridade Low/Medium/High às tarefas + testes), abrir PR para `develop` e ver o CI a correr **dentro do PR**.
- [x] **3.5** Tentar fazer merge de um PR com CI vermelho e confirmar que o GitHub bloqueia.

### Como sei que funcionou
- [x] O botão de merge fica bloqueado enquanto o CI está vermelho ou a correr.
- [x] Já fizeste pelo menos um ciclo completo feature → develop → master por PR.

### Perguntas de verificação
1. Num PR, o CI testa o código do branch ou o resultado do merge com o destino?

   > **Resposta:** o resultado do merge. No evento `pull_request`, o `actions/checkout` descarrega `refs/pull/<n>/merge`, um commit temporário que junta o teu branch com o estado actual do destino. Assim o CI apanha conflitos e incompatibilidades que só aparecem quando as duas partes se juntam, mesmo que o teu branch isolado esteja verde.

2. Porque não basta "ter cuidado" em vez de configurar regras de protecção?

   > **Resposta:** porque o cuidado falha: cansaço, pressa, um push directo por engano, um colega novo que não conhece o processo. A regra é aplicada pela máquina, sempre, a todos (incluindo a ti), e deixa registo. Um `master` que só recebe código com CI verde é sempre publicável, e isso é o que torna seguro o deployment automático da Fase 7.


**Fase 3 concluída.**

---

## Fase 4 — CI avançado

**Objectivo:** um pipeline mais rápido, mais informativo, e que produz o pacote que vai ser publicado.

### Passos
- [x] **4.1** **Cache de NuGet** — no `setup-dotnet`:
  ```yaml
  with:
    dotnet-version: '10.0.x'
    cache: true
    cache-dependency-path: '**/packages.lock.json'
  ```
  > A cache usa *lock files*: acrescenta `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` aos `.csproj`, gera os `packages.lock.json` com `dotnet restore` e faz commit deles. Compara o tempo do restore antes e depois.
- [ ] **4.2** **Verificação de formatação** — novo step antes do build:
  ```yaml
  - name: Check formatting
    run: dotnet format TaskManagerCICD.slnx --verify-no-changes
  ```
- [ ] **4.3** **Resultados e cobertura de testes como artefacto:**
  ```yaml
  - name: Run tests
    run: dotnet test TaskManagerCICD.slnx --no-build -c Release --logger trx --collect:"XPlat Code Coverage" --results-directory ./TestResults

  - name: Upload test results
    if: always()               # corre mesmo que os testes falhem
    uses: actions/upload-artifact@v4
    with:
      name: test-results
      path: ./TestResults
  ```
- [ ] **4.4** **Artefacto de publicação** — o pacote que a Fase 7 vai instalar:
  ```yaml
  - name: Publish API
    run: dotnet publish TaskManagerCICD/TaskManagerCICD.csproj -c Release --no-build -o ./publish

  - name: Upload publish artifact
    uses: actions/upload-artifact@v4
    with:
      name: taskmanager-api
      path: ./publish
      retention-days: 7
  ```
- [ ] **4.5** **Evitar execuções duplicadas** — no topo do workflow:
  ```yaml
  concurrency:
    group: ci-${{ github.ref }}
    cancel-in-progress: true
  ```
- [ ] **4.6** (Desafio) Uma `matrix` que corre os testes em `windows-latest` e `ubuntu-latest`. Quanto tempo demora cada um? O que isso te diz sobre custos de minutos?

### Como sei que funcionou
- [ ] Consegues descarregar o artefacto `taskmanager-api` da página da execução e ver lá dentro `TaskManagerCICD.dll` e `web.config`.
- [ ] O CI falha se deixares código mal formatado.
- [ ] O segundo restore é visivelmente mais rápido que o primeiro.

### Perguntas de verificação
1. O que faz `if: always()` e porque é necessário no upload dos resultados?
2. Porque é que publicamos um artefacto em vez de fazer build directamente no servidor?

---

## Fase 5 — IIS e publicação manual

**Objectivo:** antes de automatizar, fazer à mão. Se não sabes fazer o deployment manualmente, não consegues depurar o automático.

> **Onde?** O teu Windows 11 Pro serve perfeitamente para aprender: o IIS existe como funcionalidade do Windows. Quando tiveres um Windows Server, os passos são os mesmos (lá instala-se por *Server Manager* ou `Install-WindowsFeature Web-Server`).

### Passos
- [ ] **5.1** Activar o IIS (PowerShell como **Administrador**):
  ```powershell
  Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-ManagementConsole -All
  ```
- [ ] **5.2** Instalar o **ASP.NET Core 10 Hosting Bundle** (página de download do .NET 10 → *Hosting Bundle*). Depois:
  ```powershell
  iisreset
  ```
  Confirma em *IIS Manager → Modules* que existe `AspNetCoreModuleV2`.
- [ ] **5.3** Criar pasta, Application Pool e site:
  ```powershell
  Import-Module WebAdministration
  New-Item -ItemType Directory -Force C:\inetpub\TaskManager
  New-WebAppPool -Name TaskManagerPool
  Set-ItemProperty IIS:\AppPools\TaskManagerPool -Name managedRuntimeVersion -Value ""   # No Managed Code
  New-Website -Name TaskManager -Port 8080 -PhysicalPath C:\inetpub\TaskManager -ApplicationPool TaskManagerPool
  ```
- [ ] **5.4** Publicar à mão e copiar:
  ```powershell
  dotnet publish TaskManagerCICD/TaskManagerCICD.csproj -c Release -o C:\inetpub\TaskManager
  ```
- [ ] **5.5** Testar: `http://localhost:8080/api/tasks`.
- [ ] **5.6** Provocar e resolver um erro: pára o pool, apaga um ficheiro `.dll` da pasta, arranca e observa o erro **500.30**. Activa `stdoutLogEnabled="true"` no `web.config` para ver o log, e repõe tudo.

### Como sei que funcionou
- [ ] A API responde no IIS em `localhost:8080`.
- [ ] Sabes onde ver os logs quando o IIS devolve 500.30 / 502.5.
- [ ] Percebeste porque é preciso parar o pool antes de substituir os ficheiros (ficheiros bloqueados).

### Perguntas de verificação
1. Porque é que o Application Pool é *No Managed Code* se a aplicação é .NET?
2. Que identidade corre o processo da API (`IIS AppPool\TaskManagerPool`) e que permissões precisa na pasta?

---

## Fase 6 — Self-hosted runner

**Objectivo:** instalar na máquina Windows um agente que recebe jobs do GitHub. Ele liga-se **para fora** ao GitHub — não precisas de abrir portas nem IP público.

> ⚠️ **Segurança:** se o repositório for **público**, qualquer pessoa pode abrir um PR. Um runner self-hosted nunca deve correr código de PRs de terceiros. Regras para este projecto:
> - O runner só é usado pelo job de **deploy**, que só corre em `push` para `master` (nunca em `pull_request`).
> - Em *Settings → Actions → General*, exige aprovação para workflows de contribuidores externos.
> - Considera tornar o repositório privado enquanto aprendes.

### Passos
- [x] **6.1** GitHub → **Settings → Actions → Runners → New self-hosted runner → Windows x64**. Segue os comandos indicados (download, `config.cmd`), numa pasta como `C:\actions-runner`.
- [ ] **6.2** Durante o `config.cmd`:
  - Adiciona a label **`iis`** (além das automáticas `self-hosted`, `Windows`, `X64`).
  - Responde **sim** a instalar como **serviço Windows**.
- [ ] **6.3** O serviço precisa de permissões para gerir o IIS (parar/arrancar pools, escrever em `C:\inetpub\TaskManager`). Para aprender, uma conta local de administrador dedicada serve; em produção, usa uma conta de serviço com o mínimo de permissões necessárias.
- [x] **6.4** Workflow de teste `.github/workflows/runner-test.yaml`:
  ```yaml
  name: Runner test
  on: workflow_dispatch
  jobs:
    hello:
      runs-on: [self-hosted, windows, iis]
      steps:
        - run: |
            hostname
            whoami
            Get-Website | Select-Object Name, State, PhysicalPath
          shell: powershell
  ```
- [x] **6.5** Corre-o à mão no separador Actions.

### Como sei que funcionou
- [x] O runner aparece como **Idle** (verde) em *Settings → Runners*.
- [x] O job de teste mostra o hostname da tua máquina e lista o `Default Web Site` (o `TaskManager` é uma aplicação dentro dele, por isso não aparece como site).
- [ ] Reinicias o PC e o runner volta a ficar Idle sem fazeres login (está como serviço).

---

## Fase 7 — CD: deployment automático

**Objectivo:** push em `master` → build e testes na cloud → deploy no IIS pelo runner.

### Passos
- [x] **7.1** Criar o script de deployment `scripts/deploy.ps1` (versionado no repo, para o poderes testar à mão):
  ```powershell
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
  ```
- [x] **7.2** Testa o script **à mão** primeiro, num PowerShell como Administrador:
  ```powershell
  dotnet publish TaskManagerCICD/TaskManagerCICD.csproj -c Release -o $env:TEMP\tm-publish
  .\scripts\deploy.ps1 -Source $env:TEMP\tm-publish
  ```
- [x] **7.3** Criar `.github/workflows/cd.yaml`:
  ```yaml
  name: TaskManager CD

  # QUANDO corre: só em push para master (ou à mão). NUNCA em Pull Requests:
  # o job de deploy corre na tua máquina e o repositório é público
  on:
    push:
      branches: [ master ]
    workflow_dispatch:

  permissions:
    contents: read

  # Nunca cancelar um deployment a meio; os seguintes esperam na fila
  concurrency:
    group: deploy-production
    cancel-in-progress: false

  jobs:
    # Job 1: nos servidores do GitHub. Compila, testa e cria o pacote
    build:
      runs-on: windows-latest

      steps:
        - name: Checkout source
          uses: actions/checkout@v4

        - name: Setup .NET
          uses: actions/setup-dotnet@v4
          with:
            dotnet-version: '10.0.x'
            cache: true
            cache-dependency-path: '**/packages.lock.json'

        - name: Restore dependencies
          run: dotnet restore TaskManagerCICD.slnx

        - name: Build application
          run: dotnet build TaskManagerCICD.slnx --no-restore --configuration Release

        - name: Run tests
          run: dotnet test TaskManagerCICD.slnx --no-build --configuration Release

        - name: Publish API
          run: dotnet publish TaskManagerCICD/TaskManagerCICD.csproj --no-build --configuration Release --output ./publish

        # Passa o pacote ao job seguinte (corre noutra máquina)
        - name: Upload publish artifact
          uses: actions/upload-artifact@v4
          with:
            name: taskmanager-api
            path: ./publish
            if-no-files-found: error
            retention-days: 7

    # Job 2: na TUA máquina (runner self-hosted). Só começa se o build passou
    deploy:
      needs: build
      if: github.ref == 'refs/heads/master'   # mesmo com "Run workflow", só publica a partir do master
      runs-on: [self-hosted, windows, iis]

      steps:
        # Só precisa da pasta scripts, para ter o deploy.ps1
        - name: Checkout scripts
          uses: actions/checkout@v4
          with:
            sparse-checkout: scripts

        - name: Download artifact
          uses: actions/download-artifact@v4
          with:
            name: taskmanager-api
            path: ./publish

        - name: Deploy to IIS
          shell: powershell
          run: ./scripts/deploy.ps1 -Source "$PWD\publish"
  ```
- [ ] **7.4** Faz uma alteração visível (ex.: novo endpoint `GET /api/version`), PR → `develop` → `master`, e vê-a chegar ao IIS sozinha.

### Como sei que funcionou
- [x] Um merge em `master` actualiza a API no IIS sem tocares no servidor.
- [ ] Se o build ou os testes falharem, o job `deploy` **não** corre.
- [ ] Se a cópia falhar, o pool volta a arrancar (o `finally` funciona).

### Perguntas de verificação
1. Porque é que o `deploy` corre num runner diferente do `build`? Como chegam os ficheiros de um ao outro?
2. Com dois workflows (CI e CD) os testes correm duas vezes em `master`. Como resolverias? (Pista: *reusable workflows* com `workflow_call`.)

---

## Fase 8 — Configuração, segredos e base de dados

**Objectivo:** trocar o armazenamento em memória por SQL Server + EF Core, sem nenhuma password no repositório.

### Passos
- [ ] **8.1** Adicionar EF Core: `TaskManagerDbContext`, `EfTaskService : ITaskService`, migração inicial. Os testes unitários continuam a usar o serviço em memória (aqui a interface da Fase 2 compensa).
- [ ] **8.2** `appsettings.json` tem só valores não sensíveis. Em desenvolvimento usa **User Secrets**:
  ```bash
  dotnet user-secrets init --project TaskManagerCICD
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=TaskManager;..." --project TaskManagerCICD
  ```
- [ ] **8.3** No servidor, a connection string vem de **variável de ambiente do Application Pool** (não fica em nenhum ficheiro publicado):
  ```powershell
  # appcmd: define variáveis de ambiente só para este pool
  & $env:windir\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools `
    "/+[name='TaskManagerPool'].environmentVariables.[name='ConnectionStrings__DefaultConnection',value='Server=...']" /commit:apphost
  & $env:windir\system32\inetsrv\appcmd.exe set config -section:system.applicationHost/applicationPools `
    "/+[name='TaskManagerPool'].environmentVariables.[name='ASPNETCORE_ENVIRONMENT',value='Production']" /commit:apphost
  ```
  > Repara no `__` (duplo underscore): é assim que `ConnectionStrings:DefaultConnection` se escreve numa variável de ambiente.
- [ ] **8.4** **Migrações no pipeline.** Gera um script SQL idempotente no job `build` e aplica-o no `deploy`:
  ```bash
  dotnet tool install --global dotnet-ef
  dotnet ef migrations script --idempotent --project TaskManagerCICD -o ./publish/migrate.sql
  ```
  No `deploy`, aplica com `Invoke-Sqlcmd` (módulo `SqlServer`) usando a connection string guardada como **GitHub Secret** `DB_CONNECTION_STRING` (*Settings → Secrets and variables → Actions*):
  ```yaml
  - name: Apply migrations
    shell: powershell
    env:
      DB_CONNECTION_STRING: ${{ secrets.DB_CONNECTION_STRING }}
    run: Invoke-Sqlcmd -ConnectionString $env:DB_CONNECTION_STRING -InputFile ./publish/migrate.sql
  ```
- [ ] **8.5** Confirmar que o segredo aparece como `***` nos logs.

### Como sei que funcionou
- [ ] `git grep -i password` não encontra nada sensível.
- [ ] Uma nova migração (ex.: coluna `Priority`) chega à base de dados do servidor sozinha num deployment.

### Perguntas de verificação
1. Porque é que o script tem de ser `--idempotent`?
2. O que acontece se a migração for aplicada mas a cópia dos ficheiros falhar? Que ordem escolherias e porquê?

---

## Fase 9 — Health checks e rollback

**Objectivo:** o pipeline confirma que a API ficou de pé e, se não ficou, volta à versão anterior.

### Passos
- [ ] **9.1** Health check na API:
  ```csharp
  builder.Services.AddHealthChecks().AddDbContextCheck<TaskManagerDbContext>();
  app.MapHealthChecks("/health");
  ```
  (O `AddDbContextCheck` vem do pacote `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.)
- [ ] **9.2** **Smoke test** no fim do `deploy`:
  ```powershell
  $ok = $false
  foreach ($i in 1..10) {
      try { if ((Invoke-WebRequest http://localhost:8080/health -UseBasicParsing).StatusCode -eq 200) { $ok = $true; break } } catch {}
      Start-Sleep 3
  }
  if (-not $ok) { throw "Health check falhou" }
  ```
- [ ] **9.3** **Backups versionados:** antes de copiar, o `deploy.ps1` guarda a versão actual em `C:\deploy-backups\TaskManager\<data-hora>` e mantém só as últimas 5.
- [ ] **9.4** **Rollback automático:** se o smoke test falhar, restaura o último backup e marca o job como falhado.
- [ ] **9.5** **Rollback manual:** workflow `rollback.yaml` com `workflow_dispatch` e um input `backup` (nome da pasta) para restaurar uma versão escolhida.
- [ ] **9.6** Testa: faz deploy de uma versão que rebenta no arranque (ex.: `throw` no `Program.cs`) e vê o rollback a funcionar.

### Como sei que funcionou
- [ ] Uma versão partida nunca fica no ar mais do que o tempo do smoke test.
- [ ] Consegues voltar a uma versão específica com um clique no separador Actions.

---

## Fase 10 — Staging, Production e versões

**Objectivo:** duas instâncias da API, deploy automático para Staging e com aprovação para Production.

### Passos
- [ ] **10.1** No IIS: segundo site `TaskManager-Staging` (porta 8081, pool `TaskManagerStagingPool`, pasta própria, base de dados própria).
- [ ] **10.2** GitHub → **Settings → Environments**: criar `staging` e `production`.
  - `production`: *Required reviewers* = tu; restringir a deployments do branch `master`.
  - Secrets por ambiente (`DB_CONNECTION_STRING` diferente em cada um).
- [ ] **10.3** Reestruturar o CD:
  ```
  build ──► deploy-staging (environment: staging) ──► deploy-production (environment: production, aguarda aprovação)
  ```
  O `deploy.ps1` passa a receber `-SiteName`, `-AppPool` e `-Destination` como parâmetros vindos do ambiente (`vars.`).
- [ ] **10.4** **Versionamento:** cada release em produção leva uma tag `v1.2.3`. Passa a versão para o build (`-p:Version=...`) e mostra-a no endpoint `/api/version`. Cria GitHub Releases a partir das tags.
- [ ] **10.5** Verifica no separador **Deployments** do repositório o histórico do que está em cada ambiente.

### Como sei que funcionou
- [ ] Merge em `master` → Staging actualizado sozinho → Production fica à espera do teu clique em *Approve*.
- [ ] `/api/version` em Staging e Production mostra que versão está em cada um.

---

## Depois deste percurso

Docker e containers no Windows · SonarQube / análise estática · Dependabot e análise de vulnerabilidades (`dotnet list package --vulnerable`) · CodeQL · Azure DevOps Pipelines (para comparar) · deployment para Azure App Service.

---

## Problemas comuns

| Sintoma | Causa provável |
| ------- | -------------- |
| Push feito mas nada no separador Actions | Branch no `on:` não corresponde (foi o caso da Fase 0); YAML fora de `.github/workflows/`; erro de sintaxe YAML |
| `MSB1009: Project file does not exist` | Nome errado da solução (`.sln` vs `.slnx`) |
| Job fica em *Queued* para sempre | Nenhum runner com as labels pedidas está online |
| IIS devolve **500.30** | A app rebentou no arranque — ver stdout log / Event Viewer |
| IIS devolve **500.19** ou **502.5** | Hosting Bundle não instalado ou instalado antes do IIS (reinstalar + `iisreset`) |
| robocopy "falha" com código 1 | Códigos 0–7 são sucesso; só ≥ 8 é erro |
| `The process cannot access the file` | Pool não foi parado antes da cópia |

## Comandos úteis

```bash
dotnet build TaskManagerCICD.slnx -c Release
dotnet test TaskManagerCICD.slnx -c Release
dotnet publish TaskManagerCICD/TaskManagerCICD.csproj -c Release -o ./publish
```
```powershell
Get-Website; Get-WebAppPoolState TaskManagerPool
Get-Service actions.runner.*
```

---

## Registo de progresso

| Fase | Início | Concluída | Notas / problemas / o que aprendi |
| ---- | ------ | --------- | --------------------------------- |
| 1 | | | |
| 2 | | | |
| 3 | | | |
| 4 | | | |
| 5 | | | |
| 6 | | | |
| 7 | | | |
| 8 | | | |
| 9 | | | |
| 10 | | | |

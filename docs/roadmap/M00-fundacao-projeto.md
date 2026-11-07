# M00 — Repositório, projeto Unity e CI
Fase 0 · Onda 0 · Status: ⏳ não iniciado
Depende de: — · Desbloqueia: M01 (e todo o resto) · Paralelo com: D01
Branch: modulo/M00-fundacao-projeto (primeiro commit vai em `main`; ver T3)
Docs de referência: 11-arquitetura §2 (decisões 1–5, 18), §3.1 (stack e pacotes), §3.2 (pastas), §3.3 (assemblies), §3.16 (testes/CI); 00 §5 R04, R52; CLAUDE.md §3–§6; ROADMAP §1 (D1–D6).

## Objetivo
Repositório GitHub privado com Git LFS, projeto Unity 6.3 LTS (URP Forward+, Input System, 60 Hz) na raiz do repositório, estrutura de pastas e assembly definitions de 11, projeto `tools/CoreTests` (dotnet) compilando `Eclipse.Core`, e CI GameCI verde rodando testes rápidos e EditMode a cada push.

## Critérios de saída (verificáveis)
1. `ProjectSettings/ProjectVersion.txt` contém `m_EditorVersion: 6000.3.` (LTS instalada em T1).
2. Batchmode abre o projeto sem erros: `Logs/open.log` sem `error CS` nem `Compilation failed`.
3. `Packages/manifest.json` contém os pacotes de 11 §3.1 que não dependem de conta externa (lista na T4) e `Packages/packages-lock.json` está versionado.
4. Pastas de 11 §3.2 / CLAUDE.md §5 existem; os 13 asmdefs de 11 §3.3 existem (11 assemblies de runtime/editor + `Tests.EditMode` + `Tests.PlayMode`); `Eclipse.Core.asmdef` tem `"noEngineReferences": true` e nenhuma referência (R52).
5. `dotnet test tools/CoreTests` → 1 teste passando (smoke); o mesmo teste aparece e passa no EditMode (`Logs/editmode.xml` com `passed="1"` ou mais, `failed="0"`).
6. Player Settings: Active Input Handling = Input System Package; Color Space = Linear; `Time.fixedDeltaTime = 1/60` (`ProjectSettings/TimeManager.asset` com `Fixed Timestep: 0.016666668`); URP Asset Forward+ atribuído em Graphics e Quality (perfis `PC_Low`, `PC_High`).
7. `.gitignore` exclui `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build*/`, `tools/**/bin/`, `tools/**/obj/`; `.gitattributes` com LFS para binários de arte/áudio e merge YAML do Unity; `git lfs ls-files` funciona.
8. Workflow `.github/workflows/ci.yml` verde no GitHub para o push da branch (jobs `core-tests` e `unity-tests`); `build.yml` agendado (noturno) presente.
9. CLAUDE.md §5/§6/§9 atualizados com caminho exato do Unity, comandos confirmados e pegadinhas encontradas.

## Fora de escopo
- FMOD (entra no M09; exige conta/download do usuário) e Steamworks.NET (M32).
- Validador de dados no CI (job adicionado no M02).
- Qualquer código de gameplay.

## Premissas e decisões
- Unity 6.3 LTS, versão de patch mais recente disponível no Hub (D1). Projeto **na raiz do repositório** (os comandos de CLAUDE.md §6 usam `-projectPath .`).
- Mono como scripting backend em dev; IL2CPP só em builds de release (M32). API Compatibility: .NET Standard 2.1.
- Testes de `Eclipse.Core` ficam em `Assets/_Project/Code/Tests/EditMode/Core/` e são compilados **pelos dois runners** (Unity EditMode e `tools/CoreTests`), usando apenas API básica de NUnit 3 (R52).
- Código do Core em C# 9 (limite do compilador Unity): `tools/CoreTests` força `<LangVersion>9.0</LangVersion>`.
- Pastas vazias versionadas com `.gitkeep` (o Unity ignora arquivos ocultos; o `.meta` da pasta é commitado).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Instalar Unity 6.3 LTS + Windows Build Support (Mono e IL2CPP) | ação do usuário | usuário | — | — | ⏳ |
| T2 | Criar repo GitHub privado, `gh auth login`, instalar Git LFS | ação do usuário | usuário | — | — | ⏳ |
| T3 | Inicializar git, `.gitignore`, `.gitattributes`, LFS, commit inicial dos docs | execução | executor | — | `.gitignore`, `.gitattributes` | ⏳ |
| T4 | Criar projeto Unity por batchmode na raiz + manifest de pacotes | execução | executor | T1, T3 | `Assets/`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/` | ⏳ |
| T5 | Bootstrap de configurações (URP Forward+, Input System, Linear, 60 Hz) e pastas | execução | executor | T4 | `Assets/_Project/Code/Editor/Setup/ProjectBootstrap.cs`, `Assets/_Project/Settings/URP/*`, `Assets/_Project/**/.gitkeep`, `ProjectSettings/*.asset` | ⏳ |
| T6 | Os 13 assembly definitions de 11 §3.3 + arquivo âncora por assembly | execução | executor | T5 | `Assets/_Project/Code/**/*.asmdef`, `Assets/_Project/Code/**/AssemblyInfo.cs` | ⏳ |
| T7 | `tools/CoreTests` (dotnet) + smoke test compartilhado | execução | executor | T6 | `tools/CoreTests/CoreTests.csproj`, `Assets/_Project/Code/Core/CoreInfo.cs`, `Assets/_Project/Code/Tests/EditMode/Core/CoreSmokeTests.cs` | ⏳ |
| T8 | Workflows GameCI (`ci.yml`, `build.yml`) | execução | executor | T7 | `.github/workflows/ci.yml`, `.github/workflows/build.yml` | ⏳ |
| T9 | Configurar secrets do GameCI e fazer o primeiro push | ação do usuário | usuário | T2, T8 | — (secrets no GitHub) | ⏳ |
| T10 | Confirmar comandos, atualizar CLAUDE.md e ROADMAP, revisar | integração | principal | T9 | `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo | ⏳ |

## Ondas internas
1. T1 ∥ T2 ∥ T3 (usuário em paralelo com o executor)
2. T4
3. T5
4. T6
5. T7
6. T8
7. T9
8. T10

## Briefs

**T1 — Instalar Unity 6.3 LTS (usuário)**
No Unity Hub → Installs → Install Editor → aba "Official releases" → a versão **6000.3.x LTS** mais recente. Módulos: *Windows Build Support (Mono)*, *Windows Build Support (IL2CPP)*, *Documentation* (opcional). Garanta que a licença (Personal) está ativa no Hub. Informe a versão exata (ex.: `6000.3.12f1`) para o T4.

**T2 — Repositório GitHub (usuário)**
1. Instale o GitHub CLI e rode `gh auth login`.
2. Crie o repositório privado: `gh repo create project-eclipse --private` (nome à sua escolha).
3. Instale Git LFS (`git lfs install`).
Informe a URL do remoto para o T3/T9.

**T3 — Git, ignore e LFS**
- Objetivo: repositório git na raiz `C:\Users\caduc\UnityProjects\PROJECT ECLIPSE` com ignore/atributos corretos para Unity e o primeiro commit contendo `docs/` e `CLAUDE.md`.
- Contexto: projeto Unity ficará na raiz (ainda não criado); existe `tools/CoreTests` futuro (dotnet).
- Arquivos: escreve `.gitignore`, `.gitattributes`; não toca em `docs/` nem `CLAUDE.md` (só os commita).
- Restrições: branch inicial `main`; commit em português `chore(M00): inicializa repositório com GDD v0.3`; com o trailer de coautoria definido na sessão. Não fazer push.
- Conteúdo: `.gitignore` padrão Unity (`[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild/`, `[Bb]uilds/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/`, `[Mm]emoryCaptures/`, `*.csproj`/`*.sln` gerados na raiz, `.vs/`, `.idea/`) + `tools/**/bin/`, `tools/**/obj/`, `TestResults/`. **Atenção:** o ignore de `*.csproj` deve valer só na raiz (`/*.csproj`, `/*.sln`) para não ignorar `tools/CoreTests/CoreTests.csproj`. `.gitattributes`: `* text=auto`; `*.cs text eol=lf diff=csharp`; YAML do Unity (`*.unity *.prefab *.asset *.mat *.anim *.controller *.overrideController *.physicMaterial *.meta`) com `merge=unityyamlmerge eol=lf`; LFS (`filter=lfs diff=lfs merge=lfs -text`) para `*.png *.jpg *.jpeg *.tga *.psd *.tif *.exr *.hdr *.fbx *.obj *.blend *.wav *.mp3 *.ogg *.aif *.bank *.ttf *.otf *.mp4 *.mov *.zip *.dll *.so *.dylib`.
- Aceite: `git status` limpo após o commit; `git check-ignore Library/x` retorna o caminho; `git check-ignore tools/CoreTests/CoreTests.csproj` não retorna nada.
- Verificação: `git lfs env` sem erro; `git log --oneline -1` mostra o commit.
- Entrega: ≤ 100 palavras — commit criado, regras não óbvias.

**T4 — Criar projeto Unity e pacotes**
- Objetivo: projeto Unity 6.3 LTS na raiz do repositório, com os pacotes de 11 §3.1 resolvidos.
- Contexto: Unity exe em `C:\Program Files\Unity\Hub\Editor\<versão de T1>\Editor\Unity.exe` (`$UNITY_EXE`). O diretório já contém `docs/`, `CLAUDE.md` e arquivos git.
- Passos: (1) `"$UNITY_EXE" -batchmode -quit -createProject "<raiz>" -logFile Logs/create.log`. Se o Unity recusar a pasta não vazia, crie em pasta temporária (scratch) e mova `Assets/`, `Packages/`, `ProjectSettings/` para a raiz. (2) Edite `Packages/manifest.json` adicionando: `com.unity.render-pipelines.universal`, `com.unity.inputsystem`, `com.unity.cinemachine` (3.x), `com.unity.addressables`, `com.unity.ai.navigation`, `com.unity.burst`, `com.unity.collections`, `com.unity.mathematics`, `com.unity.animation.rigging`, `com.unity.timeline`, `com.unity.visualeffectgraph`, `com.unity.shadergraph`, `com.unity.nuget.newtonsoft-json`, `com.unity.localization`, `com.unity.test-framework`, `com.unity.memoryprofiler`. Use as versões verificadas para a 6000.3 (as que o Package Manager resolve; consulte a documentação de pacotes da versão se necessário). Remova pacotes de template desnecessários (`com.unity.collab-proxy`, `com.unity.visualscripting` se presentes). (3) Reabra em batchmode para resolver e gerar `packages-lock.json`: `"$UNITY_EXE" -batchmode -nographics -projectPath . -quit -logFile Logs/open.log`.
- Arquivos: escreve `Assets/`, `Packages/`, `ProjectSettings/`. Não toca em `docs/`.
- Restrições: não instalar FMOD nem Steamworks (fora de escopo); feche qualquer Editor aberto antes do batchmode.
- Aceite: critérios de saída 1–3.
- Verificação: `grep -c "error CS\|Compilation failed" Logs/open.log` → 0; `grep m_EditorVersion ProjectSettings/ProjectVersion.txt` → `6000.3.`.
- Entrega: ≤ 120 palavras — versão do Editor, versões resolvidas dos pacotes principais, desvios.

**T5 — Bootstrap de configurações e pastas**
- Objetivo: projeto configurado conforme 11 e R04, com a árvore de pastas criada.
- Arquivos: escreve `Assets/_Project/Code/Editor/Setup/ProjectBootstrap.cs` (classe estática `Eclipse.Editor.Setup.ProjectBootstrap`, método `public static void Run()`), assets gerados em `Assets/_Project/Settings/URP/` e os `.asset` de `ProjectSettings/` alterados pelo script, `.gitkeep` nas pastas.
- O script deve, de forma idempotente: criar `UniversalRenderPipelineAsset` + `UniversalRendererData` com **Rendering Path = Forward+** em dois perfis (`PC_Low.asset`, `PC_High.asset`), atribuir `PC_High` a `GraphicsSettings.defaultRenderPipeline` e cada perfil a um nível de Quality (`Low`/`High`, removendo os demais); `PlayerSettings.colorSpace = Linear`; Active Input Handling = Input System Package (via `PlayerSettings` / `ProjectSettings.asset` `activeInputHandler: 1`); `Time.fixedDeltaTime = 1f/60f` (TimeManager); API Compatibility .NET Standard 2.1; scripting backend Mono para Standalone; Company/Product name "PROJECT ECLIPSE"; criar as pastas de CLAUDE.md §5 (`Assets/_Project/Code/{Core,Data,Gameplay,AI,Run,Meta,Save,UI,Audio,Platform,Editor,Tests/EditMode,Tests/PlayMode}`, `Assets/_Project/Content/{Heroes,Items,Modifiers,Affinities,Enemies,Bosses,Regions,Sanctuary}`, `Assets/_Project/{Art,Audio,UI,Settings,Scenes}`, `Assets/Plugins`) e `DataSource/` na raiz, cada uma com `.gitkeep`.
- Execução: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Setup.ProjectBootstrap.Run -quit -logFile Logs/bootstrap.log`.
- Restrições: o script não pode depender de asmdef ainda (fica em pasta `Editor`, compila em `Assembly-CSharp-Editor`; o T6 o move para `Eclipse.Editor`). Rodar duas vezes não pode duplicar assets.
- Aceite: critério de saída 6 e pastas presentes.
- Verificação: `grep "activeInputHandler: 1" ProjectSettings/ProjectSettings.asset`; `grep "Fixed Timestep: 0.016666" ProjectSettings/TimeManager.asset`; log sem `Exception`; segunda execução não cria arquivos novos (`git status --porcelain` igual).
- Entrega: ≤ 120 palavras — o que foi configurado, o que exigiu workaround.

**T6 — Assembly definitions**
- Objetivo: os 13 asmdefs de 11 §3.3 com dependências só "para baixo".
- Arquivos: escreve um `.asmdef` por pasta e um `AssemblyInfo.cs` vazio-âncora (`// Assembly anchor`) por assembly que ainda não tenha `.cs`.

| Assembly (pasta) | Referências | Observações |
|---|---|---|
| `Eclipse.Core` (`Code/Core`) | nenhuma | `noEngineReferences: true`, `autoReferenced: true` (R52) |
| `Eclipse.Data` (`Code/Data`) | Core, Unity.Localization, Unity.Addressables | |
| `Eclipse.Gameplay` (`Code/Gameplay`) | Core, Data, Unity.InputSystem, Unity.Cinemachine, Unity.Burst, Unity.Collections, Unity.Mathematics, Unity.AI.Navigation | `allowUnsafeCode: true` |
| `Eclipse.AI` (`Code/AI`) | Core, Data, Gameplay, Unity.AI.Navigation, Unity.Mathematics | |
| `Eclipse.Run` (`Code/Run`) | Core, Data, Gameplay, AI, Unity.Addressables | |
| `Eclipse.Meta` (`Code/Meta`) | Core, Data, Run | |
| `Eclipse.Save` (`Code/Save`) | Core, Data | Newtonsoft via precompiled reference |
| `Eclipse.UI` (`Code/UI`) | Core, Data, Gameplay, Meta | |
| `Eclipse.Audio` (`Code/Audio`) | Core, Gameplay | |
| `Eclipse.Platform` (`Code/Platform`) | Core, Save | |
| `Eclipse.Editor` (`Code/Editor`) | todos acima | `includePlatforms: ["Editor"]` |
| `Eclipse.Tests.EditMode` (`Code/Tests/EditMode`) | Core, Data, Editor | Test Assembly (`UNITY_INCLUDE_TESTS`, `nunit.framework.dll`), Editor-only |
| `Eclipse.Tests.PlayMode` (`Code/Tests/PlayMode`) | Core, Data, Gameplay | Test Assembly |

- Restrições: nenhuma referência "para cima" (UI/Audio nunca referenciados por Gameplay/AI/Run); namespaces `Eclipse.<Assembly>`.
- Aceite: critério de saída 4; projeto compila.
- Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -quit -logFile Logs/open.log` → 0 ocorrências de `error CS`; `grep -L noEngineReferences Assets/_Project/Code/Core/Eclipse.Core.asmdef` não retorna o arquivo.
- Entrega: ≤ 100 palavras.

**T7 — tools/CoreTests**
- Objetivo: testes do Core rodando em segundos fora do Unity, com fonte de teste única compartilhada com o EditMode (R52).
- Arquivos: escreve `tools/CoreTests/CoreTests.csproj`, `Assets/_Project/Code/Core/CoreInfo.cs` (ex.: `namespace Eclipse.Core { public static class CoreInfo { public const string Version = "0.1.0"; } }`), `Assets/_Project/Code/Tests/EditMode/Core/CoreSmokeTests.cs` (NUnit: `[Test] public void CoreAssemblyLoads() => Assert.AreEqual("0.1.0", CoreInfo.Version);`).
- `CoreTests.csproj`: `TargetFramework net8.0` (ou a LTS do SDK instalado; SDK local é 10.0), `<LangVersion>9.0</LangVersion>`, `<Nullable>disable</Nullable>`, `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>`, `<Compile Include="../../Assets/_Project/Code/Core/**/*.cs" />` e `<Compile Include="../../Assets/_Project/Code/Tests/EditMode/Core/**/*.cs" />`; pacotes `NUnit` 3.x, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`.
- Restrições: testes do Core usam só API NUnit 3 comum (`Assert.AreEqual/IsTrue/Throws`, `[TestCase]`), nada de `UnityEngine`; os testes de Core ficam sob `Tests/EditMode/Core/` (outras pastas de teste são só do Unity).
- Aceite: critério de saída 5.
- Verificação: `dotnet test tools/CoreTests` → `Passed: 1, Failed: 0`; `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log` → `Logs/editmode.xml` com `failed="0"` e o teste `CoreAssemblyLoads`.
- Entrega: ≤ 100 palavras.

**T8 — Workflows GameCI**
- Objetivo: CI que roda a cada push/PR os testes rápidos e o EditMode/PlayMode; build Windows noturno.
- Arquivos: escreve `.github/workflows/ci.yml` e `.github/workflows/build.yml`.
- `ci.yml` (on: push, pull_request): job `core-tests` (ubuntu-latest, `actions/setup-dotnet`, `dotnet test tools/CoreTests`); job `unity-tests` (ubuntu-latest, `actions/checkout` com `lfs: true`, `actions/cache` de `Library` com chave por hash de `Assets/**`, `Packages/**`, `ProjectSettings/**`; `game-ci/unity-test-runner@v4` com `testMode: all`, `unityVersion` lido de `ProjectVersion.txt` (auto), secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`; upload de artefatos de resultado). Deixe um comentário `# data-validate: adicionado no M02`.
- `build.yml` (on: schedule diário + workflow_dispatch): `game-ci/unity-builder@v4` `targetPlatform: StandaloneWindows64`, upload do artefato.
- Restrições: sem segredos no arquivo; versões de action fixadas por tag maior (`@v4`).
- Aceite: YAML válido.
- Verificação: `python -c "import yaml,sys;[yaml.safe_load(open(f)) for f in ['.github/workflows/ci.yml','.github/workflows/build.yml']]"` (ou `actionlint` se disponível) sem erro.
- Entrega: ≤ 80 palavras.

**T9 — Secrets e primeiro push (usuário)**
1. No GitHub do repositório → Settings → Secrets and variables → Actions: crie `UNITY_EMAIL`, `UNITY_PASSWORD` e `UNITY_LICENSE` (conteúdo do arquivo `.ulf` da licença Personal; caminho no Windows: `C:\ProgramData\Unity\Unity_lic.ulf`), conforme o guia de ativação do GameCI.
2. Autorize o push: o principal faz `git remote add origin <url>` e `git push -u origin main` e a branch do módulo.
3. Confirme no GitHub Actions que `core-tests` e `unity-tests` ficaram verdes.

**T10 — Integrar (principal)**
- Conferir critérios 1–8 com evidência (logs, `git ls-files`, Actions).
- Atualizar CLAUDE.md: §3 com a versão exata; §5 com a árvore real; §6 com o caminho exato do `$UNITY_EXE`, a pasta `Logs/` e os comandos testados (marcar como confirmados); §9 com pegadinhas encontradas (ex.: `-createProject` em pasta não vazia, primeira importação lenta, cache do GameCI).
- Atualizar `docs/ROADMAP.md` §7 (M00 ✅) e o Log deste arquivo. Commits por tarefa no formato `tipo(M00): …`.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
Esperado: 0 falhas nos três; Actions verde (`ci.yml`).

## Riscos e rollback
- `-createProject` recusar pasta existente → criar em scratch e mover (T4).
- Versões de pacote incompatíveis com 6000.3 → deixar o Package Manager resolver a versão verificada e travar no lock.
- GameCI e licença Personal (ativação muda com frequência) → seguir a doc atual do GameCI; se bloquear, manter `core-tests` obrigatório e marcar `unity-tests` como pendente no Log (não bloqueia M01, que é C# puro).
- Rollback: o projeto é novo; reverter = apagar `Assets/`, `Packages/`, `ProjectSettings/` e refazer T4.

## Atualizações de documentação
- CLAUDE.md §3, §5, §6, §9 (T10). `docs/ROADMAP.md` §7. Nenhum doc de GDD muda.

## Prompt de disparo
```
Implemente o módulo M00 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M00-fundacao-projeto.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M00-fundacao-projeto.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 plano criado.

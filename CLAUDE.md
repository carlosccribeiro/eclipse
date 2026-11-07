# PROJECT ECLIPSE — instruções para o Claude

Action Roguelite / ARPG isométrico 3D, single-player, PC Windows, Unity. Este arquivo vale para toda sessão neste repositório. **Mantenha-o atualizado** (ver §8).

## 1. Fonte da verdade (ordem de precedência)

1. `docs/gdd/00-convencoes.md` §5 — registro de resoluções R01+ (vence qualquer outro texto).
2. `docs/gdd/00-convencoes.md` §1–4 — contrato: dono de cada conceito, parâmetros globais, pipeline de dano, tags.
3. `docs/gdd/01..11-*.md` — detalhamento; cada conceito tem **um** doc dono (tabela 00 §2).
4. `docs/GDD-v0.3.md` — visão geral e índice (resumo; em divergência vale o doc dono).
5. `docs/ROADMAP.md` + `docs/roadmap/Mxx-*.md` — ordem de implementação, planos de ação e status.

Regra: **nenhum número de balanceamento é inventado no código.** Valores vêm dos docs → `DataSource/*.csv` → ScriptableObjects. Se o doc não define, pergunte ou registre a lacuna no módulo; não chute.

## 2. Mapa dos docs

| Doc | Escopo |
|---|---|
| 00-convencoes | Contrato, donos, resoluções |
| 01-atributos | Fórmulas, caps, tiers de iLvl, faixas, multiplicadores de slot |
| 02-equipamentos | Bases, pool por slot, 84 itens nomeados, Uniques |
| 03-modificadores | 206 modificadores (IDs `KAE-`, `LYR-`, `NYX-`, `ORU-`, `GEN-`, `SIG-`) |
| 04-conjuntos | 12 sets, 72 peças, 24 Set Powers |
| 05-loot | Drops, iLvl, pity, smart loot, região-lar |
| 06-combate | Pipeline de dano, input, frame data, estados, telegraphs |
| 07-resonance | 24 status, 15 Resonances, Generation |
| 08-herois | Kits, recursos, Aether Lattice, Augments |
| 09-inimigos | Bestiário, Elites, chefes, mapa da run, salas, Blessings |
| 10-progressao | Economia, moedas, morte, Sanctuary, endgame |
| 11-arquitetura | Stack Unity, assemblies, dados, save, performance, ferramentas |

## 3. Stack (decidido)

- **Unity 6.3 LTS — `6000.3.28f1`**, travada em `ProjectSettings/ProjectVersion.txt`. Não abrir o projeto em outra versão.
- URP Forward+, Input System, Cinemachine 3, Addressables, AI Navigation, Burst/Jobs/Collections/Mathematics (só fora de `Eclipse.Core`), Newtonsoft JSON, Unity Localization, UI Toolkit, FMOD for Unity, Steamworks.NET, Unity Test Framework.
- Repositório GitHub privado, Git LFS para binários, CI com GameCI (GitHub Actions).
- Arte até o Vertical Slice: **greybox + assets gratuitos** (primitivas, mannequins, Mixamo). Nada de asset pago sem decisão do usuário.

## 4. Regras de arquitetura (11-arquitetura)

- Assemblies dependem só "para baixo": `Core ← Data ← Gameplay ← AI ← Run ← Meta`; `UI`/`Audio` só reagem a eventos; Gameplay nunca referencia UI/Audio.
- `Eclipse.Core` é **C# puro**: sem `UnityEngine`, sem pacotes Unity; matemática própria. É testado por `dotnet test` (rápido) **e** por EditMode tests (canônico). (00 §5 R52)
- Definições em ScriptableObject com `StableId` imutável (slug). Save nunca guarda referência de asset; instâncias guardam `StableId` + `Roll01`.
- Simulação de combate a **60 Hz fixos**; frame data de 06 em frames a 60 fps.
- Combate com **0 B de GC por frame** como alvo; pooling obrigatório (inimigos, projéteis, VFX, números, drops, áudio).
- Tags (vocabulário 00 §3.4) em bitmask; **HitFlags** são separadas e não são tags.
- Anti-recursão: modelo **Generation** (nada dispara em Gen ≥ 2), exceção só `EchoRecursion1x` declarada.
- RNG por streams com seed (`Map`, `Encounter`, `Loot`, `Combat`, `Cosmetic`); nunca `UnityEngine.Random` em regras.
- Código, identificadores e comentários em **inglês**; docs em **português** (termos de jogo em inglês como no GDD).
- Blocos do Core (M01), use em vez de reinventar: `StableId`/`IdRemapTable` (`Eclipse.Core.Ids`), `Tag`/`TagMask`/`HitFlags` (`Eclipse.Core.Tags`), `RngStreams`/`Xoshiro128StarStar` (`Eclipse.Core.Random`), `SimulationClock` (`Eclipse.Core.Time`), `EventBus<T>`/`FrameEventBuffer<T>` (`Eclipse.Core.Events`), `IDefinition`/`IDefinitionRegistry` (`Eclipse.Core.Definitions`). Hash estável de string = `RngStreams.Fnv1a64`; nunca `string.GetHashCode` em regra/seed (é randomizado no .NET).

## 5. Estrutura

```
Assets/_Project/Code/{Core,Data,Gameplay,AI,Run,Meta,Save,UI,Audio,Platform,Editor,Tests/EditMode,Tests/PlayMode}
Assets/_Project/Content/{Heroes,Items,Modifiers,Affinities,Enemies,Bosses,Regions,Sanctuary}
DataSource/            CSV versionados (fonte dos SOs)
tools/CoreTests/       projeto dotnet que compila Eclipse.Core e roda os testes rápidos
docs/                  GDD (docs/gdd), roadmap (docs/ROADMAP.md), planos de módulo (docs/roadmap), relatórios (docs/roadmap/reports)
```
(Criada no módulo M00; atualize aqui se mudar.) Projeto Unity na raiz do repo. Setup idempotente das configurações: `Assets/_Project/Code/Editor/Setup/ProjectBootstrap.cs` (`Eclipse.Editor.Setup.ProjectBootstrap.Run`). 13 asmdefs `Eclipse.*` (Core, Data, Gameplay, AI, Run, Meta, Save, UI, Audio, Platform, Editor, Tests.EditMode, Tests.PlayMode); testes de Core em `Tests/EditMode/Core/<Área>/` com namespace `Eclipse.Core.<Área>.Tests` (compilados também por `tools/CoreTests`; filtro `--filter FullyQualifiedName~Eclipse.Core.<Área>`). Subpastas do Core: `Ids, Tags, Random, Time, Events, Definitions`.

## 6. Comandos de verificação

> Confirmados no M00. Feche o Editor antes de rodar batchmode (lock do projeto). Em bash: `UNITY_EXE="/c/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe"`. Logs ficam em `Logs/` (ignorada).

```bash
# Testes rápidos do núcleo (sem Unity)
dotnet test tools/CoreTests
```
```bash
# EditMode tests (canônico)
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
# PlayMode tests
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
# Importar CSV → SOs e validar dados
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
`$UNITY_EXE` = `C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe`.

## 7. Fluxo de trabalho por módulo

- Implementação acontece **por módulo** (`docs/roadmap/Mxx-*.md`), disparado pelo prompt de disparo do próprio arquivo, em sessão própria com worktree e branch `modulo/Mxx-slug`. No máximo **2 módulos em paralelo**, e só se o roadmap marcar a onda como paralela (pastas/assemblies disjuntos).
- Antes de começar: ler o plano do módulo, os docs citados nele e este arquivo. Não implementar fora do escopo do módulo; lacuna de design → registrar no Log do módulo e perguntar.
- **Definition of Done** de um módulo: critérios de saída do arquivo atendidos; `dotnet test` + EditMode (e PlayMode, se aplicável) passando; validador de dados sem erros; sem warnings novos de compilação; status e Log atualizados no arquivo do módulo e na tabela de `docs/ROADMAP.md`; este CLAUDE.md atualizado se algo durável mudou.
- Git: commits em português no formato `tipo(Mxx): descrição` (`feat`, `fix`, `refactor`, `test`, `docs`, `chore`). Sempre commitar os `.meta`. Nunca `--no-verify`. Push/PR só quando o usuário pedir.
- Worktree novo não tem `Library/`: o primeiro batchmode reimporta (demora); não commitar `Library/`, `Temp/`, `Logs/`, `UserSettings/`.

## 8. Quando atualizar este arquivo

Atualize o CLAUDE.md na mesma mudança que:
- cria/renomeia pastas, assemblies, comandos de build/teste ou scripts de ferramenta;
- adota uma convenção nova de código, dados, nomes ou git;
- descobre uma pegadinha durável (Unity, pacote, CI, plataforma);
- registra uma nova resolução em 00 §5 que afete como o código é escrito.

Não coloque aqui status de módulo (vai no ROADMAP) nem detalhes de design (vão nos docs).

## 9. Pegadinhas conhecidas

- O Editor aberto bloqueia o batchmode no mesmo projeto.
- Batchmode exige licença Unity ativada na máquina; no CI a licença vai como secret do GameCI.
- `-createProject` não aceita pasta não vazia: criar em scratch e mover `Assets/`, `Packages/`, `ProjectSettings/`.
- Em 6000.3 o `Fixed Timestep` do `TimeManager.asset` é racional (`m_Count`/`m_Rate`; 60 Hz = `m_Count: 2352000` com rate `141120000`), não um float.
- `tools/CoreTests` tem `nuget.config` próprio (a máquina não tem fonte NuGet global) e usa `net10.0`/C# 9.
- Arquivos gerados por script saem com CRLF no Windows; `.gitattributes` normaliza para LF no commit (avisos de CRLF são esperados).
- O Hub pode abrir o Editor no projeto sozinho; confira `tasklist | grep -i unity` antes do batchmode.
- CI (GameCI): secrets `UNITY_LICENSE` (conteúdo de `C:\ProgramData\Unity\Unity_lic.ulf`), `UNITY_EMAIL`, `UNITY_PASSWORD`. O Hub 3.x com licença Personal por entitlement não gera o `.ulf`; gere com Hub → Configurações → Licenças → Adicionar licença → licença Personal gratuita. No runner o `.ulf` não bate com a máquina e o GameCI cai para ativação por e-mail/senha (aviso esperado).
- Testes de alocação (`GC.GetAllocatedBytesForCurrentThread`) ficam em `#if !UNITY_5_3_OR_NEWER`: só valem no `dotnet test`, porque o Mono/instrumentação do Unity aloca.
- Subagents que editam `Eclipse.Core` rodam **em sequência**: todos compilam o mesmo `tools/CoreTests/obj` e builds concorrentes falham.
- Arquivos `.cs` criados fora do Editor não têm `.meta`; rode um batchmode (EditMode ou `-quit`) para gerá-los e commite junto.
- Dentro de `Eclipse.Core`, `Random` resolve para o namespace `Eclipse.Core.Random`, não `System.Random` (que nem deve ser usado em regras).
- Job com `game-ci/unity-test-runner` + `githubToken` precisa de `permissions: checks: write`, senão falha com "Resource not accessible by integration" mesmo com testes verdes.

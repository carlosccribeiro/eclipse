# M01 — Núcleo C# (StableId, Tags, RNG, Event Bus)
Fase 0 · Onda 1 · Status: ✅ concluído (2026-11-07)
Depende de: M00 · Desbloqueia: M02, M03, M04, M15 · Paralelo com: — (onda solo)
Branch: modulo/M01-nucleo-core
Docs de referência: 00 §3.4 (vocabulário de tags), §5 R02 (HitFlags), R04 (60 Hz), R07 (Generation), R52 (Core puro); 06 §3.3 (tabela de HitFlags); 11 §2 decisões 4, 5, 8, 16, 18; §3.4.1 (Definition, StableId, IdRemapTable, convenção de ID); §3.7 (Event Bus).

## Objetivo
Entregar os blocos básicos de `Eclipse.Core` em C# puro, sem `UnityEngine`, cobertos por testes que rodam em `dotnet test` e no EditMode: identidade de dados (`StableId`, registro de definições, remapeamento), vocabulário de tags e HitFlags, RNG determinístico por streams, relógio de simulação a 60 Hz e Event Bus sem alocação.

## Critérios de saída (verificáveis)
1. `StableId` valida o formato `<tipo>.<escopo>.<nome>` em snake_case (11 §3.4.1): regex `^[a-z][a-z0-9_]*(\.[a-z0-9][a-z0-9_]*){1,3}$`; IDs inválidos lançam `ArgumentException`; igualdade e hash por valor ordinal.
2. `IdRemapTable` resolve cadeias de renomeação (A→B→C) e detecta ciclos.
3. `TagMask` (bitmask `ulong`) contém exatamente as 24 tags de 00 §3.4, na ordem: `Attack, Spell, Melee, Ranged, Projectile, Area, DoT, Summon, Construct, Movement, Defensive, Ultimate, Channel, Trap, Clone, Basic, Heavy, Ember, Frost, Storm, Void, Echo, Ruin, Physical`; operações `Has/HasAll/HasAny/Union/Except`; parse e `ToString` por nome. Teste falha se o enum tiver tag fora do vocabulário.
4. `HitFlags` separado de `TagMask` com as 8 flags de 06 §3.3: `Unavoidable, Unblockable, Annihilation, Knockdown, Execute, IsEcho, IsResonance, IsTriggered` (R02). Nenhuma flag existe também em `TagMask`.
5. `Xoshiro128StarStar` reproduz o vetor de referência: estado `{1, 2, 3, 4}` → saídas `11520, 0, 5927040`; seeding por SplitMix64 a partir de `ulong`; `NextFloat01()` em [0, 1); `Range(int,int)` sem viés (rejeição).
6. `RngStreams` cria os 5 streams nomeados (`Map, Encounter, Loot, Combat, Cosmetic` — 11 dec. 16) derivados de uma seed de run; consumir um stream não altera a sequência dos outros (teste).
7. `SimulationClock`: `TickRate = 60`, conversões frames ↔ segundos (`FramesToSeconds(12) == 0.2f`), acumulador de tick fixo (R04).
8. `EventBus<T>` (`T : struct`): inscrição com prioridade inteira, dispatch síncrono em ordem de prioridade, remoção durante o dispatch segura, **0 B alocados** por `Publish` após aquecimento (medido com `GC.GetAllocatedBytesForCurrentThread()` no `dotnet test`); `FrameEventBuffer<T>` (ring buffer) para eventos coletados no frame (11 §3.7).
9. `IDefinition` / `IDefinitionRegistry` + `InMemoryDefinitionRegistry` (para testes) com detecção de `StableId` duplicado e resolução via `IdRemapTable`.
10. `dotnet test tools/CoreTests` e EditMode passam (0 falhas); `Assets/_Project/Code/Core/` não contém `using UnityEngine` (grep).

## Fora de escopo
- ScriptableObjects e importação de dados (M02).
- Stats, dano e CC (M03).
- `EntityEvents` de combate por entidade (M05 usa o `EventBus` como base).

## Premissas e decisões
- Namespaces `Eclipse.Core.<Área>` (`Ids`, `Tags`, `Random`, `Time`, `Events`, `Definitions`); código e comentários em inglês (CLAUDE.md §4).
- C# 9, sem `Span`-only APIs que não existam no .NET Standard 2.1 do Unity.
- Executores rodam **em sequência**: todos compilam o mesmo `tools/CoreTests` e builds concorrentes no mesmo `obj/` falham.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | StableId e IdRemapTable | execução | executor | — | `Assets/_Project/Code/Core/Ids/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Ids/*Tests.cs` | ✅ |
| T2 | TagMask e HitFlags | execução | executor | T1 | `Assets/_Project/Code/Core/Tags/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Tags/*Tests.cs` | ✅ |
| T3 | RNG xoshiro128** + streams | execução | executor | T2 | `Assets/_Project/Code/Core/Random/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Random/*Tests.cs` | ✅ |
| T4 | SimulationClock | execução | executor | T3 | `Assets/_Project/Code/Core/Time/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Time/*Tests.cs` | ✅ |
| T5 | EventBus e FrameEventBuffer | execução | executor | T4 | `Assets/_Project/Code/Core/Events/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Events/*Tests.cs` | ✅ |
| T6 | Registro de definições | execução | executor | T5 | `Assets/_Project/Code/Core/Definitions/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Definitions/*Tests.cs` | ✅ |
| T7 | Revisão independente do Core | investigação | investigador | T6 | — (relatório na resposta) | ✅ |
| T8 | Integrar, corrigir apontamentos, atualizar docs | integração | principal | T7 | `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo | ✅ |

## Ondas internas
1. T1 → T2 → T3 → T4 → T5 → T6 (sequencial; ver Premissas)
2. T7
3. T8

## Briefs

Contexto comum a T1–T6 (inclua no prompt de cada executor):
> PROJECT ECLIPSE é um ARPG em Unity 6.3 LTS. `Eclipse.Core` (`Assets/_Project/Code/Core/`) é C# 9 puro: **proibido** `using UnityEngine`, pacotes Unity e APIs fora do .NET Standard 2.1. Testes NUnit 3 em `Assets/_Project/Code/Tests/EditMode/Core/<Área>/`, compilados tanto pelo Unity (EditMode) quanto por `tools/CoreTests` (dotnet). Use só API NUnit comum. Namespaces `Eclipse.Core.<Área>`. Código e comentários em inglês. Não edite arquivos fora dos listados.

**T1 — StableId e IdRemapTable**
- Objetivo: tipo `StableId` (readonly struct) e `IdRemapTable`.
- Contexto: 11 §3.4.1 — todo dado de definição é identificado por slug imutável `<tipo>.<escopo>.<nome>` (ex.: `item.ashfang`, `mod.cleaving_strike.echo_slash`); save nunca guarda referência de asset; renomear ID exige entrada de remapeamento.
- Arquivos: escreve `Core/Ids/StableId.cs`, `Core/Ids/IdRemapTable.cs`, `Tests/EditMode/Core/Ids/StableIdTests.cs`, `IdRemapTableTests.cs`.
- Aceite: critérios 1 e 2; `StableId.TryParse` sem exceção; `default(StableId).IsEmpty`.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Ids` → 0 falhas.
- Entrega: ≤ 100 palavras — API pública, casos de teste.

**T2 — TagMask e HitFlags**
- Objetivo: vocabulário único de tags (00 §3.4) como bitmask e flags de hit separadas (R02, 06 §3.3).
- Arquivos: escreve `Core/Tags/Tag.cs` (enum com 24 valores na ordem do critério 3), `Core/Tags/TagMask.cs` (readonly struct sobre `ulong`), `Core/Tags/HitFlags.cs` (`[Flags] enum : ushort` com as 8 flags), testes em `Tests/EditMode/Core/Tags/`.
- Restrições: não adicionar tags fora do vocabulário (só 11-arquitetura pode, 00 §3.4); HitFlags **não** são tags.
- Aceite: critérios 3 e 4; teste com a lista literal das 24 tags comparando nomes do enum.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Tags` → 0 falhas.

**T3 — RNG**
- Objetivo: gerador xoshiro128** determinístico e streams com seed (11 dec. 16).
- Arquivos: escreve `Core/Random/Xoshiro128StarStar.cs` (struct, estado 4 × `uint`, `NextUInt`, `NextFloat01`, `Range(minInclusive, maxExclusive)` por rejeição, `Chance(p)`), `Core/Random/SplitMix64.cs`, `Core/Random/RngStream.cs` (enum `Map, Encounter, Loot, Combat, Cosmetic`), `Core/Random/RngStreams.cs` (deriva 5 geradores de `ulong runSeed` via SplitMix64 + índice do stream; `Fork(StableId)` para sub-seeds determinísticas, ex.: seed por item), testes.
- Aceite: critério 5 (vetor `11520, 0, 5927040` para estado `{1,2,3,4}` sem seeding) e 6; mesma seed ⇒ mesmas 1.000 primeiras saídas; `NextFloat01` nunca retorna 1.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Random` → 0 falhas.

**T4 — SimulationClock**
- Objetivo: base de tempo da simulação a 60 Hz (R04; frame data de 06 em frames a 60 fps).
- Arquivos: escreve `Core/Time/SimulationClock.cs` (`TickRate = 60`, `TickDuration`, `FramesToSeconds`, `SecondsToFrames` com arredondamento documentado, `Tick` counter, acumulador `Advance(float dt) → int ticks` com limite de ticks por frame), testes.
- Aceite: critério 7; `Advance(1f)` → 60 ticks; acumulador preserva o resto entre chamadas.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Time` → 0 falhas.

**T5 — EventBus**
- Objetivo: barramento tipado sem alocação e buffer de frame (11 §3.7).
- Arquivos: escreve `Core/Events/EventBus.cs` (`EventBus<T> where T : struct`; `Subscribe(handler, priority) → SubscriptionToken`; `Unsubscribe`; `Publish(in T)`; handler como delegate `EventHandler<T>(in T e)`), `Core/Events/FrameEventBuffer.cs` (ring buffer de capacidade fixa com política de overflow documentada), testes.
- Restrições: sem LINQ, sem closures no caminho de `Publish`; remoção durante dispatch não pula nem repete handlers.
- Aceite: critério 8 (teste de alocação: após 10 publicações de aquecimento, 1.000 `Publish` alocam 0 B).
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Events` → 0 falhas. (O teste de alocação pode ser marcado para rodar só fora do Unity com `#if !UNITY_5_3_OR_NEWER` se o EditMode alocar por instrumentação.)

**T6 — Registro de definições**
- Objetivo: contratos que o lado Unity (M02) implementa com ScriptableObjects.
- Arquivos: escreve `Core/Definitions/IDefinition.cs` (`StableId Id { get; }`), `Core/Definitions/IDefinitionRegistry.cs` (`TryGet<T>(StableId, out T)`, `Get<T>`, `All<T>()`, resolve via `IdRemapTable`), `Core/Definitions/InMemoryDefinitionRegistry.cs`, `Core/Definitions/DuplicateDefinitionException.cs`, testes.
- Aceite: critério 9; ID remapeado resolve para a definição nova.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Definitions` → 0 falhas.

**T7 — Revisão independente**
- Objetivo: auditar o Core entregue contra os critérios de saída 1–10 e contra R52.
- Arquivos: lê `Assets/_Project/Code/Core/**`, `Tests/EditMode/Core/**`, `docs/roadmap/M01-nucleo-core.md`, `docs/gdd/00-convencoes.md` §3.4/§5, `docs/gdd/11-arquitetura.md` §3.4.1/§3.7.
- Verificação: rode `dotnet test tools/CoreTests` e `grep -rn "UnityEngine" Assets/_Project/Code/Core` (esperado: nada).
- Entrega: ≤ 200 palavras — por critério: atende/não atende com evidência `arquivo:linha`; riscos de API que vão doer em M02–M05.

**T8 — Integrar (principal)**
- Corrigir apontamentos da T7 (novo brief ao executor se preciso); rodar a verificação do módulo; registrar em CLAUDE.md §4/§5 as convenções novas (namespaces do Core, local dos testes compartilhados, regra de executores sequenciais no Core); ROADMAP §7 e Log.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
Esperado: 0 falhas nos dois; `grep -rn "using UnityEngine" Assets/_Project/Code/Core` sem resultado; CI verde.

## Riscos e rollback
- Diferença de comportamento entre o runtime do dotnet e o Mono do Unity (ex.: alocação, float) → todo teste roda nos dois; testes de alocação só no dotnet.
- API do EventBus inadequada para combate → M05 pode estender; mudanças de assinatura exigem atualizar este módulo e CLAUDE.md.
- Rollback: `git revert` dos commits do módulo.

## Atualizações de documentação
- CLAUDE.md §4 (convenções do Core), §5 (subpastas do Core). ROADMAP §7. Nenhum doc de GDD muda.

## Prompt de disparo
```
Implemente o módulo M01 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M01-nucleo-core.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M01-nucleo-core.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 plano criado.
- 2026-11-07 T1–T6 executados em sequência (executor), cada um verificado com `dotnet test` e commitado (`feat(M01)`). Decisões: StableId com validação manual por char equivalente à regex (2–4 segmentos); `IdRemapTable.Add` rejeita entradas que fecham ciclo; parse de `TagMask` por `|`/`,`, case-sensitive e sem números; seeds dos streams `SplitMix64(runSeed ^ ((i+1)·0xD1B54A32D192ED03))`; `Fork` = seed do stream ^ FNV-1a 64 do id (^ salt·φ64); `Chance` sempre consome 1 saída; delegate `InEventHandler<T>` (evita colisão com `System.EventHandler<T>`); prioridade maior roda primeiro, empate por ordem de inscrição; inscrição durante dispatch é adiada; `FrameEventBuffer` sobrescreve o mais antigo e conta `DroppedCount`; `SimulationClock` com acumulador em double (ε 1e-9 s), `MaxTicksPerAdvance` 8 e `DroppedTicks`.
- 2026-11-07 T7 (revisão independente): critérios 1–9 atendidos. 2 bugs corrigidos na T8: `Advance(+∞/float.MaxValue)` corrompia o relógio, e um token de outro bus removia a inscrição errada. `Fork` ganhou salt. Os riscos de API para M02/M05/M12/M18 estão em ROADMAP §9.
- 2026-11-07 T8: `dotnet test tools/CoreTests` 132/132; EditMode no Unity 131/131 (sem os testes de alocação, que são só do dotnet); nenhum `using UnityEngine` em Core; build sem warnings. Testes de alocação 0 B só rodam no dotnet (`#if !UNITY_5_3_OR_NEWER`).

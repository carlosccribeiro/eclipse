# M03 — Stats e dano (StatBlock, atributos, DamageResolver, DR de CC)
Fase 0 · Onda 2 · Status: ⏳ não iniciado
Depende de: M01 · Desbloqueia: M05, M08, M11, M12 · Paralelo com: M02 (worktree separado; pastas disjuntas)
Branch: modulo/M03-stats-dano
Docs de referência: 00 §3.2 (pipeline canônico), §5 R02, R07, R08, R09, R12, R13, R52; 01 §3.1 (DR), §3.2 (primários, Core Attunement), §3.3 (base dos heróis), §3.4 (mitigação recebida e causada), §3.5 (onde entram os ofensivos), §3.6 (caps), §3.7, §3.8 (tradução de recurso), §3.12 (exemplo numérico); 06 §3.1–§3.3 (tipos, pipeline ofensivo e recebido, HitFlags), §3.8.3 (CC e DR); 11 §3.5 (StatMod, StatBlock, avaliação, cache).

## Objetivo
Implementar em `Eclipse.Core` (C# puro, R52) o modelo de stats e as fórmulas de combate que todo o resto usa: `StatMod`/`StatBlock` com buckets Flat/Increased/More, diminishing returns e caps de 01, regras de primários e tradução de recurso, `DamageResolver` ofensivo idêntico a 06 §3.2, resolvedor de dano recebido de 06 §3.3, `CrowdControlTracker` com a DR única de 06 §3.8.3 e as regras de Generation de 06/07 — tudo coberto por testes que reproduzem os números dos docs.

## Critérios de saída (verificáveis)
1. Stat plano: `v = (Base + ΣFlat) × (1 + ΣIncreased) × Π(1 + More)` e depois cap/DR (11 §3.5.2; 01 §3.1). Teste: Attack Speed Raw +80% → efetivo +65%; Raw +200% → +100% (soft 50 / hard 100, 01 §3.6).
2. `StatBlock` registra e remove mods por `SourceHandle`, marca dirty por stat, emite `StatsChanged(mask)` (11 §3.5.1/§3.5.3); filtro por `RequiredTags`/`ExcludedTags` e `Condition`.
3. Caps e DR vêm de um `IStatRules` alimentado por dados (fixtures de teste com os valores de 01 §3.6); **nenhum** valor de cap hardcoded no código de runtime.
4. Primários e base de herói (01 §3.2–§3.3): efeitos universais por ponto, Secundário por herói, Core Attunement `More = PontosCore × 0,08%`, crescimento por nível `Base + (nv − 1) × PorNível`.
5. Tradução de recurso (01 §3.8) para os 4 heróis, incluindo teto 6 e exceção até 8 (R01) parametrizada por dados.
6. **Exemplo de 01 §3.12 reproduzido em teste** (tolerância ±0,5 pp nos percentuais, ±1% nos absolutos): Maximum Health 2958; Armor 4835; ArmorMit 50,4%; MitPhysical 54,4%; Elemental Res 28,8%; Void Res 6,8%; Critical Chance 21,6%; Critical Damage ×2,3; Attack Speed +24,9%; Dodge 7,5%; Fury máxima 115; Resource Generation +23%; Core Attunement +31,8% More; EHP físico ≈ 6487.
7. **Cleaving Strike do exemplo** (01 §3.12; pipeline 06 §3.2): com WeaponPower 65, coef. 220%, Increased +85%, More ×1,318, alvo Armor 30% e ArmorPen 10% → não-crítico ≈ 255, crítico ≈ 586, valor esperado ≈ 326.
8. Pipeline ofensivo (06 §3.2 passos 1–8) por componente: dano plano adicionado; ganho-como-extra calculado sobre a Base pré-conversão; conversão com soma ≤ 100% e excedente normalizado, sem cadeia; Increased filtrado pelo tipo final + tags da fonte (exemplo de 06: Cleaving Strike com Ruin convertido soma Ruin + Skill + Area, não Physical); crit rolado uma vez por hit (stream `Combat`); Ticks não critam; mitigação por família (Physical: `ArmorAlvo% × (1 − ArmorPen)`, Elemental, Void — Echo e Ruin mitigados por Void Resistance, 06 §3.1); piso −20% de resistência sob status (01 §3.4.2); Vulnerability somada com teto +60% (R12); mínimo 1 por hit.
9. Dano recebido (06 §3.3; 01 §3.4.1): i-frames e Parry negam conforme a tabela de HitFlags (`Unavoidable` não ignora i-frames; `Annihilation` ignora tudo; `Unblockable` ignora Block e Parry); Dodge só em golpes não telegrafados e nunca em Ticks/`Unavoidable`; Armor `K = 50·eLvl + 500`, cap 75%; Physical Res cap 30% multiplicativa; resistências com penalidade de dificuldade, cap 75% e mínimo −50%; Block −40% (arco frontal 180° recebido como booleano do chamador), cap 50%; Damage Reduction cap 40%; Shield antes da vida. Testes com a tabela de 01 §3.4.1 (Armor 500 vs eLvl 1 → 47,6%; 2500 vs eLvl 40 → 50,0%; 5000 vs eLvl 85 → 51,3%; 9000 vs eLvl 85 → 65,5%; 2500 vs eLvl 1 → 75% cap).
10. `CrowdControlTracker` (06 §3.8.3, R09): grupos Hard (Stun+Freeze), Root, Silence, Taunt; janela de 8 s a partir do 1º CC do grupo: 100% / 50% / 25% / imune; no jogador: Hard máx. 1,5 s, Freeze máx. 1,0 s, 2 s de imunidade a Hard após qualquer Hard, Root máx. 2 s; Status Resistance reduz duração (01 §3.4.1). Tempo pelo `SimulationClock` (M01).
11. `ProcContext`/regras de Generation (06 §3.2 tabela; R07): Gen 0 original, procs geram Gen+1, nada dispara em Gen ≥ 2; réplica (`IsEcho`) não gera réplica salvo `EchoRecursion1x` (uma vez, ainda com Gen < 2); Proc Coefficient fixos: Echo 0,5, clone 0,4, tick de área 0,25 (multiplicando o coeficiente da fonte); Execute: limiar máx. 20% em comuns, Elite metade (máx. 10%), Guardian/chefe imune com conversão única de +25% Vulnerability abaixo de 20% de vida (R08).
12. `dotnet test tools/CoreTests` e EditMode passam (0 falhas); `grep -rn "using UnityEngine" Assets/_Project/Code/Core` vazio; nenhum número de balanceamento em código de runtime fora de constantes de regra citadas em comentário com doc e seção (ex.: `// 06 §3.2: Vulnerability cap` só se for regra estrutural; valores numéricos ajustáveis vão para `IStatRules`/dados).

## Fora de escopo
- ScriptableObjects e CSV de stats (`StatDefinition`, `Stats.csv`): o primeiro módulo de gameplay que precisar de dados reais (M05) cria a tabela usando o pipeline do M02 e adapta para `IStatRules`.
- Status de afinidade e Resonance (M11), modificadores (M10), Stagger/Broken (M05/M17), itens e faixas de rolagem (M12).

## Premissas e decisões
- Constantes **estruturais** do contrato (ex.: teto de Vulnerability 60%, Generation máx. 2, mínimo 1 por hit, 60 Hz) podem existir como constantes nomeadas no Core com comentário de origem; valores **de balanceamento** (caps de stats, coeficientes, faixas) entram via `IStatRules`/perfis de dados. Na dúvida, vai para dados.
- Os testes usam fixtures com os valores dos docs e citam a seção em comentário; quando o doc mudar, o teste muda junto.
- Executores rodam **em sequência** (compartilham o build de `tools/CoreTests`).
- Namespaces: `Eclipse.Core.Stats`, `Eclipse.Core.Attributes`, `Eclipse.Core.Damage`, `Eclipse.Core.CrowdControl`, `Eclipse.Core.Procs`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | StatId, StatMod, buckets, StatBlock, IStatRules, função de DR | execução | executor | — | `Assets/_Project/Code/Core/Stats/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Stats/*Tests.cs` | ⏳ |
| T2 | Primários, base de herói, Core Attunement, tradução de recurso | execução | executor | T1 | `Assets/_Project/Code/Core/Attributes/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Attributes/*Tests.cs` | ⏳ |
| T3 | Regras de Generation, Proc Coefficient e Execute | execução | executor | T1 | `Assets/_Project/Code/Core/Procs/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Procs/*Tests.cs` | ⏳ |
| T4 | DamageInstance e DamageResolver ofensivo | execução | executor | T2, T3 | `Assets/_Project/Code/Core/Damage/DamageInstance.cs`, `DamageComponent.cs`, `OffensiveDamageResolver.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Damage/OffensiveResolverTests.cs` | ⏳ |
| T5 | Resolvedor de dano recebido | execução | executor | T4 | `Assets/_Project/Code/Core/Damage/IncomingDamageResolver.cs`, `MitigationMath.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Damage/IncomingResolverTests.cs` | ⏳ |
| T6 | CrowdControlTracker | execução | executor | T1 | `Assets/_Project/Code/Core/CrowdControl/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/CrowdControl/*Tests.cs` | ⏳ |
| T7 | Teste integrado do exemplo 01 §3.12 | execução | executor | T5, T6 | `Assets/_Project/Code/Tests/EditMode/Core/Integration/KaelReferenceExampleTests.cs`, `Assets/_Project/Code/Tests/EditMode/Core/Integration/Fixtures/*.cs` | ⏳ |
| T8 | Auditoria de fórmulas contra 01/06 | investigação | investigador | T7 | — (relatório na resposta) | ⏳ |
| T9 | Corrigir apontamentos e integrar | integração | principal | T8 | `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo | ⏳ |

## Ondas internas
1. T1
2. T2 → T3 → T6 (sequenciais por build compartilhado; logicamente independentes)
3. T4
4. T5
5. T7
6. T8
7. T9

## Briefs

Contexto comum (inclua em todos):
> PROJECT ECLIPSE é um ARPG em Unity 6.3 LTS. `Eclipse.Core` (`Assets/_Project/Code/Core/`) é C# 9 puro (proibido `UnityEngine` e pacotes Unity — 00 §5 R52). Testes NUnit 3 em `Assets/_Project/Code/Tests/EditMode/Core/<Área>/`, rodando em `dotnet test tools/CoreTests` e no EditMode. Já existem no Core (M01): `StableId`, `TagMask`/`Tag` (24 tags), `HitFlags`, `Xoshiro128StarStar`/`RngStreams` (stream `Combat`), `SimulationClock` (60 Hz), `EventBus<T>`. **Fonte da verdade das fórmulas:** `docs/gdd/01-atributos.md` e `docs/gdd/06-combate.md`, com precedência de `docs/gdd/00-convencoes.md` §5. Valores de balanceamento não vão hardcoded em runtime: use `IStatRules`/perfis de dados; nos testes, fixtures com os valores do doc e comentário citando a seção. Código/comentários em inglês. Não edite arquivos fora dos listados.

**T1 — Stats**
- Objetivo: modelo de stats de 11 §3.5 com DR de 01.
- Arquivos: escreve `Core/Stats/StatId.cs` (enum com todos os atributos de GDD §6.3 + os listados em 01 §3.5–§3.8: primários, ofensivos incl. `StatusPotency`, defensivos, utilitários, recurso, mais stats derivados necessários: `MaximumHealth`, `Armor`, `PhysicalResistance`, `ElementalResistance`, `VoidResistance`, `MovementSpeed`), `ModBucket.cs` (`Flat, Increased, More`), `StatMod.cs`, `SourceHandle.cs`, `ConditionId.cs`, `IStatRules.cs` (`StatRule { SoftCap?, HardCap?, AboveSoftFactor, Minimum? }`), `DiminishingReturns.cs` (01 §3.1), `StatBlock.cs` (add/remove por fonte, dirty por stat, `Evaluate(StatId, TagMask context, IConditionEvaluator)`, evento `StatsChanged` com máscara), testes.
- Aceite: critérios 1–3; teste de DR com Attack Speed, CDR (soft 25/hard 40), Dodge (20/40) usando fixture de 01 §3.6.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Stats` → 0 falhas.
- Entrega: ≤ 120 palavras — API, stats incluídos.

**T2 — Atributos de herói**
- Objetivo: regras de 01 §3.2, §3.3 e §3.8 como funções puras configuradas por dados.
- Arquivos: escreve `Core/Attributes/HeroAttributeProfile.cs` (Core/Secundário/Terciário, base e por nível de cada stat — preenchido por dados), `PrimaryAttributeRules.cs` (efeitos universais por ponto e do Secundário; Core Attunement como mod More nomeado), `HeroLevelScaling.cs`, `ResourceTranslation.cs` (4 tipos de recurso: Pool 0–100 Fury/Echo Energy cap 150; Charges e Constructs com +1 a cada 25%/30%, teto 6, exceção até 8 informada por parâmetro — R01; "Resto" convertido em bônus), testes com fixtures de 01 §3.2–§3.3 (Kael nível 50: STR 167, DEX 113, INT 59, Health base 1318, Armor base 1570).
- Aceite: critérios 4 e 5.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Attributes` → 0 falhas.

**T3 — Generation, Proc Coefficient, Execute**
- Objetivo: regras anti-recursão e de execução (06 §3.2; 07; R07, R08).
- Arquivos: escreve `Core/Procs/ProcContext.cs` (`Generation` 0–2, `HitFlags`, `SourceChain` curta, `ProcCoefficient`, `AllowEchoRecursionOnce`), `ProcRules.cs` (`CanTrigger`, `NextGeneration`, `CanReplicate`, `DerivedProcCoefficient(kind)` com Echo 0,5 / Clone 0,4 / tick de área 0,25 vindos de parâmetros), `ExecuteRules.cs` (limiar efetivo por classe de alvo; conversão contra chefe em Vulnerability), testes.
- Aceite: critério 11; teste "Gen 2 não dispara", "réplica de réplica só com EchoRecursion1x e uma única vez", "Elite com limiar 20% recebe 10%", "chefe abaixo de 20% recebe +25% Vulnerability uma única vez com duas fontes de Execute".
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Procs` → 0 falhas.

**T4 — DamageResolver ofensivo**
- Objetivo: pipeline exato de 06 §3.2 (espelhado em 11 §3.5.2), por componente.
- Arquivos: escreve `Core/Damage/DamageType.cs` (Physical + 6 afinidades; família de mitigação: Physical / Elemental [Ember, Frost, Storm] / Void [Void, Echo, Ruin]), `DamageComponent.cs`, `DamageInstance.cs` (fonte, alvo, componentes, `TagMask`, `HitFlags`, Hit/Tick, `IsCrit`, `ProcCoefficient`, `Generation`, `StaggerValue`, `HitGroupId` — 11 §3.5.2), `OffensiveDamageResolver.cs` (entradas: base, adicionados planos, ganhos-como-extra, conversões, `StatBlock` do atacante, `TargetDefense` com Armor%/Res% por tipo e Vulnerability acumulada, RNG `Combat`; saída: componentes finais), `Tests/EditMode/Core/Damage/OffensiveResolverTests.cs`.
- Aceite: critério 8 e o caso do critério 7 isolado (Base 143, Increased +85%, More ×1,318, mitigação 27% → 254,5 ± 1; crítico ×2,3; esperado `NãoCrit × (1 + 0,216 × 1,3)` ≈ 326). Teste de conversão com o exemplo de 06 (Ruin convertido não recebe Physical Damage). Teste do teto +60% de Vulnerability. Teste de Tick sem crit e de mínimo 1.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Damage` → 0 falhas.

**T5 — Dano recebido**
- Objetivo: pipeline de 06 §3.3 com mitigação de 01 §3.4.1.
- Arquivos: escreve `Core/Damage/MitigationMath.cs` (`ArmorMitigation(armor, eLvl)` com `K = 50·eLvl + 500` e cap; physical em duas camadas; resistência com penalidade de dificuldade, cap e piso −50%), `IncomingDamageResolver.cs` (ordem: negação por i-frames/Parry → Dodge → mitigação → Block → Damage Reduction → Shield → vida; entradas explícitas `IsTelegraphed`, `BlockArcValid`, `DifficultyResPenalty`), `Tests/EditMode/Core/Damage/IncomingResolverTests.cs`.
- Aceite: critério 9 com a tabela de Armor de 01 §3.4.1 e uma matriz de HitFlags × defesas (i-frames, Parry, Dodge, Block) conforme a tabela de 06 §3.3.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Damage` → 0 falhas.

**T6 — CrowdControlTracker**
- Objetivo: DR única de CC do jogo (06 §3.8.3; R09).
- Arquivos: escreve `Core/CrowdControl/CcGroup.cs` (`Hard, Root, Silence, Taunt`), `CcKind.cs` (Stun, Freeze, Root, Silence, Taunt), `CrowdControlTracker.cs` (por entidade; `TryApply(kind, baseDuration, isPlayer, statusResistance) → appliedDuration`; janela de 8 s; regras do jogador; tempo em ticks do `SimulationClock`), `CcRules.cs` (parâmetros: janela, sequência 100/50/25, tetos do jogador — via dados), testes.
- Aceite: critério 10; teste da sequência Stun → Freeze → Stun na mesma janela (100%/50%/25%) e 4º imune; jogador: Freeze 2 s pedido → 1,0 s aplicado, Hard seguinte dentro de 2 s → 0.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.CrowdControl` → 0 falhas.

**T7 — Exemplo integrado**
- Objetivo: prova de que o Core reproduz o exemplo numérico de referência do GDD.
- Arquivos: escreve `Tests/EditMode/Core/Integration/KaelReferenceExampleTests.cs` e fixtures em `Tests/EditMode/Core/Integration/Fixtures/` (perfil do Kael nível 50, equipamento do exemplo como `StatMod`s, regras de 01 §3.6, alvo eLvl 85 em Eclipse).
- Aceite: critérios 6 e 7, montando o personagem **pelas APIs** de T1–T5 (não recalculando fórmulas no teste).
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.Integration` → 0 falhas.
- Entrega: ≤ 120 palavras — tabela "esperado × obtido".

**T8 — Auditoria de fórmulas**
- Objetivo: revisão independente do código contra os docs, sem ver a conversa original.
- Arquivos: lê `Assets/_Project/Code/Core/{Stats,Attributes,Damage,CrowdControl,Procs}/**`, os testes, `docs/gdd/01-atributos.md` §3.1–§3.8 e §3.12, `docs/gdd/06-combate.md` §3.1–§3.3 e §3.8.3, `docs/gdd/00-convencoes.md` §3.2 e §5 (R02, R07, R08, R09, R12, R13).
- Verificação: `dotnet test tools/CoreTests`; `grep -rn "using UnityEngine" Assets/_Project/Code/Core` vazio; procurar números mágicos de balanceamento em runtime.
- Entrega: ≤ 250 palavras — divergências fórmula a fórmula com `arquivo:linha` e seção do doc; números hardcoded encontrados; critérios 1–12 atende/não atende.

**T9 — Integrar (principal)**
- Corrigir apontamentos da T8; rodar a verificação do módulo; CLAUDE.md §4 (regra "constante estrutural × valor de balanceamento" e namespaces do Core); ROADMAP §7 e Log; registrar no Log a pendência de `StatDefinition`/`Stats.csv` para o M05.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
Esperado: 0 falhas nos dois; teste `KaelReferenceExampleTests` verde; CI verde.

## Riscos e rollback
- Ambiguidade de doc (ex.: arredondamento, ordem Block × mitigação) → seguir 06 §3.3 (dono) e registrar no Log; se 01 e 06 discordarem, parar e perguntar (00 §5 vence; senão o dono do conceito).
- Float x determinismo entre Mono e .NET → tolerâncias nos testes; RNG sempre pelo stream.
- Rollback: `git revert` dos commits do módulo.

## Atualizações de documentação
- CLAUDE.md §4 (regra de constantes e namespaces). ROADMAP §7. Se a implementação revelar erro de fórmula num doc, corrigir o doc dono e registrar em 00 §5 (com aprovação do usuário).

## Prompt de disparo
```
Implemente o módulo M03 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M03-stats-dano.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M03-stats-dano.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 plano criado.

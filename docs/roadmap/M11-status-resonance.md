# M11 — Status e Resonance
Fase 2 · Onda 8 · Status: ⏳ não iniciado
Depende de: M03, M10 · Desbloqueia: M14, M21, M22, M23 · Paralelo com: M13
Branch: modulo/M11-status-resonance
Docs de referência: 07 (todo: §3 aplicação, §4 24 status, §4.7 Vulnerability, §4.8 exceções, §5 15 Resonances, §6 prioridade, §7 limites, §8 Elites/Chefes, §10 legibilidade); 06 §3.2 (DoT 0,5 s), §6 (DR de CC), §8 (VFX/legibilidade); 01 §3.5–3.7 (Status Potency, Status Resistance, Duration of Effects); 11 §3.8.1–3.8.2, §3.14, §3.16; 00 §5 R09, R10, R12, R19, R49

## Objetivo
Implementar o sistema de status de afinidade (24 status de 07 §4) e o sistema de Resonance (15 combinações de 07 §5), com buildup, stacking, ticks, DR de CC compartilhada, conversões contra chefes, bucket de Vulnerability com teto de +60% e todos os limites anti-explosão de 07 §7. A lógica fica no Core (testável em `dotnet test`); o Gameplay executa em lote. Cada status tem legibilidade visual (cor, forma, ícone) conforme 07 §10.

## Critérios de saída (verificáveis)
1. 24 status importados de `DataSource/status_effects.csv` e 15 Resonances de `DataSource/resonance_rules.csv`, validados com 0 erros.
2. Buildup (07 §3.2): SP por tipo de golpe (Basic 15, Skill 30, Heavy 40, Ultimate 100, ticks 0, derivados 50%, Gen 2 50%), × (1 + Status Potency), frações de multi-afinidade (< 10% ignoradas), decaimento de 25% do limiar/s após 1,5 s, multiplicadores de resistência (Elite ×1,5, Guardian ×2,0, Chefe ×2,5 — 07 §3.3) — tudo coberto por `dotnet test`.
3. DoT e zonas ticam a cada 0,5 s (R19); o job de lote roda a 0,25 s só como agendamento (11 §3.8.1).
4. Freeze entra no grupo Hard do `CrowdControlTracker` de 06 (janela 8 s: 100/50/25%/imune; jogador: Freeze ≤ 1,0 s, Hard ≤ 1,5 s, 2 s de imunidade); exceção de Ultimate (Freeze até 3 s em não-chefes) e Overload Circuit (+2 stacks de Shock) declaradas por fonte (R49).
5. Chefes/Guardians: CC vira Stagger, exceto **Frostbound** e **Distortion** (+10% Vulnerability a dano Void, ICD 6 s) (R10), testado com dummy marcado como chefe.
6. Vulnerability total limitada a +60% (R12) com as fontes de 07 §4.7 (Ruin e Shock em stacks; demais valor fixo por fonte, sem acumular consigo mesmas).
7. ResonanceSystem: lookup O(1) por (status presente, afinidade do hit); prioridade de 07 §6.2; 1 Resonance por evento por alvo; ICD por alvo por regra; teto de 8 procs/s por jogador (descarta o de menor dano estimado); demais tetos de 07 §7 (Explosions 4/s, Chain 6 saltos e ICD 0,2 s, zonas, clones, Echo Mark); DRef de 07 §3.1 (média dos Increased das duas afinidades; Skill Damage não se aplica).
8. As 15 Resonances com teste PlayMode próprio; o `RecursionStressTest` do M10 estendido com Resonances: ≤ 400 eventos de dano/s, nenhum Gen > 2, ≤ 8 Resonances/s.
9. Ícones de status sobre inimigos (máx. 4, prioridade definida), VFX placeholder com a paleta de 07 §10.1, VFX do jogador abaixo da camada de telegraphs (11 §3.14).
10. `dotnet test`, EditMode, PlayMode verdes; validador sem erros.

## Fora de escopo
- Codex de Resonance do Archivist (M19).
- Infusions e itens que concedem afinidade (dados de item são M12/M28; aplicação via equipamento é M13). Aqui as fontes de afinidade para teste vêm de um `AffinityInjector` de debug.
- Validação de acessibilidade de Resonance por build com ≤ 2 itens (07 §5.3) — feita no M14 (simulador).
- Status aplicados por inimigos específicos (bestiário em M16/M24+); o sistema suporta, os dados chegam depois.
- VFX final (trilha de arte A0x).

## Premissas e decisões
- **Premissa:** M03 entregou `CrowdControlTracker`, bucket de Vulnerability no `DamageResolver` e as `StatDefinition`s de Status Potency, Status Resistance e Duration of Effects (01). Se alguma stat faltar, este módulo adiciona a linha em `DataSource/stats.csv` (M13, em paralelo, **não** edita esse arquivo).
- **Premissa:** M10 entregou o serviço Generation/Proc Coefficient (`Core/Procs`) e os hooks `OnHitDealt`/`OnStatusApplied`; M06 entregou um renderer world-space em lote para barras/números que aceita ícones.
- **Decisão:** regras puras em `Core/Status` e `Core/Resonance`; execução em lote (SoA/`NativeArray`, Jobs/Burst) em `Gameplay/Status`.
- **Decisão:** os efeitos de status/Resonance são `EffectDefinition`s reutilizáveis (mesma biblioteca de efeitos de M08/M10), organizados por afinidade.
- **Paralelo com M13:** este módulo não escreve em `Core/Loadout`, `Core/Sets`, `Gameplay/Equipment`, `UI/Inventory`, `UI/Builds`, `UI/Sets`, `Data/Sets` nem em CSVs de itens/sets.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Mapear APIs de M03/M05/M06/M10 usadas por status/Resonance | investigação | investigador | — | — | ⏳ |
| T2 | Contratos do Core para status e Resonance | design | principal | T1 | `Assets/_Project/Code/Core/Status/Contracts/*.cs`, `Assets/_Project/Code/Core/Resonance/Contracts/*.cs` | ⏳ |
| T3 | `StatusEngine` (Core): buildup, resistência, stacking, duração, ticks | execução | executor | T2 | `Assets/_Project/Code/Core/Status/Engine/*.cs`, `tools/CoreTests/Status/*.cs` | ⏳ |
| T4 | `ResonanceEngine` (Core): índice, prioridade, ICD, tetos, DRef | execução | executor | T2 | `Assets/_Project/Code/Core/Resonance/Engine/*.cs`, `tools/CoreTests/Resonance/*.cs` | ⏳ |
| T5 | Dados: definições, importadores, validação e CSVs de afinidades, status e Resonances | execução | executor | T2 | `Assets/_Project/Code/Data/{Affinities,Status,Resonance}/*.cs`, `Assets/_Project/Code/Editor/DataPipeline/Importers/{Affinity,StatusEffect,ResonanceRule}TableImporter.cs`, `Assets/_Project/Code/Editor/Validation/Rules/{Status,Resonance}Rules.cs`, `DataSource/{affinities,status_effects,resonance_rules}.csv` | ⏳ |
| T6 | `StatusSystem` em lote (Gameplay) + DR de CC, conversões de chefe, Vulnerability, `AffinityInjector` | execução | executor | T3, T5 | `Assets/_Project/Code/Gameplay/Status/System/*.cs`, `Assets/_Project/Code/Gameplay/Status/Debug/AffinityInjector.cs` | ⏳ |
| T7a | Efeitos Ember + Frost (Burning, Explosion, Ignition, Combustion, Chill/Slow, Freeze, Shatter, Frozen Armor) | execução | executor | T6 | `Assets/_Project/Code/Gameplay/Status/Effects/{Ember,Frost}/*.cs`, `Assets/_Project/Code/Tests/PlayMode/Status/{Ember,Frost}/*.cs` | ⏳ |
| T7b | Efeitos Storm + Void (Shock, Chain Lightning, Overload, Static Field, Pull, Implosion, Corruption, Absorption) | execução | executor | T6 | `Assets/_Project/Code/Gameplay/Status/Effects/{Storm,Void}/*.cs`, `Assets/_Project/Code/Tests/PlayMode/Status/{Storm,Void}/*.cs` | ⏳ |
| T7c | Efeitos Echo + Ruin (Repeat, Clone, Reflection, Echo Mark, Decay, Vulnerability, Weakening, Detonation) | execução | executor | T6 | `Assets/_Project/Code/Gameplay/Status/Effects/{Echo,Ruin}/*.cs`, `Assets/_Project/Code/Tests/PlayMode/Status/{Echo,Ruin}/*.cs` | ⏳ |
| T8 | `ResonanceSystem` (Gameplay) + efeitos das 15 Resonances | execução | executor | T4, T7a, T7b, T7c | `Assets/_Project/Code/Gameplay/Resonance/*.cs`, `Assets/_Project/Code/Gameplay/Status/Effects/Resonance/*.cs`, `Assets/_Project/Code/Tests/PlayMode/Resonance/*.cs` | ⏳ |
| T9 | Legibilidade: ícones de status world-space, VFX placeholder por afinidade | execução | executor | T6 | `Assets/_Project/Code/UI/WorldStatus/*.cs`, `Assets/_Project/Content/Affinities/VFX/*`, `Assets/_Project/Content/Affinities/Icons/*` | ⏳ |
| T10 | Estender o teste de recursão com Resonances | execução | executor | T8 | `Assets/_Project/Code/Tests/PlayMode/Stress/RecursionStressTest.cs`, `tools/CoreTests/Resonance/ResonanceStressTests.cs` | ⏳ |
| T11 | Integração, verificação final e documentação | revisão | principal | T9, T10 | `CLAUDE.md` (se aplicável), `docs/ROADMAP.md`, este arquivo | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T4 ∥ T5
4. T6
5. T7a ∥ T7b ∥ T7c ∥ T9 (pastas por afinidade e UI disjuntas)
6. T8
7. T10
8. T11

## Briefs

### T1 — Mapear APIs
Tarefa T1: Mapear as APIs que status e Resonance vão consumir.
Objetivo: confirmar as premissas deste plano com arquivo:linha.
Contexto: o plano assume `CrowdControlTracker` e bucket de Vulnerability (teto +60%) no `DamageResolver` (M03); `StatDefinition`s de Status Potency, Status Resistance e Duration of Effects (M03, valores de 01); `DamageInstance`, `HitFlags.IsEcho/IsResonance`, `EntityEvents` com `OnHitDealt`/`OnStatusApplied`/`OnStatusExpired` (M05/M10); serviço Generation e Proc Coefficient (`Core/Procs`, M10); `ApplyStatus` stub (M08/M10) e quais modificadores KAE/GEN dependem de status (Log do M10); renderer world-space em lote (M06).
Arquivos: lê `Assets/_Project/Code/**`, `docs/roadmap/M10-modificadores.md` (Log), `DataSource/stats.csv`; escreve nada.
Restrições: somente leitura.
Aceite: premissas marcadas confirmada/divergente/ausente; lista de stats faltantes.
Verificação: n/a.
Entrega: até 300 palavras.

### T2 — Contratos
Tarefa T2: Definir os contratos do Core de status e Resonance.
Objetivo: tipos que T3–T8 implementam sem decisão de design pendente.
Contexto: `StatusEffectDefinition` e `ResonanceRule` de 11 §3.4.2; políticas `Refresh`, `StackDuration`, `StackIntensity(max)`, `Independent(max)`, `Strongest` (11 §3.8.1); aplicação B/C/G e tipos Status/Proc/Consumo/Zona/Auto (07 §4 legenda); ordem de resolução de evento (07 §6.1); `BossConversion` (R10).
Arquivos: escreve `Assets/_Project/Code/Core/Status/Contracts/*.cs`, `Assets/_Project/Code/Core/Resonance/Contracts/*.cs`.
Restrições: Core puro (R52); relógio injetado.
Aceite: `dotnet build tools/CoreTests` sem erros; comentários XML citando a seção de 07 de cada conceito.
Verificação: `dotnet build tools/CoreTests` → esperado: sucesso.
Entrega: até 200 palavras.

### T3 — StatusEngine
Tarefa T3: Implementar a lógica pura de status.
Objetivo: aplicar, acumular, expirar e tickar status conforme 07 §3–§4 e 01.
Contexto: SP por tipo de golpe e regras de 07 §3.2; resistência 07 §3.3 (Elite ×1,5, Guardian ×2,0, Chefe ×2,5, afinidade temática ×1,5 adicional); Duration of Effects com teto +50% e sem afetar Freeze/ICD (07 §3.4); tick 0,5 s (R19); Gen 2 aplica a 50% SP; stacks nunca acima do máximo (07 §7), salvo exceção declarada por fonte (R49).
Arquivos: escreve `Assets/_Project/Code/Core/Status/Engine/*.cs`, `tools/CoreTests/Status/*.cs`.
Restrições: sem alocação por hit (estruturas pré-dimensionadas).
Aceite: testes para cada regra e para cada política de stacking.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Status"` → esperado: todos passam.
Entrega: até 200 palavras.

### T4 — ResonanceEngine
Tarefa T4: Implementar a lógica pura de Resonance.
Objetivo: decidir qual Resonance dispara em um evento e com que valor.
Contexto: 07 §5.1 (só Gen 0/1 disparam; 1 por evento por alvo; ICD por alvo; DRef com média dos Increased), §5.2 (matriz das 15), §6.2 (prioridade; Resonance em ICD é pulada), §7 (8 procs/s por jogador descartando o de menor dano estimado; Explosions 4/s; Chain ICD 0,2 s e máx. 6 saltos; zonas; clones 3; Echo Mark teto). Resultado executa com Generation + 1 e `IsResonance`.
Arquivos: escreve `Assets/_Project/Code/Core/Resonance/Engine/*.cs`, `tools/CoreTests/Resonance/*.cs`.
Restrições: Core puro; índice `(status, afinidade)` em array, não dicionário com alocação.
Aceite: testes de prioridade, ICD, teto global e DRef.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Resonance"` → esperado: todos passam.
Entrega: até 200 palavras.

### T5 — Dados
Tarefa T5: Cadastrar afinidades, status e Resonances.
Objetivo: CSVs completos e importados como SOs validados.
Contexto: 6 afinidades com cor primária/secundária, forma, ícone e SFX (07 §10.1); 24 status com aplicação, stacking, duração, tick, efeito por stack, grupo de CC, conversão contra chefe, VFX/SFX (07 §4, §8, §10.2); 15 Resonances com par, gatilho, efeito, valor (More declarado), ICD, limites e prioridade (07 §5.2, §6.2). Validação: IDs únicos, afinidade existente, ICD > 0 em toda Resonance, More declarado, tick múltiplo de 0,25 s, grupo de CC válido.
Arquivos: escreve os arquivos da linha T5 (se faltar stat de 01 apontada no T1, também `DataSource/stats.csv`).
Restrições: um importador/regra por arquivo; números só de 07/01.
Aceite: 6 + 24 + 15 linhas; import e validação sem erros.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data-m11.log` → esperado: exit 0, 0 erros.
Entrega: até 200 palavras.

### T6 — StatusSystem em lote
Tarefa T6: Executar status em runtime.
Objetivo: status vivos em inimigos e jogador, processados em lote e integrados ao dano e ao CC.
Contexto: 11 §3.8.1 (SoA `NativeArray`, job a 0,25 s, tick lógico 0,5 s); `CrowdControlTracker` (06 §6, R09) com Freeze no grupo Hard, limites do jogador e exceção de Ultimate; `BossConversion` com Frostbound/Distortion (R10); fontes de Vulnerability no bucket com teto +60% (R12); `AffinityInjector` de debug adiciona dano/SP de qualquer afinidade a uma habilidade para testes.
Arquivos: lê T3/T5; escreve os arquivos da linha T6.
Restrições: 0 B de GC por frame em combate; Gameplay não referencia UI.
Aceite: PlayMode: Burning tica a cada 0,5 s; Freeze repetido segue 100/50/25%/imune; dummy chefe converte Freeze em Frostbound.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Status.System" -testResults Logs/playmode-m11-t6.xml -logFile Logs/playmode-m11-t6.log` → esperado: `failed="0"`.
Entrega: até 200 palavras.

### T7a / T7b / T7c — Efeitos por afinidade
Tarefa T7x: Implementar os 8 efeitos das duas afinidades atribuídas (a: Ember+Frost; b: Storm+Void; c: Echo+Ruin).
Objetivo: cada status/proc/consumo/zona/auto de 07 §4.1–4.6 funciona com os números de `DataSource/status_effects.csv`.
Contexto: 07 §4 da afinidade, §6.3 (convivência: Freeze × Burning, Freeze × Stun, Pull × Frozen, Ignition × Combustion, Echo Mark × consumo, Chill × Slow ≤ −60%), §7 (tetos), §4.8 (exceções). T7c: Repeat/Clone usam a recomendação do spike `docs/roadmap/spikes/M10-echo-clone.md`.
Arquivos: escreve somente as pastas da sua linha na tabela.
Restrições: não editar `Gameplay/Status/System` (pedir ao principal se faltar ponto de extensão); números só do CSV.
Aceite: 1 teste PlayMode por efeito.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Status.<Afinidade>" -testResults Logs/playmode-m11-t7x.xml -logFile Logs/playmode-m11-t7x.log` → esperado: `failed="0"`.
Entrega: até 200 palavras.

### T8 — ResonanceSystem
Tarefa T8: Executar as 15 Resonances em runtime.
Objetivo: cada Resonance de 07 §5.2 dispara nas condições descritas, com prioridade, ICD e tetos.
Contexto: 11 §3.8.2 (escuta `OnHitDealt`/`OnStatusApplied`, afinidade = `TagMask ∪ ResonanceAffinityMask`); `ResonanceEngine` do T4; efeitos em `Effects/Resonance` usando os efeitos de T7a–c; Resonances nativas dos heróis (07 §5.3) — testar Cinder Rot com o kit real do Kael.
Arquivos: escreve os arquivos da linha T8.
Restrições: não editar efeitos de T7a–c (reutilizar).
Aceite: 15 testes PlayMode (um por Resonance) + 1 de prioridade quando o evento qualifica 2+ Resonances.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Resonance" -testResults Logs/playmode-m11-t8.xml -logFile Logs/playmode-m11-t8.log` → esperado: `failed="0"`.
Entrega: até 200 palavras.

### T9 — Legibilidade
Tarefa T9: Representação visual de status.
Objetivo: inimigos mostram até 4 ícones de status; cada afinidade tem VFX placeholder reconhecível.
Contexto: 07 §10.1 (paleta/forma por afinidade), §10.2 (representação por status), §10.3 (regras; telegraphs nunca reduzidos); 11 §3.14 (VFX do jogador a 80% de opacidade, abaixo da camada de telegraphs; vermelho saturado no chão reservado a telegraphs). Assets gratuitos/greybox (CLAUDE.md §3).
Arquivos: escreve os arquivos da linha T9.
Restrições: UI só lê eventos (ring buffer de 11 §3.7); nenhum asset pago.
Aceite: cena de teste com os 24 status visíveis; prioridade dos 4 ícones documentada no código.
Verificação: PlayMode smoke `Eclipse.Tests.PlayMode.UI.WorldStatus` → esperado: sem falhas; captura de tela anexada ao relatório.
Entrega: até 150 palavras.

### T10 — Stress com Resonance
Tarefa T10: Estender o teste de recursão do M10.
Objetivo: garantir os tetos com Resonances ativas.
Contexto: 07 §7.1 (teste de sanidade: 40 inimigos, todas as fontes Echo + Storm + Ember maximizadas → ≤ 400 eventos de dano/s, nenhuma cadeia > Gen 2) e 11 §3.16; usar `AffinityInjector` para ativar afinidades que o Kael não tem.
Arquivos: escreve `Assets/_Project/Code/Tests/PlayMode/Stress/RecursionStressTest.cs` (estender), `tools/CoreTests/Resonance/ResonanceStressTests.cs`.
Restrições: seed fixa.
Aceite: ≤ 400 eventos/s, Gen máx. 2, ≤ 8 Resonances/s por jogador.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Stress" -testResults Logs/playmode-m11-t10.xml -logFile Logs/playmode-m11-t10.log` → esperado: `failed="0"` e números impressos no log.
Entrega: até 150 palavras com números.

### T11 — Integração
Tarefa T11: Fechar o módulo.
Objetivo: critérios de saída verificados; pendências de status do M10 resolvidas (modificadores que dependiam de status agora testados com o status real).
Arquivos: `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo.
Verificação: seção abaixo.
Entrega: resumo para o usuário.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
Esperado: exit code 0, `failed="0"`, 0 erros de validação.

## Riscos e rollback
- **Custo de CPU com 200 inimigos × vários status** → SoA + Burst desde o T6; medir no soak test de M07 com status ativos; se acima de 5 ms de gameplay (11 §3.14), reduzir frequência de agendamento de status fora da tela.
- **Interações de convivência (07 §6.3) geram loops** → cobertas pelos testes do T7 e pelo stress do T10.
- **Clone/Repeat dependem do spike do M10** → se o spike foi no-go, T7c implementa Repeat e deixa Clone com a abordagem alternativa registrada.
- Rollback: branch do módulo; `StatusSystem` substitui o stub de `ApplyStatus` só no T11.

## Atualizações de documentação
- `CLAUDE.md` §5: `Core/Status`, `Core/Resonance`, `Gameplay/Status/Effects/<Afinidade>`, `UI/WorldStatus`.
- `docs/ROADMAP.md` §7: status do M11.
- Se algum valor de 07 se mostrar inviável em teste, registrar em 07 §13 (questões) e avisar o usuário — não mudar o número no código.

## Prompt de disparo
```
Implemente o módulo M11 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M11-status-resonance.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M11-status-resonance.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

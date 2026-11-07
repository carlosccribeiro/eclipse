# M14 — Simuladores e Training Grounds
Fase 2 · Onda 9 · Status: ⏳ não iniciado
Depende de: M07, M11, M13 · Desbloqueia: M28, **Marco B (Build Sandbox)** · Paralelo com: M16
Branch: modulo/M14-simuladores-training
Docs de referência: 00 §4 (orçamento de poder e regra de ouro ±10%); 04 §10 (modelo da Rota A e resumo por set); 08 §4.7 (builds do Kael e rotas A/B/C); 01 §3.10 (alvo de referência e índices); 05 §5, §11 (raridade e densidade esperada); 07 §3.3 (resistência a status por categoria), §5.3 (regra de acessibilidade de Resonance); 11 §3.15 (itens 3, 4, 5, 7), §3.16; GDD §3.3 (Training Grounds)

## Objetivo
Dar ao time ferramentas para medir e testar builds sem jogar runs: um **simulador headless de DPS/TTK** que roda o pipeline real (SkillSpec, StatBlock, DamageResolver, Status, Resonance) e compara Rotas A × B × C das 3 builds do Kael contra a regra de ±10% (00 §4); um **Loot Simulator** que confere a distribuição e a densidade do gerador contra 05; o **Build Sandbox** (janela de editor) e o **Training Grounds** jogável (GDD §3.3), com dummies, medidor de DPS e comandos de debug. Fecha o **Marco B**.

## Critérios de saída (verificáveis)
1. Motor de simulação no Core (`Core/Simulation`), determinístico a 60 Hz, com rotação por lista de prioridades (APL) por build, cenários "alvo único 60 s" (dummy de chefe) e "pacote de 10 inimigos" contra o alvo de referência de 01 §3.10 (Armor 25%, Res 20%); saídas: DPS, TTK, procs/s, Resonances/s, Generation máx.
2. `BalanceSimBatch` (executeMethod) roda os 9 loadouts do Kael (3 builds × Rotas A/B/C de 08 §4.7, com itens e sets existentes) e gera `Logs/sim/balance_routes.csv` + `Logs/sim/balance_summary.md`; o exit code é ≠ 0 se, em alguma build, a diferença de dano entre rotas ofensivas passar de ±10% (00 §4; sets defensivos avaliados por EHP conforme 04 §2.11).
3. `LootSimBatch` (executeMethod) e janela de editor geram N drops por contexto (fonte × dificuldade × região) e comparam com 05 §5 (distribuição de raridade) e §11.1 (itens por run por raridade), gravando `Logs/sim/loot_*.csv` com desvio por linha.
4. Build Sandbox (janela de editor em Play Mode): equipar qualquer item/set/modificador/Augment no Kael, gerar itens por contexto, spawnar inimigos do M07, aplicar status/afinidade, e Skill Graph Inspector mostrando o `SkillSpec` antes/depois (11 §3.15 itens 3–4).
5. Cena **Training Grounds** jogável: dummies configuráveis (categorias Normal/Elite/Guardian/Chefe com multiplicadores de resistência de 07 §3.3, Armor/Res ajustáveis), medidor de DPS (janela, total, por habilidade), reset, spawn de inimigos; comandos de console `give item`, `give set`, `set level`, `spawn`, `god`, `clear` (11 §3.15 item 7).
6. Relatório de acessibilidade de Resonance para o Kael (07 §5.3): para cada uma das 15 Resonances, se existe build com ≤ 2 itens dedicados que dispare ≥ 1 vez a cada 10 s contra grupo; lacunas listadas (não reprova o módulo: o catálogo completo é o M28).
7. Workflow de CI `balance-sim.yml` roda import/validação, BalanceSim e LootSim quando `DataSource/**` muda e publica os relatórios como artefato.
8. `dotnet test`, EditMode, PlayMode verdes; validador sem erros.

## Fora de escopo
- Builds e loadouts de Lyra, Nyx e Orun (M21–M23 adicionam seus loadouts ao simulador).
- Run Map Previewer (M15) e simulação de runs completas.
- Balanceamento em si (mudar números): o módulo mede e reporta; mudança de valor vai ao doc dono.
- Training Grounds dentro do Sanctuary (M19 posiciona a cena no hub; aqui ela é acessível pelo menu de debug).

## Premissas e decisões
- **Premissa:** M10/M11/M12/M13 deixaram no Core: compilador de `SkillSpec`, `StatusEngine`, `ResonanceEngine`, `ItemGenerator`, `HeroLoadout`, `SetTracker`, `Core/Comparison`; M07 entregou spawner e inimigos-base; M06 entregou o console de debug com registro de comandos por arquivo. O T1 confirma.
- **Decisão:** o motor de simulação é Core puro (testado por `dotnet test`); os batches de editor carregam SOs reais e montam loadouts a partir de JSON em `DataSource/sim/`.
- **Decisão:** a regra de ±10% compara apenas rotas da mesma build no cenário "alvo único 60 s" e no "pacote"; a média dos dois é o número de referência (04 §10).
- **Paralelo com M16:** este módulo não escreve em `Code/Run`, `Content/Regions`, cenas de run, nem em `ProjectSettings/EditorBuildSettings.asset`; a cena Training Grounds é carregada pelo menu de debug via Addressables (label `scenes`). Comandos de console ficam em arquivo próprio.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Mapear APIs de M06/M07/M10–M13 para simulação e sandbox | investigação | investigador | — | — | ⏳ |
| T2 | Modelo de simulação, cenários e esquema dos loadouts JSON | design | principal | T1 | `docs/data-schemas/simulation.md`, `DataSource/sim/scenarios.json` | ⏳ |
| T3 | Motor de simulação (Core) | execução | executor | T2 | `Assets/_Project/Code/Core/Simulation/*.cs`, `tools/CoreTests/Simulation/*.cs` | ⏳ |
| T4 | `BalanceSimBatch` + 9 loadouts do Kael + relatório ±10% | execução | executor | T3 | `Assets/_Project/Code/Editor/Simulation/Balance/*.cs`, `DataSource/sim/loadouts/kael_*.json` | ⏳ |
| T5 | Loot Simulator (janela + batch) | execução | executor | T1 | `Assets/_Project/Code/Editor/Simulation/Loot/*.cs` | ⏳ |
| T6 | Build Sandbox + Skill Graph Inspector | execução | executor | T1 | `Assets/_Project/Code/Editor/Sandbox/*.cs` | ⏳ |
| T7 | Training Grounds (cena, dummies, medidor de DPS, comandos de console) | execução | executor | T1 | `Assets/_Project/Scenes/TrainingGrounds/TrainingGrounds.unity`, `Assets/_Project/Code/Gameplay/TrainingGrounds/*.cs`, `Assets/_Project/Code/UI/TrainingGrounds/*.cs`, `Assets/_Project/UI/TrainingGrounds/*.{uxml,uss}`, `Assets/_Project/Code/Gameplay/Debug/Commands/SandboxCommands.cs` | ⏳ |
| T8 | Relatório de acessibilidade de Resonance (Kael) | execução | executor | T4 | `Assets/_Project/Code/Editor/Simulation/Resonance/*.cs`, `DataSource/sim/resonance/kael_*.json` | ⏳ |
| T9 | Workflow de CI `balance-sim.yml` | execução | executor | T4, T5 | `.github/workflows/balance-sim.yml` | ⏳ |
| T10 | Integração, checklist do Marco B e documentação | revisão | principal | T6, T7, T8, T9 | `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo | ⏳ |

## Ondas internas
1. T1
2. T2 ∥ T5 ∥ T6 ∥ T7 (T5–T7 só dependem do mapeamento)
3. T3
4. T4
5. T8 ∥ T9
6. T10

## Briefs

### T1 — Mapear APIs
Tarefa T1: Confirmar as APIs para simulação e sandbox.
Objetivo: lista de pontos de entrada reais (arquivo:linha).
Contexto: o simulador precisa compilar `SkillSpec` a partir de um `HeroLoadout` em memória, avaliar `DamageResolver`, `StatusEngine` e `ResonanceEngine` sem cena, com relógio injetado; o sandbox precisa de spawner (M07), `ItemGenerator` (M12), `EquipmentComponent` (M13), `ModifierSandbox`/Augment de debug (M10), `AffinityInjector` (M11), console de debug (M06). Verificar se o `SkillController`/APL tem algo reaproveitável para rotação.
Arquivos: lê `Assets/_Project/Code/**`; escreve nada.
Restrições: somente leitura.
Aceite: premissas confirmadas/divergentes/ausentes; o que falta no Core para simular sem cena.
Verificação: n/a.
Entrega: até 300 palavras.

### T2 — Modelo de simulação
Tarefa T2: Definir o modelo de simulação e os formatos.
Objetivo: documento e cenários que T3/T4/T8 implementam.
Contexto: 60 Hz (CLAUDE.md §4); rotação por APL (lista ordenada de condições → ação, respeitando custo, cooldown, recurso e animação de frame data de 06); alvo de referência de 01 §3.10; cenários: `single_boss_60s` (dummy categoria Chefe, sem Execute direto: conversão R08) e `pack_10` (10 inimigos Standard de 09 em raio de 6 m, respawn ao morrer por 60 s); regra de ±10% (00 §4) e EHP para sets defensivos (04 §2.11: 1% de dano ≈ 1,5% EHP); esquema JSON do loadout (herói, nível, itens por `StableId` + Roll01 fixo ou "médio", Augments, Keystones, APL).
Arquivos: escreve `docs/data-schemas/simulation.md`, `DataSource/sim/scenarios.json`.
Restrições: nenhum número de balanceamento novo além dos cenários (que citam a fonte).
Aceite: documento cobre entrada, saída, cenários e critério de aprovação.
Verificação: n/a (revisão do principal).
Entrega: até 200 palavras.

### T3 — Motor de simulação
Tarefa T3: Implementar o motor de simulação no Core.
Objetivo: dado loadout compilado + cenário + seed, produzir métricas determinísticas.
Contexto: `docs/data-schemas/simulation.md`; reutilizar `DamageResolver`, `StatusEngine`, `ResonanceEngine`, `Core/Procs` (Generation, ICD) sem duplicar regra; métricas: DPS (janela total e por habilidade), TTK, procs/s, Resonances/s, eventos de dano/s, Generation máx.
Arquivos: escreve `Assets/_Project/Code/Core/Simulation/*.cs`, `tools/CoreTests/Simulation/*.cs`.
Restrições: Core puro; mesma seed → mesmo resultado; 60 s simulados em < 2 s.
Aceite: teste "golden" com um loadout fixture (resultado gravado); teste de determinismo.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Simulation"` → esperado: todos passam.
Entrega: até 150 palavras.

### T4 — BalanceSimBatch
Tarefa T4: Rodar as rotas do Kael e checar a regra de ouro.
Objetivo: relatório das 9 combinações (Inferno, Ruin, Juggernaut × A, B, C de 08 §4.7).
Contexto: rotas de 08 §4.7 (A: Legendaries individuais citados; B: set completo; C: combinações 3+3 listadas); itens de `DataSource/items_named.csv` e sets de `DataSource/set_pieces.csv`; Roll01 "médio" (0,5) e "bom" (0,85) como duas colunas; itens +6 (01 §3.9.5, referência de orçamento); entrada `-executeMethod Eclipse.Editor.Simulation.Balance.BalanceSimBatch.Run`.
Arquivos: escreve `Assets/_Project/Code/Editor/Simulation/Balance/*.cs`, `DataSource/sim/loadouts/kael_*.json`.
Restrições: não alterar dados de balanceamento; violação gera exit code 1 e linha "VIOLATION" no resumo.
Aceite: CSV e resumo gerados; resultado por build com variação entre rotas.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.Balance.BalanceSimBatch.Run -quit -logFile Logs/sim-balance.log` → esperado: `Logs/sim/balance_routes.csv` e `Logs/sim/balance_summary.md` gerados; exit 0 (ou 1 com violações listadas — reportar ao principal, não "consertar" números).
Entrega: até 200 palavras com a tabela de resultados.

### T5 — Loot Simulator
Tarefa T5: Simular drops e comparar com 05.
Objetivo: janela de editor + batch que mostram distribuição e densidade.
Contexto: 05 §5 (chances por fonte × dificuldade), §5.4 (distribuição do trash), §11.1 (itens por run por raridade; composição de run de 09: 13 linhas + chefe); usar `ItemGenerator` real com dados reais; saída por contexto: % por raridade, % por tipo de Legendary, % hero-bound, média de qualidade; entrada `-executeMethod Eclipse.Editor.Simulation.Loot.LootSimBatch.Run`.
Arquivos: escreve `Assets/_Project/Code/Editor/Simulation/Loot/*.cs`.
Restrições: não editar código do gerador (bugs encontrados vão para o Log com evidência).
Aceite: CSVs `Logs/sim/loot_rarity.csv` e `Logs/sim/loot_density.csv` com coluna de desvio vs. 05.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.Loot.LootSimBatch.Run -quit -logFile Logs/sim-loot.log` → esperado: exit 0 e CSVs gerados.
Entrega: até 150 palavras com os maiores desvios.

### T6 — Build Sandbox
Tarefa T6: Janela de editor para montar builds em Play Mode.
Objetivo: 11 §3.15 itens 3 e 4.
Contexto: equipar qualquer item (gerado por contexto ou nomeado), peça de set, modificador avulso, Augment; spawnar inimigos do M07; aplicar status/afinidade (`AffinityInjector`); Skill Graph Inspector com `SkillSpec` antes/depois por fase.
Arquivos: escreve `Assets/_Project/Code/Editor/Sandbox/*.cs`.
Restrições: Editor-only; usa APIs públicas, sem acessar internals.
Aceite: EditMode/PlayMode smoke abrindo a janela e aplicando um item; captura de tela.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Sandbox" -testResults Logs/playmode-m14-t6.xml -logFile Logs/playmode-m14-t6.log` → esperado: `failed="0"`.
Entrega: até 120 palavras.

### T7 — Training Grounds
Tarefa T7: Cena jogável de testes.
Objetivo: GDD §3.3 "testes de dano, habilidades e builds" para o jogador.
Contexto: dummies com categoria (Normal/Elite/Guardian/Chefe — multiplicadores de 07 §3.3), Armor/Res ajustáveis, opção de imunidade a morte; medidor de DPS (janela deslizante, total, por habilidade); reset; menu de spawn; comandos de console `give item <id|contexto>`, `give set <id>`, `set level <n>`, `spawn <enemyId> <n>`, `god`, `clear`.
Arquivos: escreve os arquivos da linha T7.
Restrições: não editar `ProjectSettings/EditorBuildSettings.asset`; cena via Addressables; assets greybox.
Aceite: PlayMode smoke: carregar cena, spawnar dummy, causar dano, medidor mostra DPS > 0.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.TrainingGrounds" -testResults Logs/playmode-m14-t7.xml -logFile Logs/playmode-m14-t7.log` → esperado: `failed="0"`.
Entrega: até 120 palavras + captura de tela.

### T8 — Acessibilidade de Resonance
Tarefa T8: Medir quais Resonances o Kael alcança com ≤ 2 itens dedicados.
Objetivo: relatório da regra de 07 §5.3 para o Kael.
Contexto: para cada Resonance, montar loadouts candidatos com itens/modificadores/Infusions existentes (no máximo 2 itens "dedicados" à afinidade que falta) e simular `pack_10` por 60 s; critério ≥ 1 disparo a cada 10 s.
Arquivos: escreve `Assets/_Project/Code/Editor/Simulation/Resonance/*.cs`, `DataSource/sim/resonance/kael_*.json`.
Restrições: lacuna não é falha do módulo; reportar o item/modificador que faltaria.
Aceite: `Logs/sim/resonance_access_kael.md` com 15 linhas (ok/lacuna + motivo).
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.Resonance.ResonanceAccessBatch.Run -quit -logFile Logs/sim-resonance.log` → esperado: exit 0 e relatório gerado.
Entrega: até 150 palavras.

### T9 — CI
Tarefa T9: Rodar simuladores no CI.
Objetivo: a cada mudança em `DataSource/**`, CI importa/valida, roda BalanceSim e LootSim e publica `Logs/sim/**`.
Contexto: GameCI já configurado no M00 (licença como secret); 11 §3.16 ("executado no CI a cada mudança em `DataSource/`").
Arquivos: escreve `.github/workflows/balance-sim.yml`.
Restrições: não editar workflows existentes do M00; violação de ±10% marca o job como falho, mas não bloqueia merge até o M28 (anotação `continue-on-error` documentada).
Aceite: workflow válido (sintaxe) e com gatilho por caminho.
Verificação: `gh workflow view balance-sim.yml` após push (quando o usuário autorizar) ou validação local com `actionlint` se disponível → esperado: sem erros de sintaxe.
Entrega: até 100 palavras.

### T10 — Integração e Marco B
Tarefa T10: Fechar o módulo e o Marco B.
Objetivo: checklist do Marco B (ROADMAP §3): itens gerados, equipados e com modificadores mudando habilidades; 2+ sets testáveis; relatório A/B/C gerado.
Arquivos: `CLAUDE.md` (comandos `BalanceSimBatch`, `LootSimBatch`, `ResonanceAccessBatch` em §6; pasta `DataSource/sim/`), `docs/ROADMAP.md` (status M14 e Marco B), este arquivo.
Verificação: seção abaixo + os três batches.
Entrega: resumo para o usuário com a tabela de rotas e os maiores desvios de loot.

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
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.Balance.BalanceSimBatch.Run -quit -logFile Logs/sim-balance.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.Loot.LootSimBatch.Run -quit -logFile Logs/sim-loot.log
```
Esperado: testes e validação com exit 0 e `failed="0"`; batches geram os relatórios em `Logs/sim/`; violações de ±10% são reportadas ao usuário (não corrigidas no código).

## Riscos e rollback
- **Simulador diverge do jogo** → o motor reutiliza as mesmas classes Core do runtime; teste de paridade: mesma rotação no Training Grounds e no simulador com diferença ≤ 3%.
- **Rotação APL ingênua favorece uma rota** → APL por build revisada pelo principal; resultados com Roll01 médio e bom.
- **Violação de ±10% encontrada** → é um achado de design: reportar ao usuário e registrar em 04 §14/00 §5; não ajustar números neste módulo.
- Rollback: branch do módulo; nada no jogo depende das ferramentas.

## Atualizações de documentação
- `CLAUDE.md` §6: comandos dos batches de simulação; §5: `Core/Simulation`, `Editor/{Simulation,Sandbox}`, `DataSource/sim/`.
- `docs/data-schemas/simulation.md` (novo).
- `docs/ROADMAP.md` §7: status do M14 e Marco B.

## Prompt de disparo
```
Implemente o módulo M14 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M14-simuladores-training.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M14-simuladores-training.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

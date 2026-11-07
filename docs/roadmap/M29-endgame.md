# M29 — Endgame (Ascension, Rift Sigils, modos, pinnacle)
Fase 6 · Onda 16 · Status: ⏳ não iniciado
Depende de: M27, M28 · Desbloqueia: M30, M31 · Paralelo com: — (onda solo)
Branch: modulo/M29-endgame
Docs de referência: 10-progressao §7.1–7.3, §8.1–8.7, §9; 09-inimigos §6.2, §15.1–15.6; 05-loot §3 (teto de quantidade), §4 (iLvl), Primal; 06-combate §3.10 (telegraph/Annihilation), R16; 11 §3.11, §3.13, §3.16; 00 §5 R16, R28, R31, R38, R39, R43, R44, R45

> **Refinar ao iniciar:** os nomes de classes, pastas e serviços abaixo seguem 11-arquitetura; confirme com o que M15–M20, M24–M28 realmente criaram (RunManager, EncounterDirector, Rift Navigator, Archivist, Challenge Altar stub, BossDefinition) antes de aprovar o plano.

## Objetivo
Fechar o **Marco G (Beta)**: tornar jogável todo o endgame definido em 10 §7–8 e 09 §15 sobre o conteúdo completo das 5 regiões — dificuldades Nightmare/Eclipse com regras próprias, Ascension 1–100 com Thresholds, Rift Sigils (Distortions, Boons, Peril, Attunement), Boss Echoes I–III, Challenge Altar (Daily, Weekly, Build Trials, Eclipse Trial, Endless Sundering, Eclipse Cycles), o pinnacle The Sundering Heart e o loot de endgame (Primal, Pinnacle Uniques, Set Caches).

## Critérios de saída (verificáveis)
1. Ascension N desbloqueia N+1 (A1–A20: +2) por conta; maior Ascension por herói salvo e recarregado (teste EditMode de save round-trip).
2. eLvl/escala por tier seguem 10 §7.2 (A100 = eLvl 110, iLvl de drop 100 a partir de A90) — teste EditMode parametrizado.
3. Os 11 Thresholds de 10 §7.3 são aplicados por dados (`AscensionThresholdDefinition`), sem `if` por número de Ascension no código (teste: tabela → regra ativa).
4. Rift Sigil: gera 1–4 Distortions respeitando o Threshold, 0–2 Boons, Peril somado; recompensa por Peril respeita o teto ×2,0 de quantidade (05 §3). Testes EditMode + run automatizada com Sigil de 4 Distortions.
5. Todas as 15 Distortions e 5 Boons do catálogo de 10 §8.2 existem como dados, passam no validador e reaproveitam definições de 09 (afixo/Room Modifier/Curse) por referência de `StableId`.
6. Attunement (Rift Sigil + 2 Pillar Fragments) aplica peso ×3 no set/Unique escolhido, só Eclipse+ (Loot Simulator: 10.000 drops mostram ×3 ± 5%).
7. Boss Echoes I/II/III dos 5 chefes jogáveis, com chave/custos de 10 §8.3 e chance de Unique por tier contando no pity do chefe (simulação: pity garante no 10º abate).
8. Challenge Altar com os 6 modos de 10 §8.4; Daily/Weekly com seed determinística (mesma data → mesmo mapa, teste EditMode).
9. Endless Sundering: troca de região a cada 10 salas, +1 Ascension equivalente a cada 5, checkpoint com Rift Anchor a cada 10 (PlayMode: bot atravessa 30 salas).
10. Nightmare Behaviors (8), Champion Packs, Eclipse Phenomena (5), fase extra de chefe em Eclipse, Eclipse Trial, Overrift Herald implementados conforme 09 §15.
11. The Sundering Heart com 3 fases (09 §15.5), requisito A30 + 5 Pillar Seals consumidas, drop dos 4 Pinnacle Uniques; duração alvo 6 min medida por bot de referência/playtest.
12. Primal dropa só em A50+, rolagens 85–100%, nunca acima do máximo (teste de gerador com 100.000 amostras).
13. Validador sem erros; `dotnet test`, EditMode e PlayMode verdes; nenhum telegraph de endgame com wind-up < 0,45 s.

## Fora de escopo
- Leaderboards online além do opcional Steam (fica no M32).
- Novos chefes, regiões ou itens além do catálogo do M28 (só os 4 Pinnacle Uniques se ainda não existirem como dados).
- Balanceamento fino de números (questões de playtest de 10 §13 / 09 §18).
- Otimização de performance (M30).

## Premissas e decisões
- Endgame é **dados + regras genéricas**: Distortions, Boons, Thresholds e modos são SOs que compõem modificadores de run já existentes (Room Modifiers, afixos, Curses, multiplicadores de escala). Código novo só para mecânicas sem equivalente (ex.: Endless, Eclipse Cycles).
- Seeds de Daily/Weekly: derivadas da data UTC + id do modo, no stream `Map` (11 RNG).
- Eclipse Cycles usam calendário local (10 §8.4) e rotação de 8 semanas definida em dados.
- Leaderboard "Beyond" (A100) e Weekly ficam como recorde **local** neste módulo; integração Steam opcional no M32.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Inventariar o que M15–M28 entregaram para endgame e mapear lacunas | investigação | investigador | — | `docs/roadmap/M29-endgame.md` (Log) | ⏳ |
| T2 | Dificuldades Nightmare/Eclipse/Ascension: escala eLvl, tiers, desbloqueios, Thresholds por dados | execução | executor | T1 | `Code/Core/Progression/Difficulty*`, `Code/Data/Endgame/AscensionThreshold*`, `DataSource/ascension_thresholds.csv`, testes | ⏳ |
| T3 | Rift Sigils: Distortions, Boons, Peril, recompensa e Attunement | execução | executor | T2 | `Code/Core/Endgame/Sigils/*`, `Code/Data/Endgame/Distortion*`, `DataSource/distortions.csv`, `DataSource/boons.csv`, testes | ⏳ |
| T4 | Conteúdo de inimigos de endgame: Nightmare Behaviors, Champion Packs, Eclipse Phenomena, fase extra, Overrift Herald | execução | executor | T2 | `Code/AI/Endgame/*`, `Content/Enemies/*/Nightmare*`, `Content/Regions/*/Phenomena*`, `DataSource/nightmare_behaviors.csv` | ⏳ |
| T5 | Boss Echoes I/II/III dos 5 chefes + Pillar Seals + Heart keys | execução | executor | T2, T4 | `Code/AI/Bosses/Echoes/*`, `Content/Bosses/*/Echo*`, `DataSource/boss_echoes.csv`, testes | ⏳ |
| T6 | Challenge Altar: Daily, Weekly, Build Trials, Eclipse Trial | execução | executor | T3, T5 | `Code/Meta/ChallengeAltar/*`, `DataSource/challenges.csv`, `UI/ChallengeAltar/*`, testes | ⏳ |
| T7 | Endless Sundering e Eclipse Cycles | execução | executor | T3 | `Code/Run/Endless/*`, `Code/Meta/Cycles/*`, `DataSource/eclipse_cycles.csv`, testes | ⏳ |
| T8 | The Sundering Heart (3 fases) + Pinnacle Uniques | execução | executor | T5 | `Code/AI/Bosses/SunderingHeart/*`, `Content/Bosses/SunderingHeart/*`, `DataSource/items_unique.csv` (só linhas Pinnacle, se faltarem) | ⏳ |
| T9 | Loot de endgame: Primal, Set Caches, recompensas por Peril no gerador | execução | executor | T3 | `Code/Core/Loot/Primal*`, `Code/Meta/Economy/SetCache*`, testes | ⏳ |
| T10 | Simulações e bots de endgame (Loot Simulator, Endless bot, run com Sigil máximo) | execução | executor | T6, T7, T8, T9 | `Code/Editor/Simulators/Endgame*`, `Code/Tests/PlayMode/Endgame*` | ⏳ |
| T11 | Revisão integrada contra 10 §7–8 e 09 §15; atualizar docs | revisão | principal | T10 | `docs/ROADMAP.md`, este arquivo, `CLAUDE.md` (se aplicável) | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T4
4. T5 ∥ T7 ∥ T9
5. T6 ∥ T8
6. T10
7. T11

## Briefs

**T1 — Inventariar entregas para o endgame**
- Objetivo: lista do que já existe e do que falta para cada critério de saída deste módulo.
- Contexto: endgame de 10 §7–8 e 09 §15 roda sobre sistemas de M15 (geração), M16 (fluxo/Room Modifiers/Curses), M17/M24–M27 (chefes), M18 (save), M19 (Rift Navigator, Archivist, Challenge Altar stub, economia), M20 (dificuldades), M28 (catálogo).
- Arquivos: lê `Assets/_Project/Code/**`, `DataSource/**`, `docs/roadmap/M15..M28*.md` (Log); escreve só a seção Log deste arquivo.
- Restrições: não alterar código.
- Aceite: tabela critério × {existe, parcial, falta} com caminhos.
- Verificação: tabela presente no Log com 13 linhas (uma por critério).
- Entrega: ≤ 300 palavras; lacunas que mudam o plano em destaque.

**T2 — Dificuldades e Ascension**
- Objetivo: tiers Normal→Eclipse com regras de 10 §7.1, Ascension 1–100 com desbloqueio e Thresholds por dados, eLvl conforme 10 §7.2.
- Contexto: eLvl = `iLvl_sala` de 05 §4.2; acima de A90 `eLvl = 100 + (N − 90)`; escala relativa `Vida 25×1,035^N`, `Dano 4×1,02^N`; Thresholds em 10 §7.3 (A1, A10, A20, A30, A40, A50, A60, A75, A80, A90, A100). Afixos de Elite por dificuldade são de 09 §6.2 (cite, não duplique).
- Arquivos: escreve `Assets/_Project/Code/Core/Progression/Difficulty*.cs`, `Assets/_Project/Code/Data/Endgame/AscensionThresholdDefinition.cs`, `DataSource/ascension_thresholds.csv`, testes em `Code/Tests/EditMode/Endgame/` e `tools/CoreTests`.
- Restrições: lógica de escala em `Eclipse.Core` (C# puro); nada de número mágico fora do CSV; desbloqueio por conta, recorde por herói persistido no save (M18).
- Aceite: critérios 1–3.
- Verificação: `dotnet test tools/CoreTests` e EditMode (CLAUDE.md §6) verdes; importador/validador sem erros.
- Entrega: ≤ 200 palavras + lista de arquivos.

**T3 — Rift Sigils, Peril e Attunement**
- Objetivo: Sigil com 1–4 Distortions (limitadas pelo Threshold), 0–2 Boons, região opcional; Peril somado; recompensa por Peril; Attunement.
- Contexto: 10 §8.2 (catálogo de 15 Distortions, 5 Boons; Peril +8% quantidade, +3% chance de Legendary, 5 Marks; teto ×2,0 de 05 §3). Distortion que reaproveita conteúdo de 09 referencia o `StableId` do afixo/Room Modifier/Curse. Attunement = Rift Sigil + 2 Pillar Fragments → peso ×3 (só Eclipse+, R39). Sigils dropam de Elites/chefes em Eclipse+.
- Arquivos: escreve `Code/Core/Endgame/Sigils/*`, `Code/Data/Endgame/DistortionDefinition.cs`, `BoonDefinition.cs`, `DataSource/distortions.csv`, `DataSource/boons.csv`, aplicação no Rift Navigator (`Code/Meta/Sanctuary/RiftNavigator*`), testes.
- Restrições: geração no stream `Loot`/`Map` com seed; Distortions como composição de regras existentes; validador deve recusar Distortion acima do Threshold.
- Aceite: critérios 4–6.
- Verificação: testes EditMode + Loot Simulator (11 §3.15) com relatório de ×3 ± 5%.
- Entrega: ≤ 200 palavras + relatório do simulador.

**T4 — Inimigos de endgame**
- Objetivo: Nightmare Behaviors (8 arquétipos, 35% por inimigo, 100% em Elites), Champion Packs (30% em Elite Arenas Nightmare+), 5 Eclipse Phenomena, fase extra de chefe em Eclipse (< 10% de vida, 20–40 s), Overrift Herald.
- Contexto: 09 §15.1, §15.2, §15.6; telegraphs e wind-ups seguem 06 (mínimo 0,45 s, Annihilation com anel e brecha).
- Arquivos: escreve `Code/AI/Endgame/*`, `Content/Enemies/*/Nightmare*`, `Content/Regions/*/Phenomena*`, `DataSource/nightmare_behaviors.csv`, `DataSource/eclipse_phenomena.csv`.
- Restrições: behaviors como estados/ataques adicionais no HFSM existente (M07), sem fork do inimigo.
- Aceite: critério 10 (exceto Eclipse Trial, que é T6).
- Verificação: PlayMode por arquétipo em sala de teste verifica ativação nas chances (n = 500 spawns, ±5 pp); validador sem erros.
- Entrega: ≤ 200 palavras.

**T5 — Boss Echoes**
- Objetivo: tiers I/II/III dos 5 chefes conforme 09 §15.3, chaves/custos e recompensas de 10 §8.3.
- Contexto: I = padrões das fases 1–2 misturados + arena alterada; II = +1 mecânica por chefe; III = todas as variações ✦/✧, sem adds. Chave: 1/2/3 Hearts + 300/600/1.200 Marks. Chance de Unique 35/50/70%, mesmo pity do chefe (10º abate). 1 Pillar Seal por Echo de cada chefe.
- Arquivos: escreve `Code/AI/Bosses/Echoes/*`, `Content/Bosses/*/Echo*`, `DataSource/boss_echoes.csv`, testes.
- Restrições: reaproveitar BossDefinition/sequenciador de M17; Echo = override de padrões e arena, não chefe novo.
- Aceite: critério 7; duração alvo ≤ 5 min.
- Verificação: EditMode de pity; PlayMode de entrada/saída de cada tier com bot invulnerável medindo duração com DPS de referência.
- Entrega: ≤ 200 palavras + tabela de durações medidas.

**T6 — Challenge Altar**
- Objetivo: Daily Rift, Weekly Trial, 36 Build Trials, Eclipse Trial, com desbloqueios e recompensas de 10 §8.4 e 09 §15.4.
- Contexto: Daily = seed diária + 3 Distortions fixas; Weekly = seed semanal + regra especial; Build Trials = 3 por build principal com restrição temática; Eclipse Trial = 3 chefes reduzidos (1 fase, ~90 s, 50% HP), 1 Rest entre 2º e 3º, Distortion fixa.
- Arquivos: escreve `Code/Meta/ChallengeAltar/*`, `DataSource/challenges.csv`, `UI/ChallengeAltar/*`, testes.
- Restrições: regras especiais expressas como filtros/modificadores de dados; recordes locais.
- Aceite: critério 8 e Eclipse Trial de 10.
- Verificação: EditMode (mesma data → mesmo mapa e Distortions); PlayMode abrindo cada modo.
- Entrega: ≤ 200 palavras.

**T7 — Endless Sundering e Eclipse Cycles**
- Objetivo: Endless (A10+) e Eclipse Cycles (Eclipse+) conforme 10 §8.4.
- Contexto: Endless troca região a cada 10 salas, +1 Ascension equivalente a cada 5, checkpoint com Rift Anchor a cada 10 (loot do bloco anterior fica secured), recorde de profundidade. Cycles: rotação de 8 semanas em calendário local, modificador global opcional + Distortions temáticas + cosméticos, sem reset.
- Arquivos: escreve `Code/Run/Endless/*`, `Code/Meta/Cycles/*`, `DataSource/eclipse_cycles.csv`, testes.
- Restrições: Endless reaproveita o gerador de M15 em blocos; nada de geometria procedural nova.
- Aceite: critério 9 e Cycles ativos por data.
- Verificação: PlayMode com bot atravessando 30 salas sem leak (Memory Profiler snapshot antes/depois, diferença < 50 MB); EditMode de rotação por data.
- Entrega: ≤ 200 palavras.

**T8 — The Sundering Heart**
- Objetivo: pinnacle de 3 fases de 09 §15.5 com requisito, consumo de Seals e drops de 10 §8.5.
- Contexto: Fase 1 cicla as 5 arenas a cada 30 s (2 assinaturas por chefe); Fase 2 com 6 núcleos de afinidade e janela Exposed de 8 s; Fase 3 com arena encolhendo, Resonance inimiga e Annihilation a cada 40 s (telegraph ≥ 2,0 s). Duração alvo 6 min (R16). 4 Pinnacle Uniques (1 por herói, modificam o Ultimate).
- Arquivos: escreve `Code/AI/Bosses/SunderingHeart/*`, `Content/Bosses/SunderingHeart/*`, linhas Pinnacle em `DataSource/items_unique.csv` se M28 não as criou.
- Restrições: assinaturas reaproveitam ataques dos 5 chefes por referência.
- Aceite: critério 11.
- Verificação: PlayMode de transição de fases; bot de referência mede 5–7 min.
- Entrega: ≤ 200 palavras.

**T9 — Loot de endgame**
- Objetivo: Primal (A50+, 85–100%, nunca acima do máximo), recompensa por Peril no gerador, Set Caches (400 Marks, proteção de duplicata de 05).
- Contexto: 05 Primal, 10 §8.6–8.7, teto ×2,0 (R45), R31.
- Arquivos: escreve `Code/Core/Loot/Primal*`, `Code/Meta/Economy/SetCache*`, testes.
- Restrições: em `Eclipse.Core`; seed determinística.
- Aceite: critério 12; Set Cache não repete peça até completar 6.
- Verificação: `dotnet test` com 100.000 amostras; EditMode.
- Entrega: ≤ 150 palavras.

**T10 — Simulações e bots**
- Objetivo: relatórios automáticos que provem os critérios 4–12.
- Arquivos: escreve `Code/Editor/Simulators/Endgame*`, `Code/Tests/PlayMode/Endgame*`.
- Aceite: relatórios CSV gerados em `Logs/` e testes PlayMode verdes.
- Verificação: PlayMode (CLAUDE.md §6) + execução do simulador via `-executeMethod`.
- Entrega: ≤ 150 palavras + caminhos dos relatórios.

**T11 — Revisão integrada**
- Objetivo: confirmar os 13 critérios, atualizar Status/Log, ROADMAP §7 e CLAUDE.md (novos comandos de simulação).
- Verificação: todos os comandos de §Verificação do módulo verdes.

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
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
Playtest manual de aceite do Marco G: 1 run A1 com Sigil de 2 Distortions, 1 Boss Echo I, Daily Rift, 30 salas de Endless, The Sundering Heart com build de referência.

## Riscos e rollback
- **Explosão combinatória de Distortions × afixos × Phenomena** → validador recusa combinações proibidas; teste de recursão (07) com Sigil máximo.
- **Duração do pinnacle fora do alvo** → ajuste de HP por dados, sem código.
- **Endless com vazamento de memória** → bot de 30 salas obrigatório no CI; rollback: desabilitar modo por flag de dados.
- **Escopo grande para uma sessão** → ondas internas permitem pausar entre T5 e T6; T6–T8 podem virar sub-módulos se o T1 mostrar lacunas grandes.

## Atualizações de documentação
- `docs/ROADMAP.md` §7: status M29 e Marco G.
- `CLAUDE.md` §6: comando do simulador de endgame, se for um `-executeMethod` novo.
- Lacunas de design encontradas → Log deste arquivo e pergunta ao usuário (não alterar 09/10 sem decisão).

## Prompt de disparo
```
Implemente o módulo M29 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M29-endgame.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M29-endgame.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

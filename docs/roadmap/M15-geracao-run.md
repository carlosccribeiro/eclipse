# M15 — Geração de run (mapa de nós e Encounter Director)
Fase 3 · Onda 4 · Status: ⏳ não iniciado
Depende de: M01 · Desbloqueia: M16 · Paralelo com: M05
Branch: modulo/M15-geracao-run
Docs de referência: 09 §10 (orçamento de ameaça, ondas, limites, receitas), 09 §11 (mapa de nós, pesos, restrições), 09 §14 regra 6 dos eventos (via 09 §11.3), 10 §3.1 (Estratos e pontos de progressão), 11 §3.11 (mapa e Encounter Director), 11 §3.15 item 6 (Run Map Previewer), 00 §5 R33, CLAUDE.md §4

## Objetivo
Entregar, em C# puro (`Eclipse.Core`), o gerador determinístico do mapa da run e o planejador de encontros (Encounter Director). Os dois devem respeitar todas as regras de 09 §10–11 e ser verificáveis por testes de propriedade sobre milhares de seeds. Completa o módulo uma janela de editor que gera mapas e encontros por seed e mostra estatísticas.

## Critérios de saída (verificáveis)
1. `MapGenerator.Generate(seed, ctx)` gera **13 linhas + chefe** (09 §11.1): linha 1 com 2 nós Combat; linhas 2–4 com 2–3 nós; linha 5 é Clareira com ≥1 Rest e sem combate; linhas 6–8 com 3–4 nós; linha 9 é 1 Guardian; linhas 10–12 com 2–4 nós; linha 13 é 1 Antecâmara; depois o Boss.
2. Teste de propriedade com **10.000 seeds** sem violações das 7 restrições de 09 §11.3, de arestas cruzadas ou de nós inalcançáveis.
3. Com 10.000 seeds, a média de salas por tipo no caminho escolhido aleatoriamente fica dentro da coluna "Média esperada por run" de 09 §11.2, com tolerância de ±0,5 sala.
4. Mesma seed + mesmo contexto ⇒ mapa idêntico (hash igual) em `dotnet test` e no EditMode.
5. `EncounterPlanner.Plan(...)` calcula o orçamento `(20 + 3 × Linha) × MultDensidade × MultRegião` (09 §10.1), com Elite Arena ×1,2 e matilha ≥ 50%. Divide em 2 ondas (linhas 1–4) ou 3 ondas (linhas 5+) nas proporções de 09 §10.2 e respeita os limites de 09 §10.3. Teste com 10.000 encontros: zero violações.
6. `EncounterRuntimeRules` expõe a regra de próxima onda (PA vivo < 30% da onda atual ou 25 s) e as distâncias de spawn (≥ 8 m), como funções puras testadas.
7. Run Map Previewer (Editor) gera um mapa por seed e mostra tipos de nó, arestas, caminhos de alto e baixo risco e estatísticas agregadas de N seeds.
8. `dotnet test tools/CoreTests --filter Category=RunGen` e os EditMode tests passam. Nenhum arquivo de `Code/Gameplay/Combat/**` (par M05) foi tocado.

## Fora de escopo
- Carregar salas, spawnar inimigos e a UI do mapa na run (M16).
- Overrift / Delve Deeper (fica em M29; 10 §3.5).
- Room Modifiers de Nightmare+ (09 §9.2): o gerador reserva o campo `RoomModifierId` vazio, e o sorteio entra junto com o conteúdo de dificuldade (M20/M29).
- Conteúdo de eventos e Blessings (M16): aqui o gerador só sorteia o **ID** do evento, a partir de um pool recebido por parâmetro.

## Premissas e decisões
- Os tipos de sala são os 9 de 09 §9: Combat, Elite, Guardian, Treasure, Shrine, Event, Challenge, Rest e Boss. A Antecâmara é a variante `Rest.Antechamber` (11 §3.4.2).
- O sorteio de tipo usa os pesos de 09 §11.2 por Estrato, com reamostragem guiada por restrição (rejeição local por linha, com até N tentativas e fallback determinístico). Se a rejeição falhar, o gerador **reconstrói a linha** e nunca entrega um mapa inválido; o número de tentativas é métrica do Previewer.
- A recompensa de cada porta de Combat (Equipamento, Rift Shards, Aether Dust, Blessing Lesser ou Cura — 09 §9) é sorteada no gerador, porque a restrição 7 de 09 §11.3 depende dela.
- O histórico entre runs necessário à regra 6 de 09 §11.3 (o mesmo evento não aparece em 2 runs seguidas da mesma região) chega como `RunHistory` (IDs de eventos da última run por região). Quem persiste é M18/M16.
- Os candidatos de inimigo chegam ao planner como DTO puro `EnemyCandidate { Id, Archetype, Threat(PA), Role(Frontline/Specialist/Fodder/Tank/Summoner/Caster), RegionWeight }`. Os dados reais (PA de 09 §5) vêm das definições de M07/M16. Os testes usam o bestiário da Ashen Wastes de 09 §5.1 como fixture.
- RNG: streams `Map` e `Encounter` de M01 (CLAUDE.md §4). É proibido `System.Random`/`UnityEngine.Random`.
- Pesos, frequências e constantes saem de `DataSource/run/*.csv` (D6). As tabelas são copiadas de 09; o código não traz números de 09 hardcoded, só os defaults dos fixtures de teste.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Modelo e gerador do mapa de nós | execução | executor | — | `Assets/_Project/Code/Core/RunGen/Map/*.cs` | ⏳ |
| T2 | Planejador de encontros (orçamento, ondas, limites, receitas) | execução | executor | — | `Assets/_Project/Code/Core/RunGen/Encounters/*.cs` | ⏳ |
| T3 | Testes de propriedade do mapa (10k seeds) | execução | executor | T1 | `tools/CoreTests/RunGen/MapGeneratorTests.cs`, `Assets/_Project/Code/Tests/EditMode/RunGen/MapGeneratorEditModeTests.cs` | ⏳ |
| T4 | Testes de propriedade de encontros (10k) | execução | executor | T2 | `tools/CoreTests/RunGen/EncounterPlannerTests.cs`, `Assets/_Project/Code/Tests/EditMode/RunGen/EncounterPlannerEditModeTests.cs` | ⏳ |
| T5 | Dados: SOs de configuração, CSVs e importadores | execução | executor | T1, T2 | `Assets/_Project/Code/Data/Run/*.cs`, `DataSource/run/*.csv`, `Assets/_Project/Code/Editor/DataPipeline/Importers/RunGenImporters.cs` | ⏳ |
| T6 | Run Map Previewer (janela de editor) | execução | executor | T5 | `Assets/_Project/Code/Editor/RunMapPreviewer/*` | ⏳ |
| T7 | Revisão, integração e documentação | integração | principal | T3, T4, T6 | `docs/roadmap/M15-geracao-run.md`, `docs/ROADMAP.md`, `CLAUDE.md` (se aplicável) | ⏳ |

## Ondas internas
1. T1 ∥ T2 (pastas `Map/` e `Encounters/` disjuntas)
2. T3 ∥ T4 ∥ T5
3. T6
4. T7

## Briefs

### T1 — Modelo e gerador do mapa de nós
Objetivo: `Eclipse.Core.RunGen.Map` gera, a partir de seed e contexto, um DAG em camadas válido conforme 09 §11.
Contexto: Core é C# puro (sem UnityEngine nem pacotes Unity; 00 §5 R52). Use o RNG por streams de M01 (stream `Map`). Estrutura das linhas em 09 §11.1; pesos por Estrato em 09 §11.2; restrições em 09 §11.3; Estratos I = 1–5, II = 6–9, III = 10–13 (00 §5 R33). Recompensas de porta de Combat em 09 §9 (tabela, coluna Recompensa).
Arquivos: lê `docs/gdd/09-inimigos.md` §9, §11; `Assets/_Project/Code/Core/Rng/*` e `Assets/_Project/Code/Core/Ids/*` (M01). Escreve `Assets/_Project/Code/Core/RunGen/Map/` (`RoomType.cs`, `RunMap.cs`, `MapNode.cs`, `MapGenContext.cs`, `MapGenConfig.cs`, `MapGenerator.cs`, `MapConstraints.cs`, `RunHistory.cs`, `DoorReward.cs`). Não toque em outros arquivos.
Restrições:
- `MapGenConfig` recebe pesos e contagens por parâmetro; nada de constantes mágicas fora dos defaults documentados com referência a 09.
- Arestas não se cruzam: ordene os nós por coluna e conecte só a vizinhos monotônicos. Cada nó liga a 1–2 nós da linha seguinte e todo nó é alcançável.
- `MapConstraints.Validate(map)` retorna a lista de violações (vazia = válido) e é usado pelo próprio gerador.
- API sem alocação por frame não é exigida (roda fora do combate), mas não use LINQ em laços internos.
Aceite: compila no CoreTests e no Unity. `Generate` é determinístico, nunca retorna mapa com violação e expõe `RunMap.Hash()` estável.
Verificação: `dotnet build tools/CoreTests` → 0 erros; o teste de fumaça `MapGeneratorSmoke` (crie dentro de T1, num único arquivo, em `tools/CoreTests/RunGen/MapGeneratorSmoke.cs`) gera seed 1 e valida → passa.
Entrega: no máximo 200 palavras — API pública, decisões de algoritmo, arquivo:linha das regras de 09 aplicadas, o que não foi verificado.

### T2 — Planejador de encontros
Objetivo: `Eclipse.Core.RunGen.Encounters` converte (linha, tipo de sala, dificuldade, região, pool de candidatos, receita) em um `EncounterPlan` com ondas e unidades, respeitando 09 §10.
Contexto:
- Orçamento: `(20 + 3 × Linha) × MultDensidade(dif) × MultRegião` (09 §10.1; Normal 1,0 · Veteran 1,1 · Nightmare 1,2 · Eclipse 1,3 · Ascension 1,3 + 0,005N, máx. 1,8). Elite Arena ×1,2, com a matilha Elite ≥ 50%.
- Ondas: 2 (linhas 1–4) ou 3 (linhas 5+), divisão 40/35/25 ou 55/45 (09 §10.2).
- Limites (09 §10.3): 24 unidades vivas, 40 PA vivos; ≤ 2 casters e ≤ 1 summoner por onda; specialists ≤ 40% do PA da onda; ≥ 1 frontline por onda; ≤ 1 tank por onda até a linha 6 e 2 depois.
- Receitas de 09 §10.4 (Swarm, Phalanx, Siege, Ambush, Ritual, Brood, Mixed Patrol) como dados ponderados por região e linha.
- Regras de runtime puras: próxima onda quando PA vivo < 30% da onda atual ou após 25 s; spawn a ≥ 8 m; portal de 0,8 s (09 §10.2).
Arquivos: lê `docs/gdd/09-inimigos.md` §5.1, §10; RNG de M01. Escreve `Assets/_Project/Code/Core/RunGen/Encounters/` (`EncounterPlanner.cs`, `EncounterPlan.cs`, `EnemyCandidate.cs`, `EncounterRecipe.cs`, `ThreatBudget.cs`, `EncounterLimits.cs`, `EncounterRuntimeRules.cs`).
Restrições: Core puro e stream `Encounter`. Nenhuma referência a tipos de Gameplay/AI. Os valores numéricos entram via `EncounterConfig` (default com comentário citando 09 §x).
Aceite: `Plan` nunca viola os limites; quando o orçamento não fecha dentro dos limites, reduz o PA (nunca estoura) e marca `UnderBudget`.
Verificação: `dotnet build tools/CoreTests` → 0 erros; smoke em `tools/CoreTests/RunGen/EncounterPlannerSmoke.cs` com o bestiário de 09 §5.1 → passa.
Entrega: no máximo 200 palavras — API, como os limites são garantidos, pendências.

### T3 — Testes de propriedade do mapa
Objetivo: provar os critérios 1–4 sobre 10.000 seeds.
Contexto: gerador de T1. Médias esperadas em 09 §11.2 (Combat 5–6, Elite 2–3, Treasure 1–2, Shrine 2 + Clareira, Event 1–2, Challenge 0–2). Restrições de 09 §11.3.
Arquivos: lê `Assets/_Project/Code/Core/RunGen/Map/*`. Escreve `tools/CoreTests/RunGen/MapGeneratorTests.cs` e `Assets/_Project/Code/Tests/EditMode/RunGen/MapGeneratorEditModeTests.cs` (o EditMode roda 500 seeds para ficar rápido; o dotnet roda 10.000).
Restrições: testes marcados `[Category("RunGen")]`. Sem alterar o gerador: falha de propriedade deve ser **reportada**, não contornada.
Aceite: todos passam ou as falhas estão listadas com a seed que reproduz.
Verificação: `dotnet test tools/CoreTests --filter Category=RunGen` → passa; EditMode (CLAUDE.md §6) → passa.
Entrega: no máximo 150 palavras — testes criados, resultados, seeds problemáticas.

### T4 — Testes de propriedade de encontros
Objetivo: provar os critérios 5 e 6 sobre 10.000 planos (linhas 1–13 × 4 dificuldades × 7 receitas × seeds).
Contexto: planner de T2; fixture com os 8 inimigos da Ashen Wastes (09 §5.1: PA 1, 3, 4, 5, 8, 6, 5, 5 e arquétipos correspondentes).
Arquivos: lê `Assets/_Project/Code/Core/RunGen/Encounters/*`. Escreve `tools/CoreTests/RunGen/EncounterPlannerTests.cs` e `Assets/_Project/Code/Tests/EditMode/RunGen/EncounterPlannerEditModeTests.cs`.
Restrições: os mesmos de T3.
Aceite: zero violações de limite; orçamento usado ≥ 90% do teórico em ≥ 95% dos casos sem `UnderBudget`; exemplos de 09 §10.1 (linha 1 Normal = 23 PA; linha 12 Eclipse ≈ 73 PA) reproduzidos.
Verificação: `dotnet test tools/CoreTests --filter Category=RunGen` → passa.
Entrega: no máximo 150 palavras.

### T5 — Dados de configuração da geração
Objetivo: tornar pesos, contagens, receitas e multiplicadores editáveis por CSV → ScriptableObject, sem números de 09 no código de produção.
Contexto: pipeline de dados de M02 (`Definition` com `StableId`, `DataImporter`, `DataValidator`). Tabelas: `room_weights.csv` (09 §11.2), `map_rows.csv` (09 §11.1), `door_rewards.csv` (09 §9), `encounter_recipes.csv` (09 §10.4), `density.csv` (09 §10.1), `encounter_limits.csv` (09 §10.3).
Arquivos: lê `Assets/_Project/Code/Data/Definition*.cs` e `Assets/_Project/Code/Editor/DataPipeline/*` (M02). Escreve `Assets/_Project/Code/Data/Run/MapGenConfigDefinition.cs`, `EncounterConfigDefinition.cs`, `EncounterRecipeDefinition.cs`; `DataSource/run/*.csv`; `Assets/_Project/Code/Editor/DataPipeline/Importers/RunGenImporters.cs`. Se o importador de M02 exigir registro central, adicione **uma linha** no ponto de registro e diga qual.
Restrições: os SOs convertem para os configs puros de Core (`ToCore()`). Validações no `DataValidator`: soma de pesos > 0, contagens coerentes com 09 §11.1, limites positivos.
Aceite: importar gera os SOs; `ToCore()` produz config igual aos defaults dos testes.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → sem erros; EditMode → passa.
Entrega: no máximo 150 palavras.

### T6 — Run Map Previewer
Objetivo: janela de editor (UI Toolkit) `Eclipse/Run Map Previewer` que gera e desenha o mapa por seed e mostra estatísticas de N seeds (11 §3.15 item 6).
Contexto: gerador de T1, planner de T2, configs de T5.
Funções da janela:
- campo de seed, região e dificuldade; botão Gerar;
- grafo por linha com cor por tipo e recompensa da porta;
- destaque do caminho de ≤ 2 Elites e do de ≥ 3 Elites;
- painel "Lote": gera N seeds e mostra distribuição por tipo, violações (deve ser 0), tentativas médias de reamostragem e, ao clicar em um nó de combate, o `EncounterPlan`.
Arquivos: escreve só `Assets/_Project/Code/Editor/RunMapPreviewer/` (`RunMapPreviewerWindow.cs`, `.uxml`, `.uss`).
Restrições: editor-only (assembly `Eclipse.Editor`). Nada de lógica de geração duplicada na janela.
Aceite: abre sem erros; seed 1 gera e desenha; o lote de 1.000 seeds roda em < 10 s e mostra 0 violações.
Verificação: compilação limpa no batchmode (EditMode run) + teste EditMode que instancia a janela via `EditorWindow.GetWindow` e chama `GenerateBatch(100)` sem exceção.
Entrega: no máximo 120 palavras + 1 captura descrita (o que aparece).

### T7 — Revisão e integração (principal)
Revisar diffs de T1–T6 contra 09 §10–11. Rodar as verificações do módulo, atualizar Status/Log deste arquivo e a tabela do ROADMAP. Atualizar o CLAUDE.md se surgir convenção nova (ex.: categoria de teste `RunGen`).

## Verificação do módulo
```bash
dotnet test tools/CoreTests --filter Category=RunGen
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
Conferir também: `git diff --stat` não mostra arquivos em `Assets/_Project/Code/Gameplay/` (par M05).

## Riscos e rollback
- **Rejeição sem convergência** em linhas com muitas restrições → reconstruir a linha inteira e, em último caso, usar um template determinístico válido por Estrato. A métrica de tentativas aparece no Previewer.
- **Médias fora da faixa** de 09 §11.2 → ajustar pesos no CSV (dado), nunca no código. Registrar no Log e avisar o dono de 09 se o ajuste mudar a intenção de design.
- **Limites incompatíveis com o orçamento** em linhas altas → `UnderBudget` sinalizado; reportar a 09 §18 (questão de playtest).

## Atualizações de documentação
- `CLAUDE.md` §6: acrescentar o filtro `--filter Category=RunGen` como exemplo, se a convenção de categorias for nova.
- `docs/ROADMAP.md` §7: status do M15.

## Prompt de disparo
```
Implemente o módulo M15 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M15-geracao-run.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M15-geracao-run.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

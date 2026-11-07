# M16 — Fluxo de run, salas e The Ashen Wastes (greybox)
Fase 3 · Onda 9 · Status: ⏳ não iniciado
Depende de: M07, M13, M15 (usa também M08, M11 e M18, já concluídos em ondas anteriores) · Desbloqueia: M17, M19, M20 · Paralelo com: M14
Branch: modulo/M16-fluxo-run
Docs de referência:
- 09: §3.2 (tokens), §3.3 (telegraph), §5.1 (bestiário Ashen Wastes), §6 (afixos de Elite; Normal em §6.1–6.3), §9 (tipos de sala, Challenge), §10 (orçamento e ondas), §11 (mapa), §12 (Shrines/Blessings), §13 (Curses), §14 (eventos)
- 10: §3.1 (Satchel), §3.3 (Rift Shards), §3.4 (Rest Room), §4 (morte e retenção), §4.1 (Echo of a Fallen Eclipsed)
- 05: §3.1 (fontes de loot), §3.2 (quantidade), §12 (coleta, Satchel, filtro), §13 (salvage)
- 06: §3.13 (TTK e durações), Aether Flask (recarga na Rest)
- 07: status usados por Blessings
- 11: §3.4.3 (`RunState`), §3.11 (runs e salas), §3.12 (loot)
- 00 §5: R33, R34, R35, R36, R37, R45, R46

## Objetivo
Transformar o mapa de M15 numa run jogável de ponta a ponta em The Ashen Wastes, em greybox:
- o RunManager conduz Hub → salas → escolha de caminho → … → Antecâmara → porta do chefe;
- salas carregadas por Addressables;
- todos os tipos de sala funcionando (exceto Guardian e Boss, que entram como ganchos para M17);
- os 8 inimigos da região, Elites com os afixos de Normal, Blessings, Curses, eventos mínimos, Rest Room com serviços, loot por fonte com coleta automática e Satchel;
- regras de morte e de extração, e suspend-save.

## Critérios de saída (verificáveis)
1. Uma run em Normal com seed fixa percorre as linhas 1–13 seguindo o mapa de M15. O teste PlayMode `RunSmokeTest` usa bot de navegação e god mode e chega à porta do Boss sem erro nem exceção no log.
2. Tipos de sala:
   - Combat Arena e Elite Arena usam o `EncounterPlanner` de M15, com ondas, portal de 0,8 s, spawn a ≥ 8 m e próxima onda pela regra de 09 §10.2.
   - O teto de 24 unidades e 40 PA vivos nunca é excedido: contador instrumentado no teste.
   - Treasure Room com baús, Hollow Coffer (15%) e Reliquary (escolher 1 de 3).
   - Shrine com 3 ofertas e escolha de 1.
   - Event Room com o framework e 4 eventos.
   - Challenge Room com 2 variantes.
   - Rest Room (Clareira) e Antecâmara com todos os serviços de 10 §3.4.
3. Os **8 inimigos** de 09 §5.1 jogáveis com telegraphs de 06/09, e os **9 afixos de Elite de Normal** de 09 §6.1: Molten, Glacial, Overcharged, Blighted, Siphoning, Hasted, Commanding, Vengeful e Mortar. Respeitam 1 afixo por Elite em Normal (09 §6.2), o peso regional e as incompatibilidades (09 §6.3), e geram nome conforme 09 §6.3.
4. Blessings:
   - framework com ranks Lesser/Greater/Exalted (70/25/5, Exalted só no Estrato II+), buckets Increased/More e tetos de More (≤ 8% por Blessing, ≤ 20% no total; 09 §12.1);
   - smart offer ×2 por tag;
   - **9 Blessings**: as 3 Ember e as 6 Iron de 09 §12.3;
   - Shrines de Ember, Iron e Blood (09 §12.2).
   O validador de dados rejeita uma Blessing com More acima do teto.
5. Curses Haunted, Withering e Starved (09 §13) aplicáveis por evento, removíveis por Purificar (10 §3.4).
6. Loot:
   - cada fonte da tabela 05 §3.1 presente na run gera itens via `ItemGenerator` de M12, com o contexto correto (fonte, iLvl da sala, dificuldade e herói);
   - coleta automática ao limpar a sala; Satchel de 30;
   - com o Satchel cheio, o item de menor raridade é salvado (10 §3.1);
   - filtro de auto-salvage por raridade;
   - Rift Shards por sala e fonte (10 §3.3).
7. Regras de fim de run (10 §4):
   - extração por Rift Anchor mantém tudo e converte Rift Shards em Gold (1 Shard = 5 Gold × fator de dificuldade);
   - na morte, Legendary, itens equipados e itens com Anchor são mantidos; Epic/Rare/Common viram materiais a 75% em Normal; Gold, Dust e Cores ficam em 75% em Normal; Rift Shards são perdidos; XP é 100%;
   - os testes EditMode cobrem a tabela inteira de 10 §4 em Normal e em Veteran (fatores de 50%).
8. Suspend-save (M18) é gravado na entrada de cada sala. "Continuar" restaura o `RunState` e apaga o `run.sav`. A morte grava o registro da última run morta (10 §4.1), e o evento Echo of a Fallen Eclipsed devolve 50% dos Rift Shards + 1 item conforme 10 §4.1.
9. UI da run:
   - mapa com Estrato atual visível e Estratos seguintes só com contagem (09 §11.1);
   - prévia da recompensa na porta;
   - tela de oferta de Blessing;
   - Rest Room com serviços;
   - resumo de fim de run.
10. Performance na sala mais densa da linha 12 em Normal: 60 fps e 0 B de GC por frame no profiler durante o combate (CLAUDE.md §4), registrado no Log com o hardware.
11. EditMode + PlayMode passam e o validador de dados está sem erros. `git diff --stat` não toca `Code/Editor/Simulators/**` nem a cena de Training Grounds (par M14).

## Fora de escopo
- Guardian Arena e Boss Room (M17): aqui são ganchos com placeholder (sala vazia que conclui ao entrar), para o fluxo fechar.
- Overrift / Delve Deeper (M29). Room Modifiers e afixos Veteran+ (M20 para dificuldade, M29 para endgame).
- Eventos além dos 4 deste módulo e Blessings das outras afinidades (entram com as regiões M24–M27 e no catálogo M28).
- Sanctuary (M19). Aqui o "Hub" é uma cena mínima com botão "Iniciar run" e escolha de herói, sem serviços.
- Aplicar XP a níveis (M20). Aqui só registrar o XP ganho no `RunResult`.

## Premissas e decisões
- A run usa **Kael** (único herói jogável até M21) e dificuldade **Normal**. O código lê `DifficultyDefinition`, com a tabela completa entregue por M20; aqui só existe a definição Normal, com os dados de 10 §7 e 09 §6.2.
- Quais inimigos de 09 §5.1 vieram do M07? O executor de T1 confirma lendo `Content/Enemies/AshenWastes/`; os restantes, até completar 8, são feitos em T6.
- Os 4 eventos (09 §14) são Bleeding Remnant, Altar of Embers, Gambler's Cairn e Echo of a Fallen Eclipsed. Cobrem custo em HP, custo em Rift Shards, aposta, Curse e o gancho roguelite.
- Os 2 Challenges (09 §9.1) são Trial of Haste e Untouched.
- Salas greybox são geradas por **ferramenta de editor** a partir de specs (`DataSource/rooms/ashen_wastes_rooms.csv`): retângulos/arenas com obstáculos, portas, spawn points, `RoomAnchor` e NavMesh baked. Meta: 8 Combat, 2 Elite, 2 Treasure, 1 Shrine, 2 Event, 1 Challenge, 1 Clareira e 1 Antecâmara (11 §3.11).
- XP e Rift Shards por PA (09 §10.5) — **fechados pelo D02:** XP por PA em 10 §5.1 (R56: `2 × NívelDeConteúdo(eLvl)^1,1 ÷ MultDensidade`; Guardian 60 PA, chefe 150 PA); 0,2 Rift Shard por PA em 10 §3.3 (R57). Gravar em `xp_per_threat.csv` (sem placeholder).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Mapear APIs de M07/M08/M11/M12/M13/M15/M18 e lacunas | investigação | investigador | — | `docs/roadmap/M16-fluxo-run.md` (seção Log, via principal) | ⏳ |
| T2 | RunManager, `RunState` de runtime e `RunResult` | execução | executor | T1 | `Assets/_Project/Code/Run/Flow/*.cs`, `Assets/_Project/Code/Save/Mapping/RunStateMapping.cs` | ⏳ |
| T3 | Carregamento de salas, `RoomAnchor`, portas e pré-carregamento | execução | executor | T2 | `Assets/_Project/Code/Run/Rooms/*.cs` | ⏳ |
| T4 | Ferramenta greybox e salas da Ashen Wastes | execução | executor | T1 | `Assets/_Project/Code/Editor/Greybox/*`, `DataSource/rooms/ashen_wastes_rooms.csv`, `Assets/_Project/Content/Regions/AshenWastes/Rooms/*` | ⏳ |
| T5 | Encounter runtime (ondas, portais, limites) e Elites com 9 afixos | execução | executor | T2, T6 | `Assets/_Project/Code/Run/Encounters/*.cs`, `Assets/_Project/Code/AI/Elites/*.cs`, `Assets/_Project/Content/Enemies/EliteAffixes/*`, `DataSource/enemies/elite_affixes.csv` | ⏳ |
| T6 | Inimigos restantes da Ashen Wastes (até completar 8) | execução | executor | T1 | `Assets/_Project/Content/Enemies/AshenWastes/*`, `Assets/_Project/Code/AI/Archetypes/*` (só arquétipos novos), `DataSource/enemies/ashen_wastes.csv` | ⏳ |
| T7 | Blessings, Shrines e Curses | execução | executor | T2 | `Assets/_Project/Code/Run/Blessings/*.cs`, `Assets/_Project/Code/Data/Run/BlessingDefinition.cs`, `ShrineDefinition.cs`, `CurseDefinition.cs`, `Assets/_Project/Content/Run/Blessings/*`, `DataSource/run/blessings.csv`, `DataSource/run/curses.csv` | ⏳ |
| T8 | Treasure, Event (4 eventos) e Challenge (2 variantes) | execução | executor | T2, T7 | `Assets/_Project/Code/Run/RoomTypes/*.cs`, `Assets/_Project/Code/Run/Events/*.cs`, `Assets/_Project/Content/Run/Events/*`, `DataSource/run/events.csv` | ⏳ |
| T9 | Rest Room, loot por fonte, Satchel, Rift Shards | execução | executor | T2, T7 | `Assets/_Project/Code/Run/Services/*.cs`, `Assets/_Project/Code/Run/Loot/*.cs`, `DataSource/run/rest_services.csv`, `DataSource/run/run_rewards.csv` | ⏳ |
| T10 | Fim de run: morte, extração, conversão, última run morta, suspend-save | execução | executor | T2, T9 | `Assets/_Project/Code/Run/RunEnd/*.cs` | ⏳ |
| T11 | UI da run (mapa, portas, Blessing, Rest, resumo) | execução | executor | T3, T7, T9 | `Assets/_Project/Code/UI/Run/*.cs`, `Assets/_Project/UI/Run/*.uxml`, `Assets/_Project/UI/Run/*.uss` | ⏳ |
| T12 | Testes PlayMode/EditMode da run e medição de performance | execução | executor | T3, T5, T8, T10, T11 | `Assets/_Project/Code/Tests/PlayMode/Run/*.cs`, `Assets/_Project/Code/Tests/EditMode/Run/*.cs` | ⏳ |
| T13 | Revisão, playtest guiado e documentação | integração | principal | T12 | `docs/roadmap/M16-fluxo-run.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2 ∥ T4 ∥ T6
3. T3 ∥ T5 ∥ T7
4. T8 ∥ T9
5. T10 ∥ T11
6. T12
7. T13

## Briefs

### T1 — Mapa de APIs e lacunas (investigador)
Objetivo: listar, com arquivo:linha, as APIs prontas que M16 vai consumir e as lacunas.
APIs a mapear:
- M15: `MapGenerator`, `EncounterPlanner`, `EncounterRuntimeRules`;
- M07: spawner, pooling, `EnemyDefinition`, HFSM, tokens;
- M12: `ItemGenerator`, `LootContext`;
- M13: inventário, equipamento;
- M11: aplicação de status;
- M08: Fury e Flask, se existir;
- M18: `SaveService`, `RunStateDto`;
- M01: Event Bus, RNG.
Também: quais inimigos de 09 §5.1 já existem em `Content/Enemies/AshenWastes/` e se há `DifficultyDefinition`.
Arquivos: lê `Assets/_Project/Code/**`, `Assets/_Project/Content/**`, `docs/roadmap/M07*.md`, `M12*.md`, `M13*.md`, `M15*.md`, `M18*.md`. Não escreve código.
Aceite: tabela "API → arquivo:linha → assinatura → observação" e lista de lacunas, cada uma com proposta de solução dentro de M16 ou de onde ela pertence.
Verificação: n/a (investigação).
Entrega: no máximo 400 palavras.

### T2 — RunManager e estado da run
Objetivo: `RunManager` como máquina de estados `Hub → RunStart → Room(n) → Reward → MapChoice → … → Antechamber → BossDoor → RunEnd → Hub`, com saídas `Extract` e `Death` (11 §3.11). É o único dono do `RunState` (11 §3.4.3).
Contexto: `RunState` contém seed, `RunMap`, nó atual, Blessings, Curses, Rift Shards, Satchel (30), itens com Anchor, cargas do Aether Flask, HP e recurso do herói. `RunResult` contém o desfecho, o XP ganho, as moedas, os itens retidos e convertidos e o registro de morte. O `BossDoor` chama o gancho `IBossEncounterProvider`; M17 implementa, e aqui um stub conclui a run como vitória para o fluxo fechar.
Arquivos: escreve `Assets/_Project/Code/Run/Flow/` (`RunManager.cs`, `RunStateRuntime.cs`, `RunResult.cs`, `RunStates/*.cs`, `IBossEncounterProvider.cs`, `IGuardianEncounterProvider.cs`) e `Assets/_Project/Code/Save/Mapping/RunStateMapping.cs` (completa o stub de M18; se precisar de campo novo no DTO, incremente `schemaVersion` com migração, conforme CLAUDE.md).
Restrições: transições só pelo RunManager; eventos de run publicados no Event Bus (`RunStarted`, `RoomEntered`, `RoomCleared`, `PathChosen`, `RunEnded`). Nada de lógica de UI. Usar RNG `Map` e `Encounter` com a seed da run.
Aceite: teste EditMode que simula transições com salas falsas e verifica as sequências válidas e o mapeamento `RunState ↔ DTO`.
Verificação: EditMode (CLAUDE.md §6) → passa.
Entrega: no máximo 200 palavras.

### T3 — Salas, portas e carregamento
Objetivo: carregar o prefab Addressable da sala do nó atual, posicionar o herói e controlar as portas. A sala de combate fecha ao entrar e abre ao limpar (09 §9). A porta mostra a recompensa (do `DoorReward` de M15). A próxima sala é pré-carregada durante a recompensa e a anterior é descarregada (11 §3.11).
Contexto: `RoomAnchor` (spawns, portas, NavMesh baked, câmera bounds, iluminação). `RoomDefinition` (11 §3.4.2) indica tipo, prefab, spawn points e região. Escolha da sala: sorteio no pool da região pelo tipo, com stream `Map`.
Arquivos: escreve `Assets/_Project/Code/Run/Rooms/` (`RoomAnchor.cs`, `RoomLoader.cs`, `RoomDoor.cs`, `RoomSelector.cs`) e `Assets/_Project/Code/Data/Run/RoomDefinition.cs` (se M15 não criou).
Restrições: carregamento assíncrono; sem `Resources.Load`; sem travar a main thread acima de 1 frame de 33 ms ao trocar de sala (medir).
Aceite: PlayMode carrega 3 salas em sequência e mede o pior frame de troca.
Verificação: PlayMode (CLAUDE.md §6) → passa.
Entrega: no máximo 150 palavras.

### T4 — Ferramenta greybox e salas da Ashen Wastes
Objetivo: janela/comando de editor `Eclipse/Greybox/Build Rooms From CSV` que gera os prefabs de sala a partir de `DataSource/rooms/ashen_wastes_rooms.csv`.
Contexto: cada linha do CSV descreve tipo, tamanho, forma (retângulo, círculo, corredor), obstáculos, portas, spawn points e iluminação. A ferramenta gera geometria com primitivas e material greybox, cria o `RoomAnchor`, faz bake do NavMesh (AI Navigation) e registra no grupo Addressables `Region_AshenWastes`. Quantidade: 8 Combat, 2 Elite, 2 Treasure, 1 Shrine, 2 Event, 1 Challenge, 1 Clareira (Rest) e 1 Antecâmara. As arenas de combate têm ≥ 16 m de lado útil para a câmera de 06.
Arquivos: escreve `Assets/_Project/Code/Editor/Greybox/*`, `DataSource/rooms/ashen_wastes_rooms.csv` e `Assets/_Project/Content/Regions/AshenWastes/Rooms/*` (prefabs gerados e `RoomDefinition` assets).
Restrições: editor-only; os prefabs são reconstruíveis (rodar 2× não duplica). Nada em `Code/Editor/Simulators/**` (M14).
Aceite: rodar o comando gera 18 prefabs com NavMesh e `RoomDefinition`; `ImportAndValidate` sem erros.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Greybox.GreyboxBuilder.BuildAll -quit -logFile Logs/greybox.log` → 18 salas geradas; validador sem erros.
Entrega: no máximo 150 palavras.

### T5 — Encounter runtime e Elites
Objetivo: executar o `EncounterPlan` de M15 numa sala.
Contexto:
- Spawn por ondas com portal de 0,8 s (inimigo inatingível e inativo durante o portal), spawn a ≥ 8 m e fora do campo de visão quando possível, próxima onda pela regra de 09 §10.2 e teto de 24 unidades / 40 PA vivos (09 §10.3). Usa spawner e pooling de M07 e tokens de 09 §3.2 via M07.
- Matilha Elite: 1 Elite + Retainers. Afixos de Normal (09 §6.1), 1 por Elite (09 §6.2), com peso regional, incompatibilidades e nome `[Nome], the [Adjetivo]` (09 §6.3).
- Cada afixo é um `EliteAffixDefinition` (11 §3.4.2) que adiciona componentes: rastro, orbes, chain, poça, tether, buff, aura, nova e morteiro. O telegraph segue a linguagem de 06 (laranja reservado a Ember).
Arquivos: escreve `Assets/_Project/Code/Run/Encounters/*.cs` (`EncounterRunner.cs`, `WaveSpawner.cs`, `SpawnPortal.cs`), `Assets/_Project/Code/AI/Elites/*.cs` (componentes de afixo), `Assets/_Project/Content/Enemies/EliteAffixes/*` e `DataSource/enemies/elite_affixes.csv`.
Restrições: pooling; 0 B de GC por frame no combate; Elites não consomem token (09 §3.2).
Aceite: um teste PlayMode por afixo spawna um Elite com aquele afixo e verifica o efeito observável (ex.: Molten deixa rastro por 4 s; Vengeful dispara a nova 1,5 s após a morte). Outro teste de PlayMode roda 50 ondas sem passar dos tetos.
Verificação: PlayMode → passa.
Entrega: no máximo 200 palavras.

### T6 — Inimigos restantes da Ashen Wastes
Objetivo: completar os 8 inimigos de 09 §5.1 (Cinder Husk, Ashborn Marauder, Slag Hurler, Choir Pyrecaller, Kiln Brute, Ember Matron, Cinder Stalker, Choir Cantor), com HP/Dano relativos, PA, ataque assinatura, telegraph e counterplay do doc.
Contexto: arquétipos e templates de HFSM de M07 (11 §3.10); telegraphs de 06/09 §3.3; letalidade de 06 (09 §3.4); classes de loot (09 §10.5).
Mecânicas que podem exigir componente novo:
- Pyre Shell do Cantor;
- placa frontal do Kiln Brute (−50% dano frontal);
- choco do Ember Matron (máx. 6 Husks, colapsam na morte dela);
- shimmer e dash pelas costas do Stalker;
- Flame Wall e coluna com janela ciano do Pyrecaller.
Arquivos: escreve `Assets/_Project/Content/Enemies/AshenWastes/*` (definições, prefabs greybox e animações placeholder gratuitas), `Assets/_Project/Code/AI/Archetypes/*` (só os arquétipos que M07 não criou) e `DataSource/enemies/ashen_wastes.csv`.
Restrições: números só no CSV, citando 09 §5.1. Wind-up ≥ 0,45 s (00 §5 R14). Sem tocar em `Code/AI/Elites/**` (T5).
Aceite: os 8 inimigos spawnam numa sala de teste e executam o ataque assinatura com telegraph correto. Um teste PlayMode por inimigo verifica que o golpe acerta quando o jogador fica parado e erra quando ele esquiva na janela.
Verificação: PlayMode → passa; validador sem erros.
Entrega: no máximo 200 palavras.

### T7 — Blessings, Shrines e Curses
Objetivo: framework de Blessing conforme 09 §12.1: ranks, oferta de 3 com escolha de 1, pesos 70/25/5 (Exalted só no Estrato II+), smart offer ×2 por tag do kit, buckets Increased/More com tetos (≤ 8% por Blessing, ≤ 20% no total), subida de rank e reroll de oferta.
Conteúdo:
- as **9 Blessings** de 09 §12.3: 3 Ember (Kindling, Pyre Heart, Ashfall) e 6 Iron (Hardened, Second Wind, Fleetfoot, Resource Font, Treasure Sense, Duelist's Focus);
- as Shrines de Ember, Iron e Blood (09 §12.2);
- as Curses Haunted, Withering e Starved (09 §13).
Contexto: Blessings aplicam `StatMod` do StatBlock (M03) e efeitos e status via M11. Second Wind é a única exceção à regra "sem revive" (10 §2 item 21).
Arquivos: escreve `Assets/_Project/Code/Run/Blessings/*.cs`, `Assets/_Project/Code/Data/Run/BlessingDefinition.cs`, `ShrineDefinition.cs` e `CurseDefinition.cs`, `Assets/_Project/Content/Run/Blessings/*`, `DataSource/run/blessings.csv` e `curses.csv`. Adiciona a regra de teto de More no validador: se o `DataValidator` de M02 aceitar regras plugáveis, crie `BlessingValidationRule.cs` na mesma pasta de Run/Blessings; senão, registre a lacuna.
Restrições: More só em Exalted (09 §12.1). Valores apenas no CSV.
Aceite: testes EditMode cobrem pesos de oferta (10.000 sorteios dentro de ±2%), tetos de More, smart offer e Second Wind.
Verificação: EditMode → passa; validador rejeita uma Blessing de teste com More 9%.
Entrega: no máximo 150 palavras.

### T8 — Treasure, Event e Challenge
Objetivo: implementar os comportamentos de sala:
- Treasure (09 §9): 1–2 baús; Hollow Coffer com 15% de chance (onda de PA 15 e o dobro de recompensa, com sinal visual sutil); Reliquary (escolher 1 de 3, piso Epic — 05 §3.1).
- Event: framework de escolhas data-driven (`EventDefinition`, 11 §3.4.2) e 4 eventos de 09 §14:
  - Bleeding Remnant;
  - Altar of Embers (custos em Rift Shards — 10 §3.3);
  - Gambler's Cairn;
  - Echo of a Fallen Eclipsed: luta contra o fantasma com a build registrada a 70% dos stats; a recompensa usa a API de T10.
- Challenge (09 §9.1): Trial of Haste e Untouched; porta trancada até completar ou falhar; recompensa de tier superior conforme 05 §3.1; falha → nada ou Curse.
Arquivos: escreve `Assets/_Project/Code/Run/RoomTypes/*.cs` (`TreasureRoom.cs`, `EventRoom.cs`, `ChallengeRoom.cs`, `ShrineRoom.cs`, `CombatRoom.cs`, `EliteRoom.cs`), `Assets/_Project/Code/Run/Events/*.cs`, `Assets/_Project/Content/Run/Events/*` e `DataSource/run/events.csv`.
Restrições: a regra "evento não se repete" vem do gerador (M15); aqui não é reimplementada.
Aceite: um teste PlayMode por evento e por Challenge, cobrindo cada escolha e o resultado esperado.
Verificação: PlayMode → passa.
Entrega: no máximo 200 palavras.

### T9 — Rest Room, loot e Rift Shards
Objetivo:
- Serviços da Rest Room de 10 §3.4, todos usáveis na mesma visita:
  - Recuperar: 40%, 1× por Rest;
  - Recarregar Flask: automático;
  - Anchor Item: 50 Shards, máx. 2 por run;
  - Rift Anchor: extrai;
  - Temper: 40 para reroll / 80 para elevar;
  - Purificar: substitui o Recuperar.
- Loot por fonte (05 §3.1): trash 3%; Elite com 1 garantido + 30%; recompensa "Equipment" da porta; Treasure; Challenge; Event.
- Quantidade conforme 05 §3.2, com teto de 2,0.
- Coleta automática ao limpar a sala (05 §12.1), Satchel de 30 com auto-salvage do pior item quando cheio (10 §3.1) e filtro por raridade (05 §12.3).
- Rift Shards por tipo de sala (10 §3.3: combate 5–15, Elite 20–30, Treasure 40–60).
Contexto: `ItemGenerator` e `LootContext` de M12 (11 §3.12); valores de salvage em Aether Dust conforme 10 §6.3 (use `ISalvageValueProvider` de M18).
Arquivos: escreve `Assets/_Project/Code/Run/Services/*.cs` (`RestRoomServices.cs`, `RestRoom.cs`), `Assets/_Project/Code/Run/Loot/*.cs` (`RunLootDirector.cs`, `RunSatchel.cs`, `AutoSalvageFilter.cs`, `RiftShardRewards.cs`), `DataSource/run/rest_services.csv` e `DataSource/run/run_rewards.csv`.
Restrições: RNG `Loot`. Itens só no Satchel até o fim da run (o Vault é de M19).
Aceite: testes EditMode cobrem cada serviço (custos, limites, Purificar substituindo Recuperar), Satchel cheio e distribuição de drops por fonte (10.000 simulações, raridade dentro do piso).
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T10 — Fim de run
Objetivo:
- Aplicar a tabela de 10 §4 para Vitória, Extração e Morte, com fatores de Normal 75% e Veteran+ 50%, e converter Rift Shards em Gold (10 §3.3).
- Gravar no profile as moedas e itens retidos via `SaveService` (M18); os itens retidos vão para uma fila "a guardar no Vault", que M19 consome.
- Gravar o registro da última run morta (10 §4.1) e expor `RestoreFromFallenEcho()` para o evento de T8.
- Gravar o suspend-save na entrada de cada sala; "Continuar" no Hub restaura e apaga o arquivo.
Arquivos: escreve `Assets/_Project/Code/Run/RunEnd/*.cs` (`RunEndResolver.cs`, `RetentionRules.cs`, `FallenEchoRecord.cs`, `SuspendSaveHook.cs`).
Restrições: `RetentionRules` é puro e testável. O fator de dificuldade da conversão sai de `DifficultyDefinition`, nunca é hardcoded.
Aceite: tabela-verdade completa de 10 §4 em testes EditMode (cada linha × 3 desfechos × 2 dificuldades).
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T11 — UI da run
Objetivo: telas em UI Toolkit:
- mapa da run: Estrato atual completo, seguintes só com contagem de nós (09 §11.1), caminho percorrido e escolha do próximo nó;
- prévia de recompensa na porta;
- oferta de Blessing (3 cartas, rank e tags) e reroll;
- painel da Rest Room;
- Satchel (lista e filtro);
- resumo de fim de run (desfecho, moedas, itens, XP).
Contexto: framework de UI e HUD de M06; a UI só reage a eventos e consulta o `RunStateRuntime` (CLAUDE.md §4).
Arquivos: escreve `Assets/_Project/Code/UI/Run/*.cs`, `Assets/_Project/UI/Run/*.uxml` e `*.uss`.
Restrições: a UI nunca altera estado diretamente, só via comandos do RunManager e dos serviços. Gamepad e mouse.
Aceite: um teste PlayMode navega por mapa, Blessing, Rest e resumo com input simulado.
Verificação: PlayMode → passa.
Entrega: no máximo 150 palavras.

### T12 — Testes de run e performance
Objetivo:
- `RunSmokeTest` (PlayMode): seed fixa, Kael em god mode, bot que limpa salas (mata tudo via comando de debug após 2 s) e escolhe sempre o primeiro caminho; percorre as linhas 1–13 e verifica eventos, Rift Shards, Satchel e chegada à porta do Boss.
- Testes de morte forçada na linha 6, extração na Clareira e suspend/resume na linha 7.
- Performance: a sala mais densa da linha 12 em Normal, medida com Profiler Recorder (frame time e GC alloc), com resultado gravado em `Logs/perf-m16.txt`.
Arquivos: escreve `Assets/_Project/Code/Tests/PlayMode/Run/*.cs` e `Assets/_Project/Code/Tests/EditMode/Run/*.cs`.
Aceite: todos passam; a performance cumpre o critério 10 ou o desvio fica documentado.
Verificação: PlayMode e EditMode (CLAUDE.md §6) → passam.
Entrega: no máximo 150 palavras, com números de performance.

### T13 — Revisão e playtest (principal)
Rodar uma run manual completa em Normal e checar os critérios 1–11. Revisar diffs contra 09/10/05. Registrar as lacunas (XP por PA e outras) no Log e avisar o usuário. Atualizar ROADMAP e CLAUDE.md:
- pasta `DataSource/rooms/`;
- comando greybox;
- categoria de teste `Run`.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Greybox.GreyboxBuilder.BuildAll -quit -logFile Logs/greybox.log
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

## Riscos e rollback
- **Módulo grande** (13 tarefas): se estourar, dividir em M16a (fluxo, salas, encontros, loot, fim de run) e M16b (Blessings, eventos, Challenge, UI fina). Avisar o usuário antes.
- ~~Lacuna de XP/Shards por PA~~ → fechada pelo D02 (R56, R57).
- **Carregamento de sala com hitch** → pré-carregar durante a recompensa; reduzir o bake/aquecimento de shaders (warmup) da sala.
- **GC no spawn de ondas** → pré-aquecer os pools no carregamento da sala.

## Atualizações de documentação
- `CLAUDE.md` §5: `DataSource/rooms/` e a ferramenta greybox. §6: comando `GreyboxBuilder.BuildAll`.
- `docs/ROADMAP.md` §7. Em §4, ajustar as dependências de M16, se o lead aceitar (ver relatório do plano).

## Prompt de disparo
```
Implemente o módulo M16 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M16-fluxo-run.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M16-fluxo-run.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado. Lacuna conhecida: constante de XP por PA (09 §10.5 delega a 10, que não a fixa).

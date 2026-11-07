# M17 — Framework de chefes, Guardians e The Ashen Colossus
Fase 3 · Onda 10 · Status: ⏳ não iniciado
Depende de: M10, M16 · Desbloqueia: M24 (com M19), Marco C · Paralelo com: — (onda solo)
Branch: modulo/M17-chefes
Docs de referência:
- 09: §7 (Guardians; Ashen Wastes: Cinderwright Vahl e The Kiln Hound), §8.1 (regras gerais de chefe), §8.2 (The Ashen Colossus), §9 (Guardian Arena e Boss Room), §3.3 (telegraph)
- 06: §3.8.2 (Stagger e Broken), §3.13 (TTK e duração)
- 05: §3.1 (Guardian, chefe e Rift Cache), §8.4 (garantias de primeira vez), §8.5 (Uniques de chefe)
- 10: §4 (recompensa e first-kill), §5.3 (desbloqueio do Nyx), §6.1 (Heart of [Chefe] a partir de Veteran)
- 11: §3.4.2 (`BossDefinition`), §3.10 (HFSM de fases e `PatternSequencer`)
- 00 §5: R08, R10, R11, R16
- ROADMAP §3, Marco C

## Objetivo
Criar o framework reutilizável de chefes e Guardians: fases por limiar de vida, transição com invulnerabilidade, `PatternSequencer`, barra de Stagger, Broken, soft enrage, teto de adds e HUD de chefe. Sobre ele, implementar os 2 Guardians e The Ashen Colossus da Ashen Wastes. Ligar a Guardian Arena (linha 9) e a Boss Room ao fluxo de M16, com recompensas e Rift Cache, e **fechar o Marco C (Vertical Slice)**.

## Critérios de saída (verificáveis)
1. `BossDefinition` e `GuardianDefinition` são data-driven (fases, limiares, padrões, variações ✦/✧ e recompensas). O validador de dados rejeita telegraph com wind-up < 0,45 s e chefe sem soft enrage (00 §5 R14, R16).
2. Regras gerais de 09 §8.1 cobertas por testes PlayMode:
   - transição de fase com 1,5–3 s de invulnerabilidade sinalizada, sem ataques;
   - Broken de 4 s com +30% de Vulnerability, que conta no teto de +60%;
   - limiar de Broken +50% a cada Break, até 3×;
   - CC convertido em Stagger, com as exceções Frostbound e Distortion (R10);
   - imunidade a Execute, com conversão para +25% de Vulnerability abaixo de 20% de vida (R08);
   - soft enrage: a partir de 2× a duração alvo, +15% de dano a cada 30 s;
   - adds ≤ 30% do dano recebido pelo jogador na luta (instrumentado).
3. **Cinderwright Vahl** e **The Kiln Hound** (09 §7): HP ≈ 15× Standard, massa Colossal, 2 fases (troca a 50%), barra no topo e mecânica central conforme o doc. A Guardian Arena da linha 9 sorteia um deles (50/50, sem repetir em runs consecutivas). O baú tem 1 Epic+ garantido (05 §3.1: 2 itens) mais Aether Dust e Sundered Cores (10).
4. **The Ashen Colossus** (09 §8.2), na caldeira de raio 16 m com 4 respiradouros, em 3 fases:
   - fase 1: Sweeping Arm, Cinder Slam (punho preso 2,5 s com ponto fraco +25%) e Ash Rain;
   - fase 2: núcleo exposto, Molten Fissures e 2 levas de Husks com PA 8; o Broken expõe o núcleo ao alcance melee;
   - fase 3: Eruption encolhe a arena de 16 para 10 m em 3 etapas, com salto no jogador;
   - variações ✦ (respiradouros em erupção, Nightmare+) e ✧ (salto duplo, Eclipse), atrás de flags de dificuldade.
5. Duração em Normal com Kael equipado no nível esperado (perfil de referência do simulador de M14): Colossus em 3–4 min e Guardian em 30–60 s (R16). Medida em 5 lutas por bot ou playtest e registrada no Log.
6. Recompensas:
   - chefe: baú com 4 itens (1 Epic+ garantido; 05 §3.1) e Rift Cache com 2 itens (+1 se todas as Challenge Rooms da run foram concluídas);
   - Heart of [Chefe] só a partir de Veteran (10 §6.1); em Normal, nenhum;
   - pool de Uniques de chefe vazio/desativado em Normal (05 §8.5, R28);
   - first-kill por dificuldade: Legendary garantido (05 §8.4);
   - vitória → `RunEnd` Vitória (10 §4), que grava o first-kill e emite `BossFirstKill(AshenColossus)`, usado pelo desbloqueio do Nyx em M20 (10 §5.3).
7. HUD de chefe: barra de vida com marcadores de fase, barra de Stagger, indicador de Broken, nome e afixo (Guardian Nightmare+).
8. **Marco C:** `VerticalSliceSmokeTest` (PlayMode) roda uma run completa em Normal com seed fixa, linhas 1–13 + Guardian + Colossus, com bot em god mode, sem erros, e grava o profile. Uma run manual completa sem god mode é registrada no Log (tempo total dentro de 25–35 min, 09 §11.1; build usada; bugs encontrados).
9. EditMode + PlayMode passam e o validador está sem erros.

## Fora de escopo
- Chefes das regiões 2–5 e Guardians dessas regiões (M24–M27).
- Boss Echoes, Eclipse Trial e Sundering Heart (M29).
- Overrift / Delve Deeper após o chefe (M29). O portal não aparece.
- Uniques de chefe, que são conteúdo (M28). Aqui o gancho existe e a lista fica vazia.
- Arte final e cutscene de intro: há um gancho de Timeline vazio (A-trilha / M29).

## Premissas e decisões
- Os chefes usam HFSM de fases + `PatternSequencer`, com sequências autorais e padrões ponderados por fase, sem randomização total (11 §3.10). Os ataques reaproveitam `AttackDefinition`, telegraphs e hit detection de M05/M07.
- HP do chefe = duração alvo × DPS de referência da dificuldade (09 §8.1). O DPS de referência é `DPS_ref(eLvl) = HP_Std(eLvl) / 2,2 s` no `Faixa.max` (06 §3.13, R53, D02); o uptime da luta é calibrado na T6 com o simulador de M14 (perfil "Kael Normal fim de região 1").
- Guardians e chefes usam o eLvl no topo da faixa da região × dificuldade (10 §7.2).
- A arena encolhendo usa colisores de zona letal com telegraph (anéis de magma), sem destruir NavMesh: as bordas são zonas proibidas para o jogador e o chefe não precisa navegar lá.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Framework de chefe: definição, fases, sequencer, Stagger/Broken, enrage, adds | execução | executor | — | `Assets/_Project/Code/AI/Bosses/Core/*.cs`, `Assets/_Project/Code/Data/Bosses/*.cs` | ⏳ |
| T2 | HUD de chefe | execução | executor | T1 | `Assets/_Project/Code/UI/Boss/*.cs`, `Assets/_Project/UI/Boss/*` | ⏳ |
| T3 | Guardians da Ashen Wastes (Vahl e Kiln Hound) | execução | executor | T1 | `Assets/_Project/Content/Bosses/Guardians/AshenWastes/*`, `Assets/_Project/Code/AI/Bosses/Guardians/AshenWastes/*.cs`, `DataSource/bosses/guardians_ashen_wastes.csv` | ⏳ |
| T4 | The Ashen Colossus (arena, 3 fases, ✦/✧) | execução | executor | T1 | `Assets/_Project/Content/Bosses/AshenColossus/*`, `Assets/_Project/Code/AI/Bosses/AshenColossus/*.cs`, `DataSource/bosses/ashen_colossus.csv` | ⏳ |
| T5 | Integração com a run: Guardian Arena, Boss Room, recompensas, Rift Cache, first-kill | execução | executor | T1, T3, T4 | `Assets/_Project/Code/Run/RoomTypes/GuardianArenaRoom.cs`, `Assets/_Project/Code/Run/RoomTypes/BossRoom.cs`, `Assets/_Project/Code/Run/Loot/BossRewards.cs`, `Assets/_Project/Code/Run/Flow/BossEncounterProvider.cs` | ⏳ |
| T6 | Testes de regras, calibração de duração e `VerticalSliceSmokeTest` | execução | executor | T2, T5 | `Assets/_Project/Code/Tests/PlayMode/Bosses/*.cs`, `Assets/_Project/Code/Tests/PlayMode/VerticalSlice/*.cs`, `DataSource/bosses/*.csv` (só calibração de HP) | ⏳ |
| T7 | Revisão do Vertical Slice (Marco C) e documentação | integração | principal | T6 | `docs/roadmap/M17-chefes.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2 ∥ T3 ∥ T4
3. T5
4. T6
5. T7

## Briefs

### T1 — Framework de chefe
Objetivo: classes reutilizáveis para chefes e Guardians.
Classes:
- `BossDefinition` / `GuardianDefinition` (SO): fases com limiar de vida, padrões, modificadores de arena, variações ✦/✧, duração alvo, recompensas;
- `BossPhaseMachine`: HFSM de fases, com transição invulnerável de 1,5–3 s sem ataques;
- `PatternSequencer`: sequências autorais, pesos por fase e janelas de punição;
- `BossStaggerController`: massa Colossal, CC → Stagger (06), exceções Frostbound/Distortion (07, R10), Broken de 4 s com +30% de Vulnerability (R11, teto de 60% de 07), limiar +50% por Break até 3×, hit-stop global no Break (06);
- `SoftEnrageTimer`: a partir de 2× a duração alvo, +15% de dano a cada 30 s, cumulativo;
- `AddDamageLedger`: mede a fração do dano vindo de adds e alerta acima de 30%;
- `ExposedWindow`: janela scriptada sem bônus de dano (09 §8.1).
Contexto: HFSM e utility de M07; `AttackDefinition`, telegraphs, hit detection e estados de M05; Stagger e DR de CC de M03/M05 (06 §3.8); Vulnerability e status de M11.
Arquivos: escreve `Assets/_Project/Code/AI/Bosses/Core/*.cs` e `Assets/_Project/Code/Data/Bosses/*.cs` (`BossDefinition.cs`, `GuardianDefinition.cs`, `BossPhaseDefinition.cs`, `BossPatternDefinition.cs`).
Restrições: números via SO/CSV. Eventos no Event Bus: `BossPhaseChanged`, `BossBroken`, `BossEnraged`, `BossDefeated`. Execute contra chefe = conversão R08: se o pipeline de M05/M11 já faz isso, apenas use.
Aceite: um chefe de teste ("Training Colossus", 2 fases e 2 padrões) roda numa cena de teste; os eventos disparam na ordem.
Verificação: PlayMode com o chefe de teste → passa.
Entrega: no máximo 200 palavras.

### T2 — HUD de chefe
Objetivo: barra no topo com nome, vida, marcadores de fase e barra de Stagger, mais indicador de Broken (ícone e cor) e de soft enrage. Guardian: mesma barra e afixos (Nightmare+). Reage só a eventos (CLAUDE.md §4).
Arquivos: escreve `Assets/_Project/Code/UI/Boss/*.cs` e `Assets/_Project/UI/Boss/*.uxml`/`*.uss`.
Aceite: PlayMode com o chefe de teste mostra a barra, os marcadores e o Broken.
Verificação: PlayMode → passa.
Entrega: no máximo 100 palavras.

### T3 — Guardians da Ashen Wastes
Objetivo: os dois Guardians de 09 §7, com HP ≈ 15× Standard, Colossal, 2 fases (troca a 50%), sem afixos em Normal/Veteran e duração alvo de 30–60 s.
- **Cinderwright Vahl:** forja 3 armas flamejantes que orbitam e são lançadas em sequência (linhas vermelhas). Na fase 2, as armas ficam cravadas como pilares de fogo que restringem a arena.
- **The Kiln Hound:** investidas em linha Unblockable (branco-dourado, R14) que deixam rastro Burning. Na fase 2, superaquece: atacar enquanto brilha aplica Burning no atacante; há janela de punição de 3 s após resfriar.
Arenas Guardian: 2 prefabs greybox, gerados com a ferramenta de M16 (adicionar linhas ao CSV de salas é permitido só em `DataSource/rooms/ashen_wastes_guardians.csv`, arquivo novo).
Arquivos: escreve `Assets/_Project/Content/Bosses/Guardians/AshenWastes/*`, `Assets/_Project/Code/AI/Bosses/Guardians/AshenWastes/*.cs`, `DataSource/bosses/guardians_ashen_wastes.csv` e `DataSource/rooms/ashen_wastes_guardians.csv`.
Restrições: wind-up ≥ 0,45 s; letalidade de 06; telegraph na linguagem de 06.
Aceite: PlayMode com cada Guardian mostra as duas fases e a mecânica central observável.
Verificação: PlayMode → passa.
Entrega: no máximo 150 palavras.

### T4 — The Ashen Colossus
Objetivo: implementar 09 §8.2 integralmente.
- Arena: caldeira de raio 16 m com 4 respiradouros; prefab greybox em `DataSource/rooms/ashen_wastes_boss.csv`.
- Fase 1 (100–60%):
  - Sweeping Arm: cone de 120°, 9 m, 1,2 s;
  - Cinder Slam: círculo de 6 m, 1,4 s; poça Burning por 6 s; punho preso 2,5 s com ponto fraco +25%;
  - Ash Rain: 6–8 círculos de 2 m + 2 no jogador, 1,0 s.
- Fase 2 (60–25%):
  - núcleo exposto como alvo;
  - Molten Fissures: 3 linhas, 1,2 s, persistem 3 s;
  - 2 levas de Cinder Husks (PA 8) saindo dos respiradouros;
  - Broken → ajoelha e expõe o núcleo ao alcance melee;
  - ✦ respiradouro vira coluna de fogo permanente a cada ciclo.
- Fase 3 (< 25%):
  - Eruption: a arena encolhe de 16 para 10 m em 3 etapas;
  - salto no jogador: círculo de 7 m, 1,6 s, com sombra crescente;
  - ✧ salto duplo.
- Duração alvo: 3–4 min em Normal.
Arquivos: escreve `Assets/_Project/Content/Bosses/AshenColossus/*`, `Assets/_Project/Code/AI/Bosses/AshenColossus/*.cs`, `DataSource/bosses/ashen_colossus.csv` e `DataSource/rooms/ashen_wastes_boss.csv`.
Restrições: números só no CSV, citando 09 §8.2. Ponto fraco implementado como hurtbox com multiplicador local Increased (não é More). Adds respeitam o teto de 30%.
Aceite: PlayMode por fase (forçar a vida para o limiar) verifica os padrões disponíveis e o encolhimento da arena; ✦/✧ ligados por flag de dificuldade.
Verificação: PlayMode → passa.
Entrega: no máximo 200 palavras.

### T5 — Integração com a run
Objetivo: implementar `IGuardianEncounterProvider` e `IBossEncounterProvider` (definidos em M16).
- Guardian Arena da linha 9: sorteio 50/50 entre os 2 Guardians, sem repetir em runs consecutivas da mesma região; o histórico fica no profile e usa a API de M18.
- Boss Room.
- Recompensas:
  - Guardian: 2 itens, 1 Epic+;
  - chefe: 4 itens, 1 Epic+;
  - Rift Cache: 2 itens, +1 se todas as Challenge Rooms foram concluídas;
  - pisos e bônus de iLvl de 05 §3.1;
  - Heart só a partir de Veteran (10 §6.1);
  - Legendary garantido no first-kill (05 §8.4);
  - pool de Uniques de chefe com gancho, vazio em Normal (05 §8.5).
- Vitória → `RunEnd` Vitória; grava o first-kill por dificuldade e emite `BossFirstKill`.
Arquivos: escreve os 4 arquivos listados na tabela. Se precisar de campo novo no profile (histórico de Guardian, first-kill), siga a regra de save do CLAUDE.md: incremente `schemaVersion` e adicione migração, o que também escreve `Assets/_Project/Code/Save/Dto/ProfileDto.cs` e `Assets/_Project/Code/Save/Migration/*`. Declare no relatório.
Restrições: RNG `Loot` para recompensas e `Map` para o sorteio do Guardian.
Aceite: testes EditMode das tabelas de recompensa (10.000 simulações: pisos respeitados) e PlayMode do fluxo linha 9 → Guardian → … → Boss → vitória.
Verificação: EditMode + PlayMode → passam.
Entrega: no máximo 150 palavras.

### T6 — Testes, calibração e Vertical Slice smoke
Objetivo:
- testes PlayMode de cada regra do critério 2;
- calibração de HP do Colossus e dos Guardians para cumprir as durações alvo, com bot de DPS usando a build de referência de M14 (5 lutas cada, mediana dentro da faixa de R16), ajustando **só o CSV**;
- `VerticalSliceSmokeTest`: run completa com seed fixa, linhas 1–13 + Guardian + Colossus, god mode, profile gravado e carregado de volta.
Arquivos: escreve `Assets/_Project/Code/Tests/PlayMode/Bosses/*.cs` e `Assets/_Project/Code/Tests/PlayMode/VerticalSlice/*.cs`; ajusta só HP em `DataSource/bosses/*.csv`.
Aceite: todos passam; durações registradas no relatório.
Verificação: PlayMode e EditMode → passam.
Entrega: no máximo 150 palavras, com tabela de durações medidas.

### T7 — Revisão do Vertical Slice (principal)
Jogar uma run completa sem god mode e registrar no Log: tempo total, build usada, mortes, bugs e sensação (critérios de game feel de 06). Conferir os critérios 1–9 e o Marco C do ROADMAP. Atualizar o ROADMAP (§3 e §7) e o CLAUDE.md. Propor ao usuário a lista de bugs e ajustes de feel antes de seguir para a onda 11.

## Verificação do módulo
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
- **DPS de referência ausente ou instável** (M14) → calibrar com o bot e registrar; recalibrar ao fechar M20 (progressão altera a força do herói).
- **Arena que encolhe e IA** → zonas proibidas em vez de NavMesh dinâmico; adds são spawnados fora das zonas.
- **Vertical Slice revela problema de game feel** → não "consertar" fora do escopo: registrar e abrir tarefa no módulo dono (M05/M08) antes da onda 11.

## Atualizações de documentação
- `docs/ROADMAP.md` §3 (Marco C ✅ com data) e §7.
- `CLAUDE.md`: convenção de dados de chefe (`DataSource/bosses/`) e categoria de teste `Bosses`/`VerticalSlice`.

## Prompt de disparo
```
Implemente o módulo M17 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M17-chefes.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M17-chefes.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

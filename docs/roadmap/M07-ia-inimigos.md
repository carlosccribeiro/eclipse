# M07 — IA de inimigos base + 3 inimigos + soak test
Fase 1 · Onda 5 · Status: ✅ concluído (pendente: soak no hardware alvo, executado pelo usuário; desvios de TTK/letalidade para o M14)
Depende de: M05 · Desbloqueia: M14, M16 (e M17 via M16) · Paralelo com: M08 (Skills/Heroes — só usa contratos congelados do M05)
Branch: modulo/M07-ia-inimigos
Docs de referência: 09-inimigos §3 (IA, tokens, telegraph, letalidade), §4 (arquétipos), §5.1 (The Ashen Wastes); 06-combate §3.8 (massa/Stagger), §3.10 (telegraphs, wind-up), §3.13 (TTK/letalidade); 11-arquitetura §3.4.2 (`EnemyDefinition`, `DifficultyDefinition`), §3.10, §3.14; 10-progressao §7.2 (Monster Level); 00 §5 R14, R15, R17, R47

## Objetivo
Construir o framework de IA de inimigos comuns (HFSM + Utility, tokens de ataque, locomoção em job, telegraphs com a linguagem de 06), três inimigos jogáveis da Ashen Wastes e o soak test de 200 inimigos que valida o orçamento de performance de 11 §3.14 cedo.

## Critérios de saída (verificáveis)
1. HFSM `Idle → Engage → [Approach | Reposition | Attack | Recover | Flee] → Dead` com seleção de ataque por Utility (distância, ângulo, cooldown, aliados, HP) e 15% de aleatoriedade ponderada por stream `Combat` (09 §3.1, 11 §3.10).
2. Decisões a 10 Hz para inimigos perto/na tela e 4 Hz para distantes/fora da tela (09 §3.1, 11 §3.10).
3. `AttackTokenDirector` com a tabela de 09 §3.2 por dificuldade (Normal 3 melee/2 ranged … Eclipse 5/4, intervalo mínimo 1,2 → 0,8 s), Elites e Guardians sem token, inimigo sem token orbitando a 4–7 m (09 §3.2, 06 §3.10).
4. Telegraphs com a linguagem de 06 §3.10 (vermelho, roxo, branco-dourado, ciano, Annihilation; laranja nunca), preenchimento do centro para a borda, renderizando acima do VFX do jogador; wind-up ≥ piso (comum 0,6 s Normal, piso 0,45 s; redução máx. −15%) validado pelo validador de dados (R14).
5. Três inimigos da Ashen Wastes jogáveis com os ataques de 09 §5.1: **Ashborn Marauder** (melee standard), **Slag Hurler** (ranged, poça Burning) e **Kiln Brute** (tank, placa frontal −50%, slam Unblockable) — mais o **Cinder Husk** (fodder) como unidade de enxame do soak test.
6. Dano dos inimigos calibrado para a tabela de letalidade de 06 §3.13 (golpe comum 4–6% da vida máxima do jogador em Normal) e vida para os TTKs de 06 §3.13 (Medium 1,5–3 s, Heavy 4–6 s), usando a curva base de 06 §3.13 (R53, fechada pelo D02).
7. **Soak test:** 200 inimigos ativos (Husk + Marauder) em combate contra bot por 60 s; atende 11 §3.14 no hardware alvo: frame ≤ 16,6 ms, gameplay + IA ≤ 5 ms, física/queries ≤ 2 ms, GC 0 B/frame (alerta > 1 KB). Relatório `Logs/soak.csv` + resumo no Log.
8. EditMode + PlayMode verdes.

## Fora de escopo
Elites e afixos (M16), Guardians e chefes (M17), Encounter Director com Pontos de Ameaça (M15/M16), os demais 5 inimigos da Ashen Wastes e outras regiões (M16, M24–M27), Stealth de Assassin (M16), summoners/supports (M16), status completos (M11 — aqui Burning usa o `StatusStub` do M05).

## Premissas e decisões
- **Só contratos congelados do M05.** Este módulo não edita nada em `Gameplay/Combat/Contracts`, `Gameplay/Combat/*`, asmdefs de teste, `ProjectSettings/TagManager.asset` nem `CombatSandbox.unity` (que pertence ao M08 nesta onda). Precisou de algo novo no contrato → parar e pedir ao lead (risco registrado no M05).
- **Curva base de HP/dano por Monster Level — fechada pelo D02 (R53–R55):** `HP_Std = 36 × 1,048^(eLvl − 1)`; `DanoBase = 20 × 1,048^(eLvl − 1)` até 24 e `58,8 × 1,016^(eLvl − 24)` acima, × `M_let` (1,0 / 1,2 / 1,5 / 2,0); Tank HP× 2,5 (06 §3.13, 09 §4). T0 grava esses valores em `DataSource/EnemyScaling.csv` (sem `provisional`); o simulador de M14 só recalibra os coeficientes.
- `DifficultyDefinition` nasce aqui só com os campos que a IA precisa (tokens, intervalo mínimo, redução de wind-up, multiplicador de limiar de Stagger de 06 §3.8.2). O M20 amplia o mesmo SO.
- Locomoção: steering + avoidance radial em `IJobParallelFor` (Burst), NavMesh (AI Navigation) só para caminho global (11 §3.10).
- Telegraph greybox: URP Decal Projector com shader de preenchimento radial; cores por SO `TelegraphPalette`.
- Arte greybox (D4): cápsulas com silhueta distinta por arquétipo (Husk pequeno, Brute largo com placa frontal visível).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T0 | Curva de inimigos (R53) e valores dos 4 inimigos | decisão | principal | — | `DataSource/EnemyScaling.csv`, `docs/roadmap/M07-ia-inimigos.md` (Log) | ✅ |
| T1 | Dados: `EnemyDefinition`, `AttackDefinition`, `EnemyScaling`, `DifficultyDefinition` + CSV + importadores | execução | executor | T0 | `Assets/_Project/Code/Data/Enemies/*`, `Assets/_Project/Code/Data/Difficulty/*`, `DataSource/{Enemies,EnemyAttacks,Difficulty}.csv`, `Assets/_Project/Code/Editor/DataPipeline/Importers/{EnemyImporter,DifficultyImporter}.cs` | ✅ |
| T2 | HFSM + seletor Utility + LOD de decisão | execução | executor | T1 | `Assets/_Project/Code/AI/StateMachine/*`, `Assets/_Project/Code/AI/Utility/*`, `Assets/_Project/Code/AI/Lod/*`, `Assets/_Project/Code/Tests/EditMode/AI/Brain/*` | ✅ |
| T3 | `AttackTokenDirector` | execução | executor | T1 | `Assets/_Project/Code/AI/Director/*`, `Assets/_Project/Code/Tests/EditMode/AI/Tokens/*` | ✅ |
| T4 | Locomoção em job + NavMesh global | execução | executor | T1 | `Assets/_Project/Code/AI/Locomotion/*`, `Assets/_Project/Code/AI/Eclipse.AI.asmdef` | ✅ |
| T5 | Sistema de telegraph + regra do validador | execução | executor | T1 | `Assets/_Project/Code/AI/Telegraphs/*`, `Assets/_Project/Art/Shaders/TelegraphFill.shadergraph`, `Assets/_Project/Content/Shared/Telegraphs/TelegraphPalette.asset`, `Assets/_Project/Code/Editor/Validation/Rules/TelegraphWindupRule.cs` | ✅ |
| T6 | `EnemyController` e templates de arquétipo (Fodder, Melee, Ranged, Tank) | execução | executor | T2–T5 | `Assets/_Project/Code/AI/Enemies/*` | ✅ |
| T7 | Conteúdo: Cinder Husk, Ashborn Marauder, Slag Hurler, Kiln Brute | execução | executor | T6 | `Assets/_Project/Content/Enemies/AshenWastes/*` | ✅ |
| T8 | Spawner simples + cena AI Sandbox | execução | executor | T7 | `Assets/_Project/Code/AI/Spawning/*`, `Assets/_Project/Scenes/Sandbox/AISandbox.unity` | ✅ |
| T9 | Soak test (cena, bot, coleta de métricas, build de desenvolvimento) | execução | executor | T8 | `Assets/_Project/Code/AI/Diagnostics/*`, `Assets/_Project/Scenes/Perf/SoakTest.unity`, `Assets/_Project/Code/Editor/Build/SoakBuild.cs`, `Assets/_Project/Code/Tests/PlayMode/AI/Soak/*` | ✅ |
| T10 | Testes PlayMode de comportamento | execução | executor | T8 | `Assets/_Project/Code/Tests/PlayMode/AI/Behavior/*` | ✅ |
| T11 | Revisão final e integração | integração | principal | T0–T10 | `docs/roadmap/M07-ia-inimigos.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ✅ |

## Ondas internas
1. T0
2. T1
3. T2 ∥ T3 ∥ T4 ∥ T5 (pastas disjuntas)
4. T6
5. T7
6. T8
7. T9 ∥ T10
8. T11

## Briefs

**T0 — Curva de inimigos (R53) e valores dos 4 inimigos (principal)**
- Objetivo: gravar a curva de 06 §3.13 (R53) e os valores dos 4 inimigos para que a calibragem seja testável.
- Contexto: curva descrita nas Premissas (D02). Stats relativos dos arquétipos em 09 §4 (Fodder 0,3/0,6, Standard 1,0/1,0, Ranged 0,8/1,0, Tank 2,5/1,4, R55); inimigos em 09 §5.1. Curva: 06 §3.13 (R53). Vida de referência e DPS de referência do jogador: 01 (exemplo numérico) e 02 (Weapon Power). TTK alvo: Light 0,3–0,8 s, Medium 1,5–3 s, Heavy 4–6 s; letalidade Normal: golpe comum 4–6% (06 §3.13).
- Arquivos: escreve `DataSource/EnemyScaling.csv` (por Monster Level 1–110 e tier, colunas `level, tier, std_hp, std_damage`; Ascension pela regra de 06 §3.13) e a origem (R53) no Log.
- Aceite: com os números escolhidos, Marauder (Standard) leva 1,5–3 s de TTK e causa 4–6% por golpe ao Kael nível 1 em Normal com gear recomendado (conta mostrada no Log).
- Verificação: planilha/CSV revisados; teste numérico em T10.

**T1 — Dados e importadores**
- Objetivo: definir os SOs de inimigo/ataque/dificuldade e importá-los de CSV.
- Contexto: campos de `EnemyDefinition` em 11 §3.4.2 (arquétipo, stats relativos × curva, PA, massa, barra de Stagger, `AttackDefinition[]` com scores de utility e tipo de telegraph, template de HFSM, resistências, classe de loot, tags, range preferido min/max e comportamento de reposicionamento — 09 §3.1). `AttackDefinition`: cooldown, alcance, peso, condição, telegraph (tipo de 06 §3.10 + forma: círculo/cone/linha/anel — 09 §3.3), wind-up (s, em Normal), frames ativos/recovery, dano relativo, Stagger, HitFlags (`Unblockable`, `Unavoidable`, `Knockdown`), custo de token. `DifficultyDefinition` (subset): tokens melee/ranged, intervalo mínimo, redução máx. de wind-up (−15%), multiplicador de limiar de Stagger (06 §3.8.2), multiplicadores de vida/dano (placeholder 1,0 até M20). Tabela de tokens de 09 §3.2.
- Arquivos: os listados na tabela; importadores descobertos pelo pipeline do M02 sem editar arquivo central.
- Restrições: `StableId` `enemy.ashen.marauder`, `attack.enemy.marauder.combo1` etc.; nenhum número em código.
- Aceite: import gera assets; teste EditMode confere tokens por dificuldade e um inimigo completo.
- Verificação: import/validação (CLAUDE.md §6) sem erros; EditMode `-testFilter "Eclipse.Tests.EditMode.Data.Enemies"` → verde.
- Entrega: ≤ 150 palavras.

**T2 — HFSM + Utility + LOD**
- Objetivo: cérebro de inimigo comum reutilizável por todos os arquétipos.
- Contexto: estados de 11 §3.10; Utility pontua ações disponíveis (distância, ângulo, cooldown, aliados próximos, HP próprio) e escolhe a de maior utilidade com 15% de aleatoriedade ponderada (09 §3.1) usando o stream `Combat` do M01 (nunca `UnityEngine.Random`). LOD: 10 Hz perto/na tela, 4 Hz distante/fora (09 §3.1). Sem Behavior Trees (11 decisão 9).
- Arquivos: `AI/StateMachine/*`, `AI/Utility/*`, `AI/Lod/*` e testes EditMode em `Tests/EditMode/AI/Brain/`.
- Restrições: lógica de decisão em classes C# sem `MonoBehaviour` (testável em EditMode); 0 alocação por decisão.
- Aceite: testes com seed fixa reproduzem a mesma sequência de escolhas; distribuição de 10.000 decisões com aleatoriedade dentro de ±2% de 15%; LOD troca de 10 para 4 Hz ao sair do raio.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.AI.Brain"` → verde.
- Entrega: ≤ 150 palavras.

**T3 — `AttackTokenDirector`**
- Objetivo: limitar atacantes simultâneos conforme 09 §3.2.
- Contexto: tokens por dificuldade lidos de `DifficultyDefinition`; Elites e Guardians não consomem token, mas ataques fortes respeitam o intervalo mínimo; chefes ignoram; Assassin token de emboscada (1 em Normal/Veteran, 2 em Nightmare+); sem token → orbita 4–7 m, reposiciona ou faz ação de baixo custo; inimigo sob Taunt ignora tokens (06 §3.10).
- Arquivos: `AI/Director/{AttackTokenDirector,TokenTicket,TokenPolicy}.cs` e testes EditMode.
- Aceite: com 10 melee em Normal, nunca mais de 3 atacando no mesmo tick; Elite ataca sem token; intervalo mínimo por inimigo respeitado.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.AI.Tokens"` → verde.
- Entrega: ≤ 120 palavras.

**T4 — Locomoção em job + NavMesh global**
- Objetivo: mover centenas de inimigos dentro do orçamento.
- Contexto: steering (seek/arrive/orbit/flee) + avoidance radial em `IJobParallelFor` com Burst; NavMesh (pacote AI Navigation) só para caminho global por sala; velocidades de 09 §4 (Fodder 5,0, Standard 4,2, Ranged 4,0, Tank 3,0 m/s; nenhum mais rápido que 5,8 m/s fora de investida — 09 §4/R50). Animação de inimigos distantes em taxa reduzida (11 §3.14).
- Arquivos: `AI/Locomotion/*`; adiciona referências de pacote em `Assets/_Project/Code/AI/Eclipse.AI.asmdef` (único asmdef que este módulo edita).
- Restrições: `NativeArray` persistentes; 0 GC/frame.
- Aceite: 200 agentes movendo-se em sala de 40 × 40 m sem sobreposição persistente (> 0,5 s) e sem atravessar `Wall`.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.AI.Behavior.Locomotion"` (T10) → verde.
- Entrega: ≤ 120 palavras.

**T5 — Sistema de telegraph + regra do validador**
- Objetivo: telegraphs legíveis e validados.
- Contexto: linguagem de 06 §3.10 — vermelho (padrão; i-frames, Parry melee e Block funcionam), roxo (controle), branco-dourado + brilho na arma (Unblockable), anel ciano no inimigo (janela interrompível: Stagger/CC na janela cancela e aplica Staggered imediato), Annihilation (vermelho com borda amarela tracejada ≥ 1,5 s), Unstoppable (contorno branco pulsante), linha de sniper (≥ 0,8 s), agarrão, indicador de borda para fora da tela; laranja reservado a Ember. Preenchimento do centro para a borda, impacto quando toca a borda (09 §3.3). Telegraphs e projéteis inimigos renderizam acima de todo VFX do jogador (06 §3.10). Wind-up: comum 0,6 s em Normal (piso 0,45), Elite 0,8 (0,55), chefe 1,0–1,5 (0,8); redução máx. −15% por dificuldade.
- Arquivos: os listados; a regra do validador é descoberta pelo `DataValidator` do M02 (sem editar o validador central).
- Aceite: validador rejeita `AttackDefinition` com wind-up efetivo abaixo do piso em qualquer dificuldade; teste visual manual descrito no Log; teste PlayMode confirma que o impacto ocorre no frame em que o preenchimento chega a 100%.
- Verificação: import/validação com um CSV de teste inválido → erro esperado; EditMode `-testFilter "Eclipse.Tests.EditMode.AI.Telegraphs"` → verde.
- Entrega: ≤ 150 palavras.

**T6 — `EnemyController` e templates de arquétipo**
- Objetivo: juntar cérebro, locomoção, tokens, telegraph e os componentes de combate do M05.
- Contexto: entidade usa `CombatEntity`, `StateComponent`, `StaggerComponent` (massa e limiares de 06 §3.8) e `AttackController`/hitboxes/projéteis/áreas do M05 via contratos. Templates: Fodder (aproxima em enxame, lunge curto), Melee (aproxima com desvio para cercar; combo 2–3 golpes), Ranged (mantém 8–14 m, recua se < 5 m com CD 4 s, busca LOS, não atira através de Tank aliado), Tank (avança lento, escolta casters; proteção frontal −50%) (09 §4). Inimigos fora da tela não disparam projéteis (06 §3.10). Projéteis inimigos 14 m/s em Normal (06 §3.9).
- Arquivos: `AI/Enemies/{EnemyController,EnemyBrainBinder,ArchetypeTemplates,FrontalGuard}.cs`.
- Restrições: sem editar arquivos do M05; pooling via `PoolService`.
- Aceite: cada template executa seu ciclo completo na AI Sandbox (T8) sem erros.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.AI.Behavior"` (T10) → verde.
- Entrega: ≤ 150 palavras.

**T7 — Conteúdo: 4 inimigos da Ashen Wastes**
- Objetivo: dados + prefabs greybox dos inimigos de 09 §5.1.
- Contexto: **Cinder Husk** (fodder · Ember · 0,3/0,6 · PA 1; lunge-mordida círculo 1 m 0,6 s; 25% de faísca ao morrer círculo 1,5 m 0,6 s); **Ashborn Marauder** (standard · Physical · 1,0/1,0 · PA 3; combo de 2 golpes + overhead linha 3 m 0,7 s); **Slag Hurler** (ranged · Ember · 0,8/1,0 · PA 4; escória círculo 2,5 m no destino 1,0 s, poça Burning 3 s — Burning via `StatusStub`; recua se < 5 m); **Kiln Brute** (tank · Physical/Ember · 2,5/1,4 · PA 8; placa frontal −50%; slam círculo 4 m 1,1 s Unblockable; 2 s de recuperação após o slam). Massa: Husk Light, Marauder/Hurler Medium, Brute Heavy (09 §4).
- Arquivos: `Assets/_Project/Content/Enemies/AshenWastes/<Inimigo>/` (prefab, `EnemyDefinition`, ataques); linhas nos CSV do T1.
- Aceite: os quatro jogáveis na AI Sandbox; números conferidos por teste (T10).
- Verificação: import/validação sem erros.
- Entrega: ≤ 120 palavras.

**T8 — Spawner simples + AI Sandbox**
- Objetivo: cena própria para testar IA sem tocar na `CombatSandbox` (dona: M08 nesta onda).
- Contexto: spawner por ondas com composição fixa definida em SO de teste (o Encounter Director real é M15/M16), pooling, teto de unidades vivas configurável; jogador greybox do M04/M05 com combate básico (sem habilidades); console simples de spawn por tecla de debug (o console completo é M06).
- Arquivos: `AI/Spawning/{SimpleWaveSpawner,SpawnPoint,TestWaveDefinition}.cs`, `Scenes/Sandbox/AISandbox.unity`.
- Aceite: Play na AI Sandbox spawna os 4 inimigos e eles lutam contra o jogador sem erros.
- Verificação: PlayMode "cena carrega e spawna 1 de cada" → verde.
- Entrega: ≤ 100 palavras.

**T9 — Soak test**
- Objetivo: medir o orçamento de 11 §3.14 com 200 inimigos.
- Contexto: cena `SoakTest` com 200 inimigos ativos (150 Husk + 50 Marauder) contra bot que anda em círculo, ataca e esquiva; 60 s; coleta por `ProfilerRecorder`: frame time (média, p95, p99), main thread de gameplay+IA (marcadores próprios), física/queries, GC Allocated In Frame. Orçamento: frame ≤ 16,6 ms, gameplay+IA ≤ 5 ms, física ≤ 2 ms, GC 0 B/frame (alerta > 1 KB). Dois modos: (a) PlayMode test no Editor (CI) só para GC e contagem de entidades; (b) build de desenvolvimento Windows gerada por `-executeMethod Eclipse.Editor.Build.SoakBuild.Build` que roda sozinha e grava `Logs/soak.csv`, executada pelo usuário no hardware alvo (mín.: GTX 1060 / i5-8400 — 11 §3.14).
- Arquivos: `AI/Diagnostics/{SoakTestRunner,SoakBot,PerfSampler}.cs`, `Scenes/Perf/SoakTest.unity`, `Editor/Build/SoakBuild.cs`, `Tests/PlayMode/AI/Soak/*`.
- Aceite: PlayMode test verde (GC 0 B/frame após warm-up); build gera CSV; resultados no Log com hardware usado. Se o hardware do usuário não for o alvo, registrar a diferença.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.AI.Soak"` → verde; `"$UNITY_EXE" -batchmode -projectPath . -executeMethod Eclipse.Editor.Build.SoakBuild.Build -quit -logFile Logs/soakbuild.log` → build em `Builds/Soak/`.
- Entrega: ≤ 150 palavras + tabela de métricas.

**T10 — Testes PlayMode de comportamento**
- Objetivo: provar critérios 1–6.
- Contexto: casos: Marauder aproxima e ataca só com token; 10 melee em Normal → ≤ 3 atacando; Hurler recua quando o jogador entra a < 5 m e não atira fora da tela; Brute recebe −50% de frente e 100% pelas costas; slam do Brute ignora Block (Unblockable) mas é negado por i-frames; telegraph do overhead do Marauder dura 0,7 s e o hit acontece ao completar; dano do combo do Marauder = 4–6% da vida de Kael nível 1 em Normal; TTK do Marauder 1,5–3 s e do Brute 4–6 s com o combo do Greatsword (M05); locomoção de 200 agentes (T4).
- Arquivos: `Tests/PlayMode/AI/Behavior/*`.
- Aceite: todos verdes.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.AI"` → 0 falhas.
- Entrega: ≤ 150 palavras.

**T11 — Revisão final e integração (principal)**
- Objetivo: conferir critérios, registrar métricas do soak e o TTK/letalidade medidos contra a curva de R53.
- Arquivos: este arquivo, `docs/ROADMAP.md` §7, `CLAUDE.md` (pastas `AI/*`, comando de soak build, pegadinhas de Burst/NavMesh).
- Aceite: critérios 1–8 ✅ com saídas no Log; curva de R53 gravada; desvios de TTK/letalidade medidos no soak registrados para o M14.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.AI" -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -executeMethod Eclipse.Editor.Build.SoakBuild.Build -quit -logFile Logs/soakbuild.log
```
→ validador sem erros; 0 falhas; build de soak gerada; `Logs/soak.csv` dentro do orçamento no hardware alvo (execução manual do usuário).

## Riscos e rollback
- **Orçamento estourado no soak** → é exatamente o que este módulo deve revelar cedo: priorizar (1) LOD de IA e animação, (2) agrupamento de queries, (3) instancing dos greybox; se ainda estourar, registrar números e abrir tarefa de performance antes do M16 (não esperar o M30).
- Contrato do M05 insuficiente → parar e pedir ao lead; não editar `Contracts` em paralelo com o M08.
- Curva de R53 diverge do soak → registrar no Log; o M14 recalibra os coeficientes `36`/`20` (a forma da curva é de 06).
- Decal Projector caro com muitos telegraphs → fallback para quad no chão com shader próprio, mesma API.

## Atualizações de documentação
- `CLAUDE.md`: pastas `AI/*`, `Scenes/Perf`, comando de soak build, regra "IA nunca usa `UnityEngine.Random`".
- `docs/ROADMAP.md` §7: status do M07; pendência "curva base de HP/dano por Monster Level (06/10)".

## Prompt de disparo
```
Implemente o módulo M07 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M07-ia-inimigos.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M07-ia-inimigos.
Este módulo roda em paralelo com o M08: não edite Gameplay/Combat/Contracts, asmdefs de teste nem CombatSandbox.unity.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.
- 2026-11-08 Branch `modulo/M07-ia-inimigos` criada de main (d1c7d2b); plano aprovado pelo usuário; executors em sequência no checkout.
- 2026-11-08 T0 ✅ `DataSource/EnemyScaling.csv` (níveis 1–110) gerado da curva R53 (06 §3.13, D02); confere com a tabela do doc (eLvl 1/25/50/76). Ascension (85–110): eLvl_Eclipse=84 (pior caso) e N = nível − 84, HP ×1,035^N e dano ×1,02^N (a sala real pode usar eLvl_Eclipse menor; refinar no M20/M14). `std_damage` já inclui M_let do tier. Conta Marauder (Normal, nível 1): HP 36 × 1,0 = 36; DPS_ref 16,4 → TTK 2,2 s (alvo 1,5–3 s). Dano 20 × 1,0 = 20; com 30% de mitigação = 14 de 288 de vida de referência de Kael = 4,9% (alvo 4–6%) — premissas em docs/roadmap/reports/D02-calculos.md §3.3.
- 2026-11-08 T1 ✅ (dcf8e6d) `EnemyDefinition`/`AttackDefinition`/`DifficultyDefinition`/`EnemyScalingDefinition` + CSV `Enemies|EnemyAttacks|Difficulty` + importadores; namespace `Eclipse.Data.Difficulties` (colidia com o enum `Difficulty` do Loot). Lacunas: resistências e `status_res` em 0, `condition` texto livre, `front_plate_arc_deg` 120, `max_windup_reduction` 15% em todos os tiers; números sem doc marcados `provisional`.
- 2026-11-08 T2 ✅ (eaad40e) HFSM + Utility (15% de aleatoriedade, stream `Combat`) + LOD 10/4 Hz; 18 testes. T3 ✅ (6f9364d) `AttackTokenDirector` (implementa `ITokenGate`). **Pendências do T3:** bônus de tokens de Ascension (+1 melee em A40, +1 ranged em A70, piso 0,7 s) não existe em `DifficultyDefinition` (M20); intervalo mínimo vale para todo `TryAcquire` de Elite/Guardian/Taunt (a interface não distingue ataque forte); limite de emboscada é por portadores simultâneos, não por encontro.
- 2026-11-08 T4 ✅ (041493c, fix 14d00e6) locomoção Burst: steering + avoidance O(n²) + `SeparationJob` (resolução posicional; sem ele o seek mantinha pares sobrepostos). Acceleration, pesos de steering e raios de LOD são **TUNED**. T5 ✅ (832e32f) telegraph: `TelegraphTimeline`/`WindupMath`/paleta sem laranja/shader HLSL em quad (não Decal) + `TelegraphWindupRule`. **Teste visual manual pendente (usuário):** criar um `TelegraphViewPool`, `Rent`+`Show` (círculo, cone, linha, anel), `SetProgress` 0→1 (preenchimento do centro para a borda, acima do VFX do jogador, Annihilation com borda amarela tracejada). Borda de tela, agarrão e brilho na arma são só entradas da paleta.
- 2026-11-08 T6 ✅ (ee69799) `EnemyController` + templates; placa frontal via `Hurtbox.Bind` (sem editar Contracts). T7 ✅ (44c6a4d) prefabs greybox + `EnemyPrefabCreator`. T8 ✅ (34bfb11) spawner + `AiSandboxBootstrap` + `AISandbox.unity` (teclas 1–4 inimigos, 5 onda; asmdef novo `Eclipse.AI.Sandbox` guarda as teclas porque `Eclipse.AI` não referencia o Input System).
- 2026-11-08 **Lacunas do M05 (sem editar Contracts):** Armor% do inimigo fica em 0 e resistências por tipo não chegam ao dano recebido; a dificuldade do Stagger vem de `CombatRuntimeConfig.difficulty`, não de `DifficultyDefinition`; `CombatActor` não tem Reset (o pool revive com `Death.ResetBossRoom` + `TryRevive`, publicando `OnRevive`); inimigos inativos no pool continuam registrados no `CombatWorld`/scheduler (pesa no soak — avaliar no M16).
- 2026-11-08 T9 ✅ (51b2822) soak 200 (150 Husk + 50 Marauder), 60 s, build de desenvolvimento `Eclipse.Editor.Build.SoakBuild.Build` → `Builds/Soak/` (gitignorada), roda sozinha e grava `Logs/soak.csv` (o player grava em `Builds/Soak/Logs/`). **Hardware desta medição (não é o alvo):** Ryzen 7 5700X3D, RTX 4070 SUPER, 32 GB. Resultado: frame média 16,81 ms / p95 16,94 / p99 19,89 / máx 48,6 (média presa ao refresh de 60 Hz: 3571 frames em 60 s); gameplay+IA 2,57 / 4,17 / 6,82 ms (ok até p95; ticks de IA ≈2,0 ms, locomoção ≈0,12 ms antes do fix de separação); física 0,38 / 0,69 / 3,90 ms (p99 > 2 ms); **GC não é 0:** média 89 B, p99 2004 B, 288 frames com alocação, 39 acima de 1 KB. **Pendente:** causa da alocação (investigar com Profiler no alvo) e medir na GTX 1060 / i5-8400.
- 2026-11-08 T10 ✅ (6f69495, e38ff06) PlayMode `Eclipse.Tests.PlayMode.AI`: 17/17 em duas execuções seguidas (cópia scratch); EditMode 1012/1012. Bug de produção achado e corrigido: `EnemyServices` aplicava a redução máxima de 15% de wind-up em Normal (overhead 0,595 s em vez de 0,7 s). A suíte falhava com a `AISandbox.unity` suja no working tree (segundo `PlayerMotor`, mudança do Editor aberto, não commitada). `Ten_melee`: 3 a 12 inícios de ataque por 30 s, janela ampliada para 90 s com piso 5. **Desvios para o M14 (medidos, não mascarados):** letalidade do combo do Marauder 7,05% do Kael do sandbox (240 HP, Armor 100) contra 4,86% na referência D02 (288 HP, 30% mitigação) — alvo 4–6%; TTK do Marauder 2,82 s (ok, 1,5–3 s) com DPS 13,5 contra 16,4 de referência; TTK do Brute 17,3 s de frente (alvo 4–6 s) com DPS 5,3 — o dano do jogador no sandbox não corresponde ao DPS_ref, e a placa frontal −50% pesa de frente.
- 2026-11-08 T11 ✅ Critérios: 1–5 ✅ (testes); 6 ⚠ curva R53 gravada e usada, mas letalidade/TTK medidos no sandbox fora do alvo (ver T10); 7 ⚠ soak medido fora do hardware alvo, GC ≠ 0 e p99 de física > 2 ms (ver T9); 8 ✅. Pendências do usuário: teste visual do telegraph, soak no hardware alvo, decidir sobre os arquivos soltos no working tree (Greybox `.mat`, `AISandbox.unity`, `AddressableAssetsData/Windows*` e `ProfileDataSourceSettings*`, regravados pelo Editor/build).

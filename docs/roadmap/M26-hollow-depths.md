# M26 — Região 4: The Hollow Depths + The Devouring Presence
Fase 5 · Onda 14 · Status: ⏳ não iniciado
Depende de: M25 · Desbloqueia: M27 · Paralelo com: M23
Branch: modulo/M26-hollow-depths
Docs de referência: 09 §5.4 (bestiário), §3 (IA, tokens, telegraphs, letalidade), §6 (afixos de Elite), §7 (Guardians), §8.1 e §8.5 (The Devouring Presence), §9–§11 (salas, orçamento, mapa), §12 (Shrines), §14 (eventos); 05 §4.1, §8.5, §9.2, §9.3; 02 §3.12.6; 10 (eLvl, Heart, desbloqueio de região); 06 (telegraphs, letalidade, TTK, CC em chefe); 07 (Pull, Implosion, Corruption, Decay (máx. 5 também em inimigos, R49), Vulnerability (teto +60%), Detonation, Distortion (R10)); 00 §5 (R08, R10, R11, R14–R16, R33, R35, R36, R48, R49); CLAUDE.md

> Molde comum de região: M24–M27 têm a mesma estrutura de tarefas; muda só o conteúdo. Ao iniciar, leia o Log do módulo de região anterior para herdar decisões.

## Objetivo
The Hollow Depths jogável de ponta a ponta como região 4: bestiário de 8 inimigos, mecânicas regionais, salas autorais, eventos e Shrines com pesos regionais, 2 Guardians e o chefe The Devouring Presence com fases e variações ✦ (Nightmare) e ✧ (Eclipse), recompensas e desbloqueio. Void / Ruin · Hollow. Escuridão, corrupção e puxões. Ensina gestão de espaço e de debuffs (Decay, Vulnerability).

## Critérios de saída (verificáveis)
1. 8 inimigos de 09 §5.4 com IA, ataque assinatura, telegraph na linguagem de 06 (wind-up ≥ 0,45 s) e counterplay testado.
2. Mecânicas regionais funcionando com teste: zonas de escuridão e de luz da arena (Umbral Stalker invisível na escuridão; habilidades de luz/Ember revelam); inimigos que se enterram e emergem; inimigos estáticos enraizados; ovos destrutíveis que eclodem; poças de Decay; Pull seguido de Implosion; consumo de cadáveres (cura ou Totem de Decay); absorção frontal de projéteis.
3. Salas autorais no mínimo da tabela de T6, NavMesh válido, grupo de Addressables `Region_HollowDepths`; 50 seeds sem sala faltante.
4. 2 Guardians (Mother of Maws (colmeia estática; bocas emergem pela arena; fase 2 Pull constante para o centro) e The Rotting Herald (Detonation ao atingir 5 Decay no jogador, R49; fase 2 totens que removem 2 stacks ao serem destruídos)) com 2 fases e 30–60 s.
5. The Devouring Presence com todas as fases, ✦ e ✧, Broken (R11), CC → Stagger/exceções (R10), soft enrage e duração alvo 4 min em Normal (± 20% pelo bot).
6. Run completa (13 linhas + chefe) gerada por seed, jogável pelo bot em Normal; Nightmare e Eclipse via debug sem erro; duração alvo 25–35 min.
7. Uniques: U13 (pool de região, universal) autorados aqui; pool do chefe/região completo após merge de M23 na base (O5, O13).
8. iLvl/eLvl por dificuldade (05 §4.1, 10), Heart of the Devouring Presence creditado, desbloqueio após The Fractured One.
9. Stress: sala Lightless com Brood Mother (ovos → Gnawlings) + 2 Umbral Stalkers + Rot Priest + Elite Gravitic a 60 fps no hardware mínimo, 0 B de GC por frame em combate.
10. Validador sem erros; testes do módulo verdes; Status/Log e `docs/ROADMAP.md` atualizados.

## Fora de escopo
- Heróis, modificadores, sets e itens com prefixo de herói (trilha de heróis / M28).
- Conteúdo de endgame da região (Nightmare Behaviors, Eclipse Phenomena, Boss Echoes): M29.
- Arte final de ambiente/inimigos (A00); aqui só greybox + gratuitos (D4).

## Premissas e decisões
- Frameworks de M07 (IA), M15/M16 (geração e fluxo de run, salas, eventos, Shrines), M17 (chefes/Guardians), M19 (economia, Hearts) e M14 (bot, Run Map Previewer) estão ✅ na base. *Refinar ao iniciar* (T1 confirma).
- Região-lar dos sets (Void Oracle (Lyra) e Hollow Sovereign (Orun)) é coluna do set (`homeRegion`, autorada pela trilha de heróis); a região não lista sets.
- Uniques declaram `fonteUnique`; a região só garante que o chefe/região existe como fonte. Dependência com a trilha de heróis: merge de M23 na base (O5, O13) — por isso T10 é a última tarefa de dados e o módulo só fecha após esse merge.
- Ecos de inimigos anteriores: usa até 20% do orçamento de PA com ecos de inimigos das regiões 1–3.

### Partição de arquivos (escreve) — evita colisão com M23
- `DataSource/regions/hollow_depths/**`, `DataSource/items/named/universal_hollow_depths.csv`, `DataSource/localization/{region_,items_universal_}hollow_depths.csv`
- `Assets/_Project/Code/AI/Enemies/HollowDepths/**`, `Assets/_Project/Code/AI/Bosses/{DevouringPresence,Guardians/HollowDepths}/**`, `Assets/_Project/Code/Run/Mechanics/{HollowDepths,Common}/**`
- `Assets/_Project/Content/{Regions,Enemies}/HollowDepths/**`, `Assets/_Project/Content/Bosses/{DevouringPresence,Guardians/HollowDepths}/**`, banco FMOD `Region_HollowDepths`, grupo de Addressables `Region_HollowDepths`
- `Assets/_Project/Code/Tests/{EditMode,PlayMode}/Regions/HollowDepths/**`
- `DataSource/economy/materials.csv` só se o Heart ainda não existir (T9).
- **Não escreve:** `DataSource/{heroes,sets,modifiers}/**`, `DataSource/items/named/{kael,lyra,nyx,orun,universal}.csv`, `Assets/_Project/Code/Gameplay/{Skills,Status,Heroes}/**`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Levantar o molde de região já implementado | investigação | investigador | — | — (relatório no Log) | ⏳ |
| T2 | Decidir roster de salas e mecânicas novas | decisão | principal | T1 | este arquivo (Premissas, Log) | ⏳ |
| T3 | Dados da região em CSV | execução | executor | T2 | `DataSource/regions/hollow_depths/` | ⏳ |
| T4 | Inimigos 1–4: Gnawling, Hollow Thrall, Spitting Maw, Void Whisperer | execução | executor | T3 | Code/AI/Enemies/HollowDepths/*, Content/Enemies/HollowDepths/*, testes | ⏳ |
| T5 | Inimigos 5–8 e mecânicas regionais: Abyssal Carapace, Brood Mother, Umbral Stalker, Rot Priest | execução | executor | T3 | Code/AI/Enemies/HollowDepths/*, Code/Run/Mechanics/HollowDepths/*, Content/Enemies/HollowDepths/*, testes | ⏳ |
| T6 | Salas greybox, NavMesh e grupo de Addressables | execução | executor | T2 | Content/Regions/HollowDepths/Rooms/*, Addressables group Region_HollowDepths | ⏳ |
| T7 | Guardians da região | execução | executor | T4, T5, T6 | Code/AI/Bosses/Guardians/HollowDepths/*, Content/Bosses/Guardians/HollowDepths/*, testes | ⏳ |
| T8 | Chefe The Devouring Presence — parte 1 | execução | executor | T6 | Code/AI/Bosses/DevouringPresence/*, Content/Bosses/DevouringPresence/*, testes | ⏳ |
| T9 | Chefe The Devouring Presence — parte 2 e recompensas | execução | executor | T8 | Code/AI/Bosses/DevouringPresence/*, Content/Bosses/DevouringPresence/*, testes | ⏳ |
| T10 | Uniques universais da região e conferência do pool do chefe | execução | executor | T9 + merge de M23 na base (O5, O13) | `DataSource/items/named/universal_hollow_depths.csv` | ⏳ |
| T11 | Eventos, Shrines, ambientação e áudio placeholder | execução | executor | T3, T6 | Content/Regions/HollowDepths/{Events,Ambience}/*, banco FMOD da região | ⏳ |
| T12 | Playtest da região e ajustes por dados | revisão | principal | T7, T9, T11 | CSV da região (ajustes), este arquivo (Log) | ⏳ |
| T13 | Integração: desbloqueio, run completa e docs | execução | executor | T10, T12 | testes de run, docs | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T6 (CSV × prefabs de sala)
4. T4 ∥ T5 ∥ T8 (um arquivo por inimigo; chefe em pasta própria)
5. T7 ∥ T9 ∥ T11
6. T12
7. T10 (após o merge do herói paralelo, quando houver)
8. T13

## Briefs
### T1 — Levantar o molde de região já implementado

- **Objetivo:** Mapear como The Ashen Wastes (M16/M17) e as regiões anteriores foram implementadas — CSV de região, prefabs de inimigo, salas, encounter recipes, eventos, Shrines, Guardian, chefe, recompensas, eLvl/iLvl, desbloqueio no Rift Navigator — e listar o que The Hollow Depths reaproveita e o que exige mecânica nova.
- **Contexto:** Mecânicas novas candidatas: zonas de escuridão e de luz da arena (Umbral Stalker invisível na escuridão; habilidades de luz/Ember revelam); inimigos que se enterram e emergem; inimigos estáticos enraizados; ovos destrutíveis que eclodem; poças de Decay; Pull seguido de Implosion; consumo de cadáveres (cura ou Totem de Decay); absorção frontal de projéteis.
- **Arquivos:** lê CLAUDE.md, este arquivo, docs/roadmap/M16-fluxo-run.md, M17-chefes.md e módulos de região anteriores (Log), `DataSource/regions/`, `Assets/_Project/Code/AI/`, `Assets/_Project/Content/Regions/`; escreve nada (somente relatório) (não toque em outros).
- **Restrições:** Somente leitura.
- **Aceite:** Checklist do molde (arquivo:linha) + lista de mecânicas novas com proposta de onde implementar (genérica em AI/Run × específica da região).
- **Verificação:** relatório com evidências; o principal registra no Log
- **Entrega:** no máximo 300 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T2 — Decidir roster de salas e mecânicas novas

- **Objetivo:** Fixar a lista de salas autorais por tipo (mínimo da tabela abaixo), quais mecânicas novas viram componentes genéricos (reutilizáveis por M25–M27 e pelo endgame) e confirmar a partição de arquivos com o par da onda.
- **Contexto:** Par paralelo: M23. A trilha de regiões não escreve em `Gameplay/Skills` nem `Gameplay/Status`; se precisar de algo lá, registra e pede ao principal do par.
- **Arquivos:** lê relatório de T1, docs/roadmap/M23-*.md, 09 §9–§11; escreve este arquivo (não toque em outros).
- **Restrições:** Mudança de número/regra de design → perguntar ao usuário e registrar em 00 §5.
- **Aceite:** Premissas atualizadas; tarefas ajustadas.
- **Verificação:** revisão do diff deste arquivo
- **Entrega:** no máximo 150 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T3 — Dados da região em CSV

- **Objetivo:** Autorar os dados de The Hollow Depths: 8 inimigos (arquétipo, facção, afinidade, HP×, Dano×, PA de 09 §5.4), pesos de afixos de Elite com peso regional (09 §6.3), encounter recipes e orçamento de ameaça (09 §10), lista de salas, pesos de eventos e Shrines (09 §12, §14), Guardians, chefe, perfil de loot (afinidade dominante: Void e Ruin (peso ×2, 05 §9.2)), faixas de iLvl/eLvl por dificuldade (05 §4.1, 10) e condição de desbloqueio (derrota do The Fractured One).
- **Contexto:** Void / Ruin · Hollow. Escuridão, corrupção e puxões. Ensina gestão de espaço e de debuffs (Decay, Vulnerability). Região-lar é declarada nos sets (coluna `homeRegion`, trilha de heróis) — a região não lista sets. Ecos: usa até 20% do orçamento de PA com ecos de inimigos das regiões 1–3.
- **Arquivos:** lê 09 §5.4, §6, §9–§14; 05 §4.1, §9.2; 10 (eLvl, desbloqueio de região); estrutura de CSV de M16; escreve `DataSource/regions/hollow_depths/` e `DataSource/localization/region_hollow_depths.csv` (não toque em outros).
- **Restrições:** Não escrever arquivos de herói, `DataSource/sets/**` nem `DataSource/modifiers/**`.
- **Aceite:** Importação sem erros; 8 inimigos, 2 Guardians, 1 chefe, ≥ 4 encounter recipes regionais.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T4 — Inimigos 1–4: Gnawling, Hollow Thrall, Spitting Maw, Void Whisperer

- **Objetivo:** Implementar IA (HFSM + Utility de M07), ataques assinatura com telegraph, counterplay e prefabs greybox dos 4 primeiros inimigos do bestiário.
- **Contexto:** Telegraphs na linguagem de 06 (vermelho evitável, roxo controle, branco-dourado Unblockable, ciano interrompível; laranja é Ember, não telegraph); wind-up ≥ 0,45 s; letalidade de 06 (R14, R15); tokens de ataque de 09 §3.2.
- **Arquivos:** lê 09 §3, §4, §5.4; 06 (telegraphs, letalidade, massa); 07 (Pull, Implosion, Corruption, Decay (máx. 5 também em inimigos, R49), Vulnerability (teto +60%), Detonation, Distortion (R10)); código de M07; escreve `Assets/_Project/Code/AI/Enemies/HollowDepths/`, `Assets/_Project/Content/Enemies/HollowDepths/` e `Code/Tests/PlayMode/Regions/HollowDepths/EnemyTests.cs` (não toque em outros).
- **Restrições:** Pooling; nenhum valor numérico fora do CSV; decisão de IA na cadência 10/4 Hz (11).
- **Aceite:** Teste PlayMode por inimigo: spawn, ataque assinatura com telegraph ≥ 0,45 s, morte e devolução ao pool.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: EnemyTests passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T5 — Inimigos 5–8 e mecânicas regionais: Abyssal Carapace, Brood Mother, Umbral Stalker, Rot Priest

- **Objetivo:** Implementar os 4 inimigos restantes e as mecânicas regionais: zonas de escuridão e de luz da arena (Umbral Stalker invisível na escuridão; habilidades de luz/Ember revelam); inimigos que se enterram e emergem; inimigos estáticos enraizados; ovos destrutíveis que eclodem; poças de Decay; Pull seguido de Implosion; consumo de cadáveres (cura ou Totem de Decay); absorção frontal de projéteis.
- **Contexto:** Mecânicas reaproveitáveis (decididas em T2) vão para `Code/Run/Mechanics/Common/`; específicas ficam na pasta da região. Nenhum inimigo comum é imune a uma afinidade (09 §5.6).
- **Arquivos:** lê 09 §5.4, §5.6; 06; 07; decisões de T2; escreve `Assets/_Project/Code/AI/Enemies/HollowDepths/`, `Assets/_Project/Code/Run/Mechanics/{HollowDepths,Common}/`, `Assets/_Project/Content/Enemies/HollowDepths/` e testes em `Code/Tests/PlayMode/Regions/HollowDepths/` (não toque em outros).
- **Restrições:** T4 e T5 escrevem arquivos distintos na mesma pasta: um arquivo por inimigo; nada compartilhado entre as duas tarefas sem o principal.
- **Aceite:** Teste PlayMode por inimigo e por mecânica regional (counterplay funciona: ex.: destruir o objeto/condição desativa a mecânica).
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: todos passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T6 — Salas greybox, NavMesh e grupo de Addressables

- **Objetivo:** Montar as salas autorais da região (prefabs greybox com spawn points, NavMesh baked, luz placeholder, pontos de câmera) conforme o roster de T2 e as regras de 09 §9–§11.
- **Contexto:** Mínimo de salas por tipo (Refinar ao iniciar com o que M16 usou): 8 Combat Arena, 3 Elite Arena, 2 Treasure, 2 Shrine, 3 Event, 2 Challenge, 2 Rest (incluindo Antecâmara), 1 Guardian Arena por Guardian, 1 Boss Arena. Salas autorais em prefab, mapa por seed (11 decisão 11).
- **Arquivos:** lê 09 §9–§11, 11 §3.11, salas da Ashen Wastes como referência; escreve `Assets/_Project/Content/Regions/HollowDepths/Rooms/` e o grupo de Addressables `Region_HollowDepths` (não toque em outros).
- **Restrições:** Só greybox/assets gratuitos (D4).
- **Aceite:** Run Map Previewer (M14/11 §3.15) gera 50 seeds da região sem sala faltante; NavMesh válido em todas as salas.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Regions.HollowDepths -testResults Logs/editmode.xml -logFile Logs/editmode.log` (teste de cobertura de salas por seed) → esperado: passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T7 — Guardians da região

- **Objetivo:** Implementar Mother of Maws (colmeia estática; bocas emergem pela arena; fase 2 Pull constante para o centro) e The Rotting Herald (Detonation ao atingir 5 Decay no jogador, R49; fase 2 totens que removem 2 stacks ao serem destruídos), com 2 fases (troca a 50%), massa Colossal, imunes a Execute, barra no topo e duração alvo 30–60 s.
- **Contexto:** Regras de 09 §7: Guardian Arena fixa na linha 9, 50/50 entre os dois, sem repetir em runs consecutivas; a partir de Nightmare recebem 1 afixo aleatório compatível.
- **Arquivos:** lê 09 §7, §8.1; 06 (massa, Stagger, CC em chefe); framework de chefes de M17; escreve `Assets/_Project/Code/AI/Bosses/Guardians/HollowDepths/`, `Assets/_Project/Content/Bosses/Guardians/HollowDepths/` e `Code/Tests/PlayMode/Regions/HollowDepths/GuardianTests.cs` (não toque em outros).
- **Restrições:** Usar o framework de fases/sequenciador de M17; sem código de fase fora dele.
- **Aceite:** Teste scriptado (bot de M14) derrota cada Guardian em 30–60 s com loadout de referência da dificuldade.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: GuardianTests passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T8 — Chefe The Devouring Presence — parte 1

- **Objetivo:** Implementar fases 1–2 (Hunger, Tentáculos, Exposed, Swallow com transição para a sub-arena Interior e retorno, ✦) com o framework de chefes de M17.
- **Contexto:** Resumo do chefe (09 §8.5): 3 fases, 4 min em Normal. Mecânica *Hunger* (Pull constante para a Boca; dano alto a < 3 m). Fase 1 quatro Tentáculos (HP 4×), Boca *Exposed* 6 s após cortar 2+; Lash, Grasp, Ruin Spray. Fase 2 *Swallow* (engole o jogador para a sub-arena Interior de 15 s com 3 órgãos, dano ×1,5). Fase 3 *Lightless* (escuridão total, fungos acendíveis, Umbral Stalkers). ✦ Hollow Thralls a cada 20 s; ✧ Pull dobra a cada 20 s por 3 s. Regras gerais 09 §8.1: transições com 1,5–3 s de invulnerabilidade sinalizada; adds ≤ 30% do dano recebido.
- **Arquivos:** lê 09 §8.1, §8.5; 06 (telegraphs, Broken, CC em chefe); 07 (Pull, Implosion, Corruption, Decay (máx. 5 também em inimigos, R49), Vulnerability (teto +60%), Detonation, Distortion (R10)); escreve `Assets/_Project/Code/AI/Bosses/DevouringPresence/`, `Assets/_Project/Content/Bosses/DevouringPresence/` e `Code/Tests/PlayMode/Regions/HollowDepths/BossTests.cs` (não toque em outros).
- **Restrições:** Variações ✦ (Nightmare) e ✧ (Eclipse) ligadas por dados de dificuldade, não por `if` espalhado.
- **Aceite:** Teste scriptado atravessa as fases desta parte em Normal e Nightmare sem erro; telegraphs ≥ 0,45 s.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: BossTests (parte 1) passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T9 — Chefe The Devouring Presence — parte 2 e recompensas

- **Objetivo:** Implementar fase 3 (Lightless, fungos, Umbral Stalkers, ✧), Broken, Distortion para Pull em chefe (R10), soft enrage e recompensas; ligar baú do chefe (05), Heart of the Devouring Presence (10, R36) e Legendary garantido na primeira derrota por dificuldade.
- **Contexto:** Broken: 4 s, +30% Vulnerability (teto +60%), limiar +50% por Break até 3× (R11). Soft enrage a partir de 2× a duração alvo (4 min): +15% de dano a cada 30 s. Execute em chefe vira +25% Vulnerability abaixo de 20% (R08).
- **Arquivos:** lê 09 §8.1, §8.5; 05 (baú de chefe, Uniques exclusivos §8.5); 10 (Heart, primeira derrota); escreve `Assets/_Project/Code/AI/Bosses/DevouringPresence/`, `Assets/_Project/Content/Bosses/DevouringPresence/`, `DataSource/regions/hollow_depths/boss_rewards.csv` e testes (não toque em outros).
- **Restrições:** Material de chefe: se M19 já definiu os 5 Hearts, só referenciar; senão adicionar em `DataSource/economy/materials.csv` (nenhum par paralelo edita esse arquivo nesta onda).
- **Aceite:** Teste scriptado derrota o chefe em Normal dentro de 4 min ± 20% com loadout de referência; Eclipse (✧) completa sem erro; recompensas creditadas.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: BossTests passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T10 — Uniques universais da região e conferência do pool do chefe

- **Objetivo:** Autorar U13 (pool de região, universal) e conferir que o pool do chefe e da região fecha com a quota de 05 §9.3 / 02 §3.12.6.
- **Contexto:** Itens declaram a própria origem (`fonteUnique`); a região não lista itens. Outros Uniques desta região: O5 Scepter of the Unbound King e O13 Heart of the Hollow Depths — autorados por M23 (Orun, onda paralela); L5 Eye of the Collapse — já autorado por M21. Estrutura de Unique: 5 fixos + 2 modificadores (GEN-xx ou Signature) + efeito; More ≤ 40% (R03, R25).
- **Arquivos:** lê 02 §3.12.5, §3.12.6; 05 §8.5, §9.3; 03 (GEN-xx, SIG-xx); 00 §5 R03/R25/R28; escreve `DataSource/items/named/universal_hollow_depths.csv` e `DataSource/localization/items_universal_hollow_depths.csv` (não toque em outros).
- **Restrições:** Não editar arquivos de itens de herói nem `universal.csv` (M28).
- **Aceite:** Validador sem erros; relatório lista os Uniques com `fonteUnique` deste chefe/região e quais ainda faltam (com o módulo responsável).
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T11 — Eventos, Shrines, ambientação e áudio placeholder

- **Objetivo:** Ligar o catálogo de eventos (09 §14) e Shrines (09 §12) com pesos e variações regionais, ambientação sonora e trilha placeholder da região (eventos FMOD por intensidade), e strings pt-BR/en-US da região.
- **Contexto:** Eventos usam Rift Shards como moeda de run (R36); Blessings Lesser/Greater/Exalted (R35).
- **Arquivos:** lê 09 §12–§14, 11 (FMOD), código de M09 e M16; escreve `Assets/_Project/Content/Regions/HollowDepths/Events/`, `.../HollowDepths/Ambience/`, banco FMOD `Region_HollowDepths` e `DataSource/localization/region_hollow_depths.csv` (não toque em outros).
- **Restrições:** Somente placeholders de áudio; sem assets pagos.
- **Aceite:** Cada evento regional é jogável no Event Room de teste; ambiente toca por sala.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: EventTests passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T12 — Playtest da região e ajustes por dados

- **Objetivo:** Jogar a região de ponta a ponta em Normal, Nightmare (✦) e Eclipse (✧, via debug) e conferir durações (sala 60–90 s, Guardian 30–60 s, chefe na meta, run 25–35 min), letalidade (06), legibilidade com VFX do jogador e densidade de loot (05 §11).
- **Contexto:** Ajustes só por CSV e dentro das faixas dos docs; fora delas é decisão do usuário.
- **Arquivos:** lê 06 (TTK, letalidade), 09 §8.1, §10, 05 §11, relatórios de T7–T9; escreve CSVs da região e este arquivo (não toque em outros).
- **Restrições:** No máximo 2 iterações.
- **Aceite:** Checklist preenchida no Log com medições.
- **Verificação:** medições do bot/telemetria interna
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T13 — Integração: desbloqueio, run completa e docs

- **Objetivo:** Ligar o desbloqueio da região no Rift Navigator (após The Fractured One), garantir run completa (13 linhas + chefe) gerada por seed jogável pelo bot em Normal, e atualizar status.
- **Contexto:** Mapa de 09 §11 (R33): Estratos 1–5 / 6–9 / 10–13, Guardian na linha 9, Rest garantida na linha 5, Antecâmara na 13.
- **Arquivos:** lê 09 §11, 10 (desbloqueio de regiões), código de M16/M19; escreve `Code/Tests/PlayMode/Regions/HollowDepths/FullRunTest.cs`, este arquivo, `docs/ROADMAP.md` (linha de status) e `CLAUDE.md` se surgir convenção nova (não toque em outros).
- **Restrições:** Não alterar regras de outras regiões.
- **Aceite:** FullRunTest completa 3 seeds; stress da sala mais densa (sala Lightless com Brood Mother (ovos → Gnawlings) + 2 Umbral Stalkers + Rot Priest + Elite Gravitic) a 60 fps no hardware mínimo.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log` e `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: tudo passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Regions.HollowDepths -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
Esperado: todos os testes passam (incluindo FullRunTest em 3 seeds), `Logs/data.log` sem erros, medições do T12 registradas no Log.

## Riscos e rollback
- Mecânica regional nova vazando para sistemas compartilhados → T2 decide se vira componente `Run/Mechanics/Common`; nunca editar `Gameplay/Skills` ou `Gameplay/Status` nesta trilha.
- Duração de chefe fora da meta (R16) → ajuste de HP por CSV no T12; regra nova é decisão do usuário.
- Merge do herói paralelo atrasado → fechar todas as outras tarefas, deixar T10 pendente e marcar o módulo 🔄 com nota; não autorar itens de herói aqui.
- Rollback: tudo na branch `modulo/M26-hollow-depths`; nada vai para a base sem os testes verdes.

## Atualizações de documentação
- `docs/ROADMAP.md` §7: status de M26.
- `CLAUDE.md`: só se surgir convenção nova (ex.: componente de mecânica regional reutilizável, padrão de sub-arena, padrão de selo de habilidade).
- Divergências de design → proposta ao usuário e, se aprovada, registro em `docs/gdd/00-convencoes.md` §5.

## Prompt de disparo
```
Implemente o módulo M26 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M26-hollow-depths.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M26-hollow-depths.
Este módulo roda em paralelo com M23: respeite a partição de arquivos do plano.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

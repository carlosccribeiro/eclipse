# M21 — Herói Lyra (The Veilweaver)
Fase 5 · Onda 12 · Status: ⏳ não iniciado
Depende de: M11, M20 (frameworks de status e progressão; transitivamente M08, M10, M13, M14) · Desbloqueia: M28 · Paralelo com: M24
Branch: modulo/M21-lyra
Docs de referência: 08 §5 (Lyra), §3, §8 (Aether Lattice), §9.3, §10.3, §11.1; 03 (`LYR-xx`); 04 (sets de Lyra); 02 §3.5, §3.6, §3.12.2, §3.12.6; 05 §9.1, §9.3; 06 (frame data, estados); 07 (Chill, Freeze, Shatter, Frostbound, Pull, Implosion); 00 §4 e §5 (R01, R03, R07, R08, R12, R21–R28, R48, R49); CLAUDE.md

## Objetivo
Lyra jogável de ponta a ponta: recurso Arcane Charges, 5 habilidades (Frost Lance, Void Orb, Shattered Reality, Astral Shift, Absolute Zero), 5 armas (Arcane Staff, Spellbook, Twin Wands, Crystal Orb, Void Focus), 15 Augments e os 3 ramos da Aether Lattice, com as 3 builds (Permafrost, Void Singularity, Arcane Overload) viáveis pelas Rotas A, B e C, e todos os dados do herói (modificadores `LYR-xx`, 17 itens nomeados, 3 sets) no pipeline `DataSource/` → SOs. O mecanismo **Consome** (gasta todas as cargas e escala por carga gasta) é genérico no framework de habilidades, não codificado por habilidade.

## Critérios de saída (verificáveis)
1. Recurso conforme 08 §5.1 e passivas de 08 §5.2, com teste EditMode por regra.
2. 5 habilidades + Basic/Heavy com números e tags de 08 §5.4; snapshot de SkillSpec por habilidade.
3. 5 armas com moveset/APS/bônus de 08 §5.6, frame data de 06 e DPS normalizado a ±5% (02).
4. 15 Augments + notáveis/Keystones de 08 §8.3 funcionando pela Lattice (M20), com exclusividade por habilidade.
5. 100% dos `LYR-xx` de 03 importados e compilando sem código por modificador.
6. Itens L1–L17 (02 §3.12.2) e 3 sets × 6 peças (04) importados, com `fonteUnique` e `homeRegion`; validador com 0 erros.
7. Simulador (M14): 9 loadouts (3 builds × Rotas A/B/C); rotas ofensivas a ±10% (00 §4) ou desvio levado ao usuário.
8. Stress: 15 inimigos sob Pull simultâneo de 2 zonas Void + Absolute Zero em 40 alvos a 60 fps no hardware mínimo (11 §3.14), 0 B de GC por frame após warm-up.
9. Desbloqueio: Lyra disponível desde o início (08 §9.3) — aparece no Keeper sem missão.
10. Status e Log deste arquivo e a linha de `docs/ROADMAP.md` atualizados.

## Fora de escopo
- Conteúdo de regiões, inimigos e chefes (trilha de regiões).
- Itens universais `U-xx` e catálogo restante do Kael (M28).
- Fontes de drop de Pinnacle (M29) e de Challenge Rooms (já cobertas por M16); aqui só se declara `fonteUnique`.
- Arte final, animação final e VFX final (A00). Balanceamento cruzado entre heróis (M28).

## Premissas e decisões
- Frameworks de M08 (habilidades), M10 (modificadores, Augments, Generation, spikes), M11 (status), M13 (equipamento/sets), M14 (simulador) e M20 (Lattice, desbloqueios) estão ✅ e mergeados na base. *Refinar ao iniciar* (T1 confirma).
- Estrutura de CSV proposta abaixo segue a convenção esperada de M02/M12; se M02 definiu outra, T2 ajusta os nomes mantendo a partição.
- Visual greybox + assets gratuitos (D4). Áudio: só hooks de evento e placeholders no banco FMOD do herói.
- Todos os itens com prefixo `L` (inclusive Uniques de chefe, região, Challenge e Pinnacle) são autorados aqui, porque dependem dos modificadores do herói; regiões apenas declaram que o chefe existe.

### Partição de arquivos (escreve) — evita colisão com M24
- `Assets/_Project/Code/Gameplay/Heroes/Lyra/**`, `Assets/_Project/Content/Heroes/Lyra/**`, `Assets/_Project/Code/Tests/{EditMode,PlayMode}/Heroes/Lyra/**`
- `DataSource/heroes/lyra/*.csv`, `DataSource/items/bases/lyra.csv`, `DataSource/items/named/lyra.csv`, `DataSource/sets/lyra.csv`, `DataSource/modifiers/lyr.csv`, `DataSource/sim/loadouts/lyra/`, `DataSource/localization/{hero_,items_}lyra.csv`
- Extensões genéricas de framework (somente se T2 aprovar): `Assets/_Project/Code/Gameplay/{Skills,Status}/**` — a trilha de regiões não escreve nessas pastas nesta onda.
- **Não escreve:** `DataSource/regions/**`, `DataSource/items/named/universal*.csv`, `Assets/_Project/Code/AI/{Enemies,Bosses}/**`, `Content/{Regions,Enemies,Bosses}/**`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Levantar o estado dos frameworks para Lyra | investigação | investigador | — | — (relatório no Log) | ⏳ |
| T2 | Decidir extensões de framework e fechar a partição de arquivos | decisão | principal | T1 | este arquivo (Premissas, Log) | ⏳ |
| T3 | Recurso Arcane Charges e passivas inatas | execução | executor | T2 | Code/Gameplay/Heroes/Lyra/Resource/*, testes | ⏳ |
| T4 | Dados do herói em CSV (kit, armas, Augments, Lattice) | execução | executor | T2 | `DataSource/heroes/lyra/{hero,resource,skills,weapons,augments,lattice}.csv` | ⏳ |
| T5 | Comportamentos das 5 habilidades, Basic/Heavy e movesets das 5 armas | execução | executor | T3, T4 | Code/Gameplay/Heroes/Lyra/Skills/*, Content/Heroes/Lyra/*, testes | ⏳ |
| T5b | Mecanismo genérico Consome + zonas Void com limite | execução | executor | T3 | Code/Gameplay/Skills/ResourceSpend/*, testes | ⏳ |
| T6 | Augments e nós da Lattice do herói | execução | executor | T5, T5b | Code/Gameplay/Heroes/Lyra/Augments/*, testes | ⏳ |
| T7 | Modificadores LYR-xx de 03 no pipeline | execução | executor | T5 | `DataSource/modifiers/lyr.csv`, testes | ⏳ |
| T8 | Itens nomeados L1–L17 e os 3 sets de Lyra | execução | executor | T7 | `DataSource/items/named/lyra.csv`, `DataSource/sets/lyra.csv` | ⏳ |
| T9 | Loadouts das 3 builds × Rotas A/B/C e rodada do simulador | execução | executor | T6, T8 | `DataSource/sim/loadouts/lyra/*.json`, relatório | ⏳ |
| T10 | Revisão de game feel e de balanceamento | revisão | principal | T9 | CSV do herói (ajustes), este arquivo (Log) | ⏳ |
| T11 | Integração: HUD do recurso, desbloqueio, localização e docs | execução | executor | T10 | UI/HUD/Resources/Lyra*, Meta (regra de desbloqueio), docs | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T4 (arquivos disjuntos: código do recurso × CSV)
4. T5 ∥ T5b (pastas disjuntas)
5. T6 ∥ T7
6. T8
7. T9
8. T10
9. T11

## Briefs
### T1 — Levantar o estado dos frameworks para Lyra

- **Objetivo:** Mapear o que os frameworks já entregam e o que falta para o kit completo de Lyra: habilidades (M08), modificadores/Augments/Generation (M10), status/Resonance (M11), equipamento/sets (M13), simulador (M14), Lattice/desbloqueios (M20), e a estrutura real de CSV e do validador (M02/M12).
- **Contexto:** Lyra usa Arcane Charges; habilidades Frost Lance, Void Orb, Shattered Reality, Astral Shift, Absolute Zero; estados Intangível (Astral Shift), Freeze de Ultimate até 3 s (exceção 07, R49); status Chill, Freeze, Shatter, Frostbound, Pull, Implosion. O mecanismo **Consome** (gasta todas as cargas e escala por carga gasta) é genérico no framework de habilidades, não codificado por habilidade.
- **Arquivos:** lê CLAUDE.md, este arquivo, 08 §5 e §8, docs/roadmap/M08…M20 (Log e critérios), `Assets/_Project/Code/Gameplay/Skills`, `DataSource/`; escreve nada (somente relatório) (não toque em outros).
- **Restrições:** Somente leitura.
- **Aceite:** Lista de lacunas por sistema (arquivo:linha), estrutura de CSV confirmada e lista de APIs ausentes para Lyra.
- **Verificação:** relatório com evidências; o principal registra no Log
- **Entrega:** no máximo 300 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T2 — Decidir extensões de framework e fechar a partição de arquivos

- **Objetivo:** Transformar as lacunas de T1 em decisões: o que vira extensão genérica de framework (tarefa própria) e o que é código específico do herói; confirmar a partição de arquivos com o par da onda.
- **Contexto:** Par paralelo: M24 (trilha de regiões) — não pode haver arquivo escrito pelos dois. Extensões genéricas em pastas compartilhadas (Gameplay/Skills, Gameplay/Status) só nesta trilha.
- **Arquivos:** lê relatório de T1, docs/roadmap/M24-*.md; escreve este arquivo (não toque em outros).
- **Restrições:** Mudança de número/regra de design → perguntar ao usuário e registrar em 00 §5; não decidir sozinho.
- **Aceite:** Seção Premissas atualizada; tabela de tarefas ajustada (novas T5x se necessário).
- **Verificação:** revisão do diff deste arquivo
- **Entrega:** no máximo 150 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T3 — Recurso Arcane Charges e passivas inatas

- **Objetivo:** Implementar o componente de recurso polimórfico de Lyra (Arcane Charges (cargas; base 3, teto base 6, até 8 só via 08 §11.1)) com todas as regras da tabela de 08 §5.1, a tradução dos atributos genéricos (Maximum Resource, Resource Generation, Resource Cost Reduction) de 01 e 08 §3.2, e as passivas inatas de 08 §5.2.
- **Contexto:** Regras numéricas vêm de CSV (`resource.csv`), não do código. Exceções de teto só pelas fontes de 08 §11.1 (R01).
- **Arquivos:** lê 08 §5.1–§5.2, §3.2, §11.1; 01 (tradução de recurso); 00 §5 R01; escreve `Assets/_Project/Code/Gameplay/Heroes/Lyra/Resource/` e `Assets/_Project/Code/Tests/EditMode/Heroes/Lyra/ResourceTests.cs` (não toque em outros).
- **Restrições:** Sem números hardcoded; sem alocação por frame.
- **Aceite:** Um teste por linha da tabela de recurso (ganho, gasto, decaimento, início de sala) e por passiva.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Heroes.Lyra -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: todos passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T4 — Dados do herói em CSV (kit, armas, Augments, Lattice)

- **Objetivo:** Autorar os dados de Lyra: herói (atributos-base de 08 §3.1), Basic/Heavy, 5 habilidades com custo/CD/alcance/área/coeficiente/tags de 08 §5.4, 5 armas (Arcane Staff, Spellbook, Twin Wands, Crystal Orb, Void Focus) com APS, moveset, bônus de kit (08 §5.6) e fator de DPS/implícito de 02 §3.5, Off Hand Grimoire (02 §3.6), 15 Augments (08 §5.5) e os 3 ramos da Lattice com notáveis/Keystones (08 §8.3).
- **Contexto:** Tags só do vocabulário de 00 §3.4; HitFlags separadas (R02). Frame data dos movesets é de 06 (R18).
- **Arquivos:** lê 08 §3, §5, §8; 02 §3.5–§3.6; 06 (frame data das armas de Lyra); estrutura de CSV de M02; escreve `DataSource/heroes/lyra/{hero,resource,skills,weapons,augments,lattice}.csv`, `DataSource/items/bases/lyra.csv` e `DataSource/localization/hero_lyra.csv` (não toque em outros).
- **Restrições:** Nenhum arquivo fora da pasta do herói; nenhum valor fora do que os docs definem (lacuna → Log).
- **Aceite:** Importação sem erros; contagens: 5 habilidades, 5 armas, 1 Off Hand base, 15 Augments, 3 ramos × (10 menores + 5 notáveis + 2 Keystones) + 8 nós de Núcleo.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros no Logs/data.log
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T5 — Comportamentos das 5 habilidades, Basic/Heavy e movesets das 5 armas

- **Objetivo:** Implementar Frost Lance, Void Orb, Shattered Reality, Astral Shift, Absolute Zero, Basic e Heavy de cada arma e os bônus de kit das armas, com greybox/VFX placeholder e hooks de SFX, usando o framework de habilidades (delivery/effects/triggers) e a frame data de 06.
- **Contexto:** Comportamentos em 08 §5.3–§5.6; estados Intangível (Astral Shift), Freeze de Ultimate até 3 s (exceção 07, R49) definidos por 06; status por 07. O mecanismo **Consome** (gasta todas as cargas e escala por carga gasta) é genérico no framework de habilidades, não codificado por habilidade.
- **Arquivos:** lê 08 §5; 06 (frame data, estados, telegraphs); 07 (status de Lyra); 11 §3.6; escreve `Assets/_Project/Code/Gameplay/Heroes/Lyra/Skills/`, `Assets/_Project/Content/Heroes/Lyra/` e `Code/Tests/PlayMode/Heroes/Lyra/KitSmokeTests.cs` (não toque em outros).
- **Restrições:** Nada de código por modificador; comportamento parametrizado por SkillSpec. Arte só greybox/gratuita (D4).
- **Aceite:** Smoke test PlayMode conjura cada habilidade com cada arma no sandbox sem erro; DPS normalizado das 5 armas dentro de ±5% (02) medido por teste.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Heroes.Lyra -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: KitSmokeTests passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T5b — Mecanismo genérico Consome + zonas Void com limite

- **Objetivo:** Implementar no framework de habilidades um custo do tipo `ConsumeAll` que gasta todas as cargas e expõe `chargesSpent` para escalar parâmetros (dano por carga, fraturas extras), e um registro de zonas ativas por dono com limite opcional (Eventide: máx. 2 zonas Void).
- **Contexto:** 08 §5.4: Void Orb (+50% por carga), Shattered Reality (+1 fratura por carga), Absolute Zero (+100% por carga). Paradox Cascade remove o consumo; Mana Surge soma 2 cargas extras. Deve servir também a modificadores de 03 que alteram custo (fase Recurso).
- **Arquivos:** lê 08 §5, 03 (fase Recurso), docs/gdd/11 §3.6, código de M08/M10 em Code/Gameplay/Skills; escreve `Assets/_Project/Code/Gameplay/Skills/ResourceSpend/` e `Assets/_Project/Code/Tests/EditMode/Skills/ConsumeAllTests.cs` (não toque em outros).
- **Restrições:** Não alterar a assinatura pública de SkillSpec sem registrar no Log; zero alocação no cast.
- **Aceite:** Testes cobrem 0, 1, 3 e 8 cargas; Paradox Cascade e Mana Surge representáveis só por dados.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Skills -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: todos passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T6 — Augments e nós da Lattice do herói

- **Objetivo:** Fazer os 15 Augments e os notáveis/Keystones de Lyra funcionarem pela Lattice de M20 (exclusividade de 1 Augment por habilidade, gates, 2 Keystones), preferindo ops genéricos de modificador a código específico.
- **Contexto:** Augments não contam como modificadores de equipamento e não dão More (R03). Keystones ≤ +20% More sustentado. Exceções de teto de recurso só as de 08 §11.1.
- **Arquivos:** lê 08 §5.5, §8, §10.3, §11.1; 00 §5 R01/R03; código de M20 (Lattice); escreve `Assets/_Project/Code/Gameplay/Heroes/Lyra/Augments/` e `Code/Tests/EditMode/Heroes/Lyra/AugmentSpecTests.cs` (não toque em outros).
- **Restrições:** Se um Augment exigir op nova, implementá-la como op genérica e registrar no Log.
- **Aceite:** Snapshot de SkillSpec antes/depois para cada Augment; teste de exclusividade e de teto de Keystones.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Heroes.Lyra -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: todos passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T7 — Modificadores LYR-xx de 03 no pipeline

- **Objetivo:** Importar todos os modificadores `LYR-xx` de 03 e garantir que compilam no SkillSpec pelas 6 fases (Conversão → Forma → Parâmetros → Replicação → Gatilhos → Recurso) sem código por modificador.
- **Contexto:** 03 é dono dos nomes e valores (R26). Modificadores escritos para premissas antigas das habilidades: em divergência vale 08 (D01 revisou). Grupos exclusivos e limite de 14 ativos (R30).
- **Arquivos:** lê 03 (seção de Lyra e GEN-xx), 08 §5.4, 00 §5 R07/R26/R30, docs/roadmap/D01-revisao-modificadores.md; escreve `DataSource/modifiers/lyr.csv` e `Code/Tests/EditMode/Heroes/Lyra/ModifierSpecTests.cs` (não toque em outros).
- **Restrições:** Não editar 03; divergência de design → Log + principal.
- **Aceite:** 100% dos `LYR-xx` importados; snapshot por modificador; teste de grupos exclusivos.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` e `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Heroes.Lyra -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: 0 erros e testes passando
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T8 — Itens nomeados L1–L17 e os 3 sets de Lyra

- **Objetivo:** Autorar os 17 itens nomeados de Lyra (02 §3.12.2, incluindo os Uniques de chefe/região/Challenge/Pinnacle com o campo `fonteUnique`) e os 3 sets (Frozen Empress (lar: The Frozen Cathedral), Void Oracle (lar: The Hollow Depths), Arcane Paradox (lar: The Astral Citadel)) com 6 peças cada (04), coluna `homeRegion` (05 §9.1) e Set Powers.
- **Contexto:** Estrutura de item de R25 (Legendary 4 fixos + 1 Afixo Aberto + 2 modificadores + efeito; peça de set sem efeito; Unique 5 fixos + 2 modificadores + Signature opcional). Faixas por tier de 01, pool por slot de 02, máx. 1 afixo `*` por peça de set. A origem do drop é declarada no item; regiões não listam itens. 
- **Arquivos:** lê 02 §3.12.2 e §3.12.6, 04 (sets de Lyra), 05 §9.1/§9.3, 01 §3.9, 00 §5 R03/R21–R28; escreve `DataSource/items/named/lyra.csv`, `DataSource/sets/lyra.csv` e `DataSource/localization/items_lyra.csv` (não toque em outros).
- **Restrições:** Não escrever arquivos de região nem `universal*.csv`.
- **Aceite:** 17 itens + 18 peças + 6 Set Powers importados; validador sem erros (estrutura R25, pool, Faixa Nomeada 70–115%, tetos de More R03, teto de recurso ≤ 8).
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T9 — Loadouts das 3 builds × Rotas A/B/C e rodada do simulador

- **Objetivo:** Criar 9 loadouts (Permafrost, Void Singularity, Arcane Overload × Rotas A/B/C de 08 §5.7) com equipamento de referência de 00 §4 (fim de campanha, Eclipse, iLvl ~80, itens +6) e rodar o simulador de M14.
- **Contexto:** Regra de ouro (00 §4): rotas ofensivas da mesma build a ±10% entre si; rota defensiva compara sobrevivência. Simulador usa o DamageResolver real.
- **Arquivos:** lê 08 §5.7, 04 (análise de rotas), 00 §4, documentação do simulador em docs/roadmap/M14-simuladores-training.md; escreve `DataSource/sim/loadouts/lyra/` e `Logs/sim/lyra.csv` (relatório, não versionado) (não toque em outros).
- **Restrições:** Não ajustar números nesta tarefa — só medir.
- **Aceite:** Relatório com DPS sustentado, burst e TTK por loadout; desvio entre rotas calculado.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loadouts/lyra -quit -logFile Logs/sim.log` (nome do método definido em M14 — Refinar ao iniciar) → esperado: CSV com 9 linhas
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T10 — Revisão de game feel e de balanceamento

- **Objetivo:** Jogar Lyra no Training Grounds com as 5 armas e as 3 builds, conferir a checklist de game feel de 06 e o relatório de T9; decidir ajustes dentro das faixas dos docs.
- **Contexto:** Ajuste fora das faixas definidas nos docs é decisão do usuário (registrar proposta).
- **Arquivos:** lê relatório de T9, 06 (critérios de feel), 00 §4; escreve CSVs do herói e este arquivo (não toque em outros).
- **Restrições:** No máximo 2 iterações de ajuste + re-simulação.
- **Aceite:** Rotas dentro de ±10% ou desvio justificado e levado ao usuário; checklist de feel marcada.
- **Verificação:** re-execução de T9 após ajustes
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T11 — Integração: HUD do recurso, desbloqueio, localização e docs

- **Objetivo:** Exibir Arcane Charges no HUD (M06), aplicar a regra de desbloqueio (Lyra disponível desde o início (08 §9.3) — aparece no Keeper sem missão) via sistema de M20/M19, completar strings pt-BR/en-US e atualizar status.
- **Contexto:** UI só reage a eventos (CLAUDE.md §4). Desbloqueio é dado, não código condicional por herói.
- **Arquivos:** lê 08 §9.3, código de M06/M19/M20; escreve `Assets/_Project/Code/UI/HUD/Resources/LyraResourceView.*`, `DataSource/localization/hero_lyra.csv`, este arquivo, `docs/ROADMAP.md` (linha de status) e `CLAUDE.md` se surgir convenção nova (não toque em outros).
- **Restrições:** Não alterar layout de HUD dos outros heróis.
- **Aceite:** Herói selecionável conforme a regra; HUD correto; testes do módulo verdes.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Heroes.Lyra -testResults Logs/playmode.xml -logFile Logs/playmode.log` e `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: tudo passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Heroes.Lyra -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loadouts/lyra -quit -logFile Logs/sim.log
```
Esperado: todos os testes passam, `Logs/data.log` sem erros, relatório do simulador com 9 loadouts e desvio entre rotas ofensivas ≤ 10%.

## Riscos e rollback
- Pull de várias fontes empilhado causa jitter de física → limitar força total por alvo (07) e testar no stress.
- Absolute Zero de 3 s colide com a DR de CC de 06 → teste explícito com DR ativa.
- Framework insuficiente descoberto em T1 → T2 cria tarefa de extensão genérica; se a extensão for grande, pausar e abrir módulo de framework separado (decisão do usuário).
- Rollback: o módulo vive na branch `modulo/M21-lyra`; nada vai para a base sem os testes do módulo verdes.

## Atualizações de documentação
- `docs/ROADMAP.md` §7: status de M21.
- `CLAUDE.md`: só se surgir convenção nova (ex.: op genérica de modificador, novo tipo de custo de recurso, padrão de entidade aliada).
- Divergências de design encontradas → proposta ao usuário e, se aprovada, registro em `docs/gdd/00-convencoes.md` §5 (nunca editar o doc dono sem aprovação).

## Prompt de disparo
```
Implemente o módulo M21 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M21-lyra.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M21-lyra.
Este módulo roda em paralelo com M24: respeite a partição de arquivos do plano.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

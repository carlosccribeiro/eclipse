# M28 — Catálogo completo de itens, sets e modificadores
Fase 5 · Onda 15 · Status: ⏳ não iniciado
Depende de: M14, M21, M22, M23 · Desbloqueia: M29 (Marcos E e F) · Paralelo com: M27
Branch: modulo/M28-catalogo-completo
Docs de referência: 02 (catálogo: §3.5–§3.9, §3.12, §3.13), 03 (206 modificadores), 04 (12 sets, 72 peças, análise de rotas), 05 §8–§11 (pity, target farming, densidade), 01 §3.9 (faixas por tier), 08 (builds), 11 §3.15–§3.16 (pipeline de dados, validador, simuladores), 00 §4 (orçamento de poder) e §5 (R03, R21–R28, R30, R48); CLAUDE.md

## Objetivo
Todo o catálogo de lançamento no pipeline `DataSource/` → SOs, validado e balanceado: 84 itens nomeados (54 Legendary + 30 Unique), 72 peças de set com 24 Set Powers, 206 modificadores, bases e Infusions, com localização completa; e o relatório do simulador mostrando as 12 builds × Rotas A/B/C dentro da regra de ouro (±10%, 00 §4).

## Critérios de saída (verificáveis)
1. Validador completo com 0 erros e 0 avisos de localização no catálogo.
2. `CatalogCompletenessTests` verde: 84 nomeados, 72 peças, 24 Set Powers, 206 modificadores, 30 Uniques com quota 15/4/7/4 por fonte (após merge de M27), cada herói com 3 sets em 3 regiões-lar diferentes.
3. Simulador: 36 loadouts; rotas ofensivas da mesma build a ±10% ou desvio aprovado pelo usuário e registrado.
4. Loot Simulator: métricas de 05 §11 com desvio ≤ 20% por região/dificuldade, ou ajuste registrado.
5. Nenhum valor fora das faixas dos docs sem registro em 00 §5.
6. Status e Log deste arquivo e a linha de `docs/ROADMAP.md` atualizados; Marco E fechado (e Marco F junto com M27).

## Fora de escopo
- Comportamento novo de habilidades ou de Set Powers (código) — só dados; se faltar op, registrar e abrir tarefa no módulo de framework.
- Conteúdo de região e Uniques universais de região/chefe (`universal_<região>.csv`, M24–M27).
- Fontes de drop de endgame (Pinnacle no Sundering Heart, M29); aqui só `fonteUnique`.

## Premissas e decisões
- Módulos de herói M21–M23 fecharam seus itens/sets/modificadores; Kael tem catálogo parcial das Fases 2–3 (M13/M14). *Refinar ao iniciar* (T1 mede).
- Partição de dados por arquivo (herói, universal, região) já usada pelos módulos anteriores; este módulo segue a mesma convenção (ou a que M02 fixou).
- Balanceamento por dados: ajustes dentro das faixas dos docs são decisão do principal; fora delas, do usuário.

### Partição de arquivos (escreve) — evita colisão com M27
- `DataSource/items/named/{kael,universal}.csv`, `DataSource/sets/kael.csv`, `DataSource/modifiers/{kae,generic}.csv`
- Correções de catálogo em `DataSource/items/named/{lyra,nyx,orun}.csv`, `DataSource/sets/{lyra,nyx,orun}.csv`, `DataSource/modifiers/{lyr,nyx,oru}.csv` (heróis já fechados; ninguém mais escreve nesta onda)
- `DataSource/items/bases/common.csv`, `DataSource/items/infusions.csv`, `DataSource/localization/items_*.csv` (exceto `items_universal_<região>.csv`)
- `DataSource/sim/loadouts/**`, `Assets/_Project/Code/Tests/EditMode/Catalog/**`
- **Não escreve:** `DataSource/regions/**`, `DataSource/items/named/universal_<região>.csv`, `DataSource/localization/{region_,items_universal_}*.csv`, código de gameplay/IA.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Inventário do catálogo: docs × DataSource | investigação | investigador | — | — (relatório no Log) | ⏳ |
| T2 | Fechar a partição com M27 e a ordem de entrada | decisão | principal | T1 | este arquivo (Premissas, Log) | ⏳ |
| T3 | Catálogo restante do Kael | execução | executor | T2 | `DataSource/items/named/kael.csv`, `DataSource/sets/kael.csv`, `DataSource/modifiers/kae.csv` | ⏳ |
| T4 | Itens universais restantes e modificadores GEN-xx | execução | executor | T2 | `DataSource/items/named/universal.csv`, `DataSource/modifiers/generic.csv` | ⏳ |
| T5 | Correções de catálogo nos arquivos de herói | execução | executor | T2 | `DataSource/items/named/{lyra,nyx,orun}.csv`, `DataSource/sets/{lyra,nyx,orun}.csv`, `DataSource/modifiers/{lyr,nyx,oru}.csv` | ⏳ |
| T6 | Bases, Infusions e localização completa do catálogo | execução | executor | T3, T4, T5 | `DataSource/items/bases/common.csv`, `DataSource/items/infusions.csv`, `DataSource/localization/items_*.csv` | ⏳ |
| T7 | Validação completa do catálogo | execução | executor | T6 | correções nos arquivos de M28; testes de catálogo | ⏳ |
| T8 | Simulação completa: 12 builds × Rotas A/B/C | execução | executor | T7 | `DataSource/sim/loadouts/kael/*.json` (se faltarem), relatório | ⏳ |
| T9 | Análise de balanceamento e decisões de ajuste | decisão | principal | T8 | este arquivo (Log) | ⏳ |
| T10 | Aplicar ajustes e re-simular | execução | executor | T9 | arquivos de dados de M28 (valores) | ⏳ |
| T11 | Simulação de loot e ritmo de set | execução | executor | T7 | relatório | ⏳ |
| T12 | Fechamento dos Marcos E/F e docs | revisão | principal | T10, T11 | este arquivo, docs/ROADMAP.md, CLAUDE.md | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T4 ∥ T5 (arquivos disjuntos)
4. T6
5. T7
6. T8 ∥ T11
7. T9
8. T10
9. T12

## Briefs
### T1 — Inventário do catálogo: docs × DataSource

- **Objetivo:** Comparar o que existe em `DataSource/` com os catálogos dos docs e listar o que falta, por arquivo e por módulo responsável: 84 itens nomeados (02 §3.12: 54 Legendary + 30 Unique), 72 peças de set e 24 Set Powers (04), 206 modificadores (03: 176 específicos + 30 GEN-xx + SIG-xx), bases de item (02 §3.5–§3.8), Infusions (02 §3.9.4) e os 30 Uniques por fonte (02 §3.12.6 / 05 §9.3).
- **Contexto:** Itens de herói (K/L/N/O) são autorados pelos módulos de herói (Kael desde as Fases 2–3; Lyra M21, Nyx M22, Orun M23); Uniques universais de chefe/região pelos módulos de região (M17, M24–M27). M27 (Astral Citadel) roda em paralelo com este módulo.
- **Arquivos:** lê CLAUDE.md, este arquivo, 02, 03, 04, 05 §9.3, `DataSource/**`, Logs de M13, M14, M17, M21–M26; escreve nada (somente relatório) (não toque em outros).
- **Restrições:** Somente leitura.
- **Aceite:** Tabela 'faltante × arquivo × responsável' com contagens e lista de inconsistências (IDs citados que não existem, `fonteUnique` sem fonte, `homeRegion` vazio).
- **Verificação:** relatório com evidências; o principal registra no Log
- **Entrega:** no máximo 350 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T2 — Fechar a partição com M27 e a ordem de entrada

- **Objetivo:** Decidir quais lacunas este módulo preenche, em que arquivos, e o que fica com M27 (Uniques universais da Astral Citadel: U2, U5, U8, U18, U19).
- **Contexto:** M28 escreve `kael.csv`, `universal.csv`, arquivos de herói (só correções de catálogo), `modifiers/generic.csv` e loadouts; nunca `DataSource/regions/**` nem `universal_<região>.csv`.
- **Arquivos:** lê relatório de T1, docs/roadmap/M27-astral-citadel.md; escreve este arquivo (não toque em outros).
- **Restrições:** Mudança de número/regra de design → usuário + 00 §5.
- **Aceite:** Premissas e tarefas ajustadas.
- **Verificação:** revisão do diff deste arquivo
- **Entrega:** no máximo 150 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T3 — Catálogo restante do Kael

- **Objetivo:** Completar os 16 itens nomeados do Kael (02 §3.12.1, incluindo K8 do Pale Sovereign, K12 da Ashen Wastes, K15 de Challenge e K16 Pinnacle com SIG-K16), os 3 sets do Kael (Ashen Sovereign, Ruinbreaker, Eternal Bastion: 18 peças, `homeRegion` de 05 §9.1) e todos os `KAE-xx` de 03.
- **Contexto:** Estrutura de item de R25; faixas de 01; pool de 02; peça de set com no máximo 1 afixo `*`; More ≤ 15% em Legendary e ≤ 40% em Unique (R03).
- **Arquivos:** lê 02 §3.12.1, §3.12.6; 03 (KAE-xx, SIG-K16); 04 (sets do Kael); 05 §9.1; 00 §5 R03/R21–R28; escreve `DataSource/items/named/kael.csv`, `DataSource/sets/kael.csv`, `DataSource/modifiers/kae.csv` e `DataSource/localization/items_kael.csv` (não toque em outros).
- **Restrições:** Não alterar itens já existentes além de corrigir erro de validação apontado em T1.
- **Aceite:** Contagens batem com 02/03/04 para o Kael; validador sem erros.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T4 — Itens universais restantes e modificadores GEN-xx

- **Objetivo:** Autorar os itens universais de 02 §3.12.5 que não pertencem a uma região (os de região/chefe ficam em `universal_<região>.csv`), incluindo U17 da Ashen Wastes se M16 não o criou, e completar os 30 `GEN-xx` e eventuais `SIG-U*` sem dono.
- **Contexto:** Universais usam só GEN-xx (R26). Hero-bound: item com modificador de herói não é equipável por outro herói (R27) — universais nunca têm modificador de herói.
- **Arquivos:** lê 02 §3.12.5, §3.12.6; 03 (GEN-xx, SIG-xx); 00 §5 R25–R28; escreve `DataSource/items/named/universal.csv`, `DataSource/modifiers/generic.csv` e `DataSource/localization/items_universal.csv` (não toque em outros).
- **Restrições:** Não escrever `universal_<região>.csv` (M24–M27).
- **Aceite:** Todos os U-xx de 02 existem em algum arquivo exatamente uma vez; validador sem erros.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T5 — Correções de catálogo nos arquivos de herói

- **Objetivo:** Corrigir as lacunas e inconsistências que T1 encontrou nos dados de Lyra, Nyx e Orun (itens faltantes, `fonteUnique`/`homeRegion` vazios, IDs de modificador inexistentes, Pinnacle/Challenge sem Signature).
- **Contexto:** Os módulos de herói já fecharam; aqui só se completa o catálogo. Comportamento novo de habilidade está fora de escopo.
- **Arquivos:** lê relatório de T1; 02 §3.12.2–§3.12.4; 03; 04; escreve os arquivos de dados de Lyra/Nyx/Orun listados (não toque em outros).
- **Restrições:** Nenhuma mudança de comportamento ou número sem registro no Log; não tocar em código.
- **Aceite:** Zero inconsistências da lista de T1 para esses heróis.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T6 — Bases, Infusions e localização completa do catálogo

- **Objetivo:** Fechar bases não-herói (armaduras Plate/Leather/Vestment nos 5 slots, joias, Relics — 02 §3.7–§3.8), as Infusions (02 §3.9.4 com faixas de 01) e as strings pt-BR/en-US de todos os itens, sets, Set Powers e modificadores.
- **Contexto:** Validador exige strings localizadas presentes (11 §3.15).
- **Arquivos:** lê 02 §3.7–§3.9.4; 01 §3.9.2; 11 §3.15; escreve `DataSource/items/bases/common.csv`, `DataSource/items/infusions.csv` e `DataSource/localization/items_*.csv` (exceto os de região) (não toque em outros).
- **Restrições:** Não escrever arquivos de localização de região.
- **Aceite:** Validador sem avisos de string faltante no catálogo.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros e 0 avisos de localização
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T7 — Validação completa do catálogo

- **Objetivo:** Rodar o validador completo e um teste EditMode de catálogo que confere as contagens de lançamento (84 nomeados, 72 peças, 24 Set Powers, 206 modificadores, 30 Uniques com quota 15/4/7/4 por fonte, cada herói com 3 sets em 3 regiões diferentes) e as regras de 00 §5 (R03, R21–R28, R48).
- **Contexto:** A quota de Uniques só fecha com M27 mergeado (U2, U5, U8, U18, U19); antes disso o teste reporta 25/30 como esperado parcial.
- **Arquivos:** lê 00 §5, 02 §3.12.6, 05 §9.1/§9.3, 11 §3.15; escreve arquivos de dados de M28 (correções) e `Assets/_Project/Code/Tests/EditMode/Catalog/CatalogCompletenessTests.cs` (não toque em outros).
- **Restrições:** Falha em arquivo de região → registrar para M27, não corrigir aqui.
- **Aceite:** Validador 0 erros; CatalogCompletenessTests verde (ou só a pendência explícita de M27).
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` e `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Catalog -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: 0 erros e testes passando
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T8 — Simulação completa: 12 builds × Rotas A/B/C

- **Objetivo:** Garantir os 36 loadouts (4 heróis × 3 builds × 3 rotas) com equipamento de referência de 00 §4 (Eclipse, iLvl ~80, itens +6) e rodar o simulador completo.
- **Contexto:** Regra de ouro (00 §4): 6 peças ≈ 3+3 ≈ Legendaries individuais, ±10% em cenário médio; rotas defensivas (Eternal Bastion, Eternal Warden, Abyssal Guardian) comparam sobrevivência. 04 define a regra de correção se a Rota A passar de ×1,35.
- **Arquivos:** lê 00 §4, 04 (análise de rotas), 08 (builds), docs/roadmap/M14-simuladores-training.md; escreve `DataSource/sim/loadouts/kael/` (só o que faltar) e `Logs/sim/catalog.csv` (relatório, não versionado) (não toque em outros).
- **Restrições:** Só medir; ajustes em T10.
- **Aceite:** Relatório com 36 linhas e desvio por build calculado.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loadouts -quit -logFile Logs/sim.log` (nome do método definido em M14 — Refinar ao iniciar) → esperado: CSV com 36 linhas
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T9 — Análise de balanceamento e decisões de ajuste

- **Objetivo:** Ler o relatório de T8, identificar builds/rotas fora de ±10% e decidir ajustes dentro das faixas dos docs (valores rolados, coeficientes de modificador, valores de Set Power).
- **Contexto:** Ajuste fora das faixas dos docs ou mudança de regra → proposta ao usuário e, se aprovada, registro em 00 §5.
- **Arquivos:** lê relatório de T8; 00 §4; 03/04 (orçamentos); escreve este arquivo (não toque em outros).
- **Restrições:** Nenhuma edição de doc de design sem aprovação do usuário.
- **Aceite:** Lista de ajustes com alvo numérico por item/modificador/Set Power.
- **Verificação:** revisão do Log
- **Entrega:** no máximo 250 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T10 — Aplicar ajustes e re-simular

- **Objetivo:** Aplicar os ajustes de T9 nos CSVs e re-rodar o simulador até as rotas ofensivas ficarem a ±10% (no máximo 2 iterações).
- **Contexto:** Mudanças só de valores dentro das faixas; nenhuma mudança de estrutura.
- **Arquivos:** lê Log de T9; escreve CSVs de itens/sets/modificadores autorizados para M28 em T2 (não toque em outros).
- **Restrições:** Não editar `DataSource/regions/**` nem `universal_<região>.csv`.
- **Aceite:** Relatório final com todas as rotas ofensivas a ±10% ou desvio aprovado pelo usuário.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loadouts -quit -logFile Logs/sim.log` → esperado: desvio ≤ 10% por build
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T11 — Simulação de loot e ritmo de set

- **Objetivo:** Rodar o Loot Simulator (11 §3.15) por região e dificuldade e comparar com 05 §11: itens por run, Legendaries por run, ritmo de set na região-lar em Eclipse (~2 h para 3 peças, ~6 h para 6) e distribuição de Uniques por fonte.
- **Contexto:** Pesos: set-lar ×4, itens do chefe da região-lar ×6, Attunement ×3 (05 §9.1, §9.4).
- **Arquivos:** lê 05 §5, §8, §9, §11; docs/roadmap/M14-simuladores-training.md; escreve `Logs/sim/loot_*.csv` (relatório, não versionado) (não toque em outros).
- **Restrições:** Só medir; desvio > 20% de 05 §11 vai para o principal.
- **Aceite:** Tabela região × dificuldade × métrica com desvio em relação a 05 §11.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loot -quit -logFile Logs/sim.log` (nome do método definido em M14 — Refinar ao iniciar) → esperado: CSV por região
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T12 — Fechamento dos Marcos E/F e docs

- **Objetivo:** Conferir os critérios de saída, registrar resultados e atualizar status; com M27 mergeado, confirmar o Marco F (metas do GDD §16).
- **Contexto:** Marco E = 4 heróis × 3 builds com catálogo completo; Marco F = Alpha (M24–M28).
- **Arquivos:** lê este arquivo, docs/ROADMAP.md §3; escreve este arquivo, `docs/ROADMAP.md` (linha de status) e `CLAUDE.md` se surgir convenção nova (não toque em outros).
- **Restrições:** Não marcar ✅ sem a saída dos comandos de verificação.
- **Aceite:** Checklist dos marcos preenchida.
- **Verificação:** todos os comandos da seção Verificação do módulo
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loadouts -quit -logFile Logs/sim.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loot -quit -logFile Logs/sim.log
```
Esperado: validador sem erros, testes verdes (incluindo `CatalogCompletenessTests`), relatórios de simulação dentro das metas ou desvios aprovados.

## Riscos e rollback
- Volume de dados (≈ 360 linhas de catálogo + localização) com erros silenciosos → validador e teste de completude como gate; nada de edição manual de SO.
- Rota A acima de ×1,35 ou rotas fora de ±10% → regra de correção de 04 e no máximo 2 iterações; persistindo, decisão do usuário.
- Dependência de M27 para a quota de Uniques → critério 2 aceita a pendência explícita até o merge de M27; o Marco F só fecha com os dois.
- Rollback: branch `modulo/M28-catalogo-completo`; dados só entram na base com validador e testes verdes.

## Atualizações de documentação
- `docs/ROADMAP.md` §7: status de M28 e dos Marcos E/F.
- `CLAUDE.md`: registrar qualquer convenção nova do pipeline de dados (ex.: nome de arquivo, coluna obrigatória).
- Ajustes de balanceamento que mudam faixas dos docs → aprovação do usuário + registro em `docs/gdd/00-convencoes.md` §5.

## Prompt de disparo
```
Implemente o módulo M28 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M28-catalogo-completo.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M28-catalogo-completo.
Este módulo roda em paralelo com M27: respeite a partição de arquivos do plano.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

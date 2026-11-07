# M12 — Itemização núcleo (itens, afixos, gerador, loot, pity)
Fase 2 · Onda 3 · Status: ⏳ não iniciado
Depende de: M02, M03 · Desbloqueia: M13, M18 · Paralelo com: M04
Branch: modulo/M12-itemizacao-nucleo
Docs de referência: 01 §3.9 (tiers, faixas, multiplicadores de slot, Armor implícito, upgrade), §3.10; 02 §3.1–3.12 (anatomia, nomes, bases, Off Hand, armaduras, joias/Relics, pool §3.9.3, Infusions §3.9.4, compatibilidade §3.10, catálogo §3.12.1 Kael e §3.12.5 universais, §3.12.6 fontes de Unique); 05 §2–§10, §13; 11 §3.4.1–3.4.3, §3.12; 00 §5 R21–R31

## Objetivo
Construir o modelo de item e o gerador de loot no `Eclipse.Core` (C# puro, determinístico, testado por `dotnet test`), com todas as regras de 05 (iLvl, raridade, tipo de Legendary, smart loot, afixos, Roll01, Primal, pity, região-lar, Attunement, quantidade) e de 01/02 (tiers, faixas, Faixa Nomeada, multiplicadores de slot, upgrade, requisito de nível). Os dados entram por `DataSource/*.csv` → SOs: tabelas de atributos completas, pool por slot, bases de todos os slots (armas e Off Hand do Kael), tabelas de loot e os itens nomeados do **Kael (K1–K16)** e **universais (U1–U19)**.

## Critérios de saída (verificáveis)
1. `ItemInstance`/`RolledAffix`/`RolledModifier` conforme 11 §3.4.3; tier = ceil(iLvl/20); valor efetivo = `lerp(min, max, Roll01)` × multiplicador de slot (01 §3.9.3); Faixa Nomeada `lerp(0,70·max, 1,15·max, Roll01)` (R24); upgrade × (1 + 0,03·n) só em atributos/implícito (R31); requisito `clamp(ceil(iLvl × 0,5), 1, 50)` (05 §4.3).
2. `ItemGenerator` executa os 12 passos de 05 §6 e é determinístico: mesma seed + mesmo `LootContext` → item idêntico (teste com 1.000 seeds).
3. Distribuição de raridade do trash em 200.000 amostras dentro de ±0,5 p.p. de 05 §5.4 (Normal ≈ 72,9/25/2/0,1; Eclipse ≈ 36,2/42/20/1,8); pisos por fonte respeitados; Legendary com iLvl < 10 vira Epic; nada de peça de set/Unique em Normal; Unique só com iLvl ≥ 35 (05 §5.2, §5.5).
4. Pity por herói (05 §8.1–8.5) com soft/hard por dificuldade, pity de set após 4 Legendaries (Veteran+), proteção de duplicata ×3, garantias de primeira vez e Unique de chefe no 10º abate; estado serializável em POCO (para o M18).
5. Smart loot (05 §7): 75/25 e 85/15; relevância ×0,25; primários Core ×2 / Secundário ×1 / Terciário ×0,5; grupos de exclusão de 05 §6.2; Epic sorteia modificador 75% do herói (×3 se a habilidade está equipada) / 25% `GEN-xx` via interface `IModifierPoolProvider` (fake nos testes; a fonte real chega no M10/M13).
6. CSVs completos e importados com 0 erros: atributos (01 §3.9.2), multiplicadores de slot (01 §3.9.3), pool por slot (02 §3.9.3), Infusions (02 §3.9.4), raridades, bases de todos os slots, fontes de loot, chances por fonte × dificuldade, faixas de iLvl, pity, região-lar (05 §9.1) e 35 itens nomeados (K1–K16, U1–U19).
7. Regras de validação: estrutura de item (R25), afixos no pool do slot (02; até 1 `*` só em peça de set), Faixa Nomeada dentro de 70–115%, limites de primário (máx. 1; Amulet 2; nenhum em Ring), máx. 2 afixos de afinidade, `minDropILvl`, referência de modificador (warning enquanto `DataSource/modifiers.csv` não existir; o M10 promove a erro).
8. `dotnet test` e EditMode verdes; validador sem erros.

## Fora de escopo
- Equipar, inventário, comparação e Set Powers (M13). Peças de set dos 12 conjuntos (M13 para Kael, M28 para os demais).
- Itens nomeados de Lyra, Nyx e Orun (M28).
- Loot Simulator e relatórios de densidade (M14).
- Drops em cena, beams, coleta e Satchel (M16).
- Persistência (M18), serviços do Blacksmith/Enchanter (M19).

## Premissas e decisões
- **Premissa:** M01 entregou `StableId`, `TagMask`, RNG por streams (`Loot`); M02 entregou `Definition` base, importadores/regras descobertos por atributo (um arquivo por tabela/regra) e o comando `ImportAndValidate`; M03 entregou `StatId` (gerado de `DataSource/stats.csv`) e `StatMod`. O T1 confirma.
- **Decisão:** o modelo de item é Core puro; os SOs (`Data/Items`, `Data/Loot`) só convertem para estruturas Core (`ItemDefinitionData` etc.), para que o gerador rode também no `dotnet test` com fixtures.
- **Decisão:** esquemas de CSV documentados em `docs/data-schemas/items.md` antes do cadastro (contrato para M13, M18, M28).
- **Paralelo com M04:** este módulo não escreve em `Gameplay/`, `Scenes/`, Input nem câmera.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Mapear APIs de M01/M02/M03 e extrair colunas das tabelas de 01/02/05 | investigação | investigador | — | — | ⏳ |
| T2 | Contratos Core de item/loot + esquemas de CSV | design | principal | T1 | `Assets/_Project/Code/Core/Items/Contracts/*.cs`, `Assets/_Project/Code/Core/Loot/Contracts/*.cs`, `docs/data-schemas/items.md` | ⏳ |
| T3 | Modelo de item: instância, tier, leitura de valor, Faixa Nomeada, upgrade, requisito | execução | executor | T2 | `Assets/_Project/Code/Core/Items/Model/*.cs`, `tools/CoreTests/Items/*.cs` | ⏳ |
| T4 | Gerador: iLvl, quantidade, raridade, tipo, smart loot, afixos, Roll01/Primal, modificadores de Epic, região-lar/Attunement | execução | executor | T3 | `Assets/_Project/Code/Core/Loot/Generation/*.cs`, `tools/CoreTests/Loot/Generation/*.cs` | ⏳ |
| T5 | Pity e garantias por herói | execução | executor | T3 | `Assets/_Project/Code/Core/Loot/Pity/*.cs`, `tools/CoreTests/Loot/Pity/*.cs` | ⏳ |
| T6 | Definições SO, importadores e regras de validação de itens e loot | execução | executor | T2 | `Assets/_Project/Code/Data/{Items,Loot}/*.cs`, `Assets/_Project/Code/Editor/DataPipeline/Importers/{Affix,AffixPool,SlotMultiplier,Rarity,ItemBase,NamedItem,LootSource,LootChance,IlvlRange,Pity,HomeRegion}TableImporter.cs`, `Assets/_Project/Code/Editor/Validation/Rules/{Item,Loot}Rules.cs` | ⏳ |
| T7 | Cadastro das tabelas de sistema (atributos, multiplicadores, pool, Infusions, raridades, bases, loot, iLvl, pity, região-lar) | execução | executor | T6 | `DataSource/{affixes,slot_multipliers,affix_pools,infusions,rarities,item_bases,loot_sources,loot_chances,ilvl_ranges,pity,home_regions}.csv` | ⏳ |
| T8 | Cadastro dos itens nomeados K1–K16 e U1–U19 | execução | executor | T6 | `DataSource/items_named.csv`, `DataSource/items_named_affixes.csv` | ⏳ |
| T9 | Testes de integração com dados reais e de distribuição | execução | executor | T4, T5, T7, T8 | `Assets/_Project/Code/Tests/EditMode/Items/*.cs`, `tools/CoreTests/Loot/Distribution/*.cs` | ⏳ |
| T10 | Integração, verificação final e documentação | revisão | principal | T9 | `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T6
4. T4 ∥ T5 ∥ T7 ∥ T8 (arquivos disjuntos; T7 e T8 escrevem CSVs diferentes)
5. T9
6. T10

## Briefs

### T1 — Mapear APIs e tabelas
Tarefa T1: Levantar o que existe e o que as tabelas precisam.
Objetivo: confirmar premissas e produzir a lista de colunas de cada tabela de dados deste módulo.
Contexto: premissas em "Premissas e decisões". Tabelas: 01 §3.9.2 (faixas por tier), §3.9.3 (multiplicadores de slot e Armor implícito), 02 §3.4–3.9 (dano base, bases, Off Hand, armaduras, joias, pool, Infusions), §3.12.1/§3.12.5 (itens nomeados, abreviações de 02 §3.9.1), 05 §3–§9 (fontes, quantidade, iLvl, chances, tipo de Legendary, peso de slot, pity, região-lar).
Arquivos: lê `Assets/_Project/Code/**`, `DataSource/stats.csv`, docs citados; escreve nada.
Restrições: somente leitura.
Aceite: premissas confirmadas/divergentes/ausentes; para cada tabela, colunas + chave + fonte (doc §).
Verificação: n/a.
Entrega: até 400 palavras.

### T2 — Contratos e esquemas
Tarefa T2: Fixar contratos do Core e esquemas de CSV.
Objetivo: interfaces e tipos que T3–T9 implementam, e um documento de esquema que M13/M18/M28 seguem.
Contexto: 11 §3.4.2–3.4.3 (classes de definição e instância), §3.12 (assinatura `(LootContext, Rng) → ItemInstance`); 05 §6 (passos); `IModifierPoolProvider` para o passo 9; `IPityState` por herói; `LootContext{source, region, difficulty, ascension, iLvl, hero, pity, attunement, quantityBonus, roomIndex, totalRooms}`.
Arquivos: escreve os arquivos da linha T2.
Restrições: Core puro (R52); nenhum número de balanceamento em código.
Aceite: `dotnet build tools/CoreTests` sem erros; `docs/data-schemas/items.md` com uma seção por CSV (colunas, tipos, chave, exemplo, doc de origem).
Verificação: `dotnet build tools/CoreTests` → esperado: sucesso.
Entrega: até 200 palavras.

### T3 — Modelo de item
Tarefa T3: Implementar o modelo de item.
Objetivo: instâncias de item com leitura de valor correta.
Contexto: 01 §3.9.1 (tier; Roll01 uniforme; sem interpolação por iLvl), §3.9.3 (multiplicadores por slot e família; Armor implícito `4 × iLvl × MultSlot × FatorBase`, Plate 1,5 / Leather 1,0 / Vestment 0,6), §3.9.5 (upgrade); R24 (Faixa Nomeada e bandas ↑ 95–115% / ↓ 70–80% de 02); 05 §4.3 (requisito); 11 §3.4.3 (campos).
Arquivos: escreve `Assets/_Project/Code/Core/Items/Model/*.cs`, `tools/CoreTests/Items/*.cs`.
Restrições: valores calculados na leitura, nunca salvos.
Aceite: testes com os exemplos de 01 (ex.: Ember Damage T5 20–30% → Faixa Nomeada 21–34,5%) e casos de borda (iLvl 20/21, Roll01 0 e 1, upgrade +10).
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Items"` → esperado: todos passam.
Entrega: até 150 palavras.

### T4 — Gerador
Tarefa T4: Implementar o `ItemGenerator`.
Objetivo: os 12 passos de 05 §6 com as tabelas de 05.
Contexto: iLvl 05 §4.1–4.2 (inclui fórmula de Ascension, A90 = 100); quantidade §3.2 (aditiva, teto 2,0; chance de Legendary ≤ ×2 da base); raridade §5.1–5.4 com pisos (§3.1); tipo de Legendary §5.5; peso de slot §6.1 (Ring/Relic um sorteio cada); afixos §6.2; smart loot §7; região-lar ×4 / chefe ×6 e Attunement ×3 (§9.1, §9.4); Uniques ×2 se não descobertos (§6 passo 5); Roll01 com piso por dificuldade e Primal A50+ (§10.1, §10.3, chance de 01/05); modificadores de Epic via `IModifierPoolProvider` e hero-bound (R27); registro de `DropSeed` e `Source`.
Arquivos: escreve `Assets/_Project/Code/Core/Loot/Generation/*.cs`, `tools/CoreTests/Loot/Generation/*.cs`.
Restrições: um único stream `Loot`; nenhuma alocação desnecessária em loops de amostragem.
Aceite: testes por passo com fixtures; determinismo com 1.000 seeds.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Loot.Generation"` → esperado: todos passam.
Entrega: até 250 palavras — passos cobertos, onde a tabela de 05 foi ambígua.

### T5 — Pity
Tarefa T5: Implementar pity e garantias.
Objetivo: contadores por herói que alteram o gerador conforme 05 §8.
Contexto: §8.1 (soft S/hard H por dificuldade; trash não consome hard; reset com Legendary compatível; contador mantido ao trocar de dificuldade), §8.2 (pity de set Veteran+), §8.3 (peças não possuídas ×3 — o "possuído" vem de uma interface de coleção), §8.4 (primeiro abate por dificuldade por herói; primeira conclusão de região no Normal), §8.5 (Unique de chefe no 10º abate).
Arquivos: escreve `Assets/_Project/Code/Core/Loot/Pity/*.cs`, `tools/CoreTests/Loot/Pity/*.cs`.
Restrições: estado em POCO serializável sem referência a SO (para o M18).
Aceite: testes por regra.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Loot.Pity"` → esperado: todos passam.
Entrega: até 150 palavras.

### T6 — Dados: definições, importadores, regras
Tarefa T6: Criar SOs, importadores e regras de validação.
Objetivo: cada CSV do esquema do T2 vira SO por `StableId` e é validado.
Contexto: `docs/data-schemas/items.md`; classes de 11 §3.4.2 (`ItemBaseDefinition`, `ItemDefinition` com `ItemKind`, `AffixDefinition`, `AffixPool`, `RarityDefinition`, `LootTableDefinition`); regras de 11 §3.15.2 aplicáveis a itens (estrutura R25, pool por slot, Faixa Nomeada 70–115%, primários, afinidades, `minDropILvl`, referências de modificador como warning até existir `DataSource/modifiers.csv`).
Arquivos: escreve os arquivos da linha T6.
Restrições: um importador/regra por arquivo; não editar o registro central de M02.
Aceite: importadores rodam com CSVs vazios só com cabeçalho; regras com testes EditMode de caso válido/inválido.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "Eclipse.Tests.EditMode.DataPipeline.Items" -testResults Logs/editmode-m12-t6.xml -logFile Logs/editmode-m12-t6.log` → esperado: `failed="0"`.
Entrega: até 200 palavras.

### T7 — Tabelas de sistema
Tarefa T7: Cadastrar as tabelas de sistema.
Objetivo: CSVs completos e fiéis aos docs.
Contexto: 01 §3.9.2 (todas as linhas por tier, incluindo Status Potency e Infusions), §3.9.3; 02 §3.4–3.9 (bases de todos os slots: as 5 armas do Kael e a Warbrand, as 3 famílias de armadura nos 5 slots, Rings, Amulets, Relics; pool ✓/★/— e restrições; Infusions); 05 §3.1, §3.2, §4.1, §5.2–5.5, §6.1, §8.1, §9.1.
Arquivos: escreve os CSVs da linha T7.
Restrições: copiar valores, não reinterpretar; cada linha com coluna `source_ref` (ex.: `01§3.9.2`).
Aceite: import e validação com 0 erros.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data-m12-t7.log` → esperado: exit 0, 0 erros.
Entrega: até 150 palavras — contagem de linhas por CSV.

### T8 — Itens nomeados
Tarefa T8: Cadastrar K1–K16 e U1–U19.
Objetivo: 35 itens nomeados com base, raridade, `ItemKind`, efeito (texto + referência de efeito a implementar no M13), 4/5 afixos fixos com banda, 3 opções de Afixo Aberto, 2 modificadores (IDs de 03; `SIG-*` em Uniques que têm Signature), `fonteUnique`, `minDropILvl`, lore.
Contexto: 02 §3.1 (anatomia), §3.9.1 (abreviações), §3.12.1 (Kael), §3.12.5 (universais), §3.12.6 (fontes); R25.
Arquivos: escreve `DataSource/items_named.csv`, `DataSource/items_named_affixes.csv`.
Restrições: efeitos especiais ficam como dados descritivos + `effect_id`; nenhuma lógica nesta tarefa.
Aceite: 35 itens importados; validação sem erros (referências de modificador podem gerar warning).
Verificação: mesmo comando do T7 → esperado: exit 0, 0 erros.
Entrega: até 150 palavras.

### T9 — Testes de integração e distribuição
Tarefa T9: Validar gerador com dados reais e distribuições.
Objetivo: provar os critérios de saída 2–5.
Contexto: EditMode: importar CSVs reais, gerar itens em vários contextos (fontes × dificuldades × regiões) e checar invariantes (estrutura R25, pool, faixas, hero-bound, requisito). Core: 200.000 amostras de trash por dificuldade contra 05 §5.4 (±0,5 p.p.), pisos de fonte, `minDropILvl`, ausência de set/Unique em Normal, pity hard nunca ultrapassado.
Arquivos: escreve `Assets/_Project/Code/Tests/EditMode/Items/*.cs`, `tools/CoreTests/Loot/Distribution/*.cs`.
Restrições: seeds fixas.
Aceite: todos passam; tempo do teste de distribuição < 30 s.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Loot"` e `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "Eclipse.Tests.EditMode.Items" -testResults Logs/editmode-m12-t9.xml -logFile Logs/editmode-m12-t9.log` → esperado: sem falhas.
Entrega: até 150 palavras com as distribuições medidas.

### T10 — Integração
Tarefa T10: Fechar o módulo.
Arquivos: `CLAUDE.md` (pasta `docs/data-schemas/`, regra "todo CSV tem `source_ref`"), `docs/ROADMAP.md`, este arquivo.
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
Esperado: exit code 0, `failed="0"`, 0 erros de validação.

## Riscos e rollback
- **Tabelas de 05 ambíguas ou inconsistentes entre si** → o executor registra; o principal decide pela leitura do doc dono e anota no Log (sem inventar número).
- **Desempenho do teste de distribuição** → amostragem em lote sem alocação; se > 30 s, reduzir amostras e alargar tolerância com justificativa estatística.
- **Referências a modificadores inexistentes** → warning neste módulo; o M10 promove a erro.
- Rollback: branch do módulo; nenhum sistema de cena depende deste módulo antes do M13.

## Atualizações de documentação
- `CLAUDE.md` §5: `Core/Items`, `Core/Loot`, `docs/data-schemas/`; regra de `source_ref` nos CSVs.
- `docs/data-schemas/items.md` (novo).
- `docs/ROADMAP.md` §7: status do M12.

## Prompt de disparo
```
Implemente o módulo M12 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M12-itemizacao-nucleo.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M12-itemizacao-nucleo.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

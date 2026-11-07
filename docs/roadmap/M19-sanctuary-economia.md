# M19 — Sanctuary e economia
Fase 4 · Onda 11 · Status: ⏳ não iniciado
Depende de: M16, M18 · Desbloqueia: M24 (com M17), Marco D (com M20) · Paralelo com: M20
Branch: modulo/M19-sanctuary-economia
Docs de referência:
- GDD v0.3 §3.3 (NPCs do Sanctuary)
- 10: §2 (decisões 8–12), §3.3 (conversão Rift Shards → Gold), §4 (retenção e Vault), §4.2 (relatório de itens removidos), §5.5 (níveis de NPC), §6 (moedas, escala R/S, Blacksmith, Enchanter, Merchant, Vault/Set Locker, saúde da economia), §9 (Codex)
- 05: §10 (qualidade de rolagem e Primal), §13 (salvage)
- 02: pool por slot (Enchant)
- 03: faixas de modificador (Refine)
- 11: §3.4.3 (`ItemInstance`: contadores e locks), §3.11 (Sanctuary), §3.13 (`profile.sav`, `vault.sav`)
- 00 §5: R31, R36, R39 (Imbue), R42

## Objetivo
Criar o hub permanente **The Sanctuary**, para onde o jogador volta depois de cada run. A cena tem estações de NPC e níveis de restauração, e os serviços econômicos de 10 §6:
- Blacksmith: salvage, upgrade de +0 a +10, Upgrade Transfer, Reforge, Rekindle;
- Enchanter: Enchant, Imbue, reroll do Afixo Aberto, Refine e conversões;
- Merchant: venda, buyback, estoque rotativo, Aether Caches, Heart Exchange;
- Vault: abas e Set Locker;
- Archivist: Codex básico;
- Rift Navigator: seleção de região.

As fórmulas de custo ficam em Core, com testes contra os exemplos numéricos de 10. Junto com M20, fecha o loop hub ↔ run do **Marco D**.

## Critérios de saída (verificáveis)
1. `Eclipse.Core.Economy` implementa as fórmulas de 10 §6 e os testes reproduzem os exemplos do doc:
   - fatores `R` e `S` (10 §6.2);
   - salvage (10 §6.3);
   - upgrade nas 4 fórmulas: Gold `30·L²·R·S`, Dust `3·L^1,5·R·S`, Cores `(L−5)×…` e Fragments em +9/+10;
   - Reforge e Rekindle;
   - Enchant/Imbue/Refine/Afixo Aberto com `400·R·S·1,2^min(n,12)` e multiplicadores 1,25 / 1,5;
   - venda (10 §6.5);
   - conversões com limite semanal de 3 Fragments (10 §6.1).

   O teste de referência é a tabela do Legendary iLvl 80 em 10 §6.3, com total +0→+10 ≈ 194.000 Gold / 7.200 Dust / 30 Cores / 3 Fragments (±1%), e "Legendary iLvl 80: 1ª tentativa 6.720 Gold; plateau ≈ 59.900" (10 §6.4).
2. Cena `Sanctuary` greybox, com estações interagíveis: Keeper (shell, completado por M20), Blacksmith, Enchanter, Merchant, Vault, Archivist, Rift Navigator, Training Grounds (porta para a cena de M14) e Challenge Altar (placeholder trancado, M29). Retorno da run → Sanctuary, com resumo e itens da fila de retenção de M16 depositados no Vault.
3. Blacksmith:
   - salvage com valores de 10 §6.3;
   - upgrade +0..+10 nunca falha, +3% por nível em atributos e implícito, modificadores e efeitos não sobem (R31), teto pelo nível do NPC (Nv 1 até +4, Nv 2 até +6…; 10 §5.5);
   - Upgrade Transfer: mesmo slot, raridade ≥, 25% do Gold;
   - Reforge (Nv 3) e Rekindle (Nv 5).
4. Enchanter:
   - Enchant (Common/Rare/Epic) com 2 opções do pool válido do slot (3 opções no Nv 3) + manter;
   - Imbue (Legendary/set/Unique), só o valor;
   - reroll do Afixo Aberto entre as 3 opções do item;
   - Refine de modificador (Nv 2), dentro da faixa de 03;
   - lock único de Enchant/Imbue, com o Afixo Aberto e o Refine em locks próprios;
   - contadores `n` persistidos no `ItemInstance`;
   - conversões de moeda;
   - desbloqueio após o chefe 2 (10 §5.5), com override de desenvolvimento até M24 existir.
5. Merchant:
   - venda e buyback dos 12 últimos;
   - estoque rotativo de 6 bases renovado a cada retorno ao Sanctuary (preço = 4 × venda);
   - Aether Caches (Nv 2), com odds provisórias de 10 §6.5 vindas de CSV;
   - Heart Exchange (Nv 3): 8 Hearts → 1 Unique do chefe já descoberto no Codex;
   - oferta semanal e Caches por afinidade como ganchos desligados (Nv 4–5; dependem de conteúdo de M28).
6. Vault:
   - 1 aba de 60 slots, até 8 abas (aba 2 grátis no Account Rank 2; custos das abas 3–8 de 10 §6.7);
   - Set Locker com 72 vagas, duplicatas indo para o Vault;
   - materiais fora dos slots;
   - filtros por slot, raridade e herói.
7. Archivist / Codex: Bestiary com tiers 1/50/250 abates, Armory com a melhor rolagem por item, Sets com peças e 3/6 ativados, Resonance Log com o primeiro disparo (dados de M11) e Lore com Memory Fragments. Cada entrada nova emite `CodexEntryAdded`, que vira Account XP em M20.
8. Níveis de NPC 1–5, com marcos e custos (5.000 / 25.000 / 100.000 / 300.000 Gold) e serviços liberados conforme a tabela de 10 §5.5. Os marcos ficam atrás de flags de progresso; até as regiões 2–5 existirem, há override de desenvolvimento.
9. Rift Navigator: escolha de região (só The Ashen Wastes por ora) e início da run. O seletor de dificuldade fica exposto como interface `IDifficultySelectionSource`; M20 liga as regras (T9 de M20).
10. Persistência: tudo grava via `SaveService` (M18), incrementando `schemaVersion` com migração quando o schema muda (CLAUDE.md). O relatório de itens removidos (M18) aparece como notificação no Sanctuary (10 §4.2).
11. PlayMode `HubLoopTest`: Sanctuary → Rift Navigator → run curta (seed, god mode, até a Antecâmara) → extração → Sanctuary → salvage, upgrade e Enchant de um item da run → save/load preserva tudo.
12. EditMode + PlayMode passam e o validador está sem erros. Nenhum arquivo de `Meta/Progression/**`, `UI/Progression/**` ou `Gameplay/Heroes/Lattice/**` (par M20) foi tocado.

## Fora de escopo
- Keeper completo (roster, desbloqueio de heróis, respec, loadouts), seleção de dificuldade e Account Rank: M20.
- Attunement (Archivist Nv 3), Set Caches e Eclipse Marks, Challenge Altar, Rift Sigils, Endless: M29.
- Transmog (Account Rank 60 / Archivist Nv 5): M29/M31.
- Arte final do hub (trilha A).

## Premissas e decisões
- Toda fórmula e valor numérico vem de 10 §6 via `DataSource/economy/*.csv`. O Core recebe tabelas e não tem números hardcoded; os testes usam os valores do doc como fixture.
- O pool válido para Enchant vem do `AffixPool` de M12 (02 é dono). O Enchant nunca produz combinação inválida (grupo repetido, primário em Ring etc.).
- Valor "aprimorado" no tooltip: a qualidade de rolagem (05 §10) é sempre calculada sobre o valor-base, antes do upgrade (10 §6.3).
- A taxa de conversão de Rift Shards em Gold no fim da run é de M16 (`RunEndResolver`). Aqui só exibimos o resumo.
- O Training Grounds pertence ao M14; aqui existe só a porta ou estação que carrega a cena dele.
- **Coordenação com M20 (paralelo):** M19 é o único a editar `Save/Dto/ProfileDto.cs` e `VaultDto.cs` nesta onda. Se M20 precisar de campos no profile, ele só edita `HeroesDto.cs` e pede o resto pelo Log. A estação Keeper e o seletor de dificuldade são integrados por M20 (T9 dele) **depois** do merge deste módulo.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Economia em Core: carteira, fórmulas de custo, conversões | execução | executor | — | `Assets/_Project/Code/Core/Economy/*.cs`, `DataSource/economy/*.csv`, `tools/CoreTests/Economy/*.cs` | ⏳ |
| T2 | Cena do Sanctuary e framework de estações, níveis de NPC e retorno da run | execução | executor | — | `Assets/_Project/Scenes/Sanctuary.unity`, `Assets/_Project/Code/Meta/Sanctuary/*.cs`, `Assets/_Project/Content/Sanctuary/*`, `Assets/_Project/Code/Save/Dto/ProfileDto.cs`, `Assets/_Project/Code/Save/Migration/*` | ⏳ |
| T3 | Blacksmith | execução | executor | T1, T2 | `Assets/_Project/Code/Meta/Services/Blacksmith/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Blacksmith/*`, `Assets/_Project/UI/Sanctuary/Blacksmith/*` | ⏳ |
| T4 | Enchanter | execução | executor | T1, T2 | `Assets/_Project/Code/Meta/Services/Enchanter/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Enchanter/*`, `Assets/_Project/UI/Sanctuary/Enchanter/*` | ⏳ |
| T5 | Merchant | execução | executor | T1, T2 | `Assets/_Project/Code/Meta/Services/Merchant/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Merchant/*`, `Assets/_Project/UI/Sanctuary/Merchant/*` | ⏳ |
| T6 | Vault e Set Locker | execução | executor | T2 | `Assets/_Project/Code/Meta/Vault/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Vault/*`, `Assets/_Project/UI/Sanctuary/Vault/*`, `Assets/_Project/Code/Save/Dto/VaultDto.cs` | ⏳ |
| T7 | Archivist / Codex e Rift Navigator | execução | executor | T2 | `Assets/_Project/Code/Meta/Codex/*.cs`, `Assets/_Project/Code/Meta/Services/RiftNavigator/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Archivist/*`, `Assets/_Project/Code/UI/Sanctuary/RiftNavigator/*`, `Assets/_Project/UI/Sanctuary/Archivist/*`, `Assets/_Project/UI/Sanctuary/RiftNavigator/*` | ⏳ |
| T8 | Testes de serviços e `HubLoopTest` | execução | executor | T3, T4, T5, T6, T7 | `Assets/_Project/Code/Tests/EditMode/Meta/*.cs`, `Assets/_Project/Code/Tests/PlayMode/Meta/*.cs` | ⏳ |
| T9 | Revisão, integração e documentação | integração | principal | T8 | `docs/roadmap/M19-sanctuary-economia.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1 ∥ T2
2. T3 ∥ T4 ∥ T5 (pastas de serviço disjuntas) e, em seguida, T6 ∥ T7. Se o limite de executores simultâneos for 3, rode T6 e T7 depois de T3–T5.
3. T8
4. T9

## Briefs

### T1 — Economia em Core
Objetivo: `Eclipse.Core.Economy` puro, com `Wallet` (moedas de 10 §6.1, incluindo Heart of [Chefe] por chefe) e fórmulas de custo de 10 §6.
Fórmulas:
- `R` (Common 1, Rare 1,5, Epic 2,5, Legendary 4) e `S = 1 + iLvl/25` (10 §6.2);
- salvage (10 §6.3), incluindo Primal com 2 + 3 Cores;
- upgrade nível a nível (10 §6.3) e Upgrade Transfer a 25% do Gold;
- Reforge `2000·R·S` Gold + 3 Cores (Legendary: 5 Cores + 1 Fragment);
- Rekindle `3000·S(novo)` + 5 Cores + 3 Fragments;
- Enchant/Imbue `400·R·S·1,2^min(n,12)`, Cores (Epic 1, Legendary 2; Common/Rare: 20·S Dust); Afixo Aberto ×1,25; Refine ×1,5 com Cores ×2;
- venda (10 §6.5);
- conversões (100 Dust → 1 Core; 1 Core → 40 Dust; 10 Cores → 1 Fragment, máx. 3/semana);
- custos das abas do Vault (10 §6.7) e dos níveis de NPC (10 §5.5).
Arquivos: escreve `Assets/_Project/Code/Core/Economy/*.cs`, `DataSource/economy/*.csv` (`rarity_factors.csv`, `salvage.csv`, `upgrade.csv`, `enchant.csv`, `merchant.csv`, `conversions.csv`, `vault_tabs.csv`, `npc_levels.csv`) e `tools/CoreTests/Economy/*.cs`.
Restrições: Core puro (R52); `long` para Gold; arredondamento documentado (os exemplos de 10 usam arredondamento para o inteiro mais próximo; confirme contra a tabela de 10 §6.3 e registre).
Aceite: os testes reproduzem as tabelas de 10 §6.3 (Legendary iLvl 80, nível a nível e total) e os números de 10 §6.4.
Verificação: `dotnet test tools/CoreTests --filter Category=Economy` → passa.
Entrega: no máximo 150 palavras, com divergências de arredondamento encontradas.

### T2 — Cena do Sanctuary e framework de estações
Objetivo:
- Cena `Sanctuary` greybox com câmera de 06 e controle do jogador de M04, mais estações para os 9 NPCs/estruturas (GDD §3.3). Cada estação é `SanctuaryStation` com `ServiceDefinition`s (11 §3.4.2), nível de NPC (10 §5.5) e gating.
- Fluxo: fim de run (M16) → carregar Sanctuary → mostrar o resumo → depositar a fila de retenção no Vault (via interface que T6 implementa; até lá, um stub) → reabastecer o estoque do Merchant (evento `ReturnedToSanctuary`).
- Notificação de itens removidos (M18 `RemovedItemsReport`).
- Níveis de NPC: `NpcRestoration` com marcos (flags de progresso) e custo em Gold; override de desenvolvimento no console de debug (M06).
Arquivos: escreve `Assets/_Project/Scenes/Sanctuary.unity`, `Assets/_Project/Code/Meta/Sanctuary/*.cs` (`SanctuaryStation.cs`, `SanctuaryController.cs`, `NpcRestoration.cs`, `ReturnFromRunFlow.cs`), `Assets/_Project/Content/Sanctuary/*` (greybox, `ServiceDefinition` assets), e `Assets/_Project/Code/Save/Dto/ProfileDto.cs` + migração (níveis de NPC, se M18 não incluiu).
Restrições: estações Keeper, Training Grounds e Challenge Altar entram como shells (sem serviço). Nada em `Meta/Progression/**`.
Aceite: PlayMode carrega o Sanctuary, interage com cada estação (abre o painel vazio ou shell) e volta de uma run simulada.
Verificação: PlayMode → passa.
Entrega: no máximo 150 palavras.

### T3 — Blacksmith
Objetivo: serviços de 10 §6.3 sobre `ItemInstance` (11 §3.4.3), com UI de comparação antes/depois e custo:
- salvage, inclusive em lote por filtro;
- upgrade com teto pelo nível do NPC;
- Upgrade Transfer;
- Reforge (Nv 3): rerola os valores dentro das faixas do iLvl, preservando identidade, upgrade e lock;
- Rekindle (Nv 5, só Legendary): eleva o iLvl ao máximo de drop atual e rerola.
Contexto: valor efetivo e upgrade conforme 11 §3.4.3 (`× (1 + 0,03 × UpgradeLevel)` em atributos e implícito; modificadores não sobem — R31). Reforge e Rekindle usam o RNG `Loot` e o `ItemGenerator` de M12 para rolar.
Arquivos: escreve `Assets/_Project/Code/Meta/Services/Blacksmith/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Blacksmith/*` e `Assets/_Project/UI/Sanctuary/Blacksmith/*`.
Restrições: custos sempre de `Core.Economy` (T1); operações atômicas (débito + mudança + save).
Aceite: os testes EditMode do critério 3 passam (em T8, mas crie aqui ao menos 1 teste de cada operação).
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T4 — Enchanter
Objetivo: serviços de 10 §6.4:
- Enchant (Common/Rare/Epic) com 2 opções novas (3 no Nv 3) + manter, sorteadas do pool válido do slot (02 via `AffixPool` de M12), sem gerar combinação inválida;
- Imbue (Legendary, set, Unique): rerola só o valor, 2 valores + manter;
- reroll do Afixo Aberto entre as 3 opções do item, com lock próprio;
- Refine de modificador (Nv 2): dentro da faixa de 03, nunca acima do máximo;
- lock único Enchant/Imbue (`EnchantedAffixIndex`);
- contadores `EnchantCount`/`ImbueCount`/`RefineCount` (11 §3.4.3);
- Lock-swap (Nv 4) e Primal Enchant (Nv 5) atrás do nível;
- conversões de moeda.
Desbloqueio após o chefe 2 (10 §5.5), com override de desenvolvimento.
Arquivos: escreve `Assets/_Project/Code/Meta/Services/Enchanter/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Enchanter/*` e `Assets/_Project/UI/Sanctuary/Enchanter/*`.
Restrições: RNG `Loot`; custo com plateau a partir de n = 12.
Aceite: testes de lock, contadores e validade do pool (1.000 Enchants sem combinação inválida).
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T5 — Merchant
Objetivo: serviços de 10 §6.5:
- venda, com preços de 10 §6.5, e buyback dos 12 últimos;
- estoque rotativo de 6 bases Common/Rare (Epic no Nv 3) no iLvl máximo de drop do jogador, renovado em `ReturnedToSanctuary`, preço = 4 × venda;
- Aether Caches (Nv 2): escolha de slot, `1.500 × S` Gold, garantia de Rare+, odds de CSV (provisórias de 10, dono 05);
- Heart Exchange (Nv 3): 8 Hearts do mesmo chefe → 1 dos 3 Uniques do chefe já descoberto no Codex. A lista de Uniques está vazia até M28: o serviço aparece desabilitado com o motivo.
- Oferta semanal (Nv 4) e Caches por afinidade (Nv 5) entram como ganchos desligados.
Arquivos: escreve `Assets/_Project/Code/Meta/Services/Merchant/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Merchant/*` e `Assets/_Project/UI/Sanctuary/Merchant/*`.
Restrições: estoque gerado com seed derivada do profile + contador de retornos (determinístico e testável).
Aceite: testes de venda, buyback e distribuição de Caches (10.000 aberturas dentro de ±1 p.p. das odds).
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T6 — Vault e Set Locker
Objetivo:
- Vault por conta: 1 aba de 60 slots, até 8 abas (aba 2 no Account Rank 2 — leia o rank via interface `IAccountRankQuery` com default 1, que M20 implementa; abas 3–8 com custos de 10 §6.7);
- Set Locker de 72 vagas, 1 por peça de set (duplicatas vão para o Vault);
- materiais não ocupam slots;
- filtros por slot, raridade e herói, e ordenação;
- implementar a interface de depósito da fila de retenção (T2).
Arquivos: escreve `Assets/_Project/Code/Meta/Vault/*.cs`, `Assets/_Project/Code/UI/Sanctuary/Vault/*`, `Assets/_Project/UI/Sanctuary/Vault/*` e `Assets/_Project/Code/Save/Dto/VaultDto.cs` + migração, se precisar de campo.
Restrições: o inventário do herói e o equipamento são de M13: reutilize a camada de item e de tooltip existente, sem duplicar.
Aceite: testes de capacidade, compra de abas, Set Locker (duplicata vai para o Vault) e persistência.
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T7 — Archivist / Codex e Rift Navigator
Objetivo:
- Codex (10 §9), com as 5 categorias e progressão:
  - Bestiary: tiers 1/50/250 abates (ouvir `EnemyKilled`);
  - Armory: item descoberto + melhor rolagem (ouvir `ItemAcquired`);
  - Sets: peças descobertas e 3/6 ativados (ouvir os eventos do SetTracker de M13);
  - Resonance Log: primeiro disparo (ouvir o evento de Resonance de M11);
  - Lore: Memory Fragments.
- Emitir `CodexEntryAdded`.
- UI de leitura simples.
- Rift Navigator: lista de regiões desbloqueadas (só Ashen Wastes) e início da run com o herói ativo. Seletor de dificuldade via `IDifficultySelectionSource`, com default "Normal" (M20 implementa).
Arquivos: escreve `Assets/_Project/Code/Meta/Codex/*.cs`, `Assets/_Project/Code/Meta/Services/RiftNavigator/*.cs` e as pastas de UI `Archivist`/`RiftNavigator` listadas na tabela.
Restrições: o Codex não dá poder (10 §2 item 12: só habilita Attunement/transmog no futuro).
Aceite: testes de progressão do Bestiary e do Armory; PlayMode inicia a run pelo Rift Navigator.
Verificação: EditMode + PlayMode → passam.
Entrega: no máximo 150 palavras.

### T8 — Testes e `HubLoopTest`
Objetivo: completar os testes EditMode dos critérios 3–8 e criar o `HubLoopTest` (PlayMode) descrito no critério 11.
Arquivos: escreve `Assets/_Project/Code/Tests/EditMode/Meta/*.cs` e `Assets/_Project/Code/Tests/PlayMode/Meta/*.cs`.
Aceite: todos passam.
Verificação: EditMode + PlayMode (CLAUDE.md §6) → passam.
Entrega: no máximo 150 palavras.

### T9 — Revisão (principal)
- Conferir as fórmulas contra 10 §6 e simular a saúde da economia (10 §6.8): razão `custo +6 / renda por run` entre 2,5 e 4 usando a tabela de renda de 10 §6.6. Registrar o resultado no Log; não é bloqueante, é sinal para 10.
- Atualizar o ROADMAP e o CLAUDE.md (`DataSource/economy/`, categoria `Economy`).
- Avisar M20 para executar a integração Keeper/dificuldade (T9 de M20).

## Verificação do módulo
```bash
dotnet test tools/CoreTests --filter Category=Economy
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
- **Conflito de schema de save com M20** → regra de posse de DTO (Premissas). Se colidir, o segundo a fazer merge rebaseia e encadeia a migração com número de versão maior.
- **Serviços que dependem de conteúdo inexistente** (Heart Exchange, Enchanter pós-chefe 2) → ganchos desabilitados com motivo visível e override de desenvolvimento.
- **Arredondamento** diverge dos exemplos de 10 → documentar a regra adotada e propor errata a 10 se o exemplo do doc estiver inconsistente.

## Atualizações de documentação
- `CLAUDE.md` §5: `DataSource/economy/`; §6: filtro `Category=Economy`.
- `docs/ROADMAP.md` §7 (e §3 Marco D, se fechar junto com M20).

## Prompt de disparo
```
Implemente o módulo M19 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M19-sanctuary-economia.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M19-sanctuary-economia.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

# M13 — Equipamento, inventário e Set Powers
Fase 2 · Onda 8 · Status: ⏳ não iniciado
Depende de: M06, M10, M12 · Desbloqueia: M14, M16 · Paralelo com: M11
Branch: modulo/M13-equipamento-sets
Docs de referência: GDD §6.1, §6.6, §11.1–11.2; 02 §3.6, §3.10–3.12 (Off Hand, compatibilidade, definição × instância, efeitos dos itens K1–K16 e U1–U19); 03 §3.2–3.3 (atribuição de genéricos, limites, prioridade por slot, motivos de inatividade); 04 §2–§3 (regras gerais), §4 (matriz de slots), §6 (Kael: Ashen Sovereign, Ruinbreaker, Eternal Bastion), §10; 01 §3.10 (Offense/Defense Index); 05 §10.2 (exibição de qualidade); 11 §3.4.3 (`HeroLoadout`), §3.8.3 (SetTracker), §3.13; 00 §5 R03, R07, R25–R29

## Objetivo
Permitir que o Kael equipe os 12 slots, com atributos e modificadores dos itens alimentando `StatBlock` e `SkillSpec`, efeitos especiais dos itens nomeados funcionando, os 3 sets do Kael (18 peças, 6 Set Powers) ativando pelo `SetTracker` (máx. 2 Set Powers, Set Focus, Keystone of the Sundering) e telas de inventário, comparação, sets e builds que expliquem a build ao jogador (GDD §11.1–11.2). O loadout e o inventário do herói são salvos.

## Critérios de saída (verificáveis)
1. `HeroLoadout` com 12 slots (Ring ×2, Relic ×2), nenhum slot bloqueado por nível (R29); validação de equipar: hero-bound (R27), categoria de arma do herói (só Main Hand), categoria de Off Hand do herói (Warbrand para Kael), nível requerido (05 §4.3); Ring/Relic aceitam o item em qualquer dos dois slots.
2. Cada item equipado registra uma fonte no `StatBlock` (afixos + implícito, com multiplicador de slot e upgrade) e entrega seus modificadores ao compilador do M10 com prioridade pela ordem de slot (Main Hand → Off Hand → Head → … → Relic 2); genéricos são atribuídos automaticamente à habilidade elegível com menos modificadores e podem ser reatribuídos fora de combate (03 §3.2).
3. `SetTracker` (11 §3.8.3, 04 §3.1): conta peças por definição distinta; Keystone conta +1 em sets com ≥ 2 peças (sem passar de 6); máx. 2 Set Powers (6pc conta como o 2º); desempate mais peças → Set Focus → primeiro a fechar 3; poderes inativos com motivo. Testes cobrem Rotas A, B, C, Parcial e as inválidas 6+3 e 3+3+3 (04 §3.2).
4. Os 3 sets do Kael (18 peças) importados e validados (6 peças em slots distintos fora de Main/Off Hand, Legendary hero-bound, até 1 afixo `*`, More dentro dos tetos de R03/04 §2.11); os 6 Set Powers funcionando em jogo (incluindo a transformação Ashen Ascension como `StateDefinition`).
5. Efeitos especiais de K1–K16 e U1–U19 funcionando; efeitos que aplicam status usam o `ApplyStatus` do sistema de efeitos (real se M11 já tiver feito merge; caso contrário stub) e são testados pelo pedido de aplicação.
6. Inventário com paper doll e grade; tooltip com raridade, atributos com % de qualidade (★ ≥ 95%, "Perfect" ≥ 99% — 05 §10.2), Afixo Aberto, modificadores, efeito, compatibilidade e contribuição de set; comparação com diferenças de atributos, Offense/Defense Index (01 §3.10) e linhas qualitativas de efeito/modificador/set.
7. Tela de Sets (ativos/inativos com motivo; seletor de Set Focus) e tela de Builds com as 4 fontes de poder (GDD §11.2: atributos, modificadores, Set Powers, Resonance) e a lista de modificadores com motivo de inatividade (*limite*, *duplicata*, *grupo*, *Augment*).
8. `HeroLoadout` + inventário do herói persistidos no save do M18 com migração de schema; ida e volta save/load preserva Roll01, atribuições de genéricos e Set Focus.
9. `dotnet test`, EditMode e PlayMode verdes; validador sem erros.

## Fora de escopo
- Vault, Set Locker e serviços do Sanctuary (M19); Satchel e coleta em run (M16).
- Sets e itens nomeados de Lyra, Nyx e Orun (M28).
- Status e Resonance reais (M11, em paralelo).
- Simuladores, Build Sandbox e Training Grounds (M14).
- Escolha de Augment pela Aether Lattice (M20); aqui o Augment continua vindo da fonte de debug do M10.

## Premissas e decisões
- **Premissa:** M10 entregou `SkillCompileInput`/`ModifierInstanceInput` com `Priority`, o cálculo de limites/motivos de inatividade e a regra de atribuição de genéricos (`Core/Modifiers/Assignment`); M12 entregou `ItemInstance`, leitura de valor e os SOs/CSVs de itens; M06 entregou o framework de UI Toolkit (navegação de telas, tema, tooltips base); M18 entregou a API de save com DTOs versionados e migrações encadeadas. O T1 confirma.
- **Decisão:** regras de loadout e de sets ficam no Core (`Core/Loadout`, `Core/Sets`, `Core/Comparison`), testadas por `dotnet test`; Gameplay só faz a ponte.
- **Decisão:** efeitos especiais de itens e Set Powers são `PowerSource`s compostos por stat mods, `ModifierOp` em canal próprio (Set/Item effect), triggers e estados — sem código por item, salvo `Effect` novo reutilizável.
- **Paralelo com M11:** este módulo não escreve em `Core/Status`, `Core/Resonance`, `Gameplay/Status`, `Gameplay/Resonance`, `UI/WorldStatus`, nem em `DataSource/{stats,affinities,status_effects,resonance_rules}.csv`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Mapear APIs de M06/M10/M12/M18 | investigação | investigador | — | — | ⏳ |
| T2 | Contratos de loadout/sets + fluxos de UX | design | principal | T1 | `Assets/_Project/Code/Core/{Loadout,Sets}/Contracts/*.cs`, `docs/ux/M13-inventario-builds.md` | ⏳ |
| T3 | Core Loadout: slots, validação, fontes de stat, provedor de modificadores | execução | executor | T2 | `Assets/_Project/Code/Core/Loadout/*.cs`, `tools/CoreTests/Loadout/*.cs` | ⏳ |
| T4 | Core Sets: `SetTracker` completo | execução | executor | T2 | `Assets/_Project/Code/Core/Sets/*.cs`, `tools/CoreTests/Sets/*.cs` | ⏳ |
| T5 | Dados de sets: SOs, importadores, regras, CSVs dos 3 sets do Kael | execução | executor | T2 | `Assets/_Project/Code/Data/Sets/*.cs`, `Assets/_Project/Code/Editor/DataPipeline/Importers/{Set,SetPiece,SetPower}TableImporter.cs`, `Assets/_Project/Code/Editor/Validation/Rules/SetRules.cs`, `DataSource/{sets,set_pieces,set_powers}.csv` | ⏳ |
| T6 | Ponte Gameplay: `EquipmentComponent`, recompilação, `PowerSource` runtime | execução | executor | T3, T4, T5 | `Assets/_Project/Code/Gameplay/Equipment/*.cs` | ⏳ |
| T7 | Set Powers do Kael (6) | execução | executor | T6 | `Assets/_Project/Code/Gameplay/SetPowers/Kael/*.cs`, `Assets/_Project/Content/Heroes/Kael/States/*`, `Assets/_Project/Code/Tests/PlayMode/SetPowers/Kael/*.cs` | ⏳ |
| T8 | Efeitos especiais de K1–K16 e U1–U19 | execução | executor | T6 | `Assets/_Project/Code/Gameplay/ItemEffects/{Kael,Universal}/*.cs`, `Assets/_Project/Code/Tests/PlayMode/ItemEffects/*.cs` | ⏳ |
| T9 | UI de inventário, tooltip e comparação (+ `Core/Comparison`) | execução | executor | T3 | `Assets/_Project/Code/Core/Comparison/*.cs`, `tools/CoreTests/Comparison/*.cs`, `Assets/_Project/Code/UI/{Inventory,Tooltip}/*.cs`, `Assets/_Project/UI/{Inventory,Tooltip}/*.{uxml,uss}` | ⏳ |
| T10 | UI de Sets e Builds | execução | executor | T4, T6 | `Assets/_Project/Code/UI/{Sets,Builds}/*.cs`, `Assets/_Project/UI/{Sets,Builds}/*.{uxml,uss}` | ⏳ |
| T11 | Persistência de loadout e inventário do herói | execução | executor | T3 | `Assets/_Project/Code/Save/Dto/{HeroLoadoutDto,HeroInventoryDto}.cs`, `Assets/_Project/Code/Save/Migrations/<próxima versão>_Loadout.cs`, `Assets/_Project/Code/Tests/EditMode/Save/LoadoutRoundTripTests.cs` | ⏳ |
| T12 | Testes PlayMode de fluxo de equipar e rotas de set | execução | executor | T7, T8, T9, T10 | `Assets/_Project/Code/Tests/PlayMode/Equipment/*.cs` | ⏳ |
| T13 | Integração, verificação final e documentação | revisão | principal | T11, T12 | `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T4 ∥ T5
4. T6 ∥ T9 ∥ T11
5. T7 ∥ T8 ∥ T10
6. T12
7. T13

## Briefs

### T1 — Mapear APIs
Tarefa T1: Confirmar as APIs consumidas.
Objetivo: premissas confirmadas com arquivo:linha.
Contexto: ver "Premissas e decisões". Também: como o `StatBlock` registra fontes (`AddSource/RemoveSource`), como o compilador recebe prioridade e devolve motivos de inatividade, como o framework de UI do M06 abre telas e mostra tooltips, como o M18 declara DTOs e migrações, e se o M11 já fez merge (`ApplyStatus` real ou stub).
Arquivos: lê `Assets/_Project/Code/**`; escreve nada.
Restrições: somente leitura.
Aceite: tabela premissa → situação → evidência.
Verificação: n/a.
Entrega: até 300 palavras.

### T2 — Contratos e UX
Tarefa T2: Fixar contratos do Core e fluxos das telas.
Objetivo: tipos para T3–T11 e um documento de UX com wireframes em texto.
Contexto: `HeroLoadout` (11 §3.4.3: 12 slots → `Guid`, atribuições de genéricos, Augment ativo, Set Focus, timestamp de "3 peças"); `SetTracker` (11 §3.8.3); comparação (GDD §11.1; 01 §3.10); Builds (GDD §11.2). Fluxos: equipar/desequipar, comparar ao passar o mouse, reatribuir genérico (só fora de combate), escolher Set Focus.
Arquivos: escreve os arquivos da linha T2.
Restrições: Core puro (R52).
Aceite: `dotnet build tools/CoreTests` sem erros; UX cobre os 7 itens de GDD §11.1 e as 4 fontes de §11.2.
Verificação: `dotnet build tools/CoreTests` → esperado: sucesso.
Entrega: até 200 palavras.

### T3 — Core Loadout
Tarefa T3: Implementar regras de loadout.
Objetivo: equipar/desequipar com validação e produção de fontes de stat e de modificadores.
Contexto: 02 §3.10 (compatibilidade), R27 (hero-bound), R29 (sem bloqueio por nível), 05 §4.3 (requisito); valores via modelo do M12 (multiplicador de slot, upgrade); modificadores → `ModifierInstanceInput` com `Priority` pela ordem de slot de 03 §3.3; atribuição automática de genéricos usando a regra do M10; evento `EquipmentChanged`.
Arquivos: escreve `Assets/_Project/Code/Core/Loadout/*.cs`, `tools/CoreTests/Loadout/*.cs`.
Restrições: sem lógica de UI; nenhuma referência a SO (usa dados Core do M12).
Aceite: testes para cada validação, prioridade e atribuição.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Loadout"` → esperado: todos passam.
Entrega: até 150 palavras.

### T4 — Core Sets
Tarefa T4: Implementar o `SetTracker`.
Objetivo: decidir quais Set Powers estão ativos para qualquer loadout.
Contexto: 11 §3.8.3 e 04 §2–§3.4: peça distinta = `ItemDefinition` distinta; Keystone +1 para sets com ≥ 2 peças, sem passar de 6; máx. 2 poderes, 6pc conta como o 2º; desempate mais peças → Set Focus → primeiro a fechar 3; diff Grant/Revoke; remover peça desativa no fim do frame.
Arquivos: escreve `Assets/_Project/Code/Core/Sets/*.cs`, `tools/CoreTests/Sets/*.cs`.
Restrições: Core puro.
Aceite: testes das rotas A, B, C, Parcial, 6+3 e 3+3+3 (04 §3.2), Keystone em B e C, desempates.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Sets"` → esperado: todos passam.
Entrega: até 150 palavras.

### T5 — Dados de sets
Tarefa T5: Cadastrar e validar os 3 sets do Kael.
Objetivo: 3 sets, 18 peças e 6 Set Powers como dados.
Contexto: 04 §6.1–6.3 (peças: slot, 4 fixos, assinatura ★, Afixo Aberto com 3 opções, 2 IDs de modificador de 03; poderes 3pc/6pc com condições e valores fixos), §2 (regras 7, 9–11), §4 (matriz de slots); 05 §9.1 (região-lar); estrutura R25 e esquema de `docs/data-schemas/items.md` (M12) — estender o documento com as tabelas de set. Validação: 6 peças em slots distintos fora de Main/Off Hand; Legendary hero-bound; ≤ 1 afixo `*` por peça; More declarado ≤ +30% sustentado (04 §2.11, R03); `EchoRecursion1x` só em Thousand Echoes 6pc.
Arquivos: escreve os arquivos da linha T5 e a seção nova em `docs/data-schemas/items.md`.
Restrições: um importador/regra por arquivo; poderes descritos por `power_id` + parâmetros (lógica no T7).
Aceite: import + validação com 0 erros.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data-m13.log` → esperado: exit 0, 0 erros.
Entrega: até 150 palavras.

### T6 — Ponte Gameplay
Tarefa T6: Ligar loadout ao herói em cena.
Objetivo: equipar em runtime atualiza `StatBlock`, recompila `SkillSpec`s no próximo frame e concede/revoga `PowerSource`s (itens e sets).
Contexto: 11 §3.5.3 (invalidação por `StatsChanged`), §3.6.2 (canais Item/Set), §3.8.3 (`StatBlock.Add/RemoveSource`, `GameEvents.SetBonusChanged`), estados de transformação via `HeroStateMachine` (11 §3.8.3).
Arquivos: escreve `Assets/_Project/Code/Gameplay/Equipment/*.cs`.
Restrições: troca de vários itens no mesmo frame = uma recompilação; Gameplay não referencia UI.
Aceite: PlayMode: equipar Ashfang altera o dano do Heavy e adiciona KAE-CS-01 ao Cleaving Strike.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Equipment.Bridge" -testResults Logs/playmode-m13-t6.xml -logFile Logs/playmode-m13-t6.log` → esperado: `failed="0"`.
Entrega: até 150 palavras.

### T7 — Set Powers do Kael
Tarefa T7: Implementar os 6 Set Powers do Kael.
Objetivo: Ashen Ascension/Sovereign's Wrath (Ashen Sovereign), os dois poderes de Ruinbreaker e os dois de Eternal Bastion funcionando conforme 04 §6.
Contexto: 04 §6.1–6.3; Execute (R08), Vulnerability (R12; Fracture não existe mais), Parry (R20), Generation (R07); transformação como `StateDefinition` com mods condicionais `while state active`.
Arquivos: escreve os arquivos da linha T7.
Restrições: reutilizar ops/efeitos existentes; número só dos CSVs.
Aceite: 1 teste PlayMode por poder (ativação, efeito, desativação ao remover peça).
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.SetPowers.Kael" -testResults Logs/playmode-m13-t7.xml -logFile Logs/playmode-m13-t7.log` → esperado: `failed="0"`.
Entrega: até 200 palavras.

### T8 — Efeitos de itens nomeados
Tarefa T8: Implementar os efeitos especiais de K1–K16 e U1–U19.
Objetivo: cada `effect_id` de `DataSource/items_named.csv` produz o efeito descrito em 02 §3.12.1/§3.12.5.
Contexto: tetos de More (R03: Legendary ≤ 15%, Unique ≤ 40%); Keystone of the Sundering (U19) é tratado pelo `SetTracker` (T4), aqui só o registro; U8 depende de afixos de Elite (09) — implementar com a lista de afixos disponível do M07 e registrar pendência; efeitos com status usam `ApplyStatus`.
Arquivos: escreve os arquivos da linha T8.
Restrições: efeitos genéricos reutilizáveis em `ItemEffects/Universal` quando possível.
Aceite: 1 teste PlayMode por efeito (35); efeitos dependentes de M11 testados pelo pedido de aplicação.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.ItemEffects" -testResults Logs/playmode-m13-t8.xml -logFile Logs/playmode-m13-t8.log` → esperado: `failed="0"`.
Entrega: até 200 palavras — pendências por dependência.

### T9 — Inventário, tooltip e comparação
Tarefa T9: Construir as telas de inventário e comparação.
Objetivo: o jogador entende por que um item é melhor para a build atual (GDD §11.1).
Contexto: paper doll de 12 slots + grade; tooltip (raridade, atributos com % de qualidade e ★/Perfect de 05 §10.2, Afixo Aberto, modificadores com ativo/inativo, efeito, compatibilidade/hero-bound, contribuição de set); `Core/Comparison` calcula Offense Index e Defense Index (EHP) de 01 §3.10 com o `DamageResolver` (alvo de referência Armor 25%, Res 20%); diferenças coloridas; efeitos/modificadores/sets como linhas qualitativas. Fluxo de `docs/ux/M13-inventario-builds.md`.
Arquivos: escreve os arquivos da linha T9.
Restrições: UI só lê estado e envia comandos (equipar) ao Gameplay; assets de UI gratuitos/greybox.
Aceite: dotnet tests do `Core/Comparison`; PlayMode smoke abrindo inventário, comparando e equipando.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Comparison"` e `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.UI.Inventory" -testResults Logs/playmode-m13-t9.xml -logFile Logs/playmode-m13-t9.log` → esperado: sem falhas.
Entrega: até 150 palavras + captura de tela.

### T10 — Sets e Builds
Tarefa T10: Construir as telas de Sets e de Builds.
Objetivo: mostrar Set Powers ativos/inativos e as 4 fontes de poder (GDD §11.2).
Contexto: 04 §3.1 itens 3–4 (motivo "Limite de Set Powers atingido", Set Focus); 03 §3.3 (motivos de inatividade de modificadores, fixação manual de prioridade); reatribuição de genéricos só fora de combate (03 §3.2); seção Resonance mostra os pares de afinidade da build (dados de M11 se disponíveis, senão a seção fica vazia com rótulo).
Arquivos: escreve os arquivos da linha T10.
Restrições: idem T9.
Aceite: PlayMode smoke trocando Set Focus e reatribuindo um genérico.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.UI.Builds" -testResults Logs/playmode-m13-t10.xml -logFile Logs/playmode-m13-t10.log` → esperado: `failed="0"`.
Entrega: até 150 palavras + captura de tela.

### T11 — Persistência
Tarefa T11: Salvar loadout e inventário do herói.
Objetivo: `heroes.sav` (11 §3.13) guarda loadout, atribuições de genéricos, Set Focus, timestamp de 3 peças e itens do herói.
Contexto: API e convenções de DTO/migração do M18; IDs estáveis (nunca referência de asset); `ItemInstance` já serializável (M12/M18).
Arquivos: escreve os arquivos da linha T11.
Restrições: migração encadeada a partir da versão atual do schema; não alterar DTOs existentes do M18 além do necessário.
Aceite: round-trip idêntico; save de versão anterior migra sem perda.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "Eclipse.Tests.EditMode.Save" -testResults Logs/editmode-m13-t11.xml -logFile Logs/editmode-m13-t11.log` → esperado: `failed="0"`.
Entrega: até 120 palavras.

### T12 — Testes de fluxo
Tarefa T12: Testar fluxos completos.
Objetivo: equipar Rotas A, B (Ashen Sovereign 6) e C (Ashen Sovereign 3 + Ruinbreaker 3) e verificar Set Powers, mudanças no dano e na UI.
Arquivos: escreve `Assets/_Project/Code/Tests/PlayMode/Equipment/*.cs`.
Restrições: seed fixa; itens gerados pelo `ItemGenerator` do M12 com contexto fixo.
Aceite: todos passam.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Equipment" -testResults Logs/playmode-m13-t12.xml -logFile Logs/playmode-m13-t12.log` → esperado: `failed="0"`.
Entrega: até 120 palavras.

### T13 — Integração
Tarefa T13: Fechar o módulo.
Arquivos: `CLAUDE.md` (pastas novas, regra de `PowerSource`), `docs/ROADMAP.md`, este arquivo.
Verificação: seção abaixo.
Entrega: resumo para o usuário; lista de efeitos que dependem do M11 e foram verificados após o merge dele.

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
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
Esperado: exit code 0, `failed="0"`, 0 erros de validação.

## Riscos e rollback
- **Merge com M11 (paralelo)** → efeitos de item e Set Powers dependem só do contrato `ApplyStatus`; o segundo módulo a terminar faz merge da base e roda a suíte completa antes do PR.
- **Recompilação em cascata ao trocar vários itens** → coalescer no próximo frame (11 §3.5.3); teste de performance simples no T12.
- **Comparação engana o jogador (índices ignoram efeitos)** → linhas qualitativas obrigatórias para efeito/modificador/set (01 §3.10).
- Rollback: branch do módulo; o herói continua jogável sem equipamento.

## Atualizações de documentação
- `CLAUDE.md` §5: `Core/{Loadout,Sets,Comparison}`, `Gameplay/{Equipment,SetPowers,ItemEffects}`, `UI/{Inventory,Tooltip,Sets,Builds}`, `docs/ux/`.
- `docs/data-schemas/items.md`: tabelas de set.
- `docs/ROADMAP.md` §7: status do M13.

## Prompt de disparo
```
Implemente o módulo M13 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M13-equipamento-sets.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M13-equipamento-sets.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

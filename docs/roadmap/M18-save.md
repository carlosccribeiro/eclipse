# M18 — Save system
Fase 3 · Onda 7 · Status: ⏳ não iniciado
Depende de: M12 · Desbloqueia: M16 (suspend-save e registro da última run), M19, M20 · Paralelo com: M10
Branch: modulo/M18-save
Docs de referência:
- 11 §2 itens 12 e 17 (formato e suspend-save); 11 §3.4.3 (instâncias: `ItemInstance`, `HeroLoadout`, `RunState`); 11 §3.13 (arquivos, formato, escrita atômica, migrações, IDs, Steam Cloud)
- 10 §4 (o que é retido), 10 §4.1 (última run morta para o evento Echo of a Fallen Eclipsed), 10 §4.2 (item removido → Aether Dust), 10 §6.1 (moedas), 10 §6.7 (Vault, Set Locker, loadouts)
- 05 §8 (estado de pity por herói)
- 00 §5 R31 (Primal), R36 (moedas), R46 (compensação)
- CLAUDE.md §4

## Objetivo
Persistir conta, heróis, Vault e run suspensa de forma robusta a crash, versionada e depurável:
- formato JSON (Newtonsoft) + GZip, com header `{magic, schemaVersion, CRC32}`;
- escrita atômica, com 3 backups;
- migrações encadeadas;
- IDs só por `StableId`/`Guid`;
- suspend-save gravado na entrada de cada sala e apagado ao carregar.

O módulo entrega a API usada por M16, M19 e M20, mas não liga gameplay a ela; quem faz essa ligação é M16.

## Critérios de saída (verificáveis)
1. Os cinco arquivos de 11 §3.13 existem como DTOs com `schemaVersion`: `profile.sav`, `heroes.sav`, `vault.sav`, `run.sav` e `settings.json`. O conteúdo cobre todos os campos listados em 11 §3.13 e §3.4.3.
2. Round-trip de `ItemInstance` (M12) preserva todos os campos de 11 §3.4.3 bit a bit, incluindo `Roll01` (float exato), `DropSeed`, `Source`, contadores e flags (Primal e Anchored). O teste roda com 1.000 itens gerados por `ItemGenerator` de seeds fixas.
3. Escrita atômica: num teste que interrompe a escrita após o `.tmp`, o arquivo principal continua íntegro. Ficam no máximo 3 `.bak` válidos, com rotação.
4. Corrupção: um CRC inválido no principal faz o load cair para o `.bak` mais recente válido e emitir `SaveRecoveredEvent`. Se todos estiverem corrompidos, o resultado é `SaveLoadResult.Corrupted`, sem exceção.
5. Migração encadeada v1 → v2 → v3, com migrações de exemplo nos testes, aplicada no load. `IdRemapTable` resolve ID renomeado e item removido, que é convertido em Aether Dust conforme 10 §4.2 (`salvage × 3`, + 2 Sundered Cores se Legendary; upgrades devolvidos).
6. Suspend-save: `SaveService.WriteSuspend(RunState)` e `TryResumeSuspend(out RunState)`. Uma resume bem-sucedida **apaga** o `run.sav` (anti save-scumming, 11 §2 item 17).
7. Registro da última run morta por região e por herói (10 §4.1), com API de gravar, consumir e substituir.
8. Estado de pity por herói persiste e round-trip conforme 05 §8 / 11 §3.13 (`heroes.sav`).
9. Escrita fora da main thread: a serialização faz snapshot na main thread e o IO fica em background. Nenhum `File.*` síncrono na main thread durante o gameplay (teste de API/inspeção).
10. `dotnet test tools/CoreTests --filter Category=Save` e os EditMode tests passam. Nenhum arquivo do par M10 (`Code/Gameplay/Skills/**`, `Code/Core/Modifiers/**`) foi tocado.

## Fora de escopo
- Chamar o save a partir do gameplay (entrada de sala, fim de run, serviços do hub): M16, M19 e M20.
- Steam Cloud real (M32). Aqui só o metadado de conflito `lastWriteUtc` + contador monotônico (11 §3.13).
- UI de seleção de perfil (fica com M19/M31). O módulo assume um perfil único, com o slot já no caminho.
- Conteúdo de `settings.json` além do esqueleto (M31 completa).

## Premissas e decisões
- Assembly `Eclipse.Save` (Core, Data; 11 §3.3), com Newtonsoft via `com.unity.nuget.newtonsoft-json`. A lógica de formato, migração e DTOs é C# puro + Newtonsoft, para rodar também em `tools/CoreTests`, que ganha referência ao NuGet `Newtonsoft.Json` e passa a compilar `Code/Save/**`. Se M00 ou M01 tiverem fixado outra forma de incluir fontes no CoreTests, siga a forma existente.
- DTOs são **separados** dos tipos de domínio: mappers `Domain ↔ Dto` em `Code/Save/Mapping/`. O domínio (Core) não conhece JSON.
- O `RunState` DTO segue 11 §3.4.3 (seed, mapa, nó atual, Blessings/Curses, Rift Shards, Satchel de 30, itens com Anchor, cargas do Flask, estado do herói) com campos opcionais. M16 completa o mapper de runtime sem mudar o schema, ou incrementa `schemaVersion` com migração.
- Caminho: `Application.persistentDataPath/profiles/<profileId>/`. Em testes, o diretório é injetado por `ISaveStorage` (implementações `FileSaveStorage` e `MemorySaveStorage`).
- CRC32 implementado em Core/Save (sem dependência externa). Magic `ECLP`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Formato de arquivo: header, CRC32, GZip, escrita atômica, backups | execução | executor | — | `Assets/_Project/Code/Save/Format/*.cs`, `Assets/_Project/Code/Save/Storage/*.cs` | ⏳ |
| T2 | DTOs dos 5 arquivos + mappers de domínio | execução | executor | — | `Assets/_Project/Code/Save/Dto/*.cs`, `Assets/_Project/Code/Save/Mapping/*.cs` | ⏳ |
| T3 | Migrações, `IdRemapTable` e compensação de item removido | execução | executor | T2 | `Assets/_Project/Code/Save/Migration/*.cs` | ⏳ |
| T4 | `SaveService`: API pública, IO assíncrono, suspend-save, última run morta | execução | executor | T1, T2, T3 | `Assets/_Project/Code/Save/SaveService.cs`, `Assets/_Project/Code/Save/SaveEvents.cs`, `Assets/_Project/Code/Save/Eclipse.Save.asmdef` (se ainda não existir) | ⏳ |
| T5 | Testes: round-trip, atomicidade, corrupção, migração, suspend | execução | executor | T4 | `tools/CoreTests/Save/*.cs`, `tools/CoreTests/CoreTests.csproj` (só a referência ao Newtonsoft e às fontes de Save), `Assets/_Project/Code/Tests/EditMode/Save/*.cs` | ⏳ |
| T6 | Ferramentas de editor para save | execução | executor | T4 | `Assets/_Project/Code/Editor/Save/*.cs` | ⏳ |
| T7 | Revisão, integração e documentação | integração | principal | T5, T6 | `docs/roadmap/M18-save.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1 ∥ T2
2. T3
3. T4
4. T5 ∥ T6
5. T7

## Briefs

### T1 — Formato de arquivo e armazenamento
Objetivo: ler e gravar bytes de save com header `{magic "ECLP", schemaVersion:int, payloadLength, CRC32}` seguido do payload GZip, de forma atômica e com 3 backups (11 §3.13).
Contexto: escrita atômica = grava `<file>.tmp` → flush/`FileStream.Flush(true)` → renomeia para `<file>`; antes de substituir, rotaciona o arquivo atual para `.bak1`, `.bak1` para `.bak2` e `.bak2` para `.bak3`. Leitura: valida magic e CRC; se falhar, tenta `.bak1..3` em ordem.
Arquivos: escreve `Assets/_Project/Code/Save/Format/SaveHeader.cs`, `Crc32.cs`, `SaveCodec.cs` (GZip + header) e `Assets/_Project/Code/Save/Storage/ISaveStorage.cs`, `FileSaveStorage.cs`, `MemorySaveStorage.cs` (esta permite simular falha após o `.tmp`).
Restrições: sem `UnityEngine` (caminho injetado). Use `System.IO.Compression`. Nenhuma exceção vaza: resultados tipados (`SaveReadResult`).
Aceite: codec round-trip; CRC detecta 1 byte alterado; a rotação mantém no máximo 3 `.bak`.
Verificação: `dotnet build tools/CoreTests` → 0 erros (o csproj é ajustado em T5; até lá, compile pelo Unity: EditMode run sem testes novos → compilação limpa).
Entrega: no máximo 150 palavras.

### T2 — DTOs e mappers
Objetivo: DTOs serializáveis por Newtonsoft para `profile`, `heroes`, `vault`, `run` e `settings`, com todos os campos de 11 §3.13 e §3.4.3, mais mappers para os tipos de domínio existentes.
Contexto:
- `ItemInstance`, `RolledAffix` e `RolledModifier` vêm de M12 (`Assets/_Project/Code/Core/Items/*`): leia antes.
- Moedas permanentes (10 §6.1): Gold, Aether Dust, Sundered Cores, Pillar Fragments, Eclipse Marks, Memory Fragments e Heart of [Chefe] por chefe. Rift Shards só no `RunState`.
- Vault: abas de 60 slots (até 8) e Set Locker de 72 vagas (10 §6.7).
- Loadouts: 5 por herói, até 8 (10 §6.7, §5.2); o formato é o `HeroLoadout` de 11 §3.4.3.
- Pity por herói (05 §8): contadores de Legendary e de set, e peças faltantes.
- Última run morta por região × herói (10 §4.1): build, Rift Shards perdidos e itens convertidos com as rolagens originais.
- NPC levels 1–5 (10 §5.5), Account Rank/AXP (10 §5.2), desbloqueios de heróis, regiões e dificuldades (10 §5.3, §7.1), Codex (10 §9), oferta semanal do Merchant (10 §6.5), pity de Unique de chefe (05 §8.5).
Arquivos: escreve `Assets/_Project/Code/Save/Dto/` (`ProfileDto.cs`, `HeroesDto.cs`, `VaultDto.cs`, `RunStateDto.cs`, `SettingsDto.cs`, `ItemInstanceDto.cs`, `CommonDtos.cs`) e `Assets/_Project/Code/Save/Mapping/ItemMapping.cs` (só `ItemInstance` nesta tarefa; os outros mappers ficam como stubs com TODO apontando o módulo dono: M16 para run, M19 para profile e economia, M20 para heroes e progressão).
Restrições: só `StableId` (string) e `Guid`, nunca referência de asset. `schemaVersion` em cada raiz. Floats com precisão total (`FloatFormatHandling` e `"R"` / round-trip format).
Aceite: os DTOs cobrem a lista de campos acima (checklist no relatório) e o mapper de item faz round-trip em memória.
Verificação: compilação limpa no Unity (EditMode run).
Entrega: no máximo 200 palavras, com checklist campo × doc.

### T3 — Migrações e remapeamento de IDs
Objetivo: `IMigration { int From; JObject Apply(JObject) }` encadeado no load, mais `IdRemapTable`: ID antigo → novo, ou "removido".
Contexto: item com `DefinitionId` sem definição → convertido em Aether Dust = valor de salvage × 3 (10 §6.3: Common 2×S, Rare 5×S, Epic 15×S, Legendary 40×S, com S = 1 + iLvl/25 — 10 §6.2); se Legendary, + 2 Sundered Cores; upgrades devolvidos em 100% do Gold e do Dust (10 §4.2). A notificação ao jogador é um `RemovedItemsReport` retornado no load (a UI fica para M19). As fórmulas de salvage pertencem ao doc 10, e M19 vai implementá-las em Core/Economy: aqui use uma interface `ISalvageValueProvider` com implementação default baseada em 10 §6.3, e M19 a substitui.
Arquivos: escreve `Assets/_Project/Code/Save/Migration/` (`IMigration.cs`, `MigrationRunner.cs`, `IdRemapTable.cs`, `RemovedItemCompensator.cs`, `ISalvageValueProvider.cs`).
Restrições: migrações puras sobre `JObject`; nunca removem dados sem registro em relatório.
Aceite: cadeia aplicada na ordem e idempotente quando a versão já é a atual.
Verificação: compilação limpa; os testes vêm em T5.
Entrega: no máximo 150 palavras.

### T4 — SaveService
Objetivo: fachada usada pelo jogo para todas as operações de save.
API:
- `LoadAll()`
- `SaveProfile/Heroes/Vault/Settings(...)`
- `WriteSuspend(RunStateDto)`, `TryResumeSuspend(out RunStateDto)` (apaga o `run.sav` em caso de sucesso), `DiscardSuspend()`
- `RecordDeadRun(regionId, heroId, DeadRunDto)`, `TryConsumeDeadRun(...)`
- eventos `SaveRecoveredEvent`, `SaveFailedEvent` e `MigrationsAppliedEvent`, publicados no Event Bus de M01
Contexto: IO assíncrono, com snapshot (serialização para `byte[]`) na thread chamadora e escrita numa fila de background de 1 worker; `Flush()` síncrono no quit (11 §3.13; CLAUDE.md §4). Metadado de Cloud: `lastWriteUtc` + contador monotônico por arquivo.
Arquivos: escreve `Assets/_Project/Code/Save/SaveService.cs` e `SaveEvents.cs`; cria `Eclipse.Save.asmdef` só se M00 não tiver criado.
Restrições: `SaveService` não referencia `UnityEngine` exceto num adaptador `UnitySavePaths` (pode ficar em `Code/Save/Unity/` se precisar de `Application.persistentDataPath`).
Aceite: todas as operações funcionam com `MemorySaveStorage`.
Verificação: compilação limpa no Unity e no CoreTests.
Entrega: no máximo 150 palavras.

### T5 — Testes
Objetivo: cobrir os critérios 2–9.
Testes:
- round-trip de 1.000 `ItemInstance` gerados (M12 `ItemGenerator`, seeds 1..1000);
- simulação de falha após `.tmp`;
- CRC alterado → `.bak`;
- todos corrompidos → `Corrupted`;
- migração v1→v3 (migrações de teste);
- ID removido → compensação conforme 10 §4.2;
- resume apaga o suspend;
- registro, consumo e substituição da última run morta;
- pity por herói;
- `SaveService` não chama IO síncrono na API pública (teste com storage que registra a thread).
Arquivos: escreve `tools/CoreTests/Save/*.cs`, edita `tools/CoreTests/CoreTests.csproj` **apenas** para incluir `Newtonsoft.Json` (NuGet, versão igual à do pacote Unity) e as fontes de `Assets/_Project/Code/Save/**` (exceto `Unity/`), e escreve `Assets/_Project/Code/Tests/EditMode/Save/*.cs`.
Restrições: `[Category("Save")]`. Falhas de produção são reportadas, não mascaradas.
Aceite: todos passam.
Verificação: `dotnet test tools/CoreTests --filter Category=Save` → passa; EditMode (CLAUDE.md §6) → passa.
Entrega: no máximo 150 palavras.

### T6 — Ferramentas de editor
Objetivo: menu `Eclipse/Save`:
- Open Save Folder;
- Dump Save to JSON (descompacta para `Logs/save-dump/`);
- Corrupt Main File (teste manual de recuperação);
- Delete Suspend;
- Validate Saves (roda o load e lista migrações e remaps aplicados).
Arquivos: escreve `Assets/_Project/Code/Editor/Save/SaveDebugMenu.cs`.
Restrições: editor-only.
Aceite: os itens de menu aparecem e o dump gera JSON legível.
Verificação: compilação limpa; EditMode que chama `SaveDebugMenu.DumpToJson(memoryStorage, tempDir)` → arquivos gerados.
Entrega: no máximo 100 palavras.

### T7 — Revisão (principal)
Conferir o checklist de campos de T2 contra 11 §3.13 / 10 §4 e §6.7 / 05 §8. Rodar as verificações. Atualizar o Log e o ROADMAP. No CLAUDE.md §6, acrescentar `--filter Category=Save`; em §9, registrar a pegadinha de float/round-trip, se houver.

## Verificação do módulo
```bash
dotnet test tools/CoreTests --filter Category=Save
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
`git diff --stat` sem arquivos de `Code/Gameplay/Skills/**` nem de `Code/Core/Modifiers/**` (par M10).

## Riscos e rollback
- **Newtonsoft no CoreTests** com versão divergente da Unity → fixar a mesma versão; se houver diferença de comportamento, os EditMode tests são o canônico.
- **Precisão de float** no JSON → usar formato round-trip; o teste bit a bit do critério 2 detecta.
- **Rename atômico no Windows** com antivírus segurando o handle → retry com backoff de até 3×; se falhar, mantém o `.tmp` e emite `SaveFailedEvent` (registrar em CLAUDE.md §9).
- Schema mudará em M16, M19 e M20 → toda mudança incrementa `schemaVersion` com migração e teste (regra a registrar no CLAUDE.md).

## Atualizações de documentação
- `CLAUDE.md` §4 ou §7: "Mudou um DTO de save ⇒ incremente `schemaVersion` e adicione uma `IMigration` com teste."
- `CLAUDE.md` §6: filtro de categoria `Save`.
- `docs/ROADMAP.md` §7.

## Prompt de disparo
```
Implemente o módulo M18 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M18-save.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M18-save.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

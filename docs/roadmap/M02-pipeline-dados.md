# M02 — Pipeline de dados (SO base, importador CSV, validador)
Fase 0 · Onda 2 · Status: ⏳ não iniciado
Depende de: M01 · Desbloqueia: M12 (e todo módulo que importa dados) · Paralelo com: M03 (worktree separado; pastas disjuntas)
Branch: modulo/M02-pipeline-dados
Docs de referência: 11 §2 dec. 5, 15; §3.4.1 (Definition, DefinitionRegistry, IdRemapTable, convenção de ID); §3.15 itens 1–2 (pipeline de planilhas e Data Validator); 00 §3.4 (tags), §5 R02, R48 (nomes reservados); 07 §10 (cores e identidade das afinidades); CLAUDE.md §1 (nenhum número hardcoded), §6 (comando `ImportAndValidate`).

## Objetivo
Montar o caminho único dos dados de design para o jogo: `DataSource/*.csv` (versionado) → importador por `StableId` (cria/atualiza, nunca recria) → ScriptableObjects de definição → `DefinitionRegistry` → validador com regras do contrato, executável em batchmode e no CI. Piloto: tabela de **afinidades** (6 linhas, 07).

## Critérios de saída (verificáveis)
1. `Definition` (SO abstrato, `Eclipse.Data`) implementa `Eclipse.Core.Definitions.IDefinition`; `StableId` serializado, imutável pelo inspector após criação (campo readonly no inspector com botão explícito "Rename ID" que grava em `IdRemapTable`).
2. `CsvTable` (Core, C# puro) faz parse de CSV RFC 4180 (aspas, vírgulas e quebras de linha em campo), cabeçalho obrigatório, listas `a|b|c`, células vazias, BOM UTF-8; testado em `dotnet test`.
3. Convenção de CSV documentada em `DataSource/README.md`: 1ª coluna `id` (StableId), colunas de localização `name_pt`, `name_en`, `desc_pt`, `desc_en`, enums por nome, listas com `|`, percentuais como número (`12.5` = 12,5%), comentário de fonte (`source_doc`, ex.: `07 §10`).
4. `DataPipeline.ImportAndValidate` (`Eclipse.Editor.DataPipeline`) roda pelo comando de CLAUDE.md §6, importa todas as tabelas registradas, valida e sai com **código 0** sem erros e **código ≠ 0** com erros (`EditorApplication.Exit`).
5. Importar duas vezes o mesmo CSV não altera nenhum `.asset` nem GUID (`git status` limpo após a 2ª execução); linha removida do CSV gera aviso de **órfão** (não apaga o asset).
6. Validador com as regras transversais disponíveis agora: ID único e no formato de `StableId`; tags no vocabulário de 00 §3.4 (e HitFlags fora dele — R02); chaves de localização presentes para `name`; nomes de exibição únicos por tipo e não colidindo com o registro de nomes reservados de 00 §5 R48 (lista em `DataSource/ReservedNames.csv`); referências entre definições resolvem. Cada regra tem teste EditMode com caso que passa e caso que falha.
7. Piloto: `AffinityDefinition` + `DataSource/Affinities.csv` com Ember, Frost, Storm, Void, Echo, Ruin (cores, ícone placeholder, tag de afinidade, status primário como ID textual, valores de 07 §10) → 6 assets em `Assets/_Project/Content/Affinities/` e registrados no `DefinitionRegistry`.
8. Strings de nome/descrição vão para uma String Table Collection `Definitions` (pt-BR, en-US) do Unity Localization, gerada pelo importador.
9. `ci.yml` ganha o job `data-validate` (batchmode `ImportAndValidate`) e ele passa.
10. `dotnet test tools/CoreTests` + EditMode passam; CLAUDE.md §6 confirma o comando de dados.

## Fora de escopo
- Tabelas de stats, itens, modificadores etc. (cada módulo de sistema adiciona as suas usando este pipeline).
- Regras de validação específicas de itens, sets e modificadores (R25, R03 etc.) — entram nos módulos M10, M12, M13, M28, que registram regras novas no framework daqui.
- Integração com Google Sheets (o designer exporta CSV manualmente; automação pode vir depois).

## Premissas e decisões
- O CSV é a fonte; o `.asset` é derivado. Editar valor de balanceamento direto no SO é proibido (o validador pode marcar `ImportedHash` divergente como aviso).
- `DefinitionRegistry` é um SO único em `Assets/_Project/Content/DefinitionRegistry.asset`, reconstruído no import e no pré-build; carregamento por Addressables (label `defs`) fica preparado, mas o runtime pode ler o registro direto até o M16.
- Os importadores são registrados por atributo (`[TableImporter("Affinities")]`) para que módulos futuros adicionem tabelas sem editar o `DataPipeline`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Parser `CsvTable` (Core) + testes | execução | executor | — | `Assets/_Project/Code/Core/DataTables/*.cs`, `Assets/_Project/Code/Tests/EditMode/Core/DataTables/*Tests.cs` | ⏳ |
| T2 | `Definition` SO, `DefinitionRegistry` SO, `IdRemapTable` asset | execução | executor | — | `Assets/_Project/Code/Data/Definitions/*.cs`, `Assets/_Project/Code/Editor/Definitions/*.cs` | ⏳ |
| T3 | Framework de importação + `DataPipeline.ImportAndValidate` | execução | executor | T1, T2 | `Assets/_Project/Code/Editor/DataPipeline/*.cs`, `DataSource/README.md` | ⏳ |
| T4 | Framework de validação + regras transversais | execução | executor | T3 | `Assets/_Project/Code/Editor/Validation/*.cs`, `DataSource/ReservedNames.csv`, `Assets/_Project/Code/Tests/EditMode/Data/Validation/*Tests.cs` | ⏳ |
| T5 | Tabela piloto de afinidades | execução | executor | T4 | `Assets/_Project/Code/Data/Affinities/AffinityDefinition.cs`, `Assets/_Project/Code/Editor/DataPipeline/Importers/AffinityImporter.cs`, `DataSource/Affinities.csv`, `Assets/_Project/Content/Affinities/*.asset`, `Assets/_Project/Code/Tests/EditMode/Data/Affinities/*Tests.cs` | ⏳ |
| T6 | Geração da String Table `Definitions` | execução | executor | T5 | `Assets/_Project/Code/Editor/DataPipeline/LocalizationWriter.cs`, `Assets/_Project/Settings/Localization/**` | ⏳ |
| T7 | Job `data-validate` no CI | execução | executor | T4 | `.github/workflows/ci.yml` | ⏳ |
| T8 | Revisão e integração | integração | principal | T6, T7 | `CLAUDE.md`, `docs/ROADMAP.md`, este arquivo | ⏳ |

## Ondas internas
1. T1 ∥ T2 (Core/DataTables × Data/Editor; arquivos disjuntos; T1 usa `dotnet test`, T2 usa batchmode — não concorrem pelo mesmo build)
2. T3
3. T4
4. T5 ∥ T7 (T7 só edita o YAML do CI)
5. T6
6. T8

## Briefs

Contexto comum (inclua em todos):
> PROJECT ECLIPSE é um ARPG em Unity 6.3 LTS, projeto na raiz do repositório. Assemblies: `Eclipse.Core` (C# puro, sem UnityEngine — R52), `Eclipse.Data` (ScriptableObjects de definição), `Eclipse.Editor` (ferramentas, Editor-only). `Eclipse.Core` já tem `StableId`, `IdRemapTable`, `TagMask`, `HitFlags`, `IDefinition`, `IDefinitionRegistry` (M01). Regra do projeto: números de design vêm de `DataSource/*.csv` → SOs; nada hardcoded. Código/comentários em inglês. `$UNITY_EXE` e comandos em CLAUDE.md §6; feche o Editor antes de batchmode. Não edite arquivos fora dos listados.

**T1 — CsvTable**
- Objetivo: parser de CSV puro e testado.
- Arquivos: escreve `Core/DataTables/CsvTable.cs` (`Parse(string text)`, `Rows`, `Columns`, acesso por nome de coluna, erros com linha/coluna), `Core/DataTables/CsvRow.cs` (`GetString/GetInt/GetFloat/GetBool/GetEnum<T>/GetList`, cultura invariante — ponto decimal), testes em `Tests/EditMode/Core/DataTables/`.
- Aceite: critério 2; erro claro para coluna inexistente, número inválido e enum desconhecido.
- Verificação: `dotnet test tools/CoreTests --filter FullyQualifiedName~Eclipse.Core.DataTables` → 0 falhas.
- Entrega: ≤ 100 palavras.

**T2 — Definition e registro**
- Objetivo: base de todas as definições e o registro global (11 §3.4.1).
- Arquivos: escreve `Data/Definitions/Definition.cs` (abstract SO, `[SerializeField] string stableId`, `LocalizedString displayName/description`, `Id => StableId.Parse(stableId)`, `ImportedHash` string), `Data/Definitions/DefinitionRegistry.cs` (SO que implementa `IDefinitionRegistry`; lista serializada de `Definition`; dicionário construído em `OnEnable`), `Data/Definitions/IdRemapAsset.cs` (SO que serializa pares old→new e alimenta `IdRemapTable`), `Editor/Definitions/DefinitionInspector.cs` (ID somente leitura + botão "Rename ID…" que grava no `IdRemapAsset`), `Editor/Definitions/RegistryBuilder.cs` (varre `Assets/_Project/Content/**` por `Definition`, reconstrói o registro, falha em ID duplicado).
- Aceite: critério 1; projeto compila; `RegistryBuilder.Rebuild()` testável.
- Verificação: EditMode (`$UNITY_EXE … -runTests -testPlatform EditMode …`) → 0 falhas; log de compilação sem `error CS`.
- Entrega: ≤ 120 palavras.

**T3 — Importação**
- Objetivo: framework genérico de importação por `StableId` e o ponto de entrada de batchmode.
- Arquivos: escreve `Editor/DataPipeline/TableImporterAttribute.cs`, `ITableImporter.cs` (`TableName`, `CsvPath`, `Import(CsvTable, ImportContext)`), `TableImporter<TDef>.cs` (base: encontra asset por `StableId` em `OutputFolder`, cria se não existir, atualiza campos, marca `ImportedHash`, **nunca recria** nem muda GUID, reporta órfãos), `ImportContext.cs` (log de erros/avisos, resolução de referências cruzadas em 2 passes), `DataPipeline.cs` (`public static void ImportAndValidate()` — importa todas as tabelas registradas em ordem de dependência, chama o validador, reconstrói o registro, `AssetDatabase.SaveAssets`, sai com código 0/1 em batchmode), menu `Eclipse/Data/Import & Validate`; `DataSource/README.md` com a convenção do critério 3.
- Aceite: critérios 3, 4 e 5 (verificados no T5 com a tabela piloto).
- Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → exit code 0 sem tabelas registradas; log com resumo "0 tables, 0 errors".
- Entrega: ≤ 150 palavras — API para módulos futuros registrarem tabelas.

**T4 — Validação**
- Objetivo: validador extensível e as regras transversais.
- Arquivos: escreve `Editor/Validation/IValidationRule.cs`, `ValidationRuleAttribute.cs`, `DataValidator.cs` (descobre regras por atributo, roda sobre o registro, produz `ValidationReport` com erro/aviso, arquivo e ID), regras `UniqueIdRule`, `IdFormatRule`, `TagVocabularyRule` (usa `Eclipse.Core.Tags`), `LocalizationKeyRule`, `ReservedNameRule` (lê `DataSource/ReservedNames.csv`), `ReferenceResolvesRule`; `DataSource/ReservedNames.csv` com os nomes de 00 §5 R48 e o dono de cada (`name,owner_doc`); testes em `Tests/EditMode/Data/Validation/` (uma definição de teste fake por regra, caso válido e inválido).
- Aceite: critério 6.
- Verificação: EditMode → 0 falhas; `ImportAndValidate` com um ID inválido injetado em teste retorna erro.
- Entrega: ≤ 120 palavras.

**T5 — Piloto de afinidades**
- Objetivo: primeira tabela real ponta a ponta.
- Arquivos: escreve `Data/Affinities/AffinityDefinition.cs` (`Tag affinityTag`, `Color primary`, `Color secondary`, `string primaryStatusId`, `Sprite icon`), `Editor/DataPipeline/Importers/AffinityImporter.cs`, `DataSource/Affinities.csv` (6 linhas; cores, forma e status primário conforme `docs/gdd/07-resonance.md` §10 e §4 — status primários: Burning, Chill, Shock, Corruption, Echo Mark, Decay; IDs `affinity.core.ember` etc.), assets gerados em `Content/Affinities/`, testes em `Tests/EditMode/Data/Affinities/` (importa, confere 6 assets, reimporta e confere GUIDs iguais, remove linha e confere aviso de órfão).
- Restrições: valores só do doc 07; cite `source_doc` em cada linha.
- Aceite: critérios 5 e 7.
- Verificação: rodar `ImportAndValidate` duas vezes → exit 0 e `git status --porcelain Assets/_Project/Content/Affinities` vazio após a 2ª; EditMode → 0 falhas.
- Entrega: ≤ 100 palavras.

**T6 — Localização**
- Objetivo: nomes/descrições das definições numa String Table Collection `Definitions` (pt-BR, en-US).
- Arquivos: escreve `Editor/DataPipeline/LocalizationWriter.cs` (cria a coleção e os locales se não existirem; chave = `StableId` + `.name`/`.desc`; atualiza valores a partir de `name_pt/name_en/desc_pt/desc_en`; `Definition.displayName` aponta para a entrada), assets em `Assets/_Project/Settings/Localization/`.
- Aceite: critério 8; os 6 nomes de afinidade aparecem nas duas línguas.
- Verificação: EditMode com teste que lê a tabela e confere `affinity.core.ember.name` em pt-BR; reimport idempotente.
- Entrega: ≤ 100 palavras.

**T7 — CI**
- Objetivo: validação de dados obrigatória no CI.
- Arquivos: escreve `.github/workflows/ci.yml` (substitui o comentário `# data-validate` por job que usa `game-ci/unity-builder@v4` com `customParameters: -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate` ou o runner equivalente do GameCI para `executeMethod`, mesma licença/caches do `unity-tests`; falha se exit ≠ 0).
- Verificação: YAML válido (`python -c "import yaml;yaml.safe_load(open('.github/workflows/ci.yml'))"`); job verde após push (o principal confirma).
- Entrega: ≤ 80 palavras.

**T8 — Integrar (principal)**
- Conferir critérios 1–10; atualizar CLAUDE.md §5 (estrutura de `DataSource/`), §6 (comando confirmado), §7/§8 (como um módulo adiciona tabela e regra de validação); ROADMAP §7 e Log.

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
Esperado: 0 falhas; `ImportAndValidate` exit 0; segunda execução não altera arquivos; CI com `data-validate` verde.

## Riscos e rollback
- Unity Localization em batchmode (criação de coleções) pode exigir `AssetDatabase.Refresh` entre passos → T6 isolada, tolerante a reexecução.
- `executeMethod` no GameCI depende da action usada → se o builder não servir, usar `unity-test-runner` com um teste EditMode que chama o validador (equivalente).
- Rollback: `git revert`; assets gerados são reproduzíveis a partir do CSV.

## Atualizações de documentação
- CLAUDE.md §5, §6, §7 (como adicionar tabela/regra). `DataSource/README.md` (nova). ROADMAP §7.

## Prompt de disparo
```
Implemente o módulo M02 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M02-pipeline-dados.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M02-pipeline-dados.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 plano criado.

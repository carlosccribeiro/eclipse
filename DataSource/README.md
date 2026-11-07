# DataSource

Fonte dos dados de design. Os CSV daqui são a fonte da verdade; os `.asset` em `Assets/_Project/Content/` são derivados e **não** devem ser editados à mão (o `ImportedHash` registra a linha de origem). Nenhum número de balanceamento nasce no código: ele vem do doc dono (`docs/gdd`), entra no CSV e o importador gera o ScriptableObject.

## Convenção dos CSV

- UTF-8 (BOM aceito), RFC 4180 (aspas para vírgula, aspas e quebra de linha dentro do campo), cabeçalho obrigatório, **ponto decimal** (`1.5`), cultura invariante.
- **1ª coluna: `id`**, o `StableId` no formato `<tipo>.<escopo>.<nome>` em snake_case (ex.: `affinity.core.ember`). Imutável: renomear passa pela `IdRemapTable`, nunca por edição direta do CSV.
- Localização: `name_pt`, `name_en`, `desc_pt`, `desc_en`.
- Enums pelo **nome** (`Fire`), nunca pelo número.
- Listas separadas por `|` (`tag_a|tag_b`); célula vazia = lista vazia.
- Percentuais como número: `12.5` significa 12,5%.
- Coluna `source_doc` com a origem do valor (ex.: `07 §10`).
- Referências a outras definições são o `id` delas (resolvidas em duas passagens).

## Como funciona o pipeline

`Eclipse > Data > Import & Validate` (ou o batchmode de CLAUDE.md §6):

1. Cada importador registrado lê seu CSV e casa as linhas com os assets **por `StableId`** (lido dos assets existentes, não pelo nome do arquivo). Cria o que falta, atualiza só linhas cujo hash mudou. Reimportar o mesmo CSV não altera nenhum `.asset` nem GUID.
2. Linha removida do CSV gera **aviso de órfão**; o asset não é apagado.
3. Segunda passagem resolve referências cruzadas por `StableId`.
4. O validador roda (as regras transversais chegam no T4 via `DataPipeline.Validator`; módulos futuros registram regras próprias).
5. O `DefinitionRegistry` é reconstruído e o resumo é impresso: `ImportAndValidate: N tables, E errors, W warnings`. Em batchmode o processo sai com código 0 (sem erros) ou 1.

## Como adicionar uma tabela

1. Crie o CSV em `DataSource/` seguindo a convenção acima.
2. Crie a `Definition` (ScriptableObject em `Eclipse.Data`, um arquivo por classe).
3. Crie a classe importadora em `Assets/_Project/Code/Editor/` (assembly `Eclipse.Editor`):

```csharp
[TableImporter("Affinities", Order = 10, DependsOn = new[] { "Statuses" })]
public sealed class AffinityImporter : TableImporter<AffinityDefinition>
{
    public override string CsvPath => "Affinities.csv";
    public override string OutputFolder => "Assets/_Project/Content/Affinities";

    protected override void Apply(AffinityDefinition d, CsvRow row, ImportContext ctx)
    {
        // copy columns with row.GetInt / GetFloat / GetEnum<T> / GetList ...
    }

    // optional pass 2: resolve references by id; return true if the asset changed
    protected override bool ResolveReferences(AffinityDefinition d, CsvRow row, ImportContext ctx) => false;
}
```

O importador precisa de construtor sem parâmetros. `DependsOn` define a ordem entre tabelas (ciclo = erro); `Order` desempata. Erros de célula (coluna inexistente, número inválido, enum desconhecido) viram erros do contexto com tabela, linha e id, e as demais linhas continuam.

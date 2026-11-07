# M10 — Modificadores e SkillSpec (6 fases, Generation, Augments) + spikes Echo Clone/Constructs
Fase 2 · Onda 7 · Status: ⏳ não iniciado
Depende de: M08, D01 · Desbloqueia: M11, M13, M17, M20 · Paralelo com: M18
Branch: modulo/M10-modificadores
Docs de referência: 03 §3 (todo), §4.1 (KAE-*), §5 (GEN-*), §6; 07 §7, §7.1; 06 §3.1–3.2; 08 §4.5, §8.1, §11.1; 11 §3.4.2, §3.6, §3.16, §3.18; 00 §5 R03, R07, R26, R27, R30, R47; resultado de D01

## Objetivo
Transformar `SkillDefinition` + modificadores + Augment (+ canais Set/Passive/Run, ainda vazios) em um `SkillSpec` imutável compilado no `Eclipse.Core`, aplicando as 6 fases de 03 §3.4 sem código específico por modificador. No runtime, replicação (Echo/Chain), gatilhos e procs seguem o modelo Generation (07 §7, 00 R07). Todo o catálogo **KAE-\*** e **GEN-\*** de 03 e os 15 Augments do Kael (08 §4.5) ficam funcionais por dados. Os dois maiores riscos técnicos da arquitetura (Echo Clone e Void Constructs, 11 §3.18) são validados por spikes.

## Critérios de saída (verificáveis)
1. `SkillSpecCompiler` (Core) aplica as 6 fases na ordem de 03 §3.4 e a ordem de canais de 11 §3.6.2 (Item → Augment → Set → Passive → Run, depois StableId); snapshot tests cobrem os 8 modificadores de Cleaving Strike (KAE-CS-01..08) isolados e em pares.
2. Regras de 03 §3.3 testadas: limite 3 por habilidade (2 em Ultimate/Basic/Heavy), 14 por herói, duplicata = maior rolagem, 1 por grupo exclusivo F/C/M, prioridade por ordem de slot, motivo de inatividade (*limite*, *duplicata*, *grupo*, *Augment*) exposto no `SkillSpec`.
3. Conversão (03 §3.5): soma ≤ 100% com normalização; ≥ 50% concede a tag; < 50% só a `ResonanceAffinityMask`; área escala o raio por √(1+X) (06).
4. Generation (07 §7): nada avalia triggers/procs em Gen ≥ 2; réplica `IsEcho` nunca replica, exceto fonte com `EchoRecursion1x` (1 recursão, Proc Coefficient em cadeia 0,25); ICD mínimo 0,1 s (default 0,5 s); `SourceChain` impede auto-disparo; teto de 30 entidades geradas/s por fonte (11 §3.6.5).
5. Todos os modificadores KAE-\* e GEN-\* de 03 e os 15 Augments do Kael importados de `DataSource/`, validados (0 erros) e funcionando em jogo, cada um com pelo menos 1 teste (EditMode snapshot ou PlayMode comportamental).
6. Teste de recursão (11 §3.16, sem Resonance, que entra no M11): 40 inimigos e todas as fontes Echo/Chain/Trigger de Kael + GEN ativas → ≤ 400 eventos de dano/s e nenhum evento com Generation > 2.
7. Spikes de Echo Clone e Void Constructs com relatório go/no-go em `docs/roadmap/spikes/` e números de performance (ms de CPU de gameplay por frame) medidos no hardware alvo.
8. `dotnet test tools/CoreTests`, EditMode e PlayMode verdes; validador de dados sem erros; nenhum warning novo de compilação.

## Fora de escopo
- Status de afinidade e Resonance (M11). Modificadores que aplicam status usam o efeito `ApplyStatus` genérico de M08 contra um status stub; o comportamento completo chega no M11.
- Modificadores de Lyra, Nyx e Orun (M21–M23); catálogo completo e Signatures (M28).
- Origem dos modificadores a partir de itens equipados (M13): aqui eles são injetados por uma fonte de debug.
- Pontos da Aether Lattice, gates e respec (M20): aqui o Augment ativo é escolhido por debug.
- Persistência de atribuição de modificadores/Augments (M13/M20 + M18).

## Premissas e decisões
- **Premissa:** M08 entregou `SkillDefinition` com `Delivery`/`Effect`/`Trigger` polimórficos (`[SerializeReference]`), `SkillController`, o kit do Kael e o recurso Fury; M05 entregou `DamageInstance` com `Generation`, `HitFlags`, `procCoefficient`, `SourceChain`; M03 entregou `StatBlock` e `DamageResolver`; M02 entregou importadores e regras de validação descobertos por atributo (um arquivo por tabela/regra). O T1 confirma e ajusta os briefs.
- **Premissa:** D01 já reconciliou premissas e tags das habilidades em 03 com 08. Se D01 mudou IDs de modificador, vale 03 atualizado.
- **Decisão:** o compilador e o modelo Generation ficam no Core (`Core/Modifiers`, `Core/Procs`), testados por `dotnet test`; o Gameplay só executa o `SkillSpec`.
- **Decisão:** cada `ModifierOp` é uma classe reutilizável (`[SerializeReference]`). Um comportamento novo de um modificador vira um op genérico parametrizável, nunca um `if (id == ...)`.
- **Decisão:** spikes vivem em `Gameplay/Skills/Spikes/` e podem ser descartados; o que for aprovado é promovido nos módulos M22/M23.
- **Paralelo com M18:** este módulo não escreve em `Assets/_Project/Code/Save/**` nem em DTOs de save, nem altera `ItemInstance`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Mapear APIs entregues por M02/M03/M05/M08 e o resultado de D01 | investigação | investigador | — | — | ⏳ |
| T2 | Fixar contratos do Core: `ModifierOp`, fases, canais, `SkillSpec`, `SkillCompileInput`, ADR | design | principal | T1 | `Assets/_Project/Code/Core/Modifiers/Contracts/*.cs`, `docs/adr/0010-skillspec-compilation.md` | ⏳ |
| T3 | Implementar `SkillSpecCompiler` (6 fases, canais, exclusividade, limites, conversão) | execução | executor | T2 | `Assets/_Project/Code/Core/Modifiers/Compiler/*.cs`, `tools/CoreTests/Modifiers/*.cs` | ⏳ |
| T4 | Implementar modelo Generation/procs (gate, IsEcho, EchoRecursion1x, Proc Coefficient, ICD, SourceChain, teto de spawn) | execução | executor | T2 | `Assets/_Project/Code/Core/Procs/*.cs`, `tools/CoreTests/Procs/*.cs` | ⏳ |
| T5 | Definições de dados + importadores + regras de validação de modificadores e Augments | execução | executor | T2 | `Assets/_Project/Code/Data/Modifiers/*.cs`, `Assets/_Project/Code/Editor/DataPipeline/Importers/{Modifier,Augment}TableImporter.cs`, `Assets/_Project/Code/Editor/Validation/Rules/{Modifier,Augment}Rules.cs`, `DataSource/modifiers.csv`, `DataSource/augments.csv` | ⏳ |
| T6 | Integrar `SkillSpec` ao runtime: replicação, `TriggerSystem`, recompilação, `ModifierSandbox` de debug | execução | executor | T3, T4 | `Assets/_Project/Code/Gameplay/Skills/Runtime/*.cs` (novos) + ajuste em `SkillController` | ⏳ |
| T7 | Cleaving Strike: KAE-CS-01..08 + 3 Augments de Cleaving Strike, com ops necessários | execução | executor | T5, T6 | `Assets/_Project/Code/Gameplay/Skills/Ops/Core/*.cs`, linhas KAE-CS em `DataSource/modifiers.csv`/`augments.csv`, `Assets/_Project/Code/Tests/PlayMode/Modifiers/CleavingStrike/*.cs` | ⏳ |
| T8 | Demais habilidades do Kael: KAE-IL/BC/AG/WB + 12 Augments | execução | executor | T7 | `Assets/_Project/Code/Gameplay/Skills/Ops/Kael/*.cs`, linhas KAE-IL/BC/AG/WB, `Assets/_Project/Code/Tests/PlayMode/Modifiers/Kael/*.cs` | ⏳ |
| T9 | Genéricos GEN-01..30: filtros de tags, atribuição automática, alvos Basic/Heavy | execução | executor | T7 | `Assets/_Project/Code/Gameplay/Skills/Ops/Generic/*.cs`, `Assets/_Project/Code/Core/Modifiers/Assignment/*.cs`, linhas GEN, `Assets/_Project/Code/Tests/PlayMode/Modifiers/Generic/*.cs`, `tools/CoreTests/Modifiers/Assignment/*.cs` | ⏳ |
| T10 | Teste de recursão (Core fuzz + PlayMode 40 inimigos) | execução | executor | T8, T9 | `tools/CoreTests/Procs/RecursionFuzzTests.cs`, `Assets/_Project/Code/Tests/PlayMode/Stress/RecursionStressTest.cs` | ⏳ |
| T11 | Spike Echo Clone (replicação de ações) | execução | executor | T6 | `Assets/_Project/Code/Gameplay/Skills/Spikes/EchoClone/*`, `Assets/_Project/Scenes/Sandbox/Spike_EchoClone.unity`, `docs/roadmap/spikes/M10-echo-clone.md` | ⏳ |
| T12 | Spike Void Constructs (recurso `MassToConstructs` + invocações) | execução | executor | T6 | `Assets/_Project/Code/Gameplay/Skills/Spikes/VoidConstructs/*`, `Assets/_Project/Scenes/Sandbox/Spike_VoidConstructs.unity`, `docs/roadmap/spikes/M10-void-constructs.md` | ⏳ |
| T13 | Integração, verificação final e documentação | revisão | principal | T10, T11, T12 | `CLAUDE.md` (se aplicável), `docs/ROADMAP.md` (status), este arquivo (Status/Log) | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T4 ∥ T5 (arquivos disjuntos)
4. T6
5. T7 ∥ T11 ∥ T12
6. T8 ∥ T9 (pastas de ops separadas: `Ops/Kael` × `Ops/Generic`; linhas de CSV em blocos distintos — o segundo a terminar faz merge do CSV)
7. T10
8. T13

## Briefs

### T1 — Mapear APIs entregues
Tarefa T1: Mapear as APIs que M10 vai consumir.
Objetivo: lista de tipos/métodos reais (arquivo:linha) que correspondem às premissas deste plano.
Contexto: o plano assume `SkillDefinition` com `Delivery`/`Effects`/`Triggers` polimórficos e `SkillController` (M08), `DamageInstance` com `Generation`/`HitFlags`/`procCoefficient`/`SourceChain` (M05), `StatBlock`/`StatMod`/`DamageResolver` (M03), extensão do importador/validador por classe descoberta por atributo (M02), streams de RNG e `EntityEvents` (M01). D01 pode ter mudado premissas/tags de habilidades em `docs/gdd/03-modificadores.md`.
Arquivos: lê `Assets/_Project/Code/**`, `docs/gdd/03-modificadores.md` (§3, §4.1, §5), `docs/roadmap/D01-revisao-modificadores.md` (Log); escreve nada.
Restrições: somente leitura.
Aceite: cada premissa marcada como confirmada / divergente (com o nome real) / ausente.
Verificação: n/a (relatório).
Entrega: até 300 palavras — tabela premissa → situação → evidência (arquivo:linha); mudanças de D01 que afetam KAE-*/GEN-*.

### T2 — Contratos do Core
Tarefa T2: Fixar os contratos do compilador de habilidades.
Objetivo: interfaces e tipos do Core que T3–T9 implementam sem precisar decidir design.
Contexto: 11 §3.6.1–3.6.2 (composição, pipeline de compilação, tabela de fases), 03 §3.1–3.6 (anatomia, alvo, limites, fases, conversão, Proc Coefficient), 00 R03 (Augment nunca dá More), R07 (Generation). Canais: Item, Augment, Set, Passive, Run. Políticas de stacking: `HighestRoll | Additive | FirstWins`.
Arquivos: lê relatório do T1, docs citados; escreve `Assets/_Project/Code/Core/Modifiers/Contracts/*.cs` (`ModifierOp` base + enum `ModifierPhase`, `ModifierChannel`, `ExclusivityGroup`, `SkillSpec`, `SkillSpecBuilder`, `SkillCompileInput`, `ModifierInstanceInput{ModifierId, Roll01, Channel, Priority, SourceHandle}`, `InactiveReason`), `docs/adr/0010-skillspec-compilation.md`.
Restrições: Core sem `UnityEngine` (CLAUDE.md §4, R52); nenhum número de balanceamento no código.
Aceite: compila em `dotnet build tools/CoreTests`; ADR lista decisões e alternativas descartadas.
Verificação: `dotnet build tools/CoreTests` → esperado: build sem erros.
Entrega: até 200 palavras — tipos criados e decisões do ADR.

### T3 — SkillSpecCompiler
Tarefa T3: Implementar a compilação de `SkillSpec`.
Objetivo: dado `SkillCompileInput`, produzir `SkillSpec` determinístico conforme 03 §3.3–3.5 e 11 §3.6.2.
Contexto: fases 1 Conversão → 2 Forma → 3 Parâmetros → 4 Replicação → 5 Gatilhos → 6 Recurso; dentro da fase, ordem de canal Item → Augment → Set → Passive → Run e depois StableId. Limites: 3 por habilidade (Ultimate/Basic/Heavy 2), 14 por herói; prioridade = `Priority` do input (ordem de slot, preenchida no M13); duplicata = maior Roll01; um por grupo F/C/M; valores de mesma natureza somam; conversão ≤ 100% normalizada, ≥ 50% concede tag, < 50% só `ResonanceAffinityMask`; área: raio × √(1+X); Augment ativo marca *Inativo (Augment)* o modificador do mesmo grupo F/M quando 08 declarar exclusão. Snapshot de Increased/More incondicionais (11 §3.5.3).
Arquivos: lê contratos do T2; escreve `Assets/_Project/Code/Core/Modifiers/Compiler/*.cs`, `tools/CoreTests/Modifiers/*.cs`.
Restrições: não alterar contratos sem registrar no ADR; sem alocação por cast (compilação só na troca de loadout).
Aceite: testes cobrindo cada regra acima + snapshot dos 8 KAE-CS com fixtures em código.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Modifiers"` → esperado: todos passam.
Entrega: até 200 palavras — regras cobertas, testes (nomes), pontos não cobertos.

### T4 — Generation e procs
Tarefa T4: Implementar o modelo Generation no Core.
Objetivo: serviço puro que decide se um evento pode disparar triggers/procs e com que Proc Coefficient.
Contexto: 07 §7 e §7.1; 03 §3.6; 11 §3.6.5. Gen 0 = ação direta; proc = origem + 1; `generation >= 2` → não avalia triggers, Resonances nem consumos e aplica SP a 50%; `IsEcho` nunca gera réplica, salvo `EchoRecursion1x` (uma vez, flag `EchoRecursed`, coeficientes multiplicam: 0,5 × 0,5); Proc Coefficient: Echo 0,5, Chain 0,5, explosão de Trigger 0,5, tick de área 0,25, clone 0,4; `SourceChain` até 4 handles impede auto-disparo; ICD mínimo 0,1 s, default 0,5 s; teto default de 30 entidades geradas/s por fonte (excedente descartado e contado).
Arquivos: lê contratos do T2 e o tipo `DamageInstance` (relatório T1); escreve `Assets/_Project/Code/Core/Procs/*.cs`, `tools/CoreTests/Procs/*.cs`.
Restrições: Core puro; relógio injetado (sem `Time.time`).
Aceite: testes para cada regra, incluindo várias fontes de `EchoRecursion1x` não se somando.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Procs"` → esperado: todos passam.
Entrega: até 200 palavras.

### T5 — Dados de modificadores e Augments
Tarefa T5: Criar definições, importadores e validação de modificadores e Augments.
Objetivo: `DataSource/modifiers.csv` e `DataSource/augments.csv` viram SOs por `StableId`, com validação das regras de dados.
Contexto: anatomia de 03 §3.1 (ID, nome, categoria, alvo/filtro, grupo, efeito, faixas, raridade mínima, BP, tags ±); `SkillModifierDefinition` e `AugmentDefinition` de 11 §3.4.2; validação de 11 §3.15.2 aplicável: IDs únicos e no formato `HERO-SKILL-NN`/`GEN-NN`/`SIG-*`, alvo existente, tags no vocabulário 00 §3.4 (HitFlags fora), grupos exclusivos válidos, faixa min ≤ max e máx/mín ≤ 1,5 (03 §6.2), More = 0 em Augment (R03), `EchoRecursion1x` só nas fontes de 07 §7.1, ICD declarado em todo Trigger. Nesta tarefa, cadastre as colunas e **todas as linhas KAE-\* e GEN-\*** com os dados de 03 (os `ModifierOp` concretos entram em T7–T9; aqui ficam referências de op por nome).
Arquivos: lê `docs/gdd/03-modificadores.md` §3–§6, `docs/gdd/08-herois.md` §4.5; escreve os arquivos da linha T5 da tabela.
Restrições: um importador e uma regra por arquivo (sem editar o registro central de M02); itens de M12 que referenciam IDs de modificador passam de *warning* para *erro* quando o ID não existir (ajustar a regra de M12, se ela existir como warning, apenas pelo ponto de extensão).
Aceite: import + validação passam; contagem de linhas = nº de KAE-\* + GEN-\* em 03.
Verificação: `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data-m10.log` → esperado: exit code 0 e log com 0 erros de validação.
Entrega: até 200 palavras — contagens importadas, regras criadas, IDs com dúvida.

### T6 — Runtime do SkillSpec
Tarefa T6: Fazer o `SkillController` executar `SkillSpec`.
Objetivo: habilidades do Kael rodam a partir do spec compilado; replicação e gatilhos funcionam com Generation.
Contexto: 11 §3.6.3 (execução), §3.6.4 (hooks), §3.5.3 (recompilar no próximo frame após `StatsChanged`). Replicação: `AddRepeat` (atraso e % de dano), `AddChain`, `AddProjectiles`, cada cópia herdando fases 1–3. `TriggerSystem` inscreve triggers do spec nos hooks de `EntityEvents` e consulta o serviço do T4. `ModifierSandbox` (MonoBehaviour de debug) injeta `ModifierInstanceInput`s e Augment ativo no herói.
Arquivos: lê contratos/implementações de T2–T4 e o `SkillController` de M08; escreve `Assets/_Project/Code/Gameplay/Skills/Runtime/*.cs` e o ajuste mínimo no `SkillController`.
Restrições: 0 B de GC por cast em combate (CLAUDE.md §4); UI/Audio não são referenciados.
Aceite: PlayMode test: Cleaving Strike sem modificadores mantém o dano de M08; com um op de repetição de teste gera instância Gen 1 com Proc Coefficient 0,5.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Skills" -testResults Logs/playmode-m10-t6.xml -logFile Logs/playmode-m10-t6.log` → esperado: `failed="0"` no XML.
Entrega: até 200 palavras.

### T7 — Cleaving Strike
Tarefa T7: Implementar KAE-CS-01..08 e os 3 Augments de Cleaving Strike.
Objetivo: cada modificador muda o comportamento descrito em 03 §4.1 (KAE-CS) e cada Augment o de 08 §4.5, só por dados + ops genéricos.
Contexto: Burning Arc (zona no chão 3 s), Volatile Impact (trigger 3+ alvos, ICD 0,5 s), Executioner's Reach (alcance + bônus vs. feridos), Echo Slash (repete após 0,35 s), Shattering Blow (consome Freeze → Shatter: usar stub de status até M11), Ruin Cleaver (conversão C), Blood Fury (recurso), Infernal Collapse (forma F). Augments: Wildfire Cleave, Headsman's Arc, Bulwark Sweep. Ops novos ficam em `Ops/Core` para reuso por outros heróis.
Arquivos: lê 03 §4.1, 08 §4.5, T5/T6; escreve os arquivos da linha T7.
Restrições: nenhum op com ID de modificador hardcoded; números só do CSV.
Aceite: 1 teste PlayMode por modificador/Augment verificando o comportamento (forma, nº de instâncias, Generation, recurso) e 1 teste de par conflitante do grupo F.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Modifiers.CleavingStrike" -testResults Logs/playmode-m10-t7.xml -logFile Logs/playmode-m10-t7.log` → esperado: `failed="0"`.
Entrega: até 250 palavras — ops criados (com parâmetros), testes, pendências que dependem de M11.

### T8 — Demais habilidades do Kael
Tarefa T8: Implementar KAE-IL, KAE-BC, KAE-AG, KAE-WB e os 12 Augments restantes do Kael.
Objetivo: idem T7 para Infernal Leap, Burning Chains, Ashen Guard e Worldbreaker.
Contexto: 03 §4.1; 08 §4.4–4.5 (comportamento base, Parry 0,2 s, Unstoppable, Root 2 s, Vulnerability Ruin); Execute segue R08 (limiar ≤ 20%, Elites metade, chefes/Guardians: +25% Vulnerability abaixo de 20%); Vulnerability com teto de +60% (R12) via `DamageResolver`.
Arquivos: lê T7 (ops existentes); escreve os arquivos da linha T8.
Restrições: reutilizar ops de `Ops/Core`; op novo específico do Kael só se não for generalizável (justificar no relatório).
Aceite: 1 teste por modificador/Augment; dependências de status listadas como pendência de M11.
Verificação: `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Modifiers.Kael" -testResults Logs/playmode-m10-t8.xml -logFile Logs/playmode-m10-t8.log` → esperado: `failed="0"`.
Entrega: até 250 palavras.

### T9 — Genéricos
Tarefa T9: Implementar GEN-01..30 e a atribuição de genéricos.
Objetivo: genéricos funcionam em qualquer habilidade elegível pelo filtro de tags, inclusive Basic/Heavy.
Contexto: 03 §3.2 (atribuição: o jogador escolhe; padrão automático = habilidade elegível com menos modificadores ativos; troca livre fora de combate) e §5 (catálogo). A regra de atribuição é Core (`Core/Modifiers/Assignment`); a UI de troca é do M13.
Arquivos: lê 03 §3.2 e §5; escreve os arquivos da linha T9.
Restrições: não editar `Ops/Kael` nem `Ops/Core` (se precisar de op base novo, criar em `Ops/Generic` e registrar no relatório para promoção).
Aceite: dotnet tests da atribuição; 1 teste PlayMode por GEN aplicado a uma habilidade do Kael.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Assignment"` e `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Modifiers.Generic" -testResults Logs/playmode-m10-t9.xml -logFile Logs/playmode-m10-t9.log` → esperado: ambos sem falhas.
Entrega: até 200 palavras.

### T10 — Teste de recursão
Tarefa T10: Automatizar o teste de recursão de 11 §3.16 (versão sem Resonance).
Objetivo: provar que nenhuma combinação de modificadores do Kael + GEN explode.
Contexto: 40 inimigos (dummies de M07), todas as fontes Echo/Chain/Trigger de KAE-\* e GEN-\* ativas no limite máximo das faixas; medir eventos de dano por segundo e Generation máxima por 30 s simulados. Core fuzz: grafo aleatório de procs com seeds fixas. M11 vai estender o mesmo teste com Resonances.
Arquivos: escreve `tools/CoreTests/Procs/RecursionFuzzTests.cs`, `Assets/_Project/Code/Tests/PlayMode/Stress/RecursionStressTest.cs`.
Restrições: teste determinístico (seed fixa, stream `Combat`).
Aceite: ≤ 400 eventos de dano/s; nenhum evento Gen > 2; teto de spawn respeitado.
Verificação: `dotnet test tools/CoreTests --filter "FullyQualifiedName~Recursion"` e `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Stress" -testResults Logs/playmode-m10-t10.xml -logFile Logs/playmode-m10-t10.log` → esperado: sem falhas, e o log do teste imprime pico de eventos/s e Gen máx.
Entrega: até 150 palavras com os números medidos.

### T11 — Spike Echo Clone
Tarefa T11: Validar a replicação de ações (Echo Clone).
Objetivo: protótipo de um `Delivery`/`Effect` reutilizável que cria um clone que repete as ações do herói com atraso.
Contexto: 08 §6.4 (Echo Clone de Nyx), 03 §3.6 (cópia de clone: Gen 0 do clone, Proc Coefficient 0,4, herda modificadores exceto Echo e Resource), 07 §7 (máx. 3 clones por jogador; o mais antigo expira), R07. Use o Kael como host provisório (o clone repete Basic/Cleaving Strike). Duas abordagens a comparar: (a) reexecutar o `SkillSpec` do clone a partir de eventos `OnSkillCast` gravados; (b) replay de timeline de inputs.
Arquivos: escreve `Assets/_Project/Code/Gameplay/Skills/Spikes/EchoClone/*`, `Assets/_Project/Scenes/Sandbox/Spike_EchoClone.unity`, `docs/roadmap/spikes/M10-echo-clone.md`.
Restrições: código isolado no namespace `Eclipse.Gameplay.Skills.Spikes`; não alterar `Runtime/` além de pontos de extensão já existentes.
Aceite: 3 clones repetindo ações com 120 inimigos na cena; relatório com abordagem recomendada, custo em ms/frame (Profiler), riscos e o que muda em 11 §3.6.
Verificação: abrir a cena e rodar o PlayMode smoke `Eclipse.Tests.PlayMode.Spikes.EchoClone` → esperado: sem falhas; relatório escrito.
Entrega: até 200 palavras — recomendação go/no-go e números.

### T12 — Spike Void Constructs
Tarefa T12: Validar o recurso Void Mass → Void Constructs e as invocações.
Objetivo: protótipo de `ResourceDefinition` tipo `MassToConstructs` e de construções invocadas com IA mínima.
Contexto: 08 §7.1 (Void Mass, base 3 construções, teto 6, até 8 via exceções §11.1), 01 §3.8 (tradução de Resource Generation para Void Mass), 11 §3.4.2 (`ResourceDefinition`), 00 R01. Medir 8 construções + 120 inimigos.
Arquivos: escreve `Assets/_Project/Code/Gameplay/Skills/Spikes/VoidConstructs/*`, `Assets/_Project/Scenes/Sandbox/Spike_VoidConstructs.unity`, `docs/roadmap/spikes/M10-void-constructs.md`.
Restrições: idem T11.
Aceite: construções se formam, atacam, são sacrificadas e respeitam o teto; relatório com custo por frame e recomendações para M23.
Verificação: PlayMode smoke `Eclipse.Tests.PlayMode.Spikes.VoidConstructs` → esperado: sem falhas; relatório escrito.
Entrega: até 200 palavras.

### T13 — Integração
Tarefa T13: Fechar o módulo.
Objetivo: todos os critérios de saída verificados e documentados.
Contexto: Definition of Done do CLAUDE.md §7.
Arquivos: escreve `CLAUDE.md` (pastas `Ops/*`, convenção de ops, se novas), `docs/ROADMAP.md` (status M10), este arquivo (Status/Log).
Restrições: não marcar ✅ sem a saída dos comandos de verificação.
Aceite: checklist dos 8 critérios com evidência.
Verificação: comandos da seção abaixo.
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
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
Esperado: todos com exit code 0, XMLs com `failed="0"`, log de dados com 0 erros.

## Riscos e rollback
- **Explosão de ops específicos** → regra: op novo precisa servir a ≥ 2 modificadores ou ser parametrizável; revisão no T13.
- **Ordem de fases gera resultados contraintuitivos** (ex.: replicação antes de parâmetros) → snapshot tests de pares; divergência vira questão para 03, não hack no compilador.
- **Echo Clone inviável na abordagem (a)** → adotar (b) e registrar a mudança em 11 §3.6 via resolução no 00 §5.
- **Constructs caros** → pooling e LOD de IA de 11 §3.10; se ainda acima do orçamento, reduzir teto efetivo e levar ao usuário.
- Rollback: o módulo vive na branch `modulo/M10-modificadores`; o `SkillController` mantém o caminho de M08 atrás de uma flag até o T13.

## Atualizações de documentação
- `CLAUDE.md` §5: pastas `Core/Modifiers`, `Core/Procs`, `Gameplay/Skills/{Runtime,Ops,Spikes}`; regra "op novo é genérico".
- `docs/adr/0010-skillspec-compilation.md` (novo).
- `docs/roadmap/spikes/` (novo) com os dois relatórios.
- `docs/ROADMAP.md` §7: status do M10; se um spike for no-go, nota nos módulos M22/M23.

## Prompt de disparo
```
Implemente o módulo M10 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M10-modificadores.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M10-modificadores.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

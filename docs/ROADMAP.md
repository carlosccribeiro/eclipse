# PROJECT ECLIPSE — Roadmap de desenvolvimento

Data: 2026-11-07 · Base de design: `docs/GDD-v0.3.md` + `docs/gdd/00..11` · Regras de trabalho: `CLAUDE.md`

O roadmap é dividido em **módulos** (`Mxx`). Cada módulo tem um arquivo próprio em `docs/roadmap/` com o plano de ação completo (tarefas, briefs, verificação e prompt de disparo), de modo que a implementação possa ser disparada módulo a módulo.

## 1. Decisões de início

| # | Decisão | Origem |
|---|---|---|
| D1 | Unity **6.3 LTS (6000.3.x)**, instalada pelo Unity Hub antes do M00 | usuário, 2026-11-07 |
| D2 | Repositório **GitHub privado** + Git LFS + CI **GameCI** (testes, validador, build) | usuário, 2026-11-07 |
| D3 | Implementação **por módulo, em sessões próprias com worktree/branch**, no máximo **2 em paralelo** | usuário, 2026-11-07 |
| D4 | Arte **greybox + assets gratuitos** até o Vertical Slice; arte final em trilha paralela depois | usuário, 2026-11-07 |
| D5 | `Eclipse.Core` em C# puro, testado por `dotnet test` e EditMode (00 §5 R52) | lead |
| D6 | Números de balanceamento só via `DataSource/*.csv` → SOs, nunca hardcoded | lead (11 §3.15) |

## 2. Princípios de ordenação

1. **Fundação antes de tudo:** projeto, CI, núcleo C# e pipeline de dados destravam todo o resto.
2. **Risco primeiro:** combate "sentir bem" (pilar 1), hordas de 200 inimigos e o sistema de modificadores/recursão são validados antes de qualquer conteúdo.
3. **Duas trilhas paralelas desde cedo:** *núcleo* (C# puro, testável sem cena: itemização, geração de run, save) e *gameplay* (cena, combate, IA, UI). Elas não disputam arquivos.
4. **Vertical slice antes de largura:** uma run completa de The Ashen Wastes com Kael, loot, chefe e save antes de qualquer herói ou região extra.
5. **Frameworks antes de catálogo:** o catálogo completo (84 itens, 72 peças, 206 modificadores, 40 inimigos) entra por dados quando os sistemas estão estáveis.
6. **Expansão em duas trilhas:** heróis e regiões avançam juntos, um módulo de cada por onda.
7. **Endgame e polimento por último**, sobre conteúdo completo.

## 3. Marcos

| Marco | Nome | Fecha com | Critério de saída |
|---|---|---|---|
| A | Combat Sandbox | M06, M07, M08, M09 | Kael com Greatsword, 5 habilidades e esquiva contra 3 inimigos em sala de teste; critérios de game feel de 06; soak test de 200 inimigos a 60 fps no hardware alvo |
| B | Build Sandbox | M14 | Itens gerados, equipados e com modificadores mudando habilidades; 2 sets testáveis; simulador gera relatório Rotas A/B/C |
| C | **Vertical Slice** | M16, M17, M18 | Run completa da Ashen Wastes (13 linhas + Ashen Colossus) com loot, Rest Room, morte/extração e suspend-save |
| D | Loop completo | M19, M20 | Sanctuary ↔ run, serviços, progressão de herói, dificuldades Normal → Eclipse |
| E | Elenco completo | M21, M22, M23, M28 | 4 heróis × 3 builds jogáveis com catálogo completo |
| F | **Alpha** (conteúdo completo) | M24–M28 | Metas do GDD §16 cumpridas |
| G | **Beta** | M29 | Endgame (Ascension 1–100 e modos) jogável |
| H | **Release Candidate** | M30, M31, M32 | Specs mínimas a 60 fps, acessibilidade, pt-BR/en-US, Steam, zero bloqueadores |

## 4. Módulos

| ID | Módulo | Fase | Depende de | Plano |
|---|---|---|---|---|
| D01 | Revisão de modificadores 03 × 08 (design) | 0 | — | [D01](roadmap/D01-revisao-modificadores.md) |
| D02 | Lacunas numéricas (HP/dano por eLvl, XP por PA, Rift Shards → Gold) | 0 | — | [D02](roadmap/D02-lacunas-numericas.md) |
| M00 | Repositório, projeto Unity e CI | 0 | — | [M00](roadmap/M00-fundacao-projeto.md) |
| M01 | Núcleo C# (StableId, Tags, RNG, Event Bus) | 0 | M00 | [M01](roadmap/M01-nucleo-core.md) |
| M02 | Pipeline de dados (SO base, importador CSV, validador) | 0 | M01 | [M02](roadmap/M02-pipeline-dados.md) |
| M03 | Stats e dano (StatBlock, atributos, DamageResolver, DR de CC) | 0 | M01 | [M03](roadmap/M03-stats-dano.md) |
| M04 | Controle do jogador (input, câmera, movimento, esquiva) | 1 | M01 | [M04](roadmap/M04-controle-jogador.md) |
| M05 | Runtime de combate (frame data, hit detection, estados, feedback) | 1 | M02, M03, M04 | [M05](roadmap/M05-runtime-combate.md) |
| M06 | Base de UI e HUD | 1 | M05, M08 | [M06](roadmap/M06-ui-hud.md) |
| M07 | IA de inimigos base + 3 inimigos + soak test | 1 | M05 | [M07](roadmap/M07-ia-inimigos.md) |
| M08 | Sistema de habilidades v1 + kit do Kael | 1 | M03, M05 | [M08](roadmap/M08-habilidades-kael.md) |
| M09 | Áudio base (FMOD) | 1 | M05 | [M09](roadmap/M09-audio-base.md) |
| M10 | Modificadores e SkillSpec (6 fases, Generation, Augments) + spikes Echo Clone/Constructs | 2 | M08, D01 | [M10](roadmap/M10-modificadores.md) |
| M11 | Status e Resonance | 2 | M03, M10 | [M11](roadmap/M11-status-resonance.md) |
| M12 | Itemização núcleo (itens, afixos, gerador, loot, pity) | 2 | M02, M03 | [M12](roadmap/M12-itemizacao-nucleo.md) |
| M13 | Equipamento, inventário e Set Powers | 2 | M06, M10, M12 | [M13](roadmap/M13-equipamento-sets.md) |
| M14 | Simuladores e Training Grounds | 2 | M07, M11, M13 | [M14](roadmap/M14-simuladores-training.md) |
| M15 | Geração de run (mapa de nós, Encounter Director) | 3 | M01 | [M15](roadmap/M15-geracao-run.md) |
| M16 | Fluxo de run, salas e Ashen Wastes greybox | 3 | M07, M11, M13, M15, M18, D02 | [M16](roadmap/M16-fluxo-run.md) |
| M17 | Framework de chefes + Guardian + Ashen Colossus | 3 | M10, M16 | [M17](roadmap/M17-chefes.md) |
| M18 | Save system | 3 | M12 | [M18](roadmap/M18-save.md) |
| M19 | Sanctuary e economia | 4 | M16, M18 | [M19](roadmap/M19-sanctuary-economia.md) |
| M20 | Progressão de herói, Aether Lattice e dificuldades | 4 | M10, M16, M18 | [M20](roadmap/M20-progressao.md) |
| M21 | Herói Lyra | 5 | M11, M20 | [M21](roadmap/M21-lyra.md) |
| M22 | Herói Nyx | 5 | M11, M20 | [M22](roadmap/M22-nyx.md) |
| M23 | Herói Orun | 5 | M11, M20 | [M23](roadmap/M23-orun.md) |
| M24 | Região 2 — The Frozen Cathedral + Pale Sovereign | 5 | M17, M19 | [M24](roadmap/M24-frozen-cathedral.md) |
| M25 | Região 3 — The Shattered Expanse + Fractured One | 5 | M24 | [M25](roadmap/M25-shattered-expanse.md) |
| M26 | Região 4 — The Hollow Depths + Devouring Presence | 5 | M25 | [M26](roadmap/M26-hollow-depths.md) |
| M27 | Região 5 — The Astral Citadel + First Forgotten | 5 | M26 | [M27](roadmap/M27-astral-citadel.md) |
| M28 | Catálogo completo de itens, sets e modificadores | 5 | M14, M21, M22, M23 | [M28](roadmap/M28-catalogo-completo.md) |
| M29 | Endgame (Ascension, Rift Sigils, modos, pinnacle) | 6 | M27, M28 | [M29](roadmap/M29-endgame.md) |
| M30 | Performance e otimização | 7 | M29 | [M30](roadmap/M30-performance.md) |
| M31 | Opções, acessibilidade e localização | 7 | M29 | [M31](roadmap/M31-opcoes-acessibilidade.md) |
| M32 | Steam, telemetria e release | 7 | M30, M31 | [M32](roadmap/M32-steam-release.md) |
| A01–A05 | Trilha de arte (direção, heróis, inimigos, ambientes, VFX) | paralela | Marco C | [A00](roadmap/A00-trilha-arte.md) |

## 5. Ondas de execução (máx. 2 módulos em paralelo)

| Onda | Módulos | Por que juntos / por que agora | Marco |
|---|---|---|---|
| 0 | **M00** ∥ D01 | D01 só edita `docs/gdd/03`; M00 cria o projeto | |
| 1 | **M01** ∥ D02 | Tudo depende do núcleo; D02 só edita docs | |
| 2 | M02 ∥ M03 | Data/Editor × Core/Stats — pastas disjuntas | |
| 3 | M04 ∥ M12 | Gameplay/Player × Core+Data/Items | |
| 4 | M05 ∥ M15 | Gameplay/Combat × Core/RunGen | |
| 5 | M07 ∥ M08 | AI × Skills/Heroes/Kael (contratos de combate fixados no M05) | |
| 6 | M06 ∥ M09 | UI × Audio | **A** |
| 7 | M10 ∥ M18 | Skills/Modifiers × Save | |
| 8 | M11 ∥ M13 | Status/Resonance × Equipment/Sets/Inventory | |
| 9 | M14 ∥ M16 | Editor/Simuladores × Run | **B** |
| 10 | M17 | Chefe fecha o slice | **C** |
| 11 | M19 ∥ M20 | Meta/Sanctuary × Meta/Progression | **D** |
| 12 | M21 ∥ M24 | Trilha heróis × trilha regiões | |
| 13 | M22 ∥ M25 | | |
| 14 | M23 ∥ M26 | | |
| 15 | M28 ∥ M27 | Catálogo × última região | **E**, **F** |
| 16 | M29 | | **G** |
| 17 | M30 ∥ M31 | | |
| 18 | M32 | | **H** |

Trilha de arte (A01–A05) começa após o Marco C e corre em paralelo às ondas 11–17; ela é conduzida pelo usuário/artistas, com apoio técnico do Claude (shaders, import, LOD, VFX Graph).

Regra de paralelismo: dois módulos de uma mesma onda só rodam juntos em worktrees separados; o segundo a terminar faz merge da branch de base antes de abrir PR. Se o plano de um módulo revelar sobreposição de arquivos com o par da onda, ele espera.

## 6. Como disparar um módulo

1. Confirme que todas as dependências estão ✅ na tabela de status (§7).
2. Abra uma sessão nova neste repositório (worktree próprio) e cole o **Prompt de disparo** do arquivo do módulo. Ele segue este formato:

```
Implemente o módulo Mxx do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/Mxx-<slug>.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/Mxx-<slug>.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

Para execução sem aprovação intermediária, troque "peça minha aprovação antes de executar" por "modo autônomo".

## 7. Status

| ID | Status | Atualizado | Nota |
|---|---|---|---|
| D01 | ✅ concluído | 2026-11-07 | 206 revisados (02/04 atualizados), 0 IDs removidos; script verde. 10 renomeações (LYR-SR-08 → Inward Spiral). Pendências P2–P13 em `reports/D01-divergencias.md` |
| D02 | ✅ concluído | 2026-11-07 | R53–R58 em 00 §5.6; pendências em §9 |
| M00 | ✅ concluído | 2026-11-07 | Unity 6000.3.28f1; CI GameCI verde (Core + EditMode + PlayMode). `build.yml` ativa após merge em `main` |
| M01 | ✅ concluído | 2026-11-07 | `Eclipse.Core`: Ids, Tags/HitFlags, RNG xoshiro128** + 5 streams, SimulationClock 60 Hz, EventBus 0 B, Definitions. dotnet 132/132, EditMode 131/131 |
| M02 | ✅ concluído | 2026-11-07 | CSV→SO→registro→validador; piloto Affinities (6) + String Table `Definitions`. dotnet 144/144, EditMode 188/188. Comando: `…DataPipeline.DataPipeline.ImportAndValidate`. `data-validate` no CI a confirmar |
| M03 | ✅ concluído | 2026-11-07 | `Eclipse.Core`: Stats (StatBlock, DR/caps), Attributes, Damage (ofensivo/recebido), CrowdControl, Procs. Exemplo 01 §3.12 reproduzido. dotnet 299/299, EditMode 297/297. Pendências no Log do M03 |
| M04 | ✅ concluído | 2026-11-07 | Tick 60 Hz, Input+InputBuffer 9 f, motor/mira, esquiva 06 §3.7, câmera Cinemachine+dither, CombatSandbox. EditMode 403/403, PlayMode 19/19. Checagem manual feita (2026-11-08) |
| M12 | ✅ concluído | 2026-11-07 | `Eclipse.Core`: Items (modelo, `ItemMath`) e Loot (gerador de 12 passos, pity); 15 CSVs + 35 nomeados (K1–K16, U1–U19). dotnet 460/460, EditMode 564/564, validador 0 erros. Raridade com chances absolutas de 05 §5.4; pendências no Log do M12 |
| M05 | ✅ concluído | 2026-11-08 | Contratos de combate congelados (`Gameplay/Combat/Contracts`), Greatsword do Kael na frame data de 06, hit detection, dano recebido, estados/Stagger, projéteis/áreas/DoT, feedback, pool, CombatSandbox. dotnet 462/462, EditMode 813/813, PlayMode 65/65 (Combat 46). Pendente: checagem manual; lacunas (lunge, Forte/Dash) no Log do M05 |
| M15 | ✅ concluído | 2026-11-08 | `Eclipse.Core.RunGen`: `MapGenerator` (13 linhas + chefe, 7 restrições de 09 §11.3), `EncounterPlanner` (orçamento, ondas, limites, receitas), `EncounterRuntimeRules`; 6 CSVs `DataSource/run` + SOs; Run Map Previewer. dotnet RunGen 24/24, EditMode 658/658. Pesos do mapa ajustados (TUNED) e divergências de 09 no Log do M15 |
| M07 | ✅ concluído | 2026-11-08 | `Eclipse.AI`: HFSM+Utility, tokens, locomoção Burst, telegraph, 4 inimigos Ashen Wastes, AISandbox, soak 200 (gameplay+IA média 2,6 ms). EditMode 1012/1012, PlayMode AI 17/17. Pendente: soak no hardware alvo (GC ≠ 0, física p99 > 2 ms), desvios de TTK/letalidade ao M14, teste visual do telegraph |
| M08 | ✅ concluído | 2026-11-08 | `Gameplay/Skills` (SkillController, SkillSpec v1 com ponto de extensão do M10, Deliveries/Effects/Triggers) e `Gameplay/Heroes` (Fury, passivas, 5 armas, `WeaponKitBonus`); 5 habilidades do Kael na CombatSandbox. EditMode 1052/1052, PlayMode Skills 14/14. Pendente: `ComboGcTests` (M05) instável (21–34 hits vs ≥40); frames/cone/arco/fissura provisórios (TUNED); stacks de Ruin Vulnerability do Worldbreaker (08); checagem manual de sensação |
| M06, M09–M11, M13–M14, M16–M32 | ⏳ não iniciado | 2026-11-07 | |
| A00 | ⏳ não iniciado | 2026-11-07 | |

Legenda: ⏳ não iniciado · 🔄 em andamento · ✅ concluído · ❌ bloqueado (com motivo).

## 8. Modelo do arquivo de módulo

Todo `docs/roadmap/Mxx-*.md` segue esta estrutura:

```markdown
# Mxx — <título>
Fase <n> · Onda <n> · Status: ⏳ não iniciado
Depende de: … · Desbloqueia: … · Paralelo com: …
Branch: modulo/Mxx-<slug>
Docs de referência: <doc §seção>, …

## Objetivo
## Critérios de saída (verificáveis)
## Fora de escopo
## Premissas e decisões
## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
## Ondas internas
## Briefs            (um por tarefa: Objetivo, Contexto, Arquivos, Restrições, Aceite, Verificação, Entrega)
## Verificação do módulo
## Riscos e rollback
## Atualizações de documentação
## Prompt de disparo
## Log
```

Executores: `investigador` (leitura/investigação), `executor` (implementação com escopo fechado), `principal` (decisão de design, integração e revisão, feitas pela sessão que conduz o módulo).

## 9. Notas de integração entre módulos

Regras que os planos assumem uns dos outros. Cada módulo confere as que o afetam na sua primeira tarefa.

**Contratos e donos de arquivo**
- **M01 (Core) para M02–M05/M18:** `StableId` não é serializável pelo Unity; o SO guarda `string` e expõe o `StableId` em cache (validar no import). As seeds de `RngStreams` são `ulong`, mas 11 §3.4.3 define `DropSeed` como `uint`, então o M12/M18 decide o truncamento. `RngStreams.Fork(stream, id, salt)` exige salt (contador) para que dois drops do mesmo item não rolem igual. `RngStreams` ainda não tem restore de estado para o suspend-save (o M18 adiciona, usando `Xoshiro128StarStar.GetState()` e o ctor de estado). `Get()` devolve `ref`, e `var g = streams.Get(...)` copia sem avançar o pai. `EventBus<T>` é classe com arrays e `Unsubscribe` O(n); se o M05 usar um bus por entidade, faça pooling.
- **M02:** importadores (`[TableImporter]`) e regras do validador (`[ValidationRule]`) são descobertos por atributo, um arquivo por tabela; nenhum módulo edita um arquivo central do pipeline. A partir do M10, modificador com ID inexistente vira erro (no M12 é aviso).
- **M03 (concluído):** `StatBlock.SumBucket/ProductMore` e `StatBlockDamageSource` ligam stats ao resolvedor; M05 cria `StatDefinition`/`Stats.csv` e alimenta `IStatRules`; Resource Generation/Cost Reduction (01 §3.8) ficam para o M08; o chamador aplica escalonamento de dificuldade e fornece `IsTelegraphed`/`BlockArcValid`/Parry. `DamageInstance` já nasce com todos os campos de 11 §3.5.2 (Generation, HitFlags, ProcCoefficient, HitGroupId) e o `StatId` inclui Status Potency, Status Resistance e Duration of Effects.
- **M05** congela `Gameplay/Combat/Contracts`, os asmdefs de teste e o `TagManager`; M07 e M08 não os editam. Cenas: `CombatSandbox` é do M08, `AISandbox` é do M07, e a cena do Marco A é do M06. O M05 entrega um `StatusStub` de Burning, substituído no M11.
- **M06:** comandos do console de debug são registrados por atributo, um arquivo por comando.

**Escopo deslocado**
- O M07 inclui um 4º inimigo (Cinder Husk) para o soak test e usa a curva de HP/dano de 06 §3.13 (R53, fechada pelo D02).
- O M08 inclui as 5 armas do Kael.
- O M13 cobre os efeitos especiais dos itens nomeados do M12 e o save de loadout/inventário (usa o M18).
- O Run Map Previewer (11 §3.15) fica no M15.
- O `balance-sim.yml` do M14 pressupõe o GameCI do M00.
- O M29 inclui Overrift/Delve Deeper, Nightmare Behaviors e Eclipse Phenomena (retirados de M16/M17/M20). Se o inventário inicial do M29 mostrar escopo grande demais, ele se divide em M29a (Ascension e Rift Sigils), M29b (Challenge Altar e Endless) e M29c (pinnacle).
- O M20 só roda a T9 (teste do Marco D) depois do merge do M19.

**Conteúdo**
- Heróis escrevem só `heroes/<herói>`, `items/named/<herói>.csv` e os próprios sets/modificadores; regiões escrevem `regions/<região>` e `universal_<região>.csv`. Região-lar fica na coluna `homeRegion` do set; fonte do Unique em `fonteUnique`.
- **Dependência fraca região → herói da mesma onda:** M24, M25 e M26 só executam a T10 (Uniques de chefe/região) depois do merge de M21, M22 e M23, respectivamente.
- O M23 autora o Unique O3 (Ashen Colossus); o M28 autora K8 e o restante do catálogo do Kael.
- O Marco F só fecha com M27 e M28 mergeados (a quota de 30 Uniques depende dos Uniques de M27).

**Trilha de arte e release**
- A trilha A00 usa branches `arte/Axx-<slug>` e o prompt de disparo do próprio `A00-trilha-arte.md`.
- **Pré-requisitos do M32 (decisões do usuário):** App ID da Steam, destino da telemetria opt-in e ferramenta de crash reporting.

**Lacunas numéricas (D02 fechou HP/dano por eLvl, DPS_ref, XP e Rift Shards por PA, fator Shards → Gold — R53–R58)**
- R58 (caps por alvo de KAE-BC-03 / LYR-SR-04, fragmentos de LYR-VO-04) é aplicado em `03-modificadores.md` depois do merge do D01.
- Os coeficientes `36` (HP) e `20` (dano) de R53 são recalibrados pelo simulador do M14; a forma da curva fica.
- 05 §4.3 ainda cita níveis esperados antigos (~15/30/40/44); o dono (10 §5.1, R40) diz ~25/35/45/48–50. Errata pendente para 05.
- 10 §10 "segundo herói ao 50 em 12–15 h" não fecha com Kindred Echo (~25 h no modelo do D02). Validar no M20/M14.
- 09 §11.2: a coluna "média esperada por run" soma mais que as 10 linhas livres; pelos pesos dá ~5 Combat / 1,6 Elite / 0,8 Treasure / 1,2 Shrine / 1,1 Event / 0,6 Challenge.

**Relatórios** de módulos de design e simuladores ficam em `docs/roadmap/reports/`.

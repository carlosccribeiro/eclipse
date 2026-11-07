# M20 — Progressão de herói, Aether Lattice e dificuldades
Fase 4 · Onda 11 · Status: ⏳ não iniciado
Depende de: M10, M16, M18 · Desbloqueia: M21, M22, M23, Marco D (com M19) · Paralelo com: M19
Branch: modulo/M20-progressao
Docs de referência:
- 08: §2 (decisões 4, 9 e 12), §4.5 (Augments de Kael), §4.7 (builds e Keystones de Kael), §8 (Aether Lattice: estrutura, pontos, gates, Notáveis e Keystones), §9 (ganhos por nível, curva, desbloqueio de heróis), §11.1 (exceções de teto de recurso)
- 10: §2 (decisões 6, 11, 13 e 17), §5.1 (XP de herói e Kindred Echo), §5.2 (Account Rank), §5.3 (desbloqueio de heróis), §5.4 (Hero Mastery), §6.7 (loadouts), §7.1 (tiers e requisitos), §7.2 (Monster Level e escala), §10 (curva de tempo)
- 09: §3.2 (tokens), §6.1–6.3 (afixos de Elite Veteran+ e quantidade por dificuldade), §10.1 (densidade)
- 05: §3.2 (quantidade por dificuldade), §4.1 (iLvl)
- 06: wind-up por dificuldade (00 §5 R14)
- 01: crescimento de vida/armadura por nível
- 11: §3.4.2 (`HeroDefinition`, `AugmentDefinition`, `DifficultyDefinition`), §3.4.3 (`HeroLoadout`), §3.13 (`heroes.sav`)
- 00 §5: R01, R03, R40, R41, R44

## Objetivo
Criar a progressão permanente do herói e da conta, e o sistema de dificuldades de campanha:
- XP e níveis 1–50 com a curva de 10, recompensas por nível de 08 e catch-up Kindred Echo;
- Hero Mastery e Account Rank (só desbloqueios, nunca atributos);
- a **Aether Lattice** completa como framework, com o conteúdo completo de Kael (núcleo, ramos, Notáveis, 15 Augments e 6 Keystones), respec grátis no Sanctuary;
- roster e desbloqueio de heróis, loadouts;
- dificuldades Normal → Eclipse com regras de desbloqueio, tabela de Monster Level e escalas de 10 §7, quantidade de afixos e os 9 afixos de Elite de Veteran e Nightmare de 09 §6.1.

Junto com M19, fecha o **Marco D**.

## Critérios de saída (verificáveis)
1. `XP(L) = 200 × L^1,8` com total até o nível 50 ≈ 3,9 M (±1%, 10 §5.1). Kindred Echo dá +100% de XP a heróis abaixo de (nível máximo da conta − 10). XP é 100% em qualquer desfecho (10 §4). Após o nível 50, XP vira Hero Mastery (250.000 por nível, 10 §5.4).
2. Ganhos por nível de 08 §9.1: slots Q/Shift no nível 1, E no 3, R no 5, Ultimate no 8, Augments no 10, Keystones nos níveis 20 e 35, +1 ponto por nível e crescimento de vida/armadura (fórmula de 01). Os 12 slots de equipamento ficam ativos desde o nível 1 (R29).
3. Aether Lattice (08 §8):
   - núcleo de 8 nós; 3 ramos × (10 menores + 5 Notáveis de 3 ranks + 5 Augments a 2 pontos + 2 Keystones a 3 pontos), ~131 pontos de custo total;
   - 55 pontos: 49 por nível + 6 por marcos;
   - gates: Tier 2 com 6 pontos no ramo, Tier 3 com 14; Keystone com 20 pontos no ramo **ou** 30 distribuídos entre dois ramos;
   - máximo de 2 Keystones ativos; 1 Augment ativo por habilidade (ativar um bloqueia os outros dois);
   - respec grátis só no Sanctuary e indisponível durante a run.

   Testes de propriedade validam gates e limites com 10.000 alocações aleatórias.
4. Conteúdo de Kael carregado de `DataSource/heroes/kael_lattice.csv`: ramos Inferno, Ruin e Juggernaut, com Notáveis e Keystones de 08 §8.3 e os 15 Augments de 08 §4.5.
   - Os 6 Keystones têm comportamento verificável em PlayMode: Combustion Rage, Ember Soul, Headsman, Doomsayer, Scarred Titan e Iron Bastion.
   - Augments recompilam o `SkillSpec` (M10) e não adicionam More. O validador rejeita Augment com More e Keystone com More acima de +20% sustentado (R03).
5. Account Rank: `AXP(R) = 3000 × R^1,5`; fontes de XP 1:1 + Codex (`CodexEntryAdded` de M19); recompensas da tabela de 10 §5.2 como flags de desbloqueio consultáveis (ex.: aba 2 do Vault no Rank 2, 4 opções de Shrine no Rank 30). As flags que tocam a run são aplicadas: oferta de Shrine com 3/4 opções, rerolls grátis de Shrine e Anchor +1 no Rank 50.
6. Heróis: Kael e Lyra disponíveis no início. Nyx é desbloqueado no evento `BossFirstKill(AshenColossus)` (M17) e Orun na primeira derrota do Pale Sovereign (flag, até M24). Fica registrado e persistido. Lyra, Nyx e Orun aparecem no roster como "em breve" até M21–M23.
7. Loadouts: 5 por herói (6 e 8 nos Ranks 5 e 25), cada um com itens + Augments + pontos + Keystones + Set Focus (11 §3.4.3). Trocar no Sanctuary é instantâneo.
8. Dificuldades:
   - `DifficultyDefinition` para Normal, Veteran, Nightmare e Eclipse, com tudo vindo de CSV:
     - eLvl por região (10 §7.2; aqui só a Ashen Wastes é jogável, mas a tabela tem as 5 regiões);
     - vida e dano relativos (resultado da curva de 06 §3.13: vida ≈1/3,2/8,9/22×, dano ≈1/2,4/4,3/7,9× — R53, R54);
     - quantidade de loot (05 §3.2), densidade (09 §10.1), afixos por Elite e matilhas (09 §6.2) e tokens (09 §3.2);
     - redução de wind-up limitada a −15% (R14);
     - retenção na morte (Normal 75% / Veteran+ 50%, 10 §4);
     - iLvl de drop (05 §4.1).
   - Requisitos de desbloqueio de 10 §7.1 avaliados por regras data-driven, com override de desenvolvimento enquanto The First Forgotten (M27) não existe.
   - Uma run em Veteran na Ashen Wastes usa o eLvl 25–30, Elites com 2 afixos e tokens/quantidade de Veteran. É verificada por teste PlayMode que lê o estado da run.
9. Afixos de Elite de Veteran e Nightmare de 09 §6.1 (Gravitic, Reverberant, Bastion, Phasing, Immovable e Sentinel; Soulbound, Prismatic e Carapaced) implementados, com incompatibilidades de 09 §6.3. Um teste PlayMode por afixo.
10. Integração (T9, depois do merge de M19):
    - estação Keeper com roster, respec, loadouts e Lattice;
    - Rift Navigator com seleção de dificuldade, implementando `IDifficultySelectionSource`;
    - `IAccountRankQuery` implementado para o Vault de M19.

    **Teste do Marco D:** Sanctuary → Keeper (alocar pontos, ativar Augment) → Rift Navigator (Veteran) → run → morte → XP aplicado, nível sobe, retenção de 50% → Sanctuary → respec → save/load preserva tudo.
11. EditMode + PlayMode passam e o validador está sem erros. Antes de T9, nenhum arquivo de `Meta/Sanctuary/**`, `Meta/Services/**`, `Meta/Vault/**` ou `Core/Economy/**` (par M19) foi tocado.

## Fora de escopo
- Ascension 1–100 e Thresholds (10 §7.3), Eclipse Trial, Rift Sigils, Nightmare Behaviors (09 §15.1) e Eclipse Phenomena (09 §15.2): M29. Nightmare e Eclipse funcionam aqui com escala, afixos e variações de chefe ✦/✧ (M17), sem os Behaviors/Phenomena.
- Lattice de Lyra, Nyx e Orun (M21–M23 usam este framework).
- Recompensas cosméticas do Account Rank (títulos, auras, transmog): ficam como flags; o conteúdo vai para M29/M31.

## Premissas e decisões
- Dono da curva de XP: 10 (R40). Os nomes do catch-up divergem: 10 chama de *Kindred Echo* e 08 §9.2 de *Eclipsed Memory*. Usar **Kindred Echo** (dono 10) e registrar a errata para 08 no Log.
- XP por inimigo é proporcional ao PA (09 §10.5); a constante foi **fechada pelo D02** em 10 §5.1 (R56: `2 × NívelDeConteúdo(eLvl)^1,1 ÷ MultDensidade`; Guardian 60 PA, chefe 150 PA). Gravar em `DataSource/progression/xp_per_threat.csv` e conferir contra 10 §10 com o simulador/bot (primeiro chefe ~nível 6 em 1,5–2 h), registrando no Log.
- O fator de dificuldade da conversão Rift Shards → Gold foi **fechado pelo D02** em 10 §3.3 (R57): Normal 1 · Veteran 3 · Nightmare 9 · Eclipse 19 · Ascension 19 × (1 + 0,02N).
- A Lattice é idêntica em estrutura para os 4 heróis (08 §8.1): o framework é genérico e o conteúdo é por CSV de herói.
- **Coordenação com M19 (paralelo):** M20 só edita `Save/Dto/HeroesDto.cs`; campos de profile (unlocks de dificuldade, Account Rank) vão em `ProfileDto` **depois** do merge de M19 (T9), com migração encadeada após a de M19.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Progressão em Core: XP, níveis, Kindred Echo, Mastery, Account Rank | execução | executor | — | `Assets/_Project/Code/Core/Progression/*.cs`, `DataSource/progression/*.csv`, `tools/CoreTests/Progression/*.cs` | ⏳ |
| T2 | Lattice em Core: grafo, gates, pontos, Keystone slots, respec | execução | executor | — | `Assets/_Project/Code/Core/Progression/Lattice/*.cs`, `tools/CoreTests/Progression/Lattice/*.cs` | ⏳ |
| T3 | Lattice runtime e dados: SO, efeitos de nó, Augments → SkillSpec, Keystones | execução | executor | T2 | `Assets/_Project/Code/Data/Heroes/Lattice/*.cs`, `Assets/_Project/Code/Gameplay/Heroes/Lattice/*.cs` | ⏳ |
| T4 | Conteúdo da Lattice de Kael e comportamentos dos 6 Keystones | execução | executor | T3 | `DataSource/heroes/kael_lattice.csv`, `Assets/_Project/Content/Heroes/Kael/Lattice/*`, `Assets/_Project/Content/Heroes/Kael/Augments/*` (só os que faltarem de M10), `Assets/_Project/Code/Gameplay/Heroes/Kael/Keystones/*.cs` | ⏳ |
| T5 | Dificuldades: definição, tabelas, regras de desbloqueio, aplicação na run | execução | executor | T1 | `Assets/_Project/Code/Core/Progression/Difficulty/*.cs`, `Assets/_Project/Code/Data/Difficulty/*.cs`, `DataSource/difficulty/*.csv`, `Assets/_Project/Content/Difficulty/*` | ⏳ |
| T6 | Afixos de Elite Veteran/Nightmare | execução | executor | T5 | `Assets/_Project/Code/AI/Elites/Veteran/*.cs`, `Assets/_Project/Content/Enemies/EliteAffixes/Veteran/*`, `DataSource/enemies/elite_affixes_veteran.csv` | ⏳ |
| T7 | Roster, desbloqueio de heróis, loadouts, aplicação do `RunResult` | execução | executor | T1, T3 | `Assets/_Project/Code/Meta/Progression/*.cs`, `Assets/_Project/Code/Save/Dto/HeroesDto.cs`, `Assets/_Project/Code/Save/Mapping/HeroesMapping.cs`, `Assets/_Project/Code/Save/Migration/*` (só migração de heroes) | ⏳ |
| T8 | UI de progressão: Lattice, level-up, roster, loadouts, seletor de dificuldade | execução | executor | T4, T5, T7 | `Assets/_Project/Code/UI/Progression/*.cs`, `Assets/_Project/UI/Progression/*` | ⏳ |
| T9 | Integração com o Sanctuary (após merge de M19) e teste do Marco D | execução | executor | T8, **M19 mergeado** | `Assets/_Project/Code/Meta/Sanctuary/Stations/KeeperStation.cs`, `Assets/_Project/Code/Meta/Progression/Adapters/*.cs`, `Assets/_Project/Code/Save/Dto/ProfileDto.cs` + migração, `Assets/_Project/Code/Tests/PlayMode/Progression/MarcoDLoopTest.cs` | ⏳ |
| T10 | Testes EditMode/PlayMode de progressão e dificuldade | execução | executor | T4, T6, T7 | `Assets/_Project/Code/Tests/EditMode/Progression/*.cs`, `Assets/_Project/Code/Tests/PlayMode/Progression/*.cs` (exceto `MarcoDLoopTest.cs`) | ⏳ |
| T11 | Revisão, calibração e Marco D | integração | principal | T9, T10 | `docs/roadmap/M20-progressao.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1 ∥ T2
2. T3 ∥ T5
3. T4 ∥ T6 ∥ T7
4. T8 ∥ T10
5. T9 (só depois do merge de M19 na base; se M19 ainda não terminou, M20 espera nesta tarefa)
6. T11

## Briefs

### T1 — Progressão em Core
Objetivo: `Eclipse.Core.Progression` puro, com:
- `XP(L) = 200 × L^1,8` e level-up, sem pular dois níveis com um ganho grande (aplica em laço);
- tabela de ganhos por nível (08 §9.1, via CSV);
- Kindred Echo: +100% abaixo de (máximo da conta − 10);
- Hero Mastery: 250.000 por nível após o 50;
- Account Rank: `AXP(R) = 3000 × R^1,5`, fontes 1:1 + bônus de Codex, recompensas da tabela de 10 §5.2 como `AccountUnlockFlag`;
- `ApplyRunResult(RunResult)` → XP do herói e da conta (100% em qualquer desfecho).
Arquivos: escreve `Assets/_Project/Code/Core/Progression/*.cs` (`HeroLevelCurve.cs`, `HeroProgress.cs`, `KindredEcho.cs`, `HeroMastery.cs`, `AccountRank.cs`, `AccountUnlocks.cs`, `XpAwarder.cs`), `DataSource/progression/*.csv` (`hero_levels.csv`, `account_rank.csv`, `xp_per_threat.csv` com R56) e `tools/CoreTests/Progression/*.cs`.
Restrições: Core puro (R52); números de 10/08 só em CSV e fixtures.
Aceite: total até o nível 50 ≈ 3,9 M; Rank 100 consistente com a fórmula; Kindred Echo nos limites (−9, −10, −11 níveis).
Verificação: `dotnet test tools/CoreTests --filter Category=Progression` → passa.
Entrega: no máximo 150 palavras.

### T2 — Lattice em Core
Objetivo: modelo genérico da Aether Lattice (08 §8.1–8.2):
- nós com tipo (Núcleo, Menor, Notável com ranks, Augment, Keystone), ramo, tier e custo;
- gates (Tier 2: 6 pontos no ramo; Tier 3: 14; Keystone: 20 no ramo **ou** 30 entre dois ramos);
- Augments em Tier 1 (2), Tier 2 (2) e Tier 3 (1, sempre o da Ultimate);
- slots de Keystone por nível (20 e 35), máximo de 2;
- exclusividade de Augment por habilidade;
- pontos disponíveis = (nível − 1) + marcos (máx. 6);
- `Allocate`, `Refund`, `RespecAll`, com respec bloqueado durante a run.
Arquivos: escreve `Assets/_Project/Code/Core/Progression/Lattice/*.cs` e `tools/CoreTests/Progression/Lattice/*.cs`.
Restrições: puro; `Validate(allocation)` retorna as violações; o grafo vem por parâmetro.
Aceite: testes de propriedade com 10.000 alocações aleatórias sem nenhuma alocação inválida aceita; casos-limite de gate e de exclusividade.
Verificação: `dotnet test tools/CoreTests --filter Category=Progression` → passa.
Entrega: no máximo 150 palavras.

### T3 — Lattice runtime e dados
Objetivo: `LatticeDefinition` (SO por herói, importado de CSV) → grafo de Core.
Efeitos:
- nós menores e Notáveis viram `StatMod` (bucket Increased) no StatBlock (M03);
- Augments ativam `AugmentDefinition` no canal "Augment" e recompilam o `SkillSpec` (M10, 11 §2 item 7);
- Keystones viram componente de comportamento registrado por `StableId` (implementações em T4).
Integração com o `HeroLoadout` (11 §3.4.3): pontos, Augment ativo por habilidade e Keystones.
Contexto: validador R03 (Augment com More = 0; Keystone com More ≤ +20% sustentado). Se o `DataValidator` de M02 aceitar regras plugáveis, crie `LatticeValidationRule.cs` na pasta de Data/Heroes/Lattice.
Arquivos: escreve `Assets/_Project/Code/Data/Heroes/Lattice/*.cs` e `Assets/_Project/Code/Gameplay/Heroes/Lattice/*.cs`.
Restrições: recompilar o `SkillSpec` só quando o loadout muda (11 §2 item 7).
Aceite: teste EditMode com uma Lattice de teste (2 ramos, 1 Augment, 1 Keystone): alocar → stats e `SkillSpec` mudam; respec → voltam ao estado inicial.
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T4 — Conteúdo de Kael
Objetivo:
- `kael_lattice.csv` completo: 8 nós de núcleo; ramos Inferno, Ruin e Juggernaut com 10 menores (valores alinhados ao tema; 08 §8.1 dá o exemplo +4% Increased) e os 5 Notáveis de cada ramo (08 §8.3).
- Os 15 Augments de 08 §4.5: reutilize os que M10 já criou e crie os que faltarem.
- Os 6 Keystones como componentes:
  - Combustion Rage: cada 10 Fury gastos → Explosion de 50% DA, Ember;
  - Ember Soul: 50% de Physical → Ember, e Burning pode critar;
  - Headsman: Execute de 15%, Elites 7,5%, chefe → conversão R08;
  - Doomsayer: 1 stack de Vulnerability de 4% (máx. 5) e −20% contra alvo com vida cheia;
  - Scarred Titan: Fury por dano recebido ×3, +20% de vida e −10% de Movement Speed;
  - Iron Bastion: 1% do Armor vira +1% Increased (máx. +40%) e imunidade a Knockback.
Contexto: os valores dos nós menores que 08 não lista um a um seguem o padrão do ramo e são marcados `DESIGN-FILL` no CSV para revisão do principal.
Arquivos: escreve `DataSource/heroes/kael_lattice.csv`, `Assets/_Project/Content/Heroes/Kael/Lattice/*`, `Assets/_Project/Content/Heroes/Kael/Augments/*` (só os faltantes) e `Assets/_Project/Code/Gameplay/Heroes/Kael/Keystones/*.cs`.
Restrições: o teto de Vulnerability de +60% (R12) e o modelo Generation (R07) valem para as Explosions do Combustion Rage.
Aceite: um teste PlayMode por Keystone verifica o efeito observável; importação sem erros.
Verificação: PlayMode → passa; `ImportAndValidate` → sem erros.
Entrega: no máximo 200 palavras, com a lista de valores `DESIGN-FILL`.

### T5 — Dificuldades
Objetivo: `DifficultyDefinition` (11 §3.4.2) para Normal, Veteran, Nightmare e Eclipse, a partir de `DataSource/difficulty/*.csv`:
- eLvl por região × dificuldade (10 §7.2), com eLvl = `iLvl_sala` interpolado pela posição da sala;
- vida e dano relativos (10 §7.2);
- quantidade (05 §3.2), densidade (09 §10.1), afixos por Elite e matilhas (09 §6.2) e tokens (09 §3.2);
- redução de wind-up limitada a −15% (R14);
- retenção na morte e fator de conversão de Shards (10 §3.3, R57);
- iLvl de drop (05 §4.1).
`DifficultyUnlockRules` data-driven (10 §7.1):
- Veteran exige First Forgotten em Normal;
- Nightmare exige First Forgotten em Veteran;
- Eclipse exige First Forgotten em Nightmare + Eclipse Trial;
- regiões: sequenciais em Normal; regiões 1–4 livres a partir de Veteran; Astral Citadel exige os outros 4 chefes.
Override de desenvolvimento no console (M06).
Aplicação: o `RunManager` (M16) lê a `DifficultyDefinition` selecionada; se M16 assumiu só Normal, ajuste o ponto de leitura com a mudança mínima e declare o arquivo.
Arquivos: escreve `Assets/_Project/Code/Core/Progression/Difficulty/*.cs`, `Assets/_Project/Code/Data/Difficulty/*.cs`, `DataSource/difficulty/*.csv` e `Assets/_Project/Content/Difficulty/*`. Pode editar no máximo 1 arquivo em `Assets/_Project/Code/Run/Flow/` (declarar).
Restrições: os números ficam só em CSV; a tabela de eLvl é cópia de 05 §4.1 (10 §7.2), e o validador confere que eLvl = iLvl.
Aceite: testes das regras de desbloqueio (tabela-verdade) e da tabela de eLvl; teste PlayMode com uma run em Veteran (critério 8).
Verificação: EditMode + PlayMode → passam.
Entrega: no máximo 150 palavras.

### T6 — Afixos de Elite Veteran/Nightmare
Objetivo: os 9 afixos de 09 §6.1 com dificuldade mínima Veteran ou Nightmare: Gravitic, Reverberant, Bastion, Phasing, Immovable, Sentinel, Soulbound, Prismatic e Carapaced, com assinatura visual, comportamento, counterplay e as incompatibilidades e regras de 09 §6.3 (Phasing × Immovable, Hasted × Immovable, Bastion × Prismatic, Soulbound exige 2+ Elites).
Contexto: framework de afixo de M16 (`EliteAffixDefinition`, `Code/AI/Elites/`). O Reverberant usa réplica `IsEcho` e respeita Generation (R07).
Arquivos: escreve `Assets/_Project/Code/AI/Elites/Veteran/*.cs`, `Assets/_Project/Content/Enemies/EliteAffixes/Veteran/*` e `DataSource/enemies/elite_affixes_veteran.csv` (CSV novo, para não editar o de M16).
Restrições: wind-up ≥ 0,45 s; telegraph na linguagem de 06.
Aceite: um teste PlayMode por afixo; um teste EditMode de 10.000 rolagens sem combinação incompatível.
Verificação: PlayMode + EditMode → passam.
Entrega: no máximo 150 palavras.

### T7 — Roster, desbloqueios, loadouts e `RunResult`
Objetivo:
- `HeroRoster`: Kael e Lyra no início. Nyx desbloqueia no `BossFirstKill(AshenColossus)` (M17) e Orun na flag do Pale Sovereign (10 §5.3; 08 §9.3). Heróis desbloqueados mas sem kit (M21–M23) ficam "em breve".
- `LoadoutService`: 5/6/8 loadouts por herói conforme o Account Rank (10 §6.7, §5.2), troca instantânea no Sanctuary.
- `ProgressionRunHook`: no `RunEnded` (M16), aplica XP do herói e da conta (T1), marcos da Lattice (first-kill de chefe com aquele herói → +1 ponto; 08 §8.2) e persiste via `SaveService` (M18).
Arquivos: escreve `Assets/_Project/Code/Meta/Progression/*.cs` (`HeroRoster.cs`, `LoadoutService.cs`, `ProgressionRunHook.cs`), `Assets/_Project/Code/Save/Dto/HeroesDto.cs`, `Assets/_Project/Code/Save/Mapping/HeroesMapping.cs` e a migração de heroes (incrementando `schemaVersion`).
Restrições: não edita `ProfileDto.cs` (posse de M19 nesta onda; ver Premissas).
Aceite: testes EditMode de desbloqueio por evento, limites de loadout e aplicação do `RunResult` (vitória, extração, morte).
Verificação: EditMode → passa.
Entrega: no máximo 150 palavras.

### T8 — UI de progressão
Objetivo: painéis em UI Toolkit, independentes do Sanctuary (abríveis pelo console de debug e por API, para M19/T9 embutirem):
- `LatticePanel`: grafo por ramo com gates visíveis, Augments por habilidade, slots de Keystone, prévia do efeito, respec e pontos restantes;
- `LevelUpToast`;
- `HeroRosterPanel`: seleção, bloqueados e "em breve";
- `LoadoutPanel`;
- `DifficultyPanel`: tiers com requisito e estado.
Contexto: framework de UI de M06; mouse e gamepad.
Arquivos: escreve `Assets/_Project/Code/UI/Progression/*.cs` e `Assets/_Project/UI/Progression/*`.
Restrições: a UI só envia comandos às APIs de T2/T5/T7.
Aceite: um teste PlayMode por painel com input simulado (alocar, respec, trocar loadout).
Verificação: PlayMode → passa.
Entrega: no máximo 150 palavras.

### T9 — Integração com o Sanctuary (depois do merge de M19)
Pré-condição: M19 mergeado na base e esta branch atualizada (merge da base, CLAUDE.md §7).
Objetivo:
- `KeeperStation`: abre Roster, Lattice e Loadout; respec só no hub.
- Implementar `IDifficultySelectionSource` (Rift Navigator de M19) com `DifficultyPanel` e as regras de T5.
- Implementar `IAccountRankQuery` (Vault de M19).
- Campos de profile (unlocks de dificuldade, Account Rank, first-kills por dificuldade, se ainda não existirem) em `ProfileDto` com migração encadeada **após** a de M19.
- `MarcoDLoopTest` (critério 10).
Arquivos: escreve os listados na tabela.
Restrições: não altera serviços de M19; só registra a estação e implementa interfaces.
Aceite: `MarcoDLoopTest` passa.
Verificação: PlayMode → passa; EditMode → passa.
Entrega: no máximo 150 palavras.

### T10 — Testes de progressão e dificuldade
Objetivo: completar a cobertura dos critérios 1–9:
- curvas, gates e Keystones;
- Account Rank e flags aplicadas na run (Shrine com 4 opções, rerolls grátis, Anchor +1);
- desbloqueios;
- tabela de dificuldades;
- afixos;
- a run em Veteran do critério 8.
Arquivos: escreve `Assets/_Project/Code/Tests/EditMode/Progression/*.cs` e `Assets/_Project/Code/Tests/PlayMode/Progression/*.cs` (exceto `MarcoDLoopTest.cs`).
Aceite: todos passam.
Verificação: EditMode + PlayMode → passam.
Entrega: no máximo 150 palavras.

### T11 — Revisão e Marco D (principal)
- Calibrar `xp_per_threat` contra a curva de tempo de 10 §10 com o simulador e bot de M14/M16 (primeiro chefe por volta do nível 6) e registrar no Log.
- Revisar os valores `DESIGN-FILL` de T4.
- Registrar as erratas: 08 §9.2 Eclipsed Memory → Kindred Echo; (as lacunas de XP por PA e fator de Shards foram fechadas pelo D02).
- Conferir o Marco D (ROADMAP §3) junto com M19.
- Atualizar o ROADMAP (§3 Marco D, §7) e o CLAUDE.md (`DataSource/progression/`, `DataSource/difficulty/`, categoria `Progression`).

## Verificação do módulo
```bash
dotnet test tools/CoreTests --filter Category=Progression
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
- **T9 bloqueada por M19 atrasado** → M20 fecha T1–T8 e T10 e fica 🔄 em T9. Quem terminar por último faz a integração.
- **Recompilação de `SkillSpec` cara** ao alocar pontos → recompilar só no confirmar, com prévia calculada sob demanda.
- ~~Lacunas de 10 (XP por PA, fator de Shards)~~ → fechadas pelo D02 (R56, R57). Pendência aberta pelo D02: "segundo herói ao 50 em 12–15 h" (10 §10) não fecha com Kindred Echo (~25 h no modelo) — validar no simulador.
- **Afixos Veteran com custo de performance** (Soulbound, Sentinel) → pooling e medição no soak test de M07.

## Atualizações de documentação
- `CLAUDE.md` §5: `DataSource/progression/`, `DataSource/difficulty/`, `DataSource/heroes/`. §6: filtro `Category=Progression`.
- `docs/ROADMAP.md` §3 (Marco D) e §7.
- Errata para `docs/gdd/08-herois.md` §9.2 (nome do catch-up), proposta ao usuário e não aplicada sem aprovação.

## Prompt de disparo
```
Implemente o módulo M20 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M20-progressao.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M20-progressao.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado. Lacunas conhecidas: constante de XP por PA e fator de conversão Rift Shards → Gold por dificuldade (10 §3.3) não tabelados em 10; nome do catch-up diverge entre 08 §9.2 e 10 §5.1.

# 11 — Arquitetura Técnica Detalhada (Unity)

**Dono:** 11-arquitetura · **Base:** GDD v0.2 §14 · **Contrato:** `00-convencoes.md`

---

## 1. Objetivo e escopo

Definir a stack técnica, a organização do projeto, o modelo de dados, os sistemas de runtime (stats, habilidades, modificadores, Set Powers, status/Resonance, combate, IA, runs, loot, save), metas de performance, ferramentas de editor, testes e a ordem de implementação.

Fora do escopo: valores de balanceamento (donos 01–10). Este documento define **onde** e **como** esses valores vivem e são avaliados.

---

## 2. Decisões tomadas

1. **Unity 6.3 LTS (6000.3.x)**, travada por projeto; upgrades só entre LTS e em branch dedicada. *Suporte longo, URP maduro, UI Toolkit runtime estável.* Fallback: Unity 6.0 LTS (6000.0.x) se algum pacote crítico não estiver certificado.
2. **URP** (Forward+), não HDRP. *Visual estilizado não precisa de HDRP; URP escala melhor para o hardware mínimo e para hordas.*
3. **GameObjects + MonoBehaviour como camada de apresentação, núcleo de regras em C# puro**, com hot paths (movimento de inimigos, projéteis, ticks de status) em **Jobs + Burst**. **Não** usar ECS/DOTS completo. *Produtividade de conteúdo e ferramentas > ganho marginal de ECS; Jobs/Burst cobre o gargalo real (hordas e projéteis).*
4. **Núcleo determinístico e testável sem Unity** (`Eclipse.Core`): stats, dano, modificadores, loot, Resonance e geração de runs rodam em EditMode tests e em simulação headless. *Balanceamento por simulação e testes rápidos.*
5. **Dados de definição em ScriptableObjects**, instâncias de runtime em classes/structs serializáveis puras. Todo SO de definição tem **StableId** (slug imutável) — nunca se salva referência de asset.
6. **Rolagens salvas normalizadas (Roll01, 0–1) dentro do tier de iLvl** (T1 = iLvl 1–20 … T5 = 81–100; tabelas de 01-atributos), não em valor absoluto. *Rebalancear faixas propaga para itens já existentes sem migração.* (00 §5 R21)
7. **Habilidades compiladas**: `SkillDefinition` + modificadores + Augments + Set Powers → `SkillSpec` imutável, recompilado apenas quando o loadout muda. *Zero custo de modificador no momento do cast.*
8. **Tags como bitmask `ulong`** (24 tags do contrato §3.4 cabem com folga em 64 bits). **HitFlags** (`Unavoidable`, `Unblockable`, `Annihilation`, `Execute`, `Knockdown`, `IsEcho`, `IsResonance`, `IsTriggered`) ficam em máscara separada de `DamageInstance` e **não** são tags (00 §5 R02). *Filtro de bucket Increased por hit em O(1); vocabulário de tags inalterado.*
9. **IA: HFSM por inimigo + seleção de ataque por Utility**; chefes com **HFSM de fases + sequenciador de padrões**. Sem Behavior Trees para inimigos comuns. Decisão a 10 Hz (perto) / 4 Hz (longe). *Custo previsível com 150+ inimigos; BT só complica arquétipos simples.*
10. **Hit detection por janelas de animação + queries físicas NonAlloc**; projéteis simulados por um manager com `SpherecastCommand` em lote. *Precisão e performance; projéteis não usam Rigidbody.*
11. **Salas autorais (prefabs via Addressables) + mapa de nós gerado por seed**. Sem geração procedural de geometria. *Qualidade de arena e legibilidade de combate > variedade de layout.*
12. **Save em JSON (Newtonsoft) compactado (GZip) + checksum + escrita atômica + versão de schema com migrações encadeadas**. *Depurável, versionável, robusto a crash.*
13. **Áudio: FMOD Studio**. *Trilha dinâmica por intensidade, eventos por afinidade e licença indie viável.*
14. **UI: UI Toolkit** para telas e HUD; **renderer próprio instanciado** para números de dano e barras world-space. *UI Toolkit para layouts complexos de itemização; world-space em massa precisa de batching próprio.*
15. **Sem Odin Inspector**: inspectors e janelas em UI Toolkit + pipeline de importação de planilhas. *Sem dependência paga obrigatória para toda a equipe/terceirizados.*
16. **RNG por streams com seed** (xoshiro128**): `Map`, `Encounter`, `Loot`, `Combat`, `Cosmetic`. *Reprodutibilidade de bugs e de seeds de desafio diário.*
17. **Suspend-save na entrada de cada sala** (save & quit no meio da run), apagado ao carregar. *Sessões longas (25–35 min) sem permitir save-scumming.* Regra de morte em si: 10-progressao.
18. **Simulação de combate a 60 Hz fixos** (`Time.fixedDeltaTime = 1/60`), igual à frame data de 06-combate. *Frame data de 06 é expressa em frames a 60 fps; elimina conversão e arredondamento de janelas de hit/i-frames.* (00 §5 R04)
19. **Anti-recursão pelo modelo Generation de 07** (`Generation` 0–2 + flag `IsEcho`), com exceção controlada `EchoRecursion1x` declarada por poder. *Regra única para triggers, Resonance, cadeias e ecos.* (00 §5 R07)
20. **Pipeline de modificadores em 6 fases de 03** (Conversão → Forma → Parâmetros → Replicação → Gatilhos → Recurso). *03 é dono da semântica; a arquitetura só a executa.* (00 §5 R47)
21. **`StatMod` com buckets `Flat`, `Increased`, `More`** (sem `Override`) e resolver de dano por componente idêntico a 06 §3.2 (dano plano adicionado, ganho-como-extra, conversão, teto de Vulnerability +60%). (00 §5 R12, R47)

---

## 3. Corpo do design

### 3.1 Stack e pacotes

| Área | Escolha | Observação |
|---|---|---|
| Engine | Unity 6.3 LTS | Editor travado via `ProjectVersion.txt` |
| Render | URP Forward+ | 2 perfis: `PC_Low`, `PC_High` |
| Shaders/VFX | Shader Graph, VFX Graph (GPU) + Particle System (CPU) | VFX Graph para hordas de efeitos; Shuriken para efeitos com colisão/lógica |
| Input | Input System (Action Maps: `Gameplay`, `UI`, `Debug`) | Mouse+teclado e gamepad desde o dia 1 |
| Câmera | Cinemachine 3 | Câmera isométrica fixa com leve follow e shakes por impulso |
| Conteúdo | Addressables | Regiões, salas, heróis e VFX em grupos separados |
| Navegação | AI Navigation (NavMesh) + avoidance própria | NavMesh por sala, baked no prefab |
| Animação | Animator + Animation Rigging; Timeline para cutscenes e intros de chefe | Root motion só em ataques com deslocamento |
| Performance | Burst, Collections, Mathematics, Jobs | |
| Serialização | `com.unity.nuget.newtonsoft-json` | |
| Localização | Unity Localization | pt-BR e en-US no lançamento; tabelas de string geradas pelo importador |
| Áudio | FMOD for Unity | Bancos por região e por herói |
| Plataforma | Steamworks.NET | Achievements, Cloud Save, Rich Presence |
| Testes | Unity Test Framework (EditMode + PlayMode) | CI headless |
| Profiling | Profiler, Memory Profiler, Frame Debugger | |

### 3.2 Estrutura de pastas

```
Assets/
  _Project/
    Code/
      Core/            // C# puro: Stats, Damage, Tags, Rng, Loot, Modifiers, Resonance rules, RunGen
      Data/            // Classes de definição (SO) — só dados, sem lógica de cena
      Gameplay/        // Entidades, Combat, Skills runtime, Status, Hitboxes, Projectiles
      AI/              // HFSM, Utility, Boss sequencer, Spawning
      Run/             // RunManager, RoomFlow, Encounter director
      Meta/            // Sanctuary, Progression, Economy, Vault
      Save/
      UI/
      Audio/
      Platform/        // Steam, achievements, cloud
      Editor/          // Validadores, importadores, janelas
      Tests/
        EditMode/
        PlayMode/
    Content/
      Heroes/<Hero>/   // HeroDefinition, Skills, Weapons, VFX, Anim
      Items/           // Bases, Named, Sets, AffixPools
      Modifiers/
      Affinities/      // StatusEffects, ResonanceRules
      Enemies/<Region>/
      Bosses/
      Regions/<Region>/Rooms, Events, Encounters, LootTables
      Sanctuary/
    Art/  Audio/  UI/  Settings/  Scenes/
  Plugins/
DataSource/            // fora de Assets: CSV exportados das planilhas (versionados)
```

### 3.3 Assembly Definitions

| Assembly | Depende de | Regra |
|---|---|---|
| `Eclipse.Core` | nenhuma (C# puro, `noEngineReferences: true`, sem pacotes Unity; matemática própria) | Lógica pura; compilado também por `tools/CoreTests` (dotnet) para testes rápidos (00 §5 R52) |
| `Eclipse.Data` | Core | SOs de definição; implementam interfaces de Core |
| `Eclipse.Gameplay` | Core, Data | Componentes de cena |
| `Eclipse.AI` | Core, Data, Gameplay | |
| `Eclipse.Run` | Core, Data, Gameplay, AI | |
| `Eclipse.Meta` | Core, Data, Run | |
| `Eclipse.Save` | Core, Data | Só DTOs e serialização |
| `Eclipse.UI` | Core, Data, Gameplay, Meta | Nunca o contrário |
| `Eclipse.Audio` | Core, Gameplay | Ouve eventos; não é referenciado por Gameplay |
| `Eclipse.Platform` | Core, Save | |
| `Eclipse.Editor` | todos (Editor-only) | |
| `Eclipse.Tests.EditMode` / `.PlayMode` | conforme necessidade | |

Regra: dependências apenas "para baixo". UI e Audio reagem a eventos; Gameplay não sabe que existem.

### 3.4 Modelo de dados — Definição × Instância

**Princípio:** SO = o que algo *é* (imutável em runtime). Instância = o que o jogador *tem* (serializável, mutável, salva).

#### 3.4.1 Base comum

```csharp
public abstract class Definition : ScriptableObject {
    [SerializeField] string stableId;      // "item.ashfang", "mod.cleaving_strike.echo_slash" — imutável após release
    [SerializeField] LocalizedString displayName;
    [SerializeField] LocalizedString description;
    public string StableId => stableId;
}
```

- `DefinitionRegistry` (gerado no build pelo editor): `StableId → Definition`, carregado via Addressables label `defs`.
- Renomear arquivo não quebra nada; mudar `stableId` exige entrada em `IdRemapTable` (migração).
- Convenção de ID: `<tipo>.<escopo>.<nome>` em snake_case.

#### 3.4.2 Definições (ScriptableObjects)

| Classe | Campos principais |
|---|---|
| `AffinityDefinition` | Id (Ember…Ruin), cor primária/secundária, tag, ícone, evento FMOD |
| `StatDefinition` | `StatId` (enum gerado), categoria (Primary/Offensive/Defensive/Utility), formato de exibição, cap duro, curva de DR (ref. 01-atributos), tags de filtro (ex.: `Ember Damage` → filtro `Ember`) |
| `HeroDefinition` | StableId, `ResourceDefinition`, stats base por nível (curva 1–50; velocidade base por herói de 01), lista de `SkillDefinition` (básico, pesado, 4 habilidades Q/E/R/Shift, Ultimate F — 06 §input / 08), `WeaponCategory[]` permitidas (só Main Hand), `OffHandCategory` (Warbrand/Grimoire/Quiver/Idol — 02), passivas, árvore Aether Lattice com Augments e Keystones (ref. 08-herois), lista de exceções de teto de recurso (00 §5 R01) |
| `ResourceDefinition` | Tipo (`Pool` Fury/Echo Energy, `Charges` Arcane Charges, `MassToConstructs` Void Mass → Void Constructs), máx base (Charges/Constructs: base 6, teto absoluto 8 só via exceções declaradas em 08), regras de geração/decaimento, tradução de `Maximum Resource`/`Resource Generation`/`Resource Cost Reduction` (ref. 01-atributos §tradução) |
| `WeaponDefinition` | Categoria, herói, dano base e Weapon Power por iLvl (02), attack speed base, alcance, `AttackChain` (moveset de 08; frame data de 06: animações, hitboxes, frames, multiplicadores), tags, implícitos, classe de massa/peso |
| `SkillDefinition` | StableId, tags, custo, cooldown, coeficientes (× Weapon Power — 06), `DeliveryDefinition`, `EffectDefinition[]`, `TriggerDefinition[]`, slots de modificador aceitos (03: 3 por habilidade, 2 em Ultimate/Basic/Heavy), Augments disponíveis (08) |
| `SkillModifierDefinition` | StableId, categoria (Behavior/Elemental/Chain/Echo/Area/Conversion/Trigger/Resource/Duration), skill-alvo **ou** filtro de tags, fase (1–6 de 03), `ModifierOp[]` ordenadas, parâmetros roláveis com faixa, grupo de exclusividade (Forma/Conversão/Modo de uso), raridade mínima (E/L), flag `Signature` (exclusivo de Unique), `HeroId` (hero-bound) ou `GEN` |
| `AugmentDefinition` | Skill-alvo, build associada, `ModifierOp[]` (canal "Augment"; 1 ativo por habilidade; nunca adiciona More — 00 §5 R03) |
| `ItemBaseDefinition` | Slot, categoria (ex.: Greatsword, Plate Chest, Grimoire), família de armadura (Plate/Leather/Vestment), implícito (Armor implícito pela fórmula de 01), requisitos (nível exigido = ceil(iLvl × 0,5) — 05), visual |
| `ItemDefinition` (nomeado) | Base, raridade (Epic/Legendary), `ItemKind` (`NamedLegendary` / `SetPiece` / `Unique`), estrutura de 00 §5 R25: Legendary = 4 afixos fixos + 1 Afixo Aberto (3 opções) + 2 modificadores fixos + 1 efeito; SetPiece = 4 fixos + 1 Aberto + 2 modificadores, sem efeito; Unique = 5 fixos + 2 modificadores (até 1 `Signature`) + efeito. `UniqueEffect` (lista de `ModifierOp`/`TriggerDefinition`, More declarado e validado contra os tetos de R03), Faixa Nomeada (piso 70% / teto 115% do máximo do tier — 01), `SetDefinition` opcional, restrição de herói (hero-bound), `fonteUnique` (chefe/Pinnacle/pool regional/Challenge — 02/05), `minDropILvl`, região-lar (05) |
| `AffixDefinition` | `StatId`, tabela fixa por tier T1–T5 (min/max — 01), multiplicador de slot (tabela única de 01), grupo (não repetir mesmo grupo) |
| `AffixPool` | Slot → lista ponderada de `AffixDefinition` (pool e pesos: dono 02; máx. 1 primário por item, Amulet 2, nenhum em Ring); flag `*` para o afixo fora do pool permitido em peças de set |
| `RarityDefinition` | Contagem de atributos e modificadores (contrato §3.5), cor, beam de drop, SFX |
| `SetDefinition` | Herói, 6 `ItemDefinition` (slots fixos, nunca Main/Off Hand — 04), `SetPowerDefinition` para 3 e 6 peças, região-lar (05) |
| `SetPowerDefinition` | `PowerSource`: stat mods, `ModifierOp[]` em canal próprio, triggers, transformação (ex.: Ashen Ascension como `StateDefinition`), More declarado, flag `EchoRecursion1x` (só poderes que a declaram — 00 §5 R07) |
| `StatusEffectDefinition` | Id, afinidade, primário/secundário, aplicação (Buildup/Chance/Garantida — 07), política de stacking, duração, tick (DoT 0,5 s — 06), efeito por stack, grupo de CC (Hard/Root/Silence — DR de 06), conversão contra chefe (Stagger, ou exceção nomeada Frostbound/Distortion — 07), VFX/SFX |
| `ResonanceRule` | Condição (status A presente + hit com tag/máscara de afinidade B, ou A+B no alvo), efeito resultante, ICD por alvo, limite global, prioridade (07) |
| `EnemyDefinition` | Arquétipo, stats relativos (HP/Dano × curva por Monster Level de 10), custo em PA (09), classe de massa, barra de Stagger (06), `AttackDefinition[]` com scores de utility e tipo de telegraph (06), HFSM template, resistências e resistência a status (07), classe de loot (05), tags |
| `EliteAffixDefinition` | Comportamento adicionado, telegraph visual, incompatibilidades, dificuldade mínima (09) |
| `BossDefinition` | Fases (limiar de vida, padrões, arena modifiers), Break/Broken (06), variações ✦/✧ por dificuldade (09), soft enrage, recompensas (3 Uniques de chefe, Heart of [Chefe] — 02/10) |
| `RoomDefinition` | Tipo (9 de 09: Combat, Elite, Guardian, Treasure, Shrine, Event, Challenge, Rest, Boss; Antecâmara é variante de Rest), prefab Addressable, spawn points, tamanho, região, tags de encontro |
| `EncounterDefinition` | Receita de encontro, orçamento de ameaça em PA, ondas, composição permitida, teto de unidades vivas (09) |
| `RegionDefinition` | Pools de salas, inimigos, eventos, tabelas de loot, sets e Uniques com região-lar, chefe, música |
| `EventDefinition` | Escolhas, condições, consequências (como `EffectDefinition`; custos em Rift Shards — 10) |
| `BlessingDefinition` / `ShrineDefinition` | Rank (Lesser/Greater/Exalted), afinidade, efeito, More declarado (≤ 8% por Blessing, ≤ 20% total — 09) |
| `ServiceDefinition` | Serviço do Sanctuary ou da Rest Room (cura 40%, recarga do Flask, Rift Anchor, Anchor, Temper — 10), custos e curvas |
| `LootTableDefinition` | Entradas ponderadas, condições (dificuldade, região, pity, Attunement) — ref. 05-loot |
| `DifficultyDefinition` / `AscensionDefinition` | Multiplicadores, Monster Level, modificadores de incursão, iLvl alvo, tokens de ataque (09), afixos de Elite por dificuldade (09), redução máx. de wind-up — ref. 10-progressao |

#### 3.4.3 Instâncias de runtime (serializáveis)

```csharp
[Serializable] public sealed class ItemInstance {
    public Guid InstanceId;
    public string DefinitionId;         // ItemDefinition (nomeado) ou ItemBaseDefinition (genérico)
    public Rarity Rarity;
    public int ItemLevel;               // 1–100; Tier = ceil(ItemLevel / 20) → T1..T5 (01)
    public string BoundHeroId;          // hero-bound (02/03): null = qualquer herói
    public List<RolledAffix> Affixes;   // tamanho = RarityDefinition.AttributeCount
    public byte OpenAffixChoice;        // índice da opção do Afixo Aberto (0–2) em Legendary/SetPiece
    public List<RolledModifier> Modifiers;
    public int UpgradeLevel;            // Blacksmith +0..+10 (10)
    public ItemFlags Flags;             // Locked, New, Favorite, Primal, Anchored…
    public int EnchantedAffixIndex = -1;// lock do Enchanter (10)
    public ushort EnchantCount;         // custo crescente do Enchanter (10)
    public ushort RefineCount;          // Refine de modificador (10)
    public ushort ImbueCount;           // Imbue do Enchanter (10)
    public uint DropSeed;               // seed do Loot stream que gerou o item
    public LootSourceId Source;         // fonte do drop (sala, chefe, Challenge, Cache…) — 05
}
[Serializable] public struct RolledAffix   { public string AffixId; public byte Tier; public float Roll01; public bool Named; }
[Serializable] public struct RolledModifier{ public string ModifierId; public float Roll01; }
```

- Valor efetivo calculado em leitura — **decisão 6**:
  - Afixo aleatório: `lerp(tier.min, tier.max, Roll01)` × multiplicador de slot (01).
  - Afixo de Faixa Nomeada: `lerp(0,70 × tier.max, 1,15 × tier.max, Roll01)` (01, 00 §5 R24).
  - Upgrade: `× (1 + 0,03 × UpgradeLevel)` sobre atributos e implícito; modificadores não sobem (00 §5 R31).
- O piso de Roll01 por dificuldade e a qualidade **Primal** (rolagens 85–100%, nunca acima do máximo) são aplicados **na geração** (05); a leitura não conhece dificuldade.
- `HeroLoadout`: 12 slots → `Guid` de `ItemInstance` (nenhum slot bloqueado por nível — 00 §5 R29), modificadores genéricos atribuídos a habilidades, Augment ativo por habilidade, pontos da Aether Lattice, Keystones, **Set Focus** (04).
- `RunState`: seed, mapa (13 linhas + chefe), nó atual, Blessings/Curses, Rift Shards, Satchel (30 espaços — 10), itens com Anchor, cargas do Aether Flask, estado do herói.

### 3.5 Sistema de Stats

#### 3.5.1 Representação

```csharp
public struct StatMod {
    public StatId Stat;
    public ModBucket Bucket;   // Flat, Increased, More (sem Override — 00 §5 R47)
    public float Value;
    public TagMask RequiredTags;   // vazio = sempre; ex.: Ember Damage → Ember
    public TagMask ExcludedTags;
    public ConditionId Condition;  // opcional: "alvo congelado", "Fury ≥ 50" — avaliado por hit
    public SourceHandle Source;    // item, set, passiva, buff… (remoção por fonte)
}
```

- `StatBlock` por entidade: lista de `StatMod` agrupados por `StatId`, com **dirty flag por stat**.
- Fontes registram/removem mods por `SourceHandle` (`AddSource(handle, mods)` / `RemoveSource(handle)`).

#### 3.5.2 Avaliação (contrato §3.2)

Valor de stat "plano" (vida, armor, attack speed):
```
v = (Base + ΣFlat) × (1 + ΣIncreased) × Π(1 + More)   → aplica cap/DR do StatDefinition (01-atributos)
```

Dano por hit (no `DamageResolver`, Core) — espelho exato de 06 §3.2, avaliado **por componente de dano**:
```
1 Base          = Basic/Heavy: DanoDaArma (rolado) × CoefDoGolpe | Habilidade: WeaponPower × CoefDaHabilidade
                  + dano plano adicionado (efeitos "adiciona X de dano")
2 Componentes   = 2a ganho-como-extra (sobre a Base pré-conversão) → novos componentes
                  2b conversão (soma ≤ 100%, excedente normalizado; sem cadeia)
3 IncreasedSum  = Σ Increased cujo RequiredTags ⊆ (tipo final do componente ∪ hit.Tags) e Condition verdadeira
4 MoreProduct   = Π (1 + More) com o mesmo filtro
5 Crit          = uma rolagem por hit (Combat stream), aplicada a todos os componentes; Ticks não critam
6 Mitigation    = por tipo: Physical = 1 − (1 − ArmorDR) × (1 − PhysRes); Elemental Res; Void Res (Void/Echo/Ruin)
                  ArmorDR = Armor' / (Armor' + 50·eLvl + 500), Armor' = Armor − Penetration   // 01-atributos
7 Vulnerability = min(Σ Vulnerability% no alvo, 60%)   // teto de 07 (00 §5 R12): Ruin stacks, Staggered,
                  // Broken, Distortion e conversão de Execute contra chefe entram aqui
8 Final         = Σ componentes; mínimo 1 por hit; arredonda só na exibição
```
- `DamageInstance` (06 §11): fonte, alvo, componentes `{tipo, valor}`, `TagMask`, `HitFlags`, Hit/Tick, `isCrit`, `procCoefficient`, `generation`, `staggerValue`, `hitGroupId`, `SourceChain`.
- Dano recebido pelo jogador segue a ordem de 06 §3.3: Dodge Chance (ignorada por `Unavoidable` e por Ticks) → mitigação → Block −40% (ignorado por `Unavoidable`/`Unblockable`) → Damage Reduction (cap 40% — 01) → Shield → vida. **I-frames evitam `Unavoidable`**; só `Annihilation` exige sair da área (00 §5 R13).

#### 3.5.3 Cache

- **Snapshot por skill:** ao compilar o `SkillSpec`, pré-soma-se `IncreasedSum`/`MoreProduct` dos mods **incondicionais** filtrados pelas tags da skill. Mods condicionais ficam numa lista curta avaliada por hit.
- Invalidação: `StatBlock` emite `StatsChanged(mask)`; specs que dependem do stat se marcam dirty e recompilam no próximo frame (não no mesmo frame de várias trocas de equipamento).
- Buffs temporários de alta frequência (ex.: stacks de Fury) entram como mods condicionais, não invalidam snapshot.

### 3.6 Habilidades e modificadores

#### 3.6.1 Composição de uma habilidade

```
SkillDefinition
 ├─ Delivery     : MeleeArc | Projectile | AreaAtPoint | AreaAtSelf | Leap | Dash | Beam | Summon | Trap | Channel
 ├─ Effects[]    : Damage | ApplyStatus | ApplyState | Displace(Pull/Push) | Heal | Shield | GainResource | Spawn(Entity) | Buff
 └─ Triggers[]   : (Evento, Condição, Effects[], ICD)
```

Todos são classes `[SerializeReference]` polimórficas com inspector próprio; designers montam skills sem código novo. Comportamentos realmente únicos (ex.: Echo Clone replicando inputs) são um novo `Delivery`/`Effect` em código, reutilizável por qualquer skill.

#### 3.6.2 Pipeline de compilação

```
SkillDefinition
  + ModifierOps de itens (Epic/Legendary/Unique)  canal "Item"     (máx. 3 por habilidade, 2 em Ultimate/Basic/Heavy; 14 ativos por herói — 03)
  + ModifierOps do Augment ativo (Aether Lattice) canal "Augment"  (1 por habilidade — 08)
  + ModifierOps de Set Powers                     canal "Set"      (não ocupa slot de item — GDD §6.6 regra 6)
  + ModifierOps de passivas/Keystones / Blessings canal "Passive"/"Run"
  → resolver exclusividade e duplicatas (03)
  → ordenar por Phase (1–6) e, dentro da fase, por canal (Item → Augment → Set → Passive → Run) e StableId
  → aplicar sobre SkillSpecBuilder
  → SkillSpec (imutável, cacheado)
```

**Fases de `ModifierOp`** — as 6 fases de 03-modificadores §3.4 (ordem fixa, resolve conflitos de forma determinística; 00 §5 R47):

| Phase | Categorias (03) | Ops | Exemplo |
|---|---|---|---|
| 1 Conversão | Conversion, Elemental (grupo C) | `ConvertAffinity(%)`, `AddTag`/`RemoveTag` de afinidade, máscara de Resonance | Ruin Cleaver |
| 2 Forma | Behavior (grupos F/M) | `ReplaceDelivery`, `SetUseMode(charge/channel/charges)`, `AddEffect` estrutural | Infernal Collapse: arco → explosão concentrada |
| 3 Parâmetros | Area, Duration, Elemental (adição) | `ScaleParam(radius/range/duration/count, %)` (área: raio × √(1+X) — 06), `SetParam`, `AddDamageAs(afinidade, %)`, `SpawnGroundZone` | Executioner's Reach, Burning Arc |
| 4 Replicação | Chain, Echo | `AddProjectiles`, `AddChain`, `AddRepeat(Echo)` — cada cópia herda as fases 1–3 | Echo Slash |
| 5 Gatilhos | Trigger | `AddTrigger(evento, condição, efeitos, ICD)` sobre o resultado final | Volatile Impact |
| 6 Recurso | Resource | `ModifyCost`, `ModifyCooldown`, `ResourceOnCondition` | Blood Fury |

- Regras de stacking (mesmo modificador duas vezes = vale a maior rolagem; um por grupo exclusivo Forma/Conversão/Modo de uso): definidas por 03-modificadores; a arquitetura suporta `ExclusivityGroup` + política `HighestRoll | Additive | FirstWins`.
- Conversão de afinidade é aplicada **antes** de calcular tags finais, então `Ember Damage` passa a valer para a parte convertida. Conversão ≥ 50% concede a tag da afinidade; abaixo disso, só a **máscara de Resonance** (`ResonanceAffinityMask`) recebe a afinidade, sem alterar `TagMask` (03 §3.5).

#### 3.6.3 Execução (runtime)

```
Input (buffer 150 ms / 9 frames, prioridade Esquiva > Habilidade > Forte > Básico — 06) → SkillController.TryCast(spec)
  → valida custo/cooldown/estado (Stun, Freeze, Silence, Root para Movement)
  → evento OnSkillCast
  → Animação (windup/active/recovery) — janelas definem quando Delivery dispara
  → Delivery cria DamageInstance(s) com Generation = 0
  → DamageResolver + StatusSystem + StaggerSystem
  → eventos OnHitDealt / OnCritDealt / OnKill / OnStatusApplied …
  → TriggerSystem avalia triggers inscritos (só se Generation < 2)
  → OnCastEnd
```

#### 3.6.4 Hooks de evento (vocabulário fechado)

Unificado com 06-combate §9 (os nomes de 06 são canônicos; 00 §5 R47):

- **Combate (06):** `OnHitDealt, OnCritDealt, OnTickDealt, OnKill, OnOverkill, OnExecute, OnStaggerApplied, OnStaggerBreak, OnCCApplied, OnDodge, OnPerfectDodge, OnBlock, OnParry, OnEvade, OnHitTaken, OnShieldBroken, OnLowHealth, OnFlaskUsed, OnDeath, OnRevive, OnSkillCast, OnSkillHit, OnResourceSpent, OnResourceGained, OnWallImpact`
- **Complementares (11):** `OnCastEnd, OnStatusApplied, OnStatusExpired, OnResonance, OnResourceFull, OnResourceEmpty, OnSummon, OnSummonDeath, OnMovementSkill, OnStateEnter, OnStateExit, OnRoomStart, OnRoomClear, OnTick(interval)`
- `OnRevive` só é emitido pela Blessing específica de 09 (não há revive padrão — 10).
- Novos hooks exigem entrada neste documento e no validador.

#### 3.6.5 Prevenção de recursão e explosão combinatória (modelo Generation — 07 / 00 §5 R07)

- `DamageInstance.Generation`: ação direta do jogador = **0**; cada proc (trigger, Resonance, chain, explosão, repetição Echo, zona gerada) = origem **+1**. **Nada dispara em Generation ≥ 2**: eventos de Gen 2 aplicam status a 50% de Status Potency e não avaliam triggers, Resonances nem consumos. *(Corrige o off-by-one anterior: o teste é `generation >= 2 → não avaliar`, não `depth > 2`.)*
- `HitFlags.IsEcho`: réplicas (Repeat, Clone, zonas eco) **nunca** geram novas réplicas. Exceção única: `PowerSource` com flag `EchoRecursion1x` (Thousand Echoes 6pc / Infinite Reflection — 04; Echo Chamber — 08) permite que uma réplica replique **uma** vez, ainda sujeita a `Generation < 2`. O validador rejeita a flag em qualquer outra fonte.
- **Proc Coefficient** por tipo de instância: repetição Echo 0,5; cópia de Clone 0,4; tick de área persistente 0,25; demais conforme a fonte (06/03).
- `SourceChain` (até 4 `SourceHandle`): um trigger nunca dispara a partir de um hit que ele próprio originou.
- **ICD obrigatório** em todo trigger (mínimo 0,1 s; default 0,5 s) e em toda `ResonanceRule` por alvo; uma Resonance por evento por alvo (prioridade de 07).
- Tetos globais (07): **8 procs de Resonance/s por jogador**; máx. 2 zonas eco por jogador; máx. 3 Clones; teto de entidades geradas por trigger por segundo (default 30) por fonte — excedente descartado e logado em debug.

### 3.7 Event Bus

- **Dois níveis:**
  - `EntityEvents` (por entidade, struct de listeners por hook) — combate; sem alocação.
  - `GameEvents` (global, tipado) — run, UI, áudio, achievements, analytics.
- Eventos são `struct` passados por `in`/`ref`; listeners com prioridade inteira; dispatch síncrono.
- Eventos de combate são **coletados no frame** e consumidos por UI/Audio em `LateUpdate` via ring buffer (UI/Audio nunca alteram combate).
- Nada de `UnityEvent`/`SendMessage` em hot path. `UnityEvent` liberado em UI e cenas do Sanctuary.

### 3.8 Status, Resonance e Set Powers

#### 3.8.1 StatusSystem
- Armazenamento central (SoA, `NativeArray`) de status ativos por entidade; o job de lote roda com **granularidade de 0,25 s** apenas como agendamento. O tick lógico de DoT é **0,5 s** (06 §3.2); outros status declaram tick múltiplo de 0,25 s (00 §5 R19).
- Políticas de stacking: `Refresh`, `StackDuration`, `StackIntensity(max)`, `Independent(max)`, `Strongest`. Exceções de teto declaradas por fonte (ex.: Overload Circuit +2 stacks — 07/08).
- Buildup: barra por (alvo, status primário) preenchida por Status Potency fixa por golpe (07), escalada pelo atributo Status Potency (01) e reduzida pela resistência a status do alvo (Elite ×1,5, Guardian ×2,0, Chefe ×2,5).
- **`CrowdControlTracker`** — regra única de 06 §6 (00 §5 R09):
  - Grupos de DR: **Hard** (Stun, Freeze), **Root**, **Silence**. Knockback sem DR (limitado pela massa); Staggered fora do grupo Hard.
  - Inimigos: janela de 8 s desde o 1º CC do grupo → 100% / 50% / 25% da duração, depois imune até o fim da janela. Não existem imunidades próprias de status.
  - Jogador: Hard CC máx. 1,5 s por aplicação; Freeze máx. 1,0 s; após qualquer Hard CC, 2 s de imunidade a Hard CC; Root máx. 2 s.
  - Chefes e Guardians: CC convertido em Stagger (06), exceto as conversões nomeadas de 07 — **Frostbound** (Freeze) e **Distortion** (Pull, +10% Vulnerability) — implementadas como `BossConversion` no `StatusEffectDefinition` (00 §5 R10).
  - Exceção declarada: Ultimates podem aplicar Freeze até 3 s em não-chefes (Absolute Zero — 07 / 00 §5 R49).
- **StaggerSystem:** barra de Stagger por inimigo (não escala por nível — 06); Break de chefe aplica **Broken**: 4 s, +30% Vulnerability (entra no teto de 60%), limiar +50% por Break, máx. 3× (00 §5 R11).

#### 3.8.2 ResonanceSystem
- Escuta `OnHitDealt` e `OnStatusApplied`; avalia `ResonanceRule` indexadas por `(statusPresente, afinidadeDoHit)` → lookup O(1). A afinidade do hit vem de `TagMask ∪ ResonanceAffinityMask` (conversões < 50% contam para Resonance — 03).
- Resultado é um `EffectDefinition[]` executado com `Generation + 1` e flag `IsResonance` (More orçado e declarado em 07 §12 — contrato §4).
- Limites: ICD por alvo por regra, uma Resonance por evento por alvo (tabela de prioridade de 07), 8 procs/s por jogador, máximo de zonas/clones simultâneos (07).

#### 3.8.3 SetTracker (GDD §14.5)
```
EquipmentChanged / SetFocusChanged
 → contar peças por SetDefinition, por **definição de item distinta** (duas cópias da mesma peça contam 1 — 04)
 → Keystone of the Sundering equipado: +1 peça para cada set com ≥ 2 peças equipadas (04)
 → candidatos: para cada set, poderes elegíveis (3 peças → poder 1; 6 peças → poderes 1 e 2)
 → **no máximo 2 Set Powers ativos** no total; o bônus de 6 peças conta como o segundo
   (6+3 e 3+3+3 não somam poder extra — 04)
 → desempate: mais peças → Set Focus escolhido pelo jogador → primeiro set a fechar 3 peças (timestamp no loadout)
 → diff com estado anterior → Grant/Revoke PowerSource
 → StatBlock.Add/RemoveSource + recompilar SkillSpecs afetados (canal "Set")
 → GameEvents.SetBonusChanged → UI
```
- Sets são exclusivos do herói dono (04); peças de set de outro herói são hero-bound e não equipáveis.
- Set Powers que são transformações (ex.: Ashen Ascension) são `StateDefinition` geridas pelo `HeroStateMachine` e expõem mods condicionais `while state active`.
- Aumentos de recurso concedidos por sets (+1 Arcane Charge, +1 Void Construct, +1 Echo Clone — 04) passam pela validação de teto de 00 §5 R01 (máx. 8).

### 3.9 Combate e hit detection

- **Camadas físicas:** `Hurtbox_Player`, `Hurtbox_Enemy`, `Hitbox_Player`, `Hitbox_Enemy`, `Environment`, `Projectile`, `Interactable`. Matriz de colisão mínima.
- **Melee:** `AttackChain` define por golpe `{startFrame, endFrame, shape (Box/Capsule/Arc), offset}`. Durante a janela, `Physics.OverlapCapsuleNonAlloc` por FixedUpdate + **sweep** entre poses para golpes rápidos; `HitRegistry` por swing impede hit duplo.
- **Projéteis:** `ProjectileManager` com até 2.000 projéteis ativos; movimento e colisão via `SpherecastCommand` em job; visual em pool (VFX Graph ou mesh instanciado).
- **Áreas:** `OverlapSphereNonAlloc`/`OverlapBox` no frame de impacto; sem falloff; zonas persistentes (ex.: Burning Arc) tickam no StatusSystem; zonas sobrepostas da mesma habilidade e fonte não acumulam no mesmo alvo (06).
- **Mira isométrica:** raycast do cursor contra plano do chão (altura do personagem); movimento WASD + mira no cursor (06); gamepad usa stick direito + aim assist por cone (ref. 06 para parâmetros).
- **Input (06 / 00 §5 R05):** LMB básico, RMB forte, Space esquiva, Q/E/R/Shift habilidades 1–4, F Ultimate, 1 Aether Flask; Action Map `Gameplay` gerado a partir desse mapeamento; gamepad conforme 06.
- **Esquiva (06 / 00 §5 R06):** universal para todos os heróis — 2 cargas, 4,5 m fixos, i-frames nos frames 2–13, recarga 1,5 s; Perfect Dodge emite `OnPerfectDodge`.
- **HitFlags:** `Unavoidable` (ignora Dodge Chance e Block; i-frames evitam), `Unblockable`, `Annihilation` (só sair da área evita), `Execute`, `Knockdown`, `IsEcho`, `IsResonance`, `IsTriggered` (00 §5 R02, R13).
- **Execute (06 / 00 §5 R08):** `ExecuteResolver` aplica limiar máx. 20% em inimigos comuns, metade (máx. 10%) em Elites; Guardians e chefes são imunes e recebem, uma única vez por alvo, **+25% Vulnerability abaixo de 20% de vida** (não acumula entre fontes; entra no teto de 60%).
- **Hit stop, shake, knockback, flash:** `FeedbackSystem` dirigido por `HitFeedbackProfile` (SO) por arma/skill — valores em 06-combate.
- **Simulação:** combate em `FixedUpdate` a **60 Hz** (frame data de 06 em frames a 60 fps, sem conversão); animação e input em `Update`; buffer de input de 0,15 s / 9 frames (ref. 06). Golpes rápidos (Dual Daggers etc.) continuam com sweep entre poses.

### 3.10 IA de inimigos

- **Inimigos comuns:** HFSM (`Idle → Engage → [Approach | Reposition | Attack | Recover | Flee] → Dead`) com **seleção de ataque por Utility** (scores: distância, ângulo, cooldown, aliados próximos, vida). Templates de HFSM por arquétipo (Melee, Ranged, Caster, Tank, Summoner, Assassin, Support — ref. 09).
- **Token de ataque:** `AttackTokenDirector` limita quantos inimigos atacam o jogador ao mesmo tempo, lendo a tabela de 09 §3.2 via `DifficultyDefinition` (00 §5 R17): Normal 3 melee / 2 ranged → Eclipse 5 / 4, Ascension +1 melee em A40 e +1 ranged em A70; intervalo mínimo entre ataques do mesmo inimigo por dificuldade. Elites e Guardians **não consomem token** (respeitam o intervalo mínimo nos ataques fortes); Assassinos usam token exclusivo de emboscada (1 em Normal/Veteran, 2 em Nightmare+). Inimigo sem token orbita a 4–7 m.
- **Telegraphs:** `AttackDefinition` declara o tipo de telegraph da linguagem de 06 (vermelho evitável, roxo controle, branco-dourado Unblockable, ciano janela interrompível; laranja reservado a Ember) e o wind-up, validado contra o piso de 0,45 s e a redução máx. de −15% por dificuldade (00 §5 R14).
- **LOD de IA:** decisões a **10 Hz** para inimigos próximos/na tela e **4 Hz** distantes/fora da tela (09 cita este valor); movimento em job (steering + avoidance radial), NavMesh só para path global.
- **Elites:** `EliteAffixDefinition` adiciona componentes (auras, triggers, ataques extras) ao template.
- **Chefes:** HFSM de fases + `PatternSequencer` (sequências de ataques autorais, telegraphs, janelas de punição), transições por limiar de vida e por tempo; sem randomização total — padrões ponderados por fase.

### 3.11 Runs e salas

- **Mapa (09 é dono — 00 §5 R33):** DAG em camadas gerado por seed (`Map` stream): **13 linhas + chefe** (14 salas jogadas), Estratos I (linhas 1–5), II (6–9), III (10–13); Guardian fixo na linha 9; pelo menos um nó Rest garantido na linha 5; Antecâmara (Rest) na linha 13; 2–4 caminhos por linha; distribuição dos 9 tipos de sala conforme 09. O `Run Map Previewer` valida essas restrições por seed.
- **Pós-chefe (10):** Delve Deeper / Overrift — 5 salas extras opcionais; só o loot dessas salas fica em risco.
- **Sala:** prefab Addressable com `RoomAnchor` (spawns, portas, NavMesh baked, câmera bounds, iluminação). Carregamento assíncrono da próxima sala durante a recompensa da sala atual; a anterior é descarregada.
- **Encounter Director:** gasta orçamento de Pontos de Ameaça (PA) por ondas a partir de `EncounterDefinition` + dificuldade; respeita o teto de unidades e PA vivos de 09; mantém pressão alvo (ref. 09).
- **Rest Room / Antecâmara (10 — 00 §5 R34):** `ServiceDefinition`s de cura 40% grátis, recarga do Aether Flask, Rift Anchor (extração com tudo, sem recompensa de chefe), Anchor (proteger 1 item) e Temper.
- **RunManager:** máquina de estados `Hub → RunStart → Room(n) → Reward → MapChoice → … → Boss → [Overrift] → RunEnd → Hub`, com saídas `Extract` (Rift Anchor) e `Death` (regra de 10); único dono do `RunState`. Moeda da run: **Rift Shards** (00 §5 R36).
- **Sanctuary:** cena própria persistente; NPCs são `ServiceDefinition` (Blacksmith, Enchanter, Merchant, Vault…) operando sobre `ProfileState`.

### 3.12 Loot

- `ItemGenerator` (Core, puro): `(LootContext{source, region, difficulty, iLvl, hero, pityState(hero), attunement, quantityBonus}, Rng Loot stream) → ItemInstance`.
- Passos (05): escolher raridade (pisos por fonte, pity) → escolher base/nomeado (smart loot por herói; peso de região-lar e Attunement ×3; peças de set e Uniques respeitam `minDropILvl` e não caem em Normal) → derivar tier de iLvl (T1–T5 de 01) → rolar afixos do pool do slot (02) sem repetir grupo, com Faixa Nomeada nos fixos e escolha do Afixo Aberto → rolar Roll01 com piso da dificuldade / Primal → rolar modificadores (Epic: 75% do herói / 25% `GEN-xx`, peso ×3 para habilidades equipadas — 05/03) → gravar `DropSeed` e `Source`.
- Bônus de quantidade de loot são aditivos, com teto ×2,0 no total (00 §5 R45).
- Equipamentos são **coletados automaticamente ao limpar a sala** e vão para o Satchel (30 espaços); `Pickup Radius` vale para moedas e orbes (00 §5 R30). `ItemDropView` (beam por raridade) apenas anuncia o drop.
- Determinismo: mesma seed + mesmo contexto = mesmo item (base para testes e desafios com seed).

### 3.13 Save System

| Arquivo | Conteúdo | Frequência |
|---|---|---|
| `profile.sav` | Conta (Account Rank), desbloqueios de heróis/regiões/dificuldades, moedas permanentes (Gold, Aether Dust, Sundered Cores, Pillar Fragments, Eclipse Marks, Memory Fragments, Heart of [Chefe]), progresso de dificuldade/Ascension, Codex do Archivist, níveis de NPC, oferta semanal do Merchant, contadores de pity de Unique de chefe, **resumo da última run morta** (para o evento Echo of a Fallen Eclipsed — 09/10) | Fim de run, serviços no hub |
| `heroes.sav` | Por herói: nível/XP, Aether Lattice (pontos, Augments, Keystones), loadouts, Set Focus, **estado de pity por herói** (Legendary, set, peças faltantes — 05) | Fim de run, alterações no hub |
| `vault.sav` | `ItemInstance[]` (com contadores de Enchant/Refine/Imbue, flags Primal/Anchored, `DropSeed`, `Source`), **Set Locker** (72 vagas — 10) | Alterações no Vault/inventário |
| `run.sav` | Suspend-save da run atual (`RunState`: seed, mapa, Blessings/Curses, Rift Shards, Satchel, itens com Anchor, cargas do Flask) | Entrada de sala; apagado ao carregar |
| `settings.json` | Vídeo, áudio, controles, acessibilidade (inclui opacidade de VFX do jogador) | Ao alterar |

- **Formato:** JSON (Newtonsoft) → GZip → header `{magic, schemaVersion, checksum CRC32}`.
- **Escrita atômica:** escreve `.tmp` → flush → renomeia; mantém `.bak` das 3 últimas versões válidas.
- **Versionamento:** `schemaVersion` inteiro; `IMigration { int From; JObject Apply(JObject) }` encadeadas no load.
- **IDs:** somente `StableId` e `Guid`; `IdRemapTable` resolve IDs renomeados/removidos (item removido → convertido em Aether Dust por raridade, tabela de 10-progressao — 00 §5 R46).
- **Steam Cloud:** pasta inteira sincronizada; conflito resolvido por `lastWriteUtc` + contador monotônico.

### 3.14 Performance

**Specs mínimas (1080p, preset Low, 60 fps):** Intel i5-8400 / Ryzen 5 2600, 8 GB RAM, GTX 1060 6 GB / RX 580 8 GB, SSD, Windows 10 64-bit, DX11/DX12.
**Recomendadas (1440p, High, 60+ fps):** i5-12400 / Ryzen 5 5600, 16 GB, RTX 3060 / RX 6600 XT.

| Budget (frame 16,6 ms no mínimo) | Valor |
|---|---|
| Inimigos ativos simultâneos | 120 normal, **200 pico** (alvo técnico de soak test; as composições de design de 09 — teto de unidades vivas por sala — ficam bem abaixo disso) |
| Projéteis ativos | 2.000 (sistema), 600 típicos |
| Gameplay + IA (CPU main) | ≤ 5 ms |
| Física/queries | ≤ 2 ms |
| Render CPU | ≤ 4 ms |
| GPU (Low) | ≤ 14 ms |
| Partículas vivas | 40k GPU (VFX Graph) / 3k CPU |
| Draw calls | ≤ 1.500 (SRP Batcher + GPU instancing) |
| GC alloc em combate | **0 B/frame** (alvo), alerta acima de 1 KB/frame |
| Memória | ≤ 4 GB RAM, ≤ 3 GB VRAM no Low |

- **Pooling** obrigatório: inimigos, projéteis, VFX, números de dano, drops, áudio one-shot (FMOD instances).
- Este orçamento de performance é o **alvo técnico único** do projeto; 06-combate não mantém orçamento próprio (00 §5 R47).
- **Legibilidade x VFX (GDD §12, valores de 06 §8):** telegraphs e projéteis inimigos renderizam numa camada **acima** de todo VFX do jogador; VFX do jogador com **opacidade padrão de 80%**, configurável pelo slider de acessibilidade de 06; vermelho saturado no chão é reservado a telegraphs inimigos. No máximo 4 ícones de status sobre um inimigo (07).
- Animação: GPU skinning; inimigos distantes com animação em taxa reduzida (`Animator.cullingMode` + LOD de update).

### 3.15 Ferramentas de editor

1. **Pipeline de planilhas:** designers editam Google Sheets → export CSV em `DataSource/` → `DataImporter` cria/atualiza SOs por `StableId` (nunca recria, preserva referências). Tabelas: Stats, Affixes, Items, Modifiers, Sets, StatusEffects, ResonanceRules, Enemies, LootTables, Localization.
2. **Data Validator** (rodado no import, no build e no CI): IDs únicos e no formato; raridade × contagem de afixos e estrutura de item de 00 §5 R25; set com exatamente 6 peças em slots distintos fora de Main/Off Hand; afixos dentro do pool do slot (02), salvo 1 `*` em peça de set; Faixa Nomeada dentro de 70–115% do máximo do tier; modificador aponta skill/tag existente e respeita grupos exclusivos; faixas min ≤ max; tags no vocabulário do contrato §3.4 e HitFlags fora dele; nomes no registro de nomes reservados (00 §5 R48); More declarado e dentro dos tetos de 00 §5 R03 (Augment = 0); `EchoRecursion1x` só nas fontes autorizadas; teto de recursos ≤ 8 e só via exceções de 08; telegraph com wind-up ≥ 0,45 s; strings localizadas presentes; ICD em todo trigger.
3. **Skill Graph Inspector:** visualiza `SkillSpec` compilado com modificadores aplicados (antes/depois).
4. **Build Sandbox (Training Grounds de debug):** janela para equipar qualquer item/set/modificador e spawnar inimigos; também exposta no jogo como Training Grounds (GDD §3.3).
5. **Loot Simulator:** simula N drops por contexto e exibe distribuição de raridade/afixos.
6. **Run Map Previewer:** gera mapas por seed e mostra estatísticas de tipos de sala.
7. **Console de debug in-game** (`Debug` action map): give item, set level, kill all, toggle god mode, seed da run.

### 3.16 Testes e simulação de balanceamento

- **EditMode (Core):** fórmulas de stats/dano, compilação de modificadores (snapshot tests do `SkillSpec`), Set tiers, Resonance, ItemGenerator determinístico, migrações de save.
- **PlayMode:** hit detection de cada `AttackChain`, fluxo de run curta, carregamento de salas, save/load.
- **Simulador headless de DPS/TTK:** roda o `DamageResolver` real com loadouts definidos em JSON contra alvos padrão; gera relatório CSV comparando **Rota A × B × C** (GDD §6.6) para validar a regra de ±10% do contrato §4. Executado no CI a cada mudança em `DataSource/`.
- **Soak test:** run automatizada com bot simples em sala de 200 inimigos, mede frame time e GC.
- **Teste de recursão (07):** 40 inimigos com todos os ecos/Resonances ativos → no máximo 400 eventos de dano por segundo e nenhum evento com Generation > 2.
- **Teste de Resonance (07):** build com até 2 itens dedicados deve disparar a Resonance alvo pelo menos a cada 10 s.
- **CI:** build Windows noturno + testes + validador + simulador; falha de validação bloqueia merge.

### 3.17 Telemetria (interna/playtest)

Eventos anonimizados opt-in: morte (sala, causa, build hash), tempo por sala, escolhas de caminho, itens equipados ao fim de run, uso de habilidades. Exportados para análise de balanceamento. Nada pessoal.

### 3.18 Ordem recomendada de implementação

| Fase | Entregas | Critério de saída |
|---|---|---|
| **F0 Fundação** | Projeto, assemblies, CI, Definition/Registry, Rng, Tags, Event Bus, Input, câmera | Build roda no CI; validador ativo |
| **F1 Combate vertical** | Kael com 1 arma, básico/pesado/4 habilidades + Ultimate, esquiva, hit detection a 60 Hz, feedback, 3 inimigos, StatBlock, DamageResolver, CrowdControlTracker | Combate "sente bem" em sala de teste (critérios de 06) |
| **F2 Habilidades modulares** | Delivery/Effects/Triggers, compilação de modificadores (6 fases), Augments, StatusSystem, 3 status, Generation | Modificadores de Cleaving Strike funcionando sem código por modificador; teste de recursão passando |
| **F3 Itemização** | ItemDefinition/Instance, afixos, raridades, ItemGenerator, inventário, comparação, SetTracker | Rotas A/B/C equipáveis; simulador gera relatório |
| **F4 Run loop** | Mapa de nós, salas, Encounter Director, recompensas, chefe 1, save/suspend | Run completa da Ashen Wastes |
| **F5 Meta** | Sanctuary, serviços, Vault, progressão de herói, dificuldades | Loop hub ↔ run completo |
| **F6 Resonance + heróis** | ResonanceSystem, Lyra, Nyx, Orun (incluindo Clone e Constructs) | 4 heróis × 3 builds jogáveis |
| **F7 Conteúdo** | Regiões 2–5, chefes, 12 sets, catálogo completo | Metas do GDD §16 |
| **F8 Endgame + polimento** | Ascension, desafios, performance, acessibilidade, localização, Steam | Specs mínimas a 60 fps; zero bloqueadores |

Riscos técnicos a atacar cedo: **Echo Clone** (replicação de ações — F2), **Void Constructs** como recurso + invocações (F2/F6), **hordas de 200 inimigos** (soak test já na F1).

---

## 4. Dependências e pontos de integração

| Doc | Fornece para a arquitetura | Consome da arquitetura |
|---|---|---|
| 00-convencoes | Contrato, resoluções §5 (R02, R04, R07, R09, R17, R19, R21, R33, R47 etc.) | Validador aplica as regras do contrato |
| 01-atributos | Fórmulas de mitigação, caps/DR, tradução de Resource, efeito dos primários, tabelas por tier T1–T5, multiplicadores de slot, Faixa Nomeada, atributo Status Potency | `StatDefinition`, buckets Flat/Increased/More, `StatMod` com filtro por tags, Roll01 por tier |
| 02-equipamentos | Itens nomeados, bases, pool por slot, Weapon Power por iLvl, Uniques com `fonteUnique` | `ItemDefinition` (`ItemKind`), `AffixPool`, rolagem normalizada |
| 03-modificadores | Catálogo, stacking, exclusividade, 6 fases, Signature, hero-bound | `ModifierOp` por fase 1–6, `ExclusivityGroup`, canais, `ResonanceAffinityMask` |
| 04-conjuntos | Set Powers, máx. 2 poderes ativos, Set Focus, Keystone of the Sundering | `SetDefinition`, `PowerSource`, canal "Set", `StateDefinition`, `EchoRecursion1x` |
| 05-loot | Taxas, pity por herói, smart loot, região-lar, Attunement ×3, Primal, teto de quantidade | `ItemGenerator`, `LootContext`, `LootTableDefinition`, `DropSeed`/`Source` |
| 06-combate | Pipeline §3.2, estados genéricos, DR de CC, input, esquiva, frame data a 60 fps, telegraphs, hooks, DamageInstance, opacidade de VFX | `AttackChain`, `HitFeedbackProfile`, `FixedUpdate` 60 Hz, `CrowdControlTracker`, `StaggerSystem`, HitFlags |
| 07-resonance | Status, Buildup/Status Potency, regras, Generation, tetos, conversões de chefe | `StatusEffectDefinition`, `ResonanceRule`, tick lógico 0,5 s (DoT) sobre lote de 0,25 s, ICD |
| 08-herois | Kits, passivas, recursos (Void Mass), Aether Lattice/Augments, movesets, exceções de teto | `HeroDefinition`, `ResourceDefinition`, `AugmentDefinition`, `Delivery` especiais (Clone, Construct) |
| 09-inimigos | Arquétipos, elites, chefes, mapa de 13 linhas, 9 tipos de sala, PA, tokens por dificuldade, Blessings | HFSM+Utility (10/4 Hz), `AttackTokenDirector`, `PatternSequencer`, `RoomDefinition`, `BlessingDefinition`, alvo técnico de 200 inimigos |
| 10-progressao | Dificuldades, Monster Level, moedas, regra de morte, Rest Room, Satchel, serviços do Sanctuary, compensação de item removido | `ProfileState`, saves, suspend-save, `ServiceDefinition` |

---

## 5. Conflitos com as convenções

Nenhum conflito em aberto. Alinhado à rodada de integração (00 §5):
- **R02:** `IsEcho`, `IsResonance`, `IsTriggered`, `Unavoidable`, `Unblockable`, `Annihilation`, `Execute`, `Knockdown` são **HitFlags** de `DamageInstance`, não tags — vocabulário §3.4 inalterado.
- **R04:** simulação passou de 50 Hz para 60 Hz.
- **R07:** `ProcDepth`/`AllowEchoOfEcho` substituídos pelo modelo Generation de 07 + `EchoRecursion1x`; corrigido o off-by-one (triggers não avaliam em Generation ≥ 2).
- **R47:** fases de modificador passaram de 5 para as 6 de 03; hooks unificados com 06; bucket `Override` removido (sem consumidor); bucket `Flat` mantido (necessário para vida, Armor e dano plano adicionado — já previsto em 06 §3.2 etapa 1).
- **R17 / R33 / R34:** tokens por dificuldade (09), mapa de 13 linhas + chefe (09) e serviços de Rest (10) incorporados.

---

## 6. Questões abertas para playtest

1. Limites do `AttackTokenDirector` por dificuldade (tabela de 09) — validar sensação de pressão em Nightmare/Eclipse com 60 Hz e LOD de IA 10/4 Hz.
2. Opacidade padrão de 80% dos VFX do jogador (06) com telegraphs por cima — validar legibilidade versus satisfação visual em hordas com Resonance ativa.
3. Alvo técnico de 200 inimigos no hardware mínimo — confirmar no soak test da F1.
4. Custo do `CrowdControlTracker` e do lote de status de 0,25 s com 120 inimigos e 8 procs de Resonance/s — medir no soak test da F6.

# Esquema dos CSVs de itemização e loot (M12)

Contrato entre os docs (01, 02, 05, 11), os CSVs em `DataSource/` e as classes de dados do Core (`Eclipse.Core.Items`, `Eclipse.Core.Loot`). Cada CSV tem um `TableImporter` e uma `Definition`; o Data layer monta `IItemCatalog`/`ILootCatalog` a partir delas. **Nenhum número de balanceamento vai para código**: todas as constantes dos catálogos saem de `loot_global.csv`, `rarities.csv` e `named_bands.csv`.

Convenções gerais em `DataSource/README.md` e na seção "Convenções" no fim deste arquivo.

## Colunas comuns

| Coluna | Tipo | Obrig. | Regra |
|---|---|---|---|
| `id` | StableId | sim | 1ª coluna. Minúsculo, `_` permitido, 2 a 4 segmentos separados por `.` (regex de `StableId`). |
| `name_pt` | texto | sim | Exigida por `LocalizationKeyRule` em toda tabela que vira asset. |
| `name_en` | texto | sim | Idem. |
| `desc_pt`, `desc_en` | texto | não | Opcionais. |
| `source_doc` | texto | sim | Começa pelo número do doc dono (`01 §3.9.2`); `ReservedNameRule` depende disso. |

As tabelas abaixo listam só as colunas específicas; as comuns valem para todas. Exemplos omitem `desc_*`.

## Visão geral

| # | CSV | Linhas | Classe Core |
|---|---|---|---|
| 1 | `affixes.csv` | ~56 | `AffixData` |
| 2 | `slot_multipliers.csv` | ~70 | `SlotMultiplierData` |
| 3 | `affix_pools.csv` | ~56 | `AffixPoolEntry` |
| 4 | `infusions.csv` | 12 | metadados de `AffixData` (Infusion) |
| 5 | `rarities.csv` | 4 | `RarityData` |
| 6 | `named_bands.csv` | 4 | `NamedBandRange` |
| 7 | `item_bases.csv` | ~34 | `ItemBaseData` |
| 8 | `items_named.csv` | 35 | `NamedItemData` (+ `NamedAffixSpec`) |
| 9 | `loot_sources.csv` | 10 | `LootSourceData` |
| 10 | `loot_chances.csv` | 10 | `LootChanceData` |
| 11 | `ilvl_ranges.csv` | ~17 | `IlvlRangeData` |
| 12 | `loot_difficulty.csv` | 5 | `DifficultyLootData` |
| 13 | `pity.csv` | 5 | `PityConfigData` |
| 14 | `home_regions.csv` | 12 + 5 | `HomeRegionData`, `RegionAffinityData` |
| 15 | `loot_global.csv` | ~30 | constantes de `IItemCatalog`/`ILootCatalog`, `SlotWeightData`, `SmartLootWeights` |

Total: 15 arquivos para 16 tabelas lógicas: `home_regions.csv` carrega duas (Set e Region, via `row_kind`). Ordem de importação sugerida (`DependsOn`): `affixes` -> `infusions`, `slot_multipliers`, `affix_pools`, `rarities`, `named_bands` -> `item_bases` -> `items_named`; `loot_*`, `ilvl_ranges`, `pity`, `home_regions` independentes.

---

## 1. `affixes.csv`

- **Propósito:** catálogo de afixos com as faixas T1 a T5 (inclui as 12 Infusions, `category=Infusion`).
- **Chave:** `id` (ex.: `affix.core.ember_damage`).
- **Origem:** 01 §3.9.2 (faixas), 02 §3.9.1 (abreviações), 05 §6.2 (grupos).
- **Destino:** `AffixData`. **Linhas:** ~56 (3 primários, 3 flats, 18 ofensivos, 10 defensivos, 10 utilitários, 12 Infusions).

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `abbrev` | texto | sim | `EMB`, `INF-EMB-ADD` |
| `stat` | enum `StatId` (nome) | não (vazio nas Infusions) | `EmberDamage` |
| `value_kind` | `Flat`\|`Percent` | sim | `Percent` |
| `category` | `Primary`\|`Flat`\|`Offensive`\|`Defensive`\|`Utility`\|`Infusion` | sim | `Offensive` |
| `group` | `None`\|`Primary`\|`Affinity`\|`Comfort`\|`SingleResistance` | sim | `Affinity` |
| `affinity` | TagMask (`\|`) | não | `Ember` |
| `relevance_tags` | TagMask (`\|`) | não | `Summon` |
| `t1_min`,`t1_max` ... `t5_min`,`t5_max` | número | sim (10 colunas) | `6`, `10` |

Em `Percent` o valor é número percentual (12.5 = 12,5%) e o importador divide por 100.

Exemplo:
```
id,name_pt,name_en,abbrev,stat,value_kind,category,group,affinity,relevance_tags,t1_min,t1_max,t2_min,t2_max,t3_min,t3_max,t4_min,t4_max,t5_min,t5_max,source_doc
affix.core.ember_damage,Dano de Ember,Ember Damage,EMB,EmberDamage,Percent,Offensive,Affinity,Ember,,<min>,<max>,...,01 §3.9.2
```
(`<min>/<max>` vêm de 01 §3.9.2; não repetidos aqui.)

## 2. `slot_multipliers.csv`

- **Propósito:** multiplicadores de slot (MH x1,5 em dano%, Amulet x1,5, Relic x1,25 etc.).
- **Chave:** `id` (ex.: `slotmult.main_hand.damage`).
- **Origem:** 01 §3.9.3 (e 01 l.346 para Infusions). **Destino:** `SlotMultiplierData`. **Linhas:** ~70 (uma por slot x grupo de afixos).

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `slot` | enum `EquipSlot` | sim | `MainHand` |
| `affix_ids` | lista `\|` de ids de `affixes` | sim | `affix.core.physical_damage\|affix.core.ember_damage` |
| `base_id` | id de `item_bases` | não (vazio = qualquer base) | `base.kael.warbrand` |
| `multiplier` | número | sim | `1.5` |
| `applies_to_implicit` | bool | sim | `false` |

Exemplo: `slotmult.main_hand.damage,Dano em Main Hand,Main Hand damage,MainHand,affix.core.physical_damage|affix.core.ember_damage,,1.5,false,01 §3.9.3`

## 3. `affix_pools.csv`

- **Propósito:** pool de afixos por slot (formato largo; 02 §3.9.3). Peso 0 = fora do pool.
- **Chave:** `id` = `pool.core.<afixo>`. **Origem:** 02 §3.9.3, pesos 01 §3.9.4. **Destino:** `AffixPoolEntry`. **Linhas:** ~56 (1 por afixo).

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `affix_id` | id de `affixes` | sim | `affix.core.ember_damage` |
| `w_main_hand`,`w_off_hand`,`w_head`,`w_chest`,`w_hands`,`w_legs`,`w_feet`,`w_ring`,`w_amulet`,`w_relic` | número >= 0 | sim (10 colunas, ordem de `EquipSlot`) | `20`, `10`, `0` |
| `restriction` | texto livre | não | `weapon_tag:Attack` |

Valores de `restriction`: `weapon_tag:Attack`, `weapon_tag:Spell`, `base:warbrand`, `hero:orun_except_ring_relic`, `relic_family_affinity`. Vazio = sem restrição. Limites por item (1 primário, 2 de afinidade incluindo Infusions, MS só em Feet) vão em `loot_global`.

Exemplo: `pool.core.ember_damage,Pool de Ember Damage,Ember Damage pool,affix.core.ember_damage,20,10,0,0,0,0,0,10,10,20,,02 §3.9.3`

## 4. `infusions.csv`

- **Propósito:** metadados das 12 Infusions (6 afinidades x Added/Strikes). As faixas ficam em `affixes.csv`.
- **Chave:** `id` (ex.: `infusion.ember.added`). **Origem:** 02 §3.9.4. **Destino:** metadados lidos junto de `AffixData` (Infusion). **Linhas:** 12.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `affix_id` | id de `affixes` (category=Infusion) | sim | `affix.core.infusion_ember_added` |
| `affinity` | TagMask | sim | `Ember` |
| `form` | `Added`\|`Strikes` | sim | `Added` |
| `hit_filter` | `Basic`\|`Skill`\|vazio | não | `Basic` |
| `counts_as_affinity` | bool | sim | `true` |
| `pool_rule` | texto | sim | `relic_family_affinity` (Relic) \| `any` (Amulet) |

Exemplo: `infusion.ember.added,Infusão de Ember (Added),Ember Infusion (Added),affix.core.infusion_ember_added,Ember,Added,Basic,true,any,02 §3.9.4`

## 5. `rarities.csv`

- **Propósito:** estrutura por raridade. **Chave:** `id` (`rarity.core.epic`). **Origem:** 02 §3.1, §3.2, §3.3 (R25). **Destino:** `RarityData`. **Linhas:** 4.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `rarity` | enum `Rarity` | sim | `Epic` |
| `random_affix_count` | int | sim | `3` |
| `modifier_count` | int | sim | `1` |
| `min_drop_ilvl` | int | sim | `1` (Legendary 10) |

Exemplo: `rarity.core.epic,Epic,Epic,Epic,3,1,1,02 §3.2`

## 6. `named_bands.csv`

- **Propósito:** faixas Nomeadas como fração do máximo do tier. **Chave:** `id` (`band.core.high`). **Origem:** 02 §3.9.1, R24. **Destino:** `NamedBandRange`. **Linhas:** 4.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `band` | enum `NamedBand` (`Envelope`\|`Default`\|`High`\|`Low`) | sim | `High` |
| `lo_pct` | número percentual | sim | `95` |
| `hi_pct` | número percentual | sim | `115` |

Valores: Envelope 70-115, Default 80-100, High 95-115, Low 70-80. `None` não tem linha.
Exemplo: `band.core.high,Faixa alta,High band,High,95,115,02 §3.9.1`

## 7. `item_bases.csv`

- **Propósito:** bases de arma, armadura, joia e relíquia. **Chave:** `id` (`base.kael.greatsword`, `base.armor.cuirass`, `base.relic.cinder`).
- **Origem:** 02 §3.4 a §3.8, §3.10. **Destino:** `ItemBaseData`. **Linhas:** ~34 (5 armas Kael + Warbrand + 15 armaduras + 3 Rings + 3 Amulets + 7 Relics).

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `slot` | enum `EquipSlot` | sim | `MainHand` |
| `category` | texto | sim | `Greatsword` |
| `family` | enum `ArmorFamily` | sim | `Plate` / `None` |
| `hero_id` | texto (id de herói, sem Definition ainda) | não | `kael` |
| `tags` | TagMask | não | `Attack\|Melee` |
| `implicit_affix_id` | id de `affixes` | não | `affix.core.area_damage` |
| `implicit_factor` | número | sim | `0.5` |
| `armor_factor` | número | sim (0 se não armadura) | `1.5` |
| `droppable` | bool | sim | `true` (Keystone: `false`) |
| `dps_factor`, `implicit_affinity`, `secondary_implicit_stat`, `secondary_implicit_at_100`, `implicit_effect_id` | variados | não | `1.05`, `Ember`, `DodgeChance`, `2`, ... |

Colunas extras alimentam o gerador via Data layer (ainda sem campo em `ItemBaseData`; ver Lacunas).
Exemplo: `base.armor.cuirass,Peitoral,Cuirass,Chest,Cuirass,Plate,,,,affix.core.armor,0.5,1.5,true,02 §3.6`

## 8. `items_named.csv`

- **Propósito:** Legendaries nomeadas e Uniques. **Chave:** `id` (`item.kael.ashfang`, `item.universal.keystone_of_the_sundering`).
- **Origem:** 02 §3.11, §3.12.1, §3.12.5, §3.12.6; 11 §3.4.2. **Destino:** `NamedItemData`. **Linhas:** 35 (Kael 16 + Universais 19).
- **Sem `items_named_affixes.csv`:** afixos e modificadores ficam em colunas de lista.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `catalog_no` | texto | sim | `K1`, `U15` |
| `item_kind` | `NamedLegendary`\|`SetPiece`\|`Unique` | sim | `NamedLegendary` |
| `base_id` | id de `item_bases` | sim | `base.kael.greatsword` |
| `hero_id` | texto | não | `kael` |
| `effect_id` | texto | não | `effect.ashfang` |
| `fixed_affixes` | lista `\|` de `affix_id:Banda[*]` | sim (4 ou 5) | `affix.core.ember_damage:High\|affix.core.armor:Default` |
| `open_options` | lista `\|` de `affix_id:Banda[*]` (3) | sim p/ Legendary/Set; vazio p/ Unique | `affix.core.area_damage:Default\|...` |
| `modifier_ids` | lista `\|` de códigos de 03 (2) | sim | `KAE-CS-01\|GEN-27` |
| `affinity` | TagMask | não | `Ember` |
| `set_id` | texto | só SetPiece | |
| `unique_source_kind` | `None`\|`Boss`\|`Pinnacle`\|`Region`\|`Challenge` | só Unique | `Boss` |
| `unique_source_ref` | texto | só Unique (vazio permitido em `Challenge`) | `the_ashen_colossus` |
| `min_drop_ilvl` | int | sim | `10` (Unique 35) |
| `subtitle_pt/en`, `lore_pt/en`, `build`, `effect_more_declared` | texto/número | não | `25` (R03: K3 25, K12 40, K15 30, U15 20, U18 30) |

Sufixo `*` = fora do pool do slot. Banda = `Default`\|`High`\|`Low` (nome exato de `NamedBand`).
Exemplo: `item.kael.ashfang,Presa de Brasa,Ashfang,K1,NamedLegendary,base.kael.greatsword,kael,effect.ashfang,affix.core.ember_damage:High|...,...|...|...,KAE-CS-01|GEN-27,Ember,,,,,10,02 §3.12.1`

## 9. `loot_sources.csv`

- **Propósito:** as 10 fontes de loot. **Chave:** `id` (`lootsrc.core.trash`, `.elite`, `.guardian`, `.arena_reward`, `.treasure_delivered`, `.reliquary`, `.challenge`, `.event`, `.boss`, `.rift_cache`).
- **Origem:** 05 §3.1, §3.2, §8.1. **Destino:** `LootSourceData`. **Linhas:** 10.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `source` | enum `LootSourceKind` | sim | `Elite` |
| `drop_chance_pct` | percentual | sim | `100` (trash 3) |
| `guaranteed_items` | int | sim | `1` |
| `extra_item_chance_pct` | percentual | sim | `30` |
| `extra_items_if_all_challenges` | int | sim | `0` (Rift Cache 1) |
| `choose_of` | int | sim | `0` (Reliquary 3) |
| `guaranteed_epic_plus` | int | sim | `0` |
| `rarity_floor` | enum `Rarity` | sim | `Common` |
| `affected_by_quantity` | bool | sim | `true` |
| `ilvl_bonus` | int | sim | `1` |
| `ilvl_from_range_top` | bool | sim | `false` (Boss, Rift Cache `true`) |
| `consumes_hard_pity` | bool | sim | `true` (trash `false`) |

Exemplo: `lootsrc.core.elite,Elite,Elite,Elite,100,1,30,0,0,0,Common,true,1,false,true,05 §3.1`

## 10. `loot_chances.csv`

- **Propósito:** chances de L/E/R por fonte em 6 pontos de ancoragem (Normal, Veteran, Nightmare, Eclipse, A1, A100; Ascension interpola linear).
- **Chave:** `source` (`id` = `lootchance.core.<fonte>`). **Origem:** 05 §5.2 a §5.4. **Destino:** `LootChanceData`. **Linhas:** 10 (formato largo).

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `source` | enum `LootSourceKind` | sim | `Trash` |
| `leg_normal`,`leg_veteran`,`leg_nightmare`,`leg_eclipse`,`leg_a1`,`leg_a100` | percentual | sim | `0.1` |
| `epic_normal` ... `epic_a100` (6) | percentual; vazio = piso Epic | não | `2` |
| `rare_normal` ... `rare_a100` (6) | percentual; só Trash | não | `25` |

O importador converte vazio de Epic/Rare em valor negativo (convenção de `LootChanceData`).
Exemplo (colunas abreviadas): `lootchance.core.trash,...,Trash,0.1,...,2,...,25,...,05 §5.2`

## 11. `ilvl_ranges.csv`

- **Propósito:** faixas de iLvl. **Chave:** (`difficulty`, `region_id`), `id` = `ilvl.<dificuldade>.<regiao>`. **Origem:** 05 §4.1, §4.2, R38. **Destino:** `IlvlRangeData`. **Linhas:** ~17 (3 dificuldades x 5 regiões + Eclipse + Ascension).

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `difficulty` | enum `Difficulty` | sim | `Veteran` |
| `region_id` | texto (sem Definition ainda) | não (vazio = todas) | `frozen_cathedral` |
| `ilvl_min`, `ilvl_max` | int | sim | `30`, `45` |

Exemplo: `ilvl.veteran.frozen_cathedral,...,Veteran,frozen_cathedral,30,45,05 §4.1`

## 12. `loot_difficulty.csv` (nova)

- **Propósito:** tudo que 05 dá por dificuldade: quantidade, divisão do tipo de Legendary, piso de Roll01, iLvl de Ascension, Primal.
- **Chave:** `difficulty` (`id` = `lootdiff.core.<dificuldade>`). **Origem:** 05 §3.2, §4.1, §5.5, §10.1, §10.3. **Destino:** `DifficultyLootData`. **Linhas:** 5.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `difficulty` | enum `Difficulty` | sim | `Nightmare` |
| `quantity_bonus_pct` | percentual | sim | `20` |
| `quantity_bonus_per_ascension_pct` | percentual | sim (0 fora de Ascension) | `0.4` |
| `set_share_pct`, `standard_share_pct`, `unique_share_pct` | percentual (somam 100) | sim | `40`, `42`, `18` |
| `roll_floor_pct` | percentual | sim | `20` |
| `roll_floor_per_ascension_pct` | percentual | sim | `0.4` |
| `ascension_base_ilvl`, `ascension_max_ilvl`, `ascension_ilvl_cap_level` | int | sim (0 fora de Ascension) | `85`, `100`, `90` |
| `ascension_primal_min_level` | int | sim | `50` |
| `primal_base_chance_pct`, `primal_chance_per_level_pct` | percentual | sim | `2`, `0.16` |
| `primal_roll_min_pct` | percentual | sim | `85` |

Exemplo: `lootdiff.core.nightmare,Nightmare,Nightmare,Nightmare,20,0,40,42,18,20,0,0,0,0,0,0,0,0,05 §5.5`

## 13. `pity.csv`

- **Propósito:** pity por dificuldade. **Chave:** `difficulty` (`id` = `pity.core.<dificuldade>`). **Origem:** 05 §8.1 a §8.5. **Destino:** `PityConfigData`. **Linhas:** 5.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `difficulty` | enum `Difficulty` | sim | `Veteran` |
| `soft_start`, `hard` | int | sim | `50`, `100` |
| `soft_step_pct` | percentual | sim | `5` |
| `legendary_cap_mult` | número | sim | `2` |
| `set_pity_after` | int | sim (0 = sem) | `4` |
| `dup_unowned_weight` | int | sim | `3` |
| `boss_unique_chance_pct` | percentual (A1 em Ascension) | sim | `8` |
| `boss_unique_chance_a100_pct` | percentual | sim (= base fora de Ascension) | `25` |
| `boss_unique_guarantee_kills` | int | sim | `10` |
| `boss_echo_mult` | número | sim | `1.5` |
| `first_kill_set_chance_pct` | percentual | sim | `50` |

O importador deriva `BossUniqueChancePerAscension = (a100 - a1) / 99`.
Exemplo: `pity.core.veteran,Pity Veteran,Veteran pity,Veteran,50,100,5,2,4,3,0,0,10,1.5,50,05 §8.1`

## 14. `home_regions.csv`

- **Decisão:** 1 CSV, 1 `TableImporter`, coluna `row_kind` (`Set`\|`Region`); colunas de outro tipo ficam vazias.
- **Chave:** `id` (`homeregion.set.<set>`, `homeregion.region.<regiao>`). **Origem:** 05 §9.1, §9.2, §9.4, R32, R39. **Destino:** `HomeRegionData` (Set), `RegionAffinityData` (Region). **Linhas:** 12 Set + 5 Region.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `row_kind` | `Set`\|`Region` | sim | `Set` |
| `set_id` | texto | Set | `ember_vanguard` |
| `hero_id` | texto | Set | `kael` |
| `region_id` | texto | sim | `ashen_wastes` |
| `home_weight`, `home_boss_weight`, `attunement_mult` | número | Set | `4`, `6`, `3` |
| `dominant_affinities` | TagMask | Region (vazio = nenhuma, Astral Citadel) | `Storm\|Echo` |
| `affinity_weight` | número | Region | `2` |
| `unique_bonus_pp` | percentual | Region (0 por padrão) | `3` (Astral Citadel) |

Exemplo: `homeregion.region.astral_citadel,...,Region,,,astral_citadel,,,,Void|Ruin,2,3,05 §9.2`

## 15. `loot_global.csv` (nova)

- **Propósito:** constantes globais em chave/valor. **Chave:** `id` = `lootglobal.core.<key>`. **Destino:** propriedades de `IItemCatalog`/`ILootCatalog`, `SlotWeightData`, `SmartLootWeights`. **Linhas:** ~30.

| Coluna | Tipo | Obrig. | Exemplo |
|---|---|---|---|
| `key` | texto (único) | sim | `quantity_cap` |
| `value` | número | sim | `2.0` |
| `value_kind` | `Number`\|`Percent` | sim | `Number` |

| `key` | Destino | Origem |
|---|---|---|
| `quantity_cap` (2.0) | `QuantityCap` | 05 R45 |
| `undiscovered_unique_weight` (2) | `UndiscoveredUniqueWeight` | 05 §6 passo 5 |
| `required_level_factor` (0.5), `required_level_max` (50) | `RequiredLevelFactor`, `RequiredLevelMax` | 05 §4.3 |
| `smart_current_hero_generic` (75), `smart_current_hero_legendary` (85) | `SmartLootCurrentHero*` (Percent) | 05 §7.1 |
| `weight_irrelevant_affix` (0.25), `weight_primary_core` (2), `weight_primary_secondary` (1), `weight_primary_tertiary` (0.5), `epic_hero_modifier_share` (0.75), `equipped_skill_multiplier` (3) | `SmartWeights` | 05 §6.2, §7.2, R30 |
| `armor_implicit_per_ilvl` (4) | `ArmorImplicitPerILvl` | 01 §3.9.3 |
| `upgrade_per_level` (0.03, Percent 3) | `UpgradePerLevel` | R31 |
| `slot_weight.main_hand` ... `slot_weight.relic` (10 chaves) | `SlotWeightData.Weights` | 05 §6.1 |
| `max_primary_per_item`, `max_primary_amulet`, `max_affinity_per_item` | limites do gerador | 02 §3.9.3, R23 |

Valores dos pesos de slot e limites: copiar de 05 §6.1 e 02 §3.9.3 ao criar o CSV; não repetidos aqui.
Exemplo: `lootglobal.core.quantity_cap,Teto de Quantidade,Quantity cap,quantity_cap,2.0,Number,05 R45`

---

## Convenções

- **Arquivos:** snake_case em `DataSource/`; um importador por CSV.
- **Enums:** pelo nome exato do enum C# (`Rarity`, `ItemKind`, `EquipSlot`, `ArmorFamily`, `NamedBand`, `AffixValueKind`, `AffixCategory`, `AffixGroup`, `LootSourceKind`, `Difficulty`, `UniqueSourceKind`, `StatId`); sem combinação de flags (`CsvRow.GetEnum` não aceita). Máscaras de tag usam `TagMask` com `|`.
- **Listas:** separador `|`; célula vazia = lista vazia. Elementos compostos usam `:` (`affix_id:Banda`) e sufixo `*`.
- **Referências:** por `id` (StableId). Referências a entidades sem Definition (modificadores, sets, regiões, heróis, chefes) ficam como texto/slug e não entram em `IReferencingDefinition` (senão `ReferenceResolvesRule` vira erro); o M12 valida com regra própria (aviso).
- **IDs de modificador:** códigos de 03 como texto (`KAE-CS-01`, `GEN-27`, `SIG-K16`); **não** são StableId (maiúscula e hífen).
- **Percentuais:** número percentual no CSV; o importador divide por 100 conforme `value_kind`/sufixo `_pct`. O Core usa frações.
- **Vazio opcional:** coluna opcional vazia usa o default do tipo; obrigatória vazia é erro.
- **Reimportar** duas vezes não pode alterar nenhum arquivo.

## Lacunas conhecidas

1. **`*` fora do pool:** R23 e 11 §3.15.2 limitam a peças de set; 02 §2.14 libera para itens nomeados e K3 (Unique, `HP*`) e K4 (Legendary, `BLK*`) usam. Exceção documentada; decisão do usuário pendente.
2. **U1 (Ring, `DUR`) e U3 (Amulet, `HEF`):** opções fora do pool sem `*`, contra 02 l.562 ("nenhum universal tem afixo fora do pool"). A validação de pool os reprova; tratar como exceção documentada ou marcar `*`.
3. **Affinity do nomeado:** 02 não tem coluna de afinidade para itens nomeados (só "Build"); `items_named.affinity` precisa ser cadastrada por decisão.
4. **Leather/Vestment, implícito secundário:** linear em iLvl (`x iLvl/100`, 02 §3.4); falta definir se rola ou é fixo. Colunas `secondary_implicit_*` ainda sem campo em `ItemBaseData`.
5. **Infusions sem `StatId`:** `affixes.stat` fica vazio; "Strikes [Basic ou Skill]" sem tag `Skill`; Status Potency por afinidade só existe como `StatusPotency` genérico.
6. **Slot multiplier no implícito:** `applies_to_implicit` ambíguo (Warbrand Block cita x1,5; implícito do Ashfang não).
7. **Hard pity** de 05 §8.1 não lista Guardian, Arena Reward e Reliquary; `consumes_hard_pity` deles fica a confirmar.
8. **Raridade em cascata x absoluta** (05 §5.1/§5.3 x §5.4): `loot_chances` guarda os números; a interpretação (limiares absolutos) é decisão do gerador.

## Notas de implementação (T6)

- `items_named.csv` aceita colunas extras `effect_text_pt/en` (ignoradas pela Definition por enquanto) e `subtitle_*`/`lore_*` opcionais.
- `unique_source_ref` pode ser vazio quando `unique_source_kind=Challenge` (K15, L16, N15, O15 não têm fonte única).
- `dominant_affinities` vazio em linha Region é válido (Astral Citadel: "nenhuma", 05 §9.2).
- `loot_global.csv`: as 17 chaves fixas + 10 `slot_weight.*` de `LootGlobalKeys.AllRequired()` são obrigatórias; faltando alguma (com a tabela não vazia) a regra `LootGlobalKeys` dá erro e `ItemCatalog`/`LootCatalog` lançam `InvalidOperationException`. `max_primary_per_item`, `max_primary_amulet` e `max_affinity_per_item` alimentam as regras `PrimaryLimit`/`AffinityLimit`.
- Exceções de R23 × 02 §2.14 ficam em `OffPoolAffixRule.Exceptions` (por `catalog_no` + `abbrev`): K3 `HP*`, K4 `BLK*` (com `*`) e U1 `DUR`, U3 `HEF` (opção aberta fora do pool sem `*`).
- Referências a modificadores (`ModifierReferenceRule`): formato validado sempre; existência só quando `DataSource/modifiers.csv` existir (coluna `code`, senão `id`); até lá, um único aviso.

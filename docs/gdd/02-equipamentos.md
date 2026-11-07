# 02 — Catálogo de Equipamentos

**Dono:** itens nomeados, bases de item (dano de arma e Armor implícito usando as fórmulas de 01), **pool de afixos permitidos por slot e pesos**, compatibilidade/Hero-bound e Uniques (ver `00-convencoes.md` §2).
**Versão:** GDD v0.3 — 7 nov 2026 — revisado na rodada de integração 1 (00 §5).

---

## 1. Objetivo e escopo

Definir tudo o que um item *é* antes de cair no chão:

- Regras de identidade, raridade, nomenclatura e compatibilidade por herói.
- Bases de item por slot: as 20 armas (identidade, tipo de dano, DPS de referência, implícito) e as 4 categorias de Off Hand.
- Pool de afixos permitidos por slot e seus pesos.
- Catálogo inicial de **84 itens nomeados não-set** (54 Legendary + 30 Unique).

Fora de escopo:
- Probabilidade de drop, fontes e quotas (05-loot).
- Fórmulas, tabelas de faixas por tier, multiplicadores de slot e Faixa Nomeada (01-atributos).
- Moveset das armas: APS, combo, Heavy e alcance (08-herois, R18).
- Catálogo e valores dos modificadores (03-modificadores).
- Peças de conjunto (04-conjuntos).
- Serviços de Blacksmith e Enchanter (10-progressao).

---

## 2. Decisões tomadas

1. **Toda arma ocupa apenas Main Hand; pares (Dual Axes, Twin Wands, Dual Daggers, Twin Blades, Chakrams) contam como um item.** — Mantém os 12 slots sempre úteis e evita penalidade de duas mãos.
2. **Off Hand é uma categoria própria por herói (Warbrand, Grimoire, Quiver, Idol), nunca uma segunda arma, sempre ativa e sem bloqueio por nível (R29).** — O slot nunca fica morto. "Grimoire" substitui "Codex", que é nome reservado ao Codex do Archivist (10, R48).
3. **Armas e Off Hands são exclusivas do herói; armaduras, joias e Relics são universais.** — Identidade de combate fica travada; atributos defensivos e utilitários circulam entre heróis.
4. **Hero-bound (R27):** um item com modificador específico de habilidade, ou com efeito específico de herói, **só pode ser equipado por aquele herói**. — Evita itens "dormentes" e confusão de UI.
5. **Todo Legendary é nomeado. Não existe Legendary com afixos 100% aleatórios.** — Cumpre o GDD §6.4.
6. **Estrutura por tipo (R25):**
   - **Legendary nomeado:** 4 atributos fixos + 1 Afixo Aberto (3 opções curadas) + 2 modificadores fixos + 1 efeito.
   - **Peça de set:** 4 fixos + 1 Aberto + 2 modificadores, sem efeito próprio.
   - **Unique:** 5 fixos + 2 modificadores (até 1 Signature `SIG-<item>`, 03) + efeito Unique.
7. **Teto de More (R03):** efeito Legendary ≤ +15% More (nenhum dos 54 Legendary deste catálogo usa More); efeito Unique ≤ +40% More.
8. **Raridade não muda a faixa de um afixo.** Common, Rare, Epic e Legendary do mesmo tier rolam a mesma tabela de 01; raridade adiciona *quantidade* e *modificadores*. — GDD §15.7.
9. **Valores de afixo vêm exclusivamente de 01 (R21, R22, R24).** Tiers T1 = iLvl 1–20 … T5 = 81–100, `Roll01` uniforme dentro do tier, multiplicadores de slot de 01. Itens nomeados usam a Faixa Nomeada de 01 (piso 70%, teto 115% do máximo do tier). Este doc só escolhe *quais* afixos e em qual banda nomeada.
10. **DPS normalizado entre armas (dono deste número, R18).** O DPS de referência é 10 no iLvl 1 e ~100 no iLvl 100. O fator de arma é 1,0 (padrão), 0,9 (área ampla, contra alvo único) ou 1,05 (especialista de alvo único). Entre armas padrão do mesmo herói a tolerância é ±5%. — Armas mudam *como* se joga, não *quanto* dano se causa.
11. **Implícitos não contam no limite de atributos da raridade.** Toda arma, Off Hand, joia e Relic tem 1 implícito fixo da base, com valor igual à faixa do tier (01) × 0,5. Armaduras têm Armor implícito pela fórmula de 01.
12. **Armaduras têm 3 famílias (Plate, Leather, Vestment)**, utilizáveis por qualquer herói. Fator de base para o Armor implícito: 1,5 / 1,0 / 0,6 (01). — Permite builds como "Lyra de Plate".
13. **Relics têm 6 famílias, uma por afinidade**, com implícito de Status Potency daquela afinidade (01, 07).
14. **Pool por slot (R23):** sem afixo duplicado; máximo 1 primário (Amulet 2; nenhum em Ring, Off Hand e Relic); máximo 2 afixos de afinidade (Infusions incluídas); Movement Speed só em Feet. Item nomeado pode ter **no máximo 1 afixo fora do pool**, marcado `*`.
15. **Exibir "Qualidade da Rolagem" (= `Roll01` em %) por afixo e média do item.** — Torna legível a busca por versões melhores sem exigir perfeição.
16. **Modificadores usam exclusivamente IDs de 03 (R26).** Universais usam `GEN-xx`; modificadores exclusivos de Unique usam `SIG-<item>`.
17. **Catálogo de 30 Uniques (R28)**, cada um com exatamente um `fonteUnique`: 15 de chefe (3 por chefe), 4 Pinnacle, 7 de pool regional e 4 de Challenge (05 §9.3).

---

## 3. Corpo do design

### 3.1 Anatomia de um item

| Camada | Common | Rare | Epic | Legendary nomeado | Peça de set (04) | Unique |
|---|---|---|---|---|---|---|
| Implícito / Armor base | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Atributos | 2 aleatórios | 3 aleatórios | 4 aleatórios | 4 fixos + 1 Aberto | 4 fixos + 1 Aberto | 5 fixos |
| Faixa dos atributos | Tabela do tier (01) | Tabela do tier | Tabela do tier | Faixa Nomeada (01) | Faixa Nomeada | Faixa Nomeada |
| Modificadores | 0 | 0 | 1 aleatório (75% herói / 25% `GEN-xx`) | 2 fixos | 2 fixos | 2 fixos (até 1 `SIG-`) |
| Efeito especial | — | — | — | Efeito Legendary | — (o Set Power é a identidade) | Efeito Unique |
| Nome | Base + tier | Gerado (2 partes) | Gerado (3 partes) | Próprio | Próprio (04) | Próprio |
| Cor de UI (05/11) | Cinza | Azul | Roxo | Laranja | Verde-jade | Ouro-cobre |

**Uniques nunca pertencem a conjuntos.** A única interação de Unique com sets é a Keystone of the Sundering (U19), conforme 04.

### 3.2 Convenção de nomes

| Raridade | Regra | Exemplo |
|---|---|---|
| Common | `[Prefixo de tier] [Base]` | *Tempered Greatsword* |
| Rare | `[Base] of [Epíteto do afixo dominante]` | *Greatsword of Cinders* |
| Epic | `[Adjetivo do 2º afixo] [Base] of [Nome do modificador]` | *Swift Greatsword of the Burning Arc* |
| Legendary | `[Nome próprio]` + subtítulo `— [Base] Legendary` | *Ashfang — Legendary Greatsword* |
| Unique | `[Nome próprio]` + subtítulo `— Unique [Base]` | *Colossus Knell — Unique Warhammer* |

- Prefixos de tier: **Worn** (T1), **Forged** (T2), **Tempered** (T3), **Aether-touched** (T4), **Eclipsed** (T5).
- Epítetos por afixo dominante: Ember→*Cinders*, Frost→*Rime*, Storm→*Tempests*, Void→*the Hollow*, Echo→*Repetition*, Ruin→*Decay*, Physical→*Ruin-Iron*, Crit→*the Hunt*, Defesa→*the Bastion*, Utilidade→*the Wanderer*.
- Nomes próprios em inglês, máximo 4 palavras, sem nomes de pessoas reais ou marcas. Cada nomeado tem uma linha de lore, exibida no tooltip e registrada no Codex do Archivist ao ser descoberto.
- **Nomes reservados (R48):** não usar como nome de item Event Horizon, Frostbound, Brittle, Frozen Armor, Distortion (07), Unstoppable (06), Unstable Orb (03) nem Codex (10). Crown of Cinders é deste doc (K7); 04 renomeia sua peça homônima.

### 3.3 Tiers de Item Level

Numeração, faixas e modelo de rolagem: **01 §3.9.1** (R21). Este doc só associa arte e prefixo:

| Tier | iLvl | Prefixo (Common) | Arte da base |
|---|---|---|---|
| T1 | 1–20 | Worn | Variante A |
| T2 | 21–40 | Forged | Variante A |
| T3 | 41–60 | Tempered | Variante B |
| T4 | 61–80 | Aether-touched | Variante B |
| T5 | 81–100 | Eclipsed | Variante C |

- Não há multiplicador por tier nem interpolação dentro do tier: o valor é `Mín + Roll01 × (Máx − Mín)` da tabela do tier (01).
- Itens nomeados mantêm a arte própria em todos os tiers; só um brilho de tier muda.
- `minDropILvl`: Legendary 10, Unique 35 (05 adota). Peças de set e Uniques não caem em Normal (R28).

### 3.4 Dano base de armas e Armor implícito

```
DPS_ref(iLvl)      = 10 × FatorArma × 1,0235^(iLvl − 1)        // ≈ ×10 no iLvl 100 (Weapon Power de 06)
DanoPorGolpe(iLvl) = DPS_ref(iLvl) / APS                        // APS do moveset da arma em 08 (R18)
ArmorImplícito     = 4 × iLvl × MultiplicadorDoSlot × FatorDeBase   // fórmula de 01 §3.9.3 (R30)
```

- `FatorArma`: 1,0 padrão · 0,9 área ampla (contra alvo único) · 1,05 especialista de alvo único.
- `FatorDeBase`: Plate 1,5 · Leather 1,0 · Vestment 0,6. Exemplo: Cuirass (Plate, Chest ×1,4) no iLvl 100 = 840 Armor.
- Implícitos secundários das famílias leves, no iLvl 100 (escala linear `× iLvl/100`):
  - Leather: +2% Dodge (Chest), +1,5% (Head/Legs), +1% (Hands/Feet).
  - Vestment: +6% Shield Strength (Chest), +4% (Head/Legs), +3% (Hands/Feet).

### 3.5 Armas — 20 bases

**Dono do moveset (R18):** APS, número de golpes, combo, Heavy e alcance são de **08-herois** (tabelas por herói); frame data é de 06. Este doc define a identidade, o tipo de dano, o fator de DPS e o implícito. O implícito vale a faixa do tier de 01 × 0,5.

#### Kael — `Attack`, `Melee`, dano base Physical

| Arma | Fator DPS | Implícito | Identidade |
|---|---|---|---|
| Greatsword | 0,9 (área) | Area Damage | Controle de grupo equilibrado; base da Inferno Berserker |
| Dual Axes | 1,0 | Resource Generation (Fury) | Fury mais rápida do jogo; ataques curtos e canceláveis |
| Warhammer | 0,9 (área) | Stagger Damage | Lento e pesado, quebra postura; janelas de punição claras |
| Longsword | 1,0 | Critical Chance | Precisão e contra-ataque; parceira natural de Ashen Guard (Parry) |
| Heavy Blade | 1,05 (especialista) | Armor Penetration | Execução; base da Ruin Executioner |

#### Lyra — `Spell`, `Ranged`

| Arma | Fator DPS | Implícito | Identidade |
|---|---|---|---|
| Arcane Staff | 1,0 | Skill Damage | Versátil, Frost; arma de entrada |
| Spellbook | 0,9 (área) | Cooldown Reduction | Conjuração estratégica; acelera Arcane Charges; base da Arcane Overload |
| Twin Wands | 1,0 | Cast Speed | Alta frequência de acertos; alimenta efeitos "on hit" |
| Crystal Orb | 1,0 | Frost Damage | Cristal orbitante de auto-mira; acúmulo de Frost; base da Permafrost |
| Void Focus | 1,0 | Void Damage | Chicote e puxão em cone; base da Void Singularity |

#### Nyx — `Attack`; `Melee` ou `Ranged`/`Projectile` conforme a arma

| Arma | Tags | Fator DPS | Implícito | Identidade |
|---|---|---|---|---|
| Dual Daggers | Melee | 1,05 (especialista) | Critical Damage | Assassinato à curta distância; base da Phantom Assassin |
| Longbow | Ranged, Projectile | 1,05 (especialista) | Projectile Damage | Alcance máximo, tiros pesados e posicionamento |
| Crossbow | Ranged, Projectile | 1,0 | Armor Penetration | Ritmo de rajada e recarga; ótima contra Elites com Armor |
| Twin Blades | Melee | 1,0 | Movement Speed `*` (exceção à regra de Feet) | Velocidade constante e giros; base da Storm Reaper |
| Chakrams | Ranged, Projectile | 1,0 | Echo Damage | Ida e volta, ricochete; base da Echo Architect |

#### Orun — `Attack` ou `Spell` conforme a arma

| Arma | Tags | Fator DPS | Implícito | Identidade |
|---|---|---|---|---|
| Ritual Staff | Spell, Ranged | 1,0 | Summon Damage | Comando; base da Void Commander |
| Void Hammer | Attack, Melee | 1,0 | Void Damage | Agrupamento corpo a corpo |
| Great Mace | Attack, Melee | 1,0 | Armor (flat, faixa do tier × 0,5) | Linha de frente; base da Abyssal Guardian |
| Totem | Spell, Area | 0,9 (área) | Damage Over Time | Corrupção em área e pressão constante; base da Corruption Master |
| Ancient Scepter | Spell, Ranged | 1,0 | Resource Generation (Void Mass) | Controle tático das invocações e Decay |

**Regras de arma:**
- Toda arma tem dano base de um tipo (Physical ou afinidade). Habilidades usam o Weapon Power como Base e convertem o tipo conforme sua tag de afinidade (06).
- Attack Speed só rola em armas `Attack`; Cast Speed só em armas `Spell` (01 §3.9.4).
- Trocar de arma no Sanctuary é livre; durante a run, só em Rest Room ou Treasure Room.
- Cada uma das 20 bases tem pelo menos 1 Legendary nomeado neste catálogo (§3.12).

### 3.6 Off Hand — 4 categorias exclusivas

| Herói | Categoria | Descrição | Implícito (faixa do tier × 0,5) |
|---|---|---|---|
| Kael | **Warbrand** | Braçadeira-emblema forjada em Ember, presa ao antebraço | Block Chance (×1,5 de 01) + bloqueios geram 3 Fury |
| Lyra | **Grimoire** | Tomo flutuante que orbita a heroína | Cast Speed |
| Nyx | **Quiver** | Aljava ou bainha de energia Echo (alimenta qualquer arma de Nyx) | Resource Generation (Echo Energy) |
| Orun | **Idol** | Ídolo ritual que ancora os Void Constructs | Summon Damage |

### 3.7 Armaduras — 5 slots × 3 famílias

| Slot | Plate | Leather | Vestment |
|---|---|---|---|
| Head | Helm | Hood | Circlet |
| Chest | Cuirass | Jerkin | Robe |
| Hands | Gauntlets | Grips | Wraps |
| Legs | Greaves | Leggings | Tassets |
| Feet | Sabatons | Boots | Treads |

Armor implícito e secundários: §3.4. Nenhuma família impõe penalidade de movimento ou restrição de herói.

### 3.8 Joias e Relics

| Slot | Base | Implícito (faixa do tier × 0,5) |
|---|---|---|
| Ring | Band | Critical Chance |
| Ring | Signet | Resource Generation |
| Ring | Loop | Elemental Resistance |
| Amulet | Pendant | Skill Damage |
| Amulet | Torc | Armor (flat) |
| Amulet | Locket | Duration of Effects |
| Relic | Cinder (Ember) | Status Potency de Ember |
| Relic | Rime Shard (Frost) | Status Potency de Frost |
| Relic | Stormglass (Storm) | Status Potency de Storm |
| Relic | Lodestone (Void) | Status Potency de Void |
| Relic | Bell (Echo) | Status Potency de Echo |
| Relic | Reliquary (Ruin) | Status Potency de Ruin |
| Relic | Keystone (sem afinidade) | Exclusiva da Unique U19; implícito Status Potency (todas as afinidades) × 0,5 |

Status Potency é atributo de 01 (R30) e multiplica o SP dos status de 07.

### 3.9 Pool de afixos e pesos

#### 3.9.1 Abreviações (valores em 01 §3.9.2)

Este doc não define números de afixo (R21). Os valores vêm das tabelas por tier de 01 §3.9.2, com os multiplicadores de slot de 01 §3.9.3.

| Abrev. | Atributo | Abrev. | Atributo |
|---|---|---|---|
| STR / DEX / INT | Strength / Dexterity / Intelligence | HP | Maximum Health (flat) |
| AS / CS | Attack Speed / Cast Speed | RES | Maximum Resource |
| CC / CD | Critical Chance / Critical Damage | ARM | Armor (flat) |
| PHY | Physical Damage | PRS / ERS / VRS | Physical / Elemental / Void Resistance |
| SKD | Skill Damage | DGE / BLK | Dodge / Block Chance |
| EMB / FRO / STO / VOI / ECH / RUI | Dano da afinidade | DRD | Damage Reduction |
| DOT / ARE / PRJ | Damage Over Time / Area / Projectile Damage | HPR / HEF | Health Regeneration (flat) / Healing Effectiveness |
| APN | Armor Penetration | SHS | Shield Strength |
| STG | Stagger Damage | SRS | Status Resistance |
| SPT | Status Potency | MS | Movement Speed |
| INF-x | Infusion de afinidade x (§3.9.4) | CDR | Cooldown Reduction |
| SMD / SMT | Summon Damage / Duration | RGN / RCR | Resource Generation / Cost Reduction |
| DUR | Duration of Effects | PCK / XP / GLD | Pickup Radius / Experience / Gold |

> "DR" deixou de ser usado como abreviação de Damage Reduction (R48: a sigla é de Diminishing Returns). Status Resistance passou de "STR%" para **SRS**, para não confundir com Strength.

**Bandas da Faixa Nomeada** (dentro do intervalo de 01, que vai de 70% a 115% do máximo do tier):

| Marca | Banda | Exemplo: Ember Damage T5 (aleatório 20–30%) |
|---|---|---|
| (padrão) | 80%–100% do máximo do tier | 24–30% |
| ↑ (traço de identidade) | 95%–115% do máximo do tier | 28,5–34,5% |
| ↓ (U19 Keystone) | 70%–80% do máximo do tier | 21–24% |

O Afixo Aberto usa sempre a banda padrão.

#### 3.9.2 Multiplicadores por slot

Tabela única em **01 §3.9.3** (R22): Main Hand ×1,5 em dano%, Amulet ×1,5 em primários e resistências, Relic ×1,25 em afinidades e afins, mais os multiplicadores de flats defensivos. Este doc não mantém multiplicadores próprios.

#### 3.9.3 Afixos permitidos por slot (dono: este doc — R23)

✓ = permitido (peso 10) · ★ = característico do slot (peso 20) · — = proibido (01 §3.9.4 deriva os pesos desta tabela)

| Afixo | MH | OH | Head | Chest | Hands | Legs | Feet | Ring | Amulet | Relic |
|---|---|---|---|---|---|---|---|---|---|---|
| Primários | ✓ | — | ✓ | ✓ | ✓ | ✓ | ✓ | — | ★ (até 2) | — |
| AS / CS | ★ | ✓ | — | — | ★ | — | — | ✓ | — | — |
| CC | ✓ | ✓ | ✓ | — | ★ | — | — | ★ | — | — |
| CD | ★ | — | — | — | ✓ | — | — | ✓ | ✓ | — |
| PHY | ★ | — | — | — | ✓ | — | — | ✓ | — | — |
| SKD | ★ | ★ | ✓ | — | — | — | — | ✓ | ★ | — |
| Afinidades | ★ | ✓ | — | — | ✓ | — | — | ✓ | ✓ | ★ |
| INF-x (Infusions) | — | — | — | — | — | — | — | — | ✓ | ★ |
| DOT | ✓ | ✓ | — | ✓ | — | ✓ | — | ✓ | — | ★ |
| ARE / PRJ | ✓ | ✓ | — | — | — | ✓ | — | ✓ | — | ✓ |
| APN | ✓ | — | — | — | ✓ | — | — | ✓ | — | — |
| STG | ★ | — | — | — | ✓ | — | — | — | — | — |
| SPT | ✓ | ✓ | — | — | — | — | — | — | ✓ | ★ |
| HP | — | — | ★ | ★ | ✓ | ★ | ✓ | ✓ | ✓ | — |
| RES | — | ✓ | ✓ | — | — | — | — | ✓ | ✓ | — |
| ARM | — | — | ✓ | ★ | ✓ | ✓ | ✓ | — | — | — |
| PRS / ERS / VRS | — | — | ✓ | ★ | — | ✓ | ✓ | ✓ | ★ | — |
| DGE | — | — | — | ✓ | — | ★ | ★ | — | — | — |
| BLK | — | ★ (só Warbrand) | — | ✓ | — | — | — | — | — | — |
| DRD | — | — | — | ★ | — | ✓ | — | — | ✓ | — |
| HPR / HEF | — | — | ✓ | ✓ | — | ✓ | — | ✓ | — | — |
| SHS | — | ✓ | — | ✓ | — | — | — | — | ✓ | — |
| SRS | — | — | ★ | ✓ | — | — | ✓ | — | — | ✓ |
| MS | — | — | — | — | — | — | ★ | — | — | — |
| CDR | ✓ | ★ | ★ | — | — | — | — | — | ✓ | — |
| RGN / RCR | ✓ | ★ | — | — | ✓ | — | ✓ | ★ | ✓ | — |
| PCK / XP / GLD | — | — | ✓ (XP) | — | — | — | ✓ (PCK) | ✓ | — | — |
| DUR | ✓ | ✓ | — | — | — | ✓ | — | — | ✓ | ★ |
| SMD / SMT | ✓ (Orun) | ★ (Idol) | — | ✓ | — | — | — | ✓ | — | ★ |

Coerência com 01: Main Hand não rola flats defensivos (HP, ARM, HPR); Off Hand, Ring e Relic não rolam primários; Movement Speed só em Feet (×1,5 de 01); Block no Off Hand só na Warbrand (×1,5 de 01).

**Regras de geração (aleatórios):**
1. Sem afixo duplicado; máximo 1 primário (Amulet: 2); máximo 2 afixos de afinidade, contando dano de afinidade e Infusions.
2. Afixos de Summon (SMD/SMT) só rolam em itens gerados para Orun, exceto em Ring e Relic, que rolam para qualquer herói (Echo Clone de Nyx e constructs de set podem usá-los; 08 confirma as tags `Summon`/`Clone`).
3. 05 aplica smart loot sobre estes pesos (primários: Core ×2, Secundário ×1, Terciário ×0,5 — R30).
4. O implícito sempre rola na sua faixa (§3.5–3.8); sua qualidade também é exibida.

#### 3.9.4 Infusions (pedido de 07 §5.3)

Afixos de afinidade sem restrição de herói, que tornam qualquer Resonance acessível por equipamento (GDD §7.2). Há duas formas por afinidade:

| Forma | Efeito | Faixa |
|---|---|---|
| INF-x "Added" | X% do dano do golpe adicionado como [afinidade] (ganho-como-extra, 06) | 01 §3.9.2 (T1 2–3 … T5 6–9 %) |
| INF-x "Strikes" | Golpes com tag [Basic ou Skill, escolhida pela definição] aplicam +N de SP de [afinidade] | 01 §3.9.2 (T1 3–5 … T5 12–18 SP) |

Pool: Relic ★ (só a afinidade da família da Relic) e Amulet ✓ (qualquer afinidade). Cada Infusion conta como afixo de afinidade.

### 3.10 Compatibilidade por herói

| Categoria | Regra |
|---|---|
| Armas | Exclusivas: Kael (Greatsword, Dual Axes, Warhammer, Longsword, Heavy Blade); Lyra (Arcane Staff, Spellbook, Twin Wands, Crystal Orb, Void Focus); Nyx (Dual Daggers, Longbow, Crossbow, Twin Blades, Chakrams); Orun (Ritual Staff, Void Hammer, Great Mace, Totem, Ancient Scepter). |
| Off Hand | Exclusiva por categoria (Warbrand / Grimoire / Quiver / Idol). |
| Armaduras, Rings, Amulet, Relics | Universais. |
| Hero-bound (R27) | Item com modificador específico de habilidade, ou nomeado com efeito específico de herói, **não pode ser equipado** por outro herói. |
| Epic gerado | Modificador sorteado no pool do herói que encontrou o item (75%, vira Hero-bound) ou em `GEN-xx` (25%, permanece universal). |
| Vault | Itens Hero-bound podem ser guardados e vistos por todos, mas só equipados pelo herói dono. |

Primário Core por herói (01): Kael STR, Lyra INT, Nyx DEX, Orun INT.

### 3.11 Item Definition vs Item Instance

**Item Definition (ScriptableObject — 11 implementa):** `id`, `displayName`, `subtitle`, `lore`, `itemKind` (Base / LegendaryNamed / SetPiece / Unique), `slot`, `baseType`, `family`, `rarity`, `heroBinding`, `tags`, `implicit`, `fixedAffixes[] (affixId, band: padrão/↑/↓)`, `openAffixOptions[3]`, `modifiers[2] (modifierId de 03, incluindo GEN-xx e SIG-<item>)`, `specialEffect`, `setId` (vazio para não-set), `fonteUnique` (Chefe:<chefe> / Pinnacle / Região:<região> / Challenge; só Unique), `affinity` (para pesos regionais de 05), `offPoolAffix` (no máximo 1, `*`), `minDropILvl`, `artRef`, `vfxRef`.

**Item Instance (save):** `instanceGuid`, `definitionId` (ou `baseId` para gerados), `iLvl`, `rarity`, `implicitRoll01`, `affixes[] (affixId, roll01)`, `openAffixChoice`, `modifierRolls[] (modifierId, roll01)`, `upgradeLevel` (01 §3.9.5 e 10), `primal` (05), `heroBinding`, `flags` (new, favorite, locked, junk), `source` (região, dificuldade, tipo de sala, seed, data).

`Qualidade% = Roll01 × 100`. A "qualidade média" do item é a média simples dos afixos e do implícito. O valor exibido é recalculado a partir do `Roll01` e da tabela atual (11), para que um rebalanceamento se propague.

### 3.12 Catálogo de itens nomeados não-set (84)

**Leitura:**
- Atributos fixos usam a Faixa Nomeada (§3.9.1); `↑` = banda alta; `*` = único afixo fora do pool permitido para o item.
- "Aberto" lista as 3 opções do Afixo Aberto (só Legendary).
- Modificadores pelo ID de 03, com o nome entre parênteses. `GEN-xx` = universal. `SIG-<item>` = Signature exclusiva do Unique, definida neste catálogo (03 §3.1 documenta o formato).
- Efeitos de Execute respeitam R08: limiar máximo 20% em comuns, Elites usam metade, Guardians e chefes são imunes (contra chefe, +25% Vulnerability abaixo de 20% de vida, uma única conversão). Valores de Vulnerability de itens entram no teto de +60% de 07 (R12).

Distribuição: Kael 16, Lyra 17, Nyx 16, Orun 16, Universais 19 → **54 Legendary + 30 Unique**.

#### Exemplo completo — Ashfang (T5, Main Hand)

| Campo | Valor |
|---|---|
| Base | Greatsword · Legendary · Hero-bound (Kael) |
| Implícito | Area Damage (faixa T5 × 0,5) |
| Fixos | Strength · Attack Speed · Ember Damage (×1,5 MH) · Critical Chance — banda padrão da Faixa Nomeada T5 |
| Aberto | Area Damage / Physical Damage / Resource Generation |
| Modificadores | KAE-CS-01 (Burning Arc) · KAE-IL-10 (Combustion Landing) |
| Efeito | O Heavy da Greatsword deixa um cone de chão em chamas (4 m, 3 s) que aplica Burning. |
| Lore | "Forjada no primeiro incêndio após o Sundering; ainda morde o ar." |

#### 3.12.1 Kael — The Ashbound

| # | Item | Slot · Base | Tipo | Efeito especial | Modificadores | Build |
|---|---|---|---|---|---|---|
| K1 | **Ashfang** | MH · Greatsword | Legendary | O Heavy deixa um cone em chamas (4 m, 3 s, Burning). | KAE-CS-01 (Burning Arc) · KAE-IL-10 (Combustion Landing) | Inferno |
| K2 | **Twinmaw** | MH · Dual Axes | Legendary | Cada 5º golpe consecutivo aplica 2 acúmulos de Decay. | KAE-CS-06 (Ruin Cleaver) · KAE-BC-03 (Linked Torment) | Executioner |
| K3 | **Colossus Knell** | MH · Warhammer | **Unique** | Inimigos em Stagger recebem 25% More dano de Warhammer. Worldbreaker aplica Stagger instantâneo em não-chefes e +50% de Stagger Damage em chefes. | KAE-WB-01 (Tectonic Fissures) · KAE-IL-09 (Tectonic Landing) | Juggernaut |
| K4 | **Oathkeeper's Edge** | MH · Longsword | Legendary | Um Parry (06) com Ashen Guard torna o próximo básico crítico garantido e gera 15 Fury. | KAE-AG-01 (Ember Riposte) · KAE-CS-03 (Executioner's Reach) | Juggernaut |
| K5 | **Headsman's Ember** | MH · Heavy Blade | Legendary | O Heavy executa inimigos comuns abaixo de 15% de vida (Elites: 7,5%); contra alvos não executáveis, +30% Increased dano abaixo de 30%. | KAE-BC-01 (Chain Pull) · KAE-WB-07 (Executioner's Verdict) | Executioner |
| K6 | **Brand of the Pyre-King** | OH · Warbrand | Legendary | Ao atingir 100 Fury, a próxima habilidade não custa Fury e explode (3 m, 150% wpn como Ember). | KAE-IL-01 (Meteor Descent) · KAE-WB-09 (Pyre Crown) | Inferno |
| K7 | **Crown of Cinders** | Head · Helm | Legendary | +2% Resource Generation (Fury) por inimigo em Burning a até 5 m (máx. 20%). | KAE-CS-07 (Blood Fury) · KAE-AG-06 (Ashen Nova) | Inferno |
| K8 | **Hide of the Unyielding** | Chest · Cuirass | **Unique** | Gera 1 Fury a cada 2% de vida perdida. Acima de 50 Fury, ganha +20% Damage Reduction (sujeito ao cap de 40% de 01). | KAE-AG-07 (Unyielding) · KAE-WB-06 (Shattered Earth) | Juggernaut |
| K9 | **Grips of the Sundered Oath** | Hands · Gauntlets | Legendary | Cleaving Strike consome os acúmulos de Decay do alvo e causa 40% wpn como Ruin por acúmulo. | KAE-CS-08 (Infernal Collapse) · KAE-BC-05 (Ruin Shackles) | Executioner |
| K10 | **Greaves of the Charging Bull** | Legs · Greaves | Legendary | Infernal Leap que atinge 5+ inimigos reinicia o próprio cooldown (ICD 6 s). | KAE-IL-06 (Aftershock) · KAE-IL-02 (Scorched Trail) | Inferno |
| K11 | **Treads of Scorched Earth** | Feet · Sabatons | Legendary | Infernal Leap deixa uma trilha de fogo por 4 s; sobre ela, +15% Movement Speed. | KAE-IL-05 (Fury Leap) · KAE-AG-05 (Lingering Ash) | Inferno |
| K12 | **Ring of the Last Stand** | Ring · Band | **Unique** | Abaixo de 35% de vida: 40% More Physical Damage e Fury não decai. Uma vez a cada 60 s, dano fatal deixa Kael com 1 de vida e 2 s de invulnerabilidade. | KAE-WB-05 (Fury Reservoir) · KAE-AG-03 (Bulwark Stance) | Juggernaut |
| K13 | **Pendant of the Ashbound** | Amulet · Pendant | Legendary | Cada 25 Fury gasta reduz o cooldown de Worldbreaker em 1 s. | KAE-WB-08 (Ashfall) · KAE-BC-08 (Molten Links) | Todas |
| K14 | **Ember of the First Pillar** | Relic · Cinder | Legendary | Quando um inimigo em Burning morre, o Burning se espalha para 1 inimigo próximo (4 m) com a duração total. | KAE-BC-07 (Molten Knot) · KAE-CS-02 (Volatile Impact) | Inferno |
| K15 | **Ordeal Greaves** | Legs · Greaves | **Unique** | Cada Parry (06) com Ashen Guard concede 1 acúmulo de Ordeal (máx. 5). Cada acúmulo dá +6% More dano a Cleaving Strike e Worldbreaker (máx. 30%). Receber dano sem bloquear remove 1 acúmulo. | KAE-AG-04 (Reflective Plate) · KAE-AG-02 (Absorbing Ash) | Juggernaut |
| K16 | **Sunderbrand, the First Fault** | MH · Greatsword | **Unique** (Pinnacle) | Worldbreaker abre uma Fenda do Sundering em linha de 14 m por 6 s. Cleaving Strike usado sobre a fenda repete seu impacto na fenda inteira com 40% do dano (réplica Echo, Proc Coefficient 0,5). Worldbreaker gera 50 Fury. | KAE-WB-02 (Rolling Quake) · SIG-K16 (Pillarfall) | Todas |

`SIG-K16 Pillarfall` (Worldbreaker, Behavior): antes do impacto, puxa inimigos em 6 m para o centro (em chefes vira Distortion, 07). Valor: raio de puxão [5–6] m.

| # | Atributos fixos | Aberto | Fonte | Lore |
|---|---|---|---|---|
| K1 | STR · AS · EMB · CC | ARE / PHY / RGN | — | "Forjada no primeiro incêndio após o Sundering; ainda morde o ar." |
| K2 | STR · AS↑ · RUI · CD | PHY / APN / RGN | — | "Dois machados, uma única fome." |
| K3 | STR · STG↑ · PHY · ARE · HP* | — | Chefe: The Ashen Colossus | "Seu dobre anunciou a queda do primeiro Colosso." |
| K4 | STR · CC · CD · BLK* | AS / PHY / CDR | — | "Quem a empunha jurou nunca recuar — e nunca recuou." |
| K5 | STR · APN↑ · RUI · CD | PHY / CC / STG | — | "O carrasco de Ashenreach ainda conta suas sentenças." |
| K6 | SKD · EMB · RGN · CDR | DOT / RES / RCR | — | "Marca do último rei que ardeu com seu trono." |
| K7 | STR · HP · ERS · CC | CDR / HEF / XP | — | "Uma coroa que nunca deixou de queimar." |
| K8 | STR · HP↑ · ARM · DRD · PRS | — | Chefe: The Pale Sovereign | "Pele de algo que não sabia morrer, achada sob o gelo da Catedral." |
| K9 | STR · RUI · APN · AS | CC / CD / PHY | — | "Juramentos partidos pesam mais que aço." |
| K10 | STR · HP · ARE · DGE | ARM / DOT / DUR | — | "Quem corre com elas não sabe parar." |
| K11 | STR · MS · HP · ERS | DGE / ARM / RGN | — | "Cada passo deixa o mundo um pouco mais quente." |
| K12 | HP · CD · PHY · PRS · VRS | — | Região: The Ashen Wastes | "Usado pelo último defensor do Bastião de Varr." |
| K13 | STR · DEX · SKD · CDR | RES / DRD / DUR | — | "O nome de Kael foi gravado nele antes de ele nascer." |
| K14 | EMB↑ · DOT · ARE · DUR | PRJ / SMD / SRS | — | "Uma brasa do Pilar do Fogo, ainda viva." |
| K15 | STR · HP · ARM · DRD · PRS | — | Challenge | "Forjadas para quem aceita o golpe e devolve em dobro." |
| K16 | STR · EMB↑ · RUI · CD · ARE | — | Pinnacle | "A primeira rachadura do mundo tinha um fio." |

\* K3 rola HP em Main Hand; K4 rola BLK em Main Hand.

#### 3.12.2 Lyra — The Veilweaver

| # | Item | Slot · Base | Tipo | Efeito especial | Modificadores | Build |
|---|---|---|---|---|---|---|
| L1 | **Rimeheart** | MH · Arcane Staff | Legendary | O 3º projétil do combo cria uma Frost Nova de 3 m que aplica 2 acúmulos de Chill. | LYR-FL-02 (Splinter Burst) · LYR-AZ-01 (Lingering Winter) | Permafrost |
| L2 | **Tome of Closed Doors** | MH · Spellbook | **Unique** | Habilidades em cooldown podem ser lançadas consumindo 2 Arcane Charges (o cooldown não é reiniciado). | LYR-AS-06 (Return Vector) · LYR-SR-10 (Unstable Pages) | Overload |
| L3 | **Whisper & Wane** | MH · Twin Wands | Legendary | Acertar o mesmo alvo com as duas varinhas em 1 s aplica um acúmulo de Chill e um de Corruption (habilita Resonance Frost + Void). | LYR-FL-04 (Frost Volley) · LYR-VO-02 (Orbital) | Permafrost / Singularity |
| L4 | **Heart of the Glacier** | MH · Crystal Orb | Legendary | O cristal orbitante aplica o dobro de buildup de Freeze; após 4 golpes seguidos no mesmo alvo, aplica Freeze (DR de 06). | LYR-FL-03 (Glacial Javelin) · LYR-AZ-02 (Glacial Prison) | Permafrost |
| L5 | **Eye of the Collapse** | MH · Void Focus | **Unique** | Void Orb vira singularidade estacionária: puxa inimigos em 6 m por 3 s e implode, com +5% More dano por inimigo puxado (máx. 40%, 8 inimigos). | LYR-VO-01 (Singularity Core) · LYR-SR-08 (Inward Spiral) | Singularity |
| L6 | **Grimoire of Hollow Stars** | OH · Grimoire | Legendary | Inimigos mortos enquanto Frozen concedem 1 Arcane Charge (ICD 1 s). | LYR-AS-02 (Frost Step) · LYR-FL-09 (Arcane Lance) | Permafrost / Overload |
| L7 | **Diadem of the Pale Choir** | Head · Circlet | Legendary | Freeze aplicado por Lyra marca o alvo: o próximo dano Void recebido ganha +40% Increased. | LYR-SR-09 (Frozen Fracture) · LYR-VO-10 (Cold Collapse) | Híbrida |
| L8 | **Mantle of the Veil** | Chest · Robe | Legendary | Astral Shift deixa uma ilusão que aplica Taunt (06) por 2 s e explode em Void (120% wpn). | LYR-AS-01 (Void Wake) · LYR-AZ-07 (Frozen Sanctum) | Singularity |
| L9 | **Hands of Recursive Casting** | Hands · Wraps | **Unique** | Ao gastar a última Arcane Charge, a próxima habilidade é lançada duas vezes; a repetição é uma réplica Echo com 60% do dano (ICD 6 s). | LYR-SR-03 (Mirror Zone) · LYR-FL-08 (Overflowing Lance) | Overload |
| L10 | **Leggings of Drifting Snow** | Legs · Leggings | Legendary | A cada 2 s em movimento, ganha 1 Frozen Armor (07; absorve 8% da vida máxima; máx. 3). | LYR-AS-07 (Lingering Phase) · LYR-SR-05 (Permafrost Rift) | Permafrost |
| L11 | **Starwalker Slippers** | Feet · Treads | Legendary | Astral Shift ganha +1 carga; depois de usá-lo, a próxima habilidade tem +30% Cast Speed. | LYR-VO-09 (Rift Orb) · LYR-AS-08 (Slipstream Cast) | Overload |
| L12 | **Band of the Paradox** | Ring · Signet | Legendary | +1 Arcane Charge máxima (exceção de item ao teto base 6, limitada ao teto absoluto 8 — 08 §11.1). Gastar Charges concede +3% Cast Speed por Charge por 4 s. | LYR-VO-08 (Charge Battery) · LYR-AS-05 (Arcane Refund) | Overload |
| L13 | **Locket of the Last Winter** | Amulet · Locket | Legendary | Shatter em inimigos Frozen reduz o cooldown de Absolute Zero em 1 s. | LYR-AZ-05 (Shatterstorm) · LYR-FL-06 (Shattering Lance) | Permafrost |
| L14 | **Shard of the Silent Pillar** | Relic · Lodestone | Legendary | Efeitos de Pull de Lyra duram +50% e aplicam 1 acúmulo de Chill por segundo nos alvos puxados. | LYR-VO-05 (Growing Void) · LYR-SR-04 (Collapsing Reality) | Singularity |
| L15 | **Grimoire of the Pale Court** | OH · Grimoire | **Unique** | Cada Shatter (07) causado por Lyra libera um anel de gelo de 4 m com Chill pesado e concede +6% More Frost Damage por 6 s (acumula 5×, máx. 30%). | LYR-AZ-09 (Glacial Reserve) · SIG-L15 (Court of Rime) | Permafrost |
| L16 | **Stepless Path** | Feet · Treads | **Unique** | Enquanto Lyra tiver Arcane Charges, Astral Shift não tem cooldown: cada uso consome 1 Charge. A próxima habilidade nos 2 s seguintes causa 20% More dano. | LYR-AS-04 (Swap Shift) · LYR-VO-03 (Twin Orbs) | Overload |
| L17 | **Zero Meridian** | Amulet · Locket | **Unique** (Pinnacle) | Absolute Zero pode ser reativado durante o efeito para colapsar o campo: todos os inimigos Frozen sofrem Shatter ao mesmo tempo e o campo vira uma singularidade Void por 3 s (Pull; em chefes, Distortion). Cada inimigo estilhaçado devolve 1 Arcane Charge. | LYR-AZ-03 (Void Zero) · LYR-AZ-06 (Echo of Winter) | Permafrost / Singularity |

`SIG-L15 Court of Rime` (Frost Lance, Chain): ao acertar um alvo Frozen, a lança se divide em 3 fragmentos que aplicam Chill. Valor: [35–45]% do dano por fragmento.

| # | Atributos fixos | Aberto | Fonte | Lore |
|---|---|---|---|---|
| L1 | INT · CS · FRO · SKD | PRJ / CC / CDR | — | "O coração de gelo bate uma vez por século." |
| L2 | INT · CDR↑ · SKD · CS · FRO | — | Região: The Frozen Cathedral | "Algumas portas nunca deveriam ter sido fechadas." |
| L3 | INT · CS↑ · FRO · VOI | CC / SKD / RGN | — | "Uma sussurra, a outra apaga." |
| L4 | INT · FRO↑ · DUR · CS | SKD / DOT / SPT | — | "Arrancado do glaciar que engoliu a Catedral." |
| L5 | INT · VOI↑ · ARE · SKD · CDR | — | Chefe: The Devouring Presence | "Olhar para ele é olhar para o fim." |
| L6 | CS · SKD · RES · CDR | FRO / DUR / RCR | — | "As estrelas descritas nele já não existem." |
| L7 | INT · HP · VRS · CDR | CC / ERS / XP | — | "Usada pelo coro que cantou para o gelo." |
| L8 | INT · HP · SHS · VRS | DRD / DGE / HEF | — | "Tecido com fios do próprio véu da realidade." |
| L9 | INT · CS · CC · RGN · SKD* | — | Chefe: The Fractured One | "Mãos que lançam o mesmo feitiço em dois tempos." |
| L10 | INT · HP · DGE · DUR | ARM / ERS / ARE | — | "A neve que cai sobre elas nunca derrete." |
| L11 | INT · MS↑ · HP · RCR | DGE / VRS / PCK | — | "Usadas por quem caminhava entre as estrelas." |
| L12 | CS · CC · RES · RGN | SKD / VOI / FRO | — | "Um paradoxo usado no dedo." |
| L13 | INT · DEX · FRO · CD | SKD / CDR / ERS | — | "Guarda o último inverno antes do Sundering." |
| L14 | VOI↑ · FRO · DUR · ARE | DOT / SRS / SKD | — | "Um fragmento do Pilar que silenciava o vazio." |
| L15 | CS · FRO↑ · SKD · CDR · DUR | — | Chefe: The Pale Sovereign | "A corte da Soberana Pálida ainda se reúne nestas páginas." |
| L16 | INT · MS · HP · VRS · RCR | — | Challenge | "Quem já não precisa de passos não deixa rastros." |
| L17 | INT · DEX · FRO↑ · VOI · CDR | — | Pinnacle | "A linha onde o frio e o vazio se tornam a mesma coisa." |

\* L9 rola SKD em Hands. Nenhum outro item de Lyra tem afixo fora do pool.

#### 3.12.3 Nyx — The Echo Hunter

| # | Item | Slot · Base | Tipo | Efeito especial | Modificadores | Build |
|---|---|---|---|---|---|---|
| N1 | **Twin Fangs of Vael** | MH · Dual Daggers | Legendary | Acertos pelas costas aplicam *Hunter's Brand* (efeito do item, 4 s); Shadowstep em alvo com Hunter's Brand reinicia o cooldown (ICD 4 s). | NYX-SS-03 (Blink Strike) · NYX-PS-03 (Charged Shot) | Assassin |
| N2 | **Thunderstring** | MH · Longbow | Legendary | O tiro carregado completo vira Chain Lightning com 4 saltos (70% do dano por salto). | NYX-PS-01 (Fork Shot) · NYX-ST-03 (Overload Trap) | Storm Reaper |
| N3 | **Gallows Repeater** | MH · Crossbow | Legendary | O 3º virote de cada rajada executa inimigos comuns abaixo de 12% de vida (Elites: 6%); abates reduzem a próxima recarga em 50%. | NYX-PS-08 (Marked Prey) · NYX-SS-06 (Kinetic Reload) | Assassin |
| N4 | **Tempest Pair** | MH · Twin Blades | **Unique** | Cada 10% de Movement Speed acima da base concede 6% More dano Storm (máx. 36%). A investida giratória (Heavy) aplica Shock. | NYX-SS-01 (Lightning Dash) · NYX-TR-03 (Storm Prism) | Storm Reaper |
| N5 | **Orbit of the Twice-Born** | MH · Chakrams | **Unique** | O retorno do chakram repete os efeitos "on hit" da ida (réplica Echo, Proc Coefficient 0,5). Echo Clones também arremessam chakrams. | NYX-EC-04 (Flawless Mirror) · NYX-PS-02 (Ricochet Bolt) | Echo Architect |
| N6 | **Phantom Quiver** | OH · Quiver | Legendary | A cada 4º Phantom Shot, dispara uma cópia Echo com 50% do dano. | NYX-PS-05 (Echo Shot) · NYX-EC-06 (Lingering Echo) | Echo Architect |
| N7 | **Hood of the Unseen** | Head · Hood | Legendary | Após Shadowstep, 1,5 s de Stealth (06); o primeiro acerto é crítico garantido com +50% Critical Damage. | NYX-SS-02 (Phantom Lure) · NYX-SS-07 (Execution Step) | Assassin |
| N8 | **Stormweave Vest** | Chest · Jerkin | Legendary | Inimigos em Shock que atingem Nyx sofrem descarga Storm de 80% wpn; aplicar Shock concede +5% Movement Speed por 3 s (acumula 3×). | NYX-ST-08 (Static Field Trap) · NYX-SS-08 (Static Wake) | Storm Reaper |
| N9 | **Grips of a Thousand Cuts** | Hands · Grips | Legendary | Cada acerto consecutivo concede +2% Attack Speed (máx. 20%); zera após 1,5 s sem acertos. | NYX-PS-04 (Phantom Volley) · NYX-TR-07 (Endless Cuts) | Storm Reaper / Assassin |
| N10 | **Legwraps of the Hunted** | Legs · Leggings | Legendary | Um Perfect Dodge (06) torna o próximo Phantom Shot crítico e gera 15 Echo Energy. | NYX-SS-04 (Chain Step) · NYX-ST-01 (Tesla Network) | Assassin |
| N11 | **Echo-Step Boots** | Feet · Boots | Legendary | Shadowstep deixa um Echo Clone na origem por 2 s com 30% do dano de Nyx. | NYX-EC-05 (Phase Swap) · NYX-EC-02 (Explosive Echo) | Echo Architect |
| N12 | **Signet of the Hunt** | Ring · Band | Legendary | +25% Increased dano contra inimigos abaixo de 35% de vida; abates devolvem 20 Echo Energy. | NYX-PS-06 (Void Arrow) · NYX-TR-06 (Execution Mirage) | Assassin |
| N13 | **Mirror of Many Faces** | Amulet · Locket | **Unique** | +1 Echo Clone simultâneo. Clones copiam Thousand Reflections com 40% do dano (cópia de clone, Proc Coefficient 0,4). | NYX-EC-08 (Spectral Reach) · NYX-TR-01 (Endless Reflections) | Echo Architect |
| N14 | **Spark of the Broken Pillar** | Relic · Stormglass | Legendary | Quando um efeito Echo repete uma Storm Trap, a repetição dispara Chain Lightning com 3 saltos (Generation 1). | NYX-ST-06 (Echo Mine) · NYX-ST-09 (Chain Snare) | Storm / Echo |
| N15 | **Duelist's Second Skin** | Chest · Jerkin | **Unique** | Um Perfect Dodge (06) cria um Echo Clone por 3 s que copia o próximo ataque de Nyx (Proc Coefficient 0,4). Enquanto Nyx não for atingida por 5 s, causa 20% More dano; ser atingida remove o bônus. | NYX-SS-05 (Rewind Step) · NYX-EC-07 (Taunting Shade) | Assassin / Echo |
| N16 | **The Last Reflection** | Relic · Bell | **Unique** (Pinnacle) | Ao fim de Thousand Reflections, fica um Reflexo Final por 8 s que copia a última habilidade usada por Nyx a cada 2 s com 50% do dano (cópia de clone, Proc Coefficient 0,4). Abates durante a Ultimate reduzem seu cooldown em 0,5 s (ICD 0,1 s). | NYX-TR-05 (Echo Charge) · NYX-TR-04 (Reflection Army) | Echo Architect |

| # | Atributos fixos | Aberto | Fonte | Lore |
|---|---|---|---|---|
| N1 | DEX · CD↑ · CC · AS | PHY / APN / MS* | — | "Vael caçou com elas até se tornar um eco." |
| N2 | DEX · STO↑ · PRJ · CC | AS / CD / RGN | — | "A corda vibra antes de cada tempestade." |
| N3 | DEX · APN↑ · CD · PRJ | AS / CC / PHY | — | "Seu estalo era o último som que muitos ouviam." |
| N4 | DEX · MS↑* · STO · AS · CC | — | Chefe: The Fractured One | "Duas lâminas que nunca tocam o chão." |
| N5 | DEX · ECH↑ · PRJ · AS · DUR | — | Chefe: The Fractured One | "Nasceram duas vezes; morrem nunca." |
| N6 | PRJ · ECH · RGN · CDR | SKD / CC / RCR | — | "Cada flecha guarda a memória da anterior." |
| N7 | DEX · CC · HP · DGE* | CD / CDR / VRS | — | "Ninguém viu seu dono — esse era o objetivo." |
| N8 | DEX · HP · ERS · DGE | ARM / DOT / HPR | — | "Trama de raios presos em couro de wyrm." |
| N9 | DEX · AS↑ · CC · STO | CD / PHY / RGN | — | "Mil cortes, nenhum desperdiçado." |
| N10 | DEX · DGE↑ · HP · DUR | ARM / VRS / ARE | — | "Feitas para quem é caçado e sobrevive." |
| N11 | DEX · MS · DGE · HP | SRS / RCR / PCK | — | "Seus passos chegam antes de você." |
| N12 | CC · CD · ECH · RGN | AS / PHY / STO | — | "Selo dos caçadores que nunca erram o segundo tiro." |
| N13 | DEX · INT · ECH · CDR · DUR | — | Região: The Shattered Expanse | "Mostra quem você foi, é e será — ao mesmo tempo." |
| N14 | STO · ECH · DUR · ARE | PRJ / SRS / DOT | — | "Faísca do Pilar que comandava o tempo." |
| N15 | DEX · HP · DGE · ERS · SRS | — | Challenge | "Uma pele que aprende cada golpe que não recebeu." |
| N16 | ECH↑ · STO · DUR · PRJ · SPT | — | Pinnacle | "O último reflexo é o único que não se apaga." |

\* N1 pode ter MS como Aberto em Main Hand; N4 rola MS em Main Hand; N7 rola DGE em Head.

#### 3.12.4 Orun — The Voidkeeper

| # | Item | Slot · Base | Tipo | Efeito especial | Modificadores | Build |
|---|---|---|---|---|---|---|
| O1 | **Staff of the Hollow Choir** | MH · Ritual Staff | Legendary | Cada básico de Orun faz todos os constructs dispararem um eco do projétil com 25% do dano (réplica Echo). | ORU-GF-01 (Shard Sentinels) · ORU-VT-06 (Construct Conduit) | Commander |
| O2 | **Gravemaw** | MH · Void Hammer | Legendary | Um Gravity Slam (Heavy) que puxa 5+ inimigos causa Implosion (2,5 m, 150% wpn). | ORU-DR-02 (Hungry Rift) · ORU-VT-04 (Rending Pull) | Guardian / Commander |
| O3 | **Bulwark of Nothing** | MH · Great Mace | **Unique** | Acertos concedem Shield de 3% da vida máxima (teto de 30%). Com Shield ativo, Guardian Fragment absorve 30% do dano que Orun recebe. | ORU-GF-02 (Aegis Link) · ORU-CW-07 (Empowering Wave) | Guardian |
| O4 | **Totem of the Rotting Sun** | MH · Totem | Legendary | Os pulsos do Totem aplicam Decay; o Decay aplicado pelo Totem dura +50% (o máximo de 5 acúmulos de 07 não muda). | ORU-CW-09 (Lingering Rot) · ORU-VT-03 (Ruin Tendrils) | Corruption |
| O5 | **Scepter of the Unbound King** | MH · Ancient Scepter | **Unique** | +1 Void Construct máximo (exceção de item, teto absoluto 8 — 08 §11.1). Cada construct ativo concede 5% More Summon Damage (máx. 40%). The Unbound consome os constructs, causando 100% do dano de cada um. | ORU-UB-02 (Unchained Detonation) · ORU-GF-07 (Mirrored Construct) | Commander |
| O6 | **Idol of the Forgotten** | OH · Idol | Legendary | Quando um construct morre ou expira, libera uma Corruption Wave com 50% do dano. | ORU-GF-05 (Volatile Construct) · ORU-CW-01 (Returning Tide) | Commander / Corruption |
| O7 | **Helm of the Abyss Warden** | Head · Helm | Legendary | Inimigos a até 4 m ficam com Slow de 15%; Orun ganha +2% Damage Reduction por inimigo a até 4 m (máx. 10%). | ORU-VT-05 (Lingering Tendrils) · ORU-GF-03 (Void Bastion) | Guardian |
| O8 | **Carapace of the Devourer** | Chest · Cuirass | Legendary | Devouring Rift cura 2% da vida máxima por inimigo consumido e converte o excedente em Shield. | ORU-DR-07 (Nurturing Abyss) · ORU-UB-04 (Endless Night) | Guardian |
| O9 | **Hands of the Corruptor** | Hands · Wraps | Legendary | Corruption Wave aplica 15% Vulnerability por 4 s (fonte fixa; teto de +60% de 07). | ORU-CW-02 (Expanding Ring) · ORU-DR-10 (Rotting Rift) | Corruption |
| O10 | **Tassets of the Void Legion** | Legs · Tassets | Legendary | Constructs herdam 50% do Armor e das resistências de Orun. | ORU-GF-06 (Enduring Fragment) · ORU-VT-07 (Feeding Tendrils) | Commander |
| O11 | **Stride of the Hollow** | Feet · Treads | Legendary | Com 2+ constructs a até 6 m: +12% Movement Speed e +20% Increased Summon Damage. | ORU-DR-03 (Wandering Rift) · ORU-GF-09 (Crushing Fragment) | Commander |
| O12 | **Ring of Endless Rot** | Ring · Loop | Legendary | DoTs Ruin de Orun duram +30%; inimigos que morrem com Decay espalham 50% dos acúmulos em 4 m. | ORU-CW-08 (Spreading Blight) · ORU-VT-08 (Withering Grasp) | Corruption |
| O13 | **Heart of the Hollow Depths** | Amulet · Torc | **Unique** | The Unbound não tem cooldown: é carregado pelo dano de DoT Ruin causado (cada 300% wpn em DoT = 10% de carga). | ORU-UB-08 (Rotting Unbound) · ORU-CW-04 (Plague Wall) | Corruption |
| O14 | **Splinter of the Void Pillar** | Relic · Lodestone | Legendary | Efeitos de Pull de Orun levam os inimigos até o construct mais próximo, em vez do ponto de origem. | ORU-VT-01 (Grasping Abyss) · ORU-DR-04 (Early Collapse) | Commander / Guardian |
| O15 | **Ring of the Hollow Covenant** | Ring · Signet | **Unique** | Invocar um Void Construct com o máximo já ativo sacrifica o mais antigo, que libera um Devouring Rift menor (2,5 m, 2 s, 60% do dano). Cada sacrifício concede +5% More Summon Damage por 5 s (acumula 4×, máx. 20%). | ORU-DR-05 (Rift Twins) · ORU-GF-10 (Void-Laced Fragment) | Commander |
| O16 | **Crown of the Unbound** | Head · Helm | **Unique** (Pinnacle) | The Unbound não consome os constructs: durante a Ultimate, cada construct ativo vira um Fragmento Ascendido (+50% tamanho, ataques em área de 2,5 m). Ao terminar, cada construct libera uma Corruption Wave com 60% do dano. | ORU-UB-09 (Insatiable Night) · ORU-UB-07 (Vast Form) | Commander / Corruption |

| # | Atributos fixos | Aberto | Fonte | Lore |
|---|---|---|---|---|
| O1 | INT · SMD↑ · VOI · CS | SMT / SKD / RGN | — | "Cada construct canta uma nota do coro oco." |
| O2 | STR · VOI · ARE · STG | PHY / AS / HP* | — | "Sua boca não tem fundo." |
| O3 | STR · PHY · STG · VOI · HP* | — | Chefe: The Ashen Colossus | "Feito de nada — e nada o atravessa." |
| O4 | INT · DOT↑ · RUI · DUR | ARE / SKD / CDR | — | "Um sol que só ilumina o que apodrece." |
| O5 | INT · SMD↑ · SMT · CDR · RGN | — | Chefe: The Devouring Presence | "O rei que libertou tudo e ficou sem nada." |
| O6 | SMD · SMT · RUI · RGN | DOT / CDR / VOI | — | "Esculpido por aqueles que o mundo apagou." |
| O7 | INT · HP · ARM · SRS | VRS / PRS / CDR | — | "O guardião do abismo nunca dorme." |
| O8 | INT · HP↑ · DRD · VRS | HEF / SHS / ARM | — | "Carapaça de uma fome que nunca acaba." |
| O9 | INT · RUI · DOT* · CS | CC / SKD / RGN | — | "Tudo o que tocam começa a apodrecer." |
| O10 | INT · HP · ARM · SMT* | DGE / PRS / ERS | — | "Placas de uma legião que nunca existiu." |
| O11 | INT · MS · HP · SMD* | DGE / VRS / RCR | — | "Quem anda à frente da legião anda sozinho." |
| O12 | DOT↑ · RUI · DUR · RGN | SKD / CC / VRS | — | "Gira sem fim, como a decadência." |
| O13 | INT · STR · RUI · DOT* · VRS | — | Chefe: The Devouring Presence | "Pulsa no ritmo das profundezas." |
| O14 | VOI↑ · SMD · ARE · DUR | SMT / SRS / DOT | — | "Lasca do Pilar que mantinha o vazio fechado." |
| O15 | SMD · SMT · VOI · RGN · CC | — | Challenge | "Todo pacto com o vazio cobra um dos seus." |
| O16 | INT · HP · SKD · CDR · VRS | — | Pinnacle | "Coroa de quem não precisa mais de correntes." |

\* O2 Aberto HP em Main Hand; O3 HP em Main Hand; O9 DOT em Hands; O10 SMT em Legs; O11 SMD em Feet; O13 DOT em Amulet.

#### 3.12.5 Universais (sem herói; modificadores `GEN-xx`)

| # | Item | Slot · Base | Tipo | Efeito especial | Modificadores |
|---|---|---|---|---|---|
| U1 | **Aetherbound Loop** | Ring · Loop | Legendary | Cada afinidade diferente aplicada em inimigos nos últimos 5 s concede +6% Increased dano (máx. 36%). | GEN-27 (Twin Affinity) · GEN-11 (Wide Reach) |
| U2 | **Band of Twin Eclipses** | Ring · Band | **Unique** | O primeiro status de afinidade aplicado num alvo também aplica 1 acúmulo do status primário da outra afinidade do herói. | GEN-27 (Twin Affinity) · GEN-28 (Volatile) |
| U3 | **Seal of the Remnants** | Amulet · Torc | Legendary | +20% de efeito nas Blessings de Shrine (09); Rest Rooms curam 15% adicionais. | GEN-29 (Fortifying) · GEN-25 (Thrifty Casting) |
| U4 | **Vestments of the Sanctuary** | Chest · Robe | Legendary | Ao entrar numa sala de combate, ganha Shield de 15% da vida máxima por 6 s. | GEN-29 (Fortifying) · GEN-26 (Long Fuse) |
| U5 | **Hands of the Pillarwright** | Hands · Gauntlets | **Unique** | Todos os atributos primários equipados contam como o primário Core do herói (01). | GEN-30 (Rapid) · GEN-25 (Thrifty Casting) |
| U6 | **Gauntlets of Steady Hands** | Hands · Grips | Legendary | Acertos críticos causam +50% Stagger Damage; ataques contra alvos em Stagger têm +10% Critical Chance. | GEN-30 (Rapid) · GEN-28 (Volatile) |
| U7 | **Rift Runner's Boots** | Feet · Boots | Legendary | Esquivar através de inimigos aplica Slow de 20% por 2 s e reduz a recarga da esquiva em 0,3 s (ICD 1,5 s). A esquiva base de 06 não muda (R06). | GEN-15 (Evasive Momentum) · GEN-26 (Long Fuse) |
| U8 | **Helm of the Forgotten King** | Head · Helm | **Unique** | Matar um Elite concede um de seus afixos de Elite (catálogo de 09) por 20 s. | GEN-28 (Volatile) · GEN-27 (Twin Affinity) |
| U9 | **Legplates of the Long Road** | Legs · Greaves | Legendary | Cada sala concluída sem cair abaixo de 50% de vida concede +3% Damage Reduction (máx. 15%), perdidos ao cair abaixo de 50%. | GEN-29 (Fortifying) · GEN-26 (Long Fuse) |
| U10 | **Cinder of the Sundering** | Relic · Cinder | Legendary | Qualquer explosão causada pelo herói aplica Burning. | GEN-28 (Volatile) · GEN-11 (Wide Reach) |
| U11 | **Tear of the Frozen Choir** | Relic · Rime Shard | Legendary | Slow aplicado pelo herói acumula buildup de Freeze 25% mais rápido. | GEN-26 (Long Fuse) · GEN-07 (Splitting) |
| U12 | **Stormglass Prism** | Relic · Stormglass | Legendary | Efeitos de Chain ganham +1 salto. | GEN-07 (Splitting) · GEN-30 (Rapid) |
| U13 | **Hollow Lodestone** | Relic · Lodestone | **Unique** | Efeitos de Pull são 50% mais fortes; inimigos que colidem durante um Pull sofrem 40% wpn como Void por inimigo colidido (máx. 5). | GEN-11 (Wide Reach) · GEN-27 (Twin Affinity) |
| U14 | **Bell of Second Hours** | Relic · Bell | Legendary | 15% de chance de habilidades não-Ultimate se repetirem após 1 s com 40% do dano (réplica Echo, Delayed Effect). | GEN-24 (Resonant Repeat) · GEN-26 (Long Fuse) |
| U15 | **Furnace of the Colossus** | Amulet · Torc | **Unique** | Explosões causadas pelo herói deixam uma poça de magma (2 m, 3 s) que aplica Burning. Inimigos em Burning recebem 20% More dano de Explosion do herói. | GEN-28 (Volatile) · SIG-U15 (Molten Wake) |
| U16 | **Shroud of the Still Court** | Chest · Robe | **Unique** | Slow aplicado pelo herói também aplica 1 acúmulo de Chill por segundo. Ao sofrer Hard CC, o herói libera uma nova de gelo de 4 m que congela não-chefes por 1 s (DR de 06; ICD 12 s). | GEN-29 (Fortifying) · GEN-02 (Rimebrand) |
| U17 | **Cinderwake Boots** | Feet · Boots | **Unique** | A esquiva deixa um rastro de brasas por 2 s que aplica Burning. Sobre as brasas, o herói tem +10% Movement Speed e o Burning que aplica dura +30%. | GEN-14 (Kinetic Wake) · GEN-01 (Emberbrand) |
| U18 | **Prism of the Six Pillars** | Amulet · Pendant | **Unique** | Quando uma Resonance (07) dispara, o herói ganha +5% More dano da afinidade de origem por 8 s (acumula uma vez por afinidade distinta, máx. 30%). | GEN-27 (Twin Affinity) · GEN-13 (Lingering Affliction) |
| U19 | **Keystone of the Sundering** | Relic · Keystone | **Unique** | Conta como +1 peça de **cada** set que tenha ≥ 2 peças distintas equipadas, sem levar nenhum set além de 6; respeita o limite de 2 Set Powers ativos (04). | GEN-25 (Thrifty Casting) · GEN-26 (Long Fuse) |

`SIG-U15 Molten Wake` (genérico, requer `Area`, Behavior): o ponto de impacto da habilidade atribuída deixa magma por 2 s. Valor: [15–22]% wpn/s como Ember.

| # | Atributos fixos | Aberto | Fonte | Lore |
|---|---|---|---|---|
| U1 | CC · SKD · ERS · VRS | CD / RGN / DUR | — | "Seis afinidades presas num só aro." |
| U2 | CD · CC · SKD · RGN · ECH | — | Chefe: The First Forgotten | "Dois eclipses, um único instante." |
| U3 | HP · ERS · VRS · DRD | HEF / RES / CDR | — | "Selo dos que ainda acreditam em reconstruir." |
| U4 | HP↑ · SHS · HEF · VRS | ARM / DRD / SRS | — | "Tecidas no Sanctuary, abençoadas por todos os Remnants." |
| U5 | HP · AS · CS · CC · RGN | — | Chefe: The First Forgotten | "As mãos que ergueram os Pilares." |
| U6 | CC↑ · STG · ARM · AS | CD / PHY / APN | — | "Firmes como a primeira pedra." |
| U7 | MS↑ · DGE · HP · SRS | PCK / RCR / ERS | — | "Corriam pelas fendas antes que se fechassem." |
| U8 | HP · CDR · CC · SRS · XP | — | Chefe: The First Forgotten | "O rei esquecido lembra de todos que o derrotaram." |
| U9 | HP↑ · ARM · DRD · HPR | DGE / PRS / VRS | — | "A estrada longa ensina a não cair." |
| U10 | EMB · ARE · DOT · DUR | PRJ / SPT / SRS | — | "Uma cinza do próprio Sundering." |
| U11 | FRO · DUR↑ · DOT · ARE | PRJ / SPT / SRS | — | "Lágrima congelada no meio da canção." |
| U12 | STO · ARE · PRJ · DUR | DOT / SPT / SRS | — | "Prisma que divide raios em mil caminhos." |
| U13 | VOI↑ · ARE · DUR · DOT · SMD | — | Região: The Hollow Depths | "Atrai tudo — até a luz." |
| U14 | ECH · DUR↑ · SPT · ARE | DOT / PRJ / SRS | — | "Toca sempre duas vezes." |
| U15 | STR · INT · EMB↑ · SKD · ERS | — | Chefe: The Ashen Colossus | "O coração do Colosso nunca esfriou." |
| U16 | HP · ARM · ERS · SRS · DRD | — | Chefe: The Pale Sovereign | "A corte pálida guardava silêncio sob este manto." |
| U17 | MS · HP · ERS · DGE · ARM | — | Região: The Ashen Wastes | "Cada passo é uma fagulha que o deserto lembra." |
| U18 | SKD · CD · ERS · VRS · CDR | — | Região: The Astral Citadel | "Seis Pilares, uma luz." |
| U19 | DUR↓ · DOT↓ · ARE↓ · SRS↓ · SPT↓ | — | Região: The Astral Citadel | "A pedra que fecha o arco também pode abri-lo." |

Nenhum universal tem afixo fora do pool. U5 fixa Attack Speed e Cast Speed ao mesmo tempo (ambos ★ em Hands) por ser universal. U19 usa a banda ↓ (70–80% do máximo do tier) em todos os atributos, como compensação definida por 04.

#### 3.12.6 Uniques por fonte (R28 — 05 §9.3)

| Fonte | Uniques | Total |
|---|---|---|
| Chefe: The Ashen Colossus | K3 Colossus Knell · O3 Bulwark of Nothing · U15 Furnace of the Colossus | 3 |
| Chefe: The Pale Sovereign | L15 Grimoire of the Pale Court · K8 Hide of the Unyielding · U16 Shroud of the Still Court | 3 |
| Chefe: The Fractured One | N4 Tempest Pair · N5 Orbit of the Twice-Born · L9 Hands of Recursive Casting | 3 |
| Chefe: The Devouring Presence | O5 Scepter of the Unbound King · O13 Heart of the Hollow Depths · L5 Eye of the Collapse | 3 |
| Chefe: The First Forgotten | U2 Band of Twin Eclipses · U5 Hands of the Pillarwright · U8 Helm of the Forgotten King | 3 |
| Pinnacle (The Sundering Heart) | K16 Sunderbrand · L17 Zero Meridian · N16 The Last Reflection · O16 Crown of the Unbound (1 por herói, modificam a Ultimate) | 4 |
| Região | Ashen Wastes: K12, U17 · Frozen Cathedral: L2 · Shattered Expanse: N13 · Hollow Depths: U13 · Astral Citadel: U18, U19 | 7 |
| Challenge (Eclipse+) | K15 Ordeal Greaves · L16 Stepless Path · N15 Duelist's Second Skin · O15 Ring of the Hollow Covenant | 4 |
| **Total** | | **30** |

Cada chefe tem pelo menos um Unique utilizável por qualquer herói ou por dois heróis diferentes, para que o farm de um chefe não sirva a um único herói.

### 3.13 Orçamento de efeitos (para balanceamento)

| Tipo de efeito | Teto de poder |
|---|---|
| Efeito Legendary | Até 15% de ganho efetivo médio ou mudança de comportamento equivalente; More ≤ 15% (R03). Nenhum dos 54 Legendary deste catálogo usa More. |
| Efeito Unique | Até 25% de ganho efetivo médio; More ≤ 40% (R03). |
| Mesmo Unique | Não acumula com cópia de si mesmo (2 Rings ou 2 Relics iguais: só um ativo). |
| Hero-bound | Sem limite rígido além do espaço de slots. |

Fontes More declaradas (todas de Unique, todas condicionais):

| Item | More |
|---|---|
| K3 Colossus Knell | 25% (alvo em Stagger) |
| K12 Ring of the Last Stand | 40% Physical (abaixo de 35% de vida) |
| K15 Ordeal Greaves | até 30% (5 acúmulos) |
| L5 Eye of the Collapse | até 40% (8 inimigos puxados) |
| L15 Grimoire of the Pale Court | até 30% Frost (5 acúmulos) |
| L16 Stepless Path | 20% (2 s após Astral Shift) |
| N4 Tempest Pair | até 36% Storm |
| N15 Duelist's Second Skin | 20% (sem ser atingida por 5 s) |
| O5 Scepter of the Unbound King | até 40% Summon (8 constructs, teto absoluto) |
| O15 Ring of the Hollow Covenant | até 20% Summon (4 acúmulos) |
| U15 Furnace of the Colossus | 20% em Explosion contra alvos em Burning |
| U18 Prism of the Six Pillars | até 30% (6 afinidades distintas) |

Os Pinnacle (K16, L17, N16, O16) não usam More: o poder deles está em mudar o comportamento da Ultimate.

---

## 4. Dependências e pontos de integração

| Doc | O que este doc fornece | O que este doc consome |
|---|---|---|
| 01-atributos | Pool por slot (§3.9.3, fonte dos pesos de 01 §3.9.4); Status Potency no pool; bandas nomeadas (padrão, ↑, ↓); Infusions | Tabelas de faixas por tier, Roll01, multiplicadores de slot, Faixa Nomeada 70–115%, fórmula de Armor implícito, upgrade. Faixas de Infusion incorporadas em 01 §3.9.2. |
| 03-modificadores | Uso dos IDs de 03 em todo o catálogo; 3 Signatures (SIG-K16, SIG-L15, SIG-U15) | Catálogo, valores, regra de duplicata (§3.3 de 03: vale a maior rolagem quando dois itens trazem o mesmo ID) |
| 04-conjuntos | Regra: Uniques nunca são peça de set; Keystone of the Sundering definida como U19 (banda ↓, modificadores Resource/Duration); estrutura da peça de set (§3.1); Crown of Cinders reservado a K7 | Slots ocupados pelos sets. **Pedido:** 04 renomeia sua peça "Crown of Cinders" e recalcula a Rota A com os efeitos reais deste catálogo (até 15% por Legendary, não 3,7%). |
| 05-loot | `minDropILvl` (Legendary 10, Unique 35); pesos ★/✓; `fonteUnique` dos 30 Uniques (§3.12.6); `affinity` por item; regra Epic 75/25 | Taxas de drop, quotas, smart loot, pity, Primal, região-lar |
| 06-combate | Fator de DPS por arma; uso de Parry, Perfect Dodge, Stealth, Taunt e Execute nos efeitos | Weapon Power, frame data, HitFlags, DR de CC, Proc Coefficients |
| 07-resonance | Implícito de Relics (Status Potency por afinidade); Infusions em Relic/Amulet; valores fixos de Vulnerability (O9: 15%) | Status primários, Frozen Armor, Shatter, Generation, teto de Vulnerability |
| 08-herois | Off Hand: Warbrand, Grimoire, Quiver, Idol; L12 (+1 Charge) e O5 (+1 Construct) como fontes de item do teto de recurso | Moveset das armas (APS, combo, Heavy, alcance — R18). **Pedido:** listar L12 e O5 na tabela §11.1 de exceções de teto, e confirmar as tags `Summon`/`Clone`. |
| 09-inimigos | Uniques de chefe (3 por chefe) e U8 (afixos de Elite) | Catálogo de afixos de Elite, temas dos chefes |
| 10-progressao | Campo `upgradeLevel`; 4 Uniques Pinnacle e 15 de chefe com `fonteUnique`; U3 interage com Shrine e Rest Room | Upgrade (+3%/nível), Enchant, Imbue, reroll do Afixo Aberto, Heart Exchange |
| 11-arquitetura | Campos de Item Definition e Instance (§3.11), incluindo `itemKind`, `fonteUnique`, `offPoolAffix` e Roll01 | Implementação de SO, sistema de tags e save |

---

## 5. Conflitos com as convenções

Nenhum conflito em aberto. Resoluções da rodada de integração 1 aplicadas aqui:

- **R03:** teto de More (Legendary ≤ 15%, Unique ≤ 40%); Eye of the Collapse de 50% para 40%.
- **R06:** Rift Runner's Boots não dá mais carga de esquiva.
- **R08:** execuções de K5 e N3 com metade do limiar para Elites; chefes e Guardians imunes.
- **R12:** O9 sujeito ao teto de Vulnerability.
- **R18:** moveset das armas é de 08; este doc fica com o fator de DPS.
- **R21:** tiers T1 = 1–20, sem multiplicador nem interpolação.
- **R22 e R24:** multiplicadores de slot e Faixa Nomeada de 01; ↑ limitado à banda 95–115%.
- **R23:** pool por slot mantido aqui; exceções de item limitadas a 1 por item; Off Hand sem primários (L6 corrigido).
- **R25 a R29:** coluna de peça de set; modificadores remapeados para IDs de 03; Hero-bound não equipável; 30 Uniques com `fonteUnique`; Off Hand "Grimoire".
- **R30:** Armor implícito pela fórmula de 01; Status Potency no pool.
- **R48:** Codex → Grimoire; L2 "Grimoire of Closed Doors" → **Tome of Closed Doors**; L6 "Codex of Hollow Stars" → **Grimoire of Hollow Stars**; "STR%" → SRS; "DR" → DRD.

Esclarecimentos:

- *Hunter's Brand* (N1) é efeito do próprio item, não status de 07; o antigo nome "Mark" foi trocado para não colidir com Echo Mark (07).
- Frozen Armor (L10) é o status de 07, só referenciado aqui.
- O4 deixou de aumentar o máximo de acúmulos de Decay, porque 07 só admite as exceções declaradas em R49.

---

## 6. Questões abertas para playtest

1. Se o fator 0,9 de DPS em alvo único compensa a cobertura de área de Greatsword, Warhammer, Spellbook e Totem.
2. Se Eye of the Collapse (até 40% More com 8 alvos) domina salas com hordas densas.
3. Se o multiplicador ×1,5 de Main Hand torna a arma dominante demais na escolha de upgrade (alvo: arma ≈ 25–30% da decisão de poder).
4. Se jogadores entendem a regra Hero-bound sem tutorial (medir em testes de UI do Vault).
5. Se a Keystone of the Sundering (U19) vira item obrigatório nas Rotas B e C (04 §9, item 5).
6. Se os Uniques de Challenge (K15, L16, N15, O15) são desejáveis o bastante para justificar o risco das Challenge Rooms.

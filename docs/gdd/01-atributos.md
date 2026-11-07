# 01 — Sistema completo de atributos

**Documento:** GDD v0.3 · Etapa 1 da seção 18
**Dono de:** fórmulas de atributos, caps, diminishing returns, efeito dos primários, Armor/Resistências, tradução de recursos genéricos, **tabelas de faixas de rolagem por tier de iLvl, modelo de rolagem (Roll01), multiplicadores de slot e Faixa Nomeada** (00 §2, R21–R24).
**Revisão:** rodada de integração 1 — aplica 00 §5 (R02, R13, R21–R24, R29–R31, R44).
**Contrato:** `00-convencoes.md`

---

## 1. Objetivo e escopo

Definir de forma implementável:

- O que cada atributo faz, em que bucket do pipeline de dano entra e quais são seus limites.
- Como Strength, Dexterity e Intelligence afetam cada herói.
- Fórmulas de Armor, Physical Resistance, Elemental/Void Resistance e Armor Penetration.
- Soft caps, hard caps e diminishing returns.
- Tradução de `Maximum Resource`, `Resource Generation` e `Resource Cost Reduction` para Fury, Arcane Charges, Echo Energy e Void Constructs.
- Valores base dos heróis por nível (1–50).
- Faixas de rolagem por tier de iLvl, modelo `Roll01`, multiplicadores de slot, Faixa Nomeada e regra numérica de upgrade; pesos de rolagem derivados do pool de 02.
- Índices de comparação (Offense/Defense) para a UI de comparação de equipamentos (GDD §11.1).

Fora do escopo: dano base de armas e bases de item (02), **quais afixos podem rolar em cada slot (pool por slot: 02 §3.9.3)**, coeficientes das habilidades (08), valores dos inimigos (09), regras de geração de raridade/qualidade (05), Monster Level/eLvl por região × dificuldade (10).

---

## 2. Decisões tomadas

1. **Cada herói tem um primário Core e um primário Secundário.** Core concede o único "More" de atributo do jogo (*Core Attunement*); Secundário concede um bônus específico do herói. Todos os primários têm também efeitos universais — nenhum primário é inútil para nenhum herói (GDD §15.3).
2. **Core Attunement = +0,08% More damage por ponto do primário Core.** Fonte "More" nomeada e orçada (~+30–35% no fim da campanha), cobre a fatia "primários" do orçamento de poder.
3. **Faixas de rolagem não dependem da raridade**, apenas do tier de iLvl e do slot. Raridade = mais atributos e modificadores, não números maiores (GDD §15.7). Itens nomeados usam a **Faixa Nomeada** (piso 70%, teto 115% do máximo do tier — R24), com valores próprios definidos em 02.
4. **Armor usa curva dependente do nível do inimigo**: `Armor / (Armor + 50 × eLvl + 500)`, hard cap 75%. Previsível e exibível em tooltip, sem depender do tamanho do golpe.
5. **Resistências elementais são percentuais diretos** com cap 75%, agrupadas em duas famílias: **Elemental** (Ember, Frost, Storm) e **Void** (Void, Echo, Ruin). Reduz a quantidade de stats defensivos sem perder identidade.
6. **Penalidade de resistência por dificuldade é moderada** (até −25% em Eclipse) e os inimigos são balanceados assumindo ~30% de resistência efetiva: capar resistência é otimização, não requisito (GDD §1.2.6).
7. **Diminishing returns por soft cap linear** (eficiência 50% acima do soft cap) + hard cap. Fórmula única, fácil de mostrar na UI ("23% → 21,5% efetivo").
8. **Dodge Chance e Block só atuam em golpes não telegrafados.** Ataques telegrafados carregam a HitFlag `Unavoidable` (flag de DamageInstance, não tag — R02) e só são evitados por posicionamento ou pelas i-frames da esquiva; mecânicas **Annihilation** exigem sair da área (R13). Mantém o combate baseado em habilidade.
9. **Inimigos usam Armor % e resistência % diretas** (sem curva). Armor Penetration reduz o Armor % do inimigo multiplicativamente. Não existe "Elemental Penetration" como atributo; redução de resistência vem de status (Ruin/Vulnerability, ver 07).
10. **Health Regeneration só opera em estado de combate** (sala com inimigos ativos) e em Rest Rooms. Evita "esperar curar" entre salas e valoriza builds de sustain em luta.
11. **Recursos discretos (Arcane Charges, Void Constructs) convertem `Maximum Resource` por limiares**, e o resto do percentual nunca é desperdiçado: vira bônus de dano/vida específico.
12. **Ultimates recebem CDR com 50% de eficiência.** Impede que builds de CDR tornem a Ultimate uma habilidade rotacional.
13. **DoT não crita por padrão** (modificadores/Legendaries podem habilitar). Mantém Critical e DoT como eixos de build distintos.
14. **Relics não rolam primários.** Relics são o slot de "atributos de build" (duração, summon, área, DoT, utilidade), o que dá identidade ao slot.
15. **Modelo de rolagem único (R21):** tier fixo por faixa de iLvl (T1 = 1–20 … T5 = 81–100), valor rolado uniforme dentro da faixa do tier e salvo como `Roll01` normalizado; sem interpolação por iLvl dentro do tier. Rebalancear uma faixa propaga para itens existentes (11).
16. **Pesos de rolagem derivam do pool de 02 (R23):** 01 não mantém pool próprio; ✓ = 10, ★ = 20, — = 0. Evita duas fontes de verdade.
17. **Multiplicadores de slot numa tabela única (R22)**, incluindo Main Hand ×1,5 em dano%, Amulet ×1,5 em primários e resistências e Relic ×1,25 em atributos de build.
18. **Status Potency é atributo ofensivo (R30)**: +% no SP de buildup (07). Dá a Relics e builds de status um eixo próprio sem mexer no dano.
19. **Upgrade (R31): +3% por nível (+0..+10) sobre atributos rolados e implícito, igual para todas as raridades**; modificadores não sobem. O orçamento de poder e o exemplo de referência assumem itens **+6**.
20. **Itens são coletados automaticamente ao limpar a sala (05, R30).** Pickup Radius afeta só ouro, recursos e orbes durante o combate.

---

## 3. Corpo do design

### 3.1 Função de diminishing returns

Aplica-se aos atributos com soft cap (tabela 3.6):

```
Efetivo(Raw) = Raw                                   se Raw ≤ Soft
             = Soft + (Raw − Soft) × 0,5             se Raw > Soft
Final        = min(Efetivo, Hard)
```

`Raw` = soma de todas as fontes (base + primários + equipamento + passivas). Fontes temporárias (buffs de habilidade, Shrines) somam no `Raw`. Exceções que **ignoram** o cap são sempre nomeadas e exclusivas (ex.: um efeito Unique "Seus ataques sempre critam em inimigos congelados" não altera Critical Chance — é um efeito condicional separado).

### 3.2 Primários

#### 3.2.1 Efeitos universais (todos os heróis, por ponto)

| Primário | Efeito universal por ponto | Exemplo com 300 pontos |
|---|---|---|
| **Strength** | +1 Armor · +0,05% Maximum Health (increased) | +300 Armor · +15% Max Health |
| **Dexterity** | +0,015 pp Critical Chance · +0,02 pp Dodge Chance (Raw) | +4,5 pp Crit · +6 pp Dodge |
| **Intelligence** | +0,03 pp Elemental e Void Resistance · +0,05% Resource Generation | +9 pp em ambas · +15% Resource Gen |

#### 3.2.2 Core e Secundário por herói

| Herói | Core (Core Attunement) | Secundário (bônus de herói, por ponto) | Terciário |
|---|---|---|---|
| Kael | Strength | **Dexterity:** +0,04% Attack Speed (Raw) | Intelligence |
| Lyra | Intelligence | **Dexterity:** +0,04% Cast Speed (Raw) | Strength |
| Nyx | Dexterity | **Intelligence:** +0,05% Increased damage para tags `Clone` e `Trap` | Strength |
| Orun | Intelligence | **Strength:** +0,15% vida de `Summon`/`Construct` e +0,5 Armor para eles | Dexterity |

**Core Attunement:** `More% = PontosCore × 0,08%`. Exibido na ficha como fonte nomeada.

Distribuição resultante (relevante para loot, 05): Strength é Core de 1 herói e Secundário de 1; Dexterity é Core de 1 e Secundário de 2; Intelligence é Core de 2 e Secundário de 1.

#### 3.2.3 Primários por nível

| | Nível 1 | Por nível | Nível 50 |
|---|---:|---:|---:|
| Core | 20 | +3 | 167 |
| Secundário | 15 | +2 | 113 |
| Terciário | 10 | +1 | 59 |

### 3.3 Valores base dos heróis

| Atributo | Kael | Lyra | Nyx | Orun |
|---|---:|---:|---:|---:|
| Maximum Health (nv 1 / por nível / nv 50) | 240 / +22 / 1318 | 180 / +16 / 964 | 190 / +17 / 1023 | 230 / +20 / 1210 |
| Armor (nv 1 / por nível / nv 50) | 100 / +30 / 1570 | 30 / +12 / 618 | 40 / +16 / 824 | 80 / +24 / 1256 |
| Movement Speed (m/s) | 6,0 | 6,0 | 6,5 | 5,8 |
| Critical Chance | 5% | 5% | 7% | 5% |
| Critical Damage | +50% | +50% | +60% | +50% |
| Block Chance | 0% | 0% | 0% | 0% |
| Dodge Chance | 0% | 0% | 3% | 0% |
| Resistências | 0% | 0% | 0% | 0% |
| Pickup Radius | 2,0 m | 2,0 m | 2,0 m | 2,0 m |
| Recurso base | 100 Fury | 3 Charges | 100 Echo Energy | 3 Constructs |

Valores de "Por nível" são aplicados a partir do nível 2 (`Valor(nv) = Base + (nv − 1) × PorNível`). Movement Speed fora de combate (hub e salas já limpas): +30% automático, que **não** conta para o cap.

### 3.4 Mitigação

#### 3.4.1 Jogador recebendo dano

**Armor (dano Physical):**
```
K          = 50 × eLvl + 500                // eLvl = Monster Level do atacante, definido por região × dificuldade em 10 (R44); 1–100, podendo passar de 100 em Ascension
ArmorMit   = Armor / (Armor + K)            // hard cap 75%
```

| Armor | vs eLvl 1 (K 550) | vs eLvl 40 (K 2500) | vs eLvl 85 (K 4750) |
|---:|---:|---:|---:|
| 500 | 47,6% | 16,7% | 9,5% |
| 2500 | 75% (cap) | 50,0% | 34,5% |
| 5000 | 75% (cap) | 66,7% | 51,3% |
| 9000 | 75% (cap) | 75% (cap) | 65,5% |

**Physical Resistance:** percentual direto, hard cap 30%, multiplicativo com Armor:
```
MitPhysical = 1 − (1 − ArmorMit) × (1 − PhysRes)
```

**Elemental Resistance** (cobre Ember, Frost, Storm) e **Void Resistance** (cobre Void, Echo, Ruin):
```
ResEfetiva = min(ResRaw − PenalidadeDificuldade, CapRes)      // CapRes = 75%; pode subir até 85% só por efeitos nomeados
ResEfetiva pode ser negativa (mínimo −50%), aumentando o dano recebido.
```

| Dificuldade | Penalidade de resistência |
|---|---:|
| Normal | 0 |
| Veteran | −5 pp |
| Nightmare | −15 pp |
| Eclipse | −25 pp |
| Ascension N | −25 pp − 0,15 pp × N (−40 pp em Ascension 100) |

Linha de balanceamento para 09/10: dano elemental/void dos inimigos é calibrado assumindo **30% de resistência efetiva** do jogador na dificuldade atual.

**Block Chance:** golpe bloqueado sofre −40% de dano (Block Amount base; itens nomeados/Ashen Guard podem alterar). Só bloqueia golpes vindos do arco frontal de 180°. Não se aplica a hits com HitFlag `Unavoidable` nem a ticks. Hard cap 50%. (Valores adotados por 06 — R13.)

**Dodge Chance:** evita 100% do golpe. Não se aplica a hits com HitFlag `Unavoidable` (todo ataque com telegraph no chão), ticks de DoT e auras. Hard cap 40%.

**i-frames da esquiva (06):** evitam qualquer hit, inclusive `Unavoidable`; a única exceção são mecânicas **Annihilation**, que exigem sair da área (R13).

**Damage Reduction:** multiplicador final sobre todo dano recebido, após mitigação e Block. Hard cap 40%.

**Status Resistance (jogador):**
```
BuildupRecebido = SP_inimigo × (1 − StatusRes)
DuraçãoRecebida = DuraçãoBase × (1 − StatusRes)        // estados genéricos (06) e status de afinidade (07)
```
Aplicada antes da DR de CC de 06; não reduz Stagger do jogador nem knockback de chefe marcado.

**Ordem completa (dano recebido):**
```
Recebido = DanoInimigo
         × (1 − Mitigação)                      // MitPhysical OU resistência da família da afinidade
         × (Bloqueado ? 0,6 : 1)
         × (1 − DamageReduction)
         → absorvido primeiro por Shields/Barriers, depois Health
```
Dodge é rolado antes de tudo; se evitar, nada mais é calculado.

#### 3.4.2 Jogador causando dano (mitigação do alvo)

Inimigos possuem `Armor%` (0–60%) e `Res%` por afinidade (−50% a 75%), definidos por arquétipo em 09.

```
ArmorAlvoEfetivo = ArmorAlvo% × (1 − ArmorPenetration)          // Armor Penetration hard cap 60%
Mitigação        = ArmorAlvoEfetivo         (dano Physical)
                 = ResAlvo[afinidade]       (dano elemental/void; reduzível por status de 07)
```

**Piso de resistência do alvo:** reduções por status (Corruption e afins, 07) não levam `Res%` abaixo de **−20%**. Se a resistência base do arquétipo já for menor que −20% (fraqueza declarada em 09), reduções por status não a diminuem mais.

Isso é o termo `(1 − Mitigação do alvo)` do pipeline canônico (00-convencoes §3.2).

### 3.5 Atributos ofensivos — onde entram

| Atributo | Bucket / efeito | Aplica-se a |
|---|---|---|
| Attack Speed | `APS × (1 + AS)`; acelera animação | tag `Attack` (inclui `Basic`, `Heavy`) |
| Cast Speed | `TempoDeConjuração ÷ (1 + CS)`; acelera ticks de `Channel` | tag `Spell`, `Channel` |
| Critical Chance | chance de crit (pp) | hits; DoT não crita por padrão |
| Critical Damage | `Crit = 1 + CritDamage` | hits críticos |
| Physical Damage | Increased | dano do tipo Physical |
| Skill Damage | Increased | todas as habilidades do herói (exceto `Basic` e `Heavy`) |
| Ember/Frost/Storm/Void/Echo/Ruin Damage | Increased | dano do tipo daquela afinidade |
| Damage Over Time | Increased | tag `DoT` |
| Area Damage | Increased | tag `Area` (não altera tamanho — tamanho vem de modificadores, 03) |
| Projectile Damage | Increased | tag `Projectile` |
| Armor Penetration | reduz Armor% do alvo | dano Physical |
| Stagger Damage | `StaggerAplicado × (1 + SD)` | barra de Stagger (06) |
| Summon Damage | Increased | tags `Summon`, `Construct` |
| Status Potency | `SP × (1 + SPo)` no buildup (07 §3.2); não altera dano | golpes com afinidade |

Regra: só entram no Increased os atributos cujas tags/tipo batem com o hit. Um hit possui **um** tipo de dano; conversões (03) dividem o hit em partes, cada parte com seu tipo.

### 3.6 Caps e diminishing returns

| Atributo | Soft cap (1:1 até) | Acima do soft | Hard cap | Observação |
|---|---:|---:|---:|---|
| Attack Speed | +50% | 50% | +100% | legibilidade de animação |
| Cast Speed | +50% | 50% | +100% | |
| Movement Speed | +25% | 50% | +50% | bônus fora de combate não conta |
| Cooldown Reduction | 25% | 50% | 40% | Ultimates recebem 50% do CDR final |
| Dodge Chance | 20% | 50% | 40% | só golpes não telegrafados |
| Block Chance | 30% | 50% | 50% | arco frontal 180° |
| Damage Reduction | 20% | 50% | 40% | |
| Resource Cost Reduction | 25% | 50% | 50% | |
| Status Resistance | 30% | 50% | 60% | reduz duração de estados/status no jogador |
| Critical Chance | — | — | 75% | |
| Armor Penetration | — | — | 60% | |
| Physical Resistance | — | — | 30% | |
| Elemental / Void Resistance | — | — | 75% (máx. 85% por efeitos nomeados) | após penalidade |
| Pickup Radius | — | — | +200% | |
| Experience Gain | — | — | +100% | |
| Status Potency | — | — | +100% | |
| Critical Damage, Increased%, Max Health, Armor, Healing Effectiveness, Shield Strength, Duration, Summon Duration, Gold Acquisition, Resource Generation | — | — | sem cap | controlados pelas faixas de rolagem |

Piso de Movement Speed: o resultado final, após slows (06/07), nunca fica abaixo de 40% da velocidade base.

### 3.7 Atributos defensivos e utilitários — definição

| Atributo | Efeito |
|---|---|
| Maximum Health | flat; multiplicado depois pelo % de Strength e efeitos Increased de vida |
| Maximum Resource | % normalizado, traduzido por herói (3.8) |
| Armor | flat (rolado) + Armor implícito das bases (3.9.3) |
| Health Regeneration | HP/s flat; só em estado de combate e Rest Rooms |
| Healing Effectiveness | `Cura × (1 + HE)`; inclui regen, leech e curas de habilidade |
| Shield Strength | `Shield × (1 + SS)` para shields/barriers gerados pelo jogador |
| Status Resistance | reduz duração de estados genéricos (06) e status de afinidade (07) aplicados ao jogador; não afeta Stagger do jogador nem knockback de chefes marcados |
| Movement Speed | `BaseMS × (1 + MS)` |
| Cooldown Reduction | `CD × (1 − CDR)` |
| Pickup Radius | `2 m × (1 + PR)` para ouro, recursos e orbes durante o combate; equipamentos são coletados automaticamente ao limpar a sala (05) |
| Experience Gain | `XP × (1 + XG)` |
| Gold Acquisition | `Ouro × (1 + GA)` |
| Duration of Effects | duração de buffs, debuffs, status e zonas aplicados pelo jogador; não afeta summons |
| Summon Duration | duração de summons/constructs temporários; não afeta constructs permanentes de Orun |

### 3.8 Tradução dos atributos de recurso

Os atributos de recurso rolam sempre como **percentual normalizado** e cada herói os traduz:

| Atributo genérico | Kael — Fury (0–100) | Lyra — Arcane Charges (3) | Nyx — Echo Energy (0–100) | Orun — Void Constructs (3) |
|---|---|---|---|---|
| **Maximum Resource** (X%) | +X Fury máximo (cap 150) | +1 Charge a cada 25% (máx. 6 Charges = +75%). Resto: +0,4% Increased dano de habilidades que consomem Charges por 1% | +X Echo Energy máximo (cap 150) | +1 Construct ativo a cada 30% (máx. 6 = +90%). Resto: +0,5% vida e dano de Constructs por 1% |
| **Resource Generation** (X%) | Fury ganho × (1 + X) | Intervalo de regeneração de Charge ÷ (1 + X) e chance de +1 Charge extra em hit de gerador = X × 0,5 | Regeneração de Echo Energy × (1 + X) | Void Mass ganha × (1 + X) — a barra de Void Mass gera os Constructs (08, R01) |
| **Resource Cost Reduction** (X%, cap 50%) | Custo de Fury × (1 − X) | Chance X de não consumir a Charge | Custo de Echo Energy × (1 − X) | Chance X de o Construct sacrificado não ser consumido |

A UI de equipamentos mostra o atributo genérico **e** a tradução para o herói selecionado ("+12% Maximum Resource → +12 Fury").

**Teto de recursos discretos (R01):** `Maximum Resource` leva Charges e Constructs no máximo a **6**. Só Keystones, Augments e itens listados na tabela de exceções de 08 podem elevar até **8**; acima de 6 o excedente de `Maximum Resource` continua virando o bônus de "Resto". Outros docs (03, 04) citam esta tradução em vez de redefini-la.

### 3.9 Faixas de rolagem

#### 3.9.1 Tiers de iLvl

| Tier | iLvl |
|---|---|
| T1 | 1–20 |
| T2 | 21–40 |
| T3 | 41–60 |
| T4 | 61–80 |
| T5 | 81–100 |

Numeração canônica (R21): **T1 é o tier mais baixo e T5 o mais alto**. 02, 04, 05 e 11 usam esta numeração.

Regras:
- O tier é determinado pelo iLvl do item. O valor é `Valor = Mín + Roll01 × (Máx − Mín)` da faixa do tier, com `Roll01` uniforme em [0, 1]; **não há interpolação por iLvl dentro do tier**. O save guarda o `Roll01` (11).
- **Exceção registrada (R21/R31):** 05 pode elevar o piso do `Roll01` por dificuldade e pela marca Primal (A50+: `Roll01` em [0,85; 1]). O teto nunca passa de 1.
- As faixas são iguais para Common, Rare, Epic e Legendary.
- **Faixa Nomeada (R24):** itens nomeados de 02 (Legendary, peças de set, Unique) podem definir faixas próprias dentro de **[70%, 115%] do máximo do tier** equivalente. Ex.: Ember Damage T5 aleatório 20–30% → Faixa Nomeada válida entre 21% e 34,5%.
- Pool permitido por slot e limites por item (sem duplicata, máx. 1 primário — Amulet 2, nenhum em Ring/Off Hand/Relic —, máx. 2 afixos de afinidade, Movement Speed só em Feet): **02 §3.9.3 (R23)**.
- HP, Armor e Health Regeneration são sempre **flat** em todos os itens, inclusive peças de set e Uniques.

#### 3.9.2 Tabela de faixas (multiplicador de slot 1,0)

**Primários e flats**

| Atributo | T1 | T2 | T3 | T4 | T5 |
|---|---|---|---|---|---|
| Strength / Dexterity / Intelligence | 3–6 | 6–12 | 11–18 | 16–26 | 22–35 |
| Maximum Health | 15–30 | 35–60 | 60–100 | 95–150 | 140–210 |
| Armor (rolado) | 20–40 | 50–90 | 100–170 | 170–280 | 260–420 |
| Health Regeneration (HP/s) | 1–2 | 2–4 | 4–7 | 6–10 | 9–15 |

**Ofensivos (%)**

| Atributo | T1 | T2 | T3 | T4 | T5 |
|---|---|---|---|---|---|
| Attack Speed / Cast Speed | 2–3 | 3–4 | 4–6 | 5–7 | 6–9 |
| Critical Chance (pp) | 1–1,5 | 1,5–2,5 | 2–3,5 | 3–4,5 | 4–6 |
| Critical Damage | 5–10 | 10–16 | 15–22 | 20–30 | 28–40 |
| Physical / Ember / Frost / Storm / Void / Echo / Ruin Damage | 4–8 | 8–12 | 11–17 | 15–22 | 20–30 |
| Damage Over Time / Area / Projectile / Summon Damage | 4–8 | 8–12 | 11–17 | 15–22 | 20–30 |
| Skill Damage | 3–6 | 6–9 | 8–13 | 11–17 | 15–22 |
| Armor Penetration | 2–4 | 3–5 | 5–8 | 6–10 | 8–12 |
| Stagger Damage | 5–10 | 10–15 | 14–22 | 20–30 | 25–40 |

**Defensivos (%)**

| Atributo | T1 | T2 | T3 | T4 | T5 |
|---|---|---|---|---|---|
| Physical Resistance (pp) | 1–2 | 2–3 | 3–4 | 4–5 | 5–7 |
| Elemental / Void Resistance (pp) | 3–6 | 5–9 | 8–12 | 10–15 | 13–18 |
| Dodge Chance (pp) | 1–1,5 | 1,5–2 | 2–3 | 2,5–4 | 3–5 |
| Block Chance (pp) | 2–3 | 3–4 | 4–5 | 4–6 | 5–8 |
| Damage Reduction (pp) | 1–1,5 | 1,5–2 | 2–2,5 | 2,5–3,5 | 3–4,5 |
| Healing Effectiveness | 3–6 | 5–9 | 8–12 | 10–16 | 12–20 |
| Shield Strength | 4–8 | 6–11 | 10–15 | 13–20 | 16–25 |
| Status Resistance | 3–5 | 4–7 | 6–10 | 8–12 | 10–15 |
| Maximum Resource | 3–5 | 5–8 | 7–11 | 9–14 | 12–18 |

**Utilitários (%)**

| Atributo | T1 | T2 | T3 | T4 | T5 |
|---|---|---|---|---|---|
| Movement Speed | 2–3 | 3–4 | 4–5 | 5–6 | 6–8 |
| Cooldown Reduction | 1,5–2,5 | 2,5–3,5 | 3–5 | 4–6 | 5–7,5 |
| Resource Generation | 3–6 | 5–9 | 8–12 | 10–16 | 12–20 |
| Resource Cost Reduction | 2–3 | 2,5–4 | 3–5 | 4–6,5 | 5–8 |
| Pickup Radius | 10–15 | 15–20 | 20–30 | 25–40 | 30–50 |
| Experience Gain | 3–5 | 4–6 | 5–8 | 6–10 | 8–12 |
| Gold Acquisition | 5–10 | 8–15 | 12–20 | 15–28 | 20–35 |
| Duration of Effects | 3–6 | 5–9 | 8–12 | 10–16 | 12–20 |
| Summon Duration | 5–8 | 7–11 | 10–15 | 12–20 | 15–25 |
| Status Potency | 4–7 | 6–10 | 9–13 | 11–16 | 14–20 |
| Infusion "Added" (% do golpe como afinidade, ganho-como-extra) | 2–3 | 3–4 | 4–6 | 5–7 | 6–9 |
| Infusion "Strikes" (+SP de afinidade por golpe, valor flat) | 3–5 | 5–8 | 7–11 | 9–14 | 12–18 |

Infusions (pool e regras em 02 §3.9.4) contam como afixo de afinidade e recebem o ×1,25 de Relic; não recebem o ×1,5 de Main Hand.

#### 3.9.3 Multiplicadores de slot e Armor implícito

Tabela única (R22). 02 não mantém multiplicadores próprios.

| Slot | Flats defensivos (Max Health, Armor rolado, Health Regen) | Outros multiplicadores |
|---|---:|---|
| Main Hand | — (não rola flats defensivos) | **×1,5** em Physical, Skill, afinidades, Damage Over Time, Area, Projectile e Summon Damage (não afeta Attack/Cast Speed nem Critical) |
| Off Hand | 1,0 | Block Chance ×1,5 na Warbrand (Kael) |
| Chest | 1,4 | |
| Legs | 1,2 | |
| Head | 1,0 | |
| Hands | 0,8 | |
| Feet | 0,8 | Movement Speed ×1,5 |
| Amulet | 0,8 | **×1,5** em primários e em Physical/Elemental/Void Resistance |
| Rings | 0,6 | |
| Relics | 0,6 | **×1,25** em afinidades, Damage Over Time, Summon Damage, Summon Duration, Duration of Effects e Status Potency |

**Armor implícito** (não conta como atributo, existe em Head, Chest, Hands, Legs, Feet):
```
ArmorImplícito = 4 × iLvl × MultiplicadorDoSlot × FatorDeBase
```
`FatorDeBase` (família da armadura, 02): **Plate 1,5 · Leather 1,0 · Vestment 0,6**. 02 usa esta fórmula para a defesa base (R30). Leather e Vestment mantêm seus implícitos secundários (Dodge / Shield Strength) definidos em 02.

#### 3.9.4 Pesos de rolagem por slot

Os pesos são **derivados do pool de 02 §3.9.3** (R23): ✓ = **10**, ★ = **20**, — = **0** (não pode rolar). A probabilidade de um atributo é `peso / Σ pesos elegíveis`, depois dos filtros de herói/smart loot de 05 e das regras de 3.9.1. Restrições adicionais:

- Attack Speed só rola em armas com tag `Attack`; Cast Speed só em armas `Spell`; armas híbridas (02) rolam ambos com peso 10.
- Block Chance no Off Hand só na Warbrand (Kael). Afixos de Summon seguem a restrição de herói de 02.
- Status Potency: pool definido por 02 (recomendação: ★ em Relic, ✓ em Main Hand, Off Hand e Amulet).

A tabela de pesos própria que existia nesta seção foi removida na rodada de integração 1 (R23); a única fonte de verdade do pool é 02 §3.9.3.

#### 3.9.5 Upgrade (Blacksmith)

Regra numérica (R31; custos e desbloqueios em 10):

```
ValorAprimorado = ValorRolado × (1 + 0,03 × NívelUpgrade)       // NívelUpgrade 0..10, igual para todas as raridades
```

- Aplica-se a todos os atributos rolados e ao implícito/defesa base. **Modificadores de habilidade e efeitos especiais não sobem.**
- É a única forma, junto com efeitos nomeados, de um valor passar do máximo da faixa do tier (até +30%). Hard caps e soft caps (3.6) continuam valendo.
- O valor aprimorado permanece no mesmo bucket (Increased, flat etc.).
- **Referência de orçamento:** o cenário de fim de campanha (00 §4) assume itens **+6 (+18%)**; esse ganho conta na fatia "atributos de equipamento" (~25%).

### 3.10 Índices de comparação (UI — GDD §11.1)

Para a comparação de equipamentos mostrar "por que um item é melhor para esta build":

- **Offense Index:** DPS esperado da habilidade marcada como "principal" na tela de build (ou do ataque básico, se nenhuma), contra um alvo de referência da dificuldade atual (Armor 25%, Res 20%), usando o pipeline completo e `DPS = DanoMédio × Taxa`, com `DanoMédio = DanoNãoCrit × (1 + CritChance × CritDamage)`.
- **Defense Index (EHP):** `EHP = HP / (1 − MitMédia)`, com `MitMédia = 50% × MitPhysical + 35% × ResElemental + 15% × ResVoid`, combinada com Block, Dodge (×0,5 de peso, pois só cobre golpes não telegrafados) e Damage Reduction.
- A comparação mostra a variação percentual de ambos ao trocar o item. Efeitos especiais, modificadores e contribuição de Set Power aparecem como linhas qualitativas — não são convertidos em número.

### 3.11 Relações entre atributos (interações relevantes)

- **Attack Speed × Fury / Echo Energy:** recursos gerados por hit escalam naturalmente com AS; Resource Generation e AS só coexistem em Main Hand, Off Hand, Hands e Ring (pool de 02), o que limita o loop.
- **Cooldown Reduction × Duration of Effects:** juntos permitem uptime contínuo de buffs; o hard cap de CDR (40%) e a regra de Ultimates seguram o loop.
- **Critical Chance × DoT:** eixos separados por padrão (DoT não crita).
- **Strength × Kael/Orun:** Strength dá Armor e vida a todos, mas só Kael recebe More por ela; Orun a converte em vida de Constructs.
- **Intelligence × resistências:** heróis INT (Lyra, Orun) capam resistências com menos itens, compensando vida e Armor baixos.
- **Maximum Resource discreto:** limiares (25% Lyra, 30% Orun) são visíveis na UI com barra de progresso até a próxima Charge/Construct.

### 3.12 Exemplo numérico completo

**Kael, nível 50, Inferno Berserker, dificuldade Eclipse, equipamento iLvl ~85 (T5) com upgrade +6 (+18%, R31), contra eLvl 85.** Todos os valores "equip" abaixo já incluem o +18%.

Primários: base STR 167 / DEX 113 / INT 59 + equipamento STR +230 / DEX +60 → **STR 397 · DEX 173 · INT 59**. (Respeita o pool de 02: 1 primário por item em Main Hand e nas 5 armaduras; Amulet com 2 primários ×1,5; nenhum em Ring, Off Hand ou Relic.)

| Atributo | Cálculo | Resultado |
|---|---|---:|
| Maximum Health | (1318 base + 1150 equip) × (1 + 397 × 0,05%) = 2468 × 1,1985 | **2958** |
| Armor | 1570 base + 397 STR + 1768 implícito (4 × 85 × 5,2 × FatorBase 1,0) + 1100 rolado | **4835** |
| ArmorMit | 4835 / (4835 + 4750) | 50,4% |
| Physical Resistance | equip | 8% |
| MitPhysical | 1 − 0,496 × 0,92 | **54,4%** |
| Elemental Resistance | 52 equip + 59 × 0,03 − 25 (Eclipse) | **28,8%** |
| Void Resistance | 30 equip + 1,8 − 25 | **6,8%** |
| Critical Chance | 5 + 14 equip + 173 × 0,015 | **21,6%** |
| Critical Damage | 50 + 80 equip | **+130% (×2,3)** |
| Attack Speed | 18 equip + 173 × 0,04 (Kael DEX) | **+24,9%** |
| Dodge Chance | 173 × 0,02 + 4 equip | **7,5%** |
| Cooldown Reduction | equip | 12% (Ultimate: 6%) |
| Fury máxima | 100 + 15% Maximum Resource | **115** |
| Resource Generation | 20 equip + 59 × 0,05% | **+23%** |
| Core Attunement | 397 × 0,08% | **+31,8% More** |

**Golpe de Cleaving Strike** (tags `Attack, Melee, Area, Physical`, coeficiente **220% de Weapon Power**, 08). Habilidades escalam por **Weapon Power** (06 §Base), não pelo dano por golpe:

```
WeaponPower    = 9,2 × 1,0235^84 × 1,0 APS          ≈ 65     // Greatsword iLvl 85 (02 §3.4; arma de área, −10% vs. referência ~70)
Base           = 65 × 2,20                           = 143
Increased      = 45% Physical (Main Hand ×1,5)
               + 18% Skill + 22% Area                = +85%  → ×1,85
More           = Core Attunement                     = ×1,318
Alvo           = Armor 30% × (1 − 10% ArmorPen)      = 27%   → ×0,73
NãoCrit        = 143 × 1,85 × 1,318 × 0,73           ≈ 255
Crit           = 255 × 2,3                           ≈ 586
Médio          = 255 × (1 + 0,216 × 1,3)             ≈ 326
```

Sem Set Powers/Legendary (fatia ~20%), modificadores e Resonance; com eles, o golpe médio de referência fica em ~470–500. Os números absolutos dependem da escala de vida dos inimigos de 06/09/10; o que este exemplo fixa são as **proporções** entre fontes.

**Defesa:** EHP físico = 2958 / (1 − 0,544) ≈ **6487**. EHP elemental ≈ 4155. EHP void ≈ 3174 — o ponto fraco desta build, visível no Defense Index e corrigível com 2–3 rolagens de Void Resistance, sem obrigar troca de itens-chave.

---

## 4. Dependências e pontos de integração

| Documento | Recebe deste doc | Fornece a este doc |
|---|---|---|
| 02-equipamentos | tabelas de faixas por tier (T1 = 1–20 … T5 = 81–100), modelo `Roll01`, multiplicadores de slot (tabela única), Faixa Nomeada [70%, 115%], fórmula de Armor implícito com `FatorDeBase` Plate/Leather/Vestment, regra de upgrade | **pool de afixos por slot (fonte dos pesos)**, dano base das armas e Weapon Power, quais armas são `Attack`/`Spell`/híbridas, valores nomeados dentro da Faixa Nomeada, pool de Status Potency |
| 03-modificadores | buckets (Increased/More), regra de 1 tipo por hit, Area Damage não altera tamanho, tradução de recursos (3.8; Orun inclusive) | — (modificadores não geram More) |
| 04-conjuntos | caps (Set Powers não furam hard caps salvo efeito nomeado), Core Attunement como More já existente, faixas de 01 para peças de set (HP/Armor/Regen flat), limite de 1 afixo fora do pool (R23) | More% de cada Set Power |
| 05-loot | tiers e `Roll01`, distribuição Core/Secundário/Terciário dos primários (smart loot ×2/×1/×0,5), exceção de piso de `Roll01` por dificuldade e Primal, coleta automática | iLvl por região/dificuldade |
| 06-combate | mitigação do jogador e dos alvos (Block −40%, cap de Damage Reduction 40%, K do Armor `50·eLvl + 500`), Dodge/Block não atuam em `Unavoidable`, i-frames evitam (exceto Annihilation), DoT não crita, piso de Movement Speed, fórmula de Status Resistance | estados genéricos, Stagger, i-frames, HitFlags `Unavoidable`/Annihilation, Weapon Power, DR de CC |
| 07-resonance | Status Resistance (fórmula), Status Potency (atributo), famílias de resistência, piso de resistência do alvo −20%, ausência de penetração elemental como atributo | SP por golpe, redução de resistência via Corruption/Vulnerability |
| 08-herois | Core/Secundário/Terciário, valores base (incl. Movement Speed por herói), tradução de recursos e teto 6/8 | coeficientes de habilidades, Void Mass, tabela de exceções de recurso |
| 09-inimigos | eLvl na fórmula de Armor, Armor%/Res% de inimigos (piso −20% sob status), linha de 30% de resistência efetiva, velocidades base dos heróis | valores por arquétipo |
| 10-progressao | penalidade de resistência por dificuldade, regra numérica do upgrade, orçamento com itens +6 | Monster Level (eLvl) por região × dificuldade, custos e desbloqueios do upgrade |
| 11-arquitetura | `AttributeDefinition` (bucket, tags, soft/hard cap), tabelas de tier como dados, `Roll01` normalizado, upgrade como multiplicador da instância | HitFlags (`Unavoidable`) |

---

## 5. Conflitos com as convenções

Nenhum conflito em aberto. Observações:
- **Core Attunement** é uma fonte "More" declarada conforme 00 §3.2 (orçamento ~+30–35% no fim da campanha, dentro da fatia "Arma + nível + primários").
- O bônus de Secundário de Nyx (Increased para `Clone`/`Trap`) entra no bucket Increased; não é atributo de equipamento, mas respeita a mesma regra.
- **Resolvidos na rodada de integração 1:**
  - `Unavoidable` deixou de ser tratado como tag; é HitFlag, e i-frames o evitam, exceto Annihilation (R02, R13).
  - Numeração de tiers, modelo `Roll01` e fim da interpolação por iLvl (R21).
  - Multiplicadores de slot unificados, com Main Hand ×1,5, Amulet ×1,5 em resistências e Relic ×1,25 (R22). O Relic ×1,25 vem de 02 e foi mantido por dar identidade ao slot.
  - Pesos por slot derivados do pool de 02; tabela própria removida; regra de primários de 02 (R23).
  - Faixa Nomeada [70%, 115%] no lugar do limite de 130% (R24).
  - Off Hand sempre ativo, com nomes de 02 e sem "escudo/guarda" (R29).
  - Coleta automática, Status Potency e exemplo refeito com Weapon Power (R30).
  - Upgrade +3%/nível e orçamento com itens +6 (R31).
  - eLvl passa a ser definido por 10 (R44).
  - Teto de recursos 6, com exceções até 8 (R01).
  - Fórmula de Status Resistance e piso de resistência do alvo em −20%, pedidos por 07.

---

## 6. Questões abertas para playtest

1. Valor final de Core Attunement (0,08%/ponto) contra a fatia de 30% do orçamento — validar em DPS de referência no Training Grounds.
2. Block Amount de 40% e arco de 180° — validar se Unbreakable Juggernaut se sente forte sem trivializar elites.
3. Penalidade de resistência em Ascension (−0,15 pp/nível) — validar com a curva de dano de 10.
4. Relic ×1,25 em atributos de build e Main Hand ×1,5 em dano% — validar no simulador de DPS de 11 que a fatia "atributos de equipamento" fica perto de 25% com itens +6.

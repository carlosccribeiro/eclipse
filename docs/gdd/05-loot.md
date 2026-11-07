# PROJECT ECLIPSE — 05 · Sistema de Loot

**Dono de:** drop rates, fontes de loot, raridade por região/dificuldade, Item Level, smart loot, pity, target farming, qualidade de rolagem, filtros e salvage.
**Referencia:** 00-convencoes (inclui o registro de resoluções §5), 01-atributos (tiers de iLvl, tabelas de faixas, Roll01, Faixa Nomeada), 02-equipamentos (catálogo, pool de afixos por slot, Uniques), 03-modificadores (pool de modificadores, `GEN-xx`, Signature), 04-conjuntos (sets), 09-inimigos (estrutura da run e salas), 10-progressao (moedas, regra de morte, Ascension, Attunement, serviços do Sanctuary), 11-arquitetura (dados, RNG, save).

---

## 1. Objetivo e escopo

Definir **de onde** vem cada item, **com que frequência**, **com que raridade**, **com que nível**, **com que qualidade de rolagem** e **como o jogo protege o jogador contra azar**, de modo que:

- todo item Epic+ seja um momento de decisão (pilar "Loot significativo");
- a raridade venha da **dificuldade e do risco assumido**, nunca de um atributo de equipamento;
- completar 3 peças de um set seja alcançável em uma sessão e 6 peças em poucas sessões, sem encerrar a busca por versões melhores (GDD §6.4);
- farm direcionado seja possível e explicável ao jogador.

Fora de escopo: valores de moedas e custos de serviços (10-progressao), nomes e afixos de itens específicos (02-equipamentos), modificadores específicos (03-modificadores).

---

## 2. Decisões tomadas

1. **Não existe Magic Find.** Nenhum atributo aumenta raridade de drops. Raridade é função de fonte × dificuldade × pity. *Evita o dilema "equipar para achar loot vs. equipar para lutar".*
2. **Loot de baixa contagem e alta relevância:** ~30 itens por run de 30 min no Normal, ~37 no Eclipse, ~45 em Ascension 100. *Roguelite com salas; cada drop precisa ser lido.*
3. **Raridade resolvida de cima para baixo** (Legendary → Epic → Rare → Common) com **pisos por fonte** (elites nunca dão Common, Challenge Room nunca dá abaixo de Epic).
4. **iLvl progressivo por região até Nightmare; plano a partir de Eclipse.** *Garante que no endgame qualquer região seja viável para farm direcionado.*
5. **Requisito de equipar = ceil(iLvl × 0,5)**, limitado a 1–50 (nível de herói).
6. **Qualidade de rolagem visível**: cada afixo mostra % de qualidade (Roll01 dentro da faixa do tier, 01); o item mostra a média. Dificuldades altas elevam o **piso** do Roll01, não o teto (exceção registrada em 01, R21).
7. **Primal** é uma **marca de qualidade**, não raridade: Legendaries Primal (só a partir de **Ascension 50**) têm todas as rolagens entre 85% e 100%, nunca acima do máximo. Absorve o antigo "Ascended" de 10 (R31). *Perseguição de endgame sem criar quinta raridade.*
8. **Estrutura de Legendary por tipo (R25):** Legendary nomeado = 4 atributos fixos + 1 **Afixo Aberto** (3 opções da definição) + 2 modificadores fixos + 1 efeito; peça de set = 4 fixos + 1 Aberto + 2 modificadores, sem efeito próprio; Unique = 5 fixos + 2 modificadores (até 1 Signature) + efeito. *Mantém identidade fixa (GDD §6.4) com variação controlada.*
9. **Smart loot:** 75% dos itens não-Legendary e 85% dos Legendary/Set/Unique são compatíveis com o herói em uso; o restante alimenta o Vault para outros heróis. Itens com modificador de habilidade de herói são **hero-bound** (R27).
10. **Pity em três camadas:** Legendary (soft + hard por contagem de itens), Set (garantia após 4 Legendaries sem peça de set, a partir de Veteran) e proteção contra duplicatas de peças de set.
11. **Target farming por "região-lar":** cada set tem uma região-lar (peso ×4 entre os sets do herói) e o chefe dessa região tem peso ×6. Cada herói tem seus 3 sets em 3 regiões diferentes; distribuição 2/2/3/2/3 por região (R32). No Eclipse+, **Attunement** (10) multiplica o peso do alvo escolhido por ×3 (R39).
12. **Uniques têm exatamente uma fonte:** pool de região, exclusivo de chefe, exclusivo de Challenge Room ou Pinnacle. Catálogo de lançamento: **30 Uniques** (R28). Uniques e peças de set **não caem em Normal**.
15. **Item Level não escala valores continuamente:** usa os 5 tiers de 01 (T1 = iLvl 1–20 … T5 = 81–100) com tabela fixa por tier (R21).
16. **Quantidade:** bônus de quantidade (dificuldade, Peril, Overrift, modificadores de incursão) são **aditivos**, com teto ×2,0 no total (R45).
13. **Equipamentos são coletados automaticamente ao limpar a sala** (com filtro aplicado). Pickup Radius afeta apenas moedas e orbes. *Remove fricção sem remover o momento de revelação (resumo da sala).*
14. **Sala de tesouro tem escolha:** 2 itens entregues + Reliquary (escolher 1 de 3). *Converte loot em decisão, alinhado ao pilar de escolhas de caminho.*

---

## 3. Fontes de loot

### 3.1 Tabela de fontes

| Fonte | Itens por ocorrência | Piso de raridade | Afetado por Quantidade? | Bônus de iLvl |
|---|---|---|---|---|
| Inimigo comum (trash) | 3% de chance de 1 item | — | Sim | +0 |
| Elite | 1 item garantido + 30% de chance de mais 1 | Rare | Sim (só a chance extra e o garantido arredondado estocasticamente) | +1 |
| Guardian (linha 9, 09) | 2 itens (1 garantido Epic+) | Rare | Não | +2 |
| Recompensa de Combat/Elite Arena (tipo "Equipment") | 1 item | Rare | Não | +1 |
| Treasure Room — itens entregues | 2 itens | Rare | Não | +1 |
| Treasure Room — Reliquary | escolher 1 de 3 | Epic | Não | +1 |
| Challenge Room (concluída) | 2 itens | Epic | Não | +2 |
| Event Room | 0–2 itens (definido pelo evento, média 0,7) | Rare | Não | +1 |
| Chefe | 4 itens (1 garantido Epic+) | Rare | Sim | topo da faixa +2 |
| Rift Cache (fim de região, após o chefe) | 2 itens (+1 se todas as Challenge Rooms da run foram concluídas) | Rare | Não | topo da faixa +2 |

Notas:
- Shrine Room e Rest Room (incluindo a Antecâmara) **não** dão equipamento (dão Blessings/recuperação — 09/10).
- O chefe também entrega 1 **Heart of [Chefe]** (material de chefe, quantidades em 10) e moedas; Rift Shards (moeda da run) são definidos por 10.
- O tipo de recompensa de cada Arena é mostrado na porta antes da escolha de caminho (estrutura de nós: 09-inimigos). Este doc assume ~3 de 6 Combat Arenas com recompensa "Equipment" por run.
- Recompensas de Event Room podem ter pisos próprios definidos pelo evento (ex.: "pacto" que entrega Legendary com custo).

### 3.2 Quantidade

```
Quantidade = 1 + BônusDificuldade + Σ BônusExternos      (todos aditivos; teto 2,0)
```

| Normal | Veteran | Nightmare | Eclipse | Ascension N |
|---:|---:|---:|---:|---:|
| +0 | +0,1 | +0,2 | +0,3 | +(0,3 + 0,004 × N) → A100 = +0,7 |

- **BônusExternos:** Peril e Distortions de Rift Sigil, salas de Overrift, modificadores de incursão, Blessings (10/09). Todos somam no mesmo termo.
- **Teto absoluto:** Quantidade ≤ ×2,0 (R45). Ex.: em A100 restam +0,3 para bônus externos.
- Aplica-se a: chance de drop de trash, chance extra de elite, número de itens do chefe. Não se aplica a baús fixos (Treasure, Reliquary, Challenge, Guardian, Rift Cache, recompensas de sala).
- Chance de Legendary também tem teto: ≤ ×2,0 da base da fonte (pity incluso).

---

## 4. Item Level (iLvl)

### 4.1 Faixas por dificuldade e região

| Dificuldade | Ashen Wastes | Frozen Cathedral | Shattered Expanse | Hollow Depths | Astral Citadel |
|---|---|---|---|---|---|
| Normal (1–24) | 1–6 | 6–10 | 11–15 | 16–20 | 21–24 |
| Veteran (25–49) | 25–30 | 30–35 | 35–40 | 40–45 | 45–49 |
| Nightmare (50–69) | 50–54 | 54–58 | 58–62 | 62–66 | 66–69 |
| Eclipse | 75–84 (todas as regiões) |||||
| Ascension N | iLvl base = 85 + floor((min(N, 90) − 1) × 15 / 89) → A1 = 85, **A90 = 100**, A90–A100 = 100 (todas as regiões) |||||

Faixas de iLvl por dificuldade são decisão deste doc (R38); 10-progressao as cita. Tier de 01 correspondente: Normal T1–T2, Veteran T2–T3, Nightmare T3–T4, Eclipse T4–T5, Ascension T5.

### 4.2 Cálculo do iLvl do drop

```
iLvl_sala  = Faixa.min + floor((Faixa.max − Faixa.min) × índiceDaSala / totalDeSalas)
iLvl_drop  = clamp(iLvl_sala + BônusDaFonte, 1, min(100, Faixa.max + 2))
Chefe / Rift Cache: iLvl_drop = min(100, Faixa.max + 2)
Ascension:  iLvl_drop = iLvl base de Ascension + BônusDaFonte (cap 100)
```

### 4.3 Requisito de nível

`NívelRequerido = clamp(ceil(iLvl × 0,5), 1, 50)`

Nível de herói esperado (alvo de validação; dono da curva é 10: XP = 200·L^1,8, nível 50 em ~40–50 h — R40): fim do Normal ~15, fim do Veteran ~30, fim do Nightmare ~40, entrada no Eclipse ~44, nível 50 durante o Eclipse. Em todos os pontos o nível esperado fica ≥ ao requisito do maior iLvl da dificuldade (24→12, 49→25, 69→35, 84→42).

### 4.4 Efeito do iLvl nos valores

O iLvl define o **tier** do item (01): T1 = 1–20, T2 = 21–40, T3 = 41–60, T4 = 61–80, T5 = 81–100. Cada afixo usa a **tabela fixa do tier** definida em 01 (com multiplicadores de slot de 01); não há curva contínua. Itens nomeados (Legendary, peça de set, Unique) usam a **Faixa Nomeada** de 01 (piso 70% e teto 115% do máximo do tier). O tooltip mostra a faixa do tier do item.

Valor de um afixo: `valor = Lerp(faixa.min, faixa.max, Roll01)`, com `Roll01` gerado em §10.1 e salvo normalizado (11), de modo que rebalancear uma faixa propaga para itens existentes.

---

## 5. Raridade

### 5.1 Resolução

Para cada item gerado:
1. Rolar Legendary com a chance `L` da fonte × dificuldade (× pity, × modificadores de incursão, respeitando cap).
2. Se falhar, rolar Epic com chance `E`.
3. Se falhar, rolar Rare com chance `R` (apenas trash; demais fontes têm piso Rare).
4. Senão, Common.
5. Aplicar piso da fonte (eleva à raridade mínima).

Ascension: interpolação linear entre A1 e A100.

### 5.2 Chance de Legendary por item (L, %)

| Fonte | Normal | Veteran | Nightmare | Eclipse | A1 | A100 |
|---|---:|---:|---:|---:|---:|---:|
| Trash | 0,1 | 0,4 | 0,9 | 1,8 | 2,5 | 4,0 |
| Elite | 0,5 | 1,5 | 3 | 6 | 8 | 12 |
| Recompensa de Arena | 0,5 | 1,5 | 3 | 6 | 8 | 12 |
| Treasure (entregues) | 1 | 3 | 5 | 9 | 12 | 16 |
| Reliquary (cada opção) | 2 | 5 | 8 | 12 | 15 | 20 |
| Challenge Room | 5 | 10 | 15 | 22 | 26 | 32 |
| Event Room | 1 | 3 | 5 | 9 | 12 | 16 |
| Guardian (cada item) | 2 | 4 | 7 | 11 | 14 | 18 |
| Chefe (cada item) | 4 | 8 | 13 | 20 | 24 | 30 |
| Rift Cache | 2 | 5 | 9 | 14 | 17 | 22 |

**iLvl mínimo de Legendary:** Legendaries só são gerados com iLvl ≥ 10 (minDropILvl de 02); abaixo disso a rolagem de Legendary vira Epic. Exceção: o Legendary garantido de primeiro abate (§8.4) sai com `iLvl = max(iLvl_drop, 10)`.

### 5.3 Chance de Epic por item (E, %, aplicada se não for Legendary)

| Fonte | Normal | Veteran | Nightmare | Eclipse | A1 | A100 |
|---|---:|---:|---:|---:|---:|---:|
| Trash | 2 | 6 | 12 | 20 | 25 | 32 |
| Elite | 8 | 15 | 22 | 30 | 34 | 40 |
| Recompensa de Arena | 8 | 15 | 22 | 30 | 34 | 40 |
| Treasure (entregues) | 12 | 20 | 28 | 36 | 40 | 45 |
| Reliquary | piso Epic | | | | | |
| Challenge Room | piso Epic | | | | | |
| Event Room | 10 | 18 | 25 | 33 | 37 | 42 |
| Guardian (item não garantido) | 12 | 20 | 28 | 36 | 40 | 45 |
| Chefe (itens não garantidos) | 15 | 22 | 30 | 38 | 42 | 48 |
| Rift Cache | 20 | 28 | 35 | 42 | 45 | 50 |

### 5.4 Chance de Rare para trash (R, %)

| Normal | Veteran | Nightmare | Eclipse | A1 | A100 |
|---:|---:|---:|---:|---:|---:|
| 25 | 32 | 38 | 42 | 42 | 40 |

Distribuição resultante do trash (C/R/E/L): Normal ≈ 72,9 / 25 / 2 / 0,1 — Eclipse ≈ 36,2 / 42 / 20 / 1,8 — A100 ≈ 24 / 40 / 32 / 4.

### 5.5 Tipo de Legendary

Quando um item resolve como Legendary, sorteia-se o tipo:

| Dificuldade | Peça de Set | Legendary padrão | Unique |
|---|---:|---:|---:|
| Normal | 0% | 100% | 0% |
| Veteran – Eclipse | 40% | 45% | 15% |
| Ascension | 40% | 42% | 18% |

- **Normal não gera peças de set nem Uniques** (R28; 04 libera sets a partir de Veteran).
- **Uniques exigem iLvl ≥ 35** (minDropILvl de 02): em Veteran, regiões com iLvl de drop < 35 redistribuem a fatia de Unique para Legendary padrão.
- Pity de Set (§8.2) e fontes exclusivas (§9.3) podem forçar o tipo.

---

## 6. Algoritmo de geração (passo a passo)

```
GerarItem(fonte, contexto):
 1. iLvl        ← §4.2
 2. raridade    ← §5.1 (inclui pity)
 3. tipoLeg     ← §5.5 se Legendary (respeitando minDropILvl de Legendary 10 e Unique 35)
 4. pool        ← §7 smart loot: herói atual (75% / 85%) ou qualquer herói
 5. base        ← escolher definição:
      Common/Rare/Epic: escolher SLOT por peso (§6.1) → escolher BASE do slot compatível com o pool (02)
      Set:     escolher SET (pesos de região-lar §9.1 × Attunement §9.4 + proteção de duplicata §8.3) → escolher PEÇA
      Padrão:  escolher Legendary nomeado (peso ×2 se a afinidade do item = afinidade da região, §9.2)
      Unique:  escolher entre Uniques da fonte (§9.3), peso ×2 para os ainda não descobertos, ×3 se for alvo de Attunement
 6. tier        ← tier de 01 a partir do iLvl (§4.4)
 7. afixos (pool por slot e pesos: 02):
      Common/Rare/Epic: N afixos (2/3/4) do pool do slot, ponderados (§6.2), sem repetição
      Legendary nomeado:  4 afixos fixos + 1 Afixo Aberto (sorteado entre as 3 opções da definição)
      Peça de set:        4 afixos fixos + 1 Afixo Aberto (3 opções); sem efeito próprio (04)
      Unique:             5 afixos fixos
 8. valores     ← para cada afixo: Roll01 = Rolagem(dificuldade) (§10)
                  valor = Lerp(faixa.min, faixa.max, Roll01)
                  faixa = tabela do tier (01) para Common/Rare/Epic; Faixa Nomeada do tier (01) para Legendary/Set/Unique
 9. modificadores (03):
      Epic: 1 modificador — 75% do pool do herói-alvo (ponderado por skill equipada, §7.2), 25% do pool universal GEN-xx
      Legendary/Set/Unique: 2 modificadores fixos da definição (Unique pode ter 1 Signature); valores rolados com Roll01 (§10)
10. hero-bound  ← item que recebeu modificador de habilidade de herói só pode ser equipado por esse herói (R27)
11. marca Primal (§10.3) se Ascension ≥ 50 e Legendary
12. registrar: id único, seed, iLvl, tier, Roll01 de cada afixo/modificador, fonte (para o Archivist, telemetria e save — 11)
```

### 6.1 Peso de slot (Common/Rare/Epic)

| Main Hand | Off Hand | Head | Chest | Hands | Legs | Feet | Ring | Amulet | Relic |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 7 | 8 | 8 | 8 | 8 | 8 | 9 | 6 | 7 |

Ring e Relic são um único sorteio cada (o item serve em qualquer um dos 2 slots). **Off Hand é sempre sorteável** (R29): toda arma ocupa só a Main Hand e o Off Hand é uma categoria própria de cada herói (Warbrand, Grimoire, Quiver, Idol — 02).

### 6.2 Regras de afixos aleatórios

- Pool permitido por slot, peso base e limites de primário (máx. 1 por item, Amulet 2, nenhum em Ring): 02-equipamentos (R23).
- **Peso de relevância (smart loot):** afixo cujas tags não aparecem em nenhuma habilidade do herói-alvo → ×0,25 (ex.: Summon Damage para Kael). Primários conforme o papel no herói-alvo (01): **Core ×2, Secundário ×1, Terciário ×0,5** (R30).
- **Grupos de exclusão:**
  - nenhum afixo repetido;
  - máximo 2 afixos de dano de afinidade (Ember/Frost/Storm/Void/Echo/Ruin Damage) por item;
  - máximo 1 afixo "de conforto" por item: {Pickup Radius, Experience Gain, Gold Acquisition};
  - máximo 1 resistência de categoria única (Physical/Elemental/Void Resistance) em Common/Rare.

---

## 7. Smart loot

### 7.1 Escolha do herói-alvo

| Raridade | Compatível com herói atual | Qualquer herói (uniforme entre os 4) |
|---|---:|---:|
| Common / Rare / Epic | 75% | 25% |
| Legendary / Set / Unique | 85% | 15% |

"Compatível" = a definição do item permite o herói (restrições por item, 02) e, se o item for hero-bound, o herói é o dono. Itens de outros heróis vão para o Vault ao fim da run (10/11), alimentando alts.

### 7.2 Modificadores de Epic

- **75%:** modificador do pool do herói-alvo (03), com peso ×3 se a habilidade alvo está equipada no momento e ×1 caso contrário. O item fica **hero-bound** ao herói-alvo.
- **25%:** modificador universal `GEN-xx` (03), sorteado entre os compatíveis com as tags de alguma habilidade do herói-alvo. O item não fica hero-bound.

---

## 8. Pity e proteção contra azar

Todos os contadores são **por herói** e persistidos no save (11).

### 8.1 Pity de Legendary

Contador `seca` = número de itens recebidos (qualquer fonte) desde o último Legendary.

| Dificuldade | Início do soft pity (S) | Hard pity (H) |
|---|---:|---:|
| Normal | 80 | 160 |
| Veteran | 50 | 100 |
| Nightmare | 30 | 60 |
| Eclipse | 15 | 35 |
| Ascension | 12 | 30 |

- Soft: chance de Legendary × (1 + 0,05 × (seca − S)) para seca > S (ainda sujeita ao cap ×2).
- Hard: ao atingir H, o próximo item gerado por Elite, Chefe, Treasure, Challenge, Event ou Rift Cache é Legendary garantido (trash não consome o hard pity).
- Reset ao receber qualquer Legendary compatível com o herói atual.
- Ao trocar de dificuldade, o contador é mantido e comparado com os limiares da nova dificuldade.

### 8.2 Pity de Set (Veteran+)

Após **4 Legendaries consecutivos compatíveis sem peça de set**, o próximo Legendary compatível é peça de set de um dos 3 sets do herói.

### 8.3 Proteção contra duplicatas de set

Ao sortear a peça de um set, peças que o herói **ainda não possui** (em qualquer qualidade, equipadas, no inventário ou no Vault) têm peso ×3.

### 8.4 Garantias de primeira vez

- **Primeiro abate de cada chefe por dificuldade (por herói):** 1 Legendary compatível garantido, além dos itens normais. Normal → Legendary padrão (iLvl mínimo 10); Veteran+ → 50% peça do set-lar daquela região se compatível com o herói, senão Legendary padrão.
- **Primeira conclusão de região no Normal:** Rift Cache com 1 Epic garantido.

### 8.5 Uniques exclusivos de chefe

Cada chefe tem **3 Uniques exclusivos** (09, catálogo em 02). Disponíveis a partir de Nightmare. Chance por abate de dropar um deles: Nightmare 8%, Eclipse 12%, Ascension 15% → 25% (A1 → A100); entre os 3, peso ×3 para os que o herói ainda não possui. **Garantido no 10º abate** do mesmo chefe sem obter nenhum (contador por chefe, por herói). Boss Echoes (09/10) usam a mesma tabela com chance ×1,5. Rota determinística: troca de Heart of [Chefe] pelo Unique desejado daquele chefe (custo em 10).

---

## 9. Target farming

### 9.1 Região-lar dos 12 sets

| Região | Sets-lar | Herói |
|---|---|---|
| The Ashen Wastes | Ashen Sovereign | Kael |
| | Death's Pursuit | Nyx |
| The Frozen Cathedral | Frozen Empress | Lyra |
| | Eternal Bastion | Kael |
| The Shattered Expanse | Storm Phantom | Nyx |
| | Abyssal Legion | Orun |
| | Ruinbreaker | Kael |
| The Hollow Depths | Void Oracle | Lyra |
| | Hollow Sovereign | Orun |
| The Astral Citadel | Arcane Paradox | Lyra |
| | Thousand Echoes | Nyx |
| | Eternal Warden | Orun |

Distribuição 2/2/3/2/3 (R32). Verificação: cada herói tem seus 3 sets em 3 regiões diferentes (Kael: Ashen/Frozen/Shattered; Lyra: Frozen/Hollow/Astral; Nyx: Ashen/Shattered/Astral; Orun: Shattered/Hollow/Astral).

Justificativas temáticas: Storm Phantom na região Storm (Shattered); Void Oracle na região Void (Hollow); Thousand Echoes na Astral Citadel (repetição/tempo ligados aos Pilares); Death's Pursuit nas Ashen Wastes (caça em terra devastada, região inicial com inimigos frágeis para execução); Ruinbreaker na Shattered Expanse (golpes pesados que rompem a realidade fraturada).

Pesos ao sortear qual set (entre os 3 do herói-alvo):
- set-lar da região atual: ×4;
- itens do chefe da região-lar: ×6;
- demais: ×1;
- **Attunement** (Eclipse+, §9.4): peso do set escolhido × 3, cumulativo com os pesos acima.

Sets dropam em qualquer região; a região-lar apenas concentra.

### 9.2 Afinidade de região (Legendaries padrão)

| Região | Afinidade dominante (peso ×2) |
|---|---|
| The Ashen Wastes | Ember |
| The Frozen Cathedral | Frost |
| The Shattered Expanse | Storm, Echo |
| The Hollow Depths | Void, Ruin |
| The Astral Citadel | nenhuma (pool uniforme; +3 p.p. no % de Unique do §5.5) |

Ex.: Ashfang (Legendary Greatsword Ember do GDD §6.4) tem peso ×2 em The Ashen Wastes.

### 9.3 Fontes de Unique

Cada Unique (02-equipamentos) declara **exatamente uma** fonte:

| Fonte | Regra | Quota do catálogo de lançamento (R28) |
|---|---|---|
| Exclusivo de chefe | Só dropa do chefe indicado (§8.5), Nightmare+; também Boss Echoes e troca por Heart of [Chefe] | 3 por chefe × 5 → **15** |
| Pinnacle | Só no The Sundering Heart (10) | **4** |
| Pool de região | Dropa de qualquer fonte daquela região (iLvl ≥ 35) | **7** (pelo menos 1 por região; inclui Keystone of the Sundering na Astral Citadel) |
| Exclusivo de Challenge | Só em Challenge Rooms, Eclipse+ | **4** |
| **Total** | | **30** |

O catálogo nominal (nomes, efeitos, campo `fonteUnique`) é de 02-equipamentos; este doc fixa as regras e a quota. Cada região tem ao menos 4 Uniques-alvo (3 do chefe + ≥1 do pool).

### 9.4 Attunement (Eclipse+)

Sistema único de target farming (R39). Objeto e custo: **Rift Sigil + 2 Pillar Fragments** (10). Ao ativar antes da run, o jogador escolhe **um set do herói ou um Unique**; durante a run, o alvo recebe **peso ×3** no sorteio (§6 passo 5), cumulativo com região-lar. Só disponível a partir de Eclipse. O antigo consumível "Sigil of Attunement" não existe.

---

## 10. Qualidade de rolagem

### 10.1 Rolagem

```
r = max(PisoDaDificuldade, U(0,1))
```

| Normal | Veteran | Nightmare | Eclipse | Ascension N |
|---:|---:|---:|---:|---:|
| 0 | 0 | 0,10 | 0,20 | 0,25 + 0,004 × N (A100 = 0,65) |

Implementação: o piso é aplicado reamostrando `U(piso, 1)` (distribuição uniforme no intervalo restante), não por clamp — evita acúmulo de massa no piso.

### 10.2 Exibição

- Cada afixo exibe valor, faixa do tier `[min–max]` (ao segurar Alt; padrão configurável) e **qualidade** = round(Roll01 × 100)%.
- Item exibe **Qualidade média** (média simples dos afixos e dos valores variáveis dos modificadores).
- **★ (estrela)** em afixos com r ≥ 0,95; rótulo **Perfect** em r ≥ 0,99.
- Comparação de equipamentos (UI) mostra qualidade e iLvl lado a lado.

### 10.3 Marca Primal

- Só a partir de **Ascension 50**, só em Legendary (padrão, Set ou Unique). Substitui o "Ascended" de 10 (R31).
- Chance: 2% + 0,16% × (N − 50) do Legendary dropado (A50 = 2%, A100 = 10%).
- Todas as rolagens do item (afixos e valores de modificadores) em `U(0,85, 1)`; **nunca acima do máximo** da faixa.
- Upgrade do Blacksmith (+3% por nível, 10) se aplica normalmente; não há teto extra para Primal.
- Visual: borda e feixe de drop exclusivos.
- Ganho esperado ≈ +8–10% de poder do item sobre a média de Eclipse; **nunca obrigatório** (GDD §15.8).

---

## 11. Densidade de loot esperada

Premissa de run (estrutura de 09, R33): **13 linhas + chefe** (Estratos 1–5 / 6–9 / 10–13) — 6 Combat Arenas, 1 Elite Arena, 1 Guardian (linha 9), 1 Treasure, 1 Event, 1 Shrine ou Challenge (Challenge aceita em ~60% das runs), 1 Rest (linha 5), Antecâmara (linha 13); ~230 inimigos comuns e ~6 elites (incluindo elites em Arenas). Valores abaixo arredondados; a troca de 1 Elite Arena por Guardian mantém o total de Legendary por run.

### 11.1 Itens por run de 30 min

| Dificuldade | Total | Common | Rare | Epic | Legendary |
|---|---:|---:|---:|---:|---:|
| Normal | ~30 | ~5,5 | ~19 | ~5,4 | ~0,4 (+ garantias de 1º abate) |
| Veteran | ~32 | ~5 | ~18 | ~8 | ~1,0 |
| Nightmare | ~34 | ~4,5 | ~18 | ~10 | ~1,8 |
| Eclipse | ~37 | ~3,5 | ~18 | ~12,4 | ~3,2 |
| Ascension 100 | ~45 | ~3 | ~19 | ~16,5 | ~6,5 |

Contribuição de Legendary no Eclipse: chefe ~1,04 · elites ~0,6 · Guardian ~0,22 · Reliquary ~0,32 · Rift Cache ~0,28 · Challenge ~0,26 · Arena ~0,15 · Treasure ~0,18 · trash ~0,16 · Event ~0,06.

### 11.2 Ritmo de set (herói focado, região-lar, Eclipse)

- Peças de set por run: 3,2 Legendary × 40% ≈ 1,3; compatíveis com o herói (85%) ≈ 1,1; do set-lar (×4 entre 3 sets ≈ 67%) ≈ **0,74 peça do set-alvo por run**.
- Com proteção de duplicata ×3, completar 6 peças distintas requer ~8,9 peças sorteadas → **~12 runs (~6 h)**; 3 peças em ~3,2 peças → **~4–5 runs (~2 h)**.
- Com **Attunement** (×3 sobre o set-lar → 12:1:1 ≈ 86%) ≈ 0,94 peça/run → **~20% mais rápido** (~5 h para 6 peças). Pity de set acelera marginalmente.
- Veteran/Nightmare (sem Attunement): ~2× mais lento. Normal: sem peças de set.

Esses tempos mantêm a meta: **build funcional com 3 peças em uma sessão; set completo em um fim de semana; perseguição de qualidade/Primal no longo prazo.**

---

## 12. Coleta, filtros e inventário de run

### 12.1 Coleta

- Equipamentos dropados caem no chão com feixe de raridade e são **coletados automaticamente ao limpar a sala**, com resumo de 2 s no canto da tela (ícone, raridade, ★).
- O jogador pode coletar manualmente antes do fim da sala.
- Moedas e orbes: coleta por Pickup Radius.
- Cores: Common cinza-claro · Rare azul · Epic roxo · Legendary laranja · Set verde-jade (sobre base Legendary) · Unique ouro-cobre · Primal borda branca radiante.

### 12.2 Satchel (inventário de run)

- **30 espaços** (R37). Itens podem ser equipados durante a run.
- Ao encher, o filtro aplica auto-salvage a Commons e depois Rares de menor qualidade; Epic+ nunca são descartados automaticamente sem regra explícita do jogador.
- O destino dos itens no fim da run (extração, chefe, morte, Anchor) é regra de 10-progressao.

### 12.3 Filtro de loot

Presets: **Iniciante** (mostra tudo), **Padrão** (auto-salvage Common a partir de Veteran), **Rigoroso** (auto-salvage Common e Rare; Epic só se modificador de skill equipada).

Regras customizáveis (combináveis):
- raridade mínima;
- qualidade média mínima;
- "apenas herói atual";
- "Epic apenas se modificador de habilidade equipada";
- "peça de set duplicada com qualidade ≤ a que possuo";
- iLvl mínimo relativo ao equipado (ex.: ≥ equipado − 5).

**Nunca filtráveis:** Legendary/Set/Unique novos (não descobertos pelo Archivist) e Primal.

---

## 13. Salvage e descarte

| Raridade | Rendimento (moedas de 10, R36; quantidades: 10-progressao) |
|---|---|
| Common | Aether Dust (baixo) |
| Rare | Aether Dust (médio) |
| Epic | Aether Dust (alto) + 30% de chance de 1 Sundered Core |
| Legendary / Set / Unique | 1 Sundered Core + Aether Dust |
| Primal | 3 Sundered Cores + Aether Dust |

- Venda ao Merchant em Gold (valor por raridade × iLvl: 10).
- Salvage é feito no Sanctuary (Blacksmith) ou automaticamente pelo filtro durante a run.
- Itens bloqueados (cadeado) não podem ser salvos nem vendidos.
- Descobertas: todo Legendary/Set/Unique é registrado no Archivist no primeiro drop (coleção), independente de ser mantido.

---

## 14. Dependências e pontos de integração

| Doc | Recebe deste doc | Fornece a este doc |
|---|---|---|
| 01-atributos | Requisito de nível por iLvl; exceção "piso de Roll01 por dificuldade" (§10.1) | Tiers de iLvl, tabelas de faixas por tier, multiplicadores de slot, Faixa Nomeada; papel Core/Secundário/Terciário dos primários |
| 02-equipamentos | Estrutura por tipo de Legendary (§6); quota de 30 Uniques por fonte (§9.3); pesos de slot; regras de hero-bound aplicadas na geração | Bases por slot, pool e pesos de afixos, minDropILvl (Legendary 10, Unique 35), catálogo nominal com `fonteUnique`, `afinidade`, `heróisPermitidos`, opções do Afixo Aberto |
| 03-modificadores | Peso ×3 para skill equipada; divisão 75% herói / 25% `GEN-xx` no Epic | Pool de modificadores por herói/habilidade, `GEN-xx`, Signature, faixas de valores |
| 04-conjuntos | Tabela de região-lar (§9.1); sets só a partir de Veteran | Peças e slots de cada set |
| 08-herois | Nível esperado por dificuldade (§4.3) | Off Hand por herói |
| 09-inimigos | Fontes, pisos e contagem de itens por tipo de sala (inclui Guardian); premissa de composição da run (§11) | Estrutura da run (13 linhas + chefe), quantidade de inimigos/elites, eventos com loot, 3 Uniques por chefe, Boss Echoes |
| 10-progressao | Faixas de iLvl por dificuldade (§4.1); tabela de salvage; marca Primal; teto de Quantidade | Moedas definitivas, Heart of [Chefe], Attunement (objeto e custo), regra de morte e destino do Satchel, serviços (reroll do Afixo Aberto, upgrade, Imbue), curva de XP, Peril/Overrift/Distortions |
| 11-arquitetura | Estado de pity por herói, seed por item, tier e Roll01 normalizado, registro de fonte, alvo de Attunement | `LootSourceDefinition`, `RarityTable`, `ItemLevelBand`, `PityState` em ScriptableObjects/save; RNG de loot em stream separada do RNG de combate e de geração de mapa (evita manipulação por ordem de ações) |

---

## 15. Conflitos com as convenções

Nenhum. Observações:
- "Primal" é uma marca de qualidade nova (não é raridade) — compatível com §3.5 das convenções; absorve "Ascended" (R31).
- Eclipse usa faixa de iLvl plana (75–84) em todas as regiões; compatível com "iLvl ~80 no fim da campanha" (convenções §4).
- Aplicadas nesta revisão: R21, R25, R27, R28, R29, R30, R31, R32, R33, R36, R37, R38, R39, R40, R45 (00 §5).
- R32: para manter 2/2/3/2/3 com cada herói em 3 regiões distintas, além das trocas pedidas (Storm Phantom → Shattered, Void Oracle → Hollow) foram movidos Ruinbreaker → Shattered, Thousand Echoes → Astral e Death's Pursuit → Ashen. 04 deve citar esta tabela.

---

## 16. Questões abertas para playtest

1. Chance de drop de trash (3%) gera "chuva" suficiente para sensação de ARPG sem poluir o resumo de sala?
2. ~0,4 Legendary/run no Normal + garantias de 1º abate: o primeiro Legendary chega antes da 2ª run? (Meta: sim, via 1º abate do Ashen Colossus.)
3. Ritmo de set (~2 h para 3 peças, ~6 h para 6 no Eclipse) — validar retenção sem esvaziar o objetivo de longo prazo.
4. Reliquary (1 de 3) — tempo de decisão aceitável no meio da run? Se travar o ritmo, mover para tela de fim de sala com timer opcional.

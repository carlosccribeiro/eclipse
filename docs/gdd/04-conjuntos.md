# 04 — Catálogo dos 12 Conjuntos (Set Powers)

> Documento de detalhamento da etapa 4 da seção 18 do GDD v0.2. Respeita `00-convencoes.md`, incluindo o registro de resoluções de integração (§5, rodada 1).
> Dono: conjuntos, peças de conjunto e Set Powers. As faixas numéricas dos atributos vêm de 01-atributos (tiers e Faixa Nomeada); os modificadores são IDs de 03-modificadores; os valores dos Set Powers são definitivos (sujeitos apenas a ajuste fino em playtest).

---

## 1. Objetivo e escopo

Definir os 12 conjuntos Legendary (3 por herói), suas 72 peças e os 24 Set Powers, com:

- regras gerais do sistema (contagem de peças, limite de poderes ativos, Uniques, restrição de herói);
- matriz de slots dos 12 sets e viabilidade das combinações 3+3;
- para cada set: tema, fantasia, slots, peças (nome, lore, atributos fixos, Afixo Aberto, modificadores por ID de 03), bônus de 3 e 6 peças com condições e valores, More% introduzido;
- análise Rota A × B × C por herói (GDD §6.6) dentro da regra de ouro (±10%).

Fora do escopo: faixas numéricas por tier (01), pool de afixos por slot (02), catálogo dos modificadores (03), taxas de drop, fontes e região-lar (05), status de afinidade (07), kits dos heróis (08).

---

## 2. Decisões tomadas

1. **Sets nunca ocupam Main Hand nem Off Hand.** A arma é a principal fonte de identidade de combate (GDD §5.1); manter os slots de arma livres garante que toda rota (A, B ou C) escolha arma livremente.
2. **Slots elegíveis a peças de set: os 10 não-arma** (Head, Chest, Hands, Legs, Feet, Ring ×2, Amulet, Relic ×2). Um set pode conter **duas peças distintas** de Ring ou de Relic (ocupando os dois slots), nunca duas do mesmo slot único.
3. **Cada herói tem três "pegadas" de slot diferentes** entre seus sets (sobreposição de 2 a 4 slots), para que cada par 3+3 deixe livres slots diferentes.
4. **Limite de Set Powers ativos = 2.** O bônus de 6 peças conta como um segundo poder (3pc + 6pc = 2). Isso permite exatamente as rotas B (6) e C (3+3) e impede 6+3 e 3+3+3, que seriam estritamente superiores e matariam a Rota A.
5. **Peça distinta = definição de item distinta** (`ItemDefinition` ID). Duas cópias da mesma peça (possível apenas em Ring/Relic) contam como 1. A qualidade da rolagem não afeta a contagem.
6. **Valores dos Set Powers são fixos.** Não rolam e não escalam com iLvl. A busca por versões melhores fica nos atributos e modificadores da peça (GDD §6.4).
7. **Estrutura de peça de set (R25):** 4 atributos fixos + 1 **Afixo Aberto** (sorteado entre 3 opções definidas pela peça) + 2 modificadores de habilidade fixos (IDs de 03). **Sem efeito especial individual** — a identidade da peça é a contribuição ao set. Legendaries independentes têm efeito próprio; essa é a moeda de troca entre Rota A e Rotas B/C.
8. **Sets são exclusivos do herói (Hero-bound, R27).** Os poderes referenciam habilidades e recursos específicos; uso cruzado criaria peças mortas.
9. **Composição de atributos (R21/R23):** 1 atributo de assinatura do set (★) presente nas 6 peças + 3 fixos temáticos + 1 Afixo Aberto. Todos respeitam o pool de 02 §3.9.3, com **no máximo 1 afixo fora do pool por peça, marcado `*`** (em geral a própria assinatura). Primário: máx. 1 por peça (Amulet: 2), **nenhum em Ring ou Relic**. Máx. 2 afixos de afinidade por peça. Movement Speed só em Feet. HP, Armor e Health Regeneration são **flat**.
10. **Faixas (R21/R24):** valores pela tabela de tiers de 01 §3.9.2 (T1 = iLvl 1–20 … T5 = 81–100), dentro da Faixa Nomeada de 01: atributos fixos e Afixo Aberto em **[75%, 100%] do máximo do tier**; assinatura ★ em **[90%, 115%]**. Multiplicadores de slot de 01 §3.9.3 aplicam-se depois.
11. **Orçamento de poder (R03):** cada 3pc entrega ≈ **+10 a +12% de dano efetivo**; cada 6pc ≈ **+12 a +15%** sobre o 3pc. More nominal de um Set Power ≤ **+30% sustentado**; bônus de consumo de um único acerto (Storm Phantom) até +60%. Sets defensivos convertem parte do orçamento em EHP (taxa de referência: 1% de dano ≈ 1,5% EHP).
12. **Sem requisito de tipo de arma.** Preferências existem (Ruinbreaker favorece armas pesadas), mas todo set funciona com as 5 armas do herói (pilar 6 do GDD).
13. **Execute (R08):** limiar máximo 20% em inimigos comuns; Elites = metade (máx. 10%); **Guardians e chefes imunes**. Contra chefes, qualquer efeito de Execute de set vira **+25% Vulnerability abaixo de 20% de vida** — conversão única, que não acumula com outras fontes de Execute e não é More.
14. **Vulnerability (R12):** todo bônus de Vulnerability dos sets usa o status de 07 e respeita o teto total de **+60%**. O antigo efeito próprio "Fracture" de Ruinbreaker foi substituído pelos acúmulos de Vulnerability de Ruin (07).
15. **Eco e procs (R07):** repetições de clone e cópias de construto são réplicas (`IsEcho`, Proc Coefficient 0,4) e nunca geram novas réplicas. Única exceção do catálogo: **Thousand Echoes 6pc declara "Echo Recursion 1×"**. Entidades geradas por Set Powers (Ice Spire, Voidlings, explosões) são procs Gen+1 e nada dispara em Gen ≥ 2.
16. **Um único Unique de suporte a sets no jogo base: Keystone of the Sundering** (Relic), definido em 02 (R28). Ver §3.4.
17. **Nomes (R48):** peças e efeitos que colidiam com nomes reservados foram renomeados (§11).

---

## 3. Regras gerais do sistema

### 3.1 Contagem e ativação

1. O `SetTracker` (11-arquitetura) conta peças **distintas** equipadas por set a cada equip/unequip.
2. Bônus de 3 peças ativa com ≥3; bônus de 6 peças ativa com 6 (e mantém o de 3).
3. **Budget de 2 Set Powers ativos.** Se os candidatos excederem 2, a prioridade é:
   1. o set com mais peças;
   2. empate → o set escolhido pelo jogador como **Set Focus** na tela de Builds;
   3. sem Set Focus → o set que atingiu 3 peças primeiro.
4. Poderes inativos aparecem acinzentados na UI de conjuntos com o motivo ("Limite de Set Powers atingido").
5. Remover uma peça desativa o poder imediatamente; efeitos temporários em andamento (ex.: Ashen Ascension) terminam no fim do frame.
6. Set Powers são fontes de efeito independentes e não ocupam os slots de modificador das peças (GDD §6.6 regra 6).

### 3.2 Combinações possíveis

| Rota | Composição | Set Powers | Slots não-arma livres para Legendary independentes |
|---|---|---|---|
| A — Individual | 0 peças de set | 0 | 10 |
| B — Completa | 6 de um set | 2 (3pc + 6pc) | 4 |
| C — Híbrida | 3 + 3 de dois sets | 2 (3pc + 3pc) | 4 |
| Parcial | 3 de um set | 1 | 7 |
| Inválidas por budget | 6+3, 3+3+3 | 2 (o restante fica inativo) | — |

Em todas as rotas, Main Hand e Off Hand ficam livres.

### 3.3 Restrição de herói e obtenção

- Peças de set exibem o herói dono e só podem ser equipadas por ele (R27).
- Podem ser armazenadas no Vault/Set Locker (10) e transferidas entre perfis de build do mesmo herói.
- **Não caem em Normal** (R28); a partir de Veteran seguem smart loot, pity e a **tabela de região-lar de 05 §9.1** (R32). Attunement (Eclipse+) multiplica o peso do set escolhido (05 §9.4, 10).

### 3.4 Interação com Uniques

- Uniques são Legendary independentes e **nunca** são peças de set.
- Uniques podem referenciar mecânicas de um set apenas se também funcionarem sem o set — um Unique não pode ser inútil fora de uma rota.
- **Keystone of the Sundering — Unique Relic (todos os heróis), definido em 02 (R28):** conta como +1 peça de **cada** set que tenha ≥2 peças distintas equipadas, sem levar nenhum set além de 6. Respeita o budget de 2 Set Powers. Efeito: economiza exatamente 1 slot em B (5 + Keystone) ou C (2+2 + Keystone, 5 slots). Para compensar, seus 5 atributos fixos ficam no piso da Faixa Nomeada (70–80% do máximo do tier) e seus 2 modificadores são de categoria Resource/Duration.

### 3.5 Upgrades e serviços

- Blacksmith (+0..+10, +3% por nível sobre atributos — R31) e Enchanter (reroll do Afixo Aberto entre suas 3 opções — R42; Imbue de valores) funcionam normalmente em peças de set (10).
- Nenhum serviço altera Set Powers, os 4 atributos fixos nem a qual set a peça pertence.

---

## 4. Matriz de slots

Legenda: ● = o set possui peça nesse slot; ●● = duas peças distintas (Ring ou Relic).

| Herói | Set | Head | Chest | Hands | Legs | Feet | Ring | Amulet | Relic | Livres no 6pc |
|---|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|---|
| Kael | Ashen Sovereign | ● | ● | ● | ● | ● | | ● | | Ring ×2, Relic ×2 |
| Kael | Ruinbreaker | | ● | ● | ● | | ●● | | ● | Head, Feet, Amulet, Relic |
| Kael | Eternal Bastion | ● | ● | | | ● | | ● | ●● | Hands, Legs, Ring ×2 |
| Lyra | Frozen Empress | ● | ● | | ● | ● | ● | ● | | Hands, Ring, Relic ×2 |
| Lyra | Void Oracle | ● | | ● | | | ● | ● | ●● | Chest, Legs, Feet, Ring |
| Lyra | Arcane Paradox | | ● | ● | ● | | ●● | | ● | Head, Feet, Amulet, Relic |
| Nyx | Storm Phantom | | | ● | ● | ● | ● | ● | ● | Head, Chest, Ring, Relic |
| Nyx | Thousand Echoes | ● | ● | | | | ●● | | ●● | Hands, Legs, Feet, Amulet |
| Nyx | Death's Pursuit | ● | ● | ● | | ● | | ● | ● | Legs, Ring ×2, Relic |
| Orun | Abyssal Legion | ● | ● | | | | ● | ● | ●● | Hands, Legs, Feet, Ring |
| Orun | Hollow Sovereign | | | ● | ● | ● | ●● | ● | | Head, Chest, Relic ×2 |
| Orun | Eternal Warden | ● | ● | ● | ● | ● | | | ● | Ring ×2, Amulet, Relic |

### 4.1 Viabilidade 3+3

**Toda combinação 3+3 entre dois sets quaisquer é sempre possível.** Prova: o set A ocupa 3 de seus 6 slots; no máximo 3 dos 6 slots de B ficam bloqueados; restam ≥3 slots de B. Logo a variação de pegadas não serve para viabilizar, e sim para **diversificar quais slots sobram e quanta liberdade há na escolha das peças**.

Métrica: **sobreposição (S)** = slots em comum. S baixa (2) → o jogador escolhe livremente as 3 melhores peças de cada set (por rolagem). S alta (4) → as escolhas são mais forçadas, mas o par costuma liberar slots de joia valiosos.

| Herói | Par | S | Exemplo de 3+3 | Slots livres resultantes |
|---|---|:-:|---|---|
| Kael | Ashen Sovereign + Ruinbreaker | 3 | AS: Head, Feet, Amulet · RB: Ring, Ring, Relic | Chest, Hands, Legs, Relic |
| Kael | Ashen Sovereign + Eternal Bastion | 4 | AS: Head, Hands, Legs · EB: Chest, Relic, Relic | Feet, Amulet, Ring ×2 |
| Kael | Ruinbreaker + Eternal Bastion | 2 | RB: Hands, Legs, Ring · EB: Head, Feet, Amulet | Chest, Ring, Relic ×2 |
| Lyra | Frozen Empress + Void Oracle | 3 | FE: Chest, Legs, Feet · VO: Hands, Relic, Relic | Head, Amulet, Ring ×2 |
| Lyra | Frozen Empress + Arcane Paradox | 3 | FE: Head, Feet, Amulet · AP: Hands, Ring, Relic | Chest, Legs, Ring, Relic |
| Lyra | Void Oracle + Arcane Paradox | 3 | VO: Head, Amulet, Relic · AP: Chest, Legs, Ring | Hands, Feet, Ring, Relic |
| Nyx | Storm Phantom + Thousand Echoes | 2 | SP: Hands, Legs, Feet · TE: Head, Chest, Ring | Ring, Amulet, Relic ×2 |
| Nyx | Storm Phantom + Death's Pursuit | 4 | SP: Legs, Ring, Amulet · DP: Head, Chest, Hands | Feet, Ring, Relic ×2 |
| Nyx | Thousand Echoes + Death's Pursuit | 3 | TE: Ring, Ring, Relic · DP: Hands, Feet, Amulet | Head, Chest, Legs, Relic |
| Orun | Abyssal Legion + Hollow Sovereign | 2 | AL: Head, Chest, Relic · HS: Hands, Legs, Feet | Ring ×2, Amulet, Relic |
| Orun | Abyssal Legion + Eternal Warden | 3 | AL: Amulet, Ring, Relic · EW: Hands, Legs, Feet | Head, Chest, Ring, Relic |
| Orun | Hollow Sovereign + Eternal Warden | 3 | HS: Ring, Ring, Amulet · EW: Head, Chest, Relic | Hands, Legs, Feet, Relic |

Observações:
- Todo herói tem ao menos um par de S=2 (máxima liberdade) — os pares "assinatura" de build híbrida.
- Nenhum par força o uso de um slot específico para Legendary independentes: sempre restam 4 slots não-arma + armas.

---

## 5. Atributos e modificadores das peças

### 5.1 Faixas

As tabelas de cada set listam **quais** atributos a peça possui. Os valores vêm de **01 §3.9.2** pelo tier do iLvl do item, dentro da Faixa Nomeada (decisão 10):

| Tipo de atributo | Faixa dentro do tier | Exemplo T5 (iLvl 81–100, multiplicador de slot 1,0) |
|---|---|---|
| Fixo / Afixo Aberto | 75%–100% do máximo do tier | Strength 26,3–35 · Critical Damage 30–40% · Maximum Health 158–210 (flat) |
| Assinatura ★ | 90%–115% do máximo do tier | Ember Damage ★ 27–34,5% · Armor ★ 378–483 (flat) |

Depois aplica-se o multiplicador de slot de 01 §3.9.3 (ex.: Maximum Health ×1,4 em Chest; Movement Speed ×1,5 em Feet; afinidades ×1,25 em Relic). O upgrade do Blacksmith (01 §3.9.5) é o único caminho para passar do teto da Faixa Nomeada.

Primários por herói (01 §3.2.2): Kael → Strength · Lyra → Intelligence · Nyx → Dexterity · Orun → Intelligence.

### 5.2 Notação das tabelas

- **Fixos:** os 4 atributos fixos; ★ = assinatura do set; `*` = afixo fora do pool de 02 §3.9.3 para aquele slot (máx. 1 por peça).
- **Aberto:** as 3 opções do Afixo Aberto (sempre dentro do pool do slot); a peça rola uma delas.
- **Modificadores:** 2 IDs de 03-modificadores (o par Habilidade × Categoria original foi resolvido pelo índice 03 §10.2). Regra: o modificador de uma peça **não duplica nem depende do Set Power** — ele tem valor também na Rota C, sem o 6pc. Um par nunca é preenchido por `GEN-xx`.

---

## 6. Kael — The Ashbound

Habilidades: Cleaving Strike, Infernal Leap, Burning Chains, Ashen Guard, Worldbreaker. Recurso: Fury (0–100).

### 6.1 Ashen Sovereign

- **Tema:** Fury e Ember · **Build:** Inferno Berserker
- **Fantasia:** o último rei do trono de cinzas; Kael deixa o corpo ser consumido pelo fogo e se torna uma fornalha ambulante.
- **Slots:** Head, Chest, Hands, Legs, Feet, Amulet · **Assinatura:** Ember Damage ★ · **Região-lar:** The Ashen Wastes (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Diadem of the Ash Throne | Head | Forjado das brasas do primeiro trono a queimar no Sundering. | Ember Damage ★*, Strength, Critical Chance, Maximum Health | Cooldown Reduction / Status Resistance / Maximum Resource | KAE-IL-04 Gravity Crash; KAE-WB-08 Ashfall |
| Sovereign's Pyre-Plate | Chest | O peitoral ainda arde; dizem que o fogo é o coração do antigo rei. | Ember Damage ★*, Strength, Damage Over Time, Armor | Maximum Health / Damage Reduction / Elemental Resistance | KAE-CS-01 Burning Arc; KAE-AG-04 Reflective Plate |
| Gauntlets of the Ember Oath | Hands | Quem as calça jura nunca mais conhecer o frio. | Ember Damage ★, Strength, Attack Speed, Critical Damage | Critical Chance / Armor Penetration / Resource Generation | KAE-CS-03 Executioner's Reach; KAE-BC-04 Searing Shackles |
| Cuisses of the Ashen March | Legs | Usadas pela guarda que marchou através do incêndio sem recuar. | Ember Damage ★*, Strength, Area Damage, Maximum Health | Damage Over Time / Elemental Resistance / Duration of Effects | KAE-IL-02 Scorched Trail; KAE-BC-08 Molten Links |
| Ashwalker Sabatons | Feet | Cada passo deixa uma pegada de brasa. | Ember Damage ★*, Movement Speed, Resource Generation, Maximum Health | Strength / Elemental Resistance / Status Resistance | KAE-IL-10 Combustion Landing; KAE-CS-07 Blood Fury |
| Pendant of the Burning Throne | Amulet | Um coração de pedra que bate ao ritmo das chamas. | Ember Damage ★, Strength, Critical Damage, Damage Reduction | Skill Damage / Physical Resistance / Cooldown Reduction | KAE-WB-01 Tectonic Fissures; KAE-AG-01 Ember Riposte |

**3 peças — Ashen Ascension**
- Condição: ao **gastar um total acumulado de 150 Fury**, Kael entra em Ashen Ascension por **8 s**. Recarga interna: **12 s** contados a partir do início.
- Durante Ascension:
  - ataques `Melee` têm **+25% de raio/alcance** (raio ×√1,25, regra de área de 06);
  - ataques `Attack` ganham **25% do dano como dano Ember adicional** (ganho-como-extra, 06; equivale a More 25% nominal);
  - acertos `Melee` aplicam Burning (07) com 1 acúmulo;
  - Fury **não decai**.
- Dano: **+25% nominal**, uptime esperado ~50–60% → **≈ +12% efetivo**.

**6 peças — Sovereign's Wrath**
- Durante Ashen Ascension:
  - Cleaving Strike gera uma sequência de **3 explosões** à frente (1,5 m, 3 m e 4,5 m de distância), cada uma com **60% do dano da arma** como Ember, `Area`, raio 2,5 m (procs Gen+1);
  - Kael causa **+30% More dano Ember**;
  - cada eliminação prolonga Ascension em **0,5 s**, até **+4 s** por ativação.
- Dano: **+30% More nominal** + explosões, uptime ~60–65% com extensões → **≈ +14% efetivo**.

### 6.2 Ruinbreaker

- **Tema:** armas pesadas e Ruin · **Build:** Ruin Executioner
- **Fantasia:** carrasco que racha a realidade a cada golpe; os inimigos se partem como pilares antes do colapso.
- **Slots:** Chest, Hands, Legs, Ring ×2, Relic · **Assinatura:** Ruin Damage ★ · **Região-lar:** The Shattered Expanse (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Ruinbreaker Hauberk | Chest | Feita das correntes que prendiam um Pilar em queda. | Ruin Damage ★*, Strength, Armor, Maximum Health | Damage Reduction / Physical Resistance / Damage Over Time | KAE-CS-06 Ruin Cleaver; KAE-WB-07 Executioner's Verdict |
| Fistguards of the Fault | Hands | Racham pedra só de se fecharem. | Ruin Damage ★, Strength, Critical Damage, Armor Penetration | Stagger Damage / Physical Damage / Critical Chance | KAE-CS-08 Infernal Collapse; KAE-BC-08 Molten Links |
| Tassets of Collapse | Legs | Pesam como uma catedral; quem as carrega não cede. | Ruin Damage ★*, Strength, Area Damage, Maximum Health | Armor / Damage Reduction / Damage Over Time | KAE-IL-09 Tectonic Landing; KAE-AG-07 Unyielding |
| Band of the Cracked Pillar | Ring | Um fragmento do Pilar da Lei preso em ferro. | Ruin Damage ★, Armor Penetration, Critical Chance, Physical Damage | Critical Damage / Void Resistance / Resource Generation | KAE-WB-09 Pyre Crown; KAE-IL-10 Combustion Landing |
| Signet of Final Weight | Ring | O selo que encerrava sentenças sem apelação. | Ruin Damage ★, Physical Damage, Critical Damage, Maximum Health | Attack Speed / Skill Damage / Resource Generation | KAE-CS-02 Volatile Impact; KAE-BC-02 Fan of Chains |
| Shard of the First Fault | Relic | A primeira rachadura do Sundering, ainda se alargando. | Ruin Damage ★, Damage Over Time, Area Damage, Duration of Effects | Status Resistance / Projectile Damage / Ember Damage | KAE-WB-01 Tectonic Fissures; KAE-IL-04 Gravity Crash |

**3 peças — Fault Line**
- Acertos de `Heavy` e de Cleaving Strike aplicam **Vulnerability** (status Ruin de 07: +4% por acúmulo, máx. 5, 5 s, renova).
- Com Greatsword, Warhammer ou Heavy Blade: **2 acúmulos por acerto**; com Dual Axes ou Longsword: 1 acúmulo (compensado pela cadência).
- Dano: entra no bucket **Vulnerability** (teto +60%, R12), +20% nominal com ramp-up → **≈ +12% efetivo**.

**6 peças — Ruinous Verdict**
- Inimigos comuns com 5 acúmulos de Vulnerability abaixo de **20% de vida** (Elites: **10%**) são **executados** (HitFlag `Execute`, 06) pelo próximo acerto de `Heavy` ou Cleaving Strike. Guardians e chefes são imunes.
- Contra chefes: conversão única de R08 — **+25% Vulnerability abaixo de 20% de vida** enquanto o chefe tiver 5 acúmulos.
- Toda execução gera **Verdict Shockwave** (proc Gen+1): **200% do dano da arma** como Ruin, `Area`, raio 3 m, aplicando **3 acúmulos de Vulnerability** em quem atingir.
- Kael causa **+20% More dano Ruin** contra alvos com 5 acúmulos de Vulnerability.
- Dano: +20% More nominal + execuções (remoção de vida residual) → **≈ +13% efetivo**; contra chefe ≈ +11% (Vulnerability total no chefe: 20% + 25% = 45%, abaixo do teto).

### 6.3 Eternal Bastion

- **Tema:** defesa e resistência · **Build:** Unbreakable Juggernaut
- **Fantasia:** a muralha que não caiu; cada golpe recebido é guardado e devolvido.
- **Slots:** Head, Chest, Feet, Amulet, Relic ×2 · **Assinatura:** Armor ★ (flat) · **Set defensivo** · **Região-lar:** The Frozen Cathedral (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Bastion Greathelm | Head | Selada por dentro; o último guardião nunca a tirou. | Armor ★, Strength, Maximum Health, Physical Resistance | Status Resistance / Cooldown Reduction / Health Regeneration | KAE-AG-05 Lingering Ash; KAE-CS-02 Volatile Impact |
| Aegis of the Unfallen | Chest | Mil marcas de lâmina e nenhum furo. | Armor ★, Strength, Damage Reduction, Health Regeneration | Block Chance / Maximum Health / Shield Strength | KAE-AG-03 Bulwark Stance; KAE-WB-07 Executioner's Verdict |
| Stonevow Treads | Feet | Quem jura não recuar sente o chão segurar seus pés. | Armor ★, Movement Speed, Status Resistance, Maximum Health | Physical Resistance / Dodge Chance / Resource Generation | KAE-IL-03 Twin Leap; KAE-AG-02 Absorbing Ash |
| Ward of the Last Gate | Amulet | A chave de um portão que já não existe. | Armor ★*, Strength, Maximum Health, Damage Reduction | Physical Resistance / Shield Strength / Critical Damage | KAE-BC-01 Chain Pull; KAE-CS-07 Blood Fury |
| Ember of the Watchfire | Relic | A fogueira da vigília nunca se apagou. | Armor ★*, Status Resistance, Ember Damage, Duration of Effects | Damage Over Time / Area Damage / Ruin Damage | KAE-WB-08 Ashfall; KAE-BC-07 Molten Knot |
| Mortar of Ages | Relic | Argamassa tirada da fundação do Sanctuary. | Armor ★*, Status Resistance, Area Damage, Ruin Damage | Duration of Effects / Damage Over Time / Ember Damage | KAE-AG-04 Reflective Plate; KAE-IL-09 Tectonic Landing |

**3 peças — Unyielding Wall**
- Enquanto Ashen Guard está ativa, **40% do dano mitigado/bloqueado** é armazenado como **Retribution** (máx. **50% da Vida máxima**).
- O próximo Cleaving Strike em até 5 s libera Retribution como onda frontal (`Area`, Physical, 6 m) causando **dano igual ao valor armazenado × 1,5**.
- Kael tem **+10% Damage Reduction** enquanto Fury ≥ 50 (sujeito ao cap de 40% de 01).
- Dano: **≈ +8% efetivo** (Retribution) + **≈ +15% EHP**.

**6 peças — Bastion Eternal**
- Ashen Guard ganha **+1 carga**.
- Um **Parry** (06, janela de 0,2 s no início de Ashen Guard — R20) dispara automaticamente um Cleaving Strike de contra-ataque com **+50% de dano** e concede escudo de **15% da Vida máxima** por 4 s.
- Contra-ataques causam **+20% More**.
- **Unfallen Resolve:** dano fatal deixa Kael com 1 de vida e ativa Ashen Guard gratuita por 2 s (recarga 90 s).
- Dano: **≈ +10% efetivo** + **≈ +25% EHP** + prevenção de morte.

### 6.4 Balanceamento — Kael

Modelo da Rota A em §10.1.

| Rota | Composição | Dano efetivo estimado | EHP | Leitura |
|---|---|---|---|---|
| A | 6 Legendary independentes nos slots que o set usaria | ×1,23–1,30 | base | Máxima liberdade de rolagem e de efeitos; melhor para builds fora dos 3 arquétipos. |
| B | Ashen Sovereign 6 | ×1,12 × 1,14 = **×1,28** | base | Pico de dano mais alto do herói, dependente de uptime de Ascension. |
| B | Ruinbreaker 6 | ×1,12 × 1,13 = **×1,27** | base | Forte em limpeza (execuções); contra chefe ~×1,24. |
| B | Eternal Bastion 6 | ×1,08 × 1,10 = ×1,19 | **+40%** + Unfallen Resolve | Menos dano, maior sobrevivência — dentro do orçamento pela conversão EHP. |
| C | AS 3 + RB 3 ("Ashen Executioner") | ×1,12 × 1,12 = **×1,25** | base | Ember + Ruin ativa a Resonance Cinder Rot (07); melhor Rota C ofensiva. |
| C | AS 3 + EB 3 | ×1,12 × 1,08 = ×1,21 | +15% | "Berserker blindado"; Retribution alimenta o gasto de Fury. |
| C | RB 3 + EB 3 | ×1,12 × 1,08 = ×1,21 | +15% | Contra-ataques Heavy aplicam Vulnerability; perfil anti-chefe. |

Rotas ofensivas entre ×1,21 e ×1,30 → razão máx./mín. 1,074 (±3,7% em torno da média), dentro da regra de ouro.

---

## 7. Lyra — The Veilweaver

Habilidades: Frost Lance, Void Orb, Shattered Reality, Astral Shift, Absolute Zero. Recurso: Arcane Charges (base 3, teto base 6, até 8 com exceções listadas em 08 — R01).

### 7.1 Frozen Empress

- **Tema:** Frost · **Build:** Permafrost
- **Fantasia:** a imperatriz da corte que congelou o próprio tempo para não ver o mundo acabar.
- **Slots:** Head, Chest, Legs, Feet, Ring, Amulet · **Assinatura:** Frost Damage ★ · **Região-lar:** The Frozen Cathedral (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Rimecrown of the Empress | Head | A coroa nunca derrete, nem no coração do Ashen Wastes. | Frost Damage ★*, Intelligence, Critical Chance, Maximum Health | Cooldown Reduction / Skill Damage / Elemental Resistance | LYR-FL-01 Piercing Lance; LYR-AZ-07 Frozen Sanctum |
| Glacial Court Gown | Chest | Tecida com geada que um dia foi lágrima. | Frost Damage ★*, Intelligence, Maximum Health, Shield Strength | Elemental Resistance / Damage Reduction / Damage Over Time | LYR-SR-09 Frozen Fracture; LYR-FL-02 Splinter Burst |
| Frostveil Skirts | Legs | O frio as acompanha como uma cauda real. | Frost Damage ★*, Intelligence, Area Damage, Duration of Effects | Maximum Health / Elemental Resistance / Dodge Chance | LYR-AZ-01 Lingering Winter; LYR-AS-02 Frost Step |
| Stillwater Slippers | Feet | Onde ela pisa, a água para de correr. | Frost Damage ★*, Movement Speed, Dodge Chance, Resource Generation | Intelligence / Maximum Health / Status Resistance | LYR-AS-08 Slipstream Cast; LYR-FL-08 Overflowing Lance |
| Ring of the Unthawed | Ring | Um anel que guarda o último instante antes do gelo. | Frost Damage ★, Critical Damage, Cast Speed, Skill Damage | Critical Chance / Resource Generation / Elemental Resistance | LYR-FL-09 Arcane Lance; LYR-SR-04 Collapsing Reality |
| Tear of the Pale Court | Amulet | Dizem que a imperatriz chorou uma única vez. | Frost Damage ★, Intelligence, Skill Damage, Critical Damage | Shield Strength / Cooldown Reduction / Elemental Resistance | LYR-AZ-02 Glacial Prison; LYR-VO-10 Cold Collapse |

**3 peças — Winter's Court**
- Habilidades `Frost` de Lyra aplicam **+50% de acúmulo de Freeze** (buildup de 07).
- Inimigos Frozen emitem aura de 2,5 m que aplica **Chill** (Slow de Frost, 07).
- Lyra causa **+25% More** contra inimigos Frozen. Chefes contam como Frozen para este bônus enquanto estiverem **Frostbound** (07, R10). Respeita a DR de CC de 06.
- Dano: +25% More nominal → **≈ +11% efetivo**.

**6 peças — Empress's Decree**
- Aplicar **Shatter** (07) cria uma **Ice Spire** no local por 5 s (máx. 3 ativas), que dispara um fragmento a cada 1 s com **40% do dano** de Frost Lance no inimigo mais próximo (8 m). Fragmentos são procs Gen+1.
- Cada Shatter reduz a recarga de Absolute Zero em **1 s**.
- Habilidades `Frost` + `Area` causam **+20% More**.
- Dano: **≈ +14% efetivo** (spires + bucket).

### 7.2 Void Oracle

- **Tema:** Void · **Build:** Void Singularity
- **Fantasia:** a vidente que olhou para o vazio entre os Pilares e aprendeu a dobrá-lo.
- **Slots:** Head, Hands, Amulet, Ring, Relic ×2 · **Assinatura:** Void Damage ★ · **Região-lar:** The Hollow Depths (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Veil of the Seeing Dark | Head | Quem a usa vê através das paredes da realidade. | Void Damage ★*, Intelligence, Cooldown Reduction, Maximum Health | Skill Damage / Critical Chance / Void Resistance | LYR-VO-03 Twin Orbs; LYR-SR-08 Inward Spiral |
| Gloves of Bent Light | Hands | A luz contorna os dedos de quem as calça. | Void Damage ★, Intelligence, Cast Speed, Critical Chance | Critical Damage / Resource Generation / Maximum Health | LYR-VO-05 Growing Void; LYR-AS-01 Void Wake |
| Oracle's Eye | Amulet | Um olho que nunca fecha, preso em vidro negro. | Void Damage ★, Intelligence, Skill Damage, Critical Damage | Void Resistance / Damage Reduction / Cooldown Reduction | LYR-SR-02 Time Dilation; LYR-AZ-08 Storm Front |
| Loop of the Far Edge | Ring | O limite do que pode ser visto, fechado em círculo. | Void Damage ★, Area Damage, Resource Generation, Critical Chance | Cast Speed / Skill Damage / Void Resistance | LYR-VO-06 Unstable Orb; LYR-FL-09 Arcane Lance |
| Lens of the Collapsed Star | Relic | Uma estrela que caiu para dentro de si mesma. | Void Damage ★, Area Damage, Duration of Effects, Damage Over Time | Frost Damage / Status Resistance / Projectile Damage | LYR-SR-04 Collapsing Reality; LYR-VO-09 Rift Orb |
| Astrolabe of Nothing | Relic | Mede distâncias que não existem mais. | Void Damage ★, Projectile Damage, Duration of Effects, Status Resistance | Area Damage / Damage Over Time / Frost Damage | LYR-AS-02 Frost Step; LYR-AZ-07 Frozen Sanctum |

**3 peças — Gravitic Sight**
- Void Orb aplica **Pull** (07) a inimigos a até 4 m de sua trajetória (em chefes: **Distortion**, R10).
- Inimigos que sofreram Pull ou Distortion recebem **+20% More** de dano `Area` de Lyra por 3 s.
- Dano: **≈ +11% efetivo**.

**6 peças — Oracle's Collapse**
- Ao fim da trajetória (ou ao ser reativada), Void Orb colapsa em um **Collapsar** de 3 s (raio 3,5 m) que aplica Pull contínuo e causa **30% do dano de Void Orb a cada 0,5 s**.
- Shattered Reality conjurada sobre um Collapsar o consome em **Implosion** (07): **400% do dano de Shattered Reality** + **15% por inimigo atingido** (máx. +150%).
- Implosion que atinge ≥5 inimigos devolve **1 Arcane Charge**.
- Dano: **≈ +14% efetivo** (forte em grupos, ~+9% em chefe isolado; compensado pelo uptime do Collapsar em adds de chefe).

### 7.3 Arcane Paradox

- **Tema:** magia e Arcane Charges · **Build:** Arcane Overload
- **Fantasia:** a feitiçaria que escreve a si mesma; cada carga gasta reescreve a próxima.
- **Slots:** Chest, Hands, Legs, Ring ×2, Relic · **Assinatura:** Resource Generation ★ · **Região-lar:** The Astral Citadel (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Robes of the Unwritten | Chest | Os glifos mudam cada vez que alguém tenta lê-los. | Resource Generation ★*, Intelligence, Maximum Health, Shield Strength | Damage Reduction / Elemental Resistance / Damage Over Time | LYR-SR-10 Unstable Pages; LYR-FL-04 Frost Volley |
| Paradox Wraps | Hands | As mãos conjuram antes que a mente decida. | Resource Generation ★, Intelligence, Cast Speed, Critical Chance | Critical Damage / Resource Cost Reduction / Void Damage | LYR-VO-08 Charge Battery; LYR-AS-08 Slipstream Cast |
| Leggings of Recursive Script | Legs | Uma frase que termina onde começa. | Resource Generation ★*, Intelligence, Area Damage, Maximum Health | Duration of Effects / Dodge Chance / Damage Over Time | LYR-AZ-09 Glacial Reserve; LYR-SR-03 Mirror Zone |
| Ouroboros Ring | Ring | Morde a própria cauda; nunca termina de se consumir. | Resource Generation ★, Resource Cost Reduction, Critical Damage, Maximum Resource | Cast Speed / Skill Damage / Frost Damage | LYR-FL-07 Arcane Echo Lance; LYR-VO-04 Rime Core |
| Ring of the Second Answer | Ring | Toda pergunta do Aether tem duas respostas. | Resource Generation ★, Skill Damage, Cast Speed, Maximum Health | Critical Chance / Void Damage / Maximum Resource | LYR-AS-05 Arcane Refund; LYR-FL-06 Shattering Lance |
| Hourglass Without Sand | Relic | O tempo escorre, mas nada cai. | Resource Generation ★*, Duration of Effects, Area Damage, Status Resistance | Frost Damage / Void Damage / Damage Over Time | LYR-AZ-05 Shatterstorm; LYR-VO-02 Orbital |

**3 peças — Paradox Engine**
- **+1 Arcane Charge máxima** (exceção listada em 08; teto absoluto 8 — R01).
- Cada Arcane Charge consumida concede 1 acúmulo de **Paradox** por 6 s (máx. 4): **+5% Cast Speed** e **+6% More dano `Spell`** por acúmulo.
- Dano: até +24% More nominal, uptime ~80% em 3–4 acúmulos → **≈ +12% efetivo**.

**6 peças — Infinite Recursion**
- Ao consumir a **última** Arcane Charge (ficar com 0), Lyra entra em **Paradox Surge** por 4 s (recarga interna 10 s):
  - habilidades não consomem Arcane Charges;
  - recargas de habilidades avançam **+30% mais rápido**;
  - ao fim do Paradox Surge, **todas as Arcane Charges são restauradas**.
- Acúmulos de Paradox não expiram durante Paradox Surge.
- Dano: **≈ +13% efetivo** (casts extras gratuitos + manutenção de Paradox).

### 7.4 Balanceamento — Lyra

| Rota | Composição | Dano efetivo estimado | Leitura |
|---|---|---|---|
| A | 6 Legendary independentes | ×1,23–1,30 | Liberdade total; melhor para builds que misturam Frost Lance + Void Orb sem foco. |
| B | Frozen Empress 6 | ×1,11 × 1,14 = **×1,27** | Controle máximo; dano escala com tempo de Freeze. |
| B | Void Oracle 6 | ×1,11 × 1,14 = **×1,27** | Melhor clear do jogo base em grupos densos; ~×1,21 em chefe isolado. |
| B | Arcane Paradox 6 | ×1,12 × 1,13 = **×1,27** | Rotação mais exigente; maior teto em mãos hábeis (gestão do Paradox Surge). |
| C | FE 3 + VO 3 | ×1,11 × 1,11 = **×1,23** | Pull + Freeze = Resonance Frost + Void (07); controle quase total. |
| C | FE 3 + AP 3 | ×1,11 × 1,12 = ×1,24 | Frost Lance spam com cargas extras. |
| C | VO 3 + AP 3 | ×1,11 × 1,12 = ×1,24 | Void Orb mais frequente, Pull constante. |

Variação entre todas as rotas: ×1,23–1,30 (razão 1,057; ±2,8%).

---

## 8. Nyx — The Echo Hunter

Habilidades: Phantom Shot, Shadowstep, Storm Trap, Echo Clone, Thousand Reflections. Recurso: Echo Energy (0–100).

### 8.1 Storm Phantom

- **Tema:** Storm · **Build:** Storm Reaper
- **Fantasia:** a caçadora que corre mais rápido que o trovão e chega antes do próprio som.
- **Slots:** Hands, Legs, Feet, Ring, Amulet, Relic · **Assinatura:** Storm Damage ★ · **Região-lar:** The Shattered Expanse (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Stormgrip Bracers | Hands | Faíscas saltam entre os dedos antes de cada disparo. | Storm Damage ★, Dexterity, Attack Speed, Critical Chance | Critical Damage / Armor Penetration / Resource Generation | NYX-PS-02 Ricochet Bolt; NYX-ST-02 Thrown Trap |
| Galeweave Leggings | Legs | Tecidas com o vento de uma tempestade que nunca parou. | Storm Damage ★*, Dexterity, Projectile Damage, Dodge Chance | Maximum Health / Area Damage / Elemental Resistance | NYX-SS-01 Lightning Dash; NYX-PS-08 Marked Prey |
| Thunderstep Boots | Feet | Cada passo é um trovão sem som. | Storm Damage ★*, Movement Speed, Resource Generation, Dodge Chance | Dexterity / Elemental Resistance / Maximum Health | NYX-SS-04 Chain Step; NYX-ST-03 Overload Trap |
| Ring of the Rolling Sky | Ring | Um céu em miniatura, sempre carregado. | Storm Damage ★, Critical Damage, Attack Speed, Projectile Damage | Critical Chance / Elemental Resistance / Resource Generation | NYX-ST-09 Chain Snare; NYX-TR-03 Storm Prism |
| Phantom Spark Pendant | Amulet | Uma faísca presa em vidro, que pisca quando há perigo. | Storm Damage ★, Dexterity, Critical Damage, Skill Damage | Damage Reduction / Elemental Resistance / Cooldown Reduction | NYX-PS-03 Charged Shot; NYX-EC-09 Stormbound Echo |
| Bottled Lightning | Relic | Alguém conseguiu. Ninguém sabe como. | Storm Damage ★, Area Damage, Duration of Effects, Projectile Damage | Echo Damage / Status Resistance / Damage Over Time | NYX-ST-08 Static Field Trap; NYX-TR-07 Endless Cuts |

**3 peças — Static Velocity**
- Nyx acumula **Momentum** ao se mover: 1 por metro percorrido (máx. 20). Shadowstep concede **+10** instantaneamente.
- O próximo Phantom Shot ou `Attack` consome todo Momentum: **+3% More dano por acúmulo** (até +60%, bônus de consumo de um acerto — decisão 11) como Storm, e encadeia **Chain Lightning** (07) para **1 alvo extra a cada 5 acúmulos** (máx. 4; saltos são Gen+1).
- Dano: até +60% em um acerto a cada ~2,5 s → **≈ +11% efetivo**.

**6 peças — Phantom Tempest**
- Após Shadowstep, os próximos **3 `Attack`** são **Overcharged**: aplicam Shock garantido (07) e causam **+25% More** dano Storm.
- Cada inimigo Shocked eliminado reduz a recarga de Shadowstep em **0,5 s**.
- **+10% Movement Speed** (aditivo ao atributo; sujeito aos caps de 01).
- Dano: **≈ +13% efetivo**.

### 8.2 Thousand Echoes

- **Tema:** Echo · **Build:** Echo Architect
- **Fantasia:** Nyx não é uma, é muitas; cada reflexo aprende o que a original faz.
- **Slots:** Head, Chest, Ring ×2, Relic ×2 · **Assinatura:** Echo Damage ★ · **Região-lar:** The Astral Citadel (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Mask of Many Faces | Head | Por trás dela não há rosto, só reflexos. | Echo Damage ★*, Dexterity, Cooldown Reduction, Maximum Health | Skill Damage / Critical Chance / Status Resistance | NYX-EC-04 Flawless Mirror; NYX-TR-01 Endless Reflections |
| Mirrorweave Vest | Chest | O tecido devolve a luz como um espelho líquido. | Echo Damage ★*, Dexterity, Summon Duration, Maximum Health | Summon Damage / Armor / Shield Strength | NYX-EC-06 Lingering Echo; NYX-PS-05 Echo Shot |
| Ring of the Unspoken Twin | Ring | A irmã que nunca nasceu. | Echo Damage ★, Critical Chance, Summon Damage, Skill Damage | Critical Damage / Attack Speed / Maximum Health | NYX-ST-06 Echo Mine; NYX-EC-02 Explosive Echo |
| Ring of the Spoken Twin | Ring | A irmã que nasceu duas vezes. | Echo Damage ★, Critical Damage, Summon Duration, Attack Speed | Critical Chance / Storm Damage / Resource Generation | NYX-SS-05 Rewind Step; NYX-PS-08 Marked Prey |
| Shard of the Thousandth Mirror | Relic | O último pedaço do espelho que refletia o mundo inteiro. | Echo Damage ★, Summon Damage, Area Damage, Duration of Effects | Summon Duration / Storm Damage / Status Resistance | NYX-TR-02 Focused Mirror; NYX-EC-08 Spectral Reach |
| Bell That Rings Twice | Relic | Seu som chega antes e depois do toque. | Echo Damage ★, Summon Duration, Projectile Damage, Status Resistance | Summon Damage / Duration of Effects / Storm Damage | NYX-PS-01 Fork Shot; NYX-TR-06 Execution Mirage |

Nota: o par original "Phantom Shot — Echo" aparecia em duas peças; o Bell passou a usar Phantom Shot — Chain para não repetir o mesmo modificador dentro do set.

**3 peças — Mirrored Soul**
- **+1 Echo Clone ativo máximo** e **+50% de duração** de Echo Clone (exceção listada em 08).
- Clones replicam habilidades com **+25 pontos percentuais** de eficiência de replicação sobre o valor base de 08-herois.
- Dano: **≈ +11% efetivo**.

**6 peças — Infinite Reflection** — declara **Echo Recursion 1×** (R07)
- Sempre que Nyx usa uma habilidade, **cada clone ativo a repete após 0,3 s** (Delayed Effect, 07). A repetição é réplica (`IsEcho`, Proc Coefficient 0,4).
- Cada repetição de clone tem **40% de chance de se repetir uma única vez mais** (a cópia final recebe a marca `EchoRecursed` e nunca replica; ainda sujeita a Gen < 2).
- Thousand Reflections dura **+2 s por clone ativo** no momento da conjuração.
- Dano: **≈ +14% efetivo**.

### 8.3 Death's Pursuit

- **Tema:** execução e Critical · **Build:** Phantom Assassin
- **Fantasia:** a caçada termina sempre do mesmo jeito; a presa só não sabe quando.
- **Slots:** Head, Chest, Hands, Feet, Amulet, Relic · **Assinatura:** Critical Damage ★ · **Região-lar:** The Ashen Wastes (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Hood of the Last Hunt | Head | O último rosto que muitos viram. | Critical Damage ★*, Dexterity, Critical Chance, Maximum Health | Cooldown Reduction / Skill Damage / Status Resistance | NYX-PS-08 Marked Prey; NYX-SS-03 Blink Strike |
| Pursuer's Leathers | Chest | Não fazem barulho, nem mesmo quando rasgam. | Critical Damage ★*, Dexterity, Dodge Chance, Maximum Health | Armor / Physical Resistance / Shield Strength | NYX-SS-07 Execution Step; NYX-TR-02 Focused Mirror |
| Gloves of the Quiet Kill | Hands | O gatilho é puxado entre dois batimentos. | Critical Damage ★, Dexterity, Critical Chance, Armor Penetration | Attack Speed / Physical Damage / Storm Damage | NYX-PS-04 Phantom Volley; NYX-ST-03 Overload Trap |
| Boots of the Closing Distance | Feet | A distância entre caçadora e presa sempre diminui. | Critical Damage ★*, Movement Speed, Resource Generation, Dodge Chance | Dexterity / Physical Resistance / Maximum Health | NYX-SS-06 Kinetic Reload; NYX-EC-02 Explosive Echo |
| Locket of the Quarry | Amulet | Guarda o nome de quem será o próximo. | Critical Damage ★, Dexterity, Skill Damage, Damage Reduction | Cooldown Reduction / Physical Resistance / Storm Damage | NYX-PS-06 Void Arrow; NYX-TR-06 Execution Mirage |
| Hunter's Final Arrowhead | Relic | Nunca foi disparada. Está esperando o alvo certo. | Critical Damage ★*, Projectile Damage, Duration of Effects, Storm Damage | Echo Damage / Status Resistance / Area Damage | NYX-ST-01 Tesla Network; NYX-SS-08 Static Wake |

**3 peças — Marked for Death**
- Passar através de ou terminar Shadowstep sobre um inimigo o torna **Quarry** (efeito do set; 1 alvo por vez, 8 s).
- Acertos na Quarry: **+15% Critical Chance** e **+30% Critical Damage** (entram no termo Crit do pipeline, não em More).
- Matar a Quarry **reinicia a recarga de Shadowstep** e concede **+30 Echo Energy**.
- Dano equivalente: **≈ +12% efetivo** em single-target; menor em clear.

**6 peças — Final Pursuit**
- Quarry comum abaixo de **20% de vida** (Elite: **10%**) é **executada** (HitFlag `Execute`, 06) pelo próximo crítico de Phantom Shot. Guardians e chefes são imunes.
- Contra chefes: conversão única de R08 — **+25% Vulnerability abaixo de 20% de vida** enquanto o chefe for a Quarry.
- Execuções disparam Phantom Shot novamente contra o inimigo mais próximo (12 m; proc Gen+1), que se torna a nova Quarry.
- Críticos contra a Quarry causam **+25% More**.
- Dano: **≈ +14% efetivo**.

### 8.4 Balanceamento — Nyx

| Rota | Composição | Dano efetivo estimado | Leitura |
|---|---|---|---|
| A | 6 Legendary independentes | ×1,23–1,30 | Melhor rota para builds de arma específica (Chakrams/Crossbow) com Uniques de comportamento. |
| B | Storm Phantom 6 | ×1,11 × 1,13 = **×1,25** | Recompensa movimento constante; clear rápido. |
| B | Thousand Echoes 6 | ×1,11 × 1,14 = **×1,27** | Maior custo de performance — ver questões abertas. |
| B | Death's Pursuit 6 | ×1,12 × 1,14 = **×1,28** | Melhor single-target do herói; clear inferior. Limiar de execução reduzido de 25% para 20% (R08): estimativa revista de ×1,29 para ×1,28. |
| C | SP 3 + TE 3 | ×1,11 × 1,11 = **×1,23** | Storm + Echo: Resounding Thunder (07). Sobreposição 2 = máxima liberdade de rolagem. |
| C | SP 3 + DP 3 | ×1,11 × 1,12 = ×1,24 | Shadowstep alimenta Momentum e Quarry ao mesmo tempo. |
| C | TE 3 + DP 3 | ×1,11 × 1,12 = ×1,24 | Clones replicam Phantom Shot na Quarry. |

Variação: ×1,23–1,30 (razão 1,057; ±2,8%).

---

## 9. Orun — The Voidkeeper

Habilidades: Void Tendrils, Guardian Fragment, Devouring Rift, Corruption Wave, The Unbound. Recurso: barra de Void Mass que gera Void Constructs (contagem base 3, teto base 6, até 8 com exceções de 08 — R01).

### 9.1 Abyssal Legion

- **Tema:** invocações · **Build:** Void Commander
- **Fantasia:** o arauto de um exército que existe do outro lado do vazio; Orun só abre a porta.
- **Slots:** Head, Chest, Amulet, Ring, Relic ×2 · **Assinatura:** Summon Damage ★ · **Região-lar:** The Shattered Expanse (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Helm of the Abyssal Herald | Head | Fala com vozes que não são do usuário. | Summon Damage ★*, Intelligence, Cooldown Reduction, Maximum Health | Skill Damage / Void Resistance / Status Resistance | ORU-GF-01 Shard Sentinels; ORU-UB-07 Vast Form |
| Legion Mantle | Chest | Cada fio é um soldado que ainda não foi chamado. | Summon Damage ★, Intelligence, Summon Duration, Armor | Maximum Health / Shield Strength / Damage Reduction | ORU-GF-06 Enduring Fragment; ORU-DR-02 Hungry Rift |
| Sigil of Endless Ranks | Amulet | O símbolo de uma legião sem fim. | Summon Damage ★*, Intelligence, Cooldown Reduction, Damage Reduction | Skill Damage / Void Damage / Shield Strength | ORU-UB-02 Unchained Detonation; ORU-VT-06 Construct Conduit |
| Ring of the Hollow Choir | Ring | Canta para o vazio, e o vazio responde. | Summon Damage ★, Void Damage, Resource Generation, Summon Duration | Critical Chance / Maximum Health / Void Resistance | ORU-GF-05 Volatile Construct; ORU-CW-10 Void Undertow |
| Core of the First Construct | Relic | O primeiro construto a obedecer. | Summon Damage ★, Summon Duration, Area Damage, Void Damage | Duration of Effects / Damage Over Time / Status Resistance | ORU-DR-03 Wandering Rift; ORU-GF-09 Crushing Fragment |
| Banner Shard of the Deep | Relic | Pedaço do estandarte que tremula no abismo. | Summon Damage ★, Duration of Effects, Void Damage, Status Resistance | Summon Duration / Area Damage / Ruin Damage | ORU-VT-04 Rending Pull; ORU-UB-09 Insatiable Night |

**3 peças — Legion of Nothing**
- **+1 Void Construct máximo** (base 3 → 4; exceção listada em 08 — R01).
- Construtos causam **+25% More dano**.
- Dano: **≈ +11% efetivo** (construtos respondem por ~45% do dano da build).

**6 peças — Abyssal Command**
- Ao conjurar Corruption Wave, **cada construto ativo a conjura também**, a partir de sua posição, com **50% do dano**. As ondas dos construtos são réplicas (`IsEcho`, Proc Coefficient 0,4) e não replicam (R07).
- Quando um construto expira ou é destruído, gera **2 Voidlings** por 6 s (cada um com 20% do dano do construto; não contam no limite de Void Constructs; entidades Gen+1, sujeitas ao teto de entidades geradas de 11).
- Dano: **≈ +14% efetivo**.

### 9.2 Hollow Sovereign

- **Tema:** Ruin e decadência · **Build:** Corruption Master
- **Fantasia:** o rei de uma corte que apodreceu e não percebeu; Orun carrega a coroa e a podridão.
- **Slots:** Hands, Legs, Feet, Ring ×2, Amulet · **Assinatura:** Damage Over Time ★ · **Região-lar:** The Hollow Depths (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Rotgrasp Gauntlets | Hands | Tudo o que tocam começa a envelhecer. | Damage Over Time ★*, Intelligence, Ruin Damage, Cast Speed | Critical Chance / Resource Generation / Maximum Health | ORU-CW-01 Returning Tide; ORU-VT-08 Withering Grasp |
| Legwraps of Slow Decay | Legs | Bandagens que nunca se soltam, nunca se limpam. | Damage Over Time ★, Intelligence, Area Damage, Maximum Health | Duration of Effects / Void Resistance / Dodge Chance | ORU-DR-10 Rotting Rift; ORU-CW-09 Lingering Rot |
| Sovereign's Rotted Steps | Feet | O chão escurece por onde passam. | Damage Over Time ★*, Movement Speed, Status Resistance, Resource Generation | Intelligence / Maximum Health / Void Resistance | ORU-VT-06 Construct Conduit; ORU-UB-08 Rotting Unbound |
| Ring of Withering | Ring | Murcha a mão de quem não é digno. | Damage Over Time ★, Ruin Damage, Critical Chance, Area Damage | Cast Speed / Maximum Health / Void Resistance | ORU-CW-08 Spreading Blight; ORU-DR-09 Lingering Maw |
| Ring of the Hollow Crown | Ring | Pequeno demais para a coroa que representa. | Damage Over Time ★, Skill Damage, Cast Speed, Void Resistance | Ruin Damage / Resource Generation / Critical Damage | ORU-UB-02 Unchained Detonation; ORU-CW-07 Empowering Wave |
| Hollow King's Pendant | Amulet | Ainda bate. Só não deveria. | Damage Over Time ★*, Intelligence, Ruin Damage, Damage Reduction | Skill Damage / Void Resistance / Cooldown Reduction | ORU-DR-04 Early Collapse; ORU-GF-10 Void-Laced Fragment |

Nota: o par "Corruption Wave — Trigger" do Ring of the Hollow Crown usa ORU-CW-07, não ORU-CW-05 Detonating Wave, porque este duplicaria o 6pc.

**3 peças — Creeping Hollow**
- Decay (07) aplicado por Orun entra com **2 acúmulos por aplicação** e dura **+50%** (o máximo de 5 acúmulos de 07 continua valendo — R49).
- Acertos de Corruption Wave em inimigos com ≥3 acúmulos de Decay aplicam **2 acúmulos de Weakening** (07).
- DoT de Orun causa **+20% More** contra inimigos com ≥3 acúmulos de Decay.
- Dano: **≈ +11% efetivo** + mitigação ofensiva (Weakening).

**6 peças — Sovereign of Rot**
- Corruption Wave **detona** o Decay dos inimigos atingidos: causa instantaneamente **150% do dano restante** do Decay e reaplica **50% dos acúmulos** (arredondado para cima). Compartilha o ICD de Detonation por alvo (07).
- Inimigos que morrem com ≥3 acúmulos de Decay espalham todos os acúmulos para os **2 inimigos mais próximos** a até 5 m (proc Gen+1).
- Dano: **≈ +14% efetivo**.

### 9.3 Eternal Warden

- **Tema:** defesa e sustentação · **Build:** Abyssal Guardian
- **Fantasia:** o guardião do limiar entre o mundo e o vazio; nada passa sem sua permissão.
- **Slots:** Head, Chest, Hands, Legs, Feet, Relic · **Assinatura:** Maximum Health ★ (flat) · **Set defensivo** · **Região-lar:** The Astral Citadel (05 §9.1)

| Peça | Slot | Lore | Fixos | Aberto (1 de 3) | Modificadores |
|---|---|---|---|---|---|
| Warden's Vigil Helm | Head | Seus olhos não fecham há mil anos. | Maximum Health ★, Intelligence, Armor, Void Resistance | Status Resistance / Cooldown Reduction / Health Regeneration | ORU-GF-03 Void Bastion; ORU-VT-05 Lingering Tendrils |
| Bulwark of the Deep Watch | Chest | Forjada para resistir ao que vem do abismo. | Maximum Health ★, Intelligence, Armor, Damage Reduction | Void Resistance / Shield Strength / Summon Damage | ORU-GF-05 Volatile Construct; ORU-UB-02 Unchained Detonation |
| Keeper's Gauntlets | Hands | Seguram a porta fechada, por dentro. | Maximum Health ★, Intelligence, Armor, Resource Generation | Cast Speed / Critical Chance / Void Damage | ORU-VT-01 Grasping Abyss; ORU-CW-07 Empowering Wave |
| Greaves of the Threshold | Legs | Marcam a linha que não deve ser cruzada. | Maximum Health ★, Intelligence, Health Regeneration, Damage Reduction | Physical Resistance / Armor / Dodge Chance | ORU-DR-01 Gravitic Maw; ORU-GF-06 Enduring Fragment |
| Boots of the Unmoved | Feet | Nada as move contra a vontade de quem as calça. | Maximum Health ★, Movement Speed, Status Resistance, Armor | Resource Generation / Void Resistance / Dodge Chance | ORU-VT-06 Construct Conduit; ORU-DR-08 Widening Maw |
| Lantern of the Warden | Relic | Sua luz mostra o caminho de volta. | Maximum Health ★*, Status Resistance, Summon Duration, Duration of Effects | Void Damage / Summon Damage / Area Damage | ORU-UB-09 Insatiable Night; ORU-GF-09 Crushing Fragment |

Nota: Guardian Fragment — Behavior usa ORU-GF-03, não ORU-GF-02 Aegis Link, porque este duplicaria o redirecionamento de dano do 3pc.

**3 peças — Warden's Oath**
- Guardian Fragment aplica **Taunt** (06) em inimigos a até 6 m por 2 s, a cada 6 s.
- **25% do dano recebido por Orun** é redirecionado ao Guardian Fragment ativo.
- Orun ganha **+5% Damage Reduction por construto ativo** (máx. +20%; sujeito ao cap de 40% de 01).
- Void Tendrils têm **+1 s de duração de Root** (sujeito à DR de CC de 06).
- Dano: **≈ +6% efetivo** (Root/Taunt aumentam dano útil) + **≈ +25% EHP**.

**6 peças — Unbroken Covenant**
- Ao cair abaixo de **30% de vida**, Orun consome 1 construto para ganhar escudo de **25% da Vida máxima** por 5 s e aplicar Pull (07; chefes: Distortion) + Root (2 s) em inimigos a até 6 m. Recarga interna: 20 s.
- Enquanto Orun tiver escudo, Void Tendrils causam **+30% More**.
- Guardian Fragment emite pulso de cura de **2% da Vida máxima/s** para Orun a até 4 m.
- Dano: **≈ +10% efetivo** + **≈ +20% EHP** + sustentação.

### 9.4 Balanceamento — Orun

| Rota | Composição | Dano efetivo estimado | EHP | Leitura |
|---|---|---|---|---|
| A | 6 Legendary independentes | ×1,23–1,30 | base | Melhor para híbridos summoner/caster fora dos arquétipos. |
| B | Abyssal Legion 6 | ×1,11 × 1,14 = **×1,27** | base | Dano distribuído; performance depende do pathing dos construtos (09/11). |
| B | Hollow Sovereign 6 | ×1,11 × 1,14 = **×1,27** | +10% (Weakening) | Ramp-up lento, teto alto em lutas longas. |
| B | Eternal Warden 6 | ×1,06 × 1,10 = ×1,17 | **+45%** | Tanque do jogo base. |
| C | AL 3 + HS 3 | ×1,11 × 1,11 = **×1,23** | +10% | Void + Ruin (Entropic Collapse, 07); sobreposição 2. |
| C | AL 3 + EW 3 | ×1,11 × 1,06 = ×1,18 | +25% | Construto extra alimenta Damage Reduction por construto — sinergia direta. |
| C | HS 3 + EW 3 | ×1,11 × 1,06 = ×1,18 | +35% | Controle + atrito; mais seguro em Ascension alta. |

Rotas ofensivas: ×1,23–1,30. Rotas com Eternal Warden ficam abaixo em dano e compensam em EHP pela taxa de conversão (decisão 11).

---

## 10. Orçamento e verificação da regra de ouro

### 10.1 Modelo da Rota A (recalculado com 02)

- A diferença entre as rotas está em **6 slots**: na Rota A eles têm 6 Legendary independentes (cada um com efeito próprio); nas Rotas B/C eles têm peças de set (sem efeito) e o build ganha 2 Set Powers. Os outros 4 slots não-arma e as armas são iguais em todas as rotas (os melhores efeitos individuais vão para esses slots em qualquer rota).
- 02 §3.13 limita cada efeito Legendary a **até 15% de ganho efetivo** (More ≤ 15%). Esse é um teto, não a média: a maioria dos efeitos é de comportamento/utilidade, e os 6 slots marginais da Rota A recebem o 5º ao 10º melhor efeito disponível.
- Premissa de balanceamento: ganho marginal médio de **3,5% a 4,5% por efeito** → Rota A = 1,035⁶ a 1,045⁶ = **×1,23 a ×1,30**.
- Rotas B/C ofensivas: **×1,23 a ×1,28**. Razão máxima entre quaisquer rotas ofensivas de um herói: 1,30 / 1,21 = **1,074** → todas dentro dos ±10% da regra de ouro (00 §4).
- **Guarda-corpo:** se o simulador de 11 mostrar a Rota A acima de ×1,35 (efeitos marginais perto do teto de 15%), a correção é reduzir o teto de efeito Legendary de 02 em slots de armadura para 10%, não alterar os sets.
- Uniques (até +40% More condicional) cabem em qualquer rota, nos 4 slots comuns; por isso não entram na comparação.

### 10.2 Resumo de dano por set

| Set | 3pc (nominal / efetivo) | 6pc (nominal / efetivo) | Bucket |
|---|---|---|---|
| Ashen Sovereign | +25% / +12% | +30% / +14% | Ganho-como-extra Ember (3pc), More (6pc) |
| Ruinbreaker | +20% / +12% | +20% / +13% (chefe: +25% Vulnerability <20%) | Vulnerability (3pc), More (6pc) |
| Eternal Bastion | — / +8% | +20% contra-ataques / +10% | More |
| Frozen Empress | +25% / +11% | +20% / +14% | More |
| Void Oracle | +20% / +11% | Implosion / +14% | More |
| Arcane Paradox | +24% / +12% | Paradox Surge / +13% | More |
| Storm Phantom | até +60% em 1 acerto / +11% | +25% / +13% | More (consumo no 3pc) |
| Thousand Echoes | replicação +25 pp / +11% | repetições / +14% | Réplicas |
| Death's Pursuit | Crit / +12% | +25% / +14% (chefe: +25% Vulnerability <20%) | Crit (3pc), More (6pc) |
| Abyssal Legion | +25% construtos / +11% | ondas extras / +14% | More |
| Hollow Sovereign | +20% DoT / +11% | detonação / +14% | More |
| Eternal Warden | — / +6% | +30% Tendrils / +10% | More |

"Efetivo" = ganho médio de dano em uma run típica considerando uptime, clear e chefe. Nenhum Set Power passa de +30% More nominal sustentado (decisão 11). A participação dos Set Powers no orçamento de 00 §4 (~20%) corresponde a ×1,23–1,28.

---

## 11. Renomeações desta rodada (R48)

| Antes | Depois | Motivo |
|---|---|---|
| Crown of Cinders (Head, Ashen Sovereign) | Diadem of the Ash Throne | Nome reservado a 02 |
| Heart of the Burning Throne | Pendant of the Burning Throne | Confusão com o material Heart of [Chefe] (10) |
| Heart of the Hollow King | Hollow King's Pendant | Idem |
| Event Horizon (6pc Void Oracle) | Oracle's Collapse | Nome reservado a 07 |
| Singularity (efeito do 6pc Void Oracle) | Collapsar | Evita confusão com LYR-VO-01 Singularity Core (03) |
| Loop of the Horizon | Loop of the Far Edge | Afasta de Event Horizon (07) |
| Fracture (efeito de Ruinbreaker) | Acúmulos de Vulnerability (07) | R12: um único status de Vulnerability |
| Shard of the First Fracture | Shard of the First Fault | Acompanha a remoção de Fracture |
| Ruin Detonation | Verdict Shockwave | Evita confusão com o status Detonation (07) |
| Last Stand (6pc Eternal Bastion) | Unfallen Resolve | Colide com o Unique Ring of the Last Stand (02) |
| Perfect Guard | Parry (06) | R20 |
| Overflow (6pc Arcane Paradox) | Paradox Surge | Colide com Overflow de 08 |
| Static (acúmulos de Storm Phantom) | Momentum | Evita confusão com Static Field (07) |
| Overloaded (6pc Storm Phantom) | Overcharged | Evita confusão com Overload (07) |
| Mark / Marked (Death's Pursuit) | Quarry | Evita confusão com Echo Mark (07) e Marked Prey (03) |
| Locket of the Marked | Locket of the Quarry | Idem |
| Anchored Boots | Boots of the Unmoved | Colide com o serviço Anchor da Rest Room (10) |

---

## 12. Dependências e pontos de integração

| Doc | Consome deste documento | Fornece a este documento |
|---|---|---|
| 00-convencoes | — | R01, R03, R07, R08, R12, R20, R21, R23–R28, R32, R48 |
| 01-atributos | Lista de atributos por peça; Faixa Nomeada [75–100%] / ★ [90–115%] | Tabelas de tier (§3.9.2), multiplicadores de slot (§3.9.3), upgrade (§3.9.5), primários por herói |
| 02-equipamentos | 72 peças como `ItemDefinition` `ItemKind = SetPiece` com `setId`; Keystone of the Sundering (definido em 02) | Pool de afixos por slot (§3.9.3), teto de efeito Legendary (§3.13) usado no modelo da Rota A |
| 03-modificadores | 144 IDs de modificador usados pelas peças | Catálogo, índice Habilidade × Categoria (§10.2), Proc Coefficients |
| 05-loot | Sets são Hero-bound e não caem em Normal | Região-lar (§9.1), Attunement, pity e proteção de duplicata |
| 06-combate | Execute como HitFlag, Parry, contra-ataques, crit na Quarry, Taunt, ganho-como-extra | Janela de Parry, DR de CC, regra de área (√), pipeline |
| 07-resonance | Uso de Burning, Chill, Freeze, Frostbound, Shatter, Pull/Distortion, Implosion, Shock, Chain Lightning, Delayed Effect, Decay, Weakening, Vulnerability, Detonation | Definições, buildups, teto de Vulnerability (+60%), modelo Generation |
| 08-herois | Overrides: Arcane Charges +1, Void Constructs +1, Echo Clone +1, replicação +25 pp, Ashen Guard +1 carga; Echo Recursion 1× de Infinite Reflection | Tabela de exceções de recurso (R01), valores base de clones, construtos e Fury |
| 09-inimigos | Execute contra Elites (10%); imunidade de Guardians e chefes; Taunt de Guardian Fragment | Afixos de Elite que resistem a Taunt/Pull |
| 10-progressao | Sets não são alterados por serviços além de Blacksmith/Enchanter; Set Focus na tela de Builds; Set Locker | Blacksmith, Enchanter (reroll do Afixo Aberto), Attunement |
| 11-arquitetura | `SetDefinition` (6 peças, 2 `SetPowerDefinition`), budget de 2 poderes com prioridade, contagem por ID distinto, flag `EchoRecursion1x` só em Thousand Echoes 6pc, Voidlings fora do limite de construtos | `SetTracker`, eventos de equip/unequip, `EffectSource`, simulador de rotas |
| UI (GDD §11) | Poderes inativos com motivo; contribuição de peça para set na comparação; seleção de Set Focus | — |

---

## 13. Conflitos com as convenções

Nenhum conflito em aberto. Registros:

1. **Budget de 2 Set Powers** é regra própria deste doc, compatível com o GDD (§6.6 regras 7 e 8) e implementada pelo `SetTracker` de 11.
2. **Fracture removida (R12):** Ruinbreaker usa os acúmulos de Vulnerability de 07. A tabela de fontes de Vulnerability de 07 ainda cita "Fracture" como fonte; deve passar a tratá-la como Ruin Vulnerability (ajuste de 07).
3. **Marked for Death** concede Critical Chance/Damage, que entram no termo Crit do pipeline, não em More.
4. **Afixos fora do pool (`*`):** a assinatura de vários sets (afinidade, Critical Damage, Summon Damage, Damage Over Time, Armor ou Resource Generation) não está no pool de 02 para alguns slots; em todos os casos é o único afixo `*` da peça, como R23 permite.
5. **Bônus de consumo de Storm Phantom (até +60% em um acerto)** fica acima do teto de +30% sustentado da decisão 11; é declarado como exceção de consumo e o efetivo (≈ +11%) está dentro do orçamento.

---

## 14. Questões abertas para playtest

1. **Uptime de Ashen Ascension** (gasto de 150 Fury, recarga de 12 s): validar ~50–60%; ajustar o limiar de Fury, não a duração.
2. **Performance de Thousand Echoes 6pc:** com 2 clones e 40% de Echo Recursion 1×, o pior caso é ~5 instâncias de uma habilidade em 0,6 s. Medir custo de VFX e legibilidade (GDD §12); se necessário, reduzir a escala visual das repetições, não o efeito.
3. **Void Oracle em chefe isolado** (~×1,21 estimado): confirmar se os adds dos chefes (09) bastam para manter o Collapsar relevante.
4. **Eternal Bastion e Eternal Warden:** validar a taxa de conversão 1% de dano ≈ 1,5% EHP em Ascension 50+; se tanques ficarem obrigatórios ou inúteis, ajustar a taxa, não os sets individualmente.
5. **Keystone of the Sundering:** se a taxa de uso passar de 60% nas Rotas B/C, reduzir suas faixas de atributo para o piso de 70%.
6. **Rota A no simulador (11):** confirmar a premissa de 3,5–4,5% de ganho marginal por efeito Legendary; acima de ×1,35, aplicar o guarda-corpo de §10.1.

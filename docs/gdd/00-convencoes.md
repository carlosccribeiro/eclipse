# PROJECT ECLIPSE — Convenções compartilhadas dos documentos de design (GDD v0.3)

Este arquivo é o contrato entre os 11 documentos de detalhamento (seção 18 do GDD v0.2). Todo documento deve respeitar as decisões abaixo. Se um documento precisar contrariar algo aqui, deve registrar isso explicitamente na seção "Conflitos com as convenções" com justificativa — nunca contrariar em silêncio.

## 1. Formato

- Idioma: português. Termos de jogo em inglês, exatamente como no GDD (ex.: Fury, Set Powers, Resonance, Cleaving Strike).
- Unidades: tempo em segundos (s), distância em metros (1 unidade Unity = 1 m), percentuais com `%`.
- Arquivos: `docs/gdd/NN-slug.md`.
- Estrutura obrigatória de cada documento:
  1. Objetivo e escopo
  2. Decisões tomadas (lista curta, numerada, com justificativa de 1 linha)
  3. Corpo do design (tabelas, fórmulas, catálogos)
  4. Dependências e pontos de integração (quais docs consomem/fornecem o quê)
  5. Conflitos com as convenções (se houver)
  6. Questões abertas para playtest (só o que realmente exige teste; decisões de design devem ser tomadas, não adiadas)

## 2. Dono de cada conceito

Um conceito é definido em detalhe apenas pelo seu dono. Os demais documentos referenciam (`ver 06-combate §X`) e podem usar valores provisórios coerentes com este contrato.

| Conceito | Dono |
|---|---|
| Fórmulas de atributos, caps, diminishing returns, efeito dos primários, Armor/Resist, **tabelas de faixas de rolagem por tier de iLvl, modelo de rolagem (Roll01), multiplicadores de slot, Faixa Nomeada** | 01-atributos |
| Itens nomeados, bases de item (dano/Armor implícito usando fórmulas de 01), **pool de afixos permitidos por slot e pesos**, compatibilidade/hero-bound, Uniques | 02-equipamentos |
| Modificadores de habilidade (catálogo, compatibilidade, regras de stacking) | 03-modificadores |
| 12 conjuntos, peças, Set Powers | 04-conjuntos |
| Drop rates, fontes de loot, raridade por região/dificuldade, item level, smart loot, pity | 05-loot |
| Pipeline de dano, crit, estados genéricos (stun, stagger, knockback, root, slow genérico, silence, parry, taunt, stealth, unstoppable, intangível, execute), DR de CC, input, frame data, hit detection, linguagem de telegraph, letalidade, TTK, game feel | 06-combate |
| Efeitos de status por afinidade (Burning, Freeze, Shock…), acúmulos, combinações de Resonance | 07-resonance |
| Kit completo dos heróis, passivas, recursos, progressão de herói, armas por herói | 08-herois |
| Arquétipos de inimigos, afixos de Elite, chefes, **estrutura do mapa da run (linhas/nós)**, tipos de sala, eventos, **catálogo de Shrines/Blessings**, tokens de ataque, conteúdo de inimigos do endgame | 09-inimigos |
| Dificuldades, Monster Level (eLvl), meta-progressão, **curva de XP e tempo de jogo**, economia e moedas, regra de morte, extração, **serviços da Rest Room**, Satchel, serviços do Sanctuary, endgame | 10-progressao |
| Arquitetura Unity, ScriptableObjects, eventos, save, integração | 11-arquitetura |

## 3. Parâmetros globais fixados

### 3.1 Progressão e escala
- **Nível de herói:** 1–50, permanente, por herói (não por run). Detalhes de ganho de XP e recompensas por nível: 08-herois / 10-progressao.
- **Item Level (iLvl):** 1–100. Define faixas de rolagem disponíveis. Determinado pela região + dificuldade (05-loot).
- **Dificuldades:** 4 tiers de campanha — **Normal, Veteran, Nightmare, Eclipse** — seguidos de **Ascension 1–100** como escalada de endgame (10-progressao).
- **Regiões:** ordem do GDD; The Ashen Wastes é a primeira, The Astral Citadel a última.
- **Run de região:** alvo de 25–35 min, ~12–16 salas + chefe, com mapa de nós ramificado (09-inimigos define geração; 10-progressao define regras de entrada/saída).

### 3.2 Pipeline de dano (canônico — 06-combate detalha, os demais se encaixam)

```
DanoFinal = Base
          × (1 + Σ Increased%)          // aditivo: atributos de equipamento (Skill/Elemental/Area/Projectile/DoT/Physical Damage etc.)
          × Π (1 + More%)               // multiplicativo: Set Powers, efeitos Legendary/Unique, Resonance, passivas-chave
          × Crit                        // 1 ou (1 + Critical Damage)
          × (1 − Mitigação do alvo)     // Armor / resistências, após Armor Penetration
          × (1 + Vulnerability%)        // debuffs no alvo (ex.: Ruin)
```

- Atributos de equipamento em % entram **sempre** no bucket aditivo "Increased".
- Fontes "More" são raras, nomeadas e orçadas (cada doc deve declarar o More% que introduz).
- Base = dano da arma ou coeficiente da habilidade × dano da arma.

### 3.3 Recursos dos heróis
- Kael **Fury** (0–100, gera ao bater/receber dano, decai fora de combate).
- Lyra **Arcane Charges** (cargas discretas, base 3, máximo absoluto 6).
- Nyx **Echo Energy** (0–100, regenera rápido, gasto em mobilidade/ecos).
- Orun **Void Constructs** (contagem de construções ativas, base 3).
- Os atributos genéricos `Maximum Resource`, `Resource Generation` e `Resource Cost Reduction` são traduzidos por herói (01-atributos define a tradução numérica; 08-herois define o comportamento).

### 3.4 Tags de habilidade (vocabulário único)
Usadas por modificadores, atributos, Set Powers, Resonance e arquitetura:

`Attack, Spell, Melee, Ranged, Projectile, Area, DoT, Summon, Construct, Movement, Defensive, Ultimate, Channel, Trap, Clone, Basic, Heavy`
+ afinidades: `Ember, Frost, Storm, Void, Echo, Ruin` + `Physical`.

Novas tags só podem ser adicionadas por 11-arquitetura ou registradas como conflito.

### 3.5 Itens
- Raridades e contagem de atributos/modificadores: exatamente como o GDD (Common 2/0, Rare 3/0, Epic 4/1, Legendary 5/2). Unique = subcategoria de Legendary.
- 12 slots conforme GDD.
- Set items são sempre Legendary; 6 peças; bônus em 3 e 6.

### 3.6 Estados genéricos x status de afinidade
- Estados genéricos (Stun, Stagger, Knockback, Root, Slow genérico, Silence, i-frames, Super Armor): 06-combate.
- Status de afinidade (Burning, Freeze, Shock, Pull, Repeat, Decay etc.): 07-resonance.
- Freeze e Stun compartilham a regra de imunidade/diminishing returns definida por 06-combate.

## 4. Orçamento de poder (guia para balanceamento)

Referência de poder ofensivo relativo de um personagem bem equipado no fim da campanha (Eclipse, iLvl ~80), para que nenhum sistema domine:

| Fonte | Participação alvo no dano |
|---|---|
| Arma + nível + primários | ~30% |
| Atributos de equipamento (Increased) | ~25% |
| Modificadores de habilidade | ~15% (majoritariamente comportamento, não números) |
| Set Powers ou Legendary individuais | ~20% |
| Resonance | ~10% |

Regra de ouro (GDD §15): 6 peças de um set ≈ 3+3 híbrido ≈ Legendaries individuais, com variação máxima de ±10% de poder em cenário médio — a diferença deve estar no estilo, não no número.

## 5. Registro de resoluções de integração (rodada 1)

Decisões do lead após a auditoria cruzada dos docs 01–11. Têm precedência sobre o texto de qualquer doc. Cada item indica os docs que precisam se ajustar (→).

### 5.1 Ajustes ao contrato
- **R01 Recursos:** teto base 6 para Arcane Charges e Void Constructs; Keystones/Augments/itens podem elevar até **8**, desde que listados na tabela de exceções de 08. Orun usa a barra **Void Mass** que gera Void Constructs (contagem base 3). → 08 (incluir Totem na lista de exceções).
- **R02 Flags ≠ tags:** `Unavoidable`, `Unblockable`, `Execute`, `Knockdown`, `IsEcho`, `IsResonance`, `IsTriggered` são **HitFlags** de DamageInstance (11), não tags. Vocabulário §3.4 inalterado. → 01, 06, 09, 11.
- **R03 More:** nenhum Augment dá More. Keystone ≤ +20% More sustentado; Ultimate ≤ +30% More durante sua duração. Legendary ≤ +15% More por efeito; Unique ≤ +40%. Bônus contra chefe por Execute e Broken não são More (ver R08, R11). → 02, 04, 08.

### 5.2 Combate (dono 06)
- **R04 Simulação:** 60 Hz fixos. → 11.
- **R05 Input:** LMB básico, RMB forte, Space esquiva, **Q/E/R/Shift** = 4 habilidades, **F** = Ultimate, **1** = Aether Flask. Gamepad definido por 06. → 06.
- **R06 Esquiva:** igual para todos: 2 cargas, 4,5 m, i-frames frames 2–13, recarga 1,5 s. → 08 (remover variação do Orun).
- **R07 Eco e procs:** modelo Generation de 07. Evento original = Gen 0; cada proc (gatilho, Resonance, chain, explosão) gera Gen+1; **nada dispara em Gen ≥ 2**. Réplicas (IsEcho) nunca geram novas réplicas, **exceto** poderes que declaram "Echo Recursion 1×" (Thousand Echoes 6pc / Infinite Reflection, Echo Chamber): a réplica pode replicar uma única vez, ainda sujeita a Gen < 2. Proc coefficient: repetição Echo 0,5; cópia de clone 0,4; tick de área persistente 0,25. → 03, 06, 07, 11 (corrigir off-by-one: triggers param em Gen ≥ 2), 04, 08.
- **R08 Execute:** limiar máximo 20% em inimigos comuns; Elites = metade (máx. 10%); Guardians e chefes imunes. Contra chefes, qualquer efeito de Execute vira **+25% Vulnerability abaixo de 20% de vida** (uma única conversão, não acumula entre fontes). → 03, 04 (Death's Pursuit), 06, 08 (Death Blossom 25%→20%; Heavy Blade respeita o teto).
- **R09 DR de CC:** regra única de 06 (grupo Hard inclui Stun e Freeze; janela 8 s: 100/50/25%/imune). No jogador: Hard CC máx. 1,5 s e Freeze máx. 1,0 s, seguido de 2 s de imunidade. 07 remove imunidades próprias. → 07, 09, 11.
- **R10 CC em chefe:** convertido em Stagger (06), com duas exceções nomeadas definidas por 07 e incorporadas em 06: **Frostbound** (Freeze) e **Distortion** (Pull, +10% Vulnerability em vez de More). → 06, 07, 09 (renomear seu próprio "Frostbound").
- **R11 Broken de chefe:** valores de 06 — dura 4 s, **+30% Vulnerability** (conta no teto), limiar da barra +50% por Break, máx. 3×. → 09.
- **R12 Vulnerability:** teto total **+60%** (07 dono). Inclui Staggered, Broken, Distortion e conversão de Execute. Ruin = stacks (4% × 5); outras fontes dão valor fixo por fonte, sem acumular consigo mesmas. → 06, 03, 04, 08 (valores ficam, mas sujeitos ao teto).
- **R13 Mitigação:** 06 adota 01: Block −40%, cap de Damage Reduction 40%, Armor `Armor/(Armor + 50·eLvl + 500)`. `Unavoidable` ignora Dodge Chance e Block, **mas i-frames evitam**; só ataques marcados **Annihilation** exigem sair da área. → 01, 06.
- **R14 Telegraphs:** linguagem de 06. Vermelho = ataque evitável padrão (preenche do centro para fora); roxo = controle; branco-dourado = Unblockable; ciano = janela interrompível; laranja reservado a Ember. Wind-up mínimo 0,45 s, redução máx. −15% por dificuldade. → 09.
- **R15 Letalidade:** tabela de 06 (golpe comum em Normal 4–6% da vida; chefe 20–30%; exceção Annihilation). → 09.
- **R16 TTK/duração:** sala 60–90 s; Guardian 30–60 s; chefe 3–4 min (Normal) até 5 min (Eclipse, com fase extra); soft enrage em 2× a duração alvo. Exceção aceita: o pinnacle opcional The Sundering Heart tem duração alvo de 6 min. → 06, 09.
- **R17 Tokens de ataque:** tabela de 09 (melee 3→5, ranged 2→4; Elites não consomem token); 06 cita; 11 lê por dificuldade. → 06, 11.
- **R18 Armas:** 08 define moveset (nº de golpes, alcance, comportamento); 06 ajusta suas tabelas de frame data para os movesets de 08; 02 define o número de DPS (±5% entre armas do mesmo herói; área −10% contra alvo único; especialistas +5%). → 06, 08 (cita 02).
- **R19 DoT:** tick 0,5 s (06). 0,25 s em 11 é só granularidade do lote. → 07, 11.
- **R20 Estados genéricos:** 06 define Unstoppable, Taunt, Stealth, Parry (unifica Parry/Perfect Guard/bloqueio perfeito: janela 0,2 s), Intangível, Slow genérico, Execute. → 06; 03, 04, 08 citam.

### 5.3 Itemização
- **R21 Tiers e faixas:** 01 é dono. T1 = iLvl 1–20 … T5 = 81–100; tabela fixa por tier; Roll01 uniforme dentro do tier (dificuldades altas podem elevar o piso do Roll01, exceção registrada em 01). HP, Armor e Regen são flat. → 02 (inverter numeração, remover multiplicador 40–100%), 04, 05 (remover `IlvlScale`), 11.
- **R22 Multiplicadores de slot:** tabela única em 01, incorporando Main Hand ×1,5 em dano% e Amulet ×1,5 em resistências. → 01, 02.
- **R23 Pool por slot:** 02 é dono (incluindo primário: máx. 1 por item, Amulet 2, nenhum em Ring). 01 alinha seus pesos. Peças de set podem ter no máximo 1 afixo fora do pool, marcado `*`. → 01, 04.
- **R24 Faixa Nomeada:** piso 70% e teto 115% do máximo do tier. → 01, 02.
- **R25 Estrutura de item:** Legendary nomeado = 4 atributos fixos + 1 Afixo Aberto (3 opções) + 2 modificadores fixos + 1 efeito. Peça de set = 5 atributos (4 fixos + 1 Aberto) + 2 modificadores, sem efeito próprio. Unique = 5 fixos + 2 modificadores (pode ter 1 modificador Signature exclusivo) + efeito. → 02, 03, 04, 05.
- **R26 Modificadores:** 03 é dono dos nomes. 03 cria os modificadores que faltam para cobrir os 33 pares Habilidade×Categoria usados em 04; 02 e 04 remapeiam todos os nomes para IDs existentes em 03; universais de 02 viram `GEN-xx`. Conceito Signature fica em 03 e é adotado por 02. → 03 (onda 1), 02 e 04 (onda 2).
- **R27 Hero-bound:** o item com modificador de habilidade de herói não pode ser equipado por outro herói. Epic: 75% modificador do herói / 25% `GEN-xx`. → 03, 05.
- **R28 Uniques:** meta de 30 no lançamento: 15 de chefe (3 por chefe), 4 Pinnacle (10), 11 de pool regional/Challenge (incluindo Keystone of the Sundering). 02 expande o catálogo de 16 para 30 com o campo `fonteUnique`. Uniques não caem em Normal (minDropILvl 35); peças de set não caem em Normal. → 02, 05, 10.
- **R29 Off Hand:** sempre ativo, sem bloqueio por nível; nomes de 02 com "Codex" renomeado para **Grimoire** (colide com o Codex do Archivist). Nenhum slot é bloqueado por nível. → 01, 05, 08.
- **R30 Diversos:** coleta automática ao limpar a sala (05) → 01. Smart loot de primários: Core ×2 / Secundário ×1 / Terciário ×0,5 → 05. Status Potency vira atributo ofensivo em 01 → 01, 02, 07. Armor implícito de 02 usa fórmula de 01 → 02. Exemplo numérico de 01 refeito com Weapon Power → 01. Limite de modificadores ativos por herói: 14 (03 recalcula o orçamento) → 03. Tradução de recurso do Orun em 03 cita 01 → 03.
- **R31 Upgrade e Primal:** upgrade +0..+10, +3% por nível sobre atributos e implícito, igual para todas as raridades; modificadores não sobem. Ascended é fundido em **Primal** (05 dono; A50+, rolagens 85–100%, nunca acima do máximo). → 01 (orçamento com itens +6), 05, 10.
- **R32 Região-lar:** cada set tem uma região-lar (05). Storm Phantom vai para The Shattered Expanse; Void Oracle para The Hollow Depths; 05 rebalanceia a distribuição 2/2/3/2/3. → 05, 04 (cita 05).

### 5.4 Run, economia e progressão
- **R33 Estrutura da run:** 09 é dono. 13 linhas + chefe (14 salas), Estratos 1–5 / 6–9 / 10–13; Guardian fixo na linha 9; pelo menos um nó Rest garantido na linha 5; Antecâmara (Rest) na linha 13. → 05, 10, 11.
- **R34 Rest Room:** 10 é dono. Serviços: cura 40% grátis, recarrega o Aether Flask, Rift Anchor (extração), Anchor (proteger 1 item), Temper, Purificar e subida paga de rank de Blessing (Rift Shards). A Antecâmara oferece os mesmos serviços. "Attune" deixa de existir na Rest. → 09.
- **R35 Blessings:** 09 é dono do catálogo. Ranks Lesser/Greater/Exalted (70/25/5); More ≤ 8% por Blessing, ≤ 20% no total; poder total da run +20–30%. A "Eclipse Blessing" de 10 vira uma Exalted. Escala dos inimigos calibrada para +25%. → 10.
- **R36 Moedas (10 dono):** Gold, Aether Dust, Sundered Cores, Pillar Fragments, Eclipse Marks, Memory Fragments e **Rift Shards** (única moeda da run). Resonant Essence sai (substituída por Aether Dust/Sundered Cores). "Aether (material)" = Aether Dust. **Heart of [Chefe]** entra em 10 como material específico de chefe (invoca Boss Echoes e troca pelos Uniques daquele chefe). Material de região sai. Eventos de 09 trocam "Gold de run" por Rift Shards. → 05, 09, 10.
- **R37 Satchel:** 30 espaços. → 10.
- **R38 iLvl por dificuldade:** valores de 05 (Normal 1–24, Veteran 25–49, Nightmare 50–69, Eclipse 75–84, Ascension 85–100), com **iLvl 100 alcançado em A90**. → 05, 10.
- **R39 Attunement:** um único sistema (nome reservado a target farming). Objeto: Rift Sigil + 2 Pillar Fragments (10); efeito: peso ×3 no set/Unique escolhido (05), só Eclipse+. Sigil of Attunement consumível sai. Enchanter renomeia seu "Attune" para **Imbue**. → 05, 09, 10.
- **R40 Progressão de herói:** 10 é dono da curva: XP = 200·L^1,8; nível 50 em ~40–50 h; catch-up −10%/+100%. Desbloqueios de 08: Nyx após Ashen Colossus, Orun após Pale Sovereign. → 08, 10, 05 (níveis esperados por dificuldade).
- **R41 Afixos de Elite por dificuldade:** 09 é dono. → 10.
- **R42 Enchanter:** remove a categoria "Legendary não nomeados"; adiciona reroll do Afixo Aberto entre suas 3 opções. → 10.
- **R43 Endgame:** 09 cria seção curta com Nightmare Behaviors, Eclipse Phenomena, Boss Echoes, Eclipse Trial, Sundering Heart e Overrift Herald; Distortions de 10 citam o conteúdo de 09 em vez de duplicar. Ruinous Pact passa a ser "inimigos causam +X% de dano". → 09, 10.
- **R44 eLvl:** 10 define o Monster Level por região × dificuldade, alinhado ao iLvl de drop. → 10; 01, 09 citam.
- **R45 Quantidade de loot:** bônus de quantidade são aditivos, teto ×2,0 no total (Peril, Overrift e afins inclusos). → 05, 10.
- **R46 Valores faltantes em 10:** Echo of a Fallen Eclipsed devolve 50% dos Rift Shards e 1 item perdido; sem revive (exceto Blessing específica de 09); item removido do jogo é compensado em Aether Dust por raridade. → 10.

### 5.5 Arquitetura e nomes
- **R47 Arquitetura:** 11 adota as 6 fases de 03 (Conversão → Forma → Parâmetros → Replicação → Gatilhos → Recurso); orçamento de performance de 11 (120 ativos, pico 200, 2.000 projéteis) é o alvo técnico, e 06 remove o seu; incorpora os hooks de 06; amplia o save (contadores de Enchant/Refine, Anchor, Primal, seed/fonte, pity por herói, Set Locker, última run); SetTracker implementa 04 (máx. 2 Set Powers, Set Focus, Keystone); resolver alinhado a 06 (Flat, conversão, ganho-como-extra); remove `Override` sem consumidor; IA perto 10 Hz / longe 4 Hz (09 cita); tipos de sala seguem 09; opacidade de VFX segue 06. → 06, 09, 11.
- **R48 Nomes reservados (dono mantém, outros renomeiam):** Frostbound, Brittle, Event Horizon, Frozen Armor → 07. Unstoppable, Stagger/Poise → 06. Unstable Orb → 03. Crown of Cinders → 02. Codex → 10. "Guardian" é o termo único para mini-chefe (06, 07, 09). Sigla "DR" reservada a Diminishing Returns; Damage Reduction se escreve por extenso. → 02, 03, 04, 08, 09.
- **R49 Status de 07 usados por outros:** Decay máx. 5 stacks também em inimigos (09 Rotting Herald); Ultimates podem aplicar Freeze até 3 s em não-chefes (exceção declarada em 07, Absolute Zero); Overload Circuit pode exceder o máximo de stacks em +2 (exceção declarada em 07). → 07, 08, 09.
- **R51 Ajustes aprovados na varredura final (aprovados pelo autor do projeto):** Fan of Chains (Burning Chains, Kael) atinge em 360°; o modificador de Shattered Reality (Lyra) concede zona persistente de 3 s. → 03.
- **R52 Núcleo testável fora da Unity (decisão de início do desenvolvimento):** `Eclipse.Core` não referencia `UnityEngine` nem pacotes Unity (nem Mathematics/Collections); usa matemática própria. É compilado também por um projeto `dotnet` (`tools/CoreTests`) para testes rápidos, além dos EditMode tests. Burst/Jobs ficam em `Eclipse.Gameplay`. → 11.
- **R50 Velocidade base:** valores por herói de 01 (Orun 5,8 m/s); 09 e 08 citam 01. → 08, 09.

### 5.6 Lacunas numéricas (módulo D02, aprovado pelo autor do projeto em 2026-11-07)
Cálculos e premissas: `docs/roadmap/reports/D02-calculos.md`.
- **R53 Curva base do Standard (06 dono):** `HP_Std(eLvl) = 36 × 1,048^(eLvl − 1)`; `DanoBase(eLvl) = 20 × 1,048^(eLvl − 1)` até eLvl 24 e `58,8 × 1,016^(eLvl − 24)` acima; `Dano_Std = DanoBase × M_let` (Normal 1,0 · Veteran 1,2 · Nightmare 1,5 · Eclipse 2,0). Modificador de vida por tier de campanha = 1. Ascension N: a sala usa o HP/dano da sala equivalente de Eclipse × 1,035^N (vida) e × 1,02^N (dano); o eLvl real de Ascension vale só para o K de Armor (01). DPS de referência do jogador: `DPS_ref(eLvl) = HP_Std(eLvl) / 2,2 s` (usado por 09 §8.1 e pelo simulador de 11). → 06, 09, 10.
- **R54 Letalidade vence o dano relativo:** a tabela de letalidade de 06 (R15) prevalece; a coluna "Dano relativo" de 10 §7.2 passa ao resultado de R53: Veteran ≈2,4 · Nightmare ≈4,3 · Eclipse ≈7,9 · Ascension 7,9 × 1,02^N. A vida relativa de R53 (≈3,2 / 8,9 / 22) é aceita como cumprimento de 3 / 9 / 25. → 10.
- **R55 Tank:** HP× do arquétipo Tank cai de 4,0 para **2,5** (TTK 4–7 s com R53; alvo Heavy de 06). Os tanks nomeados de 09 §5 escalam na mesma proporção (×0,625): Kiln Brute e Stormglass Golem 2,5; Glacial Reliquary e Abyssal Carapace 2,8; Pillar Guardian 3,1. → 09.
- **R56 XP por Ponto de Ameaça (10 dono):** `XP_por_PA = 2 × NívelDeConteúdo(eLvl)^1,1 ÷ MultDensidade(dificuldade)`, com `NívelDeConteúdo` interpolado de (eLvl 1 → 1), (24 → 25), (49 → 35), (69 → 45), (84 → 50) e 50 acima. Elites ×3 (09 §10.5); Guardian vale 60 PA e chefe 150 PA (no eLvl da sala). → 10, 09.
- **R57 Rift Shards (10 dono):** inimigos dão **0,2 Rift Shard por PA** (Elites ×3), sem escala por dificuldade; Treasure e Events mantêm os valores fixos de 10 §3.3. Fator de dificuldade da conversão em Gold: **Normal 1 · Veteran 3 · Nightmare 9 · Eclipse 19 · Ascension 19 × (1 + 0,02N)**. → 10, 09.
- **R58 Cap por alvo de efeitos baseados em dano causado/recebido (03 §6.3):** ≤ 50% do coeficiente base da habilidade, por alvo e por ativação. KAE-BC-03 Linked Torment ≤ 60% DA por acorrentamento; LYR-SR-04 Collapsing Reality: implosão ≤ 175% DA por alvo. Fragmentos de LYR-VO-04 Rime Core duram **2 s**. → 03 (aplicar após o merge do D01).

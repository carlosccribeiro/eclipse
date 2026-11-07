# 10 — Progressão, Economia e Endgame

**Dono de:** dificuldades, Monster Level (eLvl), meta-progressão, curva de XP e tempo de jogo, economia e moedas, regra de morte, extração, serviços da Rest Room, Run Satchel, serviços do hub (The Sanctuary), estrutura do endgame.
**Não é dono de (cita):** estrutura do mapa da run, catálogo de Blessings/Shrines, afixos de Elite e conteúdo de inimigos do endgame (09-inimigos); iLvl de drop, pity, Primal e drop rates (05-loot).
**Revisão:** rodada 1 de integração aplicada (00-convencoes §5: R28, R31, R33–R46).
**Referencia:** 00-convencoes (parâmetros globais), 01-atributos, 02-equipamentos, 05-loot, 08-herois, 09-inimigos, 11-arquitetura.

---

## 1. Objetivo e escopo

Definir como o jogador fica mais forte e mais capaz **entre** e **durante** as incursões, o que se perde ao morrer, como a economia de moedas circula no Sanctuary, como as dificuldades são desbloqueadas e o que sustenta o jogo depois da campanha (endgame de longo prazo).

Princípio central: **a meta-progressão amplia opções, não obriga números.** Poder permanente vem de equipamento (que o jogador escolhe), nível de herói (curto e finito) e aprimoramento de itens. Account Rank, Codex e NPCs desbloqueiam sistemas, conteúdo, escolhas e conveniência — nunca atributos passivos obrigatórios.

---

## 2. Decisões tomadas

1. **A morte nunca apaga um Legendary nem progresso de XP.** Perder Legendaries contradiz o pilar "loot significativo"; a tensão vem de moedas, de itens não-Legendary e do poder temporário da run.
2. **Itens não-Legendary não protegidos são convertidos em materiais (50%) ao morrer, não deletados.** Perda sentida, mas sempre há algum retorno.
3. **Extração antecipada existe (Rift Anchor nas Rest Rooms).** Cria a decisão risco × recompensa do GDD §3.2 sem punir quem prefere segurança.
4. **Após o chefe, a opção "Delve Deeper" (Overrift) implementa o "continuar a exploração" do GDD §3.1.** O loot do chefe é garantido; só o loot do Overrift fica em risco.
5. **Poder temporário de run = Blessings (catálogo de 09) + Rift Shards**, totalmente perdido ao fim da run. Mantém a identidade roguelite sem competir com o equipamento. Rift Shards são a **única** moeda da run.
6. **Dificuldades são desbloqueadas por conta, não por herói.** Heróis novos têm catch-up de XP; repetir a campanha linear por herói seria atrito sem valor.
7. **Em Veteran+ as 4 primeiras regiões ficam livres; The Astral Citadel exige derrotar os outros 4 chefes naquela dificuldade.** Liberdade de farm + clímax narrativo preservado.
8. **6 moedas permanentes + 1 temporária + materiais de chefe (Heart of [Chefe])**, cada um com fonte, uso e sink que continuam relevantes no endgame. Conversão descendente→ascendente (Dust → Cores) impede moeda morta.
9. **Upgrade no Blacksmith nunca falha**, tem o mesmo teto (+10, +3%/nível) para todas as raridades e pode ser transferido entre itens do mesmo slot. Modificadores e efeitos não sobem. RNG de falha gera frustração sem decisão.
10. **Enchanter permite alterar apenas UM atributo por item (lock)** com custo crescente e plateau; Legendary nomeado/set/Unique só rerolam o valor (**Imbue**) e o Afixo Aberto entre suas 3 opções. Rolagens importam, mas "perfeito" não é obrigatório (GDD §15.8).
11. **Account Rank (1–100) desbloqueia sistemas, slots e escolhas; não dá atributos.**
12. **Codex (Archivist) é progressão funcional:** descobrir um item o torna alvo de farm direcionado (Attunement) e de transmog.
13. **Ascension 1–100 é por conta; recorde por herói.** Push de dificuldade com regras novas a cada limiar (Thresholds), não só multiplicadores.
14. **Endgame tem 5 pilares:** Ascension + Rift Sigils, Boss Echoes, Endless Sundering, Challenge Altar (diário/semanal/trials) e o pinnacle The Sundering Heart.
15. **Loot exclusivo de endgame não cria raridade nova:** Boss Echoes têm chance elevada dos 3 Uniques de cada chefe; o pinnacle tem 4 Uniques Pinnacle; a marca de qualidade de topo é **Primal** (definida por 05). Não existe flag Ascended.
16. **Sem temporadas com reset.** Jogo single-player; conteúdo rotativo via **Eclipse Cycles** (calendário local, sem perda de progresso).
17. **Respec de passivas e habilidades é gratuito no Sanctuary.** Experimentação de build é pilar; custo de respec só pune curiosidade.
18. **Monster Level (eLvl) acompanha o iLvl de drop** por região × dificuldade (§7.2). Uma tabela só alimenta Armor (01), escala de inimigos (06/09) e drops (05).
19. **Rest Room é serviço de 10, sala de 09:** cura 40% grátis, recarrega o Aether Flask, Rift Anchor, Anchor e Temper; a Antecâmara oferece os mesmos serviços.
20. **Bônus de quantidade de loot são aditivos, com teto total ×2,0** (Peril, Overrift, Boons e afins).
21. **Sem revive.** A única forma de sobreviver a dano fatal é a Blessing *Second Wind* de 09. Revanche após a morte vem do evento Echo of a Fallen Eclipsed (§4.1).

---

## 3. Estrutura da incursão e poder temporário

### 3.1 Formato da run (estrutura definida por 09-inimigos §11; aqui só as regras de progressão)

| Estrato (09) | Linhas | Pontos de progressão relevantes para este doc |
|---|---|---|
| I | 1–5 | Introdução da região; **linha 5 tem pelo menos um nó Rest garantido** (Clareira) — primeira oportunidade de Rift Anchor |
| II | 6–9 | Guardian fixo na linha 9 |
| III | 10–13 | Encontros mais densos; **Antecâmara (Rest) fixa na linha 13**, última oportunidade de extração |
| Chefe | 14ª sala | Recompensa garantida e loot "secured" |
| Overrift (opcional) | +5 | Só após o chefe; escalada de risco/recompensa (§3.5) |

- Total: 13 linhas + chefe = **14 salas**. Alvo: 25–35 min sem Overrift; +8–10 min com Overrift.
- O jogador pode trocar equipamento a qualquer momento fora de combate.
- **Run Satchel:** **30 slots** (valor de 05-loot). Equipamentos são coletados automaticamente ao limpar a sala (05). Filtro opcional de auto-salvage para Common/Rare (configurável por raridade e por slot). Satchel cheio: o item de menor raridade é salvado automaticamente (Aether Dust creditado na hora).

### 3.2 Blessings (Shrine Rooms)

O catálogo, os ranks e o orçamento das Blessings são de **09-inimigos §12**. Este doc define só como a meta-progressão interage com elas.

Resumo do que vale (de 09):
- Ranks **Lesser / Greater / Exalted**, oferta 70% / 25% / 5% (Exalted só a partir do Estrato II).
- Lesser e Greater usam o bucket Increased; só Exalted pode ter fonte **More**: ≤ +8% por Blessing, ≤ +20% no total.
- Alvo agregado: **+20–30% de poder efetivo** ao chegar no chefe. A escala de inimigos dentro da run é calibrada para **+25%**.
- Esperado: 5–7 Blessings por run. Blessings carregam tags de afinidade e participam das condições de Resonance (07).

Interações de meta-progressão (deste doc):
- Oferta: escolher 1 entre 3; **4 opções** a partir de Account Rank 30 (§5.2).
- Rerolls de oferta de Shrine: 25 Rift Shards (+25 a cada uso) ou gratuitos via Account Rank 8/40.
- Elevar o rank de uma Blessing (Lesser → Greater, ou Greater → Exalted a partir do Estrato II) é feito pelo serviço **Temper** da Rest Room (§3.4) ou por eventos de 09.
- A antiga "Eclipse Blessing" deste doc foi absorvida pelo rank Exalted de 09. Este doc **não introduz fonte More**.

### 3.3 Rift Shards (moeda temporária)

- **Única moeda da run.** Todo evento, aposta, oferta ou compra dentro da run (09 §14) usa Rift Shards, nunca Gold.
- Fonte: salas de combate (5–15), Elites (20–30), Treasure Rooms (40–60), Events.
- Usos dentro da run: Wandering Merchant (evento), reroll de oferta de Shrine (25, +25 a cada uso), serviços da Rest Room, escolhas de eventos de 09.
- Ao fim: **Vitória/Extração** → saldo convertido em Gold (1 Shard = 5 Gold × fator de dificuldade). **Morte** → perdido (50% pode ser recuperado pelo evento Echo of a Fallen Eclipsed, §4.1).

### 3.4 Rest Room — serviços (dono: este doc)

Vale para todo nó Rest de 09, incluindo a Clareira (linha 5) e a **Antecâmara** (linha 13), que oferece exatamente os mesmos serviços. Todos podem ser usados na mesma visita.

| Serviço | Custo | Efeito |
|---|---|---|
| Recuperar | Grátis (1× por Rest) | Cura **40%** da vida máxima |
| Recarregar Flask | Grátis, automático | Recarrega todas as cargas do Aether Flask (06) |
| Anchor Item | 50 Shards por item, máx. 2 por run (3 com Account Rank 50) | O item fica protegido da conversão por morte |
| Rift Anchor (extrair) | Grátis | Encerra a run com retenção total, sem recompensa de chefe |
| Temper | 40 Shards (reroll) / 80 Shards (elevar) | Reroll de 1 Blessing Lesser em outra Lesser, **ou** eleva 1 Blessing em um rank (Greater → Exalted só a partir do Estrato II) |
| Purificar | Substitui o Recuperar | Remove 1 Curse de run (09 §13) |

Não existe serviço "Attune" na Rest Room (o nome Attunement é reservado ao target farming, §8.2).

### 3.5 Delve Deeper — Overrift

Após derrotar o chefe, um portal oferece 5 salas extras:
- Cada sala soma +1 Ascension equivalente de dificuldade em relação à run e **+15% de quantidade de loot cumulativo** (aditivo com os demais bônus; teto total ×2,0 — 05).
- Na sala 5 há um Elite "Overrift Herald" (projetado por 09) com drop garantido de Epic+ (05-loot define a tabela).
- O loot do chefe já está garantido. Morrer no Overrift aplica a regra de morte **somente ao loot obtido no Overrift**.

---

## 4. Regra de morte e retenção

A morte encerra a incursão (GDD §3.1). Tabela canônica:

| Elemento | Vitória (chefe) | Extração (Rift Anchor) | Morte |
|---|---|---|---|
| Itens equipados | Mantidos | Mantidos | Mantidos |
| Legendary no Satchel | Mantidos | Mantidos | **Mantidos** |
| Epic/Rare/Common no Satchel | Mantidos | Mantidos | Convertidos em materiais a 50% do valor de salvage (Normal: 75%) |
| Itens com Anchor | Mantidos | Mantidos | Mantidos |
| Gold, Aether Dust, Sundered Cores coletados | 100% | 100% | 50% (Normal: 75%) |
| Pillar Fragments | 100% | 100% | 100% |
| Heart of [Chefe] | Concedido | — (só pelo chefe) | — (só pelo chefe) |
| Eclipse Marks | 100% | 100% | 0% (só concedidas ao concluir) |
| Rift Shards | Convertidos em Gold | Convertidos em Gold | Perdidos |
| Blessings | Encerradas | Encerradas | Encerradas |
| XP de herói e de Account Rank | 100% | 100% | 100% |
| Descobertas do Codex | Sempre | Sempre | Sempre |
| Rift Sigil usado | Consumido | Consumido | Consumido |
| Recompensa e first-kill do chefe | Sim | Não | Não |

Justificativa: o jogador sempre sai com algo (XP, Codex, Legendaries), mas morrer custa economia e itens intermediários. A decisão "extrair ou arriscar o chefe" é real nas Rest Rooms (Clareira na linha 5 e Antecâmara na linha 13).

**Sem revive.** Não há revive, ressurreição nem "segunda vida" de meta-progressão. A única exceção é a Blessing *Second Wind* de 09 §12 (1× por run).

### 4.1 Echo of a Fallen Eclipsed (evento de 09 §14)

- O save guarda a **última run terminada em morte por região** (por herói): build, Rift Shards perdidos e a lista de itens convertidos.
- Vencer o evento devolve, **na run atual: 50% dos Rift Shards perdidos** naquela morte e **1 item** convertido (o de maior raridade; empate → o mais recente), restaurado com suas rolagens originais.
- O registro é consumido ao aparecer o evento (vencer ou fugir), ou substituído por uma nova morte na mesma região.

### 4.2 Itens removidos do jogo

Se uma atualização remover a definição de um item (StableId sem definição — 11), cada instância é convertida, na carga do save, em Aether Dust pelo valor de salvage do item × 3 (§6.3) e, se Legendary, + 2 Sundered Cores. Uma notificação no Sanctuary lista o que foi convertido. Upgrades investidos são devolvidos em 100% do Gold e Dust gastos.

---

## 5. Progressão permanente

### 5.1 Nível de herói (1–50)

- Permanente por herói. Respec de passivas e habilidades gratuito no Sanctuary (08-herois define o que cada nível concede).
- **Dono da curva: este doc** (08 aplica as recompensas por nível). XP para o próximo nível: `XP(L) = 200 × L^1.8` (total até 50 ≈ 3,9 M XP).
- Curva alvo: nível ~25 ao fim da campanha Normal, ~35 ao fim de Veteran, ~45 ao fim de Nightmare, **50 em ~40–50 h**, por volta da entrada em Eclipse. 05-loot usa esses níveis para validar o nível exigido dos itens (ceil(iLvl × 0,5)).
- **Kindred Echo (catch-up):** heróis com nível inferior a (maior nível da conta − 10) recebem **+100% XP** (limiar −10 níveis / bônus +100%; substitui o −5 / +75% proposto em 08). Reduz o custo de experimentar heróis novos sem pular o aprendizado do kit.
- Após o nível 50, XP converte em **Hero Mastery** (§5.4).

### 5.2 Account Rank (1–100)

XP de conta vem de todas as fontes de XP de herói (1:1) + bônus de primeiras conquistas, Codex e desafios. Curva: `AXP(R) = 3000 × R^1.5`. Rank 100 ≈ 300 h de jogo.

Recompensas (nenhuma é atributo passivo):

| Rank | Desbloqueio |
|---|---|
| 2 | 2ª aba do Vault (grátis) |
| 5 | Loadouts: 5 → 6 por herói |
| 8 | Shrine Reroll grátis: 1 por run |
| 12 | Path Insight: ver 1 nó adicional à frente no mapa da run |
| 15 | Filtros avançados de auto-salvage |
| 20 | Escolha do Shrine inicial: a 1ª Shrine da run sempre oferece uma Blessing da afinidade do herói |
| 25 | Loadouts: 6 → 8 por herói |
| 30 | Shrines oferecem 4 opções |
| 40 | Shrine Reroll grátis: 2 por run |
| 50 | Anchor Item: +1 por run (máx. 3) |
| 60 | Transmog de efeitos de habilidade (variações de cor por afinidade) |
| 75 | Título e aura cosmética |
| 100 | Cosmético "Eclipsed" de conta + moldura de retrato |

Ranks intermediários dão cosméticos, Gold e Eclipse Marks.

### 5.3 Desbloqueio de heróis (Keeper)

| Herói | Requisito |
|---|---|
| Kael | Início |
| Lyra | Início |
| Nyx | Derrotar The Ashen Colossus (região 1) em qualquer dificuldade |
| Orun | Derrotar The Pale Sovereign (região 2) em qualquer dificuldade |

Requisitos definidos por 08-herois. Começar com dois heróis (melee e caster) mostra o contraste de estilos desde a primeira hora; os outros dois chegam cedo na campanha (≈2 h e ≈5 h), a tempo de o jogador escolher seu principal antes do Veteran.

### 5.4 Hero Mastery (pós-nível 50)

- Níveis ilimitados; cada nível exige 250.000 XP.
- Recompensas: cosméticos do herói, títulos, molduras e **Build Trials** (§8.4). Nenhum atributo.
- Marcos (10/25/50/100) liberam skins de arma por arquétipo de build.

### 5.5 Restauração do Sanctuary (níveis de NPC)

Cada NPC tem níveis 1–5. Subir de nível exige um marco (dificuldade/chefe) + custo único em Gold. Níveis liberam **serviços**, não números passivos.

| NPC | Nv 1 | Nv 2 | Nv 3 | Nv 4 | Nv 5 |
|---|---|---|---|---|---|
| Blacksmith | Salvage, upgrade até +4 | Upgrade +6, Upgrade Transfer | Upgrade +8, Reforge | Upgrade +10 | Rekindle |
| Enchanter | — (desbloqueia após chefe 2) Enchant de atributo, Imbue, reroll de Afixo Aberto | Refine de modificador | Reveal: ver 3 opções ao Enchant (em vez de 2) | Lock-swap (§6.4) | Primal Enchant |
| Merchant | Compra/venda, buyback 12 | Aether Caches | Estoque Epic, Heart Exchange | Oferta semanal de Legendary | Caches direcionadas por afinidade |
| Archivist | Codex básico | Bestiary tier 2 | Attunement de Sigils | Lore Chapters especiais | Codex completo + transmog total |
| Rift Navigator | Regiões | Dificuldades | Rift Sigils | Endless Sundering | Pinnacle |

Marcos de referência: Nv 2 = concluir campanha Normal; Nv 3 = Veteran completo; Nv 4 = Nightmare completo; Nv 5 = Ascension 30. Custos: 5.000 / 25.000 / 100.000 / 300.000 Gold.

---

## 6. Economia

### 6.1 Moedas

| Moeda | Tipo | Fontes | Usos | Sink de longo prazo |
|---|---|---|---|---|
| **Gold** | Permanente | Drops, venda de itens, conversão de Rift Shards, fim de run | Taxas do Blacksmith/Enchanter, Merchant, Vault, níveis de NPC | Enchant com custo crescente, Aether Caches, Vault |
| **Aether Dust** | Permanente | Salvage de qualquer item | Upgrade de item, Enchant de Common/Rare, conversão em Cores | Upgrade +7 a +10, conversão |
| **Sundered Cores** | Permanente | Salvage de Epic (30%: 1) e Legendary (2), Elites em Nightmare+ (10%), chefes (3–5) | Upgrade +6 em diante, Enchant de Epic/Legendary, Reforge, Rekindle | Enchant e Refine |
| **Pillar Fragments** | Permanente, rara | Chefes de região (ver §6.6), Overrift Herald, Boss Echoes | Upgrade +9/+10 de Legendary, Rekindle, Attunement de Sigil, oferta semanal do Merchant | Attunement e Rekindle |
| **Eclipse Marks** | Permanente, endgame | Rift Sigils (5 × Peril), Daily Rift, Weekly Trial, Build Trials, Endless Sundering | Set Caches, Sigils especiais, chaves de Boss Echo, cosméticos | Set Caches |
| **Memory Fragments** | Colecionável (não gasto) | Events, Archivist, Treasure Rooms | Completam capítulos de Lore no Codex | — (progressão, não moeda gasta) |
| **Rift Shards** | Temporária, **única moeda da run** | Salas da run, eventos | Serviços da Rest Room, Shrines, eventos e Wandering Merchant | Zerada ao fim da run (convertida em Gold na vitória/extração) |
| **Heart of [Chefe]** (5 tipos) | Material de chefe | 1 por abate do chefe correspondente a partir de Veteran (2 em Eclipse+; first-kill de cada dificuldade +1) | Chave de Boss Echo daquele chefe (§8.3); **Heart Exchange** no Merchant (§6.5): 8 Hearts → 1 Unique daquele chefe à escolha, já descoberto no Codex | Boss Echoes e Heart Exchange |

**Rift Sigils** são itens (não moeda) — §8.2.

**Nomes canônicos (R36):** "Aether" como material = **Aether Dust**. Não existem *Resonant Essence*, material de região, Sigil of Attunement nem "Gold de run". Salvage de Epic dá Aether Dust + chance de Sundered Core; salvage de Primal dá 3 Sundered Cores adicionais.

**Conversões (Enchanter, com perda):** 100 Aether Dust → 1 Sundered Core; 1 Sundered Core → 40 Aether Dust; 10 Sundered Cores → 1 Pillar Fragment (limite de 3 por semana). A conversão ascendente com perda garante que nenhuma moeda acumule sem uso; o limite semanal protege a raridade dos Fragments.

### 6.2 Fatores de escala de custo

- `R` (raridade): Common 1, Rare 1,5, Epic 2,5, Legendary 4.
- `S` (escala por iLvl) = `1 + iLvl / 25` → iLvl 25 = 2; iLvl 50 = 3; iLvl 80 = 4,2; iLvl 100 = 5.
- Renda e custos escalam juntos por iLvl, então a proporção "custo de upgrade / renda por run" se mantém estável entre dificuldades.

### 6.3 Blacksmith

**Salvage**

| Raridade | Aether Dust | Outros |
|---|---|---|
| Common | 2 × S | — |
| Rare | 5 × S | — |
| Epic | 15 × S | 30% de chance de 1 Sundered Core |
| Legendary | 40 × S | 2 Sundered Cores |
| Primal (Legendary) | 40 × S | 2 + 3 Sundered Cores |

**Upgrade (+0 a +10)**

- Cada nível: **+3% nos valores rolados de todos os atributos e do implícito** do item (no máximo +30% em +10). Os valores aprimorados continuam no bucket Increased (fatia "Atributos de equipamento" do orçamento; 01 calibra presumindo itens a +6).
- **Teto igual para todas as raridades: +10** (limitado só pelo nível do Blacksmith). A raridade muda o custo (`R`), não o teto.
- **Modificadores de habilidade e efeitos especiais não sobem com upgrade.** Em +10 o item recebe só a borda visual "Tempered".
- O upgrade nunca leva um valor acima do que o tooltip mostra como "aprimorado"; a qualidade de rolagem (%, ★, Perfect — 05) é sempre calculada sobre o valor-base, antes do upgrade.
- Nunca falha.

Custo do nível `L` (de L−1 para L):

```
Gold  = 30 × L² × R × S
Dust  = 3 × L^1.5 × R × S
Cores = (L − 5) × (Common/Rare 1 | Epic 1 | Legendary 2)   // apenas L ≥ 6
Pillar Fragments = 1 em +9, 2 em +10                        // apenas Legendary
```

Exemplo — Legendary iLvl 80 (R × S = 16,8):

| Nível | Gold | Dust | Cores | Fragments |
|---:|---:|---:|---:|---:|
| +1 | 504 | 50 | — | — |
| +3 | 4.536 | 262 | — | — |
| +5 | 12.600 | 564 | — | — |
| +6 | 18.144 | 741 | 2 | — |
| +8 | 32.256 | 1.140 | 6 | — |
| +10 | 50.400 | 1.594 | 10 | 2 |
| **Total +0→+10** | **≈194.000** | **≈7.200** | **30** | **3** |

Alvo de pacing: +6 num Legendary bom ≈ 3 runs de Eclipse; +10 ≈ 13 runs (meta de longo prazo, não requisito).

**Upgrade Transfer:** transfere o nível de upgrade para outro item do **mesmo slot** e raridade igual ou superior, pagando 25% do Gold investido. O item de origem volta a +0. Trocar de item nunca "desperdiça" investimento.

**Reforge:** rerola todos os valores numéricos do item dentro das faixas do seu iLvl, preservando identidade, upgrade e o lock de Enchant. Custo fixo (sem escalada): `2000 × R × S` Gold + 3 Cores (Legendary: 5 Cores + 1 Fragment).

**Rekindle (só Legendary):** eleva o iLvl de um item ao iLvl máximo de drop atual do jogador e rerola os valores nas faixas novas. Custo: `3000 × S(novo)` Gold + 5 Cores + 3 Pillar Fragments. Permite que um favorito da campanha acompanhe o endgame, ao custo de uma moeda rara.

### 6.4 Enchanter

Todo Legendary é nomeado (02), então os serviços se dividem por estrutura de item (00 §5, R25):

| Serviço | Itens elegíveis | Efeito |
|---|---|---|
| **Enchant** | Common, Rare, Epic | O atributo escolhido é trocado por outro do pool válido do slot (02). 2 opções novas (3 no Enchanter Nv 3) + manter o atual |
| **Imbue** | Legendary, peças de set, Unique | A lista de atributos é identidade: rerola **apenas o valor** de 1 atributo fixo dentro da faixa. 2 valores + manter o atual |
| **Reroll de Afixo Aberto** | Legendary e peças de set | Troca o Afixo Aberto por outra das **3 opções** definidas pelo item (02), com valor novo. Mostra as 2 opções restantes + manter |

- **Lock:** o primeiro atributo alterado por Enchant ou Imbue torna-se o único alterável por esses serviços naquele item. O Reroll de Afixo Aberto tem lock próprio (o Afixo Aberto) e não consome o lock de Enchant/Imbue. O Enchanter Nv 4 permite **Lock-swap**: trocar o atributo travado uma vez por item, com o contador de custo zerado e taxa fixa de 5 × o custo base.
- Contador `n` por item e por serviço (Enchant/Imbue compartilham; Afixo Aberto e Refine têm o seu), começando em 0.

```
Gold  = 400 × R × S × 1.2^min(n, 12)        // plateau em ~8,9× a partir da 13ª tentativa
Cores = Epic 1 | Legendary 2                 // Common/Rare: 20 × S Aether Dust no lugar
```

Legendary iLvl 80: 1ª tentativa 6.720 Gold; 6ª ≈ 16.700; plateau ≈ 59.900.

Reroll de Afixo Aberto: custo = 1,25 × fórmula acima.

**Refine de modificador (Enchanter Nv 2):** rerola só o **valor numérico** de um modificador de habilidade dentro da faixa de 03 (o efeito é fixo — GDD §6.5; nunca acima do máximo). Lock e contador próprios, separados do Enchant/Imbue. Custo = 1,5 × fórmula acima, Cores × 2.

**Primal Enchant (Nv 5):** em itens **Primal** (05), permite um segundo Imbue em outro atributo (lock próprio). Não eleva nenhum valor acima do máximo da faixa. Último sink de alto nível.

### 6.5 Merchant

- **Venda:** Common 5 × S, Rare 12 × S, Epic 40 × S, Legendary 120 × S Gold. Buyback dos últimos 12 itens vendidos.
- **Estoque rotativo** (renova a cada retorno ao Sanctuary): 6 bases Common/Rare (Epic a partir do Nv 3) no iLvl máximo de drop do jogador. Preço = 4 × valor de venda.
- **Aether Caches (Nv 2):** escolha do slot; `1.500 × S` Gold; garante Rare+. Odds provisórias (05-loot define): Rare 84%, Epic 15%, Legendary 1%.
- **Oferta semanal (Nv 4):** 1 Legendary não-set, já descoberto no Codex, por `5.000 × S` Gold + 4 Fragments.
- **Caches por afinidade (Nv 5):** como Aether Caches, mas o item gerado tem garantia de ≥1 atributo da afinidade escolhida; custo ×2.
- **Heart Exchange (Nv 3):** 8 Heart of [Chefe] do mesmo chefe → 1 dos 3 Uniques daquele chefe (02/09) à escolha, desde que já descoberto no Codex, no iLvl máximo de drop atual. Complementa o pity de 05 (Unique garantido no 10º abate) com um caminho determinístico.

### 6.6 Renda esperada por run (provisória — calibração com 05-loot)

| Dificuldade | Gold | Aether Dust | Sundered Cores | Pillar Fragments (chefe) |
|---|---:|---:|---:|---:|
| Normal | 800 | 150 | 0–1 | 1 só no first-kill |
| Veteran | 2.500 | 350 | 1–3 | 1 |
| Nightmare | 7.000 | 600 | 3–5 | 1–2 |
| Eclipse | 15.000 | 900 | 5–8 | 2–3 |
| Ascension N | 15.000 × (1 + 0,02N) | 900 × (1 + 0,01N) | 6–10 | 3 + ⌊N/20⌋ |

### 6.7 Vault e loadouts

- **Vault (por conta):** 1 aba de 60 slots no início; até 8 abas (480). Aba 2: Account Rank 2 (grátis). Abas 3–8: 10.000 / 25.000 / 50.000 / 100.000 / 200.000 / 400.000 Gold.
- **Set Locker:** armazenamento dedicado e gratuito com 1 vaga por peça de set (72). Duplicatas vão para o Vault. Com 72 peças de set no jogo, sem isso o Vault vira gerenciamento de estoque.
- **Materiais e moedas** não ocupam slots.
- **Loadouts:** 5 por herói (até 8 com Account Rank). Um loadout guarda referências a itens + configuração de habilidades/passivas; trocar de loadout no Sanctuary é instantâneo e grátis.

### 6.8 Saúde da economia

- Todo sink relevante escala com S, então moedas não perdem valor em dificuldades altas.
- Gold tem três sinks sem teto (Enchant, Caches, Vault) e Dust tem conversão; nenhuma moeda fica inútil (GDD §10.1).
- Pillar Fragments são o gargalo intencional do topo (+9/+10, Rekindle, Attunement): gastá-los é sempre uma escolha entre caminhos.
- Telemetria de playtest: razão `custo de upgrade +6 / renda por run` deve ficar entre 2,5 e 4 em todas as dificuldades.

---

## 7. Dificuldades e desbloqueios

### 7.1 Tiers de campanha

Afixos de Elite por dificuldade: **09 §6.2** (dono). iLvl de drop: **05-loot §4** (dono), copiado aqui.

| Tier | Requisito | Nível recomendado | iLvl de drop (05) | Mudanças de regra (não só números) |
|---|---|---|---|---|
| **Normal** | Início | 1–25 | 1–24 (por região) | Elites com 1 afixo; morte retém 75%; sem Uniques nem peças de set |
| **Veteran** | Derrotar The First Forgotten em Normal | 20–35 | 25–49 | Elites com 2 afixos; Challenge Rooms com variantes novas; regiões 1–4 livres; peças de set passam a cair |
| **Nightmare** | Derrotar The First Forgotten em Veteran | 30–45 | 50–69 | Elites com 2 afixos (25% de 3); **Nightmare Behaviors** (09 §Endgame); matilhas Elite adicionais (09 §6.2); eventos novos |
| **Eclipse** | First Forgotten em Nightmare + **Eclipse Trial** (Challenge Altar) | 45–50 | 75–84 (todas as regiões) | Elites com 3 afixos; **Eclipse Phenomena** por região (09 §Endgame); chefes com fase extra; drop de Rift Sigils |
| **Ascension 1–100** | Concluir os 5 chefes em Eclipse | 50 | 85–100 (A1 = 85; +1 iLvl a cada 6 níveis; **iLvl 100 em A90**) | Escalada contínua + Thresholds (§7.3) |

Regras de região:
- **Normal:** regiões em sequência; cada uma exige o chefe da anterior.
- **Veteran+:** regiões 1–4 livres; The Astral Citadel exige os outros 4 chefes **naquela dificuldade**.
- **Eclipse Trial:** boss rush curto (3 chefes em versão reduzida) com uma Distortion fixa. Verifica se a build está pronta, para que o primeiro contato com Eclipse não seja só uma parede.

### 7.2 Monster Level (eLvl) e escala de inimigos

**Monster Level (eLvl) — dono: este doc.** Todo inimigo de uma sala tem eLvl igual ao **`iLvl_sala`** de 05 §4.2 (faixa da região × dificuldade, interpolada pelo índice da sala na run). É o `eLvl` usado na fórmula de Armor de 01 e na curva de stats base de 06. Guardians e chefes usam o topo da faixa (`Faixa.max`).

| Dificuldade | Ashen Wastes | Frozen Cathedral | Shattered Expanse | Hollow Depths | Astral Citadel |
|---|---:|---:|---:|---:|---:|
| Normal | 1–6 | 6–10 | 11–15 | 16–20 | 21–24 |
| Veteran | 25–30 | 30–35 | 35–40 | 40–45 | 45–49 |
| Nightmare | 50–54 | 54–58 | 58–62 | 62–66 | 66–69 |
| Eclipse | 75–84 (todas as regiões) |||||
| Ascension N | Até A90: igual ao iLvl base de 05 (85 → 100). **Acima de A90:** `eLvl = 100 + (N − 90)` → A100 = eLvl 110, só para escala (o iLvl de drop fica em 100) |||||

- A faixa é sempre cópia de 05 §4.1: se 05 mudar uma faixa, esta tabela segue (o eLvl é sempre igual ao `iLvl_sala`).
- O eLvl acima de 100 em A91–A100 confirma o previsto por 01 ("eLvl efetivo em Ascension pode passar de 100").

**Escala relativa por tier (alvo resultante).** A curva de HP/dano base por eLvl é de **06-combate**; os multiplicadores abaixo são o alvo de poder relativo que a combinação "curva por eLvl × modificador de tier" deve produzir, já contando com o ganho de **+25% das Blessings** na run (00 §5, R35). 06 calibra a curva; 09 usa os stats relativos por arquétipo sobre ela.

| Tier | Vida relativa | Dano relativo |
|---|---:|---:|
| Normal | 1× | 1× |
| Veteran | 3× | 1,6× |
| Nightmare | 9× | 2,5× |
| Eclipse | 25× | 4× |
| Ascension N | 25 × 1,035^N | 4 × 1,02^N |

O dano cresce mais devagar que a vida de propósito: o Ascension alto deve exigir otimização ofensiva e execução, não gerar one-shots (respeitando a tabela de letalidade de 06).

### 7.3 Ascension

- **Desbloqueio:** concluir Ascension N libera N+1. De A1 a A20, cada conclusão libera +2 (onboarding rápido).
- Desbloqueio por conta; o **maior Ascension concluído é registrado por herói** (recordes, Mastery, títulos).
- Qualquer região pode ser jogada no Ascension desbloqueado.

**Thresholds (regra nova a cada limiar):**

| Ascension | Threshold |
|---|---|
| A1 | Rift Sigils com até 2 Distortions |
| A10 | Sigils com até 3 Distortions; Endless Sundering |
| A20 | Boss Echoes tier II |
| A30 | Acesso ao pinnacle **The Sundering Heart** |
| A40 | Sigils com até 4 Distortions |
| A50 | Itens **Primal** passam a dropar (05); Elites com 35% de chance de 4 afixos (09 §6.2) |
| A60 | Toda run tem 1 Distortion obrigatória aleatória (além das do Sigil) |
| A75 | Boss Echoes tier III |
| A80 | Elites sempre com 4 afixos (09 §6.2) |
| A90 | iLvl 100; 2 Distortions obrigatórias |
| A100 | Título e cosmético "Eclipsed"; leaderboard pessoal "Beyond" (ranking de tempo) |

---

## 8. Endgame

### 8.1 Visão

O endgame tem que ampliar a variedade de builds (GDD §10.2). Cada atividade testa algo diferente:

| Atividade | O que testa | Recompensa principal |
|---|---|---|
| Ascension + Rift Sigils | Escala e adaptação a regras | iLvl alto, Marks, Primal |
| Boss Echoes | Execução contra padrões novos | Uniques do chefe com chance elevada, Hearts, Pillar Seals |
| Endless Sundering | Sustentação e risco | Marks, Fragments, recordes |
| Challenge Altar | Variedade e restrição | Marks, cosméticos, Codex |
| The Sundering Heart | Domínio completo | Cosméticos de topo, Unique pinnacle |

### 8.2 Rift Sigils (modificadores de incursão)

Dropam de Elites e chefes em Eclipse+. Um Sigil é aplicado no Rift Navigator antes da run e contém:
- **1–4 Distortions** (negativas, limitadas pelo Threshold) que somam **Peril**;
- **0–2 Boons** (positivas, de recompensa);
- **região** opcional (Sigil regional) ou livre.

**Recompensa por Peril:** cada ponto de Peril dá +8% de quantidade de loot (aditivo; teto total ×2,0 — 05 §3), +3% na chance de Legendary da fonte (05 aplica, dentro do teto ×2,0 de chance de Legendary) e 5 Eclipse Marks ao concluir.

**Catálogo inicial de Distortions (regras, não só números).** Quando uma Distortion reaproveita conteúdo de 09 (afixo, Room Modifier ou Curse), ela **cita** a definição de 09 em vez de redefini-la:

| Distortion | Peril | Efeito |
|---|---:|---|
| Unstable Gravity | 2 | Fendas Void abrem periodicamente e puxam o jogador |
| Echoing Hostility | 3 | Todas as Elites recebem o afixo **Reverberant** (09 §6) além dos seus |
| Withered Light | 2 | Regeneração de vida desativada; Rest Rooms dão escudo em vez de cura |
| Ember Tide | 2 | Inimigos mortos deixam poças Ember por 3 s |
| Hollow Hunger | 2 | Inimigos mortos soltam orbes que curam outros inimigos se não forem coletados |
| Fractured Map | 1 | Tipos de sala ficam ocultos no mapa |
| Ascendant Hunter | 3 | A run começa com a Curse **Haunted** (09 §13), não purificável, e a Forgotten Wraith é promovida a Elite com 1 afixo |
| Short Fuse | 2 | Cada sala tem timer; ao estourar, inimigos ganham Enrage |
| Frozen Momentum | 1 | Dodge com cooldown +50%; recuperar o Dodge concede 1 s de i-frame extra |
| Ruinous Pact | 3 | Inimigos causam +15% de dano; inimigos têm 20% menos vida |
| Storm Front | 2 | Todas as salas de combate recebem o Room Modifier **Aether Storm** (09 §9.2) |
| Mirror Packs | 2 | Cada pack tem um inimigo "Reflection" que copia o último afixo de Elite visto |
| Severed Supply | 1 | Treasure Rooms viram Challenge Rooms |
| Aether Drought | 2 | Geração de recurso −25%; abates concedem um pico de recurso |
| Pillar Echoes | 3 | Um afixo de chefe aleatório é aplicado a todos os Elites |

**Boons:** +1 Shrine garantida, +25% de Rift Shards, chefe com drop extra, Overrift com +2 salas, Blessing **Exalted** garantida na primeira Shrine do Estrato II.

**Attunement (Archivist Nv 3) — sistema único de target farming (R39).** Objeto: **Rift Sigil + 2 Pillar Fragments**, gastos para sintonizar o Sigil com **um set ou Unique já descoberto no Codex**. Efeito: peso **×3** para aquele set/Unique nos drops de Legendary da run (05 aplica sobre os pesos de região-lar e pity). Só existe em **Eclipse+** (Sigils só dropam a partir de Eclipse). Não há consumível de Attunement nem outro uso do nome "Attune": o serviço de valor do Enchanter chama-se **Imbue** (§6.4).

### 8.3 Boss Echoes

- Desbloqueio: Eclipse completo (tier I), A20 (II), A75 (III).
- Versões remixadas dos 5 chefes de região: padrões novos, fase extra, arena alterada (09-inimigos §Endgame projeta).
- Chave: **1 Heart of [Chefe]** do chefe correspondente + 300 Eclipse Marks (tier I), 2 Hearts + 600 (II), 3 Hearts + 1.200 (III).
- Recompensas: os **3 Uniques daquele chefe** (02/09) com chance elevada — tier I 35%, II 50%, III 70% por abate — contando para o mesmo pity do chefe em 05 (garantido no 10º abate); 2 Hearts do chefe; e **Pillar Seals** (1 por Echo de cada chefe, necessário para o pinnacle). Não existem Uniques exclusivos de Echo.

### 8.4 Challenge Altar

| Modo | Desbloqueio | Formato | Recompensa |
|---|---|---|---|
| **Daily Rift** | Campanha Normal completa | Seed diária fixa, 3 Distortions fixas, herói livre | 50 Marks no primeiro clear do dia |
| **Weekly Trial** | Veteran completo | Seed semanal, regras especiais (ex.: "só habilidades `Projectile` causam dano") | 200 Marks + cosmético rotativo; recorde pessoal (leaderboard Steam opcional) |
| **Build Trials** | Herói nível 50 | 3 trials por build principal (36 no total), com restrição temática (ex.: Inferno Berserker: "Fury nunca abaixo de 50 ou recebe dano") | Cosméticos de build, Mastery, entrada no Codex |
| **Eclipse Trial** | Nightmare completo | Gate de Eclipse (§7.1) | Desbloqueia Eclipse |
| **Endless Sundering** | A10 | Salas infinitas; a região muda a cada 10 salas; +1 Ascension equivalente a cada 5 salas; checkpoint com Rift Anchor a cada 10 salas (loot do bloco anterior fica secured) | Marks e Fragments por checkpoint; recorde de profundidade |
| **Eclipse Cycles** | Eclipse | Rotação de 8 semanas (calendário local) com tema: modificador global opcional + Distortions temáticas + cosméticos do ciclo | Cosméticos do ciclo; sem reset de progresso |

### 8.5 Pinnacle — The Sundering Heart

- Requisito: A30 + 5 Pillar Seals (um de cada Boss Echo tier I). As Seals são consumidas a cada tentativa.
- Encontro de 3 fases que usa mecânicas dos 5 chefes e das 6 afinidades (09-inimigos §Endgame projeta).
- Drops: **4 Uniques Pinnacle** (1 por herói; 02 cataloga com `fonteUnique = Pinnacle`), cosméticos de topo e Fragments.
- Narrativamente, é a resposta ao mistério do Sundering (o Archivist libera o capítulo final de Lore).

### 8.6 Loot exclusivo de endgame (sem raridade nova)

| Tipo | Fonte | Regra |
|---|---|---|
| **Uniques de chefe** (15 = 3 × 5) | Chefes de região (Nightmare+), Boss Echoes (chance elevada), Heart Exchange | Catálogo de 02; pity de 05 |
| **Pinnacle Uniques** (4) | The Sundering Heart | Um por herói; identidade forte, modifica o Ultimate |
| **Primal** (marca de qualidade, dono 05) | A50+, em qualquer Legendary | Todas as rolagens entre 85% e 100% da faixa, **nunca acima do máximo**; habilita o Primal Enchant (§6.4). Visual de borda própria |
| **Cosméticos** | Trials, Cycles, Mastery, Marks | Skins, transmog de VFX, títulos |

Meta total de Uniques no lançamento (R28): 30 = 15 de chefe + 4 Pinnacle + 11 de pool regional/Challenge (05/02). Primal não muda a contagem de atributos/modificadores do GDD §6.2.

### 8.7 Set Caches (Eclipse Marks)

- 400 Marks: escolher um set → recebe uma peça aleatória daquele set no iLvl máximo atual. A proteção de duplicata é definida por 05-loot (recomendação: não repetir peça até completar 6).
- Dá aos jogadores da Rota B (GDD §6.6) um caminho garantido, mas lento; a Rota A continua alimentada pelo drop natural e pelo Attunement.

---

## 9. Codex — Archivist

| Categoria | Progressão | Função |
|---|---|---|
| **Bestiary** | Abates por tipo: tiers 1 / 50 / 250 | Tier 2 mostra resistências e afinidades no tooltip; tier 3 mostra padrões de ataque e afixos possíveis. Só informação |
| **Armory** | Itens descobertos; registra a **melhor rolagem já encontrada** de cada item | Habilita Attunement, transmog e a oferta semanal do Merchant |
| **Sets** | Peças descobertas por set; marca 3/6 já ativados | Habilita Set Caches para o set |
| **Resonance Log** | Primeiro disparo de cada combinação de Resonance | Explica a interação com números reais da build atual (07-resonance fornece) |
| **Lore** | Memory Fragments completam capítulos | Narrativa do Sundering; recompensa de Account XP e cosméticos |

Cada entrada nova dá Account XP. Completar categorias dá títulos e cosméticos.

---

## 10. Curva de tempo esperada

Base: run de ~30 min, taxa média de sucesso de 60–70% na dificuldade apropriada.

| Marco | Horas acumuladas (jogador médio) | Nível do herói principal |
|---|---:|---:|
| Primeiro chefe (Ashen Colossus) | 1,5–2 h | ~6 |
| Campanha Normal concluída | 12–15 h | ~25 |
| Veteran concluído | 22–26 h | ~35 |
| Nightmare concluído | 35–40 h | ~45 |
| Entrada em Eclipse (Eclipse Trial) | 40–45 h | 48–50 |
| Nível 50 do herói principal | **40–50 h** | 50 |
| Eclipse completo / A1 | 50–60 h | 50 |
| Primeiro Legendary +10 | ~70 h | 50 |
| A30 / primeira tentativa do pinnacle | 90–110 h | 50 |
| **A50** | **130–160 h** | 50 |
| A100 | 300 h+ (jogador dedicado) | 50 |
| Account Rank 100 | ~300 h | — |

Um segundo herói até o nível 50 (com Kindred Echo e Vault compartilhado) leva cerca de 12–15 h.

---

## 11. Dependências e pontos de integração

| Documento | Recebe deste doc | Fornece para este doc |
|---|---|---|
| 01-atributos | Upgrade +3%/nível em atributos e implícito, teto +10 igual para todas as raridades (bucket Increased); Monster Level (eLvl) por região × dificuldade (§7.2), incluindo eLvl > 100 em A91+ | Calibrar faixas presumindo itens a ~+6 na entrada de Eclipse |
| 02-equipamentos | Enchant (Common/Rare/Epic), Imbue e Reroll de Afixo Aberto (Legendary/set/Unique); valores de venda/salvage; 4 Uniques Pinnacle e 15 de chefe com `fonteUnique`; Heart Exchange | Pool de atributos válidos por slot; 3 opções de Afixo Aberto por item; catálogo de 30 Uniques |
| 03-modificadores | Refine (reroll só do valor, nunca acima do máximo); modificadores **não** sobem com upgrade | Faixas numéricas dos modificadores |
| 04-conjuntos | Set Locker; Set Caches; Attunement por set | Lista de peças |
| 05-loot | Cita o iLvl de 05; Peril → +8% quantidade/+3% chance de Legendary (teto ×2,0); Attunement ×3 (Eclipse+); Boss Echoes com chance elevada dos Uniques de chefe e mesmo pity; odds provisórias de Aether Caches; proteção de duplicata nas Set Caches; Primal a partir de A50; Satchel 30 | Taxas reais, iLvl por região, pity, Primal, tabela do Overrift Herald, empilhamento de quantidade |
| 06-combate | Escala relativa de vida/dano por tier (alvo resultante, já com +25% de Blessings); eLvl por sala; recarga do Aether Flask na Rest; sem revive | Curva base de HP/dano por eLvl, TTK e letalidade |
| 07-resonance | Blessings carregam tags de afinidade; Resonance Log no Codex | Interações e números para o Log |
| 08-herois | XP por nível (fórmula), Kindred Echo, respec grátis, Hero Mastery, Build Trials por build | O que cada nível concede; desbloqueio de heróis (Nyx/Orun); condições temáticas das Build Trials |
| 09-inimigos | Serviços da Rest Room (inclusive Clareira e Antecâmara); Rift Shards como única moeda da run nos eventos; regras do Echo of a Fallen Eclipsed (§4.1); Heart of [Chefe] (§6.1); eLvl | Mapa de nós da run, catálogo de Blessings e ranks, afixos de Elite por dificuldade, Room Modifiers, Curses, Reverberant/Haunted/Aether Storm usados nas Distortions; Nightmare Behaviors, Eclipse Phenomena, Boss Echoes, Eclipse Trial, The Sundering Heart, Overrift Herald (seção de endgame) |
| 11-arquitetura | Dados como ScriptableObjects: `DifficultyTierDef`, `MonsterLevelTableDef`, `AscensionCurveDef`, `CurrencyDef` (inclui Heart of [Chefe]), `DistortionDef`, `SigilDef`, `NpcServiceDef` (custos como fórmulas parametrizadas), `AccountRankRewardDef` (`BlessingDef` é dado de 09); save com estado de conta (Rank, Codex, Vault, Set Locker, NPC levels, Ascension por herói, contadores de Enchant/Imbue, Afixo Aberto e Refine por instância de item, upgrade level, flags Anchor/Primal, **última run terminada em morte por região e herói** para o Echo of a Fallen Eclipsed); conversão de itens removidos (§4.2) | Implementação de save, migração e eventos |

Eventos de jogo que este doc precisa (para 11-arquitetura): `RunEnded(result: Victory|Extraction|Death)`, `BossDefeated`, `ItemDiscovered`, `ResonanceFirstTriggered`, `EnemyKilled(type)`, `CheckpointReached`, `SigilApplied`, `RestServiceUsed(service)`.

---

## 12. Conflitos com as convenções

Nenhum conflito em aberto após a rodada 1 de integração (00 §5). Esclarecimentos:

1. **Orçamento de poder (00 §4):** o poder temporário da run (Blessings de 09, +20–30%) fica **fora** da tabela de orçamento, que descreve poder permanente. A escala de inimigos é calibrada para +25% (§7.2).
2. **Upgrade do Blacksmith** soma até +30% nos valores dos atributos e do implícito, com o mesmo teto para todas as raridades (R31). Faz parte da fatia "Atributos de equipamento (~25%)": 01 calibra presumindo itens a ~+6 no cenário de referência (Eclipse, iLvl ~80).
3. **Nenhuma fonte More** é introduzida por este doc. A antiga Eclipse Blessing (10–15% More) foi removida e absorvida pelo rank Exalted de 09 (R35).
4. **Nenhuma tag nova** foi criada; Distortions e Build Trials usam o vocabulário de 00 §3.4.
5. **Resoluções aplicadas:** R28 (Uniques de chefe/Pinnacle), R31 (upgrade e Primal; Ascended removido), R33 (mapa de 09), R34 (Rest Room), R35 (Blessings), R36 (moedas e Heart of [Chefe]), R37 (Satchel 30), R38 (iLvl de 05, 100 em A90), R39 (Attunement único; Imbue), R40 (curva de XP; desbloqueios de 08), R41 (afixos de 09), R42 (Enchanter sem "Legendary não nomeados"; reroll de Afixo Aberto), R43 (Distortions citam 09; Ruinous Pact), R44 (eLvl), R45 (teto ×2,0), R46 (Echo of a Fallen, sem revive, itens removidos).

---

## 13. Questões abertas para playtest

1. Os 50% de retenção na morte (Gold/Dust/Cores) geram tensão sem desmotivar? Faixa aceitável a testar: 40–60%.
2. Heart Exchange (8 Hearts por Unique) e o pity de 05 (10º abate) se somam sem tornar os Uniques de chefe triviais? Meta: primeiro Unique de um chefe-alvo em 4–8 abates.
3. Razão `custo de upgrade +6 / renda por run` dentro de 2,5–4 em todas as dificuldades (telemetria).
4. A curva de Ascension (vida 1,035^N) cria uma parede natural entre A60 e A80 para builds medianas e permite A100 só para builds otimizadas?
5. O Eclipse Trial bloqueia jogadores que deveriam passar? Meta: ≥70% de sucesso em até 3 tentativas para jogadores com nível 48+ e equipamento de Nightmare.

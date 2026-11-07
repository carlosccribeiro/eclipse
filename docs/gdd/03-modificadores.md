# 03 — Catálogo de Modificadores de Habilidade

**Projeto:** PROJECT ECLIPSE · **Versão:** GDD v0.3 · **Dono:** Systems / Combat Design
**Referências:** GDD v0.2 §5.2, §6.2, §6.5, §14.4, §15 · `00-convencoes.md`

---

## 1. Objetivo e escopo

Definir o sistema completo de **modificadores de habilidade** (Skill Modifiers) que aparecem em equipamentos Epic (1 modificador) e Legendary (2 modificadores):

- regras de aplicação, compatibilidade, duplicatas, stacking, exclusividade e ordem de resolução;
- catálogo específico para as 20 habilidades dos 4 heróis (176 modificadores — 143 originais + 33 da rodada de integração, cobrindo todos os pares Habilidade×Categoria usados por 04);
- catálogo genérico por tag (30 modificadores) que funciona em múltiplos heróis;
- orçamento de poder por modificador e regras de rolagem de valores;
- tabela de mapeamento dos nomes usados em 02/04 para os IDs deste doc (§10).

Fora de escopo: em quais itens nomeados cada modificador aparece (02-equipamentos), probabilidade de drop (05-loot), definição de status de afinidade (07-resonance), kit base das habilidades (08-herois).

---

## 2. Decisões tomadas

1. **Dois tipos de modificador: Específico (de habilidade) e Genérico (de tag).** Específicos dão identidade ao herói; genéricos permitem builds híbridas e reaproveitam conteúdo entre heróis.
2. **Um modificador sempre se liga a um alvo:** uma habilidade específica (`KAE-CS`) ou um *conjunto de tags* (ex.: `Projectile`). Genéricos se aplicam a **uma** habilidade escolhida pelo jogador entre as elegíveis (ver §3.2). Evita efeitos globais difusos e mantém a leitura de "o que esta peça faz".
3. **Limite de modificadores ativos por habilidade: 3** (Ultimate: **2**; ataques de arma Basic/Heavy: **2** cada) e **limite global de 14 ativos por herói** (00 R30). Com 12 slots o jogador pode *equipar* até 24 modificadores, mas no máximo 14 ficam ativos (excedentes: *Inativo (limite global)*, pela mesma prioridade de §3.3). O limite força distribuição entre habilidades e impede que uma única habilidade concentre todo o poder.
4. **Duplicata (mesmo ID) nunca acumula:** vale a de maior rolagem; a outra fica *Inativa (duplicada)*. Mantém o valor de rolagem relevante sem incentivar stacking degenerado.
5. **Grupos de exclusividade** (Forma, Conversão, Modo de Uso): no máximo 1 modificador ativo por grupo em cada habilidade. Impede combinações visualmente/mecanicamente contraditórias (ex.: virar círculo e virar leque ao mesmo tempo).
6. **Ordem de resolução fixa em 6 fases** (Conversão → Forma/Comportamento → Parâmetros → Replicação → Gatilhos → Recurso). Determinístico, implementável como pipeline (11-arquitetura).
7. **Modificadores nunca concedem More% global.** Seus ganhos numéricos são *Increased local* (somados ao Σ Increased da habilidade, portanto diluídos) ou *novas instâncias de dano* (repetições, cadeias, explosões). Respeita o orçamento de ~15% do contrato e mantém More% reservado a Set Powers/Uniques/Resonance.
8. **Instâncias derivadas têm Proc Coefficient < 1 e seguem o modelo Generation de 07 (00 R07):** o hit original é Gen 0; cada instância derivada (repetição Echo, cadeia, explosão de Trigger) é Gen+1; **nada dispara em Gen ≥ 2**. Previne loops infinitos e explosões de proc.
9. **Conversão de dano: o dano convertido escala apenas com o Increased do tipo de destino** (+ genéricos como Skill/Area/Projectile Damage). Regra simples, legível, e torna a conversão uma decisão real de build.
10. **Clones e ecos herdam modificadores, exceto Echo e Resource.** Réplicas (`IsEcho`) nunca geram novas réplicas, salvo poderes que declaram *Echo Recursion 1×* (04 Thousand Echoes / Infinite Reflection, 08 Echo Chamber) — nenhum modificador deste catálogo concede Echo Recursion.
11. **Efeito fixo, valor variável:** cada modificador tem um único valor rolado (às vezes dois). Razão máx/mín ≤ 1,5 e a diferença min→max vale ≤ 5% do DPS total no cenário de referência (GDD §15.8).
12. **Hero-bound (00 R27):** um item que carrega modificador específico de habilidade só pode ser equipado pelo herói dono (regra de 02). Epic gerado sorteia 75% no pool do herói e 25% no pool `GEN-xx` (universal, não vincula o item).
13. **Raridade mínima:** `E` = pode aparecer em Epic e Legendary; `L` = só Legendary. Modificadores que mudam a forma/identidade da habilidade de modo profundo são `L`.
14. **Execute unificado (00 R08):** limiar máximo 20% em inimigos comuns; Elites usam metade (máx. 10%); Guardians e chefes são imunes. Contra chefes, um efeito de Execute vira **+25% Vulnerability abaixo de 20% de vida**, uma única conversão que não acumula entre fontes. Execute é uma HitFlag (00 R02), não tag.
15. **Vulnerability com teto (00 R12):** valores de Vulnerability deste catálogo são fixos por fonte, não acumulam consigo mesmos e entram no teto total de +60% definido por 07.
16. **Estrutura de item (00 R25):** Legendary nomeado e peça de set trazem 2 modificadores fixos; Unique traz 2, um dos quais pode ser **Signature** (exclusivo, fora de qualquer pool, ID `SIG-<item>`; o efeito é escrito em 02, as regras de limite e stacking são as deste doc).
17. **Este doc é o dono dos nomes de modificadores (00 R26).** 02 e 04 usam apenas IDs daqui; §10 traz o mapeamento dos nomes antigos.

---

## 3. Regras gerais

### 3.1 Anatomia de um modificador (dado)

| Campo | Descrição |
|---|---|
| `ID` | `HERO-SKILL-NN` (específico) ou `GEN-NN` (genérico) |
| `Nome` | Fixo |
| `Categoria` | Behavior, Elemental, Chain, Echo, Area, Conversion, Trigger, Resource, Duration |
| `Alvo` | Habilidade específica ou filtro de tags (com exclusões) |
| `Grupo de exclusividade` | `F` (Forma), `C` (Conversão), `M` (Modo de uso) ou nenhum |
| `Efeito fixo` | Comportamento — nunca muda |
| `Valor variável` | 1–2 parâmetros com faixa `[mín–máx]` |
| `Raridade mínima` | `E` ou `L` |
| `Budget (BP)` | Pontos de orçamento (ver §6) |
| `Tags adicionadas/removidas` | Ex.: conversão para Frost adiciona `Frost`; Forma em círculo pode remover `Projectile` |

### 3.2 Alvo e compatibilidade

- **Específico:** só funciona na habilidade indicada, no herói dono. O item que o carrega é Hero-bound e não pode ser equipado por outro herói (02; 00 R27).
- **Genérico:** possui filtro de tags (ex.: `requer Projectile; exclui Ultimate`). Ao equipar o item, o jogador **atribui** o modificador a uma habilidade elegível do herói atual (padrão automático: habilidade elegível com menos modificadores ativos). A atribuição é salva por item + herói e pode ser trocada livremente fora de combate (no hub ou em Rest Room) sem custo.
- **Ataques de arma** (`Basic`, `Heavy`) são alvos válidos para genéricos que aceitem essas tags.
- Tags usadas são exclusivamente as de `00-convencoes §3.4` (Execute, Unavoidable etc. são HitFlags, não tags — 00 R02). 08-herois é dono do kit e das tags finais de cada habilidade; as tags listadas abaixo devem ser lidas como espelho de 08 — em divergência, vale 08 e a elegibilidade dos genéricos é recalculada.
- Modificadores e Augments da árvore (08 Aether Lattice) são camadas distintas: Augments não ocupam o limite de modificadores. Quando um Augment e um modificador são incompatíveis (mesma forma/modo, grupo F/M, ou efeito redundante), **o modificador declara a exclusão** no seu Efeito fixo e fica *Inativo (Augment)* enquanto aquele Augment estiver ativo (08 §10.1).

### 3.3 Limites, duplicatas e exclusividade

| Regra | Comportamento |
|---|---|
| Limite por habilidade | 3 ativos (Ultimate 2; Basic 2; Heavy 2). Excedentes: *Inativo (limite)*; prioridade = ordem de slot (Main Hand → Off Hand → Head → … → Relic 2), e o jogador pode fixar prioridade manualmente na tela de builds |
| Limite global | 14 ativos por herói; mesma prioridade |
| Duplicata (mesmo ID) | Só a maior rolagem fica ativa |
| Mesmo grupo de exclusividade | Só 1 por grupo por habilidade; vence a de prioridade maior |
| Mesma categoria, sem grupo | Acumula normalmente (ex.: dois `Area` diferentes somam) |
| Valores numéricos da mesma natureza | Somam aditivamente (ex.: +20% área e +15% área = +35% área) |
| Set Powers | Não ocupam slot de modificador e **não contam** no limite por habilidade (GDD §6.6.6) |
| Modificador Signature de Unique | ID `SIG-<item>`; efeito escrito em 02; conta nos limites; nunca está em pool aleatório; máx. 1 por Unique (00 R25). Registrados em 02: SIG-K16 Pillarfall, SIG-L15 Court of Rime, SIG-U15 Molten Wake |

A UI de builds (GDD §11.2) mostra cada modificador inativo com o motivo.

### 3.4 Ordem de resolução (pipeline)

Quando uma habilidade é executada, seus modificadores ativos são aplicados nesta ordem:

| Fase | Categorias | O que faz |
|---|---|---|
| 1. Conversão | Conversion, Elemental (grupo C) | Define o tipo de dano final e as tags de afinidade |
| 2. Forma | Behavior (grupo F/M e sem grupo) | Define forma, entrega, modo de uso (carga, canal, cargas) |
| 3. Parâmetros | Area, Duration, Elemental (adição) | Ajusta números: raio, duração, quantidade |
| 4. Replicação | Chain, Echo | Gera instâncias adicionais; cada cópia herda fases 1–3 |
| 5. Gatilhos | Trigger | Registra condições sobre o resultado final (on hit, on kill, on crit…) |
| 6. Recurso | Resource | Calcula custo/geração/recarga por último |

### 3.5 Conversão e dano adicional

- **Conversão** (grupo C): move X% do dano de um tipo para outro. O total convertido de uma habilidade é limitado a 100%. Só um modificador do grupo C por habilidade; conversões vindas de Set Powers/Uniques somam e, se passarem de 100%, são normalizadas proporcionalmente.
- O dano convertido **perde** o Increased do tipo de origem e **ganha** o do tipo de destino. Increased genéricos (`Skill Damage`, `Area Damage`, `Projectile Damage`, `Damage Over Time`) continuam valendo.
- A habilidade **ganha a tag** da afinidade de destino se ≥ 50% do dano for daquele tipo; abaixo disso, ganha a tag apenas para fins de Resonance (aplicação de status).
- **Dano adicional** (Elemental sem grupo): adiciona dano extra "como X% do dano base como <afinidade>". Não remove o tipo original.

### 3.6 Instâncias derivadas e Proc Coefficient

| Tipo de instância | Proc Coefficient | Notas |
|---|---|---|
| Hit original da habilidade | 1,0 | — |
| Repetição (Echo) | 0,5 | Gen+1; não pode ser repetida de novo (salvo Echo Recursion 1× declarada fora deste doc) |
| Cadeia / fork / split (Chain) | 0,5 | Gen+1; cada salto conta como instância nova |
| Explosão disparada por Trigger | 0,5 | Gen+1 |
| Área persistente / rastro (tick) | 0,25 | Por tick (tick de DoT = 0,5 s, 06) |
| Cópia de clone (Echo Clone, Thousand Reflections) | 0,4 | Gen 0 do clone; herdam modificadores exceto Echo e Resource |

- Proc Coefficient multiplica: chance de efeitos "on hit", acúmulo de status (07-resonance), geração de recurso on hit.
- **Modelo Generation (00 R07, 07 dono):** instâncias Gen 0 e Gen 1 podem acionar Triggers; **instâncias Gen ≥ 2 nunca acionam nada** (nem Triggers, nem Resonance, nem Chain). Na prática: hit → explosão de Trigger (Gen 1) → no máximo mais um efeito (Gen 2), que é terminal. Repetições (Echo) acionam Triggers *on hit*, mas não *on cast*.
- Todo Trigger com efeito forte possui **ICD** (cooldown interno) declarado.

### 3.7 Habilidades com cargas, canais e transformações

- Modificadores que adicionam cargas usam o cooldown base por carga, salvo valor declarado.
- Cooldown Reduction (01-atributos) afeta o tempo de recarga por carga.
- Modificadores que transformam a habilidade em canal/carregamento pertencem ao grupo `M`.
- Transformações (ex.: Ashen Avatar) aplicam o estado mas não "consomem" modificadores de outras habilidades.

### 3.8 Recurso por herói (tradução de Resource)

| Herói | "Custo reduzido" significa | "Gerar recurso" significa |
|---|---|---|
| Kael (Fury) | −X Fury de custo | +X Fury |
| Lyra (Arcane Charges) | X% de chance de não consumir a carga mais recente ao consumir (08 §3.2) | +1 carga (ou chance) |
| Nyx (Echo Energy) | −X% de Echo Energy | +X Echo Energy |
| Orun (Void Mass → Void Constructs) | Chance X de o Construct sacrificado não ser consumido (01 §3.8) | Velocidade de formação de Constructs via Void Mass × (1 + X) (01 §3.8) |

Números finais de tradução: 01-atributos §3.8 (dono); comportamento: 08-herois. Para Orun, o recurso é a barra **Void Mass**, que gera Void Constructs (contagem base 3, teto base 6, até 8 por exceções listadas em 08 — 00 R01). Modificadores que elevam o limite de Constructs (ex.: Construct Overflow) precisam constar na tabela de exceções de 08. Construct Overflow (ORU-GF-08) ainda não consta (pendência registrada em `docs/roadmap/reports/D01-divergencias.md`, P2).

---

## 4. Catálogo específico por habilidade

Legenda: **Cat.** = categoria · **Gr.** = grupo de exclusividade · **Mín.** = raridade mínima (E/L) · **BP** = budget. Valores em `[mín–máx]` são rolados. "wpn" = dano de arma; "skill" = dano base da habilidade.

> As "premissas base" de cada habilidade são o comportamento assumido para escrever os modificadores. 08-herois é dono do kit e deve manter essas premissas ou revisar os modificadores afetados. Revisadas contra o kit de 08 no módulo D01 (2026-11-07; relatório `docs/roadmap/reports/D01-divergencias.md`).

### 4.1 Kael — The Ashbound

#### KAE-CS — Cleaving Strike
*Tags (08):* Attack, Melee, Area, Physical · *Premissa (08 §4.4):* arco frontal de 180°, 3,5 m, 220% DA, custa 20 Fury, sem cooldown (pode ser usado em sequência enquanto houver Fury; cancela a recuperação do Basic).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Burning Arc | Behavior | — | O golpe deixa um arco de fogo no chão por 3 s; aplica Burning. Máx. 1 arco ativo: um novo golpe substitui o arco anterior (tick 0,5 s, Proc 0,25) | [15–22]% wpn/s como Ember | E | 7 |
| 02 | Volatile Impact | Trigger | — | Atingir 3+ inimigos causa explosão Ember de 2,5 m no centro do arco (ICD 0,5 s) | [40–60]% wpn | E | 8 |
| 03 | Executioner's Reach | Area | — | +alcance; bônus contra inimigos abaixo de 35% de vida | +[25–35]% alcance; +[30–45]% Increased local vs. feridos | E | 8 |
| 04 | Echo Slash | Echo | — | Repete o golpe após 0,35 s | [40–60]% do dano replicado | L | 12 |
| 05 | Shattering Blow | Trigger | — | Contra inimigos Frozen: consome Freeze e causa Shatter (07) | +[50–75]% dano de Shatter | E | 7 |
| 06 | Ruin Cleaver | Conversion | C | Converte dano Physical em Ruin; acertos aplicam Decay (07) com a Status Potency do dano Ruin resultante. Somada a conversões de Augments/Keystones/Set Powers, se passar de 100%, é normalizada proporcionalmente (§3.5) | [40–60]% convertido | E | 6 |
| 07 | Blood Fury | Resource | — | Atingir 3+ inimigos gera Fury extra; se ao menos 1 acerto for crítico, +2 Fury (1× por uso) | +[5–7] Fury (3+ alvos) | E | 6 |
| 08 | Infernal Collapse | Behavior | F | O arco vira um impacto circular de 2,8 m à frente; +Stagger 50% | +[50–70]% Increased local | L | 12 |

#### KAE-IL — Infernal Leap
*Tags (08):* Attack, Movement, Area, Ember · *Premissa (08 §4.4):* salto mirado até 8 m (Unstoppable no ar, 0,5 s), impacto de raio 3 m com 180% DA, deixa o chão Burning por 2 s; não custa Fury e gera +15 Fury; CD 9 s.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Meteor Descent | Behavior | M | Segurar o botão prolonga a subida (até +1 s); raio e dano crescem com o tempo no ar. Unstoppable (06) cobre todo o tempo no ar | Até +60% raio; até +[40–60]% Increased local | L | 12 |
| 02 | Scorched Trail | Behavior | — | A trajetória deixa um rastro de fogo por 3 s | [10–15]% wpn/s (rastro 3 s, tick 0,5 s, Proc 0,25) | E | 6 |
| 03 | Twin Leap | Behavior | M | Ganha 2 cargas; cada impacto causa menos dano | Penalidade de [−35 a −25]% dano | E | 7 |
| 04 | Gravity Crash | Elemental | — | Antes do impacto puxa inimigos em 4 m para o centro; adiciona dano Void | +[20–30]% skill como Void | E | 8 |
| 05 | Fury Leap | Resource | — | Além dos +15 Fury de 08, gera Fury por inimigo atingido pelo impacto (máx. 3) | [6–9] Fury por inimigo | E | 7 |
| 06 | Aftershock | Echo | — | Segunda onda 0,8 s depois com raio +30% | [50–70]% do dano | L | 11 |
| 07 | Ruin Landing | Conversion | C | Converte dano Ember em Ruin; atingidos sofrem Vulnerability por 4 s (valor fixo por fonte, teto 60% — R12). Com Ruinous Descent ativo, vale apenas o maior valor de Vulnerability do impacto | [50–70]% convertido; Vulnerability +[8–12]% | E | 8 |
| 08 | Ember Retreat | Behavior | F | Salta para longe do cursor e deixa uma mina Ember na origem (arma 0,5 s) | Mina [80–120]% wpn | E | 6 |
| 09 | Tectonic Landing | Area | — | Impacto maior; atingidos recebem Stagger adicional | +[25–35]% raio; +[30–45]% Stagger Damage local | E | 7 |
| 10 | Combustion Landing | Trigger | — | O impacto aplica Combustion (07) nos atingidos com Burning, com potência reduzida; ICD 1 s por alvo; Gen+1 | [40–55]% do dano de Combustion de 07 (≈ 60–82% do dano restante) | E | 8 |

#### KAE-BC — Burning Chains
*Tags (08):* Attack, Ranged, Ember, DoT · *Premissa (08 §4.4):* correntes em cone de 8 m prendem até 5 alvos (Root 2 s) com 120% DA e aplicam Burning; quebrar a corrente (fim do Root ou distância > 10 m) causa 60% DA adicional; custa 15 Fury, CD 12 s. *Leitura dos modificadores:* "o acorrentado/o alvo" = cada alvo preso (até 5).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Chain Pull | Behavior | — | Puxa os alvos presos até Kael; contra inimigos Heavy/Colossal (06/09), Guardians e chefes, puxa Kael até o alvo. Com Tethering Chains ativo, o puxão é o do Augment e este modificador mantém o bônus e o puxão de Kael | +[17–25]% Increased local no próximo Cleaving Strike (2 s) | E | 6 |
| 02 | Fan of Chains | Behavior | F | O cone vira um círculo de 360° (raio 6 m) e prende até 8 alvos | [55–70]% dano cada | E | 9 |
| 03 | Linked Torment | Chain | — | A corrente salta para inimigos extras e os une enquanto durar o Root; uma parcela do dano direto recebido por um unido é causada aos demais (Gen+1, Proc 0,5; dano compartilhado não é compartilhado de novo; cap por alvo conforme §6.3) | +[2–3] alvos; [17–25]% compartilhado | L | 12 |
| 04 | Searing Shackles | Duration | — | Root mais longo; Burning em acorrentados dura mais | Root +[0,6–0,9] s; Burning +[30–45]% duração | E | 6 |
| 05 | Ruin Shackles | Conversion | C | Converte 100% do dano Ember em Ruin (perde Ember, ganha Ruin); a aplicação de Burning da habilidade vira Decay (07) com a mesma potência. Ao fim do Root, aplica Detonation (07) no alvo, com dano Ruin adicional | Detonation +[60–90]% wpn | L | 11 |
| 06 | Fury Siphon | Resource | — | Enquanto acorrentado, cada alvo gera Fury para Kael (máx. 10 Fury/s somando todos) | [2–3] Fury/s por alvo | E | 6 |
| 07 | Molten Knot | Trigger | — | Se o acorrentado morrer, a corrente explode (2,5 m) | [70–100]% wpn Ember | E | 7 |
| 08 | Molten Links | Elemental | — | Adiciona dano Ember extra; nos alvos presos, a Status Potency (acúmulo) de Burning da habilidade é dobrada | +[20–30]% skill como Ember | E | 6 |

#### KAE-AG — Ashen Guard
*Tags (08):* Defensive, Attack, Melee, Physical · *Premissa (08 §4.4):* postura de 1,5 s que reduz o dano recebido em 60% e bloqueia totalmente ataques frontais (exceto `Unblockable`); **Parry** nos primeiros 0,2 s (06 — 00 R20) anula o dano e dá +50% ao contra-ataque; ao fim (ou ao soltar) contra-ataca em arco de 3 m (250% DA + 30% do dano absorvido) e gera +20 Fury; sem custo, CD 14 s.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Ember Riposte | Elemental | C | Converte parte do dano do contra-ataque em Ember; o contra-ataque vira onda em cone de 5 m (em vez do arco de 3 m), mantendo 250% DA + 30% do absorvido de 08 | [60–80]% convertido | E | 8 |
| 02 | Absorbing Ash | Resource | — | Cada golpe bloqueado ou aparado (Parry) gera Fury (máx. 30 por uso) | [7–10] Fury por golpe | E | 6 |
| 03 | Bulwark Stance | Behavior | M | Vira postura alternável: bloqueia em 360° com Block Amount próprio (substitui os −60% e o bloqueio total frontal de 08), −40% de velocidade, drena 5 Fury/s (desliga com 0 Fury), permite atacar. Parry só nos primeiros 0,2 s após ligar; o contra-ataque ocorre ao desligar; o CD de 14 s começa ao desligar. Com Unbroken: vale a velocidade de 50% e a janela de Parry do Augment | Block Amount [35–45]% | L | 12 |
| 04 | Reflective Plate | Trigger | — | Projéteis bloqueados são refletidos | [80–120]% do dano original | E | 7 |
| 05 | Lingering Ash | Duration | — | Após a guarda, mantém Damage Reduction | [25–35]% Damage Reduction por 2 s (sujeito ao cap de 01) | E | 7 |
| 06 | Ashen Nova | Area | — | Parry libera nova de 4 m que aplica Burning e interrompe os atingidos (flinch, 06), sem Knockback | [60–90]% wpn | E | 7 |
| 07 | Unyielding | Behavior | F | Não bloqueia: troca o bloqueio frontal por Super Armor (06) durante a postura, mantendo a redução de dano recebida de 08; aplica Taunt (06) em inimigos a 6 m (Elites: metade; Guardians/chefes imunes) | Taunt [2,0–3,0] s | L | 12 |

#### KAE-WB — Worldbreaker (Ultimate)
*Tags (08):* Attack, Area, Ultimate, Physical, Ruin · *Premissa (08 §4.4):* golpe no chão de raio 7 m que abre 6 fissuras radiais; 600% DA + 6% DA por Fury consumida; consome toda a Fury (mín. 50); atingidos ficam com Vulnerability Ruin (07) por 6 s; CD 90 s.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Tectonic Fissures | Behavior | — | 3 das 6 fissuras radiais se estendem até 12 m e persistem por 4 s como zona de dano (tick 0,5 s, Proc 0,25) | [25–35]% wpn/s | E | 8 |
| 02 | Rolling Quake | Echo | — | Repete o impacto mais 2 vezes, com raio 70% | [35–50]% do dano cada | L | 14 |
| 03 | Ruin Epicenter | Conversion | C | Converte 100% do dano Physical em Ruin; inimigos mortos pelo golpe (Gen 0) ou por instâncias Gen 1 liberam Detonation (07) com dano Ruin adicional (Gen+1) | Detonation +[40–60]% wpn | E | 8 |
| 04 | Ashen Avatar | Behavior | F | Em vez do golpe, consome toda a Fury (mín. 50, como 08) e transforma Kael por 8 s: +30% área de Basic, Heavy e habilidades `Attack`; cada 3º Cleaving Strike gera mini-impacto. *Inativo (Augment)* com Cataclysm e Doom of Ages; Mountain's Heart se aplica ao transformar | Mini-impacto [50–70]% wpn | L | 14 |
| 05 | Fury Reservoir | Resource | — | Além do escalonamento de 08 (+6% DA por Fury), cada Fury consumida concede Increased local | +[0,25–0,35]% Increased local por Fury consumida | E | 9 |
| 06 | Shattered Earth | Duration | — | O chão permanece quebrado: inimigos sobre ele sofrem Slow genérico (06) de 30% e Vulnerability (fixa por fonte, teto 60% — R12) | [5–7] s; Vulnerability +[8–12]% | E | 8 |
| 07 | Executioner's Verdict | Trigger | — | Executa inimigos comuns que ficarem abaixo do limiar (Elite: metade; Guardian/chefe: imune — contra chefe aplica a conversão de §2 item 14) | Limiar [15–20]% vida | L | 13 |
| 08 | Ashfall | Area | — | Raio maior; a borda externa deixa cinzas que aplicam Burning por 3 s | +[20–30]% raio | E | 8 |
| 09 | Pyre Crown | Elemental | — | Adiciona dano Ember; Kael ganha uma coroa de chamas por 4 s que queima inimigos a 2 m | +[25–35]% skill como Ember; coroa [10–15]% wpn/s | E | 9 |

### 4.2 Lyra — The Veilweaver

#### LYR-FL — Frost Lance
*Tags (08):* Spell, Projectile, Ranged, Frost · *Premissa (08 §5.4):* projétil de 18 m que perfura 1, 160% DA, sem custo e sem cooldown (cast 0,45 s); aplica Chill e, em alvo Frozen, causa Shatter (07); **gera 1 Arcane Charge em acerto crítico** (ICD 1 s, 08 §5.1).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Piercing Lance | Behavior | — | Atravessa todos os inimigos; perde dano a cada inimigo atravessado. *Inativo (Augment)* com Null Lance | Perda de [18–12]% por inimigo | E | 7 |
| 02 | Splinter Burst | Chain | — | No primeiro inimigo atingido, a lança se divide em fragmentos em leque (instâncias Chain, Gen 1; fragmentos não se dividem de novo) | [3–4] fragmentos a [30–40]% | E | 8 |
| 03 | Glacial Javelin | Behavior | M | Vira carregável (até 1 s): dobra o tamanho; com carga máxima aplica Freeze (07) em inimigos comuns (Elites, Guardians e chefes não são congelados por este efeito); DR de CC de 06 (R09) | Até +[80–120]% Increased local | L | 12 |
| 04 | Frost Volley | Behavior | F | Dispara 3 lanças em leque; a geração de carga em crítico mantém o ICD de 1 s compartilhado entre as lanças. *Inativo (Augment)* com Splitting Lance (mesma forma) | [45–55]% dano cada | E | 8 |
| 05 | Void Lance | Conversion | C | Converte dano Frost em Void; puxa inimigos 1,5 m para a linha do disparo (Pull, 07; em chefes vira Distortion, R10; em Elites imunes a controle, Slow — 03 §6.3). *Inativo (Augment)* com Null Lance | [50–70]% convertido | E | 7 |
| 06 | Shattering Lance | Trigger | — | Shatter causado por Frost Lance causa mais dano e devolve 1 Arcane Charge (ICD 2 s) | +[35–50]% dano de Shatter | E | 8 |
| 07 | Arcane Echo Lance | Echo | — | Cada 3º lançamento dispara uma segunda lança após 0,2 s | [70–90]% do dano | L | 11 |
| 08 | Overflowing Lance | Resource | — | Enquanto as Arcane Charges estiverem no teto atual (base 6; até 8 pelas fontes de 08 §11.1), Frost Lance causa mais dano; a carga de crítico que excederia o teto é perdida | +[35–50]% Increased local | E | 7 |
| 09 | Arcane Lance | Elemental | — | Adiciona dano Void; a lança atravessa o primeiro escudo/barreira inimiga | +[20–30]% skill como Void | E | 6 |

#### LYR-VO — Void Orb
*Tags (08):* Spell, Projectile, Area, DoT, Void · *Premissa (08 §5.4):* orbe lento (12 m em 2 s) com Pull contínuo em raio de 2,5 m, 60% DA a cada 0,5 s e implosão de 200% DA ao fim do trajeto ou ao reativar a tecla; **Consome** todas as cargas (+50% na implosão por carga); CD 6 s.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Singularity Core | Behavior | F | Os ticks do orbe causam metade do dano; o dano retirado se acumula e é somado à implosão, que ganha raio 5 m e puxa os inimigos atingidos para o centro antes de detonar (Pull 07; chefes: Distortion, R10). Compatível com os 3 Augments | Implosão +[40–60]% Increased local | L | 13 |
| 02 | Orbital | Behavior | F | Em vez de viajar, o orbe orbita Lyra a 2,5 m por 4 s (Pull contínuo em direção ao orbe) e implode ao fim da órbita ou ao reativar; com Overcharged Orb, implode no primeiro inimigo tocado. *Inativo (Augment)* com Stillpoint | [30–40]% skill por tick (0,5 s) | E | 8 |
| 03 | Twin Orbs | Behavior | — | Dispara 2 orbes em V; as cargas consumidas valem para os dois (cada implosão escala com todas); reativar a tecla implode ambos. Zonas sobrepostas da mesma fonte não acumulam no mesmo alvo (06) | [60–70]% dano cada | E | 9 |
| 04 | Rime Core | Conversion | C | Converte dano Void em Frost; deixa fragmentos de gelo no trajeto que aplicam Chill (área persistente, Proc 0,25; duração: lacuna, ver D02). *Inativo (Augment)* com Winter Orb | [50–70]% convertido | E | 7 |
| 05 | Growing Void | Area | — | O raio cresce a cada inimigo atingido (máx. 5) | +[10–15]% raio por inimigo | E | 7 |
| 06 | Unstable Orb | Trigger | — | Ao tocar um inimigo com Shock ou Burning, detona imediatamente com explosão extra de 2,5 m | [55–80]% skill | E | 8 |
| 07 | Echoing Collapse | Echo | — | A explosão final se repete após 1 s | [50–65]% do dano | L | 11 |
| 08 | Charge Battery | Resource | — | Cada carga consumida concede dano adicional à implosão no bucket Increased local (separado do coeficiente por carga da base ou de Overcharged Orb) | +[12–18]% Increased local por carga | E | 8 |
| 09 | Rift Orb | Duration | — | A explosão final deixa uma fenda que pulsa a cada 0,5 s. A fenda é uma zona Void; ao terminar não dispara Implosion (07) | Fenda dura [1,7–2,5] s; 25% do dano da explosão por pulso | E | 7 |
| 10 | Cold Collapse | Elemental | — | A explosão final adiciona dano Frost e Chill pesado | +[20–30]% skill como Frost | E | 6 |

#### LYR-SR — Shattered Reality
*Tags (08):* Spell, Area, Void · *Premissa (08 §5.4):* área de raio 4 m a até 14 m (no cursor) que detona após 0,6 s, 350% DA; **Consome** cargas (+1 fratura extra por carga, 40% DA cada, nos inimigos mais próximos a até 6 m); CD 10 s. *Leitura dos modificadores:* "zona" = a área de 4 m; modificadores que citam duração ou efeitos "dentro da zona" fazem a área persistir por 3 s após a detonação (a persistência é concedida pelo próprio modificador; 00 R51).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Fracture Lines | Behavior | F | A área vira 3 linhas em estrela de 10 m cada a partir do cursor, que persistem 4 s; as fraturas extras por carga continuam surgindo nos inimigos próximos. *Inativo (Augment)* com Collapse Point | [85–100]% do dano por linha | E | 8 |
| 02 | Time Dilation | Behavior | — | A zona persiste 3 s após a detonação (00 R51); inimigos dentro sofrem Slow genérico (06); projéteis inimigos dentro ficam 60% mais lentos | Slow [30–45]% | E | 7 |
| 03 | Mirror Zone | Echo | — | Uma cópia da zona central (réplica Echo, Gen 1, Proc 0,5) surge sobre um inimigo aleatório a até 8 m; não cria fraturas extras e não consome nem gera cargas | [60–75]% do dano | L | 12 |
| 04 | Collapsing Reality | Trigger | — | A zona persiste 3 s (00 R51); ao fim, implode causando parte do dano total que causou (cap por alvo: lacuna, ver D02); a implosão é instância de Trigger (Gen 1) | [25–35]% do total | E | 8 |
| 05 | Permafrost Rift | Conversion | C | 100% Frost; acúmulo de Freeze acelerado dentro da zona | +[80–120]% acúmulo de Freeze | E | 8 |
| 06 | Expanding Fracture | Duration | — | A zona persiste 3 s após a detonação (00 R51), cresce do raio-base até +75% de raio ao longo da duração e dura mais | +[1,4–2,0] s de duração | E | 7 |
| 07 | Reality Anchor | Behavior | F | A zona fica centrada em Lyra e a acompanha durante o atraso de 0,6 s e durante qualquer persistência concedida por outro modificador; as fraturas extras surgem nos inimigos a até 6 m de Lyra; raio −40% | +[20–30]% Increased local | E | 7 |
| 08 | Inward Spiral | Area | — | A zona persiste 3 s (00 R51); raio maior e Pull leve contínuo para o centro (em chefes vira Distortion, 07; R10) | +[17–25]% raio | E | 7 |
| 09 | Frozen Fracture | Elemental | — | Adiciona dano Frost e acúmulo de Chill dentro da zona | +[20–30]% skill como Frost | E | 6 |
| 10 | Unstable Pages | Resource | — | Inimigos que morrem dentro da zona têm chance de devolver 1 Arcane Charge (máx. 2 por conjuração) | [10–15]% de chance | E | 6 |

#### LYR-AS — Astral Shift
*Tags (08):* Spell, Movement, Frost · *Premissa (08 §5.4):* teleporte de 7 m (2 cargas, CD 7 s), Lyra intangível durante o teleporte, nova de Frost de 80% DA na origem; gera +1 Arcane Charge (08 §5.1).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Void Wake | Behavior | — | Deixa uma fenda na origem que explode após 1 s (3 m) | [80–120]% skill | E | 7 |
| 02 | Frost Step | Elemental | — | O destino libera nova de gelo de 3 m com Chill pesado | [40–60]% skill como Frost | E | 6 |
| 03 | Phase Charges | Behavior | M | Ganha +1 carga (total 3) | Recarga por carga = [120–110]% do cooldown base | E | 7 |
| 04 | Swap Shift | Behavior | F | Mira um inimigo a até 7 m (não afeta chefes nem Guardians) e troca de posição com ele; aplica Vulnerability por 3 s (valor fixo por fonte, no teto de +60%, R12) | Vulnerability +[10–15]% | L | 11 |
| 05 | Arcane Refund | Resource | — | Astral Shift tem chance de conceder 1 Arcane Charge extra (além do +1 da base) | [27–40]% de chance | E | 7 |
| 06 | Return Vector | Echo | F | Após 2 s, Lyra retorna à origem (pode cancelar); o retorno é réplica Echo do teleporte (Gen 1): causa dano no caminho e não gera Arcane Charge | [50–70]% skill | L | 10 |
| 07 | Lingering Phase | Duration | — | i-frames mais longos e Damage Reduction após o blink | i-frames (intangibilidade, 08 l.224) +[0,24–0,35] s; Damage Reduction [20–30]% por 1,5 s (cap de 01) | E | 7 |
| 08 | Slipstream Cast | Trigger | — | Atravessar um inimigo com o teleporte faz a próxima habilidade **Consome** em até 2 s escalar pelas cargas sem gastá-las (ICD 4 s) | +[17–25]% Increased local nessa habilidade | E | 7 |

#### LYR-AZ — Absolute Zero (Ultimate)
*Tags (08):* Spell, Area, Ultimate, Frost · *Premissa (08 §5.4):* cast de 1 s, raio 8 m ao redor de Lyra, 500% DA +100% por carga; **Consome** cargas; congela não-chefes por até 3 s (exceção de Ultimate, 07 §4.8; DR de CC de 06) e aplica Frostbound em chefes; **ao terminar, Lyra fica com as cargas no máximo**; CD 100 s. *Leitura dos modificadores:* "canal" = o cast de 1 s; "fim do canal" = a detonação.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Lingering Winter | Duration | — | A detonação deixa um campo do raio da habilidade que persiste e aplica Chill (área persistente, Proc 0,25) | Campo dura [4–6] s | E | 8 |
| 02 | Glacial Prison | Behavior | F | Foco em um alvo: aplica Freeze de 3 s (exceção de Ultimate, 07 §4.8; DR de 06) — em chefes, Frostbound (07) e Stagger pesado — e depois explode em 4 m causando Shatter | Explosão [200–260]% skill | L | 14 |
| 03 | Void Zero | Conversion | C | Converte 50% do dano em Void (mantém Frost e ganha Void, 03 §3.5); durante o cast e o Freeze, puxa continuamente os inimigos para o centro (Pull 07; chefes: Distortion, R10) | Pull [2–3] m/s | E | 9 |
| 04 | Mobile Blizzard | Behavior | M | Absolute Zero vira um canal de até 3 s centrado em Lyra (adiciona a tag Channel; raio −25%); Lyra se move durante o canal; o dano da base é dividido em pulsos a cada 0,5 s e cada inimigo é congelado na primeira vez que é atingido (DR de 06); as cargas são consumidas no início | Velocidade [50–70]% | E | 9 |
| 05 | Shatterstorm | Trigger | — | Cada inimigo Frozen estilhaçado durante o efeito reduz o cooldown da Ultimate | [2,7–4,0]% por inimigo (máx. 40%) | E | 9 |
| 06 | Echo of Winter | Echo | — | Ao terminar, uma réplica Echo da detonação (Gen 1, Proc 0,5) ocorre no mesmo ponto 1,5 s depois; seu Freeze segue a DR de 06; a réplica não consome nem repõe cargas | [40–55]% do dano | L | 13 |
| 07 | Frozen Sanctum | Area | — | Raio maior; enquanto durar o Freeze da habilidade (ou o campo de LYR-AZ-01, se ativo), Lyra recebe Damage Reduction se estiver dentro do raio do ponto de conjuração (cap de 01) | +[17–25]% raio; 15% Damage Reduction | E | 9 |
| 08 | Storm Front | Elemental | — | Adiciona dano Storm e aplica Shock (ponte para Resonance Frost + Storm) | +[20–30]% skill como Storm | E | 8 |
| 09 | Glacial Reserve | Resource | — | A cada [4–3] inimigos congelados por Absolute Zero (máx. 2), uma das próximas habilidades **Consome** em até 10 s escala pelas cargas sem gastá-las | 1 uso a cada [4–3] congelados | E | 8 |

### 4.3 Nyx — The Echo Hunter

#### NYX-PS — Phantom Shot
*Tags (08):* Attack, Projectile, Ranged, Storm · *Premissa (08 §6.4):* projétil de 20 m que perfura 2, sem cooldown, pode ser disparado em movimento; custa 15 Echo Energy (+10 Echo Energy por disparo em alvo Marcado por Hunter's Mark, 08 §6.1/§6.2).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Fork Shot | Chain | — | No primeiro acerto, divide-se em 2 fragmentos (±30°); fragmentos são Chain (Gen+1, Proc 0,5), não perfuram e não se dividem de novo | [50–60]% dano cada | E | 8 |
| 02 | Ricochet Bolt | Chain | — | Ricocheteia entre inimigos, −15% por salto | [2–3] saltos | E | 8 |
| 03 | Charged Shot | Behavior | M | Segurar até 0,8 s (Nyx pode se mover carregando; soltar antes dispara o Phantom Shot normal). O disparo completo perfura todos os inimigos e aplica +3 Shock (07) | +[70–100]% Increased local | E | 9 |
| 04 | Phantom Volley | Behavior | F | Vira rajada de 3 disparos rápidos pelo custo de 1. O ganho de +10 Echo Energy em alvo Marcado (08 §6.1) ocorre no máximo 1 vez por rajada | [40–45]% dano cada | E | 8 |
| 05 | Echo Shot | Echo | — | Chance de repetir o disparo instantaneamente | [25–35]% de chance | E | 9 |
| 06 | Void Arrow | Conversion | C | Converte parte do dano em Void. O 3º acerto de Phantom Shot no mesmo alvo em 4 s causa uma explosão Void de 2 m (Trigger, Gen+1, Proc 0,5; não é Implosion de 07 e não aplica nem consome Hunter's Mark) | [50–70]% convertido; explosão [60–80]% wpn | L | 11 |
| 07 | Static Feedback | Resource | — | Um disparo que causa crítico restaura Echo Energy (máx. 1 vez por disparo) | [4–6] Echo Energy | E | 6 |
| 08 | Marked Prey | Trigger | — | Cada acerto de Phantom Shot conta como 2 acertos consecutivos para Hunter's Mark (08 §6.2). Phantom Shot contra alvo Marcado recebe bônus de dano | +[10–15]% Increased local | E | 8 |

#### NYX-SS — Shadowstep
*Tags (08):* Attack, Movement, Physical · *Premissa (08 §6.4):* dash de 8 m atravessando inimigos (100% DA aos atravessados), 2 cargas, CD 4 s, custa 20 Echo Energy; o próximo ataque em até 2 s tem +50% Critical Chance; aciona a passiva Afterimage (08 §6.2).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Lightning Dash | Elemental | — | Inimigos atravessados recebem dano Storm adicional e 1 Shock (07) | +[50–70]% wpn como Storm | E | 7 |
| 02 | Phantom Lure | Behavior | — | A imagem residual da passiva Afterimage (08 §6.2) deixada por Shadowstep dura 2 s, aplica Taunt (06) a inimigos em 5 m e explode ao sumir | [40–60]% wpn | E | 7 |
| 03 | Blink Strike | Behavior | F | Mira um inimigo a até 8 m e surge atrás dele; ele recebe os 100% DA do dash (o trajeto conta como a linha origem→destino). O próximo ataque em até 2 s é crítico garantido | +[20–30]% Critical Damage nesse ataque | L | 12 |
| 04 | Chain Step | Behavior | M | Após um Shadowstep, até 2 dashes extras em 1,5 s; os extras não consomem carga nem iniciam cooldown, e cada um conta como Shadowstep (aciona Afterimage) | 2º/3º dash custam [50–35]% do custo base | E | 8 |
| 05 | Rewind Step | Echo | F | 1,5 s depois, Nyx volta à origem recuperando parte da vida perdida nesse intervalo | [8–12]% da vida perdida | L | 10 |
| 06 | Kinetic Reload | Resource | — | Atravessar ao menos um inimigo devolve parte do custo (1 vez por dash) | [35–50]% do custo | E | 6 |
| 07 | Execution Step | Trigger | — | Atravessar inimigo comum abaixo do limiar o executa (Execute, 06) e devolve 1 carga de Shadowstep (ICD 1 s); Elites: metade do limiar; Guardians e chefes imunes, e contra chefes vale a conversão R08 (+25% Vulnerability abaixo de 20% de vida, uma conversão, sem acumular, dentro do teto de 60%) (§2 item 14) | Limiar [15–20]% vida | L | 12 |
| 08 | Static Wake | Area | — | Ao chegar, libera explosão elétrica ao redor de Nyx | Raio [2,5–3,5] m; [40–60]% wpn | E | 7 |

#### NYX-ST — Storm Trap
*Tags (08):* Attack, Trap, Area, Storm · *Premissa (08 §6.4):* arremesso até 10 m, arma em 0,5 s, gatilho raio 3 m, máx. 3 ativas (somem após 20 s); 220% DA + Shock e encadeia para 3 alvos; custa 25 Echo Energy, CD 8 s.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Tesla Network | Behavior | — | Storm Traps armadas a até 8 m entre si são ligadas por feixes elétricos; inimigos que cruzam um feixe sofrem dano Storm por tick de 0,5 s (área persistente, Proc 0,25). Com o Augment Overcharge Trap, os arcos do Augment são estes feixes (sem duplicar o dano) | Feixes [16–24]% wpn/s | L | 12 |
| 02 | Thrown Trap | Behavior | F | A armadilha arma instantaneamente (em vez de 0,5 s) e o alcance de arremesso aumenta em 50% (10 → 15 m; Crossbow 16 → 24 m) | +[14–20]% Increased local | E | 6 |
| 03 | Overload Trap | Trigger | — | Detonar em alvo que já tem Shock dispara Overload (07) imediatamente (respeita o ICD de 5 s por alvo de 07 e zera Shock); esse Overload recebe bônus de dano | +[40–60]% Increased local | E | 8 |
| 04 | Magnetic Snare | Elemental | — | Puxa inimigos em 4 m antes de detonar; adiciona dano Void | +[20–30]% skill como Void | E | 8 |
| 05 | Pulse Trap | Duration | M | Dispara 3 vezes (a cada 1 s). *Inativo (Augment)* com Echo Trap | [45–55]% dano por pulso | E | 8 |
| 06 | Echo Mine | Echo | — | Ao detonar, deixa uma armadilha-eco que detona 1,5 s depois | [50–65]% do dano | L | 11 |
| 07 | Trap Battery | Resource | — | Cada inimigo atingido restaura Echo Energy | [2–3] Echo Energy por inimigo | E | 6 |
| 08 | Static Field Trap | Area | — | Raio maior; a detonação deixa um Static Field (07) por 3 s | +[20–30]% raio | E | 8 |
| 09 | Chain Snare | Chain | — | A detonação salta para inimigos próximos e para outras Storm Traps armadas, detonando-as (Gen+1) | +[2–3] saltos a 50% | E | 8 |

#### NYX-EC — Echo Clone
*Tags (08):* Clone, Echo, Summon · *Premissa (08 §6.4):* clone por 8 s que segue Nyx e replica habilidades (exceto Ultimate) a 40% e básicos a 25%; 30% da vida de Nyx; cópias IsEcho com PC 0,4; custa 30 Echo Energy, CD 15 s.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Twin Clone | Behavior | F | Cada conjuração cria 2 clones com dano reduzido (contam como 1 no limite de clones). *Inativo (Augment)* com Perfect Mirror | [25–30]% dano cada | L | 11 |
| 02 | Explosive Echo | Trigger | — | O clone explode ao expirar ou morrer (3 m) | [120–160]% wpn Storm | E | 7 |
| 03 | Stationary Turret | Behavior | F | O clone fica parado onde surgiu, continua replicando habilidades a partir dali e dispara Phantom Shot sozinho 1,5×/s sem custo (cópia de clone, Proc 0,4); dura +4 s. Com Storm Double, prevalece a cadência deste modificador (não somam) | [35–45]% dano por disparo | E | 8 |
| 04 | Flawless Mirror | Behavior | — | O clone replica habilidades com mais dano (soma ao percentual base e de Augments, máx. 100%), mas dura 3 s a menos | +[20–30] pontos percentuais de replicação | L | 12 |
| 05 | Phase Swap | Behavior | — | Enquanto um clone estiver ativo, usar Echo Clone de novo (sem custo e sem cooldown, 1 vez por clone) troca as posições de Nyx e do clone | i-frames [0,5–0,75] s na troca | E | 6 |
| 06 | Lingering Echo | Duration | — | Duração maior | +[3–4,5] s | E | 7 |
| 07 | Taunting Shade | Behavior | — | O clone aplica Taunt (06) a inimigos em 8 m ao surgir e a cada 3 s (com Shadow Decoy, só amplia o raio do Taunt do Augment para 8 m) e tem mais vida | +[30–45] pontos percentuais de vida (base 30% da de Nyx) | E | 7 |
| 08 | Spectral Reach | Area | — | Cópias do clone ganham área e alcance (raio de Storm Trap e de efeitos de área; alcance de Phantom Shot; distância do Shadowstep) | +[20–30]% área e alcance | E | 7 |
| 09 | Stormbound Echo | Elemental | — | Ataques do clone adicionam dano Storm e aplicam Shock | +[20–30]% do dano do clone como Storm | E | 7 |

#### NYX-TR — Thousand Reflections (Ultimate)
*Tags (08):* Attack, Area, Ultimate, Clone, Echo, Storm · *Premissa (08 §6.4):* área de raio 6 m ao redor do alvo Marcado (ou do cursor) por 4 s; 12 golpes de 120% DA + golpe final de 400% DA; Nyx Intangível; sem custo, CD 90 s.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Endless Reflections | Duration | — | Duração maior (mais golpes) | +[1,0–1,5] s | E | 9 |
| 02 | Focused Mirror | Behavior | F | Todos os reflexos atacam o inimigo com mais vida | +[40–60]% Increased local | L | 13 |
| 03 | Storm Prism | Elemental | — | Cada golpe salta como raio para 2 inimigos próximos (Gen+1, Proc 0,5; respeita os limites de Chain de 07) | [30–40]% do dano do golpe | E | 9 |
| 04 | Reflection Army | Behavior | F | Ao terminar, até 4 reflexos permanecem por 6 s como clones (fora do limite de Echo Clone) que replicam Phantom Shot, Shadowstep e Storm Trap (cópia de clone, Proc 0,4; não replicam Echo Clone nem a Ultimate) | [15–20]% dano replicado cada | L | 14 |
| 05 | Echo Charge | Resource | — | Cada golpe restaura Echo Energy e reduz o cooldown de Echo Clone em 0,3 s | [2–3] Echo Energy por golpe | E | 7 |
| 06 | Execution Mirage | Trigger | — | Cada inimigo morto gera um reflexo extra (máx. 6) | [60–75]% dano do reflexo | E | 9 |
| 07 | Endless Cuts | Chain | — | Cada golpe de reflexo que mata salta para outro inimigo a até 6 m | +[2–3] saltos a 50% | E | 9 |

### 4.4 Orun — The Voidkeeper

#### ORU-VT — Void Tendrils
*Tags (08):* Spell, Area, Void · *Premissa (08 §7.4):* 3 tentáculos em raio 3 m a até 10 m; sem custo e sem cooldown (cast 0,5 s); 140% DA + Slow 30% por 2 s; gerador principal de Void Mass.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Grasping Abyss | Behavior | F | Vira círculo de 4 m ao redor de Orun; os atingidos sofrem Root (06) antes do Slow. Com Binding Tendrils, soma-se ao Root do Augment (DR de CC de 06) | Root [0,4–0,6] s | E | 7 |
| 02 | Strangle | Behavior | M | Vira canal de até 3 s (ganha tag Channel): os tentáculos drenam a cada 0,5 s, mantêm o Slow e curam Orun em 2% do dano causado; cada 1 s de canal conta como 1 uso para Void Mass (08 §7.1) | [55–80]% do dano do acerto por tick (0,5 s) | E | 8 |
| 03 | Ruin Tendrils | Conversion | C | Converte 100% do dano em Ruin (ganha Ruin, perde Void). Com Rotting Tendrils ativo, a conversão é a do Augment e este modificador mantém apenas o bônus de buildup | +[40–60]% buildup de Decay | E | 7 |
| 04 | Rending Pull | Behavior | — | Antes do Slow, aplica Pull (07) de 3 m nos atingidos em direção a Orun (chefes: Distortion, 00 R10) | +[12–18]% Increased local | E | 6 |
| 05 | Lingering Tendrils | Duration | — | Os tentáculos permanecem e reaplicam o controle da habilidade (Slow 30%; Root com Binding Tendrils) em quem entrar, 1× por inimigo por permanência. Máx. 2 permanências ativas; a mais antiga se desfaz | [2–3] s | E | 7 |
| 06 | Construct Conduit | Trigger | — | Tentáculos também surgem de até 4 Void Constructs ativas (instâncias Trigger, Gen+1, Proc 0,5). Não acumula com Commander's Will: com o Keystone ativo, cada construção usa o maior dos dois valores | [25–35]% do dano por construção | L | 13 |
| 07 | Feeding Tendrils | Resource | — | Cada conjuração que atinge inimigos reduz o cooldown restante de Guardian Fragment (máx. 3 inimigos por conjuração; ICD 2 s) | [0,2–0,3] s por inimigo | E | 6 |
| 08 | Withering Grasp | Elemental | — | Adiciona dano Ruin; alvos que já estavam sob Slow ou Root desta habilidade recebem 1 acúmulo de Weakening (07, garantido) | +[20–30]% skill como Ruin | E | 6 |

#### ORU-GF — Guardian Fragment
*Tags (08):* Summon, Construct, Defensive, Void · *Premissa (08 §7.4):* CD 10 s; invoca 1 **Sentinel** (30 s, 60% da vida de Orun) que golpeia (Slam 150% DA) a cada 2 s e aplica Taunt a cada 5 s em 6 m; ocupa 1 Void Construct (com limite cheio, substitui o Lesser Fragment mais antigo).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Shard Sentinels | Behavior | F | Invoca 3 Sentinels menores que juntos ocupam 1 Void Construct e contam como 1 construção (Voidkeeper, sacrifício, The Unbound). Taunt e Augments do Sentinel (Aegis Monolith, Plague Idol) valem só no primeiro deles | [40–50]% da vida e do dano do Sentinel, cada | E | 8 |
| 02 | Aegis Link | Behavior | — | O Sentinel se liga a Orun (até 12 m) e redireciona para si parte do dano que Orun recebe. Com Aegis Monolith ativo, em vez disso, o dano que o Sentinel recebe pela ligação é reduzido no mesmo valor | [20–30]% (redirecionado ou reduzido) | E | 8 |
| 03 | Void Bastion | Behavior | F | Vira pilar estacionário sem Slam (mantém o Taunt) que pulsa escudo a cada 1 s em 4 m | Escudo de [6–9]% da vida máx. de Orun por pulso (teto 30%) | L | 12 |
| 04 | Ruin Spikes | Conversion | C | Os ataques do Sentinel são 100% convertidos em Ruin e podem aplicar 1 acúmulo de Weakening (07, por chance) | [40–60]% de chance de Weakening por ataque | E | 7 |
| 05 | Volatile Construct | Trigger | — | Quando o Sentinel morre ou expira, explode em 3,5 m (Trigger, Gen+1). Não dispara quando ele é sacrificado (Devouring Rift, The Unbound) | [150–200]% do dano do Slam | E | 7 |
| 06 | Enduring Fragment | Duration | — | Dura mais | +[40–60]% duração | E | 6 |
| 07 | Mirrored Construct | Echo | — | Chance de repetir cada ataque | [20–30]% de chance | E | 8 |
| 08 | Construct Overflow | Resource | — | +1 no limite de Void Constructs (exceção de 08 §11.1 — pendente P2; teto absoluto 8, 00 R01); todas as construções têm menos vida | −[15–10]% vida das construções | L | 12 |
| 09 | Crushing Fragment | Area | — | Ataques do fragmento viram ondas de impacto de 2,5 m | [70–85]% do dano por alvo | E | 8 |
| 10 | Void-Laced Fragment | Elemental | — | Os ataques do Sentinel adicionam dano Void e aplicam Pull de 1 m em direção a ele (07; chefes: Distortion, 00 R10) | +[20–30]% do dano como Void | E | 7 |

#### ORU-DR — Devouring Rift
*Tags (08):* Spell, Area, DoT, Void · *Premissa (08 §7.4):* CD 12 s; fenda de raio 3,5 m por 4 s a até 12 m, 80% DA a cada 0,5 s com Pull; segurar a tecla ao conjurar sacrifica 1 construção (duração ×2, raio +50%); ao terminar, Implosion (07).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Gravitic Maw | Behavior | — | Puxão forte: aplica Pull (07) em inimigos a até 6 m do centro, até 4 m por aplicação (chefes: Distortion, 00 R10); dano reduzido | Penalidade [−15 a −10]% dano | E | 7 |
| 02 | Hungry Rift | Trigger | — | Cada inimigo que morre dentro aumenta o raio e a duração (+0,5 s por morte, máx. +2 s) | +[8–12]% raio por morte (máx. +60%) | E | 8 |
| 03 | Wandering Rift | Behavior | F | A fenda se move a 3 m/s em direção ao maior grupo próximo | +[10–15]% Increased local | E | 7 |
| 04 | Early Collapse | Behavior | M | Pressionar a tecla de novo enquanto a fenda existe (sem custo e sem cooldown; não é o "segurar" do sacrifício) colapsa a fenda e causa de imediato parte do dano restante. O colapso conta como término (Implosion, Blight Rift) | [40–60]% do dano restante | E | 7 |
| 05 | Rift Twins | Echo | — | Uma segunda fenda abre sobre o grupo de inimigos mais próximo | [50–65]% do dano | L | 12 |
| 06 | Decay Rift | Conversion | C | Converte parte do dano em Ruin; inimigos dentro recebem buildup de Decay ×2 de qualquer fonte. Com Blight Rift ativo, vale a conversão do Augment e este modificador mantém só o buildup ×2 | [60–80]% convertido | E | 7 |
| 07 | Nurturing Abyss | Resource | — | Construções dentro da fenda se curam e atacam mais rápido | Cura [3–4,5]% da vida/s; +[16–24]% velocidade de ataque | E | 7 |
| 08 | Widening Maw | Area | — | Raio maior; inimigos no centro (1,5 m) recebem mais dano | +[20–30]% raio; +[16–24]% Increased local no centro (1,5 m) | E | 7 |
| 09 | Lingering Maw | Duration | — | A fenda dura mais (soma após o ×2 do sacrifício) | +[1,6–2,4] s | E | 7 |
| 10 | Rotting Rift | Elemental | — | Adiciona dano Ruin; a cada 1 s, inimigos dentro recebem 1 acúmulo de Decay (07, garantido) | +[20–30]% skill como Ruin | E | 6 |

#### ORU-CW — Corruption Wave
*Tags (08):* Spell, Area, Ruin · *Premissa (08 §7.4):* CD 8 s; onda em cone de 10 m, 200% DA, aplica Decay e Knockback; construções atingidas emitem onda própria com 50% do dano na mesma direção (instância derivada).

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Returning Tide | Behavior | — | A onda de Orun volta até ele e atinge de novo (instância derivada, Gen+1). Ondas emitidas por construções não retornam | [50–65]% do dano no retorno | E | 8 |
| 02 | Expanding Ring | Behavior | F | Vira anel de 360° com 8 m; construções atingidas emitem sua onda na direção oposta a Orun | [85–100]% do dano | E | 7 |
| 03 | Contagion | Chain | — | Inimigos atingidos pela onda que morrem em até 4 s com Decay espalham seus acúmulos para inimigos próximos (até 4 m; Gen+1, não reespalha) | [2–3] alvos | E | 8 |
| 04 | Plague Wall | Behavior | F | A onda perde o Knockback, para a 6 m e vira parede por 4 s; quem atravessa recebe 1 acúmulo de Decay (garantido; ICD por alvo 1 s) e Slow. Ondas emitidas por construções não viram parede | Slow [30–40]% | L | 11 |
| 05 | Detonating Wave | Trigger | — | A onda detona o Decay existente (Detonation, 07) | [60–80]% de eficiência | L | 12 |
| 06 | Void Tide | Conversion | C | Converte parte do dano em Void; aplica Pull (07, até 4 m) em direção a Orun em vez de Knockback (chefes: Distortion, 00 R10) | [50–70]% convertido | E | 7 |
| 07 | Empowering Wave | Trigger | — | Construções atingidas pela onda ficam fortalecidas por 4 s | +[20–30]% Increased local de Summon | E | 7 |
| 08 | Spreading Blight | Area | — | Onda mais larga e mais longa | +[25–35]% largura; +2 m de alcance | E | 7 |
| 09 | Lingering Rot | Duration | — | O Decay aplicado pela onda dura mais e a onda deixa um rastro por 3 s que aplica 1 acúmulo de Decay (garantido) por segundo a quem estiver nele | +[30–45]% duração do Decay | E | 7 |
| 10 | Void Undertow | Elemental | — | Adiciona dano Void; ao fim, aplica Pull (07) de 2 m de volta em direção a Orun nos atingidos (chefes: Distortion, 00 R10) | +[20–30]% skill como Void | E | 7 |

#### ORU-UB — The Unbound (Ultimate)
*Tags (08):* Spell, Area, Ultimate, Void, Ruin · *Premissa (08 §7.4):* CD 100 s; sacrifica todas as construções (cada uma +2 s e +10% Increased Damage); transformação de 10 s com Unstoppable, açoites automáticos de 120% DA a cada 0,5 s e explosão final de 500% DA; usável com 0 construções.

| ID | Modificador | Cat. | Gr. | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|
| 01 | Abyssal Titan | Behavior | F | Em vez de transformar Orun, invoca um Titã por 12 s (ganha Summon; não ocupa limite), com os açoites (120% DA a cada 0,5 s) e a explosão final (500% DA) da forma; Orun não fica Unstoppable. Herald of Ruin e Titan of the Void valem durante o Titã. *Inativo (Augment)* com Abyssal Host | +[25–35]% de vida e dano do Titã por construção consumida | L | 14 |
| 02 | Unchained Detonation | Behavior | F | Cada construção consumida explode no lugar (4 m; Trigger, Gen+1). *Inativo (Augment)* com Abyssal Host | [200–260]% DA por explosão | E | 9 |
| 03 | Preserved Bonds | Resource | — | Não consome as construções: elas permanecem ativas e cada uma concede uma fração do bônus de sacrifício (+2 s e +10% Increased Damage). *Inativo (Augment)* com Abyssal Host | [60–75]% do bônus por construção | L | 13 |
| 04 | Endless Night | Duration | — | Duração maior | +[2,4–3,6] s | E | 9 |
| 05 | Ruin Ascendance | Conversion | C | Durante a forma, 50% de todo o dano de Orun (não das construções) é convertido em Ruin | +[30–45]% buildup de Decay | E | 8 |
| 06 | Echoing Release | Echo | — | Ao fim, libera um pulso de 8 m baseado no poder acumulado | [30–45]% do dano total causado durante o efeito (cap por alvo: 15% vida máx.) | E | 10 |
| 07 | Vast Form | Area | — | Durante o efeito, todas as habilidades de Orun ganham área | +[20–30]% área | E | 9 |
| 08 | Rotting Unbound | Elemental | — | Durante o efeito, ataques de Orun adicionam dano Ruin | +[16–24]% como Ruin | E | 8 |
| 09 | Insatiable Night | Trigger | — | Abates durante o efeito prolongam a duração (ICD 0,2 s) | +[0,3–0,45] s por abate (máx. +4 s) | E | 9 |

---

## 5. Catálogo genérico (por tag)

Genéricos são atribuídos a **uma** habilidade elegível do herói atual (§3.2). `Exclui` lista tags que impedem a atribuição.

| ID | Modificador | Cat. | Gr. | Requer | Exclui | Efeito fixo | Valor variável | Mín. | BP |
|---|---|---|---|---|---|---|---|---|---|
| GEN-01 | Emberbrand | Conversion | C | Attack ou Spell | Ultimate, Ember | Converte dano em Ember | [35–50]% | E | 5 |
| GEN-02 | Rimebrand | Conversion | C | Attack ou Spell | Ultimate, Frost | Converte dano em Frost | [35–50]% | E | 5 |
| GEN-03 | Stormbrand | Conversion | C | Attack ou Spell | Ultimate, Storm | Converte dano em Storm | [35–50]% | E | 5 |
| GEN-04 | Voidbrand | Conversion | C | Attack ou Spell | Ultimate, Void | Converte dano em Void | [35–50]% | E | 5 |
| GEN-05 | Echobrand | Conversion | C | Attack ou Spell | Ultimate, Echo | Converte dano em Echo | [35–50]% | E | 5 |
| GEN-06 | Ruinbrand | Conversion | C | Attack ou Spell | Ultimate, Ruin | Converte dano em Ruin | [35–50]% | E | 5 |
| GEN-07 | Splitting | Chain | — | Projectile | Area | No primeiro acerto, divide-se em 2 projéteis (Gen+1; os projéteis divididos não se dividem de novo) | [35–45]% dano cada | E | 7 |
| GEN-08 | Piercing | Behavior | — | Projectile | Area | Atravessa +1 inimigo adicional | +[10–15]% Increased local | E | 6 |
| GEN-09 | Returning | Behavior | — | Projectile | Area, Channel | O projétil volta ao herói, atingindo de novo (o acerto de retorno é instância derivada: Gen+1, proc 0,5) | [50–65]% no retorno | E | 7 |
| GEN-10 | Concentrated Effect | Area | — | Area | — | −25% área; mais dano | +[20–28]% Increased local | E | 6 |
| GEN-11 | Wide Reach | Area | — | Area | — | Aumenta a área | +[17–25]% área | E | 6 |
| GEN-12 | Accelerated Affliction | Duration | — | DoT | — | Mesmo dano total, aplicado mais rápido | Duração −[25–35]% (dano/s proporcionalmente maior) | E | 5 |
| GEN-13 | Lingering Affliction | Duration | — | DoT | — | DoTs da habilidade duram mais | +[24–35]% duração | E | 6 |
| GEN-14 | Kinetic Wake | Behavior | — | Movement | — | O trajeto deixa um rastro de dano do tipo de dano final da habilidade (após Conversão) por 2 s | [20–30]% wpn/s | E | 6 |
| GEN-15 | Evasive Momentum | Trigger | — | Movement | — | Após usar, ganha Movement Speed e Dodge Chance por 2 s (renova, não acumula; ICD 1 s) | +[17–25]% MS; +10% Dodge | E | 6 |
| GEN-16 | Pack Frenzy | Trigger | — | Summon, Construct ou Clone | Ultimate | Quando o herói usa outra habilidade, as invocações desta ganham Attack Speed por 3 s (renova, não acumula); em Clone, as réplicas do clone ganham o mesmo valor como Increased local | +[17–25]% | E | 7 |
| GEN-17 | Sacrificial Spark | Trigger | — | Summon, Construct ou Clone | Ultimate | Invocações desta habilidade explodem ao morrer ou expirar (2,5 m; sacrifício não conta) | [60–90]% Summon base | E | 6 |
| GEN-18 | Hair Trigger | Behavior | — | Trap | — | Armadilhas armam instantaneamente; +1 armadilha simultânea | +[10–15]% Increased local | E | 6 |
| GEN-19 | Retaliation | Trigger | — | Defensive | — | Após usar, a próxima outra habilidade Attack ou Spell (não Basic/Heavy) usada em até 3 s causa mais dano (ICD = cooldown desta habilidade) | +[40–60]% Increased nesse uso | E | 6 |
| GEN-20 | Cataclysmic Recharge | Resource | — | Ultimate | — | Abates reduzem o cooldown desta Ultimate (ICD 0,1 s; redução total máx. 40% do cooldown por ciclo, cap compartilhado com LYR-AZ-05) | [0,35–0,5] s por abate | L | 10 |
| GEN-21 | Rhythm | Trigger | — | Basic | — | Cada 4º golpe básico que acerta gera onda de choque de 3 m do tipo de dano da arma (ICD 0,5 s) | [50–70]% wpn | E | 6 |
| GEN-22 | Crushing Finish | Behavior | — | Heavy | — | Ataques pesados causam mais Stagger; Heavy não canalizado também aplica Knockback leve | +[40–60]% Stagger Damage local | E | 5 |
| GEN-23 | Critical Cascade | Trigger | — | Attack ou Spell, com cooldown | Ultimate, Movement | Críticos têm chance de reiniciar o cooldown (ICD 3 s) | [10–15]% de chance | L | 10 |
| GEN-24 | Resonant Repeat | Echo | — | Attack ou Spell | Ultimate, Channel, Movement, Defensive, Basic, Heavy | Chance de repetir a habilidade a 50% de dano | [15–20]% de chance | L | 10 |
| GEN-25 | Thrifty Casting | Resource | — | Custo > 0 em 08 (condição, não tag) | Ultimate | Reduz o custo (tradução por herói em §3.8; Lyra: chance de não consumir a carga mais recente, 08 §3.2) | [17–25]% (Lyra/Orun: chance) | E | 5 |
| GEN-26 | Long Fuse | Duration | — | DoT, Summon ou Trap | Ultimate | DoTs e áreas persistentes, invocações e armadilhas da habilidade duram mais | +[20–30]% duração | E | 5 |
| GEN-27 | Twin Affinity | Elemental | — | Attack ou Spell | Ultimate | A habilidade também acumula o status primário de uma afinidade (escolhida ao atribuir, diferente das afinidades nas tags da habilidade) com potência reduzida | [40–60]% da Status Potency | E | 6 |
| GEN-28 | Volatile | Trigger | — | Attack ou Spell | — | Abates com esta habilidade causam explosão de 2,5 m do tipo de dano majoritário final da habilidade (após Conversão; ICD 0,5 s) | [40–60]% wpn | E | 7 |
| GEN-29 | Fortifying | Trigger | — | Defensive ou Movement | — | Após usar, ganha Damage Reduction por 2 s (renova, não acumula; ICD 1 s; sujeito ao cap de 01) | +[17–25]% Damage Reduction | E | 6 |
| GEN-30 | Rapid | Behavior | — | Attack ou Spell | Ultimate, Channel | A habilidade é executada mais rápido | +[10–15]% Attack/Cast Speed local | E | 6 |

Notas de design:
- Os **6 "-brand"** e **Twin Affinity** são a principal ponte para Resonance cruzada (GDD §7.2): ex.: Kael com Rimebrand no Cleaving Strike habilita Thermal Shatter (Ember + Frost) sem depender de outro herói. Atendem ao teste de 07 (build com até 2 itens dedicados dispara Resonance a cada ≤ 10 s).
- Genéricos têm BP menor que específicos equivalentes: identidade de herói deve sempre valer um pouco mais.
- **GEN-18** só tem alvo em NYX-ST (única habilidade `Trap` de 08). "Arma instantaneamente" não soma com NYX-ST-02. Decisão pendente (lead/05): peso reduzido no pool `GEN-xx` fora de Nyx.

---

## 6. Orçamento de poder

### 6.1 Budget Points (BP)

**1 BP ≈ +1% de valor efetivo para a habilidade-alvo** no cenário de referência (Eclipse, iLvl ~80, 6 inimigos normais + 1 Elite, 60 s de combate). O valor pode ser dano, sobrevivência equivalente (vida efetiva) ou utilidade convertida (tempo de controle, recurso).

| Faixa | BP | Uso |
|---|---|---|
| Genérico Epic | 5–7 | Ajustes, conversões, pontes de Resonance |
| Específico Epic | 6–9 | Mudança de comportamento comum |
| Legendary-only | 10–14 | Mudança de forma/identidade, Echo pesado, execuções |
| Ultimate | +1–2 sobre a faixa | Ultimates são usadas com menos frequência |

- **Sidegrades** (mudam forma com penalidade, ex.: Infernal Collapse, Reality Anchor) são precificados pelo valor *líquido*, não pelo bônus bruto.
- O BP é por habilidade; o impacto no personagem é `BP × participação da habilidade no DPS/sobrevivência`. Com limite de 3 por habilidade (2 em Ultimate) e máximo de 14 modificadores ativos, a contribuição numérica agregada esperada fica em **~12–18%** do poder total, coerente com o alvo de ~15% de `00-convencoes §4`. O restante do valor de modificadores é *habilitador de build* (comportamento), que não entra no orçamento numérico.

### 6.2 Regras de rolagem

1. Cada modificador tem 1 valor principal rolado (alguns têm um secundário fixo ou rolado junto, sempre na mesma qualidade percentual — ex.: rolagem a 80% da faixa aplica 80% em ambos).
2. **Razão máx/mín ≤ 1,5**; diferença min→max ≤ 5% do DPS total no cenário de referência.
3. A distribuição da rolagem é uniforme dentro da faixa; restrições por iLvl (piso de qualidade) são definidas por 05-loot.
4. Valores "penalidade" (ex.: `[−35 a −25]%`) melhoram em direção ao segundo número.
5. Upgrade/reroll de valor (Enchanter/Blacksmith) pode mover a rolagem dentro da faixa, nunca acima do máximo (10-progressao define custo).

### 6.3 Guardrails de balanceamento

- **Execuções (00 R08):** limiar máx. 20% em comuns; Elites usam metade (máx. 10%); Guardians e chefes imunes — contra chefe, +25% Vulnerability abaixo de 20% de vida (uma conversão, não acumula).
- **Vulnerability (00 R12):** todo valor deste catálogo entra no teto de +60% de 07; a mesma fonte não acumula consigo.
- **Reset de cooldown:** sempre com ICD ≥ 1 s e nunca em Ultimate (exceto redução fracionada, como Shatterstorm, com cap).
- **Dano baseado em "dano total causado"** (Collapsing Reality, Echoing Release) tem cap por alvo.
- **Pull/Knockback em chefes (00 R10):** Pull vira **Distortion** (07; +10% Vulnerability) e Knockback vira Stagger (06). Em Elites com afixo de imunidade a controle (09-inimigos) vira apenas Slow.
- **Freeze** aplicado por modificadores segue a DR única de 06 (00 R09).
- **Cópias** (Echo Clone, Thousand Reflections) não copiam modificadores Echo/Resource; eco de eco só existe via Echo Recursion 1× declarada em 04/08 (00 R07).

---

## 7. Dependências e pontos de integração

| Documento | Consome deste doc | Fornece para este doc |
|---|---|---|
| 01-atributos | Lista de parâmetros locais (Increased local, área, duração) | Tradução de Resource por herói (§3.8, dono), caps de Damage Reduction, fórmulas de CDR, Duration of Effects, Area, Status Potency |
| 02-equipamentos | IDs deste doc para os itens nomeados; **tabela §10.1 para remapear todos os nomes**; regra Epic 75% herói / 25% `GEN-xx`; formato Signature `SIG-<item>` | Hero-bound, efeito dos modificadores Signature dos Uniques |
| 04-conjuntos | **Índice §10.2 (Habilidade × Categoria → IDs)** para escolher o modificador concreto de cada par das 72 peças | Garantia de que Set Powers não ocupam slots nem contam no limite; Echo Recursion 1× de Thousand Echoes |
| 05-loot | Raridade mínima (E/L), peso relativo, BP para valor de item, pool `GEN-xx` (25% dos Epics) | Smart loot (viés para o herói em uso), piso de qualidade por iLvl |
| 06-combate | Proc Coefficient, regra de conversão de dano, posição do Increased local no pipeline | Estados Stagger, Super Armor, Root, Knockback, Parry (0,2 s), Taunt, i-frames, DR de CC, flag Execute |
| 07-resonance | Quais modificadores aplicam/consomem status (Shatter, Overload, Ignition, Decay, Detonation, Vulnerability, Weakening, Static Field); conversões ≥ 50% concedem tag; Twin Affinity | Modelo Generation, valores base dos status, teto de Vulnerability, Distortion/Frostbound; acúmulo multiplicado por Proc Coefficient |
| 08-herois | Premissas base de cada habilidade (§4) | Kit e tags finais (dono); tabela de exceções de recurso (Construct Overflow); exclusões entre Augments e modificadores; Echo Chamber (Echo Recursion 1×) |
| 09-inimigos | Guardrails de execução/pull/controle contra Elites, Guardians e chefes | Afixos de imunidade, categorias Heavy/Guardian/chefe |
| 10-progressao | Necessidade de serviço de reroll de valor (Refine) e de reatribuição de genéricos | Custos de Enchanter/Blacksmith; upgrade não afeta modificadores (00 R31) |
| 11-arquitetura | Anatomia de dado (§3.1), pipeline de 6 fases (§3.4; adotado por 11 — 00 R47), limites e prioridade (§3.3), modelo Generation | Implementação via ScriptableObjects (`SkillModifierDefinition`) + instância rolada; HitFlags |

---

## 8. Conflitos com as convenções

Nenhum conflito em aberto. Resoluções da rodada de integração aplicadas aqui: R07 (modelo Generation substitui a antiga "profundidade de Trigger = 1"), R08 (Execute), R10 (Pull em chefe → Distortion), R12 (teto de Vulnerability), R20 (Parry 0,2 s, Taunt), R25 (Signature), R26 (33 modificadores novos + mapeamento §10), R27 (Hero-bound), R30 (limite global de 14; Orun cita 01), R48 (Event Horizon renomeado para Gravitic Maw; "DR" só para Diminishing Returns).

Esclarecimentos que outros docs devem adotar:

1. **Increased local** de modificadores entra no mesmo Σ Increased da habilidade (06-combate registra no pipeline).
2. **Modificadores não geram More%.** Toda fonte More fica em Set Powers, Uniques, Resonance e passivas-chave (00 R03).
3. **Proc Coefficient** é definido aqui por tipo de instância; o modelo Generation é de 07.
4. **Conversão ≥ 50% concede a tag de afinidade** — 07-resonance e 11-arquitetura consideram tags dinâmicas (máscara de Resonance em 11).
5. **Revisão D01 (2026-11-07):** 206 modificadores revisados contra 08; nenhum ID removido; 10 renomeados por colisão com nomes de 08/07 (KAE-IL-10 Combustion Landing, KAE-BC-07 Molten Knot, KAE-WB-02 Rolling Quake, LYR-FL-03 Glacial Javelin, LYR-FL-06 Shattering Lance, LYR-AZ-01 Lingering Winter, LYR-SR-02 Time Dilation, LYR-SR-08 Inward Spiral, NYX-SS-02 Phantom Lure, NYX-EC-04 Flawless Mirror); faixas ajustadas para razão ≤ 1,5. Pendências em outros docs: relatório §3.

---

## 9. Questões abertas para playtest

1. Limite de 3 modificadores por habilidade: validar se 2 é mais legível ou se 4 é necessário para builds focadas em uma habilidade (Echo Architect, Void Singularity).
2. Proc Coefficient de clones (0,4): validar se Echo Architect fica competitivo com Storm Reaper.
3. Valores de Fury/Echo Energy dos modificadores Resource dependem das taxas base finais de 08-herois.
4. Legibilidade visual com Tesla Network + Echo Mine + Twin Clone simultâneos (GDD §12).
5. Limite global de 14 ativos: validar se builds Rota B (12 modificadores de set + 2 de arma) ficam sem espaço para genéricos.

---

## 10. Anexo — Mapeamento de nomes para 02 e 04 (rodada de integração)

### 10.1 Nomes usados em 02-equipamentos → ID deste doc

Os 11 nomes que já coincidiam (ex.: Burning Arc, Ruin Cleaver, Executioner's Reach, Blood Fury, Infernal Collapse) permanecem. Os demais 99 são substituídos assim (02 passa a usar o nome de 03):

| Habilidade | Nome em 02 | → ID (nome em 03) |
|---|---|---|
| Infernal Leap | Ember Wake / Scorched Path | KAE-IL-02 (Scorched Trail) |
| Infernal Leap | Tectonic Landing | KAE-IL-09 (Tectonic Landing) — novo |
| Infernal Leap | Meteor Fall | KAE-IL-01 (Meteor Descent) |
| Infernal Leap | Echo Landing | KAE-IL-06 (Aftershock) |
| Burning Chains | Rending Links | KAE-BC-03 (Linked Torment) |
| Burning Chains | Gallows Pull | KAE-BC-01 (Chain Pull) |
| Burning Chains | Decaying Shackles | KAE-BC-05 (Ruin Shackles) |
| Burning Chains | Molten Links | KAE-BC-08 (Molten Links) — novo |
| Burning Chains | Spreading Flame | KAE-BC-07 (Molten Knot) |
| Ashen Guard | Riposte Stance | KAE-AG-01 (Ember Riposte) |
| Ashen Guard | Ember Retort | KAE-AG-06 (Ashen Nova) |
| Ashen Guard | Iron Will | KAE-AG-07 (Unyielding) |
| Ashen Guard | Ash Dash | KAE-AG-05 (Lingering Ash) |
| Ashen Guard | Last Bulwark | KAE-AG-03 (Bulwark Stance) |
| Worldbreaker | Fault Line | KAE-WB-01 (Tectonic Fissures) |
| Worldbreaker | Final Verdict | KAE-WB-07 (Executioner's Verdict) |
| Worldbreaker | Pyre Crown | KAE-WB-09 (Pyre Crown) — novo |
| Worldbreaker | Bastion Quake | KAE-WB-06 (Shattered Earth) |
| Worldbreaker | Desperate Strike | KAE-WB-05 (Fury Reservoir) |
| Worldbreaker | Ashfall | KAE-WB-08 (Ashfall) — novo |
| Frost Lance | Splitting Lance | LYR-FL-02 (Splinter Burst) |
| Frost Lance | Twin Lances | LYR-FL-04 (Frost Volley) |
| Frost Lance | Arcane Lance | LYR-FL-09 (Arcane Lance) — novo |
| Frost Lance | Overcharged Lance | LYR-FL-08 (Overflowing Lance) |
| Frost Lance | Shattering Lance | LYR-FL-06 (Shattering Lance) |
| Void Orb | Orbiting Fragments | LYR-VO-02 (Orbital) |
| Void Orb | Event Horizon | LYR-VO-01 (Singularity Core) — nome reservado a 07 |
| Void Orb | Cold Collapse | LYR-VO-10 (Cold Collapse) — novo |
| Void Orb | Rift Orb | LYR-VO-09 (Rift Orb) — novo |
| Void Orb | Charged Orb | LYR-VO-08 (Charge Battery) |
| Void Orb | Grasping Orb | LYR-VO-05 (Growing Void) |
| Shattered Reality | Unstable Pages | LYR-SR-10 (Unstable Pages) — novo |
| Shattered Reality | Gravity Well | LYR-SR-08 (Inward Spiral) — novo; renomeado no D01 (colidia com Augment de Orun) |
| Shattered Reality | Frozen Fracture | LYR-SR-09 (Frozen Fracture) — novo |
| Shattered Reality | Echoing Fracture | LYR-SR-03 (Mirror Zone) |
| Shattered Reality | Collapsing Lattice | LYR-SR-04 (Collapsing Reality) |
| Astral Shift | Recursive Step | LYR-AS-06 (Return Vector) |
| Astral Shift | Veil Decoy | LYR-AS-01 (Void Wake) |
| Astral Shift | Glacial Trail | LYR-AS-02 (Frost Step) |
| Astral Shift | Double Shift | LYR-AS-03 (Phase Charges) |
| Absolute Zero | Lingering Winter | LYR-AZ-01 (Lingering Winter) |
| Absolute Zero | Frozen Sanctum | LYR-AZ-07 (Frozen Sanctum) — novo |
| Absolute Zero | Cold Snap | LYR-AZ-05 (Shatterstorm) — Cold Snap (nome de 02) é LYR-FL-06 (Shattering Lance; nome antigo em 03: Cold Snap) |
| Phantom Shot | Piercing Phantom | NYX-PS-03 (Charged Shot) |
| Phantom Shot | Forked Bolt | NYX-PS-01 (Fork Shot) |
| Phantom Shot | Ricochet | NYX-PS-02 (Ricochet Bolt) |
| Phantom Shot | Echo Volley | NYX-PS-05 (Echo Shot) |
| Phantom Shot | Rapid Phantom | NYX-PS-04 (Phantom Volley) |
| Phantom Shot | Execution Round / Assassin's Mark | NYX-PS-08 (Marked Prey) |
| Phantom Shot | Deathmark | NYX-PS-06 (Void Arrow) |
| Shadowstep | Behind the Veil | NYX-SS-03 (Blink Strike) |
| Shadowstep | Reload Step | NYX-SS-06 (Kinetic Reload) |
| Shadowstep | Lightning Step | NYX-SS-01 (Lightning Dash) |
| Shadowstep | Veilstep | NYX-SS-05 (Rewind Step) |
| Shadowstep | Static Wake | NYX-SS-08 (Static Wake) — novo |
| Shadowstep | Evasive Step / Double Step | NYX-SS-04 (Chain Step) |
| Storm Trap | Overcharged Trap | NYX-ST-03 (Overload Trap) |
| Storm Trap | Static Field Trap | NYX-ST-08 (Static Field Trap) — novo |
| Storm Trap | Tripwire | NYX-ST-01 (Tesla Network) |
| Storm Trap | Echo Trap | NYX-ST-06 (Echo Mine) |
| Storm Trap | Chain Snare | NYX-ST-09 (Chain Snare) — novo |
| Echo Clone | Mirrored Arsenal / Perfect Mimic | NYX-EC-04 (Flawless Mirror) |
| Echo Clone | Afterimage | NYX-EC-05 (Phase Swap) — Afterimage (nome de 02) é NYX-SS-02 (Phantom Lure; nome antigo em 03: Afterimage) |
| Thousand Reflections | Storm Reflections | NYX-TR-03 (Storm Prism) |
| Thousand Reflections | Endless Cuts | NYX-TR-07 (Endless Cuts) — novo |
| Thousand Reflections | Final Reflection | NYX-TR-06 (Execution Mirage) |
| Thousand Reflections | Infinite Mirror | NYX-TR-01 (Endless Reflections) |
| Void Tendrils | Commanding Tendrils / Construct Tendrils | ORU-VT-06 (Construct Conduit) |
| Void Tendrils | Crushing Grasp | ORU-VT-04 (Rending Pull) |
| Void Tendrils | Blight Tendrils | ORU-VT-03 (Ruin Tendrils) |
| Void Tendrils | Binding Tendrils | ORU-VT-05 (Lingering Tendrils) |
| Void Tendrils | Withering Grasp | ORU-VT-08 (Withering Grasp) — novo |
| Void Tendrils | Gathering Tendrils | ORU-VT-01 (Grasping Abyss) |
| Guardian Fragment | Twin Fragments / Swarm Fragment | ORU-GF-01 (Shard Sentinels) |
| Guardian Fragment | Aegis Fragment / Escort Fragment | ORU-GF-02 (Aegis Link) |
| Guardian Fragment | Volatile Fragment | ORU-GF-05 (Volatile Construct) |
| Guardian Fragment | Sentinel Fragment | ORU-GF-03 (Void Bastion) |
| Guardian Fragment | Fortified Fragment | ORU-GF-06 (Enduring Fragment) |
| Devouring Rift | Hungering Rift | ORU-DR-02 (Hungry Rift) |
| Devouring Rift | Feeding Rift | ORU-DR-07 (Nurturing Abyss) |
| Devouring Rift | Rotting Rift | ORU-DR-10 (Rotting Rift) — novo |
| Devouring Rift | Mobile Rift | ORU-DR-03 (Wandering Rift) |
| Devouring Rift | Collapse Point | ORU-DR-04 (Early Collapse) |
| Corruption Wave | Warding Wave | ORU-CW-07 (Empowering Wave) |
| Corruption Wave | Lingering Rot | ORU-CW-09 (Lingering Rot) — novo |
| Corruption Wave | Echoing Wave | ORU-CW-01 (Returning Tide) |
| Corruption Wave | Twin Waves | ORU-CW-02 (Expanding Ring) |
| Corruption Wave | Spreading Blight | ORU-CW-08 (Spreading Blight) — novo |
| Corruption Wave | Endless Wave | ORU-CW-04 (Plague Wall) |
| The Unbound | Legion Unbound | ORU-UB-01 (Abyssal Titan) |
| The Unbound | Abyssal Armor | ORU-UB-04 (Endless Night) |
| The Unbound | Rotting Unbound | ORU-UB-08 (Rotting Unbound) — novo |

Universais de 02:

| Nome em 02 | → ID |
|---|---|
| Resonant | GEN-27 (Twin Affinity) — novo |
| Expansive | GEN-11 (Wide Reach) |
| Volatile | GEN-28 (Volatile) — novo |
| Fortifying | GEN-29 (Fortifying) — novo |
| Thrifty | GEN-25 (Thrifty Casting) |
| Lingering | GEN-26 (Long Fuse) |
| Rapid | GEN-30 (Rapid) — novo |
| Afterimage | GEN-15 (Evasive Momentum) |
| Chained | GEN-07 (Splitting) |
| Echoing | GEN-24 (Resonant Repeat) |

Quando o mapeamento fizer dois itens de 02 trazerem o mesmo ID (ex.: Ember Wake em K1 e K10), vale a regra de duplicata (§3.3); 02 pode trocar um deles por outro ID da mesma habilidade em §10.2.

### 10.2 Índice Habilidade × Categoria → IDs (para 04 escolher o modificador de cada par)

Cada par "Habilidade — Categoria" de uma peça de set escolhe **um** ID da célula correspondente, respeitando a regra de 04 (o modificador não duplica nem depende do Set Power). Todos os 100 pares distintos usados por 04 têm ao menos uma opção.

| Habilidade | Categoria: IDs |
|---|---|
| Cleaving Strike | Behavior: KAE-CS-01, KAE-CS-08 · Echo: KAE-CS-04 · Area: KAE-CS-03 · Conversion: KAE-CS-06 · Trigger: KAE-CS-02, KAE-CS-05 · Resource: KAE-CS-07 |
| Infernal Leap | Behavior: KAE-IL-01, KAE-IL-02, KAE-IL-03, KAE-IL-08 · Elemental: KAE-IL-04 · Echo: KAE-IL-06 · Area: KAE-IL-09 · Conversion: KAE-IL-07 · Trigger: KAE-IL-10 · Resource: KAE-IL-05 |
| Burning Chains | Behavior: KAE-BC-01, KAE-BC-02 · Elemental: KAE-BC-08 · Chain: KAE-BC-03 · Conversion: KAE-BC-05 · Trigger: KAE-BC-07 · Resource: KAE-BC-06 · Duration: KAE-BC-04 |
| Ashen Guard | Behavior: KAE-AG-03, KAE-AG-07 · Elemental: KAE-AG-01 · Area: KAE-AG-06 · Trigger: KAE-AG-04 · Resource: KAE-AG-02 · Duration: KAE-AG-05 |
| Worldbreaker | Behavior: KAE-WB-01, KAE-WB-04 · Elemental: KAE-WB-09 · Echo: KAE-WB-02 · Area: KAE-WB-08 · Conversion: KAE-WB-03 · Trigger: KAE-WB-07 · Resource: KAE-WB-05 · Duration: KAE-WB-06 |
| Frost Lance | Behavior: LYR-FL-01, LYR-FL-03, LYR-FL-04 · Elemental: LYR-FL-09 · Chain: LYR-FL-02 · Echo: LYR-FL-07 · Conversion: LYR-FL-05 · Trigger: LYR-FL-06 · Resource: LYR-FL-08 |
| Void Orb | Behavior: LYR-VO-01, LYR-VO-02, LYR-VO-03 · Elemental: LYR-VO-10 · Echo: LYR-VO-07 · Area: LYR-VO-05 · Conversion: LYR-VO-04 · Trigger: LYR-VO-06 · Resource: LYR-VO-08 · Duration: LYR-VO-09 |
| Shattered Reality | Behavior: LYR-SR-01, LYR-SR-02, LYR-SR-07 · Elemental: LYR-SR-09 · Echo: LYR-SR-03 · Area: LYR-SR-08 · Conversion: LYR-SR-05 · Trigger: LYR-SR-04 · Resource: LYR-SR-10 · Duration: LYR-SR-06 |
| Astral Shift | Behavior: LYR-AS-01, LYR-AS-03, LYR-AS-04 · Elemental: LYR-AS-02 · Echo: LYR-AS-06 · Trigger: LYR-AS-08 · Resource: LYR-AS-05 · Duration: LYR-AS-07 |
| Absolute Zero | Behavior: LYR-AZ-02, LYR-AZ-04 · Elemental: LYR-AZ-08 · Echo: LYR-AZ-06 · Area: LYR-AZ-07 · Conversion: LYR-AZ-03 · Trigger: LYR-AZ-05 · Resource: LYR-AZ-09 · Duration: LYR-AZ-01 |
| Phantom Shot | Behavior: NYX-PS-03, NYX-PS-04 · Chain: NYX-PS-01, NYX-PS-02 · Echo: NYX-PS-05 · Conversion: NYX-PS-06 · Trigger: NYX-PS-08 · Resource: NYX-PS-07 |
| Shadowstep | Behavior: NYX-SS-02, NYX-SS-03, NYX-SS-04 · Elemental: NYX-SS-01 · Echo: NYX-SS-05 · Area: NYX-SS-08 · Trigger: NYX-SS-07 · Resource: NYX-SS-06 |
| Storm Trap | Behavior: NYX-ST-01, NYX-ST-02 · Elemental: NYX-ST-04 · Chain: NYX-ST-09 · Echo: NYX-ST-06 · Area: NYX-ST-08 · Trigger: NYX-ST-03 · Resource: NYX-ST-07 · Duration: NYX-ST-05 |
| Echo Clone | Behavior: NYX-EC-01, NYX-EC-03, NYX-EC-04, NYX-EC-05, NYX-EC-07 · Elemental: NYX-EC-09 · Area: NYX-EC-08 · Trigger: NYX-EC-02 · Duration: NYX-EC-06 |
| Thousand Reflections | Behavior: NYX-TR-02, NYX-TR-04 · Elemental: NYX-TR-03 · Chain: NYX-TR-07 · Trigger: NYX-TR-06 · Resource: NYX-TR-05 · Duration: NYX-TR-01 |
| Void Tendrils | Behavior: ORU-VT-01, ORU-VT-02, ORU-VT-04 · Elemental: ORU-VT-08 · Conversion: ORU-VT-03 · Trigger: ORU-VT-06 · Resource: ORU-VT-07 · Duration: ORU-VT-05 |
| Guardian Fragment | Behavior: ORU-GF-01, ORU-GF-02, ORU-GF-03 · Elemental: ORU-GF-10 · Echo: ORU-GF-07 · Area: ORU-GF-09 · Conversion: ORU-GF-04 · Trigger: ORU-GF-05 · Resource: ORU-GF-08 · Duration: ORU-GF-06 |
| Devouring Rift | Behavior: ORU-DR-01, ORU-DR-03, ORU-DR-04 · Elemental: ORU-DR-10 · Echo: ORU-DR-05 · Area: ORU-DR-08 · Conversion: ORU-DR-06 · Trigger: ORU-DR-02 · Resource: ORU-DR-07 · Duration: ORU-DR-09 |
| Corruption Wave | Behavior: ORU-CW-01, ORU-CW-02, ORU-CW-04 · Elemental: ORU-CW-10 · Chain: ORU-CW-03 · Area: ORU-CW-08 · Conversion: ORU-CW-06 · Trigger: ORU-CW-05, ORU-CW-07 · Duration: ORU-CW-09 |
| The Unbound | Behavior: ORU-UB-01, ORU-UB-02 · Elemental: ORU-UB-08 · Echo: ORU-UB-06 · Area: ORU-UB-07 · Conversion: ORU-UB-05 · Trigger: ORU-UB-09 · Resource: ORU-UB-03 · Duration: ORU-UB-04 |

Nota: um par específico de 04 nunca é preenchido por um `GEN-xx` (genérico não substitui modificador de habilidade em peça de set).

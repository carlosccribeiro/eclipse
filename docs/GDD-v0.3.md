# PROJECT ECLIPSE
## Game Design Document — GDD v0.3

**Data:** 7 de novembro de 2026  
**Gênero:** Action Roguelite / ARPG  
**Plataforma:** PC — Windows  
**Engine:** Unity (Unity 6.3 LTS, URP)  
**Modo:** Single-player  
**Perspectiva:** Isométrica 3D  
**Direção artística:** Stylized Dark Fantasy / Cosmic Fantasy  
**Status:** Design detalhado — pronto para pré-produção

> Este é o documento mestre. Ele resume e amarra os 12 documentos de detalhamento em `docs/gdd/`. Em caso de divergência, vale o documento dono do conceito (tabela em `docs/gdd/00-convencoes.md` §2) e o registro de resoluções de integração (`00-convencoes.md` §5).

---

# 1. Visão geral

PROJECT ECLIPSE é um Action Roguelite com elementos de ARPG, ambientado em um universo de fantasia sombria e cósmica. O jogador escolhe entre diferentes heróis, explora regiões fragmentadas da realidade, enfrenta hordas de inimigos e chefes, coleta equipamentos e constrói combinações de habilidades e atributos cada vez mais poderosas.

O jogo combina três elementos centrais:

- **Combate:** responsivo, dinâmico e satisfatório, com importância para posicionamento, movimentação e execução das habilidades.
- **Buildcrafting:** profundo sistema de personalização por equipamentos, atributos, modificadores de habilidades e sinergias.
- **Progressão:** loot significativo, desbloqueios permanentes e variedade suficiente para incentivar novas incursões.

A experiência deve recompensar tanto a habilidade mecânica do jogador quanto sua capacidade de desenvolver estratégias e criar builds.

## 1.1 Referências

- Diablo IV — itemização, equipamentos e identidade das builds.
- Path of Exile — profundidade de combinações e sinergias.
- Hades — combate responsivo e progressão roguelite.
- Ravenswatch — combate isométrico e personagens distintos.
- Risk of Rain 2 — combinações de itens e escalada de poder.
- No Rest for the Wicked — direção artística e atmosfera.

As referências servem como inspiração de design, não como objetivo de reproduzir sistemas existentes.

## 1.2 Pilares de design

1. **Combate satisfatório:** cada ataque, habilidade e impacto deve transmitir peso e clareza.
2. **Builds expressivas:** o jogador deve conseguir transformar profundamente o funcionamento de seu personagem.
3. **Loot significativo:** encontrar um item deve gerar decisões interessantes.
4. **Replayability:** diferentes heróis, equipamentos, regiões, eventos e combinações devem oferecer experiências variadas.
5. **Universo original:** identidade visual, narrativa, personagens e criaturas próprias.
6. **Profundidade sem rigidez:** nenhuma configuração deve ser obrigatória para que uma build seja viável.

## 1.3 Orçamento de poder (novo na v0.3)

Para que nenhum sistema domine, o poder ofensivo de um personagem bem equipado no fim da campanha (Eclipse, iLvl ~80, itens +6) se distribui aproximadamente assim:

| Fonte | Participação alvo no dano |
|---|---|
| Arma + nível + primários | ~30% |
| Atributos de equipamento (Increased) | ~25% |
| Modificadores de habilidade | ~15% (majoritariamente comportamento) |
| Set Powers ou Legendaries individuais | ~20% |
| Resonance | ~10% |

Regra de ouro: 6 peças de um set ≈ 3+3 híbrido ≈ Legendaries individuais, com variação máxima de ±10% em cenário médio. Blessings da run (+20–30%) ficam fora desse orçamento permanente.

Detalhamento: `docs/gdd/00-convencoes.md` §4.

---

# 2. Universo e narrativa

## 2.1 Lore

A realidade era sustentada pelos **Pilares da Existência**, estruturas primordiais responsáveis por manter as leis fundamentais do universo.

Um evento conhecido como **The Sundering** destruiu os pilares e fragmentou a realidade. Mundos, dimensões e linhas temporais foram misturados, criando regiões instáveis e criaturas que não deveriam existir.

A energia primordial liberada pela destruição dos pilares passou a ser conhecida como **Aether**.

Alguns indivíduos sobreviveram ao contato com essa energia e desenvolveram capacidades extraordinárias. Eles são conhecidos como **The Eclipsed**.

O jogador controla esses indivíduos em sua tentativa de compreender o Sundering, sobreviver às regiões corrompidas e descobrir o que realmente aconteceu.

## 2.2 Afinidades primordiais

O Aether manifesta-se por meio de seis afinidades:

| Afinidade | Tema | Identidade de gameplay | Status primário (automático) |
|---|---|---|---|
| Ember | Fogo | Dano contínuo, explosões e propagação | Burning |
| Frost | Gelo | Controle, lentidão e congelamento | Chill |
| Storm | Eletricidade | Velocidade, encadeamento e sobrecarga | Shock |
| Void | Vácuo e distorção | Agrupamento, implosão e manipulação espacial | Corruption |
| Echo | Repetição e duplicação | Cópias, repetição e efeitos atrasados | Echo Mark |
| Ruin | Decadência e corrupção | Enfraquecimento, deterioração e detonação | Decay |

As afinidades são elementos centrais da identidade das habilidades, dos equipamentos e das sinergias. Resistências do jogador se agrupam em duas famílias: **Elemental** (Ember, Frost, Storm) e **Void** (Void, Echo, Ruin).

Detalhamento: `docs/gdd/07-resonance.md`, `docs/gdd/01-atributos.md`.

## 2.3 Facções

- **The Remnants:** sobreviventes que tentam reconstruir a realidade.
- **The Hollow:** criaturas e entidades consumidas pelo vazio.
- **The Ascendants:** indivíduos que buscam controlar o Aether.
- **The Forgotten:** seres apagados da existência que retornaram após o Sundering.

Detalhamento: bestiário por facção e região em `docs/gdd/09-inimigos.md` §5.

---

# 3. Estrutura de gameplay

## 3.1 Loop principal

O loop de gameplay é estruturado em torno de incursões:

1. O jogador chega ao hub.
2. Escolhe um herói.
3. Ajusta equipamentos e habilidades.
4. Seleciona uma região e inicia uma incursão.
5. Enfrenta salas de combate, elites, eventos e desafios.
6. Coleta equipamentos, recursos e recompensas.
7. Escolhe os próximos caminhos.
8. Enfrenta encontros progressivamente mais difíceis.
9. Enfrenta o chefe da região ou decide continuar a exploração, conforme o modo.
10. Retorna ao hub com as recompensas obtidas.

A morte encerra a incursão. Equipamentos permanentes, recursos e desbloqueios conquistados permanecem na progressão da conta.

**Saídas da run (v0.3):**

- **Vitória:** derrotar o chefe mantém tudo. Após o chefe, o jogador pode escolher **Delve Deeper (Overrift)**: até 5 salas extras de risco crescente; o loot do chefe fica garantido e só o loot do Overrift fica em risco.
- **Extração:** o **Rift Anchor**, disponível nas Rest Rooms, encerra a run mantendo tudo o que foi coletado, mas sem a recompensa do chefe.
- **Morte:** regra canônica abaixo.

| Elemento | Vitória | Extração | Morte |
|---|---|---|---|
| Itens equipados e Legendaries | Mantidos | Mantidos | **Mantidos** |
| Epic/Rare/Common no Satchel | Mantidos | Mantidos | Convertidos em materiais a 50% (Normal: 75%) |
| Itens protegidos com Anchor | Mantidos | Mantidos | Mantidos |
| Gold, Aether Dust, Sundered Cores | 100% | 100% | 50% (Normal: 75%) |
| Pillar Fragments | 100% | 100% | 100% |
| Rift Shards (moeda da run) | Convertidos em Gold | Convertidos em Gold | Perdidos |
| Blessings | Encerradas | Encerradas | Encerradas |
| XP de herói, Account Rank, Codex | 100% | 100% | 100% |

Não existe revive. A única exceção é a Blessing *Second Wind* (1× por run). O evento **Echo of a Fallen Eclipsed** permite recuperar parte do que se perdeu na última morte naquela região: 50% dos Rift Shards e 1 item.

O inventário da run (**Satchel**) tem 30 espaços. Equipamentos são coletados automaticamente ao limpar a sala, com filtro aplicado.

Detalhamento: `docs/gdd/10-progressao.md` §3–4, `docs/gdd/05-loot.md` §12.

## 3.2 Tipos de salas

| Sala | Função |
|---|---|
| Combat Arena | Combate contra grupos de inimigos |
| Elite Arena | Encontro com inimigos de maior dificuldade |
| Treasure Room | Recompensas e equipamentos (2 itens + Reliquary: escolher 1 de 3) |
| Shrine Room | Bônus, bênçãos e efeitos especiais (Blessings) |
| Event Room | Eventos com escolhas e consequências |
| Challenge Room | Desafios opcionais de maior risco (nunca obrigatória) |
| Rest Room | Recuperação e preparação |
| Boss Room | Encontro contra um chefe |
| Guardian Arena (novo) | Mini-chefe nomeado (Guardian) fixo na linha 9 |

A estrutura deve permitir escolhas de caminho, criando decisões entre risco, recompensa e progressão. A recompensa de cada sala é visível na porta.

**Estrutura da run (v0.3):** mapa de nós com **13 linhas + chefe (14 salas)**, ~25–35 min, dividido em 3 Estratos (linhas 1–5 / 6–9 / 10–13). O Guardian fica fixo na linha 9. Há pelo menos uma Rest Room garantida na linha 5 e a **Antecâmara** (Rest) na linha 13, antes do chefe. Encontros são montados por **Pontos de Ameaça**: cada inimigo tem um custo e cada sala um orçamento.

**Serviços da Rest Room:** cura de 40% grátis, recarga do Aether Flask, Rift Anchor (extração), Anchor (proteger 1 item), Temper, Purificar e subida paga de rank de Blessing (com Rift Shards).

**Poder temporário da run:** Blessings dos Shrines, nos ranks Lesser/Greater/Exalted (70/25/5%), com no máximo +8% More por Blessing e +20% More no total. A meta é +20–30% de poder até o chefe. Há também Curses opcionais de risco × recompensa.

Detalhamento: `docs/gdd/09-inimigos.md` §9–14, `docs/gdd/10-progressao.md` §3.

## 3.3 Hub — The Sanctuary

O Sanctuary é a base central do jogador.

NPCs e estruturas planejados:

- **Keeper:** gerenciamento e seleção de heróis; respec gratuito da árvore de talentos.
- **Blacksmith:** upgrade de +0 a +10 (+3% por nível, nunca falha, transferível entre itens do mesmo slot), Reforge e Rekindle.
- **Archivist:** lore, registros e descobertas (Codex); o Codex habilita o Attunement e o transmog.
- **Rift Navigator:** seleção de regiões, dificuldades e Rift Sigils.
- **Enchanter:** reroll de **um** atributo por item (lock) com custo crescente; Imbue (rerola só o valor); reroll do Afixo Aberto; conversão de moedas com perda.
- **Merchant:** compra e venda, Aether Caches, oferta semanal, Heart Exchange (8 Hearts por 1 Unique daquele chefe).
- **Training Grounds:** testes de dano, habilidades e builds.
- **Vault:** armazenamento de 60 a 480 espaços, mais um Set Locker gratuito com 72 vagas.
- **Challenge Altar:** desafios e modos especiais (diário, semanal, Build Trials, Eclipse Trial).

O **Account Rank (1–100)** e os níveis de NPC desbloqueiam sistemas, espaços e escolhas, nunca atributos.

Detalhamento: `docs/gdd/10-progressao.md` §5–6.

---

# 4. Heróis

O jogador poderá escolher entre diferentes heróis. Cada personagem possui identidade própria, recursos, habilidades, armas e estilos de combate.

O objetivo inicial de conteúdo é oferecer quatro heróis, cada um com pelo menos três arquétipos de build viáveis.

**Regras gerais (v0.3):**

- **Desbloqueio:** Kael e Lyra disponíveis desde o início; Nyx ao derrotar o Ashen Colossus pela primeira vez; Orun ao derrotar o Pale Sovereign pela primeira vez.
- **Kit fixo de 5 habilidades**, sempre equipadas, liberadas gradualmente até o nível 8. A profundidade vem de Augments, modificadores, sets e Resonance.
- **Aether Lattice:** árvore de talentos com 55 pontos (49 por nível + 6 de marcos), contra ~131 de custo total; máximo de 2 Keystones (níveis 20 e 35). Cada habilidade tem 3 **Augments**, um por build principal, com 1 ativo por habilidade.
- **Nível 1–50** permanente por herói; `XP = 200·L^1,8`; nível 50 em ~40–50 h; catch-up de +100% de XP para heróis 10+ níveis atrás.
- **Primários:** cada herói tem um primário Core (Kael STR, Lyra INT, Nyx DEX, Orun INT), um Secundário e um Terciário. Todos têm efeitos universais.
- **Armas** ocupam apenas a Main Hand (armas duplas contam como um item). O **Off Hand** é um item próprio de cada herói, não-arma, sempre ativo: Kael **Warbrand**, Lyra **Grimoire**, Nyx **Quiver**, Orun **Idol**. Nenhum espaço é bloqueado por nível.

## 4.1 Kael — The Ashbound

**Arquétipo:** Warrior / Berserker  
**Afinidades:** Ember / Ruin  
**Recurso:** Fury (0–100, gera ao bater e ao receber dano, decai fora de combate)  
**Off Hand:** Warbrand  
**Disponível:** desde o início

Kael é um combatente de curta distância especializado em dano físico, agressividade e ataques de grande impacto.

### Habilidades

- **Cleaving Strike:** golpe amplo que atinge inimigos à frente.
- **Infernal Leap:** salto ofensivo com impacto em área.
- **Burning Chains:** correntes flamejantes que atingem e prendem inimigos.
- **Ashen Guard:** postura defensiva com potencial de contra-ataque (Parry de 0,2 s).
- **Worldbreaker:** habilidade suprema de grande impacto em área.

### Armas

- Greatsword
- Dual Axes
- Warhammer
- Longsword
- Heavy Blade

### Builds principais

1. **Inferno Berserker:** dano Ember, Fury e explosões.
2. **Ruin Executioner:** execução de inimigos e dano Ruin.
3. **Unbreakable Juggernaut:** resistência, sobrevivência e contra-ataques.

## 4.2 Lyra — The Veilweaver

**Arquétipo:** Mage  
**Afinidades:** Frost / Void  
**Recurso:** Arcane Charges (base 3, teto 6; até 8 com fontes declaradas)  
**Off Hand:** Grimoire  
**Disponível:** desde o início

Lyra controla o campo de batalha por meio de magia elemental, manipulação espacial e dano em área.

### Habilidades

- **Frost Lance:** projétil de gelo.
- **Void Orb:** esfera de energia Void.
- **Shattered Reality:** distorção espacial que causa dano em uma região.
- **Astral Shift:** deslocamento mágico.
- **Absolute Zero:** habilidade suprema baseada em congelamento (pode congelar não-chefes por até 3 s).

### Armas

- Arcane Staff
- Spellbook
- Twin Wands
- Crystal Orb
- Void Focus

### Builds principais

1. **Permafrost:** congelamento e controle.
2. **Void Singularity:** agrupamento e implosões.
3. **Arcane Overload:** geração e consumo de Arcane Charges.

## 4.3 Nyx — The Echo Hunter

**Arquétipo:** Assassin / Ranger  
**Afinidades:** Storm / Echo  
**Recurso:** Echo Energy (0–100, regenera 10/s, gasto em mobilidade e ecos)  
**Off Hand:** Quiver  
**Desbloqueio:** derrotar The Ashen Colossus

Nyx é uma personagem ágil, especializada em mobilidade, ataques rápidos, duplicação e dano concentrado.

### Habilidades

- **Phantom Shot:** disparo energético.
- **Shadowstep:** deslocamento rápido.
- **Storm Trap:** armadilha elétrica.
- **Echo Clone:** cria uma duplicata que replica ações.
- **Thousand Reflections:** habilidade suprema baseada em múltiplas cópias e ataques.

### Armas

- Dual Daggers
- Longbow
- Crossbow
- Twin Blades
- Chakrams

### Builds principais

1. **Storm Reaper:** velocidade e dano elétrico.
2. **Phantom Assassin:** mobilidade e execução.
3. **Echo Architect:** duplicação e repetição de habilidades.

## 4.4 Orun — The Voidkeeper

**Arquétipo:** Warden / Summoner  
**Afinidades:** Void / Ruin  
**Recurso:** Void Constructs, gerados pela barra **Void Mass** (base 3 construções, teto 6; até 8 com fontes declaradas; as construções se desfazem entre salas)  
**Off Hand:** Idol  
**Desbloqueio:** derrotar The Pale Sovereign

Orun utiliza construções de energia, invocações e efeitos de corrupção para controlar o campo de batalha.

### Habilidades

- **Void Tendrils:** tentáculos que atingem e controlam inimigos.
- **Guardian Fragment:** invoca uma construção defensiva.
- **Devouring Rift:** abre uma fenda que consome inimigos.
- **Corruption Wave:** onda de energia Ruin.
- **The Unbound:** habilidade suprema que libera o poder acumulado.

### Armas

- Ritual Staff
- Void Hammer
- Great Mace
- Totem
- Ancient Scepter

### Builds principais

1. **Void Commander:** invocações e construções.
2. **Corruption Master:** dano Ruin e deterioração.
3. **Abyssal Guardian:** defesa, sustentação e controle.

Detalhamento: `docs/gdd/08-herois.md` (kits com coeficientes, custos, cooldowns e tags; Augments; Aether Lattice; movesets das 20 armas; builds com rotas de itemização).

---

# 5. Sistema de combate

O combate deve combinar movimentação, posicionamento, ataques básicos, ataques fortes e habilidades.

**Fundamentos (v0.3):**

- **Simulação** em tick fixo de 60 Hz; toda a frame data é expressa em frames a 60 fps.
- **Controle** twin-stick: WASD para mover, mira no cursor, sem click-to-move.
- **Input:** LMB básico, RMB forte, **Space** esquiva, **Q/E/R/Shift** as 4 habilidades (Shift = mobilidade/utilidade), **F** Ultimate, **1** Aether Flask. Input buffer de 150 ms. O mapeamento de gamepad é definido em 06.
- **Esquiva universal**, idêntica para todos os heróis: 2 cargas, 4,5 m, i-frames nos frames 2–13, recarga de 1,5 s por carga. O **Perfect Dodge** concede recurso, sem câmera lenta.
- **Aether Flask:** cura ativa com 3 cargas por run, recarregada na Rest Room.
- **Proteções do jogador:** 0,25 s sem receber novos hits após levar um (0,8 s se o hit tirar ≥ 20% da vida); nenhum ataque não-mecânico tira mais de 60% da vida.
- **Câmera** em perspectiva com FOV 30°, inclinação de 50°, rotação fixa de 45° e 22 m de distância. Hit-stop é local, exceto em Ultimates, no Break de chefe e na troca de fase.
- **Telegraphs** com linguagem única: vermelho = evitável padrão; roxo = controle; branco-dourado = Unblockable; ciano = janela interrompível; laranja fica reservado a Ember. Wind-up mínimo de 0,45 s. **Annihilation** exige sair da área.
- **Tokens de ataque** limitam quantos inimigos atacam ao mesmo tempo (melee 3→5, ranged 2→4 conforme a dificuldade).
- **Durações alvo:** sala 60–90 s; Guardian 30–60 s; chefe 3–4 min (Normal) a 5 min (Eclipse).

Detalhamento: `docs/gdd/06-combate.md`.

## 5.1 Armas

Cada arma deve possuir características próprias:

- Velocidade de ataque.
- Alcance.
- Dano base.
- Animações.
- Área de impacto.
- Comportamento dos ataques.
- Potencial de sinergia com habilidades.

Armas não devem ser apenas modificadores numéricos. A escolha da arma precisa alterar a experiência de combate.

**v0.3:** o **DPS de referência é normalizado** entre armas: 10 no iLvl 1 e ~100 no iLvl 100, com ±5% entre armas do mesmo herói. Armas de área ampla têm fator 0,9 contra alvo único e especialistas em alvo único têm 1,05. As **habilidades escalam por Weapon Power** (o DPS normalizado), de modo que a arma muda o moveset e a sensação, não o dano das habilidades. 08 define o moveset (número de golpes, alcance, comportamento) e 06 define a frame data correspondente.

Detalhamento: `docs/gdd/02-equipamentos.md` §3 (bases), `docs/gdd/08-herois.md` (movesets), `docs/gdd/06-combate.md` §3.6 (frame data).

## 5.2 Habilidades

Cada herói possui um conjunto de habilidades próprias.

As habilidades podem ser modificadas por equipamentos, efeitos especiais e poderes de conjuntos.

Os modificadores devem ter capacidade de alterar o comportamento das habilidades, e não apenas aumentar seu dano.

**v0.3:** as habilidades são descritas por tags de um vocabulário único (`Attack, Spell, Melee, Ranged, Projectile, Area, DoT, Summon, Construct, Movement, Defensive, Ultimate, Channel, Trap, Clone, Basic, Heavy` + afinidades + `Physical`). Na arquitetura, cada habilidade é compilada em um `SkillSpec` imutável a partir de definição + Augment + modificadores + Set Powers.

## 5.3 Recursos

Cada herói possui um recurso próprio:

- Kael: Fury.
- Lyra: Arcane Charges.
- Nyx: Echo Energy.
- Orun: Void Constructs (via Void Mass).

A interface de equipamentos pode apresentar atributos genéricos de capacidade e geração de recurso, mas a implementação deve respeitar o funcionamento específico de cada herói.

O atributo de capacidade de recurso não deve obrigar todos os personagens a utilizarem uma barra de mana tradicional.

**v0.3:** `Maximum Resource` vira +1 de máximo por 1% para Fury e Echo Energy (teto 150), +1 Charge a cada 25% para Lyra e +1 Construct a cada 30% para Orun. O excedente vira bônus de dano ou vida.

Detalhamento: `docs/gdd/01-atributos.md` §3 (tradução numérica), `docs/gdd/08-herois.md` (comportamento).

## 5.4 Pipeline de dano (novo na v0.3)

```
DanoFinal = Base (Weapon Power × coeficiente, ou dano da arma)
          → Conversão (dano convertido escala só com o tipo final)
          × (1 + Σ Increased%)        // atributos de equipamento, modificadores (local)
          × Π (1 + More%)             // Set Powers, Uniques, Keystones/Ultimates, Core Attunement, Blessings
          × Crit                      // 1 ou (1 + Critical Damage); DoT não crita por padrão
          × (1 − Mitigação do alvo)   // Armor/Resist após Armor Penetration
          × (1 + Vulnerability%)      // teto total de +60%
```

- DoTs fazem snapshot na aplicação e tickam a cada 0,5 s. Dano em área não tem falloff.
- **Execute:** limiar máximo de 20% em comuns, metade em Elites; Guardians e chefes imunes. Contra chefes vira +25% de Vulnerability abaixo de 20% de vida.
- **CC:** Stun e Freeze compartilham uma regra única de perda de efeito (janela de 8 s: 100/50/25%/imune). Chefes convertem CC em Stagger, com duas exceções nomeadas: **Frostbound** (Freeze) e **Distortion** (Pull). O Break de chefe (**Broken**) dura 4 s e dá +30% de Vulnerability.
- **Anti-recursão (modelo Generation):** a ação direta é Gen 0; cada proc gera Gen+1; nada dispara em Gen ≥ 2. Réplicas Echo nunca geram novas réplicas, exceto poderes que declaram *Echo Recursion 1×*.

Detalhamento: `docs/gdd/06-combate.md` §3.1–3.3, `docs/gdd/07-resonance.md` §7.

---

# 6. Sistema de equipamentos e itemização

A itemização é um dos sistemas centrais do PROJECT ECLIPSE.

O objetivo é criar um sistema inspirado na profundidade de ARPGs como Diablo e Path of Exile, mas com identidade própria.

O jogador deve encontrar itens que mudem significativamente sua build e criar combinações que não sejam limitadas às configurações inicialmente planejadas.

Detalhamento: `docs/gdd/02-equipamentos.md` (catálogo), `docs/gdd/01-atributos.md` (números), `docs/gdd/05-loot.md` (geração).

## 6.1 Slots de equipamentos

O personagem terá 12 slots:

| Slot | Quantidade |
|---|---:|
| Main Hand | 1 |
| Off Hand | 1 |
| Head | 1 |
| Chest | 1 |
| Hands | 1 |
| Legs | 1 |
| Feet | 1 |
| Rings | 2 |
| Amulet | 1 |
| Relics | 2 |
| **Total** | **12** |

Nem todo herói precisa utilizar todas as categorias de arma ou combinações de equipamento da mesma maneira. Restrições de classe e compatibilidade devem ser definidas por item.

**v0.3 — compatibilidade:**

- Armas e Off Hands são exclusivos de cada herói.
- Armaduras (3 famílias: Plate, Leather, Vestment), joias e Relics são universais.
- Relics têm 6 famílias, uma por afinidade, com implícito de Status Potency.
- **Hero-bound:** um item com modificador de habilidade específica, ou com efeito específico de herói, só pode ser equipado pelo herói dono.
- Todos os 12 slots ficam ativos desde o nível 1.

## 6.2 Raridades

O sistema terá quatro níveis principais de raridade.

| Raridade | Atributos | Modificadores de habilidade |
|---|---:|---:|
| Common | 2 | 0 |
| Rare | 3 | 0 |
| Epic | 4 | 1 |
| Legendary | 5 | 2 |

### Common

Equipamentos básicos, úteis para iniciar uma progressão e preencher slots.

Possuem dois atributos.

### Rare

Equipamentos com maior potencial de atributos e combinações.

Possuem três atributos.

### Epic

Equipamentos com quatro atributos e um modificador de habilidade.

Começam a permitir alterações relevantes na construção de uma build.

**v0.3:** o modificador do Epic vem 75% do pool do herói (o item fica hero-bound) e 25% do pool genérico `GEN-xx`.

### Legendary

Equipamentos de maior raridade e identidade.

Possuem cinco atributos e dois modificadores de habilidade.

Além disso, equipamentos Legendary podem pertencer a conjuntos e participar do sistema de Set Powers.

**Importante:** Unique não será uma raridade adicional nesta estrutura. Itens únicos poderão existir como uma categoria especial de Legendary, com identidade, nome, arte e efeitos exclusivos.

**v0.3 — estrutura por tipo de Legendary:**

| Tipo | Atributos | Modificadores | Efeito |
|---|---|---|---|
| Legendary nomeado | 4 fixos + 1 **Afixo Aberto** (sorteado entre 3 opções da definição) | 2 fixos | 1 efeito especial (≤ +15% More) |
| Peça de set | 4 fixos + 1 Afixo Aberto | 2 fixos | Nenhum próprio: a identidade é o set |
| Unique | 5 fixos | 2 (até 1 **Signature** exclusivo) | Efeito Unique (≤ +40% More) |

- Todo Legendary é nomeado: não existe Legendary com afixos totalmente aleatórios.
- Raridade não muda a faixa de um afixo: adiciona quantidade de atributos e modificadores, não números maiores.
- **Primal** é uma marca de qualidade, não uma raridade: Legendaries Primal (a partir de Ascension 50) têm todas as rolagens entre 85% e 100%.
- Uniques não caem em Normal (iLvl mínimo 35); peças de set também não caem em Normal.

## 6.3 Atributos dos equipamentos

Os atributos serão organizados por categorias para facilitar balanceamento, geração de loot e compreensão pelo jogador.

### Atributos primários

- Strength
- Dexterity
- Intelligence

Esses atributos devem ter impacto mecânico definido sobre os heróis e suas habilidades.

**v0.3:** o primário Core de cada herói concede **Core Attunement**: +0,08% More por ponto, a única fonte More vinda de atributos (~+30–35% no fim da campanha). Secundário e Terciário concedem bônus próprios do herói e efeitos universais.

### Atributos ofensivos

- Attack Speed
- Cast Speed
- Critical Chance
- Critical Damage
- Physical Damage
- Skill Damage
- Ember Damage
- Frost Damage
- Storm Damage
- Void Damage
- Echo Damage
- Ruin Damage
- Damage Over Time
- Area Damage
- Projectile Damage
- Armor Penetration
- Stagger Damage
- **Status Potency** (novo na v0.3: +% no buildup de status)

### Atributos defensivos

- Maximum Health
- Maximum Resource
- Armor
- Physical Resistance
- Elemental Resistance
- Void Resistance
- Dodge Chance
- Block Chance
- Damage Reduction
- Health Regeneration
- Healing Effectiveness
- Shield Strength
- Status Resistance

### Atributos utilitários

- Movement Speed
- Cooldown Reduction
- Resource Generation
- Resource Cost Reduction
- Pickup Radius
- Experience Gain
- Gold Acquisition
- Duration of Effects
- Summon Damage
- Summon Duration

A lista poderá crescer conforme os sistemas de combate e progressão forem detalhados.

Nem todos os atributos precisam aparecer em todos os equipamentos. A geração deve respeitar categorias, slots, heróis e identidade do item.

**v0.3 — regras numéricas principais:**

- **Armor:** `Armor / (Armor + 50 × eLvl + 500)`, teto de 75%. Resistências são percentuais diretos (teto de 75%), com penalidade por dificuldade de 0 / −5 / −15 / −25 e mais −0,15 por nível de Ascension.
- **Diminishing returns:** regra única, em que acima do soft cap o atributo rende 50% até o hard cap. Exemplos: Attack/Cast Speed 50/100, CDR 25/40 (a Ultimate recebe metade), Dodge 20/40, Damage Reduction 20/40, Movement Speed 25/50.
- **Dodge Chance e Block** só atuam em golpes não telegrafados (HitFlag `Unavoidable`). Block reduz 40%.
- **Health Regeneration** só opera em combate e na Rest Room.
- **Pool por slot:** máximo de 1 primário por item (Amulet 2; nenhum em Ring, Off Hand e Relic); máximo de 2 afixos de afinidade; Movement Speed só em Feet.

Detalhamento: `docs/gdd/01-atributos.md` (fórmulas, caps, tabelas), `docs/gdd/02-equipamentos.md` §3.9 (pool por slot).

## 6.4 Sistema híbrido de atributos e rolagens

O sistema de equipamentos utilizará um modelo híbrido.

**Cada item possui uma identidade fixa, mas seus atributos podem variar dentro de faixas definidas.**

Isso significa que dois jogadores podem encontrar exatamente a mesma espada Legendary, com o mesmo nome, arte e efeitos especiais, mas com valores de atributos diferentes.

Exemplo conceitual:

**Ashfang — Legendary Greatsword**

Identidade fixa:
- Nome e aparência.
- Tipo de arma.
- Efeito especial exclusivo.
- Modificadores de habilidade associados.

Atributos variáveis:
- Strength: 18–25.
- Attack Speed: 4%–8%.
- Ember Damage: 10%–16%.
- Critical Chance: 3%–6%.

Os valores acima são apenas ilustrativos. As faixas definitivas serão definidas durante o balanceamento.

### Objetivos do sistema

1. Criar motivos para continuar procurando versões melhores de itens conhecidos.
2. Evitar que a descoberta de um item encerre definitivamente a progressão daquele slot.
3. Criar decisões entre identidade do item e qualidade das rolagens.
4. Permitir que jogadores tenham versões diferentes de um mesmo equipamento.
5. Aumentar a longevidade do sistema de loot.

### Regras de identidade

- Nome do item: fixo.
- Arte e aparência: fixas.
- Tipo e categoria: fixos.
- Efeito especial principal: fixo.
- Atributos: valores variáveis dentro de faixas.
- Modificadores de habilidade: efeitos definidos pelo item, com valores variáveis quando aplicável.

Não haverá geração completamente aleatória de efeitos que descaracterizem o item.

**v0.3 — modelo de rolagem:**

- **Tiers de iLvl:** T1 = iLvl 1–20, T2 = 21–40, T3 = 41–60, T4 = 61–80, T5 = 81–100, com tabela fixa por tier.
- **Rolagem:** o valor é sorteado de forma uniforme dentro da faixa do tier e salvo normalizado (`Roll01`, de 0 a 1), então rebalancear uma faixa propaga para itens já existentes. Dificuldades altas elevam o piso da rolagem, não o teto.
- **Faixa Nomeada:** itens nomeados usam de 70% a 115% do máximo do tier.
- **Multiplicadores de slot:** Main Hand ×1,5 em dano%, Amulet ×1,5 em primários e resistências, Relic ×1,25 em atributos de build.
- **Qualidade da rolagem** visível por afixo (% do `Roll01`) e pela média do item, com ★ a partir de 95% e "Perfect" a partir de 99%.
- **Pós-drop:** upgrade +0..+10 (+3% por nível, igual para todas as raridades; modificadores não sobem). O Enchanter troca um atributo por item, e o Imbue rerola apenas o valor.

Detalhamento: `docs/gdd/01-atributos.md` §3.9, `docs/gdd/10-progressao.md` §6.

## 6.5 Modificadores de habilidades

Equipamentos Epic e Legendary podem modificar habilidades.

Os modificadores devem criar mudanças de comportamento, novas interações ou novas possibilidades de build.

Categorias de modificadores:

| Categoria | Comportamento |
|---|---|
| Behavior | Altera o funcionamento da habilidade |
| Elemental | Converte ou adiciona afinidades |
| Chain | Faz o efeito atingir outros alvos |
| Echo | Repete ataques ou efeitos |
| Area | Modifica alcance e área |
| Conversion | Converte dano ou recurso |
| Trigger | Ativa efeitos sob determinadas condições |
| Resource | Altera custos e geração de recurso |
| Duration | Modifica duração dos efeitos |

### Exemplo: Cleaving Strike — Kael

Possíveis modificadores:

- **Burning Arc:** o ataque deixa um arco de fogo no chão.
- **Volatile Impact:** o impacto causa uma explosão adicional.
- **Executioner's Reach:** aumenta o alcance e favorece ataques contra inimigos enfraquecidos.
- **Echo Slash:** repete o ataque após um pequeno intervalo.
- **Shattering Blow:** causa dano adicional contra inimigos congelados.
- **Ruin Cleaver:** converte parte do dano em Ruin.
- **Blood Fury:** o ataque gera Fury adicional sob determinadas condições.
- **Infernal Collapse:** concentra o impacto em uma explosão de maior área.

Os modificadores devem ser projetados para oferecer mudanças reais na jogabilidade.

### Rolagens dos modificadores

O efeito principal de cada modificador é fixo.

Quando fizer sentido, seus valores numéricos podem variar dentro de faixas.

Exemplo:

**Echo Slash**
- Efeito fixo: repete o ataque.
- Valor variável: percentual do dano replicado.

Dessa forma, a identidade do modificador é preservada, enquanto versões melhores continuam sendo desejáveis.

**v0.3 — regras do catálogo:**

- **Catálogo:** 176 modificadores específicos (6–8 por habilidade, todas as 20 cobertas) e 30 genéricos por tag (`GEN-xx`), mais modificadores Signature exclusivos de Uniques (`SIG-<item>`).
- **Limites:** até 3 ativos por habilidade (2 na Ultimate, 2 em Basic e 2 em Heavy) e no máximo 14 ativos por herói.
- **Duplicatas e exclusividade:** o mesmo modificador não acumula (vale a maior rolagem), e há grupos de exclusividade (Forma, Conversão, Modo de Uso).
- **Resolução** em 6 fases fixas: Conversão → Forma → Parâmetros → Replicação → Gatilhos → Recurso.
- **Valores:** modificadores nunca concedem More; o ganho numérico é Increased local ou instâncias novas de dano com Proc Coefficient reduzido (repetição Echo 0,5, cópia de clone 0,4, área persistente 0,25). A razão máx./mín. da rolagem é ≤ 1,5.
- **Raridade mínima:** E (Epic e Legendary) ou L (só Legendary).
- **Augments** da Aether Lattice são uma camada separada e não ocupam vaga de modificador.

Detalhamento: `docs/gdd/03-modificadores.md`.

## 6.6 Sistema de conjuntos — Set Powers

Equipamentos Legendary poderão pertencer a conjuntos.

Cada conjunto terá seis peças e oferecerá dois níveis de bônus:

- **3 peças equipadas:** desbloqueia um poder de conjunto.
- **6 peças equipadas:** desbloqueia um segundo poder de conjunto.

Os poderes podem desbloquear habilidades, modificar habilidades existentes ou alterar profundamente uma mecânica do personagem.

### Regras fundamentais

1. Cada conjunto possui seis equipamentos definidos.
2. As peças ocupam slots específicos.
3. Equipamentos de conjunto são exclusivamente Legendary.
4. O bônus de três peças é ativado ao equipar três peças distintas do mesmo conjunto.
5. O bônus de seis peças é ativado ao equipar todas as seis peças.
6. Os poderes de conjunto não consomem os espaços de modificadores individuais dos equipamentos.
7. O jogador pode combinar conjuntos diferentes.
8. Dois conjuntos podem ativar simultaneamente seus bônus de três peças.

**Regras adicionadas na v0.3:**

9. **No máximo 2 Set Powers ativos.** O bônus de 6 peças conta como o segundo poder, então 6+3 e 3+3+3 não somam poder extra. Se houver mais candidatos, a prioridade é: mais peças, depois o **Set Focus** escolhido pelo jogador, depois o primeiro set a fechar 3 peças.
10. **Sets nunca ocupam Main Hand nem Off Hand.** Usam 6 dos 10 slots não-arma, e as "pegadas" de slot variam entre os sets de cada herói.
11. **Peça distinta = definição de item distinta.** Duas cópias da mesma peça contam como 1.
12. **Sets são exclusivos do herói** (hero-bound). Os valores dos Set Powers são fixos e não escalam com iLvl.
13. Um único Unique de suporte a sets no jogo base: **Keystone of the Sundering** (Relic), que conta como +1 peça de cada set com 2 ou mais peças equipadas.

### Filosofia de design

O sistema não deve obrigar o jogador a utilizar seis peças de um único conjunto.

Devem existir três rotas viáveis de itemização:

**Rota A — Legendary individual**

O jogador utiliza equipamentos Legendary independentes, escolhidos por seus efeitos e atributos.

**Rota B — Conjunto completo**

O jogador equipa seis peças de um conjunto para desbloquear seu poder máximo.

**Rota C — Híbrida**

O jogador utiliza três peças de um conjunto e três peças de outro, ativando dois poderes diferentes.

O objetivo é evitar que conjuntos completos sejam sempre superiores às combinações individuais.

**v0.3 — orçamento:** cada 3pc entrega ≈ +10 a +12% de dano efetivo; cada 6pc ≈ +12 a +15% sobre o 3pc. As estimativas atuais colocam a Rota A em ×1,23–1,30, com as rotas ofensivas a no máximo ×1,074 umas das outras, dentro dos ±10%. Sets defensivos convertem orçamento em EHP (1% de dano ≈ 1,5% de EHP). A validação final é pelo simulador headless de 11.

Detalhamento: `docs/gdd/04-conjuntos.md`.

## 6.7 Conjuntos planejados

O objetivo inicial é criar 12 conjuntos, três para cada herói.

### Kael

**Ashen Sovereign**
- Tema: Fury e Ember.
- Foco: agressividade, transformação e explosões.

**Ruinbreaker**
- Tema: armas pesadas e Ruin.
- Foco: golpes de grande impacto, enfraquecimento e execução.

**Eternal Bastion**
- Tema: defesa e resistência.
- Foco: sobrevivência, contra-ataques e sustentação.

### Lyra

**Frozen Empress**
- Tema: Frost.
- Foco: congelamento e controle de campo.

**Void Oracle**
- Tema: Void.
- Foco: agrupamento, distorção e dano em área.

**Arcane Paradox**
- Tema: magia e Arcane Charges.
- Foco: conjuração, geração e consumo de recurso.

### Nyx

**Storm Phantom**
- Tema: Storm.
- Foco: velocidade e dano elétrico.

**Thousand Echoes**
- Tema: Echo.
- Foco: duplicação, repetição e clones.

**Death's Pursuit**
- Tema: execução e Critical.
- Foco: dano concentrado, mobilidade e finalizações.

### Orun

**Abyssal Legion**
- Tema: invocações.
- Foco: criaturas e construções Void.

**Hollow Sovereign**
- Tema: Ruin e decadência.
- Foco: corrupção, enfraquecimento e dano progressivo.

**Eternal Warden**
- Tema: defesa e sustentação.
- Foco: proteção, controle e sobrevivência.

Detalhamento: peças, slots, atributos, modificadores (IDs de 03) e os 24 Set Powers com valores em `docs/gdd/04-conjuntos.md` §6–9. Região-lar de cada set: §8.3 deste documento.

## 6.8 Exemplo detalhado de conjunto

### Ashen Sovereign — Kael

Seis peças:

1. Helmet
2. Chest
3. Gloves
4. Legs
5. Boots
6. Amulet

**Bônus de 3 peças — Ashen Ascension**

Ao atingir uma condição relacionada à Fury, Kael entra temporariamente em uma transformação que aumenta o alcance de seus ataques e adiciona efeitos Ember.

**Bônus de 6 peças — Sovereign's Wrath**

Durante Ashen Ascension, Cleaving Strike provoca uma sequência de explosões. Eliminações podem prolongar parcialmente a transformação.

Os efeitos são conceituais e deverão receber valores e condições definitivas durante o balanceamento.

**v0.3:** as condições e os valores definitivos desses dois poderes, como a ativação por gasto de Fury e a recarga, estão em `docs/gdd/04-conjuntos.md` §6. A meta de uptime de Ashen Ascension é 50–60%.

## 6.9 Quantidade inicial planejada

O planejamento inicial contempla:

- 4 heróis.
- 12 builds principais.
- 12 conjuntos Legendary.
- 6 peças por conjunto.
- 72 peças de conjunto.
- Equipamentos Legendary independentes.
- Equipamentos Common, Rare e Epic.
- Um catálogo inicial de aproximadamente 70 itens nomeados, a ser revisado e expandido para acomodar a nova estrutura de conjuntos.

O número de itens é uma meta de conteúdo, não uma limitação definitiva.

**Contagem real na v0.3:**

| Conteúdo | Quantidade |
|---|---:|
| Itens nomeados não-set | **84** (54 Legendary + 30 Unique) |
| Uniques por fonte | 15 de chefe (3 por chefe), 4 Pinnacle, 7 regionais, 4 de Challenge |
| Peças de set | 72 (12 sets × 6) |
| Bases de arma | 20 |
| Categorias de Off Hand | 4 (uma por herói) |
| Famílias de armadura | 3 |
| Famílias de Relic | 6 (uma por afinidade) |
| Modificadores específicos de habilidade | 176 |
| Modificadores genéricos (`GEN-xx`) | 30 |
| Augments | 60 (3 por habilidade × 20) |

Detalhamento: `docs/gdd/02-equipamentos.md` §3.12.

---

# 7. Sistema de Resonance

O sistema de Resonance permite combinar afinidades e produzir efeitos adicionais.

A proposta é fazer com que habilidades, equipamentos e efeitos elementais interajam entre si.

**v0.3 — regras centrais:**

- **Aplicação de status:** há três modos, **Buildup**, **Chance** e **Garantida**. O Buildup usa uma Status Potency fixa por golpe, independente do dano: Basic 15, Skill 30, Heavy 40, Ultimate 100.
- **Status primário automático:** cada afinidade tem um, aplicado por qualquer golpe dela. Os outros 18 efeitos exigem fonte explícita (habilidade, modificador, item ou Set Power).
- **Disponibilidade:** a Resonance está sempre ativa. O Codex do Archivist só registra e explica, e suas recompensas são lore e cosmético.
- **Disparo:** uma Resonance por evento por alvo, escolhida por tabela de prioridade fixa. Há ICD por alvo e um teto de 8 procs de Resonance por segundo por jogador.
- **Vulnerability:** teto total de +60% no alvo. Ruin e Shock acumulam stacks; as outras fontes dão valor fixo por fonte.
- **Chefes:** nunca ficam imunes a DoT. Recebem versões reduzidas de controle: **Frostbound** no lugar de Freeze, **Distortion** no lugar de Pull e Stagger no lugar de Stun.
- **Resistência a status:** Elite ×1,5, Guardian ×2,0, chefe ×2,5.
- **Inimigos** usam os mesmos status contra o jogador. O Freeze no jogador dura no máximo 1,0 s.
- **Acesso:** todas as combinações são acessíveis por qualquer herói via equipamento (Infusions em Relics e Amulets).

Detalhamento: `docs/gdd/07-resonance.md`.

## 7.1 Efeitos por afinidade

### Ember

- Burning
- Explosion
- Ignition
- Combustion

### Frost

- Slow
- Freeze
- Shatter
- Frozen Armor

### Storm

- Shock
- Chain Lightning
- Overload
- Static Field

### Void

- Pull
- Implosion
- Corruption
- Absorption

### Echo

- Repeat
- Clone
- Reflection
- Delayed Effect

### Ruin

- Decay
- Vulnerability
- Weakening
- Detonation

**v0.3:** os 24 status estão definidos com duração, stacks, efeito numérico, modo de aplicação, cor, forma, ícone e som. São no máximo 4 ícones sobre um inimigo, e os telegraphs inimigos nunca são reduzidos pelas opções de VFX. Detalhamento: `docs/gdd/07-resonance.md` §4 e §10.

## 7.2 Sinergias entre afinidades

Exemplos de combinações:

| Afinidades | Resultado conceitual |
|---|---|
| Ember + Frost | Thermal Shatter |
| Ember + Storm | Combustion Discharge |
| Frost + Void | Agrupamento e congelamento |
| Storm + Echo | Repetição de descargas elétricas |
| Ruin + Ember | Detonação de efeitos de decadência |
| Void + Echo | Replicação de zonas de distorção |

As sinergias devem incentivar experimentação e criar possibilidades de builds híbridas.

Não devem ser limitadas a combinações exclusivas de um único herói.

**v0.3:** a matriz completa das **15 Resonances** (todos os pares de afinidades) tem nome, gatilho, efeito, valor, ICD e limites. A meta é que a Resonance responda por ~10% do dano total, com alarme acima de 20%. Detalhamento: `docs/gdd/07-resonance.md` §5.

---

# 8. Regiões e exploração

O universo será dividido em regiões com identidade visual, inimigos, eventos e chefes próprios.

## 8.1 Regiões planejadas

1. **The Ashen Wastes:** região devastada por energia Ember.
2. **The Frozen Cathedral:** estruturas congeladas e entidades Frost.
3. **The Shattered Expanse:** realidade fragmentada e instável.
4. **The Hollow Depths:** região dominada por Void e Ruin.
5. **The Astral Citadel:** estruturas cósmicas ligadas aos Pilares da Existência.

Cada região deve possuir variações de encontros, salas, eventos, inimigos e recompensas.

**v0.3:** cada região tem 8 inimigos-base (40 no total), 2 Guardians nomeados e 1 chefe. A partir da região 3, inimigos de regiões anteriores retornam como "ecos" (até 20% do orçamento). Em Normal as regiões são sequenciais; em Veteran+ as regiões 1–4 ficam livres, e The Astral Citadel exige os outros 4 chefes naquela dificuldade.

## 8.2 Chefes principais

| Região | Chefe |
|---|---|
| The Ashen Wastes | The Ashen Colossus |
| The Frozen Cathedral | The Pale Sovereign |
| The Shattered Expanse | The Fractured One |
| The Hollow Depths | The Devouring Presence |
| The Astral Citadel | The First Forgotten |

Os chefes devem possuir padrões de ataque próprios, fases, mecânicas e recompensas específicas.

**v0.3:**

- **Lutas:** cada chefe tem fases, mecânicas centrais e variações ✦ (Nightmare) e ✧ (Eclipse, com fase extra).
- **Enrage:** não há hard enrage; o soft enrage começa em 2× a duração alvo.
- **Recompensas:**
  - 3 Uniques exclusivos por chefe;
  - **Heart of [Chefe]**, material próprio de cada chefe que serve de chave dos Boss Echoes e permite o Heart Exchange;
  - Pillar Fragments;
  - pity de Unique garantido no 10º abate.
- **Pinnacle opcional do endgame:** The Sundering Heart, com duração alvo de 6 min.

## 8.3 Região-lar dos conjuntos (novo na v0.3)

Cada set tem uma região-lar (peso ×4 entre os sets do herói; o chefe da região tem peso ×6). Cada herói tem seus 3 sets em 3 regiões diferentes:

| Região | Sets-lar |
|---|---|
| The Ashen Wastes | Ashen Sovereign (Kael), Death's Pursuit (Nyx) |
| The Frozen Cathedral | Frozen Empress (Lyra), Eternal Bastion (Kael) |
| The Shattered Expanse | Storm Phantom (Nyx), Abyssal Legion (Orun), Ruinbreaker (Kael) |
| The Hollow Depths | Void Oracle (Lyra), Hollow Sovereign (Orun) |
| The Astral Citadel | Arcane Paradox (Lyra), Thousand Echoes (Nyx), Eternal Warden (Orun) |

Sets dropam em qualquer região; a região-lar apenas concentra. No Eclipse+, o **Attunement** (Rift Sigil + 2 Pillar Fragments) multiplica por ×3 o peso de um set ou Unique escolhido.

Detalhamento: `docs/gdd/09-inimigos.md` §5–8 (bestiário e chefes), `docs/gdd/05-loot.md` §9 (target farming).

---

# 9. Inimigos e encontros

O sistema de inimigos deve incluir arquétipos com comportamentos distintos.

Categorias planejadas:

- Melee attackers.
- Ranged attackers.
- Casters.
- Tanks.
- Summoners.
- Assassins.
- Support enemies.
- Elite enemies.
- Bosses.

Inimigos Elite poderão receber modificadores que alteram suas características e criam encontros diferentes.

Os modificadores devem ser legíveis visualmente e oferecer desafios compreensíveis, sem depender exclusivamente de aumento de vida e dano.

Eventos especiais poderão combinar inimigos, objetivos e condições de combate.

**v0.3:**

- **Stats** são relativos a uma unidade "Standard" (HP 1,0 / Dano 1,0). Os valores absolutos vêm do **Monster Level (eLvl)**, que acompanha o iLvl da sala por região × dificuldade.
- **Afixos de Elite:** 18, cada um com assinatura visual e counterplay explícito. A quantidade cresce com a dificuldade: 1 no Normal, até 4 em Ascension.
- **Guardians:** 10 mini-chefes nomeados, 2 por região.
- **Conteúdo de eventos e run:** 14 Event Rooms, 6 variantes de Challenge, 6 Room Modifiers, 30 Blessings em 3 ranks e 6 Curses.
- **Endgame:** Nightmare Behaviors, Champion Packs, Eclipse Phenomena, Boss Echoes, Eclipse Trial, The Sundering Heart e Overrift Herald.
- **IA:** HFSM com seleção de ataque por Utility. Chefes usam HFSM de fases com sequenciador de padrões. As decisões rodam a 10 Hz (inimigos perto) e 4 Hz (longe).

Detalhamento: `docs/gdd/09-inimigos.md`.

---

# 10. Progressão e economia

A progressão terá dois componentes:

**Progressão durante a incursão**
- Loot.
- Recursos temporários (Rift Shards).
- Escolhas de caminhos.
- Bônus e efeitos de eventos (Blessings, Curses).
- Crescimento de poder durante a run.

**Progressão permanente**
- Equipamentos mantidos.
- Desbloqueios de conteúdo.
- Recursos persistentes.
- Evolução da conta (Account Rank 1–100, níveis de NPC, Codex).
- Acesso a desafios e modos adicionais.

A morte encerra a incursão, mas não deve apagar equipamentos permanentes e desbloqueios conquistados (regra canônica em §3.1).

**Princípio da v0.3:** a meta-progressão amplia opções, não obriga números. O poder permanente vem de equipamento, de nível de herói (curto e finito) e de upgrade de item.

## 10.1 Economia

Recursos serão utilizados em serviços do Sanctuary, incluindo:

- Aprimoramento.
- Encantamento.
- Compra e venda.
- Serviços de equipamentos.
- Desbloqueios e progressão.

A economia deve oferecer escolhas relevantes e evitar que um único recurso se torne inútil em estágios avançados.

**Moedas (v0.3):**

| Moeda | Tipo | Uso principal |
|---|---|---|
| Gold | Permanente | Taxas, Merchant, Vault, níveis de NPC |
| Aether Dust | Permanente (salvage) | Upgrade, Enchant de Common/Rare, conversão |
| Sundered Cores | Permanente | Upgrade +6 em diante, Enchant de Epic/Legendary, Reforge, Rekindle |
| Pillar Fragments | Permanente, rara | Upgrade +9/+10, Rekindle, Attunement |
| Eclipse Marks | Permanente, endgame | Set Caches, Sigils especiais, chaves |
| Memory Fragments | Colecionável | Capítulos de lore do Codex |
| Rift Shards | **Temporária, única moeda da run** | Serviços da Rest Room, Shrines, eventos |
| Heart of [Chefe] (5) | Material de chefe | Chave de Boss Echo, Heart Exchange |

A conversão com perda entre moedas (Dust ↔ Cores → Pillar Fragments, com limite semanal) impede moeda morta.

**Loot:**

- **Sem Magic Find:** a raridade depende só de fonte × dificuldade × pity, com pisos por fonte.
- **Densidade:** ~30 itens por run no Normal, ~37 no Eclipse, ~45 em A100.
- **Smart loot:** 75% dos itens não-Legendary e 85% dos Legendary/Set/Unique servem ao herói em uso.
- **Pity em três camadas:** Legendary, Set e anti-duplicata.
- **Bônus de quantidade:** aditivos, com teto ×2,0.

Detalhamento: `docs/gdd/10-progressao.md` §5–6, `docs/gdd/05-loot.md`.

## 10.2 Dificuldade e endgame

O jogo deve oferecer progressão de dificuldade e atividades para jogadores avançados.

Sistemas planejados:

- Dificuldades progressivas.
- Desafios especiais.
- Encontros de maior dificuldade.
- Conteúdo de endgame.
- Modificadores de incursão.
- Recompensas de alto nível.

O endgame deve ampliar a variedade de builds e oferecer objetivos de longo prazo, não apenas aumentar os atributos dos inimigos.

**Dificuldades (v0.3)** — desbloqueadas por conta, não por herói:

| Tier | Requisito | iLvl de drop | Mudanças de regra |
|---|---|---|---|
| Normal | Início | 1–24 (por região) | Elites com 1 afixo; morte retém 75%; sem Uniques nem peças de set |
| Veteran | First Forgotten em Normal | 25–49 | Elites com 2 afixos; peças de set passam a cair |
| Nightmare | First Forgotten em Veteran | 50–69 | Nightmare Behaviors; matilhas Elite adicionais |
| Eclipse | First Forgotten em Nightmare + Eclipse Trial | 75–84 (todas as regiões) | Elites com 3 afixos; Eclipse Phenomena; chefes com fase extra; Rift Sigils |
| Ascension 1–100 | 5 chefes em Eclipse | 85–100 (100 em A90) | Escalada contínua + Thresholds com regras novas |

**Endgame (5 pilares):**

1. Ascension + Rift Sigils, com Distortions e Peril.
2. Boss Echoes.
3. Endless Sundering.
4. Challenge Altar: diário, semanal, Build Trials e Eclipse Trial.
5. O pinnacle The Sundering Heart.

O conteúdo rotativo vem dos **Eclipse Cycles**, sem reset de progresso. Não existem temporadas com reset.

**Curva de tempo esperada:**

| Marco | Tempo |
|---|---|
| Primeiro chefe | 1,5–2 h |
| Campanha Normal | 12–15 h |
| Nível 50 | 40–50 h |
| Eclipse completo | 50–60 h |
| A50 | 130–160 h |
| A100 | 300 h+ |

Detalhamento: `docs/gdd/10-progressao.md` §7–10, `docs/gdd/09-inimigos.md` §15.

---

# 11. Interface e experiência do jogador

A interface deverá apresentar as informações necessárias sem prejudicar a leitura do combate.

Telas e sistemas planejados:

- HUD de combate.
- Barras de vida e recurso.
- Habilidades e cooldowns.
- Indicadores de status e afinidades.
- Inventário.
- Comparação de equipamentos.
- Detalhes de atributos.
- Tela de habilidades.
- Tela de builds.
- Gerenciamento de conjuntos.
- Mapa e seleção de regiões.
- Tela de recompensas.
- Vault.

**v0.3:**

- **Tecnologia:** UI Toolkit para telas e HUD; renderer próprio instanciado para números de dano e barras world-space.
- **Tooltips de atributos** mostram o valor efetivo após diminishing returns (ex.: "23% → 21,5% efetivo").
- **Itens** exibem a qualidade de rolagem por afixo, o estado de modificadores inativos (por duplicata ou pelo limite global) e o Set Focus.

## 11.1 Comparação de equipamentos

A comparação deve apresentar:

- Diferenças de atributos.
- Efeitos especiais.
- Modificadores de habilidade.
- Raridade.
- Compatibilidade com o herói.
- Contribuição para bônus de conjunto.
- Impacto sobre a build atual.

O jogador deve conseguir entender por que uma peça pode ser melhor para determinada configuração, mesmo que tenha menos atributos totais.

## 11.2 Visualização de builds

A interface deve permitir visualizar as principais fontes de poder:

1. Atributos.
2. Modificadores de habilidades.
3. Set Powers.
4. Resonance.

Essa separação ajudará o jogador a compreender como sua build funciona.

**v0.3:** a visualização acrescenta **Augments** (Aether Lattice) e **Blessings** da run como fontes separadas.

Detalhamento: `docs/gdd/11-arquitetura.md` (UI), `docs/gdd/01-atributos.md` (exibição de DR).

---

# 12. Direção artística

**Estilo:** Stylized Dark Fantasy / Cosmic Fantasy.

Características:

- Silhuetas fortes.
- Personagens visualmente distintos.
- Criaturas com aparência sobrenatural.
- Cenários com arquitetura monumental.
- Contraste entre escuridão e energia primordial.
- Efeitos de habilidades com cores e formas reconhecíveis.
- Elementos cósmicos e distorções da realidade.

A direção visual deve priorizar legibilidade durante o combate.

Os efeitos de habilidades precisam ser visualmente satisfatórios sem esconder inimigos, projéteis ou indicadores importantes.

**v0.3 — regras de legibilidade:**

- Os VFX do jogador têm opacidade padrão de 80%, e os telegraphs inimigos são sempre desenhados por cima.
- Laranja fica reservado a Ember e não é usado em telegraphs.
- Cada afinidade tem cor, forma e ícone próprios.
- Câmera com FOV baixo (30°) para dar escala monumental com leitura quase isométrica.

Detalhamento: `docs/gdd/06-combate.md` §3.10–3.12, `docs/gdd/07-resonance.md` §10.

---

# 13. Direção de áudio

O áudio deve reforçar o peso dos ataques e a identidade das afinidades.

Elementos planejados:

- Sons de impacto específicos por arma.
- Feedback sonoro de acertos críticos.
- Sons distintos para cada afinidade.
- Áudio de ativação de habilidades.
- Feedback de efeitos de status.
- Sons próprios para elites e chefes.
- Ambientes sonoros específicos por região.
- Trilha dinâmica durante encontros.

O combate precisa transmitir impacto tanto visualmente quanto sonoramente.

**v0.3:** middleware **FMOD Studio**, com trilha dinâmica por intensidade e eventos por afinidade. Os hooks de áudio saem dos eventos de combate de 06 (OnHitDealt, OnPerfectDodge, OnStaggerBreak, OnParry etc.).

Detalhamento: `docs/gdd/11-arquitetura.md`, `docs/gdd/06-combate.md` (game feel).

---

# 14. Arquitetura técnica — Unity

A arquitetura deverá favorecer expansão de conteúdo e manutenção.

A proposta é utilizar uma abordagem data-driven, com uso extensivo de ScriptableObjects para definir dados de gameplay.

**Stack técnico (v0.3):**

| Área | Decisão |
|---|---|
| Engine | Unity 6.3 LTS (fallback 6.0 LTS) |
| Render | URP Forward+ |
| Input / câmera | Input System, Cinemachine 3 |
| Conteúdo | Addressables, VFX Graph |
| UI | UI Toolkit (+ renderer world-space próprio) |
| Áudio | FMOD Studio |
| Plataforma | Steamworks.NET, Unity Localization |
| Modelo de código | GameObjects na apresentação, núcleo de regras em C# puro (`Eclipse.Core`, testável sem Unity), Jobs + Burst nos hot paths; sem ECS completo, sem Odin |
| Simulação | 60 Hz fixos |
| Save | JSON + GZip + checksum, escrita atômica, 3 backups, migrações versionadas, suspend-save por sala |
| RNG | Streams com seed (xoshiro128**): Map, Encounter, Loot, Combat, Cosmetic |
| Specs mínimas | i5-8400 / Ryzen 5 2600, 8 GB, GTX 1060 6 GB / RX 580, 1080p Low a 60 fps |
| Orçamento | 120 inimigos ativos (pico 200), 2.000 projéteis, 0 B de GC por frame em combate |

Detalhamento: `docs/gdd/11-arquitetura.md`.

## 14.1 Sistemas principais

- Hero System
- Combat System
- Weapon System
- Skill System
- Skill Modifier System
- Attribute System
- Equipment System
- Loot Generation System
- Rarity System
- Set Power System
- Resonance System
- Enemy AI System
- Boss System
- Room Generation System
- Run Management System
- Progression System
- Inventory System
- Save System
- UI System
- Audio System

**v0.3:** a esses sistemas se somam o Augment System (Aether Lattice), o Blessing/Shrine System, o Attack Token Director, o CrowdControlTracker, o Event Bus com hooks fechados e as ferramentas de editor (importação de planilhas, validador de dados no CI, simulador headless de DPS).

## 14.2 Dados e ScriptableObjects

Os seguintes conteúdos devem ser configuráveis por dados:

- Heróis.
- Armas.
- Habilidades.
- Modificadores de habilidade.
- Equipamentos.
- Atributos.
- Raridades.
- Faixas de rolagem.
- Conjuntos.
- Poderes de conjunto.
- Afinidades.
- Efeitos de status.
- Inimigos.
- Chefes.
- Regiões.
- Salas.
- Eventos.
- Recompensas.
- **Augments, Blessings/Shrines, afixos de Elite, serviços do Sanctuary e da Rest Room** (v0.3).

**v0.3:** todo SO de definição tem um **StableId** imutável, e o save nunca guarda referência de asset. Tags ficam em bitmask (`ulong`); HitFlags (`Unavoidable`, `Unblockable`, `Annihilation`, `Execute`, `Knockdown`, `IsEcho`, `IsResonance`, `IsTriggered`) ficam numa máscara separada.

## 14.3 Geração de equipamentos

O sistema de loot deve separar a definição do item da instância encontrada pelo jogador.

**Definição do item:**
- Identidade.
- Nome.
- Arte.
- Slot.
- Tipo.
- Raridade.
- Efeito especial.
- Modificadores possíveis.
- Faixas de atributos.

**Instância do item:**
- Referência à definição original.
- Valores efetivamente rolados.
- Modificadores aplicados.
- Identificador único.
- Estado de aprimoramento, se aplicável.

Isso permite que várias instâncias do mesmo item tenham rolagens diferentes sem duplicar sua definição.

**v0.3:**

- A definição tem `ItemKind` (Legendary, peça de set, Unique) e `fonteUnique`.
- A instância guarda os `Roll01` normalizados, a seed e a fonte do drop, o nível de upgrade, os contadores de Enchant/Imbue, a flag Anchor e a marca Primal.
- O pity fica salvo por herói.

## 14.4 Arquitetura de modificadores

Os modificadores de habilidades devem ser implementados de maneira modular.

O sistema precisa permitir:

- Alterar parâmetros de uma habilidade.
- Adicionar efeitos.
- Converter afinidades.
- Criar projéteis adicionais.
- Repetir efeitos.
- Adicionar condições de ativação.
- Modificar custos e duração.
- Interagir com efeitos de Resonance.

Os modificadores não devem exigir a criação de uma habilidade completamente nova para cada combinação.

**v0.3:**

- Os `ModifierOp` são aplicados nas 6 fases de 03, em canais separados: Item, Set, Augment, Passive e Run.
- O resultado é um `SkillSpec` imutável, recompilado só quando o loadout muda; o custo dos modificadores no momento do cast é zero.
- `StatMod` tem os buckets `Flat`, `Increased` e `More`.
- A anti-recursão segue o modelo Generation, com a exceção `EchoRecursion1x` declarada por poder.

## 14.5 Arquitetura de Set Powers

O sistema de conjuntos deve observar os equipamentos atualmente equipados.

Ao equipar ou remover uma peça:

1. Identificar os conjuntos representados.
2. Contabilizar as peças de cada conjunto.
3. Verificar os requisitos de três e seis peças.
4. Ativar ou desativar os respectivos poderes.
5. Atualizar a interface e os efeitos do personagem.

Os poderes devem ser tratados como fontes independentes de efeitos, sem ocupar os espaços de modificadores dos equipamentos individuais.

**v0.3:** o SetTracker conta peças por definição distinta, aplica o limite de 2 Set Powers ativos, resolve a prioridade com o Set Focus e trata a Keystone of the Sundering.

## 14.6 Ordem de implementação (novo na v0.3)

| Fase | Entregas |
|---|---|
| F0 Fundação | Projeto, assemblies, CI, Definition/Registry, RNG, Tags, Event Bus, Input, câmera |
| F1 Combate vertical | Kael com 1 arma, kit completo, esquiva, hit detection a 60 Hz, 3 inimigos, StatBlock, DamageResolver; soak test de hordas |
| F2 Habilidades modulares | Pipeline de modificadores, Augments, StatusSystem, Generation |
| F3 Itemização | Itens, afixos, raridades, ItemGenerator, inventário, comparação, SetTracker, simulador |
| F4 Run loop | Mapa de nós, salas, Encounter Director, chefe 1, save/suspend |
| F5 Meta | Sanctuary, serviços, Vault, progressão, dificuldades |
| F6 Resonance + heróis | ResonanceSystem, Lyra, Nyx, Orun |
| F7 Conteúdo | Regiões 2–5, chefes, 12 sets, catálogo completo |
| F8 Endgame + polimento | Ascension, desafios, performance, acessibilidade, localização, Steam |

Riscos técnicos a atacar cedo: **Echo Clone**, **Void Constructs** e **hordas de 200 inimigos**.

Detalhamento: `docs/gdd/11-arquitetura.md` §3–4.

---

# 15. Princípios de balanceamento

O balanceamento numérico ainda será desenvolvido.

As regras de design que devem orientar essa etapa são:

1. Nenhum conjunto completo deve ser automaticamente superior a qualquer combinação híbrida.
2. Equipamentos Legendary individuais precisam continuar relevantes.
3. Atributos devem ter utilidade real, evitando opções claramente inúteis.
4. Modificadores devem criar mudanças interessantes, não apenas aumentos numéricos.
5. Builds precisam ser viáveis por diferentes caminhos.
6. Habilidades devem continuar úteis em diferentes estágios de progressão.
7. A raridade deve representar potencial e complexidade, não garantir que qualquer item de raridade superior seja sempre melhor.
8. A qualidade das rolagens deve importar sem tornar obrigatória a busca por valores perfeitos.
9. Sinergias devem recompensar experimentação sem eliminar a diversidade de builds.
10. A dificuldade deve crescer de forma que o jogador perceba a necessidade de melhorar sua estratégia e equipamentos.

**Ferramentas e tetos da v0.3:**

- Orçamento de poder por fonte (§1.3).
- Tetos de More:
  - modificadores: 0;
  - Augments: 0;
  - Legendary: ≤ +15%;
  - Unique: ≤ +40%;
  - Keystone: ≤ +20%;
  - Ultimate: ≤ +30% durante a duração;
  - Set Power: ≤ +30% sustentado;
  - Blessings: ≤ +20% no total.
- Teto de Vulnerability de +60%.
- Tetos de recurso: 6 de base, até 8 com fontes declaradas.
- Limite de 14 modificadores ativos e de 2 Set Powers.
- Modelo Generation contra recursão.
- Simulador headless de DPS em `Eclipse.Core` comparando as Rotas A/B/C, com guarda-corpo se a Rota A passar de ×1,35.

Detalhamento: `docs/gdd/00-convencoes.md` §4–5, `docs/gdd/04-conjuntos.md` §10.

---

# 16. Metas de conteúdo inicial

| Categoria | Meta inicial | v0.3 (detalhado) |
|---|---:|---:|
| Heróis | 4 | 4 |
| Builds principais | 12 | 12 |
| Afinidades | 6 | 6 (24 status, 15 Resonances) |
| Armas por herói | 5 | 5 (20 bases) |
| Regiões | 5 | 5 |
| Chefes principais | 5 | 5 + 1 pinnacle |
| Guardians | — | 10 |
| Inimigos-base | — | 40 |
| Afixos de Elite | — | 18 |
| Conjuntos Legendary | 12 | 12 |
| Peças de conjunto | 72 | 72 |
| Peças por conjunto | 6 | 6 |
| Bônus por conjunto | 2 | 2 (24 Set Powers) |
| Itens nomeados não-set | ~70 | 84 (54 Legendary + 30 Unique) |
| Modificadores de habilidade | — | 176 específicos + 30 genéricos |
| Augments | — | 60 |
| Salas | 8 categorias | 9 tipos (com Guardian) |
| Eventos | — | 14 |
| Blessings / Curses | — | 30 / 6 |
| Raridades | 4 | 4 (+ marca Primal) |

Essas metas representam a direção de conteúdo planejada. Novos conteúdos poderão ser adicionados durante o desenvolvimento.

---

# 17. Decisões consolidadas

## 17.1 Decisões da v0.2 (mantidas)

As seguintes decisões estão estabelecidas nesta versão do documento:

- O jogo será um Action Roguelite / ARPG isométrico 3D.
- A direção artística será Stylized Dark Fantasy / Cosmic Fantasy.
- O jogador poderá escolher entre múltiplos heróis.
- O conteúdo inicial terá quatro heróis.
- Cada herói terá pelo menos três arquétipos principais de build.
- A itemização terá quatro raridades: Common, Rare, Epic e Legendary.
- Common terá dois atributos.
- Rare terá três atributos.
- Epic terá quatro atributos e um modificador de habilidade.
- Legendary terá cinco atributos e dois modificadores de habilidade.
- Equipamentos terão identidade e efeitos especiais fixos.
- Atributos poderão variar dentro de faixas definidas.
- Modificadores terão efeitos definidos, com valores variáveis quando aplicável.
- Legendary poderá fazer parte de conjuntos.
- Cada conjunto terá seis peças.
- Três peças desbloqueiam um primeiro Set Power.
- Seis peças desbloqueiam um segundo Set Power.
- Set Powers não consomem os espaços de modificadores individuais dos equipamentos.
- O jogador poderá combinar dois conjuntos diferentes e ativar dois bônus de três peças.
- O sistema de Resonance permitirá interações entre afinidades.
- A build será construída pela combinação de atributos, modificadores, Set Powers e Resonance.
- O jogo terá um hub permanente chamado The Sanctuary.
- A progressão combinará recompensas de incursão e desbloqueios permanentes.
- A arquitetura técnica deverá favorecer sistemas modulares e data-driven.

## 17.2 Decisões novas da v0.3

**Combate e heróis**
- Simulação de combate a 60 Hz fixos; controle twin-stick (WASD + cursor), sem click-to-move.
- Input fixo: LMB/RMB, Space esquiva, Q/E/R/Shift habilidades, F Ultimate, 1 Aether Flask.
- Esquiva universal idêntica para todos: 2 cargas, 4,5 m, i-frames nos frames 2–13, recarga de 1,5 s.
- Habilidades escalam por Weapon Power (DPS normalizado); DPS de referência igual entre armas (±5%).
- Kit fixo de 5 habilidades; 3 Augments por habilidade (1 ativo) na árvore Aether Lattice; 55 pontos; 2 Keystones.
- Kael e Lyra iniciais; Nyx após o Ashen Colossus; Orun após o Pale Sovereign.
- Armas só na Main Hand; Off Hand própria do herói (Warbrand, Grimoire, Quiver, Idol), sempre ativa.
- Execute: até 20% em comuns, metade em Elites; Guardians e chefes imunes (vira +25% de Vulnerability).
- CC com regra única de perda de efeito; chefes convertem CC em Stagger, com exceções Frostbound e Distortion; Broken de 4 s com +30% de Vulnerability.
- Linguagem única de telegraph (vermelho, roxo, branco-dourado, ciano) com wind-up mínimo de 0,45 s; anti-one-shot de 60%.

**Atributos, itens e loot**
- Primário Core por herói concede Core Attunement (+0,08% More por ponto), a única fonte More de atributos.
- Armor `Armor/(Armor + 50·eLvl + 500)`; resistências em duas famílias (Elemental, Void); teto de 75%.
- Diminishing returns por soft cap linear com eficiência de 50%; Dodge e Block não atuam em golpes telegrafados.
- Tiers de iLvl T1–T5 de 20 em 20; rolagem uniforme salva como `Roll01`; raridade não muda a faixa.
- Legendary nomeado = 4 fixos + 1 Afixo Aberto + 2 modificadores + efeito; peça de set sem efeito próprio; Unique com Signature.
- Hero-bound para itens com modificador de herói; Epic 75% herói / 25% genérico.
- Sem Magic Find; smart loot de 75/85%; pity em três camadas; coleta automática; Satchel de 30 espaços.
- Primal é marca de qualidade (A50+), não raridade; upgrade +0..+10 (+3%/nível) igual para todas as raridades.
- 30 Uniques com fonte única cada; Uniques e peças de set não caem em Normal.

**Modificadores, sets e Resonance**
- 176 modificadores específicos + 30 genéricos; 3 por habilidade, 14 ativos no total; nunca dão More.
- Resolução de modificadores em 6 fases fixas; duplicatas não acumulam; grupos de exclusividade.
- Máximo de 2 Set Powers ativos (6pc conta como segundo); sets fora de Main Hand/Off Hand; sets exclusivos do herói.
- Cada set tem uma região-lar; Attunement ×3 no Eclipse+.
- Status por Buildup/Chance/Garantida, com Status Potency fixa por golpe; status primário automático por afinidade.
- Modelo Generation (nada dispara em Gen ≥ 2) com exceção Echo Recursion 1×; 8 procs de Resonance por segundo.
- Vulnerability com teto total de +60%.

**Run, economia e progressão**
- Run de 13 linhas + chefe (14 salas) em 3 Estratos; Guardian na linha 9; Rest garantida na linha 5 e Antecâmara.
- Morte nunca apaga Legendary nem XP; itens intermediários viram materiais a 50%; extração por Rift Anchor; Overrift após o chefe; sem revive.
- Rift Shards são a única moeda da run; 6 moedas permanentes + Heart of [Chefe].
- Blessings Lesser/Greater/Exalted, +20–30% de poder, fora do orçamento permanente.
- Dificuldades por conta (Normal, Veteran, Nightmare, Eclipse com Eclipse Trial, Ascension 1–100 com Thresholds); iLvl 100 em A90.
- Account Rank e níveis de NPC liberam sistemas, nunca atributos; respec gratuito; sem temporadas com reset.
- Curva de XP `200·L^1,8`; nível 50 em 40–50 h; A50 em 130–160 h.

**Arquitetura**
- Unity 6.3 LTS, URP, Input System, Addressables, UI Toolkit, FMOD; núcleo em C# puro testável, Jobs/Burst nos hot paths, sem ECS.
- ScriptableObjects com StableId; instâncias serializáveis; save JSON com migrações; RNG por streams com seed.
- Habilidades compiladas em `SkillSpec`; tags em bitmask e HitFlags separadas; IA HFSM + Utility.
- Specs mínimas GTX 1060 / i5-8400 a 1080p 60 fps; 120 inimigos ativos (pico 200).

---

# 18. Documentos de design

As próximas etapas de design listadas na v0.2 foram detalhadas nos documentos abaixo. Todos passaram por uma rodada de auditoria cruzada e correção (registro em `00-convencoes.md` §5, resoluções R01–R50).

| Doc | Escopo | Status |
|---|---|---|
| `docs/gdd/00-convencoes.md` | Contrato entre docs: formato, dono de cada conceito, parâmetros globais, pipeline de dano, tags, orçamento de poder, registro de resoluções R01–R50 | Vigente |
| `docs/gdd/01-atributos.md` | Fórmulas, caps, diminishing returns, primários, Armor/Resist, tabelas de rolagem por tier, multiplicadores de slot, recursos, upgrade | Detalhado, integrado |
| `docs/gdd/02-equipamentos.md` | Bases de item, 20 armas, Off Hands, pool de afixos por slot, compatibilidade, 84 itens nomeados (54 Legendary + 30 Unique) | Detalhado, integrado |
| `docs/gdd/03-modificadores.md` | Regras gerais, 176 modificadores específicos, 30 genéricos, Signature, orçamento, mapeamento de nomes | Detalhado, integrado |
| `docs/gdd/04-conjuntos.md` | 12 sets, 72 peças, 24 Set Powers, matriz de slots, análise das Rotas A/B/C | Detalhado, integrado |
| `docs/gdd/05-loot.md` | Fontes, iLvl, raridade, algoritmo de geração, smart loot, pity, target farming, qualidade, densidade, salvage | Detalhado, integrado |
| `docs/gdd/06-combate.md` | Pipeline de dano, input, frame data, estados genéricos, CC, hit detection, telegraphs, letalidade, TTK, câmera, game feel | Detalhado, integrado |
| `docs/gdd/07-resonance.md` | 24 status, 15 Resonances, prioridade, Generation, limites, Elites/chefes, Codex, legibilidade | Detalhado, integrado |
| `docs/gdd/08-herois.md` | Kits dos 4 heróis, recursos, Augments, Aether Lattice, movesets, builds, progressão 1–50 | Detalhado, integrado |
| `docs/gdd/09-inimigos.md` | Arquétipos, bestiário, afixos de Elite, Guardians, chefes, salas, mapa da run, Blessings, Curses, eventos, endgame | Detalhado, integrado |
| `docs/gdd/10-progressao.md` | Regra de morte, extração, moedas, serviços do Sanctuary, dificuldades, eLvl, Account Rank, endgame, curva de tempo | Detalhado, integrado |
| `docs/gdd/11-arquitetura.md` | Stack Unity, modelo de dados, stats, skills, eventos, IA, runs, save, performance, ferramentas, ordem de implementação | Detalhado, integrado |

## 18.1 Questões abertas para playtest

1. **Core Attunement (0,08%/ponto)** contra a fatia de ~30% do orçamento — validar no Training Grounds (01).
2. **Multiplicadores de slot** (Main Hand ×1,5, Relic ×1,25) — confirmar no simulador que atributos de equipamento ficam perto de 25% e que a arma não domina a decisão de upgrade (01, 02).
3. **Rota A no simulador** — confirmar o ganho marginal de 3,5–4,5% por efeito Legendary e aplicar o guarda-corpo acima de ×1,35 (04).
4. **Keystone of the Sundering** — se o uso passar de 60% nas Rotas B/C, reduzir as faixas (02, 04).
5. **Limite de modificadores** (3 por habilidade, 14 no total) — verificar se a Rota B fica sem espaço para genéricos (03).
6. **Proc Coefficient de clones (0,4)** e **Thousand Echoes 6pc** — competitividade de Echo Architect e custo de VFX no pior caso (03, 04).
7. **Uptime de Ashen Ascension** — meta de 50–60% (04).
8. **Ritmo de set** — ~2 h para 3 peças e ~6 h para 6 no Eclipse, sem esvaziar o objetivo de longo prazo (05).
9. **Primeiro Legendary antes da 2ª run** — via primeiro abate do Ashen Colossus (05).
10. **Grace period de 0,25 s em Eclipse** — testar 0,15 s nesse tier se o jogador ficar resistente demais (06).
11. **Stagger de chefe** — meta de 2–3 Breaks por luta para build dedicada e 1 para build sem foco (06).
12. **Participação da Resonance** no dano total (meta ~10%, alarme acima de 20%) e frequência ≥ 1 a cada 10 s com 2 itens dedicados (07).
13. **Decaimento de Fury** e **tempo até Orun "ficar online"** a cada sala (≤ ~5 s) (08).
14. **Retenção de 50% na morte** — tensão sem desmotivar; faixa de teste de 40–60% (10).
15. **Performance** — 200 inimigos no hardware mínimo e custo do CrowdControlTracker com 8 procs/s (11).

---

# 19. Changelog v0.2 → v0.3

**Estrutura de documentação**
- Criados 12 documentos de detalhamento em `docs/gdd/` (00–11), cobrindo as 11 etapas da seção 18 da v0.2.
- Criado o contrato `00-convencoes.md`, com o dono de cada conceito, parâmetros globais, pipeline de dano, vocabulário de tags e orçamento de poder.
- Feita uma rodada de auditoria cruzada (itemização, combate/conteúdo, meta/tecnologia) e aplicadas 50 resoluções de integração (R01–R50).
- Seção 18 da v0.2 ("Próximas etapas") substituída pelo índice de documentos e pelas questões de playtest.

**Mudanças e concretizações no design**
- §1.3: orçamento de poder por fonte.
- §3.1: regra de morte canônica, extração (Rift Anchor), Overrift, Satchel e ausência de revive.
- §3.2: mapa de 13 linhas + chefe, Guardian Arena, serviços da Rest Room e Blessings.
- §3.3: serviços definidos de cada NPC e Account Rank.
- §4: desbloqueio de heróis, Off Hand por herói, Aether Lattice/Augments, recursos detalhados e curva de XP.
- §5: input, esquiva universal, Aether Flask, câmera, telegraphs, tokens de ataque, durações alvo, Weapon Power e pipeline de dano (§5.4).
- §6.2: estrutura Legendary nomeado / peça de set / Unique, Afixo Aberto e marca Primal.
- §6.3: Status Potency adicionado e regras numéricas resumidas.
- §6.4: tiers de iLvl, `Roll01`, Faixa Nomeada e multiplicadores de slot.
- §6.5: catálogo de modificadores, limites e fases de resolução.
- §6.6: máximo de 2 Set Powers ativos, Set Focus, sets fora dos slots de arma e Keystone of the Sundering.
- §6.9: contagem real de conteúdo (84 itens nomeados, 30 Uniques).
- §7: modelo de aplicação de status, status primário automático, Generation, teto de Vulnerability e versões reduzidas de CC para chefes.
- §8: estrutura das regiões, recompensas de chefe e **§8.3 região-lar dos sets**.
- §9: Monster Level, 18 afixos de Elite, Guardians, eventos e conteúdo de endgame.
- §10: tabela de moedas, tabela de dificuldades, 5 pilares de endgame e curva de tempo.
- §11–13: tecnologia de UI, regras de legibilidade de VFX e FMOD.
- §14: stack técnico, novos sistemas, StableId, HitFlags e ordem de implementação (§14.6).
- §15: tetos numéricos e ferramenta de simulação.
- §16: coluna com as quantidades detalhadas.
- §17: 41 decisões novas consolidadas (§17.2).

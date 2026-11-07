# 08 — Design detalhado dos heróis

> PROJECT ECLIPSE · GDD v0.3 · Dono: kit dos heróis, passivas, recursos, árvore de talentos, armas por herói e progressão de herói 1–50.
> Respeita `00-convencoes.md`. Valores numéricos são de primeira passada de balanceamento: são decisões, não placeholders, e devem ser ajustados apenas por playtest.

---

## 1. Objetivo e escopo

Definir, para Kael, Lyra, Nyx e Orun:

- fantasia e mecânica de recurso (geração, gasto, decaimento, tradução dos atributos genéricos de recurso);
- ataque básico, ataque forte e esquiva;
- as 5 habilidades (dano, custo, cooldown, alcance, área, tags, comportamento, Augments);
- passivas inatas;
- árvore de talentos (**Aether Lattice**) com estrutura, pontos e nós-chave;
- como cada uma das 5 armas muda o kit;
- loadout;
- as 3 builds principais com rotas de itemização A/B/C;
- progressão de herói 1–50 e desbloqueio dos heróis.

Fora de escopo: fórmulas de atributos (01), itens nomeados (02), modificadores de equipamento (03), Set Powers (04), status de afinidade (07), estados genéricos e frames de animação (06).

---

## 2. Decisões tomadas

1. **Kit fixo de 5 habilidades por herói, sempre equipadas.** A profundidade vem de Augments, modificadores, sets e Resonance, não de trocar habilidades. *Justificativa:* mantém identidade forte do herói (Ravenswatch/Hades) e torna os catálogos de modificadores (03) e sets (04) finitos e testáveis.
2. **Mapeamento fixo de input (00 R05, dono 06):** Basic (LMB), Heavy (RMB), Esquiva (Espaço), Slot 1 (Q), Slot 2 (E), Slot 3 (R), Slot 4 de mobilidade/utilidade (Shift), Ultimate (F), Aether Flask (1). Gamepad definido por 06. *Justificativa:* memória muscular compartilhada entre heróis.
3. **Esquiva universal e idêntica para os 4 heróis (00 R06, dono 06):** dash de 4,5 m, 2 cargas, recarga de 1,5 s por carga, i-frames nos frames 2–13. Nenhum herói altera a esquiva base; variações vêm só de habilidades (ex.: Shadowstep). *Justificativa:* combate responsivo exige uma ferramenta defensiva que não compete com a build, e uma esquiva única mantém a leitura de telegraphs igual entre heróis.
4. **Augments de habilidade vêm da árvore de talentos:** cada habilidade tem 3 Augments (um alinhado a cada build principal); **apenas 1 Augment ativo por habilidade**. Augments são uma camada separada dos modificadores de equipamento (03) e podem coexistir. *Justificativa:* dá ao jogador uma alavanca de build sem depender de loot, e garante as 3 builds principais desde o nível 10.
5. **Todas as 20 armas ocupam apenas o slot Main Hand** (armas duplas são um único item "par"). O slot Off Hand recebe sempre um **item de Off Hand específico do herói, não-arma**, com os nomes de 02 (Kael: **Warbrand**; Lyra: **Grimoire**; Nyx: **Quiver**; Orun: **Idol**). Off Hand fica ativo desde o nível 1 e **nenhum slot é bloqueado por nível** (00 R29). *Justificativa:* elimina a distinção 1H/2H, mantém os 12 slots sempre úteis e simplifica comparação de itens.
6. **Habilidades escalam com Weapon Power (DPS-base normalizado da arma, 06); ataques básicos escalam com o dano por golpe.** 08 define o moveset de cada arma (nº de golpes, alcance, comportamento) e 06 ajusta a frame data a esses movesets; o número de DPS de cada arma é de 02 (00 R18). *Justificativa:* evita que armas lentas sejam sempre superiores para habilidades; a escolha de arma muda o moveset e a sensação, não o dano das skills.
7. **Respec gratuito da árvore no Sanctuary** (Keeper). *Justificativa:* roguelite pede experimentação; o custo de troca é tempo de run, não moeda.
8. **55 pontos de talento no total** (49 por nível + 6 de marcos), contra ~131 pontos de custo total por árvore. *Justificativa:* permite uma ramificação completa + parte de outra, forçando escolhas reais.
9. **Máximo de 2 Keystones ativos** (desbloqueados nos níveis 20 e 35).
10. **Heróis iniciais: Kael e Lyra** (melee e ranged). Nyx desbloqueia ao derrotar o Ashen Colossus pela primeira vez; Orun ao derrotar o Pale Sovereign pela primeira vez (mantido por 00 R40). *Justificativa:* onboarding com dois estilos contrastantes e duas metas de progressão claras no começo.
11. **Habilidades desbloqueadas gradualmente nos níveis 1–8** do herói (onboarding), kit completo a partir do nível 8.
12. **Fontes "More" (00 R03):** nenhum Augment dá More; Keystones dão no máximo +20% More sustentado; Ultimates no máximo +30% More durante sua duração. Penalidades ("Less") não têm teto. Lista em §10.3.
13. **Tetos de recurso (00 R01):** Arcane Charges e Void Constructs têm teto base 6; só as fontes listadas em §11.1 podem elevar o teto, e nunca acima de **8**. Orun usa a barra Void Mass que gera Void Constructs.
14. **Curva de XP é de 10-progressao (00 R40):** `XP = 200·L^1,8`, nível 50 em ~40–50 h, catch-up −10 níveis / +100%. Este doc mantém os desbloqueios por nível e de heróis.
15. **Execute segue 06 (00 R08):** limiar máximo 20% em inimigos comuns, Elites com metade (máx. 10%), Guardians e chefes imunes; contra chefes, o efeito vira +25% Vulnerability abaixo de 20% de vida (uma conversão, não acumula).

---

## 3. Convenções deste documento

| Termo | Definição |
|---|---|
| **DA** | Dano da Arma. Para habilidades: DPS-base normalizado da arma equipada (dano médio por golpe × APS ÷ APS de referência 1,0). Para ataques básicos/fortes: dano por golpe da arma × multiplicador do golpe. |
| **APS** | Ataques por segundo do combo básico. Afetado por Attack Speed. |
| **Cast** | Tempo de conjuração. Habilidades `Spell` escalam com Cast Speed; habilidades `Attack` têm animação acelerada por Attack Speed. |
| **CD** | Cooldown em segundos, antes de Cooldown Reduction. |
| **Increased / More** | Conforme pipeline do contrato §3.2. |
| **Execute** | Estado de 06 (flag `Execute` da DamageInstance, não tag). Mata alvos abaixo do limiar: máx. 20% em comuns, Elites com metade (máx. 10%); Guardians e chefes imunes — contra chefes vira +25% Vulnerability abaixo de 20% de vida (00 R08). Bônus de limiar (Heavy Blade, Death Sentence) nunca passam do teto de 20%. |
| **Vulnerability** | Status de 07. Os valores deste doc são por fonte e somam com outras fontes até o teto total de **+60%** (00 R12). |
| **Parry** | Estado de 06 (janela base 0,2 s; 00 R20). Augments e armas podem ampliar a janela. |
| **Unstoppable / Taunt / Stealth / Intangível** | Estados genéricos definidos por 06 (00 R20). |

### 3.1 Atributos-base por herói (01 é dono da escala e dos valores)

| Herói | Atributo principal | Vida-base relativa | Armor-base relativa | Movement Speed (01) | Faixa de engajamento |
|---|---|---:|---:|---:|---|
| Kael | Strength | 120% | 130% | 6,0 m/s | 0–4 m |
| Lyra | Intelligence | 85% | 75% | 6,0 m/s | 8–18 m |
| Nyx | Dexterity | 90% | 85% | 6,5 m/s | 0–20 m (híbrida) |
| Orun | Intelligence (secundário Strength) | 105% | 110% | 5,8 m/s | 3–12 m |

100% = vida/armadura de referência definidas em 01 para o nível do herói. Movement Speed citado de 01 (00 R50).

### 3.2 Tradução dos atributos genéricos de recurso

| Atributo de equipamento | Kael (Fury) | Lyra (Arcane Charges) | Nyx (Echo Energy) | Orun (Void Constructs) |
|---|---|---|---|---|
| Maximum Resource | +1 Fury máxima por 1% (100 → teto 150) | +1 carga a cada 25% (3 → teto base 6) | +1 Echo Energy máxima por 1% (100 → teto 150) | +1 construção a cada 30% (3 → teto base 6) |
| Resource Generation | +% Fury gerada | +% progresso de geração de carga | +% regeneração e ganho por acerto | +% Void Mass gerada |
| Resource Cost Reduction | −% custo de Fury | chance de não consumir a carga mais recente ao consumir | −% custo de Echo Energy | chance de não consumir uma construção ao sacrificar |

Conversões numéricas e o tratamento do excedente (dano/vida) são de 01 §Recursos. O teto base 6 de Lyra/Orun só é ultrapassado pelas fontes da tabela §11.1, até 8.

---

## 4. Kael — The Ashbound

**Fantasia:** um guerreiro que transforma dor em fogo. Quanto mais perto da morte e mais tempo em combate, mais perigoso fica. Recompensa agressividade sustentada e leitura de quando gastar tudo.

### 4.1 Recurso — Fury (0–100, teto 150)

| Regra | Valor |
|---|---|
| Ganho por ataque básico | +5 no primeiro alvo atingido, +1 por alvo adicional (máx. +9 por golpe) |
| Ganho por ataque forte | +10 (+15 se totalmente carregado) |
| Ganho por dano recebido | +1 por 1% de vida máxima perdida (máx. 15 Fury/s) |
| Ganho por abate | +3 |
| Gasto | Habilidades de custo (Cleaving Strike, Burning Chains); Worldbreaker consome tudo |
| Decaimento | Após 4 s sem causar nem receber dano: −8 Fury/s |
| Início de sala | 0 Fury (Rest/Shrine não alteram) |

### 4.2 Passivas inatas

- **Embers of Rage:** cada 10 Fury atual concede +2% Increased Physical e Ember Damage (máx. +20% a 100; continua escalando até o teto de 150 = +30%).
- **Ashbound Resolve:** abaixo de 35% de vida, Kael recebe 15% menos dano e gera Fury 25% mais rápido.

### 4.3 Ataques e esquiva (com Greatsword, arma de referência)

| Ação | Comportamento | Dano |
|---|---|---|
| Basic | Combo de 3 golpes em arco 120°, 2,8 m. APS 0,9 | 110% / 110% / 160% do dano por golpe |
| Heavy | Golpe descendente carregável. Toque: 0,6 s. Carga total: 1,2 s. Cone de 3 m. Alto Stagger | 280% (toque) / 420% (carregado) |
| Esquiva | Universal (§2, decisão 3) | — |

### 4.4 Habilidades

| Slot | Habilidade | Custo | CD | Alcance / Área | Dano | Tags |
|---|---|---:|---:|---|---|---|
| Q | Cleaving Strike | 20 Fury | — | Arco 180°, 3,5 m | 220% DA | Attack, Melee, Area, Physical |
| Shift | Infernal Leap | gera +15 Fury | 9 s | Salto até 8 m, impacto raio 3 m | 180% DA | Attack, Movement, Area, Ember |
| E | Burning Chains | 15 Fury | 12 s | Cone 8 m, até 5 alvos | 120% DA + Burning | Attack, Ranged, Ember, DoT |
| R | Ashen Guard | — | 14 s | Pessoal / contra-ataque arco 3 m | 250% DA + 30% do dano absorvido | Defensive, Attack, Melee, Physical |
| F | Worldbreaker | consome toda a Fury (mín. 50) | 90 s | Raio 7 m, fissuras radiais | 600% DA + 6% DA por Fury consumida | Attack, Area, Ultimate, Physical, Ruin |

**Comportamentos:**
- **Cleaving Strike:** spender principal. Pode ser usado em sequência sem cooldown enquanto houver Fury; cancela a recuperação do ataque básico (combo-friendly).
- **Infernal Leap:** salto mirado; Kael é Unstoppable (06) durante o ar (0,5 s). Deixa o chão Burning por 2 s no impacto.
- **Burning Chains:** correntes prendem (Root 2 s) e aplicam Burning. Enquanto acorrentados, os alvos recebem a Burning de Kael; quebrar a corrente (fim do Root ou distância > 10 m) causa 60% DA adicional.
- **Ashen Guard:** postura de 1,5 s; reduz dano recebido em 60% e bloqueia completamente ataques frontais (exceto `Unblockable`, 06). Ao final (ou ao soltar o botão), contra-ataca. Ser atingido nos primeiros 0,2 s da postura = **Parry** (06): dano anulado e o contra-ataque causa +50%. Gera +20 Fury ao contra-atacar.
- **Worldbreaker:** golpe no chão que abre 6 fissuras radiais; inimigos atingidos ficam com Vulnerability Ruin (07) por 6 s.

### 4.5 Augments (1 ativo por habilidade)

Builds: **I** = Inferno Berserker, **R** = Ruin Executioner, **J** = Unbreakable Juggernaut.

| Habilidade | I | R | J |
|---|---|---|---|
| Cleaving Strike | **Wildfire Cleave:** 50% do dano convertido em Ember; aplica Burning | **Headsman's Arc:** +100% dano contra alvos abaixo de 35% de vida; abates devolvem 10 Fury | **Bulwark Sweep:** concede escudo de 4% da vida máxima por inimigo atingido (máx. 20%, 4 s) |
| Infernal Leap | **Meteor Fall:** impacto vira Explosion (07), raio +50%, chão em chamas por 4 s | **Ruinous Descent:** aplica Vulnerability 15% por 5 s | **Iron Landing:** Taunt de 3 s no raio e +30% Damage Reduction por 3 s |
| Burning Chains | **Searing Links:** Burning +100% duração; ao morrer, o alvo propaga as correntes para 2 inimigos | **Hangman's Chains:** alvos acorrentados recebem +25% dano de Kael (Increased); quebrar a corrente aplica Execute de 15% (Elites 7,5%; R08) | **Tethering Chains:** puxa todos os alvos até Kael; +10% Armor por alvo acorrentado (máx. 50%) |
| Ashen Guard | **Flashpoint:** o contra-ataque libera uma nova Ember (raio 5 m, 200% DA) e concede +40 Fury | **Grim Riposte:** o contra-ataque aplica Execute de 20% (Elites 10%; R08) | **Unbroken:** postura dura 3 s, Kael pode se mover a 50% da velocidade; janela de Parry 0,35 s |
| Worldbreaker | **Cataclysm:** as fissuras entram em erupção 3 vezes (a cada 1 s) com 40% do dano | **Doom of Ages:** após 2 s, alvos atingidos sofrem Detonation (07) de 300% DA; abaixo de 20% de vida a Detonation aplica Execute (Elites 10%; chefes/Guardians recebem a conversão R08) | **Mountain's Heart:** concede escudo de 30% da vida máxima e Unstoppable (06) por 4 s |

### 4.6 Armas

| Arma | APS | Moveset Basic | Moveset Heavy | Bônus de kit |
|---|---:|---|---|---|
| Greatsword | 0,9 | 3 golpes, arcos 120°, 2,8 m | Golpe descendente carregável (cone 3 m) | Cleaving Strike +20% área |
| Dual Axes | 1,6 | 5 golpes rápidos, 2,2 m; 5º golpe é um giro 360° | Redemoinho de 1,2 s (pode andar) | +2 Fury por golpe básico; Burning Chains +1 corrente |
| Warhammer | 0,7 | 2 golpes pesados, 2,5 m, alto Stagger | Slam no chão com onda de choque radial 4 m | Infernal Leap e Worldbreaker +25% Stagger Damage; raio do Leap +1 m |
| Longsword | 1,2 | 4 golpes; 4º é uma estocada com avanço de 3 m | Estocada em linha de 5 m que perfura | Janela de Parry do Ashen Guard +0,1 s; contra-ataque +20% dano |
| Heavy Blade | 0,8 | 3 cortes largos de 180°, 2,6 m | Golpe de execução: +100% dano contra alvos abaixo de 35% | Limiar de todas as execuções de Kael +5 pontos percentuais (nunca acima de 20%; Elites metade) |

O número de DPS de cada arma é de 02 (±5% entre armas do mesmo herói; armas de área −10% contra alvo único; especialistas +5%). A frame data destes movesets é de 06 (00 R18).

### 4.7 Builds principais

**Inferno Berserker** (I)
- **Loop:** gerar Fury rapidamente com Dual Axes ou Greatsword, manter Fury alta para Embers of Rage, gastar em Cleaving Strike (Wildfire Cleave) e explodir com Infernal Leap (Meteor Fall). Burning espalhando com Searing Links.
- **Prioridades:** Ember Damage, Attack Speed, Resource Generation, Area Damage, Maximum Health.
- **Keystones:** Combustion Rage + Ember Soul.
- **Rota A:** Legendaries individuais focados em Ember/Fury (ex.: Ashfang, Greatsword) + modificadores Burning Arc/Volatile Impact (03).
- **Rota B:** Ashen Sovereign 6 peças (transformação Ashen Ascension).
- **Rota C:** Ashen Sovereign 3 + Ruinbreaker 3 (transformação + golpes pesados com Ruin).

**Ruin Executioner** (R)
- **Loop:** aplicar Vulnerability com Infernal Leap (Ruinous Descent) e Worldbreaker, finalizar com Cleaving Strike (Headsman's Arc), encadear abates para devolver Fury. Heavy Blade é a arma assinatura.
- **Prioridades:** Ruin Damage, Critical Chance, Critical Damage, Armor Penetration, Stagger Damage.
- **Keystones:** Headsman + Doomsayer.
- **Rota A:** Legendaries de execução + modificadores Executioner's Reach e Ruin Cleaver (03).
- **Rota B:** Ruinbreaker 6 peças.
- **Rota C:** Ruinbreaker 3 + Eternal Bastion 3 (execução com sobrevivência).

**Unbreakable Juggernaut** (J)
- **Loop:** puxar com Tethering Chains, segurar a horda com Ashen Guard (Unbroken), punir com contra-ataques; dano recebido gera Fury que vira escudo.
- **Prioridades:** Armor, Maximum Health, Block Chance, Damage Reduction, Cooldown Reduction.
- **Keystones:** Scarred Titan + Iron Bastion.
- **Rota A:** Legendaries defensivos com efeitos de contra-ataque.
- **Rota B:** Eternal Bastion 6 peças.
- **Rota C:** Eternal Bastion 3 + Ashen Sovereign 3 (tanque que vira ofensivo em transformação).

---

## 5. Lyra — The Veilweaver

**Fantasia:** uma maga que dobra o espaço e o frio. Controla onde os inimigos estão e por quanto tempo podem agir. Recompensa posicionamento, previsão e o timing de "acumular e soltar".

### 5.1 Recurso — Arcane Charges (cargas; base 3, teto base 6, até 8 só via §11.1)

| Regra | Valor |
|---|---|
| Ganho por ataque básico | 1 carga a cada 3 golpes que acertam |
| Ganho por Frost Lance | 1 carga em acerto crítico (ICD 1 s) |
| Ganho por Astral Shift | +1 carga |
| Ganho passivo | 1 carga a cada 5 s em combate |
| Gasto | Habilidades com rótulo **Consome** gastam **todas** as cargas e escalam por carga gasta |
| Decaimento | Após 6 s fora de combate: −1 carga a cada 4 s |
| Início de sala | Mantém as cargas da sala anterior |

### 5.2 Passivas inatas

- **Arcane Lattice:** cada carga mantida concede +5% Cast Speed e +3% Increased Spell Damage.
- **Veilweaver's Grace:** após usar Astral Shift, o próximo Spell em até 2 s tem +25% Critical Chance.

### 5.3 Ataques e esquiva (com Arcane Staff)

| Ação | Comportamento | Dano |
|---|---|---|
| Basic | 3 projéteis arcanos, 14 m; o 3º perfura. APS 1,1 | 120% / 120% / 150% |
| Heavy | Canaliza um raio de Frost de 12 m (ticks a cada 0,25 s, até 2 s). Pode girar lentamente | 45% por tick; aplica Chill (07) |
| Esquiva | Universal | — |

### 5.4 Habilidades

| Slot | Habilidade | Custo | CD | Alcance / Área | Dano | Tags |
|---|---|---:|---:|---|---|---|
| Q | Frost Lance | — | — (cast 0,45 s) | Projétil 18 m, perfura 1 | 160% DA | Spell, Projectile, Ranged, Frost |
| E | Void Orb | **Consome** | 6 s | Orbe lento, 12 m em 2 s, raio de puxão 2,5 m | 60% DA a cada 0,5 s + implosão 200% DA (+50% por carga) | Spell, Projectile, Area, DoT, Void |
| R | Shattered Reality | **Consome** | 10 s | Área raio 4 m até 14 m; detona após 0,6 s | 350% DA; +1 fratura extra por carga (40% DA cada) | Spell, Area, Void |
| Shift | Astral Shift | — | 7 s (2 cargas) | Teleporte 7 m | Nova de Frost na origem: 80% DA | Spell, Movement, Frost |
| F | Absolute Zero | **Consome** | 100 s (cast 1 s) | Raio 8 m ao redor de Lyra | 500% DA + 100% por carga | Spell, Area, Ultimate, Frost |

**Comportamentos:**
- **Frost Lance:** ferramenta de dano contínuo; aplica Chill. Em alvo Frozen, causa Shatter (07).
- **Void Orb:** viaja lentamente aplicando Pull contínuo; implode ao fim do trajeto ou ao reativar a tecla.
- **Shattered Reality:** a fratura central aparece onde o cursor está; as fraturas extras aparecem nos inimigos mais próximos dentro de 6 m.
- **Astral Shift:** Lyra fica intangível durante o teleporte (i-frames do deslocamento).
- **Absolute Zero:** congela todos os não-chefes por 3 s (exceção de Ultimate declarada em 07, 00 R49; DR de CC de 06 se aplica). Chefes recebem Frostbound (07). Ao terminar, Lyra fica com as cargas no máximo.

### 5.5 Augments

Builds: **P** = Permafrost, **V** = Void Singularity, **A** = Arcane Overload.

| Habilidade | P | V | A |
|---|---|---|---|
| Frost Lance | **Glacial Spear:** +100% dano contra alvos Chilled/Frozen; Shatter garantido em Frozen | **Null Lance:** converte em Void, perfura todos os alvos e aplica Pull leve | **Splitting Lance:** dispara 3 lanças em leque (55% cada); chance de carga em crítico passa a ser por lança |
| Void Orb | **Winter Orb:** converte em Frost; deixa um rastro congelante de 3 s | **Stillpoint:** o orbe para no cursor; raio de puxão +100%; duração +1,5 s | **Overcharged Orb:** implosão +100% por carga (em vez de +50%), mas explode no primeiro alvo atingido |
| Shattered Reality | **Rime Fracture:** adiciona Frost; congela alvos Chilled | **Collapse Point:** todas as fraturas convergem para o centro, causando uma única implosão com Pull | **Paradox Cascade:** não consome cargas; gera 1 carga por inimigo atingido (máx. 2) |
| Astral Shift | **Frozen Wake:** o trajeto aplica Freeze por 1 s | **Rift Step:** deixa uma mini-singularidade na origem (Pull por 2 s) | **Mana Surge:** a próxima habilidade **Consome** em até 3 s conta 2 cargas extras |
| Absolute Zero | **Eternal Winter:** ao fim do Freeze, os alvos ficam com Chill máximo por 4 s e recebem +20% Increased Frost Damage durante esse tempo (o Freeze não passa do teto de 3 s) | **Singularity Zero:** puxa todos os inimigos no raio para o centro antes de congelar | **Overflow:** por 10 s, carga máxima +2 (pode exceder o teto base 6, até 8) e gera 1 carga/s |

### 5.6 Armas

| Arma | APS | Moveset Basic | Moveset Heavy | Bônus de kit |
|---|---:|---|---|---|
| Arcane Staff | 1,1 | 3 projéteis, 14 m, 3º perfura | Raio de Frost canalizado | Shattered Reality raio +20% |
| Spellbook | 1,4 | Páginas teleguiadas, 12 m | Tempestade de páginas em cone de 7 m | Ataques básicos geram carga a cada 2 golpes (em vez de 3) |
| Twin Wands | 2,2 | Projéteis alternados rápidos, 13 m | Rajada de 5 projéteis em leque | Frost Lance +1 perfuração; Chill aplicado +25% |
| Crystal Orb | 1,0 | Cristal orbitante que golpeia o inimigo mais próximo (6 m, auto-mira) | Lança o orbe; ele retorna atingindo duas vezes | Void Orb viaja 30% mais devagar (mais ticks) e tem +20% raio |
| Void Focus | 1,3 | Chicote de Void em cone de 6 m (caster de média distância) | Gravity Grip: puxa inimigos de um cone de 8 m | Força de Pull de todas as habilidades +30%; 25% do dano básico convertido em Void |

### 5.7 Builds principais

**Permafrost** (P)
- **Loop:** Chill com Twin Wands/Frost Lance, Freeze com Rime Fracture e Frozen Wake, Shatter com Glacial Spear. Absolute Zero como reset de controle.
- **Prioridades:** Frost Damage, Duration of Effects, Cast Speed, Critical Chance, Cooldown Reduction.
- **Keystones:** Heart of Winter + Glacial Body.
- **Rota A:** Legendaries de Freeze/Shatter + modificador Shattering (03).
- **Rota B:** Frozen Empress 6.
- **Rota C:** Frozen Empress 3 + Void Oracle 3 (Frost + Void = agrupamento e congelamento, Resonance §7.2 do GDD).

**Void Singularity** (V)
- **Loop:** agrupar com Stillpoint e Rift Step, empilhar zonas, finalizar com Collapse Point. Void Focus ou Crystal Orb.
- **Prioridades:** Void Damage, Area Damage, Damage Over Time, Duration of Effects, Cast Speed.
- **Keystones:** Singularity Mind + Eventide.
- **Rota A:** Legendaries de Pull e implosão.
- **Rota B:** Void Oracle 6.
- **Rota C:** Void Oracle 3 + Arcane Paradox 3.

**Arcane Overload** (A)
- **Loop:** gerar cargas com Spellbook e Splitting Lance, gastar com Overcharged Orb, renovar com Paradox Cascade e Mana Surge. Ritmo alto de "encher e esvaziar".
- **Prioridades:** Resource Generation, Maximum Resource, Skill Damage, Cast Speed, Critical Damage.
- **Keystones:** Overcharge + Charge Engine.
- **Rota A:** Legendaries de geração de cargas.
- **Rota B:** Arcane Paradox 6.
- **Rota C:** Arcane Paradox 3 + Frozen Empress 3.

---

## 6. Nyx — The Echo Hunter

**Fantasia:** uma caçadora que nunca está onde o inimigo olha. Cada ação deixa ecos que repetem o golpe. Recompensa movimento constante, preparação (armadilhas, clones) e finalizações precisas.

### 6.1 Recurso — Echo Energy (0–100, teto 150)

| Regra | Valor |
|---|---|
| Regeneração passiva | 10/s (sempre, inclusive fora de combate) |
| Ganho por ataque básico | +2 por acerto |
| Ganho por Phantom Shot em alvo Marcado | +10 |
| Gasto | Phantom Shot, Shadowstep, Storm Trap, Echo Clone |
| Decaimento | Não decai |
| Início de sala | Cheia |

### 6.2 Passivas inatas

- **Hunter's Mark:** o 4º acerto consecutivo de Nyx no mesmo alvo o **Marca** por 6 s (1 alvo marcado por vez; Elites e chefes são marcados no 3º acerto). Alvos marcados recebem +10% Critical Chance de Nyx.
- **Afterimage:** usar Shadowstep ou esquiva deixa uma imagem residual na origem que repete o próximo ataque básico de Nyx com 40% do dano (tag Echo).

### 6.3 Ataques e esquiva (com Dual Daggers)

| Ação | Comportamento | Dano |
|---|---|---|
| Basic | Combo de 4 golpes rápidos, 2 m. APS 1,8 | 80% / 80% / 90% / 130% |
| Heavy | Estocada com avanço de 3 m; +50% dano pelas costas | 260% |
| Esquiva | Universal (§2, decisão 3) | — |

### 6.4 Habilidades

| Slot | Habilidade | Custo | CD | Alcance / Área | Dano | Tags |
|---|---|---:|---:|---|---|---|
| Q | Phantom Shot | 15 | — | Projétil 20 m, perfura 2 | 180% DA | Attack, Projectile, Ranged, Storm |
| Shift | Shadowstep | 20 | 4 s (2 cargas) | Dash 8 m atravessando inimigos | 100% DA aos atravessados | Attack, Movement, Physical |
| E | Storm Trap | 25 | 8 s | Arremesso até 10 m; gatilho raio 3 m; máx. 3 ativas | 220% DA + Shock; encadeia para 3 alvos | Attack, Trap, Area, Storm |
| R | Echo Clone | 30 | 15 s | Clone por 8 s | Replica habilidades a 40% e básicos a 25% | Clone, Echo, Summon |
| F | Thousand Reflections | — | 90 s | Área raio 6 m ao redor do alvo; 4 s | 12 golpes de 120% DA + golpe final 400% DA | Attack, Area, Ultimate, Clone, Echo, Storm |

**Comportamentos:**
- **Phantom Shot:** pode ser disparado durante movimento e sem cancelar o combo básico.
- **Shadowstep:** o próximo ataque em até 2 s tem +50% Critical Chance.
- **Storm Trap:** arma em 0,5 s; dispara ao primeiro inimigo; desaparece após 20 s.
- **Echo Clone:** o clone aparece ao lado de Nyx, segue-a e repete cada habilidade usada (exceto Ultimate) no mesmo alvo/direção. Tem 30% da vida de Nyx e não pode ser curado. Cópias do clone são réplicas (`IsEcho`, proc coefficient 0,4) e não geram novas réplicas (00 R07).
- **Thousand Reflections:** Nyx fica Intangível (06) por 4 s; reflexos atravessam o alvo marcado (ou a área do cursor). Pode ser cancelado para terminar com o golpe final antecipadamente.

### 6.5 Augments

Builds: **S** = Storm Reaper, **P** = Phantom Assassin, **E** = Echo Architect.

| Habilidade | S | P | E |
|---|---|---|---|
| Phantom Shot | **Thunderbolt:** o disparo gera Chain Lightning para 3 alvos (50%) | **Killshot:** +150% dano contra alvos abaixo de 30%; abates recarregam uma carga de Shadowstep | **Doubled Shot:** um eco repete o disparo após 0,3 s com 60% do dano |
| Shadowstep | **Static Dash:** deixa um Static Field (07) no trajeto por 3 s | **Assassinate:** aplica Execute de 20% (Elites 10%; R08); se não executar, o primeiro acerto causa +100% (Increased) | **Mirror Step:** deixa um clone na origem por 2 s que copia a próxima habilidade |
| Storm Trap | **Overcharge Trap:** armadilhas próximas se conectam por arcos elétricos; acúmulo de Shock gera Overload | **Snare Trap:** Root de 2 s; alvos presos são marcados imediatamente | **Echo Trap:** a armadilha dispara duas vezes (a 2ª com 70%) |
| Echo Clone | **Storm Double:** o clone dispara Phantom Shot automaticamente a cada 1 s | **Shadow Decoy:** o clone aplica Taunt; Nyx ganha Stealth por 2 s e o primeiro acerto saindo de Stealth é crítico garantido | **Perfect Mirror:** o clone replica a 70%; até 2 clones ativos |
| Thousand Reflections | **Tempest:** todo acerto aplica Shock; golpe final gera Chain Lightning em todos os alvos da área | **Death Blossom:** ao terminar, aplica Execute de 20% em todos os alvos da área (Elites 10%; chefes/Guardians recebem a conversão R08) | **Infinite Mirror:** cada clone ou armadilha ativa adiciona +3 reflexos |

### 6.6 Armas

| Arma | APS | Moveset Basic | Moveset Heavy | Bônus de kit |
|---|---:|---|---|---|
| Dual Daggers | 1,8 | 4 golpes rápidos, 2 m | Estocada com avanço; +50% pelas costas | Shadowstep +1 carga |
| Longbow | 1,1 | Flechas de 22 m que perfuram 1 | Disparo carregado (até 1 s) que perfura todos | Phantom Shot +30% dano e +5 m de alcance |
| Crossbow | 1,0 (rajada de 3) | Rajada de 3 virotes, 16 m | Virote explosivo (raio 2,5 m) | Storm Trap arremessada a 16 m; +1 armadilha máxima |
| Twin Blades | 1,4 | Cortes giratórios 360°, 2,5 m | Investida giratória de 5 m | Echo Clone replica ataques básicos a 40% (em vez de 25%) |
| Chakrams | 1,2 | Discos de 12 m que retornam e ricocheteiam em 2 inimigos | Discos orbitam Nyx por 3 s (raio 3 m) | Todo efeito de Chain da Nyx +1 alvo |

### 6.7 Builds principais

**Storm Reaper** (S)
- **Loop:** velocidade constante, Static Dash + Overcharge Trap criando campos elétricos, Thunderbolt encadeando. Chakrams ou Crossbow.
- **Prioridades:** Storm Damage, Movement Speed, Attack Speed, Area Damage, Resource Generation.
- **Keystones:** Lightning Veins + Overload Circuit.
- **Rota A:** Legendaries de Chain e velocidade.
- **Rota B:** Storm Phantom 6.
- **Rota C:** Storm Phantom 3 + Thousand Echoes 3 (Storm + Echo = repetição de descargas).

**Phantom Assassin** (P)
- **Loop:** marcar, entrar com Shadowstep (Assassinate), finalizar com Killshot, recuperar Shadowstep no abate. Shadow Decoy para reposicionar. Dual Daggers ou Longbow.
- **Prioridades:** Critical Chance, Critical Damage, Physical Damage, Dexterity, Cooldown Reduction.
- **Keystones:** Executioner's Creed + Shadow Doctrine.
- **Rota A:** Legendaries de crítico e execução.
- **Rota B:** Death's Pursuit 6.
- **Rota C:** Death's Pursuit 3 + Storm Phantom 3.

**Echo Architect** (E)
- **Loop:** preparar o campo com Echo Traps e Perfect Mirror, então cada habilidade vira 3–4 instâncias. Twin Blades.
- **Prioridades:** Echo Damage, Summon Damage, Summon Duration, Resource Cost Reduction, Skill Damage.
- **Keystones:** Echo Chamber + Hollow Self.
- **Rota A:** Legendaries de clone e repetição + modificadores da categoria Echo (03).
- **Rota B:** Thousand Echoes 6.
- **Rota C:** Thousand Echoes 3 + Death's Pursuit 3.

---

## 7. Orun — The Voidkeeper

**Fantasia:** um guardião que arquiteta o campo de batalha com construções vivas de Void. Constrói, sacrifica e reconstrói. Recompensa planejamento, controle de zona e gestão de "peças no tabuleiro".

### 7.1 Recurso — Void Constructs (base 3, teto base 6, até 8 só via §11.1) + Void Mass

Orun tem duas camadas de recurso:

- **Void Mass (barra 0–100, oculta da build mas visível no HUD):** ao chegar a 100, cria automaticamente um **Lesser Fragment** (se houver espaço) e volta a 0.
- **Void Constructs:** contagem de construções ativas (Lesser Fragments e Sentinel). São o recurso gasto por habilidades que **sacrificam**.

| Regra | Valor |
|---|---|
| Void Mass por ataque básico | +6 por acerto |
| Void Mass por acerto de habilidade | +4 por inimigo (máx. +12 por uso) |
| Void Mass por dano recebido | +1 por 1% de vida máxima perdida |
| Lesser Fragment | Pulsa 60% DA de Void a cada 1,5 s em raio de 3 m; vida = 25% da de Orun; dura até ser destruído ou sacrificado |
| Gasto | Devouring Rift (opcional, segurar a tecla), The Unbound (todas) |
| Decaimento | Construções duram indefinidamente na sala; ao sair da sala, todas se desfazem e retornam 25 Void Mass cada (máx. 100) |
| Início de sala | 0 construções; Void Mass mantida |

### 7.2 Passivas inatas

- **Voidkeeper:** cada construção ativa concede +3% Damage Reduction a Orun (máx. +18%).
- **Ruinous Bond:** ataques das construções aplicam 1 acúmulo de Decay (07).

### 7.3 Ataques e esquiva (com Ritual Staff)

| Ação | Comportamento | Dano |
|---|---|---|
| Basic | 3 projéteis de Void, 12 m. APS 1,0 | 110% / 110% / 140% |
| Heavy | Feixe de dreno canalizado de 10 m (até 2 s); cura 1% da vida máxima por tick | 40% por tick (0,25 s) |
| Esquiva | Universal (§2, decisão 3) | — |

### 7.4 Habilidades

| Slot | Habilidade | Custo | CD | Alcance / Área | Dano | Tags |
|---|---|---:|---:|---|---|---|
| Q | Void Tendrils | — | — (cast 0,5 s) | Área até 10 m; 3 tentáculos em raio 3 m | 140% DA + Slow 30% por 2 s | Spell, Area, Void |
| E | Guardian Fragment | — | 10 s | Invoca 1 Sentinel (ocupa 1 construção) | Slam 150% DA a cada 2 s | Summon, Construct, Defensive, Void |
| R | Devouring Rift | — (opcional: sacrifica 1 construção) | 12 s | Fenda raio 3,5 m por 4 s, até 12 m | 80% DA a cada 0,5 s + Pull | Spell, Area, DoT, Void |
| Shift | Corruption Wave | — | 8 s | Onda em cone de 10 m | 200% DA + Decay + Knockback | Spell, Area, Ruin |
| F | The Unbound | sacrifica todas as construções | 100 s | Transformação 10 s | Açoites automáticos de 120% DA a cada 0,5 s; explosão final 500% DA | Spell, Area, Ultimate, Void, Ruin |

**Comportamentos:**
- **Void Tendrils:** gerador principal de Void Mass e controle leve.
- **Guardian Fragment:** invoca o **Sentinel** (nome da construção; "Guardian" é reservado a mini-chefes, 00 R48), construção de 30 s com 60% da vida de Orun; aplica Taunt a cada 5 s em raio de 6 m. Se o limite de construções estiver cheio, substitui o Lesser Fragment mais antigo.
- **Devouring Rift:** segurar a tecla ao conjurar sacrifica 1 construção: duração ×2 e raio +50%.
- **Corruption Wave:** construções atingidas pela onda emitem uma onda própria com 50% do dano na mesma direção.
- **The Unbound:** cada construção sacrificada adiciona +2 s de duração e +10% Increased Damage durante a forma. Orun fica Unstoppable (06) durante a transformação. Usável com 0 construções (forma base de 10 s).

### 7.5 Augments

Builds: **C** = Void Commander, **M** = Corruption Master, **G** = Abyssal Guardian.

| Habilidade | C | M | G |
|---|---|---|---|
| Void Tendrils | **Spawning Tendrils:** 15% de chance por acerto de criar um Tendril temporário (6 s, não ocupa limite) | **Rotting Tendrils:** converte em Ruin; cada acerto aplica 2 Decay | **Binding Tendrils:** Root de 1,5 s em vez de Slow; +5% Armor por inimigo preso (máx. 25%) |
| Guardian Fragment | **Legion Core:** ao invocar, cria também 2 Lesser Fragments; CD −3 s | **Plague Idol:** o Sentinel emite aura de Ruin (Decay a cada 1 s em 5 m) | **Aegis Monolith:** o Sentinel se liga a Orun e absorve 30% do dano que Orun recebe |
| Devouring Rift | **Hungry Maw:** cada inimigo morto na fenda gera +25 Void Mass | **Blight Rift:** converte em Ruin; ao terminar, causa Detonation (07) dos acúmulos de Decay | **Gravity Well:** Pull +50%; inimigos dentro causam 20% menos dano |
| Corruption Wave | **Commanding Wave:** construções atingidas ganham +30% velocidade de ataque e Increased Damage por 5 s | **Plague Tide:** aplica 3 Decay e Vulnerability 10% por 4 s | **Bulwark Wave:** concede escudo de 5% da vida máxima por inimigo atingido (máx. 30%) |
| The Unbound | **Abyssal Host:** não sacrifica as construções; elas também se tornam Unbound durante a forma (+60% Increased Damage e +50% vida; Augment não dá More, 00 R03) | **Herald of Ruin:** aura que aplica Decay a cada 1 s em 7 m; ao fim, Detonation em todos os alvos afetados | **Titan of the Void:** +50% Damage Reduction; cura 2% da vida máxima por segundo |

### 7.6 Armas

| Arma | APS | Moveset Basic | Moveset Heavy | Bônus de kit |
|---|---:|---|---|---|
| Ritual Staff | 1,0 | 3 projéteis de Void, 12 m | Feixe de dreno canalizado | Construções +20% vida e duração |
| Void Hammer | 0,8 | 3 golpes melee pesados, 2,8 m | Gravity Slam: puxa inimigos em raio de 5 m e golpeia | Void Mass por ataque básico +50% |
| Great Mace | 1,1 | 4 golpes melee médios, 2,4 m | Golpe de escudo de Void: bloqueia frontalmente por 0,5 s e empurra | Sentinel +30% vida; Orun +15% Armor |
| Totem | 0,9 | Pulsos de Void em cone curto de 6 m | Planta o Totem por 6 s (funciona como Lesser Fragment, não ocupa limite; CD 8 s) | Limite de construções +1 (exceção de teto, §11.1) |
| Ancient Scepter | 1,2 | Açoite de Ruin que encadeia em 2 alvos, 9 m | Projétil de maldição: aplica 3 Decay em área de 2,5 m | Corruption Wave +3 m de alcance; Decay aplicado +1 acúmulo |

### 7.7 Builds principais

**Void Commander** (C)
- **Loop:** encher o limite de construções rapidamente (Legion Core, Totem), comandar com Commanding Wave, transformar o exército com Abyssal Host.
- **Prioridades:** Summon Damage, Summon Duration, Resource Generation, Maximum Resource, Void Damage.
- **Keystones:** Legion of the Deep + Commander's Will.
- **Rota A:** Legendaries de construções.
- **Rota B:** Abyssal Legion 6.
- **Rota C:** Abyssal Legion 3 + Eternal Warden 3.

**Corruption Master** (M)
- **Loop:** empilhar Decay com Rotting Tendrils, Plague Idol e Ancient Scepter; Plague Tide para Vulnerability; detonar com Blight Rift e Herald of Ruin.
- **Prioridades:** Ruin Damage, Damage Over Time, Duration of Effects, Area Damage, Cast Speed.
- **Keystones:** Plaguebearer + Entropy.
- **Rota A:** Legendaries de Decay/Detonation.
- **Rota B:** Hollow Sovereign 6.
- **Rota C:** Hollow Sovereign 3 + Abyssal Legion 3.

**Abyssal Guardian** (G)
- **Loop:** prender com Binding Tendrils e Gravity Well, absorver dano com Aegis Monolith, escudos com Bulwark Wave; Titan of the Void para fases difíceis. Great Mace.
- **Prioridades:** Armor, Maximum Health, Shield Strength, Damage Reduction, Status Resistance.
- **Keystones:** Living Bastion + Unmoving.
- **Rota A:** Legendaries defensivos e de controle.
- **Rota B:** Eternal Warden 6.
- **Rota C:** Eternal Warden 3 + Hollow Sovereign 3.

---

## 8. Aether Lattice — árvore de talentos

### 8.1 Estrutura (idêntica para os 4 heróis)

```
                       [Keystone] [Keystone]
                              ▲
     Ramo A (build 1)    Ramo B (build 2)    Ramo C (build 3)
          │                   │                   │
        Tier 3 (gates em §8.2)
        Tier 2
        Tier 1
          └──────────── Núcleo (Core) ────────────┘
```

| Componente | Quantidade por herói | Custo | Efeito |
|---|---:|---|---|
| Núcleo | 8 nós menores | 1 ponto cada | Atributos genéricos do herói (vida, recurso, atributo principal) |
| Nós menores | 10 por ramo (30) | 1 ponto cada | +% pequenos alinhados ao tema do ramo (ex.: +4% Increased Ember Damage) |
| Notáveis | 5 por ramo (15) | 1 ponto por rank, 3 ranks | Efeitos médios condicionais |
| Augments | 5 por ramo (15) | 2 pontos cada | Um Augment por habilidade por ramo (§4.5, §5.5, §6.5, §7.5). Exclusivos: ativar um Augment de uma habilidade bloqueia os outros dois dela |
| Keystones | 2 por ramo (6) | 3 pontos cada | Mudam regras. Máximo de 2 ativos |
| **Custo total** | — | **~131 pontos** | — |

### 8.2 Pontos e gates

- **49 pontos** por nível (1 por nível, do 2 ao 50).
- **6 pontos de marcos de herói:** primeira derrota de cada um dos 5 chefes principais com aquele herói (+1 cada) e primeira conclusão da dificuldade Eclipse com ele (+1).
- **Total: 55 pontos.**
- **Gates:** Tier 1 livre; Tier 2 exige 6 pontos no ramo; Tier 3 exige 14 pontos no ramo. Augments ficam em Tier 1 (2 deles), Tier 2 (2) e Tier 3 (1, sempre o da Ultimate). Keystones exigem 20 pontos no ramo **ou** 30 pontos totais distribuídos entre dois ramos (permite builds híbridas).
- **Desbloqueio de Keystone por nível:** 1º slot no nível 20, 2º slot no nível 35.
- **Respec:** gratuito e instantâneo no Sanctuary; indisponível durante a run.

### 8.3 Notáveis e Keystones por herói

**Kael**

| Ramo | Notáveis (por rank) | Keystones |
|---|---|---|
| Inferno | Kindling (+5% Fury gerada); Pyroclasm (Burning tem 5% de chance de Explosion); Searing Momentum (+3% Attack Speed com ≥50 Fury); Heatstroke (+6% dano contra Burning); Ash Skin (+4% Elemental Resistance com ≥50 Fury) | **Combustion Rage:** cada 10 Fury gastos desencadeiam uma Explosion (50% DA, Ember) ao redor de Kael. **Ember Soul:** 50% do dano Physical é convertido em Ember; Burning pode causar crítico |
| Ruin | Butcher's Eye (+3% Critical Chance contra alvos abaixo de 50%); Bleeding Edge (+5% Ruin Damage); Grim Harvest (abates curam 1% da vida); Sunder (+4% Armor Penetration); Death Sentence (+1 ponto percentual nos limiares de execução, respeitando o teto de 20%) | **Headsman:** acertos aplicam Execute de 15% (Elites 7,5%; chefes/Guardians: conversão R08). **Doomsayer:** acertos aplicam 1 acúmulo de Vulnerability (4%, máx. 5); Kael causa 20% menos dano a alvos com vida cheia |
| Juggernaut | Thick Hide (+5% Armor); Defiant Roar (Ashen Guard CD −1 s); Stonewall (+3% Block Chance); Pain Engine (+5% Fury por dano recebido); Last Stand (Ashbound Resolve ativa a 45%) | **Scarred Titan:** Fury por dano recebido ×3; +20% vida máxima; −10% Movement Speed. **Iron Bastion:** 1% do Armor total vira +1% Increased Damage (máx. +40%); Kael não pode ser Knockback |

**Lyra**

| Ramo | Notáveis | Keystones |
|---|---|---|
| Permafrost | Bitter Cold (+5% Chill aplicado); Glacier Heart (+4% Frost Damage); Crystalline (Frozen +0,2 s); Cold Snap (Frost Lance +4% Critical Chance); Ice Ward (+5% Shield Strength após Freeze) | **Heart of Winter:** Chill nunca decai em inimigos a até 8 m de Lyra; inimigos Frozen que morrem causam Shatter em área (3 m). **Glacial Body:** Lyra ganha **Rime Shell** permanente: +25% Armor, mas −10% Movement Speed (não é o status Frozen Armor de 07; nome reservado, 00 R48) |
| Void | Gravity (+10% força de Pull); Abyssal Echo (+4% Void Damage); Event Lens (+5% raio de áreas Void); Long Night (+6% duração de zonas); Implosive (+5% dano de implosões) | **Singularity Mind:** todo Pull de Lyra puxa para a zona Void ativa mais próxima. **Eventide:** zonas Void duram +50%, mas apenas 2 zonas Void podem existir ao mesmo tempo |
| Arcane | Flux (+5% geração de carga); Arcane Edge (+3% Critical Damage por carga mantida); Resonant Mind (+4% Skill Damage); Surge (Astral Shift CD −0,5 s); Capacitor (cargas decaem 50% mais devagar) | **Overcharge:** +2 cargas máximas (pode exceder o teto base 6, até 8), mas cargas decaem em combate (−1 a cada 3 s). **Charge Engine:** CDs de Void Orb, Shattered Reality e Astral Shift −50%; cada uso exige ≥1 carga |

**Nyx**

| Ramo | Notáveis | Keystones |
|---|---|---|
| Storm | Livewire (+4% Storm Damage); Quickening (+2% Movement Speed); Arc Flash (+5% Shock aplicado); Conductor (Chain +1 alvo no rank 3); Surge Runner (+5 Echo Energy por Shadowstep) | **Lightning Veins:** cada 1% de Movement Speed acima da base vira +1% Increased Storm Damage. **Overload Circuit:** Shock pode exceder o máximo de acúmulos de 07 em +2 (exceção declarada em 07, 00 R49); ao atingir o novo máximo, causa Overload (07) e reinicia |
| Phantom | Killer Instinct (+3% Critical Chance); Lethality (+6% Critical Damage); Ghost (Stealth +0,3 s); Opportunist (+8% dano em alvo marcado); Swift End (abates +10 Echo Energy) | **Executioner's Creed:** críticos contra alvos abaixo de 50% recebem +40% Critical Damage; não-críticos causam 20% menos dano. **Shadow Doctrine:** após 3 s sem receber dano, Nyx entra em Stealth; o primeiro ataque saindo de Stealth é crítico garantido |
| Echo | Resonance Trace (+4% Echo Damage); Lingering (+1 s duração do clone); Fractal (+5% dano de clones); Recursion (Echo Trap +5% dano no 2º disparo); Mirror Shell (clones +10% vida) | **Echo Chamber (Echo Recursion 1×):** toda réplica (`IsEcho`) de Nyx pode se replicar **uma única vez** a 50% do dano, ainda sujeita a Generation < 2 (00 R07); a réplica da réplica nunca replica. **Hollow Self:** Nyx causa 30% menos dano (Less); clones causam +20% More e o limite de clones aumenta em +1 |

**Orun**

| Ramo | Notáveis | Keystones |
|---|---|---|
| Commander | Architect (+5% Void Mass); Hardened Forms (+6% vida de construções); Swift Builders (Guardian Fragment CD −0,5 s); Overseer (+4% Summon Damage); Rally (construções +3% velocidade de ataque) | **Legion of the Deep:** +2 construções máximas (pode exceder o teto base 6, até 8); cada construção causa 25% menos dano. **Commander's Will:** Void Tendrils e Corruption Wave são conjuradas também por cada construção ativa a 30% |
| Corruption | Blight (+5% Ruin Damage); Festering (+0,5 s duração de Decay); Withering Touch (+4% Damage Over Time); Spread (Decay se espalha para 1 alvo a 3 m ao morrer, rank 3); Fragility (+3% Vulnerability aplicada, sujeita ao teto de 60%) | **Plaguebearer:** inimigos com Decay que morrem transferem todos os acúmulos para até 3 inimigos próximos. **Entropy:** Decay não expira em alvos a até 6 m de uma construção |
| Guardian | Bulwark (+5% Armor); Rooted Will (+5% Status Resistance); Shared Pain (Aegis Monolith +3%); Bastion Pulse (escudos +5%); Steadfast (+4% vida máxima) | **Living Bastion:** 30% do dano recebido por Orun é redirecionado igualmente para suas construções. **Unmoving:** ficar parado por 1 s concede +30% Damage Reduction até se mover |

---

## 9. Progressão de herói 1–50

### 9.1 Ganhos por nível

| Nível | Desbloqueio |
|---|---|
| 1 | Basic, Heavy, esquiva, Slot Q, Slot Shift. Arma inicial Common do tipo de referência |
| 2 | 1º ponto de talento (Núcleo disponível) |
| 3 | Slot E |
| 5 | Slot R |
| 8 | Ultimate (F) |
| 10 | Augments disponíveis (Tier 1 dos ramos) |
| 20 | 1º slot de Keystone |
| 35 | 2º slot de Keystone |
| 50 | Nível máximo. Progressão adicional é responsabilidade de 10-progressao (Ascension) |

Todo nível concede +1 ponto de talento e crescimento de vida/armadura-base (fórmula em 01). Os 12 slots de equipamento, incluindo Off Hand e os 2 Relics, ficam ativos desde o nível 1 (00 R29).

### 9.2 Curva de XP (dona: 10-progressao)

- `XP_para_próximo(L) = 200 × L^1,8` (L = nível atual), conforme 10 (00 R40).
- Meta de tempo: nível 50 do primeiro herói em **~40–50 h** de jogo; ritmo por dificuldade e tempos intermediários em 10.
- **Kindred Echo (catch-up, valores de 10):** heróis abaixo de (nível do herói mais alto − 10) recebem +100% de XP.
- XP é ganha durante a run e **concedida mesmo em caso de morte** (a regra de morte para loot e recursos é de 10-progressao).

### 9.3 Desbloqueio dos heróis

| Herói | Condição |
|---|---|
| Kael | Disponível desde o início |
| Lyra | Disponível desde o início |
| Nyx | Primeira derrota do The Ashen Colossus (qualquer herói) → missão curta no Keeper |
| Orun | Primeira derrota do The Pale Sovereign (qualquer herói) → missão curta no Keeper |

Um herói desbloqueado começa no nível 1, mas recebe Kindred Echo e acesso ao Vault compartilhado (itens compatíveis com ele).

---

## 10. Dependências e pontos de integração

### 10.1 Este documento fornece

| Para | O quê |
|---|---|
| 01-atributos | Atributo principal de cada herói, vida/armadura-base relativas, tradução dos atributos genéricos de recurso (§3.2) a ser convertida em números |
| 02-equipamentos | Lista de 20 tipos de arma com APS, moveset e bônus; armas só em Main Hand e Off Hand não-arma por herói com os nomes de 02 (Warbrand, Grimoire, Quiver, Idol). O número de DPS é de 02 |
| 03-modificadores | As 20 habilidades com tags, números e comportamentos; Augments ocupam uma camada própria (1 por habilidade) e **não** contam como modificadores de equipamento; modificadores de 03 devem ser compatíveis com qualquer Augment ativo ou declarar exclusão |
| 04-conjuntos | Loops das 12 builds e combinações de Rota B/C sugeridas para cada set |
| 05-loot | Builds e prioridades de atributo por herói (para smart loot) |
| 06-combate | Movesets das 20 armas (06 ajusta a frame data a eles, 00 R18); usos de Parry, Unstoppable, Taunt, Stealth, Intangível, Execute, Root e Knockback (estados definidos por 06, 00 R20) |
| 07-resonance | Uso de Burning, Chill, Freeze, Shatter, Shock, Chain Lightning, Overload, Static Field, Pull, Implosion, Decay, Vulnerability, Detonation, Explosion, Repeat. Pede a 07 as exceções declaradas de R49: Freeze de 3 s em Absolute Zero e +2 acúmulos de Shock em Overload Circuit |
| 09-inimigos | Faixas de engajamento dos heróis para desenhar inimigos ranged/melee; cada herói tem resposta a stealth (Hunter's Mark/Static Field/Void Tendrils/Infernal Leap em área) e a escudo frontal (Pull, Leap, Shadowstep pelas costas) |
| 10-progressao | 6 pontos de marcos, condições de desbloqueio de Nyx e Orun, respec gratuito. Consome a curva de XP e o catch-up de 10 |
| 11-arquitetura | Estrutura da Aether Lattice como dados (nós, ranks, gates, exclusividade de Augments, slots de Keystone); recursos com regras distintas exigem um componente de recurso polimórfico por herói |

### 10.2 Este documento consome

- Pipeline de dano e buckets (contrato §3.2) e resoluções de integração (00 §5).
- Fórmulas de atributos, escala de vida/armadura, Movement Speed e tradução numérica de recurso (01).
- Definições de status de afinidade e teto de Vulnerability (07); estados genéricos, Execute, Parry, esquiva, input e frame data (06).
- DPS das armas e nomes de Off Hand (02); curva de XP e catch-up (10).
- Set Powers (04) — as Rotas B/C citadas assumem os temas do GDD §6.7.

### 10.3 Fontes "More" introduzidas

Regra (00 R03): nenhum Augment dá More; Keystone ≤ +20% More sustentado; Ultimate ≤ +30% More durante sua duração. Penalidades (Less) não têm teto.

| Fonte | Tipo | Valor | Condição |
|---|---|---:|---|
| Todos os Augments (incl. Abyssal Host) | Increased / comportamento | — | Abyssal Host: +60% Increased Damage e +50% vida nas construções durante The Unbound |
| Iron Bastion (Kael) | Increased | até +40% | — |
| Hollow Self (Nyx, Keystone) | More para clones / Less para Nyx | +20% / −30% | Sustentado |
| Executioner's Creed (Nyx, Keystone) | Critical Damage / Less em não-crit | +40% / −20% | Condicional |
| The Unbound (Orun, Ultimate) | Increased | +10% por construção sacrificada | Durante a forma |
| Eternal Winter (Lyra, Augment) | Increased | +20% Frost | 4 s após o Freeze |

Nenhuma fonte More nova além de Hollow Self (+20%). Todas as demais bonificações deste documento são Increased, conversões ou comportamento.

### 10.4 Estados genéricos usados

`Unstoppable`, `Taunt`, `Stealth`, `Execute`, `Parry`, `Intangível` e `Slow` genérico são definidos por 06 (00 R20). `Execute`, `Unblockable` e afins são HitFlags (00 R02), não tags.

---

## 11. Conflitos com as convenções

Nenhum conflito com `00-convencoes.md` após a rodada de integração (00 §5).

### 11.1 Exceções ao teto de recurso (00 R01)

Únicas fontes que elevam o teto base 6 de Arcane Charges / Void Constructs. O teto absoluto com qualquer combinação é **8**.

| Fonte | Herói | Tipo | Efeito no teto | Condição |
|---|---|---|---:|---|
| Overcharge | Lyra | Keystone | +2 | Permanente; cargas decaem em combate |
| Overflow | Lyra | Augment (Absolute Zero) | +2 | 10 s |
| Legion of the Deep | Orun | Keystone | +2 | Permanente; −25% dano por construção |
| Totem | Orun | Bônus de arma | +1 | Enquanto equipado |
| Itens/sets que concedem +1 Charge/Construct (04) | Lyra/Orun | Set Power | +1 | Conta no teto absoluto 8 |
| Band of the Paradox (02 L12) | Lyra | Legendary (Ring) | +1 | Enquanto equipado |
| Scepter of the Unbound King (02 O5) | Orun | Unique (Main Hand) | +1 | Enquanto equipado; não acumula com Totem (ambos Main Hand) |

### 11.2 Renomeações por colisão de nomes (00 R48)

| Antes | Depois | Motivo |
|---|---|---|
| Event Horizon (Augment de Void Orb) | Stillpoint | Nome reservado a 07 |
| Unstable Orb (Augment de Void Orb) | Overcharged Orb | Nome reservado a 03 |
| Brittle (notável de Orun) | Fragility | Nome reservado a 07 |
| Frozen Armor (Glacial Body) | Rime Shell | Status reservado a 07 |
| Guardian (construção de Orun) | Sentinel | "Guardian" é o termo único de mini-chefe |
| Anchor Chains / Anchor (notável) | Tethering Chains / Rooted Will | "Anchor" é serviço da Rest Room (10) |
| Warbanner / Tome / Quiver-Talisman | Warbrand / Grimoire / Quiver | Nomes de Off Hand de 02 (R29) |

Nomes de skill e build vindos do GDD v0.2 (Guardian Fragment, Abyssal Guardian) foram mantidos.

### 11.3 Outras mudanças desta rodada

- Esquiva uniforme (R06): removidas a carga extra de Nyx e a esquiva curta do Orun.
- Execute (R08): Death Blossom, Assassinate, Doom of Ages, Grim Riposte, Hangman's Chains e Headsman seguem limiar ≤ 20%, Elites metade e conversão contra chefe.
- More (R03): Abyssal Host deixou de dar More; Hollow Self caiu de +100% para +20% More.
- Echo Chamber passou a ser "Echo Recursion 1×" (R07).
- Slots bloqueados por nível removidos (R29); curva de XP e catch-up passam a ser os de 10 (R40).

---

## 12. Questões abertas para playtest

1. **Decaimento de Fury (4 s / −8/s):** verificar se salas com pausas entre ondas zeram a Fury cedo demais.
2. **Phantom Shot sem CD com custo 15:** validar se o equilíbrio Echo Energy 10/s não torna o disparo spam ótimo para todas as builds de Nyx.
3. **Construções que se desfazem entre salas:** medir o tempo até Orun "ficar online" no início de cada sala; ajustar o retorno de Void Mass (25 por construção) se passar de ~5 s.
4. **Hollow Self (Nyx) a +20% More:** confirmar que a troca −30% Nyx / +20% clones + 1 clone extra ainda é atraente para Echo Architect.
5. **Charge Engine (Lyra):** checar se −50% CD com custo de carga não supera Arcane Overload sem o Keystone em todos os cenários.
6. **55 pontos vs ~131 de custo:** confirmar que builds híbridas (Rota C) conseguem os 2 Keystones desejados sem sacrificar o loop principal.

# PROJECT ECLIPSE — 09 · Inimigos, chefes, salas e encontros

> Documento de detalhamento da etapa 9 (GDD v0.2 §18). Respeita `00-convencoes.md`.
> Dono de: arquétipos de inimigos, bestiário, afixos de Elite (incluindo quantidade por dificuldade), Guardians (mini-chefes), chefes, tipos de sala, estrutura do mapa de nós da run, orçamento de ameaça, tokens de ataque, eventos, Shrines/catálogo de Blessings, Curses de run e conteúdo de inimigos do endgame (Nightmare Behaviors, Eclipse Phenomena, Boss Echoes, Eclipse Trial, The Sundering Heart, Overrift Herald).
> Revisado na rodada 1 de integração (`00-convencoes §5`).

---

## 1. Objetivo e escopo

Definir **quem o jogador enfrenta, como esses inimigos se comportam e como os encontros são montados** dentro de uma run de região. O foco é legibilidade (o jogador sempre entende por que levou dano), variedade (os mesmos ~8 inimigos por região geram encontros diferentes) e teste de builds (cada arquétipo pressiona uma fraqueza diferente: AoE, single target, mobilidade, defesa, controle).

Fora de escopo (referenciado): curvas absolutas de HP/dano por nível e Monster Level (06-combate + 10-progressao), linguagem de telegraph, wind-ups mínimos, letalidade e TTK (06-combate), tabelas de drop (05-loot), status de afinidade (07-resonance), regra de morte, moedas, serviços da Rest Room e escalada de dificuldade (10-progressao), implementação (11-arquitetura).

---

## 2. Decisões tomadas

1. **Sistema de Pontos de Ameaça (PA)** para montar todo encontro — cada inimigo tem custo; cada sala tem orçamento. *Encontros escalam de forma previsível e data-driven, sem salas desenhadas à mão.*
2. **Stats de inimigos são relativos a uma unidade "Standard" (HP 1.0 / Dano 1.0).** Os valores absolutos vêm do Monster Level (10-progressao) e da curva base de 06-combate. *Separa identidade do inimigo de balanceamento numérico.*
3. **Sistema de tokens de ataque (dono: este doc)**: só N inimigos atacam o jogador ao mesmo tempo (melee 3→5, ranged 2→4 conforme dificuldade; Elites não consomem token). 06 cita, 11 lê a tabela por dificuldade (R17). *Hordas sem dano injusto; densidade não vira spam de dano.*
4. **Telegraphs, wind-ups mínimos e letalidade seguem 06-combate §3.10/§3.13** (R14, R15). Este doc só escolhe formas e tempos de cada ataque, sempre ≥ os pisos de 06. Todos os tempos citados aqui são de Normal. *Uma única linguagem visual em todo o jogo.*
5. **Mapa de nós com 13 linhas + chefe (14 salas), em 3 Estratos (linhas 1–5 / 6–9 / 10–13)** (R33), com Guardian fixo na linha 9, **pelo menos um nó Rest garantido na linha 5** e Antecâmara (Rest) na linha 13. *Garante ~25–35 min e arco de tensão estável.*
6. **Cada região tem 8 inimigos-base (40 no total), 2 Guardians nomeados e 1 chefe.** *Cobre os 9 arquétipos em todas as regiões sem inflar produção.*
7. **18 afixos de Elite**, cada um com assinatura visual e um counterplay explícito; nenhum afixo é "só +HP/+dano". Quantidade de afixos cresce com a dificuldade (1 → 4).
8. **Chefes sem hard enrage (sem one-shot por tempo)**: soft enrage a partir de 2× a duração alvo. Duração alvo de chefe 3–4 min em Normal, até 5 min em Eclipse com a fase extra; Guardian 30–60 s; sala de combate 60–90 s (R16). *Builds lentas mas seguras continuam viáveis (GDD pilar 6), mas são punidas progressivamente.*
9. **Challenge Room nunca é obrigatória**: toda linha que contém Challenge também contém pelo menos um nó sem risco extra.
10. **Blessings são o crescimento de poder temporário da incursão (dono do catálogo: este doc, R35)**: ranks Lesser/Greater/Exalted (70/25/5), More ≤ 8% por Blessing e ≤ 20% no total, alvo de +20–30% de poder efetivo até o chefe. A escala dos inimigos é calibrada assumindo +25%. Ficam fora do orçamento permanente de `00-convencoes §4`.
11. **Recompensa de sala é visível na porta** (ícone do tipo de recompensa). *Decisões de caminho informadas, coerentes com o pilar risco × recompensa.*
12. **Inimigos de regiões anteriores retornam em regiões posteriores** como "ecos" (até 20% do orçamento a partir da região 3). *Coerente com a lore de realidades misturadas e amplia variedade sem custo de arte.*
13. **A moeda de uma run é Rift Shards** (R36). Eventos e mercadores dentro da run cobram em Rift Shards; Gold é permanente (10). *Uma moeda temporária clara, sem misturar com a economia do hub.*
14. **CC em chefe vira Stagger (06)**, com as exceções nomeadas Frostbound e Distortion de 07 (R10). **Broken** de chefe usa os valores de 06: 4 s, +30% Vulnerability dentro do teto de +60% (R11, R12). Nenhuma fonte More nova neste doc além das Blessings Exalted.
15. **Este doc projeta o conteúdo de inimigos do endgame** (§15, R43); 10 define desbloqueios, chaves e recompensas.

---

## 3. Fundamentos de IA e legibilidade

### 3.1 Arquitetura de comportamento (visão de design)

Todo inimigo é definido por dados:

- **Range preferido** (min/max em metros) e **comportamento de reposicionamento** (aproximar, manter distância, flanquear, recuar).
- **Lista de ações** com: cooldown, alcance, peso, condição (ex.: "jogador a < 4 m"), telegraph, custo de token.
- **Seletor por utilidade**: o inimigo pontua ações disponíveis (distância, ângulo, cooldown, aliados próximos, HP próprio) e escolhe a de maior utilidade com 15% de aleatoriedade ponderada. Cadência de decisão: 10 Hz para inimigos perto do jogador, 4 Hz para os distantes (LOD de IA de 11-arquitetura, R47). *Evita padrões robóticos sem perder previsibilidade.*
- **Classe de massa** (Light/Medium/Heavy/Colossal) e barra de Stagger: mecânica e limiares em 06-combate §3.8. A classe de cada arquétipo está na §4.

### 3.2 Tokens de ataque (dono: este doc — 06 cita; 11 carrega a tabela por dificuldade)

| Dificuldade | Tokens melee | Tokens ranged/caster | Intervalo mínimo entre ataques do mesmo inimigo |
|---|---:|---:|---:|
| Normal | 3 | 2 | 1,2 s |
| Veteran | 3 | 3 | 1,0 s |
| Nightmare | 4 | 3 | 0,9 s |
| Eclipse | 5 | 4 | 0,8 s |
| Ascension | +1 melee a A40, +1 ranged a A70 | | piso 0,7 s |

- Inimigo sem token **orbita** a 4–7 m do jogador, se reposiciona ou faz ataques de baixo custo (ex.: projétil fraco, buff).
- Elites e Guardians **não consomem token** (sempre podem atacar), mas seus ataques fortes obedecem ao intervalo mínimo. Chefes ignoram tokens.
- Assassinos usam um **token exclusivo de emboscada** (1 por encontro em Normal/Veteran, 2 em Nightmare+).

### 3.3 Linguagem de telegraph (aplicação da linguagem de 06-combate §3.10)

**Formas usadas por este doc:** Círculo, Cone, Linha (retângulo), Anel (donut — seguro dentro ou no centro).
O decalque de chão **preenche do centro para a borda** durante o wind-up; o impacto ocorre quando o preenchimento toca a borda (06).

| Código usado no bestiário | Visual (06) | Significado | Resposta esperada |
|---|---|---|---|
| **vermelho** | Decal vermelho padrão | Dano evitável padrão | Esquivar (i-frames), sair da área ou bloquear |
| **Unblockable** | Decal com borda branco-dourada | Ignora Block e posturas defensivas (Ashen Guard, Guardian Fragment, Parry) | Esquivar / sair |
| **roxo** | Decal roxo | Controle (Pull, Root, Freeze, Grab) | Sair, usar mobilidade ou i-frames |
| **ciano** | Flash ciano no inimigo | Janela de interrupção: dano de Stagger neste wind-up interrompe o ataque | Punir com Heavy/skill de Stagger |
| **Annihilation** | Decal vermelho com borda amarela tracejada | Mecânica letal de chefe; i-frames **não** protegem (R13) | Sair da área |

- Laranja é reservado ao VFX de Ember (jogador e inimigos) e **nunca** é cor de telegraph.
- `Unavoidable` e `Unblockable` são HitFlags (R02): `Unavoidable` ignora Dodge Chance e Block, mas i-frames evitam; só Annihilation exige sair da área.

Regras:

- **Wind-ups mínimos (06):** comum 0,6 s em Normal (piso absoluto 0,45 s); Elite 0,8 s (piso 0,55 s); golpe pesado de chefe 1,0–1,5 s (piso 0,8 s). Redução máxima por dificuldade: −15%, nunca abaixo do piso. Todos os tempos deste doc são de Normal.
- **Tempos de referência deste doc:** fodder 0,6 s · standard 0,6–0,8 s · heavy 1,0–1,2 s · Elite +0,2 s sobre a base (mín. 0,8 s) · chefe 1,0–2,0 s.
- **Ataques fora da tela** sempre têm indicador de borda de tela + cue sonoro direcional.
- **VFX de inimigo** usam paleta com saturação/brilho menores que VFX do jogador, exceto telegraphs. *Habilidades do jogador nunca escondem um telegraph inimigo* (decalques de telegraph renderizam acima dos VFX do jogador; opacidade de VFX do jogador conforme 06).

### 3.4 Letalidade

A tabela de dano por golpe (% da vida máxima com defesas recomendadas) é de **06-combate §3.13** (R15): golpe comum 4–6% em Normal até 8–12% em Eclipse; golpe de Elite 10–15% até 18–28%; golpe pesado de chefe 20–30% até 35–50%; nenhum ataque não-mecânico acima de 60% da vida. A única exceção são mecânicas **Annihilation** de chefe, letais, sempre telegrafadas por ≥ 1,5 s. Mortes devem vir de erros acumulados.

---

## 4. Os 9 arquétipos

Stats relativos à unidade Standard; valores absolutos vêm do Monster Level (10) e da curva base de 06. **PA** = custo no orçamento de ameaça. **Massa** = classe de 06 §3.8.1 (define flinch, knockback e limiar de Stagger). Velocidades em m/s; a referência é a velocidade base dos heróis em 01-atributos (Orun 5,8 m/s, o mais lento) (R50). Nenhum inimigo comum é mais rápido que o herói mais lento, exceto Assassins em investida.

| Arquétipo | HP× | Dano× | Velocidade | Massa (06) | PA | Função no encontro |
|---|---:|---:|---:|---|---:|---|
| Melee (fodder) | 0,3 | 0,6 | 5,0 | Light | 1 | Volume, testa AoE |
| Melee (standard) | 1,0 | 1,0 | 4,2 | Medium | 3 | Linha de frente, pressão constante |
| Ranged | 0,8 | 1,0 | 4,0 | Medium | 4 | Força movimento, pune ficar parado |
| Caster | 0,9 | 1,3 | 3,6 | Medium | 5 | Zonas e controle, alvo prioritário |
| Tank | 4,0 | 1,4 | 3,0 | Heavy | 8 | Bloqueia espaço, testa Stagger/penetração |
| Summoner | 1,6 | 0,6 | 3,4 | Medium | 6 | Cresce com o tempo, testa single target |
| Assassin | 0,8 | 1,6 | 5,5 (investida 9,0) | Light | 5 | Pune posicionamento descuidado |
| Support | 1,0 | 0,5 | 3,8 | Medium | 5 | Multiplica aliados, alvo prioritário |
| Elite | base × 6 | base × 1,3 | base × 1,1 | massa base, regra Elite de 06 | 15 + 4/afixo | Teste de build, recompensa |
| Guardian | ver §7 | | | Colossal | — | Teste intermediário da run |
| Boss | ver §8 | | | Colossal | — | Exame final da região |

### 4.1 Melee attackers
- **IA:** aproxima em linha reta com desvio leve para cercar; ao ganhar token, avança e executa combo. Fodder ataca em enxame com lunge curto.
- **Ataques:** combo 2–3 golpes (vermelho, cone 90°, 2 m, 0,6 s), golpe forte (vermelho ou Unblockable, linha 3 m, 0,8 s).
- **Counterplay:** esquiva lateral, knockback, AoE.
- **Regra:** nunca mais de 6 melee standard vivos por onda.

### 4.2 Ranged attackers
- **IA:** mantém 8–14 m; se o jogador entra a < 5 m, recua (cooldown 4 s) ou troca para ataque de empurrão. Procura linha de visão; não atira através de aliados Tank.
- **Ataques:** projétil reto (velocidade 14–18 m/s, visível), arco de artilharia (círculo vermelho no destino), rajada de 3.
- **Counterplay:** fechar distância, usar obstáculos. Projéteis inimigos **não** são destruídos por ataques comuns — só por Block ou efeitos específicos de reflexão/absorção (preserva o valor de Block e de modificadores de reflexão).

### 4.3 Casters
- **IA:** mantém 10–16 m; conjura zonas no jogador ou em pontos de previsão (posição + 0,5 s de velocidade). Teleporta curto (6 m) ao ser alcançado, cooldown 8 s.
- **Ataques:** zona (círculo/anel, 1,0–1,4 s, frequentemente com janela ciano), canalização longa interrompível, linha de feixe.
- **Counterplay:** interrupção (Stagger na janela ciano), pressão direta, ficar fora de zonas.

### 4.4 Tanks
- **IA:** avança lento até o jogador; prioriza bloquear o caminho entre o jogador e casters/supports aliados (comportamento "escolta").
- **Mecânicas:** proteção frontal (–50% dano de frente, visual de placa/escudo) **ou** armadura que só cai com Stagger. Golpe de área Unblockable com wind-up longo.
- **Counterplay:** flanquear, encher a barra de Stagger, Armor Penetration, ignorar e matar o que ele protege.

### 4.5 Summoners
- **IA:** foge para a borda da arena (≥ 12 m), invoca a cada 7–9 s até o teto (máx. 6 invocações vivas por summoner, 10 por sala). Invocações custam 0 PA, mas contam para o teto de unidades vivas.
- **Regra:** matar o summoner faz as invocações dele **colapsarem em 1,5 s** (não instantâneo, para não invalidar o dano em área já aplicado).
- **Counterplay:** mobilidade, dano a distância, priorização.

### 4.6 Assassins
- **IA:** entra em **Stealth (estado de 06) com shimmer visível** (nunca invisível total, exceto mecânica de região explícita), circula para as costas do jogador e ataca com o token de emboscada. Após atacar, recua e re-entra em stealth em 4 s.
- **Ataques:** dash-strike (linha vermelha 0,6 s + cue sonoro "sibilo" direcional), combo rápido.
- **Counterplay:** cue sonoro, virar e esquivar; assassinos têm pouco HP e massa Light — punir após o dash.

### 4.7 Support enemies
- **IA:** fica atrás de outros inimigos (6–10 m de um aliado, 10+ m do jogador); aplica buffs em aliados próximos com tether visível.
- **Buffs possíveis:** escudo, haste, cura, imunidade a Stagger, ressurreição limitada.
- **Regra:** buffs têm **visual inequívoco no alvo** (casca, aura, ícone sobre a cabeça) e são removidos se o tether quebrar por distância (> 12 m) ou com a morte do support.
- **Counterplay:** priorizar, quebrar tether com Pull/Knockback, separar o grupo.

### 4.8 Elite enemies
- Qualquer inimigo-base pode ser promovido a Elite (exceto fodder). Recebe multiplicadores da tabela, **escala visual 1,25×**, contorno dourado, nameplate com nome gerado (ex.: *Vharok, the Molten*) e afixos (§6).
- Elites vêm em **matilhas**: 1 Elite + 2–4 *Retainers* (versões normais da mesma facção, PA normal).
- Elites ganham uma ação extra de "assinatura de afixo" (ver afixos).

### 4.9 Bosses
Ver §8. Barra de vida no topo da tela, fases sinalizadas por transição (cinemática curta ≤ 3 s, pulável a partir da 2ª vez), padrões em ciclo com variação ponderada.

---

## 5. Bestiário por região

Formato: **Nome — arquétipo · facção · afinidade · HP× / Dano× · PA.** Ataque assinatura (telegraph). *Counterplay.*

Afinidade indica o tipo de dano e o status aplicado (status definidos em 07-resonance).

### 5.1 The Ashen Wastes — Ember / Physical · Hollow + Ascendants (*Cinder Choir*)
Região de introdução: ensina telegraphs, Stagger e priorização. Densidade alta, inimigos frágeis.

1. **Cinder Husk** — melee fodder · Hollow · Ember · 0,3 / 0,6 · 1. Enxame; lunge-mordida (círculo 1 m, 0,6 s). Ao morrer, 25% de chance de faísca (círculo 1,5 m, 0,6 s, vermelho). *AoE; não lutar parado sobre corpos.*
2. **Ashborn Marauder** — melee standard · Hollow · Physical · 1,0 / 1,0 · 3. Combo de 2 golpes + golpe overhead (linha 3 m, 0,7 s). *Esquiva lateral após o 2º golpe.*
3. **Slag Hurler** — ranged · Hollow · Ember · 0,8 / 1,0 · 4. Arremessa escória (círculo 2,5 m no destino, 1,0 s) que deixa poça Burning por 3 s. Recua se jogador < 5 m. *Fechar distância; não ficar em poças.*
4. **Choir Pyrecaller** — caster · Ascendants · Ember · 0,9 / 1,3 · 5. Coluna de fogo no jogador (círculo 3 m, 1,2 s, janela ciano) e Flame Wall (linha 8 m que persiste 4 s). *Interromper; contornar a parede.*
5. **Kiln Brute** — tank · Hollow · Physical/Ember · 4,0 / 1,4 · 8. Placa frontal (–50% dano frontal). Slam (círculo 4 m, 1,1 s, Unblockable). *Flanquear; punir após slam (2 s de recuperação).*
6. **Ember Matron** — summoner · Hollow · Ember · 1,6 / 0,6 · 6. Choca 3 Cinder Husks a cada 8 s (máx. 6). Foge. *Matar primeiro; Husks colapsam quando ela morre.*
7. **Cinder Stalker** — assassin · Ascendants · Ember · 0,8 / 1,6 · 5. Shimmer de calor; dash-strike pelas costas (linha 6 m, 0,6 s) que aplica Burning. *Ouvir o cue; punir na recuperação.*
8. **Choir Cantor** — support · Ascendants · Ember · 1,0 / 0,5 · 5. Concede *Pyre Shell* a até 3 aliados (casca laranja; absorve 30% do HP máximo do alvo por 8 s). *Matar ou separar; o shell se desfaz com a morte do Cantor.*

### 5.2 The Frozen Cathedral — Frost / Physical · Forgotten (*Pale Congregation*) + Ascendants (*Rimeseekers*)
Ensina controle: Slow, Freeze, posicionamento em arenas com colunas e corredores.

1. **Frostbitten Penitent** — melee fodder · Forgotten · Frost · 0,35 / 0,5 · 1. Arrasta-se em grupos; agarra (roxo, círculo 1 m, 0,6 s) aplicando Slow. *Não deixar cercar; knockback.*
2. **Cathedral Sentinel** — melee standard · Forgotten · Physical · 1,2 / 1,0 · 3. Escudo frontal que bloqueia projéteis; estocada de lança (linha 4 m, 0,7 s). *Atacar pelas costas ou encher a barra de Stagger.*
3. **Rime Archer** — ranged · Forgotten · Frost · 0,8 / 0,9 · 4. Rajada de 3 flechas em leque que aplica Chill (acúmulo para Freeze). *Movimento lateral; usar colunas.*
4. **Hymnal Wraith** — caster · Forgotten · Frost/Echo · 0,9 / 1,2 · 5. Canta onda em anel (donut 3–9 m, 1,3 s, roxo: Freeze) — seguro colado nele ou longe. *Ler a forma do anel.*
5. **Glacial Reliquary** — tank · construct · Frost · 4,5 / 1,2 · 8. Rola em linha reta (linha 12 m, 1,2 s, Unblockable) e ganha *Hoarfrost Carapace* (não acumula Stagger) até receber dano Ember ou um Heavy. *Esquivar a rolagem; Ember/Heavy quebra a armadura.*
6. **Bellringer** — summoner · Forgotten · Frost · 1,4 / 0,4 · 6. Toca sino a cada 9 s chamando 3 Penitents. O **sino é um objeto destrutível** separado (HP 1,0×). *Destruir o sino para cortar a invocação.*
7. **Shard Wraith** — assassin · Forgotten · Frost · 0,7 / 1,5 · 5. Desaparece em espelhos de gelo da arena e surge atrás do jogador (cue: espelho racha 0,6 s antes). *Destruir espelhos; virar ao som da rachadura.*
8. **Rimeseeker Adept** — support · Ascendants · Frost · 1,0 / 0,5 · 5. Aplica *Hoarfrost Carapace* a aliados (não acumulam Stagger, –30% dano recebido) por 6 s. *Hoarfrost Carapace* é um buff exclusivo de inimigos, distinto do status Frozen Armor de 07. *Ember ou Heavy remove; priorizar.*

### 5.3 The Shattered Expanse — Storm / Echo · Forgotten + Hollow + Ascendants
Realidade instável: plataformas fragmentadas, ataques que se repetem, teleporte. Ensina leitura de padrões atrasados.

1. **Shardling** — melee fodder · Hollow · Storm · 0,3 / 0,7 · 1. Corre e explode em Shock ao chegar a 1,5 m (círculo 2 m, 0,6 s). *Matar à distância ou esquivar; explosões ferem inimigos também.*
2. **Echo Duelist** — melee standard · Forgotten · Echo · 1,0 / 0,9 · 3. Todo combo é **repetido por um eco fantasma 0,8 s depois** no mesmo lugar (ver Repeat em 07). *Esquivar duas vezes ou sair do lugar.*
3. **Arc Lancer** — ranged · Ascendants · Storm · 0,8 / 1,1 · 4. Feixe carregado (linha 14 m, 1,0 s) que encadeia entre jogador e clones/invocações próximas. *Não alinhar; espalhar invocações.*
4. **Rift Seer** — caster · Ascendants · Echo/Void · 0,9 / 1,3 · 5. Coloca 3 glifos atrasados que detonam após 2,0 s (círculos 2,5 m). Teleporta ao ser alcançado. *Sair dos glifos; perseguir após teleporte (cooldown 8 s).*
5. **Stormglass Golem** — tank · construct · Storm · 4,0 / 1,3 · 8. Static Field passivo (raio 3 m, Shock leve). A cada 10 s acumula carga visível (arcos crescentes) e descarrega (círculo 6 m, 1,4 s, Unblockable). *Recuar na carga; punir após descarga.*
6. **Paradox Weaver** — summoner · Forgotten · Echo · 1,5 / 0,5 · 6. Transforma cadáveres de aliados em **Ecos** (30% HP, mesmos ataques, translúcidos) — máx. 4. *Matar primeiro; AoE em corpos não remove — matar o Weaver colapsa os Ecos.*
7. **Blink Reaver** — assassin · Hollow · Storm · 0,8 / 1,5 · 5. Teleporta em flash elétrico (cue: estalo + marca no chão 0,6 s) e faz 3 cortes rápidos (cone 2 m). *Esquivar ao estalo.*
8. **Tempo Warden** — support · Forgotten · Echo · 1,1 / 0,4 · 5. Aura de haste (+25% velocidade de ataque/movimento a aliados em 8 m). Uma vez por encontro, **rebobina** o HP de um aliado para o valor de 3 s atrás (ampulheta visível sobre o alvo 1,0 s antes). *Matar primeiro ou interromper (ciano) a rebobinagem.*

### 5.4 The Hollow Depths — Void / Ruin · Hollow
Escuridão, corrupção e puxões. Ensina gestão de espaço e de debuffs (Decay, Vulnerability).

1. **Gnawling** — melee fodder · Hollow · Ruin · 0,3 / 0,6 · 1. Se enterra e emerge sob o jogador (círculo 1,5 m, 0,6 s, vermelho). *Manter-se em movimento.*
2. **Hollow Thrall** — melee standard · Hollow · Ruin · 1,1 / 1,0 · 3. Golpes aplicam Decay. Ao morrer, deixa poça de Decay 2 s. *Não lutar em cima das poças.*
3. **Spitting Maw** — ranged (estático) · Hollow · Ruin · 1,2 / 1,0 · 4. Planta enraizada; cospe ácido em arco (círculo 2 m, 1,0 s) deixando poça Decay 4 s. Não se move. *Prioridade de rota; pode ser contornado.*
4. **Void Whisperer** — caster · Hollow · Void · 0,9 / 1,3 · 5. Orbe de Pull (roxo, puxa para um ponto por 1,5 s) seguido de Implosion no mesmo ponto (círculo 3 m, 1,0 s após o fim do Pull). *Usar mobilidade contra o Pull ou interromper.*
5. **Abyssal Carapace** — tank · Hollow · Void · 4,5 / 1,3 · 8. Absorve projéteis vindos da frente (fenda visível). Grab de tentáculo (linha 7 m, 0,9 s, roxo) que puxa o jogador até ele. *Atacar pelas laterais; esquivar o grab.*
6. **Brood Mother** — summoner · Hollow · Ruin · 1,8 / 0,6 · 6. Põe 2 ovos (HP 0,4× cada) a cada 8 s; eclodem em 5 s, cada um gera 2 Gnawlings. *Destruir ovos; ou matar a Mother.*
7. **Umbral Stalker** — assassin · Hollow · Void · 0,8 / 1,7 · 5. **Invisível dentro de zonas de escuridão** da arena, visível em zonas iluminadas (mecânica explícita da região). Fora da escuridão, apenas shimmer. *Lutar perto de fontes de luz; algumas habilidades de luz/Ember revelam.*
8. **Rot Priest** — support · Hollow · Ruin · 1,0 / 0,5 · 5. Consome cadáveres para curar aliados em 8 m (15% HP, tether verde-escuro) ou criar um Totem de Decay (HP 0,5×, aura de Decay 4 m). *Matar rápido; limpar totens.*

### 5.5 The Astral Citadel — todas as afinidades · Forgotten + Ascendants (*Pillar-born*)
Exame final: combina mecânicas das regiões anteriores e cicla afinidades. Usa até 20% do orçamento com ecos de inimigos anteriores.

1. **Astral Mote** — fodder voador · Forgotten · Storm/Echo · 0,3 / 0,6 · 1. Orbita em grupos de 3 e mergulha (linha 4 m, 0,6 s). *AoE; se desfazem rápido.*
2. **Citadel Templar** — melee standard · Ascendants · cíclica · 1,2 / 1,1 · 3. A arma brilha na cor da afinidade atual (troca a cada 6 s em ordem fixa Ember → Frost → Storm → Void → Echo → Ruin) e seus golpes aplicam o status dessa afinidade. *Ler a cor; evitar a afinidade que mais prejudica a build.*
3. **Starbinder Archer** — ranged · Ascendants · Storm · 0,8 / 1,1 · 4. Flecha que se divide em 3 após 6 m (cones visíveis). *Encostar ou ficar longe; nunca a 6–10 m.*
4. **Constellation Magus** — caster · Ascendants · Void/Echo · 1,0 / 1,4 · 5. Cria 4 estrelas no chão e, após 1,2 s, liga-as com feixes (linhas vermelhas entre estrelas). *Ler as linhas; destruir uma estrela quebra duas linhas.*
5. **Pillar Guardian** — tank · Forgotten · Physical · 5,0 / 1,4 · 9. **Imune a dano** enquanto ligado ao seu Nódulo de Pilar (cristal HP 0,8×, tether brilhante, a 6–10 m). Slam Unblockable. *Destruir o cristal primeiro.*
6. **Herald of the First** — summoner · Forgotten · cíclica · 1,6 / 0,6 · 7. Abre fendas que trazem inimigos das regiões 1–4 (PA 3–4 cada, máx. 2 por vez). *Prioridade alta; interromper (ciano) a abertura.*
7. **Null Executioner** — assassin · Forgotten · Void · 1,0 / 1,8 · 6. Aplica *Unwriting Mark* no jogador (ícone sobre a cabeça, contagem de 3 s); ao fim, teleporta e executa (círculo 2 m, Unblockable, 0,6 s). Quebrar linha de visão por 1 s ou acertá-lo com Stagger cancela. *Usar obstáculos ou interromper.*
8. **Choir of the Pillars** — support (trio flutuante, conta como 1 unidade) · Ascendants · Echo · 3 × 0,6 / 0,4 · 7. Escudam aliados; se um membro morrer e os outros não morrerem em 4 s, ele renasce. *AoE ou foco rápido nos três.*

### 5.6 Regras de distribuição regional
- Afinidade dominante da região responde por ≥ 60% do orçamento de PA em salas comuns.
- A partir da região 3, até 20% do orçamento pode vir de "ecos" (inimigos de regiões anteriores) com visual de distorção.
- Resistências de inimigos: cada inimigo resiste à afinidade da própria região em grau moderado (valores em 01-atributos). **Nenhum inimigo comum é imune a uma afinidade** — imunidade é reservada a mecânicas explícitas (ex.: Pillar Guardian) para não invalidar builds mono-afinidade.

---

## 6. Afixos de Elite

### 6.1 Catálogo

| # | Afixo | Afinidade | Assinatura visual | Comportamento | Counterplay | Dif. mínima |
|---:|---|---|---|---|---|---|
| 1 | **Molten** | Ember | Rachaduras laranja no corpo | Deixa rastro de chão em brasa (dura 4 s) ao se mover | Não perseguir no rastro; puxar para fora | Normal |
| 2 | **Glacial** | Frost | Crosta de gelo + névoa | A cada 8 s solta 3 orbes de gelo que explodem em Freeze após 1,5 s (círculos roxos) | Sair dos círculos | Normal |
| 3 | **Overcharged** | Storm | Arcos azul-brancos | A cada 4º ataque, lança Chain Lightning que salta entre jogador, clones e invocações | Espalhar; não amontoar invocações | Normal |
| 4 | **Gravitic** | Void | Halo escuro distorcendo o chão | Pulso de Pull a cada 7 s (anel roxo visível 1,0 s antes) | Mobilidade/i-frame no pulso | Veteran |
| 5 | **Reverberant** | Echo | Silhueta duplicada atrasada | Todo ataque é repetido por um eco 0,8 s depois | Esquivar duas vezes | Veteran |
| 6 | **Blighted** | Ruin | Veias verde-negras | Golpes aplicam Weakening; ao morrer deixa poça de Decay 5 s | Não finalizá-lo corpo a corpo sem mobilidade | Normal |
| 7 | **Bastion** | — | Escudo de energia frontal | Bloqueia 100% do dano frontal; vira lentamente (90°/s) | Flanquear, Pull, clones | Veteran |
| 8 | **Siphoning** | — | Tether vermelho ao acertar | Cura 5% HP por acerto no jogador; tether quebra a > 10 m | Não trocar golpes; jogar à distância | Normal |
| 9 | **Hasted** | — | Afterimages | +35% velocidade de movimento e ataque; –20% HP | Controle (Slow/Freeze), burst | Normal |
| 10 | **Phasing** | — | Contorno tremeluzente | Teleporta para perto do jogador se ele estiver a > 10 m (cooldown 5 s; marca no chão 0,6 s antes) | Esperar a marca e esquivar | Veteran |
| 11 | **Soulbound** | — | Correntes de luz entre Elites | Elites da matilha dividem o dano recebido entre si (requer 2+ Elites; senão liga aos Retainers) | Dano em área; quebrar distância (> 12 m quebra o vínculo) | Nightmare |
| 12 | **Commanding** | — | Estandarte/glifo sobre a cabeça | Retainers em 10 m ganham +25% dano e não podem ser Stunned | Matar o Elite primeiro ou isolar | Normal |
| 13 | **Immovable** | — | Armadura pesada, pegadas que racham o chão | Imune a Knockback, Pull e Stun; –25% velocidade | Usar dano, não controle; kiting | Veteran |
| 14 | **Vengeful** | — | Núcleo pulsante no peito | Ao morrer, nova de 6 m após 1,5 s (círculo vermelho) | Afastar-se após o kill | Normal |
| 15 | **Sentinel** | — | Mochila de cristais | Planta até 2 torretas (HP 0,5×) que disparam projéteis lentos | Destruir torretas ou matar rápido | Veteran |
| 16 | **Mortar** | — | Tubos/órbitas nas costas | Bombardeia 3–5 círculos vermelhos a cada 9 s em qualquer distância | Manter-se em movimento | Normal |
| 17 | **Prismatic** | — | Superfície espelhada que cintila | Alterna 3 s de *Reflect* (reflete projéteis, brilho intenso) / 5 s normal | Pausar projéteis no brilho; melee livre | Nightmare |
| 18 | **Carapaced** | — | Casco segmentado | –60% dano recebido até sua barra de Stagger encher pela 1ª vez; depois o casco cai | Dano de Stagger, Heavy | Nightmare |

### 6.2 Quantidade de afixos por dificuldade

| Dificuldade | Afixos por Elite | Matilhas Elite por Elite Arena |
|---|---|---|
| Normal | 1 | 1 |
| Veteran | 2 | 1 |
| Nightmare | 2 (25% de chance de 3) | 1 (30% de chance de 2) |
| Eclipse | 3 | 1–2 |
| Ascension 1–49 | 3 | 2 |
| Ascension 50+ | 3 (35% de chance de 4) | 2 |
| Ascension 80+ | 4 | 2 |

### 6.3 Regras de rolagem
- **Peso regional:** o afixo de afinidade da região tem peso ×3; afixos de afinidades opostas ao bioma (ex.: Molten no Frozen Cathedral) peso ×0,5.
- **Incompatibilidades:** Phasing × Immovable; Hasted × Immovable; Bastion × Prismatic; Soulbound exige 2+ Elites na sala; no máximo 1 afixo "de zona persistente" (Molten, Blighted, Mortar) por Elite.
- **Máximo de 1 afixo de afinidade** por Elite até Eclipse (até 2 em Ascension 50+).
- **Nome gerado:** `[Nome próprio da facção], the [Adjetivo do 1º afixo]` — ex.: *Ysmira, the Glacial*.
- **Legibilidade:** com 3+ afixos, ícones dos afixos aparecem sob o nameplate e no tooltip ao mirar.

---

## 7. Guardians (mini-chefes nomeados)

Toda run tem uma **Guardian Arena fixa na linha 9** (fim do Estrato II) com um dos 2 Guardians da região (50/50; o mesmo não se repete em runs consecutivas da mesma região). Guardians também podem aparecer em Challenge Rooms de Nightmare+.

**Stats:** HP ≈ 15× Standard, massa Colossal (CC vira Stagger, 06), imunes a Execute (R08), barra no topo da tela, 2 fases (troca a 50%), duração alvo 30–60 s (R16), sem afixos aleatórios em Normal/Veteran; a partir de Nightmare recebem 1 afixo aleatório compatível.
**Recompensa:** baú garantido com 1 item Epic+ e chance aumentada de Legendary (05-loot), + Aether Dust e Sundered Cores (quantidades em 10-progressao).

| Região | Guardian | Mecânica central |
|---|---|---|
| Ashen Wastes | **Cinderwright Vahl** (Ascendant) | Forja 3 armas flamejantes que orbitam e são lançadas em sequência (linhas vermelhas). Fase 2: as armas ficam cravadas no chão como pilares de fogo que restringem a arena. |
| Ashen Wastes | **The Kiln Hound** (Hollow) | Fera de escória: investidas em linha (Unblockable) que deixam rastro Burning. Fase 2: superaquece — atacá-lo enquanto brilha causa Burning no jogador; janela de punição após resfriar (3 s). |
| Frozen Cathedral | **Sister Mourne, the Bell-Keeper** | Toca 3 sinos (objetos). Cada sino ativo concede uma aura (Slow, escudo, invocação). Destruir sinos enfraquece-a. Fase 2: badaladas em anel (donut) em sequência. |
| Frozen Cathedral | **The Rimebound Penitent** | Gigante acorrentado; correntes delimitam alcance. Fase 2: rompe correntes e ganha investidas; *Hoarfrost Carapace* a cada 15 s que precisa ser quebrada com Heavy/Ember. |
| Shattered Expanse | **The Twice-Born** | Dupla de duelistas espelhados que compartilham HP; um ataca, o outro ecoa 1,0 s depois em espelho. Fase 2: trocam de posição por teleporte. |
| Shattered Expanse | **Stormglass Matriarch** | Golem de vidro elétrico que planta 4 pilares; quando dois pilares estão ligados por raio, o feixe corta a arena. Destruir pilares é obrigatório para chegar nela na fase 2. |
| Hollow Depths | **Mother of Maws** | Planta-colmeia estática no centro; bocas emergem em pontos da arena (Gnawling × alto volume). Fase 2: Pull constante para o centro (fraco), com anel de mordida. |
| Hollow Depths | **The Rotting Herald** | Arauto que espalha Decay; ao atingir o máximo de 5 stacks de Decay no jogador (07, R49), dispara Detonation e consome os stacks. Fase 2: invoca totens de Decay; destruí-los remove 2 stacks. |
| Astral Citadel | **Pillar-Warden Kessiel** | Cavaleiro que cicla 3 afinidades (postura visível). Fase 2: copia a última habilidade usada pelo jogador (versão enfraquecida, telegraphada). |
| Astral Citadel | **The Unwritten Knight** | Forgotten que "apaga" partes do chão (zonas sem piso por 6 s, telegraph roxo). Fase 2: apaga temporariamente **um slot de habilidade do jogador** por 6 s (ícone selado, aviso 1,5 s antes; nunca o Ultimate nem a habilidade de mobilidade). |

---

## 8. Chefes de região

### 8.1 Regras gerais
- **Duração alvo** com equipamento adequado: 3–4 min em Normal; a fase/variação extra de Eclipse leva a até 5 min (R16, TTK de 06 §3.13). Valores por chefe abaixo são de Normal. HP é derivado da duração alvo × DPS esperado da dificuldade (06-combate/10-progressao fornecem o DPS de referência).
- **Fases** por limiar de HP; transição com 1,5–3 s de invulnerabilidade sinalizada (o chefe não ataca durante a transição).
- **Stagger de chefe:** chefes são massa Colossal; barra e limiares de 06 §3.8.2. Ao encher, o chefe entra em **Broken** por 4 s com **+30% Vulnerability** (conta no teto de +60% de 07; não é fonte More) e hit-stop global. Após cada Break o limiar cresce 50%, até 3× (R11).
- **CC em chefe:** Stun, Freeze, Root e Knockback viram Stagger (06). Exceções nomeadas de 07: Freeze vira **Frostbound** e Pull vira **Distortion** (+10% Vulnerability) (R10). Chefes e Guardians são imunes a Execute; efeitos de Execute viram +25% Vulnerability abaixo de 20% de vida (R08).
- **Janelas scriptadas (*Exposed*):** algumas mecânicas abrem uma janela em que o chefe recebe dano normalmente e pode ser Staggered; *Exposed* não dá bônus de dano e não é Broken.
- **Soft enrage:** a partir de 2× a duração alvo, +15% de dano do chefe a cada 30 s (cumulativo, sem teto). Sem hard enrage.
- **Adds** nunca representam mais de 30% do dano recebido em uma luta de chefe.
- **Escala por dificuldade:** Nightmare adiciona 1 mecânica (marcada ✦); Eclipse adiciona uma variação de padrão em cada fase (✧) e uma fase extra curta (§15.2); Ascension usa os modificadores de 10-progressao.
- **Recompensas comuns:** baú do chefe (05-loot define quantidade/raridade), material de chefe *Heart of [Chefe]* (definido por 10-progressao, R36: invoca Boss Echoes e é trocado pelos Uniques daquele chefe), e **pool de 3 Uniques exclusivos por chefe** (catalogados em 02-equipamentos, R28; não caem em Normal). Primeira derrota em cada dificuldade: Legendary garantido + desbloqueio (10-progressao).

### 8.2 The Ashen Colossus — The Ashen Wastes
*Colosso de escória e cinzas animado por Ember residual dos Pilares. Luta-tutorial: ensina telegraphs grandes, janelas de punição e Stagger.*

- **Arena:** caldera circular, raio 16 m, 4 respiradouros de magma nas bordas.
- **Duração alvo:** 3–4 min. **Fases:** 3.
- **Fase 1 (100–60%):**
  - *Sweeping Arm* — cone 120°, 9 m, 1,2 s (vermelho).
  - *Cinder Slam* — círculo 6 m no punho, 1,4 s (vermelho), deixa poça Burning 6 s; após o slam o punho fica preso 2,5 s (janela de punição, ponto fraco: +25% dano no punho).
  - *Ash Rain* — 6–8 círculos 2 m aleatórios + 2 no jogador, 1,0 s.
- **Fase 2 (60–25%):** o núcleo no peito fica exposto e aparece como alvo.
  - *Molten Fissures* — 3 linhas atravessando a arena a partir dele (linhas vermelhas, 1,2 s, persistem 3 s).
  - Invoca 2 levas de Cinder Husks (PA 8) dos respiradouros.
  - Quebrar o Stagger o faz ajoelhar e expõe o núcleo ao alcance melee (estado Broken).
  - ✦ *Respiradouros em erupção*: um respiradouro vira coluna de fogo permanente a cada ciclo.
- **Fase 3 (< 25%):** *Eruption* — a arena encolhe em anéis de magma (raio 16 → 10 m em 3 etapas); salto do Colosso para o ponto do jogador (círculo 7 m, 1,6 s, vermelho, sombra crescente).
  - ✧ salto duplo encadeado.
- **Recompensas específicas:** Uniques temáticos de Ember/armas pesadas; *Heart of the Colossus*.

### 8.3 The Pale Sovereign — The Frozen Cathedral
*Rainha dos Forgotten congelada em seu trono por um eco do Sundering. Ensina gestão de Freeze e posicionamento.*

- **Arena:** nave da catedral, 30 × 18 m, 4 braseiros (zonas quentes de raio 3 m) e o trono ao fundo.
- **Duração alvo:** 3,5 min. **Fases:** 3.
- **Mecânica central — *Frostbite*:** fora das zonas dos braseiros, o jogador acumula Chill lentamente (07-resonance) durante a fase 2; ao completar, Freeze por 1,0 s (teto de Freeze no jogador, R09), seguido de 2 s de imunidade a Hard CC (06). Braseiros podem ser apagados por ataques dela (reacende após 15 s).
- **Fase 1 (100–65%):** duelo com espada de gelo.
  - *Pale Cut* — combo de 3 cortes (cones 2,5 m, 0,6 s cada).
  - *Glacial Lunge* — linha 12 m, 0,9 s.
  - *Shatterline* — linha de estacas de gelo no chão que explode após 1,2 s.
- **Fase 2 (65–30%):** retorna ao trono (imune) e conjura.
  - 3 *Ice Effigies* (HP 3× Standard) protegem o trono; destruir as 3 a derruba do trono (janela *Exposed* de 6 s: recebe dano normal e pode ser Staggered; não é o Broken de 06).
  - *Blizzard Bell* — anéis em sequência a partir do trono (donuts alternados, 1,3 s).
  - Frostbite ativo.
  - ✦ Rimeseeker Adepts reaplicam *Hoarfrost Carapace* nas Effigies.
- **Fase 3 (< 30%):** *Coronation of Frost* — lutas corpo a corpo de novo, com velocidade +20%; cada 3º golpe cria estátua de gelo que explode 2 s depois (círculo 3 m). Explodir uma estátua com dano Ember ou Heavy causa *Thermal Shatter* nela própria (interação de Resonance em 07).
  - ✧ estátuas surgem em pares.
- **Recompensas específicas:** Uniques temáticos de Frost/controle; *Heart of the Pale Sovereign*.

### 8.4 The Fractured One — The Shattered Expanse
*Entidade que existe em vários momentos ao mesmo tempo. Ensina padrões atrasados e leitura de múltiplas ameaças.*

- **Arena:** 3 plataformas hexagonais (raio 7 m) ligadas por pontes; plataformas giram/trocam de posição a cada transição de fase.
- **Duração alvo:** 4 min. **Fases:** 3.
- **Mecânica central — *Temporal Echo*:** cada ataque é repetido por seu eco 2,0 s depois no mesmo ponto (o eco tem cor mais fria e borda tracejada).
- **Fase 1 (100–65%):**
  - *Fracture Beam* — feixe giratório 180° (linha, 1,2 s de telegraph, gira em 2 s).
  - *Storm Lattice* — grade de linhas elétricas em xadrez (alternância de casas seguras).
  - *Shard Volley* — 5 projéteis que ricocheteiam nas bordas da plataforma uma vez.
- **Fase 2 (65–30%):** divide-se em **3 Fragmentos** (um por plataforma). Apenas o Fragmento com núcleo brilhante recebe dano; o núcleo migra a cada 12 s (indicador entre plataformas). Matar os 3 dentro de 10 s um do outro funde a entidade; caso contrário, os mortos voltam com 40% HP.
  - ✦ pontes colapsam temporariamente (Storm) por 4 s após cada migração.
- **Fase 3 (< 30%):** *Rewind* — a cada 25 s marca uma âncora temporal (cristal na plataforma). Se o cristal não for destruído em 8 s, a entidade **rebobina 10% do HP**. Ataques de fase 1 retornam, com ecos duplos (2,0 s e 4,0 s).
  - ✧ duas âncoras simultâneas em plataformas diferentes.
- **Recompensas específicas:** Uniques temáticos de Storm/Echo; *Heart of the Fractured One*.

### 8.5 The Devouring Presence — The Hollow Depths
*Fome sem corpo — a garganta do Hollow. Ensina controle de espaço sob Pull constante e gestão de debuffs.*

- **Arena:** "estômago" circular, raio 18 m, com a Boca no centro; tentáculos emergem do chão; iluminação por fungos que podem ser acesos.
- **Duração alvo:** 4 min. **Fases:** 3.
- **Mecânica central — *Hunger*:** Pull suave e constante em direção à Boca (–20% velocidade ao se afastar dela). Entrar no raio de 3 m da Boca causa dano alto contínuo.
- **Fase 1 (100–65%):** 4 Tentáculos (HP 4× cada) são os alvos; a Boca só recebe dano quando 2+ tentáculos são cortados (janela *Exposed* de 6 s).
  - *Lash* — linha 10 m (vermelho); *Grasp* — roxo, agarra e puxa 4 m.
  - *Ruin Spray* — cone de Decay a partir da Boca.
- **Fase 2 (65–35%):** *Swallow* — com telegraph de 2,0 s (anel roxo gigante com zona segura em movimento), engole o jogador se não for evitado; dentro do **Interior** (sub-arena de 15 s) o jogador ataca 3 órgãos pulsantes (dano real na barra do chefe, ×1,5). Ao fim é cuspido. Evitar o Swallow é possível mas não traz vantagem de dano — é uma escolha de risco.
  - ✦ Hollow Thralls emergem a cada 20 s.
- **Fase 3 (< 35%):** *Lightless* — escuridão total exceto raio de 6 m ao redor do jogador e fungos acesos (acender = atacar o fungo, ilumina 8 m por 15 s). Tentáculos reaparecem; Umbral Stalkers (PA 5) entram na escuridão.
  - ✧ Pull dobra de força a cada 20 s por 3 s (anel de aviso).
- **Recompensas específicas:** Uniques temáticos de Void/Ruin/invocação; *Heart of the Devouring Presence*.

### 8.6 The First Forgotten — The Astral Citadel
*O primeiro ser apagado da existência, guardião ou causador do Sundering. Exame final: todas as afinidades e todas as lições anteriores.*

- **Arena:** topo da Cidadela, círculo de raio 20 m com 6 Pilares quebrados (um por afinidade) nas bordas.
- **Duração alvo:** 4 min (Normal), até 5 min em Eclipse; é a exceção de topo da faixa. **Fases:** 2 formas, 4 fases (as fases 1 e 3 são mais curtas que nos outros chefes).
- **Forma I — o Cavaleiro-Oráculo (100–50%)**
  - **Fase 1 (100–75%) — Stances:** cicla posturas de afinidade a cada 15 s (cor no corpo e no chão); cada postura troca o conjunto de ataques: Ember (slams com poças), Frost (anéis de Freeze), Storm (dashes encadeados), Void (Pull + Implosion), Echo (ataques ecoados), Ruin (Decay + Detonation).
  - **Fase 2 (75–50%) — Unwriting:** a cada 20 s, sela um slot de habilidade do jogador por 6 s (aviso 1,5 s; nunca o Ultimate nem mobilidade); postura dupla (2 afinidades ao mesmo tempo).
- **Transição (50%):** cinemática curta; ele absorve os Pilares e se torna colossal.
- **Forma II — o Colosso Astral (50–0%)**
  - **Fase 3 (50–20%) — Restoration:** o chefe é imune até o jogador **restaurar Pilares**: ficar 3 s dentro do círculo de um Pilar (interrompido por receber Stun). Cada Pilar restaurado deixa-o vulnerável por 12 s e concede ao jogador um buff da afinidade daquele Pilar até o fim da luta (stack até 3). Ataques de arena inteira (*Starfall*, *Erasure Wave* — anel roxo com 1 brecha).
  - **Fase 4 (< 20%) — Final Eclipse:** a arena se reduz ao círculo central (raio 10 m), todos os Pilares restaurados ficam ativos e ele usa ataques combinados de duas afinidades (Resonance inimiga, ver 07). ✧ meteoros com ecos.
  - ✦ (Nightmare+) a cada restauração de Pilar, um Herald of the First abre uma fenda.
- **Recompensas específicas:** Uniques de Resonance multi-afinidade; *Heart of the First Forgotten*; primeira derrota em Eclipse libera o endgame (Ascension — 10-progressao).

---

## 9. Tipos de sala

Toda sala de combate fecha as portas ao entrar e abre ao limpar. Recompensa de cada porta é mostrada antes da escolha.

| Sala | Duração alvo | Conteúdo | Recompensa (tipo; quantidades em 05-loot) |
|---|---|---|---|
| **Combat Arena** | 60–90 s | 2–3 ondas, orçamento §10 | 1 recompensa do tipo indicado na porta: Equipamento, Rift Shards, Aether Dust, Blessing Lesser ou Cura |
| **Elite Arena** | 75–120 s | 1 matilha Elite (+ 1 onda de apoio) ou 2 matilhas (Nightmare+) | Equipamento Rare+ garantido, chance de Epic/Legendary, + recompensa da porta |
| **Guardian Arena** | 30–60 s | Guardian nomeado (§7) | Baú Epic+, Aether Dust e Sundered Cores |
| **Treasure Room** | 20–40 s | 1–2 baús; 15% de chance de *Hollow Coffer* (baú-emboscada: onda de PA 15 ao abrir, dobro de recompensa) — sinalizado por baú com tentáculos sutis para quem observar | Equipamento (escolha 1 de 3 em uma das versões de baú, ver 05) e Rift Shards |
| **Shrine Room** | 15–30 s | 1 Shrine (§12); 20% de chance de 2 Shrines diferentes (escolher 1) | Blessing |
| **Event Room** | 30–90 s | 1 evento do catálogo (§11) | Variável |
| **Challenge Room** | 45–120 s | 1 desafio (§9.1); ao entrar a porta só abre ao completar ou falhar | Recompensa de tier superior: chance alta de Legendary, materiais raros; ao falhar: nada ou uma Curse |
| **Rest Room** | 15–30 s | Fogueira do Sanctuary (Remnant) | Serviços definidos em 10-progressao (R34): cura 40% grátis, recarga do Aether Flask (06), Rift Anchor (extração), Anchor (proteger 1 item), Temper e subida de rank de Blessing paga em Rift Shards |
| **Boss Room** | 3–5 min | Chefe (§8) | Baú do chefe, Heart, chance de Unique |

### 9.1 Variantes de Challenge Room
1. **Trial of Haste** — eliminar PA 40 em 45 s.
2. **Trial of Endurance** — sobreviver 60 s a ondas infinitas (ondas aceleram a cada 15 s).
3. **Untouched** — limpar uma sala de PA 30 recebendo no máximo 3 acertos.
4. **Gauntlet** — corredor com armadilhas de região e inimigos ranged; chegar ao fim.
5. **Elite Crucible** — 2 matilhas Elite seguidas com +1 afixo.
6. **Guardian's Shadow** (Nightmare+) — o outro Guardian da região, com 1 afixo.

### 9.2 Modificadores de sala (Room Modifiers)
Aplicados por eventos, Curses, Ascension ou com 10% de chance em salas de combate a partir de Nightmare; sempre exibidos na porta.

- **Aether Storm** — raios aleatórios (círculos vermelhos 1,0 s) atingem jogador e inimigos.
- **Unstable Ground** — partes do chão colapsam ciclicamente (roxo).
- **Dim** — visão reduzida a 10 m (só Hollow Depths/Astral Citadel).
- **Resonant Field** — todos os status de afinidade aplicados têm +50% de duração (para ambos os lados).
- **Bloodmoon** — inimigos +20% dano, recompensa +50%.
- **Swarm** — dobro de fodder, sem heavies.

---

## 10. Orçamento de ameaça e composição

### 10.1 Orçamento por sala

```
OrçamentoSala = (20 + 3 × Linha) × MultDensidade(Dificuldade) × MultRegião
```

- `Linha` = posição da sala no mapa (1–12).
- `MultDensidade`: Normal 1,0 · Veteran 1,1 · Nightmare 1,2 · Eclipse 1,3 · Ascension: 1,3 + 0,005 × nível (máx. 1,8 em A100).
- `MultRegião`: 1,0 em todas (a escala de dificuldade entre regiões é feita por Monster Level, não por densidade, para manter leitura constante).
- Exemplo: linha 1 Normal = 23 PA; linha 12 Eclipse = 56 × 1,3 ≈ 73 PA.
- Elite Arena: orçamento da linha × 1,2, sendo a matilha Elite ≥ 50% do total.

### 10.2 Divisão em ondas
- 2 ondas (linhas 1–4) ou 3 ondas (linhas 5+). Divisão padrão: 40% / 35% / 25%, ou 55% / 45%.
- Próxima onda entra quando o PA vivo cai abaixo de 30% do PA da onda atual, ou após 25 s (o que vier primeiro). *Mantém ritmo para builds de ramp-up sem deixar a sala parar.*
- Spawns acontecem a ≥ 8 m do jogador, fora do campo de visão quando possível, com portal de 0,8 s (inimigos não podem ser atingidos nem atacar durante o portal).

### 10.3 Limites de leitura
- Teto de unidades vivas: **24** (incluindo invocações), **teto de 40 PA vivos** na tela.
- No máximo 2 casters e 1 summoner por onda (exceto receitas temáticas).
- Specialists (caster, support, summoner, assassin) ≤ 40% do PA da onda.
- Toda onda tem pelo menos 1 unidade de linha de frente (melee/tank).
- No máximo 1 Tank por onda até a linha 6; 2 depois.

### 10.4 Receitas de encontro (Encounter Recipes)
Templates data-driven que o gerador sorteia (peso por região e linha):

| Receita | Composição | Pressiona |
|---|---|---|
| **Swarm** | 70% fodder + 1 summoner | AoE |
| **Phalanx** | Tank + standards + 1 support | Penetração, flanqueio |
| **Siege** | Ranged/casters atrás de 1–2 tanks | Mobilidade, gap-close |
| **Ambush** | Spawn em volta do jogador + 2 assassins | Defesa, reação |
| **Ritual** | 2 casters canalizando + escolta | Interrupção, priorização |
| **Brood** | 2 summoners + fodder | Single target |
| **Mixed Patrol** | Distribuição padrão por arquétipo | Geral |

### 10.5 Recompensas por inimigo
- XP e Rift Shards dropados por inimigo são **proporcionais ao PA** (constantes por PA definidas em 10-progressao). Elites valem ×3 seu PA em XP/Rift Shards.
- Classes de loot para 05-loot: `Fodder`, `Standard`, `Heavy` (Tank), `Specialist`, `Elite`, `Guardian`, `Boss`.

---

## 11. Mapa de nós da run

### 11.1 Estrutura

| Linha | Estrato | Nós na linha | Regras |
|---:|---|---|---|
| 1 | I — Limiar | 2 | Combat apenas |
| 2–4 | I | 2–3 | Combat, Shrine, Event, Treasure (a partir da 2), Elite (a partir da 3) |
| 5 | I (fim) | 2–3 | *Clareira* (sem combate): **sempre pelo menos 1 nó Rest**, mais Shrine ou Treasure |
| 6–8 | II — Fratura | 3–4 | Todos exceto Rest; Challenge permitido |
| 9 | II (fim) | 1 | **Guardian Arena** (fixo) |
| 10–12 | III — Núcleo | 2–4 | Todos exceto Rest; mais Elites e Challenges |
| 13 | III (fim) | 1 | **Antecâmara** (Rest fixo, mesmos serviços da Rest Room definidos em 10) |
| — | — | 1 | **Boss Room** |

- **Estratos:** I = linhas 1–5, II = linhas 6–9, III = linhas 10–13 (R33). 10-progressao, 05-loot e 11-arquitetura usam esta estrutura.
- **Salas visitadas por run:** 13 + chefe = 14 salas. Duração estimada: 13 × ~1,5 min + chefe 3–4 min + decisões ≈ **25–35 min**.
- **Extração:** o Rift Anchor (10) está disponível nas Rest Rooms da linha 5 e na Antecâmara.
- **Overrift (Delve Deeper, 10):** as salas extras após o chefe usam o orçamento da linha 13 + 1 por sala, com receitas de Estrato III e o *Overrift Herald* na última (§15.6).
- **Conexões:** cada nó liga a 1–2 nós da linha seguinte; as arestas não se cruzam; todo nó é alcançável.
- O mapa inteiro do Estrato atual é visível; os Estratos seguintes mostram só a quantidade de nós (sem tipo) até o jogador entrar neles. *Planejamento de curto prazo sem paralisia de análise.*

### 11.2 Frequência por tipo (pesos de geração)

| Tipo | Estrato I (linhas 1–4) | Estrato II (6–8) | Estrato III (10–12) | Média esperada por run |
|---|---:|---:|---:|---:|
| Combat | 55 | 40 | 35 | 5–6 |
| Elite | 10 (linha 3+) | 20 | 25 | 2–3 |
| Treasure | 10 | 8 | 8 | 1–2 |
| Shrine | 15 | 12 | 10 | 2 (+ Clareira) |
| Event | 10 | 12 | 10 | 1–2 |
| Challenge | 0 | 8 | 12 | 0–2 (opcional) |

### 11.3 Restrições de geração
1. Nenhuma Elite nas linhas 1–2; nunca 2 Elites consecutivas no mesmo caminho.
2. Existe sempre pelo menos 1 caminho com ≤ 2 Elites (excluindo Guardian) e 1 caminho com ≥ 3 Elites (caminho de alto risco).
3. Pelo menos 1 Shrine alcançável nas linhas 2–4.
4. Todo caminho tem ≥ 4 Combat Arenas.
5. Toda linha com Challenge tem ≥ 1 nó alternativo que não é Challenge nem Elite.
6. Mesmo evento não se repete na mesma run; o mesmo evento não aparece em 2 runs seguidas da mesma região.
7. Tipos de recompensa das portas de Combat são balanceados por run: pelo menos 2 portas de Equipamento e 2 de Blessing alcançáveis no caminho mais curto.

---

## 12. Shrines e Blessings

### 12.1 Regras
- **Blessings** são bônus que duram **apenas a incursão atual**. O jogador escolhe 1 entre 3 ofertas.
- **Ranks:** Lesser → Greater → Exalted (dono: este doc; a "Eclipse Blessing" de 10 corresponde a uma Exalted). O serviço pago da Rest Room (10) e alguns eventos sobem o rank de uma Blessing já obtida (Lesser → Greater; Exalted só por oferta direta).
- **Raridade de oferta:** Lesser 70% / Greater 25% / Exalted 5% (Exalted só a partir do Estrato II). 10-progressao pode adicionar desbloqueios que alteram esses pesos.
- **Orçamento:** Blessings Lesser/Greater entram no bucket **Increased**; apenas Exalted podem ter fonte **More**, limitada a **+8% More por Blessing** e **+20% More total** de Blessings. Alvo agregado: **+20–30% de poder efetivo** ao chegar no chefe; HP e dano dos inimigos (06/10) são calibrados assumindo **+25%** de poder de run (R35).
- **Tags:** toda Blessing usa as tags de `00-convencoes §3.4`; ofertas são ponderadas pelas tags das habilidades equipadas (×2 para tags presentes no kit atual) — *smart offer*, nunca exclusivo.
- Blessings de afinidade podem aplicar status de afinidade; o comportamento desses status é de 07-resonance.

### 12.2 Tipos de Shrine
| Shrine | Oferta |
|---|---|
| **Shrine of Ember / Frost / Storm / Void / Echo / Ruin** | 3 Blessings da afinidade (o tipo é visível no mapa) |
| **Shrine of Iron** | Blessings defensivas/genéricas |
| **Shrine of the Pillars** (raro, 5% das Shrines, Estrato II+) | 3 Blessings Greater+ de afinidades diferentes |
| **Blood Shrine** (variante 15%) | Pagar 15% do HP máximo (até o fim da run) para receber a oferta com +1 rank |

### 12.3 Catálogo inicial (valores em rank Lesser / Greater / Exalted)

**Ember**
- *Kindling* — acertos têm chance de aplicar Burning (10 / 15 / 20%).
- *Pyre Heart* — +Ember Damage (10 / 18 / 25% Increased).
- *Ashfall* (Exalted-only) — inimigos que morrem com Burning explodem (dano = 30% do HP máx. do alvo, círculo 3 m); +6% More Ember.

**Frost**
- *Rime Touch* — habilidades aplicam Chill adicional (+1 / +2 / +3 acúmulos).
- *Glacial Ward* — ao congelar um inimigo, ganha escudo (3 / 5 / 8% HP).
- *Absolute Edge* (Exalted-only) — +8% More dano contra inimigos Frozen ou Slowed.

**Storm**
- *Static Step* — movimento deixa Static Field (1 / 2 / 3 s de duração).
- *Arc Conduit* — Chain Lightning salta +1 / +1 / +2 alvos.
- *Overload Surge* (Exalted-only) — a cada 5ª habilidade, descarga em área (círculo 5 m); +5% More Storm.

**Void**
- *Gravity Well* — habilidades de Area puxam levemente (raio +10 / 15 / 20%).
- *Widening Void* — +Area Damage (10 / 18 / 25% Increased).
- *Singularity Seed* (Exalted-only) — a cada 8 s, a próxima habilidade de Area termina com Implosion; +6% More Void.

**Echo**
- *Afterimage* — 8 / 12 / 16% de chance de habilidades se repetirem com 40% do dano (a repetição é uma réplica IsEcho: não replica de novo e usa proc coefficient 0,5, R07).
- *Resound* — efeitos atrasados (Delayed Effect) têm +20 / 35 / 50% de dano.
- *Perfect Recall* (Exalted-only) — o Ultimate é repetido uma vez com 50% de potência.

**Ruin**
- *Rot* — acertos aplicam Decay (+1 / +1 / +2 acúmulos).
- *Ruinous Fate* — Vulnerability aplicada tem +2 / 3 / 4 pontos percentuais (sempre dentro do teto de +60% de 07, R12).
- *Final Rites* (Exalted-only) — inimigos abaixo de 15% HP com Decay detonam; +5% More contra Elites e chefes.

**Iron (genéricas)**
- *Hardened* — +Armor e Elemental Resistance (8 / 14 / 20% Increased).
- *Second Wind* — 1 vez por run, ao receber dano fatal, sobrevive com 30% HP (Lesser: 20%, Greater: 30%, Exalted: 40% + 2 s de invulnerabilidade).
- *Fleetfoot* — +Movement Speed (5 / 8 / 12%).
- *Resource Font* — +Resource Generation (8 / 12 / 18%, traduzido por herói conforme 01).
- *Treasure Sense* — +Gold Acquisition e chance de baú extra em Combat (10 / 15 / 25%).
- *Duelist's Focus* — +Skill Damage contra Elites e chefes (8 / 14 / 20% Increased).

> Meta inicial: 36 Blessings (6 por afinidade + 6 Iron + Pillars). As 30 acima são o núcleo; as restantes seguem o mesmo padrão por afinidade (1 ofensiva Increased, 1 utilitária/comportamental, 1 Exalted).

---

## 13. Curses de run
Aplicadas por eventos, falha em Challenge ou escolhas de risco. Duram até o fim da run ou até serem purificadas (Storm Caravan, eventos específicos ou serviço de Rest se 10 o oferecer).

| Curse | Efeito |
|---|---|
| **Haunted** | Uma *Forgotten Wraith* (PA 5) persegue o jogador em todas as salas de combate |
| **Unarmored** | –15% Armor e resistências |
| **Hunted** | +1 afixo em todas as Elites |
| **Withering** | Cura recebida –30% |
| **Starved** | Recompensas de Rift Shards e Gold –50% |
| **Fraying** | Perde 1 Blessing aleatória ao entrar no Estrato seguinte |

---

## 14. Eventos (Event Rooms)

Cada evento tem 2–3 escolhas; recompensas/penalidades são explícitas quando o personagem "saberia" e ocultas (marcadas "?") quando o mistério é o ponto. Eventos podem ser exclusivos de região (R) ou globais (G).

1. **The Bleeding Remnant** (G) — Um sobrevivente ferido.
   - *Ajudar* (–10% HP atual): ganha Gold e, ao fim da run, entrada de lore no Archivist + pequena reputação Remnant (10-progressao).
   - *Saquear*: Gold ×2, recebe Curse **Haunted**.
   - *Ignorar*: nada.
2. **Altar of Embers** (R: Ashen Wastes) — Ofertar 30% dos Rift Shards por uma Blessing Ember Greater garantida; ou tocar o altar de graça (?: 50% Blessing Lesser / 50% Burning em si por 10 s e nada).
3. **The Mirror That Remembers** (G, Estrato II+) — Lutar contra **seu próprio Eco** (cópia do herói com a build atual, 60% dos stats, IA de duelo). Vitória: baú com chance alta de Legendary. Recusar: nada.
4. **Ascendant Broker** (G) — Trocar 10% de HP máximo (run) por uma Blessing Exalted à escolha entre 2; ou trocar uma Blessing existente por outra de rank superior de afinidade diferente.
5. **The Forgotten Name** (G) — Uma lápide pede um nome entre 3. Cada nome dá uma Blessing diferente; o nome "correto" (revelado por lore desbloqueada no Archivist) dá a versão Exalted. *Recompensa conhecimento de lore.*
6. **Void Breach** (R: Hollow Depths / Shattered Expanse) —
   - *Fechar*: sobreviver 30 s a ondas Void (PA 30) → Aether Dust ×2 (quantidade em 10).
   - *Deixar aberta*: as próximas 3 salas de combate ganham afixo Gravitic nas Elites e +50% de recompensa.
7. **Frozen Pilgrims** (R: Frozen Cathedral) — Libertar peregrinos presos no gelo (combate contra Penitents, PA 20): a próxima Rest Room cura 100% em vez de 40%. Ou levar seus pertences: equipamento Rare garantido + Curse **Withering**.
8. **Gambler's Cairn** (G) — Apostar 25% / 50% / 100% dos Rift Shards: 45% de chance de dobrar, 10% de triplicar, 45% de perder.
9. **Ruin Bloom** (R: Hollow Depths) — Consumir a flor: 1 Blessing Greater aleatória + 1 Curse aleatória. Queimar a flor (requer causar dano Ember): Aether Dust. Ignorar: nada.
10. **Fallen Pillar Stele** (G, Estrato II+) — Alinhar-se a uma afinidade: todas as ofertas de Blessing dessa afinidade têm peso ×3 pelo resto da run e a próxima Shrine é dessa afinidade. *Ferramenta para focar uma build.*
11. **Storm Caravan** (R: Shattered Expanse / G a partir de Nightmare) — Mercador Remnant itinerante: vende cura (30% HP), reroll de oferta de Blessing, purificação de Curse e 1 equipamento aleatório Rare/Epic, tudo com Rift Shards.
12. **The Hollow Child** (G) — Uma criança feita de sombra acena.
    - *Segui-la*: a próxima sala vira Treasure secreta com 3 baús, mas a seguinte vira Elite Arena com +1 afixo (o mapa muda visivelmente).
    - *Ignorar*: nada. *Atacá-la*: ?: ela vira um Elite Phasing sozinho (sem Retainers) com recompensa de Elite.
13. **Echo of a Fallen Eclipsed** (G) — Aparece se a última run do jogador nessa região terminou em morte: o fantasma do herói caído (build daquela run, 70% dos stats). Vencer devolve **50% dos Rift Shards e 1 item perdidos** na morte (R46; regras em 10-progressao). *Gancho roguelite de revanche.*
14. **The Archivist's Lost Page** (G, raro) — Uma página de lore. Ler: ganha entrada no Archivist e XP extra; *Queimar* para iluminar: próxima sala tem modificador Dim removido e +20% Gold.

---

## 15. Conteúdo de inimigos do endgame (R43)

10-progressao define desbloqueios, chaves, custos e recompensas; este doc define o que o jogador enfrenta. Nenhum desses conteúdos usa telegraph, letalidade ou wind-up fora das regras de 06.

### 15.1 Nightmare Behaviors (a partir de Nightmare)
Cada arquétipo ganha **uma variante de ataque** adicional, sorteada com 35% de chance por inimigo (100% em Elites):

| Arquétipo | Nightmare Behavior |
|---|---|
| Melee fodder | *Frenzy:* ao morrer um aliado a 4 m, ganha +25% velocidade de ataque por 4 s (aura vermelha) |
| Melee standard | *Feint:* o 2º golpe do combo pode ser atrasado 0,3 s (ciano no atraso) |
| Ranged | *Suppressing Fire:* rajada de 5 projéteis em leque estreito a cada 10 s |
| Caster | *Overchannel:* a zona principal ganha um segundo anel externo 0,8 s depois |
| Tank | *Charge:* investida Unblockable em linha de 10 m (wind-up 1,2 s) |
| Summoner | *Sacrifice:* consome uma invocação para curar 20% do próprio HP (ciano interrompe) |
| Assassin | *Double Strike:* após o dash, segundo dash em direção oposta 0,6 s depois |
| Support | *Rally:* uma vez por encontro, remove CC de todos os aliados em 8 m |

**Champion Packs** (referidos por 10): Elite Arenas de Nightmare+ podem gerar uma matilha com **2 Elites** em vez de 1 Elite + Retainers (30% de chance, já prevista na §6.2); são o único caso em que o afixo Soulbound liga dois Elites.

### 15.2 Eclipse Phenomena (a partir de Eclipse)
Uma regra ambiental fixa por região, sempre ativa em Eclipse e Ascension e exibida no mapa:

| Região | Phenomenon | Efeito |
|---|---|---|
| Ashen Wastes | *Spreading Cinders* | Poças Burning se expandem 0,5 m/s por 4 s antes de apagar; inimigos também são afetados |
| Frozen Cathedral | *Deep Cold* | Ficar parado por 2 s aplica Chill no jogador; mover-se remove |
| Shattered Expanse | *Temporal Drift* | A cada 20 s, todos os projéteis inimigos são repetidos 1,0 s depois (eco, borda tracejada) |
| Hollow Depths | *Encroaching Dark* | Zonas de escuridão cobrem 30% a mais da arena; Umbral Stalkers aparecem em salas comuns |
| Astral Citadel | *Shifting Pillars* | A afinidade dominante da sala muda a cada onda, e os inimigos ganham resistência moderada a ela |

**Fase extra de chefe em Eclipse:** cada chefe ganha uma fase curta (20–40 s) abaixo de 10% de vida, com um ataque assinatura combinado das fases anteriores. Isso cabe no teto de 5 min de R16.

### 15.3 Boss Echoes
Versões remixadas dos 5 chefes (tiers I/II/III conforme 10):

- **Tier I:** padrões das fases 1 e 2 se misturam desde o início; arena alterada (Ashen Colossus luta numa ponte de escória; Pale Sovereign sem braseiros fixos, eles se movem; Fractured One em 2 plataformas; Devouring Presence com Pull alternando de direção; First Forgotten com posturas duplas desde a fase 1).
- **Tier II:** +1 mecânica nova por chefe (ex.: Colossus invoca um segundo braço independente; Fractured One ganha o segundo eco a 4,0 s em todas as fases).
- **Tier III:** todas as variações ✦ e ✧ ativas ao mesmo tempo, sem fases de adds.
- Duração alvo igual à do chefe em Eclipse (até 5 min). Recompensas: os 3 Uniques daquele chefe com chance maior e Pillar Seals (10, R28).

### 15.4 Eclipse Trial
Boss rush de **3 chefes em versão reduzida** (uma fase cada, ~90 s por chefe, sem adds), sorteados entre os 4 primeiros, com 1 Rest entre o 2º e o 3º e uma Distortion fixa (10). Serve de teste de prontidão: os ataques têm os números de Eclipse, mas os chefes têm 50% do HP.

### 15.5 The Sundering Heart (pinnacle)
- **Fase 1 — Convergence (100–60%):** o Coração cicla as 5 arenas dos chefes de região a cada 30 s; em cada uma usa 2 ataques assinatura daquele chefe.
- **Fase 2 — Six Pillars (60–25%):** 6 núcleos de afinidade orbitam o Coração; cada núcleo ativo concede a ele um padrão de afinidade. Destruir um núcleo abre uma janela *Exposed* de 8 s e aplica no jogador o buff daquela afinidade.
- **Fase 3 — Collapse (< 25%):** a arena encolhe; ataques combinam duas afinidades por vez (Resonance inimiga de 07) e há uma mecânica Annihilation a cada 40 s (anel com 1 brecha, telegraph ≥ 2,0 s).
- Duração alvo 6 min (única exceção acima de 5 min, por ser pinnacle opcional).

### 15.6 Overrift Herald
Elite especial na última sala do Overrift (10): um Ascendant com **2 afixos fixos (Phasing + Commanding)** mais os afixos normais da dificuldade, acompanhado de 1 onda de Retainers. Massa da Elite base Medium, HP de Elite × 1,5. Drop garantido de Epic+ (tabela em 05).

---

## 16. Dependências e pontos de integração

| Documento | Este doc fornece | Este doc consome |
|---|---|---|
| 01-atributos | Classes de inimigo, resistências por região (qualitativas) | Fórmulas de Armor/resistência de inimigos, valores de resistência, velocidade base dos heróis (R50) |
| 02-equipamentos | Temas dos pools de 3 Uniques por chefe | Catálogo de Uniques de chefe (15 = 3 × 5, R28) |
| 03-modificadores | Interações com clones/invocações (Arc Lancer, Overcharged), projéteis refletidos (Prismatic) | — |
| 05-loot | Classes de loot por inimigo, tipos de sala e recompensas por sala, estrutura de 13 linhas + chefe, Guardian/chefe, Overrift Herald | Quantidades, raridades, drop rates, pity, baú "escolha 1 de 3" |
| 06-combate | Tabela de tokens de ataque (R17), classes de massa por arquétipo, tempos de cada ataque (≥ pisos de 06), durações de sala/Guardian/chefe | Linguagem de telegraph, wind-ups mínimos, letalidade, TTK, curva base de HP/dano, Stagger/Broken, DR de CC, estados Stealth/Execute, HitFlags |
| 07-resonance | Inimigos que aplicam e usam status; Thermal Shatter na Pale Sovereign; Resonance inimiga na First Forgotten e no Sundering Heart | Definição de Burning, Chill/Freeze, Shock, Pull, Repeat, Decay (máx. 5), Vulnerability (teto +60%), Weakening, Detonation, Frostbound, Distortion, Frozen Armor |
| 08-herois | Inimigos que exigem respostas de kit (Stealth, Pull, selamento de slot, escudo frontal) | Garantia de que cada herói tem resposta a Stealth/escudo frontal |
| 10-progressao | Estrutura da run (linhas/Estratos), catálogo de Blessings e ranks, Curses, afixos de Elite por dificuldade, conteúdo de endgame (§15), evento Echo of a Fallen Eclipsed | Monster Level por região/dificuldade, regra de morte, moedas (Rift Shards, Heart of [Chefe], Aether Dust, Sundered Cores), serviços da Rest Room, Overrift, Distortions, modificadores de Ascension, constantes XP/Rift Shards por PA, reputação Remnant |
| 11-arquitetura | Tudo aqui é data-driven: `EnemyDefinition`, `EliteAffixDefinition`, `EncounterRecipe`, `RoomDefinition`, `NodeMapRules`, `EventDefinition`, `BlessingDefinition`, `CurseDefinition`, `BossPhaseDefinition`; tabela de tokens por dificuldade; lista de tipos de sala (9, incluindo Guardian Arena) | Sistema de tags e HitFlags, LOD de IA (10/4 Hz), eventos de combate, save de "última run morta" (para evento 13) |

---

## 17. Conflitos com as convenções

Nenhum em aberto. Resolvidos na rodada 1 (`00-convencoes §5`):

1. **Blessings de run:** camada temporária fora do orçamento permanente, confirmada por R35 (este doc é dono; inimigos calibrados para +25%).
2. **Broken de chefe:** deixou de ser fonte More; agora é +30% Vulnerability com os valores de 06 (R11).
3. **Heart of [Chefe]:** definido por 10-progressao (R36).
4. **Renomeações por nomes reservados (R48):** afixo *Frostbound* → **Glacial**; afixo *Unstoppable* → **Immovable**; Curse *Brittle* → **Unarmored**; Blessing *Event Horizon* → **Widening Void**; *Brittle Fate* → **Ruinous Fate**; buff inimigo *Frozen Armor* → **Hoarfrost Carapace**; "Guardião/mini-chefe" → **Guardian**; "Poise" → barra de Stagger e classe de massa de 06; evento *Shattered Pillar Fragment* → **Fallen Pillar Stele** (colidia com a moeda Pillar Fragments).
5. **Telegraphs:** laranja deixou de ser cor de telegraph; o bestiário usa vermelho/Unblockable/roxo/ciano/Annihilation de 06 (R14).

---

## 18. Questões abertas para playtest

1. Valor final de tokens de ataque em Eclipse/Ascension (5 melee pode ser excessivo com 4 afixos).
2. Se *Swallow* (Devouring Presence) é percebido como recompensa ou punição — ajustar multiplicador ×1,5 do Interior.
3. Frequência do selamento de slot (Unwritten Knight / First Forgotten): 6 s a cada 20 s pode frustrar builds de uma habilidade só.
4. Divisão 40/35/25 e gatilho de 30% das ondas: verificar ritmo para builds de summoner/DoT (ramp-up).
5. Peso de Challenge Rooms no Estrato III (12) — medir taxa de escolha; meta: 30–50% das runs entram em pelo menos uma.

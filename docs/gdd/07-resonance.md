# 07 — Sistema de Resonance

Documento dono de: status de afinidade (24 efeitos do GDD §7.1), acúmulos, aplicação, as 15 Resonances entre pares de afinidades, limites anti-explosão, interação com Elites/Chefes, descoberta (Codex) e legibilidade visual.

Respeita `00-convencoes.md` (incluindo o registro de resoluções §5, rodada 1). Estados genéricos (Stun, Stagger, Knockback, Root, Slow genérico, i-frames, Super Armor) e a DR de CC (diminishing returns) compartilhada por Freeze e Stun pertencem a 06-combate.

> Nota de nomenclatura (R48): neste documento **DRef** = Dano de Referência. A sigla "DR" é reservada a *diminishing returns*.

---

## 1. Objetivo e escopo

Resonance é a camada que faz afinidades conversarem entre si. Objetivos:

1. Dar a cada afinidade uma identidade mecânica legível (o que ela *faz*, não só quanto dano dá).
2. Recompensar builds híbridas com interações nomeadas, previsíveis e descobríveis.
3. Ser acessível a qualquer herói via equipamento (GDD §7.2).
4. Nunca virar fonte de explosão combinatória: todo efeito tem limite, cooldown e profundidade máxima.

Fora de escopo: valores de atributos (01), catálogo de modificadores que concedem/convertem afinidades (03), fórmula geral de dano e mitigação (06), afixos de Elite (09).

---

## 2. Decisões tomadas

1. **Três modos de aplicação apenas — Buildup, Chance e Garantida.** Buildup para status de acúmulo/controle; Chance para procs; Garantida para efeitos disparados explicitamente por habilidades/itens. Justificativa: previsibilidade e um único modelo de resistência.
2. **Buildup usa Status Potency fixa por golpe, não o dano.** Justificativa: impede que status escalem quadraticamente com dano e mantém CC controlável no endgame.
3. **Cada afinidade tem um status primário aplicado automaticamente por qualquer golpe dela** (Ember→Burning, Frost→Chill, Storm→Shock, Void→Corruption, Echo→Echo Mark, Ruin→Decay). Os demais 18 efeitos exigem fonte explícita (habilidade, modificador, item, Set Power). Justificativa: garante que "ter dano X" já participe de Resonances, enquanto efeitos fortes continuam sendo escolhas de build.
4. **Resonance é sempre ativa** — não há desbloqueio. O Codex apenas registra e explica. Justificativa: descoberta não deve punir quem ainda não leu o Codex.
5. **Uma Resonance por evento por alvo**, escolhida por tabela de prioridade fixa. Justificativa: legibilidade e controle de burst.
6. **Modelo Generation (dono deste doc, R07).** Ação direta = Gen 0; cada proc (gatilho, Resonance, chain, explosão, eco) = Gen da origem + 1; **nada dispara em Gen ≥ 2** (Gen 2 só aplica status a 50% SP). Réplicas (flag `IsEcho`/Echoed) **nunca geram novas réplicas**, exceto poderes que declaram **Echo Recursion 1×** (§7.1). Justificativa: elimina recursão (Echo repetindo Echo, explosões em cadeia infinitas) com uma regra única para 03, 04, 06, 08 e 11.
7. **Proc Coefficient fixo por tipo de evento (R07):** repetição Echo 0,5; cópia de Clone 0,4; tick de área persistente 0,25. Justificativa: alinhado a 03 e 06.
8. **Teto global de 8 procs de Resonance por segundo por jogador** e ICD por alvo para cada Resonance. Justificativa: performance e leitura visual em hordas.
9. **Bursts de Resonance/consumo podem dar crítico; DoTs, zonas e liberação de Echo Mark não.** Justificativa: mantém Critical relevante para builds de detonação sem multiplicar DoTs por crit.
10. **Bucket de Vulnerability com teto total de +60%** no alvo (dono deste doc, R12), incluindo Staggered, Broken, Distortion e a conversão de Execute contra chefe. Ruin aplica stacks; todas as outras fontes dão valor fixo por fonte, sem acumular consigo mesmas. Justificativa: Ruin + Storm + Sets + estados de chefe não podem empilhar vulnerabilidade sem limite.
11. **Sem imunidades próprias de CC (R09).** Freeze pertence ao grupo Hard CC de 06 e usa a DR de 06 (janela 8 s: 100/50/25%/imune). Justificativa: uma regra de CC só no jogo.
12. **Chefes nunca ficam imunes a DoT nem a todas as afinidades**; CC é convertido em Stagger (06), com duas exceções nomeadas definidas aqui: **Frostbound** (Freeze) e **Distortion** (Pull, +10% Vulnerability). Justificativa: builds de status precisam funcionar contra chefes sem introduzir More novo.
13. **Ticks de DoT e zonas a cada 0,5 s (R19).** Justificativa: cadência de 06; 0,25 s em 11 é só granularidade de lote.
14. **Status Potency é um atributo ofensivo de 01 (R30)** que escala o SP. Justificativa: dono de atributos é 01.
15. **Exceções declaradas (R49):** Ultimates podem aplicar Freeze de até 3 s em não-chefes (Absolute Zero); Overload Circuit eleva o máximo de Shock em +2. Justificativa: fantasias de build de 08 sem quebrar regras gerais.
16. **Inimigos usam os mesmos status contra o jogador**, com durações reduzidas e os mesmos máximos de stacks. Justificativa: um vocabulário só para o jogador aprender.

---

## 3. Modelo de aplicação

### 3.1 Dano de Referência (DRef)

Valores de status e Resonance são expressos em % de **DRef**:

```
DRef = Base da arma equipada do aplicador
   × (1 + Σ Increased aplicáveis às tags do efeito)
   × Π (1 + More aplicáveis)
```

- Para efeitos de uma afinidade: Increased da afinidade + Increased genéricos de status (Damage Over Time para DoTs, Area Damage para bursts em área). Skill Damage **não** se aplica a status nem a Resonance.
- Para Resonances (duas afinidades): usa a **média** dos Increased das duas afinidades + genéricos aplicáveis (evita double-dip).
- Depois de calculado, o DRef passa por Crit (quando permitido), mitigação do alvo e Vulnerability conforme o pipeline de 06-combate.

### 3.2 Status Potency e Buildup

Cada golpe com afinidade carrega **Status Potency (SP)**. O SP preenche a barra de buildup do status primário daquela afinidade no alvo; ao atingir o limiar, aplica 1 stack (o excedente transborda para o próximo stack).

SP padrão por tipo de golpe (08-herois pode ajustar por habilidade, mantendo a ordem de grandeza):

| Tipo de golpe | SP padrão |
|---|---:|
| Basic | 15 |
| Skill (não Basic) | 30 |
| Heavy | 40 |
| Ultimate | 100 |
| Tick de DoT / zona | 0 (não aplica buildup) |
| Projétil secundário, chain, Repeat | 50% do SP de origem |
| Evento geração 2 | 50% do SP calculado |

**Status Potency (atributo, 01):** multiplica o SP de todos os golpes do jogador: `SP efetivo = SP padrão × (1 + Status Potency%)`. Faixas, slots e teto são definidos em 01-atributos.

**Golpes com múltiplas afinidades:** cada afinidade presente recebe `SP × fração do dano daquela afinidade`. Frações abaixo de 10% são ignoradas.

**Decaimento do buildup:** barras parciais decaem 25% do limiar por segundo após 1,5 s sem receber SP.

### 3.3 Resistência a status

`LimiarEfetivo = LimiarBase × MultiplicadorDeResistência`

| Categoria de alvo | Multiplicador | Equivalente |
|---|---:|---|
| Inimigo normal | ×1,0 | 0% |
| Elite | ×1,5 | ~33% |
| Guardian (09) | ×2,0 | 50% |
| Chefe | ×2,5 | 60% |
| Jogador | ×1,0 base, reduzido por Status Resistance (fórmula em 01) | — |

Inimigos com tema de afinidade (09) podem ter ×1,5 adicional apenas na própria afinidade (ex.: criaturas Ember em The Ashen Wastes), nunca imunidade.

### 3.4 Interação com atributos (01 detalha números)

- **Damage Over Time:** Increased para Burning, Decay e ticks de zonas.
- **Duration of Effects:** aumenta duração de status aplicados pelo jogador; **não** afeta Freeze nem ICDs. Teto de +50% sobre a duração base.
- **Area Damage:** Increased de bursts em área (Explosion, Shatter, Overload, Implosion, Detonation, Resonances em área).
- **Status Resistance (jogador):** reduz buildup recebido e duração de status inimigos.
- **Shield Strength:** escala Absorption e Frozen Armor (parte escudo).
- **Summon Damage/Duration:** aplica-se a Clone (Echo).

---

## 4. Os 24 status de afinidade

Legenda — **Aplicação:** B = Buildup (limiar de SP), C = Chance, G = Garantida (pela fonte). **Tipo:** Status (no alvo), Proc (evento instantâneo), Consumo (remove status para gerar efeito), Zona, Auto (buff no jogador). ★ = status primário da afinidade.

**Cadência (R19):** todos os DoTs e zonas deste documento ticam a cada **0,5 s** (06). Valores "por segundo" são divididos em 2 ticks iguais. A granularidade de 0,25 s de 11 é apenas o passo do processamento em lote.

### 4.1 Ember — dano contínuo, explosões, propagação

| Efeito | Tipo | Aplicação | Duração | Stacks | Efeito |
|---|---|---|---|---|---|
| ★ **Burning** | Status DoT | B — 25 SP/stack | 4 s (renova a cada stack) | máx. 10 | 8% DRef Ember por stack por segundo (80% DRef/s em 10). |
| **Ignition** | Status | Automático ao atingir 10 Burning, ou G | 3 s | — | A cada 1 s espalha 3 Burning para até 3 inimigos em 3 m. Ao terminar, Burning cai para 5. ICD por alvo: 6 s. |
| **Explosion** | Proc | Inimigo morre com ≥5 Burning | Instantâneo | — | Explosão 3 m: 60% DRef Ember + 3 Burning nos atingidos. Explosões de geração 2 não explodem de novo. |
| **Combustion** | Consumo | G (Heavy Ember com modificador, itens, Resonance) | Instantâneo | — | Consome todos os Burning: dano = dano restante do Burning × 1,5, de uma vez. Pode crit. |

### 4.2 Frost — controle, lentidão, congelamento

| Efeito | Tipo | Aplicação | Duração | Stacks | Efeito |
|---|---|---|---|---|---|
| ★ **Slow (Chill)** | Status | B — 20 SP/stack | 3 s | máx. 5 | −8% Movement e Attack Speed por stack (máx. −40%). Não confundir com Slow genérico de 06. |
| **Freeze** | Status (CC duro) | B — com 5 Chill, cada SP Frost adicional enche a barra de Freeze (limiar 60) | 1,5 s (normal) | — | Alvo paralisado. Grupo Hard CC de 06: DR de CC compartilhada com Stun (janela 8 s: 100/50/25%/imune). Sem imunidade própria; Chill continua aplicável. Não escala com Duration of Effects (exceção de Ultimate em §4.8). |
| **Shatter** | Consumo | Golpe Heavy ou crítico em alvo Frozen; ou morte de alvo Frozen | Instantâneo | — | Encerra Freeze. Burst 150% DRef Frost em 2,5 m (100% se por morte). Pode crit. |
| **Frozen Armor** | Auto | G ao aplicar Freeze ou Shatter | 4 s | máx. 2 | +10% Damage Reduction por stack; atacantes corpo a corpo recebem 1 Chill. |

### 4.3 Storm — velocidade, encadeamento, sobrecarga

| Efeito | Tipo | Aplicação | Duração | Stacks | Efeito |
|---|---|---|---|---|---|
| ★ **Shock** | Status | B — 20 SP/stack | 4 s | máx. 10 | +2% dano **Storm** recebido por stack (máx. +20%; entra no bucket de Vulnerability, só para Storm). |
| **Chain Lightning** | Proc | C — 15% em golpes Storm (fontes podem aumentar; máx. 60%) | Instantâneo | — | Encadeia para 2 inimigos em 6 m, 40% do dano do golpe, aplica 1 Shock. Chains não encadeiam (geração +1). ICD por fonte: 0,2 s. |
| **Overload** | Proc | Automático ao atingir 10 Shock | Telegrafado 0,5 s | — | Descarga 4 m: 200% DRef Storm + Stun 0,5 s (regras de 06). Zera Shock. ICD por alvo: 5 s. |
| **Static Field** | Zona | Criado por Overload ou G | 4 s | máx. 3 por jogador | Raio 3 m. Inimigos dentro: 1 Shock a cada 0,5 s. Aliados: +10% Movement Speed. |

### 4.4 Void — agrupamento, implosão, manipulação espacial

| Efeito | Tipo | Aplicação | Duração | Stacks | Efeito |
|---|---|---|---|---|---|
| **Pull** | Deslocamento | G (habilidade/item define força) | 0,3 s padrão | — | Puxa até 4 m em direção a um ponto. Categoria Knockback em 06. Pode mover alvos Frozen. |
| **Implosion** | Proc | Ao término de uma zona Void (Void Orb, Shattered Reality, Devouring Rift, itens) | Instantâneo | — | 100% DRef Void + 10% por inimigo dentro (máx. +100%). Aplica 2 Corruption. |
| ★ **Corruption** | Status | B — 25 SP/stack | 5 s | máx. 5 | −6 pontos percentuais de resistência Void e Ruin por stack (máx. −30 p.p.). Piso de resistência resultante: definido por 01 (proposta: −20%). |
| **Absorption** | Auto | Matar alvo com Corruption, ou Implosion atingindo ≥3 inimigos | 5 s | — | Escudo Void = 4% Maximum Health por inimigo (máx. 20%). Escala com Shield Strength. |

### 4.5 Echo — cópias, repetição, efeitos atrasados

| Efeito | Tipo | Aplicação | Duração | Stacks | Efeito |
|---|---|---|---|---|---|
| **Repeat** | Proc | C ou G (fonte define) | Repete após 0,4 s | — | Repete o golpe a 35% do dano (modificadores: 25–50%). O golpe repetido é **Echoed** (Proc Coefficient 0,5): aplica status (SP 50%) mas nunca Repeat/Clone/eco de zona, salvo Echo Recursion 1× (§7.1). |
| **Clone** | Invocação | G | 4 s padrão (Nyx: definido em 08) | máx. 3 por jogador | Cópia que replica as próximas ações do jogador a 50% (versão genérica de item: replica Basic a 30%, 6 s). Não copia Ultimates. Todos os golpes de Clone são Echoed (Proc Coefficient 0,4). |
| **Reflection** | Auto | G (janela de 1 s após habilidade Defensive Echo) ou C passiva 10% contra projéteis | — | — | Reflete o golpe/projétil recebido: 100% do dano recebido + 50% como Echo. ICD 0,5 s. |
| ★ **Delayed Effect (Echo Mark)** | Status | B — 40 SP | 3 s | 1 por alvo | Armazena 25% de todo dano recebido pelo alvo durante a marca; libera como dano Echo ao expirar. Teto: 15% Max Health (normal/Elite) ou 300% DRef (Chefe). Reaplicar não libera antes da hora. Liberação não pode crit e é geração +1. |

### 4.6 Ruin — enfraquecimento, deterioração, detonação

| Efeito | Tipo | Aplicação | Duração | Stacks | Efeito |
|---|---|---|---|---|---|
| ★ **Decay** | Status DoT | B — 25 SP/stack | 6 s | máx. 5 | 5% DRef Ruin por stack por segundo, **+20% de dano por segundo ativo** (máx. +100% após 5 s). Novos stacks não resetam a rampa. |
| **Vulnerability** | Status | G ou C (fonte define) | 5 s | máx. 5 | +4% dano recebido de todas as fontes por stack (máx. +20%). Bucket de Vulnerability. |
| **Weakening** | Status | G ou C | 5 s | máx. 4 | −5% dano causado pelo alvo por stack (máx. −20%; Chefes máx. −10%). |
| **Detonation** | Consumo | G (habilidade/modificador/Resonance) | Instantâneo | — | Consome todos os debuffs Ruin: Decay × 40% DRef + Vulnerability × 20% DRef + Weakening × 20% DRef, em 2,5 m ao redor do alvo, dano Ruin. Pode crit. ICD por alvo: 3 s. |

### 4.7 Bucket de Vulnerability (dono: este doc — R12)

Termo `(1 + Vulnerability%)` do pipeline (00 §3.2). Somatório no alvo, **teto total +60%** (06 cita este teto; não há outro).

| Fonte | Forma | Valor | Dono do valor |
|---|---|---|---|
| Ruin Vulnerability | Stacks | 4% × até 5 (+20%) | 07 |
| Shock | Stacks, só dano Storm | 2% × até 10 (+20%) | 07 |
| Brittle (Resonance) | Altera stacks de Ruin enquanto Frozen | 6% × até 5 (+30%) | 07 |
| Distortion (chefe) | Fixo | +10% Void, 3 s | 07 |
| Staggered | Fixo | conforme 06 | 06 |
| Broken (chefe) | Fixo | +30% por 4 s | 06 |
| Conversão de Execute contra chefe | Fixo, abaixo de 20% de vida | +25% (uma única conversão, não acumula entre fontes) | 06 (R08) |
| Fontes de 02/03/04/08/09 (Ruinbreaker usa os stacks de Ruin, não fonte própria) | Fixo por fonte | declarado pela fonte | doc da fonte |

Regras:
1. **Apenas Ruin (e Shock, restrito a Storm) usa stacks.** Toda outra fonte dá um valor fixo declarado pela própria fonte.
2. **Mesma fonte não acumula consigo mesma:** reaplicar renova a duração; vale o maior valor ativo daquela fonte.
3. **Fontes distintas somam** até o teto de +60%. O excedente é descartado, não convertido.
4. Shock conta no teto apenas quando o golpe é Storm.

### 4.8 Exceções declaradas (R49)

| Exceção | Fonte | Regra |
|---|---|---|
| Freeze longo de Ultimate | Absolute Zero (Lyra, 08) e qualquer Ultimate que aplique Freeze | Freeze de até **3 s** em inimigos não-chefes. Continua sujeito à DR de CC de 06 (a 2ª aplicação na janela dura 50%). Em chefes vira Frostbound normal. |
| Shock acima do máximo | Overload Circuit (08) | Máximo de Shock passa de 10 para **12** (+24% Storm). Overload dispara ao atingir o novo máximo. Teto do bucket de Vulnerability (+60%) continua valendo. |
| Echo Recursion 1× | Ver §7.1 | Única exceção à regra "réplica não replica". |
| Limite de stacks em inimigos | Status aplicados por inimigos ao jogador (09) | Os mesmos máximos de §4 valem para inimigos (ex.: Decay máx. 5 stacks — Rotting Herald incluído). Nenhum inimigo excede o máximo. |

---

## 5. As 15 Resonances

### 5.1 Regra geral de ativação

Uma Resonance dispara quando um alvo **com status da afinidade A** recebe um **evento qualificado da afinidade B** (golpe, proc, consumo ou fim de zona, conforme a tabela). Salvo indicação, a ordem A/B é indiferente.

- Só eventos de geração 0 ou 1 disparam Resonance.
- Uma Resonance por evento por alvo (prioridade em §6).
- Cada Resonance tem ICD **por alvo**; o teto global (§7) se aplica por cima.
- Dano de Resonance usa DRef com a média dos Increased das duas afinidades.
- Valores em **More%** são declarados explicitamente (orçamento de 00-convencoes §4).

### 5.2 Matriz

| # | Par | Nome | Gatilho | Efeito | Valor | ICD por alvo | Limites |
|---|---|---|---|---|---|---|---|
| 1 | Ember + Frost | **Thermal Shatter** | Alvo com ≥3 Burning é congelado, **ou** alvo Frozen recebe golpe Ember | Encerra Freeze, remove metade do Burning; burst em 3 m, 50% Ember / 50% Frost | 250% DRef | 4 s | Conta como Shatter para efeitos que reagem a Shatter. Pode crit. |
| 2 | Ember + Storm | **Combustion Discharge** | Golpe Ember em alvo com ≥5 Shock, **ou** golpe Storm em alvo com ≥5 Burning | Consome 5 Shock; arcos levam os Burning do alvo (até 5 stacks) para 3 inimigos em 5 m | 80% DRef Storm por arco | 3 s | Stacks espalhados não disparam nova Combustion Discharge no mesmo segundo. |
| 3 | Ember + Void | **Collapsing Star** | Implosion com inimigos em Burning dentro da zona | Implosion ganha bônus por inimigo em Burning e deixa chão em chamas 3 m por 3 s (2 Burning/s) | +15% More por inimigo em Burning (máx. +75% More) | 4 s (por zona) | 1 chão em chamas por zona. |
| 4 | Ember + Echo | **Afterburn** | Golpe Echoed (Repeat, Clone) em alvo com Burning; **ou** Burning expira naturalmente em alvo com Echo Mark | Golpe Echoed adiciona +2 Burning. Expiração com Echo Mark: Burning reacende com 50% dos stacks | +2 stacks / reacende 50% | Reacender: 8 s | Reacender só por expiração natural, nunca após consumo. |
| 5 | Ember + Ruin | **Cinder Rot** | Golpe Ember em alvo com ≥3 Decay | Consome Decay; burst 2,5 m Ember; aplica Burning igual ao Decay consumido | 60% DRef por Decay consumido | 3 s | Compartilha ICD com Detonation no mesmo alvo. Pode crit. |
| 6 | Frost + Storm | **Superconduct** | Golpe Storm em alvo Frozen ou com 5 Chill; **ou** Overload em alvo Frozen | Chain Lightning garantido a partir do alvo com +2 saltos, saltos aplicam 2 Chill. Overload em Frozen: +0,5 s de Freeze | Overload +50% More | 2 s (chain) / 5 s (Overload) | Extensão de Freeze conta como nova aplicação na DR de CC de 06. |
| 7 | Frost + Void | **Event Horizon** | Pull em alvo com Chill; **ou** Implosion em alvo com ≥3 Chill | Pull aplica +2 Chill. Implosion congela instantaneamente (ignora buildup de Freeze) | Freeze 1,5 s | 6 s | Respeita a DR de CC de 06. Em Chefes: aplica Frostbound. |
| 8 | Frost + Echo | **Rime Echo** | Shatter em alvo; **ou** Echo Mark aplicada em alvo Frozen | Shatter se repete 0,6 s depois no mesmo ponto (Echoed). Echo Mark em Frozen armazena 40% em vez de 25% | Eco a 50% do Shatter | 2 s | Eco não dispara Shatter/Resonance novamente. Teto do Echo Mark inalterado. |
| 9 | Frost + Ruin | **Brittle** | Alvo Frozen com Vulnerability; **ou** Detonation em alvo Frozen | Vulnerability vale 6% por stack enquanto Frozen (máx. 30%). Detonation em Frozen também executa Shatter | +50% valor de Vulnerability | 3 s (Detonation) | Teto do bucket (+60%) continua valendo. |
| 10 | Storm + Void | **Singularity Arc** | Static Field sobreposto a zona Void; **ou** Overload dentro de zona Void | Static Field é puxado ao centro e vira Charged Singularity: puxa 1 m/s e causa dano a cada 0,5 s com 1 Shock. Overload em zona Void: raio +50% | 30% DRef Storm por tick | — (por zona) | 1 Charged Singularity por jogador. |
| 11 | Storm + Echo | **Resounding Thunder** | Overload em alvo com Echo Mark; **ou** golpe Storm Echoed | Overload se repete 0,5 s depois (Echoed, não zera Shock nem aplica Stun). Chain Lightning de golpes Echoed ganha +1 salto | Eco a 60% do Overload | 5 s | Overload Echoed não cria Static Field. |
| 12 | Storm + Ruin | **Corroding Current** | Chain Lightning saltando de alvo com debuff Ruin; **ou** Overload em alvo com Decay | Cada salto copia 1 stack aleatório de Decay/Vulnerability/Weakening para o alvo atingido. Overload detona Decay a 50% sem consumir | 1 stack por salto / 50% Detonation | 0,5 s (por fonte) / 5 s | Stack copiado tem duração restante da origem. |
| 13 | Void + Echo | **Recursive Rift** | Implosion de zona Void em alvo/área com Echo Mark, ou zona Void criada por fonte Echo | Zona reaparece 0,5 s depois no mesmo ponto com 50% de duração e dano; pode implodir | 50% dano / 50% duração | — (por zona) | Zona eco é Echoed: nunca ecoa de novo. Máx. 2 zonas eco por jogador. |
| 14 | Void + Ruin | **Entropic Collapse** | Implosion em alvo com Corruption + qualquer debuff Ruin | Detonation gratuita a 50% sem consumir debuffs Ruin; consome Corruption. Com Corruption + Vulnerability, Corruption também reduz mitigação física em 3 p.p. por stack | 50% Detonation | 4 s | Compartilha ICD de Detonation. |
| 15 | Echo + Ruin | **Lingering Doom** | Echo Mark libera em alvo com Decay; **ou** Detonation em alvo com Echo Mark | Liberação reaplica Decay já na rampa máxima. Detonation libera a marca imediatamente | Detonation +50% More | 4 s | Liberação antecipada respeita o teto do Echo Mark. |

### 5.3 Pares nativos x acesso por equipamento

| Herói | Par nativo | Resonance nativa |
|---|---|---|
| Kael | Ember + Ruin | Cinder Rot |
| Lyra | Frost + Void | Event Horizon |
| Nyx | Storm + Echo | Resounding Thunder |
| Orun | Void + Ruin | Entropic Collapse |

As outras 11 Resonances (e as 3 nativas de outros heróis) precisam ser alcançáveis por **qualquer** herói via:

1. **Modificadores Elemental/Conversion (03):** cada herói deve ter, para cada uma das 6 afinidades, pelo menos uma habilidade com um modificador que adiciona ou converte dano para ela.
2. **Infusions (02):** pool de efeitos em Relics e Amulets, sem restrição de herói, nas 6 afinidades: "X% do dano adicionado como [afinidade]" e "Golpes [tag] aplicam +N SP de [afinidade]".
3. **Efeitos de status secundários:** pelo menos um item Legendary/Unique genérico (sem restrição de herói) por afinidade que concede um dos efeitos secundários (Combustion, Shatter em crit, Chain Lightning, Pull, Repeat, Detonation).
4. **Blessings de Shrine (09, dono do catálogo — R35):** Shrine Rooms oferecem Infusions temporárias da run em todas as afinidades.

Regra de validação para 02/03: para cada herói × par, deve existir uma build com no máximo 2 itens dedicados que dispare a Resonance de forma consistente (≥1 vez a cada 10 s em combate contra grupo).

---

## 6. Prioridade quando múltiplos status coexistem

### 6.1 Ordem de resolução de um evento

1. Mitigação e dano do golpe.
2. Aplicação de SP/buildup e status.
3. **Resonance** (no máximo uma, ver §6.2).
4. **Consumos** (Shatter, Combustion, Detonation) não consumidos pela Resonance.
5. **Procs** (Chain Lightning, Repeat, Explosion).
6. Auto-buffs do jogador (Frozen Armor, Absorption).

### 6.2 Prioridade entre Resonances (quando o mesmo evento qualifica várias)

`Thermal Shatter > Event Horizon > Superconduct > Brittle > Cinder Rot > Entropic Collapse > Lingering Doom > Combustion Discharge > Collapsing Star > Singularity Arc > Corroding Current > Resounding Thunder > Rime Echo > Recursive Rift > Afterburn`

Critério: controle e consumo primeiro (mudam o estado do alvo), amplificadores depois, ecos por último (dependem do resultado dos anteriores). Uma Resonance em ICD é pulada e a próxima elegível é avaliada.

### 6.3 Regras de convivência

- **Freeze x Burning:** aplicar Freeze em alvo com ≥3 Burning dispara Thermal Shatter. Se em ICD, o Freeze ocorre e Burning tique a 50% enquanto congelado.
- **Freeze x Stun:** mesmo grupo Hard CC (06), com DR de CC compartilhada. Freeze se sobrepõe a Stun ativo; o tempo restante de Stun é descartado.
- **Pull x Frozen:** permitido; o bloco de gelo desliza. Colisão com parede não causa dano extra.
- **Ignition x Combustion:** Combustion durante Ignition encerra Ignition e consome tudo.
- **Echo Mark x Detonation/Shatter:** dano de consumo é armazenado normalmente pela marca (antes da liberação).
- **Corruption x resistências:** redução aplicada antes de Armor Penetration (06 confirma a ordem final).
- **Chill x Slow genérico:** não somam além de −60% de Movement Speed total no alvo (o maior valor de Attack Speed reduzido vale, não somam).

---

## 7. Limites anti-explosão

| Limite | Valor | Comportamento |
|---|---|---|
| Generation | Gen 0–2 | Ação direta = Gen 0. Cada proc (gatilho, Resonance, chain, explosão, eco) = origem + 1. **Nada dispara em Gen ≥ 2**: Gen 2 aplica status a 50% SP e não dispara procs, Resonances, consumos nem gatilhos. (11: a checagem é `Gen ≥ 2 → não avalia triggers`.) |
| Flag Echoed (`IsEcho`) | — | Réplicas nunca geram novas réplicas: eventos Echoed nunca disparam Repeat, Clone, Rime Echo, Recursive Rift ou Resounding Thunder (exceto o bônus de salto explicitado e Echo Recursion 1×, §7.1). |
| Proc Coefficient | Echo 0,5 / Clone 0,4 / tick de área 0,25 | Multiplica a chance de procs e o SP dos eventos derivados. |
| Resonance por evento por alvo | 1 | §6.2. |
| Procs de Resonance por jogador | 8/s | Excedente é descartado (o de menor dano estimado primeiro). Não é enfileirado. |
| Explosions (Ember) | 4/s por jogador | Excedente: inimigo morre sem explodir. |
| Chain Lightning | ICD 0,2 s por fonte, máx. 6 saltos totais por evento | Somando todas as fontes de salto adicional. |
| Zonas simultâneas | Static Field 3, Charged Singularity 1, zonas eco 2, chão em chamas 1 por zona Void | Ao exceder, a mais antiga some. |
| Clones | 3 por jogador | O mais antigo expira. |
| Stacks | Conforme §4 | Nunca ultrapassam o máximo; fontes que dão "+stacks" só aceleram o preenchimento. |
| Vulnerability total | +60% | §4.7. |
| Duration of Effects | +50% sobre base | Não afeta Freeze nem ICD. |
| Echo Mark | 15% Max Health / 300% DRef em Chefe | Teto por marca. |

### 7.1 Echo Recursion 1× (R07)

Única exceção à regra "réplica não replica". Só existe quando o poder a declara explicitamente com o rótulo **Echo Recursion 1×**. Poderes com o rótulo no lançamento:

| Poder | Doc |
|---|---|
| Infinite Reflection (Thousand Echoes, bônus de 6 peças) | 04 |
| Echo Chamber (Keystone de Nyx) | 08 |

Regras:
1. Uma réplica (Gen 1) pode gerar **uma** réplica adicional. A réplica da réplica recebe a flag `EchoRecursed` e nunca replica.
2. A recursão continua sujeita à Generation: a réplica da réplica é Gen 2 — é executada (dano e status a 50% SP), mas não dispara procs, Resonances, consumos nem gatilhos.
3. Proc Coefficient se multiplica em cadeia (Echo 0,5 × 0,5 = 0,25).
4. Várias fontes de Echo Recursion 1× não se somam: no máximo 1 recursão por evento original.

Teste de sanidade obrigatório (11-arquitetura deve automatizar): sala com 40 inimigos, build com todas as fontes Echo + Storm + Ember maximizadas; o número de eventos de dano por segundo não pode passar de 400 e nenhuma cadeia pode exceder geração 2.

---

## 8. Elites e Chefes

### 8.1 Elites
- Limiar ×1,5 (§3.3).
- Freeze: duração 1,0 s; sem imunidade própria — DR de CC de 06.
- Execute: limiar à metade (máx. 10%), conforme 06 (R08).
- Pull: 50% da distância.
- Weakening: máximo −20% (igual a normal).
- Afixos de Elite (09) podem conceder **imunidade a no máximo 1 status primário**, sempre telegrafada visualmente (aura da cor da afinidade com símbolo cortado). Nunca imunidade a DoT em geral.

### 8.2 Guardians e Chefes
- Limiar ×2,0 (Guardian) / ×2,5 (Chefe).
- **Execute:** Guardians e chefes são imunes. Contra chefes, efeitos de Execute viram +25% Vulnerability abaixo de 20% de vida (06, R08), dentro do teto de §4.7.
- **Regra geral (06):** CC em chefe é convertido em Stagger. As duas exceções nomeadas abaixo são definidas aqui e incorporadas por 06 (R10). Os nomes **Frostbound** e **Distortion** são reservados a este doc.
- **Freeze → Frostbound:** ao completar a barra de Freeze, o chefe recebe Slow 50% por 1,0 s e fica **Shatterable** (o próximo Shatter/Thermal Shatter funciona normalmente). Sem imunidade própria: Frostbound segue a DR de CC de 06 (janela 8 s: 100/50/25%/imune).
- **Stun (Overload) → Stagger:** convertido em Stagger Damage conforme 06; a descarga causa dano normal.
- **Pull → Distortion:** chefe não se move; recebe **+10% Vulnerability a dano Void** por 3 s (conta no teto de +60%; não é More). ICD por alvo 6 s.
- **Weakening:** máximo −10%.
- **Echo Mark:** teto 300% DRef.
- **Vulnerability:** normal (cap do bucket aplica).
- Chefes podem ter **fases de resistência** (09): ×1,5 adicional em uma afinidade por fase, nunca imunidade.
- Toda Resonance que atinge um chefe soma Stagger Damage bônus (06 define o valor; proposta: +25% do Stagger do golpe).

### 8.3 Status aplicados ao jogador
- Mesmo vocabulário e cores, com contorno vermelho (hostil).
- Freeze no jogador: 1,0 s máximo (Hard CC em geral: 1,5 s máximo); depois, 2 s de imunidade a Hard CC conforme 06 (R09). Inputs de movimento reduzem 0,1 s por toque (máx. −0,4 s).
- Burning/Decay/Corruption no jogador: valores definidos pelo inimigo em 09, sempre dentro dos máximos de stacks de §4 (ex.: Decay máx. 5); Status Resistance reduz buildup e duração (01).
- Inimigos não aplicam Echo Mark, Clone, Reflection, Absorption, Frozen Armor nem Ignition ao jogador (só inimigos especiais de chefe, documentados em 09).

---

## 9. Descoberta — Resonance Codex (Archivist)

1. **Codex no Archivist** com 15 entradas de Resonance e 24 de status. Status começam revelados; Resonances começam como silhueta "???" com a dica das duas afinidades (ícones).
2. **Primeira ativação** em qualquer run: banner discreto no HUD "RESONANCE DESCOBERTA — Thermal Shatter" (1,5 s, sem pausar), entrada desbloqueada com gatilho, valores, limites e um clipe curto.
3. **Recompensas de descoberta:** fragmento de lore do Archivist por Resonance; ao completar as 15, recompensa cosmética (efeito visual de Resonance alternativo). Nenhuma recompensa de poder.
4. **Training Grounds:** Resonances descobertas podem ser reproduzidas em bonecos com status pré-aplicados selecionáveis.
5. **Tela de builds (GDD §11.2), aba Resonance:** mostra quais Resonances a build atual consegue disparar (calculado por tags/afinidades das habilidades e itens), estimativa de frequência no Training Grounds e quais itens contribuem.
6. **Comparação de equipamentos (GDD §11.1):** item que habilita uma Resonance nova para a build atual mostra selo "Habilita: Superconduct".

---

## 10. Legibilidade visual e sonora

### 10.1 Paleta e linguagem por afinidade

| Afinidade | Cor principal | Cor secundária | Forma/linguagem | Assinatura sonora |
|---|---|---|---|---|
| Ember | Laranja-fogo `#FF6A1A` | Brasa `#FFC23D` | Chamas ascendentes, partículas subindo | Crepitar, estalo grave |
| Frost | Ciano-gelo `#9EE7FF` | Branco `#F2FBFF` | Cristais angulares, crosta | Rangido cristalino, tilintar |
| Storm | Amarelo-elétrico `#F5E663` | Violeta-claro `#B49CFF` | Linhas quebradas, arcos | Estalido seco, zumbido |
| Void | Violeta-profundo `#5A2BBF` | Borda magenta `#E040FB` | Espirais para dentro, bordas negras | Sucção, grave invertido |
| Echo | Turquesa-prata `#6FFFE0` | Prata `#D8E3E8` | Anéis concêntricos, afterimages translúcidas | Reverb, ataque duplicado |
| Ruin | Verde-ocre `#8FBF3A` | Oliva escuro `#4A5A1E` | Rachaduras, partículas caindo | Corrosão, chiado úmido |

### 10.2 Representação por status

| Status | No alvo | Ícone (sobre barra de vida) | Telegrafia |
|---|---|---|---|
| Burning | Chamas proporcionais aos stacks | Chama + número | Intensidade cresce por stack |
| Ignition | Aura pulsante a cada 1 s | Chama dupla | Pulso visível antes do espalhamento |
| Explosion | Flash + anel 3 m | — | Anel no chão 0,1 s antes |
| Combustion | Implosão de chamas → burst | — | — |
| Chill | Crosta nos pés, brilho ciano | Floco + número | Crosta sobe por stack |
| Freeze | Bloco de gelo envolvendo | Cristal | Som de congelamento distinto |
| Shatter | Fragmentos radiais | — | — |
| Frozen Armor | Placas de gelo no jogador | Escudo de gelo | — |
| Shock | Faíscas intermitentes | Raio + número | Frequência sobe por stack |
| Chain Lightning | Arco entre alvos | — | — |
| Overload | Esfera crescente + anel de 4 m | Raio circulado | 0,5 s de aviso obrigatório |
| Static Field | Disco elétrico no chão | — | Borda nítida |
| Pull | Linhas espirais na direção do ponto | — | — |
| Implosion | Contração da zona → flash | — | Zona escurece 0,3 s antes |
| Corruption | Veias negras com borda magenta | Olho fechado + número | — |
| Absorption | Casca violeta no jogador | Escudo Void | — |
| Repeat | Afterimage do golpe | — | — |
| Clone | Silhueta turquesa translúcida | — | Contorno sempre visível |
| Reflection | Brilho prateado no jogador | Espelho | Janela de 1 s visível |
| Echo Mark | Anéis concêntricos girando, preenchendo | Anéis | Anéis fecham conforme expira |
| Decay | Partículas caindo, pele rachada | Gota + número | Cor escurece com a rampa |
| Vulnerability | Runa rachada no peito | Escudo rachado + número | — |
| Weakening | Correntes caídas | Espada partida + número | — |
| Detonation | Rachaduras → burst verde | — | — |

### 10.3 Regras de legibilidade
- Máximo de **4 ícones** sobre a barra de vida de um inimigo; prioridade: CC (Freeze/Overload iminente) > consumíveis (Echo Mark) > maior stack.
- Status nunca são diferenciados apenas por cor: cada um tem forma e ícone próprios (modos de daltonismo trocam paleta mantendo formas).
- VFX de status do jogador têm opacidade reduzida em densidade alta conforme a regra de opacidade de 06 (R47) e respeitam a opção "Effects Intensity"; **telegrafias inimigas nunca são reduzidas**. As cores de afinidade acima não se confundem com a linguagem de telegraph de 06 (laranja é reservado a Ember; vermelho, roxo, branco-dourado e ciano são de telegraph).
- Status aplicados ao jogador: contorno vermelho e ícone no HUD do jogador.
- Banner de Resonance no mundo: nome curto flutuante (0,6 s) apenas nas 3 primeiras ativações de cada Resonance por run, para não poluir.

---

## 11. Dependências e pontos de integração

| Doc | O que consome deste doc | O que este doc precisa dele |
|---|---|---|
| 01-atributos | Lista de interações de atributos (§3.4); uso de Status Potency (§3.2) | Fórmula de Status Resistance do jogador; piso de resistência (proposta −20%); atributo **Status Potency** (R30: faixas, slots, teto). |
| 02-equipamentos | Requisito de Infusions e itens genéricos por afinidade (§5.3); nomes reservados (Frostbound, Brittle, Event Horizon, Frozen Armor) | Pool de Infusions em Relics/Amulet; ≥1 item genérico por efeito secundário; valor fixo de Vulnerability de cada item (§4.7). |
| 03-modificadores | Efeitos secundários concedidos por modificadores; modelo Generation e Proc Coefficient (Echo 0,5 / Clone 0,4 / tick 0,25) | Modificadores Elemental/Conversion para as 6 afinidades em cada herói; modificadores Trigger que reagem a Resonance; valores de Vulnerability por fonte. |
| 04-conjuntos | Vocabulário de status e Resonances; rótulo Echo Recursion 1× (Infinite Reflection) | Declaração de More% e Vulnerability fixa adicionados por Set Powers. |
| 05-loot | — | Infusions de drops alinhadas às regiões. |
| 06-combate | DRef, ordem de resolução §6.1, modelo Generation/Echoed, teto de Vulnerability +60%, exceções Frostbound/Distortion | DR de CC (grupo Hard, janela 8 s), regras de CC no jogador, Stagger/Broken, conversão de Execute, Knockback, ordem final de mitigação, tick 0,5 s, regra de opacidade de VFX. |
| 08-herois | SP padrão (§3.2), status primários, exceções §4.8 (Absolute Zero, Overload Circuit), Echo Recursion 1× (Echo Chamber) | SP por habilidade, Clone de Nyx, Implosions de Lyra/Orun. |
| 09-inimigos | Multiplicadores de resistência, regras de Elite/Guardian/Chefe, status contra jogador, máximos de stacks (Decay 5) | Afixos de imunidade, fases de resistência, inimigos com tema de afinidade; catálogo de Shrines/Blessings (R35). 09 deve renomear seu próprio "Frostbound" (R10/R48). |
| 10-progressao | Recompensas do Codex | Codex no Archivist. |
| 11-arquitetura | Metadados de evento (Generation, `IsEcho`, `EchoRecursed`, SP, afinidades com frações), filas de procs, teste de sanidade | Event bus de dano, sistema de status data-driven (ScriptableObject por status e por Resonance); checagem `Gen ≥ 2 → não avalia triggers` (corrige off-by-one, R07); tick 0,5 s com lote de 0,25 s (R19); DR de CC única. |

---

## 12. Conflitos com as convenções

Nenhum conflito em aberto. Esclarecimentos:
- `Echoed`/`IsEcho`, `EchoRecursed` e `Generation` são **metadados de evento / HitFlags** (R02), não tags de habilidade (não alteram o vocabulário de §3.4 das convenções).
- O bucket `(1 + Vulnerability%)` recebe teto de +60% e uma parcela restrita a Storm (Shock) — refinamento, não contradição.
- Resonance contribui majoritariamente como **instâncias de dano adicionais** (dentro do orçamento de ~10%); os únicos More% introduzidos aqui são locais a um único evento: Collapsing Star (até +75% sobre a Implosion), Superconduct (+50% sobre Overload) e Lingering Doom (+50% sobre Detonation). Distortion deixou de ser More (agora +10% Vulnerability).
- **Nomes reservados a este doc (R48):** Frostbound, Brittle, Event Horizon, Frozen Armor, Distortion. Outros docs que usem esses nomes para outra coisa devem renomear.

Resoluções do registro 00 §5 aplicadas nesta rodada: R07 (Generation, Echo Recursion 1×, Proc Coefficient), R09 (imunidades próprias removidas, DR de CC de 06), R10 (Frostbound e Distortion), R12 (teto +60%, stacks × valor fixo por fonte), R19 (tick 0,5 s), R30 (Status Potency de 01), R35 (Blessings de 09), R47 (opacidade de VFX de 06), R48 (sigla DRef, Guardian, nomes reservados), R49 (exceções Absolute Zero, Overload Circuit, máximos de stacks de inimigos).

---

## 13. Questões abertas para playtest

1. Limiares de SP (20–40) e decaimento de buildup: validar se o tempo até primeiro Freeze em Elite fica entre 2–4 s numa build Frost dedicada.
2. Teto global de 8 procs/s: validar leitura visual e performance em salas de 40+ inimigos.
3. Frequência de Resonances com 2 itens dedicados (meta: ≥1 a cada 10 s) para cada herói × par.
4. Participação real de Resonance no dano total (meta ~10%; alarme acima de 20%).

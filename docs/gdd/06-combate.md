# PROJECT ECLIPSE — 06 · Sistema de Combate

**Versão:** GDD v0.3 · **Dono:** Combat Design · **Contrato:** `00-convencoes.md`

---

## 1. Objetivo e escopo

Definir o funcionamento completo do combate em tempo real: pipeline de dano (ofensivo e defensivo), controles e input, frame data por arma, cancelamentos, esquiva, estados genéricos (Stun, Stagger, Knockback, Root, Silence, Slow genérico, Super Armor, Unstoppable, Taunt, Stealth, Parry, Intangível, Execute) com diminishing returns (DR), hit detection, linguagem de telegraph e ameaça dos inimigos, game feel (hit-stop, screen shake, números, câmera, hooks de VFX/SFX), metas de TTK e letalidade, regras de morte/revive.

Fora de escopo (apenas referenciado): fórmulas de Armor/Resist e caps de atributos (01), DPS de referência e dano por golpe das armas (02), status de afinidade, Vulnerability e combinações (07), kits e movesets dos heróis (08), comportamentos de IA, tabela de attack tokens e arenas (09), economia e serviços de Rest (10), implementação e orçamento de performance (11).

Este documento aplica as resoluções R02, R04–R20, R47 e R48 do registro `00-convencoes.md §5`.

---

## 2. Decisões tomadas

1. **Lógica de combate em tick fixo de 60 Hz**; toda frame data é expressa em frames a 60 fps e Attack Speed 1.0. — Determinismo, frame data legível e independente do framerate de render.
2. **Movimento WASD + mira no cursor (twin-stick)**, sem click-to-move. — Combate de ação com posicionamento é pilar; click-to-move conflita com esquiva e mira.
3. **Habilidades escalam por Weapon Power (DPS normalizado da arma), ataques básicos por dano por golpe.** — A escolha da arma muda o estilo, não o dano das habilidades; armas lentas não viram "armas de skill" obrigatórias.
4. **DPS de ataque básico equivalente entre armas, com o número definido por 02** (±5% entre armas do mesmo herói; armas de área −10% contra alvo único; especialistas em alvo único +5%). — 02 é dono do valor; 06 só garante que a frame data respeita o APS de 08.
5. **Conversão de dano: o dano convertido só se beneficia de modificadores do tipo final** (mais os genéricos de tag). Sem conversão em cadeia. — Tooltip simples e conversão vira decisão real de build.
6. **DoTs fazem snapshot na aplicação, tickam a cada 0,5 s e não critam por padrão.** — Previsível, barato de simular e dá valor a "aplicar forte e detonar" (Ruin).
7. **Sem falloff de dano em área.** Algumas habilidades podem ter "epicentro" explícito. — Legibilidade: o que o círculo mostra é o que acontece.
8. **Hitboxes lógicas (setor/caixa/cápsula) ancoradas no personagem e ativadas por janelas de frame data**, com sweep entre ticks; não usamos colliders de malha da arma. — Determinístico, autorável por dados, imune a variações de animação.
9. **Esquiva universal idêntica para todos os heróis:** 2 cargas, 4,5 m fixos, i-frames nos frames 2–13, recarga 1,5 s por carga (R06). Cargas ou distância extras só por fonte explícita de item/talento. — Segurança de balanceamento das i-frames; a mobilidade de herói vem de habilidades.
10. **Perfect Dodge** (esquivar até 6 frames antes do impacto) concede recurso do herói e um hook de Trigger, **sem slow-motion**. — Recompensa habilidade sem quebrar o ritmo.
11. **Barra de Stagger em moeda não escalada por nível**; chefes e Guardians Colossal são imunes a CC duro e convertem CC em Stagger, com duas exceções nomeadas de 07: **Frostbound** (Freeze) e **Distortion** (Pull) (R10). — Builds de controle (Permafrost, Abyssal Guardian) continuam úteis contra chefes.
12. **Procs seguem o modelo Generation de 07** (R07): evento original = Gen 0; cada proc gera Gen+1; nada dispara em Gen ≥ 2. Réplicas (`IsEcho`) nunca geram novas réplicas, exceto poderes que declaram "Echo Recursion 1×". — Evita explosão combinatória e de performance com uma única regra para engine e design.
13. **Grace period de 0,25 s após qualquer hit recebido** e 0,8 s após hit ≥ 20% da vida máxima. — Hordas não "trituram" o jogador por sobreposição de hits.
14. **Regra anti-one-shot:** nenhum ataque não-mecânico pode causar mais de 60% da vida máxima com defesas recomendadas. — Morte deve ser compreensível.
15. **Câmera perspectiva de FOV baixo (30°)**, pitch 50°, yaw 45° fixo. — Profundidade e escala monumental com leitura quase isométrica.
16. **Hit-stop local (atacante + alvo)**, nunca global, exceto Ultimates, quebra de Stagger de chefe e troca de fase. — Peso sem travar a leitura do restante da tela.
17. **Aether Flask:** cura ativa com 3 cargas por run. — Recuperação por decisão do jogador, alinhada a Rest Room e economia de run.
18. **Input fixo (R05):** LMB básico, RMB forte, Space esquiva, Q/E/R/Shift = 4 habilidades (Shift = slot de mobilidade/utilidade), F = Ultimate, 1 = Aether Flask. — Memória muscular compartilhada entre heróis, alinhada a 08.
19. **Execute é uma HitFlag (R02, R08):** limiar máximo 20% em comuns, metade em Elites (máx. 10%), Guardians e chefes imunes; contra chefe vira **+25% Vulnerability abaixo de 20% de vida**, uma única conversão. — Execução mantém valor sem trivializar chefes e sem criar fonte More.
20. **Vulnerability tem teto total de +60%, definido por 07** (R12); Staggered, Broken, Distortion e a conversão de Execute entram nesse teto. — Um único bucket de debuff, orçado num só lugar.
21. **Mitigação adota 01 (R13):** Armor `Armor/(Armor + 50·eLvl + 500)`, Block −40%, cap de Damage Reduction 40%. `Unavoidable` ignora Dodge Chance e Block, mas i-frames evitam; só **Annihilation** exige sair da área. — Esquiva ativa sempre tem valor; defesa passiva não anula mecânicas.
22. **Parry unificado (R20):** janela de 0,2 s para toda ação defensiva de parry (Ashen Guard, Perfect Guard, bloqueio perfeito), teto 0,3 s com bônus. — Um único conceito, uma única regra de UI e de engine.
23. **Linguagem de telegraph de 06 é única no jogo (R14):** vermelho = padrão, roxo = controle, branco-dourado = Unblockable, ciano = janela interrompível; laranja reservado a Ember. — Legibilidade; 09 aplica.
24. **Attack tokens seguem a tabela de 09 (R17)**; este doc só define o comportamento de quem não tem token. — 09 é dono dos números por arquétipo e dificuldade.

---

## 3. Corpo do design

### 3.1 Tipos de dano e mitigação

| Tipo de dano | Mitigado por | Observação |
|---|---|---|
| Physical | Armor (após Armor Penetration) e Physical Resistance | Duas camadas: `1 − (1 − ArmorDR) × (1 − PhysRes)` |
| Ember, Frost, Storm | Elemental Resistance (+ resistência específica de inimigo, se houver) | |
| Void, Echo, Ruin | Void Resistance | Grupo "Primordial" |

- Fórmula de `ArmorDR` e caps de resistência: **01-atributos** (adotada aqui, R13): `ArmorDR = Armor / (Armor + 50 × eLvl + 500)`, hard cap 75%, onde eLvl é o nível do atacante (Monster Level de 10 para inimigos). Physical Resistance é % direto (cap 30%, 01).
- Um hit pode ter **vários componentes de dano** (ex.: 60% Physical + 40% Ruin). Cada componente passa pela própria mitigação; o crit é rolado **uma vez por hit** e aplicado a todos os componentes.

### 3.2 Pipeline de dano ofensivo (ordem exata)

Executado por componente de dano, por hit:

```
1. Base
   Ataque básico/forte: DanoDaArma (rolado entre min–max) × CoefDoGolpe
   Habilidade:          WeaponPower × CoefDaHabilidade
   + Dano plano adicionado (efeitos de item/set que dizem "adiciona X de dano")

2. Conversão / Ganho como extra
   2a. "Ganha X% como dano Y" (extra): calculado sobre a Base pré-conversão, cria componente Y.
   2b. "Converte X% em Y": move parte da Base para Y. Soma de conversões de um tipo limitada a 100%;
       excedente é normalizado proporcionalmente. Sem cadeia (Y não converte novamente).

3. Increased (aditivo, um único bucket)
   × (1 + Σ Increased% aplicáveis)
   Aplicáveis = tipo final do componente + tags da fonte.
   Ex.: Cleaving Strike com Ruin convertido soma Ruin Damage + Skill Damage + Area Damage, não Physical Damage.

4. More (multiplicativo)
   × Π (1 + More%) — Set Powers, efeitos Legendary/Unique, Resonance, Keystones/Ultimates, Blessings, Core Attunement (01) etc.
   Tetos por fonte: 00 §5 R03. Este doc não introduz nenhuma fonte More (Broken e Execute contra chefe vão para Vulnerability).

5. Crit
   × (1 + CritDamage) se critou; base Critical Chance 5%, base Critical Damage +50%.
   Critical Damage do equipamento soma aos 50% base. DoT não crita (exceto efeito explícito).

6. Mitigação do alvo
   × (1 − Mitigação) — Armor Penetration reduz o Armor do alvo antes do cálculo de ArmorDR.

7. Vulnerability
   × (1 + Σ Vulnerability%) — debuffs no alvo (Ruin Vulnerability, Staggered, Broken, Distortion,
   conversão de Execute contra chefe etc.) somam entre si.
   Teto de Vulnerability total: +60% (dono: 07, R12). Mesma fonte não acumula consigo mesma; Ruin usa stacks (07).

8. Finalização
   Arredonda para inteiro apenas na exibição; HP é float. Dano mínimo por hit = 1.
```

**Escopo dos atributos ofensivos no pipeline:**

| Atributo | Aplica-se a |
|---|---|
| Physical / Ember / Frost / Storm / Void / Echo / Ruin Damage | Componentes daquele tipo |
| Skill Damage | Habilidades do herói (não ataques Basic/Heavy da arma) |
| Area Damage | Fontes com tag `Area` |
| Projectile Damage | Fontes com tag `Projectile` |
| Damage Over Time | Ticks de DoT (snapshot inclui este bônus) |
| Summon Damage | Dano causado por `Summon`/`Construct` (herdam os demais buckets do dono) |
| Attack Speed | Velocidade de animação de Basic, Heavy e habilidades com tag `Attack` |
| Cast Speed | Velocidade de animação de habilidades com tag `Spell` |
| Armor Penetration | Etapa 6 (Physical) |
| Stagger Damage | Pipeline de Stagger (§3.7) |

**Weapon Power:** `WeaponPower = DanoMédioPorGolpe × GolpesPorSegundoNormalizadosDaClasse`. É o "DPS de folha" da arma e aparece no tooltip. Como todas as classes têm DPS equivalente, Weapon Power de mesmo iLvl é praticamente igual entre classes. Para armas duplas, usa-se a Main Hand. Valores absolutos por iLvl: **02-equipamentos**.

**Hit vs Tick:**
- **Hit:** instância direta (golpe, projétil, explosão). Pode critar, aplica on-hit, gera Stagger e recurso.
- **Tick:** dano periódico (DoT, zona persistente). Não crita, não aplica on-hit, não gera Stagger salvo indicação.
- Toda fonte de Hit tem **Proc Coefficient** (0–1, padrão 1.0; multi-hits rápidos e canais usam 0,2–0,5). Chance de efeitos "ao acertar" = chance base × Proc Coefficient. Recurso gerado on-hit também é multiplicado.
- Proc Coefficient fixos (R07): repetição Echo **0,5** × coef. da fonte; cópia de clone **0,4** × coef. da fonte; tick de área persistente **0,25**.

**DoT:** snapshot de todos os buckets no momento da aplicação; **tick a cada 0,5 s** (R19 — 06 é dono; 07 usa este valor; 11 processa em lotes de 0,25 s apenas como granularidade); reaplicação segue regras de acúmulo de **07-resonance**. DoT genérico de um mesmo skill+fonte não acumula: o mais forte permanece e a duração renova.

**Área:** sem falloff. Raio de área é alterado apenas por modificadores (03) — aumento de área de X% multiplica o raio por `√(1 + X)`. Zonas persistentes da mesma habilidade e mesma fonte sobrepostas não acumulam no mesmo alvo (vale a mais forte).

**Gerações de proc (R07, modelo de 07):**

| Evento | Generation | Pode disparar procs? |
|---|---:|---|
| Hit/Tick original (ataque, habilidade, construção, clone agindo por conta própria) | 0 | Sim |
| Proc de Gen 0 (gatilho, Resonance, chain, explosão, réplica Echo) | 1 | Sim, gera Gen 2 |
| Proc de Gen 1 | 2 | **Não** — nada dispara em Gen ≥ 2 |

**Réplicas (Echo, Clone, Repeat):** cada réplica é um Hit novo (flag `IsEcho`) com rolagem de crit própria e Proc Coefficient da tabela acima. **Réplicas nunca geram novas réplicas**, exceto quando o poder declara **"Echo Recursion 1×"** (Thousand Echoes 6 peças / Infinite Reflection — 04; Echo Chamber — 08): a réplica pode ser replicada **uma única vez**, ainda sujeita a Gen < 2. A recursão nunca é recursiva sobre si mesma.

**Execute (HitFlag `Execute`, R08):** fontes com a flag matam instantaneamente alvos abaixo do limiar.

| Alvo | Limiar máximo |
|---|---|
| Comum (Light/Medium/Heavy) | 20% da vida (soma de todos os bônus de limiar; teto absoluto) |
| Elite | Metade do limiar efetivo (máx. 10%) |
| Guardian e chefe | Imune |

Contra chefes, qualquer efeito de Execute é convertido em **+25% Vulnerability enquanto o chefe está abaixo de 20% de vida** — uma única conversão, não acumula entre fontes, conta no teto de Vulnerability de 07. Bônus de limiar (ex.: Heavy Blade +5 pp) nunca passam do teto de 20%. Evento: `OnExecute`.

### 3.3 Pipeline de dano recebido (jogador)

```
0. I-frames (esquiva, Intangível, revive) e Parry ativo: o Hit é negado por completo.
   I-frames negam também Hits com flag Unavoidable; NÃO negam Annihilation.
   Parry não nega Unblockable nem Annihilation.
1. Base do ataque inimigo × escala de dificuldade (09/10)
2. Dodge Chance: rolagem para evitar completamente o hit. Só se aplica a golpes NÃO telegrafados (01);
   nunca a ticks nem a Hits com flag Unavoidable.
3. Mitigação por tipo (Armor + Physical Resistance / Elemental / Void Resistance)
4. Block Chance: hit bloqueado sofre −40% (01) e não causa flinch/knockback.
   Não se aplica a ticks nem a Hits com flag Unavoidable ou Unblockable.
5. Damage Reduction (genérico, aditivo entre fontes, cap 40% — 01)
6. Shield absorve antes da vida (Shield Strength amplia escudos gerados)
7. Vida
```

Status Resistance reduz duração de estados genéricos e status de afinidade no jogador (fórmula e cap: 01). Caps de Dodge/Block/Damage Reduction: **01-atributos** (Dodge soft 20%/hard 40%; Damage Reduction soft 20%/hard 40%). Piso de Movement Speed do jogador: 40% da base (01).

**HitFlags de dano** (R02 — flags de DamageInstance em 11, não tags):

| Flag | Efeito |
|---|---|
| `Unavoidable` | Ignora Dodge Chance e Block. I-frames e Parry ainda funcionam |
| `Unblockable` | Ignora Block e Parry. I-frames funcionam. Telegraph branco-dourado |
| `Annihilation` | Mecânica de chefe: ignora i-frames, Parry, Block e Dodge Chance; só sair da área evita. Sempre telegrafada ≥ 1,5 s |
| `Knockdown` | Causa queda no jogador (§3.8.4) |
| `Execute` | §3.2 |
| `IsEcho`, `IsResonance`, `IsTriggered` | Marcadores de origem usados pelas regras de Generation e Proc Coefficient |

### 3.4 Controles e input

**Mouse + teclado (todas as teclas remapeáveis):**

| Ação | Tecla |
|---|---|
| Movimento | WASD |
| Mira | Cursor projetado no plano do chão (altura do peito do personagem) |
| Ataque básico | LMB (segurar repete o combo) |
| Ataque forte | RMB (segurar carrega, quando a classe permitir) |
| Esquiva | Space |
| Habilidades 1–3 | Q, E, R |
| Habilidade 4 (mobilidade/utilidade) | Shift |
| Ultimate | F |
| Aether Flask | 1 |
| Interagir | G |
| Mapa da run | Tab |
| Inventário / Personagem | I / C |
| Ping de mira/foco em alvo (opcional) | Alt |

**Gamepad (layout Xbox):**

| Ação | Botão |
|---|---|
| Movimento / Mira | Stick esquerdo / stick direito (sem stick direito, mira = direção do movimento) |
| Ataque básico / forte | X / Y |
| Esquiva | A |
| Interagir | B |
| Habilidades 1–3 | LB, RB, LT |
| Habilidade 4 (mobilidade/utilidade) | RT |
| Ultimate | LB + RB juntos |
| Aether Flask | D-pad ↑ |
| Mapa / Menu | View / Menu |

**Assistência de mira:**
- Gamepad: cone de magnetismo de 25° para seleção de alvo; projéteis ganham correção de até 8°.
- M+K: sem correção de projéteis; ataques melee giram para o cursor nos frames de startup.
- **Lunge melee:** golpes melee avançam 0,5–1,5 m em direção ao alvo mais próximo dentro de um cone de 60° à frente (definido por golpe); nunca atravessam inimigos.
- Rotação: livre durante startup; travada nos frames ativos (exceto golpes com flag `Tracking`); livre a partir da recovery.

**Input buffer:** 9 frames (150 ms). Prioridade quando há conflito no buffer: **Esquiva > Habilidade > Forte > Básico**. Dentro da mesma prioridade, vale a última entrada.

### 3.5 Estrutura de um ataque e cancelamentos

Cada ataque tem três fases: **Startup → Active → Recovery**. Attack Speed/Cast Speed escalam as três igualmente (cap de velocidade via 01; provisório 2,0×). Hit-stop não é escalado.

| De → Para | Regra |
|---|---|
| Básico → Esquiva | Permitido em toda a Recovery. Em Startup, só nos 4 primeiros frames e apenas para armas de peso **Leve** e para Chakrams e Twin Wands |
| Básico → Habilidade | A partir do primeiro frame de Recovery |
| Básico → próximo golpe do combo | Janela de combo = Recovery inteira; combo reseta após 0,8 s sem input |
| Forte (carregando) → Esquiva | Sempre; a carga é perdida |
| Habilidade → Esquiva | Somente na Recovery, se a habilidade não tiver flag `Commit` |
| Ultimate → qualquer | Nunca; Ultimates concedem Unstoppable e, quando 08 indicar, Intangível |
| Ação de Parry → qualquer | Durante a janela de Parry, não cancelável; após Parry bem-sucedido, cancelável em qualquer ação por 12 f |
| Esquiva → Ataque | Básico nos últimos 8 frames da esquiva vira **Dash Attack** da classe |
| Flinch do jogador → Esquiva | Após 6 frames |

**Movimento durante ataques:**
- Melee: movimento travado (apenas lunge).
- Básico de Ranged/Caster: movimento a 60% da velocidade.
- Habilidades: definem `MoveDuringCast` (0%, 50% ou 100%) — 08.

**Animação:** animações de ataque são *in place*; deslocamentos (lunge, saltos, Shadowstep) são curvas dirigidas por dados. Root motion só em chefes e cinemáticas. Frame data é autoritativa nos dados; a velocidade da animação é ajustada para casar com ela. Animações enviam eventos nomeados (`HitWindowOpen/Close`, `CancelWindowOpen`, `VFX_*`, `SFX_*`, `Footstep`).

### 3.6 Frame data por arma

Valores a 60 fps, Attack Speed 1.0. `S/A/R` = Startup/Active/Recovery. **Moveset (nº de golpes, alcance, comportamento) e coeficientes de dano são de 08** (R18); este doc fixa tempos, hitbox, Stagger, hit-stop e Proc Coefficient para casar com o APS de 08 (`frames por golpe ≈ 60 / APS`). O DPS de referência e o dano por golpe são de **02** (±5% entre armas do mesmo herói; área −10% contra alvo único; especialistas +5%).

#### 3.6.1 Classes de peso (regras de cancelamento, flinch e hit-stop)

| Peso | Armas |
|---|---|
| Pesada | Greatsword, Warhammer, Heavy Blade, Void Hammer |
| Média | Longsword, Great Mace, Twin Blades |
| Leve | Dual Axes, Dual Daggers |
| Ranged | Longbow, Crossbow, Chakrams |
| Caster | Arcane Staff, Spellbook, Twin Wands, Crystal Orb, Void Focus, Ritual Staff, Totem, Ancient Scepter |

**Dash Attack** (básico nos últimos 8 frames da esquiva), por peso: Pesada 10/6/18, Stagger 20, hit-stop 5 f · Média 8/5/14, Stagger 12, 4 f · Leve 6/4/10, Stagger 6, 3 f · Ranged 6/2/12 (disparo curto que não consome munição/chakram) · Caster 8/2/12 (projétil rápido). Coeficiente: 08 (padrão 1,0 se 08 não definir).

#### 3.6.2 Kael

| Arma (APS) | Golpe | S/A/R | Hitbox | Stagger | Hit-stop | Proc |
|---|---|---|---|---:|---:|---:|
| **Greatsword** (0,9) | 1 / 2 | 20/6/40 | Setor 2,8 m, 120° | 15 | 5 f | 1,0 |
| | 3 | 26/8/34 | Setor 2,8 m, 120° | 35 | 8 f | 1,0 |
| | Forte | toque 36 f · carga até 72 f / 6 / 26 | Cone 3 m | 60 → 100 | 10 f | 1,0 |
| **Dual Axes** (1,6) | 1–4 | 10/3/24 | Setor 2,2 m, 110° | 5 | 2 f | 0,8 |
| | 5 (giro) | 14/10/16 | Círculo 2,2 m | 12 | 4 f | 0,8 |
| | Forte (redemoinho) | 8 / 72 / 14 | Círculo 2,2 m, re-hit 0,15 s; anda a 50% | 4/hit | 1 f/hit | 0,3 |
| **Warhammer** (0,7) | 1 | 28/8/50 | Setor 2,5 m, 140° | 30 | 7 f | 1,0 |
| | 2 | 32/8/46 | Setor 2,5 m, 160° | 40 | 8 f | 1,0 |
| | Forte (slam) | 40/6/34 | Onda radial 4 m | 90 | 10 f | 1,0 |
| **Longsword** (1,2) | 1 / 2 | 14/4/32 | Setor 2,6 m, 120° | 8 | 3 f | 1,0 |
| | 3 | 16/5/29 | Setor 2,8 m, 140° | 10 | 3 f | 1,0 |
| | 4 (estocada, avanço 3 m) | 20/6/24 | Caixa 1 × 3,5 m | 25 | 6 f | 1,0 |
| | Forte (estocada perfurante) | 22/6/30 | Caixa 1 × 5 m | 45 | 7 f | 1,0 |
| **Heavy Blade** (0,8) | 1 / 2 | 22/7/46 | Setor 2,6 m, 180° | 18 | 5 f | 1,0 |
| | 3 | 26/8/41 | Setor 2,6 m, 180° | 28 | 7 f | 1,0 |
| | Forte (execução) | 30/6/36 | Setor 3 m, 90° | 60 | 10 f | 1,0 |

#### 3.6.3 Lyra

| Arma (APS) | Golpe | S/A/R | Hitbox / projétil | Stagger | Hit-stop | Proc |
|---|---|---|---|---:|---:|---:|
| **Arcane Staff** (1,1) | 1 / 2 | 16/2/37 | Projétil 25 m/s, 14 m | 6 | 3 f | 1,0 |
| | 3 (perfura) | 18/2/35 | Projétil 25 m/s, 14 m, Pierce ∞ | 10 | 3 f | 1,0 |
| | Forte (raio de Frost) | 10 / canal até 120 f / 14 | Feixe 12 m, tick 15 f; gira 90°/s | 2/tick | — | 0,3 |
| **Spellbook** (1,4) | 1–3 | 12/2/29 | Página teleguiada 20 m/s, 12 m, Homing 180°/s | 5 | 2 f | 0,8 |
| | Forte (tempestade) | 18 / 30 / 20 | Cone 7 m, 60°, 6 hits | 4/hit | 1 f/hit | 0,3 |
| **Twin Wands** (2,2) | Alternado contínuo | 8/2/17 | Projétil 30 m/s, 13 m | 3 | 2 f | 0,7 |
| | Forte (rajada) | 16/2/26 | 5 projéteis em leque de 40° | 5 cada | 2 f | 0,5 |
| **Crystal Orb** (1,0) | Cristal orbitante | 20/4/36 | Auto-mira no inimigo mais próximo em 6 m | 8 | 3 f | 0,8 |
| | Forte (arremesso) | 18/2/30 | Projétil 18 m/s, 12 m; retorna (2 hits) | 12 cada | 3 f | 0,8 |
| **Void Focus** (1,3) | Chicote | 14/4/28 | Cone 6 m, 50° | 8 | 3 f | 0,8 |
| | Forte (Gravity Grip) | 20/6/30 | Cone 8 m, 60°; Pull 3 m | 20 | 4 f | 1,0 |

#### 3.6.4 Nyx

| Arma (APS) | Golpe | S/A/R | Hitbox / projétil | Stagger | Hit-stop | Proc |
|---|---|---|---|---:|---:|---:|
| **Dual Daggers** (1,8) | 1–3 | 8/3/22 | Setor 2 m, 100° | 4 | 2 f | 0,8 |
| | 4 | 12/4/17 | Setor 2 m, 120° | 10 | 4 f | 0,8 |
| | Forte (estocada, avanço 3 m) | 14/5/24 | Caixa 1 × 3,5 m | 20 | 5 f | 1,0 |
| **Longbow** (1,1) | Disparo | 18/2/35 | Flecha 35 m/s, 22 m, Pierce 1 | 6 | 3 f | 1,0 |
| | Forte (carga até 60 f) | +2 / 18 | Pierce ∞ na carga máxima | 20 → 40 | 5 f | 1,0 |
| **Crossbow** (1,0 rajada) | Rajada de 3 | 6/1/8 cada + 15 f de recarga | Virote 45 m/s, 16 m | 5 cada | 2 f | 0,7 |
| | Forte (virote explosivo) | 20/2/24 | Explosão raio 2,5 m | 30 | 6 f | 1,0 |
| **Twin Blades** (1,4) | 1–3 | 12/5/26 | Círculo 2,5 m (360°) | 7 | 3 f | 0,8 |
| | Forte (investida giratória) | 12 / 24 / 16 | Cápsula 5 m, 4 hits | 6/hit | 2 f/hit | 0,4 |
| **Chakrams** (1,2) | Arremesso | 14/2/34 | Disco 25 m/s, 12 m, retorna; Chain 2; máx. 2 no ar | 5 | 2 f | 0,7 |
| | Forte (órbita) | 12/2/18 | Discos orbitam Nyx 3 s, raio 3 m, tick 0,25 s | 2/tick | — | 0,25 |

#### 3.6.5 Orun

| Arma (APS) | Golpe | S/A/R | Hitbox / projétil | Stagger | Hit-stop | Proc |
|---|---|---|---|---:|---:|---:|
| **Ritual Staff** (1,0) | 1 / 2 | 18/2/40 | Projétil 24 m/s, 12 m; marca o alvo como foco das construções | 6 | 3 f | 1,0 |
| | 3 | 20/2/38 | Idem | 10 | 3 f | 1,0 |
| | Forte (feixe de dreno) | 10 / canal até 120 f / 14 | Feixe 10 m, tick 15 f | 2/tick | — | 0,3 |
| **Void Hammer** (0,8) | 1 / 2 | 22/7/46 | Setor 2,8 m, 140° | 25 | 6 f | 1,0 |
| | 3 | 28/8/39 | Setor 2,8 m, 160° | 35 | 8 f | 1,0 |
| | Forte (Gravity Slam) | 34/6/34 | Círculo 5 m; Pull até 3 m antes do impacto | 70 | 10 f | 1,0 |
| **Great Mace** (1,1) | 1–3 | 16/5/34 | Setor 2,4 m, 120° | 12 | 4 f | 1,0 |
| | 4 | 20/6/29 | Setor 2,4 m, 150° | 22 | 5 f | 1,0 |
| | Forte (golpe de escudo) | 4 / 30 (bloqueio frontal 120°) / 20 | Empurrão 2 m ao fim; conta como **Parry** nos primeiros 12 f | 30 | 6 f | 1,0 |
| **Totem** (0,9) | Pulso | 20/4/43 | Cone 6 m, 70° | 6 | 2 f | 0,8 |
| | Forte (plantar) | 24 / — / 16 | Totem por 6 s (CD 8 s); pulsos próprios conforme 08 | — | — | 0,25 |
| **Ancient Scepter** (1,2) | Açoite | 14/3/33 | Linha 9 m; Chain 2 | 6 | 3 f | 0,8 |
| | Forte (maldição) | 20/2/26 | Projétil 20 m/s; área 2,5 m | 15 | 4 f | 1,0 |

**Verificação de APS:** a soma `S + A + R` dos golpes do combo dividida pelo número de golpes fica em `60 / APS ± 3 f` para todas as armas. Variações de 08 permitidas sem revisão deste doc: ±15% em tempos, desde que o APS declarado em 08 seja mantido.

### 3.7 Esquiva

| Parâmetro | Valor |
|---|---|
| Duração | 18 frames (0,30 s) |
| Distância | 4,5 m fixos (curva ease-out) |
| I-frames | Frames 2–13 |
| Recovery | 3 frames (cancelável em ataque/habilidade a partir do frame 15) |
| Cargas | 2, iguais para todos os heróis (R06). Cargas extras só por fonte explícita de item/talento ("+1 carga de esquiva"), máx. 3 |
| Recarga | 1,5 s por carga, sequencial |
| Colisão | Atravessa inimigos Light e Medium; para em Heavy, Colossal e paredes |
| Perfect Dodge | Hit inimigo que seria recebido nos 6 frames anteriores ao início das i-frames → evento `OnPerfectDodge`: recurso do herói (08) + flash e SFX de 0,15 s |

Movement Speed **não** altera a distância ou duração da esquiva. Cooldown Reduction **não** afeta a recarga da esquiva (apenas efeitos que digam explicitamente "recarga de esquiva"). Não há variação inata por herói (distância, i-frames ou cargas); efeitos pós-esquiva (ex.: buff defensivo de Orun) são permitidos como passiva de 08, desde que não alterem a esquiva em si.

### 3.8 Estados genéricos

#### 3.8.1 Classes de massa (inimigos)

| Classe | Exemplos | Flinch | Knockback recebido | CC duro |
|---|---|---|---|---|
| Light | Fodder, enxames | Todo hit com Stagger ≥ 1 interrompe | 100% | 100% duração |
| Medium | Soldados, casters | Hits com Stagger ≥ 10 interrompem | 70% | 100% |
| Heavy | Tanks, brutos | Não sofre flinch; Super Armor durante ataques | 30% | 100% |
| Elite (qualquer massa) | — | Como a massa base, mas flinch exige o dobro | ×0,6 | ×0,6 duração |
| Colossal | Chefes, Guardians grandes | Nunca | 0% | Imune; convertido em Stagger (exceções Frostbound/Distortion, §3.8.2) |

Terminologia (R48): **Guardian** é o termo único para mini-chefe nomeado (09). **Stagger** é o único termo para a barra de equilíbrio; "Poise" não é usado.

#### 3.8.2 Stagger

- Cada Hit gera **Stagger** = `StaggerDoGolpe × (1 + Stagger Damage%)`. Valores por golpe em §3.6; habilidades definem o seu (08).
- Inimigos Medium e acima têm **barra de Stagger** visível ao ser preenchida.

| Alvo | Limiar base |
|---|---:|
| Medium | 40 |
| Heavy | 120 |
| Elite | Limiar da massa × 2,5 |
| Guardian | 400 |
| Chefe de região | 800 |

- Multiplicador de limiar por dificuldade: Normal 1,0 · Veteran 1,2 · Nightmare 1,4 · Eclipse 1,6 · Ascension +1% por nível (aditivo).
- Barra decai 15%/s após 3 s sem receber Stagger.
- **Staggered** (barra cheia): alvo atordoado e **+20% Vulnerability**. Duração: 1,5 s comuns · 2,0 s Elite · 3,0 s Guardian.
- **Broken** (barra cheia em chefe — R11, valores deste doc; 09 aplica): dura **4 s**, alvo atordoado e **+30% Vulnerability** (não acumula com Staggered; conta no teto de +60% de 07), hit-stop global de 6 f, evento `OnStaggerBreak`. Não é fonte More.
- Após cada Break, o limiar do chefe aumenta 50%, até no máximo 3 aumentos (limiar final ×2,5 do base com crescimento aditivo: 1,0 → 1,5 → 2,0 → 2,5).
- **CC → Stagger em Colossal:** cada aplicação de Stun/Root/Knockback em alvo imune converte em Stagger = `duração base do CC (s) × 40`.
- **Exceções nomeadas (R10, efeitos definidos em 07):**
  - **Frostbound:** Freeze em Colossal não congela; aplica Frostbound (07) e também converte em Stagger pela fórmula acima.
  - **Distortion:** Pull em Colossal não desloca; aplica Distortion (07): **+10% Vulnerability** (não More) pela duração definida em 07.

#### 3.8.3 CC duro e diminishing returns

| Estado | Efeito | Grupo de DR |
|---|---|---|
| Stun | Não age, não se move | Hard |
| Freeze | Efeito e buildup definidos em 07; duração e DR seguem este doc | Hard (compartilhado com Stun) |
| Root | Pode atacar e conjurar, não se move nem esquiva | Root |
| Silence | Não usa habilidades (inimigos casters perdem magias) | Silence |
| Taunt | Alvo prioriza atacar a fonte (ignora attack tokens enquanto durar); máx. 3 s, Elites 50% da duração; Guardians e chefes imunes; nunca aplicado ao jogador | Taunt |
| Knockback | Deslocamento por força; colisão com parede = **Wall Impact** (dano = 10% do hit original, Stun 0,5 s) | Sem DR, limitado pela massa |
| Pull | Deslocamento em direção a um ponto; força e regras de 07; em Colossal vira Distortion | Sem DR, limitado pela massa |
| Slow (genérico) | −X% Movement Speed (e, quando indicado, −X% Attack/Cast Speed). Não acumula: vale o mais forte. Teto −60% em inimigos; no jogador, piso de 40% da velocidade base (01). Chill (07) é um Slow com buildup próprio | Sem DR |
| Super Armor | Ignora flinch, knockback e interrupção; ainda recebe dano, Stagger e demais CC | — |
| Unstoppable | Super Armor + imunidade a todo CC (Stun, Freeze, Root, Silence, Taunt, Knockback, Pull, Slow). Em inimigos: a barra de Stagger enche, mas o Staggered só é aplicado quando o estado termina. Telegraph: contorno branco pulsante. Concedido ao jogador durante Ultimates e por efeitos de 08 | — |
| Stealth | A entidade não pode ser escolhida como alvo por IA/auto-mira; inimigos perdem o foco e investigam o último ponto conhecido. Quebra ao causar dano (exceto efeitos que digam o contrário) ou ao receber dano direto. Duração máx. 4 s por aplicação; ICD de 1 s para reentrar. Inimigos em Stealth (09) aparecem como distorção visível e são revelados por dano de área ou a ≤ 3 m do jogador | — |
| Parry | Janela de **0,2 s (12 f)** aberta por ação de parry (Ashen Guard, golpe de escudo da Great Mace, Perfect Guard de 04, bloqueio perfeito de 03). Hit recebido na janela é negado; o atacante melee sofre 50 de Stagger e o evento `OnParry` dispara. Bônus de janela somam até o teto de **0,3 s**. Não funciona contra `Unblockable` nem `Annihilation` | — |
| Intangível | I-frames de deslocamento de habilidade (Shadowstep, Astral Shift etc.): mesma semântica das i-frames da esquiva; atravessa inimigos, não atravessa paredes | — |

**DR por grupo (inimigos, R09 — regra única do jogo):** dentro de uma janela de 8 s desde o primeiro CC do grupo: 1º = 100% duração · 2º = 50% · 3º = 25% · depois imune até a janela terminar. Staggered/Broken não entram no grupo Hard. 07 não define imunidades próprias para Freeze; 11 implementa um único rastreador (CrowdControlTracker) com estes grupos.

**CC no jogador:**
- Hard CC: máximo **1,5 s** por aplicação; **Freeze: máximo 1,0 s**. Após qualquer Hard CC, **2 s de imunidade** a Hard CC.
- Root no jogador: máximo 2 s; esquiva bloqueada, habilidades liberadas.
- Silence no jogador é exclusivo de mecânicas de chefe telegrafadas.
- Status Resistance reduz durações (fórmula e cap: 01).

#### 3.8.4 Reação do jogador a hits

- Hits leves (fodder): apenas flash de dano, sem interrupção.
- Hits médios: flinch de 8 f (cancelável em esquiva após 6 f).
- Ataques pesados com flag `Knockdown`: queda de 24 f; i-frames nos últimos 10 f.

### 3.9 Hit detection

**Hurtboxes:** cápsula por personagem. A do jogador tem raio 15% menor que a silhueta visual (fairness); inimigos usam a silhueta.

**Melee:**
- Formas: setor (raio + arco), caixa ou cápsula, definidas por golpe nos dados e ancoradas ao personagem.
- Avaliação em cada tick dos frames ativos; para Attack Speed > 1,3 ou formas rápidas, é feito **sweep** entre a pose do tick anterior e a atual para evitar tunneling.
- **Hit registry:** cada alvo só é atingido uma vez por *Hit Group*; golpes multi-hit definem grupos separados ou um intervalo de re-hit (ex.: redemoinho das Dual Axes: 0,15 s).
- Golpes melee não atravessam paredes (raycast do centro do personagem até o alvo na camada `Wall`).

**Projéteis:**
- Cinemáticos, spherecast por tick (sem física rigidbody).
- Propriedades: velocidade, raio, alcance/tempo de vida, `Pierce`, `Chain`, `Fork`, `Return`, `Homing` (graus/s). Contagens alteradas por modificadores (03).
- Um projétil não atinge o mesmo alvo duas vezes, exceto no `Return` ou por efeito explícito.
- Bloqueados por `Wall`; atravessam obstáculos baixos (`LowCover`) apenas se a habilidade tiver tag `Area`.
- Projéteis inimigos: velocidade máxima 14 m/s (Normal) a 18 m/s (Eclipse); sempre com núcleo brilhante e contorno escuro.

**Área:**
- Explosões: overlap no frame de detonação, com raycast de LOS contra `Wall` a partir do centro.
- Zonas persistentes: avaliam ocupantes a cada 0,5 s (alinhado ao tick de DoT).

**Orçamento de performance:** definido por **11-arquitetura** (R47: 120 inimigos ativos, pico 200, 2.000 projéteis a 60 fps no hardware mínimo). Projéteis e zonas usam pooling (11).

### 3.10 Ameaça e telegraphs dos inimigos

**Attack tokens** (quantos inimigos podem atacar o jogador ao mesmo tempo): **a tabela é de 09** (R17 — melee 3 → 5 e ranged/caster 2 → 4 de Normal a Eclipse; Elites não consomem token; chefes ignoram tokens); 11 carrega os valores por dificuldade. Regra deste doc: inimigos sem token reposicionam, circulam, fazem ações de suporte ou ataques sem dano direto (buffs, invocação); nunca ficam parados à espera. Inimigo sob Taunt ignora tokens.

**Linguagem de telegraph (R14 — 06 é dono; 09 e todo VFX inimigo aplicam):**

| Tipo | Visual | Regra |
|---|---|---|
| Padrão (evitável) | Decal no chão **vermelho**, preenchimento crescendo **do centro para a borda**; impacto quando o preenchimento toca a borda | I-frames, Parry (melee) e Block funcionam |
| Controle | Decal **roxo**, mesmo preenchimento | Aplica CC (Stun, Root, Pull, Freeze); i-frames evitam |
| Unblockable | Decal **branco-dourado** + brilho na arma do inimigo | Flag `Unblockable`: ignora Block e Parry; i-frames funcionam |
| Janela interrompível | Anel **ciano** ao redor do inimigo durante o wind-up | Stagger/CC aplicado dentro da janela cancela o ataque e causa Staggered imediato |
| Annihilation | Decal vermelho com **borda amarela tracejada** animada, ≥ 1,5 s | Flag `Annihilation`: i-frames não protegem; é preciso sair da área |
| Unstoppable | Contorno branco pulsante no inimigo + sting sonoro | Não pode ser interrompido por Stagger/CC (estado §3.8.3) |
| Linha (sniper) | Laser fino que engrossa | 0,8 s mínimo |
| Agarrão | Ícone de corrente sobre o inimigo | Esquiva escapa; Block não |
| Off-screen | Seta na borda da tela com cor do tipo | Inimigos fora da tela não disparam projéteis |

Laranja/amarelo no chão é **reservado a Ember** (VFX do jogador e status de 07) e nunca usado como telegraph inimigo.

**Tempos mínimos de wind-up:**

| Fonte | Normal | Redução máxima em dificuldades altas | Piso absoluto |
|---|---:|---:|---:|
| Comum (inclui fodder) | 0,6 s | −15% | 0,45 s |
| Elite | 0,8 s | −15% | 0,55 s |
| Chefe (golpe pesado) | 1,0–1,5 s | −15% | 0,8 s |

Todo ataque pesado tem cue sonoro de wind-up distinto do ataque leve.

**Prioridade de renderização (legibilidade):** telegraphs inimigos e projéteis inimigos renderizam **acima** de todo VFX do jogador. VFX do jogador têm opacidade configurável (padrão 80%). Vermelho saturado no chão é **reservado** a telegraphs inimigos; Ember usa laranja/amarelo.

### 3.11 Game feel

**Hit-stop (local, atacante + alvo):**

| Situação | Duração |
|---|---|
| Peso Leve / Twin Wands | 2 f |
| Peso Médio / Ranged / Caster | 3–4 f |
| Peso Pesado | 5–8 f |
| Valores por golpe | §3.6 |
| Crit | +2 f |
| Kill de Elite | 8 f |
| Break de chefe / troca de fase / Ultimate | 6 f **global** |

Limite: no máximo 1 hit-stop a cada 100 ms para o atacante (evita engasgo em multi-hit). Hit-stop de projéteis afeta apenas o alvo.

**Screen shake (modelo de trauma):** `shake = trauma²`; trauma decai 1,5/s. Valores: hit Heavy 0,15 · crit Heavy 0,25 · Wall Impact 0,2 · Ultimate 0,5 · golpe de chefe recebido 0,4. Hits de Light/Medium não geram shake. Slider de intensidade 0–100% (padrão 70%).

**Números de dano:**
- Cor por tipo: Physical branco · Ember laranja · Frost ciano · Storm amarelo-elétrico · Void violeta · Echo verde-água · Ruin verde-doentio. Paleta alternativa para daltonismo (07 define ícones de forma por afinidade).
- Crit: 1,5× tamanho + leve "punch". Ticks de DoT: menores e semitransparentes.
- Tamanho também escala com % da vida máxima do alvo.
- Opções: desligado · individual · agregado por alvo (soma em janela de 0,5 s).

**Feedback de impacto no alvo:** flash branco de 2 f, empurrão visual (squash) de 0,1 s em Light/Medium, partículas de material (carne, armadura, pedra, etéreo).

**Câmera:**

| Parâmetro | Valor |
|---|---|
| Projeção | Perspectiva, FOV vertical 30° |
| Pitch / Yaw | 50° / 45° fixo (sem rotação pelo jogador) |
| Distância | 22 m (zoom 18–26 m) |
| Look-ahead | 15% da distância até o cursor/direção de mira, limitado a 3 m |
| Arenas de chefe | Distância e enquadramento definidos por arena (09), zoom-out automático suave |
| Oclusão | Geometria entre câmera e jogador fica translúcida (dither) |

**Hooks de eventos de combate** (consumidos por VFX, SFX, UI, Resonance, modificadores e Set Powers — implementação em 11):

`OnHitDealt, OnCritDealt, OnTickDealt, OnKill, OnOverkill, OnExecute, OnStaggerApplied, OnStaggered, OnStaggerBreak, OnCCApplied, OnInterrupt, OnDodge, OnPerfectDodge, OnParry, OnBlock, OnEvade, OnHitTaken, OnShieldBroken, OnLowHealth, OnFlaskUsed, OnDeath, OnRevive, OnSkillCast, OnSkillHit, OnResourceSpent, OnResourceGained, OnWallImpact, OnStealthEnter, OnStealthBreak`

Esta lista é o vocabulário canônico de eventos de combate: 11 incorpora todos (R47) com estes nomes; outros docs (03, 04, 07, 08) usam estes nomes nos gatilhos. Todo evento carrega a Generation do evento de origem (§3.2).

**Áudio (diretrizes para o doc de áudio):** impacto por classe de arma × material do alvo; camada adicional para crit; transiente de áudio sincronizado ao primeiro frame de hit-stop; cue de wind-up para todo ataque pesado inimigo; ducking de música durante Ultimate e Break de chefe.

### 3.12 Cura e Aether Flask

- 3 cargas por run (aumentáveis por meta-progressão — 10).
- Cura 35% da vida máxima em 1 s (cura por tempo; dano recebido não cancela). Healing Effectiveness amplia.
- Uso não interrompe movimento; não pode ser usado durante ataques Heavy e Ultimates.
- Recarga: Rest Room e Antecâmara restauram todas (serviço da Rest definido por 10, R34); kill de Elite restaura 1 carga (máximo 1 por sala).

### 3.13 Metas de TTK e letalidade

TTK = tempo de dano focado de um jogador com equipamento adequado ao conteúdo (iLvl recomendado, build coerente). A dificuldade aumenta vida e dano, mas o equipamento esperado acompanha; por isso as metas são as mesmas em todos os tiers **para o jogador no gear recomendado**. A coluna "abaixo do gear" mostra a faixa aceitável para quem entra 1 tier acima do seu equipamento.

| Alvo | TTK alvo | Abaixo do gear |
|---|---:|---:|
| Light (fodder) | 0,3–0,8 s (1–3 hits) | até 1,5 s |
| Medium | 1,5–3 s | até 5 s |
| Heavy / Tank | 4–6 s | até 10 s |
| Elite | 8–15 s | até 25 s |
| Guardian | 30–60 s | até 100 s |
| Chefe de região (Normal–Nightmare) | 3–4 min | até 7 min |
| Chefe de região (Eclipse+, com fase extra) | até 5 min | até 8 min |
| Sala de combate completa | 60–90 s | até 150 s |

**Durações (R16 — 06 é dono; 09 dimensiona fases e ondas para caber):** soft enrage de chefe e Guardian começa em **2× a duração alvo** (sem hard enrage). Com isso a duração total de uma run de 14 salas fica em 25–35 min (00 §3.1).

**Letalidade contra o jogador (dano por hit em % da vida máxima, defesas recomendadas):**

| Fonte | Normal | Veteran | Nightmare | Eclipse |
|---|---:|---:|---:|---:|
| Golpe comum | 4–6% | 5–7% | 6–9% | 8–12% |
| Golpe de Elite | 10–15% | 12–18% | 15–22% | 18–28% |
| Golpe pesado de chefe | 20–30% | 25–35% | 30–42% | 35–50% |
| Mecânica de chefe "Annihilation" | Letal, flag `Annihilation`, sempre telegrafada ≥ 1,5 s (§3.10) | | | |

Teto: nenhum ataque não-mecânico > 60% da vida máxima com defesas recomendadas (validado por ferramenta de simulação — 11). Esta tabela é a única referência de letalidade (R15); 09 calibra o dano relativo dos inimigos para caber nela. A exceção é exclusivamente Annihilation.

### 3.14 Invulnerabilidade, morte e revive

**Grace period:** 0,25 s de imunidade a novos Hits após qualquer Hit recebido; 0,8 s após Hit ≥ 20% da vida máxima. Ticks de DoT/zonas não iniciam grace e não são bloqueados por ele (mas são limitados a 1 tick por fonte a cada 0,5 s).

**Morte:**
- Vida ≤ 0 → 0,5 s de câmera lenta (0,3× apenas na apresentação; lógica pausa), então tela de morte.
- **Death Recap:** últimos 5 hits recebidos (fonte, tipo de dano, valor, mitigação aplicada) e status ativos no momento.
- A morte encerra a incursão; o que é mantido é definido por **10-progressao**.

**Revive:**
- Base: 0 revives. **Única fonte:** a Blessing específica de revive do catálogo de 09 (R46). Não há revive por meta-progressão nem por item.
- Ao reviver: 50% da vida, 3 s de invulnerabilidade total, onda de knockback de raio 6 m (sem dano), recurso do herói restaurado a 50%.
- Máximo de 1 revive por sala de chefe, independentemente das fontes.

### 3.15 Acessibilidade de combate

- Segurar para atacar (LMB/X) com repetição automática de combo.
- Toggle de habilidades canalizadas (pressionar para iniciar/parar).
- Sliders: screen shake, opacidade de VFX do jogador, intensidade de flashes.
- Modo alto contraste para telegraphs (borda branca espessa).
- Paleta de afinidades para daltonismo + ícones de forma.
- Opção de mira por direção do movimento no gamepad.

---

## 4. Dependências e pontos de integração

| Doc | Recebe deste doc | Fornece a este doc |
|---|---|---|
| 01-atributos | Pipeline e escopo de cada atributo; buckets; pontos onde caps entram; HitFlags que afetam Dodge/Block | Fórmula de ArmorDR (adotada), caps de Crit/Dodge/Block/Damage Reduction/Attack Speed/Status Resistance, piso de Movement Speed, efeito dos primários |
| 02-equipamentos | Weapon Power, frame data por arma, classes de peso | DPS de referência, dano por golpe por arma e iLvl, tolerâncias (±5%, área −10%, especialista +5%), regras de Main/Off Hand |
| 03-modificadores | Proc Coefficient (Echo 0,5 / clone 0,4 / área persistente 0,25), modelo Generation, área `√(1+X)`, Pierce/Chain/Fork/Return, conversão, Execute (limiares), Parry unificado, vocabulário de eventos | Catálogo que usa esses campos |
| 04-conjuntos | Bucket More (orçamento), Vulnerability (teto 07), Execute contra chefe, Echo Recursion 1×, eventos | Set Powers que declaram More% e quais declaram Echo Recursion 1× |
| 07-resonance | Grupo de DR Hard (Stun+Freeze), CC no jogador (Freeze máx. 1,0 s), tick de DoT 0,5 s, conversão de CC em Stagger, cores de dano, HitFlags | Status de afinidade, buildup, acúmulos, **teto de Vulnerability (+60%)**, modelo Generation, efeitos de Frostbound e Distortion |
| 08-herois | Frame data por arma (§3.6), esquiva universal, Perfect Dodge, Parry, estados genéricos, flags `Commit`/`MoveDuringCast`/`Tracking`, input | Movesets e APS das 20 armas, coeficientes, Stagger de habilidades, recurso ganho em Perfect Dodge, quais habilidades concedem Unstoppable/Intangível/Stealth/Taunt |
| 09-inimigos | Classes de massa, limiares de Stagger, Broken, linguagem de telegraph, wind-ups mínimos, letalidade, TTK/durações, comportamento sem token | Tabela de attack tokens, arquétipos, padrões de ataque, arenas e câmera de chefe, Blessing de revive |
| 10-progressao | Flask, escala de limiar por dificuldade, TTK por tier | Escala de vida/dano por dificuldade, Monster Level (eLvl), regra de morte, serviços da Rest Room, cargas extras de Flask |
| 11-arquitetura | Tick 60 Hz, DamageInstance, HitFlags, Generation, hit registry, vocabulário de eventos, grupos de DR | Implementação, orçamento de performance (120/200 inimigos, 2.000 projéteis), ferramenta de simulação de dano/TTK |

**Estrutura mínima de um DamageInstance (para 11):** fonte, alvo, componentes `{tipo, valor}`, tags (vocabulário 00 §3.4), flag Hit/Tick, `isCrit`, `procCoefficient`, `generation` (0–2), `staggerValue`, `hitGroupId`, **HitFlags** (`Unavoidable`, `Unblockable`, `Annihilation`, `Knockdown`, `Execute`, `IsEcho`, `IsResonance`, `IsTriggered`).

---

## 5. Conflitos com as convenções

1. **Refinamento do §3.2 (Base):** o contrato diz "Base = dano da arma ou coeficiente × dano da arma". Este doc especifica que **habilidades usam Weapon Power (DPS normalizado da arma)** e não o dano por golpe. Não contradiz a fórmula, mas muda o significado de "dano da arma" para habilidades. 02 e 08 usam esta definição.
Resolvidos nesta rodada (registro 00 §5): esquiva uniforme sem variação inata por herói (R06; 08 já removeu a carga extra de Nyx e a esquiva curta de Orun); mitigação de Echo/Ruin por Void Resistance (01 já concordava); teto de Vulnerability passou de +100% para +60% e pertence a 07 (R12); recursão de eco passou para o modelo Generation de 07 (R07); Broken deixou de ser fonte More (R11).

## 6. Questões abertas para playtest

1. Janela de Perfect Dodge (6 f) — validar se é perceptível em gamepad sem ser trivial em M+K.
2. Grace period de 0,25 s pode deixar o jogador "tanky" demais contra enxames em Eclipse; testar 0,15 s nesse tier.
3. Limiar de Stagger de chefe (800) versus builds de Heavy/Stagger: alvo é 2–3 Breaks por luta para build dedicada, 1 para build sem foco.
4. FOV 30° / distância 22 m em monitores ultrawide — validar enquadramento e leitura de telegraphs nas bordas.

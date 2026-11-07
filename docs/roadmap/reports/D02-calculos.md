# D02 — Cálculos das lacunas numéricas

Status: **aprovado pelo usuário em 2026-11-07** (Q1–Q4 com as opções recomendadas) e registrado como R53–R58 em 00 §5.6.

Modelo reproduzível em `docs/roadmap/reports/D02-modelo.py` (`python -I docs/roadmap/reports/D02-modelo.py`).

## 1. Alvos (T1)

| # | Alvo | Valor | Fonte | Restringe |
|---|---|---|---|---|
| A1 | TTK Medium (Standard) | 1,5–3 s (abaixo do gear: até 5 s) | 06 §3.13 | HP Standard |
| A2 | TTK Light / Heavy / Elite / Guardian | 0,3–0,8 s / 4–6 s / 8–15 s / 30–60 s | 06 §3.13, R16 | HP × arquétipo de 09 §4 |
| A3 | Sala completa | 60–90 s | 06 §3.13, R16 | HP × orçamento de PA (09 §10.1) |
| A4 | TTK igual em todos os tiers no gear recomendado | — | 06 §3.13 | Crescimento do HP = crescimento do DPS do jogador |
| A5 | Vida relativa por tier | Normal 1 · Veteran 3 · Nightmare 9 · Eclipse 25 · Ascension 25 × 1,035^N | 10 §7.2 | Curva HP × modificador de tier |
| A6 | Dano relativo por tier | 1 · 1,6 · 2,5 · 4 · 4 × 1,02^N | 10 §7.2 | Curva de dano |
| A7 | Letalidade golpe comum (% da vida, defesas recomendadas) | N 4–6 · V 5–7 · NM 6–9 · E 8–12 | 06 §3.13, R15 (única referência) | Dano Standard |
| A8 | Escala de inimigos dentro da run calibrada para Blessings +25% | — | R35, 10 §3.2 | Rampa de eLvl na run |
| A9 | eLvl = iLvl_sala (faixa região × dificuldade, interpolada pela sala); Guardian/chefe = Faixa.max | N 1–24 · V 25–49 · NM 50–69 · E 75–84 · A até 110 | 10 §7.2, 05 §4.1–4.2, R44 | Domínio da curva |
| A10 | Weapon Power | `10 × FatorArma × 1,0235^(iLvl−1)` | 02 §3.4 | DPS do jogador |
| A11 | Faixas por tier, Core Attunement, upgrade +3%/nível, exemplo Kael Eclipse | — | 01 §3.2, §3.9, §3.12 | DPS e vida de referência |
| A12 | Mitigação de referência do alvo | Armor 25%, Res 20% | 01 §3.10 | DPS efetivo |
| A13 | Resistência efetiva de referência do jogador | 30% | 01 §3.4.1 | Dano bruto do inimigo |
| A14 | XP para o próximo nível | `200 × L^1,8` (≈ 3,97 M até o 50) | 10 §5.1, R40 | XP/PA |
| A15 | Curva de tempo | nv ~6 em 1,5–2 h; ~25 em 12–15 h; ~35 em 22–26 h; ~45 em 35–40 h; 48–50 na entrada do Eclipse (40–45 h); 50 em 40–50 h | 10 §10, §5.1 | XP/PA |
| A16 | Run ~30 min, sucesso 60–70%; XP 100% retido na morte | — | 10 §10, §4 | XP/PA |
| A17 | Orçamento de PA, densidade por dificuldade, Elites valem ×3 PA em XP/Shards | `(20 + 3 × Linha) × MultDensidade` (N 1,0 · V 1,1 · NM 1,2 · E 1,3) | 09 §10.1, §10.5 | XP/PA, Shards/PA |
| A18 | Pesos de geração do mapa | — | 09 §11.2 | PA e Shards por run |
| A19 | Rift Shards por fonte | Combat 5–15 · Elite 20–30 · Treasure 40–60 · Events | 10 §3.3 | Shards/PA, Shards por run |
| A20 | Conversão | 1 Shard = 5 Gold × fator de dificuldade (só vitória/extração) | 10 §3.3, §4 | Fator |
| A21 | Gold por run | N 800 · V 2.500 · NM 7.000 · E 15.000 · A 15.000 × (1 + 0,02N) | 10 §6.6 | Fator |
| A22 | Chefe: HP = duração alvo × DPS de referência da dificuldade | — | 09 §8.1 | DPS de referência (M17 tem `TODO(M14)`) |
| A23 | Caps por alvo / duração pendentes de 03 | KAE-BC-03, LYR-SR-04 (cap por alvo), LYR-VO-04 (duração dos fragmentos) | D01 P11, 03 §6.3 | — |

## 2. Modelo do jogador de referência (premissas)

O "jogador no gear recomendado" de 06 §3.13 é modelado como Kael (dano) e como a média dos 4 heróis (vida). Premissas declaradas — o simulador do M14 recalibra:

- Gear com iLvl = eLvl do conteúdo; nível do herói pela curva alvo de 10 §10 (eLvl 1→1, 24→25, 49→35, 69→45, 84→50).
- Cobertura de afixos úteis por tier: N 60% · V 85% · NM 95% · E 100%; upgrade médio N +0 · V +2 · NM +4 · E +6 (R31).
- Increased = 3 afixos de dano no meio da faixa do tier (um em Main Hand ×1,5), como no exemplo de 01 §3.12; Aether Lattice ≈ +0,5% Increased por nível.
- Set Powers / Legendary / modificadores / Resonance: ×1,10 (N) · ×1,25 (V) · ×1,40 (NM) · ×1,50 (E), coerente com o "+45–53%" de 01 §3.12.
- Rotação single target ≈ 1,8 × Weapon Power (básicos + habilidades de 08 §4.3–4.4); alvo de referência com mitigação média 22,5% (A12); Blessings médias da run ×1,125.
- Vida de referência = média dos 4 heróis (01 §3.3) + flats de vida em 5,6 slots-equivalentes no meio do tier.

Resultado: o DPS de referência cresce ~18× de Normal a Eclipse e a vida de referência ~4×. Isso torna A6 incompatível com A7 (ver §3.2).

## 3. Propostas (T2)

### 3.1 Curva de HP do Standard (dono 06)

```
HP_Std(eLvl) = 36 × 1,048^(eLvl − 1)                      // campanha, eLvl 1–84; modificador de tier de vida = 1
HP_Std(Ascension N, sala) = HP_Std(eLvl_Eclipse(sala)) × 1,035^N
DPS_ref(eLvl) = HP_Std(eLvl) / 2,2 s                       // DPS focado de referência (09 §8.1, M14, M17)
```
- `eLvl_Eclipse(sala)` = eLvl que a mesma sala teria em Eclipse (75–84 pela interpolação de 05 §4.2). Em Ascension o eLvl real (85–110) continua valendo para o K de Armor de 01.
- Rampa dentro da run: 1,048^(Faixa.max − Faixa.min) = ×1,21–1,26 em faixas de 4–5 níveis ≈ Blessings +25% (A8). Eclipse (faixa de 9 níveis) rampa ×1,52: TTK no fim da run ~20% maior que no início, dentro de A1.

### 3.2 Curva de dano do Standard (dono 06)

```
DanoBase(eLvl) = 20 × 1,048^(eLvl − 1)                     se eLvl ≤ 24
               = 58,8 × 1,016^(eLvl − 24)                   se eLvl > 24
Dano_Std(eLvl, tier) = DanoBase(eLvl) × M_let(tier)         // M_let: Normal 1,0 · Veteran 1,2 · Nightmare 1,5 · Eclipse 2,0
Dano_Std(Ascension N, sala) = Dano_Std(eLvl_Eclipse(sala), Eclipse) × 1,02^N
```
- Calibração: golpe comum do Standard = centro da faixa de letalidade de 06 (5 / 6 / 7,5 / 10%) × vida de referência ÷ (1 − 30%). `M_let` = razão entre os centros das faixas.
- Dois segmentos porque a vida do jogador cresce rápido no Normal (nível 1→25) e devagar depois (nível 25→50 em 60 eLvl).

### 3.3 Verificação por eLvl

| eLvl | Tier | HP Std | Dano Std | DPS_ref | Nível ref | Vida ref | TTK Std (s) | Golpe Std (% vida) |
|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | N | 36 | 20,0 | 16,4 | 1 | 288 | 1,68 | 4,9% |
| 6 | N | 46 | 25,3 | 20,7 | 6 | 384 | 1,82 | 4,6% |
| 10 | N | 55 | 30,5 | 25,0 | 10 | 465 | 1,93 | 4,6% |
| 15 | N | 69 | 38,6 | 31,5 | 16 | 568 | 2,09 | 4,7% |
| 20 | N | 88 | 48,7 | 39,9 | 21 | 672 | 2,27 | 5,1% |
| 24 | N | 106 | 58,8 | 48,1 | 25 | 842 | 2,19 | 4,9% |
| 25 | V | 111 | 71,7 | 50,4 | 25 | 925 | 1,74 | 5,4% |
| 35 | V | 177 | 84,0 | 80,6 | 29 | 1009 | 2,15 | 5,8% |
| 49 | V | 342 | 104,9 | 155,3 | 35 | 1297 | 2,48 | 5,7% |
| 50 | NM | 358 | 133,2 | 162,8 | 35 | 1373 | 2,07 | 6,7% |
| 58 | NM | 521 | 151,3 | 236,8 | 39 | 1460 | 2,45 | 7,2% |
| 69 | NM | 873 | 180,2 | 396,7 | 45 | 1845 | 2,57 | 6,8% |
| 75 | E | 1156 | 264,2 | 525,6 | 47 | 1992 | 2,53 | 9,4% |
| 80 | E | 1462 | 286,0 | 664,4 | 49 | 2016 | 2,82 | 10,0% |
| 84 | E | 1763 | 304,8 | 801,4 | 50 | 2399 | 2,39 | 8,9% |

Resumo por tier — TTK Std: N 1,68–2,27 s · V 1,75–2,48 · NM 2,08–2,57 · E 2,23–2,82 (A1 ✅). Golpe comum: N 4,6–5,1% · V 5,3–6,1% · NM 6,3–7,4% · E 8,5–10,0% (A7 ✅).

Relativo à média do Normal: **vida** V 3,2 · NM 8,9 · E 22,2 (A5: 3/9/25, desvio ≤ 12%, aceito). **Dano** V 2,4 · NM 4,3 · E 7,9 (A6: 1,6/2,5/4 — **conflito**, ver §4 Q1).

Ascension (relativo à sala equivalente de Eclipse): A10 vida ×1,41 / dano ×1,22 · A50 ×5,6 / ×2,7 · A90 ×22 / ×5,9 · A100 ×31 / ×7,2.

### 3.4 TTK por arquétipo (TTK Std médio 2,2 s; faixa 1,7–2,8 s)

| Arquétipo (09 §4) | HP× | TTK | Alvo 06 | |
|---|---:|---:|---|---|
| Fodder | 0,3 | 0,5–0,8 s | 0,3–0,8 | ✅ |
| Standard / Support | 1,0 | 1,7–2,8 s | 1,5–3 | ✅ |
| Ranged / Assassin | 0,8 | 1,3–2,3 s | 1,5–3 | ⚠ 1,3–1,5 s só nos eLvl 1–5 |
| Caster | 0,9 | 1,5–2,5 s | 1,5–3 | ✅ |
| Summoner | 1,6 | 2,7–4,5 s | 1,5–3 (até 5) | ⚠ acima; aceitável (testa single target) |
| **Tank** | **4,0** | **6,7–11,3 s** | **4–6** | ❌ ver §4 Q2 (com 2,5×: 4,2–7,0 s) |
| Elite (Standard) | 6,0 | 10–17 s | 8–15 (até 25) | ⚠ até 17 s no meio do Eclipse |
| Guardian | 15 | 25–42 s focado | 30–60 | ✅ com transições de fase e mecânicas |

Sala completa (A3): linha 1 Normal = 23 PA ≈ 6,4 HP Std; linha 12 Eclipse ≈ 73 PA ≈ 20 HP Std. Com eficiência de área 1,5–2× e ~50% de tempo atacando (esquivas, ondas a cada ≤ 25 s, portais), dá 30–60 s de combate + transições ≈ 60–90 s. Coerente; a confirmação é do M14/M16.

### 3.5 XP por Ponto de Ameaça (dono 10)

```
NívelDeConteúdo(eLvl) = interpolação linear de (1→1), (24→25), (49→35), (69→45), (84→50); 50 acima de 84
XP_por_PA(eLvl, dif)  = 2 × NívelDeConteúdo(eLvl)^1,1 ÷ MultDensidade(dif)       // MultDensidade de 09 §10.1
XP do inimigo = PA × XP_por_PA (Elites ×3, 09 §10.5) · Guardian = 60 PA-equivalentes · Chefe = 150 PA-equivalentes
```
- `NívelDeConteúdo` é a curva alvo de 10 §10 (nível esperado do herói naquele conteúdo); dividir pela densidade mantém o XP por sala igual entre dificuldades e evita que o ganho dependa só de quantidade de inimigos.
- Ascension: `NívelDeConteúdo` = 50 → XP/PA constante (≈ 148/MultDensidade); XP vira Hero Mastery (10 §5.4).

| eLvl | 1 | 6 | 12 | 24 | 25 | 49 | 50 | 69 | 75 | 84 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| XP/PA | 2,0 | 14,9 | 32,1 | 69,0 | 63,8 | 90,8 | 84,5 | 109,7 | 106,3 | 113,8 |

PA por run completa (pesos de 09 §11.2, sem Guardian/chefe): N 354 · V 395 · NM 464 · E 507; com Guardian + chefe: 564–717. Por tentativa (65% sucesso; morte em média a 55% da run, XP retido): ~1.060 PA/h em Normal.

Verificação contra A15 (cronograma de 10 §10, 1 herói, sem Experience Gain nem XP de eventos):

| Marco | Horas | Nível simulado | Alvo |
|---|---:|---:|---|
| Primeiro chefe | 1,75 | 6,3 | ~6 ✅ |
| Normal concluído | 13,5 | 23,8 | ~25 ✅ |
| Veteran concluído | 24 | 34,3 | ~35 ✅ |
| Nightmare concluído | 37,5 | 45,1 | ~45 ✅ |
| Entrada no Eclipse | 42,5 | 48,5 | 48–50 ✅ |
| Nível 50 | **43,3** | 50 | 40–50 h ✅ |

Experience Gain (até +12%/item) e XP de eventos (Archivist) aceleram alguns %; continua dentro de 40–50 h.

### 3.6 Rift Shards por PA (dono 10; 09 §10.5 pede a constante)

`Rift Shards por PA = 0,2` (Elites ×3), sem escala por dificuldade. Reproduz 10 §3.3: sala de combate linha 1 Normal 4,6 → linha 12 Eclipse 14,6 (5–15 ✅); Elite Arena 13–34, média ~22 (20–30 ✅ na média). Treasure e Events seguem os valores fixos de 10 §3.3. Bruto por run completa: N ~118 · E ~147 Shards.

### 3.7 Fator de dificuldade Rift Shards → Gold (dono 10)

Saldo final típico ≈ 65 Shards (bruto 118–147 − gastos típicos: 1 Temper, rerolls, às vezes Anchor). Participação da conversão no Gold de 10 §6.6:

| Opção | N | V | NM | E | Participação no Gold da run |
|---|---:|---:|---:|---:|---|
| **Proposta: proporcional ao Gold/run de 10 §6.6** | 1 | 3 | 9 | 19 (A: 19 × (1 + 0,02N)) | 41% · 39% · 42% · 41% (constante) |
| Placeholder do M20 | 1 | 1,5 | 2 | 3 | 41% · 20% · 9% · 6% |

Proposta mantém o peso da decisão "gastar Shards na run × converter na extração" (e do Echo of a Fallen Eclipsed) igual em todas as dificuldades. O restante (~60%) do Gold de 10 §6.6 vem de drops e vendas (calibração M19/M20).

### 3.8 P11 do D01 (opcional, dono 03)

Regra única para "cap por alvo" de 03 §6.3: **dano por alvo ≤ 50% do coeficiente base da habilidade** (em % DA, antes do pipeline).
- KAE-BC-03 Linked Torment: ≤ 60% DA por alvo por acorrentamento (base 120% DA).
- LYR-SR-04 Collapsing Reality: implosão ≤ 175% DA por alvo (base 350% DA).
- LYR-VO-04 Rime Core: fragmentos duram **2 s** (igual ao chão Burning de Infernal Leap, 08 §4.4).

## 4. Decisões do usuário (todas: opção recomendada)

- **Q1 — Dano relativo (10 §7.2) × letalidade (06 §3.13).** A vida de referência cresce ~4× de Normal a Eclipse; manter o dano 4× deixaria o golpe comum de Eclipse em ~5% da vida (alvo 8–12%). Recomendação: a letalidade de 06 vence (R15) e a coluna "Dano relativo" de 10 §7.2 passa ao resultado da curva (≈ 2,4 / 4,3 / 7,9; Ascension mantém × 1,02^N).
- **Q2 — Tank HP× 4,0 (09 §4).** Com 4,0×, nenhum TTK Standard cabe ao mesmo tempo em Medium (≥ 1,5 s para Ranged 0,8×) e Heavy (≤ 6 s). Recomendação: Tank HP× 2,5 em 09 (4,2–7,0 s). Alternativa: manter 4,0 e registrar exceção de TTK do Tank (até ~10 s).
- **Q3 — Fator Shards → Gold:** proposta 1 / 3 / 9 / 19 / A: 19 × (1 + 0,02N).
- **Q4 — P11:** incluir agora como resolução (03 aplica após o merge do D01) ou deixar para depois.

## 5. Achados fora do escopo (pendências, não editadas)

- **05 §4.3** usa níveis esperados antigos (fim do Normal ~15, Veteran ~30, Nightmare ~40, Eclipse ~44); o dono da curva (10 §5.1/§10, R40) diz 25 / 35 / 45 / 48–50. A checagem de nível requerido continua válida com os dois.
- **10 §10:** "segundo herói até o 50 em 12–15 h" não fecha com Kindred Echo (+100% só até 10 níveis abaixo do principal): o modelo dá ~25 h começando direto em Veteran. Revisar Kindred Echo ou o alvo no M20.
- **09 §11.2:** a coluna "média esperada por run" (Combat 5–6, Elite 2–3, Treasure 1–2, Shrine 2, Event 1–2, Challenge 0–2) soma mais que as 10 linhas livres; pelos pesos da tabela o esperado é ~5 Combat, ~1,6 Elite, ~0,8 Treasure, ~1,2 Shrine, ~1,1 Event, ~0,6 Challenge.
- **09 §8.1:** HP de chefe = duração × `DPS_ref` × fator de uptime; o uptime (≈ 0,7 com fases e janelas) fica para o M17 calibrar.

# M08 — Sistema de habilidades v1 + kit do Kael
Fase 1 · Onda 5 · Status: ⏳ não iniciado
Depende de: M03, M05 · Desbloqueia: M06, M10 · Paralelo com: M07 (AI — só contratos congelados do M05)
Branch: modulo/M08-habilidades-kael
Docs de referência: 08-herois §3 (convenções, DA/APS), §4 (Kael inteiro), §11.1; 06-combate §3.2 (Weapon Power, Execute), §3.5 (cancelamentos), §3.6.2 (frame data das 5 armas do Kael), §3.8.3 (Unstoppable, Root, Parry), §3.11 (hit-stop global de Ultimate); 07-resonance §4 (Burning, Ruin Vulnerability); 11-arquitetura §3.4.2 (`HeroDefinition`, `ResourceDefinition`, `SkillDefinition`), §3.6.1, §3.6.3; 00 §5 R01, R03, R05, R08, R12, R20

## Objetivo
Construir o sistema de habilidades data-driven (Delivery/Effects/Triggers polimórficos, recurso, custo, cooldown, slots de input) sem modificadores ainda, e entregar o kit completo do Kael — Fury, passivas, 5 habilidades com a Ultimate e as 5 armas — jogável na Combat Sandbox.

## Critérios de saída (verificáveis)
1. `SkillDefinition` com `Delivery`, `Effect[]` e `Trigger[]` via `[SerializeReference]` (11 §3.6.1); novas habilidades do Kael montadas por dados, com código novo só para comportamentos genuinamente únicos.
2. `SkillController` valida custo/cooldown/estado (Stun, Freeze, Silence; Root bloqueia só `Movement`), emite `OnSkillCast`, `OnSkillHit`, `OnCastEnd`, `OnResourceSpent`, `OnResourceGained` e integra com os cancelamentos de 06 §3.5 e o input Q/E/R/Shift + F (R05).
3. Fury conforme 08 §4.1 (ganhos por básico/forte/dano recebido/abate, decaimento −8/s após 4 s, 0 no início da sala, teto 100 → 150 via Maximum Resource) e passivas Embers of Rage e Ashbound Resolve (08 §4.2).
4. As 5 habilidades com custo, CD, área, dano (coeficiente × Weapon Power — 06 §3.2) e comportamento de 08 §4.4: Cleaving Strike, Infernal Leap, Burning Chains, Ashen Guard (Parry de 12 f — R20) e Worldbreaker (Ultimate com Unstoppable e hit-stop global de 6 f — 06 §3.11).
5. As 5 armas do Kael (Greatsword, Dual Axes, Warhammer, Longsword, Heavy Blade) com a frame data de 06 §3.6.2, moveset e bônus de kit de 08 §4.6.
6. Testes EditMode (Fury, custo, CD, passivas) e PlayMode (cada habilidade: dano esperado, área, Root, Parry, Unstoppable) verdes.

## Fora de escopo
Modificadores de habilidade, `SkillSpec` compilado com fases e Augments (M10); Aether Lattice/Keystones (M20); StatusSystem completo e Resonance (M11 — aqui Burning e Ruin Vulnerability usam `StatusStub`/`TimedModifier` do M05); HUD de habilidades (M06); outros heróis (M21–M23).

## Premissas e decisões
- **Só contratos congelados do M05.** Não editar `Gameplay/Combat/Contracts`, demais arquivos de `Gameplay/Combat/*`, asmdefs de teste nem `ProjectSettings`. Este módulo é o dono da `CombatSandbox.unity` nesta onda (o M07 usa `AISandbox.unity`).
- **`SkillSpec` v1:** cópia imutável dos números da definição + snapshot de stats (11 §3.5.3), sem fases de modificador. A API pública (`SkillSpec`, `ISkillSpecSource`) é desenhada para o M10 inserir a compilação de 6 fases sem quebrar chamadores.
- **Dados:** a estrutura (qual Delivery, quais Effects) é autorada no asset do SO; os números (custo, CD, coeficientes, raios, durações) vêm de `DataSource/Skills.csv` e são aplicados pelo importador por `StableId`, sem recriar o asset (11 §3.15).
- **Weapon Power** vem do `WeaponDefinition` (M05/02); habilidades usam `WeaponPower × Coef` e básicos usam dano por golpe × multiplicador (06 §3.2, 08 §3).
- **Lacuna — Worldbreaker:** 08 §4.4 diz "Vulnerability Ruin (07) por 6 s" sem quantidade de stacks; 07 define Ruin Vulnerability como 4% × até 5 stacks. Provisório: 1 stack por fissura que atingir o alvo (máx. 5 = +20%), duração 6 s. Registrar no Log para confirmação do dono (08).
- **Bônus de kit das armas** (08 §4.6) entram como `WeaponKitBonus` simples (StatMod condicional ou ajuste de parâmetro de habilidade) até o M10 migrá-los para `ModifierOp` no canal "Item".
- Arte greybox (D4): VFX placeholder com primitivas e cores de afinidade de 06 §3.11 (Ember laranja, Ruin verde-doentio).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Dados: `SkillDefinition` polimórfico, `ResourceDefinition`, `HeroDefinition` + importador de números | execução | executor | — | `Assets/_Project/Code/Data/Skills/*`, `Assets/_Project/Code/Data/Heroes/*`, `DataSource/{Skills,Heroes,Resources}.csv`, `Assets/_Project/Code/Editor/DataPipeline/Importers/{SkillImporter,HeroImporter}.cs` | ⏳ |
| T2 | Recurso Fury + passivas do Kael | execução | executor | T1 | `Assets/_Project/Code/Gameplay/Heroes/Resources/*`, `Assets/_Project/Code/Gameplay/Heroes/Kael/{KaelFury,KaelPassives}.cs`, `Assets/_Project/Code/Tests/EditMode/Heroes/Kael/*` | ⏳ |
| T3 | `SkillController` + `SkillSpec` v1 + slots de input | execução | executor | T1 | `Assets/_Project/Code/Gameplay/Skills/Runtime/*`, `Assets/_Project/Code/Tests/EditMode/Skills/Runtime/*` | ⏳ |
| T4 | Biblioteca de Deliveries e Effects | execução | executor | T3 | `Assets/_Project/Code/Gameplay/Skills/Delivery/*`, `Assets/_Project/Code/Gameplay/Skills/Effects/*`, `Assets/_Project/Code/Gameplay/Skills/Triggers/*` | ⏳ |
| T5 | Kit do Kael: 5 habilidades | execução | executor | T2, T4 | `Assets/_Project/Content/Heroes/Kael/Skills/*`, `Assets/_Project/Code/Gameplay/Heroes/Kael/Skills/*`, `Assets/_Project/Content/Heroes/Kael/Data/Kael_Hero.asset` | ⏳ |
| T6 | As 5 armas do Kael + bônus de kit | execução | executor | T3 | `DataSource/AttackSteps.csv` (linhas do Kael), `DataSource/Weapons.csv` (linhas do Kael), `Assets/_Project/Content/Heroes/Kael/Weapons/*`, `Assets/_Project/Code/Gameplay/Heroes/WeaponKitBonus.cs` | ⏳ |
| T7 | Integração na Combat Sandbox + VFX placeholder | execução | executor | T5, T6 | `Assets/_Project/Scenes/Sandbox/CombatSandbox.unity`, `Assets/_Project/Content/Heroes/Kael/Prefabs/Kael_Greybox.prefab`, `Assets/_Project/Content/Heroes/Kael/VFX/*` | ⏳ |
| T8 | Testes PlayMode das habilidades e armas | execução | executor | T7 | `Assets/_Project/Code/Tests/PlayMode/Skills/*` | ⏳ |
| T9 | Revisão final e integração | integração | principal | T1–T8 | `docs/roadmap/M08-habilidades-kael.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2 ∥ T3
3. T4 ∥ T6 (T6 só toca dados/armas)
4. T5
5. T7
6. T8
7. T9

## Briefs

**T1 — Dados de habilidade, recurso e herói**
- Objetivo: SOs de 11 §3.4.2 para habilidade, recurso e herói, com importador de números.
- Contexto: `SkillDefinition`: StableId, tags (vocabulário 00 §3.4), custo, cooldown, coeficientes (× Weapon Power), `MoveDuringCast` (0/50/100% — 06 §3.5), flags `Commit` e `Tracking`, `DeliveryDefinition`, `EffectDefinition[]`, `TriggerDefinition[]` (`[SerializeReference]`), slots de modificador aceitos (3; 2 em Ultimate/Basic/Heavy — 03, só declarados). `ResourceDefinition`: tipo `Pool` (Fury), máximo base 100, teto 150, regras de ganho/decaimento como dados. `HeroDefinition` (mínimo para o Kael): recurso, `MovementProfile` (M04), skills por slot (Basic, Heavy, Q, E, R, Shift, F — R05), categorias de arma permitidas, passivas, atributos-base relativos (08 §3.1: vida 120%, Armor 130%, 6,0 m/s, Strength).
- Arquivos: os listados na tabela; importadores descobertos pelo pipeline do M02.
- Restrições: `StableId` `skill.kael.cleaving_strike`, `hero.kael`, `resource.fury`; importador nunca recria asset (preserva referências) — 11 §3.15.
- Aceite: import preenche os números de 08 §4.4 nos assets; teste EditMode confere cada número.
- Verificação: import/validação (CLAUDE.md §6) sem erros; EditMode `-testFilter "Eclipse.Tests.EditMode.Data.Skills"` → verde.
- Entrega: ≤ 150 palavras.

**T2 — Fury + passivas**
- Objetivo: recurso do Kael exato e passivas como mods condicionais.
- Contexto (08 §4.1): básico +5 no 1º alvo, +1 por alvo adicional (máx. +9 por golpe); Forte +10 (+15 carregado); dano recebido +1 por 1% da vida máxima perdida (máx. 15 Fury/s); abate +3; decaimento −8/s após 4 s sem causar nem receber dano; 0 Fury no início da sala; teto 100 (até 150 com Maximum Resource: +1 por 1% — 08 §3.2). Ganhos on-hit × Proc Coefficient (06 §3.2). Passivas (08 §4.2): Embers of Rage = +2% Increased Physical e Ember Damage por 10 Fury (máx. +20% a 100; +30% a 150); Ashbound Resolve = abaixo de 35% de vida, 15% menos dano recebido e +25% de geração de Fury.
- Arquivos: `Gameplay/Heroes/Resources/{ResourcePool,ResourceComponent}.cs`, `Gameplay/Heroes/Kael/{KaelFury,KaelPassives}.cs`, testes EditMode.
- Restrições: passivas como `StatMod` condicionais no `StatBlock` (11 §3.5.3 — buffs de alta frequência não invalidam snapshot); eventos `OnResourceGained/Spent/Full/Empty`.
- Aceite: testes para cada regra de ganho, teto, decaimento e as duas passivas.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Heroes.Kael"` → verde.
- Entrega: ≤ 150 palavras.

**T3 — `SkillController` + `SkillSpec` v1 + slots**
- Objetivo: ciclo de execução de 11 §3.6.3 sem modificadores.
- Contexto: `TryCast(spec)` → valida custo/CD/estado (Stun, Freeze, Silence bloqueiam; Root bloqueia só skills `Movement`) → `OnSkillCast` → fases windup/active/recovery dirigidas pelos frames da skill → Delivery cria `DamageInstance` com `Generation = 0` → dano/estado/Stagger via M05 → eventos → `OnCastEnd`. Cancelamentos: Básico→Habilidade a partir do 1º frame de Recovery; Habilidade→Esquiva só na Recovery se não for `Commit`; Ultimate não cancelável (06 §3.5) — usar `AttackController.CanCancelInto` do M05. Input: slots Q/E/R/Shift/F do `InputBuffer` do M04 (R05). Cooldown afetado por CDR (caps de 01), recarga de esquiva não. `SkillSpec` v1 = snapshot imutável dos números + stats incondicionais filtrados pelas tags (11 §3.5.3), recompilado em `StatsChanged`.
- Arquivos: `Gameplay/Skills/Runtime/{SkillController,SkillSpec,SkillSpecBuilder,ISkillSpecSource,CooldownTracker,SkillSlotBinding}.cs`, testes EditMode.
- Restrições: `SkillSpecBuilder` com ponto de extensão explícito para o M10 (fases 1–6) sem mudar a assinatura pública.
- Aceite: testes de custo insuficiente, CD, bloqueio por estado, Root só em Movement, cancelamentos.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Skills.Runtime"` → verde.
- Entrega: ≤ 180 palavras.

**T4 — Biblioteca de Deliveries e Effects**
- Objetivo: blocos reutilizáveis para montar o Kael e os próximos heróis.
- Contexto: Deliveries de 11 §3.6.1 necessárias agora: `MeleeArc`, `Leap` (salto mirado com curva por dados), `AreaAtSelf`, `AreaAtPoint`, `ConeMultiTarget` (até N alvos), `Stance` (postura com duração e saída por soltar o botão), `RadialLines` (fissuras). Effects: `Damage` (coef × Weapon Power, componentes e conversão via `DamageResolver`), `ApplyState` (Root, Unstoppable, Parry window, Taunt — estados do M05), `ApplyStatus` (via `IStatusReceiver` — Burning stub), `ApplyTimedModifier` (Vulnerability — `TimedModifier` do M05), `GainResource`, `Shield`, `GroundZone` (zona persistente do M05), `Tether` (vínculo com quebra por distância). Triggers: `(Evento, Condição, Effects[], ICD)` com ICD mínimo 0,1 s (11 §3.6.5); só avaliados se `Generation < 2`.
- Arquivos: `Gameplay/Skills/Delivery/*`, `Gameplay/Skills/Effects/*`, `Gameplay/Skills/Triggers/*`.
- Restrições: classes `[Serializable]` polimórficas com inspector utilizável; usam projéteis/áreas/pool do M05, sem duplicar.
- Aceite: testes EditMode de cada Effect isolado com alvos falsos (`ICombatEntity` mock).
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Skills.Effects"` → verde.
- Entrega: ≤ 180 palavras.

**T5 — Kit do Kael: 5 habilidades**
- Objetivo: montar as habilidades de 08 §4.4 com números exatos.
- Contexto: **Cleaving Strike** (Q): 20 Fury, sem CD, arco 180° 3,5 m, 220% DA, tags Attack/Melee/Area/Physical, encadeável enquanto houver Fury e cancela a recovery do básico. **Infernal Leap** (Shift): gera +15 Fury, CD 9 s, salto até 8 m, impacto raio 3 m 180% DA, Attack/Movement/Area/Ember, Unstoppable no ar (0,5 s), chão Burning 2 s. **Burning Chains** (E): 15 Fury, CD 12 s, cone 8 m até 5 alvos, 120% DA + Burning, Root 2 s; alvos acorrentados recebem a Burning do Kael; quebra (fim do Root ou > 10 m) causa 60% DA. **Ashen Guard** (R): CD 14 s, postura 1,5 s, −60% dano recebido, bloqueio total frontal (exceto `Unblockable`), contra-ataque ao fim/soltar arco 3 m 250% DA + 30% do dano absorvido, +20 Fury; hit nos primeiros 0,2 s = Parry (12 f, R20): dano anulado e contra-ataque +50%. **Worldbreaker** (F): consome toda a Fury (mín. 50), CD 90 s, raio 7 m, 6 fissuras radiais, 600% DA + 6% DA por Fury consumida, Attack/Area/Ultimate/Physical/Ruin, Ruin Vulnerability 6 s (provisório: 1 stack por fissura, máx. 5 — ver Premissas), Unstoppable durante o cast, hit-stop global 6 f e shake 0,5 (06 §3.11). Tags exatamente como 08.
- Arquivos: assets em `Content/Heroes/Kael/Skills/`, código único (stance do Ashen Guard, tether das correntes, fissuras) em `Gameplay/Heroes/Kael/Skills/`, `Kael_Hero.asset`.
- Restrições: números só via CSV/import; nenhum Augment (M10/M20).
- Aceite: cada habilidade executável na sandbox e coberta por teste em T8.
- Verificação: import/validação sem erros; PlayMode T8.
- Entrega: ≤ 200 palavras.

**T6 — As 5 armas do Kael + bônus de kit**
- Objetivo: frame data e moveset das 4 armas restantes (Greatsword já existe do M05) e bônus de kit das 5.
- Contexto: frame data de 06 §3.6.2: Dual Axes (APS 1,6; golpes 1–4 10/3/24 setor 2,2 m 110°; 5º giro 14/10/16 círculo 2,2 m; Forte redemoinho 8/72/14 re-hit 0,15 s, anda a 50%); Warhammer (0,7; 28/8/50, 32/8/46; slam 40/6/34 onda 4 m); Longsword (1,2; 14/4/32 ×2, 16/5/29, estocada 20/6/24 avanço 3 m; Forte estocada perfurante caixa 1 × 5 m); Heavy Blade (0,8; 22/7/46 ×2, 26/8/41; Forte execução 30/6/36 setor 3 m 90°) — Stagger, hit-stop e Proc conforme a tabela. Bônus de kit (08 §4.6): Greatsword Cleaving Strike +20% área; Dual Axes +2 Fury por básico e Burning Chains +1 corrente; Warhammer Leap/Worldbreaker +25% Stagger Damage e raio do Leap +1 m; Longsword janela de Parry do Ashen Guard +0,1 s (teto 0,3 s — R20) e contra-ataque +20%; Heavy Blade +5 pp nos limiares de execução (nunca acima de 20%; Elites metade — R08). Forte da Heavy Blade: +100% dano contra alvos abaixo de 35% (não é Execute). DPS por arma: 02 (±5% entre armas do Kael).
- Arquivos: linhas do Kael em `DataSource/AttackSteps.csv` e `DataSource/Weapons.csv`, assets em `Content/Heroes/Kael/Weapons/`, `Gameplay/Heroes/WeaponKitBonus.cs`.
- Restrições: não alterar as linhas do Greatsword criadas no M05 além do bônus de kit; aumento de área multiplica o raio por `√(1+X)` (06 §3.2).
- Aceite: teste confere frames de cada golpe e o DPS de folha dentro de ±5% entre as 5 armas.
- Verificação: import/validação sem erros; PlayMode T8.
- Entrega: ≤ 150 palavras.

**T7 — Integração na Combat Sandbox + VFX placeholder**
- Objetivo: Kael completo jogável.
- Contexto: Kael greybox com `HeroDefinition`, `SkillController`, Fury; troca de arma por tecla de debug na sandbox (as 5 armas); VFX placeholder por afinidade (cores de 06 §3.11); bonecos de treino do M05 e o atacante de teste para exercitar Ashen Guard/Parry.
- Arquivos: `CombatSandbox.unity`, `Kael_Greybox.prefab`, `Content/Heroes/Kael/VFX/*`.
- Aceite: na sandbox, todas as habilidades e armas funcionam sem erros no console.
- Verificação: PlayMode "cena carrega com Kael completo" → verde.
- Entrega: ≤ 100 palavras.

**T8 — Testes PlayMode das habilidades e armas**
- Objetivo: provar critérios 2–5.
- Contexto: casos: Cleaving Strike a 220% de Weapon Power em boneco sem mitigação e arco 180°/3,5 m (alvo a 3,6 m não é atingido); spam de Cleaving enquanto houver Fury; Leap gera 15 Fury, Unstoppable nos 0,5 s de ar (Stun recusado), chão Burning 2 s; Chains atingem no máx. 5 alvos no cone de 8 m, Root 2 s, quebra a > 10 m causa 60%; Ashen Guard −60% durante a postura, Parry só nos primeiros 12 f, contra-ataque +50% após Parry, `Unblockable` atravessa o bloqueio frontal; Worldbreaker recusado com Fury < 50, dano 600% + 6% × Fury, hit-stop global 6 f; frame data das 5 armas; bônus de kit (área do Cleaving com Greatsword = raio × √1,2).
- Arquivos: `Tests/PlayMode/Skills/*`.
- Aceite: todos verdes.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Skills"` → 0 falhas.
- Entrega: ≤ 150 palavras.

**T9 — Revisão final e integração (principal)**
- Objetivo: conferir critérios e preparar o terreno do M10.
- Arquivos: este arquivo, `docs/ROADMAP.md` §7, `CLAUDE.md` (pastas `Gameplay/Skills`, `Gameplay/Heroes`, convenção de StableId de skill, regra "estrutura no SO, números no CSV").
- Aceite: critérios 1–6 ✅ com saídas no Log; lacuna do Worldbreaker registrada; nota para o M10 sobre o ponto de extensão do `SkillSpecBuilder` e a migração de `WeaponKitBonus`; checklist manual de sensação do Kael registrado.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Skills" -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
→ validador sem erros; 0 falhas.

## Riscos e rollback
- `[SerializeReference]` com refatoração de tipos quebra assets → usar `[MovedFrom]` ao renomear e manter tipos estáveis; snapshot test dos assets do Kael.
- Contrato do M05 insuficiente (ex.: falta `Tether` em `StatusRequest`) → parar e pedir ao lead; não editar `Contracts` em paralelo com o M07.
- Ashen Guard/Parry sensível a timing → testes por frame (12 f exatos) antes de qualquer ajuste de sensação.
- Edição concorrente de `DataSource/AttackSteps.csv` → o M07 grava ataques de inimigos em `EnemyAttacks.csv`; se houver conflito, ele é de linhas distintas e resolve-se no merge.

## Atualizações de documentação
- `CLAUDE.md`: pastas `Gameplay/Skills/*` e `Gameplay/Heroes/*`; regra "estrutura de skill no SO, números no CSV"; convenção de `StableId` de skill/herói/recurso.
- `docs/ROADMAP.md` §7: status do M08; pendência "stacks de Ruin Vulnerability do Worldbreaker (08)".

## Prompt de disparo
```
Implemente o módulo M08 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M08-habilidades-kael.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M08-habilidades-kael.
Este módulo roda em paralelo com o M07: não edite Gameplay/Combat/Contracts, asmdefs de teste nem arquivos de AI/.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

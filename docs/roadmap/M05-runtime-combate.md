# M05 — Runtime de combate (frame data, hit detection, estados, feedback)
Fase 1 · Onda 4 · Status: ✅ concluído (pendente: checagem manual do usuário)
Depende de: M03, M04 · Desbloqueia: M06, M07, M08, M09 · Paralelo com: M15 (Core/RunGen — pastas disjuntas)
Branch: modulo/M05-runtime-combate
Docs de referência: 06-combate §2, §3.1–§3.3, §3.5, §3.6.1–§3.6.2 (Greatsword), §3.8, §3.9, §3.11, §3.12, §3.14; 08-herois §4.3 (multiplicadores do Greatsword); 07-resonance §4 (Burning, só para o stub de status); 11-arquitetura §3.5.2, §3.6.4, §3.7, §3.9, §3.14; 00 §5 R02, R07, R08, R09, R12, R13, R20

## Objetivo
Entregar o "motor" de combate genérico — contratos compartilhados, ataques por frame data, hit detection, aplicação de dano pelo `DamageResolver` do M03, estados genéricos, projéteis, áreas, pooling e feedback — validado com o Greatsword do Kael contra bonecos de treino. Este módulo **congela os contratos** que M07 (IA) e M08 (habilidades) usam em paralelo.

## Critérios de saída (verificáveis)
1. Contratos compartilhados publicados em `Gameplay/Combat/Contracts` (entidade, time, dano recebido, eventos de combate com os nomes canônicos de 06 §3.11, requisição de status, hitbox) e documentados no Log; M07/M08 não precisam editá-los.
2. Greatsword do Kael com combo 3 golpes + Forte carregável exatamente na frame data de 06 §3.6.2 (20/6/40, 20/6/40, 26/8/34; Forte toque 36 f, carga até 72 f, 6 ativos, 26 recovery) e multiplicadores de 08 §4.3 (110/110/160%; 280/420%).
3. Regras de cancelamento de 06 §3.5 e input buffer do M04 respeitados; combo reseta após 0,8 s sem input.
4. Hit detection por janelas de frame data com formas setor/caixa/cápsula, sweep para Attack Speed > 1,3, `HitRegistry` por Hit Group e bloqueio por `Wall` (06 §3.9).
5. Dano aplicado via `DamageResolver` (M03) para ofensa e pipeline de dano recebido de 06 §3.3 (i-frames, Parry, Dodge Chance, mitigação, Block −40%, Damage Reduction cap 40%, Shield, vida) com HitFlags de R02/R13.
6. Estados genéricos de 06 §3.8 funcionando: massa/flinch/knockback (Wall Impact), barra de Stagger e Staggered (+20% Vulnerability), CC via `CrowdControlTracker` do M03 (DR 100/50/25/imune em 8 s; jogador 1,5 s / Freeze 1,0 s + 2 s de imunidade), Super Armor, Unstoppable, Root, Silence, Slow, Taunt, Stealth, Intangível e Parry (12 f, teto 0,3 s).
7. Grace period 0,25 s / 0,8 s, morte com Death Recap dos últimos 5 hits, Aether Flask (3 cargas, 35% em 1 s) (06 §3.12, §3.14).
8. `ProjectileManager` (spherecast em job, pool, ≥ 2.000 projéteis), áreas instantâneas e zonas persistentes com tick 0,5 s, DoT genérico com snapshot e `StatusStub` para Burning provisório.
9. Feedback: hit-stop local por golpe (+2 f crit, máx. 1 a cada 100 ms), hit-stop global de 6 f para Ultimate/Break, screen shake por trauma (valores de 06 §3.11), flash de 2 f, squash 0,1 s.
10. Pool genérico usado por inimigos, projéteis, VFX e drops; 0 B de GC por frame no cenário de teste do módulo (11 §3.14).
11. EditMode + PlayMode verdes; testes medem frame data, hit-stop e i-frames.

## Fora de escopo
Habilidades (M08), IA (M07), HUD e números de dano visuais (M06), áudio (M09), modificadores (M10), StatusSystem/Resonance completos (M11), demais armas (Kael no M08; outros heróis M21–M23), Execute em alvo real (o resolver de Execute fica pronto, mas só é usado por M08/M10).

## Premissas e decisões
- **Divisão Core × Gameplay:** `DamageInstance`, `DamageResolver`, `StatBlock` e `CrowdControlTracker` vêm do M03 (Core, C# puro). Este módulo só os consome e cria a camada de cena. Se o M03 não tiver algum campo de `DamageInstance` de 11 §3.5.2 (`generation`, `procCoefficient`, `staggerValue`, `hitGroupId`, `SourceChain`, `HitFlags`), registrar no Log e abrir ajuste no M03 antes de seguir — não duplicar o tipo.
- **Contratos congelados para M07 ∥ M08** (só podem ser estendidos por novos arquivos, nunca editados por eles): `EntityId`, `Team`, `ICombatEntity`, `IDamageReceiver`, `IStatusReceiver`, `IStaggerable`, `CombatEvent` structs (um por hook de 06 §3.11), `CombatEventStream` (ring buffer lido em `LateUpdate` — 11 §3.7), `HitboxShape`, `AttackStep`, `StatusRequest`, `DotRequest`, `ProjectileSpec`, `AreaSpec`.
- **Asmdefs de teste:** este módulo ajusta uma única vez `Eclipse.Tests.EditMode.asmdef` e `Eclipse.Tests.PlayMode.asmdef` para referenciar Core, Data, Gameplay, AI, UI, Audio, Input System, Cinemachine, Burst, Collections e Mathematics — M07/M08/M06/M09 não editam asmdefs de teste.
- **Animação greybox:** golpes "in place" com arma primitiva girando por curva dirigida pelos frames do `AttackStep` (06 §3.5); sem animações externas (D4). Frame data é autoritativa nos dados.
- **`StatusStub`:** implementação mínima de `IStatusReceiver` com política `StackIntensity(max)` + duração renovável, suficiente para Burning provisório com aplicação garantida (07 §4: 4 s, máx. 10 stacks, 8% DRef Ember/stack/s). Buildup, Resonance e os 24 status completos são do M11, que substitui a implementação sem mudar o contrato.
- **Vulnerability e debuffs temporários:** `TimedModifier` aplica `StatMod` por duração em `StatBlock` (fonte removível — 11 §3.5.1); o teto de +60% é do resolver (M03/R12).
- Dados de arma e ataque via `DataSource/*.csv` → SO (pipeline do M02); nenhum número hardcoded (CLAUDE.md §1).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Contratos compartilhados + asmdefs de teste | execução | executor | — | `Assets/_Project/Code/Gameplay/Combat/Contracts/*`, `Assets/_Project/Code/Tests/EditMode/Eclipse.Tests.EditMode.asmdef`, `Assets/_Project/Code/Tests/PlayMode/Eclipse.Tests.PlayMode.asmdef`, `Assets/_Project/Code/Gameplay/Eclipse.Gameplay.asmdef` | ✅ |
| T2 | Pool genérico | execução | executor | T1 | `Assets/_Project/Code/Gameplay/Pooling/*`, `Tests/EditMode/Gameplay/Pooling/*` | ✅ |
| T3 | Dados de arma e ataque (SO + CSV + importador) | execução | executor | T1 | `Assets/_Project/Code/Data/Combat/{WeaponDefinition,AttackChainDefinition,AttackStep,HitFeedbackProfile}.cs`, `DataSource/Weapons.csv`, `DataSource/AttackSteps.csv`, `DataSource/HitFeedback.csv`, `Assets/_Project/Code/Editor/DataPipeline/Importers/WeaponImporter.cs`, `Assets/_Project/Content/Heroes/Kael/Weapons/*` | ✅ |
| T4 | Entidade de combate, vida, dano recebido, morte, Flask | execução | executor | T1 | `Assets/_Project/Code/Gameplay/Combat/Entities/*`, `.../Combat/Damage/*`, `.../Combat/Death/*`, `.../Player/AetherFlask.cs` | ✅ |
| T5 | `AttackController`: combo, cancelamentos, lunge, rotação | execução | executor | T3, T4 | `Assets/_Project/Code/Gameplay/Combat/Attacks/*` | ✅ |
| T6 | Hit detection (formas, sweep, `HitRegistry`, LOS) | execução | executor | T3, T4 | `Assets/_Project/Code/Gameplay/Combat/HitDetection/*` | ✅ |
| T7 | Estados genéricos e Stagger | execução | executor | T4 | `Assets/_Project/Code/Gameplay/Combat/States/*`, `.../Combat/Stagger/*` | ✅ |
| T8 | Projéteis, áreas, zonas, DoT genérico, `StatusStub`, `TimedModifier` | execução | executor | T2, T4 | `Assets/_Project/Code/Gameplay/Combat/Projectiles/*`, `.../Combat/Areas/*`, `.../Combat/Status/*` | ✅ |
| T9 | Feedback: hit-stop, shake, flash, squash | execução | executor | T5, T6 | `Assets/_Project/Code/Gameplay/Feedback/*` | ✅ |
| T10 | Greatsword greybox do Kael + bonecos de treino na sandbox | execução | executor | T5–T9 | `Assets/_Project/Content/Heroes/Kael/Prefabs/Kael_Greybox.prefab`, `Assets/_Project/Content/Shared/Prefabs/TrainingDummy_*.prefab`, `Assets/_Project/Scenes/Sandbox/CombatSandbox.unity` | ✅ |
| T11 | Testes PlayMode de combate (frame data, hits, estados, GC) | execução | executor | T10 | `Assets/_Project/Code/Tests/PlayMode/Combat/*` | ✅ |
| T12 | Revisão final, congelamento de contratos e integração | integração | principal | T1–T11 | `docs/roadmap/M05-runtime-combate.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ✅ |

## Ondas internas
1. T1
2. T2 ∥ T3 ∥ T4 (pastas disjuntas)
3. T5 ∥ T6 ∥ T7 ∥ T8 (pastas disjuntas; no máximo 2 executores simultâneos se preferir)
4. T9
5. T10
6. T11
7. T12

## Briefs

**T1 — Contratos compartilhados + asmdefs de teste**
- Objetivo: publicar os tipos que M06, M07, M08 e M09 usam sem editar, e preparar asmdefs de teste.
- Contexto: hooks canônicos de 06 §3.11 (`OnHitDealt, OnCritDealt, OnTickDealt, OnKill, OnOverkill, OnExecute, OnStaggerApplied, OnStaggered, OnStaggerBreak, OnCCApplied, OnInterrupt, OnDodge, OnPerfectDodge, OnParry, OnBlock, OnEvade, OnHitTaken, OnShieldBroken, OnLowHealth, OnFlaskUsed, OnDeath, OnRevive, OnSkillCast, OnSkillHit, OnResourceSpent, OnResourceGained, OnWallImpact, OnStealthEnter, OnStealthBreak`) + complementares de 11 §3.6.4. Todo evento carrega `Generation` (06 §3.11). `EntityEvents` sem alocação e `GameEvents` globais vêm do M01; `CombatEventStream` é o ring buffer que UI/Audio leem em `LateUpdate` (11 §3.7). `DamageInstance` é do M03 (não redefinir). `MassClass`/`IMassBody` já existem (M04).
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Combat/Contracts/{EntityId,Team,ICombatEntity,IDamageReceiver,IStatusReceiver,IStaggerable,CombatEvents,CombatEventStream,HitboxShape,StatusRequest,DotRequest,ProjectileSpec,AreaSpec}.cs`; ajusta `Eclipse.Gameplay.asmdef` (Burst, Collections, Mathematics) e os dois asmdefs de teste (referências listadas nas Premissas).
- Restrições: structs blittable onde possível; nenhuma dependência de UI/Audio; nomes de eventos idênticos a 06.
- Aceite: compila; teste EditMode garante que existe um tipo de evento por hook de 06 §3.11 (reflexão sobre a lista).
- Verificação: compilação limpa + EditMode `-testFilter "Eclipse.Tests.EditMode.Gameplay.Contracts"` → verde.
- Entrega: ≤ 200 palavras; lista dos tipos e a regra "M07/M08 não editam estes arquivos".

**T2 — Pool genérico**
- Objetivo: `PoolService` para GameObjects e objetos C# (11 §3.14: inimigos, projéteis, VFX, números, drops, áudio).
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Pooling/{PoolService,GameObjectPool,IPoolable,PoolConfig}.cs` e testes EditMode.
- Restrições: pré-aquecimento configurável; `Get/Release` sem alocação após warm-up; callbacks `OnSpawned/OnDespawned`.
- Aceite: teste aloca 1.000 get/release após warm-up com `GC.GetAllocatedBytesForCurrentThread` delta = 0.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Gameplay.Pooling"` → verde.
- Entrega: ≤ 120 palavras.

**T3 — Dados de arma e ataque**
- Objetivo: `WeaponDefinition` + `AttackChainDefinition` + `AttackStep` + `HitFeedbackProfile` alimentados por CSV.
- Contexto: `AttackStep` = startup/active/recovery (frames a 60 fps), forma da hitbox (setor raio+arco, caixa, cápsula, círculo, cone), offset, Stagger, hit-stop (f), Proc Coefficient, multiplicador de dano (08), lunge (0,5–1,5 m em cone 60° — 06 §3.4), Hit Group / re-hit, flags `Tracking`, `Commit`, carga (mín./máx. frames). Greatsword: golpes 1/2 20/6/40 setor 2,8 m 120° Stagger 15 hit-stop 5 f; golpe 3 26/8/34 Stagger 35 8 f; Forte toque 36 f, carga até 72 f, 6 ativos, 26 recovery, cone 3 m, Stagger 60→100, 10 f; Proc 1,0 (06 §3.6.2). Multiplicadores 110/110/160%, Forte 280/420% (08 §4.3). Dash Attack da classe Pesada 10/6/18, Stagger 20, 5 f (06 §3.6.1). Classe de peso da arma (06 §3.6.1). Weapon Power e dano por golpe por iLvl: tabela de 02 (usar o iLvl 1 para a sandbox).
- Arquivos: escreve os SOs em `Assets/_Project/Code/Data/Combat/`, os CSV em `DataSource/`, o importador em `Assets/_Project/Code/Editor/DataPipeline/Importers/WeaponImporter.cs` (descoberto pelo pipeline do M02 sem editar arquivo central) e os assets gerados em `Assets/_Project/Content/Heroes/Kael/Weapons/`.
- Restrições: `StableId` no formato `weapon.kael.greatsword` / `attack.kael.greatsword.1` (11 §3.4.1); o validador do M02 deve rejeitar frames ≤ 0.
- Aceite: import gera os assets; teste EditMode lê o SO e confere todos os números acima.
- Verificação: comando de import/validação do CLAUDE.md §6 → sem erros; EditMode `-testFilter "Eclipse.Tests.EditMode.Data.Combat"` → verde.
- Entrega: ≤ 150 palavras.

**T4 — Entidade de combate, vida, dano recebido, morte, Flask**
- Objetivo: `CombatEntity` (implementa `ICombatEntity`, `IDamageReceiver`) com `StatBlock` do M03, vida/escudo, pipeline de dano recebido e morte.
- Contexto: ordem de 06 §3.3: i-frames/Intangível/Parry negam (i-frames negam `Unavoidable`, não `Annihilation`; Parry não nega `Unblockable` nem `Annihilation`) → base × escala → Dodge Chance (só golpes não telegrafados; nunca ticks/`Unavoidable`) → mitigação por tipo (`DamageResolver`) → Block −40% (não para ticks/`Unavoidable`/`Unblockable`) → Damage Reduction (cap 40%) → Shield → vida. Grace period: 0,25 s após hit; 0,8 s após hit ≥ 20% da vida máxima; ticks não iniciam grace e são limitados a 1 por fonte/0,5 s (06 §3.14). Morte: 0,5 s de câmera lenta só na apresentação; Death Recap guarda os últimos 5 hits (fonte, tipo, valor, mitigação, status). Revive: API pronta, sem fonte (só Blessing de 09 depois). Flask: 3 cargas, 35% da vida em 1 s, não usável durante Heavy/Ultimate, kill de Elite devolve 1 carga (máx. 1 por sala) (06 §3.12). `InvulnerabilityState` do M04 é reaproveitado.
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Combat/Entities/{CombatEntity,HealthComponent,ShieldComponent}.cs`, `.../Combat/Damage/{DamageApplier,IncomingDamagePipeline,GracePeriod}.cs`, `.../Combat/Death/{DeathHandler,DeathRecap}.cs`, `Assets/_Project/Code/Gameplay/Player/AetherFlask.cs`.
- Restrições: nenhum cálculo de dano fora do `DamageResolver`; eventos `OnHitTaken`, `OnBlock`, `OnEvade`, `OnShieldBroken`, `OnLowHealth`, `OnDeath`, `OnFlaskUsed` emitidos.
- Aceite: testes EditMode cobrindo cada etapa da ordem (incluindo flags) e grace period.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Gameplay.Damage"` → verde.
- Entrega: ≤ 180 palavras.

**T5 — `AttackController`**
- Objetivo: máquina de combo genérica dirigida por `AttackChainDefinition`.
- Contexto: Startup → Active → Recovery; Attack Speed escala as três igualmente (cap provisório 2,0× — 01), hit-stop não é escalado; tabela de cancelamentos de 06 §3.5 (Básico→Esquiva em toda a Recovery; em Startup só 4 primeiros frames para peso Leve/Chakrams/Twin Wands; Básico→Habilidade a partir do 1º frame de Recovery; janela de combo = Recovery; reset após 0,8 s; Forte carregando→Esquiva sempre, perde carga; Esquiva→Ataque nos últimos 8 frames vira Dash Attack; Flinch→Esquiva após 6 f). Movimento: melee travado (só lunge), Ranged/Caster 60% (06 §3.5) via `MovementLock` do M04. Rotação: livre no startup, travada no ativo (exceto `Tracking`), livre na recovery (06 §3.4). Input vem do `InputBuffer` do M04. Expor `CanCancelInto(ActionKind)` para o M08.
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Combat/Attacks/{AttackController,AttackRuntimeState,CancelRules,LungeSolver,ChargeTracker}.cs`.
- Restrições: avança somente por `FixedTick`; respeita `EntityClock` (hit-stop congela o atacante).
- Aceite: teste conta frames de cada fase do Greatsword (Attack Speed 1,0 e 1,5) e valida cada linha da tabela de cancelamento.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Combat.Attacks"` (T11) → verde.
- Entrega: ≤ 180 palavras.

**T6 — Hit detection**
- Objetivo: avaliar hitboxes lógicas por janela ativa.
- Contexto: formas setor/caixa/cápsula ancoradas no personagem; avaliação em cada tick ativo com `Physics.OverlapCapsuleNonAlloc`/`OverlapBoxNonAlloc` + filtro angular para setor; **sweep** entre pose anterior e atual quando Attack Speed > 1,3 ou forma rápida; `HitRegistry`: cada alvo uma vez por Hit Group, ou re-hit por intervalo; raycast de LOS na camada `Wall` (06 §3.9, 11 §3.9). Hurtbox do jogador com raio 15% menor que a silhueta (06 §3.9). Saída: `DamageInstance` (M03) com `Generation = 0`, tags da fonte, Proc Coefficient, Stagger, `hitGroupId`.
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Combat/HitDetection/{HitboxEvaluator,ShapeQueries,HitRegistry,Hurtbox,LineOfSight}.cs`.
- Restrições: buffers NonAlloc pré-alocados; 0 B GC por tick.
- Aceite: testes PlayMode: alvo dentro/fora do setor; não atinge através de `Wall`; um hit por grupo; Attack Speed 2,0 com alvo fino não sofre tunneling.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Combat.HitDetection"` (T11) → verde.
- Entrega: ≤ 150 palavras.

**T7 — Estados genéricos e Stagger**
- Objetivo: todos os estados de 06 §3.8 num `StateComponent` + `StaggerComponent`.
- Contexto: massa (06 §3.8.1: Light flinch com Stagger ≥ 1, Medium ≥ 10, Heavy sem flinch e Super Armor ao atacar, Elite dobro e ×0,6 knockback/CC, Colossal imune); knockback com Wall Impact (10% do hit, Stun 0,5 s); Stagger: limiares Medium 40 / Heavy 120 / Elite ×2,5 / Guardian 400 / Chefe 800, multiplicador por dificuldade, decaimento 15%/s após 3 s, Staggered 1,5/2,0/3,0 s com +20% Vulnerability, Broken de chefe 4 s +30% Vulnerability e hit-stop global 6 f, limiar +50% por Break até 3× (06 §3.8.2, R11); CC → Stagger em Colossal (`duração × 40`); estados de 06 §3.8.3 (Stun, Freeze, Root, Silence, Taunt, Knockback, Pull, Slow (vale o mais forte; teto −60% inimigo; piso 40% jogador), Super Armor, Unstoppable, Stealth (máx. 4 s, ICD 1 s, quebra ao causar/receber dano), Parry 12 f com teto 0,3 s, Intangível); DR por grupo via `CrowdControlTracker` do M03 (R09); CC no jogador: Hard máx. 1,5 s, Freeze 1,0 s, 2 s de imunidade, Root 2 s; reação do jogador (flinch 8 f cancelável após 6 f; Knockdown 24 f com i-frames nos últimos 10) (06 §3.8.4). Frostbound/Distortion apenas como ganchos (efeitos do M11).
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Combat/States/*`, `Assets/_Project/Code/Gameplay/Combat/Stagger/*`.
- Restrições: valores em SO/CSV (`DataSource/CombatStates.csv`, importador próprio em `Editor/DataPipeline/Importers/CombatStatesImporter.cs`); eventos `OnStaggerApplied`, `OnStaggered`, `OnStaggerBreak`, `OnCCApplied`, `OnInterrupt`, `OnParry`, `OnWallImpact`, `OnStealthEnter/Break`.
- Aceite: testes EditMode/PlayMode para limiares, decaimento, DR (100/50/25/imune), caps do jogador, Parry 12 f, Unstoppable bloqueando CC e adiando Staggered.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Gameplay.States"` + PlayMode `-testFilter "Eclipse.Tests.PlayMode.Combat.States"` → verdes.
- Entrega: ≤ 200 palavras.

**T8 — Projéteis, áreas, zonas, DoT, `StatusStub`, `TimedModifier`**
- Objetivo: serviços de entrega de dano compartilhados por IA e habilidades.
- Contexto: `ProjectileManager` cinemático com `SpherecastCommand` em job, até 2.000 ativos, propriedades velocidade/raio/vida/`Pierce`/`Chain`/`Fork`/`Return`/`Homing` (as quatro últimas podem ficar com implementação básica + testes; o M10 aprofunda), bloqueio por `Wall`, `LowCover` só com tag `Area`, não atinge o mesmo alvo duas vezes, trajetória de artilharia em arco (09 §4.2) (06 §3.9, 11 §3.9). Áreas: overlap no frame de detonação com LOS do centro; zonas persistentes avaliam ocupantes a cada 0,5 s, zonas da mesma habilidade+fonte não acumulam no alvo (06 §3.2, §3.9). DoT genérico: snapshot na aplicação, tick 0,5 s, sem crit, mesma skill+fonte não acumula (vale o mais forte, renova duração) (06 §3.2). `StatusStub` (implementa `IStatusReceiver`): `StackIntensity(max)` com duração renovável — usado como Burning provisório (07 §4: 4 s, máx. 10 stacks, 8% DRef Ember/stack/s; aplicação garantida, sem buildup). `TimedModifier`: aplica `StatMod` por duração (Vulnerability etc.).
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Combat/Projectiles/*`, `.../Combat/Areas/*`, `.../Combat/Status/{StatusStub,DotRunner,TimedModifier}.cs`.
- Restrições: pooling do T2; 0 B GC por tick; dano sempre via `DamageResolver`.
- Aceite: testes PlayMode: 2.000 projéteis ativos sem GC; zona tick a cada 30 ticks; DoT snapshot não muda com buff aplicado depois; Burning stub limita 10 stacks.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Combat.Delivery"` (T11) → verde.
- Entrega: ≤ 180 palavras.

**T9 — Feedback**
- Objetivo: `FeedbackSystem` dirigido por `HitFeedbackProfile`.
- Contexto: hit-stop local (atacante + alvo) por golpe (§3.6.2), +2 f em crit, 8 f em kill de Elite, 6 f **global** para Ultimate/Break de chefe/troca de fase; no máximo 1 hit-stop a cada 100 ms por atacante; em projéteis só o alvo. Shake por trauma (`trauma²`, decai 1,5/s): hit Heavy 0,15, crit Heavy 0,25, Wall Impact 0,2, Ultimate 0,5, golpe de chefe recebido 0,4; Light/Medium não geram shake (06 §3.11). Flash branco 2 f, squash 0,1 s em Light/Medium, partículas de material greybox. Usa `EntityClock` (M04) e `CameraShakeService` (M04). Hooks `VFX_*`/`SFX_*` emitidos como eventos para M06/M09 (não chamar UI/Audio diretamente).
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Feedback/{FeedbackSystem,HitStopService,ImpactFlash,SquashEffect,MaterialImpactVfx}.cs`, perfis em `Assets/_Project/Content/Shared/Feedback/`.
- Aceite: teste PlayMode mede congelamento exato de 5 f (golpe 1) e 7 f (golpe 1 crit) do atacante e alvo, e terceiro alvo não congela; global 6 f congela todos.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Combat.Feedback"` → verde.
- Entrega: ≤ 150 palavras.

**T10 — Greatsword greybox + bonecos de treino**
- Objetivo: tornar o combate visível e testável à mão.
- Contexto: Kael greybox (prefab do M04) ganha `CombatEntity`, `AttackController`, arma primitiva com giro procedural sincronizado ao `AttackStep`; bonecos `TrainingDummy_Light/Medium/Heavy/Colossal` com massa, barra de Stagger (gizmo/debug text, HUD é M06) e vida infinita opcional; atacante de teste simples que dispara um golpe telegrafado a cada 2 s (para testar esquiva, Parry e grace period).
- Arquivos: escreve os prefabs listados e atualiza `Assets/_Project/Scenes/Sandbox/CombatSandbox.unity`.
- Restrições: este é o último módulo antes de M08 a editar `CombatSandbox.unity`; M07 usa cenas próprias.
- Aceite: Play na sandbox permite combo, Forte carregado, esquiva e ver Stagger/hit-stop nos bonecos sem erros no console.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Combat.Sandbox"` (cena carrega e boneco recebe dano de um combo simulado) → verde.
- Entrega: ≤ 120 palavras.

**T11 — Testes PlayMode de combate**
- Objetivo: provar os critérios 2–10 com testes determinísticos.
- Contexto: casos mínimos: frames de cada fase do Greatsword a AS 1,0 e 1,5; tabela de cancelamento; dano do golpe 3 = 160% do dano por golpe contra boneco sem mitigação; uma única aplicação por Hit Group; sem tunneling a AS 2,0; i-frames da esquiva negam hit `Unavoidable` mas não `Annihilation`; Parry nega hit comum e não `Unblockable`; grace 0,25/0,8 s; Stagger até Staggered no Medium (40); DR de CC 100/50/25/imune; hit-stop 5/7/6-global; 2.000 projéteis e combo de 60 s com 0 B GC/frame (`ProfilerRecorder` "GC Allocated In Frame").
- Arquivos: escreve `Assets/_Project/Code/Tests/PlayMode/Combat/*`.
- Aceite: todos verdes em batchmode.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Combat"` → 0 falhas.
- Entrega: ≤ 150 palavras; lista de casos.

**T12 — Revisão final, congelamento de contratos e integração (principal)**
- Objetivo: confirmar critérios, registrar no Log a lista final de contratos congelados e quem pode estendê-los.
- Arquivos: este arquivo, `docs/ROADMAP.md` §7, `CLAUDE.md` (regra "contratos de `Gameplay/Combat/Contracts` só mudam com aval do lead"; pastas novas; comandos de teste de combate).
- Aceite: critérios 1–11 ✅ com saídas no Log; checklist manual do usuário (combo e esquiva "sentem bem" no teclado e gamepad) registrado.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Combat" -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
→ validador sem erros; 0 falhas nos dois XML; nenhum `error CS`.

## Riscos e rollback
- **Contrato insuficiente para M07/M08** → é o risco principal; o T12 revisa a lista contra os briefs de M07/M08 antes de fechar. Mudança posterior exige PR próprio no `Contracts` antes de M07/M08 continuarem.
- `SpherecastCommand` com 2.000 projéteis acima do orçamento → reduzir para um job de distância simples + broadphase por grade, mesma API.
- Sweep caro → aplicar só em golpes marcados ou AS > 1,3 (já previsto).
- Hit-stop local conflitando com tick global → `EntityClock` isolado (M04); se falhar, hit-stop só visual (animação) mantendo lógica; registrar.

## Atualizações de documentação
- `CLAUDE.md`: pasta `Gameplay/Combat/Contracts` e regra de congelamento; comandos de teste de combate; pegadinhas de NonAlloc/Jobs.
- `docs/ROADMAP.md` §7: status do M05; nota para M07/M08 sobre os contratos.
- Se o M03 precisou de ajuste em `DamageInstance`, registrar no Log do M03.

## Prompt de disparo
```
Implemente o módulo M05 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M05-runtime-combate.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M05-runtime-combate.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.
- 2026-11-08 Implementado T1–T11 (commits `feat(M05)`/`test(M05)` em `modulo/M05-runtime-combate`). Verificação: dotnet 462/462 (Core intocado), EditMode 813/813, PlayMode completo 65/65 (Combat 46 + smoke 2 + M04 17; limpeza de cena em `CombatSceneCleanup` evita que a sandbox carregada bloqueie os testes de Player), `ImportAndValidate` 20 tabelas, 0 erros (1 aviso antigo: `ModifierReference`, `modifiers.csv` não existe), 0 `error CS`.
- **Contratos congelados** (`Gameplay/Combat/Contracts`, namespace `Eclipse.Gameplay.Combat`): `EntityId, Team, ICombatEntity, IDamageReceiver (+HitResult/DamageOutcome), IStatusReceiver, IStaggerable, StatusRequest, DotRequest, ProjectileSpec, AreaSpec, HitboxShape, CombatEvents` (um struct por hook de 06 §3.11 + 13 complementares de 11 §3.6.4, todos com `Frame`/`Generation`; `OnDodge`/`OnPerfectDodge` reaproveitam `Eclipse.Gameplay.Events` do M04) e `CombatEventStream` (um `FrameEventBuffer<T>` por tipo; `EndFrame` no fim do `LateUpdate`; `Prewarm<T>()` no load). M07/M08/M06/M09 só estendem por arquivos novos; mudança exige aval do lead e PR próprio. Asmdefs de teste ajustados uma vez (AI/UI/Audio/Input System/Cinemachine/Burst/Collections/Mathematics). Eventos extras fora dos contratos: `OnGlobalHitStop` (`Combat/States`), `FeedbackHooks.OnVfxHook/OnSfxHook`.
- **APIs para M07/M08:** `CombatEntity` (+overload `ReceiveHit(hit, dir, IncomingHitInfo)` com telegraph/eLvl, pois o contrato assume telegrafado), `StateComponent`/`StaggerComponent`, `AttackController.CanCancelInto(ActionKind[, commit])`, `ProjectileManager`/`AreaDeliverer`/`ZoneRunner`/`DotRunner`, `StatusStub.BurningProvisional`, `TimedModifierSet`, `PoolService`. `CombatEntity` já publica `OnKill/OnOverkill/OnParry`; quem emitir hits de melee/projétil deve publicar `OnHitDealt` (feito em projéteis/áreas e `CombatReceiver`). `Eclipse.Gameplay.Player.HitResult` colide com `Combat.HitResult` (usar alias).
- **Lacunas de docs (nada inventado; M08/lead decidem):** distância do lunge do Greatsword = 0 (06 §3.4 dá só 0,5–1,5 m "por golpe"; solver pronto, curva uniforme na Startup); arco do cone do Forte e hitbox do Dash Attack vazios (tratados como "sem hitbox" + aviso único); dano por golpe iLvl 1 derivado de 02 §3.4 (Weapon Power 9 → 10); cap de Attack Speed provisório 2,0 no prefab; 06 §3.14 omite se Annihilation ignora grace (assumido: ignora); Flask sem reuso durante a cura (assumido); decaimento de Stagger 15% do máximo/s; Heavy sem Knockdown vira Flinch; Elite Light sem barra; Colossal usa barra Guardian 400 na sandbox; `lowHealthFraction` = 0 (desligado); valores do atacante/boneco de teste em `CombatRuntimeConfig`; Burning (07 §4) em código, sem SO/CSV (M11 substitui); `status.burning` como slug; zona tica 1ª vez após 30 f; `Return` volta ao ponto de origem.
- **Desvios/pendências:** Dodge→Basic usa os últimos 8 f (06 §3.5) enquanto `DodgeConfig.cancelFromFrame` do M04 = 15 (últimos 4 f); Dash Attack sobrepõe os últimos frames da esquiva; sem `StatId.Vulnerability` (Vulnerability de Staggered/Broken vem do Stagger; `TimedModifierSet` pronto); sem reação de flinch/knockdown/knockback do jogador na sandbox; Chain/Homing sem teste ponta a ponta; `PhysicsAreaQuery`/`PhysicsProjectileTargetSearch` sem teste de física real; teste de GC no PlayMode usa tolerância (mediana ≤ ociosa; o Editor gera ruído de 0,6–3% dos frames) — 0 B estrito só vale no dotnet/Player build; teste de 0 B do pool só roda fora do Unity e não é compilado pelo CoreTests (pool vive em Gameplay); `SandboxBuilder.Run` 2× sem alterar arquivos não foi verificado.
- **Pendente do usuário:** checklist manual — combo, Forte carregado e esquiva "sentem bem" no teclado e no gamepad na `CombatSandbox` (F1 parry, F2 vida infinita, F3 overlay, F4 atacantes).


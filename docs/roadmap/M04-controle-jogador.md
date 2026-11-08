# M04 — Controle do jogador (input, câmera, movimento, esquiva)
Fase 1 · Onda 3 · Status: ✅ concluído (pendente: checagem manual do usuário)
Depende de: M01 (e M00) · Desbloqueia: M05 · Paralelo com: M12 (Core/Items — pastas disjuntas)
Branch: modulo/M04-controle-jogador
Docs de referência: 06-combate §2 (decisões 1, 2, 9, 10, 15, 18), §3.4, §3.5 (movimento durante ataques), §3.7, §3.11 (câmera); 08-herois §3.1 (Movement Speed); 01-atributos (piso de Movement Speed); 11-arquitetura §3.1, §3.3, §3.9; 00 §5 R04, R05, R06

## Objetivo
Ter um personagem greybox controlável na sala de teste, com input M+K e gamepad, buffer de input, câmera isométrica de 06 e esquiva universal com i-frames exatos, tudo rodando sobre um agendador de tick fixo de 60 Hz que permite testes frame a frame.

## Critérios de saída (verificáveis)
1. `Time.fixedDeltaTime = 1/60` no projeto; todo sistema de gameplay deste módulo roda via `TickScheduler` e pode ser avançado manualmente em teste (06 §2.1, R04).
2. Action Maps `Gameplay`, `UI`, `Debug` com o mapeamento de 06 §3.4 (M+K e gamepad Xbox), incluindo Q/E/R/Shift, F e 1 (R05).
3. `InputBuffer` de 9 frames com prioridade Esquiva > Habilidade > Forte > Básico e "última entrada vence" na mesma prioridade (06 §3.4) — testes EditMode verdes.
4. Movimento WASD + mira no cursor projetada no plano do chão à altura do peito; gamepad com stick direito e fallback para direção do movimento (06 §3.4). Velocidade de Kael 6,0 m/s (08 §3.1/01).
5. Esquiva: 18 f, 4,5 m (ease-out), i-frames nos frames 2–13, recovery 3 f, cancelável a partir do frame 15, 2 cargas, recarga sequencial 1,5 s/carga; Movement Speed e CDR não alteram (06 §3.7, R06). Testes PlayMode medem distância (±0,05 m) e janela de i-frames frame a frame.
6. API de Perfect Dodge pronta para o M05 (06 §3.7) e evento `OnDodge`/`OnPerfectDodge` emitidos.
7. Câmera Cinemachine 3: perspectiva FOV 30°, pitch 50°, yaw 45° fixo, distância 22 m (zoom 18–26), look-ahead 15% até 3 m, oclusão por dither (06 §3.11).
8. Camadas físicas e matriz de colisão de 11 §3.9 configuradas (únicas para todo o projeto).
9. EditMode + PlayMode verdes; projeto compila sem warnings novos.

## Fora de escopo
Ataques, hitboxes e dano (M05); habilidades (M08); HUD (M06); remapeamento de teclas em tela de opções (M31); variação de esquiva por herói (não existe — R06).

## Premissas e decisões
- **Tick:** `TickScheduler` (MonoBehaviour único) chama `IFixedTickable.FixedTick(int frame)` em `FixedUpdate`; em teste, `TickScheduler.StepManual(n)` avança n ticks sem depender de tempo real. Suporta **relógio local por entidade** (`EntityClock`) com "frames congelados", necessário para o hit-stop local do M05 (06 §3.11).
- **Movimento cinemático** com cápsula + `Physics.CapsuleCast` (sem Rigidbody dinâmico), para determinismo.
- **Classe de massa:** o enum `MassClass` (Light/Medium/Heavy/Colossal — 06 §3.8.1) e a interface `IMassBody` nascem aqui porque a esquiva precisa deles (atravessa Light/Medium, para em Heavy/Colossal/paredes — 06 §3.7). O M05 os reutiliza.
- **Perfect Dodge (interpretação de 06 §3.7):** um hit inimigo negado pelas i-frames cujo frame de impacto ocorre até 6 frames depois do início das i-frames (frames 2–7 da esquiva) é Perfect Dodge. Registrar no Log e confirmar com o usuário no fechamento.
- `MovementProfile` (SO) guarda velocidade base e aceleração; o `HeroDefinition` (M08) passará a referenciá-lo.
- Arte: cápsula greybox com "nariz" indicando direção (D4).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Tick fixo 60 Hz, `TickScheduler`, `EntityClock` | execução | executor | — | `Assets/_Project/Code/Gameplay/Simulation/*`, `ProjectSettings/TimeManager.asset`, `Assets/_Project/Code/Tests/EditMode/Gameplay/Simulation/*` | ✅ |
| T2 | Camadas físicas e matriz de colisão | execução | executor | — | `ProjectSettings/TagManager.asset`, `ProjectSettings/DynamicsManager.asset`, `Assets/_Project/Code/Gameplay/Physics/PhysicsLayers.cs` | ✅ |
| T3 | Input System: actions, wrapper e `InputBuffer` | execução | executor | T1 | `Assets/_Project/Settings/Input/EclipseInput.inputactions`, `Assets/_Project/Code/Gameplay/Input/*`, `Assets/_Project/Code/Tests/EditMode/Gameplay/Input/*` | ✅ |
| T4 | Player controller: movimento e mira | execução | executor | T1, T2, T3 | `Assets/_Project/Code/Gameplay/Player/PlayerMotor.cs`, `.../Player/PlayerAim.cs`, `.../Player/MovementProfile.cs`, `Assets/_Project/Code/Gameplay/Entities/MassClass.cs`, `.../Entities/IMassBody.cs` | ✅ |
| T5 | Esquiva universal + API de i-frames/Perfect Dodge | execução | executor | T4 | `Assets/_Project/Code/Gameplay/Player/DodgeController.cs`, `.../Player/InvulnerabilityState.cs`, `.../Player/DodgeConfig.cs` | ✅ |
| T6 | Câmera isométrica Cinemachine + oclusão por dither | execução | executor | T4 | `Assets/_Project/Code/Gameplay/Camera/*`, `Assets/_Project/Art/Shaders/OcclusionDither.shadergraph`, `Assets/_Project/Settings/Camera/*` | ✅ |
| T7 | Cena Combat Sandbox greybox + prefabs de bootstrap e jogador | execução | executor | T4, T5, T6 | `Assets/_Project/Scenes/Sandbox/CombatSandbox.unity`, `Assets/_Project/Content/Heroes/Kael/Prefabs/Kael_Greybox.prefab`, `Assets/_Project/Content/Shared/Prefabs/GameplayBootstrap.prefab` | ✅ |
| T8 | Testes PlayMode de movimento, esquiva e câmera | execução | executor | T5, T6, T7 | `Assets/_Project/Code/Tests/PlayMode/Player/*` | ✅ |
| T9 | Revisão final e integração | integração | principal | T1–T8 | `docs/roadmap/M04-controle-jogador.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ✅ |

## Ondas internas
1. T1 ∥ T2
2. T3
3. T4
4. T5 ∥ T6 (arquivos disjuntos)
5. T7
6. T8
7. T9

## Briefs

**T1 — Tick fixo 60 Hz, `TickScheduler`, `EntityClock`**
- Objetivo: base de simulação determinística a 60 Hz usada por todo o gameplay.
- Contexto: 06 §2.1 e 11 §3.9 exigem combate em `FixedUpdate` a 60 Hz com frame data em frames; testes precisam avançar frames sem tempo real. Hit-stop do M05 é local (atacante + alvo), então cada entidade precisa poder "congelar" n frames sem parar o resto (06 §3.11).
- Arquivos: lê `CLAUDE.md`, 11 §3.9; escreve `Assets/_Project/Code/Gameplay/Simulation/{TickScheduler,IFixedTickable,EntityClock,SimulationFrame}.cs`, `ProjectSettings/TimeManager.asset` (Fixed Timestep 0.0166667), testes em `Assets/_Project/Code/Tests/EditMode/Gameplay/Simulation/`.
- Restrições: assembly `Eclipse.Gameplay`; sem alocação por tick; ordem de execução determinística (prioridade inteira + ordem de registro).
- Aceite: `StepManual(60)` chama cada tickable 60 vezes com frames 1..60; `EntityClock.Freeze(5)` faz a entidade pular exatamente 5 ticks; tickables registrados durante o tick entram no próximo.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Gameplay.Simulation"` → esperado: todos verdes.
- Entrega: ≤ 150 palavras; arquivos, decisões de API, o que não foi verificado.

**T2 — Camadas físicas e matriz de colisão**
- Objetivo: definir, uma única vez no projeto, as camadas de 11 §3.9 e a matriz mínima.
- Contexto: camadas `Hurtbox_Player`, `Hurtbox_Enemy`, `Hitbox_Player`, `Hitbox_Enemy`, `Environment`, `Projectile`, `Interactable`; acrescentar `Wall` e `LowCover` exigidas por 06 §3.9 (golpes não atravessam `Wall`; projéteis atravessam `LowCover` só com tag `Area`) e `Character` para colisão de movimento. Módulos seguintes (M05, M07, M08) **não podem** editar estes arquivos.
- Arquivos: escreve `ProjectSettings/TagManager.asset`, `ProjectSettings/DynamicsManager.asset`, `Assets/_Project/Code/Gameplay/Physics/PhysicsLayers.cs` (constantes e máscaras).
- Restrições: queries de combate usam máscaras explícitas; nenhuma colisão física entre hitboxes (são consultas, não colliders ativos).
- Aceite: `PhysicsLayers` expõe constantes que batem com o TagManager; teste EditMode verifica `LayerMask.NameToLayer` de cada nome ≥ 0 e a matriz esperada (`Physics.GetIgnoreLayerCollision`).
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Gameplay.Physics"` → verde.
- Entrega: ≤ 120 palavras; tabela camada → índice.

**T3 — Input System: actions, wrapper e `InputBuffer`**
- Objetivo: mapear 06 §3.4 em Action Maps e entregar o buffer de input com prioridade.
- Contexto: M+K: WASD, mira no cursor, LMB básico (segurar repete), RMB forte (segurar carrega), Space esquiva, Q/E/R habilidades 1–3, Shift habilidade 4, F Ultimate, 1 Flask, G interagir, Tab mapa, I/C inventário/personagem, Alt ping. Gamepad Xbox: sticks, X/Y básico/forte, A esquiva, B interagir, LB/RB/LT habilidades 1–3, RT habilidade 4, LB+RB Ultimate, D-pad ↑ Flask, View/Menu. Buffer: 9 frames (150 ms); prioridade Esquiva > Habilidade > Forte > Básico; na mesma prioridade vale a última. Mapas `UI` e `Debug` (console de debug do M06 usa `Debug`; tecla sugerida `` ` ``).
- Arquivos: escreve `Assets/_Project/Settings/Input/EclipseInput.inputactions` (+ classe C# gerada), `Assets/_Project/Code/Gameplay/Input/{PlayerInputReader,InputBuffer,BufferedAction,InputActionId}.cs`, testes em `Tests/EditMode/Gameplay/Input/`.
- Restrições: o buffer consome frames do `TickScheduler` (T1), não `Time.time`; o reader não conhece habilidades (só IDs de slot).
- Aceite: testes cobrindo expiração após 9 frames, prioridade entre tipos, última entrada vence, segurar LMB gera repetição, LB+RB gera Ultimate sem disparar LB/RB isolados.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Gameplay.Input"` → verde.
- Entrega: ≤ 150 palavras.

**T4 — Player controller: movimento e mira**
- Objetivo: movimento twin-stick determinístico e mira projetada.
- Contexto: WASD + mira no cursor projetada no plano do chão à altura do peito; gamepad: stick direito, sem ele mira = direção do movimento (06 §3.4). Velocidade base de Kael 6,0 m/s (08 §3.1, valores de 01); piso de Movement Speed 40% da base (01). Movimento cinemático com `CapsuleCast` contra `Environment`/`Wall`/`Character`. Criar `MassClass` e `IMassBody` (06 §3.8.1) em `Gameplay/Entities/` para uso da esquiva e do M05.
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Player/{PlayerMotor,PlayerAim,MovementProfile}.cs`, `Assets/_Project/Code/Gameplay/Entities/{MassClass,IMassBody}.cs`, `Assets/_Project/Content/Heroes/Kael/Data/Kael_MovementProfile.asset`.
- Restrições: sem `Rigidbody` dinâmico; movimento aplicado em `FixedTick`; interpolação visual em `Update`. Expor `MovementLock` (0%, 60%, 100%) para o M05 (06 §3.5).
- Aceite: em 60 ticks de input "frente" o deslocamento é 6,0 m ± 0,05 (sem aceleração se `MovementProfile.accel = ∞`); não atravessa paredes; mira retorna ponto correto para câmera de teste.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Player.Movement"` (escrito em T8) e compilação limpa.
- Entrega: ≤ 150 palavras.

**T5 — Esquiva universal + API de i-frames/Perfect Dodge**
- Objetivo: esquiva exatamente como 06 §3.7 e API de invulnerabilidade para o M05.
- Contexto: 18 f; 4,5 m com curva ease-out; i-frames frames 2–13; recovery 3 f; cancelável em ataque/habilidade a partir do frame 15; 2 cargas (máx. 3 só por fonte explícita); recarga 1,5 s por carga, sequencial; Movement Speed e CDR não alteram; atravessa `IMassBody` Light/Medium e para em Heavy/Colossal/`Wall`. Perfect Dodge: hit negado cujo impacto cai até 6 frames após o início das i-frames → `OnPerfectDodge`. Input via buffer (prioridade máxima).
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Player/{DodgeController,InvulnerabilityState,DodgeConfig}.cs`, `Assets/_Project/Content/Heroes/Shared/Data/DodgeConfig_Universal.asset`.
- Restrições: valores só no `DodgeConfig` (SO), nada hardcoded; `InvulnerabilityState` é genérico (servirá a Intangível/revive no M05 — 06 §3.8.3, §3.14). Eventos `OnDodge`/`OnPerfectDodge` como `struct` em `EntityEvents` do M01.
- Aceite: `InvulnerabilityState.IsInvulnerable(frame)` verdadeiro só nos frames 2–13 da esquiva; `NotifyIncomingHit(impactFrame)` retorna `Negated`/`PerfectDodge`/`NotNegated`; terceira esquiva sem carga é recusada; recarga sequencial.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Player.Dodge"` (T8) → verde.
- Entrega: ≤ 150 palavras.

**T6 — Câmera isométrica Cinemachine + oclusão por dither**
- Objetivo: câmera de 06 §3.11.
- Contexto: perspectiva, FOV vertical 30°, pitch 50°, yaw 45° fixo, distância 22 m (zoom 18–26 m), look-ahead 15% da distância até cursor/direção de mira limitado a 3 m, geometria entre câmera e jogador translúcida por dither; impulso de shake recebe "trauma" do M05 (`shake = trauma²`, decai 1,5/s — o modelo de trauma é implementado aqui como `CameraShakeService` e alimentado depois).
- Arquivos: escreve `Assets/_Project/Code/Gameplay/Camera/{IsoCameraRig,CameraLookAhead,OcclusionFader,CameraShakeService}.cs`, `Assets/_Project/Art/Shaders/OcclusionDither.shadergraph`, `Assets/_Project/Settings/Camera/IsoCamera_Default.asset`.
- Restrições: Cinemachine 3 (`CinemachineCamera`); parâmetros num SO; slider de intensidade de shake (0–100%, padrão 70%) já exposto em API (opções no M31).
- Aceite: teste PlayMode verifica FOV, pitch/yaw e distância; look-ahead nunca passa 3 m; objeto entre câmera e jogador recebe o fader.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Player.Camera"` (T8) → verde.
- Entrega: ≤ 150 palavras.

**T7 — Cena Combat Sandbox greybox + prefabs**
- Objetivo: sala de teste reutilizada pelos módulos de combate.
- Contexto: arena ~30 × 30 m com piso, paredes (`Wall`), obstáculos baixos (`LowCover`), pilar para teste de oclusão; `GameplayBootstrap.prefab` com `TickScheduler`, câmera e serviços; `Kael_Greybox.prefab` (cápsula + indicador de direção) com motor, mira e esquiva. Arte greybox (D4).
- Arquivos: escreve `Assets/_Project/Scenes/Sandbox/CombatSandbox.unity`, `Assets/_Project/Content/Heroes/Kael/Prefabs/Kael_Greybox.prefab`, `Assets/_Project/Content/Shared/Prefabs/GameplayBootstrap.prefab`, materiais greybox em `Assets/_Project/Art/Greybox/`.
- Restrições: a cena deve abrir e entrar em Play sem erros no console; nada de lógica na cena além de prefabs.
- Aceite: Play na cena permite andar, mirar e esquivar com teclado e gamepad (checagem manual descrita no Log) e o teste "cena carrega sem erros" passa.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Player.Sandbox"` → verde.
- Entrega: ≤ 120 palavras.

**T8 — Testes PlayMode de movimento, esquiva e câmera**
- Objetivo: provar os critérios 4–7 frame a frame.
- Contexto: usar `TickScheduler.StepManual`; input simulado pelo `InputTestFixture` do Input System. Casos: distância da esquiva 4,5 m ± 0,05 após 18 ticks; i-frames exatamente frames 2–13; cancelamento só a partir do frame 15; 2 cargas e recarga sequencial 1,5 s; Movement Speed +50% não altera a esquiva; esquiva para em `IMassBody` Heavy e atravessa Medium; Perfect Dodge classificado; câmera conforme T6; cena sandbox carrega sem erros.
- Arquivos: escreve `Assets/_Project/Code/Tests/PlayMode/Player/*`.
- Restrições: testes determinísticos, sem `WaitForSeconds` de tempo real.
- Aceite: todos verdes em batchmode.
- Verificação: PlayMode (CLAUDE.md §6) `-testFilter "Eclipse.Tests.PlayMode.Player"` → verde; EditMode completo verde.
- Entrega: ≤ 150 palavras; lista de casos e resultados.

**T9 — Revisão final e integração (principal)**
- Objetivo: conferir critérios de saída, atualizar status e documentação.
- Arquivos: escreve este arquivo (Status/Log), `docs/ROADMAP.md` §7, `CLAUDE.md` (novas pastas `Gameplay/Simulation`, `Settings/Input`, comando de teste por filtro, pegadinhas do Input System/Cinemachine).
- Aceite: todos os critérios ✅ com saída dos comandos colada no Log; interpretação de Perfect Dodge confirmada com o usuário.
- Verificação: EditMode + PlayMode completos → verdes.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -quit -logFile Logs/compile.log
```
→ exit 0 e nenhum `error CS` em `Logs/compile.log`.
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Player" -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
→ 0 falhas nos dois XML. Checagem manual do usuário: andar/mirar/esquivar com teclado e gamepad na `CombatSandbox`.

## Riscos e rollback
- Cinemachine 3 mudou API em relação ao 2.x → usar só `CinemachineCamera`/`CinemachinePositionComposer`; se travar, câmera própria simples atrás de `IsoCameraRig` (mesma interface).
- `InputTestFixture` instável em batchmode → testar `InputBuffer`/reader com eventos sintéticos sem dispositivo.
- Interpretação de Perfect Dodge errada → parâmetro `PerfectDodgeWindowFrames` no `DodgeConfig`; ajuste só de dado.

## Atualizações de documentação
- `CLAUDE.md` §5 (pastas novas) e §6 (exemplo de `-testFilter`); §9 pegadinhas encontradas.
- `docs/ROADMAP.md` §7: status do M04.

## Prompt de disparo
```
Implemente o módulo M04 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M04-controle-jogador.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M04-controle-jogador.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.
- 2026-11-07 Implementado T1–T8 (commits `feat(M04)` em `modulo/M04-controle-jogador`). Verificação final: EditMode 403/403, PlayMode 19/19 (18 Player + smoke), 0 `error CS` nos logs. `dotnet test` não rodado (Core intocado).
- Decisões/desvios: (1) `OcclusionDither` é shader HLSL URP (`.shader`), não `.shadergraph` (JSON manual frágil). (2) Não existia `EntityEvents` no M01: `OnDodge`/`OnPerfectDodge` em `Gameplay/Events/DodgeEvents.cs`, via `EventBus<T>`. (3) Ease-out da esquiva = quadrática (`easeOutPower = 2`, no `DodgeConfig`). (4) Perfect Dodge = impacto nos 6 frames a partir do início das i-frames (`PerfectDodgeWindowFrames`, só dado) — **confirmada pelo usuário em 2026-11-07**. (5) Desambiguação LB+RB: ombro espera `ChordWindowFrames` = 3 frames (valor não vem do GDD). (6) `PlayerAim.ChestHeight` = 1,2 m (placeholder; GDD não define). (7) Namespace da câmera é `Eclipse.Gameplay.Cameras` (evita conflito com `UnityEngine.Camera`). (8) `EclipseInput.cs` é wrapper escrito à mão; refazer se o `.inputactions` mudar. (9) Testes PlayMode usam eventos sintéticos no `PlayerInputReader`, não `InputTestFixture`.
- Pendências: `MovementLock` não cobre `MoveDuringCast` 50% (06 §3.5) — M05; look-ahead com stick fica ~nulo (aim a 1 m); parâmetros de feel da câmera (suavização, shake offset, fade) são placeholders; `PlayerInputReader.Bind` com dispositivos reais sem teste automático; cena fora dos Build Settings; Gameplay de checagem manual (andar/mirar/esquivar com teclado e gamepad na `CombatSandbox`) **não feita**. Regenerar a cena: apagar `CombatSandbox.unity` e rodar `Eclipse.Editor.Setup.SandboxBuilder.Run`.

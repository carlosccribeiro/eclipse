# M06 — Base de UI e HUD
Fase 1 · Onda 6 · Status: ⏳ não iniciado
Depende de: M05, M08 · Desbloqueia: M13 (e fecha o Marco A junto com M07, M08, M09) · Paralelo com: M09 (Audio — pastas disjuntas; M09 não edita cenas)
Branch: modulo/M06-ui-hud
Docs de referência: GDD-v0.3 §11; 06-combate §3.11 (números de dano, câmera, acessibilidade visual), §3.12 (Flask), §3.14 (morte e Death Recap), §3.15; 07-resonance (ícones de status: máx. 4 sobre um inimigo, forma por afinidade); 08-herois §4.1 (Fury); 11-arquitetura §2 decisão 14, §3.7, §3.14, §3.15 item 7; 00 §5 R05

## Objetivo
Criar a fundação de UI (UI Toolkit, tema, pilha de telas, troca de Action Map, localização) e o HUD de combate, com renderer próprio instanciado para números de dano e barras world-space, console de debug e tela de morte — e montar a cena integrada do **Marco A (Combat Sandbox)** com o checklist de game feel para o usuário.

## Critérios de saída (verificáveis)
1. UI Toolkit com `PanelSettings`, tema por tokens USS (cores de afinidade de 06 §3.11, tipografia, escala), `ScreenStack` (abrir/fechar/voltar) e troca automática `Gameplay` ↔ `UI` no Input System.
2. HUD: vida + escudo, recurso por tipo de `ResourceDefinition` (barra para Pool/Fury; widgets de Charges e Constructs prontos para M21/M23), barra de habilidades Q/E/R/Shift/F com cooldown e indisponibilidade por custo, cargas de esquiva, cargas do Aether Flask, ícones de status do jogador.
3. World-space instanciado: números de dano (cor por tipo, crit 1,5× com "punch", ticks menores e semitransparentes, tamanho escala com % da vida do alvo; modos desligado/individual/agregado por alvo em 0,5 s — 06 §3.11), barra de vida/Stagger de inimigo (Medium+ quando a barra enche), máx. 4 ícones de status por inimigo (07).
4. Toda a UI lê o `CombatEventStream` em `LateUpdate` (11 §3.7) e nunca altera combate; 0 B GC/frame no HUD em combate (11 §3.14).
5. Console de debug no Action Map `Debug` (kill all, god mode, set level, spawn inimigo, seed, time scale, mostrar hitboxes), com comandos registrados por atributo para que módulos futuros adicionem comandos sem editar arquivo central (11 §3.15 item 7).
6. Tela de morte com Death Recap (últimos 5 hits: fonte, tipo, valor, mitigação, status ativos — 06 §3.14).
7. Strings da UI em tabelas da Unity Localization (pt-BR e en-US).
8. Cena `MarcoA_CombatSandbox.unity` com Kael completo (M08), os 3 inimigos + Husk (M07) e HUD; checklist de game feel em `docs/playtest/MarcoA-checklist.md`.
9. EditMode + PlayMode verdes.

## Fora de escopo
Inventário, comparação de itens e tela de builds (M13); mapa da run e tela de recompensas (M16); barra de chefe (M17); telas do Sanctuary (M19); opções e remapeamento (M31); arte final de UI (trilha A).

## Premissas e decisões
- UI Toolkit para telas e HUD; renderer próprio (GPU instancing / `Graphics.RenderMeshInstanced`) para números e barras world-space (11 decisão 14).
- UI só observa: consome eventos do `CombatEventStream` e estado via interfaces somente-leitura dos contratos do M05; nunca chama sistemas de combate (11 §3.3: "UI nunca o contrário").
- Console de debug só em builds de desenvolvimento (`DEVELOPMENT_BUILD || UNITY_EDITOR`).
- O M09 inicializa o áudio sem editar cenas (bootstrap por `RuntimeInitializeOnLoadMethod`); a cena do Marco A é deste módulo.
- Arte placeholder (D4): fonte livre (licença OFL), ícones geométricos simples por afinidade.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Fundação UI Toolkit: PanelSettings, tema, `ScreenStack`, troca de Action Map, localização | execução | executor | — | `Assets/_Project/Code/UI/Core/*`, `Assets/_Project/UI/Theme/*`, `Assets/_Project/UI/PanelSettings/*`, `Assets/_Project/Settings/Localization/*` | ⏳ |
| T2 | HUD de combate | execução | executor | T1 | `Assets/_Project/Code/UI/Hud/*`, `Assets/_Project/UI/Hud/*` | ⏳ |
| T3 | Renderer world-space: números de dano, barras e ícones de inimigo | execução | executor | T1 | `Assets/_Project/Code/UI/WorldSpace/*`, `Assets/_Project/Art/Shaders/DamageNumber.shader`, `Assets/_Project/UI/WorldSpace/*` | ⏳ |
| T4 | Console de debug | execução | executor | T1 | `Assets/_Project/Code/UI/DebugConsole/*`, `Assets/_Project/UI/DebugConsole/*` | ⏳ |
| T5 | Tela de morte + Death Recap | execução | executor | T1 | `Assets/_Project/Code/UI/Screens/Death/*`, `Assets/_Project/UI/Screens/Death/*` | ⏳ |
| T6 | Testes PlayMode de UI | execução | executor | T2–T5 | `Assets/_Project/Code/Tests/PlayMode/UI/*` | ⏳ |
| T7 | Cena do Marco A + checklist de game feel | integração | principal | T2–T6 | `Assets/_Project/Scenes/Milestones/MarcoA_CombatSandbox.unity`, `docs/playtest/MarcoA-checklist.md` | ⏳ |
| T8 | Revisão final e integração | integração | principal | T7 | `docs/roadmap/M06-ui-hud.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2 ∥ T3 ∥ T4 ∥ T5 (pastas disjuntas; até 2 executores simultâneos)
3. T6
4. T7
5. T8

## Briefs

**T1 — Fundação UI Toolkit**
- Objetivo: base comum a todas as telas do jogo.
- Contexto: UI Toolkit runtime (11 decisão 14); tema por variáveis USS (cores de afinidade de 06 §3.11: Physical branco, Ember laranja, Frost ciano, Storm amarelo-elétrico, Void violeta, Echo verde-água, Ruin verde-doentio; paleta alternativa para daltonismo como segundo tema); `ScreenStack` com push/pop/replace e foco de navegação por gamepad; ao abrir tela modal troca o Action Map para `UI` e volta para `Gameplay` ao fechar (Action Maps do M04); Unity Localization com tabela `UI` em pt-BR e en-US e helper para `LocalizedString`.
- Arquivos: `UI/Core/{UiRoot,ScreenStack,UiScreen,InputMapSwitcher,UiLocalization}.cs`, `Assets/_Project/UI/Theme/{Tokens.uss,Theme_Default.tss,Theme_Colorblind.tss}`, `Assets/_Project/UI/PanelSettings/Main.asset`, `Assets/_Project/Settings/Localization/*`.
- Restrições: assembly `Eclipse.UI` (depende de Core, Data, Gameplay, Meta — 11 §3.3); nenhum `UnityEvent` em hot path.
- Aceite: teste PlayMode abre/fecha uma tela fictícia e o Action Map troca e volta; string localizada resolve nos dois idiomas.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.UI.Core"` → verde.
- Entrega: ≤ 150 palavras.

**T2 — HUD de combate**
- Objetivo: HUD do critério 2.
- Contexto: vida com escudo sobreposto e flash de dano; recurso por tipo (`Pool`: barra 0–máx com marca de 100 quando teto é 150; `Charges`: N orbes; `MassToConstructs`: barra de Void Mass + contador — os dois últimos com dado falso até M21/M23); barra Q/E/R/Shift/F com ícone placeholder, sweep radial de cooldown, estado "sem recurso" e destaque quando pronto; cargas de esquiva (2, recarga sequencial visível — 06 §3.7); cargas do Flask (3 — 06 §3.12); ícones de buffs/debuffs do jogador. Atualização por eventos (`OnResourceGained/Spent`, `OnSkillCast`, `OnDodge`, `OnFlaskUsed`, `OnHitTaken`, `OnShieldBroken`, `OnLowHealth`) lidos do `CombatEventStream` em `LateUpdate`.
- Arquivos: `UI/Hud/{CombatHud,HealthWidget,ResourceWidget,SkillBarWidget,DodgeChargesWidget,FlaskWidget,PlayerStatusWidget}.cs`, UXML/USS em `Assets/_Project/UI/Hud/`.
- Restrições: 0 B GC/frame em combate (sem concatenação de strings por frame; textos numéricos com cache).
- Aceite: teste PlayMode: gastar Fury atualiza a barra no mesmo frame lógico + 1; cooldown do Leap (9 s) mostra progresso correto.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.UI.Hud"` (T6) → verde.
- Entrega: ≤ 150 palavras.

**T3 — Renderer world-space**
- Objetivo: números de dano, barras e ícones de inimigo em massa.
- Contexto: 06 §3.11 — cor por tipo; crit 1,5× + "punch"; ticks de DoT menores e semitransparentes; tamanho escala com % da vida máxima do alvo; modos desligado/individual/agregado por alvo (soma em 0,5 s); barra de vida + Stagger em inimigos Medium+ visível quando a barra de Stagger começa a encher (06 §3.8.2); máx. 4 ícones de status sobre um inimigo (07); nameplate de Elite (dourado) preparado para M16. Renderização por instancing (atlas de dígitos), pool do M05.
- Arquivos: `UI/WorldSpace/{DamageNumberRenderer,DamageNumberAggregator,EnemyBarRenderer,StatusIconRenderer}.cs`, `Art/Shaders/DamageNumber.shader`, atlas em `Assets/_Project/UI/WorldSpace/`.
- Restrições: 0 B GC/frame com 200 inimigos e 300 números simultâneos; render abaixo dos telegraphs inimigos (06 §3.10 — telegraphs acima de todo VFX do jogador).
- Aceite: teste PlayMode com 300 números/s por 10 s → GC 0 B/frame; modo agregado soma hits do mesmo alvo dentro de 0,5 s.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.UI.WorldSpace"` (T6) → verde.
- Entrega: ≤ 150 palavras.

**T4 — Console de debug**
- Objetivo: console in-game de 11 §3.15 item 7.
- Contexto: abre pelo Action Map `Debug` (M04); comandos iniciais: `kill_all`, `god`, `set_level <n>`, `spawn <enemyId> [n]`, `seed [n]`, `timescale <x>`, `hitboxes on|off`, `give_fury <n>`. Registro por atributo `[DebugCommand("nome")]` descoberto por reflexão no início, para que M12+ adicionem `give_item` etc. sem editar o console.
- Arquivos: `UI/DebugConsole/{DebugConsole,DebugCommandAttribute,DebugCommandRegistry,BuiltInCommands}.cs`, UXML/USS em `Assets/_Project/UI/DebugConsole/`.
- Restrições: compilado só em Editor/Development Build; comandos agem por APIs públicas dos sistemas, nunca por campos privados.
- Aceite: teste EditMode do parser/registry; teste PlayMode `kill_all` limpa a sala de teste.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.UI.DebugConsole"` + PlayMode (T6) → verdes.
- Entrega: ≤ 120 palavras.

**T5 — Tela de morte + Death Recap**
- Objetivo: tela de 06 §3.14.
- Contexto: ao `OnDeath` do jogador, após os 0,5 s de câmera lenta de apresentação, abre a tela com os últimos 5 hits recebidos (fonte, tipo de dano, valor, mitigação aplicada) e status ativos no momento, lidos do `DeathRecap` do M05; botões "Reiniciar sala de teste" e "Sair" (o fluxo real de fim de run é M16).
- Arquivos: `UI/Screens/Death/{DeathScreen,DeathRecapView}.cs`, UXML/USS em `Assets/_Project/UI/Screens/Death/`.
- Aceite: teste PlayMode: matar o jogador com 5 hits conhecidos mostra os 5 na ordem correta.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.UI.Death"` (T6) → verde.
- Entrega: ≤ 100 palavras.

**T6 — Testes PlayMode de UI**
- Objetivo: consolidar e completar os testes de T1–T5.
- Contexto: além dos casos de cada brief: HUD não alocando por 60 s de combate (`ProfilerRecorder` "GC Allocated In Frame"); UI sem referência a tipos de escrita de combate (teste de arquitetura: assembly `Eclipse.UI` não chama métodos mutáveis de `Eclipse.Gameplay` — verificação por análise de referências ou revisão registrada).
- Arquivos: `Tests/PlayMode/UI/*`.
- Aceite: todos verdes.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.UI"` → 0 falhas.
- Entrega: ≤ 120 palavras.

**T7 — Cena do Marco A + checklist de game feel (principal)**
- Objetivo: integrar M05/M07/M08/M06 numa cena e entregar ao usuário um roteiro de playtest objetivo.
- Contexto: cena `MarcoA_CombatSandbox` com Kael completo (5 armas trocáveis por debug), Cinder Husk, Ashborn Marauder, Slag Hurler, Kiln Brute em ondas configuráveis, HUD, números de dano e console. O áudio entra sozinho se o M09 estiver integrado. Checklist (marcar sim/não + nota 1–5): combo do Greatsword legível; hit-stop perceptível sem "travar"; esquiva responsiva (buffer 150 ms); i-frames confiáveis contra o overhead do Marauder e o slam do Brute; Parry do Ashen Guard executável em 12 f; telegraphs lidos sem olhar o HUD; Slag Hurler força movimento; Brute exige flanquear; números de dano não escondem telegraphs; 60 fps estáveis em sala cheia; teclado e gamepad equivalentes. Critérios objetivos de 06 já cobertos por testes são listados como "verificados por teste".
- Arquivos: `Assets/_Project/Scenes/Milestones/MarcoA_CombatSandbox.unity`, `docs/playtest/MarcoA-checklist.md`.
- Aceite: cena abre e roda sem erros; checklist pronto para o usuário.
- Verificação: PlayMode "cena do Marco A carrega e uma onda completa termina sem exceção" → verde.

**T8 — Revisão final e integração (principal)**
- Arquivos: este arquivo, `docs/ROADMAP.md` §7 (status do M06 e, se M07/M08/M09 ✅, do Marco A), `CLAUDE.md` (pastas `UI/*`, regra "UI só observa", como registrar comando de debug).
- Aceite: critérios 1–9 ✅ com saídas no Log; resultado do checklist do usuário registrado (o Marco A só fecha com o checklist preenchido).

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.UI" -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
→ 0 falhas; checklist do Marco A preenchido pelo usuário.

## Riscos e rollback
- UI Toolkit alocando por frame em labels dinâmicos → cache de strings numéricas e atualização só quando o valor muda; números de dano já ficam fora do UI Toolkit.
- Instancing de números sem suporte no hardware mínimo → fallback para mesh combinada por frame, mesma API.
- Checklist do Marco A reprovado → abrir ajustes de dados (frame data, hit-stop, telegraph) no módulo dono (M05/M07/M08) antes de seguir para a onda 7.

## Atualizações de documentação
- `CLAUDE.md`: pastas `UI/*` e `Assets/_Project/UI/*`; regra "UI só observa"; como adicionar comando de debug; onde ficam as tabelas de localização.
- `docs/ROADMAP.md` §7: status do M06 e do Marco A.

## Prompt de disparo
```
Implemente o módulo M06 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M06-ui-hud.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M06-ui-hud.
Este módulo roda em paralelo com o M09: não edite Assets/_Project/Code/Audio nem o projeto FMOD.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

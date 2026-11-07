# M09 — Áudio base (FMOD)
Fase 1 · Onda 6 · Status: ⏳ não iniciado
Depende de: M05 · Desbloqueia: Marco A (junto com M06, M07, M08); base para áudio de M16/M17/M19+ · Paralelo com: M06 (UI — este módulo não edita cenas nem `UI/`)
Branch: modulo/M09-audio-base
Docs de referência: GDD-v0.3 §13 (direção de áudio); 06-combate §3.10 (cue de wind-up), §3.11 (diretrizes de áudio, hit-stop, ducking), §3.7 (SFX de Perfect Dodge); 11-arquitetura §2 decisão 13, §3.1 (FMOD, bancos por região e herói), §3.3 (`Eclipse.Audio` só ouve eventos), §3.7, §3.14 (pooling de instâncias); 00 §5 R47

## Objetivo
Integrar FMOD Studio ao projeto com uma camada de áudio que só ouve eventos de combate, cobrindo impactos por classe de arma × material, camada de crit, cues de wind-up de inimigos, esquiva/Perfect Dodge/Parry, música dinâmica por intensidade e ducking — com placeholders gerados por script, testável sem placa de som via backend nulo.

## Ações do usuário (pré-requisitos — o Claude não pode fazê-las)
1. Criar conta em fmod.com e aceitar a EULA do FMOD.
2. Baixar e instalar **FMOD Studio** na versão recomendada pelo plugin compatível com Unity 6.3 LTS (o T1 confirma a versão exata).
3. Baixar o pacote **FMOD for Unity** (`.unitypackage`) da mesma versão e informar o caminho do arquivo (ou importá-lo pelo menu Assets → Import Package).
4. Antes do lançamento comercial: registrar o projeto para a licença indie do FMOD (gratuita abaixo do limite de faturamento vigente) — registrado também no M32.
5. (Opcional) Fornecer packs de SFX CC0 se quiser placeholders melhores que os sintetizados.

## Critérios de saída (verificáveis)
1. Plugin FMOD importado, `FMODStudioSettings` apontando para o projeto Studio em `FMOD/EclipseAudio/`, bancos compilando e carregando em Editor e em build de desenvolvimento.
2. Projeto Studio com estrutura de buses (`Master/Music`, `Master/SFX/{Player,Enemies,Impacts,UI,Ambience}`, `Master/VO`), snapshots (`Ducking_Ultimate`, `Ducking_BossBreak`) e eventos placeholder para: impacto por classe de peso (Pesada/Média/Leve/Ranged/Caster — 06 §3.6.1) × material do alvo (carne, armadura, pedra, etéreo — 06 §3.11), camada de crit, 6 afinidades (GDD §13), esquiva, Perfect Dodge (0,15 s — 06 §3.7), Parry, Block, wind-up de ataque pesado inimigo (distinto do leve — 06 §3.10), morte, Flask, UI básica, música de combate com parâmetro `Intensity`.
3. `Eclipse.Audio` com `IAudioBackend` (FMOD e Null), `AudioService` inicializado sem editar cenas, `AudioEventMap` (SO) ligando hooks de 06 §3.11 a eventos FMOD, consumo do `CombatEventStream` em `LateUpdate` (11 §3.7) e pool de instâncias one-shot (11 §3.14).
4. Transiente de impacto disparado no primeiro frame de hit-stop; ducking de música durante Ultimate e Break de chefe (06 §3.11).
5. Música dinâmica: `Intensity` (0–1) derivada de inimigos ativos/ameaça na sala, com suavização.
6. Validador: todo hook de combate usado no `AudioEventMap` aponta para evento existente ou "sem som" explícito.
7. EditMode + PlayMode verdes usando `NullAudioBackend` (sem depender de dispositivo de áudio no CI); 0 B GC/frame no áudio em combate.

## Fora de escopo
Trilhas e SFX finais (trilha A / produção de áudio); ambientes sonoros por região (M16, M24–M27); áudio de chefes (M17); volumes nas opções (M31 — aqui só a API); VO.

## Premissas e decisões
- **Backend abstrato:** `IAudioBackend` com `FmodAudioBackend` e `NullAudioBackend`; testes e batchmode usam o nulo (FMOD em batchmode sem dispositivo é instável).
- **Bootstrap sem cena:** `AudioBootstrap` via `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` cria o `AudioService` persistente; nenhuma cena é editada (a cena do Marco A é do M06).
- **Placeholders sintetizados:** script em `tools/audio/` gera WAVs curtos (ruído filtrado, transientes, tons) por categoria — sem baixar áudio da internet. Packs CC0 só se o usuário fornecer.
- **Listener** na posição do jogador com orientação da câmera (câmera a 22 m deixaria a atenuação 3D desbalanceada — 06 §3.11).
- Criação de eventos no Studio por **script JS do FMOD Studio** (`FMOD/Scripts/eclipse_setup.js`), executado via linha de comando do Studio se disponível; senão, o usuário roda pelo menu Scripts do Studio (instruções no Log).
- Bancos: `Master`, `Music`, `SFX_Common`, `SFX_Kael` agora; por região e por herói conforme 11 §3.1 nos módulos de conteúdo.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Confirmar versão FMOD ↔ Unity 6.3 LTS e checklist de instalação | investigação | investigador | — | — (resultado no Log) | ⏳ |
| T2 | Ação do usuário: instalar FMOD Studio e importar FMOD for Unity | manual | usuário | T1 | `Assets/Plugins/FMOD/*` (gerado pelo import) | ⏳ |
| T3 | Projeto Studio + script de setup + placeholders sintetizados | execução | executor | T2 | `FMOD/EclipseAudio/*`, `FMOD/Scripts/eclipse_setup.js`, `tools/audio/*` | ⏳ |
| T4 | Camada `Eclipse.Audio`: backend, serviço, bootstrap, pool | execução | executor | T2 | `Assets/_Project/Code/Audio/Core/*`, `Assets/_Project/Code/Audio/Eclipse.Audio.asmdef`, `Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset` | ⏳ |
| T5 | `AudioEventMap` + ouvintes de combate + sincronia com hit-stop | execução | executor | T3, T4 | `Assets/_Project/Code/Audio/Combat/*`, `Assets/_Project/Audio/Maps/AudioEventMap_Combat.asset` | ⏳ |
| T6 | Música dinâmica e snapshots de ducking | execução | executor | T4 | `Assets/_Project/Code/Audio/Music/*`, `Assets/_Project/Audio/Maps/MusicConfig.asset` | ⏳ |
| T7 | Regra do validador + testes EditMode/PlayMode | execução | executor | T5, T6 | `Assets/_Project/Code/Editor/Validation/Rules/AudioEventMapRule.cs`, `Assets/_Project/Code/Tests/EditMode/Audio/*`, `Assets/_Project/Code/Tests/PlayMode/Audio/*` | ⏳ |
| T8 | Revisão final e integração | integração | principal | T1–T7 | `docs/roadmap/M09-audio-base.md`, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2 (usuário)
3. T3 ∥ T4 (FMOD/ e tools/ × Code/Audio/)
4. T5 ∥ T6
5. T7
6. T8

## Briefs

**T1 — Versão FMOD ↔ Unity 6.3 LTS**
- Objetivo: dizer ao usuário exatamente o que baixar.
- Contexto: Unity 6.3 LTS (6000.3.x), URP, Windows (CLAUDE.md §3). Precisamos da versão do FMOD for Unity que declara suporte a Unity 6.3 e da versão de FMOD Studio correspondente (mesma major/minor), e se a linha de comando do Studio aceita execução de script (`-script`) para automatizar o T3.
- Arquivos: lê documentação oficial da FMOD (fmod.com/docs); escreve nada.
- Aceite: versão recomendada, links das páginas de download (sem baixar), notas de compatibilidade e se `-script` existe.
- Verificação: fontes citadas.
- Entrega: ≤ 150 palavras.

**T2 — Instalação (usuário)**
- O usuário segue as "Ações do usuário" 1–3. O executor do T4 confirma o import: pasta `Assets/Plugins/FMOD/` existe e o projeto compila.

**T3 — Projeto Studio + script + placeholders**
- Objetivo: projeto FMOD versionado com estrutura completa e sons provisórios.
- Contexto: buses e snapshots do critério 2; eventos: `Impact/{Heavy,Medium,Light,Ranged,Caster}` com parâmetro `Material` (Flesh, Armor, Stone, Ethereal), `Impact/CritLayer`, `Affinity/{Ember,Frost,Storm,Void,Echo,Ruin}`, `Player/{Dodge,PerfectDodge,Parry,Block,FlaskUse,Death}`, `Enemy/{WindupLight,WindupHeavy,Telegraph_Unblockable}`, `UI/{Click,Back,Error}`, `Music/Combat_Ashen` (parâmetro `Intensity` 0–1 com 3 camadas). Placeholders: script em `tools/audio/` (Python ou dotnet) gera WAVs mono 48 kHz curtos por categoria (transientes graves para Pesada, cliques agudos para Leve, varreduras para afinidades). Bancos `Master`, `Music`, `SFX_Common`, `SFX_Kael`.
- Arquivos: `FMOD/EclipseAudio/` (projeto Studio), `FMOD/Scripts/eclipse_setup.js`, `tools/audio/gen_placeholders.*` e WAVs gerados em `FMOD/EclipseAudio/Assets/Placeholders/`.
- Restrições: nada baixado da internet; arquivos de áudio no Git LFS (D2); bancos compilados não versionados (gerados no build).
- Aceite: Studio abre o projeto, "Build" gera os bancos sem erros; se a CLI do Studio permitir, build por linha de comando registrado no Log.
- Verificação: build dos bancos (CLI do Studio ou manual do usuário) → sem erros; lista de eventos no Log.
- Entrega: ≤ 150 palavras.

**T4 — Camada `Eclipse.Audio`**
- Objetivo: serviço de áudio desacoplado do combate.
- Contexto: `Eclipse.Audio` depende só de Core e Gameplay (11 §3.3) e não é referenciado por Gameplay; `IAudioBackend` (`PlayOneShot(eventId, position, params)`, `SetParameter`, `SetSnapshot`, `SetBusVolume`); `FmodAudioBackend` e `NullAudioBackend` (registra chamadas para testes); `AudioService` criado por `AudioBootstrap` (`RuntimeInitializeOnLoadMethod`), escolhe o backend nulo em batchmode/testes; pool de instâncias one-shot (11 §3.14); API de volume por bus para o M31; listener na posição do jogador com orientação da câmera.
- Arquivos: `Code/Audio/Core/{IAudioBackend,FmodAudioBackend,NullAudioBackend,AudioService,AudioBootstrap,AudioListenerFollower,OneShotPool}.cs`, `Code/Audio/Eclipse.Audio.asmdef` (referência a FMOD), `FMODStudioSettings.asset`.
- Restrições: não editar cenas, prefabs de gameplay nem `Gameplay/*`; 0 B GC por disparo após warm-up.
- Aceite: compila; com `NullAudioBackend`, disparo de 1.000 one-shots não aloca.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Audio.Core"` (T7) → verde.
- Entrega: ≤ 150 palavras.

**T5 — `AudioEventMap` + ouvintes de combate**
- Objetivo: tradução de eventos de combate em som.
- Contexto: `AudioEventMap` (SO) mapeia hooks de 06 §3.11 (`OnHitDealt`, `OnCritDealt`, `OnDodge`, `OnPerfectDodge`, `OnParry`, `OnBlock`, `OnDeath`, `OnFlaskUsed`, `OnStaggerBreak`, `OnSkillCast` etc.) e eventos de telegraph (wind-up leve × pesado) a eventos FMOD; impacto escolhido pela classe de peso da arma × material do alvo; camada adicional em crit; transiente no primeiro frame de hit-stop (06 §3.11); afinidade do hit adiciona camada da afinidade dominante. Consome o `CombatEventStream` em `LateUpdate` (11 §3.7); limite de vozes simultâneas por categoria para hordas (ex.: máx. 12 impactos por frame, prioridade por proximidade e crit).
- Arquivos: `Code/Audio/Combat/{CombatAudioListener,ImpactResolver,VoiceLimiter}.cs`, `Assets/_Project/Audio/Maps/AudioEventMap_Combat.asset`.
- Restrições: nunca escrever em sistemas de combate.
- Aceite: teste PlayMode com `NullAudioBackend`: um combo do Greatsword contra boneco "Armor" dispara 3 `Impact/Heavy` com `Material=Armor` sincronizados ao hit-stop; crit adiciona `CritLayer`; Perfect Dodge dispara o evento próprio.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Audio.Combat"` (T7) → verde.
- Entrega: ≤ 150 palavras.

**T6 — Música dinâmica e ducking**
- Objetivo: trilha de encontro reativa (GDD §13) e ducking de 06 §3.11.
- Contexto: `Intensity` 0–1 calculada a partir de inimigos ativos ponderados por massa/PA e dano recente, suavizada (subida 0,5 s, descida 3 s); snapshot de ducking ao `OnSkillCast` de skill com tag `Ultimate` e ao `OnStaggerBreak` de chefe, com duração do hit-stop global + 1 s.
- Arquivos: `Code/Audio/Music/{MusicDirector,IntensityEstimator,DuckingController}.cs`, `Assets/_Project/Audio/Maps/MusicConfig.asset`.
- Aceite: teste PlayMode com backend nulo: spawn de 10 inimigos eleva `Intensity` acima de 0,5 em ≤ 1 s; Ultimate ativa o snapshot.
- Verificação: PlayMode `-testFilter "Eclipse.Tests.PlayMode.Audio.Music"` (T7) → verde.
- Entrega: ≤ 120 palavras.

**T7 — Validador + testes**
- Objetivo: garantir cobertura e os critérios 3–7.
- Contexto: regra de validador (descoberta pelo `DataValidator` do M02) que falha se um hook usado não tem evento nem "sem som" explícito, ou se o evento não existe na lista exportada pelo FMOD; testes EditMode (pool, backend nulo, mapa) e PlayMode (T5, T6, GC 0 B/frame em 60 s de combate com backend nulo).
- Arquivos: `Editor/Validation/Rules/AudioEventMapRule.cs`, `Tests/EditMode/Audio/*`, `Tests/PlayMode/Audio/*`.
- Aceite: todos verdes; validador acusa erro com um mapa de teste inválido.
- Verificação: EditMode `-testFilter "Eclipse.Tests.EditMode.Audio"` + PlayMode `-testFilter "Eclipse.Tests.PlayMode.Audio"` → 0 falhas.
- Entrega: ≤ 120 palavras.

**T8 — Revisão final e integração (principal)**
- Arquivos: este arquivo, `docs/ROADMAP.md` §7, `CLAUDE.md` (pasta `FMOD/`, como rodar o script de setup e compilar bancos, regra "Audio só ouve eventos", backend nulo em testes, versão FMOD travada).
- Aceite: critérios 1–7 ✅ com saídas no Log; teste auditivo rápido do usuário na cena do Marco A (impactos, wind-ups, música subindo com a sala cheia).

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter "Eclipse.Tests.EditMode.Audio" -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter "Eclipse.Tests.PlayMode.Audio" -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
→ validador sem erros; 0 falhas; bancos FMOD compilados; escuta manual do usuário registrada.

## Riscos e rollback
- Versão do FMOD sem suporte oficial a Unity 6.3 → usar a mais recente que declare suporte a Unity 6.x e registrar; se nenhuma, manter `NullAudioBackend` e adiar o backend FMOD sem bloquear o Marco A (registrar como pendência).
- FMOD em batchmode/CI sem dispositivo → testes sempre com backend nulo; CI só compila bancos.
- Script do Studio indisponível pela CLI → usuário executa pelo menu Scripts (passo a passo no Log).
- Hordas saturando vozes → `VoiceLimiter` por categoria (já previsto).

## Atualizações de documentação
- `CLAUDE.md`: pasta `FMOD/` e `tools/audio/`, versão do FMOD travada, comando de build de bancos, regra "Audio só ouve eventos" e "testes usam NullAudioBackend".
- `docs/ROADMAP.md` §7: status do M09; pendência de licença indie FMOD no M32.

## Prompt de disparo
```
Implemente o módulo M09 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M09-audio-base.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M09-audio-base.
Antes do T3/T4 preciso instalar o FMOD (seção "Ações do usuário"): me diga a versão exata após o T1.
Este módulo roda em paralelo com o M06: não edite cenas nem Assets/_Project/Code/UI.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

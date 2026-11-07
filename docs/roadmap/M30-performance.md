# M30 — Performance e otimização
Fase 7 · Onda 17 · Status: ⏳ não iniciado
Depende de: M29 · Desbloqueia: M32 · Paralelo com: M31
Branch: modulo/M30-performance
Docs de referência: 11-arquitetura §3.9 (combate a 60 Hz), §3.10 (IA 10/4 Hz), §3.14 (specs e orçamento), §3.16 (soak test, teste de recursão); 06-combate §3.11–3.12 (hit-stop, VFX); 07-resonance (limites de procs); 09 §10.3 (limites de leitura); CLAUDE.md §4

> **Refinar ao iniciar:** os gargalos reais só aparecem com o conteúdo completo. O T1 mede antes de qualquer otimização; as tarefas T3–T7 podem ser reordenadas ou descartadas conforme o relatório do T1.

## Objetivo
Garantir o orçamento técnico de 11 §3.14 nas **specs mínimas** (i5-8400 / Ryzen 5 2600, 8 GB, GTX 1060 6 GB / RX 580, 1080p, preset Low, 60 fps) em todo o conteúdo do jogo (5 regiões, 5 chefes, pinnacle, endgame), com testes automatizados que impeçam regressão.

## Critérios de saída (verificáveis)
1. Soak test de **200 inimigos ativos** + 600 projéteis típicos (picos de 2.000) em sala de referência: frame time p95 ≤ 16,6 ms nas specs mínimas, preset Low.
2. Orçamento por frame no mínimo: Gameplay + IA ≤ 5 ms, física/queries ≤ 2 ms, render CPU ≤ 4 ms, GPU ≤ 14 ms (Profiler capture anexada ao Log).
3. **0 B de GC alloc por frame** em combate (alerta acima de 1 KB/frame) — teste PlayMode com `Recorder`/`ProfilerRecorder` falha acima do limite.
4. Draw calls ≤ 1.500; partículas vivas ≤ 40k GPU / 3k CPU no pior caso medido.
5. Memória ≤ 4 GB RAM e ≤ 3 GB VRAM no Low após 3 runs seguidas (sem crescimento > 5% entre runs).
6. Teste de recursão (07): 40 inimigos com todos os ecos/Resonances ativos → ≤ 400 eventos de dano/s e nenhum evento com Generation > 2.
7. Tempo de carregamento de sala (Addressables) ≤ 3 s em SSD; hub → run ≤ 10 s.
8. Presets `PC_Low` e `PC_High` (URP) documentados; preset High atinge 60+ fps nas specs recomendadas (i5-12400 / RTX 3060, 1440p).
9. Testes de performance rodando no CI (job dedicado) com thresholds acima; relatório CSV comparando com a baseline.

## Fora de escopo
- Plataformas além de PC Windows.
- Redução de conteúdo ou mudança de design para caber no orçamento (vira decisão do usuário).
- Opções gráficas da UI (M31 expõe; M30 define os presets).

## Premissas e decisões
- Medição primeiro: nenhuma otimização sem captura de Profiler antes/depois no Log.
- Máquina de referência das specs mínimas: o usuário fornece acesso ou o teste roda em hardware equivalente; sem ela, os números do critério 1–5 ficam marcados "medido em <máquina>" e o critério só fecha com a medição real.
- Ferramentas: Unity Profiler, Memory Profiler, Frame Debugger, Profile Analyzer, Performance Testing package (`com.unity.test-framework.performance`).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Baseline: capturas de Profiler/Memory em 6 cenários e relatório de gargalos | investigação | investigador | — | `docs/perf/baseline-AAAA-MM-DD.md` | ⏳ |
| T2 | Harness de performance: cenários automatizados + Performance Testing + job de CI | execução | executor | T1 | `Code/Tests/PlayMode/Performance/*`, `Content/Scenes/Perf/*`, `.github/workflows/perf.yml` | ⏳ |
| T3 | CPU de gameplay/IA: Jobs/Burst em movimento de inimigos, avoidance, ticks de status; LOD de update | execução | executor | T2 | `Code/Gameplay/**`, `Code/AI/**` (arquivos apontados pelo T1) | ⏳ |
| T4 | GC zero: pooling faltante, remoção de alocações em hot paths, strings/LINQ/closures | execução | executor | T2 | arquivos apontados pelo T1 | ⏳ |
| T5 | Render: SRP Batcher, GPU instancing, LODs, oclusão, shaders estilizados com variantes reduzidas, VFX Graph budgets | execução | executor | T2 | `Settings/URP/*`, `Art/Shaders/**`, `Content/**/VFX/*` | ⏳ |
| T6 | Física e projéteis: `SpherecastCommand` em lote, layers/matriz de colisão, NonAlloc | execução | executor | T2 | `Code/Gameplay/Combat/Projectiles/*`, `ProjectSettings/DynamicsManager.asset` | ⏳ |
| T7 | Memória e carregamento: Addressables (grupos, unload), texturas/áudio (compressão), streaming de salas | execução | executor | T2 | `Assets/AddressableAssetsData/**`, import presets | ⏳ |
| T8 | Presets `PC_Low` / `PC_High` e auto-detecção inicial | execução | executor | T5 | `Settings/URP/PC_Low*`, `PC_High*`, `Code/Platform/QualityBootstrap.cs` | ⏳ |
| T9 | Medição final nas specs mínimas/recomendadas e relatório de aceite | revisão | principal | T3–T8 | `docs/perf/final-AAAA-MM-DD.md`, este arquivo, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T5 (CPU gameplay × render — arquivos disjuntos)
4. T4 ∥ T6
5. T7 ∥ T8
6. T9

## Briefs

**T1 — Baseline**
- Objetivo: saber onde estão os gargalos antes de otimizar.
- Contexto: orçamento de 11 §3.14; cenários: (a) sala de 200 inimigos (soak), (b) sala com 2.000 projéteis, (c) The Sundering Heart fase 3, (d) Ultimate de cada herói em pack de 40 inimigos com Resonance máxima, (e) hub Sanctuary, (f) 3 runs seguidas (memória).
- Arquivos: lê o projeto; escreve `docs/perf/baseline-AAAA-MM-DD.md` (crie a pasta).
- Restrições: não alterar código; capturas em build Development (não no Editor) com preset Low.
- Aceite: por cenário, frame time p50/p95/p99, ms por categoria (Gameplay/IA, física, render CPU, GPU), GC/frame, draw calls, partículas, memória; top 10 hotspots com arquivo/método.
- Verificação: arquivo de baseline com 6 cenários preenchidos e as capturas `.data` referenciadas em `Logs/perf/`.
- Entrega: ≤ 300 palavras + ranking de gargalos.

**T2 — Harness de performance**
- Objetivo: os 6 cenários do T1 rodam automaticamente e falham acima dos thresholds.
- Contexto: Performance Testing package; thresholds dos critérios 1–7; CI GameCI (D2) já existe.
- Arquivos: escreve `Code/Tests/PlayMode/Performance/*`, cenas em `Content/Scenes/Perf/`, `.github/workflows/perf.yml`.
- Restrições: cenários determinísticos (seed fixa no stream `Combat`/`Encounter`); job de perf separado do job de testes rápidos.
- Aceite: `-runTests -testPlatform PlayMode -testCategory Performance` gera `Logs/perf/*.json` e falha quando o threshold é violado (provar com um caso forçado).
- Verificação: PlayMode com categoria Performance (CLAUDE.md §6 + `-testCategory Performance`).
- Entrega: ≤ 200 palavras.

**T3 — CPU de gameplay e IA**
- Objetivo: Gameplay + IA ≤ 5 ms com 200 inimigos.
- Contexto: 11 decisão 3 (Jobs + Burst em `Eclipse.Gameplay`, não no Core — R52), IA 10 Hz perto / 4 Hz longe, `Animator.cullingMode` e LOD de update.
- Arquivos: escreve só os arquivos de `Code/Gameplay/**` e `Code/AI/**` listados como hotspots no T1.
- Restrições: comportamento idêntico (testes EditMode/PlayMode existentes continuam verdes); `Eclipse.Core` permanece C# puro.
- Aceite: cenário (a) com Gameplay + IA ≤ 5 ms p95.
- Verificação: PlayMode completo + categoria Performance; captura antes/depois no Log.
- Entrega: ≤ 200 palavras + ms antes/depois.

**T4 — GC zero**
- Objetivo: 0 B/frame em combate.
- Contexto: pooling obrigatório (CLAUDE.md §4): inimigos, projéteis, VFX, números de dano, drops, áudio one-shot.
- Arquivos: hotspots de alocação do T1.
- Restrições: sem mudança de comportamento.
- Aceite: critério 3 nos 6 cenários.
- Verificação: teste PlayMode de GC (`ProfilerRecorder` "GC Allocated In Frame") verde.
- Entrega: ≤ 150 palavras.

**T5 — Render**
- Objetivo: render CPU ≤ 4 ms, GPU ≤ 14 ms no Low, draw calls ≤ 1.500, partículas no orçamento.
- Contexto: URP Forward+, SRP Batcher, instancing; legibilidade de 06 (telegraphs acima de VFX, opacidade 80%) não pode piorar.
- Arquivos: `Settings/URP/*`, `Art/Shaders/**`, assets de VFX em `Content/**/VFX/`.
- Restrições: mudanças visuais visíveis precisam de captura antes/depois para aprovação do usuário/artista (A01).
- Aceite: critérios 2 (render) e 4.
- Verificação: categoria Performance + Frame Debugger com contagem de draw calls no Log.
- Entrega: ≤ 200 palavras + capturas.

**T6 — Física e projéteis**
- Objetivo: física/queries ≤ 2 ms com 2.000 projéteis.
- Contexto: 11 decisão 10 (queries NonAlloc, projéteis por manager com `SpherecastCommand`, sem Rigidbody).
- Arquivos: `Code/Gameplay/Combat/Projectiles/*`, matriz de colisão.
- Restrições: hit detection idêntica (testes de `AttackChain` verdes).
- Aceite: cenário (b) dentro do orçamento.
- Verificação: PlayMode completo + Performance.
- Entrega: ≤ 150 palavras.

**T7 — Memória e carregamento**
- Objetivo: critérios 5 e 7.
- Arquivos: grupos Addressables, import presets de textura/áudio.
- Restrições: sem perda visual perceptível sem aprovação (A01).
- Aceite: 3 runs sem crescimento > 5%; sala ≤ 3 s; hub → run ≤ 10 s.
- Verificação: cenário (f) do harness + medição de load no Log.
- Entrega: ≤ 150 palavras.

**T8 — Presets de qualidade**
- Objetivo: `PC_Low` e `PC_High` com auto-detecção na primeira execução (GPU/VRAM).
- Arquivos: `Settings/URP/PC_Low*`, `PC_High*`, `Code/Platform/QualityBootstrap.cs`.
- Aceite: critério 8; M31 expõe a troca na UI.
- Verificação: EditMode do bootstrap (mapa GPU → preset) + medição High nas specs recomendadas.
- Entrega: ≤ 150 palavras.

**T9 — Aceite**
- Objetivo: medir nas specs mínimas e recomendadas, comparar com a baseline, fechar o módulo.
- Verificação: todos os comandos abaixo verdes e relatório final com os 9 critérios.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testCategory Performance -testResults Logs/perf.xml -logFile Logs/perf.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
dotnet test tools/CoreTests
```
Medição manual em build Development nas specs mínimas, registrada em `docs/perf/final-*.md`.

## Riscos e rollback
- **Sem hardware de specs mínimas** → critério fica aberto; pedir ao usuário máquina de teste ou instância equivalente antes do T9.
- **Otimização muda comportamento** → toda tarefa roda a suíte completa; branch por tarefa e revert se a suíte quebrar.
- **Orçamento inatingível sem cortar conteúdo** → reportar ao usuário com opções (reduzir teto de unidades vivas por sala de 09, densidade de VFX) — decisão de design, não do executor.

## Atualizações de documentação
- `CLAUDE.md` §6: comando de testes de performance; §9: pegadinhas de profiling encontradas.
- `docs/ROADMAP.md` §7: status M30.
- 11 §3.14: atualizar só se o usuário aprovar mudança de orçamento.

## Prompt de disparo
```
Implemente o módulo M30 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M30-performance.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M30-performance.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

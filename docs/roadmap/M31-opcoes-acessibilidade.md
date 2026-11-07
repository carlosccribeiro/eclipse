# M31 — Opções, acessibilidade e localização
Fase 7 · Onda 17 · Status: ⏳ não iniciado
Depende de: M29 · Desbloqueia: M32 · Paralelo com: M30
Branch: modulo/M31-opcoes-acessibilidade
Docs de referência: 06-combate §3.4 (input), §3.12 (feedback, screen shake, cores), §3.15 (acessibilidade de combate); 07-resonance §10 (cor/forma/ícone por afinidade, paleta de daltonismo); 11-arquitetura §3.1 (Input System, Unity Localization pt-BR/en-US), §3.14 (opacidade de VFX); GDD-v0.3 §11–12; CLAUDE.md §4

> **Refinar ao iniciar:** confirmar o estado das telas de UI (M06, M13, M19) e das tabelas de string criadas pelo importador (M02) antes de aprovar.

## Objetivo
Entregar o menu completo de opções (vídeo, áudio, controles, gameplay, acessibilidade), rebinding total de teclado/mouse e gamepad, todas as opções de acessibilidade de 06 §3.15 e 07 §10, e o jogo inteiro localizado em **pt-BR e en-US** com Unity Localization.

## Critérios de saída (verificáveis)
1. Opções de vídeo: resolução, modo de janela, V-Sync, limite de fps, preset `PC_Low`/`PC_High` (M30) e ajustes individuais; aplicadas sem reiniciar e persistidas.
2. Opções de áudio: master, música, SFX, UI, voz/ambiente (buses FMOD) e "silenciar ao perder foco".
3. **Rebinding** de todas as ações dos action maps `Gameplay` e `UI` (teclado/mouse e gamepad), com detecção de conflito, reset por mapa e glyphs corretos do dispositivo ativo; persistido via `SaveBindingOverridesAsJson`.
4. Acessibilidade de combate de 06 §3.15: segurar para atacar com combo automático, toggle de canalizadas, sliders de screen shake (padrão 70%), opacidade de VFX do jogador (padrão 80%), intensidade de flashes, modo alto contraste de telegraphs (borda branca espessa), paleta de afinidades para daltonismo + ícones de forma (07), mira pela direção do movimento no gamepad.
5. Acessibilidade geral: escala de UI (80–150%), escala de números de dano, legendas/textos de NPC com tamanho ajustável, opção de reduzir movimento de câmera, aviso de fotossensibilidade na primeira execução.
6. Invariante de legibilidade: com qualquer combinação de opções, telegraphs inimigos continuam renderizando acima do VFX do jogador e vermelho saturado no chão continua exclusivo deles (teste PlayMode de camada/ordem).
7. **Localização:** 100% das strings visíveis em tabelas Unity Localization (zero texto hardcoded — validador falha se encontrar); pt-BR e en-US completos; troca de idioma em runtime; números/datas formatados por locale.
8. Pseudo-localização (texto +30% e acentos) não quebra nenhuma tela (capturas das telas principais sem overflow).
9. Opções persistidas em arquivo de configuração separado do save de progresso (sobrevive a deletar o save).

## Fora de escopo
- Idiomas além de pt-BR e en-US.
- Dublagem/voz.
- Textos de loja Steam (M32).
- Presets gráficos em si (M30).

## Premissas e decisões
- Fonte das strings: tabelas geradas pelo importador de planilhas (11 §3.15 — `Localization` é uma das tabelas do DataSource) + tabelas manuais de UI.
- Tradução: pt-BR é o idioma de autoria dos textos de jogo; en-US é produzido pelo Claude e revisado pelo usuário (ou por tradutor contratado — decisão do usuário antes do T6).
- Termos de jogo em inglês (afinidades, nomes de itens, habilidades) **permanecem em inglês nos dois idiomas**, conforme o GDD; só descrições e UI são traduzidas.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Auditoria: telas existentes, strings hardcoded, ações de input, opções já presentes | investigação | investigador | — | Log deste arquivo | ⏳ |
| T2 | Framework de opções: modelo, persistência em config separada, aplicação em runtime | execução | executor | T1 | `Code/Platform/Settings/*`, `Code/UI/Options/OptionsModel*`, testes | ⏳ |
| T3 | Telas de opções (vídeo, áudio, gameplay) em UI Toolkit | execução | executor | T2 | `UI/Options/*.uxml`, `*.uss`, `Code/UI/Options/*` | ⏳ |
| T4 | Rebinding completo + glyphs por dispositivo | execução | executor | T2 | `Code/UI/Options/Rebinding*`, `UI/Options/Controls*`, `Settings/Input/*`, testes | ⏳ |
| T5 | Acessibilidade de combate e geral (critérios 4–6) | execução | executor | T2 | `Code/Gameplay/Accessibility/*`, `Art/Shaders/Telegraph*` (alto contraste), paleta de daltonismo, `UI/Options/Accessibility*`, testes | ⏳ |
| T6 | Localização: migrar strings, tabelas pt-BR/en-US, validador anti-hardcode, formatação por locale | execução | executor | T1 | `Assets/_Project/Localization/**`, `Code/Editor/Validation/LocalizationRule*`, `DataSource/localization_*.csv` | ⏳ |
| T7 | Pseudo-localização e capturas de todas as telas | execução | executor | T3, T4, T5, T6 | `Code/Editor/Localization/PseudoLoc*`, `Logs/loc-screens/` | ⏳ |
| T8 | Revisão integrada, revisão de en-US com o usuário, docs | revisão | principal | T7 | este arquivo, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2 ∥ T6 (opções × localização — pastas disjuntas)
3. T3 ∥ T4
4. T5
5. T7
6. T8

## Briefs

**T1 — Auditoria**
- Objetivo: inventário do que existe e do que falta para os 9 critérios.
- Arquivos: lê `Code/UI/**`, `UI/**`, `Settings/Input/*`, `Code/**/*.cs` (busca por literais de string em UI); escreve o Log deste arquivo.
- Restrições: não editar código.
- Aceite: lista de telas, contagem de strings hardcoded por arquivo, lista de ações de input por mapa, opções já existentes.
- Verificação: Log com as 4 listas.
- Entrega: ≤ 300 palavras.

**T2 — Framework de opções**
- Objetivo: modelo de opções tipado, versionado, salvo em arquivo próprio (`settings.json`, fora do save de progresso), aplicado em runtime sem reiniciar.
- Contexto: save de progresso é do M18 (JSON + GZip + CRC); opções usam JSON simples com versão de schema.
- Arquivos: `Code/Platform/Settings/*`, `Code/UI/Options/OptionsModel*`, testes EditMode.
- Restrições: UI e Gameplay leem opções por evento/serviço; Gameplay não referencia UI (CLAUDE.md §4).
- Aceite: critério 9; round-trip de todas as opções.
- Verificação: EditMode verde.
- Entrega: ≤ 150 palavras.

**T3 — Telas de opções**
- Objetivo: vídeo, áudio e gameplay (critérios 1–2) navegáveis por mouse e gamepad.
- Arquivos: `UI/Options/*.uxml`, `*.uss`, `Code/UI/Options/*`.
- Restrições: UI Toolkit; todas as strings via Localization desde o início.
- Aceite: critérios 1–2.
- Verificação: PlayMode abrindo cada aba e alterando cada controle (valor aplicado e persistido).
- Entrega: ≤ 150 palavras.

**T4 — Rebinding**
- Objetivo: critério 3.
- Contexto: Input System, action maps `Gameplay`, `UI` (`Debug` não é rebindável); esquema padrão de 06/R05 (LMB, RMB, Space, Q/E/R/Shift, F, 1).
- Arquivos: `Code/UI/Options/Rebinding*`, `UI/Options/Controls*`, assets de glyph em `Settings/Input/`.
- Restrições: conflito exige confirmação de troca; não permitir desvincular ações essenciais (mover, esquivar, menu).
- Aceite: critério 3; bindings persistidos e reaplicados no boot.
- Verificação: PlayMode com `InputTestFixture` simulando rebind e uso.
- Entrega: ≤ 150 palavras.

**T5 — Acessibilidade**
- Objetivo: critérios 4–6.
- Contexto: 06 §3.15, §3.12 (shake = trauma², slider padrão 70%), 07 §10 (cor, forma, ícone por afinidade; máximo 4 ícones sobre inimigo), 11 §3.14 (telegraphs em camada acima de todo VFX do jogador; opacidade padrão 80%).
- Arquivos: `Code/Gameplay/Accessibility/*`, variante de shader de telegraph alto contraste, paleta alternativa em dados, `UI/Options/Accessibility*`.
- Restrições: nenhuma opção pode colocar VFX do jogador acima de telegraph inimigo nem usar vermelho saturado de chão fora de telegraph.
- Aceite: critérios 4–6.
- Verificação: PlayMode que percorre combinações extremas (opacidade 100%, flashes 100%, alto contraste on/off) e checa ordem de render e cores; EditMode do modelo.
- Entrega: ≤ 200 palavras + capturas antes/depois.

**T6 — Localização**
- Objetivo: critério 7.
- Contexto: Unity Localization; tabelas do DataSource geradas pelo importador (M02); termos de jogo em inglês nos dois idiomas.
- Arquivos: `Assets/_Project/Localization/**`, `DataSource/localization_*.csv`, regra nova do validador em `Code/Editor/Validation/LocalizationRule*`.
- Restrições: validador falha com literal de UI fora de tabela; chaves estáveis (`ui.options.video.resolution`), nunca texto como chave.
- Aceite: zero hardcoded; en-US 100% preenchido; troca em runtime.
- Verificação: `-executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate` sem erros + PlayMode trocando idioma.
- Entrega: ≤ 200 palavras + contagem de chaves por tabela.

**T7 — Pseudo-localização**
- Objetivo: critério 8.
- Arquivos: `Code/Editor/Localization/PseudoLoc*`; capturas em `Logs/loc-screens/`.
- Aceite: capturas de todas as telas principais (HUD, inventário, comparação, mapa, Sanctuary, opções, Codex) sem overflow/corte.
- Verificação: script de captura em batchmode gera as imagens; revisão visual pelo principal.
- Entrega: ≤ 100 palavras + lista de telas corrigidas.

**T8 — Revisão integrada**
- Objetivo: fechar os 9 critérios; usuário aprova o en-US (ou indica tradutor).
- Verificação: todos os comandos de §Verificação do módulo verdes.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```

## Riscos e rollback
- **Volume de texto (84 itens, 206 modificadores, eventos, Codex)** → migração por tabela, com o validador mostrando o progresso; T6 pode ser dividido por domínio.
- **Qualidade do en-US** → revisão do usuário obrigatória no T8; termos de jogo fixos reduzem ambiguidade.
- **Rebinding quebra a ação de UI** → ações essenciais não desvinculáveis + reset por mapa.

## Atualizações de documentação
- `CLAUDE.md` §4/§8: regra "toda string visível vai para tabela de Localization; chave estável" e convenção de chaves.
- `docs/ROADMAP.md` §7: status M31.

## Prompt de disparo
```
Implemente o módulo M31 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M31-opcoes-acessibilidade.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M31-opcoes-acessibilidade.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

# M32 — Steam, telemetria e release
Fase 7 · Onda 18 · Status: ⏳ não iniciado
Depende de: M30, M31 · Desbloqueia: Marco H (Release Candidate) · Paralelo com: — (onda solo)
Branch: modulo/M32-steam-release
Docs de referência: 11-arquitetura §3.1 (Steamworks.NET), §3.13 (save: formato, backups, migração), §3.17 (telemetria opt-in); 10-progressao §7.3 (A100 leaderboard "Beyond"), §8.4 (Weekly Trial leaderboard opcional), §9 (Codex/títulos — fonte de achievements); docs/ROADMAP.md §3 (Marco H); CLAUDE.md §7

> **Refinar ao iniciar:** App ID, depots, preço, data e materiais de loja são decisões do usuário; tarefas que dependem delas ficam bloqueadas até a resposta (perguntar no início).

## Objetivo
Integrar o jogo à Steam (achievements, Cloud Save, Rich Presence, leaderboards opcionais, overlay), implementar telemetria opt-in de playtest/balanceamento, montar o pipeline de build e release (Development, Release, upload SteamPipe por branch) e fechar o **Marco H — Release Candidate** com um checklist verificável.

## Critérios de saída (verificáveis)
1. Steamworks.NET inicializa com o App ID do projeto; jogo funciona offline / sem Steam (degrada com elegância, sem crash) — testado com `steam_appid.txt` presente e ausente.
2. **Achievements** definidos em dados (`DataSource/achievements.csv`) e disparados por eventos do Event Bus (nunca chamada direta de Gameplay → Platform); ≥ 1 achievement por marco de campanha, por herói, por set completo, Ascension (A10/A50/A100), pinnacle e Codex. Desbloqueio validado no Steamworks (app de teste).
3. **Cloud Save**: save de progresso (M18) e `settings.json` (M31) sincronizados via Steam Auto-Cloud ou Remote Storage; conflito de save resolvido por timestamp + backup local mantido (3 backups de 11 §3.13).
4. **Rich Presence**: herói, região, dificuldade/Ascension, Sanctuary vs run.
5. Leaderboards opcionais: Weekly Trial e "Beyond" (A100) — enviados só se o jogador aceitar; ausência de Steam não quebra o recorde local (M29).
6. **Telemetria opt-in** (11 §3.17): eventos anonimizados — morte (sala, causa, build hash), tempo por sala, escolhas de caminho, itens equipados ao fim da run, uso de habilidades. Consentimento explícito na primeira execução, revogável nas opções; nada pessoal; desligada por padrão.
7. **Pipeline de build**: comando batchmode gera build Windows x64 Release e Development; versão semântica + hash do commit embutidos; GameCI gera artefatos no CI; script SteamPipe envia para a branch `beta` (Release) e `dev` (Development).
8. **Crash/log**: logs locais rotacionados e relatório de crash anexável (Unity Cloud Diagnostics ou equivalente — decisão do usuário), sem dados pessoais.
9. **Checklist de RC (Marco H)** 100% verde: specs mínimas a 60 fps (M30), acessibilidade e pt-BR/en-US (M31), zero bugs bloqueadores/críticos abertos, save migra de todas as versões beta, fluxo completo novo jogo → A1 → pinnacle jogável, EULA/créditos/licenças de terceiros (FMOD, pacotes, fontes, assets gratuitos) presentes.

## Fora de escopo
- Materiais de marketing, trailer, página da loja (texto/arte) — trilha do usuário.
- Outras lojas/plataformas.
- Backend próprio além do necessário para receber telemetria (decisão do usuário: serviço gerenciado vs nenhum).

## Premissas e decisões
- Todo acesso à Steam fica em `Eclipse.Platform` atrás de interfaces (`IAchievementService`, `ICloudStorage`, `IPresenceService`, `ILeaderboardService`) com implementação nula para builds sem Steam e para testes.
- Achievements ouvem eventos (Event Bus), seguindo a regra de dependência "para baixo" de CLAUDE.md §4.
- Destino da telemetria: **decisão do usuário** (opções: arquivo local exportável apenas, serviço gerenciado como Unity Analytics, endpoint próprio). Até decidir, implementar com sink local (`Logs/telemetry/*.jsonl`) e interface pronta.
- Builds assinadas e uploads só com credenciais do usuário configuradas como secrets; o Claude nunca digita credenciais.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Coletar decisões do usuário (App ID, destino da telemetria, crash reporting, branches Steam) e inventariar eventos disponíveis | investigação | principal | — | Log deste arquivo | ⏳ |
| T2 | Camada Platform: interfaces, implementação nula, bootstrap Steamworks.NET | execução | executor | T1 | `Code/Platform/**`, `Plugins/Steamworks.NET/**`, testes | ⏳ |
| T3 | Achievements por dados + listener de eventos | execução | executor | T2 | `Code/Platform/Achievements/*`, `Code/Data/Platform/AchievementDefinition.cs`, `DataSource/achievements.csv`, testes | ⏳ |
| T4 | Cloud Save + resolução de conflito | execução | executor | T2 | `Code/Platform/Cloud/*`, ajuste mínimo em `Code/Save/*` (hook), testes | ⏳ |
| T5 | Rich Presence + leaderboards opcionais | execução | executor | T2 | `Code/Platform/Presence/*`, `Code/Platform/Leaderboards/*` | ⏳ |
| T6 | Telemetria opt-in: eventos, consentimento, sink | execução | executor | T1 | `Code/Platform/Telemetry/*`, `UI/Consent/*`, testes | ⏳ |
| T7 | Pipeline de build e release: comando de build, versionamento, workflow CI, script SteamPipe | execução | executor | T2 | `Code/Editor/Build/BuildPipeline*.cs`, `.github/workflows/release.yml`, `tools/steampipe/*` | ⏳ |
| T8 | Logs, crash reporting, créditos/licenças/EULA | execução | executor | T1 | `Code/Platform/Diagnostics/*`, `UI/Credits/*`, `docs/release/third-party-licenses.md` | ⏳ |
| T9 | Checklist de RC e bug bash: rodar o checklist, triar bugs, fechar bloqueadores | revisão | principal | T3–T8 | `docs/release/rc-checklist.md`, este arquivo, `docs/ROADMAP.md`, `CLAUDE.md` | ⏳ |

## Ondas internas
1. T1
2. T2 ∥ T6
3. T3 ∥ T7
4. T4 ∥ T5
5. T8
6. T9

## Briefs

**T1 — Decisões e inventário**
- Objetivo: obter do usuário App ID, destino da telemetria, ferramenta de crash, nomes das branches Steam; listar eventos do Event Bus disponíveis para achievements/telemetria.
- Arquivos: lê `Code/**` (catálogo de eventos); escreve o Log deste arquivo.
- Aceite: 4 decisões registradas (ou marcadas como pendentes com default local) + lista de eventos.
- Verificação: Log atualizado.
- Entrega: ≤ 200 palavras.

**T2 — Camada Platform**
- Objetivo: `Eclipse.Platform` com interfaces e implementações Steam e nula; bootstrap seguro.
- Contexto: 11 §3.3 (Platform depende de Core e Save); Steamworks.NET.
- Arquivos: `Code/Platform/**`, `Plugins/Steamworks.NET/**`, testes EditMode com implementação nula.
- Restrições: nenhum assembly de Gameplay referencia Platform; falha de init da Steam → modo offline sem exceção.
- Aceite: critério 1.
- Verificação: EditMode verde; build Development roda com e sem `steam_appid.txt` (log mostra o modo escolhido).
- Entrega: ≤ 150 palavras.

**T3 — Achievements**
- Objetivo: critério 2.
- Arquivos: `Code/Platform/Achievements/*`, `Code/Data/Platform/AchievementDefinition.cs`, `DataSource/achievements.csv`, testes.
- Restrições: definição por dados (StableId + evento + condição); validador checa que todo achievement tem evento existente e string localizada (M31).
- Aceite: lista coberta pelos grupos do critério 2; disparo idempotente.
- Verificação: EditMode com Event Bus simulado; importador/validador sem erros.
- Entrega: ≤ 150 palavras + contagem de achievements por grupo.

**T4 — Cloud Save**
- Objetivo: critério 3.
- Contexto: save JSON + GZip + CRC, escrita atômica, 3 backups (11 §3.13); suspend-save por sala.
- Arquivos: `Code/Platform/Cloud/*`; no `Code/Save/*` apenas um hook de "salvou/carregou".
- Restrições: nunca sobrescrever save local mais novo sem manter backup; CRC validado antes de aceitar a versão da nuvem.
- Aceite: cenários (local mais novo, nuvem mais novo, corrompido) cobertos.
- Verificação: EditMode com storage fake para os 3 cenários.
- Entrega: ≤ 150 palavras.

**T5 — Rich Presence e leaderboards**
- Objetivo: critérios 4–5.
- Arquivos: `Code/Platform/Presence/*`, `Code/Platform/Leaderboards/*`.
- Restrições: textos de presence localizados; leaderboards só com opt-in.
- Aceite: presence muda nos 4 contextos; envio de score do Weekly/Beyond só com opt-in.
- Verificação: EditMode com serviço fake; teste manual no cliente Steam (registro no Log).
- Entrega: ≤ 150 palavras.

**T6 — Telemetria**
- Objetivo: critério 6.
- Contexto: 11 §3.17; eventos listados no T1.
- Arquivos: `Code/Platform/Telemetry/*`, `UI/Consent/*`, testes.
- Restrições: desligada por padrão; sem identificadores pessoais (id de instalação aleatório, regenerável); sink local até o usuário decidir o destino.
- Aceite: consentimento na 1ª execução; revogar para o envio imediatamente; payload validado por schema.
- Verificação: EditMode (sem consentimento → zero eventos; com consentimento → eventos no sink com schema válido).
- Entrega: ≤ 150 palavras + exemplo de payload.

**T7 — Pipeline de build e release**
- Objetivo: critério 7.
- Arquivos: `Code/Editor/Build/BuildPipeline*.cs` (`-executeMethod Eclipse.Editor.Build.BuildPipeline.BuildRelease` / `BuildDevelopment`), `.github/workflows/release.yml`, `tools/steampipe/` (scripts e `app_build_*.vdf` sem credenciais).
- Restrições: credenciais só como secrets do GitHub; versão = tag git + hash.
- Aceite: build local e no CI gera `Builds/Windows/<versão>/`; upload para `dev` funciona com credenciais do usuário.
- Verificação: comando de build batchmode termina com código 0 e o executável abre até o menu principal (smoke test automatizado).
- Entrega: ≤ 200 palavras.

**T8 — Diagnóstico, créditos e licenças**
- Objetivo: critério 8 e parte de licenças do critério 9.
- Arquivos: `Code/Platform/Diagnostics/*`, `UI/Credits/*`, `docs/release/third-party-licenses.md`.
- Restrições: logs sem dados pessoais; inventário de licenças gerado a partir de `Packages/manifest.json`, `Plugins/` e assets gratuitos usados (D4).
- Aceite: créditos acessíveis no menu; arquivo de licenças completo.
- Verificação: PlayMode abrindo créditos; revisão do inventário pelo principal.
- Entrega: ≤ 150 palavras.

**T9 — Checklist de RC**
- Objetivo: critério 9 e fechamento do Marco H.
- Arquivos: `docs/release/rc-checklist.md` (itens do critério 9 + resultado + evidência).
- Verificação: checklist 100% com evidência; ROADMAP §7 com Marco H ✅.
- Entrega: ≤ 200 palavras.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Build.BuildPipeline.BuildRelease -quit -logFile Logs/build.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```

## Riscos e rollback
- **Decisões do usuário pendentes (App ID, telemetria)** → implementação nula/sink local permite avançar; T3–T5 testados com fakes.
- **Cloud Save sobrescreve progresso** → backups locais obrigatórios + testes dos 3 cenários antes de ligar a sincronização.
- **Build quebra no CI por licença Unity** → mesma configuração de secret do GameCI do M00; documentar em CLAUDE.md §9.
- **Bloqueadores no bug bash** → RC só fecha com zero bloqueadores; novos bugs viram tarefas no Log com dono.

## Atualizações de documentação
- `CLAUDE.md` §6: comandos de build Release/Development e upload; §9: pegadinhas de Steamworks/CI.
- `docs/ROADMAP.md` §3 e §7: Marco H.
- `docs/release/`: checklist de RC e licenças de terceiros.

## Prompt de disparo
```
Implemente o módulo M32 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M32-steam-release.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M32-steam-release.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

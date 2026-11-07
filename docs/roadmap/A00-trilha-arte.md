# A00 — Trilha de arte (A01–A05)
Fase paralela · Ondas 11–17 · Status: ⏳ não iniciado
Depende de: Marco C (Vertical Slice) · Desbloqueia: arte final para M30 (orçamento real) e M32 (RC) · Paralelo com: M19–M31
Branch: `arte/Axx-<slug>` (uma por submódulo)
Docs de referência: GDD-v0.3 §12 (direção artística, regras de legibilidade), §13 (áudio — referência de identidade por afinidade); 06-combate §3.10–3.12 (telegraphs, cores por tipo de dano, câmera FOV 30°, hit-stop, opacidade de VFX 80%), §3.15 (acessibilidade); 07-resonance §10 (cor/forma/ícone por afinidade); 08-herois (silhuetas e armas); 09-inimigos §5 (bestiário por região), §8 (chefes); 11-arquitetura §3.1 (URP, Shader Graph, VFX Graph), §3.14 (orçamento de draw calls, partículas, memória)

> **Quem conduz:** esta trilha é conduzida pelo **usuário e artistas** (decisão D4: greybox até o Marco C, arte final depois). O Claude dá **apoio técnico** (pipeline, shaders, import, LOD, VFX Graph, validação), nunca cria arte final de personagem/criatura nem compra assets sem decisão do usuário.
>
> **Refinar ao iniciar cada submódulo:** volume de assets, fornecedores (freelancer, estúdio, asset store) e cronograma são decisões do usuário.

## Objetivo
Substituir o greybox por arte final coerente com o Stylized Dark Fantasy / Cosmic Fantasy do GDD, sem perder a legibilidade de combate (telegraphs acima de VFX, laranja reservado a Ember, cor/forma/ícone por afinidade) e dentro do orçamento técnico de 11 §3.14.

## Submódulos
| ID | Submódulo | Depende de | Conduz | Apoio do Claude |
|---|---|---|---|---|
| A01 | Direção de arte e pipeline técnico | Marco C | Usuário/diretor de arte | Specs de import, presets, shaders estilizados base, LOD, validador de assets, cena de lookdev |
| A02 | Heróis (4 heróis × 20 armas) | A01 | Artistas de personagem/animação | Retarget, Animator/AttackChain com frame data de 06, sockets de VFX, validação |
| A03 | Inimigos e chefes (40 inimigos-base, 10 Guardians, 5 chefes + Sundering Heart) | A01 | Artistas de criatura/animação | Import, LOD, variantes de Elite (shader de afixo), telegraphs presos ao rig |
| A04 | Ambientes (5 regiões + Sanctuary) | A01 | Artistas de ambiente | Kits modulares, NavMesh por sala, iluminação baked/URP, oclusão |
| A05 | VFX final, UI final e ícones | A01 (VFX de herói depois de A02) | Artistas de VFX/UI | VFX Graph, budgets, paleta de daltonismo, UI Toolkit themes, ícones de status |

Ordem: A01 primeiro (bloqueia todos). Depois A02 ∥ A04, depois A03 ∥ A05.

## Critérios de saída (verificáveis)
1. Style guide aprovado pelo usuário (`docs/art/style-guide.md`) com paleta, regras de silhueta, materiais, paleta por afinidade compatível com 07 §10 e paleta de daltonismo.
2. Todo asset importado passa no **validador de assets** (tris/LOD, texel density, texturas, nomes, materiais permitidos, layers) — zero erros no CI.
3. 4 heróis e 20 armas com animações finais cujo timing respeita a frame data de 06 (testes de `AttackChain` existentes continuam verdes).
4. 40 inimigos-base, 10 Guardians, 5 chefes e o pinnacle com modelos/animações finais; Elites legíveis por shader de afixo.
5. 5 regiões e o Sanctuary com kits finais; todas as salas autorais de M16/M24–M27 migradas sem quebrar NavMesh nem encontros.
6. VFX finais para todas as habilidades, status e Resonances; **invariante de legibilidade** (telegraphs acima de todo VFX do jogador; vermelho saturado no chão só em telegraph; opacidade de VFX padrão 80%) verificada por teste PlayMode.
7. UI final (tema UI Toolkit, ícones de 24 status, 6 afinidades, raridades, slots) aplicada a todas as telas.
8. Orçamento de 11 §3.14 mantido após cada submódulo (testes de performance do M30 verdes).
9. Inventário de licenças de todo asset de terceiros atualizado em `docs/release/third-party-licenses.md`.

## Fora de escopo
- Áudio final (FMOD já integrado no M09; trilha sonora/SFX finais são decisão do usuário e seguem processo análogo, se desejado).
- Marketing, key art, trailer.

## Premissas e decisões
- Greybox e assets gratuitos permanecem até a substituição de cada categoria (D4); nenhuma categoria fica sem asset funcional no meio da troca.
- Toda troca de arte é **por dados/prefab**, sem alterar código de gameplay; se exigir código, vira tarefa de apoio do Claude separada.
- Câmera e escala definidas por 06 (FOV 30°, inclinação 50°, rotação fixa 45°, distância 22 m): lookdev sempre nessa câmera.

## Tarefas
Legenda de executor: **usuário/artista** (produção de arte, aprovação), `investigador`/`executor`/`principal` (apoio técnico do Claude).

### A01 — Direção de arte e pipeline técnico
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| A01-T1 | Definir referências, paleta e regras de silhueta; aprovar style guide | produção | usuário/artista | — | `docs/art/style-guide.md` (conteúdo) | ⏳ |
| A01-T2 | Redigir style guide técnico (paletas por afinidade de 07, cores de 06, invariantes de legibilidade) a partir do A01-T1 | execução | executor | A01-T1 | `docs/art/style-guide.md` (seção técnica) | ⏳ |
| A01-T3 | Specs de import e budgets por categoria (tris, LODs, texturas, ossos, materiais) | execução | executor | A01-T1 | `docs/art/asset-specs.md`, `Settings/ImportPresets/*` | ⏳ |
| A01-T4 | Shaders estilizados base (personagem, ambiente, Elite/afixo, dissolve, telegraph) em Shader Graph | execução | executor | A01-T2 | `Art/Shaders/**` | ⏳ |
| A01-T5 | Validador de assets no import e no CI | execução | executor | A01-T3 | `Code/Editor/Validation/AssetRules*`, `.github/workflows/*` (job) | ⏳ |
| A01-T6 | Cena de lookdev na câmera de jogo com teste de legibilidade | execução | executor | A01-T4 | `Content/Scenes/Lookdev/*`, `Code/Tests/PlayMode/Readability*` | ⏳ |
| A01-T7 | Aprovar pipeline com 1 asset piloto por categoria (herói, inimigo, prop, VFX) | revisão | usuário/artista | A01-T5, A01-T6 | — | ⏳ |

### A02 — Heróis
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| A02-T1 | Modelos, texturas e rigs dos 4 heróis e 20 armas | produção | usuário/artista | A01-T7 | `Art/Heroes/**` | ⏳ |
| A02-T2 | Animações (locomoção, esquiva, básico/forte por arma, 5 habilidades, hit/morte) no timing de 06 | produção | usuário/artista | A02-T1 | `Art/Heroes/**/Anim/*` | ⏳ |
| A02-T3 | Integração: retarget, Animator, eventos de janela de hit, sockets de VFX | execução | executor | A02-T2 | `Content/Heroes/<Hero>/*` (prefabs, controllers) | ⏳ |
| A02-T4 | Verificação de frame data e legibilidade por herói | revisão | principal | A02-T3 | Log deste arquivo | ⏳ |

### A03 — Inimigos e chefes
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| A03-T1 | Modelos/animações dos 40 inimigos-base e 10 Guardians (por região, 09 §5) | produção | usuário/artista | A01-T7 | `Art/Enemies/**` | ⏳ |
| A03-T2 | Modelos/animações dos 5 chefes + The Sundering Heart (fases de 09 §8, §15.5) | produção | usuário/artista | A01-T7 | `Art/Bosses/**` | ⏳ |
| A03-T3 | Integração: prefabs, LODs, shader de afixo de Elite, telegraphs presos ao rig com wind-up de 06 | execução | executor | A03-T1, A03-T2 | `Content/Enemies/**`, `Content/Bosses/**` | ⏳ |
| A03-T4 | Verificação: wind-up ≥ 0,45 s, letalidade/telegraph legíveis, performance | revisão | principal | A03-T3 | Log deste arquivo | ⏳ |

### A04 — Ambientes
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| A04-T1 | Kits modulares das 5 regiões e do Sanctuary | produção | usuário/artista | A01-T7 | `Art/Environments/**` | ⏳ |
| A04-T2 | Migrar salas autorais para os kits finais (dressing) | produção | usuário/artista | A04-T1 | `Content/Regions/**/Rooms/*` | ⏳ |
| A04-T3 | Apoio técnico: iluminação URP, oclusão, NavMesh rebake, validação de encontros | execução | executor | A04-T2 | `Content/Regions/**`, `Settings/URP/*` | ⏳ |
| A04-T4 | Verificação: NavMesh, contagem de spawn points, performance por sala | revisão | principal | A04-T3 | Log deste arquivo | ⏳ |

### A05 — VFX final, UI final e ícones
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| A05-T1 | Design de VFX por habilidade/status/Resonance e de UI (tema, ícones) | produção | usuário/artista | A01-T7 | `Art/VFX/**`, `Art/UI/**` | ⏳ |
| A05-T2 | Implementar VFX em VFX Graph/Shuriken com budgets e opacidade configurável | execução | executor | A05-T1, A02-T3 | `Content/**/VFX/*` | ⏳ |
| A05-T3 | Aplicar tema UI Toolkit e ícones a todas as telas | execução | executor | A05-T1 | `UI/**/*.uss`, `Art/UI/Icons/*` | ⏳ |
| A05-T4 | Verificação de legibilidade (teste PlayMode) e daltonismo | revisão | principal | A05-T2, A05-T3 | Log deste arquivo | ⏳ |

## Ondas internas
1. A01-T1 → A01-T2 ∥ A01-T3 → A01-T4 ∥ A01-T5 → A01-T6 → A01-T7
2. A02 ∥ A04 (produção do usuário em paralelo; tarefas de apoio do Claude em sequência)
3. A03 ∥ A05

## Briefs (tarefas de apoio do Claude)

**A01-T2 — Style guide técnico**
- Objetivo: seção técnica do style guide que torna as regras de legibilidade checáveis.
- Contexto: cores por tipo de dano (06 §3.12), telegraphs (vermelho evitável, roxo controle, branco-dourado Unblockable, ciano interrompível; laranja só Ember — R14), 07 §10, opacidade 80%, máximo de 4 ícones sobre inimigo.
- Arquivos: escreve a seção técnica de `docs/art/style-guide.md`.
- Aceite: tabela de cores com valores (hex/HDR), regras de camada de render e de saturação.
- Verificação: revisão do usuário (A01-T1) + cores conferidas contra 06/07.
- Entrega: ≤ 200 palavras.

**A01-T3 — Specs de import e budgets**
- Objetivo: budgets por categoria coerentes com 11 §3.14.
- Arquivos: `docs/art/asset-specs.md`, presets em `Settings/ImportPresets/`.
- Aceite: tabela por categoria (herói, inimigo comum, Elite, chefe, prop, kit de ambiente, VFX) com tris por LOD, texturas, ossos, materiais.
- Verificação: presets aplicados automaticamente a uma pasta de teste.
- Entrega: ≤ 200 palavras.

**A01-T4 — Shaders base**
- Objetivo: Shader Graphs de personagem, ambiente, afixo de Elite, dissolve e telegraph (com variante alto contraste de M31).
- Arquivos: `Art/Shaders/**`.
- Restrições: SRP Batcher compatível; variantes mínimas; nenhuma cor de telegraph fora do style guide.
- Aceite: shaders compilam sem erro e aparecem na cena de lookdev.
- Verificação: build Development sem erros de shader; teste de compatibilidade SRP Batcher no Frame Debugger.
- Entrega: ≤ 150 palavras.

**A01-T5 — Validador de assets**
- Objetivo: impedir asset fora da spec.
- Arquivos: `Code/Editor/Validation/AssetRules*`, job no CI.
- Aceite: asset fora da spec gera erro com motivo; asset dentro passa.
- Verificação: EditMode com assets de fixture (1 válido, 1 inválido por regra).
- Entrega: ≤ 150 palavras.

**A01-T6 — Lookdev e teste de legibilidade**
- Objetivo: cena na câmera de jogo (06) e teste PlayMode que garante telegraph acima de todo VFX do jogador e cores reservadas.
- Arquivos: `Content/Scenes/Lookdev/*`, `Code/Tests/PlayMode/Readability*`.
- Aceite: teste falha se um VFX do jogador renderizar acima de telegraph ou usar vermelho saturado de chão.
- Verificação: PlayMode verde.
- Entrega: ≤ 150 palavras.

**A02-T3 / A03-T3 / A04-T3 / A05-T2 / A05-T3 — Integrações**
- Objetivo: integrar os assets entregues pelo usuário/artista sem mudar comportamento.
- Contexto: frame data de 06, NavMesh por sala (11), budgets de A01-T3, invariantes de legibilidade.
- Arquivos: só os prefabs/controllers/VFX/USS da categoria do submódulo.
- Restrições: nenhuma mudança em código de gameplay; se necessária, abrir tarefa separada.
- Aceite: validador de assets sem erros; testes de `AttackChain`, encontros e legibilidade verdes; performance do M30 verde.
- Verificação: EditMode, PlayMode e categoria Performance (comandos abaixo).
- Entrega: ≤ 200 palavras por integração + capturas antes/depois.

## Verificação do módulo
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testCategory Performance -testResults Logs/perf.xml -logFile Logs/perf.log
```
Revisão visual do usuário na cena de lookdev e em uma run completa por região.

## Riscos e rollback
- **Arte final fura o orçamento** → validador de assets + testes de performance bloqueiam o merge; LOD/budget revistos com o artista.
- **Animação final muda o timing de combate** → frame data de 06 é a referência; animação se ajusta ao timing, não o contrário.
- **Identidade visual genérica (assets prontos)** → qualquer compra de pack exige aprovação do usuário e checagem contra o style guide.
- **Volume de produção** → submódulos são independentes; o greybox continua funcional enquanto uma categoria não fecha.

## Atualizações de documentação
- `CLAUDE.md` §5/§6/§9: pastas `Art/`, comando/regra do validador de assets e pegadinhas de import.
- `docs/ROADMAP.md` §7: status de A01–A05.
- `docs/release/third-party-licenses.md`: todo asset de terceiros.

## Prompt de disparo
```
Implemente o submódulo A0x da trilha de arte do PROJECT ECLIPSE (apoio técnico).
Leia CLAUDE.md, docs/roadmap/A00-trilha-arte.md e os docs de referência listados nele.
Execute apenas as tarefas de apoio técnico do submódulo A0x cujas dependências de produção (usuário/artista) já estão concluídas;
use a skill /planejar e peça minha aprovação antes de executar. Trabalhe na branch arte/A0x-<slug>.
Ao final, atualize o Status e o Log do submódulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

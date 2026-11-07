# M23 — Herói Orun (The Voidkeeper)
Fase 5 · Onda 14 · Status: ⏳ não iniciado
Depende de: M11, M20 (frameworks de status e progressão; transitivamente M08, M10, M13, M14) · Desbloqueia: M28 · Paralelo com: M26
Branch: modulo/M23-orun
Docs de referência: 08 §7 (Orun), §3, §8 (Aether Lattice), §9.3, §10.3, §11.1; 03 (`ORU-xx`); 04 (sets de Orun); 02 §3.5, §3.6, §3.12.4, §3.12.6; 05 §9.1, §9.3; 06 (frame data, estados); 07 (Decay (máx. 5), Detonation, Vulnerability (teto +60%), Pull, Distortion); 00 §4 e §5 (R01, R03, R07, R08, R12, R21–R28, R48, R49); CLAUDE.md

## Objetivo
Orun jogável de ponta a ponta: recurso Void Mass / Void Constructs, 5 habilidades (Void Tendrils, Guardian Fragment, Devouring Rift, Corruption Wave, The Unbound), 5 armas (Ritual Staff, Void Hammer, Great Mace, Totem, Ancient Scepter), 15 Augments e os 3 ramos da Aether Lattice, com as 3 builds (Void Commander, Corruption Master, Abyssal Guardian) viáveis pelas Rotas A, B e C, e todos os dados do herói (modificadores `ORU-xx`, 16 itens nomeados, 3 sets) no pipeline `DataSource/` → SOs. As **Void Constructs** saem do spike de M10 para produção: Void Mass gera Lesser Fragments, Sentinel com Taunt, sacrifício por habilidades, dissolução ao sair da sala devolvendo 25 Void Mass por construção, e todas as exceções de teto de 08 §11.1 (Totem +1, Legion of the Deep +2, O5 +1, sets +1; teto absoluto 8).

## Critérios de saída (verificáveis)
1. Recurso conforme 08 §7.1 e passivas de 08 §7.2, com teste EditMode por regra.
2. 5 habilidades + Basic/Heavy com números e tags de 08 §7.4; snapshot de SkillSpec por habilidade.
3. 5 armas com moveset/APS/bônus de 08 §7.6, frame data de 06 e DPS normalizado a ±5% (02).
4. 15 Augments + notáveis/Keystones de 08 §8.3 funcionando pela Lattice (M20), com exclusividade por habilidade.
5. 100% dos `ORU-xx` de 03 importados e compilando sem código por modificador.
6. Itens O1–O16 (02 §3.12.4) e 3 sets × 6 peças (04) importados, com `fonteUnique` e `homeRegion`; validador com 0 erros.
7. Simulador (M14): 9 loadouts (3 builds × Rotas A/B/C); rotas ofensivas a ±10% (00 §4) ou desvio levado ao usuário.
8. Stress: 8 construções + Tendrils temporários + Totem plantado contra 120 inimigos ativos a 60 fps no hardware mínimo (11 §3.14), 0 B de GC por frame após warm-up.
9. Desbloqueio: Orun desbloqueado pela primeira derrota do The Pale Sovereign (qualquer herói) + missão curta no Keeper (08 §9.3).
10. Status e Log deste arquivo e a linha de `docs/ROADMAP.md` atualizados.

## Fora de escopo
- Conteúdo de regiões, inimigos e chefes (trilha de regiões).
- Itens universais `U-xx` e catálogo restante do Kael (M28).
- Fontes de drop de Pinnacle (M29) e de Challenge Rooms (já cobertas por M16); aqui só se declara `fonteUnique`.
- Arte final, animação final e VFX final (A00). Balanceamento cruzado entre heróis (M28).

## Premissas e decisões
- Frameworks de M08 (habilidades), M10 (modificadores, Augments, Generation, spikes), M11 (status), M13 (equipamento/sets), M14 (simulador) e M20 (Lattice, desbloqueios) estão ✅ e mergeados na base. *Refinar ao iniciar* (T1 confirma).
- Estrutura de CSV proposta abaixo segue a convenção esperada de M02/M12; se M02 definiu outra, T2 ajusta os nomes mantendo a partição.
- Visual greybox + assets gratuitos (D4). Áudio: só hooks de evento e placeholders no banco FMOD do herói.
- O Unique **O3 Bulwark of Nothing** (chefe The Ashen Colossus, já implementado em M17) é autorado aqui, porque depende dos modificadores ORU-xx.

### Partição de arquivos (escreve) — evita colisão com M26
- `Assets/_Project/Code/Gameplay/Heroes/Orun/**`, `Assets/_Project/Content/Heroes/Orun/**`, `Assets/_Project/Code/Tests/{EditMode,PlayMode}/Heroes/Orun/**`
- `DataSource/heroes/orun/*.csv`, `DataSource/items/bases/orun.csv`, `DataSource/items/named/orun.csv`, `DataSource/sets/orun.csv`, `DataSource/modifiers/oru.csv`, `DataSource/sim/loadouts/orun/`, `DataSource/localization/{hero_,items_}orun.csv`
- Extensões genéricas de framework (somente se T2 aprovar): `Assets/_Project/Code/Gameplay/{Skills,Status}/**` — a trilha de regiões não escreve nessas pastas nesta onda.
- **Não escreve:** `DataSource/regions/**`, `DataSource/items/named/universal*.csv`, `Assets/_Project/Code/AI/{Enemies,Bosses}/**`, `Content/{Regions,Enemies,Bosses}/**`.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Levantar o estado dos frameworks para Orun | investigação | investigador | — | — (relatório no Log) | ⏳ |
| T2 | Decidir extensões de framework e fechar a partição de arquivos | decisão | principal | T1 | este arquivo (Premissas, Log) | ⏳ |
| T3 | Recurso Void Mass / Void Constructs e passivas inatas | execução | executor | T2 | Code/Gameplay/Heroes/Orun/Resource/*, testes | ⏳ |
| T4 | Dados do herói em CSV (kit, armas, Augments, Lattice) | execução | executor | T2 | `DataSource/heroes/orun/{hero,resource,skills,weapons,augments,lattice}.csv` | ⏳ |
| T5 | Comportamentos das 5 habilidades, Basic/Heavy e movesets das 5 armas | execução | executor | T3, T4 | Code/Gameplay/Heroes/Orun/Skills/*, Content/Heroes/Orun/*, testes | ⏳ |
| T5b | Void Constructs de produção (a partir do spike de M10) | execução | executor | T3 | Code/Gameplay/Heroes/Orun/Constructs/*, Code/AI/Allies/*, testes | ⏳ |
| T5c | The Unbound, Commander's Will e Living Bastion | execução | executor | T5b | Code/Gameplay/Heroes/Orun/{Unbound,Keystones}/*, testes | ⏳ |
| T6 | Augments e nós da Lattice do herói | execução | executor | T5, T5b, T5c | Code/Gameplay/Heroes/Orun/Augments/*, testes | ⏳ |
| T7 | Modificadores ORU-xx de 03 no pipeline | execução | executor | T5 | `DataSource/modifiers/oru.csv`, testes | ⏳ |
| T8 | Itens nomeados O1–O16 e os 3 sets de Orun | execução | executor | T7 | `DataSource/items/named/orun.csv`, `DataSource/sets/orun.csv` | ⏳ |
| T9 | Loadouts das 3 builds × Rotas A/B/C e rodada do simulador | execução | executor | T6, T8 | `DataSource/sim/loadouts/orun/*.json`, relatório | ⏳ |
| T10 | Revisão de game feel e de balanceamento | revisão | principal | T9 | CSV do herói (ajustes), este arquivo (Log) | ⏳ |
| T11 | Integração: HUD do recurso, desbloqueio, localização e docs | execução | executor | T10 | UI/HUD/Resources/Orun*, Meta (regra de desbloqueio), docs | ⏳ |

## Ondas internas
1. T1
2. T2
3. T3 ∥ T4 (arquivos disjuntos: código do recurso × CSV)
4. T5 ∥ T5b (habilidades × construções)
5. T5c ∥ T7
6. T6 ∥ T8
7. T9
8. T10
9. T11

## Briefs
### T1 — Levantar o estado dos frameworks para Orun

- **Objetivo:** Mapear o que os frameworks já entregam e o que falta para o kit completo de Orun: habilidades (M08), modificadores/Augments/Generation (M10), status/Resonance (M11), equipamento/sets (M13), simulador (M14), Lattice/desbloqueios (M20), e a estrutura real de CSV e do validador (M02/M12).
- **Contexto:** Orun usa Void Mass / Void Constructs; habilidades Void Tendrils, Guardian Fragment, Devouring Rift, Corruption Wave, The Unbound; estados Taunt (Sentinel), Unstoppable (The Unbound), Root (Binding Tendrils), Knockback (Corruption Wave); status Decay (máx. 5), Detonation, Vulnerability (teto +60%), Pull, Distortion. As **Void Constructs** saem do spike de M10 para produção: Void Mass gera Lesser Fragments, Sentinel com Taunt, sacrifício por habilidades, dissolução ao sair da sala devolvendo 25 Void Mass por construção, e todas as exceções de teto de 08 §11.1 (Totem +1, Legion of the Deep +2, O5 +1, sets +1; teto absoluto 8).
- **Arquivos:** lê CLAUDE.md, este arquivo, 08 §7 e §8, docs/roadmap/M08…M20 (Log e critérios), `Assets/_Project/Code/Gameplay/Skills`, `DataSource/`; escreve nada (somente relatório) (não toque em outros).
- **Restrições:** Somente leitura.
- **Aceite:** Lista de lacunas por sistema (arquivo:linha), estrutura de CSV confirmada e lista de APIs ausentes para Orun.
- **Verificação:** relatório com evidências; o principal registra no Log
- **Entrega:** no máximo 300 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T2 — Decidir extensões de framework e fechar a partição de arquivos

- **Objetivo:** Transformar as lacunas de T1 em decisões: o que vira extensão genérica de framework (tarefa própria) e o que é código específico do herói; confirmar a partição de arquivos com o par da onda.
- **Contexto:** Par paralelo: M26 (trilha de regiões) — não pode haver arquivo escrito pelos dois. Extensões genéricas em pastas compartilhadas (Gameplay/Skills, Gameplay/Status) só nesta trilha.
- **Arquivos:** lê relatório de T1, docs/roadmap/M26-*.md; escreve este arquivo (não toque em outros).
- **Restrições:** Mudança de número/regra de design → perguntar ao usuário e registrar em 00 §5; não decidir sozinho.
- **Aceite:** Seção Premissas atualizada; tabela de tarefas ajustada (novas T5x se necessário).
- **Verificação:** revisão do diff deste arquivo
- **Entrega:** no máximo 150 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T3 — Recurso Void Mass / Void Constructs e passivas inatas

- **Objetivo:** Implementar o componente de recurso polimórfico de Orun (Void Constructs (contagem; base 3, teto base 6, até 8 via 08 §11.1) + barra Void Mass (0–100)) com todas as regras da tabela de 08 §7.1, a tradução dos atributos genéricos (Maximum Resource, Resource Generation, Resource Cost Reduction) de 01 e 08 §3.2, e as passivas inatas de 08 §7.2.
- **Contexto:** Regras numéricas vêm de CSV (`resource.csv`), não do código. Exceções de teto só pelas fontes de 08 §11.1 (R01).
- **Arquivos:** lê 08 §7.1–§7.2, §3.2, §11.1; 01 (tradução de recurso); 00 §5 R01; escreve `Assets/_Project/Code/Gameplay/Heroes/Orun/Resource/` e `Assets/_Project/Code/Tests/EditMode/Heroes/Orun/ResourceTests.cs` (não toque em outros).
- **Restrições:** Sem números hardcoded; sem alocação por frame.
- **Aceite:** Um teste por linha da tabela de recurso (ganho, gasto, decaimento, início de sala) e por passiva.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Heroes.Orun -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: todos passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T4 — Dados do herói em CSV (kit, armas, Augments, Lattice)

- **Objetivo:** Autorar os dados de Orun: herói (atributos-base de 08 §3.1), Basic/Heavy, 5 habilidades com custo/CD/alcance/área/coeficiente/tags de 08 §7.4, 5 armas (Ritual Staff, Void Hammer, Great Mace, Totem, Ancient Scepter) com APS, moveset, bônus de kit (08 §7.6) e fator de DPS/implícito de 02 §3.5, Off Hand Idol (02 §3.6), 15 Augments (08 §7.5) e os 3 ramos da Lattice com notáveis/Keystones (08 §8.3).
- **Contexto:** Tags só do vocabulário de 00 §3.4; HitFlags separadas (R02). Frame data dos movesets é de 06 (R18).
- **Arquivos:** lê 08 §3, §7, §8; 02 §3.5–§3.6; 06 (frame data das armas de Orun); estrutura de CSV de M02; escreve `DataSource/heroes/orun/{hero,resource,skills,weapons,augments,lattice}.csv`, `DataSource/items/bases/orun.csv` e `DataSource/localization/hero_orun.csv` (não toque em outros).
- **Restrições:** Nenhum arquivo fora da pasta do herói; nenhum valor fora do que os docs definem (lacuna → Log).
- **Aceite:** Importação sem erros; contagens: 5 habilidades, 5 armas, 1 Off Hand base, 15 Augments, 3 ramos × (10 menores + 5 notáveis + 2 Keystones) + 8 nós de Núcleo.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros no Logs/data.log
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T5 — Comportamentos das 5 habilidades, Basic/Heavy e movesets das 5 armas

- **Objetivo:** Implementar Void Tendrils, Guardian Fragment, Devouring Rift, Corruption Wave, The Unbound, Basic e Heavy de cada arma e os bônus de kit das armas, com greybox/VFX placeholder e hooks de SFX, usando o framework de habilidades (delivery/effects/triggers) e a frame data de 06.
- **Contexto:** Comportamentos em 08 §7.3–§7.6; estados Taunt (Sentinel), Unstoppable (The Unbound), Root (Binding Tendrils), Knockback (Corruption Wave) definidos por 06; status por 07. As **Void Constructs** saem do spike de M10 para produção: Void Mass gera Lesser Fragments, Sentinel com Taunt, sacrifício por habilidades, dissolução ao sair da sala devolvendo 25 Void Mass por construção, e todas as exceções de teto de 08 §11.1 (Totem +1, Legion of the Deep +2, O5 +1, sets +1; teto absoluto 8).
- **Arquivos:** lê 08 §7; 06 (frame data, estados, telegraphs); 07 (status de Orun); 11 §3.6; escreve `Assets/_Project/Code/Gameplay/Heroes/Orun/Skills/`, `Assets/_Project/Content/Heroes/Orun/` e `Code/Tests/PlayMode/Heroes/Orun/KitSmokeTests.cs` (não toque em outros).
- **Restrições:** Nada de código por modificador; comportamento parametrizado por SkillSpec. Arte só greybox/gratuita (D4).
- **Aceite:** Smoke test PlayMode conjura cada habilidade com cada arma no sandbox sem erro; DPS normalizado das 5 armas dentro de ±5% (02) medido por teste.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Heroes.Orun -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: KitSmokeTests passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T5b — Void Constructs de produção (a partir do spike de M10)

- **Objetivo:** Implementar Lesser Fragment (pulso 60% DA Void a cada 1,5 s, raio 3 m, vida 25% de Orun), Sentinel (slam 150% DA a cada 2 s, Taunt a cada 5 s em 6 m, 30 s, vida 60%), API de sacrifício (Devouring Rift segurar, The Unbound todas), substituição do Fragment mais antigo quando cheio, dissolução ao sair da sala (+25 Void Mass cada, máx. 100) e passivas Voidkeeper (+3% DR por construção, máx. 18%) e Ruinous Bond (ataques aplicam 1 Decay).
- **Contexto:** 08 §7.1–§7.4, §11.1. IA aliada reaproveita a HFSM de M07 (alvo, seguir, atacar) com custo previsível (decisão 10/4 Hz, 11). Teto de construções resolvido por um único `ResourceCap` que soma exceções declaradas (validador de M02 limita a 8).
- **Arquivos:** lê 08 §7, §11.1, 07 (Decay), 11 §3.10 e §3.14, relatório do spike de Constructs no Log de docs/roadmap/M10-modificadores.md, código de M07 (AI); escreve `Assets/_Project/Code/Gameplay/Heroes/Orun/Constructs/`, `Assets/_Project/Code/AI/Allies/` e `Code/Tests/{EditMode,PlayMode}/Heroes/Orun/` (não toque em outros).
- **Restrições:** Pooling obrigatório; construções não contam como inimigos para tokens de ataque; nada de `Instantiate` em combate.
- **Aceite:** Testes cobrem teto 6/7/8 com cada exceção, não-acúmulo Totem × O5 (ambos Main Hand), dissolução e devolução de Void Mass; stress do módulo a 60 fps.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Heroes.Orun -testResults Logs/playmode.xml -logFile Logs/playmode.log` → esperado: ConstructTests e StressTest passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T5c — The Unbound, Commander's Will e Living Bastion

- **Objetivo:** Implementar a transformação The Unbound (10 s +2 s e +10% Increased por construção sacrificada, Unstoppable, açoites 120% DA a cada 0,5 s, explosão 500% DA; Abyssal Host mantém as construções e as torna Unbound com +60% Increased e +50% vida — sem More, R03), Commander's Will (construções reconjuram Tendrils/Wave a 30%, réplicas `IsEcho`) e Living Bastion (30% do dano recebido redistribuído às construções).
- **Contexto:** 08 §7.4, §7.5, §8.3. Corruption Wave atingindo construções gera ondas de 50% (Generation +1). Reconjuração por construção não pode recursar (00 R07).
- **Arquivos:** lê 08 §7, §8.3, 00 §5 R03/R07, 06 (Unstoppable); escreve `Assets/_Project/Code/Gameplay/Heroes/Orun/Unbound/`, `.../Orun/Keystones/` e testes em `Code/Tests/EditMode/Heroes/Orun/` (não toque em outros).
- **Restrições:** Nenhuma fonte More nova; respeitar Generation < 2 nas ondas e reconjurações.
- **Aceite:** Snapshot do SkillSpec de The Unbound com e sem Abyssal Host; teste de Generation para Commander's Will + Corruption Wave.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Heroes.Orun -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: todos passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T6 — Augments e nós da Lattice do herói

- **Objetivo:** Fazer os 15 Augments e os notáveis/Keystones de Orun funcionarem pela Lattice de M20 (exclusividade de 1 Augment por habilidade, gates, 2 Keystones), preferindo ops genéricos de modificador a código específico.
- **Contexto:** Augments não contam como modificadores de equipamento e não dão More (R03). Keystones ≤ +20% More sustentado. Exceções de teto de recurso só as de 08 §11.1.
- **Arquivos:** lê 08 §7.5, §8, §10.3, §11.1; 00 §5 R01/R03; código de M20 (Lattice); escreve `Assets/_Project/Code/Gameplay/Heroes/Orun/Augments/` e `Code/Tests/EditMode/Heroes/Orun/AugmentSpecTests.cs` (não toque em outros).
- **Restrições:** Se um Augment exigir op nova, implementá-la como op genérica e registrar no Log.
- **Aceite:** Snapshot de SkillSpec antes/depois para cada Augment; teste de exclusividade e de teto de Keystones.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Heroes.Orun -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: todos passam
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T7 — Modificadores ORU-xx de 03 no pipeline

- **Objetivo:** Importar todos os modificadores `ORU-xx` de 03 e garantir que compilam no SkillSpec pelas 6 fases (Conversão → Forma → Parâmetros → Replicação → Gatilhos → Recurso) sem código por modificador.
- **Contexto:** 03 é dono dos nomes e valores (R26). Modificadores escritos para premissas antigas das habilidades: em divergência vale 08 (D01 revisou). Grupos exclusivos e limite de 14 ativos (R30).
- **Arquivos:** lê 03 (seção de Orun e GEN-xx), 08 §7.4, 00 §5 R07/R26/R30, docs/roadmap/D01-revisao-modificadores.md; escreve `DataSource/modifiers/oru.csv` e `Code/Tests/EditMode/Heroes/Orun/ModifierSpecTests.cs` (não toque em outros).
- **Restrições:** Não editar 03; divergência de design → Log + principal.
- **Aceite:** 100% dos `ORU-xx` importados; snapshot por modificador; teste de grupos exclusivos.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` e `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode.Heroes.Orun -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: 0 erros e testes passando
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T8 — Itens nomeados O1–O16 e os 3 sets de Orun

- **Objetivo:** Autorar os 16 itens nomeados de Orun (02 §3.12.4, incluindo os Uniques de chefe/região/Challenge/Pinnacle com o campo `fonteUnique`) e os 3 sets (Abyssal Legion (lar: The Shattered Expanse), Hollow Sovereign (lar: The Hollow Depths), Eternal Warden (lar: The Astral Citadel)) com 6 peças cada (04), coluna `homeRegion` (05 §9.1) e Set Powers.
- **Contexto:** Estrutura de item de R25 (Legendary 4 fixos + 1 Afixo Aberto + 2 modificadores + efeito; peça de set sem efeito; Unique 5 fixos + 2 modificadores + Signature opcional). Faixas por tier de 01, pool por slot de 02, máx. 1 afixo `*` por peça de set. A origem do drop é declarada no item; regiões não listam itens. O Unique **O3 Bulwark of Nothing** (chefe The Ashen Colossus, já implementado em M17) é autorado aqui, porque depende dos modificadores ORU-xx.
- **Arquivos:** lê 02 §3.12.4 e §3.12.6, 04 (sets de Orun), 05 §9.1/§9.3, 01 §3.9, 00 §5 R03/R21–R28; escreve `DataSource/items/named/orun.csv`, `DataSource/sets/orun.csv` e `DataSource/localization/items_orun.csv` (não toque em outros).
- **Restrições:** Não escrever arquivos de região nem `universal*.csv`.
- **Aceite:** 16 itens + 18 peças + 6 Set Powers importados; validador sem erros (estrutura R25, pool, Faixa Nomeada 70–115%, tetos de More R03, teto de recurso ≤ 8).
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log` → esperado: 0 erros
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T9 — Loadouts das 3 builds × Rotas A/B/C e rodada do simulador

- **Objetivo:** Criar 9 loadouts (Void Commander, Corruption Master, Abyssal Guardian × Rotas A/B/C de 08 §7.7) com equipamento de referência de 00 §4 (fim de campanha, Eclipse, iLvl ~80, itens +6) e rodar o simulador de M14.
- **Contexto:** Regra de ouro (00 §4): rotas ofensivas da mesma build a ±10% entre si; rota defensiva compara sobrevivência. Simulador usa o DamageResolver real.
- **Arquivos:** lê 08 §7.7, 04 (análise de rotas), 00 §4, documentação do simulador em docs/roadmap/M14-simuladores-training.md; escreve `DataSource/sim/loadouts/orun/` e `Logs/sim/orun.csv` (relatório, não versionado) (não toque em outros).
- **Restrições:** Não ajustar números nesta tarefa — só medir.
- **Aceite:** Relatório com DPS sustentado, burst e TTK por loadout; desvio entre rotas calculado.
- **Verificação:** `"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loadouts/orun -quit -logFile Logs/sim.log` (nome do método definido em M14 — Refinar ao iniciar) → esperado: CSV com 9 linhas
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T10 — Revisão de game feel e de balanceamento

- **Objetivo:** Jogar Orun no Training Grounds com as 5 armas e as 3 builds, conferir a checklist de game feel de 06 e o relatório de T9; decidir ajustes dentro das faixas dos docs.
- **Contexto:** Ajuste fora das faixas definidas nos docs é decisão do usuário (registrar proposta).
- **Arquivos:** lê relatório de T9, 06 (critérios de feel), 00 §4; escreve CSVs do herói e este arquivo (não toque em outros).
- **Restrições:** No máximo 2 iterações de ajuste + re-simulação.
- **Aceite:** Rotas dentro de ±10% ou desvio justificado e levado ao usuário; checklist de feel marcada.
- **Verificação:** re-execução de T9 após ajustes
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

### T11 — Integração: HUD do recurso, desbloqueio, localização e docs

- **Objetivo:** Exibir Void Mass / Void Constructs no HUD (M06), aplicar a regra de desbloqueio (Orun desbloqueado pela primeira derrota do The Pale Sovereign (qualquer herói) + missão curta no Keeper (08 §9.3)) via sistema de M20/M19, completar strings pt-BR/en-US e atualizar status.
- **Contexto:** UI só reage a eventos (CLAUDE.md §4). Desbloqueio é dado, não código condicional por herói.
- **Arquivos:** lê 08 §9.3, código de M06/M19/M20; escreve `Assets/_Project/Code/UI/HUD/Resources/OrunResourceView.*`, `DataSource/localization/hero_orun.csv`, este arquivo, `docs/ROADMAP.md` (linha de status) e `CLAUDE.md` se surgir convenção nova (não toque em outros).
- **Restrições:** Não alterar layout de HUD dos outros heróis.
- **Aceite:** Herói selecionável conforme a regra; HUD correto; testes do módulo verdes.
- **Verificação:** `"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Heroes.Orun -testResults Logs/playmode.xml -logFile Logs/playmode.log` e `"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log` → esperado: tudo passa
- **Entrega:** no máximo 200 palavras — o que mudou/achou, evidência (arquivo:linha), o que não foi verificado/lido, pendências.

## Verificação do módulo
```bash
dotnet test tools/CoreTests
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Eclipse.Tests.EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
```
```bash
"$UNITY_EXE" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Eclipse.Tests.PlayMode.Heroes.Orun -testResults Logs/playmode.xml -logFile Logs/playmode.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.DataPipeline.ImportAndValidate -quit -logFile Logs/data.log
```
```bash
"$UNITY_EXE" -batchmode -nographics -projectPath . -executeMethod Eclipse.Editor.Simulation.BalanceSim.Run -simArgs loadouts/orun -quit -logFile Logs/sim.log
```
Esperado: todos os testes passam, `Logs/data.log` sem erros, relatório do simulador com 9 loadouts e desvio entre rotas ofensivas ≤ 10%.

## Riscos e rollback
- Invocações + hordas estouram orçamento de CPU (11 §3.14) → stress test com 8 construções e 120 inimigos é gate.
- Taunt do Sentinel quebra a leitura de tokens de ataque (09 §3.2) → construções recebem tokens próprios, sem roubar os do jogador.
- Commander's Will multiplica eventos → teste de Generation e teto de 30 entidades geradas/s por fonte (11).
- Framework insuficiente descoberto em T1 → T2 cria tarefa de extensão genérica; se a extensão for grande, pausar e abrir módulo de framework separado (decisão do usuário).
- Rollback: o módulo vive na branch `modulo/M23-orun`; nada vai para a base sem os testes do módulo verdes.

## Atualizações de documentação
- `docs/ROADMAP.md` §7: status de M23.
- `CLAUDE.md`: só se surgir convenção nova (ex.: op genérica de modificador, novo tipo de custo de recurso, padrão de entidade aliada).
- Divergências de design encontradas → proposta ao usuário e, se aprovada, registro em `docs/gdd/00-convencoes.md` §5 (nunca editar o doc dono sem aprovação).

## Prompt de disparo
```
Implemente o módulo M23 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/M23-orun.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/M23-orun.
Este módulo roda em paralelo com M26: respeite a partição de arquivos do plano.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 Plano criado.

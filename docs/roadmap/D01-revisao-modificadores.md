# D01 — Revisão de modificadores 03 × 08 (design)
Fase 0 · Onda 0 · Status: ✅ concluído
Depende de: — · Desbloqueia: M10 · Paralelo com: M00
Branch: modulo/D01-revisao-modificadores
Docs de referência: 03-modificadores §3 (regras), §4 (catálogo por habilidade), §5 (genéricos), §6 (orçamento), §10 (mapeamentos); 08-herois §4.4, §5.4, §6.4, §7.4 (habilidades), §4.5/§5.5/§6.5/§7.5 (Augments), §11.2 (renomeações); 00 §5 R07, R08, R12, R20, R26, R48, R51; 02 §3.12 e 04 (IDs citados).

## Objetivo
Garantir que cada um dos 176 modificadores específicos (e os 30 `GEN-xx`) de `docs/gdd/03-modificadores.md` descreve um efeito coerente com o comportamento **atual** da habilidade em `docs/gdd/08-herois.md` (tags, delivery, alcance, cargas/canal, recurso, Augments). Pendência registrada na varredura final do GDD v0.3: vários modificadores foram escritos sobre premissas antigas das habilidades.

## Critérios de saída (verificáveis)
1. Relatório `docs/roadmap/reports/D01-divergencias.md` lista as 20 habilidades, cada uma com veredito por modificador: `ok` / `ajuste textual` / `redesign` / `remover`.
2. Nenhum modificador classificado como `ajuste textual`, `redesign` ou `remover` permanece sem tratamento em 03.
3. Todo ID de modificador citado em `docs/gdd/02-equipamentos.md` e `docs/gdd/04-conjuntos.md` existe em 03 com o mesmo nome (checagem por script, saída anexada ao relatório).
4. Orçamento: todo modificador alterado continua entre 5 e 14 BP (03 §6.1) e respeita 03 §6.2 (máx./mín. ≤ 1,5×); nenhum modificador introduz More (00 §5 R03).
5. Grupos de exclusividade (Forma, Conversão, Modo de uso — 03 §3.3) continuam consistentes; Execute respeita R08; Echo respeita R07 (sem eco de eco fora de `Echo Recursion 1×`).
6. As duas decisões aprovadas em R51 (Fan of Chains 360°; zona de 3 s do modificador de Shattered Reality) estão refletidas em 03.
7. Se algum ID for removido ou renomeado, 02 e 04 são atualizados na mesma entrega (mesmo script de checagem verde).

## Fora de escopo
- Mudar habilidades em 08 (08 vence; divergência que exigir mudar 08 vira questão para o usuário).
- Rebalancear números fora dos modificadores alterados.
- Código.

## Premissas e decisões
- 08 é a fonte de verdade do comportamento das habilidades (00 §2; 03 §1 declara isso após a varredura final).
- Preferir **ajuste textual** a redesign; redesign só quando o efeito não faz sentido com o kit atual.
- IDs são estáveis: renomear nome é permitido, mudar ID só se o modificador for removido (e então 02/04 remapeiam para outro ID existente).

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Levantar divergências 03 × 08 por habilidade | investigação | investigador | — | `docs/roadmap/reports/D01-divergencias.md` | ✅ 206 avaliados (5 investigadores em paralelo) |
| T2 | Decidir tratamento de cada divergência | decisão | principal | T1 | `docs/roadmap/reports/D01-divergencias.md` (coluna Decisão) | ✅ Q1 (9 renomeações + 02/04) e Q2 (opção a) decididas pelo usuário |
| T3 | Aplicar decisões em 03 — Kael e Lyra | execução | executor | T2 | `docs/gdd/03-modificadores.md` | ✅ §4.1/§4.2: 64 linhas + 10 premissas |
| T4 | Aplicar decisões em 03 — Nyx, Orun e genéricos | execução | executor | T3 | `docs/gdd/03-modificadores.md` | ✅ §4.3/§4.4/§5: 89 linhas + 10 premissas; §3.2, §3.8, §4, §8, §10.1 |
| T5 | Remapear IDs removidos em 02/04 (só se houver) | execução | executor | T4 | `docs/gdd/02-equipamentos.md`, `docs/gdd/04-conjuntos.md` | ✅ nenhum ID removido; 16 nomes entre parênteses atualizados em 02/04 (autorizado pelo usuário) |
| T6 | Verificar IDs, orçamento e regras | investigação | investigador | T5 | `docs/roadmap/reports/D01-divergencias.md` (seção Verificação) | ✅ script verde; revisão das 153 linhas; F2/F3 corrigidas |
| T7 | Integrar: registrar no 00 §5 se algo mudou o contrato; status | integração | principal | T6 | `docs/gdd/00-convencoes.md` (só se necessário), `docs/ROADMAP.md`, este arquivo | ✅ ROADMAP §7 atualizado; 00 §5 sem nova R (nenhuma regra transversal mudou) |

## Ondas internas
1. T1
2. T2
3. T3 → T4 (sequencial: mesmo arquivo)
4. T5
5. T6
6. T7

## Briefs

**T1 — Levantar divergências 03 × 08**
- Objetivo: tabela de divergências entre cada modificador de habilidade e a habilidade atual.
- Contexto: PROJECT ECLIPSE é um ARPG; `docs/gdd/08-herois.md` define as 20 habilidades (5 por herói: Kael, Lyra, Nyx, Orun) e é a fonte de verdade; `docs/gdd/03-modificadores.md` define modificadores por habilidade (§4, IDs `KAE-`, `LYR-`, `NYX-`, `ORU-`) e genéricos (§5, `GEN-`). Muitos modificadores foram escritos antes de 08 fechar tags/delivery/canal/cargas.
- Arquivos: lê `docs/gdd/08-herois.md` §4–7 e §11, `docs/gdd/03-modificadores.md` §3–6, `docs/gdd/00-convencoes.md` §5 (R07, R08, R12, R20, R48, R51); escreve só `docs/roadmap/reports/D01-divergencias.md`.
- Restrições: não editar docs do GDD.
- Aceite: para cada habilidade, um bloco com (a) resumo da habilidade em 08 (tags, delivery, alcance, custo/recurso, cargas/canal); (b) tabela `ID completo (ex.: KAE-CS-01) | Nome | Veredito (ok/ajuste textual/redesign/remover) | Motivo (1 linha) | Proposta`. Genéricos `GEN-xx` avaliados contra o filtro de tags e o vocabulário 00 §3.4.
- Verificação: `grep -c "^| KAE-\|^| LYR-\|^| NYX-\|^| ORU-\|^| GEN-" docs/roadmap/reports/D01-divergencias.md` → ≥ 206 linhas.
- Entrega: ≤ 200 palavras — contagem por veredito e por herói, os 5 casos mais graves, o que não foi lido.

**T2 — Decidir tratamento** (principal)
- Para cada `redesign`/`remover`, decidir o novo efeito ou o ID substituto; registrar na coluna Decisão. Divergência que só se resolve mudando 08 → perguntar ao usuário (uma pergunta agrupada).

**T3 / T4 — Aplicar decisões em 03**
- Objetivo: 03 coerente com as decisões da coluna Decisão do relatório.
- Contexto: mesmo de T1; a coluna Decisão é final.
- Arquivos: lê o relatório e 08; escreve só `docs/gdd/03-modificadores.md` (T3: §4.1 Kael e §4.2 Lyra; T4: §4.3 Nyx, §4.4 Orun, §5 genéricos e §10 mapeamentos se IDs mudarem).
- Restrições: não mudar IDs exceto remoções decididas; manter faixas dentro de 03 §6.2 e BP 5–14; sem More; manter estrutura obrigatória de 03 (atualizar §2 Decisões e §8 Conflitos se necessário).
- Aceite: cada linha decidida aplicada; Fan of Chains 360° e zona de 3 s de Shattered Reality presentes (R51).
- Verificação: `grep -n "Fan of Chains\|Shattered Reality" docs/gdd/03-modificadores.md` mostra os textos atualizados; contagem de IDs por habilidade inalterada salvo remoções.
- Entrega: ≤ 150 palavras — IDs alterados/removidos, pendências.

**T5 — Remapear IDs removidos** (só se T3/T4 removeram IDs)
- Objetivo: nenhum ID removido citado em 02/04.
- Arquivos: lê `docs/gdd/03-modificadores.md` §10 e o relatório; escreve `docs/gdd/02-equipamentos.md`, `docs/gdd/04-conjuntos.md`.
- Restrições: substituto deve ter a mesma habilidade e, em 04, a mesma categoria do par Habilidade×Categoria; sem repetir ID dentro de um set (04).
- Verificação: script da T6 sem IDs ausentes.

**T6 — Verificar**
- Objetivo: confirmar critérios de saída 3–6.
- Arquivos: lê 02, 03, 04, 08 e 00 §5; escreve a seção "Verificação" do relatório.
- Verificação: script (Python ou PowerShell) que extrai IDs `(KAE|LYR|NYX|ORU)-[A-Z]{2}-\d{2}|GEN-\d{2}|SIG-[A-Z0-9]+` de 02 e 04 e confere existência e nome em 03 → saída "0 ausentes, 0 nomes divergentes"; tabela de BP dos alterados.
- Entrega: ≤ 120 palavras — resultado do script, violações.

**T7 — Integrar** (principal)
- Se a revisão mudou alguma regra transversal, registrar R5x em `docs/gdd/00-convencoes.md` §5. Atualizar status deste arquivo e de `docs/ROADMAP.md` §7.

## Verificação do módulo
- Script da T6 → "0 ausentes, 0 nomes divergentes".
- Relatório com 206 linhas avaliadas e coluna Decisão preenchida para todo veredito ≠ ok.

## Riscos e rollback
- Redesigns em cascata mudando 04 (Set Powers dependem de modificadores) → limitar redesign ao modificador; se um Set Power ficar incoerente, registrar como questão para M28/04 em vez de reescrever o set.
- Rollback: `git revert` do commit do módulo (só docs).

## Atualizações de documentação
- `docs/gdd/03-modificadores.md` (§2, §4, §5, §8, §10), `docs/gdd/02`/`04` se T5 rodar, `docs/gdd/00-convencoes.md` §5 se necessário, `docs/ROADMAP.md` §7. CLAUDE.md não muda.

## Prompt de disparo
```
Implemente o módulo D01 do PROJECT ECLIPSE.
Leia CLAUDE.md, docs/roadmap/D01-revisao-modificadores.md e os docs de referência listados nele.
Use a skill /planejar tomando o plano do módulo como base (tarefas, briefs e verificação já definidos);
peça minha aprovação antes de executar. Trabalhe na branch modulo/D01-revisao-modificadores.
Ao final, atualize o Status e o Log do módulo, a tabela de status de docs/ROADMAP.md e o CLAUDE.md, se aplicável.
```

## Log
- 2026-11-07 plano criado.
- 2026-11-07 T1: 206 modificadores avaliados (53 ok, 148 ajuste textual, 5 redesign, 0 remover); baseline de IDs 02/04 → 03: 0 ausentes, 0 nomes divergentes; 58 faixas com máx/mín > 1,5. Relatório: `docs/roadmap/reports/D01-divergencias.md`.
- 2026-11-07 T2: propostas aceitas; pendentes Q1 (9 renomeações que tocam 02/04) e Q2 (Construct Overflow × 08 §11.1). Pendências P1–P12 para outros docs registradas no relatório §3. Restrição da sessão: 02/04/08/00 não editados aqui.
- 2026-11-07 Q1/Q2 decididas pelo usuário: renomear os 9 nomes em colisão e atualizar 02/04 (só os nomes entre parênteses); Construct Overflow mantém +1 no teto, com pendência em 08 §11.1 (P2).
- 2026-11-07 T3/T4: decisões aplicadas em 03 (§3.2 agora diz que o modificador declara a exclusão com o Augment, alinhado a 08 §10.1; §3.8; §4 premissas → 08 §x.4; §8 item 5; §10.1). T5: 16 citações de nome em 02/04 atualizadas.
- 2026-11-07 T6: `docs/roadmap/reports/D01-check_ids.py` → 206 modificadores; 0 ausentes, 0 nomes divergentes; BP 5–14; 0 faixas > 1,5; sem More. Revisão independente das 153 linhas: 3 falhas leves; F2 e F3 corrigidas, F1 mantida pela regra §3.2. Nova colisão de nome: LYR-SR-08 Gravity Well = Augment de Orun (P13, aguarda decisão). Pendências P2–P13 para 08/02/D02: relatório §3.
- 2026-11-07 P13 decidida pelo usuário: LYR-SR-08 Gravity Well → **Inward Spiral** em 03, 02 l.392 e 04 l.304 (10 renomeações no total).

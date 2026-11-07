# D02 — Lacunas numéricas de design
Fase 0 · Onda 1 · Status: ⏳ não iniciado
Depende de: — · Desbloqueia: M07 (valores finais), M16, M19, M20 · Paralelo com: M01
Branch: modulo/D02-lacunas-numericas
Docs de referência: 06 §3.13 (TTK), 09 §2 (stats relativos, Pontos de Ameaça), 10 §5 (XP), §6 (moedas, Rift Shards), §7 (Monster Level), 00 §5 R16, R35, R36, R40, R44

## Objetivo
Fechar os números que os planos dos módulos encontraram sem dono definido, para que nenhum módulo precise usar placeholder `TODO(design)` em dados finais.

## Critérios de saída (verificáveis)
1. **Curva base de HP e dano por Monster Level** (unidade "Standard" de 09, eLvl 1–110) definida em tabela e fórmula, coerente com o TTK de 06 §3.13 e o eLvl de 10 §7 — registrada em 06 (dono do TTK) com referência cruzada em 09 e 10.
2. **XP por Ponto de Ameaça** (constante e escala por dificuldade) definida em 10, coerente com a curva de XP de 10 §5 (nível 50 em 40–50 h).
3. **Fator de dificuldade da conversão Rift Shards → Gold** definido em 10 §6.
4. Cada decisão registrada como nova resolução em `docs/gdd/00-convencoes.md` §5 (R53+), com os docs afetados.
5. Planilhas de verificação em `docs/roadmap/reports/D02-calculos.md` mostrando que os valores batem com os alvos (TTK por dificuldade, horas até o nível 50, Gold por run).

## Fora de escopo
Implementação; mudança de qualquer outro número já decidido.

## Premissas e decisões
- Até o D02 fechar, M07 usa a curva provisória derivada de 06 §3.13 (registrada no plano do M07).
- Se um alvo dos docs for inalcançável com fórmulas simples, a decisão é da sessão principal e vira resolução nova.

## Tarefas
| ID | Tarefa | Tipo | Executor | Depende | Escreve | Status |
|----|--------|------|----------|---------|---------|--------|
| T1 | Levantar alvos e restrições numéricas nos docs | investigação | investigador | — | `docs/roadmap/reports/D02-calculos.md` (seção Alvos) | ⏳ |
| T2 | Propor curva HP/dano por eLvl, XP/PA e fator Rift Shards→Gold, com cálculos | decisão | principal | T1 | `docs/roadmap/reports/D02-calculos.md` | ⏳ |
| T3 | Registrar nos docs donos e em 00 §5 | execução | executor | T2 | `docs/gdd/06-combate.md`, `docs/gdd/10-progressao.md`, `docs/gdd/09-inimigos.md` (referência), `docs/gdd/00-convencoes.md` | ⏳ |

## Ondas internas
1. T1 · 2. T2 · 3. T3

## Briefs

**T1 — Levantar alvos**
> Objetivo: listar todos os alvos numéricos que restringem (a) HP/dano de inimigos por Monster Level, (b) XP por Ponto de Ameaça, (c) conversão Rift Shards→Gold.
> Contexto: PROJECT ECLIPSE, docs em `docs/gdd/`; precedência de `00-convencoes.md` §5. Alvos relevantes: TTK de 06 §3.13 e R16, stats relativos e orçamento de PA de 09 §2, curva de XP e tempos de 10 §5, Monster Level de 10 §7, Rift Shards de 10 §6, letalidade de 06 (R15), dano do jogador do exemplo de 01 §3.12.
> Arquivos: lê `docs/gdd/*`; escreve só `docs/roadmap/reports/D02-calculos.md` (seção "Alvos").
> Aceite: cada alvo com valor, doc §, e qual lacuna ele restringe.
> Verificação: revisão da sessão principal.
> Entrega: até 200 palavras + o arquivo.

**T2 — Propor valores** (sessão principal): fórmulas simples (exponencial por eLvl, multiplicador por dificuldade), tabela de verificação por dificuldade e região, decisão final.

**T3 — Registrar**
> Objetivo: inserir as decisões de T2 nos docs donos sem alterar outros números.
> Arquivos: escreve `06-combate.md` (curva base por eLvl, perto de §3.13), `10-progressao.md` (XP/PA em §5, fator de conversão em §6), `09-inimigos.md` (só a referência à curva), `00-convencoes.md` §5 (R53+).
> Aceite: `grep -n "TODO(10)\|TODO(06)" docs/` não encontra mais essas lacunas nos docs.
> Verificação: `grep -rn "R53" docs/gdd/00-convencoes.md` → resolução presente.
> Entrega: até 150 palavras.

## Verificação do módulo
- `docs/roadmap/reports/D02-calculos.md` com alvos e cálculos.
- Resoluções R53+ presentes em 00 §5; docs donos atualizados.

## Riscos e rollback
- Curva provisória do M07 divergir muito da final → M07 só troca o CSV; nenhum código depende dos valores.

## Atualizações de documentação
`docs/ROADMAP.md` §7 (status) e §9 (lacunas).

## Prompt de disparo
```
Execute o módulo D02 do PROJECT ECLIPSE.
Leia CLAUDE.md e docs/roadmap/D02-lacunas-numericas.md e siga o plano com a skill /planejar;
peça minha aprovação antes de registrar as decisões nos docs. Trabalhe na branch modulo/D02-lacunas-numericas.
Ao final, atualize o Status e o Log do módulo e a tabela de status de docs/ROADMAP.md.
```

## Log
- 2026-11-07 Criado a partir das lacunas apontadas pelos planos de M07, M16, M19 e M20.

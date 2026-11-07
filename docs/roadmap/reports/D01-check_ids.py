"""D01 ID check: every modifier ID cited in 02/04 exists in 03 with the same name.
Also checks 03 §10.2 index categories vs catalog categories, and BP/ratio rules.
Usage: python -I docs/roadmap/reports/D01-check_ids.py .   (PYTHONIOENCODING=utf-8 no Windows)
"""
import re, sys, pathlib

root = pathlib.Path(sys.argv[1])
gdd = root / "docs" / "gdd"
t03 = (gdd / "03-modificadores.md").read_text(encoding="utf-8").splitlines()

cat = {}  # id -> (name, category, group, rarity, bp, value_text, line)
prefix = None
in_catalog = False
for i, line in enumerate(t03, 1):
    if line.startswith("## 4."):
        in_catalog = True
    if line.startswith("## 6."):
        in_catalog = False
    if not in_catalog:
        continue
    m = re.match(r"^#### ((?:KAE|LYR|NYX|ORU)-[A-Z]{2}) ", line)
    if m:
        prefix = m.group(1)
        continue
    if line.startswith("## 5."):
        prefix = None
    cells = [c.strip() for c in line.strip().strip("|").split("|")] if line.startswith("|") else None
    if not cells or len(cells) < 6:
        continue
    if prefix and re.fullmatch(r"\d{2}", cells[0]):
        # | NN | Nome | Cat | Gr | Efeito | Valor | Mín | BP |
        cat[f"{prefix}-{cells[0]}"] = (cells[1], cells[2], cells[3], cells[-2], cells[-1], cells[5], i)
    elif re.fullmatch(r"GEN-\d{2}", cells[0]):
        # | ID | Nome | Cat | Gr | Requer | Exclui | Efeito | Valor | Mín | BP |
        cat[cells[0]] = (cells[1], cells[2], cells[3], cells[-2], cells[-1], cells[7], i)

print(f"03 catalog: {len(cat)} modifiers "
      f"({sum(1 for k in cat if not k.startswith('GEN'))} specific, {sum(1 for k in cat if k.startswith('GEN'))} GEN)")

norm = lambda s: re.sub(r"[*_`’']", "", s).strip().lower()
idre = re.compile(r"((?:KAE|LYR|NYX|ORU)-[A-Z]{2}-\d{2}|GEN-\d{2})(?:\s*\(([^)]+)\)|\s+([A-Z][^;|·,()\n]*?))?(?=\s*(?:[;|·,)]|$|\s—|\s-\s))")
missing, mismatched, cites = [], [], 0
for fn in ["02-equipamentos.md", "04-conjuntos.md"]:
    for i, line in enumerate((gdd / fn).read_text(encoding="utf-8").splitlines(), 1):
        for m in idre.finditer(line):
            cites += 1
            mid, name = m.group(1), (m.group(2) or m.group(3) or "").strip()
            if mid not in cat:
                missing.append(f"{fn}:{i} {mid}")
            elif name and norm(name) != norm(cat[mid][0]) and not norm(name).startswith(norm(cat[mid][0])):
                mismatched.append(f"{fn}:{i} {mid} cita '{name}' / 03 = '{cat[mid][0]}'")
print(f"02+04 citations: {cites}; {len(missing)} ausentes, {len(mismatched)} nomes divergentes")
for x in missing + mismatched:
    print("  ", x)

# §10.2 index vs catalog category
bad_idx = []
in_idx = False
for i, line in enumerate(t03, 1):
    if line.startswith("### 10.2"):
        in_idx = True
        continue
    if in_idx and line.startswith("|") and "Categoria" not in line and "---" not in line:
        for part in line.split("|")[2].split("·"):
            if ":" not in part:
                continue
            c, ids = part.split(":", 1)
            for mid in re.findall(r"[A-Z]{3}-[A-Z]{2}-\d{2}", ids):
                if mid not in cat:
                    bad_idx.append(f"03:{i} índice cita {mid} inexistente")
                elif cat[mid][1] != c.strip():
                    bad_idx.append(f"03:{i} índice põe {mid} em {c.strip()}, catálogo diz {cat[mid][1]}")
print(f"§10.2 index: {len(bad_idx)} inconsistências")
for x in bad_idx:
    print("  ", x)

# BP and roll ratio
bp_bad, ratio_bad = [], []
for k, (name, c, g, rar, bp, val, ln) in cat.items():
    try:
        b = int(bp)
        if not 5 <= b <= 14:
            bp_bad.append(f"{k} {name} BP={b}")
    except ValueError:
        bp_bad.append(f"{k} {name} BP='{bp}'")
    for lo, hi in re.findall(r"\[\s*[−-]?\s*([\d,\.]+)\s*(?:–|-|a)\s*[−-]?\s*([\d,\.]+)\s*\]", val):
        lo_f, hi_f = float(lo.replace(",", ".")), float(hi.replace(",", "."))
        a, z = sorted((lo_f, hi_f))
        if a > 0 and z / a > 1.5 + 1e-9:
            ratio_bad.append(f"{k} {name} [{lo}–{hi}] = {z/a:.2f}")
print(f"BP fora de 5–14: {len(bp_bad)}")
for x in bp_bad:
    print("  ", x)
print(f"Faixas com máx/mín > 1,5: {len(ratio_bad)}")
for x in ratio_bad:
    print("  ", x)
more = [f"{k} {v[0]}" for k, v in cat.items() if re.search(r"\bMore\b", v[5] + " " + t03[v[6]-1])]
print(f"Menções a More no catálogo: {len(more)}")
for x in more:
    print("  ", x)

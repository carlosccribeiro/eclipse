"""D02 - reference model for the numeric gaps (see D02-calculos.md).

Run: python -I docs/roadmap/reports/D02-modelo.py
All player-side numbers are declared assumptions (section 2 of the report); M14 recalibrates.
"""

# ---------------- proposed formulas ----------------
def hp_std(e):
    return 36 * 1.048 ** (e - 1)

def dmg_base(e):
    return 20 * 1.048 ** (e - 1) if e <= 24 else 20 * 1.048 ** 23 * 1.016 ** (e - 24)

M_LET = {"N": 1.0, "V": 1.2, "NM": 1.5, "E": 2.0}
DENS = {"N": 1.0, "V": 1.1, "NM": 1.2, "E": 1.3}

def tier_of(e):
    return "N" if e <= 24 else "V" if e <= 49 else "NM" if e <= 69 else "E"

def dmg_std(e):
    return dmg_base(e) * M_LET[tier_of(e)]

def dps_ref(e):
    return hp_std(e) / 2.2

CONTENT_PTS = [(1, 1), (24, 25), (49, 35), (69, 45), (84, 50), (110, 50)]

def content_level(e):
    for (a, la), (b, lb) in zip(CONTENT_PTS, CONTENT_PTS[1:]):
        if a <= e <= b:
            return la + (lb - la) * (e - a) / (b - a)
    return 50

def xp_per_pa(e):
    return 2 * content_level(e) ** 1.1 / DENS[tier_of(min(e, 84))]

SHARDS_PER_PA = 0.2
GUARDIAN_PA, BOSS_PA = 60, 150
GOLD_FACTOR = {"N": 1, "V": 3, "NM": 9, "E": 19}

# ---------------- reference player (assumptions) ----------------
TIERS = [(1, 20), (21, 40), (41, 60), (61, 80), (81, 100)]
DMG = [6, 10, 14, 18.5, 25]; SKL = [4.5, 7.5, 10.5, 14, 18.5]; CC = [1.25, 2, 2.75, 3.75, 5]
CD = [7.5, 13, 18.5, 25, 34]; AS = [2.5, 3.5, 5, 6, 7.5]; PRI = [4.5, 9, 14.5, 21, 28.5]
HPM = [22.5, 47.5, 80, 122.5, 175]
COV = {"N": 0.6, "V": 0.85, "NM": 0.95, "E": 1.0}
UPG = {"N": 0, "V": 2, "NM": 4, "E": 6}
SETS = {"N": 1.10, "V": 1.25, "NM": 1.40, "E": 1.50}
ROTATION, TARGET_MIT, BLESS_MID, PLAYER_RES = 1.8, 0.775, 1.125, 0.30

def item_tier(i):
    return next(k for k, (a, b) in enumerate(TIERS) if a <= i <= b)

def hero_level(e):
    return content_level(e)

def weapon_power(i):
    return 10 * 1.0235 ** (min(i, 100) - 1)

def player_dps(e):
    d, t, L = tier_of(e), item_tier(e), hero_level(e)
    c, up = COV[d], 1 + 0.03 * UPG[d]
    inc = 1 + c * (1.5 * DMG[t] + SKL[t] + DMG[t]) * up / 100 + 0.005 * (L - 1)
    dex = 15 + 2 * (L - 1) + c * 2 * PRI[t] * up
    crit = 1 + (5 + c * 2 * CC[t] * up + dex * 0.015) / 100 * (50 + c * 2 * CD[t] * up) / 100
    aspd = 1 + (c * 2 * AS[t] * up + dex * 0.04) / 100
    core = 1 + (20 + 3 * (L - 1) + c * 7 * PRI[t] * up) * 0.0008
    return weapon_power(e) * ROTATION * inc * crit * aspd * core * SETS[d] * TARGET_MIT

def player_life(e):
    d, t, L = tier_of(e), item_tier(e), hero_level(e)
    base = 210 + 18.75 * (L - 1) + COV[d] * 5.6 * HPM[t] * (1 + 0.03 * UPG[d])
    return base * (1 + (13.75 + 1.75 * (L - 1)) * 0.0005)

# ---------------- run model ----------------
AFFIXES = {"N": 1, "V": 2, "NM": 2.25, "E": 3}
PACKS = {"N": 1, "V": 1, "NM": 1.5, "E": 1.5}

def line_weights(L):
    if L == 1: return {"C": 1}
    if L == 2: return {"C": 55 / 90, "Sh": 15 / 90, "Ev": 10 / 90, "Tr": 10 / 90}
    if L <= 4: return {"C": .55, "E": .10, "Tr": .10, "Sh": .15, "Ev": .10}
    if L <= 8: return {"C": .40, "E": .20, "Tr": .08, "Sh": .12, "Ev": .12, "Ch": .08}
    return {"C": .35, "E": .25, "Tr": .08, "Sh": .10, "Ev": .10, "Ch": .12}

def run_pa_and_shards(d):
    pa = shards = 0.0
    for L in [1, 2, 3, 4, 6, 7, 8, 10, 11, 12]:
        w, bud = line_weights(L), (20 + 3 * L) * DENS[d]
        elite = 1.2 * bud + 2 * (15 + 4 * AFFIXES[d]) * PACKS[d]
        pa += w.get("C", 0) * bud + w.get("E", 0) * elite + w.get("Ch", 0) * 40 * DENS[d] + w.get("Ev", 0) * 10
        shards += SHARDS_PER_PA * (w.get("C", 0) * bud + w.get("E", 0) * elite + w.get("Ch", 0) * 40) \
            + w.get("Tr", 0) * 50 + w.get("Ev", 0) * 10
    return pa, shards

SUCCESS, FAIL_AT, RUN_MIN = 0.65, 0.55, 30

def xp_per_hour(e):
    d = tier_of(min(e, 84))
    pa, _ = run_pa_and_shards(d)
    xp = (SUCCESS * (pa + GUARDIAN_PA + BOSS_PA) + (1 - SUCCESS) * FAIL_AT * pa) * xp_per_pa(e)
    minutes = SUCCESS * RUN_MIN + (1 - SUCCESS) * FAIL_AT * RUN_MIN * 0.9
    return xp * 60 / minutes

CUM = [0.0]
for _L in range(1, 50):
    CUM.append(CUM[-1] + 200 * _L ** 1.8)

def level_from_xp(x):
    for L in range(1, 50):
        if CUM[L] > x:
            return L - 1 + (x - CUM[L - 1]) / (200 * L ** 1.8)
    return 50.0

SCHEDULE = [(1.75, (1, 6))] + [((13.5 - 1.75) / 4, r) for r in [(6, 10), (11, 15), (16, 20), (21, 24)]] \
    + [(10.5 / 5, r) for r in [(25, 30), (30, 35), (35, 40), (40, 45), (45, 49)]] \
    + [(13.5 / 5, r) for r in [(50, 54), (54, 58), (58, 62), (62, 66), (66, 69)]] \
    + [(5.0, (66, 69)), (12.5, (75, 84))]

def main():
    print("eLvl tier  HP_std  dmg_std  dps_ref  TTK_std  hit%life")
    for e in [1, 6, 10, 15, 20, 24, 25, 35, 49, 50, 58, 69, 75, 80, 84]:
        ttk = hp_std(e) / (player_dps(e) * BLESS_MID)
        hit = dmg_std(e) * (1 - PLAYER_RES) / player_life(e) * 100
        print(f"{e:4d} {tier_of(e):>4} {hp_std(e):7.0f} {dmg_std(e):8.1f} {dps_ref(e):8.1f} {ttk:8.2f} {hit:8.1f}%")
    avg = lambda f, r: sum(map(f, r)) / len(r)
    n = range(1, 25)
    for name, r in [("V", range(25, 50)), ("NM", range(50, 70)), ("E", range(75, 85))]:
        print(f"relative {name}: life x{avg(hp_std, r) / avg(hp_std, n):.2f}  damage x{avg(dmg_std, r) / avg(dmg_std, n):.2f}")
    t = x = 0.0
    reached = None
    print("\nhours  level  eLvl")
    for hrs, (a, b) in SCHEDULE:
        for k in range(400):
            e = round(a + (b - a) * (k + 0.5) / 400)
            x += xp_per_hour(e) * hrs / 400
            t += hrs / 400
            if reached is None and x >= CUM[49]:
                reached = t
        print(f"{t:5.1f} {level_from_xp(x):6.1f}  {a}-{b}")
    print(f"level 50 at {reached:.1f} h")
    for d in DENS:
        pa, sh = run_pa_and_shards(d)
        print(f"run {d}: PA {pa:.0f} (+G/B {pa + GUARDIAN_PA + BOSS_PA:.0f}), gross shards {sh:.0f}")
    gold = {"N": 800, "V": 2500, "NM": 7000, "E": 15000}
    print("conversion share of run Gold (65 shards left):",
          {d: f"{65 * 5 * GOLD_FACTOR[d] / gold[d]:.0%}" for d in gold})

if __name__ == "__main__":
    main()

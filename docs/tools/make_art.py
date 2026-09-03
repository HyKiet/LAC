"""Sinh sprite pixel art cho LAC bang bang mau Dong Ho 24 mau da chot (T-17).

Moi sprite duoc viet duoi dang luoi ky tu, moi ky tu la mot chi so mau. Cach nay
cho phep kiem soat tung diem anh va doc lai duoc diff trong Git, khac voi anh nhi phan.
"""
import os
from PIL import Image

OUT = "Assets/_LAC/Art/Sprites"

P = {
    '.': None, ' ': None,
    'a': "F4EADA", 'b': "E0CFAF", 'c': "BFA981", 'd': "94805C",
    'e': "6E6555", 'f': "47403A", 'g': "2B2724", 'h': "15130F",
    'i': "FBDD82", 'j': "EDBB3E", 'k': "C08D20", 'l': "8A5F14",
    'm': "9CCFC0", 'n': "4FA694", 'o': "2F7480", 'p': "1C4B5C", 'q': "112E3E",
    'r': "B37F4F", 's': "7F5432", 't': "50331E",
}


def rgba(ch):
    h = P.get(ch)
    if h is None:
        return (0, 0, 0, 0)
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def grid(rows, w=32, h=32):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()
    for y in range(h):
        row = rows[y] if y < len(rows) else ""
        row = row.ljust(w, '.')[:w]
        for x, ch in enumerate(row):
            px[x, y] = rgba(ch)
    return im


def sheet(frames, path, w=32, h=32):
    im = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        im.paste(f, (i * w, 0), f)
    im.save(path)
    return im


def shift(rows, dy):
    """Doi toan bo hinh xuong dy dong, dung cho nhun nguoi."""
    if dy == 0:
        return list(rows)
    if dy > 0:
        return ["." * 32] * dy + list(rows[:-dy])
    return list(rows[-dy:]) + ["." * 32] * (-dy)


# =====================================================================
# THACH SANH — tieu phu, khan mo ri, ao nau yem cham, om dan bau
# Chan cham dat o y=29. Nhan vat cao 26px => 0.81 don vi o PPU 32.
# =====================================================================
TS_HEAD = [
    "................................",
    "................................",
    "................................",
    "..........hhhhhh................",
    ".........htttttth...............",
    "........httssssthh..............",
    "........htsssssssh..............",
    ".........htttttth...............",
    "..........hbbbbh................",
    ".........hbaaaabh...............",
    ".........hbagagbh...............",
    ".........hbaaaabh...............",
    "..........hbbbbh................",
    "..........hcccch................",
]
TS_TORSO = [
    ".......hhhrrrrrrhhh.............",
    "......hrrrrrrrrrrrrh............",
    "......hrsssssssssssrh...........",
    "......hrsooooooooosrh...........",
    "......hrsooooooooosrh...........",
    ".......hrsssssssssrh............",
    "........hssssssssh..............",
    "........htttttth................",
    ".........hsssssh................",
]
TS_LEGS_STAND = [
    ".........hcc..cch...............",
    ".........hcc..cch...............",
    ".........hbc..cbh...............",
    "........hbbc..cbbh..............",
    "........hffh..hffh..............",
    ".........hh....hh...............",
]
TS_LEGS_A = [
    "........hcc...cch...............",
    ".......hcc.....cch..............",
    ".......hbc.....cbh..............",
    "......hbbc.....cbbh.............",
    "......hffh.......hffh...........",
    ".......hh.........hh............",
]
TS_LEGS_B = [
    "..........hcccch................",
    "..........hcccch................",
    "..........hbccbh................",
    ".........hbbccbbh...............",
    ".........hffhhffh...............",
    "..........hhhhh.................",
]


def thach(legs, dy=0, arm=None):
    rows = TS_HEAD + TS_TORSO + legs
    rows = shift(rows, dy)
    if arm:
        rows = paint(rows, arm)
    return grid(rows)


def paint(rows, marks):
    """Ve them diem anh len luoi: marks = [(x, y, ch), ...]"""
    out = [list(r.ljust(32, '.')[:32]) for r in rows]
    while len(out) < 32:
        out.append(list("." * 32))
    for x, y, ch in marks:
        if 0 <= x < 32 and 0 <= y < 32:
            out[y][x] = ch
    return ["".join(r) for r in out]


# Dan bau: mot day dan doc, can dan cong, KEM CANH TAY noi tu than den dan.
# Khong ve tay thi nhac cu troi lo lung ben canh nguoi — nhin ra ngay o ban dau.
def dan(cy):
    """Dan bau voi truc dat o dong cy, cong canh tay nam ngang o dong 17."""
    m = []
    # canh tay phai: tu vai (x=19) vuon ra (x=23)
    for x in range(19, 24):
        m.append((x, 17, 'b'))
        m.append((x, 18, 'h'))
    m.append((19, 16, 'h'))
    # than dan doc
    for dy in range(-4, 5):
        m.append((24, cy + dy, 'k' if dy == 0 else 'l'))
    # can dan cong ve phia truoc
    m += [(25, cy - 5, 'j'), (26, cy - 6, 'i'), (26, cy - 7, 'i'),
          (25, cy + 5, 'l'), (24, cy + 6, 'l')]
    # bau dan
    m += [(23, cy + 2, 'l'), (23, cy + 3, 'k'), (22, cy + 3, 'l')]
    return m


DAN_LOW = dan(19)
DAN_MID = dan(17)
DAN_HIGH = dan(15)
DAN_STRIKE = dan(17) + [(26, 15, 'm'), (27, 16, 'm'), (26, 19, 'm'),
                        (28, 17, 'm'), (27, 13, 'm'), (27, 21, 'm')]

# Nam guc: dau ben trai, than duoi ra ngang — doc ra "da nga" chu khong phai "bien mat".
TS_DOWN = [
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    "................................",
    ".....hhhh.......................",
    "....htttth..hhhhhhhh............",
    "...hbaaabh.hrsssssssh...........",
    "...hbagabhhrsoooooosrh..........",
    "....hbbbbhhrssssssssrh..........",
    ".....hhhhh.hhhhhhhhhhh..........",
    "................................",
    "................................",
]

ts_idle = [thach(TS_LEGS_STAND, 0, DAN_LOW), thach(TS_LEGS_STAND, 0, DAN_LOW),
           thach(TS_LEGS_STAND, 1, DAN_LOW), thach(TS_LEGS_STAND, 0, DAN_LOW)]
ts_walk = [thach(TS_LEGS_A, 0, DAN_LOW), thach(TS_LEGS_STAND, 1, DAN_LOW),
           thach(TS_LEGS_B, 0, DAN_LOW), thach(TS_LEGS_A, 0, DAN_LOW),
           thach(TS_LEGS_STAND, 1, DAN_LOW), thach(TS_LEGS_B, 0, DAN_LOW)]
ts_atk = [thach(TS_LEGS_STAND, 0, DAN_MID), thach(TS_LEGS_STAND, -1, DAN_HIGH),
          thach(TS_LEGS_STAND, 1, DAN_STRIKE), thach(TS_LEGS_STAND, 0, DAN_MID)]
ts_hurt = [thach(TS_LEGS_STAND, 1, DAN_LOW), thach(TS_LEGS_STAND, 2, DAN_LOW),
           thach(TS_LEGS_STAND, 1, DAN_LOW)]
ts_death = [thach(TS_LEGS_STAND, 1, DAN_LOW),
            thach(TS_LEGS_B, 4, DAN_LOW),
            thach(TS_LEGS_B, 8, dan(27)),
            grid(TS_DOWN)]

# =====================================================================
# CO HON — hon lang thang, khong co chan, duoi ao rach bay
# Dung nhom cham lam than, nhom than lam net. Rong 20px => 0.63 don vi,
# nho hon ban kinh gian cach 0.85 nen 40 con khong dinh thanh mot khoi.
# =====================================================================
CH_TOP = [
    "................................",
    "................................",
    "................................",
    "................................",
    "..........hhhhhh................",
    ".........hoooooooh..............",
    "........hoppppppooh.............",
    "........hopmmmmpooh.............",
    "........hopmhmhmpoh.............",
    "........hopmmmmmpoh.............",
    ".........hopmmmpoh..............",
    "..........hooooh................",
    ".......hhhoooooohhh.............",
    "......hoooooooooooooh...........",
    ".....hoopppppppppppooh..........",
    ".....hopppqqqqqqqpppoh..........",
    ".....hopppqqqqqqqpppoh..........",
    ".....hoopppppppppppooh..........",
    "......hooppppppppppoh...........",
    ".......hoopppppppooh............",
]
CH_TAIL_A = [
    "........hoopppppooh.............",
    ".........hooppppooh.............",
    "..........hoopppoh..............",
    "...........hooppoh..............",
    "............hoopoh..............",
    ".............hooh...............",
    "..............hh................",
]
CH_TAIL_B = [
    ".........hoopppppooh............",
    ".........hoopppppooh............",
    "..........hooppppoh.............",
    "..........hoopppoh..............",
    "...........hoopoh...............",
    "...........hooh.................",
    "............hh..................",
]
CH_TAIL_C = [
    ".......hoopppppooh..............",
    ".......hoopppppooh..............",
    "........hoopppooh...............",
    ".........hooppoh................",
    ".........hoopoh.................",
    "..........hooh..................",
    "..........hh....................",
]


def cohon(tail, dy=0, marks=None):
    rows = CH_TOP + tail
    rows = shift(rows, dy)
    if marks:
        rows = paint(rows, marks)
    return grid(rows)


# Mieng ha ra khi danh. Dat o dong 9-11, ngay duoi hai mat o dong 8.
CH_BITE = [(11, 9, 'h'), (12, 9, 'q'), (13, 9, 'q'), (14, 9, 'q'), (15, 9, 'h'),
           (11, 10, 'h'), (12, 10, 'h'), (13, 10, 'h'), (14, 10, 'h'), (15, 10, 'h'),
           (12, 11, 'a'), (14, 11, 'a')]


def lunge(im, dx):
    """Lao ve truoc: doi ca hinh sang ngang, dung cho khoanh khac cham."""
    out = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    out.paste(im, (dx, 0), im)
    return out

ch_idle = [cohon(CH_TAIL_A, 0), cohon(CH_TAIL_B, 0), cohon(CH_TAIL_C, 1), cohon(CH_TAIL_B, 0)]
ch_walk = [cohon(CH_TAIL_A, 0), cohon(CH_TAIL_B, 1), cohon(CH_TAIL_C, 0),
           cohon(CH_TAIL_A, 1), cohon(CH_TAIL_B, 0), cohon(CH_TAIL_C, 1)]
ch_atk = [cohon(CH_TAIL_A, 0, CH_BITE),
          lunge(cohon(CH_TAIL_C, -1, CH_BITE), 2),
          lunge(cohon(CH_TAIL_C, 0, CH_BITE), 4),
          cohon(CH_TAIL_B, 1)]
ch_hurt = [cohon(CH_TAIL_B, 1), cohon(CH_TAIL_A, 2), cohon(CH_TAIL_B, 1)]


def dissolve(im, keep):
    """Xoa dan diem anh theo mot mau co dinh — hon tan ra chu khong mo dan."""
    out = im.copy()
    px = out.load()
    for y in range(32):
        for x in range(32):
            if px[x, y][3] == 0:
                continue
            if ((x * 7 + y * 13) % 10) >= keep:
                px[x, y] = (0, 0, 0, 0)
    return out


ch_death = [cohon(CH_TAIL_B, 1), dissolve(cohon(CH_TAIL_C, 2), 7),
            dissolve(cohon(CH_TAIL_C, 4), 4), dissolve(cohon(CH_TAIL_C, 6), 2)]

# =====================================================================
# TILESET SAN DINH — san gach Bat Trang truoc dinh lang
# =====================================================================
FLOOR_A = [
    "gggggggggggggggggggggggggggggggg",
    "gttttttttttttttggttttttttttttttg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtssssrssssssstggtssssssssrssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssssssssrsstggtssssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssrsssssssstggtssssrssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssssssssssstggtssssssssrssstg",
    "gtssssssssrssstggtsssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gttttttttttttttggttttttttttttttg",
    "gggggggggggggggggggggggggggggggg",
    "gggggggggggggggggggggggggggggggg",
    "gttttttttttttttggttttttttttttttg",
    "gtsssssssssssstggtsssssrsssssstg",
    "gtsssssrssssssstggtsssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssssssssssstggtssssssssssrstg",
    "gtssssssssrssstggtsssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtssrssssssssstggtsssssssssssstg",
    "gtsssssssssssstggtssssrssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gtsssssssrsssstggtsssssssssssstg",
    "gtsssssssssssstggtsssssssssssstg",
    "gttttttttttttttggttttttttttttttg",
    "gggggggggggggggggggggggggggggggg",
]
FLOOR_B = [r.replace('r', 's') for r in FLOOR_A]
FLOOR_B = [r if i % 8 else r.replace('sss', 'sds', 1) for i, r in enumerate(FLOOR_B)]

WALL = [
    "hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh",
    "hffffffffffffffffffffffffffffffh",
    "hfeeeeeeeeeeeeeeeeeeeeeeeeeeeefh",
    "hfeddddddddddddddddddddddddddefh",
    "hfedcccccccccccccccccccccccccdefh"[:32],
    "hfedcbbbbbbbbbbbbbbbbbbbbbbcdefh",
    "hfedcbccccccccccccccccccccbcdefh",
    "hfedcbcddddddddddddddddddcbcdefh",
    "hfedcbcdeeeeeeeeeeeeeeeedcbcdefh",
    "hfedcbcdeffffffffffffffedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdefhhhhhhhhhhhhfedcbcdefh",
    "hfedcbcdeffffffffffffffedcbcdefh",
    "hfedcbcdeeeeeeeeeeeeeeeedcbcdefh",
    "hfedcbcddddddddddddddddddcbcdefh",
    "hfedcbccccccccccccccccccccbcdefh",
    "hfedcbbbbbbbbbbbbbbbbbbbbbbcdefh",
    "hfedddddddddddddddddddddddddcefh",
    "hffffffffffffffffffffffffffffffh",
    "hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh",
]

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    jobs = [
        ("Player_ThachSanh_Idle", ts_idle), ("Player_ThachSanh_Walk", ts_walk),
        ("Player_ThachSanh_Attack", ts_atk), ("Player_ThachSanh_Hurt", ts_hurt),
        ("Player_ThachSanh_Death", ts_death),
        ("Enemy_CoHon_Idle", ch_idle), ("Enemy_CoHon_Walk", ch_walk),
        ("Enemy_CoHon_Attack", ch_atk), ("Enemy_CoHon_Hurt", ch_hurt),
        ("Enemy_CoHon_Death", ch_death),
    ]
    for name, frames in jobs:
        sheet(frames, os.path.join(OUT, name + ".png"))
        print("%-28s %d khung" % (name, len(frames)))

    for name, rows in (("Tile_SanDinh_A", FLOOR_A), ("Tile_SanDinh_B", FLOOR_B),
                       ("Tile_SanDinh_Wall", WALL)):
        grid(rows).save(os.path.join(OUT, name + ".png"))
        print("%-28s tile 32x32" % name)

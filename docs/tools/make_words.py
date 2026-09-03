"""Sinh anh chu pixel tieng Viet cho giao dien LAC.

Khong dung TextMeshPro: phong vector bi lam mo khi thu ve co pixel art va pha vo
luoi diem anh. Cung khong dung mot he thong phong day du — giao dien hien chi can
ba cum tu, nen ve san thanh anh la du va re hon nhieu.

Chu hoa 5x5, o chua 5x8: hai dong tren cho dau phu, nam dong giua cho chu cai,
mot dong duoi cho dau nang.
"""
import os
from PIL import Image

OUT = "Assets/_LAC/Art/Sprites"

# Chu cai 5 rong x 5 cao
GLYPH = {
    'A': ["01110", "10001", "11111", "10001", "10001"],
    'B': ["11110", "10001", "11110", "10001", "11110"],
    'C': ["01111", "10000", "10000", "10000", "01111"],
    'D': ["11110", "10001", "10001", "10001", "11110"],
    'E': ["11111", "10000", "11110", "10000", "11111"],
    'G': ["01111", "10000", "10011", "10001", "01111"],
    'H': ["10001", "10001", "11111", "10001", "10001"],
    'I': ["11111", "00100", "00100", "00100", "11111"],
    'L': ["10000", "10000", "10000", "10000", "11111"],
    'N': ["10001", "11001", "10101", "10011", "10001"],
    'O': ["01110", "10001", "10001", "10001", "01110"],
    'P': ["11110", "10001", "11110", "10000", "10000"],
    'R': ["11110", "10001", "11110", "10010", "10001"],
    'T': ["11111", "00100", "00100", "00100", "00100"],
    ' ': ["00000", "00000", "00000", "00000", "00000"],
}

# Dau phu dat o hai dong tren (dong 0 va 1 cua o)
MARK = {
    'acute':  {(1, 3), (0, 2)},                 # sac
    'grave':  {(1, 1), (0, 2)},                 # huyen
    'hook':   {(0, 2), (1, 2), (1, 3)},         # hoi
    'circ':   {(1, 1), (0, 2), (1, 3)},         # dau mu a^ e^ o^
    'breve':  {(0, 1), (1, 2), (0, 3)},         # dau trang a(
    'dotlow': 'below',                          # nang — ve o dong duoi cung
}


def glyph(base, marks=()):
    """Tra ve luoi 5x8 cua mot chu, da ghep dau."""
    cell = [[0] * 5 for _ in range(8)]
    for y, row in enumerate(GLYPH[base]):
        for x, c in enumerate(row):
            if c == '1':
                cell[y + 2][x] = 1
    for m in marks:
        if m == 'dotlow':
            cell[7][2] = 1
            continue
        for (my, mx) in MARK[m]:
            cell[my][mx] = 1
    return cell


# Chu co dau, mo ta bang chu goc + danh sach dau
SPECIAL = {
    'Ế': ('E', ('circ', 'acute')),
    'Ể': ('E', ('circ', 'hook')),
    'Ắ': ('A', ('breve', 'acute')),
    'Ấ': ('A', ('circ', 'acute')),
    'Ạ': ('A', ('dotlow',)),
    'Ơ': ('O', ()),      # sung ve rieng ben duoi
    'Ợ': ('O', ('dotlow',)),
    'Đ': ('D', ()),      # gach ngang ve rieng ben duoi
    'Ị': ('I', ('dotlow',)),
}


def cell_for(ch):
    if ch in SPECIAL:
        base, marks = SPECIAL[ch]
        c = glyph(base, marks)
        if ch in ('Ơ', 'Ợ'):
            c[2][4] = 1          # sung nho o goc tren phai
            c[1][4] = 1
        if ch == 'Đ':
            c[4][0] = 1          # gach ngang xuyen qua chu D
        return c
    return glyph(ch)


def render(text, scale=1, pad=1):
    cells = [cell_for(ch) for ch in text]
    w = len(cells) * (5 + pad) - pad
    im = Image.new("RGBA", (w, 8), (0, 0, 0, 0))
    px = im.load()
    for i, c in enumerate(cells):
        ox = i * (5 + pad)
        for y in range(8):
            for x in range(5):
                if c[y][x]:
                    px[ox + x, y] = (244, 234, 218, 255)   # DiepSang
    if scale > 1:
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
    return im


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    words = {
        "UI_ChienThang": "CHIẾN THẮNG",
        "UI_ThatBai": "THẤT BẠI",
        "UI_ChoiLai": "NHẤN R ĐỂ CHƠI LẠI",
        "UI_Dot": "ĐỢT",
    }
    for name, text in words.items():
        im = render(text)
        im.save(os.path.join(OUT, name + ".png"))
        print("%-16s %dx%d" % (name, im.width, im.height))

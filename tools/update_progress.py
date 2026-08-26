#!/usr/bin/env python3
"""Đọc docs/TASKS.md, đếm ô đã tick, rồi ghi bảng tiến độ vào README.md.

Chạy tay:   python tools/update_progress.py
Tự động:    .github/workflows/progress.yml chạy mỗi khi docs/TASKS.md đổi.

Nguồn sự thật vẫn là docs/TASKS.md — file này chỉ đọc, không bao giờ sửa nó.
"""

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TASKS = ROOT / "docs" / "TASKS.md"
README = ROOT / "README.md"

START = "<!-- PROGRESS:START -->"
END = "<!-- PROGRESS:END -->"

BOX = re.compile(r"^\s*-\s\[( |x|X)\]")
# Chỉ tính các mục "## 📅 TUẦN ..." và "## 🚦 ...", bỏ qua mục Đã hoãn / Ngoài phạm vi
SECTION = re.compile(r"^##\s+(.*)$")
SKIP = ("Đã hoãn", "Ngoài phạm vi", "Cách dùng")


def bar(done: int, total: int, width: int = 20) -> str:
    if total == 0:
        return "░" * width
    filled = round(width * done / total)
    return "█" * filled + "░" * (width - filled)


def main() -> int:
    if not TASKS.exists():
        print(f"khong tim thay {TASKS}", file=sys.stderr)
        return 1

    sections: list[tuple[str, int, int]] = []
    current = None
    done = total = 0

    for line in TASKS.read_text(encoding="utf-8").splitlines():
        m = SECTION.match(line)
        if m:
            if current is not None:
                sections.append((current, done, total))
            current, done, total = m.group(1).strip(), 0, 0
            continue
        b = BOX.match(line)
        if b and current is not None:
            total += 1
            if b.group(1).lower() == "x":
                done += 1
    if current is not None:
        sections.append((current, done, total))

    rows = [s for s in sections if s[2] > 0 and not any(k in s[0] for k in SKIP)]
    all_done = sum(r[1] for r in rows)
    all_total = sum(r[2] for r in rows)
    pct = round(100 * all_done / all_total) if all_total else 0

    out = [
        START,
        "",
        f"### Tiến độ — {all_done}/{all_total} task ({pct}%)",
        "",
        f"`{bar(all_done, all_total, 28)}` **{pct}%**",
        "",
        "| Giai đoạn | Xong | Tiến độ |",
        "|---|---|---|",
    ]
    for name, d, t in rows:
        p = round(100 * d / t) if t else 0
        out.append(f"| {name} | {d}/{t} | `{bar(d, t)}` {p}% |")
    out += ["", f"*Tự cập nhật từ [docs/TASKS.md](docs/TASKS.md).*", "", END]
    block = "\n".join(out)

    readme = README.read_text(encoding="utf-8")
    if START in readme and END in readme:
        new = re.sub(
            re.escape(START) + r".*?" + re.escape(END), block, readme, flags=re.S
        )
    else:
        new = readme.rstrip() + "\n\n---\n\n" + block + "\n"

    if new != readme:
        README.write_text(new, encoding="utf-8", newline="\n")
        print(f"README.md da cap nhat: {all_done}/{all_total} ({pct}%)")
    else:
        print(f"khong doi: {all_done}/{all_total} ({pct}%)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

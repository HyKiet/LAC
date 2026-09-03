#!/bin/sh
# Đường xử lý tay: chạy khi merge driver bó tay và bạn gõ `git mergetool`.
#
# Cùng lý do phải có lớp bọc như unityyamlmerge.sh — xem chú thích trong file đó.
# Khác biệt duy nhất: git mergetool đưa file kết quả thành tham số riêng thay vì
# ghi đè lên bản của mình.
#
# git gọi với: $BASE $REMOTE $LOCAL $MERGED

BASE="$1"
REMOTE="$2"
LOCAL="$3"
MERGED="$4"

EXE=$(git config --get unityyamlmerge.path)
if [ -z "$EXE" ] || [ ! -f "$EXE" ]; then
    echo "unityyamlmerge: chua cau hinh duong dan cong cu. Chay tools/setup-git.ps1 mot lan." >&2
    exit 1
fi

EXT=".unity"
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

cp "$BASE"   "$TMP/base$EXT"
cp "$REMOTE" "$TMP/theirs$EXT"
cp "$LOCAL"  "$TMP/ours$EXT"

win() {
    if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi
}

"$EXE" merge -p -h --fallback none \
    "$(win "$TMP/base$EXT")" \
    "$(win "$TMP/theirs$EXT")" \
    "$(win "$TMP/ours$EXT")" \
    "$(win "$TMP/out$EXT")"
STATUS=$?

if [ $STATUS -eq 0 ] && [ -f "$TMP/out$EXT" ]; then
    cp "$TMP/out$EXT" "$MERGED"
    exit 0
fi

echo "unityyamlmerge: van con xung dot. Lay ban tren remote roi lam lai trong Unity:" >&2
echo "  git checkout --theirs <duong-dan-file>" >&2
exit 1

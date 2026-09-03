#!/bin/sh
# Lớp bọc để git gọi được UnityYAMLMerge làm merge driver tự động.
#
# Ba lý do phải có lớp bọc thay vì gọi thẳng công cụ:
#
# 1. Git đưa cho merge driver các file tạm KHÔNG có phần mở rộng
#    (merge_file_aB3xY), trong khi UnityYAMLMerge nhìn đuôi file để quyết định
#    cách merge. Gọi thẳng sẽ báo "Don't know how to merge ... files".
#
# 2. Công cụ chỉ nhận diện đuôi scene và prefab, còn .asset thì từ chối. Mọi
#    asset YAML của Unity đều cùng một định dạng và quy tắc merge được khớp theo
#    loại đối tượng chứ không theo đuôi file, nên đặt .unity cho tất cả là an
#    toàn và merge được cả .asset lẫn .mat.
#
# 3. UnityYAMLMerge là chương trình Windows, không hiểu đường dẫn kiểu POSIX mà
#    Git Bash sinh ra (/tmp/...). Phải đổi sang dạng Windows bằng cygpath.
#
# git gọi với: %O %B %A %P
#   $1 tổ tiên chung   $2 bản của họ   $3 bản của mình (cũng là file kết quả)
#   $4 đường dẫn thật, chỉ để ghi vào thông báo lỗi
#
# Mã thoát 0 nghĩa là đã merge xong; khác 0 thì git đánh dấu file là xung đột
# để người dùng xử lý tay.

BASE="$1"
THEIRS="$2"
OURS="$3"
REALPATH="$4"

EXE=$(git config --get unityyamlmerge.path)
if [ -z "$EXE" ] || [ ! -f "$EXE" ]; then
    echo "unityyamlmerge: chua cau hinh duong dan cong cu. Chay tools/setup-git.ps1 mot lan." >&2
    exit 1
fi

EXT=".unity"

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

cp "$BASE"   "$TMP/base$EXT"
cp "$THEIRS" "$TMP/theirs$EXT"
cp "$OURS"   "$TMP/ours$EXT"

win() {
    if command -v cygpath >/dev/null 2>&1; then
        cygpath -w "$1"
    else
        printf '%s' "$1"
    fi
}

"$EXE" merge -p -h --fallback none \
    "$(win "$TMP/base$EXT")" \
    "$(win "$TMP/theirs$EXT")" \
    "$(win "$TMP/ours$EXT")" \
    "$(win "$TMP/out$EXT")" >/dev/null 2>&1
STATUS=$?

if [ $STATUS -eq 0 ] && [ -f "$TMP/out$EXT" ]; then
    cp "$TMP/out$EXT" "$OURS"
else
    echo "unityyamlmerge: khong tu merge duoc $REALPATH - xu ly tay bang 'git mergetool'." >&2
    STATUS=1
fi

exit $STATUS

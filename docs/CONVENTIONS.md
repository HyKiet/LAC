# CONVENTIONS — Quy ước làm việc

> Đọc hết một lần trước commit đầu tiên. Sau đó chỉ tra khi cần.

---

## 1. Luật scene — quan trọng nhất, đọc trước tiên

File `.unity` và `.prefab` là **nguồn conflict tệ nhất** trong Unity. Git không merge được chúng một cách tự nhiên. Ba quy tắc:

### 🔴 Mỗi lúc chỉ MỘT người được sửa scene

Trước khi mở và sửa `Arena.unity`, **nhắn vào nhóm chat: "tôi đang giữ Arena scene"**. Sửa xong, commit + push, rồi nhắn "đã trả Arena". Nghe thủ công nhưng đây là cách các studio nhỏ thật sự làm.

### 🔴 Làm mọi thứ trong prefab, không trong scene

Scene chỉ nên chứa: điểm spawn, camera, tilemap, và các manager. Mọi thứ khác là prefab. Hai người sửa hai prefab khác nhau thì không bao giờ conflict.

### 🔴 Cài UnityYAMLMerge — một lần trên mỗi máy

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver \
  '"C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

Không có bước này, `.gitattributes` không có tác dụng và conflict scene sẽ phải sửa tay.

---

## 2. Nhánh

```
main        luôn chạy được. Không push thẳng.
dev         nhánh tích hợp. PR trỏ về đây.
feat/T-102-dash-iframe    nhánh làm việc, đặt tên theo mã task
fix/T-205-boss-desync
```

- Một task = một nhánh = một PR.
- Nhánh sống **tối đa 2 ngày**. Lâu hơn là sắp conflict.
- **Kéo `dev` về mỗi sáng** trước khi làm: `git pull origin dev`.

---

## 3. Commit

```
<loại>(<mã task>): <mô tả ngắn, tiếng Việt, không viết hoa đầu>
```

Loại: `feat` · `fix` · `refactor` · `art` · `docs` · `chore` · `balance`

```
feat(T-032): dash có i-frame và ghost trail
fix(T-314): trống đồng bị kích 2 lần khi 2 người dash cùng lúc
art(T-050): sprite thạch sanh idle + đi
balance(T-510): giảm sát thương gốc sáo trúc 12 -> 9
docs(T-000): thêm luật netcode
```

**Mỗi commit hoàn thành một task phải kèm sửa đổi ở `TASKS.md` và `PROGRESS.md`.** Không có hai file đó thì PR bị trả về.

---

## 4. Pull Request

Tiêu đề = dòng commit. Nội dung dùng mẫu ở [.github/pull_request_template.md](../.github/pull_request_template.md).

Cần **1 người duyệt**. Người duyệt kiểm tra ba thứ:
1. Có vi phạm **Ba Luật Sắt** trong [CLAUDE.md](../CLAUDE.md) không?
2. `TASKS.md` đã tick chưa, `PROGRESS.md` đã ghi chưa?
3. Console Unity có sạch không?

---

## 5. Quy ước code

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Class, method, property | PascalCase | `PlayerDash`, `TryActivate()` |
| Field private | `_camelCase` | `_cooldownTimer` |
| Field serialize | `[SerializeField] private` + `_camelCase` | `[SerializeField] private float _dashSpeed;` |
| Hằng số | PascalCase | `MaxEnemies` |
| Interface | `I` + PascalCase | `ITargetable` |
| ScriptableObject | tên + `Data` | `CardData`, `EnemyData` |
| Namespace | theo thư mục | `LAC.Combat`, `LAC.Net` |
| File | trùng tên class | `PlayerDash.cs` |

**Bắt buộc:**
- `[SerializeField] private`, **không** dùng `public` field.
- Không `GameObject.Find` / `FindObjectOfType` trong gameplay loop.
- Không `Instantiate` / `Destroy` trong gameplay loop — dùng `ObjectPool`.
- Không `Debug.Log` sót lại trong code đã merge.
- Không dùng `UnityEngine.Random` trong gameplay — dùng `RunRandom`. **Luật Sắt số 3.**

---

## 6. Làm việc với AI (vibe coding)

Cả nhóm code chủ yếu bằng AI. Ba quy tắc để AI không phá kiến trúc:

### 6.1 Luôn bắt AI đọc tài liệu trước

Câu mở đầu mọi phiên làm việc:

> *"Đọc CLAUDE.md, docs/ARCHITECTURE.md và docs/NETCODE.md trước. Sau đó làm task T-XXX trong docs/TASKS.md."*

### 6.2 Đưa kiến trúc cho AI, đừng để AI tự nghĩ ra

Sai: *"thêm multiplayer vào game này"* → AI sẽ gắn `NetworkIdentity` lên mọi thứ, băng thông vỡ, desync khắp nơi.

Đúng: *"theo bảng đồng bộ ở NETCODE.md mục 2, cài `DongSonDrum` với cooldown dùng chung do host giữ. Client gọi `CmdTryActivate`, host kiểm tra rồi phát `RpcPlayShockwave`."*

### 6.3 Bắt AI tự cập nhật tài liệu

Kết thúc mọi task:

> *"Cập nhật docs/TASKS.md và docs/PROGRESS.md theo mẫu, rồi commit."*

### 6.4 Ba câu hỏi trước khi merge code AI viết

1. Nó có tạo hệ thống trùng với thứ đã có trong `PROGRESS.md` không?
2. Nó có vi phạm Luật Sắt nào không?
3. Nó có `Instantiate` trong `Update()` không?

---

## 7. Cấm commit

- File trong `Library/`, `Temp/`, `Logs/`, `UserSettings/` — đã có trong `.gitignore`
- File `.csproj`, `.sln`, `.slnx` — Unity tự sinh lại
- Ảnh/âm thanh **không qua LFS** — kiểm tra bằng `git lfs status` trước khi push
- Code có `Debug.Log` rác hoặc `// TODO` không kèm mã task

---

## 8. Nếu lỡ conflict scene

```bash
# 1. Đừng hoảng, đừng tự sửa file .unity bằng tay
git checkout --theirs Assets/_LAC/Scenes/Arena.unity   # lấy bản trên server
# 2. Mở Unity, làm lại thay đổi của mình (thường chỉ vài phút)
# 3. Rút kinh nghiệm: lần sau nhắn nhóm trước khi giữ scene
```

Sửa tay file YAML của Unity gần như luôn làm hỏng scene. Làm lại nhanh hơn sửa.

---

## 9. Tick task cho nhanh — cài 4 extension

Mở dự án bằng VS Code, nó sẽ hiện thông báo *"This workspace has extension recommendations"* → bấm **Install All**. Danh sách nằm ở [.vscode/extensions.json](../.vscode/extensions.json), cấu hình đã có sẵn ở [.vscode/settings.json](../.vscode/settings.json).

| Extension | Dùng để làm gì |
|---|---|
| **Markdown All in One** | Đặt con trỏ vào dòng task → **`Alt+C`** để tick / bỏ tick |
| **Markdown Preview Enhanced** | **`Ctrl+K V`** mở bản xem trước → **bấm chuột thẳng vào ô vuông**, nó tự ghi ngược vào file |
| **Todo Tree** | Cây task ở thanh bên trái: ô cam = chưa xong, ô xanh = xong, có đếm số |
| **Markdown Mermaid** | Xem sơ đồ kiến trúc trong [ARCHITECTURE.md](ARCHITECTURE.md) |

**Ba cách tick, chọn cái bạn thích:**

1. Gõ tay `- [ ]` → `- [x]`
2. `Alt+C` trong editor
3. `Ctrl+K V` rồi bấm chuột vào ô vuông trong preview

Cả ba đều ghi vào cùng file `docs/TASKS.md` — nên **AI vẫn đọc được y nguyên**. Không có cơ sở dữ liệu ẩn nào cả.

### Thanh tiến độ tự chạy

Mỗi lần `docs/TASKS.md` được push, GitHub Action [progress.yml](../.github/workflows/progress.yml) tính lại và ghi bảng tiến độ vào [README.md](../README.md). Muốn xem trước ở máy mình:

```bash
python tools/update_progress.py
```

### Xem trên GitHub

GitHub tự render `- [ ]` thành ô vuông kèm thanh tiến độ. Vào thẳng [docs/TASKS.md](TASKS.md) trên web là thấy — không cần cài gì.

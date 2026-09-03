# Quy trình làm việc nhóm — LẠC

Ba người cùng sửa một dự án Unity thì làm sao không dẫm chân nhau. Đọc trước khi commit lần đầu.

Việc cần làm: [TASKS.md](TASKS.md) · Ràng buộc kiến trúc: [CLAUDE.md](../CLAUDE.md)

---

## 1. Cài đặt — làm một lần sau khi clone

```powershell
powershell -ExecutionPolicy Bypass -File tools\setup-git.ps1
```

Script tự tìm Unity trên máy bạn và cài công cụ merge cho file scene. **Không chạy thì hai người cùng sửa một scene sẽ tạo ra xung đột Git không gỡ được**, và cách duy nhất là một người vứt bỏ công việc của mình.

Cần cài sẵn: Unity **6000.5.6f1** (đúng bản này) và [Git LFS](https://git-lfs.github.com).

---

## 2. Ba nguyên tắc

**Mỗi thư mục có đúng một chủ.** Xem bảng ở [TASKS.md](TASKS.md#phân-công). Cần sửa file ngoài thư mục của mình thì nhắn cho chủ thư mục trước.

**Mỗi scene chỉ một người mở tại một thời điểm.** Nhắn nhóm trước khi mở, pull trước, xong thì commit và push ngay, rồi nhắn lại.

**Cái gì mới thì làm thành prefab, đừng đặt thẳng vào scene.** Giao diện, hiệu ứng, menu — dựng prefab rồi sinh bằng mã lúc chạy. Nhờ vậy @Kang làm xong hệ thống thẻ mà không cần mở `Arena.unity` lần nào.

| Scene | Chủ |
|---|---|
| `Scenes/Boot.unity` — menu, cài đặt, chọn nhân vật | **@Hung** |
| `Scenes/Arena.unity` — đấu trường, mạng, HUD | **@Kiet** |

---

## 3. Nhánh và commit

```
main   Ổn định, chỉ merge từ dev tại mỗi cổng nghiệm thu
dev    Nhánh chung, mọi người merge vào đây
feat/T-21-card-data    Nhánh việc riêng, sống tối đa 2 ngày
```

Một ngày làm việc:

```bash
git pull origin dev                    # sáng: lấy việc của người khác
git checkout -b feat/T-21-card-data
# ... làm việc ...
git add -A && git commit
git checkout dev && git pull origin dev
git merge feat/T-21-card-data
git push origin dev                    # chiều: đẩy lên trước khi nghỉ
```

**Đừng giữ thay đổi trên máy quá một ngày** — để càng lâu càng khó merge.

Commit theo mẫu `<loại>(<mã>): <mô tả>`:

```
feat(T-21): CardData và cơ chế áp hiệu ứng lên chỉ số
fix(T-22): thẻ không đóng khi hết 10 giây
art(T-33): sprite Gióng
```

Loại: `feat` · `fix` · `art` · `balance` · `docs` · `chore`. Phần thân commit ghi **lý do** của những quyết định không hiển nhiên — đó là nơi lưu kiến thức, không phải TASKS.md.

---

## 4. Chỗ nối giữa ba mảng

**@Kang → lõi.** Hết đợt quái, `RunManager` chuyển sang `CardSelection` và phát sự kiện `WaveCleared`. Hệ thống thẻ nghe sự kiện đó, mở giao diện, cả hai người chọn xong thì gọi:

```csharp
RunManager.Instance.ReportCardSelectionComplete();   // chỉ host gọi
```

Hàm này đã có sẵn và đang chạy. Hiện `WaveManager` gọi tạm sau 1.5 giây qua cờ `_autoAdvanceCardSelection`; xong T-23 thì tắt cờ đó — đó là toàn bộ việc bàn giao.

**@Hung → lõi.** Menu vào game bằng Mirror:

```csharp
NetworkManager.singleton.StartHost();     // chơi đơn VÀ tạo phòng — cùng một lệnh
NetworkManager.singleton.StartClient();   // vào phòng người khác
```

Chuyển scene để Mirror lo (`ServerChangeScene`), không gọi `SceneManager.LoadScene` khi mạng đang chạy.

---

## 5. Ba cái bẫy

**Thẻ sửa thẳng vào `CharacterData`.** Nó là ScriptableObject — sửa lúc chạy sẽ **ghi đè vĩnh viễn vào file asset**, và ván sau bắt đầu với chỉ số đã cộng dồn. Cần một lớp chỉ số của ván, sao chép từ `CharacterData` rồi cộng lên bản sao.

**Viết nhánh riêng cho chơi đơn.** Chơi đơn cũng phải `StartHost` — CLAUDE.md mục 3.1. Chơi đơn là host một client, chơi đôi là host hai client, **một luồng mã duy nhất**.

**Dùng `UnityEngine.Random` trong gameplay.** Mọi thứ ảnh hưởng đến ván — bể thẻ, thứ tự thẻ, quái rơi gì — phải qua `RunRandom`. Gọi sai một lần là hai máy thấy hai kết quả khác nhau. Hiệu ứng hình ảnh và âm thanh thuần trang trí thì được miễn.

---

## 6. Trước khi push — kiểm 4 điều

1. Vào Play mode, chơi thử một ván, **Console không có lỗi đỏ**
2. Đã đánh dấu `[x]` và viết dòng `>` trong TASKS.md, **cùng commit với mã**
3. Không sửa file ngoài thư mục mình sở hữu, hoặc đã hỏi chủ thư mục
4. `git pull origin dev` rồi chạy lại lần nữa

---

## 7. Khi hỏng

| Triệu chứng | Cách xử lý |
|---|---|
| Game không chạy sau khi pull | Unity → `Assets > Reimport All`. Vẫn lỗi thì xoá thư mục `Library/` rồi mở lại |
| Xung đột scene mà Git không tự gỡ | `git mergetool`. Vẫn không xong thì `git checkout --theirs <file>` rồi làm lại trong Unity |
| Scene trống trơn, mất object | Merge sai. Lấy bản remote, làm lại thay đổi. **Đừng sửa tay file YAML** |
| Chơi 1 người bình thường, 2 người thì lỗi | Đã sửa trạng thái ở phía client thay vì để host quyết. Đọc CLAUDE.md mục 3.2 |
| Lỗi vô nghĩa, truy mãi không ra | Có sửa file `.cs` trong lúc Play mode đang chạy không? **Luôn thoát Play mode trước khi sửa mã** |

---

## 8. Dùng công cụ AI

**Chỉ định tài liệu trước.** *"Đọc CLAUDE.md và docs/TASKS.md, sau đó làm T-21."*

**Đưa kiến trúc, đừng để nó tự chọn.** Yêu cầu chung chung kiểu *"thêm multiplayer"* sẽ khiến nó gắn `NetworkIdentity` lên từng viên đạn và làm sập băng thông.

**Kiểm ba câu trước khi merge:** chức năng này đã có trong TASKS.md chưa · có vi phạm mục 3 của CLAUDE.md không · có `Instantiate` hay `FindObjectOfType` trong `Update()` không.

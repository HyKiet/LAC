# Quy trình làm việc nhóm — LẠC

Tài liệu này trả lời một câu hỏi: **ba người cùng sửa một dự án Unity thì làm sao để không dẫm chân nhau.**

Đọc trước khi commit lần đầu. Ràng buộc kiến trúc nằm ở [CLAUDE.md](../CLAUDE.md), danh sách công việc ở [TASKS.md](TASKS.md).

---

## 1. Ba nguyên tắc, học thuộc

**Một — mỗi thư mục có đúng một chủ.** Bảng sở hữu ở [TASKS.md](TASKS.md#phân-công). Muốn sửa file ngoài thư mục của mình thì nhắn cho chủ thư mục trước, không sửa lặng lẽ.

**Hai — mỗi scene chỉ một người mở tại một thời điểm.** File `.unity` và `.prefab` là YAML, Git **không merge được**. Hai người cùng sửa một scene là mất việc của một trong hai.

**Ba — mọi thứ mới đều là prefab, không phải object trong scene.** Giao diện thẻ, menu, hiệu ứng: dựng thành prefab rồi sinh lúc chạy. Nhờ vậy `@Kang` làm giao diện thẻ mà **không bao giờ phải mở `Arena.unity`**.

---

## 2. Chia scene để không đụng nhau

| Scene | Chủ | Nội dung |
|---|---|---|
| `Scenes/Boot.unity` | **@Hung** | Menu chính, cài đặt, chọn nhân vật. Là scene khởi động. |
| `Scenes/Arena.unity` | **@Kiet** | Đấu trường, `NetworkManagerLAC`, `RunManager`, HUD |

`@Kang` **không sở hữu scene nào** — toàn bộ giao diện thẻ là prefab, một dòng mã sinh ra khi cần. Đây không phải thiệt thòi mà là cách né hoàn toàn nguồn xung đột lớn nhất.

Nếu bắt buộc phải mở scene của người khác:

1. Nhắn nhóm: *"mình mở Arena.unity 20 phút"*
2. `git pull origin dev` trước khi mở
3. Sửa xong, save, commit, push **ngay**
4. Nhắn nhóm: *"xong Arena.unity"*

Nghe thủ công nhưng với ba người thì đây là cách rẻ nhất và chắc nhất.

## 3. Cài UnityYAMLMerge — làm một lần, bắt buộc

Chưa cài thì chỉ cần một lần xung đột scene là mất buổi. Mỗi máy chạy một lần:

```bash
git config --global merge.tool unityyamlmerge
git config --global mergetool.unityyamlmerge.trustExitCode false
git config --global mergetool.unityyamlmerge.cmd \
  '"D:/Unity/Hub/Editor/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```

Sửa đường dẫn theo máy mình. Khi xung đột scene: `git mergetool`.

**Nếu vẫn rối:** đừng sửa tay file YAML. Lấy bản trên remote rồi làm lại thay đổi trong Unity — nhanh hơn nhiều.

```bash
git checkout --theirs Assets/_LAC/Scenes/Arena.unity
```

---

## 4. Nhánh và commit

```
main   Ổn định. Chỉ merge từ dev tại mỗi cổng nghiệm thu.
dev    Nhánh chung. Mọi người merge vào đây.
feat/T-21-card-data    Nhánh việc của từng người, sống tối đa 2 ngày.
```

**Một ngày làm việc:**

```bash
git checkout dev && git pull origin dev     # sáng: lấy việc của người khác
git checkout -b feat/T-21-card-data         # tạo nhánh cho hạng mục đang làm
# ... làm việc ...
git add -A && git commit                    # commit theo định dạng bên dưới
git checkout dev && git pull origin dev
git merge feat/T-21-card-data
git push origin dev                         # chiều: đẩy lên trước khi nghỉ
```

**Không giữ thay đổi trên máy quá một ngày.** Càng để lâu càng khó merge.

**Định dạng commit:** `<loại>(<mã>): <mô tả ngắn>`

```
feat(T-21): CardData và cơ chế áp hiệu ứng lên chỉ số
fix(T-22): thẻ không đóng khi hết 10 giây
art(T-33): sprite Gióng
docs(T-24): danh sách 32 thẻ nền
```

Loại: `feat` · `fix` · `art` · `balance` · `docs` · `chore`

Phần thân commit ghi **lý do** của những quyết định không hiển nhiên. Đây là nơi lưu kiến thức, không phải TASKS.md.

---

## 5. Ba chỗ nối giữa các mảng

Đây là nơi công việc của ba người gặp nhau. Chốt trước để không phải sửa lại.

### @Kang → vòng lặp lõi

Đợt quái kết thúc → `RunManager` chuyển sang trạng thái `CardSelection` và phát sự kiện `WaveCleared`. Hệ thống thẻ nghe sự kiện đó, mở giao diện, và khi cả hai người chơi đã chọn xong thì gọi:

```csharp
RunManager.Instance.ReportCardSelectionComplete();   // chỉ host được gọi
```

**Hàm này đã tồn tại và đang chạy.** Hiện `WaveManager` gọi tạm sau 1.5 giây qua cờ `_autoAdvanceCardSelection`. Khi T-23 xong, tắt cờ đó — đó là toàn bộ việc bàn giao.

> **Bẫy phải tránh:** đừng cho thẻ sửa thẳng vào `CharacterData`. Nó là ScriptableObject — sửa lúc chạy sẽ **ghi đè vĩnh viễn vào file asset** trong Editor, và ván sau sẽ bắt đầu với chỉ số đã bị cộng dồn. Cần một lớp chỉ số của ván, khởi tạo bằng cách sao chép từ `CharacterData`.

### @Hung → vòng lặp lõi

Menu vào game bằng cách gọi Mirror:

```csharp
NetworkManager.singleton.StartHost();     // chơi đơn VÀ tạo phòng — cùng một lệnh
NetworkManager.singleton.StartClient();   // tham gia phòng người khác
```

> **Bẫy phải tránh:** chơi đơn cũng phải `StartHost`. Không được viết nhánh riêng kiểu `if (chơiĐơn)` — CLAUDE.md mục 3.1. Chơi đơn là host với một client, chơi đôi là host với hai client, **một luồng mã duy nhất**.

Chuyển scene do Mirror lo (`NetworkManager.ServerChangeScene`), không dùng `SceneManager.LoadScene` trực tiếp khi mạng đang chạy.

### Ai cũng phải biết

**Mọi thứ có ảnh hưởng đến gameplay đều phải qua `RunRandom`, không dùng `UnityEngine.Random`.** Bể thẻ, thứ tự thẻ, quái rơi gì — tất cả. Một lần gọi sai là hai máy thấy hai kết quả khác nhau. Hiệu ứng hình ảnh và âm thanh thuần trang trí thì được miễn.

**Chỉ số và nội dung game nằm trong ScriptableObject ở `Data/`, không hard-code trong C#.**

---

## 6. Trước khi push — kiểm 4 điều

1. Vào Play mode, chơi thử một ván, **Console không có lỗi đỏ**
2. Đã đánh dấu `[x]` và viết dòng `>` trong TASKS.md, **cùng commit với mã**
3. Không sửa file ngoài thư mục mình sở hữu (hoặc đã hỏi)
4. `git pull origin dev` rồi chạy lại một lần nữa — để chắc việc của người khác không làm hỏng việc của mình

## 7. Khi có gì đó hỏng

**Game không chạy sau khi pull** → Unity → `Assets > Reimport All`. Vẫn lỗi thì xoá thư mục `Library/` rồi mở lại (Unity tự dựng lại, mất vài phút).

**Scene trống trơn hoặc mất object** → gần như chắc chắn là xung đột YAML merge sai. Lấy lại bản remote, làm lại thay đổi.

**Lỗi chỉ xảy ra khi chơi 2 người, chơi 1 người thì bình thường** → gần như chắc chắn là đã sửa trạng thái ở phía client thay vì để host quyết. Đọc lại bảng đồng bộ ở CLAUDE.md mục 3.2.

**Sửa mã trong lúc Play mode đang chạy** → đừng. Unity sẽ nạp lại domain giữa chừng, huỷ `NetworkManager` và xoá trạng thái tĩnh, để lại các lỗi vô nghĩa mà truy mãi không ra. **Luôn thoát Play mode trước khi sửa `.cs`.**

---

## 8. Dùng công cụ AI

Nhóm làm chủ yếu bằng AI-assisted. Ba quy định để kiến trúc không bị phá:

**Chỉ định tài liệu trước khi giao việc.** *"Đọc CLAUDE.md và docs/TASKS.md. Sau đó thực hiện T-21."*

**Đưa kiến trúc, đừng để công cụ tự chọn.** Yêu cầu chung chung kiểu *"thêm multiplayer"* sẽ khiến nó gắn `NetworkIdentity` lên từng viên đạn và làm sập băng thông. Nói rõ: *"theo bảng đồng bộ mục 3.2, đồng bộ định danh thẻ chứ không đồng bộ hiệu ứng."*

**Kiểm ba câu trước khi merge:**
1. Chức năng này đã có trong TASKS.md chưa?
2. Có vi phạm ràng buộc nào ở mục 3 của CLAUDE.md không?
3. Có `Instantiate` hoặc `FindObjectOfType` trong `Update()` không?

# LẠC — Children of the Dragon

> **AI ĐỌC FILE NÀY TRƯỚC TIÊN.** Đây là bản đồ của toàn bộ dự án.
> Đọc xong file này, đọc tiếp theo đúng thứ tự ở mục 7.
> Không đoán. Không tự tạo hệ thống mới khi chưa đọc [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## 1. Game này là gì

**Arena survival roguelite 2D top-down, pixel art, nền thần thoại Việt Nam.**

| | |
|---|---|
| Thể loại | Arena Survival Roguelite (dòng Vampire Survivors / Brotato) |
| Nền tảng | PC — Steam |
| Người chơi | 1–2, **co-op online** (bắt buộc, yêu cầu của giảng viên) |
| Thời lượng ván | 15 phút · 16 đợt quái |
| Engine | **Unity 6000.5.6f1** · URP 2D · C# |
| Mạng | Mirror + Steamworks.NET (FizzySteamworks), **host-authoritative** |
| Giá dự kiến | **$4.99** (đã điều chỉnh từ $6.99) |

**Vòng lặp cốt lõi:** đợt quái (30–50s) → chọn 1 trong 3 thẻ (10s) → lặp ×15 → trùm Chằn Tinh ở đợt 16.

**Chỉ có hai thao tác:** di chuyển + lướt (dash). Vũ khí **tự động khai hỏa**. Người chơi điều khiển vị trí và thời điểm, không điều khiển việc bắn.

**Ba nhân vật, vũ khí cố định không thay thế được:**

| Nhân vật | Vũ khí | HP | Tốc độ | Tầm | Chu kỳ | Đặc tính |
|---|---|---|---|---|---|---|
| Thạch Sanh | Đàn bầu | 6 | 5 | 4 (vòng tròn) | 0.9s | Cân bằng |
| Gióng | Roi sắt | 10 | 3 | 2.5 (hình cung) | 0.6s | Trâu bò, cận chiến |
| Tấm | Sáo trúc | 4 | 8 | 7 (tia) | 0.12s | Mỏng manh, sát thương cao |

---

## 2. Ba cơ chế bản sắc — KHÔNG ĐƯỢC BỎ

Đây là thứ phân biệt LẠC với 200 game survivors khác trên Steam. Nếu được giao việc động vào các hệ thống này, đọc mục tương ứng trong [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) trước.

### 2.1 Money shot — sóng âm Đông Sơn

Mọi vũ khí đều là **nhạc cụ**. Đòn đánh hiển thị dưới dạng **vòng tròn đồng tâm lan ra** mang hoa văn trống đồng Đông Sơn. Càng về cuối ván, màn hình càng đầy sóng âm của chính người chơi. Đây là hình ảnh dùng cho trailer và TikTok.

> **Ràng buộc bắt buộc:** giữ riêng một màu trong bảng 24 màu Đông Hồ **chỉ dành cho đòn tấn công của kẻ địch**. VFX của người chơi **không bao giờ** dùng màu đó. Sóng âm vẽ alpha thấp + additive; đòn địch vẽ đặc, luôn ở sorting layer trên cùng.

Tham chiếu để xem: build Pulse của *Nova Drift*, vũ khí Song of Mana / Garlic của *Vampire Survivors*.

### 2.2 Trống Đồng — nút panic ở tâm đấu trường

Một chiếc trống đồng cố định ở **giữa đấu trường**. Người chơi **lướt vào nó** → phát sóng xung kích: xoá sạch đạn địch trên màn + đẩy lùi + choáng 1 giây. Hồi chiêu ~20 giây.

> **Trong co-op: hai người dùng CHUNG một hồi chiêu.** Đây là chủ đích thiết kế — nó tạo khoảnh khắc phối hợp ("đừng dùng, để tao!"). Trạng thái do **host** giữ, không phải mỗi client một bản.

Tham chiếu: Blank của *Enter the Gungeon* + Teleporter của *Risk of Rain 2* + thùng tiếp tế của *Deep Rock Galactic*.

### 2.3 Tiến hoá thẻ — 8 công thức

Gom đủ thẻ nền sẽ tiến hoá thành thẻ đặc biệt. Đây là **lý do chơi lại chính** của game — không cắt xuống dưới 8 công thức.

| Nguyên liệu | Kết quả |
|---|---|
| Xuyên thấu ×3 + Nảy tường ×3 | **Nỏ Thần** |
| Nổ ×3 + Vệt cháy ×3 | **Lửa Thiêng** |
| +2 đạn ×3 + Tách đạn ×3 | **Trăm Trứng** |
| *(5 công thức còn lại — chốt ở tuần 4)* | |

### 2.4 Hồn — soul pickup

Quái chết rơi ra "hồn", tự hút về người chơi, cao độ âm thanh tăng dần khi nhặt liên tiếp. Hồn nạp cho Trống Đồng. Đây là vòng lặp dopamine 2 giây mà thể loại này bắt buộc phải có.

---

## 3. BA LUẬT SẮT — vi phạm là hỏng game

### 🔴 LUẬT 1 — Không bao giờ có "chế độ chơi đơn" riêng

```
Chơi đơn = Mirror host mode, 1 client
Chơi đôi = Mirror host mode, 2 client
         --> MỘT code path duy nhất
```

Kể cả khi test một mình, game vẫn chạy qua Mirror host mode. **Không viết bất kỳ nhánh `if (isSinglePlayer)` nào.** Lý do: lắp mạng vào sau là nguyên nhân số 1 giết các dự án Unity sinh viên.

### 🔴 LUẬT 2 — Đồng bộ *sự kiện*, không đồng bộ *trạng thái*

| Đối tượng | Đồng bộ? | Cách làm |
|---|---|---|
| Người chơi (2) | ✅ | `NetworkTransform`, client dự đoán nhân vật của mình |
| Quái | ⚠️ một phần | Đồng bộ **seed + đặc tả đợt**; hai máy tự spawn. Snapshot vị trí 2 lần/giây |
| Đạn | ❌ **KHÔNG BAO GIỜ** | Spawn cục bộ. Đạn phía client thuần trang trí |
| Sát thương / chết | ✅ host quyết | Host là chân lý. Phát RPC sự kiện, không sync thanh máu liên tục |
| Chọn thẻ | ✅ | Đồng bộ *lựa chọn*, hai bên tự áp dụng |
| VFX, hit-stop, screen shake | ❌ | Hoàn toàn cục bộ |

**Không gắn `NetworkIdentity` lên đạn hay lên VFX.** Nếu thấy mình sắp làm vậy, dừng lại và đọc [docs/NETCODE.md](docs/NETCODE.md).

### 🔴 LUẬT 3 — Không `UnityEngine.Random` trong gameplay

Mọi ngẫu nhiên ảnh hưởng gameplay phải đi qua `LAC.Core.RunRandom` (seeded). Một lệnh `Random.Range` lạc loài = một desync giữa host và client. Ngoại lệ duy nhất: VFX và âm thanh thuần trang trí.

---

## 4. Cấu trúc thư mục

Tất cả code và tài nguyên **do nhóm tạo ra** nằm trong `Assets/_LAC/`. Thư mục khác trong `Assets/` là của package bên thứ ba — **không sửa**.

```
Assets/_LAC/
├── Scripts/
│   ├── Core/       Vòng đời ván, quản lý đợt, object pool, RunRandom, event bus
│   ├── Player/     Di chuyển, dash, máu, bộ điều khiển nhân vật
│   ├── Enemies/    FSM quái, hành vi, spawner
│   ├── Combat/     Sát thương, đạn, hitbox, targeting
│   ├── Cards/      Bể thẻ, hiệu ứng thẻ, tiến hoá, UI chọn thẻ
│   ├── Director/   AI Đạo Diễn (LinUCB), telemetry
│   ├── Net/        Mirror, Steamworks, lobby, đồng bộ
│   ├── Drum/       Trống Đồng
│   ├── UI/         HUD, menu, màn kết quả
│   ├── VFX/        Sóng âm Đông Sơn, hit feedback, camera shake
│   ├── Audio/      Nhạc, SFX, hệ thống cao độ tăng dần
│   └── Utils/      Extension, helper toán học
├── Data/           ScriptableObject: Cards, Characters, Enemies, Waves
├── Prefabs/        Player, Enemies, Projectiles, VFX, UI
├── Art/            Sprites, Animations, VFX, UI, Palettes
├── Audio/          SFX, Music
└── Scenes/
```

**Quy tắc:** file mới luôn đặt đúng thư mục con. Không tạo script ở gốc `Assets/`.

---

## 5. Quy tắc code

- Namespace bám theo thư mục: `LAC.Core`, `LAC.Player`, `LAC.Net`, `LAC.Cards`…
- Nội dung (thẻ, quái, nhân vật, đợt) **luôn** là `ScriptableObject` trong `Data/`, **không** hardcode trong C#.
- Mọi thứ spawn nhiều lần (đạn, quái, VFX, số sát thương) **phải** qua object pool. Không `Instantiate` trong gameplay loop.
- Ngân sách hiệu năng: 60 FPS với **40 quái + 200 đạn** cùng lúc.
- Tiếng Anh cho tên biến/hàm/class. Tiếng Việt cho comment và văn bản hiển thị.
- Comment chỉ giải thích *tại sao*, không mô tả lại điều code đã nói rõ.

---

## 6. Quy trình sau mỗi chức năng — BẮT BUỘC

Khi hoàn thành một chức năng, làm **cả ba** bước trong **cùng một commit**:

1. **Tick ô trong [docs/TASKS.md](docs/TASKS.md)** — đổi `- [ ]` thành `- [x]`, điền tên người làm và ngày.
2. **Ghi một mục vào [docs/PROGRESS.md](docs/PROGRESS.md)** — chức năng đó *làm gì*, file nào, dùng thế nào. Theo mẫu có sẵn trong file.
3. **Commit** theo định dạng: `feat(T-102): dash có i-frame và cooldown`

Chi tiết: [docs/CONVENTIONS.md](docs/CONVENTIONS.md).

> **Nếu bạn là AI và vừa code xong một task: tự làm bước 1 và 2 trước khi báo hoàn thành.** Không bỏ qua.

---

## 7. Đọc gì tiếp theo

| # | File | Khi nào cần |
|---|---|---|
| 1 | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | **Luôn luôn.** Bản đồ hệ thống, luồng dữ liệu, ai gọi ai |
| 2 | [docs/TASKS.md](docs/TASKS.md) | **Luôn luôn.** Việc đang làm, đã xong, ai làm gì |
| 3 | [docs/NETCODE.md](docs/NETCODE.md) | Trước khi động vào mạng, spawn, sát thương |
| 4 | [docs/GDD.md](docs/GDD.md) | Cần con số cụ thể: chỉ số quái, chi tiết thẻ, thông số trùm |
| 5 | [docs/CONVENTIONS.md](docs/CONVENTIONS.md) | Trước commit đầu tiên |
| 6 | [docs/PROGRESS.md](docs/PROGRESS.md) | Muốn biết chức năng nào đã tồn tại (đừng viết lại) |

---

## 8. GDD có chỗ đã lỗi thời

[docs/GDD.md](docs/GDD.md) là bản gốc. Khi mâu thuẫn, **CLAUDE.md này thắng**. Các điểm đã bị ghi đè:

| GDD nói | Thực tế |
|---|---|
| Unity 6.3 LTS | **6000.5.6f1** |
| Giá $6.99 | **$4.99** |
| 48 thẻ | **32 thẻ nền + 8 tiến hoá = 40** |
| 4 cấp độ khó | **2 cấp** |
| Không có vật phẩm rơi ra | **Có hồn (soul pickup)** |
| Không có Trống Đồng, không có tiến hoá thẻ | **Có — xem mục 2** |
| AI Đạo Diễn chạy mọi chế độ | **Chỉ chơi đơn.** Co-op dùng bảng đợt cố định |
| Co-op làm ở tuần 10–12 | **Kiến trúc mạng từ tuần 1** — xem Luật 1 |
| Tấm: ×2 sát thương 1s sau dash | **Lỗi thiết kế** — dash CD 0.4s < 1s nên buff bật vĩnh viễn. Sửa: buff áp cho **phát bắn kế tiếp**, không theo thời gian |

---

## 9. Môi trường

- Unity **6000.5.6f1** — cả nhóm phải dùng đúng bản này
- Unity-MCP đã cài (88 tool). Mất kết nối thì: mở Unity → focus cửa sổ → chờ domain reload
- Cấu hình MCP ở `.mcp.json` (đã commit). Cấu hình riêng máy ở `UserSettings/` (không commit)

# LẠC — Children of the Dragon

> **AI và người mới đọc file này trước tiên.** Đọc xong là hiểu đủ để bắt đầu làm.
> Chỉ 3 file tài liệu: file này, [docs/TASKS.md](docs/TASKS.md) (việc), [docs/GDD.md](docs/GDD.md) (chi tiết thiết kế).
> Cần biết code nằm đâu, ai gọi ai → [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## 1. Game này là gì

**Arena survival roguelite 2D top-down, pixel art, nền thần thoại Việt Nam.** Dòng Vampire Survivors / Brotato.

| | |
|---|---|
| Nền tảng | PC — Steam · giá **$4.99** |
| Người chơi | 1–2, **co-op online bắt buộc** (yêu cầu của giảng viên) |
| Một ván | 15 phút · 16 đợt quái |
| Engine | **Unity 6000.5.6f1** · URP 2D · C# |
| Mạng | Mirror + Steamworks.NET, **host quyết mọi thứ** |

**Vòng lặp:** đợt quái (30–50s) → chọn 1 trong 3 thẻ (10s) → lặp ×15 → trùm Chằn Tinh ở đợt 16.

**Chỉ hai thao tác: di chuyển + lướt (dash).** Vũ khí tự khai hỏa. Người chơi điều khiển vị trí và thời điểm, không điều khiển việc bắn.

| Nhân vật | Vũ khí | HP | Tốc độ | Tầm | Chu kỳ |
|---|---|---|---|---|---|
| Thạch Sanh | Đàn bầu | 6 | 5 | 4 (vòng tròn) | 0.9s |
| Gióng | Roi sắt | 10 | 3 | 2.5 (hình cung) | 0.6s |
| Tấm | Sáo trúc | 4 | 8 | 7 (tia) | 0.12s |

Vũ khí gắn chết với nhân vật, không thay thế được. Đổi nhân vật là đổi game.

---

## 2. Bốn thứ làm nên bản sắc — không được bỏ

Đây là thứ phân biệt LẠC với 200 game survivors khác trên Steam.

**① Sóng âm Đông Sơn.** Mọi vũ khí là nhạc cụ. Đòn đánh hiện ra thành **vòng tròn đồng tâm lan ra** mang hoa văn trống đồng. Cuối ván màn hình đầy sóng âm của chính người chơi. Đây là hình ảnh cho trailer và TikTok.
> ⚠️ Giữ riêng **1 màu trong bảng 24 màu Đông Hồ chỉ cho đòn địch**. VFX người chơi không bao giờ dùng màu đó, vẽ alpha thấp + additive, nằm layer dưới. Đòn địch vẽ đặc, layer trên cùng. Không có luật này thì cuối ván không ai thấy đạn địch.

**② Trống Đồng.** Trống đặt cố định giữa đấu trường. Dash vào nó → xoá sạch đạn địch + đẩy lùi + choáng 1 giây. Hồi chiêu ~20s.
> ⚠️ **Co-op: hai người dùng CHUNG một hồi chiêu.** Đây là chủ đích — nó tạo khoảnh khắc "đừng dùng, để tao!". Host giữ trạng thái, không phải mỗi máy một bản.

**③ Tiến hoá thẻ, 8 công thức.** Gom đủ thẻ nền → tiến hoá thành thẻ đặc biệt. Đây là **lý do chơi lại chính**, không cắt xuống dưới 8.
> Xuyên thấu×3 + Nảy tường×3 = **Nỏ Thần** · Nổ×3 + Vệt cháy×3 = **Lửa Thiêng** · +2 đạn×3 + Tách đạn×3 = **Trăm Trứng** · *(5 công thức còn lại chốt ở tuần 4)*

**④ Hồn.** Quái chết rơi ra hồn, tự hút về, âm thanh cao dần khi nhặt liên tiếp. Hồn nạp cho Trống Đồng. Đây là vòng lặp dopamine 2 giây mà thể loại này bắt buộc phải có.

---

## 3. BA LUẬT SẮT

Ba lỗi này sẽ giết dự án. Mọi dòng code phải tuân thủ.

### 🔴 1 — Không bao giờ có "chế độ chơi đơn" riêng

```
Chơi đơn = Mirror host mode, 1 client
Chơi đôi = Mirror host mode, 2 client
         --> MỘT code path duy nhất
```

Kể cả test một mình cũng chạy qua host mode. **Không viết `if (isSinglePlayer)`.**
Lý do: lắp mạng vào sau là nguyên nhân số 1 giết dự án Unity sinh viên. Làm từ tuần 1 tốn ~6 ngày; lắp ở tuần 10 tốn ~15 ngày và rất dễ hỏng.

### 🔴 2 — Đồng bộ *sự kiện*, không đồng bộ *trạng thái*

| Thứ | Đồng bộ? | Cách |
|---|---|---|
| Người chơi (2) | ✅ | `NetworkTransform`, client dự đoán nhân vật của mình |
| Máu, sát thương, chết | ✅ | **Chỉ host quyết.** Phát RPC sự kiện, không sync thanh máu liên tục |
| Quái | ⚠️ một phần | Đồng bộ **seed + đặc tả đợt**, hai máy tự spawn. Host gửi snapshot vị trí 2 lần/giây |
| Chọn thẻ | ✅ | Đồng bộ *id thẻ đã chọn*, hai máy tự áp hiệu ứng |
| Hồi chiêu Trống Đồng | ✅ | `SyncVar` trên host, dùng chung |
| **Đạn** | ❌ **không bao giờ** | Spawn cục bộ. Đạn phía client thuần trang trí |
| VFX, khựng hình, rung màn, SFX | ❌ | Hoàn toàn cục bộ |

Một câu để nhớ: **host mô phỏng sự thật, client mô phỏng hình ảnh.**

Ba sai lầm cụ thể:
- ❌ Gắn `NetworkIdentity` lên đạn → 200 object đồng bộ → vỡ băng thông. *Đây là thứ AI tự động làm nếu bạn chỉ nói "thêm multiplayer".*
- ❌ Client tự trừ máu mình → hai máy lệch trong vài giây.
- ❌ Chỉ test localhost 0ms → tuần 15 test với bạn ở tỉnh khác thì game vỡ, hết thời gian sửa. **Bật giả lập trễ 100ms ngay từ tuần 1.**

Mẫu chuẩn — client xin, host quyết, phát về:
```csharp
[Command] void CmdTryActivateDrum() {
    if (!_isReady) return;        // host kiểm tra, không phải client
    _isReady = false;             // SyncVar tự lan về client
    ApplyShockwave();             // host thi hành
    RpcPlayVfx();                 // client chỉ nhận phần nhìn
}
```

### 🔴 3 — Không `UnityEngine.Random` trong gameplay

Mọi ngẫu nhiên ảnh hưởng gameplay đi qua `LAC.Core.RunRandom` (có seed). Một lệnh `Random.Range` lạc loài = một lần desync. Ngoại lệ duy nhất: VFX và âm thanh thuần trang trí.

---

## 4. Code nằm ở đâu

Code và tài nguyên của nhóm nằm hết trong `Assets/_LAC/`. Thư mục khác trong `Assets/` là package bên thứ ba — **không sửa**.

```
Assets/_LAC/
├── Scripts/
│   ├── Core/      Vòng đời ván, quản lý đợt, object pool, RunRandom
│   ├── Player/    Di chuyển, dash, máu
│   ├── Enemies/   FSM quái, spawner
│   ├── Combat/    Sát thương, đạn, ngắm mục tiêu
│   ├── Cards/     Bể thẻ, hiệu ứng, tiến hoá, UI chọn thẻ
│   ├── Director/  AI Đạo Diễn, telemetry
│   ├── Net/       Mirror, Steam, lobby
│   ├── Drum/      Trống Đồng
│   ├── UI/  VFX/  Audio/  Utils/
├── Data/          ScriptableObject: Cards, Characters, Enemies, Waves
├── Prefabs/  Art/  Audio/  Scenes/
```

**File mới luôn đặt đúng thư mục con. Không tạo script ở gốc `Assets/`.**

---

## 5. Quy tắc code

- Namespace theo thư mục: `LAC.Core`, `LAC.Player`, `LAC.Net`…
- Class/method PascalCase · field private `_camelCase` · interface `ITargetable` · ScriptableObject `CardData`
- `[SerializeField] private`, **không** dùng `public` field
- Nội dung (thẻ, quái, nhân vật, đợt) **luôn** là ScriptableObject trong `Data/`, **không** hardcode trong C#
- Thứ spawn nhiều lần (đạn, quái, VFX) **phải** qua `ObjectPool`. Không `Instantiate` trong gameplay loop
- Không `GameObject.Find` / `FindObjectOfType` trong gameplay loop
- Ngân sách: **60 FPS với 40 quái + 200 đạn** cùng lúc
- Tên biến/hàm tiếng Anh · comment và chữ hiển thị tiếng Việt
- Comment chỉ giải thích *tại sao*, không mô tả lại điều code đã nói rõ

---

## 6. Quy trình làm việc

### Git — giữ đơn giản

```
main   bản chạy được. Chỉ merge từ dev ở mỗi cổng nghiệm thu.
dev    mọi người làm ở đây.
```

Sáng ra: `git pull origin dev` **trước khi làm gì cả.** Xong việc: commit + push ngay, đừng ôm code quá một ngày.

Commit: `feat(T-11): dash có i-frame` — loại là `feat` · `fix` · `art` · `docs` · `balance`

### 🔴 Luật scene — nguồn đau khổ lớn nhất của Unity + Git

File `.unity` và `.prefab` git không merge được. Ba việc:

1. **Mỗi lúc chỉ MỘT người sửa scene.** Nhắn nhóm "tôi đang giữ Arena scene" → sửa → push → nhắn "đã trả".
2. **Làm mọi thứ trong prefab, không trong scene.** Scene chỉ chứa điểm spawn, camera, tilemap, manager.
3. **Cài UnityYAMLMerge một lần trên mỗi máy** (xem [README.md](README.md)).

Nếu lỡ conflict scene: **đừng sửa tay file YAML** — lấy bản trên server (`git checkout --theirs`) rồi làm lại trong Unity. Nhanh hơn nhiều.

### Sau mỗi chức năng — bắt buộc

Trong **cùng một commit** với code:

1. Tick ô trong [docs/TASKS.md](docs/TASKS.md) *(`Alt+C`)* + điền tên và ngày
2. **Viết một dòng `>` ngay dưới** nói chức năng đó làm gì, nằm ở file nào

> **AI vừa code xong task: tự làm 2 bước này trước khi báo hoàn thành.** Không có dòng `>` thì coi như chưa xong.

### Ra lệnh cho AI thế nào

Mở đầu: *"Đọc CLAUDE.md trước. Sau đó làm task T-11 trong docs/TASKS.md."*

Đưa kiến trúc cho AI, đừng để AI tự nghĩ:
- ❌ *"thêm multiplayer vào game này"* → nó gắn `NetworkIdentity` lên mọi thứ
- ✅ *"theo bảng đồng bộ ở mục 3, cài Trống Đồng với hồi chiêu dùng chung do host giữ"*

Trước khi merge code AI viết, hỏi 3 câu: (1) có trùng chức năng đã có trong TASKS.md không? (2) có phạm Luật Sắt nào không? (3) có `Instantiate` trong `Update()` không?

---

## 7. GDD có chỗ đã lỗi thời

[docs/GDD.md](docs/GDD.md) là bản gốc. **Khi mâu thuẫn, file này thắng.**

| GDD nói | Thực tế |
|---|---|
| Unity 6.3 LTS · giá $6.99 | **6000.5.6f1** · **$4.99** |
| 48 thẻ · 4 cấp độ khó | **32 thẻ nền + 8 tiến hoá** · **2 cấp** |
| Không có vật phẩm rơi ra | **Có hồn (soul pickup)** |
| Không có Trống Đồng, không có tiến hoá thẻ | **Có — xem mục 2** |
| AI Đạo Diễn chạy mọi chế độ | **Chỉ chơi đơn.** Co-op dùng bảng đợt cố định *(hai người khác build khác máu → không có câu trả lời đúng; và thực nghiệm khoá luận chạy chơi đơn nên sạch hơn)* |
| Đạo diễn siết máu về 15–25% mọi lúc | **Bất đối xứng:** yếu thì được giảm áp lực; mạnh thì đổi **thành phần và hướng spawn**, không tăng số lượng/máu. Siết máu là trừng phạt người chơi vì xây build tốt |
| Co-op làm ở tuần 10–12 | **Kiến trúc mạng từ tuần 1** — xem Luật 1 |
| Tấm: ×2 sát thương 1s sau dash | **Lỗi thiết kế** — dash CD 0.4s < 1s nên buff bật vĩnh viễn. Sửa: buff áp cho **phát bắn kế tiếp** |

---

## 8. Môi trường

- Unity **6000.5.6f1** — cả nhóm phải đúng bản này, bản khác sẽ nâng cấp file project và gây conflict
- Unity-MCP đã cài (88 tool). Mất kết nối thì: mở Unity → focus cửa sổ → chờ nạp lại

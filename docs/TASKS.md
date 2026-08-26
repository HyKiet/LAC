# TASKS — Bảng kế hoạch LẠC

> **Đây là nguồn sự thật duy nhất về việc ai đang làm gì.**
> Xong một task → tick ô ở đây **và** ghi vào [PROGRESS.md](PROGRESS.md), **trong cùng một commit**.

## Cách dùng

```
- [ ] **T-101** Tên task — `@ai-lam` — *phụ thuộc: T-100*
- [x] **T-101** Tên task — `@HyKiet` — ✅ 2026-08-27
```

- **Nhận việc:** điền tên mình vào chỗ `@ai-lam` rồi push ngay, để hai người kia không làm trùng.
- **Xong việc:** đổi `[ ]` → `[x]`, thay phần đuôi bằng `✅ YYYY-MM-DD`, rồi viết mục trong [PROGRESS.md](PROGRESS.md).
- **Không tick** nếu chưa chạy được trong Unity. "Code xong" ≠ "xong".

**Thành viên:** `@HyKiet` · `@dev2` · `@dev3` · `@artist`
*(Đổi `@dev2` / `@dev3` thành tên thật rồi commit.)*

---

## 🚦 Cổng nghiệm thu

| Cổng | Tuần | Tiêu chí đạt |
|---|---|---|
| **Cổng 1** | 3 | Di chuyển + dash + tự bắn *đã đã tay*, chạy qua Mirror host mode, 2 người cùng chơi trên LAN có giả lập trễ 100ms |
| **Cổng 2** | 7 | Ba nhân vật chơi ra ba cảm giác khác nhau **và** có clip 15 giây đăng TikTok được |
| **Cổng 3** | 12 | Co-op qua Steam invite hoạt động với người ngoài mạng LAN, chơi hết 16 đợt không desync |
| **Cổng 4** | 16 | Demo miễn phí lên Steam, bảo vệ khoá luận |

---

## 📅 TUẦN 1–3 — CỔNG 1: cảm giác chiến đấu + xương sống mạng

> Ba tuần này quyết định dự án sống hay chết. Kiến trúc mạng phải đúng **ngay từ commit đầu**, không lắp sau.

### Hạ tầng

- [ ] **T-001** Tạo repo, push cấu trúc thư mục + tài liệu — `@HyKiet`
- [ ] **T-002** Cả 3 máy clone được, mở Unity 6000.5.6f1 không lỗi — `@ai-lam` — *phụ thuộc: T-001*
- [ ] **T-003** Cài Mirror (UPM) + FizzySteamworks + Steamworks.NET — `@ai-lam`
- [ ] **T-004** Cấu hình `UnityYAMLMerge` trên cả 3 máy (chống conflict scene) — `@ai-lam` — *xem CONVENTIONS.md*
- [ ] **T-005** Bật Git LFS, xác nhận file .png đi qua LFS — `@ai-lam`

### Nền móng code

- [ ] **T-010** `RunRandom` — bọc `System.Random`, seed đồng bộ được — `@ai-lam`
- [ ] **T-011** `ObjectPool<T>` chung — `@ai-lam`
- [ ] **T-012** `GameEvents` event bus tĩnh — `@ai-lam`
- [ ] **T-013** `RunManager` khung: bắt đầu ván → 16 đợt → thắng/thua — `@ai-lam`
- [ ] **T-014** `CharacterData` ScriptableObject + 1 asset mẫu (Thạch Sanh) — `@ai-lam`

### Mạng — LÀM NGAY, KHÔNG ĐỂ TUẦN 10

- [ ] **T-020** `NetworkManagerLAC`, chạy host mode ngay cả khi chơi 1 mình — `@ai-lam` — *phụ thuộc: T-003*
- [ ] **T-021** `NetPlayerSpawner` — spawn 1–2 nhân vật đúng chỗ — `@ai-lam`
- [ ] **T-022** Bật `LatencySimulation` transport, mặc định 100ms + 2% mất gói khi dev — `@ai-lam`
- [ ] **T-023** `RunSync` — đồng bộ seed ván + nhân vật đã chọn — `@ai-lam`
- [ ] **T-024** Chạy 2 build cùng lúc trên 1 máy, cả hai vào được ván — `@ai-lam`

### Cảm giác chiến đấu

- [ ] **T-030** Input System: di chuyển 8 hướng + nút dash (bàn phím + tay cầm) — `@ai-lam`
- [ ] **T-031** `PlayerMovement` — di chuyển mượt, client dự đoán nhân vật mình — `@ai-lam`
- [ ] **T-032** `PlayerDash` — i-frame, cooldown, ghost trail — `@ai-lam`
- [ ] **T-033** `WeaponAuto` — chu kỳ, chọn mục tiêu gần nhất, bắn — `@ai-lam`
- [ ] **T-034** `Projectile` qua pool, **không NetworkIdentity** — `@ai-lam`
- [ ] **T-035** `DamageSystem` — điểm vào duy nhất, host-only — `@ai-lam`
- [ ] **T-036** `PlayerHealth` — host trừ máu, client nhận SyncVar — `@ai-lam`
- [ ] **T-037** Quái đầu tiên (Cô Hồn) — FSM đuổi theo, chết được — `@ai-lam`
- [ ] **T-038** `HitFeedback` — hit-stop, nháy trắng, đẩy lùi, số sát thương — `@ai-lam`
- [ ] **T-039** `CameraShake` theo cường độ — `@ai-lam`

### Money shot — bản đầu

- [ ] **T-040** Shader/VFX sóng âm đồng tâm, hoa văn Đông Sơn — `@ai-lam`
- [ ] **T-041** `SoundWaveVFX` gắn vào `WeaponAuto`, additive + alpha thấp — `@ai-lam`
- [ ] **T-042** Chốt bảng 24 màu Đông Hồ, **khoá 1 màu riêng cho đòn địch** — `@artist`

### Mỹ thuật tuần 1–3

- [ ] **T-050** Sprite Thạch Sanh 32×32, 4 frame/hành động (idle, đi, dash) — `@artist`
- [ ] **T-051** Sprite Cô Hồn — `@artist`
- [ ] **T-052** Tileset Sân Đình 16×16 — `@artist`

### Marketing — bắt đầu từ TUẦN 2, không phải tuần 15

- [ ] **T-060** Mua Steam Direct ($100), tạo app — `@HyKiet`
- [ ] **T-061** **Dựng trang Steam ở tuần 2** — wishlist tích từ ngày đó — `@HyKiet`
- [ ] **T-062** Lập TikTok + YouTube Shorts, đăng devlog số 1 — `@HyKiet`
- [ ] **T-063** Lịch đăng devlog **hàng tuần** từ tuần 2 đến tuần 16 — `@HyKiet`

### ✅ Cổng 1 — nghiệm thu tuần 3

- [ ] **G-1** Hai người chơi cùng ván qua Mirror, có giả lập trễ 100ms, đánh quái đã tay — `cả nhóm`

---

## 📅 TUẦN 4–5 — Thẻ, tiến hoá, hồn

- [ ] **T-100** `CardData` ScriptableObject + `CardEffect` — `@ai-lam`
- [ ] **T-101** `PlayerStats` — cộng dồn hiệu ứng thẻ, vũ khí đọc từ đây — `@ai-lam`
- [ ] **T-102** `CardPool` — bốc 3 thẻ bằng `RunRandom`, lọc theo nhân vật — `@ai-lam`
- [ ] **T-103** `CardPickUI` — 10 giây, 2 lượt đổi, tạm dừng game — `@ai-lam`
- [ ] **T-104** **Đồng bộ chọn thẻ trong co-op** — đợt sau chỉ bắt đầu khi cả hai chọn xong — `@ai-lam`
- [ ] **T-105** Viết 12 thẻ chỉ số (đợt 1) — `@ai-lam`
- [ ] **T-106** Viết 20 thẻ biến đổi vũ khí (đợt 2) — `@ai-lam`
- [ ] **T-110** `CardEvolution` — máy kiểm tra công thức — `@ai-lam`
- [ ] **T-111** Chốt 8 công thức tiến hoá, viết vào GDD — `cả nhóm`
- [ ] **T-112** Cài 3 thẻ tiến hoá đầu: Nỏ Thần, Lửa Thiêng, Trăm Trứng — `@ai-lam`
- [ ] **T-113** Cài 5 thẻ tiến hoá còn lại — `@ai-lam`
- [ ] **T-114** UI báo tiến hoá — hiệu ứng lớn, ăn mừng — `@ai-lam`
- [ ] **T-120** `SoulPickup` — rơi ra, tự hút, hiệu ứng — `@ai-lam`
- [ ] **T-121** Âm thanh nhặt hồn cao độ tăng dần theo chuỗi — `@ai-lam`
- [ ] **T-130** 40 icon thẻ (32 nền + 8 tiến hoá) — `@artist`

---

## 📅 TUẦN 6–7 — CỔNG 2: ba nhân vật + money shot hoàn chỉnh

- [ ] **T-200** Gióng — roi sắt, đòn hình cung, đặc tính riêng — `@ai-lam`
- [ ] **T-201** Tấm — sáo trúc, tia, **buff áp cho phát bắn kế tiếp** (không theo thời gian) — `@ai-lam`
- [ ] **T-202** Màn chọn nhân vật + đồng bộ lựa chọn qua mạng — `@ai-lam`
- [ ] **T-203** Thẻ riêng theo nhân vật (6 thẻ × 3) — `@ai-lam`
- [ ] **T-210** Money shot hoàn chỉnh: sóng âm riêng cho từng nhạc cụ — `@ai-lam`
- [ ] **T-211** Kiểm tra đọc hiểu: 40 quái + 200 đạn vẫn thấy được đòn địch — `cả nhóm`
- [ ] **T-212** Đo hiệu năng: 60 FPS ở 40 quái + 200 đạn — `@ai-lam`
- [ ] **T-220** Sprite Gióng + Tấm — `@artist`
- [ ] **T-221** VFX ba nhạc cụ, ba hình sóng khác nhau — `@artist`

### ✅ Cổng 2 — nghiệm thu tuần 7

- [ ] **G-2** Ba nhân vật cho ba cảm giác khác nhau **và** có clip 15s đăng được — `cả nhóm`

---

## 📅 TUẦN 8–9 — Quái, trùm, Trống Đồng

- [ ] **T-300** Ma Trơi (bắn xa) — `@ai-lam`
- [ ] **T-301** Bù Nhìn (chậm, trâu) — `@ai-lam`
- [ ] **T-302** Ma Da (nhanh, yếu) — `@ai-lam`
- [ ] **T-303** Quỷ Nhỏ (chết thì tách đôi) — `@ai-lam`
- [ ] **T-304** Snapshot vị trí quái 2 lần/giây để chống trôi — `@ai-lam`
- [ ] **T-310** `DongSonDrum` — cooldown **dùng chung**, host giữ trạng thái — `@ai-lam`
- [ ] **T-311** `CmdTryActivate` khi dash chạm trống — `@ai-lam`
- [ ] **T-312** `DrumShockwave` — xoá đạn + đẩy lùi + choáng 1s — `@ai-lam`
- [ ] **T-313** Hồn nạp cho trống, hiển thị vòng nạp trên HUD — `@ai-lam`
- [ ] **T-314** Test co-op: hai người tranh nhau trống, không desync — `@ai-lam`
- [ ] **T-320** Trùm Chằn Tinh — pha 1 — `@ai-lam`
- [ ] **T-321** Chằn Tinh — pha 2 + chuyển pha — `@ai-lam`
- [ ] **T-322** Trùm trong co-op: máu nhân theo số người chơi — `@ai-lam`
- [ ] **T-330** Sprite 4 quái còn lại — `@artist`
- [ ] **T-331** Sprite Chằn Tinh + hoạt ảnh 2 pha — `@artist`
- [ ] **T-332** Sprite trống đồng + VFX sóng xung kích — `@artist`
- [ ] **T-333** Tileset Ruộng Lúa + Âm Phủ — `@artist`

---

## 📅 TUẦN 10–12 — CỔNG 3: co-op qua Steam + AI Đạo Diễn

> Phần khó của mạng đã xong từ tuần 1. Ba tuần này chỉ là ghép Steam và xử lý trường hợp biên.

- [ ] **T-400** `SteamLobby` — tạo phòng, mời bạn qua overlay Steam — `@ai-lam`
- [ ] **T-401** Chuyển từ transport LAN sang FizzySteamworks — `@ai-lam`
- [ ] **T-402** Test với người **ngoài mạng LAN** — `@ai-lam`
- [ ] **T-403** Hạ gục + hồi sinh (người kia đứng cạnh 3 giây) — `@ai-lam`
- [ ] **T-404** Xử lý client rớt giữa ván — `@ai-lam`
- [ ] **T-405** Xử lý host thoát — client về menu êm, không treo — `@ai-lam`
- [ ] **T-406** Chơi hết 16 đợt co-op không desync, 3 lần liên tiếp — `cả nhóm`
- [ ] **T-410** `Telemetry` — ghi CSV mọi đợt (**cắm từ tuần 8**) — `@ai-lam`
- [ ] **T-411** `FixedWaveTable` — bảng đợt cố định (co-op + nhóm đối chứng) — `@ai-lam`
- [ ] **T-412** `ContextVector` + `WaveSpec` — `@ai-lam`
- [ ] **T-413** `AIDirector` LinUCB — `@ai-lam`
- [ ] **T-414** `SafetyConstraints` — chặn đợt vượt ngưỡng — `@ai-lam`
- [ ] **T-415** **Đạo diễn bất đối xứng**: mạnh thì đổi thành phần/hướng, không tăng số lượng/máu — `@ai-lam`
- [ ] **T-416** **Đạo diễn hiện hình trên HUD** — người chơi thấy nó đang làm gì — `@ai-lam`

### ✅ Cổng 3 — nghiệm thu tuần 12

- [ ] **G-3** Co-op Steam với người ngoài LAN, hết 16 đợt, không desync — `cả nhóm`

---

## 📅 TUẦN 13–14 — Tiến trình, cân bằng, đánh giá

- [ ] **T-500** Tiền tệ Ngọc + lưu tiến trình — `@ai-lam`
- [ ] **T-501** Bảng mở khoá (3 gói thẻ, không phải 5) — `@ai-lam`
- [ ] **T-502** 2 cấp độ khó (không phải 4) — `@ai-lam`
- [ ] **T-503** Màn kết quả sau ván + thống kê — `@ai-lam`
- [ ] **T-510** Cân bằng: chốt sát thương gốc cho cả 3 vũ khí (**GDD đang thiếu**) — `cả nhóm`
- [ ] **T-511** Cân bằng đường cong 16 đợt — `cả nhóm`
- [ ] **T-520** **Thực nghiệm khoá luận: 15 người đạo diễn AI vs 15 người bảng cố định, chơi đơn** — `@HyKiet`
- [ ] **T-521** Phân tích số liệu, viết chương đánh giá — `@HyKiet`
- [ ] **T-530** Nhạc nền 3 bối cảnh + nhạc trùm — `@artist`
- [ ] **T-531** Toàn bộ SFX — `@ai-lam`

---

## 📅 TUẦN 15–16 — CỔNG 4: demo + bảo vệ

- [ ] **T-600** Cắt demo miễn phí (đề xuất: 8 đợt đầu, 1 nhân vật) — `@ai-lam`
- [ ] **T-601** Dựng trailer — dùng money shot làm 5 giây đầu — `@HyKiet`
- [ ] **T-602** Ảnh chụp + capsule art cho trang Steam — `@artist`
- [ ] **T-603** Đăng demo lên Steam — `@HyKiet`
- [ ] **T-604** Đăng ký **Steam Next Fest** đợt gần nhất — `@HyKiet`
- [ ] **T-605** Đặt giá vùng: $4.99 · VN 49.000–59.000₫ (**đè lên gợi ý ~78.000₫ của Steam**) — `@HyKiet`
- [ ] **T-610** Slide + demo bảo vệ khoá luận — `cả nhóm`
- [ ] **T-611** Viết chương Hạn chế: *"mở rộng đạo diễn cho nhiều người chơi cần hàm mục tiêu đa tác nhân — ngoài phạm vi"* — `@HyKiet`

> **KHÔNG bán game ở tuần 16.** Ra demo + bảo vệ khoá luận. Chỉ đặt ngày phát hành khi đạt ~8.000 wishlist. Bạn chỉ có đúng một lần ra mắt.

---

## 🅿️ Đã hoãn — chỉ làm nếu còn thời gian

- [ ] **P-01** Daily challenge + Steam Leaderboard
- [ ] **P-02** Nhân vật thứ 4
- [ ] **P-03** Trùm thứ 2
- [ ] **P-04** Đạo diễn AI chạy trong co-op (hàm mục tiêu đa tác nhân)

## ❌ Ngoài phạm vi — đã chốt không làm

Co-op 4 người · matchmaking · cửa hàng trang phục · bản mobile · cắt cảnh · rẽ nhánh Núi/Biển *(nhóm đã bác ở tuần 0)*

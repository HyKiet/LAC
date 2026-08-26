# TASKS — Kế hoạch LẠC

> **Đây là file duy nhất theo dõi công việc.** Ai đang làm gì, đã xong gì, chức năng đó làm gì — tất cả ở đây.

## Cách dùng — 3 bước

**1. Nhận việc:** thay `chưa ai nhận` bằng tên bạn, push ngay để hai người kia không làm trùng.

**2. Làm xong:** đổi `[ ]` → `[x]` *(để con trỏ vào dòng, bấm `Alt+C`)*, thêm tên + ngày, rồi **viết một dòng `>` ngay dưới** nói chức năng đó làm gì và nằm ở file nào.

**3. Commit:** `feat(T-05): dash có i-frame`

```markdown
Trước:
- [ ] **T-05** Dash có i-frame và cooldown — chưa ai nhận

Sau:
- [x] **T-05** Dash có i-frame và cooldown — @HyKiet · 27/08
  > Dash 6m trong 0.15s, bất tử suốt lúc dash, hồi 0.4s. `Player/PlayerDash.cs`. Gọi `TryDash()` từ `PlayerController`.
```

Dòng `>` đó là thứ để người sau (và AI) biết chức năng đã tồn tại, đừng viết lại. **Không có dòng đó thì coi như chưa xong.**

**Thành viên:** `@HyKiet` · `@dev2` · `@dev3` · `@artist` — *đổi thành tên thật rồi commit*

---

# 🚩 CỔNG 1 — Tuần 1–3

**Đạt khi:** hai người cùng chơi một ván qua mạng, đánh quái thấy đã tay.

### Chuẩn bị

- [ ] **T-01** Cả 3 máy clone repo, mở được bằng Unity 6000.5.6f1 — chưa ai nhận
- [ ] **T-02** Cài Mirror + FizzySteamworks + Steamworks.NET — chưa ai nhận

### Nền móng

- [ ] **T-03** `RunRandom` — nguồn ngẫu nhiên duy nhất, có seed — chưa ai nhận
- [ ] **T-04** `ObjectPool` — dùng chung cho đạn, quái, VFX — chưa ai nhận
- [ ] **T-05** `RunManager` — bắt đầu ván → 16 đợt → thắng/thua — chưa ai nhận
- [ ] **T-06** `CharacterData` (ScriptableObject) + asset Thạch Sanh — chưa ai nhận

### Mạng — làm ngay tuần 1, không để tuần 10

- [ ] **T-07** `NetworkManagerLAC` chạy host mode, kể cả khi chơi một mình — chưa ai nhận
- [ ] **T-08** Bật giả lập trễ 100ms làm mặc định khi dev — chưa ai nhận
- [ ] **T-09** Spawn 1–2 nhân vật, hai máy cùng vào được ván — chưa ai nhận

### Cảm giác chiến đấu

- [ ] **T-10** Di chuyển 8 hướng (bàn phím + tay cầm) — chưa ai nhận
- [ ] **T-11** Dash — i-frame, cooldown, vệt mờ — chưa ai nhận
- [ ] **T-12** Vũ khí tự bắn — chu kỳ, ngắm mục tiêu gần nhất — chưa ai nhận
- [ ] **T-13** `DamageSystem` — mọi sát thương đi qua đây, chỉ host quyết — chưa ai nhận
- [ ] **T-14** Quái đầu tiên (Cô Hồn) — đuổi theo, chết được — chưa ai nhận
- [ ] **T-15** Phản hồi khi đánh trúng — khựng hình, nháy trắng, đẩy lùi, số sát thương, rung màn — chưa ai nhận
- [ ] **T-16** **Sóng âm Đông Sơn** — vòng tròn lan ra, hoa văn trống đồng — chưa ai nhận

### Mỹ thuật

- [ ] **T-17** Chốt bảng 24 màu Đông Hồ, **khoá riêng 1 màu cho đòn địch** — chưa ai nhận
- [ ] **T-18** Sprite Thạch Sanh + Cô Hồn + tileset Sân Đình — chưa ai nhận

### Marketing — bắt đầu tuần 2, không phải tuần 15

- [ ] **T-19** Mua Steam Direct ($100), **dựng trang Steam ngay tuần 2** — chưa ai nhận
- [ ] **T-20** Lập TikTok, đăng devlog số 1, đặt lịch đăng hàng tuần — chưa ai nhận

---

# 🚩 CỔNG 2 — Tuần 4–7

**Đạt khi:** ba nhân vật chơi ra ba cảm giác khác nhau, **và** quay được clip 15 giây đăng TikTok.

- [ ] **T-21** Thẻ nâng cấp — `CardData` + áp hiệu ứng lên chỉ số — chưa ai nhận
- [ ] **T-22** Màn chọn 1 trong 3 thẻ, 10 giây, 2 lượt đổi — chưa ai nhận
- [ ] **T-23** **Đồng bộ chọn thẻ:** đợt sau chỉ bắt đầu khi cả hai chọn xong — chưa ai nhận
- [ ] **T-24** Viết 32 thẻ nền — chưa ai nhận
- [ ] **T-25** **Tiến hoá thẻ** — máy kiểm tra công thức + UI ăn mừng — chưa ai nhận
- [ ] **T-26** Chốt và cài 8 công thức tiến hoá — chưa ai nhận
- [ ] **T-27** **Hồn** — quái chết rơi ra, tự hút về, âm thanh cao dần — chưa ai nhận
- [ ] **T-28** Gióng — roi sắt, đòn hình cung — chưa ai nhận
- [ ] **T-29** Tấm — sáo trúc, tia. **Buff áp cho phát bắn kế tiếp**, không theo thời gian — chưa ai nhận
- [ ] **T-30** Màn chọn nhân vật + đồng bộ qua mạng — chưa ai nhận
- [ ] **T-31** Sóng âm riêng cho từng nhạc cụ — chưa ai nhận
- [ ] **T-32** Kiểm tra: 40 quái + 200 đạn vẫn 60 FPS và vẫn thấy được đòn địch — chưa ai nhận
- [ ] **T-33** Sprite Gióng + Tấm + 40 icon thẻ — chưa ai nhận

---

# 🚩 CỔNG 3 — Tuần 8–12

**Đạt khi:** co-op qua Steam với người ngoài mạng LAN, chơi hết 16 đợt không lỗi đồng bộ.

- [ ] **T-34** 4 quái còn lại — Ma Trơi, Bù Nhìn, Ma Da, Quỷ Nhỏ — chưa ai nhận
- [ ] **T-35** Snapshot vị trí quái 2 lần/giây để chống trôi — chưa ai nhận
- [ ] **T-36** **Trống Đồng** — dash vào → xoá đạn + đẩy lùi + choáng 1s — chưa ai nhận
- [ ] **T-37** **Trống dùng chung một hồi chiêu**, host giữ trạng thái — chưa ai nhận
- [ ] **T-38** Hồn nạp cho trống, hiện vòng nạp trên HUD — chưa ai nhận
- [ ] **T-39** Trùm Chằn Tinh — 2 pha, máu nhân theo số người chơi — chưa ai nhận
- [ ] **T-40** Mời bạn qua overlay Steam, đổi sang FizzySteamworks — chưa ai nhận
- [ ] **T-41** Hạ gục + hồi sinh (đứng cạnh 3 giây) — chưa ai nhận
- [ ] **T-42** Xử lý rớt mạng: client rớt, host thoát — không treo — chưa ai nhận
- [ ] **T-43** Ghi telemetry ra CSV cho phần đánh giá khoá luận — chưa ai nhận
- [ ] **T-44** Bảng đợt cố định (dùng cho co-op + nhóm đối chứng) — chưa ai nhận
- [ ] **T-45** **AI Đạo Diễn** LinUCB — chỉ chạy chơi đơn — chưa ai nhận
- [ ] **T-46** Đạo diễn **bất đối xứng** + **hiện trên HUD** cho người chơi thấy — chưa ai nhận
- [ ] **T-47** Sprite 4 quái + Chằn Tinh + trống đồng + 2 tileset — chưa ai nhận

---

# 🚩 CỔNG 4 — Tuần 13–16

**Đạt khi:** demo miễn phí lên Steam, bảo vệ xong khoá luận.

- [ ] **T-48** Tiền tệ Ngọc + lưu tiến trình + bảng mở khoá — chưa ai nhận
- [ ] **T-49** 2 cấp độ khó + màn kết quả sau ván — chưa ai nhận
- [ ] **T-50** **Cân bằng: chốt sát thương gốc cho 3 vũ khí** *(GDD đang thiếu con số này)* — chưa ai nhận
- [ ] **T-51** Cân bằng đường cong 16 đợt — chưa ai nhận
- [ ] **T-52** Nhạc + toàn bộ SFX — chưa ai nhận
- [ ] **T-53** **Thực nghiệm: 15 người AI Đạo Diễn vs 15 người bảng cố định** (chơi đơn) — chưa ai nhận
- [ ] **T-54** Phân tích số liệu, viết chương đánh giá — chưa ai nhận
- [ ] **T-55** Cắt demo miễn phí (8 đợt đầu, 1 nhân vật) — chưa ai nhận
- [ ] **T-56** Trailer — dùng sóng âm Đông Sơn làm 5 giây đầu — chưa ai nhận
- [ ] **T-57** Đăng demo + đăng ký Steam Next Fest — chưa ai nhận
- [ ] **T-58** Đặt giá: $4.99 · VN 49.000–59.000₫ *(đè lên gợi ý ~78.000₫ của Steam)* — chưa ai nhận
- [ ] **T-59** Slide + demo bảo vệ khoá luận — chưa ai nhận

> **Tuần 16 KHÔNG bán game.** Ra demo + bảo vệ. Chỉ đặt ngày phát hành khi đạt ~8.000 wishlist — bạn chỉ có đúng một lần ra mắt.

---

## Để dành — chỉ làm nếu dư thời gian

Daily challenge + bảng xếp hạng Steam · nhân vật thứ 4 · trùm thứ 2 · AI Đạo Diễn chạy trong co-op

## Đã chốt không làm

Co-op 4 người · matchmaking · cửa hàng trang phục · bản mobile · cắt cảnh · rẽ nhánh Núi/Biển

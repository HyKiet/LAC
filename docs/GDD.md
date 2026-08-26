# LẠC
## *Children of the Dragon*

**Game Design Document**

| | |
|---|---|
| Thể loại | Arena Survival Roguelite |
| Góc nhìn | Top-down 2D, pixel art |
| Nền tảng | PC (Steam) |
| Người chơi | 1–2 (co-op online) |
| Thời lượng ván | 15 phút |
| Engine | Unity 6.3 LTS · URP 2D · C# |
| Giá | $6.99 |

---

## 1. TỔNG QUAN

### 1.1 High Concept

Người chơi hóa thân thành hậu duệ của Lạc Long Quân và Âu Cơ, chiến đấu qua 16 đợt quái trong một đấu trường khép kín để giành lại Trống Đồng. Vũ khí tự động khai hỏa; người chơi kiểm soát vị trí và thời điểm lướt. Sau mỗi đợt, người chơi chọn một trong ba thẻ nâng cấp, dần biến vũ khí khởi đầu thành một cỗ máy hoàn toàn khác.

### 1.2 Pitch

> Arena survival roguelite trên nền thần thoại Việt Nam. Ba anh hùng, ba lối chơi. Mỗi ván là một cách xây dựng sức mạnh.

### 1.3 Điểm khác biệt

| | |
|---|---|
| **Nhân vật định hình lối chơi** | Vũ khí cố định theo nhân vật, không thay thế được. Đổi nhân vật là đổi game |
| **Bản sắc thị giác** | Bảng màu tranh Đông Hồ, hoa văn trống đồng Đông Sơn, nhạc cụ dân tộc |
| **AI Đạo Diễn** | Hệ thống sinh đợt thích ứng theo hành vi người chơi theo thời gian thực |

### 1.4 Đối tượng

Người chơi 15–30 tuổi quen thuộc với roguelite, tìm phiên chơi ngắn. Thị trường mục tiêu: toàn cầu qua Steam, trọng tâm ban đầu là Việt Nam và Đông Nam Á.

### 1.5 Tham chiếu

**Brotato** — cấu trúc đợt, tự động tấn công · **Soul Knight** — cảm giác bắn, thiết kế nhân vật · **Vampire Survivors** — đường cong sức mạnh · **Hades** — hệ thống nâng cấp theo lượt chọn

---

## 2. CORE LOOP

### 2.1 Vòng trong ván

```
ĐỢT CHIẾN ĐẤU (30–50s)  →  CHỌN THẺ (10s)  →  lặp ×15  →  TRÙM (đợt 16)
```

**Đợt chiến đấu.** Quái xuất hiện từ mép đấu trường theo nhịp do AI Đạo Diễn quyết định. Vũ khí tự bắn. Đợt kết thúc khi dọn sạch quái. Quái không rơi vật phẩm.

**Chọn thẻ.** Thời gian dừng. Ba thẻ hiện ra, chọn một. Mỗi ván có 2 lượt đổi thẻ.

**Kết thúc ván.** Thắng hoặc chết → nhận Ngọc → mở khóa → ván mới.

### 2.2 Vòng ngoài ván

```
Ngọc  →  Mở nhân vật · Mở thẻ vào bể · Tăng lượt đổi thẻ · Mở cấp độ khó
```

### 2.3 Hai thao tác

Toàn bộ tương tác của người chơi gồm **di chuyển** và **lướt**. Không có nút tấn công, không có nút ngắm, không có nút kỹ năng.

---

## 3. ĐIỀU KHIỂN

| Thao tác | Bàn phím | Tay cầm |
|---|---|---|
| Di chuyển | WASD | Stick trái |
| Lướt | Space | A / X |

**Dash:** bất tử 0.2s · hồi chiêu 1.0s · khoảng cách 3 đơn vị

**Tấn công:** tự động, chu kỳ theo chỉ số vũ khí

**Chọn mục tiêu:**
1. Lọc quái trong tầm hiệu dụng
2. Ưu tiên khoảng cách gần nhất
3. Khóa mục tiêu tới khi mục tiêu chết hoặc ra khỏi tầm

**Ràng buộc kỹ thuật:** mọi truy vấn input đi qua interface `IPlayerInput`. Không gọi trực tiếp `Input.GetKey` trong tầng gameplay.

---

## 4. NHÂN VẬT

Mỗi nhân vật có một vũ khí đặc trưng cố định, một đặc điểm di chuyển, một năng lực bị động. Vũ khí không thay thế được — nó biến đổi qua thẻ nâng cấp.

### 4.1 Bảng nhân vật

| | THẠCH SANH | GIÓNG | TẤM |
|---|---|---|---|
| **Nguyên mẫu** | Người hùng dân gian | Cậu bé hóa khổng lồ | Lọ Lem Việt Nam |
| **Vũ khí** | Đàn bầu | Roi sắt | Sáo trúc |
| **Kiểu tấn công** | Sóng âm lan vòng tròn quanh người | Quét vòng cung tầm gần | Tia liên tục tầm trung |
| **Máu** | 6 | 10 | 4 |
| **Tốc độ** | 5 | 3 | 8 |
| **Tầm** | 4 (vòng tròn) | 2.5 (cung 120°) | 7 (tia) |
| **Chu kỳ bắn** | 0.9s | 0.6s | 0.12s |
| **Bị động** | Sóng âm đẩy lùi quái | Mỗi 10 quái hạ được: +5% kích thước, +5% sát thương (tối đa ×10) | Hồi chiêu lướt 0.4s; sát thương ×2 trong 1s sau lướt |
| **Lối chơi** | Đứng giữa đám đông | Áp sát, chịu đòn | Cơ động liên tục, không đứng yên |
| **Độ khó** | Dễ | Trung bình | Khó |

### 4.2 Nhân vật mở rộng

| Nhân vật | Vũ khí | Định hướng |
|---|---|---|
| Cuội | Rìu hồi lực | Chỉ số ngẫu nhiên mỗi ván, tỉ lệ thẻ hiếm cao |
| Mị Châu | Nỏ liên châu | Tầm cực xa, sát thương cực cao, máu 3 |
| Sơn Tinh | Cột đá | Dựng vật cản, tốc độ 2, máu 14 |

### 4.3 Ràng buộc nội dung

Nhân vật chỉ lấy từ truyền thuyết, thần thoại và truyện cổ tích. Không sử dụng nhân vật lịch sử có thật.

---

## 5. THẺ NÂNG CẤP

### 5.1 Cấu trúc bể thẻ

| Nhóm | Số lượng | Nội dung |
|---|---|---|
| Chỉ số | 12 | +25% tốc bắn · +20% sát thương · +1 máu · +15% tốc chạy · −10% hồi chiêu lướt |
| Cải biến vũ khí | 18 | Xuyên thấu · Nảy tường · +2 đạn · Nổ khi trúng · Tách đạn · Bám mục tiêu · Vệt cháy |
| Riêng nhân vật | 6 × 3 | Khóa theo nhân vật đang chơi |
| **Tổng** | **48** | |

### 5.2 Quy tắc

- Hiệu ứng cộng dồn và kết hợp được. Chọn lặp cùng hướng tạo ra thay đổi về chất.
- Thẻ hiếm chiếm 10% bể, viền vàng, lợi ích lớn kèm bất lợi rõ ràng.
- 2 lượt đổi thẻ mỗi ván. Có 1 thẻ cộng thêm lượt đổi. Ngọc mở tối đa 4 lượt.
- Thẻ đã chọn hiển thị trong HUD suốt ván.

### 5.3 Thẻ hiếm — mẫu

| Tên | Hiệu ứng | Bất lợi |
|---|---|---|
| Nỏ Thần | Sát thương ×2 | Tốc bắn −50% |
| Hồn Lìa | Tốc chạy ×1.5 | Máu tối đa −2 |
| Lửa Thiêng | Đạn gây cháy lan | Người chơi mất máu khi đứng trong lửa |

### 5.4 Ví dụ phân nhánh build

```
TẤM — Sáo trúc (tia đơn, 0.12s)
├─ +2 đạn ×3          → 7 tia tỏa quạt, kiểm soát diện rộng
├─ Xuyên thấu ×3      → tia xuyên hàng, sát thương dồn
└─ Nổ khi trúng ×3    → nổ dây chuyền
```

---

## 6. KẺ ĐỊCH

### 6.1 Bảng quái

| Quái | Máu | Tốc độ | Hành vi |
|---|---|---|---|
| Cô Hồn | 10 | 3 | Truy đuổi trực tiếp |
| Ma Trơi | 8 | 2 | Bay, giữ khoảng cách, bắn đạn chậm |
| Bù Nhìn | 40 | 1.5 | Truy đuổi chậm, sát thương va chạm cao |
| Ma Da | 12 | 6 | Lao vọt theo nhịp 2s |
| Quỷ Nhỏ | 15 | 2.5 | Chết thì tách thành 2 con Cô Hồn |

Hành vi triển khai bằng FSM đơn giản. Độ thử thách đến từ thành phần đợt, không từ trí thông minh cá thể.

### 6.2 Đọc hiểu bằng thị giác

| Đặc điểm | Quy ước |
|---|---|
| Kích thước nhỏ, màu sáng | Nhanh, ít máu |
| Kích thước lớn, màu tối | Chậm, nhiều máu |
| Viền sáng nhấp nháy | Sắp thực hiện đòn tấn công |

### 6.3 Trùm — Chằn Tinh

Mãng xà khổng lồ. Máu 800. Hai giai đoạn, chuyển tại 50% máu.

| Giai đoạn | Đòn tấn công |
|---|---|
| 1 | Lao thẳng theo đường dài · Phun 5 viên đạn hình quạt |
| 2 | Triệu hồi 4 Ma Da mỗi 8s · Quét đuôi 360° |

---

## 7. AI ĐẠO DIỄN TRẬN ĐẤU

### 7.1 Chức năng

Sinh thành phần từng đợt theo thời gian thực dựa trên hiệu suất người chơi, thay cho bảng đợt cố định.

### 7.2 Đầu vào — véc-tơ ngữ cảnh

| Trường | Kiểu | Mô tả |
|---|---|---|
| `healthRatio` | float | Tỉ lệ máu còn lại |
| `clearTime` | float | Thời gian dọn đợt trước |
| `hitsTaken` | int | Số lần trúng đòn đợt trước |
| `dashRate` | float | Tần suất lướt |
| `buildType` | enum | AOE / Single / Short / Long |
| `characterId` | enum | Nhân vật đang chơi |

### 7.3 Đầu ra — đặc tả đợt

```
{ enemyMix[], totalCount, spawnDirections[], spawnCadence }
```

### 7.4 Hàm mục tiêu

Giữ tỉ lệ máu mất mỗi đợt trong dải **15–25%**.

```
reward = 1 − |healthLost − 0.20| / 0.20
```

### 7.5 Thuật toán

**Contextual Multi-Armed Bandit — LinUCB.** Mỗi đặc tả đợt khả dĩ là một cánh tay. Hồi quy ridge cho từng cánh tay, chọn theo cận trên tin cậy.

### 7.6 Lớp ràng buộc an toàn

Luật cứng, ghi đè chính sách học được:
- Số quái đồng thời ≤ 40
- Không sinh quái trong bán kính 3 quanh người chơi
- Khi `healthRatio` < 0.25: giảm 30% số lượng đợt kế tiếp
- Mọi đợt phải có ít nhất một loại quái tầm gần

### 7.7 Ma trận phản ứng

| Trạng thái người chơi | Phản ứng |
|---|---|
| Build AOE | Ít quái, máu cao, phân tán |
| Build đơn mục tiêu | Nhiều quái, máu thấp, tập trung |
| Nhân vật Tấm | Tăng tỉ lệ Ma Trơi (ép né đạn) |
| Nhân vật Gióng | Tăng số hướng xuất hiện (ép xoay trở) |
| Dọn đợt quá nhanh | Tăng số lượng, giảm giãn cách |
| Máu thấp | Giảm áp lực |

### 7.8 Telemetry và đánh giá

Ghi log mọi quyết định kèm kết quả ra tệp cục bộ. Giao diện debug hiển thị ngữ cảnh, hành động chọn và phần thưởng.

**Phiên bản đối chứng:** bảng đợt cố định, dùng cho thực nghiệm so sánh 15 người / 15 người.

---

## 8. TIẾN TRÌNH

### 8.1 Tiền tệ

**Ngọc** — nhận cuối ván, giữ vĩnh viễn. Không có tiền tệ trong ván.

| Nguồn | Số lượng |
|---|---|
| Sống sót mỗi 4 đợt | 5 |
| Hạ trùm | 25 |
| Lần đầu phá đảo mỗi nhân vật | 40 |

### 8.2 Bảng mở khóa

| Mục | Giá |
|---|---|
| Nhân vật Gióng | 30 |
| Nhân vật Tấm | 60 |
| Gói thẻ mới (×5) | 20 / 35 / 50 / 75 / 100 |
| Lượt đổi thẻ thứ 3 | 40 |
| Lượt đổi thẻ thứ 4 | 80 |

### 8.3 Cấp độ khó

Mở tuần tự bằng cách phá đảo cấp trước.

| Cấp | Máu quái | Số lượng | Ngọc thưởng |
|---|---|---|---|
| 1 | ×1.0 | ×1.0 | ×1.0 |
| 2 | ×1.3 | ×1.2 | ×1.3 |
| 3 | ×1.6 | ×1.4 | ×1.6 |
| 4 | ×2.0 | ×1.6 | ×2.0 |

---

## 9. CHƠI MẠNG

| Hạng mục | Đặc tả |
|---|---|
| Số người | 2 |
| Mô hình | Host-authoritative |
| Thư viện | Mirror + Steamworks.NET |
| Vào phòng | Steam invite hoặc mã 6 ký tự |
| Không hỗ trợ | Ghép trận tự động, phòng chờ, bảng xếp hạng |
| Máu | Độc lập từng người |
| Thẻ | Chọn độc lập; 6 thẻ có hiệu ứng cho cả hai |
| Lượt đổi thẻ | Độc lập, 2 lượt mỗi người |
| Hạ gục | Trạng thái chờ cứu; đồng đội đứng cạnh 3s để hồi sinh |
| Thua | Khi cả hai bị hạ gục |
| Trần độ trễ | 150ms không mất đồng bộ |

**Nguyên tắc:** chế độ chơi đơn hoàn chỉnh và tự đủ. Co-op không là điều kiện cho bất kỳ tiến trình nào.

**Phương án dự phòng:** co-op cục bộ 2 tay cầm trên một máy.

---

## 10. GIAO DIỆN

### 10.1 Sơ đồ luồng

```
MÀN HÌNH CHÍNH
├── Chơi ngay ──────────→ Chọn nhân vật ──→ Đấu trường
├── Chơi với bạn ──→ Tạo/Vào phòng ──→ Chọn nhân vật (chung) ──→ Đấu trường
├── Bộ sưu tập thẻ
└── Cài đặt
```

### 10.2 Màn chọn nhân vật

Hiển thị: chân dung · tên · vũ khí · máu · tốc độ · bị động · một dòng mô tả lối chơi.

Nhân vật khóa hiện mờ kèm giá Ngọc, mở khóa tại chỗ.

Ở co-op: hiển thị lựa chọn của người kia theo thời gian thực. Cho phép trùng nhân vật. Cả hai bấm Sẵn sàng để bắt đầu.

### 10.3 HUD trong trận

```
┌────────────────────────────────────────┐
│ ❤❤❤❤♡              ĐỢT 7/16           │
│                                        │
│                  🧍                    │
│                                        │
│ [🃏🃏🃏🃏]                    [⚡Dash] │
└────────────────────────────────────────┘
```

Góc trái: máu · Giữa: tiến độ đợt · Dưới trái: thẻ đã chọn · Dưới phải: trạng thái hồi chiêu lướt

### 10.4 Màn chọn thẻ

Ba thẻ ngang. Thẻ hiếm viền vàng phát sáng. Nút đổi thẻ hiển thị số lượt còn lại, mờ khi hết.

### 10.5 Ràng buộc UX

Thời gian từ khởi động tới khung hình chiến đấu đầu tiên ≤ 20 giây.

---

## 11. MỸ THUẬT

### 11.1 Định hướng

| Yếu tố | Nguồn |
|---|---|
| Bảng màu | Tranh Đông Hồ — đỏ son, vàng hoè, xanh chàm, đen than, trắng điệp |
| Hoa văn UI | Trống đồng Đông Sơn — vòng tròn đồng tâm, chim lạc |
| Kiến trúc | Đình làng Bắc Bộ |

### 11.2 Thông số kỹ thuật

```
Nhân vật      32×32 px
Tile          16×16 px
PPU           16
Bảng màu      24 màu, khóa cứng
Animation     4 frame/hành động
Filter Mode   Point (no filter)
Compression   None
```

### 11.3 Bối cảnh

| Đợt | Đấu trường |
|---|---|
| 1–5 | Sân Đình |
| 6–10 | Ruộng Lúa |
| 11–16 | Âm Phủ |

Kích thước đấu trường: 32×18 tile. 3–5 vật cản tĩnh.

### 11.4 Danh mục tài nguyên

| Hạng mục | Số lượng |
|---|---|
| Nhân vật (3 × idle/chạy/tấn công) | 36 frame |
| Quái (5 × idle/di chuyển) | 20 frame |
| Trùm | 12 frame |
| Đạn | 5 sprite |
| Hiệu ứng va chạm / tiêu diệt | 5 bộ |
| Ảnh nền | 3 |
| Icon thẻ | 48 |
| Chân dung nhân vật | 3 |
| Icon UI | 15 |

### 11.5 Phản hồi chiến đấu

Bắt buộc, tính là yêu cầu chức năng:

| Sự kiện | Phản hồi |
|---|---|
| Đạn trúng quái | Hạt va chạm · số sát thương · hit-stop tỉ lệ sát thương |
| Quái bị tiêu diệt | Nổ hạt · mảnh vỡ có hướng · rung màn hình tỉ lệ kích thước · âm thanh phân lớp |
| Người chơi trúng đòn | Nhấp nháy đỏ · rung mạnh · âm trầm |

Cường độ rung màn hình và hit-stop tùy chỉnh được, bao gồm tắt hoàn toàn.

### 11.6 Quy định sử dụng công cụ AI

| Được phép | Không được phép |
|---|---|
| Ảnh nền tĩnh | Sprite trong game |
| Chân dung nhân vật | Animation |
| Hoa văn, khung viền | Hiệu ứng |
| Poster, capsule Steam | Icon thẻ |

Khai báo trên Steam: *"Ảnh nền và tranh minh họa có sử dụng công cụ AI hỗ trợ. Toàn bộ sprite, animation và hiệu ứng do nhóm thực hiện."*

---

## 12. ÂM THANH

**Nhạc nền:** đàn bầu, sáo trúc, trống chầu phối cùng nhạc điện tử. Ba bản theo ba bối cảnh, một bản cho trùm.

**Hiệu ứng:** phân lớp — lớp va chạm, lớp vật liệu, lớp không gian. Mỗi loại quái có âm tiêu diệt riêng.

**Nhịp âm thanh:** cường độ nhạc tăng theo số đợt đã qua.

---

## 13. KỸ THUẬT

### 13.1 Nền tảng

| | |
|---|---|
| Engine | Unity 6.3 LTS, URP 2D |
| Ngôn ngữ | C# |
| Mạng | Mirror, Steamworks.NET |
| Lưu trữ | JSON cục bộ, có phiên bản hóa |
| Quản lý mã | Git + Git LFS |
| Công cụ art | Aseprite |

### 13.2 Kiến trúc

- Nội dung định nghĩa bằng ScriptableObject: nhân vật, thẻ, quái, tham số đợt
- Thêm nhân vật hoặc thẻ mới không sửa mã gameplay
- Tách logic gameplay khỏi tầng trình bày để kiểm thử đơn vị không cần khởi tạo scene
- Object pooling cho toàn bộ đạn, quái, hiệu ứng, số sát thương
- Sinh ngẫu nhiên tất định theo seed

### 13.3 Ngân sách hiệu năng

| Chỉ tiêu | Ngưỡng |
|---|---|
| Khung hình | 60 FPS ổn định |
| Thực thể đồng thời | ≤ 200 |
| Độ trễ input | ≤ 1 frame |
| Draw call | ≤ 100 |

### 13.4 Sẵn sàng cho mobile

- Toàn bộ input qua `IPlayerInput`
- Không hardcode chuỗi văn bản
- Sprite atlas nén ASTC
- UI dùng anchor co giãn, vùng chạm ≥ 44dp

---

## 14. PHẠM VI SẢN XUẤT

### 14.1 Bắt buộc

Chiến đấu cốt lõi · 3 nhân vật · 48 thẻ · 5 loại quái · Trùm Chằn Tinh · 16 đợt · 3 bối cảnh · AI Đạo Diễn · Co-op 2 người · Lưu trữ và mở khóa · 4 cấp độ khó

### 14.2 Ngoài phạm vi

Nhân vật thứ 4 trở đi · Trùm thứ 2 · Co-op 4 người · Ghép trận tự động · Cửa hàng trang trí · Thử thách hằng ngày · Bản mobile · Cutscene

### 14.3 Bổ sung theo thứ tự ưu tiên

1. Nhân vật Cuội
2. Mini-boss Đại Bàng Tinh (đợt 8)
3. Trùm Thuồng Luồng
4. LLM sinh lore thẻ hiếm

---

## 15. LỘ TRÌNH

| Tuần | Hạng mục | Cột mốc |
|---|---|---|
| 1–3 | Di chuyển, lướt, tự động bắn, 1 quái, 1 đấu trường, lớp input | **Cổng 1:** cảm giác chiến đấu đạt yêu cầu |
| 4–5 | Hệ thống thẻ, 30 thẻ chung, màn chọn thẻ | Build phân nhánh rõ rệt qua 10 đợt |
| 6–7 | 3 nhân vật, 18 thẻ riêng, màn chọn nhân vật | **Cổng 2:** 3 nhân vật khác biệt về lối chơi |
| 8–9 | 5 quái, phản hồi chiến đấu, trùm, 16 đợt | Ván chơi hoàn chỉnh |
| 10–12 | Co-op 2 người | 2 máy hoàn thành ván không mất đồng bộ |
| 13 | Telemetry, AI Đạo Diễn, phiên bản đối chứng | Dữ liệu so sánh |
| 14 | Mở khóa, lưu trữ, cân bằng | Thử nghiệm 10 người ngoài nhóm |
| 15 | Sửa lỗi, trailer | |
| 16 | Demo Steam, tài liệu | Bàn giao |

### 15.1 Phân công

| Vai trò | Phạm vi |
|---|---|
| Gameplay | Điều khiển, nhân vật, quái, trùm, phản hồi chiến đấu |
| Hệ thống & AI | Thẻ nâng cấp, cân bằng, telemetry, AI Đạo Diễn |
| Mạng & UI | Co-op, giao diện, pipeline mỹ thuật, marketing (20% thời lượng) |

### 15.2 Cổng chất lượng

**Cổng 1 — tuần 3.** Vòng lặp chiến đấu phải đạt cảm giác thỏa mãn qua thử nghiệm có cấu trúc. Không đạt: dừng toàn bộ phát triển tính năng.

**Cổng 2 — tuần 7.** Ba nhân vật phải khác biệt về lối chơi, không chỉ về chỉ số. Không đạt: rút xuống hai nhân vật.

**Điểm quyết định — tuần 12.** Co-op online không hoạt động: chuyển sang co-op cục bộ.

---

## 16. BỐI CẢNH

Lạc Long Quân — rồng — kết duyên cùng Âu Cơ — tiên. Trăm trứng nở thành trăm người con, tổ tiên người Việt.

Trống Đồng giữ cân bằng giữa các cõi. Quỷ Vương đoạt lấy, ranh giới vỡ ra, ma quỷ tràn lên dương gian.

Người chơi là một trong trăm người con.

**Phương thức kể:** một màn hình văn bản bốn dòng khi khởi động. Một câu thoại trước trận trùm. Không cutscene, không lồng tiếng.

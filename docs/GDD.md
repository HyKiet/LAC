# LẠC
## *Children of the Dragon*

**Game Design Document**

> Cập nhật 04/10/2026: một ván có **15 đợt tổng cộng**, gồm 14 đợt thường và Chằn Tinh ở đợt 15; có 14 lượt chọn thẻ. Mỹ thuật ThirdParty đang dùng để thử nghiệm được mô tả tại mục 11.7. Các nội dung khác chưa cập nhật theo triển khai vẫn theo quyết định hiện hành trong `CLAUDE.md`.

> Quyết định 07/10/2026: bản đồ án dùng **Warrior, Archer, Lancer** từ TinySwords thay cho Thạch Sanh, Gióng, Tấm. Dùng asset sẵn để giảm công mỹ thuật; giữ co-op host-authoritative, 15 wave, thẻ, Trống Đồng, Hồn và telemetry. AI Đạo Diễn và thực nghiệm so sánh vẫn là phần cần hoàn thành để đánh giá đề tài, không bị thay bằng màn chọn nhân vật.

| | |
|---|---|
| Thể loại | Arena Survival Roguelite |
| Góc nhìn | Top-down 2D, pixel art |
| Nền tảng | PC (Steam) |
| Người chơi | 1–2 (co-op online) |
| Thời lượng ván | 15 phút |
| Engine | Unity 6000.5.6f1 · URP 2D · C# |
| Giá | $2.99 |

---

## 1. TỔNG QUAN

### 1.1 High Concept

Người chơi chọn Warrior, Archer hoặc Lancer, chiến đấu qua 15 đợt quái trong một đấu trường khép kín để giành lại Trống Đồng. Bối cảnh và quái giữ cảm hứng thần thoại Việt Nam; bộ ba người chơi là các lớp chiến đấu dùng mỹ thuật TinySwords. Vũ khí tự động khai hỏa; người chơi kiểm soát vị trí và thời điểm lướt. Sau mỗi đợt thường, người chơi chọn một trong ba thẻ nâng cấp. Hạ trùm ở đợt 15 kết thúc ván ngay, không mở lượt chọn thẻ thứ 15.

### 1.2 Pitch

> Arena survival roguelite trên nền thần thoại Việt Nam. Ba anh hùng, ba lối chơi. Mỗi ván là một cách xây dựng sức mạnh.

### 1.3 Điểm khác biệt

| | |
|---|---|
| **Nhân vật định hình lối chơi** | Vũ khí cố định theo nhân vật, không thay thế được. Đổi nhân vật là đổi game |
| **Bản sắc thị giác** | Nền tối, hoa văn Đông Sơn và Trống Đồng; người chơi dùng TinySwords, vũ khí kiếm/cung/thương |
| **AI Đạo Diễn** | Hệ thống sinh đợt thích ứng theo hành vi người chơi theo thời gian thực |

### 1.4 Đối tượng

Người chơi 15–30 tuổi quen thuộc với roguelite, tìm phiên chơi ngắn. Thị trường mục tiêu: toàn cầu qua Steam, trọng tâm ban đầu là Việt Nam và Đông Nam Á.

### 1.5 Tham chiếu

**Brotato** — cấu trúc đợt, tự động tấn công · **Soul Knight** — cảm giác bắn, thiết kế nhân vật · **Vampire Survivors** — đường cong sức mạnh · **Hades** — hệ thống nâng cấp theo lượt chọn

---

## 2. CORE LOOP

### 2.1 Vòng trong ván

```
ĐỢT CHIẾN ĐẤU (30–50s)  →  CHỌN THẺ (10s)  →  lặp ×14  →  TRÙM (đợt 15)
```

**Đợt chiến đấu.** Quái xuất hiện từ mép đấu trường theo nhịp do AI Đạo Diễn quyết định. Vũ khí tự bắn. Đợt kết thúc khi dọn sạch quái. Quái không rơi vật phẩm.

**Chọn thẻ.** Tạm khoá di chuyển/lướt/tấn công của player trong 10 giây; đồng hồ mạng vẫn chạy. Ba thẻ hiện ra, chọn một; hết hạn host chọn thay. Mỗi ván có 2 lượt đổi thẻ. Co-op chờ cả hai hoàn tất.

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

### 4.1 Bảng nhân vật hiện hành

| | WARRIOR | ARCHER | LANCER |
|---|---|---|---|
| **Sprite** | Yellow Warrior | Blue Archer | Purple Lancer |
| **Vũ khí** | Kiếm | Cung / mũi tên | Thương |
| **Kiểu tấn công** | Quét cung 110° hướng tới quái gần nhất | Tên bay thẳng, trúng mục tiêu đầu tiên | Đâm xuyên tất cả quái trong dải hẹp phía trước |
| **Máu** | 10 | 6 | 8 |
| **Tốc độ** | 4 | 5.5 | 4.5 |
| **Tầm** | 2.2 | 8 | 3.2; dải rộng 0.8 |
| **Sát thương gốc** | 4 | 4 | 5 |
| **Chu kỳ** | 0.65s | 0.7s | 0.85s |
| **Hồi dash** | 1s | 0.8s | 1s |
| **Đặc điểm** | Chịu đòn và quét nhiều quái gần | Tên kế tiếp sau dash ×2 sát thương; tốc tên 16 đơn vị/s | Xuyên hàng có sẵn; không bắn đạn |
| **Lối chơi** | Áp sát có kiểm soát | Giữ khoảng cách, né đòn | Canh hướng để nhiều quái nằm cùng hàng |

Cả ba dash 4 đơn vị trong 0.15s. Chỉ số cân bằng khởi đầu nằm trong `Data/Characters/*.asset`, không phải kết quả cân bằng cuối. Thẻ sát thương/tốc đánh/tầm đánh áp dụng cả ba; thẻ yêu cầu đạn chỉ xuất hiện cho Archer.

### 4.2 Thi hành đòn đánh

Chọn quái gần nhất trong tầm, tự xoay hướng tới mục tiêu rồi khai hoả. Warrior/Lancer gây sát thương tức thời ở vùng thể hiện bằng vệt chém/đâm. Quái ngoài tầm, phía sau hoặc ngoài góc/bề rộng không trúng. Archer gây sát thương khi tên chạm quái; tên hết tầm được thu hồi, dùng object pool và không có NetworkIdentity. Client chỉ dựng biểu diễn, sát thương do host quyết định.

Lancer có clip đâm ngang, lên, xuống và hai hướng chéo; hướng trái dùng lật sprite. TinySwords không có Hurt/Death: dùng phản hồi trúng đòn và vòng hồi sinh hiện có, khi gục giữ một khung tĩnh.

### 4.3 Phạm vi bản đồ án

Mở sẵn cả ba lớp, không yêu cầu vẽ lại người chơi hoặc thêm nhân vật thứ tư. Tên và vai trò truyền thuyết cũ là thiết kế lịch sử đã thay thế; không quảng bá Warrior/Archer/Lancer là sprite anh hùng dân gian. Trọng tâm kỹ thuật giữ ở gameplay, co-op, AI Đạo Diễn và đánh giá bằng telemetry.

---

## 5. THẺ NÂNG CẤP

### 5.1 Cấu trúc bể thẻ

| Nhóm | Số lượng | Nội dung |
|---|---|---|
| Thẻ nền hiện có | 12 | Chỉ số và cải biến đạn, lấy từ Data/Cards |
| Tiến hoá mục tiêu | 8 | T-25/T-26 chưa hoàn thành; giữ trong phạm vi đề tài |
| **Tổng mục tiêu** | **20** | 12 nền + 8 tiến hoá; không biên soạn thêm 48 thẻ |

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
ARCHER — Cung (tên đơn, 0.7s)
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

Mãng xà khổng lồ xuất hiện ở đợt 15. Máu 800 khi một người, 1600 khi hai người. Hai giai đoạn, chuyển tại 50% máu.

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
| Archer | Thay đổi tỉ lệ Ma Trơi để tạo tình huống né đạn |
| Warrior / Lancer | Thay đổi hướng xuất hiện để tạo tình huống xoay trở |
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
| Warrior / Archer / Lancer | Mở sẵn cả ba trong bản đồ án |
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
| Vào phòng thử nghiệm | Chọn lớp, tạo host hoặc nhập IP host qua KCP |
| Vào phòng mục tiêu | Steam invite, T-40 chưa hoàn thành |
| Không hỗ trợ | Ghép trận tự động, bảng xếp hạng |
| Máu | Độc lập từng người |
| Thẻ | Chọn độc lập; 6 thẻ có hiệu ứng cho cả hai |
| Lượt đổi thẻ | Độc lập, 2 lượt mỗi người |
| Hạ gục | Trạng thái chờ cứu; đồng đội đứng cạnh 3s để hồi sinh |
| Thua | Khi cả hai bị hạ gục |
| Trần độ trễ | 150ms không mất đồng bộ |

**Nguyên tắc:** chế độ chơi đơn hoàn chỉnh và tự đủ. Co-op không là điều kiện cho bất kỳ tiến trình nào.

Co-op trực tuyến là yêu cầu kỹ thuật của đề tài. Thử nghiệm LAN/IP hiện có chưa thay thế nghiệm thu Steam ngoài LAN.

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

Màn chọn thử nghiệm nằm ngay trong scene Arena trước khi khởi động mạng: ba thẻ ngang, chân dung, tên, vũ khí, máu/tốc độ/sát thương/tầm/chu kỳ và mô tả. Lớp đang chọn được đánh dấu rõ. Wave bằng 0, chưa sinh player hoặc quái.

Chọn lớp rồi bấm **Vào trận / Tạo host**; chơi một người cũng dùng Mirror host. Người thứ hai chọn lớp trên máy mình, nhập IP host rồi bấm **Vào host**. Lựa chọn được gửi trước khi sinh player, host thẩm định ID. Cho phép trùng lớp, người vào muộn tham gia wave đang chạy. Có huỷ kết nối và thử lại. Đây là màn chọn trước trận, chưa có sảnh cùng bấm Sẵn sàng hoặc Steam invite. Chơi lại tại chỗ giữ lớp đã chọn.

### 10.3 HUD trong trận

```
┌────────────────────────────────────────┐
│ [██████░░] 6/8      ĐỢT 7/15           │
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
Nhân vật      TinySwords: frame 192×192 / Lancer 320×320, PPU 64
Tile          32×32 px
PPU           32 (mỹ thuật của nhóm)
Bảng màu      24 màu, khóa cứng
Animation     Dùng số frame gốc của gói; không giới hạn 4 frame
Filter Mode   Point (no filter)
Compression   None
```

### 11.3 Bối cảnh

| Đợt | Đấu trường |
|---|---|
| 1–5 | Sân Đình |
| 6–10 | Ruộng Lúa |
| 11–15 | Âm Phủ — Chằn Tinh ở đợt cuối |

Sân thử nghiệm hiện tại: 36×20 đơn vị thế giới, nền gạch tối và biên va chạm hiện có. Đã bỏ toàn bộ bụi cỏ khỏi ba chặng. Đá rìa sân không có collider, đổi sắc ở đợt 6/11; tileset hoàn chỉnh là công việc sau, không là điều kiện để thử ba lớp.

### 11.4 Danh mục tài nguyên

| Hạng mục | Số lượng |
|---|---|
| Nhân vật (3 × idle/chạy/tấn công) | 36 frame |
| Quái (5 × idle/di chuyển) | 20 frame |
| Trùm | 12 frame |
| Đạn | 5 sprite |
| Hiệu ứng va chạm / tiêu diệt | 5 bộ |
| Ảnh nền | 3 |
| Icon thẻ | 20 mục tiêu (12 nền + 8 tiến hoá) |
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

### 11.7 Asset 2D đang tích hợp — cập nhật 07/10/2026

Đã rà 34 PNG TinyRPG và 410 PNG TinySwords tại `Assets/ThirdParty/`. Đây là bộ mỹ thuật thử nghiệm; bảng màu và mật độ pixel gốc khác chuẩn mỹ thuật cuối cùng của nhóm.

| Vai trò | Asset chọn | Lý do |
|---|---|---|
| Warrior | TinySwords Yellow Warrior, Idle/Run/Attack1 | Kiếm quét cung, giáp vàng dễ nhận ra |
| Archer | TinySwords Blue Archer, Idle/Run/Shoot/Arrow | Cung và tên đúng hình dạng đòn đánh |
| Lancer | TinySwords Purple Lancer, Idle/Run/5 hướng Attack | Đâm thương theo hướng mục tiêu, áo tím phân biệt hai lớp còn lại |
| Cô Hồn | Giữ TinyRPG Orc | Có Walk/Hurt/Death và thân nhỏ, đọc được khi nhiều quái |
| Trang trí sân | TinySwords Rocks | Đá thưa ở rìa sân, làm tối, nằm dưới nhân vật và đạn, không collider; đã gỡ Bushes |
| Quái khác, Chằn Tinh, sóng âm, Trống Đồng | Giữ tài nguyên của nhóm | Gói ngoài không có hình tương ứng phù hợp thần thoại và vai trò chiến đấu |

Warrior: 8 khung đứng, 6 chạy, 4 đánh; Archer: 6 đứng, 6 chạy, 8 bắn; Lancer: 12 đứng, 6 chạy, 3 khung cho mỗi hướng đâm. Animation lấy trực tiếp từ sprite đã slice, giữ import gốc. Không đổi hitbox theo vùng trong suốt của sheet 192×192/320×320.

Prefab `ArenaStageDecor` giữ ba nhóm đá: tông ấm (1–5), xanh trầm (6–10), xanh tối (11–15). Chặng lấy từ số wave đã đồng bộ nên người vào muộn và chơi lại hiển thị đúng. Đây là trang trí thử nghiệm, chưa thay thế ba tileset hoàn chỉnh.

Không nhập nguyên nhà châu Âu, cây lớn, cừu, vịt hoặc hiệu ứng nổ/lửa sáng vào sân. Không sửa ảnh và metadata gốc trong ThirdParty. Chi tiết phân tích, tham chiếu và thay thế asset: [ASSETS_ThirdParty.md](ASSETS_ThirdParty.md).

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
| Engine | Unity 6000.5.6f1, URP 2D |
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

Chiến đấu cốt lõi · 3 nhân vật · 48 thẻ · 5 loại quái · Trùm Chằn Tinh · 15 đợt · 3 bối cảnh · AI Đạo Diễn · Co-op 2 người · Lưu trữ và mở khóa · 4 cấp độ khó

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
| 8–9 | 5 quái, phản hồi chiến đấu, trùm, 15 đợt | Ván chơi hoàn chỉnh |
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

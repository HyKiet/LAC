# Rà soát cân bằng — 02/10/2026

Phạm vi đã xác nhận: **chỉ chỉnh Cards; phần chiến đấu, đợt quái và VFX chỉ báo cáo**.
Không thay `CharacterData`, `EnemyData`, bảng đợt, dash, hiệu ứng hoặc scene trong lượt này.
T-32, T-50 và T-51 vẫn chưa hoàn tất.

## 1. Cân bằng thẻ: sửa lựa chọn có thể làm giảm DPS

Cuồng Nộ trước đây cộng +25% sát thương gốc và trừ 10% tốc đánh gốc mỗi cấp.
Khi đã có Cường Công ×3, Thiết Bích ×2 và tiến hoá Thánh Gióng, lấy Cuồng Nộ cấp hai
làm hệ số DPS lý thuyết giảm **2,025 → 2,000**. Đây là bất lợi do cộng dồn, không phải
chỉ một lựa chọn kém hiệu quả trong hoàn cảnh cụ thể.

Đã giảm phần phạt tốc đánh của Cuồng Nộ từ **−10% xuống −8% mỗi cấp**, giữ nguyên
sát thương, giới hạn hai cấp, trọng số và công thức tiến hoá. Build hồi quy trên giờ tăng
**2,070 → 2,100**. Thay đổi nằm trong dữ liệu Cards và trình dựng dữ liệu demo.

Kiểm tra Edit Mode dùng `PlayerUpgradeState` và danh mục tám tiến hoá thật:

- 1.152 tổ hợp cộng dồn trên ba nhân vật đều tăng hệ số DPS sau khi nhận Cuồng Nộ;
  mức tăng nhỏ nhất là **0,030 lần DPS gốc** trong các trường hợp đã thử.
- Hồi quy bể 12 thẻ: 45.000 lựa chọn mô phỏng, giới hạn cộng dồn, lọc vũ khí, giữ
  sát thương phần lẻ, reset và không ghi đè asset.
- Trần **riêng thẻ nền** Cường Công ×3 + Liên Kích ×3 + Cuồng Nộ ×2 là
  **2,709× DPS gốc**; thêm Song Tiễn là **3,7926×** nếu cả hai đạn đều trúng.
  Các con số này chưa cộng bonus tiến hoá, xuyên và nổ.

Đây là kiểm tra công thức/cộng dồn, **không phải phép đo DPS trong trận**. Trượt đạn,
overkill, mật độ quái, tầm đánh, hình dạng đòn và thao tác người chơi vẫn ảnh hưởng
kết quả. Không dùng các kiểm tra này để tuyên bố toàn game đã cân bằng.

Nguồn: [`CuongNo.asset`](../Assets/_LAC/Data/Cards/Resources/Cards/CuongNo.asset),
[`CardBalanceChecks.cs`](../Assets/_LAC/Scripts/Cards/Editor/CardBalanceChecks.cs),
[`PlayerUpgradeState.cs`](../Assets/_LAC/Scripts/Cards/PlayerUpgradeState.cs),
[danh mục thẻ](CARDS.md).

## 2. Ba nhân vật: baseline cần đo trong trận — @Kiet / T-50

| Nhân vật | Sát thương / chu kỳ | DPS lý thuyết trên một mục tiêu | Phạm vi đòn |
|---|---|---:|---|
| Thạch Sanh | 2 / 0,9 s | 2,22 | Vòng tròn bán kính 4; có thể đánh nhiều quái |
| Gióng | 3 / 0,6 s | 5,00 | Cung 110°, bán kính 3,2; có thể đánh nhiều quái |
| Tấm | 1 / 0,12 s | 8,33 | Đạn một mục tiêu trước nâng cấp; tầm 7 |

Tấm có phần thưởng ×2 sát thương cho **loạt bắn tiếp theo** sau dash, không phải
nhân đôi DPS liên tục. Thạch Sanh và Gióng gây sát thương diện rộng nên không thể
chốt cân bằng bằng cách đưa ba con số DPS đơn mục tiêu về bằng nhau.
Quái hiện có trong bảng là Cô Hồn: 10 máu, tốc độ 2,2, sát thương chạm 1.

Đề nghị đo cùng seed/vị trí cho ba nhân vật ở nhóm quái thưa, nhóm quái dày và boss:
thời gian dọn, sát thương thực, số mục tiêu/đòn, máu mất và tỉ lệ thắng. So sánh build
không thẻ, build điển hình và build mạnh ở cùng số lượt chọn; chỉ chốt sát thương gốc
sau khi đủ loại quái/boss. **Chưa đổi các chỉ số nhân vật trong lượt này.**

Nguồn: [dữ liệu nhân vật](../Assets/_LAC/Data/Characters/),
[`CoHon.asset`](../Assets/_LAC/Data/Enemies/CoHon.asset),
[`WeaponAuto.cs`](../Assets/_LAC/Scripts/Combat/WeaponAuto.cs).

## 3. Đường cong 16 đợt: nhịp sinh không phải thời lượng đợt — @Kiet / T-51

Số quái hiện hành theo đợt:
`6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 33, 36, 40` — tổng **343 Cô Hồn**.
Đợt 16 có nhãn Chằn Tinh nhưng dữ liệu vẫn chỉ sinh Cô Hồn; boss thuộc T-39.

Nhịp danh nghĩa `Burst / SpawnInterval` tăng từ khoảng 0,417 lên 3,333 quái/giây.
Riêng đợt 2 → 3 tăng **0,455 → 0,769 quái/giây, khoảng +69%**, đồng thời tăng từ
một lên hai hướng vào sân. Đây là điểm cần kiểm tra khả năng vượt qua, không tự
động kết luận là quá khó chỉ từ dữ liệu.

Theo lịch `floor((count−1)/Burst) × SpawnInterval`, con cuối vào sân sau **10,4–16,8 s**
tính từ đầu đợt. Đợt chỉ kết thúc khi sinh hết và không còn quái sống; vì vậy thời gian
này **không phải** thời lượng chiến đấu. Mục tiêu 30–50 s/đợt và khoảng 15 phút/ván
chưa được kiểm chứng bằng chơi đủ ván.

Đề nghị ghi thời điểm bắt đầu/kết thúc, quái sống cao nhất, máu mất, build và nguyên nhân
thua từng đợt; kiểm điểm đổi Burst/hướng sinh trước khi chỉnh tổng số quái. Sau T-34/T-39
mới đánh giá lại thành phần đợt và co-op; không bù độ mạnh của build bằng tăng máu quái
trái quyết định điều tiết bất đối xứng trong CLAUDE.md.

Nguồn: [`WaveTable_CoDinh.asset`](../Assets/_LAC/Data/Waves/WaveTable_CoDinh.asset),
[`WaveManager.cs`](../Assets/_LAC/Scripts/Core/WaveManager.cs),
[ràng buộc thiết kế](../CLAUDE.md).

## 4. Dash và đọc hiểu: rủi ro ngoài Cards cần bàn giao

- **Dash/co-op — @Kiet:** hồi gốc 0,4 s, thời gian dash/i-frame 0,15 s. Bộ Pháp +
  Lạc Phong giảm hồi còn 0,26 s. Host bù nửa RTT tối đa 0,2 s vào i-frame và nới
  điều kiện nhận dash. Khi phần bù lớn, cửa sổ bảo vệ có thể chồng nhau; cần đo tần
  suất dash được host chấp nhận và thời gian thực sự có thể nhận sát thương ở các
  mức RTT. Không tự giảm sức mạnh Lạc Phong để che vấn đề xử lý mạng.
- **T-32 — @Kiet + artist:** baseline 40 quái/200 đạn đạt P95 toàn khung
  **3,90–7,45 ms** trong Editor, nhưng còn hitch, cảnh báo DamageNumber vượt ngưỡng
  128 và hình ảnh đạn/số sát thương/sóng chồng che nhân vật. Chưa sửa phần này và
  chưa đủ điều kiện đóng T-32; xem [số liệu, ảnh và giới hạn phép đo](T32_TESTS.md).

Nguồn dash: [`PlayerDash.cs`](../Assets/_LAC/Scripts/Player/PlayerDash.cs),
[`LacPhong.asset`](../Assets/_LAC/Data/Cards/EvolutionBonuses/LacPhong.asset).

## 5. Điều kiện để chốt cân bằng toàn game

Ưu tiên bàn giao: kiểm dash/RTT và T-32 → bổ sung quái/boss → thu telemetry T-43 →
chạy đủ 16 đợt với ba nhân vật, nhiều build và cả host một người/co-op hai người.
Cần ghi tỉ lệ chọn thẻ, tiến hoá, thắng/thua và máu mất theo đợt để phân biệt thẻ mạnh
với vũ khí hoặc nhịp quái chưa cân bằng. **T-50/T-51 giữ `[ ]`** cho tới khi có bằng
chứng thực chiến phù hợp; báo cáo này không thay nghiệm thu hai hạng mục đó.

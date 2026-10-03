# Tác động thẻ rõ hơn, giữ trần sức mạnh — T-24B

Phạm vi theo yêu cầu: Cards và bộ kiểm Cards. Không đổi sát thương gốc, quái,
đợt, dash/i-frame, Combat/VFX hoặc scene. T-32/T-50/T-51 vẫn mở.

## Thiết kế

Sáu thẻ dồn lợi ích vào cấp đầu, cấp sau nhỏ hơn. Trần, số cấp, trọng số và số
lượt chọn không đổi; không nhân lũy tiến lên chỉ số đã tăng. Curves được lưu
trong `CardDefinition._statScales`, không nhánh hard-code theo ID trong gameplay.

| Thẻ | Cấp đầu cũ → mới | Các cấp sau mới | Tổng khi đủ cấp |
|---|---|---|---|
| Cường Công | +20% → +30% sát thương | +15%, +15% | +60% |
| Liên Kích | +15% → +22,5% tốc đánh | +11,25%, +11,25% | +45% |
| Khinh Thân | +8% → +12% tốc đi bộ | +6%, +6% | +24% |
| Âm Vang | +10% → +15% tầm | +7,5%, +7,5% | +30% |
| Thiết Bích | +15% → +22,5% bảo vệ sau trúng đòn | +7,5% | +30% |
| Cuồng Nộ | +25%/−8% → +37,5% sát thương/−12% tốc đánh | +12,5%/−4% | +50%/−16% |

Giữ Sinh Lực +20% mỗi cấp: chia lại tỷ lệ máu có thể làm cấp thứ hai của Tấm
không tăng ô máu vì làm tròn tổng. Hồi Xuân vẫn 1 máu/cấp/đợt; Bộ Pháp vẫn −20%
hồi lướt. Không tăng hồi phục hay giảm thêm dash để tạo cảm giác mạnh giả.
Song Tiễn/Xuyên Tâm/Bộc Phá và tám bonus tiến hoá không đổi.

## Mức tác động cụ thể

Với quái 10 máu, chỉ Cường Công cấp đầu, không dash/tiến hoá:

| Nhân vật | Sát thương không thẻ → cấp đầu | Số đòn không thẻ → cấp đầu mới |
|---|---|---|
| Thạch Sanh | 2 → 2,6 | 5 → 4 |
| Gióng | 3 → 3,9 | 4 → 3 |
| Tấm | 1 → 1,3 | 10 → 8 |

Thạch Sanh cũ +20% vẫn cần 5 đòn; Tấm cũ cần 9 đòn. Gióng đã chạm mốc 3 đòn
với cấp đầu cũ; không dùng bảng này để kết luận ba vũ khí đã cân bằng nhau.

Liên Kích cấp đầu trên Gióng: chu kỳ 0,60 → khoảng 0,490 s. Khinh Thân: tốc đi
3 → 3,36. Âm Vang: tầm 3,2 → 3,68; diện tích hình tròn/cung tăng 32,25%, không
phải chỉ 15%. Thiết Bích với cửa sổ gốc 0,6 s: 0,60 → 0,735 s, không tăng dash.
Đây là chỉ số/tình huống kiểm có kiểm soát, không phải telemetry thắng/thua.

Cuồng Nộ mạnh về từng đòn nhưng đánh chậm rõ hơn; Cường Công thiên về sát thương
duy trì. Cuồng Nộ đơn lẻ cấp đầu cho DPS 1,21×, đủ hai cấp 1,26×.
Tổ hợp Thánh Gióng + Cuồng Nộ cấp 1→2 phải tăng 2,090→2,100×; không được làm
người chơi yếu đi khi nhận cấp sau. Kiểm mọi cấp của các nguyên liệu liên quan
trên cả ba nhân vật và danh mục tám tiến hoá thật, không chỉ build này.

Trần thẻ nền vẫn 2,709× DPS; thêm Song Tiễn 3,7926× nếu cả hai viên trúng.
Máu/di chuyển/tầm/phòng thủ/lướt/hồi/đạn đủ cấp giữ trần cũ. Tiếng Đàn Thần mở
ở Cường Công ×2 nên sát thương tại thời điểm mở là 1,70× thay 1,65×; bonus vẫn
+25%, và sau Cường Công ×3 trần vẫn 1,85×. Không nói mọi build trung gian giữ nguyên.

## Preview đúng với gameplay

`CardUpgradePreview` dùng cùng phép cộng hiệu ứng với `PlayerUpgradeState`.
`ProjectCard` sao chép chỉ số, áp đúng cấp sắp nhận và các bonus đủ điều kiện;
không tạo GameObject, không thay cấp/health/event hoặc asset. UI chỉ tính khi
đề nghị thay đổi, không tính mỗi frame. Màn chọn nêu lợi ích cấp sắp nhận, cấp đã có, bất lợi và
chỉ số trước → sau; nếu mở công thức, nêu tên tiến hoá ngay trên thẻ.

Bonus luôn áp ở hệ số 1, không dùng curve cấp đầu của nguyên liệu. Hồi máu,
số đạn, xuyên, spread và nổ không nhân curve phần trăm. Reset/replay vẫn theo
thứ tự nhận thẻ để giữ kết quả mạng và làm tròn tổng máu.

## Kiểm chứng

- Edit Mode: 45.000 lượt chọn; sáu curves × ba nhân vật; các mốc đòn trên quái
  10 HP bằng `Enemy.ApplyDamage`; trần chỉ số, làm tròn máu, dash/hồi/đạn và asset
  bất biến đều đạt. Đây không phải phép đo TTK qua toàn bộ auto-weapon trong arena.
- 1.152 tổ hợp Cuồng Nộ với ba nhân vật/tám công thức đạt; lợi ích DPS nhỏ nhất
  +0,0100× so với DPS gốc, không có cấp làm giảm DPS.
- Preview: 142 lựa chọn, mọi cấp tiếp theo và ngưỡng tiến hoá hai thứ tự, hai
  tiến hoá đồng thời, null/capped/invalid và thiếu dữ liệu nhân vật đạt. Chiếu
  trước khớp toàn bộ chỉ số `Apply`, 10 lần đọc lặp không phát `Changed`, không
  đổi state/máu/31 asset. `Editor/CardImpactPreviewChecks.cs`.
- Hồi quy lịch sử: 58 histories/174 final-only events; 10.000 idle tick và
  ADD/INSERT/SET/REMOVE/CLEAR, dữ liệu tới muộn, reconnect đều đạt.
- Play Mode host một client: 12/12 thẻ qua lựa chọn thật, pause/nhấp đôi/tiếp
  tục input và vật lý/hồi giờ/reset đạt. 8/8 công thức thật qua panel/ACK/reset
  đạt, Tiếng Đàn nhận đúng 1,70×. Fixture host-only đưa hết lịch spawn vào sân;
  không dùng kết quả này để chứng minh đồng bộ quái.
- Typography/UI: 75 next-rank views × mỗi độ phân giải, cả ba nhân vật/tám
  tiến hoá, hai tiến hoá đồng thời, glyph tiếng Việt, clipping và tương phản
  ≥7:1 đạt tại 1280×720 và 1920×1080. Mô tả 22/33 px, không best-fit/blur.
  5 raycast targets, bấm ảnh/focus/dim/consume/reset và 10.000 idle update đạt.
- Đã nhìn ảnh Game View: [cấp đầu 720p](screenshots/card-impact-first-720p-20261002.png),
  [đánh đổi trên Tấm 720p](screenshots/card-impact-tradeoff-720p-20261002.png),
  [hai tiến hoá 1080p](screenshots/card-impact-evolutions-1080p-20261002.png).
  Đây là preview tạm với dữ liệu thật, không scene/prefab mới. Debug HUD ngoài
  Cards vẫn nhìn thấy chồng ở góc trái, không chỉnh trong lượt này.
- Co-op Editor host + development client mới, KCP/LatencySimulation 100 ms:
  lựa chọn sai ID/token, nhấp đôi, chờ đồng đội, lượt đổi riêng, yêu cầu đổi trùng,
  hết giờ 11,30 s (gồm ACK animation), ngắt kết nối gỡ pause và restart đều đạt.
  Đề nghị kiểm có kiểm soát: Cường Công ở đợt 1, Liên Kích ở đợt 2; không sửa
  asset hoặc đẩy nhanh lịch spawn riêng ở host. Đủ bốn chữ ký 14 chỉ số (hai
  người × đợt 2–3) khớp client: damage 1,3000×, rồi speed 1,2250×.
  Log cục bộ: `Logs/Editor.log`, `.codex/CardImpact-Coop-20261002-155816.log`.
- Console native cuối Play: **0 lỗi đỏ, 1 cảnh báo** SpriteAnimator tự tăng tốc
  clip Attack 0,13 s về trần 0,10 s. Không sửa Animator/Player trong lượt này.
  Client không ghi Exception/error trong log kiểm. Đã thoát Play và dừng đúng
  tiến trình client kiểm thử; không lưu scene.
- Chạy lại toàn bộ Edit checks sau khi thoát Play vẫn đạt. Console khi dọn
  lượt: **0 lỗi đỏ, 2 cảnh báo** (cảnh báo hoạt ảnh trên và Unity-MCP
  `BufferedFileLogStorage: Flush called but already disposed`, không lỗi Cards).

## Phần cần bàn giao ngoài Cards

- Số sát thương nổi làm tròn, nên đạn 1,3 có thể vẫn hiện 1 dù trừ máu đúng.
- Bộc Phá/Lửa Thiêng gây splash thật nhưng chưa có VFX nổ riêng trong Combat.
- Thiết Bích tăng bảo vệ thật; nháy trúng đòn VFX không kéo dài cùng cửa sổ đó.
- Giữ nguyên rủi ro bù i-frame dash theo RTT và HUD demo chồng bảng; không bù
  bằng cách tăng/giảm thẻ ngoài mục tiêu lượt này.

Những mục này là lý do không kết luận đọc hiểu toàn gameplay đã hoàn tất; cần
quyền sửa mảng @Kiet để xử lý. Không sửa âm thầm từ Scripts/Cards.

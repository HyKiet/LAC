# UI và hiệu ứng Cards — 03/10/2026

Phạm vi: giao diện chọn/tiến hoá và hiệu ứng Cards. Không sửa Menu, HUD,
Combat, Player, scene, dữ liệu sức mạnh hoặc giao thức mạng.

## Trình bày

- `CardRitualGraphic`: hoa văn trống đồng đồng–ngọc dựng bằng mesh UI, sau ảnh
  và không nhận raycast. Hai huy hiệu đầu bảng và nền ảnh là tĩnh; chỉ vòng
  trang trí tiến hoá xoay chậm bằng thời gian không tỉ lệ.
- `CardHoverVisual`: ba thẻ hiện lần lượt trong 0,22 giây, lệch nhau 0,06 giây.
  Chỉ đổi alpha/viền, không co giãn hay di chuyển chữ. Idle/hover vẫn đứng yên,
  không khôi phục nhịp thở. Xác nhận giữ nguyên thẻ trong 0,42 giây rồi bay theo
  cung về nhân vật; tổng 1,05 giây và thời điểm ACK giữ nguyên.
- `CardSelectionView`/`Controller`: thanh thời gian 420 px đọc cùng deadline host
  với bộ đếm. Chỉ ghi chiều rộng khi pixel thay đổi; không tạo chuỗi mỗi khung.
  Thanh ẩn khi nhận lựa chọn, chờ đồng đội hoặc tiến hoá. Khung bảng/nút đổi có
  lóe sáng nhẹ, không phủ ảnh/chữ.
- `CardBattleEffect`: vòng nhận thẻ lan nhanh rồi chậm lại, hoa văn quay 24°;
  khiên hiện theo nhịp ngắn với nét chặn hai bên; hồi máu thêm tia ngọc hình
  thoi; vệt gió kéo dài và thu hẹp khi tan. Số hồi vẫn giữ tỷ lệ cố định.
  Không thêm đối tượng/texture mỗi lần phát; giữ mesh tái sử dụng, pool 24,
  alpha tối đa 0,22, sorting order và bảng màu cũ. Không dùng nhóm Son.

## Kiểm chứng

- Unity Play mode, Mirror host: `CardTypographyChecks.Validate` đạt ở 1280×720
  và 1920×1080: 75 preview cấp tiếp theo/3 nhân vật, 8 tiến hoá, tiếng Việt,
  không tràn, tương phản ≥7:1. Mô tả 22/33 px, không best-fit hay bóng/viền chữ.
- `CardUI`: đúng 5 graphic nhận raycast; bấm icon, hover/focus, dim/reset và
  consume đạt. Kiểm 10.000 idle update không ghi transform/màu; thêm 10.000
  cập nhật thanh cùng pixel không ghi transform. Thanh kẹp đúng 0–420 px,
  nửa deadline = 210 px; nhịp xác nhận không xoay/thu phóng chữ.
- Preview hiện thẻ kết thúc ở alpha 1, transform đứng yên sau 500 ms dù
  `timeScale=0`. Đã nhìn Game View trạng thái thường, hover và tiến hoá.
- `CardSelectionPlayModeChecks`: 12/12 thẻ, nhấp đôi, countdown, input/physics,
  bảo toàn pause và restart đạt. Luật chọn 10 giây và ACK không đổi.
- `CardBattleFeedbackChecks`: trigger thẻ/máu thật, đầy máu, hook trùng, khiên,
  di chuyển/dash/idle/pause, vòng đời, 1.000 yêu cầu giới hạn 24, thời lượng,
  restart và dữ liệu bất biến đạt. Chạy lại sau khi chụp ảnh để kiểm lượt cuối
  không phụ thuộc trạng thái preview. Console gameplay cuối: 0 lỗi đỏ/0 cảnh báo.

Lúc domain reload có bốn cảnh báo Unity-MCP. Một đoạn lệnh chụp thử tham chiếu
nhầm kiểu `EnemyBase` bị Roslyn từ chối, tạo ba mục lỗi công cụ; không phải lỗi
biên dịch mã dự án. Đã đọc lỗi, sửa đoạn lệnh và tách khỏi lượt gameplay cuối.
Kiểm dùng fresh domain tạm thời; trả lại cấu hình Editor/Game View sau khi dừng.
Sau Stop có một cảnh báo `BufferedFileLogStorage` đã dispose từ Unity-MCP;
Console vẫn 0 lỗi đỏ và scene không dirty. Không tính cảnh báo teardown công cụ
là kết quả gameplay.

Không chạy lại client co-op trong lượt trình bày này; kết quả mạng T-24D vẫn
ghi riêng tại [CARD_BATTLE_FEEDBACK.md](CARD_BATTLE_FEEDBACK.md). Không diễn giải
kiểm UI/pool thành benchmark 40 quái/200 đạn. T-32/T-50/T-51 vẫn mở.

## Ảnh Game View

Ảnh gốc từ render texture Game View, không phóng từ độ phân giải khác.
Ảnh trận tạm giữ thời gian và gọi từng hiệu ứng, đặt nhân vật ở tư thế nghỉ,
ẩn renderer quái để thấy nét. Đây là preview thị giác; trigger thật được kiểm
bằng bài host bên trên. Không lưu các thiết lập preview vào scene/asset.

- [Chọn thẻ 720p](screenshots/card-polish-selection-720p.png)
- [Chọn thẻ 1080p, deadline 6/10](screenshots/card-polish-selection-1080p.png)
- [Hover 720p](screenshots/card-polish-hover-720p.png)
- [Tiến hoá 1080p](screenshots/card-polish-evolution-1080p.png)
- [Nhận thẻ 1080p](screenshots/card-polish-upgrade-1080p.png),
  [720p](screenshots/card-polish-upgrade-720p.png)
- [Thiết Bích 1080p](screenshots/card-polish-shield-1080p.png)
- [Hồi máu 720p](screenshots/card-polish-heal-720p.png)
- [Vệt gió 720p](screenshots/card-polish-wind-720p.png)

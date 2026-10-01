# Khung và bố cục chọn thẻ — T-22B

Ngày: 01/10/2026. Theo yêu cầu: bỏ nhịp thở, tự dựng khung và thêm lóe sáng nhẹ.

## Thiết kế

- Thẻ đứng yên lúc nghỉ và hover. Hover đổi viền sang ngọc xanh.
- `CardFrameGraphic.cs` vẽ khung đồng vát góc bằng UI mesh: viền kép, cạnh sáng/tối,
  các thanh góc và hình thoi. Khung co giãn theo RectTransform, dùng chung cho thẻ,
  bảng chọn, nút đổi và thông báo tiến hoá.
- Vệt sáng chạy trên viền trong 0,72 giây, chu kỳ khoảng 5,2 giây và lệch pha giữa
  ba thẻ. Hover/nhấn kích hoạt vệt sáng. Không phủ ánh sáng lên ảnh hoặc mô tả.
- `CardHoverVisual.cs` vẫn giữ animation xoay/bay khi chọn và thời lượng 1,05 giây
  để khớp ACK lựa chọn của host. Toàn bộ hiệu ứng là biểu diễn cục bộ.
- `CardSelectionView.cs` nới bảng lên 1360×860, thẻ 340×560 (tọa độ thiết kế 1920×1080),
  tăng chữ mô tả lên 22, tách khu vực bộ đếm và đổi thẻ ở chân bảng.
- Màn chờ đồng đội ẩn cả thẻ, bóng và nút đổi; tiêu đề/hướng dẫn chuyển sang trạng thái chờ.
- 20 icon đang dùng giữ nguyên. UI sinh lúc chạy, không sửa scene/prefab hoặc luật chọn thẻ.

## Kiểm chứng

- Unity 6000.5.6f1, Arena chạy Mirror host một người. Console cuối lượt: 0 lỗi,
  0 cảnh báo. Chưa chạy lại hai tiến trình co-op cho thay đổi trình bày này.
- Hồi quy có sẵn `CardSelectionPlayModeChecks`: 12/12 thẻ và `ALL PASSED`, gồm
  pause, chống nhấp đôi, hit-stop, sang đợt 2, input/vật lý, reset và hủy lựa chọn.
- Đo 12 tên/mô tả bằng `Text.preferredHeight`: tất cả vừa vùng hiển thị.
- Theo dõi 3 giây với `Time.timeScale = 0`: cả ba thẻ giữ nguyên vị trí,
  `localScale = (1,1,1)` và góc xoay khi nghỉ/hover. Timer lóe sáng giảm từ
  5,2 xuống 2,944 giây trong lúc gameplay tạm dừng.
- So sánh mesh ở hai pha nghỉ/lóe: 155 đỉnh viền đổi màu; mesh ảnh/chữ không thay đổi.
  Ảnh pha lóe được giữ tạm trong preview để chụp, không thay tham số runtime.
- Kiểm tra PointerExit cũ không xóa hover mới; khung có `raycastTarget = false`.
- Chờ đồng đội ẩn bóng/nút đổi; mở lại khôi phục nút. Đã nhìn các ảnh Game View
  bên dưới ở độ phân giải 785×442.

![Bố cục và hover mới](screenshots/card-frame-selection.png)

![Pha lóe sáng nhẹ trên viền](screenshots/card-frame-glint.png)

![Chờ đồng đội](screenshots/card-frame-waiting.png)

![Thông báo tiến hoá dùng khung mới](screenshots/card-frame-evolution.png)

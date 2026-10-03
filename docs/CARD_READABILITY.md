# Chữ và bố cục thẻ — T-22C

Ngày 01/10/2026. Giữ bộ 20 icon, khung đồng–ngọc xanh và lóe sáng nhẹ của T-22B.

## Thay đổi

- Thiết kế trực tiếp ở 1280×720 thay vì thu chữ từ 1920×1080. `CanvasScaler.Expand`
  giữ toàn bộ bảng trong khung hình ở các tỉ lệ khác nhau; `Canvas.pixelPerfect = true`.
- Bảng 1184×656, ba thẻ 344×432. Ảnh 176×176, tên 26 px, mô tả 22 px,
  số cấp 18 px tại 720p. Tại 1080p, mô tả được raster ở khoảng 33 px thay vì 22 px trước đây.
- Mô tả căn trái, giãn dòng 1,08; tên nằm trên nền đặc, khoảng cách giữa các vùng rõ ràng.
  Chữ Điệp sáng / tên Hoè sáng có tương phản ít nhất 7:1 trên nền nghỉ và hover được kiểm tra.
- Giữ font động `LegacyRuntime.ttf` hỗ trợ dấu tiếng Việt. Không bật best-fit, không dùng
  bóng/outline nhân mesh chữ, không phủ vệt lóe lên chữ và không phóng/thu thẻ khi hover.
  Căn pixel giúp giảm lệch lưới; anti-alias của font vẫn được giữ, không ép Point lên atlas chữ.
- Nan dọc của khung không còn vượt ra ngoài nút đổi thấp. Danh sách nâng cấp tạm ẩn khi
  mở bảng, hiện lại sau khi đóng. Không thay luật thẻ, thời hạn 10 giây, ACK hay chỉ số.

Mức kiểm chứng: 1280×720 và 1920×1080. Ở cửa sổ thấp hơn 720p hoặc Game View đang zoom
thu nhỏ, chữ vẫn bị giảm kích thước; không xem ảnh preview bị thu nhỏ là ảnh 1:1 của game.

## Kiểm thử

`Cards/Editor/CardTypographyChecks.cs` thêm các menu trong `LAC/Tests`:

1. Mở Arena, Play qua Mirror host, đặt Game View 1280×720 hoặc 1920×1080.
2. Khi ván ở WaveActive, chạy **Validate Card Typography (Play Mode)**.
3. Kiểm đủ 12 tên/mô tả, 8 thông báo tiến hoá, màn chờ; `Text.preferredHeight` không vượt
   vùng chứa, font có toàn bộ ký tự đang dùng, không best-fit/mesh effect; tương phản tên/mô tả >=7:1.
4. Preview dùng hierarchy tạm và pause ván; **Close Card Typography Preview** khôi phục timeScale.
   Thoát Play cũng loại bỏ preview. Không sửa ScriptableObject hay lưu scene.

Kết quả: PASS cả hai độ phân giải; cỡ mô tả đo được 22,0 / 33,0 px.
Hồi quy `CardSelectionPlayModeChecks` đạt 12/12 và ALL PASSED: pause, nhấp đôi,
hit-stop, sang đợt, khôi phục input/vật lý, reset và hủy coroutine cũ.
Đã nhìn ảnh Game View thật; Console sau hồi quy/typography có 0 lỗi đỏ.
Chưa chạy lại hai tiến trình co-op: phần mạng/luật chọn không đổi.

Trong ảnh preview, canvas của controller gốc và NetworkManagerHUD được tạm tắt để
không chồng hai bộ UI. Đây chỉ là trạng thái thử trong Play, không lưu vào scene/prefab.

![Chọn thẻ 720p](screenshots/card-readable-720p.png)

![Chọn thẻ 1080p](screenshots/card-readable-1080p.png)

![Tiến hoá với chữ lớn](screenshots/card-readable-evolution.png)

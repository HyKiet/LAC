# Kiểm thử T-22, T-23 và T-24

Unity 6000.5.6f1. Không sửa mã khi đang ở Play Mode.

## Hồi quy host

Mở Arena, vào Play Mode, chờ host sinh người chơi. Chạy
`LAC > Demo > Check Card Resume (Play Mode - restarts run)`.
Bài kiểm tra chạy đủ 12 thẻ (thẻ đạn trên Tấm, thẻ chung trên Gióng), nhấp đôi, hit-stop, phục hồi input/vật lý,
bảo toàn timeScale có trước và hủy animation của ván cũ khi chơi lại.
Kết quả thành công: `[CardResume] ALL PASSED`.

## Cân bằng bể thẻ

Thoát Play Mode, chạy `LAC > Tests > Validate 12 Card Balance (Edit Mode)`.
Bài kiểm tra dùng 1.000 seed cho mỗi kiểu vũ khí, 15 lượt chọn và 2 lần đổi mỗi ván.
Kiểm tra 3 lựa chọn duy nhất, thẻ phù hợp, đủ lựa chọn đến lượt 15, giới hạn chỉ số,
sát thương 0,7/0,21 không bị làm tròn lên, máu theo chỉ số gốc, reset và không sửa asset.
Kết quả thành công: `[CardBalance] ALL PASSED` (45.000 lượt chọn).

Play Mode còn kiểm tra Khinh Thân nối với di chuyển, Âm Vang nối với tầm vũ khí,
Hồi Xuân hồi đúng 1 máu khi sang đợt, Thiết Bích kéo dài bảo vệ sau trúng đòn
và Cuồng Nộ áp dụng cả tăng sát thương lẫn giảm tốc đánh.

## Hai tiến trình, giả lập độ trễ

1. Thoát Play Mode, chạy `LAC > Tests > Build Card Network Client`.
2. Vào Play Mode ở Arena để chạy host.
3. Chạy `Builds/CardNetworkTests/CardClient.exe --lac-card-test-client -batchmode -nographics -logFile <đường-dẫn-log>`.
4. Khi host có hai người, chạy `LAC > Tests > Check Cards With Remote Test Client`.
5. Tìm `[CardNet] ALL PASSED` trong Console; kiểm tra log client không có exception.

Client tự đổi hai lần rồi chọn ở lượt đầu; lượt thứ hai cố ý chờ hết giờ.
Host kiểm tra ID/token không hợp lệ, nhấp đôi, hai yêu cầu đổi cùng revision,
giới hạn hai lần đổi mỗi ván độc lập cho từng người, chờ đồng đội, tự chọn
sau 10 giây, mất kết nối trong lượt chọn và reset nâng cấp khi chơi lại.
Kết thúc bài kiểm tra, client bị ngắt kết nối có chủ ý.

Trình điều khiển client chỉ được biên dịch trong Editor/development build và chỉ
hoạt động khi có tham số `--lac-card-test-client`. Build và log không đưa vào Git.

## Quy tắc đồng bộ

- Host bốc đề nghị bằng `RunRandom.Cards`, gửi định danh qua TargetRpc.
- Command lấy người chơi từ kết nối gửi; kiểm tra token lượt, revision đề nghị,
  thời hạn, giới hạn cộng dồn và số lượt đổi trên host.
- Đồng hồ dùng `NetworkTime.time`, tiếp tục chạy khi gameplay tạm dừng.
- Lịch sử định danh thẻ được đồng bộ để client dựng lại nâng cấp, gồm người vào muộn.
- Chỉ sang đợt khi mọi người tham gia đã hoàn tất. Host chờ ACK animation,
  có hạn chờ dự phòng để client không phản hồi không giữ ván vô hạn.
- Người đã gục hoặc rời mạng không giữ lượt chọn. Người vào giữa lượt
  chờ sang đợt tiếp theo, bắt đầu nhận đề nghị từ lượt chọn kế tiếp.
- Thời gian 10 giây và 2 lượt đổi nằm trong `CardSelectionRules.asset`.

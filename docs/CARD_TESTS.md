# Kiểm thử T-22 và T-23

Unity 6000.5.6f1. Không sửa mã khi đang ở Play Mode.

## Hồi quy host

Mở Arena, vào Play Mode, chờ host sinh người chơi. Chạy
`LAC > Demo > Check Card Resume (Play Mode - restarts run)`.
Bài kiểm tra chạy đủ 7 thẻ, nhấp đôi, hit-stop, phục hồi input/vật lý,
bảo toàn timeScale có trước và hủy animation của ván cũ khi chơi lại.
Kết quả thành công: `[CardResume] ALL PASSED`.

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

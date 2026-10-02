# T-32 — Hiệu năng và khả năng đọc hình ảnh

`PerformanceScenario` chỉ chạy trong Editor/Development, khi gọi `Begin()` hoặc
truyền cờ `--lac-perf`. Không tự kích hoạt khi người chơi chạy game bình thường.
Prefab kiểm thử lấy từ `Data/Tests/Resources/Tests/Performance.asset`.

## Tải kiểm thử

- 40 quái chia đều 5 loại; AI, giãn cách, vật lý và snapshot vẫn chạy.
- Duy trì 200 đạn người chơi bằng pool, quét va chạm thật, xuyên nhiều mục tiêu.
  Sát thương đặt 0 để giữ đủ 40 quái trong suốt lần đo. Đạn hết hạn được bù ở
  LateUpdate; số tối thiểu báo cáo là số đo tại thời điểm đó mỗi khung hình.
- Ma Trơi vẫn bắn, Ma Da vẫn lao. Đạn địch được cộng thêm ngoài 200 đạn người chơi.
- Người chơi di chuyển theo vòng, bất tử; 10 hiệu ứng sóng âm mỗi giây.
- Từ T-31 (03/10/2026), thêm 10 hiệu ứng nhạc cụ/giây, luân phiên ba profile
  trong Performance.asset. Kết quả tải mở rộng: [INSTRUMENT_REVIVE_TESTS.md](INSTRUMENT_REVIVE_TESTS.md).
- Tắt VSync, giới hạn ở 240 FPS trong thời gian đo; trả lại cấu hình sau khi đo.
- Khởi động 10 giây, lấy mẫu 30 giây. Không ghi file trong khoảng lấy mẫu.

Đạt khi số lượng tối thiểu đủ 40/200 và phân vị 95 thời gian khung hình không vượt
16.67 ms, đồng thời ít nhất 90% mẫu có callback render URP với camera. Báo cáo thêm
trung bình, main thread p95 và GC bytes/frame. Mức trần 240 FPS giữ bài đo ổn định,
vẫn cao hơn nhiều so với mục tiêu 60 FPS.

## Chạy lại

Editor: Play Arena, gọi `LAC.Core.PerformanceScenario.Begin()` qua MCP script-execute.
Sau khi đo, đọc `Logs/T32-editor.json`; thoát Play để xóa cấu hình kiểm thử.

Windows Development build:

```powershell
.\Builds\BossTests\LAC.exe --lac-perf -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile Logs/T32-player.log
```

Chạy từ gốc dự án, Editor ở Edit Mode. Kết quả là `Logs/T32-standalone.json`.
Không chạy đồng thời nhiều bài đo hoặc chụp ảnh trong lần đo dùng để nghiệm thu.
Ảnh Game View lấy trong một lần chạy riêng để đánh giá đạn son, vùng báo lao,
nhân vật và HUD khi nhiều sóng âm chồng nhau.

## Kết quả

Ngày 01/10/2026, i9-12900H và RTX 3060 Laptop GPU:

| Lần đo | Độ phân giải | Mẫu | Khung có render | Trung bình | p95 |
|---|---|---:|---:|---:|---:|
| Windows Development | 1920×1080 | 7175 | 7174 | 4.182 ms | **4.187 ms** |
| Editor | 1311×623 | 7009 | 7008 | 4.281 ms | **4.865 ms** |

Cả hai duy trì tối thiểu 40 quái và 200 đạn người chơi. Mốc trung bình xấp xỉ
239 FPS ở bản Windows đang giới hạn 240 FPS; đây là thời gian toàn khung, có cả
chờ giới hạn FPS. Không suy ra tốc độ tối đa từ số này. Console Editor 0 lỗi/0
cảnh báo; log Windows không có exception/cảnh báo pool sau sửa.

JSON gốc: [Windows](validation/T32-standalone.json), [Editor](validation/T32-editor.json).
Lần Editor có thao tác MCP/chụp ảnh, dùng để tham khảo; Windows đo riêng, không
gọi MCP hay chụp ảnh trong khoảng lấy mẫu. Bản Windows đo sau khi đổi màu đạn.

Hai sửa đổi từ kiểm tra: pool dùng chung nhận yêu cầu dung lượng lớn hơn từ hệ thống
đến sau (Trống Đồng trước đây khóa pool SoundWave ở ngưỡng 4); đạn người chơi dùng
màu gỉ đồng, alpha 100/255 thay cho trắng đặc. Đạn địch và đường báo lao vẫn ở lớp
99/100, phía trên đạn người chơi (8) và sóng âm (2).

Đã xem [ảnh Game View dưới tải](validation/T32-game-view.png): đạn địch màu son
vẫn nổi trên đạn người chơi trong suốt; nhân vật và HUD vẫn nhìn thấy. Đây là
đánh giá trực quan trên máy phát triển, chưa phải nghiên cứu với người chơi hay
kiểm thử khả năng tiếp cận cho người mù màu.

Kết quả trên máy phát triển không thay thế đo cấu hình PC tối thiểu khi phát hành.

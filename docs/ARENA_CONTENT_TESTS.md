# T-44B — 15 wave và mỹ thuật ThirdParty

> Phạm vi hiện hành 11/10/2026: [THESIS_SCOPE.md](THESIS_SCOPE.md), 10 wave, đồ án không thương mại, AI luật/FSM và Internet qua KCP. Kết quả bên dưới giữ nguyên ngày/cấu hình lịch sử; không chứng nhận AI thích ứng, Internet hoặc build mới.

Ngày kiểm thử: 04/10/2026, Unity 6000.5.6f1. Phạm vi: phần lõi của Kiệt và tích hợp asset theo yêu cầu; không triển khai T-45/T-45B hoặc thay nội dung thẻ.

## Thay đổi

- Bảng cố định còn 15 dòng: giữ wave 1–14, bỏ wave thường cũ số 15, chuyển Chằn Tinh từ 16 xuống 15. 14 lượt chọn thẻ; boss chết thì Victory ngay.
- `RunManager` đọc tổng từ bảng; fallback C# và giá trị scene đều là 15. HUD và màn kết quả lấy tổng động. Bài `BossChecks` lấy `run.TotalWaves` thay cho hằng 16.
- Gióng dùng Yellow Warrior: idle 8, run 6, attack 4 khung, bộ animation và body sprite cùng nguồn. Không đổi chỉ số hoặc hitbox. Khi gục giữ khung tĩnh vì gói không có Death; dùng vòng hồi sinh hiện có.
- `ArenaStageDecor.prefab`: ba nhóm, chỉ một nhóm 20 sprite hoạt động. Stage 0 ở wave 1–5, stage 1 ở 6–10, stage 2 ở 11–15. Không collider, NetworkIdentity hoặc RNG gameplay.
- Scene được lưu bằng Unity API. Unity đồng thời dọn các trường đã bỏ trong mã (`_snapshotBatch`, `_autoAdvanceCardSelection/_autoAdvanceDelay`) và tuần tự hoá HUD máu theo cấu trúc thanh hiện hành; không thêm lại hành vi cũ.

## Kiểm thử Play Mode

Mở Arena, Play host, chạy `LAC > Tests > Check 15 Wave Content (Play Mode)`.

Kết quả: **ALL PASSED: 15 waves, 14 selections, boss, telemetry, stages, restart**.

Bài này tăng tốc lịch sinh và dùng sát thương thử nghiệm nhưng vẫn đi qua WaveManager, EnemySpawner và điều kiện kết thúc thật. Lượt chọn thẻ đầu đợi timeout/ACK thật; các lượt sau hoàn tất chủ động để rút thời gian. Xác nhận 15 tổng kết wave, `waves_cleared=15`, `victory`, boss chỉ ở đợt cuối, chuyển nhóm trang trí và Restart về wave 1/stage 0. Không phải playtest cân bằng 15 phút; tác động mất một lượt nâng cấp cần đánh giá tiếp ở T-50/T-51.

CSV: [sự kiện](validation/T44B/events.csv), [từng wave](validation/T44B/waves.csv), [tổng kết](validation/T44B/runs.csv).

Lần chạy fixture đầu đếm lại cùng cửa sổ chọn thẻ trong lúc đợi ACK nên thất bại ở bộ đếm; đã sửa fixture dùng tập hợp số wave và chạy lại đạt. Không sửa cơ chế chọn thẻ.

## Kiểm tra hình ảnh và hiệu năng

Đã xem trực tiếp Game View 1309×616:

- [Wave 1](validation/T44B/stage1.png): Warrior vàng rõ trên sàn; cây/đá thưa ở mép.
- [Wave 6](validation/T44B/stage2.png): nhóm cây nhiều hơn, sân giữa trống.
- [Wave 15 và boss](validation/T44B/stage3-boss.png): đá xanh tối, HUD `15 / 15`, boss 800 máu, đường báo đòn son vẫn rõ.
- [Victory co-op](validation/T44B/victory15.png): HUD hoàn thành 15/15, màn kết quả có 15 dấu tiến trình.

Kiểm tra renderer khi phát hoạt ảnh: Idle/Run/Attack tiến khung đúng bộ; trạng thái gục giữ một khung; Unlock trở về Idle. [Kết quả](validation/T44B/animation.txt).

Ảnh bố trí dùng nhân vật đứng yên gần mép trên; không dùng để khẳng định toàn bộ độ khó hoặc mọi tình huống chiến đấu.

Phép đo Game View với telemetry bật và prefab mới: i9-12900H / RTX 3060 Laptop, 40 quái và 200 đạn xuyên suốt, 7.071 mẫu/7.070 khung render, trung bình **4,24 ms**, P95 **4,76 ms**, dưới 16,67 ms. [Số đo](validation/T44B/performance.json). Đây là kết quả trên cấu hình/độ phân giải này.

## Build và hai tiến trình

Windows Development build `Builds/ArenaContentTests/LAC.exe`: **Succeeded, 0 lỗi, 8 cảnh báo**. Hai cảnh báo assembly rỗng của MCP tilemap/inputsystem; sáu cảnh báo API Unity cũ trong các fixture có sẵn `PerformanceScenario`, `BossNetworkTestClient`, `EnemyNetworkTestClient`. [Build](validation/T44B/build.txt).

Editor host + Windows client mới, `LatencySimulation.latency=100`:

1. Host đang wave 6, client vào muộn: nhận `total=15`, `stage=1`, Gióng dùng `Giong_TinySwords`, Thạch Sanh dùng bộ cũ.
2. Host chuyển đến wave 15: client nhận `stage=2`; host xác nhận boss 1600 máu cho hai người.
3. Hạ boss: cả hai `wave=15`, `Victory`; không có wave 16 hoặc lượt chọn thẻ sau boss.
4. Restart: client về `wave=1`, `stage=0`, vẫn kết nối. Client không ghi telemetry.

[Log client](validation/T44B/client.txt). Đây là kiểm tra các điểm chuyển trạng thái và vào muộn; bài đủ 15 wave phía trên chạy host một người.

Console cuối sau nghiệm thu hai tiến trình: **0 lỗi, 0 cảnh báo**, `scriptCompilationFailed=false`. [Console](validation/T44B/console.txt). Client đã thoát và Editor đã dừng Play.

## Tái tạo nội dung

Ở Edit Mode trong Arena: `LAC > Content > Apply 15 Waves and ThirdParty Art`. Chạy lại không cắt thêm wave. Không sửa C# hay asset khi đang Play; thoát Play sau nghiệm thu để bỏ vị trí/bất tử/lịch sinh thử nghiệm.

Phân tích lựa chọn asset và giới hạn mỹ thuật: [ASSETS_ThirdParty.md](ASSETS_ThirdParty.md). GDD mục 11.7 và CLAUDE đã cập nhật số wave mới; bằng chứng kiểm thử T-39/T-43 cũ được giữ nguyên với chú thích lịch sử.

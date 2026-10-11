# T-44D — nhịp sinh tồn 10 wave

Quyết định 11/10/2026: chuyển nhịp chơi theo hướng Brotato, giữ ba lớp, vũ khí cố định, dash/Trống Đồng, thẻ và co-op. Tham chiếu vòng đợt có thời hạn: [Brotato trên Steam](https://store.steampowered.com/app/1942280/Brotato/).

## Hành vi

- Wave 1–9: 20/25/30/35/40/45/50/55/60 giây, tổng 6 phút chiến đấu thường. Hết giờ khi còn người sống thì vào chọn thẻ; giết hết quái không kết thúc sớm.
- Lặp thành phần quái đến hết giờ. Host quyết định từng sự kiện sinh theo trần 40 (dự trù quái con) và khoảng cách an toàn quanh người sống; client nhận ID/loại/vị trí và dùng snapshot cho vào muộn. Không gắn NetworkIdentity lên quái/đạn.
- Hết giờ: trả quái/đạn/VFX về pool, không tính là kill hoặc tạo Hồn; giữ Hồn đã rơi để tiếp tục nhặt. Không mang quái sang wave mới.
- Chín lượt chọn thẻ, tối đa 10 giây/lượt. Boss ở wave 10 không có timer; phải hạ boss để thắng. Giữ nguyên chỉ số boss hiện có.
- HUD hiển thị số wave và giây còn lại từ NetworkTime. Trang trí đổi ở wave 4/7; Restart về wave 1 và timer 20 giây.
- Thời lượng ván mục tiêu 8–10 phút; thời gian hạ boss phụ thuộc build. Chưa có cửa hàng hoặc nhiều ô vũ khí. Cân bằng T-50/T-51 và AI T-45 vẫn còn việc.

## Kiểm chứng

- `PlayableClassesChecks`: ba lớp/chọn nhân vật/HP/đòn kiếm-tên-thương/thưởng dash/chống đổi lớp sau sinh/không bụi cỏ/reconnect **ALL PASSED** với tổng 10 wave.
- Wave đầu chạy đủ **20 giây thực**: chống dọn sớm, sinh lại sau kill, hết giờ dọn quái còn sống. Fixture vô hiệu di chuyển quái và cho player bất tử để tách điều kiện timer khỏi cân bằng; tối đa 13 quái sống, 14 lượt sinh. [Kết quả](validation/T44D/natural.txt).
- `ArenaContentChecks`: **ALL PASSED** 10 wave/9 pha thẻ, bảo vệ timer, cap tính cả quái con, cleanup, boss cuối, Victory, telemetry và Restart. Fixture đẩy deadline để tăng tốc, không phải playtest cân bằng cả ván. CSV [wave](validation/T44D/waves.csv), [run](validation/T44D/runs.csv).
- Đã sửa telemetry tính cả kết quả `survived` vào số wave vượt qua; cleanup không phát sự kiện EnemyDied. CSV đối chứng có 10 wave hoàn thành và chỉ 1 kill boss trong fixture không giết quái thường.
- Đã xem ảnh [màn chọn](validation/T44D/selection.png), [đồng hồ](validation/T44D/timer.png), [co-op](validation/T44D/coop.png).
- Hai Editor với LatencySimulation 100 ms: Lancer 8 HP / Archer 6 HP cùng timer 20s, tự chuyển vào pha thẻ khi hết giờ, cả hai có roster rỗng; hết hạn chọn thẻ chuyển wave 2/timer 25s. Sai khác đồng hồ tại các probe khoảng 0.1–0.25s; probe không chụp đồng thời tuyệt đối. [Host](validation/T44D/coop-host.txt), [client](validation/T44D/client.txt).
- Client rời/vào lại khi wave 9 còn khoảng 55s: nhận wave/seed đúng, có roster quái từ sự kiện/snapshot. [Host](validation/T44D/late-active.txt), [client](validation/T44D/client-late.txt), seed 1050. Defeat/Restart hai người về wave 1/timer 20s và HP 8/6: [kết quả](validation/T44D/restart.txt). Không dùng các probe này để chứng nhận đầy đủ 10 wave co-op.
- Fixture riêng dồn lịch wave 9: **40 suất dân số / 36 quái sống**, tính dự trù quái con; điểm sinh thử tránh mọi player; hết giờ dọn hết quái, Hồn đang hoạt động vẫn còn 1 trước/sau. [Kết quả](validation/T44D/cap.txt).

Client kiểm thử dùng bản sao TEMP, không sửa dự án chính. Lần khởi động đầu cache package thiếu metadata nên biên dịch lỗi; đã phục hồi cache từ Editor chính. Client còn gặp lỗi Burst JIT bị Windows chặn (4551) lúc khởi động, nhưng các probe gameplay trên đã chạy được. Không báo Console toàn bộ client sạch lỗi môi trường.

Nghiệm thu cuối trên Editor chính: `ArenaContentChecks` ALL PASSED, compilationFailed false, Console **0 lỗi / 0 cảnh báo**. [Kết quả](validation/T44D/run.txt), [Console](validation/T44D/console.txt). Một lần vào Play khi import mã còn chờ đã gây reload và mất tham chiếu UI; đã dừng Play, chờ refresh/compile hoàn tất rồi chạy lại phiên sạch, không dùng phiên lỗi đó để nghiệm thu.

Tải Game View 1070×616, i9-12900H/RTX 3060 Laptop: 40 quái/200 đạn, warmup 10s + đo 30s, 7162 samples, P95 **4.980ms**, mean 4.189ms, PASS. [JSON](validation/T44D/performance.json). Fixture khóa WaveManager và vũ khí, tạo tải VFX chung; không chứng nhận cân bằng, mọi máy/độ phân giải hoặc toàn bộ tải sinh quái thích ứng. Ảnh [wave 9](validation/T44D/pressure.png) là preview có đẩy deadline và bất tử, không phải ván chơi tự nhiên.

## Chạy lại

Mở Arena, Edit Mode: `LAC > Content > Apply 10 Wave Survival`. Công cụ cập nhật WaveTable và ngưỡng trang trí tại chỗ, không thay bộ nhân vật. Vào Play, chọn lớp rồi tạo host. Kiểm thử tích hợp: `LAC > Tests > Check 10 Wave Content (Play Mode)`.

Giữ các báo cáo T-44B/T-44C làm lịch sử nghiệm thu cấu hình 15 wave. Thay đổi hiện tại không chứng nhận executable Windows; Application Control chặn Unity DLL đã ghi ở [báo cáo trước](PLAYABLE_CLASSES_TESTS.md).

# Trống Đồng — T-36 / T-37 / T-38

> Phạm vi hiện hành 11/10/2026: [THESIS_SCOPE.md](THESIS_SCOPE.md), 10 wave, đồ án không thương mại, AI luật/FSM và Internet qua KCP. Kết quả bên dưới giữ nguyên ngày/cấu hình lịch sử; không chứng nhận AI thích ứng, Internet hoặc build mới.

## Cấu hình và luồng chơi

- Trống cố định tại tâm `ArenaBounds`, không có collider cản người chơi.
- Nhặt đủ **20 Hồn**, rồi **lướt chạm trống** để kích hoạt. Đi bộ qua không kích hoạt.
- Tiêu toàn bộ năng lượng đang nạp; hồi **20 giây** dùng chung. Hồn vẫn nạp trong lúc hồi, tối đa một lần dùng; Hồn vượt trần vẫn được tính vào tổng Hồn của ván.
- Đẩy quái đang sống ra xa tâm **2.5 đơn vị**, kẹp trong sân, choáng **1 giây**, không gây sát thương.
- HUD góc dưới phải: vòng vàng là năng lượng, vòng chàm là hồi chiêu, chữ báo trạng thái. Khi sẵn sàng, mặt trống sáng và nhịp nhẹ.
- Các giá trị nằm trong `Assets/_LAC/Data/Drum/Resources/Drum/DrumRules.asset`.
- Sprite trống dựng hình học theo bảng màu, dùng tạm trước mỹ thuật T-47.

## Đồng bộ

`DrumNetwork.cs` là phần mở rộng của `RunManager`: tận dụng NetworkIdentity hiện có, không thêm danh tính mạng lên trống hay sóng. Năng lượng, thời điểm hết hồi và cờ sẵn sàng là SyncVar do host giữ. Client hiển thị đồng hồ bằng NetworkTime, không tự cấp quyền kích hoạt.

PlayerDash kiểm tra đoạn **đã đi** giữa hai bước vật lý, kể cả bước cuối; gửi Command qua đối tượng thuộc người chơi. Host kiểm tra nhân vật sống, cửa sổ dash hợp lệ, khoảng cách, đoạn chạm, seed ván, trạng thái chiến đấu, năng lượng và hồi chiêu. Host tiêu năng lượng/khóa hồi trước khi áp dụng hiệu ứng; yêu cầu tiếp theo cùng tick bị từ chối. RPC mang danh sách ID quái bị tác động; host không áp dụng hai lần. Sóng dùng pool SoundWave và alpha thấp theo bảng màu.

Khi chơi lại, trạng thái trống reset và SoulSpawner trả Hồn còn trên sân về pool để không nạp nhầm ván mới.

## Đạn địch và T-34

T-34 đã nối `Enemies/EnemyProjectile` của Ma Trơi với `EnemyProjectileClearable`: `Launch` gắn callback trả viên đạn về pool; xung kích thu hồi trên host và client qua sự kiện trống. Không chạm đến `Combat.Projectile` của người chơi. Prefab đạn địch mới về sau cũng cần marker và `BindRelease`; callback phải thu hồi đồng bộ. Đạn không có callback sẽ được tắt. Kiểm thử bổ sung: [ENEMY_TESTS.md](ENEMY_TESTS.md).

## Kiểm tra có thể chạy lại

1. Mở Arena, vào Play Mode host.
2. Chọn `LAC > Tests > Check Drum (Play Mode - restarts run)`.
3. Chờ hơn 22 giây, tìm `[DrumChecks] ALL PASSED`.

Bài kiểm tra gồm: không dash, thiếu Hồn, giới hạn nạp, ngoài tầm, NaN, đoạn lướt xuyên tâm và đoạn trượt ngoài, phân biệt phe đạn, tiêu năng lượng, chặn kích hoạt kép, đẩy lùi, đứng yên khi choáng, hết choáng, chờ hồi 20 giây thực, nạp trong lúc hồi, cú lướt vật lý thực, khóa lúc chọn thẻ và reset ván.

Kiểm tra mạng: build qua `LAC > Tests > Build Drum Network Client`; chạy `Builds/DrumNetworkTests/DrumClient.exe --lac-drum-test-client -batchmode -nographics -logFile <log>`. Client tự lướt qua trống lần đầu khi host nạp đầy. Log `[DrumClient]` ghi năng lượng, cờ sẵn sàng, hồi chiêu và số lần kích hoạt nhận qua mạng. Harness chỉ biên dịch trong Editor/development build và chỉ chạy khi có cờ trên.

## Kết quả nghiệm thu — 30/09/2026

- Play Mode host: `DrumChecks` đạt `ALL PASSED`, bao gồm chờ hồi 20 giây thực và cú lướt vật lý qua tâm.
- Sau sửa màu sóng, chạy lại `DrumChecks`: `ALL PASSED`, Console 0 lỗi/0 cảnh báo trong Play Mode. Khi thoát Play, plugin MCP có cảnh báo `Flush called but already disposed`; không xuất phát từ mã gameplay.
- Windows Development build thành công. Host Editor + client độc lập qua LatencySimulation (RTT quan sát khoảng 257 ms): client lướt từ (-3, 0) đến (3, 0), nhận `activation=1 energy=0 ready=False cooldown=20.0`.
- Hai người yêu cầu cùng tick: lần đầu thành công, lần sau bị chặn. Chơi lại trả Hồn cũ về pool và reset số kích hoạt/năng lượng/hồi chiêu.
- Đã nhìn ảnh Game View của HUD sẵn sàng, chọn thẻ và sóng trống. Phát hiện SpriteRenderer tint bị bỏ qua trên đường render additive; `SoundWave` truyền màu/alpha qua MaterialPropertyBlock, giữ vật liệu dùng chung và không tạo material mỗi frame. Ảnh sau sửa cho thấy sóng mờ, nhân vật/quái và HUD vẫn rõ.
- Đạn địch thử bằng đối tượng có marker; chưa có quái bắn xa T-34 để thử một đợt đạn địch thật. Không coi kiểm tra này là nghiệm thu T-34.
- Khi thử co-op, đưa host ra ngoài đường lướt client (ví dụ (0, 6)): collider người chơi có thể chặn đường lướt, đây không phải lỗi kích hoạt trống.

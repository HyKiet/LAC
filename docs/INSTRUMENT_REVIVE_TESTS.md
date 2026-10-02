# T-31 và T-41

## Sóng âm riêng cho từng vũ khí

`CharacterData.InstrumentWave` trỏ tới ba asset trong `Data/VFX`. `InstrumentWave`
dùng chung một prefab, ba LineRenderer và ObjectPool; không NetworkIdentity,
không thay đổi sát thương hay chọn mục tiêu. `WeaponAuto` sinh biểu diễn trên từng
máy giống luồng khai hỏa hiện có.

- Đàn bầu: ba vòng lan đồng tâm, nhịp nan hoa 24 cánh, màu chàm sáng.
- Roi sắt: ba cung cộng hưởng màu gỉ đồng, lấy đúng góc và tầm đã nâng cấp; không vẽ
  vòng tròn phía sau người chơi. Cung ngoài hiện ngay vì sát thương là tức thời.
- Sáo trúc: cụm ba gợn nhỏ chạy theo từng hướng bắn, kể cả khi tăng số đạn.
  Đây là dư âm ngắn tại đầu đường bắn; đạn thật vẫn quyết định va chạm/tầm bay.

Vật liệu `LAC/SpriteAdditive`, sorting order 2, alpha tối đa 0.10/0.16/0.10.
Không dùng nhóm son. Sóng Trống Đồng tiếp tục dùng `SoundWave` riêng.
Menu dựng lại dữ liệu/prefab: `LAC > Combat > Rebuild T31 T41 Content` (Edit Mode).

## Hạ gục và hồi sinh

Máu về 0 khóa điều khiển, lướt và vũ khí; quái bỏ qua người đã gục. Host kiểm tra
một đồng đội còn sống trong bán kính 1.5 đơn vị liên tục 3 giây. Ra ngoài, người
cứu gục/rời mạng hoặc rời pha chiến đấu sẽ hủy tiến độ. Không cần thêm phím bấm.

`Data/Characters/ReviveRules.asset` chứa khoảng cách, thời gian, máu hồi 50% tối
đa (làm tròn lên) và bất tử 1 giây sau khi đứng dậy. Đây là giá trị cân bằng ban
đầu, có thể chỉnh trong asset. Giữ các nâng cấp đã nhận; thẻ tăng máu không tự
hồi sinh người đã gục. Chọn thẻ tạm ngừng cứu; khi wave mới bắt đầu có thể cứu lại.

`PlayerRevive.cs` là phần mở rộng `PlayerHealth`. Chỉ gửi thời điểm bắt đầu/hủy
và người cứu qua SyncVar; thanh tiến độ nội suy từ NetworkTime. Máu hồi do host
quyết định. `VFX/ReviveMarker` là con của prefab Player, hiển thị vòng phạm vi,
lời nhắc và thanh tiến độ trên đầu người bị hạ.

Một người bị hạ còn chờ cứu; tất cả bị hạ thì Defeat. Cùng quy tắc áp dụng cho
host chỉ có một người. Restart xóa tiến độ và hồi đầy máu. Người đã gục rời mạng
không trừ số người sống lần nữa; người sống cuối cùng rời khi đang đánh thì thua.

## Chạy lại kiểm thử

1. Mở Arena, Play host. Dựng Windows Development với Arena.
2. Chạy bản dựng với `--lac-revive-client -logFile Logs/ReviveClient.log`.
   Cờ chỉ phục vụ Editor/Development: nối localhost, tắt tự di chuyển/bắn phía
   client, ghi trạng thái máu, tiến độ và khóa điều khiển.
3. Khi đủ hai người, chọn `LAC > Tests > Check T31 T41 (Two Players)`.
4. Chờ `[CombatChecks] ALL PASSED`; thoát Play và đóng client để dọn bài thử.

Bộ thử kiểm tra: ngoài phạm vi không hồi; trong phạm vi đủ thời gian mới hồi;
ra rồi vào phải tính lại; máu hồi theo chỉ số tối đa; bất tử sau hồi; quái bỏ qua
người gục; thẻ không hồi sinh; hồi sinh cập nhật số người sống; cả hai gục thì
thua; Restart sạch; ba profile khớp dạng vũ khí, dùng pool và nằm dưới đòn địch.

## Kết quả

Ngày 02–03/10/2026, Unity 6000.5.6f1:

- Windows Development build thành công; host Editor + client Windows qua
  LatencySimulation 100 ms. `CombatChecks`: **ALL PASSED** cả hai lần chạy.
- [Log client](validation/T41-coop.txt) xác nhận 0/8 → 4/8 máu, tiến độ hủy và
  tính lại, input tắt khi gục/bật khi hồi, Defeat khi cả hai gục và Restart về wave 1.
- Thử bổ sung: người gục rời mạng giữ ván WaveActive với một người; người còn lại
  gục thì Defeat. Không có lỗi đỏ. Lần thử đổi nhân vật và bắn ngay cùng frame
  có cảnh báo SpriteAnimator tự tăng tốc clip; đây là fixture đổi nhân vật, không
  phải lỗi sóng âm/hồi sinh.
- Gọi đường khai hỏa thật: đàn bầu trừ 2 máu, roi sắt trừ 3; đạn sáo chạm sau
  khi bay và trừ 1. Không thay đổi sát thương gốc trong task này.
- Đã xem [ba kiểu sóng](validation/T31-instruments.png) và [HUD hồi sinh](validation/T41-revive.png).
  Ảnh so sánh sóng giữ một khung hiệu ứng ở ba vị trí và thêm tên nhạc cụ chỉ trong
  fixture; ảnh hồi sinh giữ tiến độ để kiểm tra nhãn. Không lưu các bố trí đó vào scene.

Phép đo tải dùng `--lac-perf`, bổ sung 10 hiệu ứng nhạc cụ/giây (luân phiên ba
profile) ngoài 10 sóng Trống Đồng/giây, 40 quái và 200 đạn. Cửa sổ game phải hiển
thị để có callback render; lần chạy ẩn có 0 khung render đã bị loại.

Windows Development 1920×1080, i9-12900H/RTX 3060 Laptop: **p95 4.296 ms**,
trung bình 4.185 ms, 7.170 mẫu/7.169 khung render, tối thiểu 40 quái và 200 đạn;
**đạt** ngưỡng 16.67 ms. [JSON](validation/T31-performance.json). Đo riêng, không
chụp ảnh trong khoảng lấy mẫu. Log không exception/cảnh báo pool. Sau phép đo,
màu cung roi được chốt về gỉ đồng theo PALETTE; hình học, alpha và tải không đổi.
Đã xem [Game View dưới tải](validation/T31-stress.png) sau khi chốt màu: đạn son
vẫn nổi phía trên các lớp sóng, nhân vật và HUD còn đọc được. Console lần kiểm tra
hình ảnh cuối: 0 lỗi/0 cảnh báo. Đây là đánh giá trực quan, không thay thế kiểm thử
khả năng tiếp cận với người chơi.

Chưa kiểm thử Steam/WAN hoặc cân bằng hồi sinh với người chơi thật. Máu hồi,
bán kính cứu và bất tử hiện là tham số ban đầu trong ReviveRules, dành cho T-50/T-51
điều chỉnh sau khi chơi thử.

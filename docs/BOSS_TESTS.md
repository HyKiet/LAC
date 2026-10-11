# T-39 — Chằn Tinh

> Phạm vi hiện hành 11/10/2026: [THESIS_SCOPE.md](THESIS_SCOPE.md), 10 wave, đồ án không thương mại, AI luật/FSM và Internet qua KCP. Kết quả bên dưới giữ nguyên ngày/cấu hình lịch sử; không chứng nhận AI thích ứng, Internet hoặc build mới.

Chằn Tinh hiện ở wave cuối **10 theo T-44D**. T-44B từng chuyển từ 16 xuống 15; kết quả T-39 bên dưới thu khi còn 16 wave. Nghiệm thu cấu hình 10 wave xem TEN_WAVE_TESTS.
`EnemyData` chứa 800 máu gốc; `ChanTinhRules.asset` chứa hệ số cộng thêm 100% cho mỗi
người chơi bổ sung. Host chốt 800/1600 máu khi trùm xuất hiện; vào/rời mạng sau đó
không làm thay đổi máu tối đa trong trận trùm đang diễn ra.

- Pha 1: luân phiên lao 8 đơn vị và phun 5 viên theo quạt 60°.
- Pha 2 bắt đầu tại 50% máu, ngắt đòn cũ và báo chuyển pha. Triệu hồi 4 Ma Da,
  khoảng cách tối thiểu giữa hai lượt triệu hồi là 8 giây; giữa các lượt là quét đuôi
  360°, bán kính 3.5. Thời điểm triệu hồi chờ đòn hiện tại kết thúc.
- Mỗi đòn báo trước 0.9 giây, hồi 1.2 giây. Sát thương 2, đi qua `DamageSystem`
  nên i-frame vẫn có hiệu lực. Trống Đồng ngắt đòn, choáng và xóa đạn.
- Trần 40 quái gồm cả trùm. Quái triệu hồi dùng ID âm riêng, không làm lệch ID lịch
  wave. Hạ trùm dọn quái con và đạn, sau đó WaveManager báo Victory.
- Thanh máu vàng, số máu, tên và pha ở dưới HUD wave. Nhóm màu son chỉ dùng cho
  đạn/vùng báo đòn. Sprite mãng xà hiện là hình tạm; mỹ thuật cuối thuộc T-47.

## Đồng bộ

`BossNetwork.cs` mở rộng EnemySpawner hiện có. Quái và đạn không có NetworkIdentity.
Host đổi các SyncVar chỉ khi xuất hiện, nhận sát thương, chuyển pha hoặc bắt đầu/ngắt
đòn; thời điểm NetworkTime cho phép client dựng lại đòn đang chạy. Máu tối đa và pha
cũng có trong trạng thái ban đầu cho người vào giữa trận. Vị trí vẫn dùng snapshot
T-35. Sự kiện triệu hồi có seed/wave; snapshot toàn roster sửa trường hợp thiếu quái.

## Chạy lại

1. Mở Arena, Play (Mirror host).
2. Chọn `LAC > Tests > Check T39 Boss (Play Mode)`.
3. Chờ `[BossChecks] ALL PASSED`. Bộ kiểm thử chuyển thẳng tới `run.TotalWaves` (hiện là 10), đặt bất tử
   tạm thời, tắt vũ khí và tự kiểm tra cả sát thương trong/ngoài vòng quét.
4. Thoát Play sau kiểm thử để xóa toàn bộ cấu hình kiểm thử.

Kiểm tra bao gồm wave cuối chỉ có một trùm; 800 máu; 5 đạn; đứng yên khi báo lao;
dịch chuyển khi lao; Trống Đồng ngắt đòn; 401 máu vẫn pha 1, 400 sang pha 2;
4 Ma Da; sát thương vòng quét; trần 40; Victory; chơi lại wave 1 xóa trạng thái trùm.

Build Development có thể chạy thêm `--lac-boss-client` để tự kết nối localhost,
tắt vũ khí client và ghi `[BossClient]`: máu/pha, đòn, số quái/đạn, từ chối sát thương
do client đề nghị. Cờ này không tồn tại trong luồng phát hành thông thường.

## Kết quả

Ngày 02/10/2026, Unity 6000.5.6f1:

- `BossChecks`: **ALL PASSED**, Console 0 lỗi/0 cảnh báo.
- Windows Development build thành công. Editor host + tiến trình client độc lập,
  Mirror LatencySimulation 100 ms: 1600/1600 máu; gây 801 sát thương trên host đưa
  cả hai sang pha 2 tại 799/1600, triệu hồi 4 Ma Da rồi quét đuôi.
- Client gọi DamageSystem không trừ được máu; trạng thái máu cục bộ khớp host.
- Rời rồi vào lại giữa pha 2 nhận đúng 799/1600 và đòn hiện tại.
- Hạ trùm: client nhận Victory, 0 quái/0 đạn. Restart đổi seed, về wave 1,
  máu boss 0/0 và pha 0. Console lần chạy cuối 0 lỗi/0 cảnh báo.
- Đã nhìn [ảnh Game View](validation/T39-boss.png): thanh máu vàng dưới HUD wave,
  vòng báo quét màu son và nhân vật phân biệt được trên nền sân.

[Log sự kiện client](validation/T39-coop.txt) ghi ba lần chạy. Lần chụp ảnh tạm
dừng Editor quá 10 giây gây timeout KCP; kiểm thử Victory/Restart sau đó được chạy
lại không pause và đạt. Không coi lần pause này là lỗi gameplay. Chưa kiểm thử
Steam/WAN; sprite hiện tại là hình tạm, không nghiệm thu mỹ thuật T-47.

Kiểm tra hồi quy `EnemyChecks` sau đó cũng ALL PASSED, 0 lỗi đỏ. Dừng tiến trình
client cưỡng bức sau kiểm thử tạo một cảnh báo timeout KCP trên host; cảnh báo này
xảy ra lúc dọn bài thử, sau lần co-op 0 lỗi/0 cảnh báo nêu trên.

# T-34 / T-35 — Quái và hiệu chỉnh co-op

## Nội dung

| Quái | Máu | Tốc độ | Hành vi |
|---|---:|---:|---|
| Ma Trơi | 8 | 2 | Giữ khoảng cách 5, bắn mỗi 2 giây trong tầm 9; đạn tốc độ 3, sống 6 giây, gây 1 sát thương |
| Bù Nhìn | 40 | 1.5 | Truy đuổi, va chạm gây 3 sát thương |
| Ma Da | 12 | 6 khi lao | Nhịp 2 giây, báo hướng 0.45 giây rồi lao 0.5 giây; hướng khóa từ lúc báo trước |
| Quỷ Nhỏ | 15 | 2.5 | Chết tách đúng hai Cô Hồn; quái con không tách tiếp |

Thông số trong `Data/Enemies/*.asset`. Bảng wave giới thiệu Ma Trơi từ đợt 2, Bù Nhìn từ đợt 4, Ma Da từ đợt 6, Quỷ Nhỏ từ đợt 8. Ngân sách quái tính Quỷ Nhỏ là hai suất để cả đợt vẫn không vượt 40 khi tách. Cân bằng toàn ván thuộc T-50/T-51; sprite hình học là tạm, mỹ thuật hoàn chỉnh thuộc T-47.

`EnemyProjectile` không có NetworkIdentity. Host phát sự kiện khai hoả, mỗi máy lấy đạn từ pool và tự bay; chỉ host kiểm tra đoạn bay/va chạm và gọi DamageSystem. Đạn tắt khi hết tuổi, ra ngoài sân, hết chiến đấu hoặc trúng người chơi phía host. Client không dùng va chạm dự đoán để quyết định sát thương. Trống thu hồi đạn qua callback đã gắn trên prefab.

Ma Da có đường báo lao màu SonSang, sorting order 99. Đạn địch đặc, sorting order 100; thân quái không dùng nhóm Son. Trống gây choáng sẽ hủy cú lao đang báo/đang chạy và dời nhịp lao kế tiếp.

## Snapshot

- Gửi mỗi 0.5 giây theo đồng hồ không co giãn; reliable để giữ thứ tự với sự kiện chết/tách.
- Toàn bộ roster, kể cả khi rỗng; không bỏ quái sau phần tử 64. Truyền loại quái bằng chỉ số catalog cố định cùng bản build.
- ID theo đợt, ID âm cho con tách; chơi lại xóa pool, dấu chết, roster và sequence.
- Client bù vị trí theo vận tốc và tuổi gói tối đa 0.5 giây. Sai lệch nhỏ triệt dần trong FixedUpdate; lớn hơn 4 đơn vị snap để không giữ quái ở sai khu vực quá lâu.
- Snapshot đến trước lịch sinh sẽ dựng quái cục bộ; lượt Spawn sau nhận lại object sẵn có. Quái đã chết không được lịch sinh trễ hồi sinh. Việc sửa roster không phát lại Hồn hay kill count.
- Đây là hiệu chỉnh 2 Hz, không bảo đảm vị trí hai máy trùng tuyệt đối giữa hai gói; host vẫn là thẩm quyền sát thương.

## Kiểm thử

Host: mở Arena, vào Play, chọn `LAC > Tests > Check T34 Enemies (Play Mode)`. Bài thử chạy khoảng 4 giây: Ma Trơi giữ khoảng cách/bắn, thu hồi đạn bằng trống, sát thương và bất tử, Bù Nhìn đánh 3 máu, Ma Da báo trước/lao/hủy khi choáng, Quỷ Nhỏ tách đúng ID, giữ nguyên bộ đếm lịch sinh, ngân sách 16 wave và reset pool.

Client development: chạy build Arena với `--lac-enemy-test-client -batchmode -nographics -logFile <path>`. Harness chỉ bật bằng cờ này, tự kết nối localhost và tắt vũ khí để đo. Bố trí host 70 quái bất động (vượt 64 để kiểm tra hồi quy), quái 1000 ở (6, 6), quái 1003 là Quỷ Nhỏ. Harness gây lệch 2 đơn vị ở client, đo sau 1.5 giây, thử gói sai seed/sequence và xác nhận client không thể gây sát thương. Giết quái 1003 trên host để xác nhận hai con ID -2007/-2008 xuất hiện trên client. 70 quái chỉ là dữ liệu kiểm thử, không sửa trần gameplay 40.

Không sửa C# trong Play Mode. Kết thúc bài thử, thoát Play và đóng tiến trình client thử nghiệm.

## Kết quả — 30/09/2026

- Host `EnemyChecks`: ALL PASSED, Console 0 lỗi/0 cảnh báo.
- Đã xem ảnh Game View bốn sprite tạm, đạn Ma Trơi và đường báo lao Ma Da; đòn địch rõ, không dùng màu Son cho thân quái.
- Windows development build thành công. Hai tiến trình dùng LatencySimulation, RTT quan sát 272 ms: client nhận đủ 70 quái; sai lệch 2 đơn vị giảm còn 0.001 sau 1.5 giây. Gói sai seed/gói cũ bị bỏ qua; DamageSystem từ client trả false.
- Giết Quỷ Nhỏ 1003 trên host: cả hai máy có 71 quái và đúng hai con ID -2007/-2008. Giết toàn bộ: client về 0, không có quái ma do lịch sinh trễ.
- Bài co-op hỗn hợp: client ghi nhận đạn Ma Trơi và các nhịp lao Ma Da. Kích hoạt trống trên host thu hồi đạn thật; client nhận `shots=0 charges=0 drum=1`.
- Thua và Restart ngay trong cùng lần gọi ở wave 1: client nhận seed mới, số snapshot trở về 1 rồi tăng 5/10/15/20/25 cùng lịch sinh 1–6 quái. Không kẹt bởi sequence của ván cũ. Console host 0 lỗi/0 cảnh báo; log client không có exception/error/warning.

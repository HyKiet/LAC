# LẠC — Children of the Dragon

**Game Design Document — bản đồ án tốt nghiệp, cập nhật 11/10/2026.**

Không phát hành thương mại. Phạm vi thu gọn theo yêu cầu người dùng và xác nhận giảng viên chỉ yêu cầu game có AI, không bắt buộc học máy/LinUCB hoặc 30 người thực nghiệm. Đánh giá và tiêu chí nghiệm thu: [THESIS_SCOPE.md](THESIS_SCOPE.md). Kiến trúc theo [CLAUDE.md](../CLAUDE.md); trạng thái triển khai theo [TASKS.md](TASKS.md).

| Hạng mục | Phạm vi |
|---|---|
| Thể loại | Arena survival roguelite, top-down 2D pixel art |
| Nền tảng bàn giao | PC Windows, bản chạy độc lập cho đồ án |
| Người chơi | 1–2; co-op online qua Internet |
| Ván chơi | 10 wave, mục tiêu 8–10 phút |
| Engine | Unity 6000.5.6f1 · URP 2D · C# |
| Mạng | Mirror + KCP, host-authoritative |
| Phát hành/giá | Không phát hành, không định giá, không yêu cầu Steam |

## 1. Tổng quan và yêu cầu đề tài

Người chơi chọn Thánh Gióng (lớp Warrior), Archer hoặc Lancer, sinh tồn trong arena và hạ Chằn Tinh ở wave cuối. Vũ khí tự đánh; người chơi di chuyển và dash. Chọn thẻ giữa wave để nâng cấp. Thánh Gióng dùng sprite sheet do AI tạo và chuẩn hóa ngày 18/10/2026; các lớp khác dùng asset sẵn để giảm công mỹ thuật.

Ba yêu cầu đã biết của giảng viên: **online multiplayer, có AI, có yếu tố mới lạ độc đáo**. Điểm riêng được chọn là Hồn nạp Trống Đồng dùng chung trong co-op, kết hợp đạo diễn thích ứng xét người yếu nhất. Không tuyên bố thuật toán mới hoặc cơ chế chưa từng có trong mọi game.

Tham chiếu cảm giác chơi: nhịp sinh tồn theo giờ và tự đánh của Brotato. Không triển khai shop hoặc nhiều ô vũ khí. Đối tượng sử dụng là giảng viên, nhóm và người tham gia thử nghiệm; không lập kế hoạch thị trường/Steam.

## 2. Vòng lặp

```text
Chọn lớp → tạo/vào phòng → sinh tồn → chọn 1/3 thẻ
→ 9 wave thường / 9 lượt nâng cấp → boss wave 10 → kết quả → chơi lại
```

Wave 1–9 dài 20, 25, 30, 35, 40, 45, 50, 55, 60 giây; tổng 6 phút chiến đấu thường. Host quyết thời điểm kết thúc bằng NetworkTime, không kết thúc sớm khi hết quái. Quái vào liên tục theo nhịp, trần 40 suất gồm quái con, tránh mọi người sống ít nhất 3 đơn vị. Hết giờ dọn quái/đạn bằng pool, không tính kill hoặc rơi Hồn; giữ Hồn đã rơi. Boss không có timer; hạ boss thì thắng, không chọn thẻ sau boss.

Mỗi lượt thẻ tối đa 10 giây, hai lượt đổi/người/ván; host tự chọn khi hết hạn. Co-op chờ mọi người chọn xong. Chết hết thì thua. Chơi lại tại chỗ giữ lớp, reset HP/thẻ/Hồn/trống/seed và trạng thái AI. Không có tiền tệ hay tiến trình vĩnh viễn.

## 3. Điều khiển và phản hồi

| Thao tác | Bàn phím | Tay cầm |
|---|---|---|
| Di chuyển | WASD | Stick trái |
| Dash | Space | A/X |

Vũ khí chọn mục tiêu gần nhất trong tầm rồi tự khai hoả. Cả ba dash 4 đơn vị trong 0.15s; hồi theo lớp. HP/sát thương/i-frame do host quản lý, input local phản hồi ngay. Đòn đánh có nháy, knockback, số sát thương và VFX sẵn có; đòn địch màu son và vẽ trên hiệu ứng người chơi.

## 4. Ba lớp hiện có

Thánh Gióng hiện dùng pixel toàn thân T-18H theo yêu cầu20/10/2026: Idle4/Run8/Down4, chỉ cây tre tách riêng. Tre có đốt/mấu tự nhiên và điểm cầm theo frame; chân chạy theo vận tốc thực trong khi tre diễn timeline host. Giữ chỉ số/vùng đánh, flash/afterimage; chỉ lớp này đổi mỹ thuật. Nguồn AI, GIF và giới hạn tại [THANH_GIONG_PIXEL.md](THANH_GIONG_PIXEL.md). T-18D/E/F/G là lịch sử.

Hướng tinh chỉnh combat T-50B: tham khảo nhịp tự đánh/ra đòn của Brotato, ưu tiên mốc contact, hoạt ảnh theo tốc độ thật, giữ hướng và feedback/co-op. Phương án tại [BROTATO_COMBAT_FEEL.md](BROTATO_COMBAT_FEEL.md); chưa đổi chỉ số/cách gây sát thương đang chạy. Timing/feel được chốt trước cân bằng damage T-50 và wave T-51.

| Chỉ số | Thánh Gióng (Warrior) | Archer | Lancer |
|---|---|---|---|
| Sprite | AI sinh, chuẩn hóa 64×64 / PPU 32 | TinySwords Blue | TinySwords Purple |
| Máu | 10 | 6 | 8 |
| Tốc độ | 4 | 5.5 | 4.5 |
| Vũ khí | Tre ngà (quét cung) | Cung | Thương |
| Sát thương | 4 | 4 | 5 |
| Tầm | 2.2, cung 110° | 8, tên tốc độ 16 | 3.2, dải rộng 0.8 |
| Chu kỳ | 0.65s | 0.7s | 0.85s |
| Hồi dash | 1s | 0.8s | 1s |
| Khác biệt | Quét nhiều quái gần | Tên kế tiếp sau dash ×2 | Đâm xuyên các quái cùng dải |

Đây là chỉ số đang dùng, chưa là cân bằng cuối T-50/T-51. Mở sẵn cả ba, cho phép chọn trùng trong co-op. Dữ liệu nằm trong `Data/Characters`; nâng cấp sửa bản sao chỉ số của ván, không sửa asset. Thẻ đạn chỉ dành Archer. Warrior/Lancer gây sát thương tức thời theo đúng vùng VFX; Archer chỉ gây sát thương khi tên chạm, tên không có NetworkIdentity.

## 5. Thẻ nâng cấp

Giữ **12 thẻ nền đã triển khai**: Cường Công, Liên Kích, Sinh Lực, Bộ Pháp, Song Tiễn, Xuyên Tâm, Bộc Phá, Khinh Thân, Âm Vang, Hồi Xuân, Thiết Bích, Cuồng Nộ. Chỉ số, ID, giới hạn và trọng số tại [CARDS.md](CARDS.md).

Sau mỗi wave thường bốc ba thẻ hợp lệ bằng RunRandom; chọn một; lọc loại vũ khí/giới hạn cộng dồn; lựa chọn độc lập từng người và đồng bộ ID qua host. **Không bắt buộc tám tiến hoá.** T-25/T-26 là dự phòng, tối đa 1–2 công thức dùng hiệu ứng có sẵn nếu còn thời gian sau nghiệm thu. Không thêm thẻ hiếm/tỷ lệ 10% hoặc mở bể thẻ bằng Ngọc.

## 6. Quái, boss và Trống Đồng

| Quái | HP | Tốc độ | Hành vi hiện có |
|---|---:|---:|---|
| Cô Hồn | 10 | 3 | Truy đuổi |
| Ma Trơi | 8 | 2 | Giữ khoảng cách, bắn đạn chậm |
| Bù Nhìn | 40 | 1.5 | Chậm, sát thương va chạm cao |
| Ma Da | 12 | 6 khi lao | Nhịp 2s, báo hướng 0.45s rồi lao 0.5s |
| Quỷ Nhỏ | 15 | 2.5 | Chết tách hai Cô Hồn |

FSM điều khiển hành vi cá thể, không cần ML-Agents. Chằn Tinh ở wave 10: HP 800/1600 chốt theo một/hai người lúc xuất hiện, chuyển pha ở 50%. Pha 1 lao/phun đạn; pha 2 triệu hồi Ma Da/quét đuôi. Đòn báo trước, trống có thể ngắt. Chỉ số thực tế lấy asset; [ENEMY_TESTS.md](ENEMY_TESTS.md) và [BOSS_TESTS.md](BOSS_TESTS.md) là bằng chứng lịch sử, không thay playtest cấu hình 10 wave.

Trống nằm ở tâm, không chặn người chơi. Đủ 20 Hồn và hết hồi 20s, dash chạm trống sẽ xoá đạn địch, đẩy lùi 2.5 đơn vị, choáng 1s. Không gây sát thương. Năng lượng/hồi dùng chung do host quyết, HUD thể hiện trạng thái. Hồn vẫn nạp trong lúc hồi; Restart dọn Hồn cũ. Cơ chế đã có, giữ làm điểm riêng chính của đồ án.

## 7. AI đạo diễn — luật và FSM

### 7.1 Vai trò và thuật toán

**AI đạo diễn chính là Chằn Tinh, chủ dungeon.** Hắn điều binh trong wave 1–9 rồi trực tiếp xuất hiện ở wave 10; HUD thể hiện ý đồ/lời thoại từ kết quả host. “Giãn binh” tương ứng Hỗ trợ, “Điều binh” tương ứng Bình thường, “Đổi thế vây” tương ứng Đổi chiến thuật. Đây là cùng boss hiện có với hai tầng hành vi: điều binh giữa wave và FSM chiến đấu trong wave cuối. Ngưỡng/giới hạn gameplay giữ nguyên; lời thoại nằm trong `DungeonMaster.asset`. Giải thích từng bước, ví dụ co-op và cách báo cáo tại [AI_CHAN_TINH.md](AI_CHAN_TINH.md).

**AI dựa trên luật kết hợp FSM**, ba trạng thái: Hỗ trợ, Bình thường, Đổi chiến thuật. Chạy trên host trong cả một/hai người, quyết một lần trước wave 2–9. Wave 1/boss wave 10 giữ nguyên. Không LinUCB, không huấn luyện hoặc hàm thưởng ép tổn thất máu về 15–25%.

Mã hiện có: `Director/AIDirector.cs`, `ContextAggregator.cs`, `SafetyConstraints.cs` và partial `RunDirector.cs`. Ngưỡng/hệ số nằm trong `Data/Director/Resources/Director/DirectorRules.asset`. Bằng chứng và giới hạn kiểm thử cập nhật tại [DIRECTOR_TESTS.md](DIRECTOR_TESTS.md); cân bằng tự nhiên và nghiệm thu Internet vẫn là phần riêng.

### 7.2 Đầu vào

`healthMin`: HP ratio thấp nhất của người còn kết nối, người gục = 0. `damageRatioMax`: tổn thất thực trong wave/max HP đầu wave lớn nhất giữa các người. `downs`: lần gục của đội. `ContextAggregator` đóng băng HP/MaxHP khi kết thúc wave, trước hồi máu/nâng cấp; trước đợt kế chỉ cập nhật người vào/rời. Người rời không còn quyết định HP thấp nhất, nhưng tổn thất/lần gục đã ghi không bị xoá. Vào muộn không có lịch sử đủ wave thì đánh dấu thiếu ngữ cảnh và dùng baseline. Không dùng `clearTime` với wave tính giờ. Xem định nghĩa tại [THESIS_SCOPE.md](THESIS_SCOPE.md#4-đạo-diễn-ai-dễ-giải-thích).

### 7.3 Đầu ra — WaveSpec

```text
{ Mix[], Directions[], SpawnInterval, Burst, Duration }
```

Mix là thành phần **một chu kỳ lặp**, không phải tổng số quái sinh trong wave. Đạo diễn dựng đặc tả từ baseline; giữ Duration. `RunManager` chuẩn bị quyết định trước hồi máu đầu wave, rồi `WaveManager` trên host lấy đặc tả qua `GetHostWaveSpec`. Client không chạy chính sách thích ứng lần nữa; nó biểu diễn sự kiện sinh do host phát. Telemetry ghi cả baseline và đặc tả thực thi.

### 7.4 Quy tắc quyết định ban đầu

| Trạng thái | Điều kiện | Thay đổi |
|---|---|---|
| Hỗ trợ | HP thấp nhất ≤30%, hoặc tổn thất cao nhất ≥35%, hoặc có gục | Chu kỳ sinh ×1.25; burst `max(1, floor(baseline ×0.75))` |
| Đổi chiến thuật | HP thấp nhất ≥70%, tổn thất cao nhất ≤10%, không gục và không bị giữ ở Hỗ trợ | Đổi hướng/thành phần trong ngân sách baseline; không tăng HP/sát thương/tốc độ/quy mô/nhịp sinh |
| Bình thường | Còn lại | Bảng gốc |

Sau Hỗ trợ, chỉ thoát khi HP thấp nhất ≥50%, tổn thất cao nhất ≤20% và không gục. Tham số cần cân bằng tại T-51. Không biến một người mạnh thành lý do tăng áp lực lên người yếu.

### 7.5 An toàn, đồng bộ, fallback

Giữ trần 40 gồm quái con; điểm sinh tránh mọi người sống ≥3; chỉ dùng quái đã giới thiệu; không sửa asset trong ván. Thiếu ngữ cảnh/luật hoặc đầu ra thích ứng không hợp lệ thì dùng baseline hợp lệ và ghi lý do. Nếu chính baseline không hợp lệ, host dừng ván an toàn thay vì sinh quái từ cấu hình lỗi. Client nhận kết quả/sự kiện spawn/HUD, không tự quyết AI. WaveManager/EnemySpawner là nơi host thi hành spawn; reset ván xoá ngữ cảnh và trạng thái FSM.

### 7.6 Quan sát và đánh giá

Màn chọn lớp có **Cố định / Thích ứng** trước khi kết nối; vào host dùng chế độ host đã chọn, không đổi giữa ván hoặc khi Restart trong phòng. HUD góc dưới trái thể hiện ý đồ/lời thoại của Chằn Tinh, có chuyển động xuất hiện mỗi wave; **F3** mở lý do thật, ngữ cảnh, nguồn và fallback, cờ thiếu dữ liệu tách biệt với HP bằng 0. Lời thoại theo kết quả chung của host, không là thuật toán khác hoặc cơ chế tăng chỉ số boss.

CSV **schema 2** thêm `directors.csv`, một quyết định trước mỗi wave; `waves.csv` kèm quyết định của wave, `runs.csv` tổng hợp nguồn/số quyết định thực tế. `source` phân biệt `fixed_table`, `adaptive_rules`, `fixed_fallback` và cấu hình baseline lỗi; giữ tổn thất thô và bản kẹp [0,1]. CSV cũ vẫn là bằng chứng phiên cũ. Chi tiết tại [DIRECTOR_TESTS.md](DIRECTOR_TESTS.md). Đánh giá bằng kiểm thử luật, chơi thật và so sánh nhỏ theo THESIS_SCOPE; không mặc định bắt buộc 15/15 người.

## 8. Tiến trình ngoài ván

Không Ngọc, không mở khoá, không nhiều cấp khó, không lưu build. Chỉ lưu cài đặt cần thiết. Bản đồ án có một mức cân bằng cho người mới; ba lớp và 12 thẻ mở sẵn. Restart không cộng lợi thế từ ván trước.

## 9. Multiplayer online

Mirror + KCP, 1–2 người, host-authoritative. Chơi một người cũng StartHost. Tạo/vào bằng địa chỉ; không Steamworks/Steam invite. Dùng đường kết nối Internet được nhóm thiết lập và ghi rõ; không gọi localhost/LAN là nghiệm thu online.

T-40 đổi phạm vi sang hai máy ở hai mạng Internet khác nhau, giữ transport hiện có. Không làm host migration, tự reconnect hoặc matchmaking. T-42: client rớt thì host tiếp tục; host thoát thì client thông báo/về menu. Sảnh gọn cho phép chọn lớp và xác nhận vào phòng; host bắt đầu sau khi đủ người muốn chơi. Sảnh chính thức/menu chưa có, màn chọn thử nghiệm Arena đã có.

Máu/thẻ/lượt đổi độc lập. Đồng đội đứng cạnh 3s để hồi sinh; mọi người gục thì thua. Giả lập 100ms dùng khi phát triển; ghi độ trễ thật khi nghiệm thu Internet. Đồng bộ sự kiện, không gắn NetworkIdentity lên đạn/VFX.

## 10. Giao diện

Luồng tối thiểu: menu → chọn lớp → tạo/vào phòng → arena → kết quả/chơi lại/về menu. Tái sử dụng giao thức chọn lớp T-44C. HUD hiện có máu, wave/timer, trống, boss, hồi sinh và đạo diễn; F3 chỉ mở dữ liệu quan sát, không thay đổi gameplay. Màn thống kê T-49 dùng CSV/counters hiện có, không xây kho tiến trình.

Cài đặt tối thiểu: âm lượng, cửa sổ/toàn màn hình và thông số hiển thị sẵn của Unity; gán lại phím là dự phòng. Mở menu tạm dừng trong co-op không dừng thời gian cả đội. Thông báo thiếu host/rớt mạng phải rõ và có nút thử lại/về menu.

## 11. Mỹ thuật

Một arena nền tối hiện có; giữ nhóm đá đổi sắc wave 4/7, không cần ba tileset hoàn chỉnh. Không bụi cỏ. TinySwords cho ba lớp, TinyRPG Orc cho Cô Hồn, tài nguyên tạm cho quái/boss/trống còn lại. Ưu tiên hình dáng khác nhau và đọc được đòn, không bắt buộc tự vẽ toàn bộ.

TinySwords giữ frame 192×192/320×320 và PPU64; mỹ thuật nhóm nền 32×32/PPU32. Ma Da 16×16 theo TinyRPG là hướng có thể thử, chưa đổi tài nguyên game trong lần chỉnh phạm vi. Phải kiểm tra tỷ lệ, pivot/hitbox và ảnh Game View khi thay. Không áp một PPU chung lên mọi gói ngoài.

Palette Đông Hồ là chuẩn nền/VFX của nhóm, giữ nhóm son cho đòn địch. Asset ngoài được giữ nguồn; không cần tô lại cả gói. Cho phép dùng công cụ AI hỗ trợ thiết kế/ảnh, kiểm tra và chỉnh sprite/animation trước nhập Unity; ghi đúng nguồn và mức đóng góp. Ảnh AI tạo tài nguyên không thay thế AI gameplay. Không còn bảng cấm AI mỹ thuật và yêu cầu capsule Steam của thiết kế thương mại cũ.

Danh mục bắt buộc: sprite dùng được cho ba lớp/năm quái/boss/trống, 12 icon nền rõ nghĩa (có thể tái sử dụng/ký hiệu), một tileset hiện có. Không áp số frame/icon của thiết kế chưa triển khai. Nguồn hiện hành: [ASSETS_ThirdParty.md](ASSETS_ThirdParty.md).

## 12. Âm thanh

Giữ SFX có sẵn; thêm một nhạc nền và vài tín hiệu thiết yếu nếu còn thời gian: nhặt Hồn, dùng trống, trúng đòn, boss/thắng/thua. Không bắt buộc bốn bản nhạc hoặc âm riêng cho từng quái. Âm lượng điều chỉnh được.

## 13. Kỹ thuật và kiểm chứng

Unity 6000.5.6f1, URP2D, Mirror, Git/LFS. Nội dung dùng ScriptableObject; pool cho sinh lặp; RunRandom cho gameplay; một luồng host cho solo/co-op. Ngân sách 60 FPS với 40 quái và 200 đạn; số đo fixture không thay thế chơi thật hoặc benchmark trên máy bàn giao.

Windows build mới hiện bị Application Control chặn Unity DLL; cần build trên môi trường hợp lệ và nghiệm thu executable sau các thay đổi. Không bắt buộc khởi chạy qua Unity Editor để bảo vệ. Không đổi chính sách Windows trong lần chỉnh tài liệu này.

## 14. Phạm vi và thứ tự bàn giao

**Bắt buộc:** gameplay đã có, 10 wave/một arena, co-op qua Internet, FSM quái và đạo diễn ba trạng thái, Trống Đồng/Hồn, menu/phòng/mất kết nối, kết quả, telemetry, build/video/báo cáo.

**Dự phòng:** 1–2 tiến hoá, mỹ thuật tinh chỉnh, gán lại phím, nhạc bổ sung. **Ngoài phạm vi:** phát hành Steam/thương mại, shop, Ngọc/mở khoá, nhiều cấp khó/map mới, nhân vật/boss thêm, mobile, matchmaking, quảng bá.

Thứ tự: ổn định môi trường → đạo diễn và online/menu song song → cân bằng/thống kê → build hai máy → đánh giá/báo cáo/diễn tập. Phân công giữ theo TASKS; không cắt online xuống co-op cục bộ. Tên đề tài và tiêu chí chấm cuối cùng theo giảng viên; không cam kết đạt tốt nghiệp chỉ dựa trên phạm vi này.

## 15. Bối cảnh

Trống Đồng giữ cân bằng giữa các cõi. Chằn Tinh làm chủ dungeon, điều khiển các đợt ma quỷ thử sức người xâm nhập; khi quân không cản được đội, hắn tự ra trận ở wave 10. Người chơi cùng bảo vệ trống và hạ hắn. Ý đồ/lời thoại ngắn trên HUD nối các wave với cùng một đối thủ; không cần cutscene/lồng tiếng. Thánh Gióng là ảnh AI đã chuẩn hóa; Archer/Lancer dùng asset ngoài, không nhận là sprite tự vẽ.

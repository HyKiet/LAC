# Phạm vi đồ án và đánh giá LẠC

Quyết định 11/10/2026: **chỉ làm đồ án tốt nghiệp, không phát hành thương mại**. Người dùng xác nhận giảng viên không yêu cầu học máy/LinUCB hoặc thực nghiệm đủ 30 người. Tài liệu này ghi phạm vi đã thu gọn; ràng buộc kiến trúc vẫn theo [CLAUDE.md](../CLAUDE.md), trạng thái công việc theo [TASKS.md](TASKS.md).

## 1. Yêu cầu đã biết của giảng viên

> Game phải chơi online multiplayer, có sử dụng AI, phải có yếu tố mới lạ độc đáo.

Đây là yêu cầu chức năng được cung cấp, chưa phải toàn bộ rubric chấm điểm. Steam, thuật toán học máy, tám công thức tiến hoá và mẫu 30 người là lựa chọn trong thiết kế cũ, không phải yêu cầu trong tin nhắn. Phạm vi mới không cam kết trước điểm số hoặc việc hội đồng chấp nhận đề tài.

| Yêu cầu | Cách đáp ứng được chọn | Bằng chứng cần bàn giao |
|---|---|---|
| Online multiplayer | Hai người cùng chơi qua Internet, Mirror + KCP; host quản lý kết quả | Hai máy ở hai mạng khác nhau hoàn thành ván, log kết nối, video, hướng dẫn thiết lập |
| Có AI | FSM quái/boss hiện có + đạo diễn thích ứng dựa trên luật, ba trạng thái | Sơ đồ trạng thái, bảng luật, mã nguồn, tình huống chuyển trạng thái và CSV quyết định |
| Yếu tố riêng | Hồn nạp Trống Đồng dùng chung, dash kích hoạt giải nguy; đạo diễn xét người yếu nhất | Demo hai người nạp/kích hoạt trống, hồi sinh và chứng minh AI đổi cách sinh quái |

Trống Đồng và đạo diễn là điểm riêng trong cách kết hợp của đồ án. Không tuyên bố đây là cơ chế chưa từng có ở bất kỳ game nào hoặc đóng góp thuật toán AI mới.

## 2. Đánh giá hiện trạng

**Mốc đánh giá ban đầu ngày 11/10/2026, trước triển khai đạo diễn:** dựa trên tài liệu thiết kế, báo cáo kiểm thử trong `docs/`, dữ liệu kiểm chứng và kiểm tra các điểm nối mã nguồn; không phải nghiệm thu chạy game mới trong phiên thu gọn tài liệu. Bảng dưới giữ bối cảnh của mốc đó; cập nhật triển khai sau đánh giá được ghi ngay bên dưới.

**Nền gameplay khá đầy đủ cho một đồ án.** T-44D đã có 10 wave, ba lớp, đòn đánh khác nhau, thẻ, năm loại quái, boss, thắng/thua/chơi lại. Kiến trúc host-authoritative, pool, seed và CSV tạo cơ sở tốt để giải thích kỹ thuật. Lợi ích lớn nhất của thu gọn là tránh thêm hệ thống trước khi hoàn tất ba yêu cầu chính.

| Mảng | Bằng chứng tại mốc đánh giá | Khoảng trống tại mốc đánh giá |
|---|---|---|
| Gameplay | [TEN_WAVE_TESTS.md](TEN_WAVE_TESTS.md), [PLAYABLE_CLASSES_TESTS.md](PLAYABLE_CLASSES_TESTS.md) | Chưa playtest cân bằng trọn ván tự nhiên của cả ba lớp |
| Co-op | Hai tiến trình/Editor, latency simulation, đồng hồ, thẻ, hồi sinh, snapshot | Chưa chứng nhận hai máy qua Internet; T-42 chưa hoàn thành |
| AI quái/boss | [ENEMY_TESTS.md](ENEMY_TESTS.md), [BOSS_TESTS.md](BOSS_TESTS.md) | `Director` mới có telemetry ở mốc đánh giá; phần triển khai T-45/T-45B/T-46 được bổ sung sau đó |
| Điểm riêng | [DRUM_TESTS.md](DRUM_TESTS.md), [INSTRUMENT_REVIVE_TESTS.md](INSTRUMENT_REVIVE_TESTS.md) | Cần một kịch bản demo dễ quan sát, không mở thêm cơ chế mới |
| Thẻ | 12 thẻ đã có, bốc/lọc/đồng bộ và giới hạn cộng dồn | Tám tiến hoá chưa có, công thức cũ không khớp ID/giới hạn thẻ hiện tại |
| Giao diện | HUD máu/wave/trống/boss, màn chọn lớp thử nghiệm, màn kết quả cơ bản | Luồng menu/phòng, thông báo mất kết nối và thống kê sau ván cần khép lại |
| Mỹ thuật | TinySwords, TinyRPG Orc, nền tối và tài nguyên tạm; đã xem ảnh màn chọn/wave 9 tại `validation/T44D` khi đánh giá | Màn chọn/HUD đọc rõ, thân quái còn khác phong cách; ưu tiên silhouette và tỷ lệ, không vẽ lại toàn game |
| Hiệu năng | T-44D P95 4.98 ms với fixture 40 quái/200 đạn trên máy phát triển | Không suy rộng sang mọi máy; đo lại sau khi tích hợp đạo diễn |
| Bàn giao | Build cũ từng thành công | Build mới bị Application Control chặn Unity DLL; bản 10 wave chưa có executable nghiệm thu |

**Bổ sung cùng ngày, sau triển khai T-45/T-45B/T-46:** đã có đạo diễn luật/FSM, luật ScriptableObject, snapshot đội đóng băng trước thẻ/hồi máu, nguồn WaveSpec trên host, chọn Cố định/Thích ứng, HUD/F3 và CSV schema 2. Kiểm tra đã chạy gồm 863 xác nhận luật, 75 kiểm tra tích hợp host, 76 kiểm tra hai Editor và 29 kiểm tra co-op có kiểm soát; phần co-op gồm hai lượt hồi sinh thật, tổn thất thô vượt 100%, rời khi chọn thẻ, vào muộn giữa wave/lúc chọn thẻ và phục hồi ngữ cảnh. Bằng chứng và giới hạn tại [DIRECTOR_TESTS.md](DIRECTOR_TESTS.md). Các fixture này không thay thế chơi tự nhiên để cân bằng, hai máy qua Internet hoặc bản Windows mới.

Tại mốc đánh giá, tài liệu cũ lẫn thiết kế tương lai với chức năng đã có: README còn 15 phút/Steam; GDD dùng `clearTime` cho wave tính giờ, mục tiêu tổn thất hai chiều và tiến trình Ngọc; ARCHITECTURE mô tả LinUCB chưa triển khai; learning ghi trạng thái đầu dự án. Phạm vi và mô tả hiện hành đã được cập nhật; các báo cáo lịch sử 15/16 wave giữ nguyên kết quả, không sửa thành bằng chứng 10 wave.

## 3. Bản tối thiểu để bảo vệ

- PC Windows, hai người co-op online; chơi một người vẫn là Mirror host.
- Một arena hiện có, 10 wave: chín wave 20–60 giây, boss Chằn Tinh ở wave 10.
- Giữ Warrior/Archer/Lancer, năm loại quái, boss hai pha và 12 thẻ đã làm.
- Giữ dash, Hồn, Trống Đồng dùng chung, hạ gục/hồi sinh và chơi lại.
- Menu gọn: chọn lớp, tạo/vào phòng bằng địa chỉ, trạng thái kết nối, vào ván, thoát; chỉ host bắt đầu khi hai người đã vào nếu chơi đôi. Solo vẫn được bắt đầu với một người.
- Đạo diễn ba trạng thái chạy trên host, có bật/tắt trước ván, HUD/debug và CSV để giải thích quyết định.
- Màn tổng kết dùng dữ liệu đã có: kết quả, wave, thời gian, quái hạ, sát thương nhận và số lần dùng trống.
- Một bản Windows chạy trên hai máy, tài liệu sử dụng, sơ đồ kiến trúc, kiểm thử và video demo.

Không thay sprite TinySwords toàn bộ bằng 16×16 trong giai đoạn này. Sprite Ma Da TinyRPG 16×16 là hướng nội dung được cân nhắc; phải kiểm tra tỷ lệ thế giới, pivot, animation và khả năng đọc trước khi tích hợp. Kích thước texture không tự quyết định hitbox.

**Bỏ khỏi phạm vi bắt buộc:** Steamworks/invite/Steam Direct/giá bán, Ngọc và mở khoá, shop/nhiều ô vũ khí, tám tiến hoá, hai tileset mới, bộ nhạc riêng theo từng bối cảnh, thành tích/bảng xếp hạng/mobile/marketing. Không xoá tính năng đã chạy chỉ để giảm số mục. Tiến hoá có thể làm tối đa 1–2 công thức bằng hiệu ứng sẵn có sau khi các tiêu chí chính đạt; không là điều kiện bảo vệ.

## 4. Đạo diễn AI dễ giải thích

**Chọn AI dựa trên luật + FSM; không gọi là AI học máy.** Không cần huấn luyện, ma trận LinUCB, online learning hoặc tối ưu hàm thưởng. FSM quái vẫn thể hiện AI cấp cá thể; đạo diễn thể hiện quyết định thích ứng ở cấp trận đấu. [Unity mô tả FSM như một thuật toán AI hành vi](https://learn.unity.com/project/finite-state-machines-1).

Chỉ quyết định **một lần trước mỗi wave 2–9**, từ snapshot cuối wave trước, trước hồi máu/nâng cấp đầu wave mới. Wave 1 và boss wave 10 giữ cấu hình gốc. Chọn chế độ Fixed/Adaptive trước ván và ghi vào CSV; không đổi giữa ván đối chứng.

Triển khai hiện có tại `Director/AIDirector.cs`, `ContextAggregator.cs`, `SafetyConstraints.cs` và partial `RunDirector.cs`. Ngưỡng/hệ số nằm trong `Data/Director/Resources/Director/DirectorRules.asset`. Màn chọn lớp chốt chế độ trước kết nối, host quyết chế độ cho cả đội; Restart trong phòng giữ chế độ đã chọn. `WaveManager` trên host lấy đầu ra qua `RunManager.GetHostWaveSpec`, client chỉ biểu diễn sự kiện sinh và quyết định đã đồng bộ.

### Dữ liệu đầu vào nhỏ

Theo yêu cầu người dùng cùng ngày: đạo diễn chính là Chằn Tinh, chủ dungeon. Hắn điều binh wave 1–9 rồi ra trận ở wave 10; ý đồ/lời thoại trình bày kết quả host, không thêm boss hoặc đổi luật chỉ để tăng tính đối kháng. Hướng dẫn dễ hiểu tại [AI_CHAN_TINH.md](AI_CHAN_TINH.md).

| Giá trị | Định nghĩa |
|---|---|
| `healthMin` | Tỉ lệ HP thấp nhất trong người còn kết nối; người gục có tỉ lệ 0 |
| `damageRatioMax` | Lớn nhất của máu thực mất trong wave / máu tối đa đầu wave, xét từng người |
| `downs` | Tổng lần bị hạ gục trong wave trước |

Máu mất cộng từ sát thương đã được host chấp nhận, không tính `HP đầu − HP cuối` vì hồi máu/hồi sinh có thể che tổn thất. `damageRatioMax` kẹp [0,1] cho quyết định; CSV giữ giá trị thô, kể cả tổn thất vượt máu tối đa. HP/MaxHP được đóng băng cuối wave trước thẻ/hồi máu; trước wave kế chỉ cập nhật người vào/rời. Người rời không còn quyết định `healthMin`, nhưng tổn thất/lần gục đã ghi được giữ. Người vào muộn thiếu lịch sử đầy đủ thì ngữ cảnh không hợp lệ, không giả tạo lịch sử cả wave; dùng bảng cố định cho quyết định đó và thu mẫu mới từ wave kế.

### Ba trạng thái — tham số khởi đầu, chưa cân bằng

| Trạng thái | Điều kiện vào theo thứ tự ưu tiên | Hành động |
|---|---|---|
| Hỗ trợ / Relief | `healthMin ≤ 0.30`, hoặc `damageRatioMax ≥ 0.35`, hoặc có hạ gục | Giãn chu kỳ sinh ×1.25; giảm burst còn `max(1, floor(burst ×0.75))`; không tăng số quái khó |
| Đổi chiến thuật / Variety | Không cần hỗ trợ, `healthMin ≥ 0.70`, `damageRatioMax ≤ 0.10`, không hạ gục | Đổi hướng xuất hiện/thành phần trong ngân sách baseline; không tăng HP, tốc độ, sát thương, burst hoặc tốc độ sinh |
| Bình thường / Balanced | Các trường hợp còn lại | Dùng cấu hình bảng gốc |

Nếu wave trước ở Relief, chỉ thoát khi `healthMin ≥ 0.50`, `damageRatioMax ≤ 0.20` và không hạ gục. Đây là khoảng trễ để tránh đổi trạng thái liên tục. Chỉ hoán đổi loại quái đã được giới thiệu đến wave đó; không đưa boss vào wave thường. Ngân sách phải tính cả quái con, không dùng `TotalCount` như tổng số quái của cả wave tính giờ.

Chế độ hai người dùng **cùng thuật toán**, chỉ tổng hợp dữ liệu đội; không thêm mô hình học hoặc số hạng công bằng. Không tăng áp lực chỉ vì một người mạnh khi đồng đội sắp gục.

Tầng an toàn luôn áp dụng cho Fixed và Adaptive: tối đa 40 suất quái kể cả con tách; điểm sinh cách mọi người sống ít nhất 3 đơn vị; giữ Duration; thiếu điểm sinh thì bỏ lượt/chờ lượt sau; không sửa ScriptableObject trong ván. Thiếu dữ liệu/luật hoặc đầu ra thích ứng không hợp lệ thì dùng WaveSpec baseline hợp lệ và ghi lý do fallback. Nếu chính baseline không hợp lệ thì host dừng ván an toàn, không tự dựng một wave từ cấu hình lỗi.

HUD đã giải thích hành động/lý do ở phía trên thanh máu góc dưới trái; **F3** mở ngữ cảnh, nguồn, lý do và fallback. Thiếu ngữ cảnh được ghi rõ, không hiển thị như người chơi có 0% HP. CSV **schema 2** thêm `directors.csv` với một quyết định mỗi wave, gồm mode, trạng thái, ba đầu vào, lý do, baseline/chỉ số áp dụng và fallback; `waves.csv` gắn quyết định wave, `runs.csv` tổng hợp nguồn và số quyết định thực. Nguồn phân biệt bảng cố định, luật thích ứng và fallback, không giả gắn mọi ván chọn Adaptive là đã thích ứng. Không dùng CSV cũ để chứng minh AI mới; hướng dẫn đọc và kiểm chứng tại [DIRECTOR_TESTS.md](DIRECTOR_TESTS.md).

`clearTime` bị bỏ khỏi đầu vào: wave tính giờ luôn hết sau Duration nên thời gian hoàn thành không đo hiệu quả tiêu diệt. Không đặt mục tiêu ép người chơi mất 15–25% HP.

## 5. Nghiệm thu online không cần Steam

Giữ Mirror/KCP và luồng nhập địa chỉ đã có. Chuẩn bị một đường kết nối qua Internet có thể tái tạo: host có UDP port forwarding nếu mạng cho phép, hoặc đường mạng riêng/relay phù hợp với môi trường nhóm. Phải ghi rõ hạ tầng trung gian được sử dụng; CGNAT có thể làm cách mở cổng không dùng được. Chưa chọn/triển khai thêm dịch vụ trong lần chỉnh tài liệu này. [Mirror FAQ về kết nối và port forwarding](https://mirror-networking.gitbook.io/docs/manual/faq).

Hai Editor trên cùng máy, localhost hoặc LAN chỉ là kiểm thử phát triển. Nghiệm thu bắt buộc dùng hai máy và hai kết nối Internet khác nhau; video ghi cấu hình và luồng tạo/vào phòng. Không thay yêu cầu online bằng co-op cục bộ.

Không làm host migration, reconnect tự động hoặc matchmaking. Khi client rớt, host tiếp tục được với người còn lại; khi host thoát, client nhận thông báo và về menu để tạo/vào lại. Quy tắc này phải được kiểm thử cả lúc chiến đấu và chọn thẻ.

## 6. Đánh giá vừa sức và trung thực

Không còn mặc định thực nghiệm 15/15 là điều kiện hoàn thành. Mục tiêu là chứng minh hệ thống chạy đúng và quan sát tác động của đạo diễn; không tuyên bố AI luôn vui hơn hoặc tốt hơn trước khi có dữ liệu.

1. **Kiểm thử chức năng:** mỗi trạng thái AI, ngưỡng vào/ra, thiếu dữ liệu, fallback, boss giữ nguyên, giới hạn 40, an toàn sinh quái, đồng bộ HUD/CSV, reset ván. Dựng tình huống có kiểm soát và phân biệt với chơi thật.
2. **Chơi thật:** thử cả ba lớp và đủ 10 wave; ghi nơi dễ chết, thời gian boss và lỗi đọc đòn. Sửa cấu hình T-50/T-51 cho người mới có thể hiểu cơ chế và tiến bộ.
3. **So sánh nhỏ:** đề xuất 8–12 người, mỗi người thử Fixed và Adaptive với lớp/cấu hình thống nhất, thứ tự chia AB/BA và seed tương ứng. Hai lượt phải reset nâng cấp/trạng thái AI. Đây là mục tiêu kế hoạch, chưa có dữ liệu; số mẫu cuối theo lịch và hướng dẫn của giảng viên.
4. **Online:** ít nhất ba ván trọn vẹn qua hai mạng khác nhau; thử client rời, host thoát, hồi sinh, trống và lượt chọn thẻ. Ghi độ trễ thực, không chỉ độ trễ giả lập.

Tổng kết bằng tỉ lệ hoàn thành, số lần hạ gục, tổn thất HP, phân bố trạng thái AI và nhận xét dễ hiểu/vừa sức/phối hợp. Với mẫu nhỏ dùng thống kê mô tả và nói rõ hạn chế; không coi mỗi dòng wave hoặc mỗi người trong cùng đội là mẫu độc lập. Chỉ số `source` và quyết định phải phân biệt Fixed/Adaptive, không bịa log AI.

## 7. Thứ tự hoàn thiện và phân công

| Thứ tự | Hạng mục | Người |
|---|---|---|
| 1 | T-01: mở dự án/build trên máy nhóm, gỡ điểm chặn build bằng môi trường hợp lệ | Hung/Kang, Kiệt hỗ trợ kiểm tra |
| 2 | T-45 → T-45B → T-46: đạo diễn ba trạng thái, ngữ cảnh đội, HUD/CSV | Kiệt |
| Song song | T-60/T-62/T-30: menu/phòng gọn; T-40 online qua Internet; T-42 mất kết nối | Kang |
| 3 | T-49: thống kê cuối ván; T-50/T-51: cân bằng ván cho người mới | Kang / Kiệt |
| 4 | T-55: build Windows đầy đủ; kiểm thử hai máy/Internet và ghi video | Chưa phân công |
| 5 | T-53/T-54/T-59: thu dữ liệu, báo cáo, slide và diễn tập | Kiệt / nhóm |

Chặng đạo diễn đã có mã và các kiểm thử tích hợp nêu ở mục 2; trạng thái hoàn tất hạng mục lấy [TASKS.md](TASKS.md) cùng bằng chứng tại [DIRECTOR_TESTS.md](DIRECTOR_TESTS.md). Công việc còn lại vẫn ưu tiên nghiệm thu Internet, menu/mất kết nối, cân bằng và build bàn giao; không mở lại phạm vi thương mại.

Hung ưu tiên kiểm tra/hồi quy hệ thống thẻ sẵn có và cùng hiểu luồng mạng; không bắt buộc phát triển tám tiến hoá. Việc nhận thêm mã ngoài thư mục sở hữu vẫn cần nhóm thống nhất. T-33/T-47 chỉ hoàn thiện icon/silhouette thiết yếu nếu còn thời gian; không làm lại hệ thống mỹ thuật.

## 8. Cách trình bày khi bảo vệ

Tên đề tài gợi ý: **“Xây dựng game sinh tồn 2D co-op trực tuyến với cơ chế Trống Đồng và điều tiết đợt quái bằng AI dựa trên luật.”** Đây là gợi ý tên, không tự thay tên đề tài đã đăng ký.

Mỗi thành viên phải giải thích được phần mình: host/client và thẩm quyền sát thương; luồng wave → chọn thẻ → wave; tại sao pool/seed; FSM quái; điều kiện chuyển trạng thái đạo diễn; Trống Đồng dùng chung; nguồn và giới hạn telemetry. Không nhận asset ngoài hoặc ảnh AI là tự vẽ.

Kịch bản trình bày: vào phòng hai máy → chọn hai lớp → chơi wave → cùng nhặt Hồn/kích hoạt trống → hồi sinh → minh hoạ một quyết định AI có lý do → boss/kết quả → mở CSV. Chuyển thẳng wave hoặc đặt HP bằng công cụ debug phải nói rõ là thao tác demo, không là bằng chứng chơi trọn ván. Giữ video và CSV dự phòng cho sự cố mạng tại buổi bảo vệ, nhưng vẫn có nghiệm thu online thật trước đó.

Chỉ chốt hoàn tất đồ án khi có bản build chạy được, multiplayer Internet được kiểm chứng, AI và điểm riêng quan sát được, nhóm giải thích được mã và báo cáo phản ánh đúng bằng chứng.

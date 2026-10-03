# Phản hồi thẻ trong trận

Hoàn tất 03/10/2026. Bổ sung bốn dấu hiệu quanh nhân vật để đọc được tác động của
thẻ sau khi màn chọn đóng. Phần này chỉ thêm biểu diễn trong phạm vi Cards;
chỉ số, sát thương, máu, thời gian bất tử, lịch đợt và công thức tiến hoá giữ
nguyên kết quả của [T-24B](CARD_IMPACT_BALANCE.md).

## Bốn hiệu ứng

| Hiệu ứng | Khi nào xuất hiện | Cách đọc |
|---|---|---|
| Vòng đồng nhận nâng cấp | Số cấp thẻ tăng trong lượt chọn; phát khi trở lại đợt chiến đấu | Một vòng nét đồng nở nhẹ trong 0,85 s, xác nhận nâng cấp đã áp dụng |
| Dấu hồi máu và số máu | Máu thực nhận tăng, nhân vật có hiệu ứng tăng máu tối đa hoặc hồi đầu đợt | Dấu cộng màu chàm sáng và số nguyên nổi trên đầu trong 0,9 s; số lấy từ chênh lệch máu thật |
| Vệt gió | Nhân vật có hệ số tốc đi lớn hơn 1 và đang thực sự di chuyển, ngoài pha dash | Ba nét gió ngắn lưu ở vị trí đã đi qua, sống 0,4 s, cách nhau tối thiểu 0,2 s |
| Dấu Thiết Bích | Máu thực giảm nhưng nhân vật còn sống và có hệ số bảo vệ sau trúng đòn lớn hơn 1 | Viền khiên loé 0,35 s để báo hiệu cơ chế bảo vệ sau trúng đòn vừa kích hoạt |

Dấu khiên là **thông báo kích hoạt**, không tạo thêm lớp giáp, không hấp thụ
sát thương và không biểu diễn toàn bộ thời gian bất tử. Đòn đã bị chặn không
làm giảm máu nên không phát thêm dấu khiên. Các bonus tiến hoá tương ứng cũng
được nhận diện qua chỉ số thực tế, không cần thêm nhánh theo tên thẻ.

Số hồi máu phản ánh số máu đã nhận, kể cả khi hồi bị giới hạn bởi máu tối đa.
Máu không đổi hoặc hook lặp cùng giá trị không phát số mới; nhân vật đã gục
không được coi là vừa hồi máu. Bộ quan sát đọc thay đổi máu đã đồng bộ, không
nhận định danh nguồn hồi từ Combat, nên số này không phân loại nguồn hồi.

## Đọc hiểu và giới hạn hiển thị

Nét đồng dùng Hoè `#EDBB3E`; dấu hồi và khiên dùng Chàm Sáng `#9CCFC0`; vệt gió
dùng Gỉ Đồng `#4FA694`, theo [bảng Đông Hồ](PALETTE.md). Nét vẽ dùng vật liệu
additive hiện có, alpha tối đa 0,22; vệt gió mặc định 0,18. Sorting order của
nét là 4, số hồi là 8, dưới nhân vật thử nghiệm ở order 10. Không dùng nhóm son
dành cho đòn địch. Số dùng sprite chữ số pixel hiện có, không thêm phông vector.

Dấu nhận thẻ, hồi và khiên đi theo nhân vật; vệt gió ở lại vị trí phát.
Điểm đặt số hồi lấy từ mép trên sprite, có giới hạn để phần trong suốt của
sprite thử nghiệm không đẩy số quá xa. Nét giữ độ sáng trong phần đầu rồi mờ
dần. Hiệu ứng không làm phóng to hoặc nhấp nhô nhân vật.

Hiệu ứng chỉ hiển thị khi `RunState.WaveActive`. Khi chọn thẻ/tạm dừng, không
phát thêm vệt gió; thông báo nhận thẻ và hồi trong lượt chọn chờ chiến đấu
tiếp tục. Kết thúc ván, chết, đổi nhân vật hoặc reset lịch sử dọn thông báo
đang chờ phù hợp với vòng đời nhân vật. Không dùng dấu hiệu này để suy ra tầm
đánh, vùng nổ hay trạng thái bất tử có thẩm quyền.

## Mạng và hiệu năng

`CardBattleFeedback` được gắn bởi `CardSelectionController`, theo dõi cả hai
nhân vật qua `PlayerRegistry`. Mỗi máy tự vẽ từ lịch sử thẻ và máu đã đồng bộ:
không thêm RPC, SyncVar, `NetworkIdentity` hay thay đổi mô phỏng host. Chơi một
người vẫn đi qua Mirror host mode.

Observer được gắn và cập nhật trong `LateUpdate`, sau bước replay lịch sử
thẻ trong `RunManager.Update`. Snapshot khi mới vào ván được lấy làm baseline.
Delta hồi máu có thể đến trước replay thẻ, nên được giữ tạm để xét lại với
chỉ số cuối; không suy đoán lượng hồi từ mô tả thẻ. Khi cấp thẻ giảm do reset,
baseline và các thông báo chờ được đặt lại.

Pool tạo sẵn theo dữ liệu, mặc định 24 đối tượng dùng chung. Khi hết đối tượng
nhàn rỗi, bỏ yêu cầu trang trí mới; không mở rộng pool giữa trận. Mesh, mảng
đỉnh/chỉ số và `MaterialPropertyBlock` được tái sử dụng. Danh sách nhân vật
được duyệt bằng chỉ số; không tìm đối tượng theo tên hoặc tạo hiệu ứng bằng
`Instantiate` cho mỗi lần phát. Thời lượng hiệu ứng theo thời gian gameplay.

Các giới hạn này kiểm soát chi phí của phần Cards, chưa chứng minh ngân sách
60 FPS với 40 quái/200 đạn và mọi hiệu ứng chiến đấu cùng hoạt động.

## Hoàn thiện vòng đời và vệt gió — 03/10/2026

Vệt Khinh Thân nay nghe sự kiện `Dashed` hiện có và chờ hết thời lượng dash
cộng 0,35 s trước khi phát lại. Bộ lọc độ dời cũng loại bước nhanh hơn
1,75 lần tốc đi thật, dùng bước thời gian tối thiểu bằng `fixedDeltaTime` để
không loại nhầm di chuyển ở FPS cao. Hai tham số trang trí lưu trong
`CardBattleFeedback.asset`; không thay quãng đường, hồi chiêu hoặc bất tử dash.
Điều này chặn bước vật lý cuối và đuôi nội suy mạng bị nhận nhầm là đi bộ.

Hiệu ứng đang chạy được trả pool ngay khi target bị disable/ẩn hoặc đổi
`CharacterData`. Disable Feedback thu hồi cả pool khi danh sách observer đã
rỗng. Observer tháo đăng ký `Dashed`, máu và thẻ khi rời vòng đời; bật lại không
nhân đôi phản hồi. `Emit` cũng từ chối target đang bị ẩn.

Bộ kiểm host mở rộng đạt `HOST ALL PASSED`: dash qua input thật, đi tiếp sau
dash, disable component/GameObject/Feedback, pool còn active với observer rỗng,
đổi nhân vật khi hồi đang chờ, kiểm số subscriber và hit/heal sau re-enable.
Các thay đổi dữ liệu của fixture được khôi phục, asset hiệu ứng giữ nguyên.

Kiểm co-op bổ sung bằng `CardBattleCoopChecks.PrepareLateJoin()`:
host nhận Khinh Thân rồi mở chọn Sinh Lực ở đợt 2, client mới chạy với
`--lac-card-battle-probe` vào ngay màn chọn. Fixture tự tiếp tục khi đủ hai
người, chọn Khinh Thân cho cả hai ở lượt sau và kiểm walk/dash/idle/resume
qua Input System trên client. Chỉ lượt chờ khởi chạy client trong fixture được
nới 300 s; cấu hình chọn thẻ của game vẫn 10 s. Cleanup trả danh mục thẻ và
thời gian bảo vệ về giá trị cũ. Host ghi `LATE JOIN PASSED`,
`REMOTE MOVEMENT PASSED` và `ALL PASSED`; client ghi `MOVEMENT PASSED`.

| Trường hợp | Host / client |
|---|---|
| Client mới vào màn chọn đợt 2 | Client dựng Khinh Thân cấp 1 của host, 0 vòng nâng cấp/0 hồi giả |
| Sinh Lực trong lượt đã mở trước khi client vào | Chỉ host nhận thẻ/hồi; người vào muộn bắt đầu chọn ở lượt sau |
| Đồng đội đi bộ | 5 / 5 vệt gió |
| Dash và đuôi nội suy | 0 / 0 vệt gió |
| Đứng yên rồi đi tiếp | Client báo idle 0, resume 4; host có tổng 9 vệt trước khi tự di chuyển |
| Reset ván co-op | Pool active 0, không lặp vòng nâng cấp hoặc hồi cũ |

Pool hai máy vẫn 24. Transport giữ LatencySimulation 100 ms, jitter 0,02,
loss/scramble 2%. Đây là kiểm trigger Cards: code review còn chỉ ra bộ đếm ID
quái của `EnemySpawner` không replay các đợt đã qua cho client vào ở màn chọn.
Vì vậy không dùng lượt này để xác nhận đồng bộ quái; vấn đề lõi được ghi nhận
ngoài phạm vi chỉnh sửa Cards.

Điều kiện chạy Editor: phép thử cần một lần nạp domain sạch. Với tuỳ chọn
`DisableDomainReload` bật, phiên khởi động đầu đã gặp trạng thái Mirror/Input
System cũ và lỗi hoạt ảnh ngoài Cards. Đã bật domain reload tạm cho kiểm thử;
tuỳ chọn Editor gốc được khôi phục khi dọn lượt. Một lời gọi MCP trúng lúc nạp
domain còn ghi 7 lỗi `ThreadAbortException`/tool và 4 cảnh báo plugin; stack
đều ngoài Cards. Đã tách log khởi động đó và chạy lại host ổn định cuối:
`HOST ALL PASSED`, Console 0 lỗi đỏ/0 cảnh báo.

## Mã và tài sản

- `Assets/_LAC/Scripts/Cards/CardBattleFeedback.cs`: gắn observer, quản lý pool và vòng đời ván.
- `Assets/_LAC/Scripts/Cards/CardBattleObserver.cs`: đọc thay đổi thẻ/máu/di chuyển và chống phát trùng.
- `Assets/_LAC/Scripts/Cards/CardBattleEffect.cs`: hình nét, số hồi, theo nhân vật, mờ dần và trả pool.
- `Assets/_LAC/Scripts/Cards/CardBattleFeedbackData.cs` và `Data/Cards/Resources/CardBattleFeedback.asset`: màu, thời lượng, nhịp và dung lượng pool.
- `Assets/_LAC/Prefabs/UI/Cards/CardBattleEffect.prefab`: prefab trong phạm vi Cards, dùng lại vật liệu/`PixelNumber` hiện có.
- `Assets/_LAC/Scripts/Cards/Editor/CardBattleFeedbackSetup.cs`: tạo tài sản thiếu, giữ nguyên tài sản đã có.
- `Assets/_LAC/Scripts/Cards/Editor/CardBattleFeedbackChecks.cs`: kiểm thử tích hợp host trong Play mode.
- `Assets/_LAC/Scripts/Cards/Editor/CardBattleCoopChecks.cs`: chuẩn bị và tự kiểm client vào muộn, chuyển động qua mạng, reset và cleanup fixture.
- `Assets/_LAC/Scripts/Cards/CardBattleNetworkProbe.cs`: chỉ cài trong Editor/Development khi có flag `--lac-card-battle-probe`; nhập walk/dash và báo bộ đếm client.

## Kiểm chứng

Menu kiểm: `LAC > Tests > Check Card Battle Feedback (Play Mode)`, với host
một người. Bộ kiểm đi qua đề nghị/chọn thẻ thật, sát thương và hồi máu phía
host; kiểm Sinh Lực, Hồi Xuân, Thiết Bích, Khinh Thân, hook máu trùng, đầy máu,
đòn bị chặn, đứng yên, pause, vòng đời hiệu ứng, restart và dữ liệu bất biến.
Fixture có tăng tốc hoàn tất đợt để thử lựa chọn, không đại diện một ván tự
nhiên. Thử 1.000 yêu cầu phát kiểm việc pool không vượt dung lượng; đó không
phải phép đo frame time hay chứng minh không có mọi loại cấp phát bộ nhớ.

| Hạng mục | Trạng thái |
|---|---|
| Tích hợp host | `HOST ALL PASSED` cả sau chỉnh cuối `LateUpdate` và giới hạn điểm đặt số |
| Hình ảnh Game View | Đã xem bốn hiệu ứng bản cuối ở 1920×1080; ảnh bên dưới |
| Hai tiến trình, độ trễ 100 ms | `ALL PASSED`, gồm chờ đồng đội, chống chọn/đổi trùng, timeout 11,33 s, disconnect và restart; bộ đếm hiệu ứng khớp ở đợt 2–3 |
| Console bản cuối | 0 lỗi đỏ, 0 cảnh báo sau phép thử co-op và restart; không có lỗi biên dịch |

Build Development mới đạt `BUILD Succeeded`. Editor làm host, client riêng chạy
`CardClient.exe --lac-card-test-client -batchmode -nographics`; transport thực tế:
latency 100 ms, jitter 0,02, unreliable loss/scramble 2%. Dùng bộ hồi quy
`CardNetworkPlayModeChecks.Run`, tạm giới hạn đề nghị đợt 1 vào Sinh Lực,
đợt 2 vào Thiết Bích; khôi phục danh mục gốc sau đó. Hai người đều là Gióng.
Ngay khi đợt 3 bắt đầu, fixture gây một sát thương thật lên mỗi người qua
`DamageSystem`, sau đó bảo vệ lại để không nhiễu phép đếm.

| Thời điểm | Vòng nâng cấp host/client | Hồi host/client | Khiên host/client | Pool mỗi máy |
|---|---|---|---|---|
| Bắt đầu đợt 2 | 2 / 2 | 2 / 2, mỗi lần +2 HP | 0 / 0 | 24 |
| Bắt đầu đợt 3, sau trúng đòn | 4 / 4 | 2 / 2 | 2 / 2 | 24 |

Bộ đếm cộng dồn trên cả hai nhân vật. Client vào giữa đợt trước khi fixture
restart nhận đúng lịch sử có sẵn và báo 0 hiệu ứng mới, không hiện lại nâng cấp
cũ. Phép thử T-24C ban đầu chưa kiểm vào đúng giữa màn chọn thẻ hoặc vệt gió
của người ở xa; hai trường hợp đã được bổ sung trong lượt hoàn thiện trên. Client chạy
không đồ hoạ nên phép thử mạng kiểm trigger/bộ đếm, không thay ảnh Game View.

Ảnh Game View trực tiếp trong Mirror host, tạm giữ thời gian và gọi từng hiệu ứng
để nhìn rõ hình nét; đây là preview thị giác, không phải bằng chứng trigger hay
lượng máu của lần chọn thẻ đó. Trigger được kiểm riêng bằng bộ kiểm tích hợp trên.

- [Vòng nhận thẻ](screenshots/card-battle-upgrade-1080p.png)
- [Hồi máu +3](screenshots/card-battle-heal-1080p.png)
- [Dấu Thiết Bích](screenshots/card-battle-shield-1080p.png)
- [Vệt tốc độ](screenshots/card-battle-wind-1080p.png)
- [Hồi máu +3 ở 720p](screenshots/card-battle-heal-720p.png)
- [Vệt tốc độ ở 720p](screenshots/card-battle-wind-720p.png)

Đã nhìn cả bốn hình nét ở 1280×720 trong lượt hoàn thiện; số hồi vẫn đọc được,
viền khiên bao quanh thân và các nét không phủ lên nhân vật. Lưu thêm hai ảnh
720p trên bằng render texture Game View gốc, không phóng ảnh 1080p xuống.

T-32, T-50 và T-51 vẫn mở. Phần này không đóng kiểm thử hiệu năng toàn trận,
không chốt sát thương gốc ba vũ khí hoặc cân bằng đường cong 16 đợt. Các hiệu
ứng nổ, đạn, sát thương và bảo vệ trong Combat/Player/VFX vẫn cần đánh giá
riêng theo phạm vi phân công.

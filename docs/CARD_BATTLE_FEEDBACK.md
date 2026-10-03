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

## Mã và tài sản

- `Assets/_LAC/Scripts/Cards/CardBattleFeedback.cs`: gắn observer, quản lý pool và vòng đời ván.
- `Assets/_LAC/Scripts/Cards/CardBattleObserver.cs`: đọc thay đổi thẻ/máu/di chuyển và chống phát trùng.
- `Assets/_LAC/Scripts/Cards/CardBattleEffect.cs`: hình nét, số hồi, theo nhân vật, mờ dần và trả pool.
- `Assets/_LAC/Scripts/Cards/CardBattleFeedbackData.cs` và `Data/Cards/Resources/CardBattleFeedback.asset`: màu, thời lượng, nhịp và dung lượng pool.
- `Assets/_LAC/Prefabs/UI/Cards/CardBattleEffect.prefab`: prefab trong phạm vi Cards, dùng lại vật liệu/`PixelNumber` hiện có.
- `Assets/_LAC/Scripts/Cards/Editor/CardBattleFeedbackSetup.cs`: tạo tài sản thiếu, giữ nguyên tài sản đã có.
- `Assets/_LAC/Scripts/Cards/Editor/CardBattleFeedbackChecks.cs`: kiểm thử tích hợp host trong Play mode.

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
cũ. Lượt này chưa kiểm vào đúng giữa màn chọn thẻ hoặc vệt gió của người ở xa;
không dùng kết quả trên để khẳng định đã bao phủ hai trường hợp đó. Client chạy
không đồ hoạ nên phép thử mạng kiểm trigger/bộ đếm, không thay ảnh Game View.

Ảnh Game View trực tiếp trong Mirror host, tạm giữ thời gian và gọi từng hiệu ứng
để nhìn rõ hình nét; đây là preview thị giác, không phải bằng chứng trigger hay
lượng máu của lần chọn thẻ đó. Trigger được kiểm riêng bằng bộ kiểm tích hợp trên.

- [Vòng nhận thẻ](screenshots/card-battle-upgrade-1080p.png)
- [Hồi máu +3](screenshots/card-battle-heal-1080p.png)
- [Dấu Thiết Bích](screenshots/card-battle-shield-1080p.png)
- [Vệt tốc độ](screenshots/card-battle-wind-1080p.png)

T-32, T-50 và T-51 vẫn mở. Phần này không đóng kiểm thử hiệu năng toàn trận,
không chốt sát thương gốc ba vũ khí hoặc cân bằng đường cong 16 đợt. Các hiệu
ứng nổ, đạn, sát thương và bảo vệ trong Combat/Player/VFX vẫn cần đánh giá
riêng theo phạm vi phân công.

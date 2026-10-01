# Danh mục thẻ — LẠC

Tài liệu riêng để lưu nội dung, chỉ số và trạng thái triển khai của toàn bộ thẻ trong game.
Quyết định kiến trúc vẫn theo [CLAUDE.md](../CLAUDE.md); tiến độ thực hiện vẫn theo
[TASKS.md](TASKS.md). Khi số liệu trong GDD cũ mâu thuẫn với tài liệu này, ưu tiên tài
liệu này.

---

## 1. Phạm vi hiện hành

| Loại | Số lượng mục tiêu | Trạng thái |
|---|---:|---|
| Thẻ nền | 12 | Đã triển khai đủ 12 thẻ; cân bằng ban đầu ở T-24 |
| Thẻ tiến hoá | 8 | Đã triển khai T-25/T-26; nghiệm thu co-op 01/10/2026 |
| **Tổng** | **20** | Đủ 12 thẻ nền + 8 tiến hoá; cân bằng toàn game ở T-50/T-51 |

Con số 48 thẻ trong GDD và mục tiêu 32 thẻ nền trước đây đã lỗi thời. Theo quyết định
ngày 24/09/2026, phạm vi hiện hành là **12 thẻ nền + 8 thẻ tiến hoá**.

## 2. Quy tắc chung

- Sau mỗi đợt, mỗi người chơi được đề nghị 3 thẻ và chọn 1 trong 10 giây.
- Mỗi người có 2 lượt đổi thẻ trong một ván. Lượt đổi và lựa chọn là độc lập trong co-op.
- Đợt tiếp theo chỉ bắt đầu khi toàn bộ người chơi đã hoàn tất lựa chọn.
- Bốc thẻ bằng `LAC.Core.RunRandom.Cards`; không dùng `UnityEngine.Random`.
- Thẻ đã đạt giới hạn cộng dồn không còn hợp lệ để xuất hiện trong đề nghị.
- Ba thẻ Song Tiễn, Xuyên Tâm, Bộc Phá chỉ xuất hiện cho vũ khí đạn (`WeaponShape.Line`).
- Hiệu ứng chỉ áp lên `PlayerUpgradeState` của ván hiện tại, không sửa trực tiếp
  `CharacterData` hoặc asset `CardDefinition`.
- Qua mạng chỉ đồng bộ định danh/lựa chọn thẻ; host giữ thẩm quyền với thay đổi gameplay,
  còn mỗi máy tự áp dụng phần biểu diễn.
- Toàn bộ 12 thẻ nền và 8 tiến hoá dùng bộ ảnh AI đồng nhất đồng cổ–ngọc xanh.
  Nguồn ảnh, prompt và cách nhập lại được lưu tại [CARD_ART.md](CARD_ART.md).

## 3. Giao diện và phản hồi tương tác hiện hành

Hướng mỹ thuật của bản demo lấy cảm hứng từ màn chọn nâng cấp ARAM Hỗn Loạn: nền tối,
khung kim loại vàng, hoạ tiết xanh lam-ngọc và icon lớn ở nửa trên thẻ. Chỉ lấy cảm
hứng về nhịp thị giác, không sao chép asset hoặc bố cục của Liên Minh Huyền Thoại.

### 3.1. Bộ icon đồng nhất

- Có 20 ảnh riêng tại `Assets/_LAC/Art/Sprites/UI/Cards/DongHo_2026/`;
  12 ảnh gắn vào `CardDefinition`, 8 ảnh gắn vào recipe và bonus tương ứng.
- Cùng nền than tối, đồng vàng, ngọc xanh, hoạ tiết mặt trời/chim Lạc và ánh sáng
  góc trên trái. Mỗi biểu tượng diễn giải một công dụng khác nhau.
- Texture được import dạng `Sprite/Single`, không mipmap, kích thước tối đa 512,
  `Point`, không nén, Full Rect. Bản PNG sinh gốc được giữ nguyên trong dự án.
- `LAC/Art/Apply Unified Card Icons` gán lại toàn bộ ảnh;
  `LAC/Art/Validate Unified Card Icons` kiểm tra đủ 20 ảnh và đúng ánh xạ.
- Bộ 7 ảnh cũ trong `AI_Demo/` được giữ để tham khảo, không còn được thẻ dùng.
- `CardDemoAssetGenerator` chỉ tự bổ sung asset/icon còn thiếu sau reload script;
  không tự ghi đè chỉ số hoặc nội dung của `CardDefinition` đang tồn tại. Menu
  `LAC/Demo/Rebuild Card Demo Assets` mới chủ động dựng lại và chạy validation;
  khi gán ảnh, ưu tiên bộ mới nếu có.

### 3.2. Màu và trạng thái thẻ

| Trạng thái | Quy tắc biểu diễn |
|---|---|
| Nghỉ | Khung và đường nhấn màu vàng `#C08D20` |
| Hover/focus | Chỉ đúng một thẻ chuyển xanh dương-ngọc `#2F7480`; các thẻ còn lại vẫn vàng |
| Đang chọn | Viền sáng `#9CCFC0`; khoá toàn bộ nút để chống nhấp đôi |
| Không được chọn | Giảm độ sáng trong lúc thẻ đã chọn thực hiện animation |

Không tự focus thẻ đầu tiên khi mở bảng bằng chuột. `CardSelectionView` giữ quyền
điều phối trạng thái highlight để `PointerExit` của thẻ cũ không thể xoá hover của
thẻ mới.

### 3.3. Chuyển động

- Thẻ giữ nguyên vị trí, kích thước và góc xoay lúc nghỉ/hover; đã bỏ nhịp thở.
- Khung đồng vát góc được vẽ bằng UI mesh trong `CardFrameGraphic`: hai đường viền,
  nét sáng/tối, thanh góc và họa tiết hình thoi, cùng bảng màu vàng–ngọc của icon.
- Vệt sáng mảnh chỉ chạy trên viền trong 0,72 giây, cách nhau khoảng 5,2 giây;
  các thẻ bắt đầu lệch pha. Hover/nhấn cũng kích hoạt vệt sáng; ảnh và chữ giữ nguyên.
- Hiệu ứng dùng `Time.unscaledDeltaTime`, tiếp tục khi chọn thẻ tạm dừng gameplay.
- Khi chọn: thẻ xoay đủ một vòng tại chỗ trong 0,38 giây; chỉ sau đó mới bắt đầu bay
  ở mốc 0,42 giây về toạ độ màn hình của player, đồng thời thu nhỏ và mờ dần.
- Tổng thời lượng phản hồi là 1,05 giây. Sau animation, client xác nhận với host và
  hiển thị chờ đồng đội; chỉ đóng bảng khi host cho sang đợt mới.

### 3.4. Lifecycle và độ ổn định

- UI được dựng lúc runtime trong canvas `ScreenSpaceOverlay`, không ghi thêm object
  giao diện vào `Arena.unity`.
- `EventSystem` dự phòng chỉ được tạo lúc runtime và không tạo trùng nếu scene đã có.
- Các cờ tĩnh của `CardSelectionController` được reset ở
  `RuntimeInitializeLoadType.SubsystemRegistration`. Nhờ vậy màn thẻ vẫn hoạt động
  qua nhiều lần Stop/Play khi Unity tắt Domain Reload.
- `Hide()` reset transform, alpha, màu và highlight của cả ba slot để animation cũ
  không rò sang lần mở bảng tiếp theo.

### 3.5. Trạng thái nghiệm thu ngày 09/09/2026

- Unity 6000.5.6f1 compile sạch; Console cuối phiên không có lỗi đỏ.
- Đã kiểm tra trong Play Mode và chụp Game View cho trạng thái nghỉ, hover, xoay và
  bay vào player.
- Check hồi quy host chạy đủ 7/7 thẻ và báo `ALL PASSED`: pause, chống nhấp đôi,
  phục hồi input/physics, chuyển wave và reset ván.
- Cloud Unity-MCP đang dùng cấu hình HTTP trong `.mcp.json` và đã gọi được trạng thái
  Editor, refresh asset, Console, Play Mode và Game View.
- Tại mốc 09/09, T-22 còn thiếu bộ đếm/tự chọn và T-23 chưa có xác nhận hai người.

### 3.6. Nghiệm thu T-22/T-23 ngày 24/09/2026

- Hoàn tất đếm ngược 10 giây theo đồng hồ host, tự chọn thẻ hợp lệ bằng `RunRandom.Cards`.
- Mỗi người có 2 lượt đổi cho cả ván; đổi không gia hạn đồng hồ. Host kiểm tra token
  lượt, revision đề nghị, kết nối sở hữu, thẻ hợp lệ và giới hạn cộng dồn.
- Đồng bộ định danh thẻ qua TargetRpc/Command và lịch sử nhận thẻ qua SyncList;
  client dựng lại hiệu ứng, chỉ host thay đổi máu. Người vào muộn nhận lịch sử hiện tại.
- Chờ mọi người chọn và hoàn tất animation trước khi chuyển đợt; có hạn chờ ACK
  dự phòng. Người đã gục/rời mạng không chặn lượt; người vào giữa lượt chờ lượt kế.
- Gỡ hoàn toàn cơ chế tự chuyển đợt tạm sau 1,5 giây trong `WaveManager`.
- Hồi quy host 7/7 thẻ đạt. Kiểm thử Editor host + client Windows riêng, dùng
  LatencySimulation 100 ms, đạt: chờ đồng đội, lượt đổi độc lập, từ chối yêu cầu
  sai/trùng, hết giờ, rời mạng và reset ván. Tự chọn rồi sang đợt đo được 11,39 giây
  bao gồm animation và truyền mạng. Log client xác nhận nâng cấp của cả hai người.
- Kiểm tra giới hạn cộng dồn và hết bể thẻ đạt; đã nhìn ảnh đếm ngược và trạng thái
  chờ trong Game View. Console cuối phiên: 0 lỗi, 0 cảnh báo.
- Quy trình chạy lại: [CARD_TESTS.md](CARD_TESTS.md). Cấu hình thời hạn/lượt đổi:
  `Data/Cards/Resources/CardSelectionRules.asset`.

## 4. Dữ liệu của một thẻ

Mỗi thẻ nền là một `CardDefinition` ScriptableObject tại
`Assets/_LAC/Data/Cards/Resources/Cards/` với các trường sau:

| Trường | Ý nghĩa |
|---|---|
| `Id` | Định danh ổn định dùng trong mã và đồng bộ mạng |
| `DisplayName` | Tên tiếng Việt hiển thị cho người chơi |
| `Description` | Mô tả chính xác hiệu ứng và cách cộng dồn |
| `Icon` | Biểu tượng thẻ |
| `MaxStacks` | Số lần tối đa có thể nhận trong một ván |
| `Weight` | Trọng số xuất hiện trong bể thẻ |
| `Accent` | Màu nhấn giao diện; không dùng nhóm Son dành riêng cho đòn địch |

## 5. Thẻ nền đã triển khai

Các hàng dưới đây phản ánh asset và logic đang chạy, không phải chỉ số dự kiến.

| ID | Tên | Hiệu ứng mỗi lần nhận | Cộng dồn tối đa | Trọng số | Trạng thái |
|---|---|---|---:|---:|---|
| `CuongCong` | Cường Công | +20% sát thương cơ bản, cộng theo chỉ số gốc | 3 | 1 | Đã triển khai |
| `LienKich` | Liên Kích | +15% tốc độ đánh cơ bản | 3 | 1 | Đã triển khai |
| `SinhLuc` | Sinh Lực | +20% máu gốc; tổng làm tròn lên; hồi đúng phần máu vừa tăng | 3 | 1 | Đã triển khai |
| `BoPhap` | Bộ Pháp | −20% thời gian hồi lướt | 1 | 1 | Đã triển khai |
| `SongTien` | Song Tiễn | Chỉ vũ khí đạn: 2 đạn trong góc mở 7°, mỗi đạn 70% sát thương | 1 | 0,65 | Đã triển khai |
| `XuyenTam` | Xuyên Tâm | Chỉ vũ khí đạn: xuyên thêm 2 địch, tối đa 3 mục tiêu | 1 | 0,65 | Đã triển khai |
| `BocPha` | Bộc Phá | Chỉ vũ khí đạn: chạm đầu nổ bán kính 1,75; 30% sát thương lên địch khác | 1 | 0,65 | Đã triển khai |
| `KhinhThan` | Khinh Thân | +8% tốc độ di chuyển gốc; không tăng tốc lướt | 3 | 1 | Đã triển khai |
| `AmVang` | Âm Vang | +10% tầm đánh gốc; không tăng bán kính nổ | 3 | 1 | Đã triển khai |
| `HoiXuan` | Hồi Xuân | Hồi 1 máu khi bắt đầu đợt 2 trở đi; không hồi sinh | 2 | 0,8 | Đã triển khai |
| `ThietBich` | Thiết Bích | +15% thời gian bất tử sau khi trúng đòn; không tăng i-frame lướt | 2 | 1 | Đã triển khai |
| `CuongNo` | Cuồng Nộ | +25% sát thương gốc, đổi lại −10% tốc độ đánh gốc | 2 | 0,75 | Đã triển khai |

Ghi chú: `SongTien`, `XuyenTam` và `BocPha` đã kết hợp được trên cùng một viên đạn.
Mọi tham số hiệu ứng, giới hạn và trọng số nằm trong asset `CardDefinition`.
`PlayerUpgradeState` chỉ cộng dồn lên bản sao trong ván. ID 0–6 được giữ nguyên,
năm thẻ mới nối tiếp ID 7–11 để không làm lệch các tham chiếu cũ.

## 6. Cân bằng bộ 12 thẻ

Danh sách chính thức là 12 hàng ở mục 5. Có 9 thẻ chung và 3 thẻ dành cho vũ khí đạn.
Hai kiểu vũ khí vòng tròn/hình cung có tổng 22 cấp có thể nhận; vũ khí đạn có 25.
Muốn vét bể thẻ chung xuống còn dưới 3 ID phải tiêu ít nhất 16 lựa chọn: vì vậy
trước lượt chọn thứ 15 vẫn còn ít nhất 3 thẻ hợp lệ, kể cả cách chọn bất lợi nhất.

| Hướng xây dựng | Trần từ thẻ | Đánh đổi/giới hạn |
|---|---|---|
| Sát thương + tốc đánh | Cường Công ×3, Liên Kích ×3, Cuồng Nộ ×2: 2,625× DPS gốc | Tốn 8 lựa chọn; Cuồng Nộ giảm tốc đánh |
| Thêm Song Tiễn | 3,675× DPS gốc nếu cả hai đạn trúng một mục tiêu | Chỉ vũ khí đạn; tốn tổng 9 lựa chọn; chưa tính trượt/overkill |
| Máu | Tấm 4→7, Thạch Sanh 6→10, Gióng 10→16 | Tốn 3 lựa chọn; thay mức +75 máu của demo cũ |
| Di chuyển / tầm đánh | +24% / +30% | Mỗi hướng tốn 3 lựa chọn; không tăng lướt/bán kính nổ |
| Hồi phục | 2 máu giữa các đợt | Tốn 2 lựa chọn; không hồi giữa giao tranh hoặc hồi sinh |
| Phòng thủ | +30% bất tử sau trúng đòn (0,6→0,78 giây với cấu hình hiện tại) | Tốn 2 lựa chọn; không giảm sát thương và không tăng i-frame lướt |

Các phần trăm cộng theo chỉ số gốc, không nhân lũy tiến qua từng cấp. Sát thương thật
và máu quái giữ phần lẻ: đạn 0,7 không còn bị nâng thành 1; vụ nổ 30% của nó là 0,21.
Số sát thương nổi vẫn làm tròn cho giao diện, không dùng để tính máu quái.
Vụ nổ không đánh lại mục tiêu vừa trúng trực tiếp, không tạo chuỗi nổ và chỉ nổ một lần/đạn.

Đã mô phỏng 1.000 seed × 15 lượt × 3 kiểu vũ khí = **45.000 lựa chọn**, kiểm tra đổi thẻ,
lọc theo vũ khí, giới hạn cộng dồn, toàn bộ thẻ đều xuất hiện và không sửa asset.
Đã kiểm thử Play Mode đủ 12 thẻ, gồm tác động thực của năm thẻ mới, reset và chuyển đợt;
đã nhìn ảnh Game View của năm thẻ mới. Chạy lại bằng `LAC > Tests > Validate 12 Card Balance`.
Hồi quy co-op Editor host + client Windows riêng với độ trễ 100 ms đã đạt toàn bộ
kiểm tra; client nhận đúng định danh/cộng dồn mới, lượt hết giờ sang đợt sau 11,40 giây
kể cả animation và truyền mạng. Console cuối phiên: 0 lỗi, 0 cảnh báo.

Đây là cân bằng ban đầu của **bể thẻ**, chưa thay thế T-50/T-51: cần chơi thử với đủ
quái/boss và thu thập tỉ lệ thắng, lựa chọn thẻ để chốt độ khó toàn game.

## 7. Tám công thức tiến hoá — T-25/T-26

Đủ nguyên liệu sẽ tự mở tiến hoá sau khi host chấp nhận thẻ. Mỗi công thức chỉ
áp dụng một lần mỗi ván; không tốn lượt chọn hay lượt đổi. Nguyên liệu giữ cấp và
hiệu ứng, phần thưởng dưới đây cộng thêm đúng một lần, không nhân đôi chỉ số nền.
Một nguyên liệu có thể dùng cho nhiều công thức; các tiến hoá cùng đủ điều kiện
được áp dụng và thông báo lần lượt. Không có nguyên liệu tiến hoá dây chuyền.

| Tiến hoá | Nguyên liệu | Phần thưởng thêm | Phạm vi |
|---|---|---|---|
| Nỏ Thần | Xuyên Tâm ×1 + Âm Vang ×3 | Đạn xuyên thêm 2 địch; +20% tầm đánh gốc. | Vũ khí đạn |
| Lửa Thiêng | Bộc Phá ×1 + Cuồng Nộ ×2 | Nổ rộng 2,5 đơn vị, gây 60% sát thương đạn lên địch khác. | Vũ khí đạn |
| Trăm Trứng | Song Tiễn ×1 + Liên Kích ×3 | Bắn 4 đạn trong góc 14°. Mỗi đạn còn 52,5% sát thương. | Vũ khí đạn |
| Thánh Gióng | Cường Công ×3 + Thiết Bích ×2 | +40% sát thương gốc; +15% thời gian bảo vệ sau trúng đòn. | Cả ba nhân vật |
| Tiếng Đàn Thần | Cường Công ×2 + Âm Vang ×3 | +25% sát thương gốc; +15% tốc độ đánh gốc. | Cả ba nhân vật |
| Lạc Phong | Bộ Pháp ×1 + Khinh Thân ×3 | Giảm thêm 15% hồi lướt gốc; +12% tốc độ di chuyển gốc. | Cả ba nhân vật |
| Bất Tử | Sinh Lực ×3 + Hồi Xuân ×2 | +20% máu gốc; hồi thêm 1 máu đầu đợt. Không hồi sinh. | Cả ba nhân vật |
| Kim Cang | Sinh Lực ×2 + Thiết Bích ×2 | +20% máu gốc; +30% thời gian bảo vệ sau trúng đòn. | Cả ba nhân vật |

Tất cả chỉ số phần trăm cộng theo chỉ số gốc, trừ hệ số sát thương đạn nhân nhau.
Trăm Trứng nhân thêm 0,75 vào 0,7 của Song Tiễn nên mỗi viên còn 0,525; tổng 4 viên
là 2,1 lần sát thương trước hệ số đạn nếu đều trúng. Nỏ Thần đạt 5 mục tiêu/viên.
Lửa Thiêng nâng bán kính và hệ số nổ bằng giá trị lớn nhất, không cộng bán kính.
Bất Tử chỉ tăng máu/hồi đầu đợt, không cung cấp hồi sinh hoặc bất tử vĩnh viễn.

Tám công thức dùng 3–5 lượt chọn nguyên liệu, nằm trong 15 lượt của một ván.
Bonus nằm ngoài `Resources/Cards` nên bể bốc vẫn đúng 12 thẻ. Đây là cân bằng ban
đầu của tiến hoá; đường cong độ khó toàn game vẫn thuộc T-50/T-51.
Chi tiết dữ liệu và kiểm thử: [EVOLUTION_TESTS.md](EVOLUTION_TESTS.md).

## 8. Mẫu biên soạn thẻ mới

Sao chép một hàng mẫu dưới đây khi bổ sung thẻ, rồi cập nhật cả danh sách ở mục 6 hoặc
mục 7. Mô tả phải nêu rõ giá trị, cách cộng dồn và mọi bất lợi.

| ID | Tên | Loại/nhóm | Hiệu ứng | Bất lợi | Max stacks | Weight | Nhân vật | Công thức liên quan | Trạng thái |
|---|---|---|---|---|---:|---:|---|---|---|
| `CardId` | Tên hiển thị | Chỉ số / Cải biến / Riêng / Tiến hoá | Giá trị chính xác | Không hoặc mô tả | 1 | 1 | Chung hoặc tên nhân vật | Không hoặc tên tiến hoá | Ý tưởng / Đã duyệt / Đã triển khai |

## 9. Vị trí triển khai

| Thành phần | Vị trí |
|---|---|
| Định danh | `Assets/_LAC/Scripts/Cards/CardId.cs` |
| Dữ liệu thẻ | `Assets/_LAC/Scripts/Cards/CardDefinition.cs` |
| Chỉ số trong ván | `Assets/_LAC/Scripts/Cards/PlayerUpgradeState.cs` |
| Bốc thẻ | `Assets/_LAC/Scripts/Cards/CardDeck.cs` |
| Điều khiển lựa chọn | `Assets/_LAC/Scripts/Cards/CardSelectionController.cs` |
| Điều phối mạng trên host | `Assets/_LAC/Scripts/Cards/CardSelectionNetwork.cs` |
| Cấu hình lượt chọn | `Assets/_LAC/Data/Cards/Resources/CardSelectionRules.asset` |
| Giao diện | `Assets/_LAC/Scripts/Cards/CardSelectionView.cs` |
| Hover/animation chọn | `Assets/_LAC/Scripts/Cards/CardHoverVisual.cs` |
| Khung đồng và lóe sáng viền | `Assets/_LAC/Scripts/Cards/CardFrameGraphic.cs` |
| Asset | `Assets/_LAC/Data/Cards/Resources/Cards/` |
| Icon demo AI | `Assets/_LAC/Art/Sprites/UI/Cards/AI_Demo/` |
| 20 icon đang dùng | `Assets/_LAC/Art/Sprites/UI/Cards/DongHo_2026/` |
| Prompt và kiểm chứng ảnh | `docs/CARD_ART.md` |
| Prefab | `Assets/_LAC/Prefabs/UI/Cards/Resources/CardSelection.prefab` |

# Danh mục thẻ — LẠC

Tài liệu riêng để lưu nội dung, chỉ số và trạng thái triển khai của toàn bộ thẻ trong game.
Quyết định kiến trúc vẫn theo [CLAUDE.md](../CLAUDE.md); tiến độ thực hiện vẫn theo
[TASKS.md](TASKS.md). Khi số liệu trong GDD cũ mâu thuẫn với tài liệu này, ưu tiên tài
liệu này.

---

## 1. Phạm vi hiện hành

| Loại | Số lượng mục tiêu | Trạng thái |
|---|---:|---|
| Thẻ nền | 32 | 7 thẻ demo đã triển khai; 25 thẻ còn lại chờ T-24 |
| Thẻ tiến hoá | 8 | Chờ T-25 và T-26 |
| **Tổng** | **40** | Không tính các cấp cộng dồn là thẻ riêng |

Con số 48 thẻ trong GDD gốc đã lỗi thời. Phạm vi hiện hành là **32 thẻ nền + 8 thẻ
tiến hoá** theo CLAUDE.md mục 7.

## 2. Quy tắc chung

- Sau mỗi đợt, mỗi người chơi được đề nghị 3 thẻ và chọn 1 trong 10 giây.
- Mỗi người có 2 lượt đổi thẻ trong một ván. Lượt đổi và lựa chọn là độc lập trong co-op.
- Đợt tiếp theo chỉ bắt đầu khi toàn bộ người chơi đã hoàn tất lựa chọn.
- Bốc thẻ bằng `LAC.Core.RunRandom.Cards`; không dùng `UnityEngine.Random`.
- Thẻ đã đạt giới hạn cộng dồn không còn hợp lệ để xuất hiện trong đề nghị.
- Hiệu ứng chỉ áp lên `PlayerUpgradeState` của ván hiện tại, không sửa trực tiếp
  `CharacterData` hoặc asset `CardDefinition`.
- Qua mạng chỉ đồng bộ định danh/lựa chọn thẻ; host giữ thẩm quyền với thay đổi gameplay,
  còn mỗi máy tự áp dụng phần biểu diễn.
- Bộ 7 thẻ demo hiện dùng ảnh AI đồng nhất để kiểm chứng bố cục và tương tác. Đây là
  **mỹ thuật tạm**, không thay thế bộ 40 icon chính thức do hoạ sĩ thực hiện ở T-33.

## 3. Giao diện và phản hồi tương tác hiện hành

Hướng mỹ thuật của bản demo lấy cảm hứng từ màn chọn nâng cấp ARAM Hỗn Loạn: nền tối,
khung kim loại vàng, hoạ tiết xanh lam-ngọc và icon lớn ở nửa trên thẻ. Chỉ lấy cảm
hứng về nhịp thị giác, không sao chép asset hoặc bố cục của Liên Minh Huyền Thoại.

### 3.1. Bộ icon demo

- Có 7 ảnh vuông 512×512 tương ứng với 7 `CardDefinition`, đặt tại
  `Assets/_LAC/Art/Sprites/UI/Cards/AI_Demo/`.
- Các ảnh dùng chung ngôn ngữ mỹ thuật: nền xanh đen, vật thể vàng, dòng năng lượng
  xanh ngọc và độ tương phản phù hợp pixel-art UI.
- Texture được import dạng `Sprite/Single`, không mipmap, kích thước tối đa 512,
  `Bilinear` và `CompressedHQ`.
- `CardDemoAssetGenerator` chỉ tự bổ sung asset/icon còn thiếu sau reload script;
  không tự ghi đè chỉ số hoặc nội dung của `CardDefinition` đang tồn tại. Menu
  `LAC/Demo/Rebuild Card Demo Assets` mới chủ động dựng lại và chạy validation.

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

- Mỗi thẻ có nhịp đập kép với pha lệch nhau, gồm thay đổi nhẹ kích thước, độ cao và
  độ sáng khung. Animation dùng `Time.unscaledTime`/`Time.unscaledDeltaTime`, vì lúc
  chọn thẻ gameplay đang có `Time.timeScale = 0`.
- Thẻ đang hover vẫn tiếp tục nhịp đập, đồng thời nhấc lên và phóng lớn nhẹ.
- Khi chọn: thẻ xoay đủ một vòng tại chỗ trong 0,38 giây; chỉ sau đó mới bắt đầu bay
  ở mốc 0,42 giây về toạ độ màn hình của player, đồng thời thu nhỏ và mờ dần.
- Tổng thời lượng phản hồi là 1,05 giây. Controller chỉ đóng bảng và cho phép chuyển
  đợt sau khi animation kết thúc.

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
- T-22 vẫn còn thiếu bộ đếm và xử lý tự chọn sau 10 giây; T-23 vẫn còn đồng bộ xác
  nhận thật giữa hai người chơi. Vì vậy hai task này **chưa được đánh dấu hoàn thành**.

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
| `SinhLuc` | Sinh Lực | +25 máu tối đa và hồi ngay 25 máu | 3 | 1 | Đã triển khai |
| `BoPhap` | Bộ Pháp | −20% thời gian hồi lướt | 1 | 1 | Đã triển khai |
| `SongTien` | Song Tiễn | Bắn 2 đạn lệch 7°; mỗi đạn gây 70% sát thương hiện tại | 1 | 1 | Đã triển khai |
| `XuyenTam` | Xuyên Tâm | Đạn xuyên thêm 2 địch, tối đa chạm 3 mục tiêu khác nhau | 1 | 1 | Đã triển khai |
| `BocPha` | Bộc Phá | Lần chạm đầu phát nổ trong bán kính 1,75, gây 30% sát thương đạn | 1 | 1 | Đã triển khai |

Ghi chú: `SongTien`, `XuyenTam` và `BocPha` đã kết hợp được trên cùng một viên đạn.
Các tham số góc lệch và bán kính nổ hiện nằm trong `PlayerUpgradeState`; khi biên soạn
đủ bể thẻ cần chuyển toàn bộ chỉ số nội dung sang dữ liệu thay vì hard-code.

## 6. Danh sách 32 thẻ nền

T-24 sẽ chốt bảng này. Không điền chỉ số chưa được duyệt vào mã hoặc asset.

| # | ID | Tên | Nhóm | Hiệu ứng | Giới hạn | Trọng số | Nhân vật | Trạng thái |
|---:|---|---|---|---|---:|---:|---|---|
| 1 | `CuongCong` | Cường Công | Chỉ số | +20% sát thương cơ bản | 3 | 1 | Chung | Đã triển khai |
| 2 | `LienKich` | Liên Kích | Chỉ số | +15% tốc độ đánh cơ bản | 3 | 1 | Chung | Đã triển khai |
| 3 | `SinhLuc` | Sinh Lực | Chỉ số | +25 máu tối đa, hồi 25 máu | 3 | 1 | Chung | Đã triển khai |
| 4 | `BoPhap` | Bộ Pháp | Chỉ số | −20% hồi chiêu lướt | 1 | 1 | Chung | Đã triển khai |
| 5 | `SongTien` | Song Tiễn | Cải biến vũ khí | 2 đạn, mỗi đạn 70% sát thương | 1 | 1 | Chung | Đã triển khai |
| 6 | `XuyenTam` | Xuyên Tâm | Cải biến vũ khí | Xuyên thêm 2 địch | 1 | 1 | Chung | Đã triển khai |
| 7 | `BocPha` | Bộc Phá | Cải biến vũ khí | Nổ 30% sát thương khi chạm đầu | 1 | 1 | Chung | Đã triển khai |
| 8–32 | — | — | — | — | — | — | — | Chờ biên soạn |

## 7. Công thức tiến hoá

Khi đủ toàn bộ nguyên liệu, hệ thống tự hợp nhất thành thẻ tiến hoá. Tên nguyên liệu
dưới đây là tên thiết kế từ CLAUDE.md; T-24/T-26 phải ánh xạ chúng sang ID chính thức
trước khi triển khai.

| # | Nguyên liệu | Kết quả | Trạng thái |
|---:|---|---|---|
| 1 | Xuyên thấu ×3 + Nảy tường ×3 | Nỏ Thần | Đã chốt ở mức thiết kế |
| 2 | Nổ ×3 + Vệt cháy ×3 | Lửa Thiêng | Đã chốt ở mức thiết kế |
| 3 | +2 đạn ×3 + Tách đạn ×3 | Trăm Trứng | Đã chốt ở mức thiết kế |
| 4–8 | Chưa chốt | Chưa chốt | Chờ T-26 |

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
| Giao diện | `Assets/_LAC/Scripts/Cards/CardSelectionView.cs` |
| Hover/nhịp đập/animation chọn | `Assets/_LAC/Scripts/Cards/CardHoverVisual.cs` |
| Asset | `Assets/_LAC/Data/Cards/Resources/Cards/` |
| Icon demo AI | `Assets/_LAC/Art/Sprites/UI/Cards/AI_Demo/` |
| Prefab | `Assets/_LAC/Prefabs/UI/Cards/Resources/CardSelection.prefab` |

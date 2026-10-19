# AGENTS.md

Hướng dẫn cho mọi công cụ AI làm việc trên dự án này. Đọc hết file này trước khi sửa bất cứ thứ gì — nó ngắn.

**LẠC** là game arena survival roguelite 2D, đồ hoạ pixel art, thần thoại Việt Nam. Unity 6000.5.6f1 · URP 2D · Mirror. Co-op 2 người, mô hình host-authoritative. Chỉ phục vụ đồ án tốt nghiệp, không phát hành thương mại hoặc yêu cầu Steam.

## Thứ tự đọc

| Tài liệu | Dùng để |
|---|---|
| [CLAUDE.md](CLAUDE.md) | **Tài liệu chủ đạo.** Ràng buộc kiến trúc, quy ước lập trình. Khi mâu thuẫn, file này thắng |
| [docs/TASKS.md](docs/TASKS.md) | Việc gì đã xong, việc gì còn lại, ai phụ trách |
| [docs/WORKFLOW.md](docs/WORKFLOW.md) | Quy trình nhóm, chỗ nối giữa các mảng |
| [docs/PALETTE.md](docs/PALETTE.md) | Bảng 24 màu đã chốt và ràng buộc màu cho đòn địch |
| [docs/GDD.md](docs/GDD.md) | Thiết kế bản đồ án đã thu gọn ngày 11/10/2026 |

## Ba ràng buộc không được vi phạm

Diễn giải đầy đủ ở mục 3 của CLAUDE.md.

1. **Không có nhánh mã riêng cho chơi đơn.** Chơi đơn là Mirror host mode với một client. Cấm `if (isSinglePlayer)`.
2. **Đồng bộ sự kiện, không đồng bộ trạng thái.** Không gắn `NetworkIdentity` lên đạn hay hiệu ứng. Host mô phỏng trạng thái thật, client mô phỏng biểu diễn.
3. **Cấm `UnityEngine.Random` trong luồng gameplay.** Dùng `LAC.Core.RunRandom`. Ngoại lệ duy nhất là hiệu ứng hình ảnh và âm thanh thuần trang trí.

## Phạm vi đồ án — quyết định 11/10/2026

Giảng viên yêu cầu online multiplayer, có AI và yếu tố riêng; người dùng xác nhận không bắt buộc học máy/LinUCB hoặc mẫu 30 người. Đọc [docs/THESIS_SCOPE.md](docs/THESIS_SCOPE.md) trước khi thêm tính năng. Đạo diễn dựa trên luật + FSM ba trạng thái đã có mã T-45/T-45B/T-46; bằng chứng và giới hạn nghiệm thu tại [docs/DIRECTOR_TESTS.md](docs/DIRECTOR_TESTS.md). Giữ 12 thẻ, Trống Đồng/Hồn và một arena. Không tự mở lại Steam, Ngọc/mở khoá, tám tiến hoá hoặc ba tileset. Multiplayer vẫn phải nghiệm thu hai máy qua Internet, không thay bằng localhost/LAN.

## Đang ở đâu

Cổng 1 đã xong: vòng lặp một ván chạy được đầu-cuối — di chuyển, lướt, vũ khí tự bắn, quái theo đợt, sát thương, phản hồi khi đánh trúng, HUD máu, kết thúc ván và chơi lại tại chỗ.

**Cấu hình đang chạy là cấu hình thử nghiệm, đừng nhầm là bản cuối:**

- Người chơi chọn **Thánh Gióng (ID Warrior) / Archer / Lancer** trước trận. Thánh Gióng dùng sprite AI đã chuẩn hóa trong `_LAC/`, hai lớp còn lại dùng TinySwords xanh/tím. Cô Hồn dùng Orc. Ván có 10 đợt: 9 đợt sinh tồn tính giờ 20–60s, boss ở đợt 10; rìa sân chỉ còn đá, đổi sắc ở đợt 4 và 7. Xem [docs/ASSETS_ThirdParty.md](docs/ASSETS_ThirdParty.md).
- Mỹ thuật T-18 vẫn có tại `Data/Animations/ThachSanh.asset` và `CoHon.asset` để tham khảo; Thánh Gióng thay phần hiển thị Warrior ngày 18/10/2026, hai lớp còn lại giữ TinySwords. Pipeline và bằng chứng: [docs/THANH_GIONG.md](docs/THANH_GIONG.md).
  > T-18F hiện dùng chibi thân ~32px (ô48/PPU32), Idle2/Run6/Down1 và tre rời; chân tiếp tục chạy khi đánh. `ThanhGiongChibiContent`, `PlayerWeaponVisual`, `TreNgaVisual.asset`; builder cũ giữ chibi. Working tree T-50 dùng 0.30s/contact0.15s, giữ chu kỳ/damage/tầm. Nguồn AI/pipeline/bằng chứng: [docs/THANH_GIONG_CHIBI.md](docs/THANH_GIONG_CHIBI.md). T-18D/E giữ để đối chiếu, không gán lại thủ công.
- T-22/T-23 đã có chọn thẻ 10 giây và đồng bộ co-op; đã gỡ cơ chế tự sang đợt sau 1.5 giây. Kiểm thử: [docs/CARD_TESTS.md](docs/CARD_TESTS.md)
- Chọn **Cố định / Thích ứng** trước kết nối; host chốt chế độ, Restart giữ chế độ phòng. `Director/AIDirector.cs` dùng luật từ `DirectorRules.asset` trước wave 2–9; `ContextAggregator` đóng băng HP cuối wave trước thẻ/hồi máu. Host dùng đặc tả đó trong `WaveManager`, client chỉ nhận sự kiện sinh và HUD. **F3** mở lý do/ngữ cảnh/fallback; CSV schema 2 thêm `directors.csv`. Không dùng CSV cũ làm bằng chứng AI mới.
- AI đạo diễn **chính là Chằn Tinh**, chủ dungeon điều binh rồi ra trận ở wave 10. `DungeonMaster.asset` chứa ý đồ/lời thoại; `DirectorHud` diễn kết quả host, không tự quyết luật. Giãn binh/Điều binh/Đổi thế vây tương ứng Relief/Balanced/Variety. Hướng dẫn: [docs/AI_CHAN_TINH.md](docs/AI_CHAN_TINH.md).

## Nhận việc thế nào

1. Mở [docs/TASKS.md](docs/TASKS.md), tìm hạng mục `[ ]` thuộc mảng đang làm
2. Kiểm xem chức năng đã tồn tại chưa — dòng `>` dưới mỗi hạng mục đã xong ghi rõ nó nằm ở file nào
3. Làm xong thì đánh dấu `[x]`, thêm dòng `>` mô tả, **commit cùng lúc với mã**. Thiếu bước này thì hạng mục chưa tính là xong

Mỗi thư mục có đúng một người chịu trách nhiệm — xem bảng phân công trong TASKS.md. Đừng sửa file ngoài phạm vi hạng mục đang làm.

## Năm cái bẫy đã từng mất nhiều giờ

**Sửa file `.cs` trong lúc Play mode đang chạy.** Unity nạp lại domain giữa chừng, huỷ `NetworkManager` và xoá trạng thái tĩnh, để lại một loạt lỗi vô nghĩa không truy được. **Luôn thoát Play mode trước khi sửa mã.**

**Đổi giá trị mặc định trong C# mà tưởng đã xong.** Component đã tồn tại trong scene hoặc prefab giữ giá trị đã tuần tự hoá, và nó **đè lên** mặc định mới. Phải sửa cả trong asset. Bẫy này đã vấp hai lần trong một buổi.

**`SpriteRenderer.color` là hệ số nhân, không phải màu thay thế.** Gán `Color.white` là nhân với 1 — không có gì thay đổi trên màn hình. Muốn loé sáng thì truyền hệ số lớn hơn 1, ví dụ `(6, 6, 6)`.

**Sửa `ScriptableObject` lúc chạy.** Nó ghi đè **vĩnh viễn** vào file asset trong Editor. Thẻ nâng cấp phải cộng dồn lên một bản sao chỉ số của ván, không phải lên `CharacterData`.

**Kết luận về thị giác bằng cách đọc mã.** Ràng buộc đọc hiểu ở mục 2.1 chỉ kiểm chứng được bằng ảnh chụp màn hình. Hai lỗi nặng nhất về hình ảnh — sóng âm phủ trắng màn hình, và nền sân sáng làm nhân vật biến mất — đều lọt qua khâu đọc mã.

## Kiểm chứng

Dự án có **Unity-MCP** đã cấu hình. Dùng nó để vào Play mode, chụp màn hình Game View và đọc Console — đừng chỉ dựa vào việc mã biên dịch được.

Một hạng mục chỉ được báo hoàn thành khi: Console không có lỗi đỏ, đã chạy thử trong Play mode, và với thay đổi về hình ảnh thì **đã nhìn ảnh chụp**.

Ngân sách hiệu năng là **60 FPS với 40 quái và 200 đạn cùng lúc**. Fixture T-44D trên máy phát triển đạt P95 4.98 ms, ngưỡng 16.67 ms; không thay thế đo build bàn giao hoặc tải đạo diễn sau tích hợp. Xem `docs/TEN_WAVE_TESTS.md`.

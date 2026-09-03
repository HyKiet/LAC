# AGENTS.md

Hướng dẫn cho mọi công cụ AI làm việc trên dự án này. Đọc hết file này trước khi sửa bất cứ thứ gì — nó ngắn.

**LẠC** là game arena survival roguelite 2D, đồ hoạ pixel art, thần thoại Việt Nam. Unity 6000.5.6f1 · URP 2D · Mirror. Co-op 2 người, mô hình host-authoritative. Vừa là khoá luận vừa là sản phẩm thương mại trên Steam.

## Thứ tự đọc

| Tài liệu | Dùng để |
|---|---|
| [CLAUDE.md](CLAUDE.md) | **Tài liệu chủ đạo.** Ràng buộc kiến trúc, quy ước lập trình. Khi mâu thuẫn, file này thắng |
| [docs/TASKS.md](docs/TASKS.md) | Việc gì đã xong, việc gì còn lại, ai phụ trách |
| [docs/WORKFLOW.md](docs/WORKFLOW.md) | Quy trình nhóm, chỗ nối giữa các mảng |
| [docs/PALETTE.md](docs/PALETTE.md) | Bảng 24 màu đã chốt và ràng buộc màu cho đòn địch |
| [docs/GDD.md](docs/GDD.md) | Đặc tả gốc — **một số nội dung đã lỗi thời**, xem mục 7 của CLAUDE.md |

## Ba ràng buộc không được vi phạm

Diễn giải đầy đủ ở mục 3 của CLAUDE.md.

1. **Không có nhánh mã riêng cho chơi đơn.** Chơi đơn là Mirror host mode với một client. Cấm `if (isSinglePlayer)`.
2. **Đồng bộ sự kiện, không đồng bộ trạng thái.** Không gắn `NetworkIdentity` lên đạn hay hiệu ứng. Host mô phỏng trạng thái thật, client mô phỏng biểu diễn.
3. **Cấm `UnityEngine.Random` trong luồng gameplay.** Dùng `LAC.Core.RunRandom`. Ngoại lệ duy nhất là hiệu ứng hình ảnh và âm thanh thuần trang trí.

## Đang ở đâu

Cổng 1 đã xong: vòng lặp một ván chạy được đầu-cuối — di chuyển, lướt, vũ khí tự bắn, quái theo đợt, sát thương, phản hồi khi đánh trúng, HUD máu, kết thúc ván và chơi lại tại chỗ.

**Cấu hình đang chạy là cấu hình thử nghiệm, đừng nhầm là bản cuối:**

- Người chơi mặc định là **Gióng** dùng sprite Soldier, quái dùng sprite Orc — cả hai lấy từ `Assets/ThirdParty/`, xem [docs/ASSETS_ThirdParty.md](docs/ASSETS_ThirdParty.md)
- Mỹ thuật thật của T-18 đã có sẵn tại `Data/Animations/ThachSanh.asset` và `CoHon.asset`, đổi lại chỉ là hai trường dữ liệu
- Cờ `WaveManager._autoAdvanceCardSelection` tự sang đợt kế sau 1.5 giây — **chỗ giữ tạm**, tắt khi hệ thống thẻ (T-22, T-23) xong

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

Ngân sách hiệu năng là **60 FPS với 40 quái và 200 đạn cùng lúc**. Mốc hiện tại: 3.72 ms mỗi khung với 40 quái, ngưỡng 16.67 ms.

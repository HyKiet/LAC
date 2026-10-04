# Asset bên thứ ba — đánh giá và tích hợp 04/10/2026

**Vị trí nguồn:** `Assets/ThirdParty/`. Hai gói đang được dùng làm mỹ thuật thử nghiệm theo yêu cầu tích hợp ngày 04/10; không sửa ảnh hoặc metadata gốc. Bộ mỹ thuật cuối cùng vẫn theo `PALETTE.md` và T-33/T-47.

## Kiểm kê và phân tích

Đã kiểm kê 444 PNG và xem trực tiếp sheet Warrior, Bushes, Rocks, tileset. Các tham chiếu runtime được kiểm tra trong Unity.

| Gói | Nội dung trên đĩa | Điểm mạnh | Giới hạn |
|---|---|---|---|
| TinyRPG | 34 PNG; Soldier, Orc, mũi tên; file Aseprite | Thân nhỏ; Idle, Walk, Attack, Hurt, Death; bản có bóng giúp định vị | Chỉ hai hình nhân vật, tư thế nhìn nghiêng; phong cách trung cổ |
| TinySwords | 410 PNG; 5 loại quân × 5 màu, công trình, terrain, UI, FX | Dáng quân khác nhau; đủ tài nguyên phụ trợ; UI có thanh nền/fill riêng | Không có Hurt/Death cho quân; chi tiết và mật độ pixel khác TinyRPG; nhiều nội dung RTS không hợp arena |

TinyRPG dùng sheet 100×100 nhưng phần thân chỉ khoảng 17×21 (Soldier) hoặc 22×15 (Orc), PPU 16. TinySwords Warrior dùng khung 192×192, thân khoảng 79×89, PPU 64: cao khoảng 1,39 đơn vị so với Soldier khoảng 1,31. Vì vậy không đo kích thước nhân vật bằng toàn bộ khoảng trong suốt của sheet. Lancer khung 320×320, vũ khí dài và hoạt ảnh nhiều hướng, không phù hợp với đòn cung hiện tại bằng Warrior.

Sprite nhập Point, không mipmap, không nén. Giữ PPU của từng gói; đây là khác biệt mật độ pixel đã chấp nhận trong bản thử nghiệm, không phải chuẩn mỹ thuật thống nhất cuối cùng.

## Phân vai đang dùng

| Hạng mục | Chọn | Cách tích hợp |
|---|---|---|
| Gióng | TinySwords **Yellow Warrior** | Bộ mới `Data/Animations/Giong_TinySwords.asset`: Idle 8 khung/10fps, Run 6/12fps, Attack1 4/12fps. `Data/Characters/Giong.asset` tham chiếu bộ này và sprite đứng đầu |
| Thạch Sanh | TinyRPG Soldier | Giữ bộ đang dùng; sprite nhỏ, hoạt ảnh đầy đủ |
| Tấm | TinySwords Blue Archer | Giữ `Data/Animations/Tam.asset`; dáng bắn tầm xa khác Gióng |
| Cô Hồn | TinyRPG Orc | Giữ bộ có hoạt ảnh chết, phù hợp số lượng lớn |
| Bốn quái khác, Chằn Tinh | Sprite của nhóm | Gói ngoài không có hình tương ứng đáng tin cậy; không thay quái bằng quân lính cùng dáng |
| Rìa sân | TinySwords **Bushes 1–4, Rocks 1–4** | Prefab `Prefabs/Environment/ArenaStageDecor.prefab`, 20 sprite hoạt động mỗi chặng, không collider |
| UI | Các phần TinySwords đã tích hợp trước đây | Đợt này không thay cấu trúc HUD/thẻ |
| Đạn, sóng âm, Trống Đồng | Tài nguyên hiện tại của nhóm | Giữ tín hiệu đòn địch và hoa văn Đông Sơn |

Warrior vàng dễ nhận ra giữa các quái màu chàm và khác Archer xanh. Kích thước trong thế giới tương đương Soldier nên không chỉnh collider, bán kính sát thương hay tầm đánh. Attack1 chỉ là biểu diễn: sát thương vẫn do vũ khí hiện tại quyết định. TinySwords không có Death: Gióng gục giữ một khung đứng tĩnh và dùng vòng cứu/tiến độ của T-41; không gọi đó là hoạt ảnh ngã.

## Ba chặng trang trí

| Wave | Nhóm prefab | Bố trí |
|---|---|---|
| 1–5 | SanDinh | Bụi cây xen đá, sắc trầm ấm |
| 6–10 | RuongLua | Nhiều bụi cây, xanh trầm |
| 11–15 | AmPhu | Đá xanh tối; boss ở wave 15 |

Bụi dùng khung đầu tiên, không phát chuyển động trên toàn sân. Các cụm đặt thưa ở mép, renderer sorting order -80 (trên sàn -100, dưới nhân vật/đòn đánh). Tint giảm độ sáng; không che vùng trung tâm và không thêm vật cản hay thay vị trí spawn. Sàn gạch và biên va chạm giữ nguyên. Đây là dấu hiệu chặng thử nghiệm, chưa phải ba bản đồ hoàn chỉnh.

`VFX/ArenaStageVisuals.cs` chọn nhóm bằng `RunManager.CurrentWave` đã đồng bộ. Cùng một luồng cho host và client, người vào muộn nhận đúng chặng; Restart về nhóm đầu. Prefab được dựng sẵn, không sinh/huỷ trang trí trong vòng lặp và không dùng RNG gameplay.

## Asset chưa chọn

- **Buildings:** lâu đài, nhà gỗ kiểu châu Âu làm lệch bối cảnh đình làng.
- **Lancer/Pawn/Monk:** thêm quân mới không giúp phân biệt vai trò hơn ba hình đã chọn.
- **Cây lớn, mây, cừu, vịt, vàng/thịt:** dễ che sân hoặc khiến người chơi tưởng có tài nguyên/cơ chế thu thập khác.
- **Tileset cỏ sáng, nước:** cần bố trí chuyển tiếp đầy đủ và cân chỉnh tương phản; không thay toàn bộ nền gạch tối chỉ vì gói có sẵn.
- **Fire/Explosion:** sáng, diện tích lớn và sắc nóng dễ lẫn với tín hiệu đòn địch; giữ FX chiến đấu của nhóm.

## Tái tạo và thay thế

Mở Arena ở Edit Mode, chạy `LAC > Content > Apply 15 Waves and ThirdParty Art`. Công cụ trong `VFX/Editor/ArenaContentUpgrade.cs` cập nhật asset và prefab bằng Unity API; chạy lại không xoá thêm wave. Bảng 16 dòng được bỏ dòng thường thứ 15, giữ nguyên 1–14 và boss cuối. Bảng 15 dòng được giữ nguyên.

Khi thay bằng mỹ thuật cuối, cập nhật `CharacterData._bodySprite/_animationSet` và renderer trong prefab trang trí trước khi gỡ gói. Không xoá toàn bộ ThirdParty khi dữ liệu nhân vật/UI còn tham chiếu. Không tìm thấy tệp giấy phép trong hai thư mục hiện có; nguồn và quyền phát hành cần được ghi nhận trước khi đóng gói thương mại.

Kiểm chứng của lần tích hợp này: [ARENA_CONTENT_TESTS.md](ARENA_CONTENT_TESTS.md).

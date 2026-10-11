# Asset bên thứ ba — cập nhật 07/10/2026

Nguồn: `Assets/ThirdParty/`, đã kiểm kê 34 PNG TinyRPG và 410 PNG TinySwords. Giữ ảnh, sprite slice, Point filtering, PPU và metadata nguồn. Dữ liệu, animation và prefab tích hợp nằm trong `_LAC/`.

Theo yêu cầu ngày 07/10, bản đồ án dùng ba lớp TinySwords thay cho bộ ba truyền thuyết. Mục tiêu là hoàn thiện gameplay và hệ thống kỹ thuật nhanh hơn, không yêu cầu tự vẽ lại người chơi.

| Vai trò | Asset | Tích hợp |
|---|---|---|
| Warrior | `Units/Yellow Units/Warrior` | Kiếm chém cung; `Data/Characters/Warrior.asset`, `Data/Animations/Warrior_TinySwords.asset` |
| Archer | `Units/Blue Units/Archer` | Cung, clip Shoot và Arrow; asset Archer và `Prefabs/Projectiles/Arrow.prefab` |
| Lancer | `Units/Purple Units/Lancer` | Thương đâm xuyên; clip Right/Up/Down/UpRight/DownRight; asset Lancer và `Prefabs/VFX/SpearThrust.prefab` |
| Cô Hồn | TinyRPG Orc | Giữ bộ quái hiện có với hoạt ảnh chết |
| Quái khác / boss | Asset của nhóm | Giữ silhouette và tín hiệu đòn đang dùng |
| Rìa sân | TinySwords Rocks | Đã gỡ toàn bộ Bushes khỏi cả ba chặng |

Warrior/Archer có frame 192×192, Lancer 320×320; cùng PPU 64 nên không lấy kích thước cả sheet làm kích thước thân. Chân dung Lancer dùng tư thế thương ngang để không đè tiêu đề thẻ chọn. Không đổi collider player theo phần trong suốt của sheet.

Idle chạy 10 FPS, Run 12 FPS, Attack 18 FPS để mỗi đòn hoàn tất trong chu kỳ. Lancer chọn clip theo hướng mục tiêu, sprite lật cho hướng trái. Các lớp dùng Death một khung tĩnh vì gói không có hoạt ảnh ngã; trạng thái gục/hồi sinh hiển thị bằng vòng cứu hiện có, trúng đòn dùng SpriteFlash.

## Bản đồ và phản hồi chiến đấu

`ArenaStageDecor.prefab` chỉ giữ đá rìa sân; 1–3 tông ấm, 4–6 xanh trầm, 7–10 xanh tối. Không collider, sorting order -80, dưới người/quái/đòn. Nhóm được chọn từ wave đã đồng bộ; vào muộn và Restart vẫn đúng. Nền gạch và biên arena giữ nguyên.

Warrior dùng vệt cung đúng tầm/góc; Lancer dùng đường viền dải đâm đúng tầm/bề rộng; Archer dùng sprite tên. Tất cả được pool, không gắn NetworkIdentity. FX người chơi dùng chàm, đòn địch giữ nhóm son và lớp vẽ cao hơn. Sóng Đông Sơn/Trống Đồng vẫn có trong hệ thống, ba vũ khí vật lý không vẽ thêm sóng nhạc cụ.

Chưa chọn Buildings/Pawn/Monk/cây lớn/mây/động vật: không thêm chức năng cần thiết cho vòng arena và có thể che sân. Không thay nền bằng tileset cỏ sáng hoặc thêm hiệu ứng lửa/nổ sáng chỉ vì gói có sẵn.

## Tái tạo

Mở Arena ở Edit Mode, chạy `LAC > Content > Apply Warrior Archer Lancer`. Công cụ `Player/Editor/PlayableClassesContent.cs` tạo animation, cấu hình dữ liệu, prefab vũ khí/màn chọn và gỡ bụi. Chạy lại không nhân đôi màn chọn. GUID của ba CharacterData cũ được giữ khi đổi tên asset, bảo toàn tham chiếu.

Công cụ nội dung (10 wave hiện hành) đã cập nhật để không khôi phục bụi hoặc bộ ba cũ. Dữ liệu animation truyền thuyết còn trên đĩa để tham khảo, registry hiện hành chỉ trỏ ba lớp mới.

Nguồn và quyền phát hành của asset cần được lưu cùng hồ sơ thương mại: không tìm thấy tệp giấy phép trong bản thư mục đã nhận. Việc dùng tài nguyên bên ngoài phải được ghi đúng trong báo cáo đồ án; không ghi sprite này là tự vẽ.

Nhịp 10 wave tính giờ: [TEN_WAVE_TESTS.md](TEN_WAVE_TESTS.md). Kiểm chứng ba lớp trước đó: [PLAYABLE_CLASSES_TESTS.md](PLAYABLE_CLASSES_TESTS.md). Kiểm chứng lần tích hợp 15 wave trước: [ARENA_CONTENT_TESTS.md](ARENA_CONTENT_TESTS.md).

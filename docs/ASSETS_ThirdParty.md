# Asset bên thứ ba và nhân vật tích hợp — cập nhật 18/10/2026

Nguồn: `Assets/ThirdParty/`, đã kiểm kê 34 PNG TinyRPG và 410 PNG TinySwords. Giữ ảnh, sprite slice, Point filtering, PPU và metadata nguồn. Dữ liệu, animation và prefab tích hợp nằm trong `_LAC/`.

Theo yêu cầu ngày 07/10, bản đồ án dùng ba lớp TinySwords thay cho bộ ba truyền thuyết. Ngày 18/10, người dùng yêu cầu thay Warrior bằng **Thánh Gióng cầm tre** từ sprite sheet tạo bằng imagegen; Archer/Lancer giữ TinySwords. Không thêm lớp thứ tư hoặc đổi ID mạng.

| Vai trò | Asset | Tích hợp |
|---|---|---|
| Thánh Gióng (Warrior) | Ảnh sinh bằng imagegen, chuẩn hóa trong `_LAC/` | Tre ngà quét cung; giữ `Data/Characters/Warrior.asset`, dùng `Data/Animations/ThanhGiong.asset` |
| Archer | `Units/Blue Units/Archer` | Cung, clip Shoot và Arrow; asset Archer và `Prefabs/Projectiles/Arrow.prefab` |
| Lancer | `Units/Purple Units/Lancer` | Thương đâm xuyên; clip Right/Up/Down/UpRight/DownRight; asset Lancer và `Prefabs/VFX/SpearThrust.prefab` |
| Cô Hồn | TinyRPG Orc | Giữ bộ quái hiện có với hoạt ảnh chết |
| Quái khác / boss | Asset của nhóm | Giữ silhouette và tín hiệu đòn đang dùng |
| Rìa sân | TinySwords Rocks | Đã gỡ toàn bộ Bushes khỏi cả ba chặng |

Archer có frame 192×192, Lancer 320×320, cùng PPU 64. Thánh Gióng có frame 64×64, PPU 32; không lấy kích thước cả sheet làm kích thước thân. Chân dung tính theo kích thước thế giới để hỗ trợ cả hai mật độ ảnh. Không đổi collider player theo phần trong suốt của sheet.

## Thánh Gióng — 18/10/2026

Sau phản hồi chơi thử, T-18E dùng thêm `ThanhGiongMotion.png`: tám frame chạy/tám frame vung tre, cùng pivot/PPU; Idle theo tư thế mới. Nhịp chân theo tốc độ thực, giữ pha qua one-shot; Hurt dùng SpriteFlash để không cắt động tác đánh. Motion hiện tại và GIF kiểm tra: [THANH_GIONG.md](THANH_GIONG.md). Phần mô tả 24 frame dưới đây là sheet v1 giữ làm nguồn Hurt/Death và đối chiếu.

Ảnh nguồn do AI tạo theo yêu cầu người dùng: `Art/Source/ThanhGiong.png` và prompt đi kèm; không ghi là sprite tự vẽ. `Player/Editor/ThanhGiongContent.cs` dùng Unity API lấy mẫu điểm ảnh nguồn 1536×1024 xuống 384×256, cắt alpha ở 192/255 thành trong suốt/đặc, đưa màu về 20 màu Đông Hồ không thuộc nhóm Son, rồi cắt 24 frame 64×64. Point filtering, không mipmap/nén; pivot chung (0.5, 0.375). Đây là pixel art chuẩn hóa từ ảnh AI, không phải sprite gốc 16×16.

`Art/Sprites/ThanhGiong.png` có bốn hàng Idle/Walk/Attack/Hurt, mỗi hàng sáu frame. AnimationSet thêm Death bằng một tư thế Hurt tĩnh giữ tới lúc hồi sinh; không nhận là có animation chết riêng. Đòn vung tre dùng cùng cung 110°, tầm 2.2 và timing/damage của Warrior hiện tại; không tăng sức mạnh chỉ vì đổi mỹ thuật. Đòn do host chốt, cùng ID Warrior trên host/client; không thêm NetworkIdentity.

Chạy `LAC.Player.Editor.ThanhGiongContent.Build()` qua MCP ở Edit Mode để tái tạo. `PlayableClassesContent.BuildCharacters()` tự áp lại phần hiển thị này nếu đã có sheet, nên thao tác dựng bộ ba không đưa kiếm TinySwords trở lại. Animation TinySwords cũ vẫn giữ làm phương án dự phòng.

TinySwords dùng Idle 10 FPS, Run 12 FPS, Attack 18 FPS; Thánh Gióng dùng Idle 8, Walk 12, Attack 30 và Hurt 18 FPS. Thời lượng đánh thực theo timing của host. Lancer chọn clip theo hướng mục tiêu, sprite lật cho hướng trái. Các lớp dùng Death một khung tĩnh; trạng thái gục/hồi sinh hiển thị bằng vòng cứu hiện có. Thánh Gióng có Hurt sáu frame, hai lớp còn lại dùng SpriteFlash.

## Bản đồ và phản hồi chiến đấu

`ArenaStageDecor.prefab` chỉ giữ đá rìa sân; 1–3 tông ấm, 4–6 xanh trầm, 7–10 xanh tối. Không collider, sorting order -80, dưới người/quái/đòn. Nhóm được chọn từ wave đã đồng bộ; vào muộn và Restart vẫn đúng. Nền gạch và biên arena giữ nguyên.

Warrior dùng vệt cung đúng tầm/góc; Lancer dùng đường viền dải đâm đúng tầm/bề rộng; Archer dùng sprite tên. Tất cả được pool, không gắn NetworkIdentity. FX người chơi dùng chàm, đòn địch giữ nhóm son và lớp vẽ cao hơn. Sóng Đông Sơn/Trống Đồng vẫn có trong hệ thống, ba vũ khí vật lý không vẽ thêm sóng nhạc cụ.

Chưa chọn Buildings/Pawn/Monk/cây lớn/mây/động vật: không thêm chức năng cần thiết cho vòng arena và có thể che sân. Không thay nền bằng tileset cỏ sáng hoặc thêm hiệu ứng lửa/nổ sáng chỉ vì gói có sẵn.

## Tái tạo

Mở Arena ở Edit Mode, chạy `LAC > Content > Apply Warrior Archer Lancer`. Công cụ `Player/Editor/PlayableClassesContent.cs` tạo animation, cấu hình dữ liệu, prefab vũ khí/màn chọn và gỡ bụi. Chạy lại không nhân đôi màn chọn. GUID của ba CharacterData cũ được giữ khi đổi tên asset, bảo toàn tham chiếu.

Công cụ nội dung (10 wave hiện hành) đã cập nhật để không khôi phục bụi hoặc bộ ba cũ. Dữ liệu animation truyền thuyết còn trên đĩa để tham khảo, registry hiện hành chỉ trỏ ba lớp mới.

Nguồn và điều kiện sử dụng TinyRPG/TinySwords cần được lưu cùng báo cáo đồ án, dù không phát hành: không tìm thấy tệp giấy phép trong bản thư mục hai gói đã nhận. Việc dùng tài nguyên bên ngoài phải được ghi đúng trong báo cáo đồ án; không ghi sprite này là tự vẽ.

## TextMesh Pro cho giao diện đạo diễn

Giao diện mới dùng TMP/uGUI chính thức từ package `com.unity.ugui`: bản sao MobileSDF tại `Assets/_LAC/Art/Shaders/Resources/UI/DirectorSDF.shader`, include `TMPro_Properties.cginc` và giấy phép `Art/Shaders/TMP-LICENSE.txt` (Unity Companion License). Quy tắc ngắt dòng `Leading.txt`/`Following.txt` và cấu hình riêng nằm ở `Data/UI/Resources/DirectorUI/`; shader trong Resources được bàn giao cùng tài nguyên UI, không phụ thuộc thao tác nhập TMP Essentials trên máy nhóm.

Phần bổ sung phục vụ text của HUD đạo diễn và chọn chế độ; font **Liberation Sans** được sao chép nguyên vào `Art/Fonts/Resources/UI/DirectorFont.ttf`, kèm `Art/Fonts/LiberationSans-OFL.txt`. Font TMP tiếng Việt tạo từ dữ liệu TTF nhúng này. Cấu hình TMP được sao chép riêng cho phiên Play, không ghi sửa asset cấu hình khi chạy. Giữ nguyên thư mục TMP/tài nguyên người dùng đã có. HUD đạo diễn nằm phía trên thanh máu góc dưới trái, F3 mở dữ liệu; xem [DIRECTOR_TESTS.md](DIRECTOR_TESTS.md). Tài nguyên shader/font đã có trong dự án không thay thế nghiệm thu executable mới.

Nhịp 10 wave tính giờ: [TEN_WAVE_TESTS.md](TEN_WAVE_TESTS.md). Kiểm chứng ba lớp trước đó: [PLAYABLE_CLASSES_TESTS.md](PLAYABLE_CLASSES_TESTS.md). Kiểm chứng lần tích hợp 15 wave trước: [ARENA_CONTENT_TESTS.md](ARENA_CONTENT_TESTS.md).

Phạm vi 11/10/2026: một arena, không bắt buộc ba tileset/mỹ thuật tự vẽ. Hướng sprite Ma Da TinyRPG 16×16 mới là đề xuất thiết kế, chưa tích hợp; giữ TinySwords đang chạy, kiểm tra tỷ lệ và pivot khi bổ sung. Xem [THESIS_SCOPE.md](THESIS_SCOPE.md).

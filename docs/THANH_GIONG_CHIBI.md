# Thánh Gióng chibi và tre ngà riêng — T-18F, 19/10/2026

Theo yêu cầu người dùng, thử riêng Thánh Gióng theo hướng chibi pixel art thần thoại Việt ít chi tiết. Giữ Warrior ID 1, registry, máu/tốc độ/sát thương/tầm/chu kỳ và collider hiện tại. Archer/Lancer giữ mỹ thuật cũ. Đây là thử nghiệm mỹ thuật và biểu diễn chiến đấu; không mở thêm hệ thống vũ khí hay thay phạm vi đồ án.

## Ảnh và dữ liệu đang dùng

Hai ảnh nguồn được tạo bằng **built-in image_gen**, không phải nhóm tự vẽ hoặc sprite được vẽ gốc trên lưới 32px:

- [Nguồn cơ thể](../Assets/_LAC/Art/Source/ThanhGiongChibi.png) và [prompt đầy đủ](../Assets/_LAC/Art/Source/ThanhGiongChibi.prompt.txt): đầu lớn, tóc búi, giáp đồng, khăn chàm, quần sáng; tay không cầm vũ khí; 3×3 dáng.
- [Nguồn tre ngà](../Assets/_LAC/Art/Source/TreNga.png) và [prompt đầy đủ](../Assets/_LAC/Art/Source/TreNga.prompt.txt): một cây tre ngang, chuôi bên trái, không tay/nhân vật/vệt chém.
- Runtime: [ThanhGiongChibi.png](../Assets/_LAC/Art/Sprites/ThanhGiongChibi.png), 144×144, chín ô 48×48, PPU32, pivot (24,20). Phần thân khoảng 32px cao; không gọi kích thước ô là độ chi tiết nhân vật. Idle hai frame/2 FPS; chạy sáu frame/12 FPS ở tốc độ 4; gục một dáng ngồi.
- Runtime: [TreNga.png](../Assets/_LAC/Art/Sprites/TreNga.png), ô 64×16 với phần tre dài 48px, pivot chuôi (14,8), PPU32.
- [ThanhGiongChibi.asset](../Assets/_LAC/Data/Animations/ThanhGiongChibi.asset), [TreNgaVisual.asset](../Assets/_LAC/Data/Animations/TreNgaVisual.asset), gán qua `CharacterData.DetachedWeapon` chỉ cho Warrior.

`ThanhGiongChibiContent.Build()` trong [công cụ Editor](../Assets/_LAC/Scripts/Player/Editor/ThanhGiongChibiContent.cs) tái tạo runtime qua Unity API khi **đã thoát Play**. Lấy mẫu nearest-neighbor, cùng hệ số cho mọi dáng, căn chân và trục đầu, alpha dưới 192 bị bỏ, pixel còn lại về alpha255 và 20 màu Đông Hồ không thuộc Son. Point, không mipmap/nén. Sprite ID được giữ khi dựng lại. Các builder Gióng v1/motion áp lại chibi nếu đã có tài nguyên, tránh vô tình trở về sheet cũ.

## Chạy và đánh hoạt động thế nào

`PlayerAnimatorDriver` tạo một `PlayerWeaponVisual` trong khởi tạo player; component tạo một renderer con `DetachedWeapon`, dùng cùng vật liệu cơ thể. Không sinh/hủy theo từng cú đánh, không có NetworkIdentity hay RPC mới trên tre. Các lớp không có dữ liệu vũ khí rời thì renderer này ẩn.

Trong working tree T-50 hiện hành, driver nhận `AttackStarted` do host chốt và truyền hướng, thời lượng, contact, tầm, độ trễ đã qua cho tre. Vũ khí dùng góc lấy đà → quét đúng hướng tại contact → vung quá đà → thu về từ `TreNgaVisual.asset`. Thời lượng gốc vẫn 0.30s/contact0.15s/chu kỳ0.65s; tốc đánh nâng cấp rút timeline theo cue. Sự kiện đến muộn bắt kịp pha, không diễn lại đòn đã hết. Không gọi one-shot Attack toàn thân, nên Idle/Walk tiếp tục và nhịp chân theo tốc độ thực.

Tre giữ chiều dài qua cả đòn, không phồng/thu từng frame. Tại contact, đầu tre nằm ở bán kính host đã chốt; chỉ nâng cấp tầm mới thay chiều dài biểu diễn. Tre nằm sau cơ thể khi hướng lên, trước khi hướng xuống/ngang, và nhận hệ số chớp của cơ thể. Đây vẫn là một sprite nhìn nghiêng xoay trong mặt phẳng, không phải tám bộ hình bàn tay/góc nhìn riêng.

Gục ẩn tre và giữ dáng Down mới; đổi lớp, hủy đòn, chọn thẻ, kết thúc và Restart xóa pha đang diễn. Vùng sát thương vẫn do `WeaponAuto` trên host quyết định và vệt cung hiện có báo toàn bộ vùng quét; hình tre không phải collider mới.

Nhánh nền trước T-50 dùng sự kiện `Fired` tại contact: adapter tương thích bắt đầu biểu diễn ở pha contact và thu về. Phần lấy đà có trước sát thương dùng đường `AttackStarted` của working tree T-50; không dùng commit mỹ thuật này để báo T-50 hoàn thành.

## Kiểm chứng và giới hạn

- [Chuỗi ảnh](validation/T18F/frames.png), [GIF](validation/T18F/motion.gif), [CSV frame](validation/T18F/frames.csv): đã xem hình Game View thật. Fixture 5s điều khiển di chuyển phải/trái, vừa đi vừa đánh, đứng đánh; 121 frame, đủ sáu Run, 39 frame vũ khí đang đánh, 25 frame vừa Run vừa đánh. Địch đứng yên nhận sát thương thật từ 40 xuống 15 trước reset.
- [83 kiểm tra](validation/T18F/motion-checks.txt): hướng/contact bán kính/lớp trước sau ở tám hướng; hủy, cue muộn/hết hạn, Archer/Lancer không có tre, Warrior trở lại, gục/Restart. Một số kiểm tra được lặp mỗi frame đang đánh.
- [Ba lớp](validation/T18F/classes.txt): chọn, tạo host, HP, đòn, dash và nối lại. [243 kiểm tra chiến đấu](validation/T18F/combat.txt): host thật, one contact, nhóm mục tiêu, tốc đánh tối đa, thẻ/gục/kết thúc/Restart; Warrior kiểm tra tre đánh trong khi locomotion không bị chiếm.
- [Audit](validation/T18F/assets.txt): alpha nhị phân, không Son, import/pivot/clip, 3.646 pixel đặc, ID ổn định qua Build và builder cũ giữ chibi; chỉ số hiện tại không đổi.
- [Console](validation/T18F/console.txt): 0 lỗi/0 cảnh báo trong Play trước Stop. MCP đã kết nối đúng dự án; lỗi kết nối/import-worker cũ được đọc trước lượt thử sạch.

Các fixture dùng địch đứng yên, điều khiển input bằng mã và tạm bất tử cho người chơi trong đoạn quay. Không là chơi tự nhiên để cân bằng, bản build Windows, hai máy qua Internet hoặc chứng nhận cảm giác chủ quan. Animation AI vẫn có giới hạn nhất quán ở từng pixel; chưa có góc nhìn lưng riêng hoặc rig tay bám chuôi theo tám hướng. Các thử nghiệm này không hoàn tất T-50B/T-50/T-51 hoặc yêu cầu online của đồ án.

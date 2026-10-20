# Thánh Gióng trong game — 18/10/2026

Thánh Gióng cầm tre ngà thay phần hiển thị của Warrior. Màn chọn nhân vật có **Thánh Gióng / Archer / Lancer**; chọn Thánh Gióng rồi bấm **Vào trận / Tạo host** để chơi. Giữ `CharacterId.Warrior = 1`, GUID của `Warrior.asset` và registry ba lớp. Các máy cùng dùng tài nguyên dự án; mạng vẫn chỉ truyền ID và sự kiện, không gửi ảnh hay gắn NetworkIdentity mới.

## Hiện hành — T-18I, 20/10/2026

Theo mẫu mới được chọn: **Forge32 liền cả cây tre**, chuyển động cơ bản nhún/nghiêng toàn hình, không vẽ chồng tre rời. Giữ ID/chỉ số. Nguồn/prompt/pipeline/ảnh trong game và giới hạn từ nguồn tĩnh: [THANH_GIONG_FORGE.md](THANH_GIONG_FORGE.md). [Pixel T-18H](THANH_GIONG_PIXEL.md) và [cartoon T-18G](THANH_GIONG_CARTOON.md) là lịch sử.

## T-18F — lịch sử, 19/10/2026

Đã thử **chibi ít chi tiết, cơ thể không cầm sẵn vũ khí và tre ngà là sprite riêng** theo yêu cầu người dùng. `Warrior.asset` dùng `ThanhGiongChibi.asset`/`TreNgaVisual.asset`; chân tiếp tục chạy trong cú đánh. Nguồn AI, prompt, cách dựng lại và bằng chứng mới: [THANH_GIONG_CHIBI.md](THANH_GIONG_CHIBI.md). Phần T-18D/E bên dưới là lịch sử và tài nguyên đối chiếu, không còn là bộ frame đang gán cho Warrior.

## Chỉnh chạy/đánh sau phản hồi chơi thử — T-18E (lịch sử)

Bản v1 có chân/thân đổi dáng và hướng cây tre thiếu liên tục giữa các frame chạy; cú đánh từ giơ tre sang cúi thấp trong 0.18s tạo cảm giác giật. Bản hiện tại dùng sheet motion mới: [nguồn AI](../Assets/_LAC/Art/Source/ThanhGiongMotion.png), [prompt](../Assets/_LAC/Art/Source/ThanhGiongMotion.prompt.txt), [ảnh runtime](../Assets/_LAC/Art/Sprites/ThanhGiongMotion.png). Công cụ [ThanhGiongMotionContent.cs](../Assets/_LAC/Scripts/Player/Editor/ThanhGiongMotionContent.cs) chuẩn hóa lưới 4×4 thành 16 frame 64×64, PPU 32, cùng alpha/palette của v1.

- Chạy tám frame, 16 FPS ở tốc độ 4; giữ tre cùng phía, có chân chống/đưa chân qua/đổi chân. Idle lấy tư thế kết thúc Swing để giảm đổi dáng khi chuyển trạng thái.
- `SpriteAnimationSet.WalkReferenceSpeed` bật nhịp chân theo tốc độ thực cho Thánh Gióng. `PlayerMovement.VisualSpeed` đọc vận tốc local hoặc độ dời remote; không đổi vật lý/mạng. `SpriteAnimator` làm mượt nhịp chân, giới hạn 0.35–1.75 lần; asset cũ để reference 0 giữ FPS cũ.
- Chu kỳ chân vẫn tiến trong lúc one-shot và được dùng lại khi trở về Walk; mỗi cú đánh không bắt chu kỳ chạy quay lại frame 0.
- Vung tre tám frame, có lấy đà/vung/thu tay. Trong working tree T-50 hiện tại, động tác tăng 0.18 → **0.30s**, contact tăng 0.09 → **0.15s**, cùng nguồn dữ liệu host. Sát thương, tầm và chu kỳ 0.65s không đổi. Không chỉ kéo dài hình ở client.
- Tắt phát Hurt toàn thân tự động để cúi người không cắt ngang cú đánh liên tục khi áp sát; SpriteFlash/HP/số sát thương vẫn báo bị đánh. Hurt/Death v1 giữ làm tài nguyên gục, chưa có animation chết riêng.

Đã xem [chuỗi frame Game View](validation/T18E/frames.png); có [GIF chạy và vừa di chuyển vừa đánh](validation/T18E/motion.gif) từ 99 frame thực, cùng [CSV](validation/T18E/frames.csv). Quan sát đủ tám Run/tám Swing; địch thử HP 40 → 25. [Đo nhịp chân](validation/T18E/rate.txt): 15.94 frame tiến/giây khi chạy thường, 5.88 khi di chuyển 1.2 đơn vị/giây; trở lại Walk ở frame 01/05/01 thay vì luôn frame 00. [243 kiểm tra chiến đấu](validation/T18E/timing.txt) đạt trên working tree T-50. Đây là bằng chứng kỹ thuật/quan sát có kiểm soát; cảm giác cuối vẫn cần người chơi thử, không thay nghiệm thu Internet hoặc hoàn thành các task cân bằng.

Các mục v1 và bằng chứng T-18D dưới đây giữ để đối chiếu lịch sử. Dựng lại v1 tự áp motion v2 nếu đã có sheet; có thể gọi `LAC.Player.Editor.ThanhGiongMotionContent.Build()` ở Edit Mode để tái tạo motion riêng.

Kiểm tra sau khi sửa: [ba lớp](validation/T18E/classes.txt), [timing chạy lại](validation/T18E/timing-clean.txt), [Console 0 lỗi/0 cảnh báo](validation/T18E/console.txt), [asset và dựng lại giữ ID](validation/T18E/assets.txt). Lỗi kích thước ảnh đầu vào và lỗi tên biến trong snippet quay thử đã được sửa trước lượt kiểm tra sạch; không phải lỗi còn lại trong gameplay.

V1 thay mỹ thuật và tên, giữ chỉ số/vùng đánh của Warrior: 10 HP, tốc độ 4, cung 110°, tầm 2.2, chu kỳ 0.65 giây. Không thêm lớp, vũ khí, thẻ hoặc kỹ năng. T-18E sau đó điều chỉnh thời lượng động tác/contact như phần trên; không nghiệm thu T-50/T-50B/T-51.

## Tài nguyên và cách tái tạo

- Ảnh gốc: [ThanhGiong.png](../Assets/_LAC/Art/Source/ThanhGiong.png), sinh bằng imagegen theo yêu cầu người dùng; [prompt](../Assets/_LAC/Art/Source/ThanhGiong.prompt.txt). Không ghi là nhóm tự vẽ.
- Ảnh dùng trong game: [ThanhGiong.png](../Assets/_LAC/Art/Sprites/ThanhGiong.png), 384×256, 6 cột × 4 hàng, 24 frame 64×64; **không phải sprite gốc 16×16**.
- Animation: [ThanhGiong.asset](../Assets/_LAC/Data/Animations/ThanhGiong.asset). Idle/Walk/Attack/Hurt có sáu frame mỗi clip; Death giữ một frame Hurt, chưa có hoạt ảnh ngã riêng.
- Công cụ: [ThanhGiongContent.cs](../Assets/_LAC/Scripts/Player/Editor/ThanhGiongContent.cs). Qua MCP gọi `LAC.Player.Editor.ThanhGiongContent.Build()` khi đã thoát Play. Không sửa asset ScriptableObject trong lúc chạy.

Công cụ lấy mẫu nearest-neighbor 4:1, bỏ alpha dưới 192/255, đưa pixel còn lại về alpha 255 và màu gần nhất trong 20 màu Đông Hồ không thuộc nhóm Son. Vì vậy không có quầng alpha mềm hoặc màu đòn địch trong ảnh runtime. Import Point, PPU 32, không mipmap/nén, wrap Clamp. Pivot chung (32,24) trong mỗi ô; không đổi collider hoặc scale prefab chung.

Sprite ID được giữ qua lần build sau. `PlayableClassesContent.BuildCharacters()` tự áp lại Thánh Gióng nếu đã có sheet để tránh dựng nội dung trả Warrior về kiếm cũ. Cách tính chân dung hỗ trợ PPU 32 lẫn PPU 64 của TinySwords. Bộ animation Warrior TinySwords cũ được giữ để tham khảo.

## Kiểm chứng

Đã xem ảnh trong Unity: [màn chọn](validation/T18D/selection.png), [trong sân](validation/T18D/game.png), [contact phải](validation/T18D/attack-right.png), [contact trái](validation/T18D/attack-left.png). Nhân vật đọc được trên nền tối, chân dung không đè chữ, sprite lật theo hướng đánh.

- [Audit ảnh/asset](validation/T18D/audit-result.txt): đủ 24 sprite/clip/pivot/import; alpha nhị phân, không màu Son, giữ ID/chỉ số. 28.831 pixel đặc trong sheet runtime.
- [Ba lớp](validation/T18D/classes-result.txt): chọn lớp, tạo host, HP, cấu hình 10 wave, đòn đánh và đóng/tạo trận lại đạt.
- [Chiến đấu](validation/T18D/timing-proof.txt): 243 kiểm tra timing, hướng/di chuyển, dash, gục/Death, thẻ, kết thúc và Restart đạt trên working tree hiện tại.
- [Contact phải](validation/T18D/contact-proof.txt) / [trái](validation/T18D/left-proof.txt): `ThanhGiong_Attack_03`, flip false/true, HP địch 40 → 35 đúng một contact.
- [Console](validation/T18D/console-proof.txt): 0 lỗi, 0 cảnh báo trước khi thoát Play.
- [Dựng lại](validation/T18D/rebuild.txt): gọi Build lần nữa giữ nguyên cả 24 sprite ID và CharacterData; Editor đã thoát Play/pause, không biên dịch và không lỗi đỏ.

Các ảnh contact dùng địch đứng yên và tạm dừng đúng frame để quan sát, không phải chứng minh chơi tự nhiên, chất lượng hoạt ảnh chủ quan, bản executable hoặc co-op hai máy qua Internet. Kiểm tra timing dùng mã T-50 đang triển khai trong working tree; không đánh dấu các task cân bằng hoàn tất từ kết quả này. Lần đầu gọi fixture ba lớp sau StopHost không thỏa điều kiện wave 0; đã vào lại Play mới và chạy đạt.

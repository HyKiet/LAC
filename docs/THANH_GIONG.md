# Thánh Gióng trong game — 18/10/2026

Thánh Gióng cầm tre ngà thay phần hiển thị của Warrior. Màn chọn nhân vật có **Thánh Gióng / Archer / Lancer**; chọn Thánh Gióng rồi bấm **Vào trận / Tạo host** để chơi. Giữ `CharacterId.Warrior = 1`, GUID của `Warrior.asset` và registry ba lớp. Các máy cùng dùng tài nguyên dự án; mạng vẫn chỉ truyền ID và sự kiện, không gửi ảnh hay gắn NetworkIdentity mới.

Đây là thay mỹ thuật và tên, giữ chỉ số/vùng đánh hiện tại của Warrior: 10 HP, tốc độ 4, cung 110°, tầm 2.2, chu kỳ 0.65 giây. Không thêm lớp, vũ khí, thẻ hoặc kỹ năng. Bản cân bằng đang làm tại T-50 có sát thương 5, động tác 0.18s/contact 0.09s; việc thay sprite không nghiệm thu T-50/T-50B/T-51.

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

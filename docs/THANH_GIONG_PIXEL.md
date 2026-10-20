# Thánh Gióng pixel toàn thân — T-18H, 20/10/2026

**Lịch sử:** người dùng đã chọn [mẫu Forge32 liền cả cây tre, T-18I](THANH_GIONG_FORGE.md) để đưa vào game. Tài nguyên/kiểm chứng T-18H dưới đây giữ để đối chiếu.

Theo yêu cầu mới, Warrior trở lại pixel art 2D. **Mỗi frame là một sprite toàn thân; chỉ cây tre là sprite riêng.** Không dùng rig tay/chân rời của T-18G. Giữ ID Warrior1, GUID nhân vật, collider, chỉ số và luật sát thương hiện có. Archer/Lancer không đổi.

## Nguồn và tài nguyên

Ảnh được tạo bằng **built-in image_gen**, sau đó chuẩn hóa trong Unity. Đây là asset AI hỗ trợ, không nhận là nhóm tự vẽ hoặc đã được họa sĩ nghiệm thu từng pixel.

- [Sheet nguồn toàn thân](../Assets/_LAC/Art/Source/ThanhGiongPixelBody.png), [prompt nhân vật](../Assets/_LAC/Art/Source/ThanhGiongPixelBody.prompt.txt): búi tóc đen, giáp đồng, khăn chàm, quần sáng, giày nâu; 16 dáng trong lưới4×4. Các frame có cả tay/chân.
- [Nguồn tre](../Assets/_LAC/Art/Source/TreNgaPixel.png), [prompt tre](../Assets/_LAC/Art/Source/TreNgaPixel.prompt.txt): cây tre thon, đốt/mấu rõ, đầu cắt rỗng; không có mũi giáo, chuôi kiếm hoặc vòng kim loại.
- [Sheet runtime](../Assets/_LAC/Art/Sprites/ThanhGiongPixel.png): 256×256, 16 ô64×64, thân khoảng40px cao, PPU32, pivot(32,24). Idle4/4FPS, Run8/16FPS ở tốc độ4, Down4/12FPS giữ frame cuối. Không gọi kích thước ô64px là độ chi tiết nhân vật.
- [Tre runtime](../Assets/_LAC/Art/Sprites/TreNgaPixel.png): ô64×16, phần vẽ56×6px, pivot(10,8). Độ dày được chuẩn hóa để đọc rõ trên Game View.
- `Data/Animations/ThanhGiongPixel.asset`, `TreNgaPixel.asset`, tham chiếu từ `Data/Characters/Warrior.asset`.

Gọi `LAC.Player.Editor.ThanhGiongPixelContent.Build()` khi **đã thoát Play**. Công cụ lấy mẫu nearest-neighbor, cùng hệ số cho các dáng, căn chân/trục đầu, alpha nhị phân và 20 màu Đông Hồ ngoài nhóm Son. Import Point, không mipmap/nén. SpriteEditorDataProvider kiểm tra quyền sửa và giữ ID qua Build; không sửa metadata bằng tay. Chuỗi builder cũ áp lại pixel cuối cùng, tránh vô tình quay về cartoon.

## Animation và đòn đánh

`SpriteAnimator` phát sprite toàn thân; Walk điều chỉnh theo vận tốc thực, giữ pha chân khi đánh. Không xoay/tách riêng tay chân. `CharacterData.CartoonVisual` được bỏ trống; các renderer mảnh cũ không vẽ. Chân dung và dash-afterimage dùng sprite toàn thân mới.

`TreNgaPixel.asset` lưu điểm cầm cho12 frame Idle/Run. Công cụ tìm vùng da nắm tay từ nguồn trước khi ép palette, căn về lưới pixel và lưu tọa độ cạnh sprite tương ứng. `DetachedWeaponData.HandPosition` tra điểm cầm/lật trái phải; `PlayerWeaponVisual` đặt chuôi tại đó. Tay vẫn nằm trong ảnh nhân vật. Dữ liệu cũ không có điểm cầm tiếp tục dùng offset cũ.

Tre lấy đà → quét qua hướng contact → vung quá đà → thu về bằng timeline host hiện có; không gọi Attack one-shot toàn thân làm chân dừng chạy. Trong working tree T-50:0.30s tổng/contact0.15s, tăng tốc theo cue và bắt kịp cue trễ; không đổi damage/tầm/chu kỳ. Tip radius và lớp trước/sau vẫn được kiểm tra. Host quyết sát thương; tre không có NetworkIdentity hoặc collider gây damage.

Gục phát bốn frame toàn thân và ẩn tre; hồi sinh/Restart khôi phục. Hủy đòn, đổi lớp và cue hết hạn không phát lại cú đánh cũ.

## Bằng chứng

- Đã xem [màn chọn](validation/T18H/selection.png), [trận mới](validation/T18H/game-fresh.png), [chuỗi frame Game View](validation/T18H/frames.png) và [ảnh fixture](validation/T18H/game.png). [GIF chuyển động](validation/T18H/motion.gif), [CSV frame](validation/T18H/frames.csv).
- [Chuyển động và vòng đời](validation/T18H/motion-checks.txt): đủ8 frame Run, vừa chạy vừa đánh, chuôi bám nắm tay theo frame, tám hướng/contact/bán kính/lớp, hủy/cue trễ/hết hạn, đổi lớp, Down cuối và Restart. Địch đứng yên nhận sát thương thật40→15 trước reset.
- [243 kiểm tra chiến đấu](validation/T18H/combat.txt): Mirror host thật, contact/damage/nhóm mục tiêu/tốc đánh/thẻ/gục/kết thúc/Restart. [Ba lớp](validation/T18H/classes.txt) đạt. [Console](validation/T18H/console.txt) không lỗi đỏ trong Play.
- [Audit](validation/T18H/assets.txt):16 sprite, import/alpha/palette, điểm cầm lật gương, ID ổn định, builder cũ giữ pixel, chỉ số không đổi. [Trận mới](validation/T18H/menu.txt): Thánh Gióng10/10HP, cartoon tắt.

Fixture điều khiển input bằng mã và tạm bất tử trong đoạn quay; không là chơi tự nhiên hoặc nghiệm thu Internet/build/cân bằng. HUD8/10 ở ảnh fixture là HP trước khi công cụ bật bất tử, không phải máu gốc mới. Đầu/giáp AI vẫn có sai khác nhỏ giữa các dáng; chưa có bộ nhìn lưng/tám hướng riêng. Tre xoay trong mặt phẳng, không phải animation cánh tay tám hướng. Chất lượng cảm giác cuối cùng cần người dùng chơi thử; không dùng kiểm tra kỹ thuật để tuyên bố giống hệt Brotato hoặc hoàn tất T-50B/T-50/T-51.

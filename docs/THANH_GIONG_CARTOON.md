# Thánh Gióng cartoon — T-18G, 19/10/2026

**Lịch sử:** ngày20/10/2026, người dùng yêu cầu [pixel toàn thân T-18H](THANH_GIONG_PIXEL.md), chỉ tách cây tre. Các tài nguyên/kiểm chứng cartoon dưới đây giữ để đối chiếu.

Người dùng đã duyệt mẫu nét mượt, viền đậm theo hướng Brotato và yêu cầu áp dụng vào game ngày19/10. T-18G từng thay pixel T-18F. Giữ ID Warrior1, GUID nhân vật, chỉ số, collider và vùng sát thương. Archer/Lancer và bản đồ giữ asset hiện tại.

## Tài nguyên và cách dựng

- [Mẫu duyệt](../Assets/_LAC/Art/Source/ThanhGiongCartoonPreview.png), [prompt](../Assets/_LAC/Art/Source/ThanhGiongCartoonPreview.prompt.txt); [nguồn parts](../Assets/_LAC/Art/Source/ThanhGiongCartoonParts.png), [prompt tách mảnh](../Assets/_LAC/Art/Source/ThanhGiongCartoonParts.prompt.txt). Cả hai do **built-in image_gen** tạo, không phải nhóm tự vẽ.
- [Atlas runtime](../Assets/_LAC/Art/Sprites/ThanhGiongCartoon.png): 1024×1024, sáu sprite thân/hai chân/hai tay/tre ngang, PPU320. [Ảnh toàn thân](../Assets/_LAC/Art/Sprites/ThanhGiongCartoonFull.png): 256×256/PPU172.8, ghép từ các mảnh, dùng cho portrait/dash afterimage. Giữ màu và alpha mượt; Bilinear, không mipmap/nén. Không đổi PPU/camera toàn dự án.
- `Data/Animations/ThanhGiongCartoonParts.asset`, `ThanhGiongCartoon.asset`, `TreNgaCartoon.asset`: mảnh, nhịp bước, vị trí tay/chân, sprite tre và hình dáng cú vung.

Gọi `LAC.Player.Editor.ThanhGiongCartoonContent.Build()` qua MCP khi **đã thoát Play**. Nguồn AI không tuân thủ hàng đều, nên công cụ dùng vùng đã quan sát và bbox alpha, resample/cắt qua SpriteEditorDataProvider với capability/ID checks. ID ổn định qua Build. Builder Gióng v1/motion/chibi áp lại cartoon nếu đã có; không sửa `.meta` thủ công hoặc asset trong Play.

## Chạy và đánh

`CharacterData.CartoonVisual` chỉ có trên Warrior. `PlayerWeaponVisual.Awake` tạo `PlayerCartoonVisual` một lần; các renderer con cũng tạo một lần. Không thêm NetworkIdentity/RPC hoặc sinh/hủy theo cú đánh. Renderer gốc vẫn nhận sprite/lật/chớp và giữ ảnh toàn thân cho afterimage, chỉ giấu phần vẽ bằng `forceRenderingOff`. Các mảnh dùng cùng material và hệ số màu. Đổi sang lớp khác khôi phục renderer thường để Archer/Lancer không biến mất.

Chân bước luân phiên theo tốc độ thực, thân nhún/nghiêng nhẹ, tay cầm đặt đúng chuôi sau khi tre cập nhật. Không dùng AI vẽ lại mặt mỗi frame. Tre tiếp tục timeline host của working tree T-50: lấy đà/contact/vung quá đà/thu về; chân không bị one-shot Attack chiếm. Gục ẩn tay/chân/tre và nghiêng thân, hồi sinh/Restart khôi phục. Đây là animation ghép mảnh trong mặt phẳng, chưa có tám góc nhìn hoặc rig tay đầy đủ. Không đổi damage/tầm hoặc tuyên bố gameplay giống hệt Brotato.

## Kiểm chứng và giới hạn

- Đã xem [màn chọn](validation/T18G/selection.png), [trận mới](validation/T18G/game-fresh.png), [chuỗi ảnh](validation/T18G/frames.png) và [Game View fixture](validation/T18G/game.png). [GIF](validation/T18G/motion.gif), [CSV frame](validation/T18G/frames.csv).
- [Chuyển động](validation/T18G/motion-checks.txt): 196 xác nhận, 115 frame/5s, 81 vị trí chân, 36 frame tre đánh/21 frame vừa chạy vừa đánh; tay bám chuôi, tám hướng/contact/tầm/lớp trước sau, hủy/cue muộn/hết hạn/đổi lớp/gục/Restart đạt. Số xác nhận có kiểm tra lặp mỗi frame. Địch đứng yên mất HP40→15; tạm bất tử cho player trong đoạn quay.
- [243 kiểm tra chiến đấu](validation/T18G/combat.txt) và [ba lớp](validation/T18G/classes.txt) đạt trên working tree. Lượt ba lớp ghi0 lỗi/2 cảnh báo; lượt host sau đó có [Console0 lỗi/0 cảnh báo](validation/T18G/console.txt).
- [Audit](validation/T18G/assets.txt): đủ mảnh, alpha trong suốt/AA, Bilinear, không Son với sai số0.005, ID ổn định, builder cũ giữ cartoon, chỉ số không đổi. Không tuyên bố mọi shade màu trùng 20 màu pixel.
- [Trận mới](validation/T18G/menu.txt): chọn Warrior trước kết nối, sinh Thánh Gióng10/10 HP, cartoon bật, Console0/0. HUD8/8 ở ảnh fixture là trạng thái công cụ đổi lớp, không phải máu gốc Warrior.

Không dùng các fixture này để đóng cân bằng tự nhiên, executable Windows, hai máy qua Internet hoặc nghiệm thu cảm giác chủ quan. T-50B/T-50/T-51 và yêu cầu online vẫn giữ trạng thái riêng.

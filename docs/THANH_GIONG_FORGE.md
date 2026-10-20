# Thánh Gióng Forge32 liền cây tre — T-18I, 20/10/2026

Theo yêu cầu áp dụng mẫu vừa duyệt: Warrior dùng **sprite32×32 có sẵn cây tre trong cùng ảnh**. Tắt dữ liệu vũ khí rời/cartoon; không vẽ chồng hai cây tre. Giữ ID Warrior1, GUID nhân vật, collider, chỉ số và vùng đánh. Archer/Lancer giữ tài nguyên hiện có.

## Nguồn và nhập Unity

- [Mẫu32 được áp dụng](../Assets/_LAC/Art/Source/ThanhGiongForge32.png), [nguồn image_gen](../Assets/_LAC/Art/Source/ThanhGiongForgeMaster.png), [prompt thực tế](../Assets/_LAC/Art/Source/ThanhGiongForgeMaster.prompt.txt).
- Mẫu do **built-in image_gen** tạo, Agent Sprite Forge0.4.0 `master_still.py` và `finish_frames.py pixel` xử lý ở lượt trước. Không phải nhóm tự vẽ; model ID và giá không được host tool trả về. [QA nguồn](validation/T18I/forge-source-qa.json), [finish report gốc](validation/T18I/forge-finish.json); các đường dẫn tương đối bên trong hai báo cáo này thuộc bundle gốc `output/sprites/thanh-giong-32-v1/`.
- Mẫu đã duyệt dùng22 màu học từ nguồn để giữ màu da/tre; không ép về20 màu cố định. Vẫn kiểm tra loại bốn màu Son với sai số0.005. Ngoại lệ này chỉ áp dụng Thánh Gióng; nền/VFX/đòn địch theo quy tắc cũ.
- [Portrait32](../Assets/_LAC/Art/Sprites/ThanhGiongForge32.png) giữ **nguyên byte** mẫu đã duyệt. [Atlas chuyển động](../Assets/_LAC/Art/Sprites/ThanhGiongForgeMotion.png)256×384, 24 ô64×64; hình nhân vật gốc vẫn khoảng29px cao, không phải64px. Import PPU32/Point/không mipmap/nén; không đổi camera hoặc PPU toàn game.
- `Data/Animations/ThanhGiongForge.asset`: Idle4/4FPS, Walk8/14FPS, Attack8/0.30s gốc và Down4/12FPS giữ cuối. Hurt dùng nháy hiện có. `Player/Editor/ThanhGiongForgeContent.Build()` dựng tài nguyên qua Unity API khi **đã thoát Play**; giữ ID qua rebuild, có kiểm tra quyền sửa sprite. Chuỗi builder cũ áp lại Forge cuối cùng.

## Giới hạn chuyển động và chiến đấu

**Nguồn chỉ là một dáng tĩnh.** Các frame được bake bằng dịch/nghiêng **cả hình liền khối**, không phải24 dáng chạy/vung tay mới do AI hoặc họa sĩ vẽ. Idle nhún1px; Walk nhún0–2px/nghiêng3°; Attack lấy đà/nghiêng và tiến2px rồi thu về; Down nghiêng cả hình. Đây là chuyển động cơ bản để áp dụng mẫu ngay. Chưa có nguồn image-to-video tương thích để dựng bộ chuyển động từ master.

`SpriteAnimator` và driver hiện có phát Attack theo cue host của working tree T-50, bắt kịp cue muộn và tăng tốc theo chu kỳ thực. Di chuyển vật lý/dash vẫn hoạt động trong Attack; vì là ảnh liền khối, clip Attack tạm thay clip Walk. Không tạo runtime renderer hoặc NetworkIdentity mới; flash và afterimage dùng sprite toàn thân mới. Gục/Restart/đổi lớp dùng vòng đời hiện có.

Tre trong dáng tĩnh **không diễn được toàn bộ cung110° hoặc bán kính2.2**. VFX cung vẫn báo vùng đánh thực do host chốt; không thay thông số gameplay để khớp chiều dài cây tre trong ảnh. Cần bộ pose/clip vung tre thật nếu muốn cả tay/tre khớp hình học đầy đủ. Không dùng bản nhập ảnh này để đánh dấu T-50B/T-50/T-51 hoàn tất.

## Kiểm chứng

- [Audit tài nguyên](validation/T18I/assets.txt): PNG32 nguyên byte, 24 frame biến đổi cả ảnh, import/alpha/không Son, tre rời/cartoon tắt, ID ổn định, builder cũ giữ Forge và chỉ số không đổi.
- Đã xem [màn chọn](validation/T18I/selection.png), [trận mới](validation/T18I/game-fresh.png), [chuỗi frame](validation/T18I/frames.png), [ảnh fixture](validation/T18I/game.png). [GIF](validation/T18I/motion.gif), [CSV frame](validation/T18I/frames.csv).
- [Ba lớp](validation/T18I/classes.txt), [chuyển động/vòng đời](validation/T18I/motion-checks.txt), [combat](validation/T18I/combat.txt), [Console trong Play](validation/T18I/console.txt). Fixture điều khiển input bằng mã, địch đứng yên, tạm bất tử; không là cân bằng tự nhiên, build hoặc nghiệm thu hai máy qua Internet.

Bundle Forge gốc có một bản thử đã bỏ mang cảnh báo `canvas-fit`: value=`{"targetHeight":30.0,"fittedHeight":29.699}`, threshold=`[32,32]`, note=`the clip did not fit the canvas at the target; it was finished smaller`; lưu nguyên văn trong [QA nguồn](validation/T18I/forge-source-qa.json). Bản `pixel-final` đang được nhập không cówarn/fail số học và đã được xem ảnh; điều này không chứng nhận chất lượng mỹ thuật hoặc animation tự nhiên.

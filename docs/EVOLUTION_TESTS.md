# T-25/T-26 — Tiến hoá thẻ

Đủ nguyên liệu sẽ tự mở tiến hoá sau khi host chấp nhận thẻ. Mỗi công thức chỉ
áp dụng một lần mỗi ván; không tốn lượt chọn hay lượt đổi. Nguyên liệu giữ cấp và
hiệu ứng, phần thưởng dưới đây cộng thêm đúng một lần, không nhân đôi chỉ số nền.
Một nguyên liệu có thể dùng cho nhiều công thức; các tiến hoá cùng đủ điều kiện
được áp dụng và thông báo lần lượt. Không có nguyên liệu tiến hoá dây chuyền.

| Tiến hoá | Nguyên liệu | Phần thưởng thêm | Phạm vi |
|---|---|---|---|
| Nỏ Thần | Xuyên Tâm ×1 + Âm Vang ×3 | Đạn xuyên thêm 2 địch; +20% tầm đánh gốc. | Vũ khí đạn |
| Lửa Thiêng | Bộc Phá ×1 + Cuồng Nộ ×2 | Nổ rộng 2,5 đơn vị, gây 60% sát thương đạn lên địch khác. | Vũ khí đạn |
| Trăm Trứng | Song Tiễn ×1 + Liên Kích ×3 | Bắn 4 đạn trong góc 14°. Mỗi đạn còn 52,5% sát thương. | Vũ khí đạn |
| Thánh Gióng | Cường Công ×3 + Thiết Bích ×2 | +40% sát thương gốc; +15% thời gian bảo vệ sau trúng đòn. | Cả ba nhân vật |
| Tiếng Đàn Thần | Cường Công ×2 + Âm Vang ×3 | +25% sát thương gốc; +15% tốc độ đánh gốc. | Cả ba nhân vật |
| Lạc Phong | Bộ Pháp ×1 + Khinh Thân ×3 | Giảm thêm 15% hồi lướt gốc; +12% tốc độ di chuyển gốc. | Cả ba nhân vật |
| Bất Tử | Sinh Lực ×3 + Hồi Xuân ×2 | +20% máu gốc; hồi thêm 1 máu đầu đợt. Không hồi sinh. | Cả ba nhân vật |
| Kim Cang | Sinh Lực ×2 + Thiết Bích ×2 | +20% máu gốc; +30% thời gian bảo vệ sau trúng đòn. | Cả ba nhân vật |

## Quy ước dữ liệu

1. Tạo `CardEvolutionData` bằng `Create > LAC > Cards > Evolution Recipe` tại
   `Assets/_LAC/Data/Cards/Resources/Evolutions/`.
2. ID chuỗi ổn định, duy nhất; tên/mô tả tiếng Việt; ít nhất hai loại nguyên liệu
   tham chiếu đúng asset thẻ nền, số cấp 1…MaxStacks. Không lặp một ID nguyên liệu.
3. `Bonus` là `CardDefinition` riêng, đặt **ngoài** `Resources/Cards/`, ví dụ
   `Assets/_LAC/Data/Cards/EvolutionBonuses/`. Không dùng lại asset thẻ nền.
   ID/Weight/MaxStacks của bonus không tham gia bốc thẻ; chỉ đọc các trường hiệu ứng.
4. Bonus chỉ chứa **phần thưởng tăng thêm**. Nguyên liệu giữ chỉ số và cấp đã nhận;
   không bị trừ rồi bốc lại, không nhân đôi hiệu ứng nền. Một thẻ nền có thể đóng
   góp vào nhiều công thức. Cân bằng hiệu ứng chồng nhau là trách nhiệm T-26.
5. Tiến hoá ghi một ID riêng vào trạng thái ván, HUD hiển thị riêng bên phải.
   Công thức không tạo thêm lựa chọn hay tiêu lượt đổi thẻ. Không có tiến hoá dây
   chuyền: nguyên liệu chỉ là thẻ nền. Công thức duyệt theo ID ordinal ổn định.
6. Danh mục sai (ID trùng, nguyên liệu không tồn tại/vượt cấp, bonus nằm trong bể
   nền) bị từ chối toàn bộ và ghi lỗi Console; không chọn bản đầu tuỳ thứ tự tải.
7. Cả 8 recipe đã gán icon riêng theo [CARD_ART.md](CARD_ART.md); huy hiệu Đông Sơn
   chung chỉ còn là fallback nếu một recipe mới chưa có ảnh.

Host chỉ kiểm tra sau khi chấp nhận thẻ. Client dựng cùng kết quả từ lịch sử
định danh thẻ đã được host duyệt, không tự gửi yêu cầu tiến hoá. TargetRpc gửi
các ID tiến hoá mới cho đúng người nhận thông báo; lịch sử/replay không phát lại
thông báo. Không sửa CharacterData/CardDefinition lúc chạy.

Thông báo nối sau animation chọn thẻ, mỗi công thức 2,4 giây bằng thời gian thực,
vẫn chạy lúc gameplay pause. Host chờ ACK sau toàn bộ hàng đợi, với hạn dự phòng
tăng tương ứng; kết thúc ván/mất kết nối huỷ coroutine và ẩn thông báo.

## Chạy lại kiểm thử

- Edit Mode, danh mục thật: `LAC > Tests > Validate Eight Evolutions (Edit Mode)`.
  Kiểm đủ 8 công thức trên 3 nhân vật, ngưỡng nguyên liệu, giá trị phần thưởng theo
  `CARDS.md`, lọc vũ khí, dựng lại lịch sử đảo thứ tự, tiến hoá đồng thời và reset.
  Bộ này kiểm asset thực tế, không cài fixture.
- Host Play Mode, danh mục thật: `LAC > Tests > Check Eight Evolutions Play Mode (restarts run)`.
  Tạo nguyên liệu đầu trong lịch sử host rồi chọn nguyên liệu cuối qua UI thật;
  kiểm cả 8 thông báo, nhấp đôi, ACK, chỉ số và máu tối đa thực tế của người chơi.
- Co-op, danh mục thật: build lại `LAC > Tests > Build Card Network Client`,
  mở `Builds/CardNetworkTests/CardClient.exe --lac-card-test-client`, chờ host có
  hai người rồi chạy `Check Eight Evolutions Play Mode`. **Không** thêm
  `--lac-evolution-test-client` khi kiểm danh mục thật. Đối chiếu từng chữ ký
  `[EvolutionCatalog] PLAY PASS` ở host với `[EvolutionClient] wave=2` của cả hai
  người trong log client; chữ ký gồm ID tiến hoá và toàn bộ chỉ số nâng cấp.
- Edit Mode: `LAC > Tests > Validate Evolution Rules`.
- Host Play Mode trong Arena: `LAC > Tests > Check Evolution Play Mode (restarts run)`.
  Kiểm thử chủ động restart ván, buộc chọn Cường Công rồi Liên Kích, kích hoạt
  đồng thời hai công thức thử; kiểm tra nhấp đôi, thông báo khi pause, chờ ACK và reset.
- Hồi quy: `LAC > Demo > Check Card Resume (Play Mode - restarts run)` và
  `LAC > Tests > Validate 12 Card Balance` khi không cài fixture.
- Hai tiến trình: Stop Play, `LAC > Tests > Build Card Network Client`, chạy lại host
  Editor rồi mở `Builds/CardNetworkTests/CardClient.exe` với cả hai tham số
  `--lac-card-test-client --lac-evolution-test-client`. Chạy menu kiểm thử tiến hoá
  khi `PlayerRegistry.Count == 2`. Client thử đổi thẻ/đợi hết giờ theo bộ test mạng.
  Log client phải có wave=3, evolutions=2, damage=1.3 cho cả hai người.

Không dùng các fixture để đánh giá cân bằng T-26. Không lưu scene trong Play Mode.

## Kết quả nghiệm thu 30/09–01/10/2026

- Edit Mode: `EvolutionRules` đạt kiểm tra nguyên liệu thiếu/trùng/vượt cấp, hai
  tiến hoá đồng thời, mỗi người một trạng thái, replay, reset và asset bất biến.
- Danh mục thật: 8 công thức × 3 nhân vật đạt kiểm tra ngưỡng, lọc vũ khí, phần
  thưởng theo giá trị trong `CARDS.md`, replay đảo thứ tự, cộng thưởng một lần.
- Hồi quy T-24: 45.000 lượt chọn thẻ nền đạt; giữ nguyên bể 12 thẻ và giới hạn cấp.
- Host một người (30/09): 8/8 công thức đi qua lựa chọn thật, nhấp đôi, thông báo,
  ACK, phục hồi input và chơi lại. Đã nhìn ảnh Game View thông báo Kim Cang.
- Co-op hai tiến trình (01/10): Editor host + development client, KCP bọc
  LatencySimulation 100 ms. 8/8 công thức đạt, gồm máu tối đa thực tế trên host;
  đối chiếu đủ 16 chữ ký (8 công thức × 2 người) trong client: **0 sai lệch**.
  Log cục bộ: `Logs/Editor.log`, `Logs/EvolutionClient-Coop-20261001.log`.
- Hai tiến hoá đồng thời (01/10): hai tiến trình đạt nhấp đôi, hàng đợi thông báo,
  chờ ACK, tự chọn sau 10 giây và reset. Client ghi đủ hai người tại đợt 3 với
  `evolutions=2`, `damage=1.3`, `timeScale=1`; log
  `Logs/EvolutionFixture-Final-20261001.log`. Đã nhìn ảnh Game View thông báo
  thứ hai; Console cuối lượt kiểm thử: **0 lỗi, 0 cảnh báo**.
- Bộ test đổi nhân vật **trước** khi restart để khởi tạo đúng máu gốc; khi kết
  thúc cũng khôi phục nhân vật trước khi reset ván. Không sửa asset nhân vật.

Kiểm thử dùng nguyên liệu được chuẩn bị trong bộ nhớ để tới đúng ngưỡng; nguyên
liệu cuối vẫn đi qua đề nghị, thao tác chọn và xác nhận mạng thật. Đây chưa phải
kiểm thử cân bằng cả 16 đợt, hiệu năng T-32 hay kết nối Steam T-40.

## Hình dùng trong T-25

Skill `imagegen`, công cụ tạo ảnh tích hợp, tạo ảnh mới ngày 25/09/2026.
Asset: `Assets/_LAC/Art/Sprites/UI/Cards/Resources/EvolutionEmblem.png`.
Nhập Sprite Single, Point, không mipmap, giới hạn texture 512; icon dùng trong
thông báo, không phải hiệu ứng chiến đấu. Chữ tiếng Việt do UI vẽ, không nằm trong ảnh.

Prompt đã dùng:

> Use case: stylized-concept. Asset type: production game UI evolution emblem for LẠC, Vietnamese mythology pixel art arena survival. Create ONE square icon: two small bronze talisman cards merging into a central circular Dong Son bronze drum sun emblem, stylized Lac bird motifs around the rim, clear chunky silhouette. Pixel art with crisp stepped square pixel edges, restrained ornament, looks authored at 128x128, no soft gradients. Centered with 12 percent empty margins. Flat dark charcoal background #15130F. Palette restricted to #FBDD82 #EDBB3E #C08D20 #8A5F14 #9CCFC0 #4FA694 #2F7480 #1C4B5C #112E3E #F4EADA #2B2724 #15130F. NO red orange pink, no text, no lettering, no watermark, no modern items. Readable at 96 pixels. This is a UI badge not a whole screen.

# Rà soát và tối ưu Cards — 02/10/2026

Phạm vi theo xác nhận của người dùng: chỉ mã và dữ liệu Cards. Không đổi nhân vật,
đợt quái, VFX chiến đấu hoặc kiến trúc mạng; báo cáo phần còn lại ở
[BALANCE_AUDIT.md](BALANCE_AUDIT.md). T-32/T-50/T-51 vẫn chưa hoàn tất.

## Thay đổi

- `CardSelectionController`: chỉ tạo chuỗi/ghi bộ đếm khi số giây thay đổi; hết giờ
  khoá nút một lần. Reset cache khi mở đề nghị mới (kể cả đổi cùng giây) và đóng bảng.
  Vẫn dùng thời hạn host và `NetworkTime.time`, không đổi luật 10 giây/ACK.
- `CardHoverVisual`: lúc nghỉ không ghi transform hoặc màu. Chỉ animate alpha khi
  dim, animate transform khi consume, đổi màu khi trạng thái tương tác thay đổi.
  Cache camera/parent cho animation, dừng cập nhật consume sau 1,05 giây.
- `CardSelectionView`: chỉ overlay chặn nền, ba gốc thẻ và nút đổi nhận raycast;
  ảnh/chữ/trang trí không tham gia. Giữ chữ 22 px tại 720p, khung đồng, thẻ đứng yên
  và vệt lóe chỉ trên viền; không thêm hiệu ứng làm nhòe chữ.
- Cuồng Nộ giữ +25% sát thương nhưng phạt tốc đánh −8% thay vì −10% mỗi cấp;
  sửa asset, mô tả, generator và các giá trị nghiệm thu cùng lúc.

## Cân bằng được kiểm tra

Build Cường Công ×3 + Thiết Bích ×2 mở Thánh Gióng. Với dữ liệu cũ, Cuồng Nộ
cấp 1→2 làm DPS 2,025→2,000×, giảm 1,23%. Với dữ liệu mới: 2,070→2,100×.

`CardBalanceChecks` thử 1.152 trạng thái thật: mọi cấp Cường Công 0–3, Thiết Bích
0–2, Âm Vang 0–3, Liên Kích 0–3, Cuồng Nộ trước lựa chọn 0–1, trên ba nhân vật;
dùng cả tám công thức thật. Các thẻ còn lại không đổi hệ số damage/tốc đánh.
Mọi cấp Cuồng Nộ tiếp theo tăng DPS, nhỏ nhất +0,030× DPS gốc.

Trần DPS **chỉ thẻ nền** đổi 2,625→2,709×; thêm Song Tiễn khi cả hai viên trúng:
3,675→3,7926× (+3,2%). Không đổi sức mạnh những thẻ/tiến hoá khác.
Đây là chỉ số giải tích, không đo sát thương thực theo thời gian/tỉ lệ thắng.

## Chạy lại

1. Thoát Play: `LAC/Tests/Validate 12 Card Balance (Edit Mode)` — 45.000 lựa chọn,
   cộng dồn/lọc vũ khí/reset/bảo toàn asset; thêm dòng `CuongNo marginal PASS`.
2. `LAC/Tests/Validate Eight Evolutions (Edit Mode)` — 8 công thức × 3 nhân vật.
3. Arena Mirror host một người: `LAC/Demo/Check Card Resume` — 12 thẻ, pause,
   nhấp đôi, input/physics, hit-stop, reset; phải có `ALL PASSED`.
   Bài bộ đếm kiểm 10.000 update giữ cùng đối tượng chuỗi, mở lại đề nghị trong
   cùng giây, khoá nút quá hạn và khôi phục sang đợt mới (`COUNTDOWN PASS`).
4. Đặt Game View 1280×720 và 1920×1080, chạy `Validate Card Typography` — đủ
   12 thẻ/8 tiến hoá, font tiếng Việt, không tràn và tương phản ≥7:1.
   Kiểm UI bổ sung: đúng 5 graphic nhận raycast; bấm tâm icon tới gốc thẻ;
   hover/focus không xoá nhầm nhau; 10.000 idle update không ghi transform/màu;
   dim, consume endpoint và reset. Đóng preview để trả lại timeScale.

Không diễn giải các kiểm tra UI cô lập thành benchmark FPS toàn game hoặc GC=0 toàn
khung. Kiểm thử hai tiến trình co-op chưa được chạy lại trong lượt này; fixture
fast-forward lịch quái cần được rà soát trước khi dùng để kết luận đồng bộ gameplay.

## Kết quả lượt này

- Edit Mode: 45.000 lượt bốc, 1.152 marginal Cuồng Nộ và 8 công thức × 3 nhân vật PASS.
- Mirror host: 12/12 thẻ và kiểm bộ đếm PASS; tám tiến hoá 8/8 PASS với đề nghị
  thật, nhấp đôi, UI, ACK và reset. Lửa Thiêng nhận đúng tốc đánh 0,84× mới.
- UI/typography: PASS 720p/1080p, mô tả 22/33 px; đã nhìn Game View và lưu ảnh dưới.
- Không lưu scene/prefab hoặc sửa dữ liệu ngoài Cards; không chạy lại co-op hai tiến trình.
- Console ở cuối kiểm gameplay/typography: 0 lỗi đỏ, 0 cảnh báo. Lỗi MCP kết nối
  lúc khởi động Editor và khoá file sinh skill ở lượt refresh trước đã được tách
  khỏi lượt kiểm cuối; không ghi nhận lại trong lượt này.
- Sau Stop: 0 lỗi đỏ, một cảnh báo MCP `BufferedFileLogStorage` đã dispose; scene không
  dirty, Game View được trả về cấu hình ban đầu. Không tính cảnh báo công cụ này
  là kết quả gameplay. API đo allocation theo thread trả 0 cả khi cố cấp phát 1 MB
  trên runtime này, nên không dùng nó để tuyên bố GC=0.

![Cards 720p](screenshots/card-optimized-720p.png)

![Cards 1080p](screenshots/card-optimized-1080p.png)

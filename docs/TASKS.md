# Kế hoạch công việc — LẠC

Tài liệu này là căn cứ duy nhất về tình trạng công việc của dự án: phân công, tiến độ và mô tả các chức năng đã hoàn thành.

## Quy trình sử dụng

**Bước 1 — Nhận hạng mục.** Thay `Chưa phân công` bằng tên thành viên và đẩy lên remote ngay, tránh trùng lặp công việc.

**Bước 2 — Ghi nhận hoàn thành.** Chuyển `[ ]` thành `[x]` (đặt con trỏ tại dòng, nhấn `Alt+C`), bổ sung tên và ngày, sau đó viết **một dòng trích dẫn `>` ngay bên dưới** mô tả chức năng và vị trí tệp.

**Bước 3 — Commit.** Định dạng: `feat(T-05): dash có i-frame và thời gian hồi`

```markdown
Trước khi thực hiện:
- [ ] **T-05** Dash — i-frame, thời gian hồi, vệt mờ — Chưa phân công

Sau khi hoàn thành:
- [x] **T-05** Dash — i-frame, thời gian hồi, vệt mờ — @Kiet · 27/08
  > Dash 6 đơn vị trong 0.15 s, bất tử toàn bộ thời gian dash, hồi 0.4 s.
  > `Player/PlayerDash.cs`. Gọi `TryDash()` từ `PlayerController`.
```

Dòng trích dẫn là cơ sở để thành viên khác và công cụ AI xác định chức năng đã tồn tại, tránh triển khai trùng lặp. **Hạng mục thiếu dòng này không được tính là hoàn thành.**

**Thành viên:** `@Kiet` · `@Hung` · `@Kang` · `@artist`

---

# Cổng 1 — Tuần 1 đến 3

**Tiêu chí nghiệm thu:** hai người chơi cùng hoàn tất một ván qua mạng; phản hồi chiến đấu đạt yêu cầu về độ đã tay.

### Chuẩn bị môi trường

- [ ] **T-01** Cả ba máy clone repository và mở dự án bằng Unity 6000.5.6f1 không phát sinh lỗi — Chưa phân công
- [ ] **T-02** Cài đặt Mirror (Asset Store, miễn phí) và dùng `KcpTransport` — Chưa phân công

> **Steamworks.NET và FizzySteamworks đã dời sang T-40 ở tuần 10.** Mirror kèm `KcpTransport` đáp ứng đầy đủ Cổng 1 và Cổng 2, bao gồm giả lập độ trễ. Cài Steamworks sớm buộc mỗi lần chạy thử phải mở Steam và cần App ID — thứ chỉ có sau khi hoàn tất T-46B.

### Hạ tầng nền tảng

- [x] **T-03** `RunRandom` — bộ sinh số ngẫu nhiên có seed, nguồn ngẫu nhiên duy nhất của gameplay — @Kiet · 28/08
  > Bộ sinh Xorshift128 tự cài đặt, **không dùng `System.Random`** vì thuật toán nội bộ của nó thay đổi giữa các phiên bản .NET và không bảo đảm giống nhau trên mọi máy.
  > Ngẫu nhiên chia thành bốn kênh độc lập — `Enemies`, `Cards`, `Loot`, `Director` — để một hệ thống rút thêm hoặc thiếu một lần không làm lệch pha các hệ thống còn lại và gây phân kỳ host/client.
  > `Core/RunRandom.cs` (mặt tiền tĩnh, `Initialize(seed)` · `CreateSeed()` · `Reset()`), `Core/RandomStream.cs` (`NextUInt` · `NextFloat` · `Range` · `Chance` · `Pick` · `Shuffle`).
  > Đã kiểm chứng: cùng seed cho cùng dãy số, hai kênh khác nhau cho dãy khác nhau, 20.000 mẫu có trung bình 0.5021, `Shuffle` bảo toàn tập phần tử.
- [x] **T-04** `ObjectPool` — pool dùng chung cho đạn, quái và hiệu ứng — @Kiet · 28/08
  > `ObjectPool<T>` với `Get` · `Release` · `ReleaseAll` · `Clear`, hỗ trợ prewarm và cảnh báo khi phải cấp phát thêm giữa trận. Phát hiện và chặn lỗi trả về pool hai lần.
  > `PoolRegistry` tra cứu pool theo prefab qua bảng băm, loại bỏ nhu cầu gọi `FindObjectOfType` trong vòng lặp gameplay.
  > `IPoolable` với `OnSpawned` và `OnDespawned` — bắt buộc cho mọi đối tượng có trạng thái riêng theo lần sử dụng, vì đối tượng lấy từ pool giữ nguyên giá trị của lần trước.
  > `Core/ObjectPool.cs`, `Core/PoolRegistry.cs`, `Core/IPoolable.cs`.
- [ ] **T-05** `RunManager` — vòng đời một ván: khởi tạo, 16 đợt, điều kiện thắng thua — Chưa phân công
- [ ] **T-06** `CharacterData` (ScriptableObject) kèm asset cấu hình Thạch Sanh — Chưa phân công

### Kiến trúc mạng — triển khai tại tuần 1

- [ ] **T-07** `NetworkManagerLAC` vận hành ở host mode kể cả khi chơi đơn — Chưa phân công
- [ ] **T-08** Kích hoạt giả lập độ trễ 100 ms làm cấu hình mặc định khi phát triển — Chưa phân công
- [ ] **T-09** Sinh 1–2 nhân vật; hai máy cùng vào được một ván — Chưa phân công

### Cơ chế chiến đấu

- [ ] **T-10** Di chuyển 8 hướng, hỗ trợ bàn phím và tay cầm — Chưa phân công
- [ ] **T-11** Dash — i-frame, thời gian hồi, vệt mờ — Chưa phân công
- [ ] **T-12** Vũ khí khai hoả tự động — chu kỳ bắn, chọn mục tiêu gần nhất — Chưa phân công
- [ ] **T-13** `DamageSystem` — điểm vào duy nhất cho mọi sát thương, thẩm quyền thuộc host — Chưa phân công
- [ ] **T-14** Quái vật đầu tiên (Cô Hồn) — hành vi truy đuổi và trạng thái chết — Chưa phân công
- [ ] **T-15** Phản hồi khi đánh trúng — hit-stop, nháy trắng, đẩy lùi, số sát thương, rung màn — Chưa phân công
- [ ] **T-16** **Sóng âm Đông Sơn** — vòng tròn đồng tâm lan toả mang hoa văn trống đồng — Chưa phân công

### Mỹ thuật

- [ ] **T-17** Chốt bảng 24 màu Đông Hồ, **dành riêng một màu cho đòn tấn công của địch** — Chưa phân công
- [ ] **T-18** Sprite Thạch Sanh, sprite Cô Hồn, tileset Sân Đình — Chưa phân công

### Quảng bá — khởi động tại tuần 2, không phát sinh chi phí

- [ ] **T-19** Chuẩn bị bộ tài sản trang cửa hàng: mô tả sản phẩm, ảnh chụp màn hình, ảnh bìa, GIF sóng âm — lưu tại `docs/store/` — Chưa phân công
- [ ] **T-20** Thiết lập kênh TikTok, xuất bản devlog số 1, lập lịch đăng hàng tuần — Chưa phân công

> **Đăng ký Steam Direct đã dời sang tuần 10 — hạng mục T-46B.** Khoản phí $100 chỉ chi khi sản phẩm đã có bản chạy được, nhằm loại bỏ rủi ro tài chính ở giai đoạn đầu. Toàn bộ hoạt động quảng bá không phát sinh chi phí vẫn khởi động từ tuần 2 vì lượng wishlist tích luỹ theo thời gian.

---

# Cổng 2 — Tuần 4 đến 7

**Tiêu chí nghiệm thu:** ba nhân vật cho ba trải nghiệm phân biệt rõ ràng, và sản phẩm đã đủ điều kiện quay một clip 15 giây phục vụ quảng bá.

- [ ] **T-21** Hệ thống thẻ nâng cấp — `CardData` và cơ chế áp hiệu ứng lên chỉ số — Chưa phân công
- [ ] **T-22** Giao diện chọn 1 trong 3 thẻ — giới hạn 10 giây, 2 lượt đổi thẻ — Chưa phân công
- [ ] **T-23** **Đồng bộ lựa chọn thẻ:** đợt kế tiếp chỉ khởi động khi cả hai người chơi đã chọn xong — Chưa phân công
- [ ] **T-24** Biên soạn 32 thẻ nền — Chưa phân công
- [ ] **T-25** **Hệ thống tiến hoá thẻ** — bộ kiểm tra công thức và giao diện thông báo — Chưa phân công
- [ ] **T-26** Chốt và triển khai 8 công thức tiến hoá — Chưa phân công
- [ ] **T-27** **Cơ chế Hồn** — rơi ra khi quái chết, tự hút về, âm thanh tăng dần cao độ — Chưa phân công
- [ ] **T-28** Nhân vật Gióng — roi sắt, đòn đánh hình cung — Chưa phân công
- [ ] **T-29** Nhân vật Tấm — sáo trúc, đòn tia. **Hiệu ứng tăng sát thương áp cho phát bắn kế tiếp**, không theo cửa sổ thời gian — Chưa phân công
- [ ] **T-30** Màn chọn nhân vật và đồng bộ lựa chọn qua mạng — Chưa phân công
- [ ] **T-31** Sóng âm riêng biệt cho từng nhạc cụ — Chưa phân công
- [ ] **T-32** Kiểm thử hiệu năng và đọc hiểu: 60 FPS với 40 quái và 200 đạn, đòn địch vẫn phân biệt được — Chưa phân công
- [ ] **T-33** Sprite Gióng, sprite Tấm, 40 icon thẻ — Chưa phân công

---

# Cổng 3 — Tuần 8 đến 12

**Tiêu chí nghiệm thu:** co-op qua Steam hoạt động với người chơi ngoài mạng LAN, hoàn tất 16 đợt không phát sinh sai lệch trạng thái.

- [ ] **T-34** Bốn quái vật còn lại — Ma Trơi, Bù Nhìn, Ma Da, Quỷ Nhỏ — Chưa phân công
- [ ] **T-35** Snapshot vị trí quái 2 lần/giây để hiệu chỉnh sai lệch — Chưa phân công
- [ ] **T-36** **Trống Đồng** — kích hoạt bằng dash, xoá đạn, đẩy lùi, gây choáng 1 giây — Chưa phân công
- [ ] **T-37** **Thời gian hồi Trống Đồng dùng chung**, trạng thái do host quản lý — Chưa phân công
- [ ] **T-38** Hồn nạp năng lượng cho Trống Đồng, hiển thị vòng nạp trên HUD — Chưa phân công
- [ ] **T-39** Trùm Chằn Tinh — hai pha, lượng máu tỉ lệ theo số người chơi — Chưa phân công
- [ ] **T-40** Mời bạn qua Steam overlay, chuyển transport sang FizzySteamworks — Chưa phân công
- [ ] **T-41** Cơ chế hạ gục và hồi sinh — đồng đội đứng cạnh 3 giây — Chưa phân công
- [ ] **T-42** Xử lý mất kết nối: client rớt mạng và host thoát ván, không treo ứng dụng — Chưa phân công
- [ ] **T-43** Thu thập telemetry ra CSV phục vụ phần đánh giá của khoá luận — Chưa phân công
- [ ] **T-44** Bảng đợt cố định — dùng cho nhóm đối chứng và làm phương án dự phòng — Chưa phân công
- [ ] **T-45** **AI Đạo Diễn** (LinUCB) — lõi thuật toán, `ContextVector`, `WaveSpec` — Chưa phân công
- [ ] **T-45B** **Đạo diễn hoạt động trong co-op** — `ContextAggregator` hợp thành ngữ cảnh N người chơi, số hạng công bằng trong hàm thưởng, tầng an toàn đánh giá theo người yếu nhất — Chưa phân công
- [ ] **T-46** Điều tiết bất đối xứng, đòn bẩy *chia cắt* và *dồn ép* theo hướng sinh quái, **hiển thị hoạt động của đạo diễn trên HUD** — Chưa phân công
- [ ] **T-46B** Đăng ký Steam Direct ($100), hoàn tất tax interview và xác minh tài khoản, dựng trang cửa hàng — **thực hiện tại tuần 10** — Chưa phân công
- [ ] **T-47** Sprite bốn quái, sprite Chằn Tinh, sprite trống đồng, hai tileset còn lại — Chưa phân công

---

# Cổng 4 — Tuần 13 đến 16

**Tiêu chí nghiệm thu:** demo miễn phí đã phát hành trên Steam; hoàn tất bảo vệ khoá luận.

- [ ] **T-48** Hệ thống tiền tệ Ngọc, lưu tiến trình, bảng mở khoá — Chưa phân công
- [ ] **T-49** Màn thống kê sau ván — *cấp độ khó thứ hai chuyển sang giai đoạn sau bảo vệ, lấy quỹ thời gian cho T-45B* — Chưa phân công
- [ ] **T-50** **Cân bằng: xác định sát thương gốc cho cả ba vũ khí** — *GDD hiện chưa có thông số này* — Chưa phân công
- [ ] **T-51** Cân bằng đường cong độ khó qua 16 đợt — Chưa phân công
- [ ] **T-52** Nhạc nền và toàn bộ hiệu ứng âm thanh — Chưa phân công
- [ ] **T-53** **Thực nghiệm đánh giá: 15 người dùng AI Đạo Diễn, 15 người dùng bảng đợt cố định**, chế độ chơi đơn. Thu thập thêm mẫu quan sát định tính ở chế độ co-op — Chưa phân công
- [ ] **T-54** Phân tích số liệu và biên soạn chương đánh giá — Chưa phân công
- [ ] **T-55** Đóng gói demo miễn phí — 8 đợt đầu, một nhân vật — Chưa phân công
- [ ] **T-56** Dựng trailer, sử dụng sóng âm Đông Sơn cho 5 giây mở đầu — Chưa phân công
- [ ] **T-57** Phát hành demo và đăng ký Steam Next Fest — Chưa phân công
- [ ] **T-58** Thiết lập giá: $2.99, khu vực Việt Nam 29.000–39.000₫ — *ghi đè mức Steam đề xuất* — Chưa phân công
- [ ] **T-59** Chuẩn bị slide và bản demo phục vụ buổi bảo vệ — Chưa phân công

> **Không mở bán tại tuần 16.** Phạm vi của mốc này là phát hành demo và hoàn tất bảo vệ. Ngày phát hành chính thức chỉ được ấn định khi lượng wishlist đạt khoảng 8.000 — thời điểm ra mắt không thể thực hiện lại lần thứ hai.

---

## Hạng mục dự phòng — triển khai nếu còn quỹ thời gian

Thử thách hằng ngày kèm bảng xếp hạng Steam · nhân vật thứ tư · trùm thứ hai · cấp độ khó thứ hai · thực nghiệm định lượng cho chế độ co-op

## Hạng mục ngoài phạm vi — đã chốt không triển khai

Co-op 4 người · matchmaking · cửa hàng vật phẩm trang trí · bản mobile · cắt cảnh · nhánh rẽ Núi/Biển

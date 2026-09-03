# Kế hoạch công việc — LẠC

Căn cứ duy nhất về tình trạng công việc: phân công, tiến độ, và **danh sách những gì đã tồn tại**.

Quy trình làm việc hằng ngày và cách tránh xung đột: [docs/WORKFLOW.md](WORKFLOW.md).
Ràng buộc kiến trúc: [CLAUDE.md](../CLAUDE.md).

---

## Phân công

| Thành viên | Mảng phụ trách | Thư mục sở hữu |
|---|---|---|
| **@Kiet** | Vòng lặp lõi: chiến đấu, quái, mạng, đạo diễn | `Scripts/Core` `Combat` `Enemies` `Player` `Net` `VFX` · `Scenes/Arena.unity` |
| **@Hung** | Màn hình vào game: menu chính, cài đặt, tạo và vào phòng, tạm dừng | `Scripts/Menu` · `Scenes/Boot.unity` · `Prefabs/UI/Menu` |
| **@Kang** | Hệ thống thẻ nâng cấp và giao diện thẻ | `Scripts/Cards` · `Data/Cards` · `Prefabs/UI/Cards` |
| **@artist** | Sprite, tileset, icon | `Art/Sprites` `Art/Palettes` |

> Tên là phân công hiện tại, đổi được. Nguyên tắc không đổi: **mỗi thư mục có đúng một người chịu trách nhiệm.** Cần sửa file ngoài thư mục của mình thì hỏi người sở hữu trước.

## Cách ghi một hạng mục

1. **Nhận việc** — thay `Chưa phân công` bằng tên mình, đẩy lên remote ngay để người khác biết.
2. **Hoàn thành** — đổi `[ ]` thành `[x]` (`Alt+C`), thêm tên và ngày, rồi viết **một dòng `>` bên dưới** ghi rõ *chức năng làm được gì* và *nằm ở file nào*.
3. **Commit** — `feat(T-21): CardData và cơ chế áp hiệu ứng`

Dòng `>` là cách người khác và công cụ AI biết chức năng đã tồn tại, tránh làm trùng. **Thiếu dòng này thì hạng mục chưa được tính là xong.** Lý do đằng sau mỗi quyết định nằm trong commit message, không nhồi vào đây.

---

# Cổng 1 — Tuần 1–3 · Đã hoàn tất

**Nghiệm thu:** hai người chơi cùng hoàn tất một ván qua mạng; phản hồi chiến đấu đủ đã tay.

### Môi trường và hạ tầng

- [ ] **T-01** Cả ba máy clone repository và mở bằng Unity 6000.5.6f1 không lỗi — **@Hung @Kang**
- [x] **T-02** Mirror 96.0.1 kèm `KcpTransport` — @Kiet · 29/08
  > Commit thẳng vào repo tại `Assets/Mirror/` để ba máy chắc chắn dùng cùng một phiên bản. Steamworks dời sang T-40.
- [x] **T-03** `RunRandom` — nguồn ngẫu nhiên duy nhất của gameplay — @Kiet · 28/08
  > Xorshift128 tự cài, bốn kênh độc lập (Enemies · Cards · Loot · Director) để hệ thống này rút thêm không làm lệch hệ thống kia. `Core/RunRandom.cs` · `Core/RandomStream.cs`.
- [x] **T-04** `ObjectPool` dùng chung cho đạn, quái, hiệu ứng — @Kiet · 28/08
  > `Get` · `Release` · `ReleaseAll`, có prewarm và cảnh báo khi phải cấp phát giữa trận. `Core/ObjectPool.cs` · `PoolRegistry.cs` · `IPoolable.cs`.
- [x] **T-05** `RunManager` — vòng đời một ván — @Kiet · 29/08
  > Host giữ thẩm quyền. SyncVar: seed, đợt hiện tại, trạng thái. Nhận báo cáo qua `ReportWaveCleared` · `ReportCardSelectionComplete` · `ReportPlayerDown`; phát `WaveStarted` · `WaveCleared` · `RunEnded`. `Core/RunManager.cs` · `RunState.cs`.
- [x] **T-05B** Đóng vòng lặp ván — kết thúc, màn hình kết quả, chơi lại tại chỗ — @Kiet · 03/09
  > `RestartRun` và `CmdRequestRestart` (client xin, host thi hành). Chơi lại **không nạp lại scene** để co-op không bị ngắt. `Core/RunManager.cs` · `UI/RunEndScreen.cs` · chữ pixel `Art/Sprites/UI_*.png`.
- [x] **T-06** `CharacterData` và asset ba nhân vật — @Kiet · 29/08
  > Mọi chỉ số nhân vật và vũ khí nằm trong ScriptableObject. `Player/CharacterData.cs` · `Data/Characters/`.

### Mạng

- [x] **T-07** `NetworkManagerLAC` luôn chạy host mode, kể cả chơi đơn — @Kiet · 29/08
  > Không có nhánh mã riêng cho chơi đơn. Gán nhân vật trước khi sinh để chỉ số có mặt trong gói trạng thái đầu tiên. `Net/NetworkManagerLAC.cs`.
- [x] **T-08** Giả lập độ trễ 100 ms làm mặc định khi phát triển — @Kiet · 29/08
  > `LatencySimulation` tự chọn trong Editor và development build, transport thật khi phát hành.
- [x] **T-09** Sinh 1–2 nhân vật, hai máy cùng vào một ván — @Kiet · 30/08

### Chiến đấu

- [x] **T-10** Di chuyển 8 hướng, bàn phím và tay cầm — @Kiet · 30/08
  > `Player/PlayerMovement.cs` · `PlayerInputReader.cs` · `Data/Input/LACControls.inputactions`. Client tự chạy vật lý nhân vật của mình.
- [x] **T-10B** Đấu trường — nền lát, biên va chạm — @Kiet · 30/08
  > `Core/ArenaBounds.cs` · tilemap trong `Scenes/Arena.unity` · `Data/Tiles/`.
- [x] **T-11** Dash — i-frame, thời gian hồi, vệt mờ — @Kiet · 30/08
  > Client lướt cục bộ ngay, đồng thời gửi `CmdDash` để host mở cửa sổ bất tử của nó. `Player/PlayerDash.cs` · `VFX/DashAfterimage.cs`.
- [x] **T-12** Vũ khí khai hoả tự động, chọn mục tiêu gần nhất — @Kiet · 30/08
  > Ba hình dạng: vòng tròn, hình cung, tia. Phát sự kiện `Fired` cho hoạt ảnh. `Combat/WeaponAuto.cs` · `WeaponShape.cs`.
- [x] **T-13** `DamageSystem` — điểm vào duy nhất cho mọi sát thương — @Kiet · 30/08
  > Chỉ có hiệu lực trên host, tự bỏ qua ở client nên không cần lệnh rẽ nhánh. `Combat/DamageSystem.cs`.
- [x] **T-14** Quái Cô Hồn — truy đuổi, giãn cách, trạng thái chết — @Kiet · 30/08
  > Hai máy tự sinh cùng đàn quái từ seed chung; host gửi kết quả chết qua RPC. `Enemies/Enemy.cs` · `EnemyData.cs` · `EnemySpawner.cs` · `EnemyRegistry.cs`.
- [x] **T-14B** `WaveManager` — sinh quái theo đợt, kết thúc đợt — @Kiet · 30/08
  > Mỗi đợt một luồng ngẫu nhiên riêng gieo từ seed + số đợt, để người vào giữa ván tính ra cùng kết quả. `Core/WaveManager.cs`.
  > **Còn một chỗ giữ tạm:** cờ `_autoAdvanceCardSelection` tự sang đợt kế sau 1.5 giây. **Tắt khi T-22 và T-23 xong.**
- [x] **T-15** Phản hồi khi đánh trúng — hit-stop, nháy sáng, đẩy lùi, số sát thương, rung màn — @Kiet · 30/08
  > Gom về một chỗ để điều tiết theo mức độ: đánh thường chỉ nháy, quái chết mới dừng hình. `VFX/HitFeedback.cs` · `SpriteFlash.cs` · `HitStop.cs` · `DamageNumber.cs` · `PixelNumber.cs`.
- [x] **T-15B** HUD máu — @Kiet · 30/08
  > Ô rời chứ không phải thanh liền, để người chơi đếm được còn chịu mấy đòn. `UI/PlayerHud.cs`.
- [x] **T-15C** Sửa ba lỗi khi chơi thử — @Kiet · 03/09
  > Nháy trúng đòn (`SpriteRenderer.color` là hệ số nhân, truyền trắng là nhân với 1 nên vô hiệu từ T-15); quái vây xác người chơi (`PlayerRegistry.Nearest` chưa lọc người đã gục); tốc độ quái bằng Gióng nên không thoát được vòng vây — hạ quái xuống 2.2, nâng tầm Gióng lên 3.2.
- [x] **T-16** Sóng âm Đông Sơn — @Kiet · 30/08
  > Ba vòng đồng tâm lệch pha, 24 vạch nan hoa, shader additive tự viết cho URP. `VFX/SoundWave.cs` · `Art/Shaders/SpriteAdditive.shader`.

### Mỹ thuật

- [x] **T-17** Chốt bảng 24 màu Đông Hồ, dành riêng nhóm son cho đòn địch — @Kiet · 03/09
  > Đặc tả và số đo tương phản: [docs/PALETTE.md](PALETTE.md). `Art/Palettes/DongHo24.asset` (mã đọc được) · `.gpl` cho Aseprite · `Utils/PaletteData.cs`.
- [x] **T-18** Sprite Thạch Sanh, Cô Hồn, tileset Sân Đình — @Kiet · 03/09
  > Mật độ chốt ở 32 px, PPU 32 — một ô lát bằng 1 đơn vị. `Art/Sprites/` · trình sinh `docs/tools/make_art.py`. **Chất lượng là bản đầu, hoạ sĩ tinh lại.**
- [x] **T-18B** Hệ thống hoạt ảnh nhân vật — @Kiet · 03/09
  > Trình chạy sprite tự viết thay cho Animator của Unity: `.controller` là YAML không merge được và không phải ScriptableObject. `VFX/SpriteAnimationSet.cs` · `SpriteAnimator.cs` · `Player/PlayerAnimatorDriver.cs`.
- [x] **T-18C** Hoạt ảnh cho quái — @Kiet · 03/09
  > Dùng lại nguyên hệ thống T-18B, chỉ nối dây. `EnemyData._animationSet` · `Enemy._animator`.

> **Cấu hình đang chạy là cấu hình thử nghiệm:** người chơi là Gióng với sprite Soldier, quái dùng sprite Orc, cả hai lấy từ `Assets/ThirdParty/` — xem [docs/ASSETS_ThirdParty.md](ASSETS_ThirdParty.md). Mỹ thuật thật của T-18 nằm sẵn ở `Data/Animations/`, đổi lại chỉ là hai trường dữ liệu.

---

# Cổng 2 — Tuần 4–7

**Nghiệm thu:** vào được game từ menu, ba nhân vật cho ba lối chơi khác nhau, hệ thống thẻ hoạt động trong co-op.

### Màn hình và luồng vào game — @Hung

- [ ] **T-60** Scene `Boot.unity` và menu chính: Chơi · Chơi cùng bạn · Cài đặt · Thoát — **@Hung**
- [ ] **T-61** Màn hình cài đặt: âm lượng, độ phân giải, gán lại phím; lưu bằng `PlayerPrefs` — **@Hung**
- [ ] **T-62** Luồng vào ván: tạo phòng, tham gia bằng địa chỉ, chuyển sang `Arena.unity` — **@Hung**
  > Bắt buộc đi qua `NetworkManagerLAC`. **Chơi đơn cũng phải `StartHost`**, không được có nhánh riêng — CLAUDE.md mục 3.1.
- [ ] **T-63** Tạm dừng trong ván: tiếp tục · cài đặt · thoát về menu — **@Hung**
  > Trong co-op, tạm dừng **không** được dừng thời gian của cả hai máy; chỉ mở giao diện tại máy đó.
- [ ] **T-30** Màn chọn nhân vật, đồng bộ lựa chọn qua mạng — **@Hung**
  > Chỉ đồng bộ định danh nhân vật, không đồng bộ chỉ số — mục 3.2.

### Hệ thống thẻ — @Kang

- [ ] **T-21** `CardData` và cơ chế áp hiệu ứng lên chỉ số — **@Kang**
  > **Không sửa trực tiếp `CharacterData`.** Đó là ScriptableObject; sửa lúc chạy sẽ ghi đè vĩnh viễn vào asset trong Editor. Cần một lớp chỉ số của ván, khởi tạo từ `CharacterData` rồi cho thẻ cộng dồn lên bản sao đó.
- [ ] **T-22** Giao diện chọn 1 trong 3 thẻ — 10 giây, 2 lượt đổi thẻ — **@Kang**
  > Dựng thành prefab trong `Prefabs/UI/Cards`, sinh lúc chạy. Không đặt sẵn vào `Arena.unity`.
- [ ] **T-23** Đồng bộ chọn thẻ: đợt kế chỉ khởi động khi cả hai người đã chọn xong — **@Kang**
  > Chỗ nối đã có sẵn: gọi `RunManager.ReportCardSelectionComplete()`. Xong việc này thì tắt cờ `_autoAdvanceCardSelection` ở T-14B.
- [ ] **T-24** Biên soạn 32 thẻ nền — **@Kang**
- [ ] **T-25** Hệ thống tiến hoá thẻ — kiểm tra công thức và thông báo — **@Kang**
- [ ] **T-26** Chốt và triển khai 8 công thức tiến hoá — **@Kang**

### Vòng lặp lõi — @Kiet

- [ ] **T-27** Cơ chế Hồn — rơi khi quái chết, tự hút về, âm thanh tăng dần cao độ — **@Kiet**
- [ ] **T-28** Nhân vật Gióng — roi sắt, đòn hình cung — **@Kiet**
- [ ] **T-29** Nhân vật Tấm — sáo trúc, đòn tia; tăng sát thương áp cho **phát bắn kế tiếp** — **@Kiet**
- [ ] **T-31** Sóng âm riêng cho từng nhạc cụ — **@Kiet**
- [ ] **T-32** Kiểm thử hiệu năng và đọc hiểu: 60 FPS với 40 quái và 200 đạn — **@Kiet**
- [ ] **T-33** Sprite Gióng, Tấm, 40 icon thẻ — **@artist**

---

# Cổng 3 — Tuần 8–12

**Nghiệm thu:** co-op qua Steam chạy được ngoài mạng LAN, hoàn tất 16 đợt không sai lệch trạng thái.

- [ ] **T-34** Bốn quái còn lại — Ma Trơi, Bù Nhìn, Ma Da, Quỷ Nhỏ — **@Kiet**
- [ ] **T-35** Snapshot vị trí quái 2 lần/giây để hiệu chỉnh sai lệch — **@Kiet**
- [ ] **T-36** Trống Đồng — kích hoạt bằng dash, xoá đạn, đẩy lùi, choáng 1 giây — **@Kiet**
- [ ] **T-37** Thời gian hồi Trống Đồng dùng chung, host quản lý — **@Kiet**
- [ ] **T-38** Hồn nạp năng lượng cho Trống Đồng, vòng nạp trên HUD — **@Kiet**
- [ ] **T-39** Trùm Chằn Tinh — hai pha, máu tỉ lệ theo số người chơi — **@Kiet**
- [ ] **T-40** Mời bạn qua Steam overlay, chuyển sang FizzySteamworks — **@Hung**
- [ ] **T-41** Hạ gục và hồi sinh — đồng đội đứng cạnh 3 giây — **@Kiet**
- [ ] **T-42** Xử lý mất kết nối: client rớt mạng, host thoát ván — **@Hung**
- [ ] **T-43** Thu thập telemetry ra CSV cho phần đánh giá khoá luận — **@Kiet**
- [ ] **T-44** Bảng đợt cố định — dùng cho nhóm đối chứng và làm phương án dự phòng — **@Kiet**
- [ ] **T-45** AI Đạo Diễn (LinUCB) — lõi thuật toán, `ContextVector`, `WaveSpec` — **@Kiet**
- [ ] **T-45B** Đạo diễn trong co-op — hợp thành ngữ cảnh N người, số hạng công bằng, tầng an toàn theo người yếu nhất — **@Kiet**
- [ ] **T-46** Điều tiết bất đối xứng, đòn bẩy chia cắt và dồn ép; hiển thị hoạt động đạo diễn trên HUD — **@Kiet**
- [ ] **T-46B** Đăng ký Steam Direct, tax interview, xác minh tài khoản — **tuần 10** — Chưa phân công
- [ ] **T-47** Sprite bốn quái, Chằn Tinh, trống đồng, hai tileset còn lại — **@artist**

---

# Cổng 4 — Tuần 13–16

**Nghiệm thu:** demo chạy ổn định; hoàn tất bảo vệ khoá luận.

- [ ] **T-48** Tiền tệ Ngọc, lưu tiến trình, bảng mở khoá — **@Hung**
- [ ] **T-49** Màn thống kê sau ván — **@Hung**
- [ ] **T-50** Cân bằng: xác định sát thương gốc cho cả ba vũ khí — **@Kiet**
- [ ] **T-51** Cân bằng đường cong độ khó qua 16 đợt — **@Kiet**
- [ ] **T-52** Nhạc nền và hiệu ứng âm thanh — Chưa phân công
- [ ] **T-53** Thực nghiệm đánh giá: 15 người dùng AI Đạo Diễn, 15 người dùng bảng đợt cố định — **@Kiet**
- [ ] **T-54** Phân tích số liệu và biên soạn chương đánh giá — **@Kiet**
- [ ] **T-55** Đóng gói demo — 8 đợt đầu, một nhân vật — Chưa phân công
- [ ] **T-58** Thiết lập giá: $2.99, khu vực Việt Nam 29.000–39.000₫ — Chưa phân công
- [ ] **T-59** Slide và bản demo phục vụ buổi bảo vệ — Chưa phân công

---

## Dự phòng — làm nếu còn quỹ thời gian

Thử thách hằng ngày kèm bảng xếp hạng · nhân vật thứ tư · trùm thứ hai · cấp độ khó thứ hai · thực nghiệm định lượng cho co-op

## Ngoài phạm vi — đã chốt không làm

Co-op 4 người · matchmaking · cửa hàng vật phẩm trang trí · bản mobile · cắt cảnh · nhánh rẽ Núi/Biển · **toàn bộ hạng mục quảng bá** (trang cửa hàng, trailer, TikTok, Next Fest)

> **Quảng bá đã đưa ra khỏi kế hoạch** theo quyết định của nhóm, để dồn quỹ thời gian cho sản phẩm và khoá luận. Nếu sau bảo vệ muốn phát hành thương mại thì dựng lại thành một kế hoạch riêng.

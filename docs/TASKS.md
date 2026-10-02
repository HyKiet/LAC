# Kế hoạch công việc — LẠC

Căn cứ duy nhất về tình trạng công việc: phân công, tiến độ, và **danh sách những gì đã tồn tại**.

Quy trình làm việc hằng ngày và cách tránh xung đột: [docs/WORKFLOW.md](WORKFLOW.md).
Ràng buộc kiến trúc: [CLAUDE.md](../CLAUDE.md).

---

## Phân công

| Thành viên | Mảng phụ trách | Thư mục sở hữu |
|---|---|---|
| **@Kiet** | Vòng lặp lõi: chiến đấu, quái, đồng bộ trong ván, đạo diễn | `Scripts/Core` `Combat` `Enemies` `Player` `VFX` · `Scripts/Net` *(trừ `Net/Lobby`)* · `Scenes/Arena.unity` |
| **@Hung** | Hệ thống thẻ nâng cấp và giao diện thẻ | `Scripts/Cards` · `Data/Cards` · `Prefabs/UI/Cards` |
| **@Kang** | Màn hình vào game và kết nối: menu, cài đặt, tạo và vào phòng, tạm dừng, Steam, mất kết nối | `Scripts/Menu` · `Scripts/Net/Lobby` · `Scenes/Boot.unity` · `Prefabs/UI/Menu` |
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
  > Mỗi đợt một luồng ngẫu nhiên riêng gieo từ seed + số đợt, để người vào giữa ván tính ra cùng kết quả. `Core/WaveManager.cs`. **Nội dung đợt đã chuyển sang bảng dữ liệu ở T-44.**
  > T-23 đã gỡ cơ chế tự chuyển sau 1.5 giây; hệ thống thẻ quyết định khi nào được sang đợt.
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

### Màn hình, luồng vào game và kết nối — @Kang

- [ ] **T-60** Scene `Boot.unity` và menu chính: Chơi · Chơi cùng bạn · Cài đặt · Thoát — **@Kang**
- [ ] **T-61** Màn hình cài đặt: âm lượng, độ phân giải, gán lại phím; lưu bằng `PlayerPrefs` — **@Kang**
- [ ] **T-62** Luồng vào ván: tạo phòng, tham gia bằng địa chỉ, chuyển sang `Arena.unity` — **@Kang**
  > Bắt buộc đi qua `NetworkManagerLAC`. **Chơi đơn cũng phải `StartHost`**, không được có nhánh riêng — CLAUDE.md mục 3.1.
- [ ] **T-63** Tạm dừng trong ván: tiếp tục · cài đặt · thoát về menu — **@Kang**
  > Trong co-op, tạm dừng **không** được dừng thời gian của cả hai máy; chỉ mở giao diện tại máy đó.
- [ ] **T-30** Màn chọn nhân vật, đồng bộ lựa chọn qua mạng — **@Kang**
  > Chỉ đồng bộ định danh nhân vật, không đồng bộ chỉ số — mục 3.2.

### Hệ thống thẻ — @Hung

- [x] **T-21** `CardData` và cơ chế áp hiệu ứng lên chỉ số — **@Kang** · 06/09
  > **Không sửa trực tiếp `CharacterData`.** Đó là ScriptableObject; sửa lúc chạy sẽ ghi đè vĩnh viễn vào asset trong Editor. Cần một lớp chỉ số của ván, khởi tạo từ `CharacterData` rồi cho thẻ cộng dồn lên bản sao đó.
  > Demo 7 thẻ dùng `CardDefinition` và `PlayerUpgradeState`, áp sát thương/tốc đánh/máu/lướt cùng combo Song Tiễn–Xuyên Tâm–Bộc Phá mà không sửa asset nhân vật. `Scripts/Cards` · `Data/Cards` · `Combat/WeaponAuto.cs` · `Combat/Projectile.cs`.
- [x] **T-22** Giao diện chọn 1 trong 3 thẻ — 10 giây, 2 lượt đổi thẻ — **@Kang** · 24/09
  > Đồng hồ theo host, tự chọn khi hết 10 giây, 2 lượt đổi mỗi ván, khóa nhấp đôi và giao diện chờ đồng đội. `Cards/CardSelectionController.cs` · `CardSelectionView.cs` · `CardSelectionRulesData.cs`. Đã kiểm tra Play Mode và Game View.
  > Dựng thành prefab trong `Prefabs/UI/Cards`, sinh lúc chạy. Không đặt sẵn vào `Arena.unity`.
- [x] **T-22B** Làm lại khung và khu vực chọn thẻ, bỏ nhịp thở, thêm lóe sáng nhẹ — 01/10/2026
  > `Cards/CardFrameGraphic.cs` vẽ khung đồng vát góc/viền kép, lóe sáng chỉ trên viền; `CardHoverVisual.cs` giữ thẻ đứng yên khi nghỉ/hover; `CardSelectionView.cs` nới bố cục, tăng chữ, tách chân bảng và dọn màn chờ. Đạt hồi quy host 12/12 thẻ, đo chữ không tràn, kiểm tra transform cố định/timer chạy ở timeScale=0, đã nhìn ảnh Game View và Console cuối lượt 0 lỗi. Chi tiết: `docs/CARD_FRAME_UI.md`.
- [x] **T-22C** Tối ưu độ rõ chữ và bố cục thẻ ở 720p/1080p — Codex · 01/10/2026
  > `Cards/CardSelectionView.cs` dùng canvas căn pixel, thiết kế 720p, mô tả 22/33 px tại 720p/1080p, căn trái và không best-fit/hiệu ứng làm nhòe; `CardFrameGraphic.cs` sửa nan khung nút thấp. `Cards/Editor/CardTypographyChecks.cs` kiểm 12 thẻ + 8 tiến hoá, dấu tiếng Việt, không tràn và tương phản >=7:1; hồi quy host 12/12 đạt, đã nhìn ảnh Game View, Console 0 lỗi đỏ. Bằng chứng: `docs/CARD_READABILITY.md`.
- [x] **T-22D** Tối ưu cập nhật và tương tác UI thẻ — Codex · 02/10/2026
  > `Cards/CardSelectionController.cs` cache giây đếm ngược; `CardHoverVisual.cs` không ghi transform/màu lúc nghỉ, cache camera animation; `CardSelectionView.cs` bỏ raycast trang trí. Đạt kiểm 10.000 update bộ đếm/idle, đề nghị cùng giây/quá hạn, bấm icon/focus/dim/consume/reset; typography và ảnh Game View 720p/1080p đạt, hồi quy host 12/12. `docs/CARD_OPTIMIZATION.md`.
- [x] **T-22E** Sửa tint và trạng thái nút đổi thẻ — Codex · 02/10/2026
  > `Cards/CardSelectionView.cs` không nhân tint hai lần, giữ khoá đổi khi hết lượt kể cả bật lại tương tác; `Editor/CardTypographyChecks.cs` bổ sung hồi quy. Đạt typography/UI 12 thẻ + 8 tiến hoá ở 720p/1080p, đã nhìn ảnh nút thường/hover và Console cuối lượt 0 lỗi đỏ. `docs/CARD_HISTORY_OPTIMIZATION.md` ghi rõ các cảnh báo ngoài Cards.
- [x] **T-23** Đồng bộ chọn thẻ: đợt kế chỉ khởi động khi cả hai người đã chọn xong — **@Kang** · 24/09
  > Host thẩm định đề nghị/lượt đổi/lựa chọn theo kết nối, đồng bộ lịch sử định danh, chờ ACK animation, bỏ người rời mạng khỏi điều kiện chờ. `Cards/CardSelectionNetwork.cs` (partial `RunManager`) · `Core/WaveManager.cs`. Hai tiến trình với LatencySimulation đã đạt toàn bộ kiểm tra; xem `docs/CARD_TESTS.md`.
- [x] **T-23A** Tối ưu cache và replay lịch sử thẻ phía client — Codex · 02/10/2026
  > `Cards/CardSelectionNetwork.cs` chỉ dựng lịch sử khi dirty, nhận SET cùng số lượng/dữ liệu đến muộn/component mới và làm mới hook start/stop; `PlayerUpgradeState.cs` replay đúng thứ tự, phát một Changed trạng thái cuối. Đạt 58 lịch sử/174 notification, 10.000 idle tick, hồi quy 45.000 lượt bốc/1.152 marginal, host 12/12 và hai tiến trình 100 ms; 4/4 chữ ký đợt 2–3 khớp, snapshot client mới dựng đúng Nỏ Thần. `docs/CARD_HISTORY_OPTIMIZATION.md`; không đóng T-32/T-50/T-51.
- [x] **T-24** Biên soạn và cân bằng 12 thẻ nền — **@Kang** · 24/09
  > Phạm vi thu gọn theo yêu cầu: 7 thẻ cũ + Khinh Thân, Âm Vang, Hồi Xuân, Thiết Bích, Cuồng Nộ. Tham số lưu trong `Data/Cards/Resources/Cards`; lọc thẻ đạn theo vũ khí, Sinh Lực theo máu gốc, giữ sát thương phần lẻ. `Cards/PlayerUpgradeState.cs` · `CardDefinition.cs` · `Editor/CardBalanceChecks.cs`. Đạt 45.000 lượt chọn mô phỏng và Play Mode 12/12 thẻ; cân bằng độ khó toàn game vẫn thuộc T-50/T-51.
  > Hợp nhất vào `main` ngày 27/09: giữ gameplay T-27/T-28/T-29/T-44, ghép nâng cấp với thưởng dash, kiểm tra 45.000 lượt chọn và 12/12 thẻ trên host. Chi tiết và giới hạn kiểm chứng: `docs/CARD_TESTS.md`.
- [x] **T-24A** Rà soát Cuồng Nộ với tiến hoá và sửa cấp làm giảm DPS — Codex · 02/10/2026
  > `Data/Cards/Resources/Cards/CuongNo.asset` giảm phạt tốc đánh −10%→−8%, giữ +25% damage/2 cấp/trọng số; generator, mô tả và giá trị kiểm đồng bộ. `Cards/Editor/CardBalanceChecks.cs` đạt 45.000 lượt bốc + 1.152 tổ hợp thật/3 nhân vật/8 công thức, mức tăng DPS nhỏ nhất +0,030×; hồi quy host 12/12 đạt. Phần ngoài Cards chỉ báo cáo trong `docs/BALANCE_AUDIT.md`, không đóng T-50/T-51.
- [x] **T-25** Hệ thống tiến hoá thẻ — kiểm tra công thức và thông báo — **@Hung** · 01/10
  > Tự kiểm nguyên liệu sau lựa chọn host duyệt, cộng thưởng một lần mỗi ván, giữ cấp thẻ nền; replay bằng định danh, thông báo nối hàng và chờ ACK trong co-op. `Cards/CardEvolutionData.cs` · `CardEvolutionCatalog.cs` · `PlayerUpgradeState.cs` · `CardSelectionController.cs` · `CardSelectionNetwork.cs` · `CardSelectionView.cs`. Đã kiểm Play Mode, Game View và hai tiến trình với độ trễ 100 ms; xem `docs/EVOLUTION_TESTS.md`.
- [x] **T-26** Chốt và triển khai 8 công thức tiến hoá — **@Hung** · 01/10
  > Đủ Nỏ Thần, Lửa Thiêng, Trăm Trứng, Thánh Gióng, Tiếng Đàn Thần, Lạc Phong, Bất Tử, Kim Cang từ 12 thẻ nền; bonus tách khỏi bể bốc. `Data/Cards/Resources/Evolutions` · `Data/Cards/EvolutionBonuses` · `Cards/Editor/CardEvolutionCatalogChecks.cs`. Đạt 8 công thức × 3 nhân vật, 8/8 trong Play Mode và 16/16 chữ ký chỉ số client khớp host; cân bằng cả ván vẫn thuộc T-50/T-51.

### Vòng lặp lõi — @Kiet

- [x] **T-27** Cơ chế Hồn — rơi khi quái chết, tự hút về, âm thanh tăng dần cao độ — @Kiet · 08/09
  > Số Hồn rơi ra tra theo định danh quái qua `RandomStream.Hash01` chứ không rút tuần tự khỏi luồng, nên người vào giữa ván vẫn ra cùng kết quả. Đường bay và tiếng động là biểu diễn cục bộ; **chỉ host cộng bộ đếm** `RunManager.SoulsCollected` — nguồn nạp cho Trống Đồng ở T-38. `Core/SoulPickup.cs` · `Core/SoulSpawner.cs` · `Audio/PitchLadder.cs` · `Prefabs/VFX/Soul.prefab` · `Art/Sprites/VFX_Hon.png` · `Audio/SFX_NhatHon.wav` (tạm, chờ T-52).
- [x] **T-28** Nhân vật Gióng — roi sắt, đòn hình cung — @Kiet · 08/09
  > Vùng sát thương hình cung đã có từ T-12; hạng mục này khép ba chỗ hở. Nửa góc cung chuyển từ trường trên `WeaponAuto` vào `CharacterData.ArcHalfAngle` để mỗi nhân vật một góc và khâu cân bằng sửa dữ liệu chứ không sửa prefab. Thêm bán kính cận chiến: quái đứng đè lên người chơi làm `normalized` trả về véc-tơ không nên trước đó nó là con duy nhất không ăn đòn. Vệt roi `ArcSlash` quét qua trước mặt thay cho sóng tròn — Gióng vốn `SpawnSoundWave` tắt nên trước đó đòn đánh không có hiệu ứng nào. `VFX/ArcSlash.cs` · `Combat/WeaponAuto.cs` · `Player/CharacterData.cs` · `Prefabs/VFX/ArcSlash.prefab` · `Art/Sprites/VFX_RoiSat.png` (tạm, chờ T-33).
- [x] **T-29** Nhân vật Tấm — sáo trúc, đòn tia; tăng sát thương áp cho **phát bắn kế tiếp** — @Kiet · 08/09
  > Phần thưởng nạp một lần khi lướt và **tiêu ngay lúc khai hoả**, không phải lúc đạn trúng — tiêu lúc trúng thì hai viên bắn liên tiếp cùng ăn một phần thưởng. Không dùng cửa sổ thời gian: hồi chiêu lướt 0.4 s ngắn hơn cửa sổ 1 s của GDD nên hiệu ứng sẽ bật vĩnh viễn, xem mục 7. `PlayerDash.Dashed` phát trên mọi máy để host cũng áp được phần thưởng cho nhân vật của client. `CharacterData.DashDamageMultiplier` = 2 · `Combat/WeaponAuto.ConsumeDamage` · `Player/PlayerDash.cs` · `Data/Animations/Tam.asset` (TinySwords Archer, tạm — chờ T-33).
- [ ] **T-31** Sóng âm riêng cho từng nhạc cụ — **@Kiet**
- [ ] **T-32** Kiểm thử hiệu năng và đọc hiểu: 60 FPS với 40 quái và 200 đạn — **@Kiet**
  > Đã đo baseline T-32A bên dưới; chưa đóng T-32 vì fixture dày che nhân vật/số sát thương, pool DamageNumber vượt ngưỡng 128 và còn hitch. Cần kiểm standalone + đạn địch sau T-34; xem `docs/T32_TESTS.md`.
- [x] **T-32A** Fixture tải 40 quái/200 đạn và baseline Editor, không sửa mã lõi — Codex · 01/10/2026
  > `docs/tools/T32LoadProbe.cs` dùng pool/component thật, đo hai tình huống ở 720p/1080p, mỗi lượt warmup 3 s + đo 30 s; đủ 40/200 suốt lấy mẫu, P95 toàn khung 3,90–7,45 ms. Đã nhìn ảnh Game View, Console cuối lượt 0 lỗi đỏ và cảnh báo DamageNumber được ghi nguyên trạng. JSON: `docs/measurements/`; giới hạn, phát hiện đọc hiểu và cách chạy: `docs/T32_TESTS.md`. Không thay gameplay, asset hay scene.
- [ ] **T-33** Sprite Gióng, Tấm, 20 icon thẻ (12 nền + 8 tiến hoá) — **@artist**
  > Phần icon đã hoàn tất ở T-33A bên dưới; T-33 còn sprite Gióng và Tấm.
- [x] **T-33A** Bộ 20 icon thẻ đồng nhất, gán vào dữ liệu và kiểm tra trong Unity — 01/10/2026
  > `Art/Sprites/UI/Cards/DongHo_2026/` có 12 ảnh nền + 8 ảnh tiến hoá tạo bằng imagegen, cùng chất liệu đồng cổ–ngọc xanh và ánh sáng góc trên trái. `Cards/Editor/CardArtSetup.cs` nhập Sprite/Single, Point, không mipmap và kiểm tra ánh xạ; 8 bonus dùng ảnh recipe tương ứng. Đã chạy Mirror host trong Play mode, nhìn ảnh cả bộ/màn chọn thẻ/thông báo tiến hoá, kiểm tra 8/8 panel dùng đúng icon, Console cuối lượt có 0 lỗi đỏ. Prompt và bằng chứng: `docs/CARD_ART.md`. Ảnh demo cũ được giữ nguyên.

---

# Cổng 3 — Tuần 8–12

**Nghiệm thu:** co-op qua Steam chạy được ngoài mạng LAN, hoàn tất 16 đợt không sai lệch trạng thái.

- [ ] **T-34** Bốn quái còn lại — Ma Trơi, Bù Nhìn, Ma Da, Quỷ Nhỏ — **@Kiet**
- [ ] **T-35** Snapshot vị trí quái 2 lần/giây để hiệu chỉnh sai lệch — **@Kiet**
- [ ] **T-36** Trống Đồng — kích hoạt bằng dash, xoá đạn, đẩy lùi, choáng 1 giây — **@Kiet**
- [ ] **T-37** Thời gian hồi Trống Đồng dùng chung, host quản lý — **@Kiet**
- [ ] **T-38** Hồn nạp năng lượng cho Trống Đồng, vòng nạp trên HUD — **@Kiet**
- [ ] **T-39** Trùm Chằn Tinh — hai pha, máu tỉ lệ theo số người chơi — **@Kiet**
- [ ] **T-40** Mời bạn qua Steam overlay, chuyển sang FizzySteamworks — **@Kang**
  > Đổi transport phải sửa `Net/NetworkManagerLAC.cs` — file của @Kiet. Báo trước khi động vào.
- [ ] **T-41** Hạ gục và hồi sinh — đồng đội đứng cạnh 3 giây — **@Kiet**
- [ ] **T-42** Xử lý mất kết nối: client rớt mạng, host thoát ván — **@Kang**
- [ ] **T-43** Thu thập telemetry ra CSV cho phần đánh giá khoá luận — **@Kiet**
- [x] **T-44** Bảng đợt cố định — quái vào sân theo nhịp, theo hướng, theo thành phần — @Kiet · 10/09
  > Thay công thức tuyến tính hard-code trong `WaveManager` bằng một tài sản dữ liệu. Ba thay đổi về lối chơi: quái **vào sân theo nhịp** thay vì đổ hết trong một khung hình; mỗi đợt có **số hướng vào sân riêng**, trải đều trên chu vi rồi xoay ngẫu nhiên; nhiều loại quái trong một đợt **đan xen theo vòng** chứ không hết loại này tới loại kia. `WaveManager` giờ chỉ biết `WaveSpec` — đúng đầu ra của AI Đạo Diễn ở docs/GDD.md mục 7.3 — nên T-45 lắp vào không phải sửa tệp này. Trần 40 quái được kẹp **lúc dựng đặc tả**, không phải lúc sinh, vì đếm quái sống trên sân cho ra hai con số khác nhau ở hai máy. Tổng số đợt chuyển từ `RunManager` sang bảng. `Core/WaveSpec.cs` · `Core/WaveTable.cs` · `Core/WaveManager.cs` · `Core/RunManager.cs` · `Data/Waves/WaveTable_CoDinh.asset`.
  > **Còn hai chỗ hở, cố ý.** Thành phần đợt hiện chỉ có Cô Hồn vì bốn quái còn lại thuộc T-34; cấu trúc đã sẵn sàng, chỉ cần thêm dòng vào bảng. Luật cấm sinh trong bán kính 3 quanh người chơi (GDD mục 7.6) **chưa cài** — nó cần vị trí người chơi, mà vị trí đó lệch nhau giữa hai máy; luật này thuộc tầng an toàn của đạo diễn ở T-45 nơi host quyết một mình.
- [ ] **T-45** AI Đạo Diễn (LinUCB) — lõi thuật toán, `ContextVector`, `WaveSpec` — **@Kiet**
- [ ] **T-45B** Đạo diễn trong co-op — hợp thành ngữ cảnh N người, số hạng công bằng, tầng an toàn theo người yếu nhất — **@Kiet**
- [ ] **T-46** Điều tiết bất đối xứng, đòn bẩy chia cắt và dồn ép; hiển thị hoạt động đạo diễn trên HUD — **@Kiet**
- [ ] **T-46B** Đăng ký Steam Direct, tax interview, xác minh tài khoản — **tuần 10** — Chưa phân công
- [ ] **T-47** Sprite bốn quái, Chằn Tinh, trống đồng, hai tileset còn lại — **@artist**

---

# Cổng 4 — Tuần 13–16

**Nghiệm thu:** demo chạy ổn định; hoàn tất bảo vệ khoá luận.

- [ ] **T-48** Tiền tệ Ngọc, lưu tiến trình, bảng mở khoá — **@Kang**
- [ ] **T-49** Màn thống kê sau ván — **@Kang**
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

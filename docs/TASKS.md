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
  > Ban đầu dùng ô rời; ngày 29/09 đổi sang thanh tiến trình liền theo yêu cầu, có số máu hiện tại/tối đa, cập nhật cả khi chỉ đổi máu tối đa. `UI/PlayerHud.cs`.
- [x] **T-15D** HUD dạng thanh tiến trình và sửa độ chính xác tầm đánh — Codex · 29/09/2026
  > HUD nền tối, màu chàm, kích thước cố định; chọn mục tiêu tính cả mép tầm; đạn bỏ phần bay dư 35%, quét va chạm theo đường bay và thứ tự chạm; cung Gióng khớp bán kính/góc sát thương. `UI/PlayerHud.cs` · `Combat/WeaponAuto.cs` · `Combat/Projectile.cs` · `Enemies/Enemy.cs` · `Enemies/EnemyRegistry.cs` · `VFX/ArcSlash.cs`. Đã kiểm tra host Play mode, ảnh Game View và Console; chi tiết/giới hạn tại `docs/HUD_RANGE_TESTS.md`.
- [x] **T-15E** HUD đợt và thông báo chuyển đợt có chuyển động — Codex · 29/09/2026
  > Số đợt/tổng đợt ở giữa phía trên, bảng thông báo trượt vào/phóng nhẹ rồi mờ và trượt ra trong 1.75 giây; đọc trạng thái RunManager đã đồng bộ, xử lý chọn thẻ, đợt cuối, thắng/thua và chơi lại. `UI/WaveHud.cs` · `UI/PlayerHud.cs` · `Prefabs/UI/Resources/UI/WaveHud.prefab`. Đạt kiểm tra host Play mode, ảnh Game View, Console 0 lỗi/0 cảnh báo; xem `docs/WAVE_HUD_TESTS.md`.
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
- [x] **T-23** Đồng bộ chọn thẻ: đợt kế chỉ khởi động khi cả hai người đã chọn xong — **@Kang** · 24/09
  > Host thẩm định đề nghị/lượt đổi/lựa chọn theo kết nối, đồng bộ lịch sử định danh, chờ ACK animation, bỏ người rời mạng khỏi điều kiện chờ. `Cards/CardSelectionNetwork.cs` (partial `RunManager`) · `Core/WaveManager.cs`. Hai tiến trình với LatencySimulation đã đạt toàn bộ kiểm tra; xem `docs/CARD_TESTS.md`.
- [x] **T-24** Biên soạn và cân bằng 12 thẻ nền — **@Kang** · 24/09
  > Phạm vi thu gọn theo yêu cầu: 7 thẻ cũ + Khinh Thân, Âm Vang, Hồi Xuân, Thiết Bích, Cuồng Nộ. Tham số lưu trong `Data/Cards/Resources/Cards`; lọc thẻ đạn theo vũ khí, Sinh Lực theo máu gốc, giữ sát thương phần lẻ. `Cards/PlayerUpgradeState.cs` · `CardDefinition.cs` · `Editor/CardBalanceChecks.cs`. Đạt 45.000 lượt chọn mô phỏng và Play Mode 12/12 thẻ; cân bằng độ khó toàn game vẫn thuộc T-50/T-51.
  > Hợp nhất vào `main` ngày 27/09: giữ gameplay T-27/T-28/T-29/T-44, ghép nâng cấp với thưởng dash, kiểm tra 45.000 lượt chọn và 12/12 thẻ trên host. Chi tiết và giới hạn kiểm chứng: `docs/CARD_TESTS.md`.
- [ ] **T-25** Hệ thống tiến hoá thẻ — kiểm tra công thức và thông báo — **@Hung**
- [ ] **T-26** Chốt và triển khai 8 công thức tiến hoá — **@Hung**

### Vòng lặp lõi — @Kiet

- [x] **T-27** Cơ chế Hồn — rơi khi quái chết, tự hút về, âm thanh tăng dần cao độ — @Kiet · 08/09
  > Số Hồn rơi ra tra theo định danh quái qua `RandomStream.Hash01` chứ không rút tuần tự khỏi luồng, nên người vào giữa ván vẫn ra cùng kết quả. Đường bay và tiếng động là biểu diễn cục bộ; **chỉ host cộng bộ đếm** `RunManager.SoulsCollected` — nguồn nạp cho Trống Đồng ở T-38. `Core/SoulPickup.cs` · `Core/SoulSpawner.cs` · `Audio/PitchLadder.cs` · `Prefabs/VFX/Soul.prefab` · `Art/Sprites/VFX_Hon.png` · `Audio/SFX_NhatHon.wav` (tạm, chờ T-52).
- [x] **T-28** Nhân vật Gióng — roi sắt, đòn hình cung — @Kiet · 08/09
  > Vùng sát thương hình cung đã có từ T-12; hạng mục này khép ba chỗ hở. Nửa góc cung chuyển từ trường trên `WeaponAuto` vào `CharacterData.ArcHalfAngle` để mỗi nhân vật một góc và khâu cân bằng sửa dữ liệu chứ không sửa prefab. Thêm bán kính cận chiến: quái đứng đè lên người chơi làm `normalized` trả về véc-tơ không nên trước đó nó là con duy nhất không ăn đòn. Vệt roi `ArcSlash` quét qua trước mặt thay cho sóng tròn — Gióng vốn `SpawnSoundWave` tắt nên trước đó đòn đánh không có hiệu ứng nào. `VFX/ArcSlash.cs` · `Combat/WeaponAuto.cs` · `Player/CharacterData.cs` · `Prefabs/VFX/ArcSlash.prefab` · `Art/Sprites/VFX_RoiSat.png` (tạm, chờ T-33).
- [x] **T-29** Nhân vật Tấm — sáo trúc, đòn tia; tăng sát thương áp cho **phát bắn kế tiếp** — @Kiet · 08/09
  > Phần thưởng nạp một lần khi lướt và **tiêu ngay lúc khai hoả**, không phải lúc đạn trúng — tiêu lúc trúng thì hai viên bắn liên tiếp cùng ăn một phần thưởng. Không dùng cửa sổ thời gian: hồi chiêu lướt 0.4 s ngắn hơn cửa sổ 1 s của GDD nên hiệu ứng sẽ bật vĩnh viễn, xem mục 7. `PlayerDash.Dashed` phát trên mọi máy để host cũng áp được phần thưởng cho nhân vật của client. `CharacterData.DashDamageMultiplier` = 2 · `Combat/WeaponAuto.ConsumeDamage` · `Player/PlayerDash.cs` · `Data/Animations/Tam.asset` (TinySwords Archer, tạm — chờ T-33).
- [x] **T-31** Sóng âm riêng cho từng nhạc cụ — **@Kiet**
  > Codex · 03/10/2026: `VFX/InstrumentWave.cs`, `InstrumentWaveData.cs`, `Data/VFX` và `Combat/WeaponAuto.cs`: đàn bầu có ba vòng nan hoa, roi sắt ba cung theo tầm/góc thật, sáo trúc gợn nhỏ theo hướng từng viên. Dùng pool, additive, màu chàm/gỉ đồng và lớp dưới đòn địch. Đã xem Game View, thử khai hỏa ba vũ khí và đo Windows 1080p với 40 quái/200 đạn + hiệu ứng mới: p95 4.296 ms. [INSTRUMENT_REVIVE_TESTS.md](INSTRUMENT_REVIVE_TESTS.md).
- [x] **T-32** Kiểm thử hiệu năng và đọc hiểu: 60 FPS với 40 quái và 200 đạn — **@Kiet**
  > Codex · 02/10/2026: `Core/PerformanceScenario.cs` duy trì 40 quái, 200 đạn và 10 sóng âm/giây; Windows Development 1080p đạt p95 4.187 ms trên i9-12900H/RTX 3060 Laptop. Sửa dung lượng pool dùng chung trong `ObjectPool.cs`/`PoolRegistry.cs`, đổi đạn người chơi sang gỉ đồng trong suốt. Đã xem Game View; Console không lỗi đỏ. Kịch bản, JSON và giới hạn phép đo: [PERFORMANCE_TESTS.md](PERFORMANCE_TESTS.md).
- [ ] **T-33** Sprite Gióng, Tấm, 20 icon thẻ (12 nền + 8 tiến hoá) — **@artist**

---

# Cổng 3 — Tuần 8–12

**Nghiệm thu:** co-op qua Steam chạy được ngoài mạng LAN, hoàn tất 15 đợt không sai lệch trạng thái.

- [x] **T-34** Bốn quái còn lại — Ma Trơi, Bù Nhìn, Ma Da, Quỷ Nhỏ — **@Kiet**
  > Codex · 30/09/2026: `Enemies/EnemyBehaviours.cs`, `EnemyData.cs`, `EnemyProjectile.cs`, bốn asset quái và `WaveTable_CoDinh.asset`. Ma Trơi giữ khoảng cách/bắn chậm; Bù Nhìn đánh 3 máu; Ma Da báo hướng rồi lao nhịp 2 giây; Quỷ Nhỏ tách hai Cô Hồn từ sự kiện host. Đạn dùng pool, nối Trống Đồng; ngân sách wave dự trù cả quái con. Sprite tạm, chờ T-47. Play Mode ALL PASSED, đã xem Game View và thử bắn/lao/xóa đạn trong co-op; [ENEMY_TESTS.md](ENEMY_TESTS.md).
- [x] **T-35** Snapshot vị trí quái 2 lần/giây để hiệu chỉnh sai lệch — **@Kiet**
  > Codex · 30/09/2026: `EnemySpawner.cs`, `Enemy.cs`, `EnemyCatalogData.cs`, `WaveManager.cs`: snapshot toàn roster mỗi 0.5 giây, bù độ trễ/triệt sai lệch dần, ID theo wave và ID con riêng, bỏ gói cũ/khác ván, sửa quái sinh thiếu/dư và reset khi seed đổi. Hai tiến trình RTT ~272 ms nhận đủ 70 quái; lệch 2 đơn vị còn 0.001 sau 1.5 giây; tách quái, roster rỗng và Restart wave 1 đạt. Build Windows thành công, Console không lỗi đỏ.
- [x] **T-36** Trống Đồng — kích hoạt bằng dash, xoá đạn, đẩy lùi, choáng 1 giây — **@Kiet**
  > Codex · 30/09/2026: `Drum/DongSonDrum.cs`, `Drum/DrumShockwave.cs`, `PlayerDash.cs`, `Enemy.cs`; kiểm tra đoạn lướt đã đi, đẩy lùi 2.5 đơn vị và choáng 1 giây. `EnemyProjectileClearable` xoá đạn đã đăng ký, giữ đạn người chơi; T-34 phải gắn marker khi thêm đạn địch. Sóng dùng pool, sửa truyền alpha qua MaterialPropertyBlock trong `SoundWave.cs`; đã xem Game View. Kiểm thử: [DRUM_TESTS.md](DRUM_TESTS.md).
- [x] **T-37** Thời gian hồi Trống Đồng dùng chung, host quản lý — **@Kiet**
  > Codex · 30/09/2026: `Drum/DrumNetwork.cs` mở rộng RunManager, SyncVar thời điểm hồi và RPC hiệu ứng, hồi chung 20 giây, khóa trước hiệu ứng chống kích hoạt kép. Đã thử host + client độc lập qua LatencySimulation, cú lướt client kích hoạt đúng một lần và nhận lại trạng thái hồi.
- [x] **T-38** Hồn nạp năng lượng cho Trống Đồng, vòng nạp trên HUD — **@Kiet**
  > Codex · 30/09/2026: `DrumData.cs`, `DrumRules.asset`, `UI/DrumHud.cs` và prefab HUD: 20 Hồn/lần, vòng năng lượng + vòng hồi, nhắc lướt khi sẵn sàng. `SoulSpawner` thu hồi Hồn cũ khi chơi lại. Đã thử nạp trong hồi, giới hạn nạp, reset và HUD; Console không lỗi đỏ.
- [x] **T-39** Trùm Chằn Tinh — hai pha, máu tỉ lệ theo số người chơi — **@Kiet**
  > Codex · 02/10/2026: `Enemies/EnemyBoss.cs`, `BossNetwork.cs`, `BossData.cs`, dữ liệu Chằn Tinh và `UI/BossHud.cs`; wave 16 có boss 800/1600 máu, lao/phun đạn rồi quét đuôi/triệu hồi Ma Da ở 50% máu, Trống Đồng ngắt đòn. Play Mode ALL PASSED; hai tiến trình với giả lập 100 ms xác nhận chuyển pha, vào giữa trận, từ chối sát thương client, Victory và Restart. Đã xem HUD/vùng báo đòn, Console cuối 0 lỗi/0 cảnh báo. Sprite tạm chờ T-47; [BOSS_TESTS.md](BOSS_TESTS.md).
- [ ] **T-40** Mời bạn qua Steam overlay, chuyển sang FizzySteamworks — **@Kang**
  > Đổi transport phải sửa `Net/NetworkManagerLAC.cs` — file của @Kiet. Báo trước khi động vào.
- [x] **T-41** Hạ gục và hồi sinh — đồng đội đứng cạnh 3 giây — **@Kiet**
  > Codex · 03/10/2026: `Player/PlayerRevive.cs`, `ReviveData.cs`, `VFX/ReviveMarker.cs` và prefab Player. Host kiểm tra liên tục trong bán kính 1.5, đủ 3 giây hồi 50% máu tối đa + bất tử 1 giây; rời vùng hủy tiến độ. Vòng cứu, lời nhắc và thanh tiến độ trên đầu đồng đội; khóa di chuyển/lướt/bắn khi gục. Hai tiến trình với độ trễ giả lập 100 ms ALL PASSED: hủy/cứu lại, máu và input, Defeat, Restart; Console không lỗi đỏ, đã xem Game View. [INSTRUMENT_REVIVE_TESTS.md](INSTRUMENT_REVIVE_TESTS.md).
- [ ] **T-42** Xử lý mất kết nối: client rớt mạng, host thoát ván — **@Kang**
- [x] **T-43** Thu thập telemetry ra CSV cho phần đánh giá khoá luận — **@Kiet**
  > Codex · 04/10/2026: `Director/TelemetrySession.cs`, `TelemetryCsv.cs`, `RunTelemetry.cs`; host ghi CSV sự kiện, từng người/từng wave và tổng kết ván, có seed, sát thương thực, hạ gục/hồi sinh, dash, vào/rời mạng và reset. 99 kiểm tra độc lập; Play Mode một người và co-op hai tiến trình 100 ms ALL PASSED; kiểm tra vào/rời giữa wave, chống đếm dash kép và 16 wave thật qua chọn thẻ/boss. Development build 0 lỗi, Console nghiệm thu co-op 0 lỗi/0 cảnh báo. Game View 40 quái/200 đạn, telemetry bật: P95 4,78 ms. [Hướng dẫn và CSV kiểm chứng](TELEMETRY_TESTS.md).
- [x] **T-44** Bảng đợt cố định — quái vào sân theo nhịp, theo hướng, theo thành phần — @Kiet · 10/09
  > Thay công thức tuyến tính hard-code trong `WaveManager` bằng một tài sản dữ liệu. Ba thay đổi về lối chơi: quái **vào sân theo nhịp** thay vì đổ hết trong một khung hình; mỗi đợt có **số hướng vào sân riêng**, trải đều trên chu vi rồi xoay ngẫu nhiên; nhiều loại quái trong một đợt **đan xen theo vòng** chứ không hết loại này tới loại kia. `WaveManager` giờ chỉ biết `WaveSpec` — đúng đầu ra của AI Đạo Diễn ở docs/GDD.md mục 7.3 — nên T-45 lắp vào không phải sửa tệp này. Trần 40 quái được kẹp **lúc dựng đặc tả**, không phải lúc sinh, vì đếm quái sống trên sân cho ra hai con số khác nhau ở hai máy. Tổng số đợt chuyển từ `RunManager` sang bảng. `Core/WaveSpec.cs` · `Core/WaveTable.cs` · `Core/WaveManager.cs` · `Core/RunManager.cs` · `Data/Waves/WaveTable_CoDinh.asset`.
  > T-34 đã bổ sung bốn loại quái vào thành phần đợt, dự trù quái con khi tính trần 40. Luật cấm sinh trong bán kính 3 quanh người chơi (GDD mục 7.6) **chưa cài** — nó cần vị trí người chơi, mà vị trí đó lệch nhau giữa hai máy; luật này thuộc tầng an toàn của đạo diễn ở T-45 nơi host quyết một mình.
- [x] **T-44B** Ván 15 đợt và tích hợp mỹ thuật thử nghiệm ThirdParty — **@Kiet**
  > Codex · 04/10/2026: `WaveTable_CoDinh.asset` còn 14 đợt thường + Chằn Tinh ở đợt 15; fallback scene/mã và GDD/CLAUDE đồng bộ. `Giong_TinySwords.asset` dùng Yellow Warrior; `ArenaStageDecor.prefab` + `VFX/ArenaStageVisuals.cs` đổi cây/đá ở wave 6 và 11. Đã xem Game View ba chặng và Victory 15/15; Play đủ 15 wave/14 lượt chọn thẻ, telemetry, animation, Restart đạt. Co-op hai tiến trình 100 ms xác nhận vào muộn, boss 1600 máu ở wave 15, Victory/Restart; Console cuối 0 lỗi/0 cảnh báo. Development build 0 lỗi; 40 quái/200 đạn P95 4,76 ms. [Phân tích asset](ASSETS_ThirdParty.md), [kiểm chứng](ARENA_CONTENT_TESTS.md).
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
- [ ] **T-51** Cân bằng đường cong độ khó qua 15 đợt — **@Kiet**
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

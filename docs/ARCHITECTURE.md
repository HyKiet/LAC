# Kiến trúc hệ thống — LẠC

Tài liệu tham chiếu kỹ thuật. Xác định các module tồn tại trong hệ thống, trách nhiệm của từng module và quan hệ gọi giữa chúng.

Đọc [CLAUDE.md](../CLAUDE.md) trước tài liệu này.

---

## 1. Sơ đồ tổng thể

```mermaid
graph TD
    NM[NetworkManagerLAC<br/>Mirror + Steamworks] --> RM[RunManager<br/>vòng đời một ván]
    RM --> WM[WaveManager<br/>quản lý đợt]
    RM --> CM[CardManager<br/>chọn thẻ giữa các đợt]
    RM --> RNG[RunRandom<br/>bộ sinh có seed]

    WM --> DIR[AIDirector<br/>chơi đơn và co-op]
    WM --> FIX[FixedWaveTable<br/>nhóm đối chứng]
    DIR --> AGG[ContextAggregator<br/>hợp thành ngữ cảnh N người chơi]
    DIR --> SAFE[SafetyConstraints<br/>chốt theo người yếu nhất]
    WM --> SPW[EnemySpawner]

    SPW --> POOL[ObjectPool]
    SPW --> ENE[Enemy FSM]

    PC[PlayerController] --> MOV[PlayerMovement + PlayerDash]
    PC --> WEAP[WeaponAuto<br/>khai hoả tự động]
    WEAP --> PROJ[Projectile<br/>cục bộ, không đồng bộ]
    WEAP --> SFX[SoundWaveVFX<br/>sóng âm Đông Sơn]

    ENE -->|khi chết| SOUL[SoulPickup]
    SOUL --> DRUM[DongSonDrum<br/>trạng thái do host giữ]
    PC -->|dash vào| DRUM
    DRUM -->|RPC| SHOCK[Shockwave<br/>xoá đạn, đẩy lùi, choáng]

    CM --> EVO[CardEvolution<br/>8 công thức]
    CM --> STATS[PlayerStats]
    STATS --> WEAP

    DIR --> TEL[Telemetry<br/>CSV cho khoá luận]
    RM --> TEL
```

---

## 2. Đặc tả module

### 2.1 Core — hạ tầng nền tảng

| Lớp | Trách nhiệm | Ghi chú |
|---|---|---|
| `RunManager` | Vòng đời một ván: khởi tạo, điều phối 16 đợt, xác định điều kiện thắng thua | Chỉ host điều khiển tiến trình |
| `WaveManager` | Yêu cầu đặc tả đợt, phát lệnh sinh quái, theo dõi số quái còn sống, kết thúc đợt | Chỉ host |
| `RunRandom` | Bao bọc `System.Random` với seed của ván. **Nguồn ngẫu nhiên duy nhất trong gameplay** | Seed được đồng bộ một lần khi khởi tạo ván |
| `ObjectPool<T>` | Pool dùng chung cho đạn, quái, hiệu ứng, số sát thương | Bắt buộc thay thế cho `Instantiate` trong gameplay |
| `GameEvents` | Event bus tĩnh: `OnEnemyDied`, `OnWaveCleared`, `OnCardPicked`, `OnPlayerHit` | Giảm phụ thuộc chéo giữa các module |

**Trình tự một đợt:**

```
WaveManager.StartWave(n)
  → Lấy WaveSpec (AIDirector, hoặc FixedWaveTable với nhóm đối chứng)
  → EnemySpawner.Spawn(spec, RunRandom)
  → Chờ số quái còn sống bằng 0
  → GameEvents.OnWaveCleared
  → CardManager.OfferCards(3)
  → Chờ toàn bộ người chơi hoàn tất lựa chọn
  → StartWave(n + 1)
```

### 2.2 Player

| Lớp | Trách nhiệm |
|---|---|
| `PlayerController` | Điều phối các thành phần, giữ tham chiếu `CharacterData` |
| `PlayerMovement` | Di chuyển 8 hướng; client dự đoán cục bộ nhân vật của mình |
| `PlayerDash` | Dash, i-frame, thời gian hồi. Đồng thời là tín hiệu kích hoạt Trống Đồng |
| `PlayerHealth` | Quản lý máu. **Chỉ host được phép thay đổi.** Client nhận qua `SyncVar` và RPC biểu diễn |
| `PlayerStats` | Chỉ số hợp thành sau khi áp toàn bộ thẻ. Vũ khí đọc dữ liệu từ đây |

`CharacterData` (ScriptableObject) chứa: máu gốc, tốc độ, prefab vũ khí, sprite, đặc tính riêng.

### 2.3 Combat

| Lớp | Trách nhiệm |
|---|---|
| `WeaponAuto` | Đếm chu kỳ bắn, chọn mục tiêu, phát đạn. Đọc chỉ số từ `PlayerStats` |
| `Projectile` | Quỹ đạo, va chạm, xuyên thấu và nảy. **Không mang `NetworkIdentity`** |
| `DamageSystem` | Điểm vào duy nhất cho mọi sát thương. Chỉ thực thi trên host |
| `ITargetable` | Interface cho mọi đối tượng có thể nhận sát thương |

> Toàn bộ sát thương đi qua `DamageSystem.Apply()`. Không lớp nào được tự thay đổi máu. Đây là cơ chế bảo đảm tính nhất quán trạng thái trong co-op.

### 2.4 Enemies

Máy trạng thái ba trạng thái: `Spawning` → `Chasing` → `Attacking`. `EnemyData` (ScriptableObject) chứa máu, tốc độ, sát thương, hành vi và sprite.

Hai máy cùng sinh quái từ một seed nên mô phỏng song song. Host gửi snapshot vị trí 2 lần/giây để hiệu chỉnh sai lệch tích luỹ. Sự kiện chết do host phát qua RPC.

### 2.5 Cards

| Lớp | Trách nhiệm |
|---|---|
| `CardPool` | 32 thẻ nền, lọc theo nhân vật, bốc 3 thẻ bằng `RunRandom` |
| `CardEffect` | Áp hiệu ứng lên `PlayerStats` |
| `CardEvolution` | Kiểm tra 8 công thức sau mỗi lần nhận thẻ |
| `CardPickUI` | Giao diện chọn thẻ, giới hạn 10 giây, 2 lượt đổi |

Trong co-op, mỗi người chơi chọn thẻ độc lập nhưng đợt kế tiếp chỉ khởi động khi toàn bộ người chơi hoàn tất. Hệ thống đồng bộ định danh thẻ; mỗi máy tự áp dụng hiệu ứng.

### 2.6 Drum — Trống Đồng

| Lớp | Trách nhiệm |
|---|---|
| `DongSonDrum` | Đối tượng cố định tại tâm đấu trường. Giữ thời gian hồi dùng chung dưới dạng `SyncVar` do host quản lý |
| `DrumShockwave` | Thi hành hiệu ứng: xoá đạn địch, đẩy lùi, gây choáng 1 giây |
| `SoulPickup` | Hồn rơi ra khi quái chết, tự hút về người chơi, nạp năng lượng cho trống |

**Luồng kích hoạt:**

```
Client dash chạm trống
  → CmdTryActivate()
  → Host kiểm tra thời gian hồi
  → Nếu hợp lệ: host thi hành hiệu ứng, phát RpcPlayShockwave() cho phần biểu diễn
```

Client không tự xác định trạng thái sẵn sàng của trống; client chỉ hiển thị giá trị `SyncVar` nhận từ host.

### 2.7 Director — AI Đạo Diễn

Hoạt động ở **cả chế độ chơi đơn và co-op**. `FixedWaveTable` được giữ lại làm bảng đợt của nhóm đối chứng trong thực nghiệm khoá luận và làm phương án dự phòng khi đạo diễn gặp lỗi.

| Lớp | Trách nhiệm |
|---|---|
| `AIDirector` | Thuật toán LinUCB — contextual bandit. Chọn `WaveSpec` cho đợt kế tiếp |
| `ContextVector` | Véc-tơ đặc trưng đầu vào của LinUCB |
| `ContextAggregator` | Hợp thành véc-tơ ngữ cảnh từ 1 hoặc 2 người chơi về cùng một dạng |
| `WaveSpec` | Đầu ra: số lượng quái, chủng loại, **hướng sinh**, nhịp độ |
| `SafetyConstraints` | Chặn các cấu hình đợt vượt ngưỡng an toàn |
| `Telemetry` | Ghi dữ liệu CSV phục vụ phần đánh giá của khoá luận |

#### Véc-tơ ngữ cảnh

Đạo diễn không nhận trực tiếp trạng thái của từng người chơi. `ContextAggregator` chuyển trạng thái của **N người chơi** về một véc-tơ có số chiều cố định, nhờ đó một mô hình LinUCB duy nhất phục vụ được cả hai chế độ và không tồn tại hai nhánh mã.

| Thành phần | Chơi đơn | Co-op |
|---|---|---|
| `healthMean` | Máu còn lại của người chơi | Trung bình máu còn lại |
| `healthMin` | Trùng với `healthMean` | **Máu còn lại của người yếu nhất** |
| `healthSpread` | 0 | Chênh lệch tổn thất máu giữa hai người |
| `clearTime` | Thời gian dọn đợt | Thời gian dọn đợt — vốn đã dùng chung |
| `hitsTaken` | Số lần trúng đòn | Tổng số lần trúng đòn, chuẩn hoá theo số người chơi |
| `dashRate` | Tần suất dash | Trung bình tần suất dash |
| `partyArchetype` | Định danh nhân vật | **Nhóm tổ hợp đội hình:** cận chiến kép · tầm xa kép · hỗn hợp |
| `buildType` | Phân loại build | Phân loại build trội của đội |

> Số chiều của véc-tơ được giữ cố định và ở mức nhỏ. Với cỡ mẫu thực nghiệm 30 người, việc nối hai véc-tơ build riêng biệt sẽ làm số chiều tăng gấp đôi và LinUCB không hội tụ kịp trong 16 đợt. `partyArchetype` chỉ có ba giá trị chính vì lý do này.

#### Hàm thưởng

Chơi đơn giữ nguyên đặc tả gốc: tổn thất máu mỗi đợt hướng về khoảng mục tiêu.

Co-op bổ sung **số hạng công bằng**. Một đợt quái đẩy toàn bộ áp lực về một người chơi trong khi người còn lại không chịu rủi ro là một đợt kém chất lượng, kể cả khi tổn thất máu trung bình nằm đúng khoảng mục tiêu:

```
reward = w1 · f(healthLostMean)  −  w2 · healthSpread
```

Trong đó `f` là hàm khoảng mục tiêu của đặc tả gốc, `healthSpread` là chênh lệch tổn thất giữa hai người chơi. Số hạng thứ hai buộc đạo diễn tạo ra các đợt gây áp lực lên cả hai người.

#### Tầng an toàn

`SafetyConstraints` trong co-op **đánh giá theo `healthMin`, không theo `healthMean`**. Khi người chơi yếu nhất xuống dưới ngưỡng an toàn, đạo diễn giảm áp lực bất kể trung bình của cả đội đang tốt. Đây là điểm khác biệt then chốt: trung bình che giấu trường hợp một người sắp bị hạ gục.

#### Đòn bẩy điều tiết trong co-op

Ở chế độ hai người chơi, **hướng sinh quái là công cụ điều tiết mạnh hơn số lượng quái**. Đạo diễn có hai hành vi không tồn tại trong chơi đơn:

- *Chia cắt* — sinh quái ở hai hướng đối nhau, buộc hai người chơi tách khỏi nhau
- *Dồn ép* — sinh quái theo một hướng duy nhất, buộc hai người chơi phối hợp cùng một trục phòng thủ

Hai hành vi này thay đổi độ khó mà không tăng lượng máu hay số lượng quái, phù hợp với nguyên tắc điều tiết bất đối xứng.

**Hai điều chỉnh so với GDD:**

*Điều tiết bất đối xứng.* Khi người chơi gặp khó, đạo diễn được phép giảm áp lực. Khi người chơi đang mạnh, đạo diễn thay đổi thành phần và hướng sinh quái thay vì tăng số lượng hoặc lượng máu. Cơ sở: siết tổn thất máu về khoảng 15–25% ở cả hai chiều đồng nghĩa với việc trừng phạt người chơi vì xây dựng build hiệu quả, làm triệt tiêu cảm giác tưởng thưởng — yếu tố quyết định giá trị thương mại của thể loại.

*Hiển thị hoạt động của đạo diễn.* HUD thông báo hành vi hiện tại của hệ thống, ví dụ *"Đạo Diễn: tăng áp lực từ hướng Bắc"*. Một hệ thống AI không quan sát được thì không tồn tại đối với người chơi lẫn hội đồng đánh giá.

#### Thiết kế thực nghiệm

Đạo diễn hoạt động ở cả hai chế độ, nhưng **thực nghiệm định lượng của khoá luận chạy ở chế độ chơi đơn**: 15 người dùng đạo diễn, 15 người dùng `FixedWaveTable`. Chế độ co-op thu thập mẫu quan sát bổ sung mang tính định tính.

Cơ sở của việc tách rời này: với cỡ mẫu 30, hai người chơi khác nhân vật, khác build và khác trình độ tạo ra biến số không kiểm soát được, làm kết luận thống kê mất giá trị. Yêu cầu của giảng viên hướng dẫn là hệ thống phải **hoạt động** trong co-op — đây là yêu cầu về phạm vi chức năng, không phải về thiết kế thí nghiệm.

Trong chế độ co-op, đạo diễn ghi telemetry đầy đủ. Nếu dữ liệu quan sát đủ chất lượng, phần này trở thành một mục thảo luận bổ sung của khoá luận.

### 2.8 Net

Xem [CLAUDE.md](../CLAUDE.md) mục 3.2 để biết bảng đồng bộ đầy đủ. Các lớp chính: `NetworkManagerLAC`, `SteamLobby`, `NetPlayerSpawner`, `RunSync`.

### 2.9 VFX

| Lớp | Trách nhiệm |
|---|---|
| `SoundWaveVFX` | Vòng tròn đồng tâm lan toả mang hoa văn Đông Sơn, chế độ additive |
| `HitFeedback` | Hit-stop, nháy trắng, đẩy lùi, số sát thương |
| `CameraShake` | Rung màn hình theo cường độ va chạm |

**Ràng buộc đọc hiểu thị giác:** hiệu ứng của người chơi dùng alpha thấp, chế độ additive, sorting layer thấp. Đòn tấn công của địch vẽ đặc, dùng màu dành riêng, sorting layer cao nhất. Thứ tự này không được đảo ngược trong bất kỳ trường hợp nào.

---

## 3. Dữ liệu cấu hình

Toàn bộ thông số nằm tại `Assets/_LAC/Data/` dưới dạng ScriptableObject. Không hard-code trong C#.

```
Data/
├── Characters/   ThachSanh.asset · Giong.asset · Tam.asset
├── Cards/        32 thẻ nền + 8 thẻ tiến hoá
├── Enemies/      CoHon · MaTroi · BuNhin · MaDa · QuyNho · ChanTinh
└── Waves/        FixedWaveTable.asset — co-op và nhóm đối chứng
```

Cách tổ chức này cho phép điều chỉnh cân bằng qua Inspector mà không chạm vào mã nguồn, đồng thời tránh xung đột với thành viên đang sửa code.

---

## 4. Scene

| Scene | Nội dung |
|---|---|
| `Boot` | Khởi tạo hệ thống, chuyển sang Menu |
| `Menu` | Menu chính, chọn nhân vật, lobby Steam |
| `Arena` | Đấu trường thi đấu |

Cả ba bối cảnh (Sân Đình, Ruộng Lúa, Âm Phủ) dùng chung một scene `Arena`; sự khác biệt được tạo bằng cách thay tilemap và bảng màu, không tạo scene riêng.

> Tệp `.unity` là nguồn xung đột nghiêm trọng nhất trong quản lý phiên bản. Quy định: tại một thời điểm chỉ một thành viên được chỉnh sửa scene; mọi thay đổi khác thực hiện trong prefab. Chi tiết tại [CLAUDE.md](../CLAUDE.md) mục 6.2.

---

## 5. Trình tự khởi tạo

```
1. NetworkManagerLAC      Host khởi động, client kết nối
2. RunSync                Đồng bộ seed và nhân vật đã chọn
3. RunManager.StartRun()   Host phát lệnh
4. NetPlayerSpawner       Sinh 1–2 nhân vật
5. DongSonDrum            Đặt tại tâm đấu trường, trạng thái sẵn sàng
6. WaveManager.StartWave(1)
```

Sai lệch trình tự này là nguyên nhân của lỗi client vào được ván nhưng không có nhân vật.

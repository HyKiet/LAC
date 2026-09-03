# 02 — Toàn dự án LẠC hoạt động như thế nào?

> Bài 01 dạy cách đọc từng câu code. Bài này dạy cách nối nhiều script thành một trò chơi.
>
> Chưa cần nhớ tên mọi class. Hãy nhớ “ai chịu trách nhiệm việc gì” và “một hành động đi qua những đâu”.

## 1. Kiến trúc là chia việc

Một tiệm ăn không để một người vừa nhận đơn, nấu, thu tiền và giao món. Game cũng vậy.

```text
Người chơi bấm phím
→ bộ phận Input ghi nhận
→ bộ phận Movement di chuyển
→ bộ phận Camera đi theo
→ mạng báo vị trí cho máy còn lại
```

“Kiến trúc” chỉ là cách chia trách nhiệm và quy định các bộ phận nói chuyện với nhau.

Nếu một script làm tất cả, lúc sửa Dash có thể vô tình làm hỏng máu hoặc camera. LẠC chia nhỏ để mỗi script có một lý do chính để thay đổi.

---

## 2. Game LẠC là gì?

LẠC là game 2D nhìn từ trên xuống:

- 1–2 người chơi trong đấu trường;
- người chơi di chuyển và lướt;
- vũ khí tự tìm mục tiêu và tự đánh;
- quái xuất hiện theo đợt, đuổi và chạm người chơi;
- host làm trọng tài cho máu, sát thương và cái chết;
- hiệu ứng hình ảnh chạy cục bộ để nhẹ mạng.

Chơi một mình vẫn chạy theo chế độ host có một người. Dự án không có một bộ code chơi đơn khác.

---

## 3. Bản đồ thư mục

Mã do dự án tự viết nằm trong `Assets/_LAC/`.

```text
Assets/_LAC/
├── Scripts/
│   ├── Core/       luật chung của một ván
│   ├── Player/     input, di chuyển, Dash, máu, nhân vật
│   ├── Enemies/    dữ liệu, trạng thái, sinh và quản lý quái
│   ├── Combat/     vũ khí, đạn, sát thương
│   ├── Net/        tạo player và kết nối Mirror
│   ├── UI/         thanh máu
│   ├── Utils/      camera
│   └── VFX/        rung, nháy, số damage, sóng âm
├── Data/           các file thông số .asset
├── Prefabs/        mẫu object lắp sẵn
├── Scenes/         sân khấu
└── Art/Shaders/    cách GPU vẽ pixel
```

Các thư mục như `Cards`, `Director`, `Drum` và `Audio` được thiết kế cho phần tương lai nhưng hiện chưa có script first-party để học.

---

## 4. Bốn lớp cần phân biệt

### Dữ liệu

Ví dụ `CharacterData` và `EnemyData`: máu, tốc độ, sprite, tầm đánh. Đây là “con số và tài nguyên”.

### Luật

Ví dụ `Enemy.cs`: lúc nào đuổi, lúc nào đánh, trừ máu ra sao. Đây là “cách hành động”.

### Vật thể Unity

Prefab nối SpriteRenderer, Rigidbody2D, Collider2D và script lại thành một mẫu hoàn chỉnh.

### Sân khấu

Scene đặt camera, đấu trường, NetworkManager và các object cần có.

Khi game sai tốc độ, kiểm tra Data. Khi quái chọn sai hành vi, kiểm tra code. Khi biến bị null, kiểm tra prefab/scene wiring. Đừng mặc định mọi lỗi đều nằm trong thuật toán C#.

---

## 5. Những “nhân vật hậu trường”

| Bộ phận | Hình dung | Trách nhiệm |
|---|---|---|
| `RunManager` | quản lý trận đấu | bắt đầu, đổi trạng thái, kết thúc |
| `WaveManager` | quản lý từng hiệp | thời gian đợt quái |
| `NetworkManagerLAC` | lễ tân online | tạo đúng player khi kết nối |
| `PlayerInputReader` | tai nghe | ghi phím người chơi |
| `PlayerMovement` | đôi chân | biến input thành chuyển động |
| `PlayerDash` | kỹ năng né | lướt, hồi chiêu, bất tử |
| `PlayerHealth` | sổ máu | giữ HP thật do host quản lý |
| `WeaponAuto` | người bắn tự động | tìm mục tiêu và tạo đòn |
| `Projectile` | viên đạn | bay, va chạm, báo hit |
| `DamageSystem` | trọng tài sát thương | quyết định trừ máu |
| `EnemySpawner` | cổng sinh quái | lấy quái từ pool, cấp ID, đồng bộ |
| `Enemy` | một con quái | xuất hiện, đuổi, đánh, chết |
| `GameEvents` | loa phát thanh | truyền thông báo giữa hệ thống |
| `HitFeedback` | đạo diễn cảm giác | rung, flash, số damage |

Một class không nhất thiết tự làm mọi thứ liên quan tới tên nó. `Enemy` không tự trừ máu player; nó báo sự kiện để `DamageSystem` làm.

---

## 6. Luồng 1 — người chơi di chuyển

```text
Bàn phím/gamepad
→ LACControls.inputactions đặt tên hành động Move
→ PlayerInputReader nhớ hướng
→ PlayerMovement đọc hướng ở FixedUpdate
→ Rigidbody2D nhận velocity
→ Transform thay đổi vị trí
→ NetworkTransform chuyển vị trí cho máy kia
→ CameraFollow bám local player ở LateUpdate
```

Tại sao chia như vậy?

- Input không cần biết vật lý.
- Movement không cần biết phím W hay cần analog.
- Camera chỉ quan sát, không sửa vị trí player.
- Chỉ người sở hữu nhân vật được điều khiển nó.

Câu hỏi tự kiểm tra: bỏ điều kiện `isOwned` thì cả hai máy có thể cố điều khiển cùng một player.

---

## 7. Luồng 2 — người chơi lướt

```text
Người chơi bấm Dash
→ PlayerInputReader ghi “vừa bấm”
→ PlayerDash kiểm tra quyền sở hữu và hồi chiêu
→ BeginLocalDash cho nhân vật lao đi ngay
→ CmdDash gửi yêu cầu lên host
→ host mở khoảng bất tử nếu hợp lệ
→ RpcDashStarted báo máy khác vẽ động tác
→ DashAfterimage lấy ảnh mờ từ pool
```

Vì sao phải “lướt ngay” rồi mới chờ host? Nếu chờ gói tin đi lên và về, phím né sẽ có cảm giác chậm.

Vì sao bất tử vẫn do host biết? Vì host là máy quyết định sát thương. Nếu chỉ client tự nghĩ mình bất tử, host vẫn có thể trừ máu.

Đây là ví dụ một hành động có hai phần:

- cảm giác tức thời: client làm ngay;
- kết quả gameplay thật: host quyết định.

---

## 8. Luồng 3 — vũ khí bắn và quái mất máu

```text
WeaponAuto hết thời gian chờ
→ tìm quái gần nhất trong EnemyRegistry
→ tạo Projectile từ ObjectPool
→ Projectile bay và chạm quái
→ báo GameEvents.ProjectileHitEnemy
→ DamageSystem nghe sự kiện
→ host gọi EnemySpawner.DamageEnemy
→ Enemy.ApplyDamage trả lời “đã chết chưa?”
→ nếu chết: host Kill và gửi ID xuống client
→ HitFeedback tạo flash, số damage, rung
→ Projectile được trả về pool
```

Điểm khó nhưng quan trọng:

- Đạn hình ảnh có thể tồn tại ở mỗi máy, không mang `NetworkIdentity`.
- Client nhìn thấy va chạm nhưng không được tự quyết HP thật.
- Host quyết định quái chết nào, rồi báo bằng ID.

“Đồng bộ sự kiện, không đồng bộ mọi trạng thái” nghĩa là gửi tin “quái 17 chết” thay vì gửi vị trí và mọi thuộc tính của từng viên đạn mỗi frame.

---

## 9. Luồng 4 — quái chạm người chơi

```text
Enemy ở trạng thái Chasing
→ tới gần thì chuyển Attacking
→ đủ nhịp đánh thì phát EnemyTouchedPlayer
→ DamageSystem nghe
→ kiểm tra đang chạy trên host
→ kiểm tra player có đang bất tử vì Dash
→ PlayerHealth trừ HP thật
→ SyncVar chuyển HP mới tới client
→ hook HealthChanged phát thông báo
→ PlayerHud đổi số tim
→ HitFeedback rung/nháy
```

Tại sao `Enemy` không gọi thẳng `PlayerHealth`?

Vì Enemy chỉ nên biết “tôi chạm player”. Quy tắc ai được trừ máu và bất tử nằm ở DamageSystem. Sau này thêm giáp hoặc độ khó, ta sửa một nơi.

---

## 10. Luồng 5 — một con quái sống như thế nào?

```text
EnemySpawner lấy Enemy từ pool
→ Initialize gán ID, dữ liệu, vị trí và máu
→ Spawning: đứng yên để báo trước
→ Chasing: tìm player gần nhất và đuổi
→ Attacking: ở trong tầm thì đánh theo nhịp
→ Dead: tắt collider, phát event, rời Registry
→ object được trả về pool để dùng lại
```

`EnemyState` là tấm thẻ “hiện đang làm gì”. `FixedUpdate` nhìn thẻ rồi gọi đúng hàm.

Quái còn tính lực giãn cách để không dồn thành một chấm. Nó tự tính thay vì để 40 Rigidbody đẩy nhau, nhằm giữ kết quả hai máy gần giống nhau hơn.

---

## 11. Registry, pool và event khác nhau

Ba thứ này dễ lẫn:

### Registry là danh bạ

`PlayerRegistry` và `EnemyRegistry` cho hệ thống khác biết những ai đang tồn tại. Tìm player gần nhất hoặc quái theo ID tại đây.

### Pool là kho đồ

`ObjectPool` giữ object chưa dùng để lấy ra lại. Nó giải quyết chi phí tạo/huỷ liên tục.

### Event là loa

`GameEvents` thông báo một việc vừa xảy ra. Nó giải quyết việc các bộ phận cần nghe nhau mà không nối cứng.

```text
Registry: “Hiện có những ai?”
Pool:     “Có món nào rảnh để dùng?”
Event:    “Chuyện gì vừa xảy ra?”
```

---

## 12. Random có seed là gì?

Random thường giống rút số từ túi. Hai máy rút riêng có thể ra số khác nhau.

LẠC dùng `RunRandom` và `RandomStream`:

```text
cùng seed + cùng kênh + cùng thứ tự gọi
→ cùng dãy số
→ hai máy có thể sinh cùng nội dung
```

Kênh giống nhiều bộ bài riêng: random sinh quái không làm xáo trộn random thẻ.

Gameplay không dùng `UnityEngine.Random`. Ngoại lệ là trang trí thuần hình ảnh, như vị trí lệch nhẹ của số damage; hai máy nhìn khác chút cũng không đổi kết quả chơi.

---

## 13. Ba luật kiến trúc bắt buộc

### Luật 1 — một luồng chơi duy nhất

```text
Chơi một người = host + 1 client
Chơi hai người = host + 2 client
```

Không viết `if (isSinglePlayer)` để tạo đường code riêng.

### Luật 2 — gửi điều cần biết

Không biến từng viên đạn/VFX thành object mạng. Host giữ kết quả thật; client tự dựng phần nhìn và nhận các sự kiện cần thiết.

### Luật 3 — random gameplay phải tái tạo được

Dùng `LAC.Core.RunRandom`, không dùng `UnityEngine.Random` trong luật gameplay.

Ba luật này không phải kiến thức C#; chúng là quyết định riêng của dự án. Code đúng cú pháp nhưng phá ba luật vẫn là code sai đối với LẠC.

---

## 14. Host, client, local và owned

| Từ | Nghĩa dễ hiểu |
|---|---|
| host/server | máy làm trọng tài |
| client | máy của người tham gia |
| local player | player đại diện cho mình trên máy này |
| owned | object mà kết nối này có quyền gửi điều khiển |
| remote player | player của người khác ta chỉ quan sát |

Host có thể vừa là server vừa là client. Vì vậy đừng nghĩ “host” và “người chơi” luôn là hai object tách biệt.

Khi đọc code mạng, ghi bên cạnh mỗi method:

```text
Chạy ở đâu?
Ai được gọi?
Ai quyết định kết quả?
Máy khác biết kết quả bằng cách nào?
```

---

## 15. Vòng đời và việc dọn dẹp

Một object thường:

```text
được tạo/lấy từ pool
→ được bật
→ đăng ký event/registry
→ chạy nhiều frame
→ bị tắt/trả pool
→ gỡ event/registry và xoá dữ liệu cũ
```

Nếu có `+=` mà không có `-=`, hoặc có `Register` mà không `Unregister`, danh sách có thể giữ “bóng ma” của object cũ.

Khi review một component, luôn tìm cặp:

- `OnEnable` ↔ `OnDisable`;
- `Register` ↔ `Unregister`;
- `Get` ↔ `Release`;
- tạo connection ↔ ngắt connection.

---

## 16. Cách lần theo code mà không bị ngợp

Đừng mở 10 file cùng lúc. Chọn một câu hỏi rất cụ thể, ví dụ “bấm Dash thì chuyện gì xảy ra?”

1. Bắt đầu tại hành động nhìn thấy.
2. Tìm method xử lý nó.
3. Ghi những method/class được gọi tiếp.
4. Mỗi lần chỉ nhảy sang một nơi.
5. Vẽ mũi tên.
6. Dừng khi đã tới kết quả nhìn thấy hoặc dữ liệu cuối.

Mẫu:

```text
Sự kiện bắt đầu:
File/method đầu:
→ gọi:
→ phát event:
→ ai nghe:
Dữ liệu nào đổi:
Thứ người chơi nhìn thấy:
Máy nào có quyền quyết định:
```

---

## 17. Những phần hiện có và chưa có

Hiện code first-party đã có nền:

- vòng đời run/wave;
- input, movement, Dash, health;
- vũ khí tự động, projectile, damage;
- quái, spawn, registry và snapshot;
- HUD máu, camera và VFX;
- Mirror NetworkManager;
- pool và random có seed.

Các hệ thống lớn như thẻ nâng cấp hoàn chỉnh, Hồn, Trống Đồng, AI Director, Steam lobby/save/audio vẫn là phần tiếp theo của dự án. Tài liệu thiết kế nói về chúng không có nghĩa code đã tồn tại.

---

## 18. Bài tự kiểm tra

Không nhìn bài, tự vẽ năm luồng:

1. WASD → player di chuyển.
2. Space → Dash.
3. Projectile → quái chết.
4. Quái chạm → HUD mất máu.
5. Spawner → quái vào pool trở lại.

Với mỗi luồng, trả lời:

- file nào bắt đầu;
- dữ liệu nào đổi;
- host hay client quyết định;
- event nào được phát;
- object nào cần cleanup.

Nếu chưa trả lời được, mở đúng một luồng và theo mũi tên lại. Không cần học thuộc toàn sơ đồ.

## Tóm tắt bằng một câu

LẠC là nhiều component nhỏ phối hợp: input tạo ý định, gameplay xử lý luật, host giữ kết quả thật, event chuyển thông báo, registry tìm object, pool tái sử dụng object, còn UI/VFX biến kết quả thành thứ người chơi nhìn và cảm nhận được.

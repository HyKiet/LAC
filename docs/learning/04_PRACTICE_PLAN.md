# 04 — Lộ trình từ đọc code đến tự viết game

> Mục tiêu không phải “đọc hết tài liệu”. Mục tiêu là tự nghĩ, tự viết, tự kiểm tra và tự giải thích được một tính năng game nhỏ.
>
> Thời gian dưới đây là gợi ý, không phải hạn chót. Nếu một tuần cần ba tuần thì vẫn hoàn toàn bình thường.

## 1. Ba năng lực phải luyện cùng lúc

### Đọc

Nhìn code người khác và kể được dữ liệu đi đâu.

### Viết

Biến một yêu cầu nhỏ thành biến, điều kiện và hàm.

### Debug

So sánh điều dự đoán với điều thật sự xảy ra, rồi tìm dòng đầu tiên khác nhau.

Chỉ đọc sẽ không tự viết được. Chỉ chép code sẽ không debug được. Vì vậy mỗi buổi phải có cả dự đoán, gõ code và quan sát.

---

## 2. Luật học để không phụ thuộc AI lần nữa

### Luật 15 phút

Trước khi hỏi AI, tự làm 15 phút:

1. Viết đầu vào.
2. Viết kết quả mong muốn.
3. Viết thử bằng lời Việt.
4. Chuyển từng câu thành code.
5. Ghi lỗi hoặc chỗ kẹt cụ thể.

Sau 15 phút vẫn kẹt thì hỏi. Đây không phải cấm AI; đây là buộc não mình tập nâng tạ trước khi có người đỡ.

### Luật dự đoán trước Play

Trước mỗi lần bấm Play, viết một câu:

```text
Tôi dự đoán khi bấm Space lần thứ hai sau 0.5 giây,
Console sẽ không in Dash vì hồi chiêu là 1 giây.
```

Nếu sai, đó là dữ liệu học tập chứ không phải thất bại.

### Luật tự gõ

Không copy cả khối code trong giai đoạn nền tảng. Nhìn mẫu, đóng mẫu, rồi tự gõ. Cú pháp gõ sai và tự sửa giúp não nhớ cấu trúc.

### Luật giải thích

Không giữ đoạn code nào trong bài tập nếu chưa nói được:

- biến nào là trạng thái;
- ai gọi method;
- điều kiện nào chặn;
- kết quả nhìn thấy;
- lỗi nào có thể xảy ra.

---

## 3. Chuẩn bị nơi luyện an toàn

Không dùng scene chính `Arena.unity` làm vở nháp.

Tạo một project Unity 2D nhỏ riêng hoặc một scene học tập không được đưa vào gameplay chính. Mỗi bài chỉ cần sprite hình vuông, Console và vài component cơ bản.

Quy tắc:

- không sửa package bên thứ ba;
- không đổi con số cân bằng LẠC chỉ để thử cú pháp;
- không chỉnh file scene/prefab YAML bằng tay;
- trước khi sửa code dự án, xem `git diff` để biết mình đang thay gì;
- không đánh dấu task dự án hoàn thành chỉ vì đã làm bài học.

---

# Chặng 1 — tự viết C# rất nhỏ

## Mục tiêu

Tự dùng được biến, `if`, hàm, vòng lặp và List mà không nhìn đáp án.

## Buổi 1 — máy tính máu

Viết một script Console hoặc MonoBehaviour:

- bắt đầu 5 HP;
- `TakeDamage(2)` còn 3;
- sát thương âm bị đổi thành 0;
- HP không xuống dưới 0;
- `Die()` chỉ chạy đúng một lần.

Trước code, viết tiếng Việt:

```text
Khi nhận sát thương:
1. Nếu đã chết thì dừng.
2. Chặn sát thương âm.
3. Trừ máu.
4. Nếu máu hết thì chết.
```

## Buổi 2 — xu và cửa hàng

- có 10 xu;
- `TryBuy(price)` trả `true` nếu đủ tiền;
- đủ thì trừ tiền;
- thiếu thì giữ nguyên;
- giá âm không hợp lệ.

Bài này luyện đầu vào, kết quả trả về và thứ tự kiểm tra trước khi thay đổi dữ liệu.

## Buổi 3 — hồi chiêu

- bấm Space in “Dash”;
- phải chờ 1 giây;
- hiển thị số giây còn lại;
- thử bấm liên tục.

Dùng `Time.time` và `_readyAt`. Tự giải thích vì sao điều kiện là `Time.time < _readyAt`.

## Buổi 4 — danh sách quái giả

Tạo List số máu: `[3, 7, 2, 10]`.

- in từng số;
- đếm bao nhiêu quái còn sống;
- tìm máu nhỏ nhất;
- trừ 1 cho từng quái.

Không cần GameObject. Mục tiêu chỉ là vòng lặp và chỉ số.

## Bài kiểm tra chặng 1

Tự viết “rương khoá”:

- biến `hasKey`;
- `TryOpen()`;
- không có chìa thì báo khoá;
- có chìa thì mở;
- đã mở rồi thì không mở lần hai.

Không xem tài liệu trong 20 phút đầu.

---

# Chặng 2 — Unity một người chơi

## Mục tiêu

Tạo một mini game: hình vuông di chuyển, Dash, bị quái đuổi và mất máu.

## Buổi 5 — GameObject và component

Tạo Player gồm:

- Transform;
- SpriteRenderer;
- Rigidbody2D;
- Collider2D;
- script `PracticePlayerMovement`.

Trong Play Mode, thay tốc độ Inspector và giải thích vì sao `[SerializeField] private` phù hợp.

## Buổi 6 — WASD

- đọc input;
- tạo Vector2 hướng;
- chuẩn hoá;
- di chuyển theo tốc độ mỗi giây;
- thử 30 FPS và 120 FPS.

Quan sát để hiểu `deltaTime`, không chỉ thuộc định nghĩa.

## Buổi 7 — giới hạn sân

Tự viết phiên bản đơn giản:

```csharp
float clampedX = Mathf.Clamp(position.x, minX, maxX);
float clampedY = Mathf.Clamp(position.y, minY, maxY);
```

Sau đó mới mở `ArenaBounds.cs` và so sánh. Ghi ba điểm code dự án xử lý kỹ hơn code tập.

## Buổi 8 — Dash offline

Chưa dùng mạng:

- bấm Space;
- lao theo hướng đang nhìn;
- có cooldown;
- không Dash chồng;
- đổi màu khi đang Dash.

Tách thành các hàm `CanDash`, `BeginDash` và `EndDash`. Nếu một hàm dài quá 20 dòng, thử kể xem nó đang làm mấy việc.

## Buổi 9 — quái đuổi

Tạo một Enemy đơn giản:

- biết Transform player;
- tính `toPlayer = playerPosition - enemyPosition`;
- dùng `normalized`;
- tới gần thì dừng.

Chưa cần pool, event hoặc mạng.

## Buổi 10 — máu và va chạm

- quái chạm player thì gọi `TakeDamage`;
- player nháy màu;
- hết máu thì dừng điều khiển;
- UI Text hiển thị HP.

Ban đầu có thể gọi trực tiếp. Sau khi chạy đúng mới thử tách event để hiểu event giải quyết vấn đề gì.

## Sản phẩm cuối chặng 2

Một scene chơi được 30 giây:

```text
di chuyển → Dash → né quái → chạm thì mất máu → hết máu game over
```

Bạn phải quay video ngắn và tự nói:

- có những component nào;
- dữ liệu HP nằm ở đâu;
- Update và FixedUpdate làm gì;
- một bug đã gặp và cách tìm ra.

---

# Chặng 3 — đọc LẠC theo lát nhỏ

## Mục tiêu

Không còn nhìn một file 200 dòng như “bức tường”. Biết chia nó thành nhóm trách nhiệm.

## Buổi 11 — file cực nhỏ

Đọc:

1. `RunState.cs`
2. `EnemyState.cs`
3. `WeaponShape.cs`
4. `IPoolable.cs`

Với mỗi file, tự viết một câu “nếu bỏ file này thì code nào mất từ vựng/cam kết?”

## Buổi 12 — Data

Đọc `CharacterData.cs`, `EnemyData.cs` và mở các file `.asset` trong Inspector.

Thử thay một giá trị trong bản sao test, dự đoán rồi Play. Hoàn tác sau thí nghiệm; không biến thử nghiệm thành balance chính thức.

## Buổi 13 — input đến movement

Vẽ:

```text
Input Actions
→ PlayerInputReader
→ PlayerMovement
→ Rigidbody2D
→ CameraFollow
```

Mỗi mũi tên ghi tên property hoặc method thật truyền dữ liệu.

## Buổi 14 — máu đến HUD

Vẽ:

```text
Enemy event
→ DamageSystem
→ PlayerHealth
→ HealthChanged
→ PlayerHud và HitFeedback
```

Tự tìm cửa chặn khiến client không tự trừ máu.

## Buổi 15–17 — `Enemy.cs`, mỗi buổi một phần

- buổi 15: Initialize, Spawned, Despawned, Kill;
- buổi 16: state machine, Chase, Attack;
- buổi 17: Step, Separation, Snapshot.

Không đọc cả file trong một buổi. Với mỗi method, điền mẫu sáu câu ở bài 01.

## Buổi 18–19 — `PlayerDash.cs`

- buổi 18: Dash cục bộ và vật lý;
- buổi 19: Command, RPC và bất tử host.

Vẽ hai dòng thời gian song song cho host và client. Chỉ khi hình dung được hai máy mới đọc chi tiết bù độ trễ.

---

# Chặng 4 — học cấu trúc bằng cách tự xây bản nhỏ

## Mục tiêu

Tự viết phiên bản đơn giản trước, rồi so sánh với phiên bản production.

## Bài Pool mini

Tạo pool cho 10 hình vuông:

- Get lấy object tắt;
- Release tắt và cất;
- reset màu/vị trí;
- log nếu Release hai lần.

Sau đó đọc `ObjectPool.cs` và liệt kê những trường hợp dự án bảo vệ thêm.

## Bài Event mini

Tạo:

- Enemy phát `Damaged`;
- UI nghe để tăng bộ đếm;
- VFX nghe để đổi màu;
- OnDisable gỡ cả hai.

Thử cố tình bỏ `-=`, bật/tắt object nhiều lần và quan sát lỗi để hiểu cleanup bằng trải nghiệm.

## Bài Registry mini

Tạo ba Enemy, đăng ký vào List, tìm con gần player nhất. Sau đó thêm Dictionary theo ID và giải thích hai cấu trúc phục vụ hai câu hỏi khác nhau.

## Bài state machine mini

Enemy có ba trạng thái:

- Idle 1 giây;
- Chase tới gần;
- Attack theo cooldown.

Vẽ sơ đồ chuyển trạng thái trước khi code.

---

# Chặng 5 — multiplayer sau cùng

## Điều kiện bắt đầu

Chỉ bắt đầu khi bạn tự viết được mini game offline ở chặng 2 và kể được luồng damage ở LẠC. Mạng nhân đôi số nơi phải suy nghĩ; học quá sớm dễ biến mọi thứ thành phép thuật.

## Thí nghiệm 1 — hai cuốn sổ

Chạy host và client, log:

```text
name
isServer
isClient
isLocalPlayer
isOwned
```

Lập bảng object nào thấy giá trị nào. Không học thuộc định nghĩa suông.

## Thí nghiệm 2 — Command

Client bấm nút gửi yêu cầu tăng một bộ đếm. Host kiểm tra rồi đổi. Quan sát thời điểm log ở hai máy.

## Thí nghiệm 3 — SyncVar

Host đổi HP, client chỉ hiển thị hook cũ/mới. Thử cho client tự đổi và quan sát vì sao đó không phải nguồn thật.

## Thí nghiệm 4 — RPC

Host phát một hiệu ứng màu một lần. Phân biệt sự kiện “vừa xảy ra” với trạng thái “đang là bao nhiêu”.

## Thí nghiệm 5 — độ trễ

Bật mô phỏng khoảng 100 ms:

- Dash local ngay;
- Command tới host muộn;
- RPC tới máy kia muộn;
- vẽ timeline bằng mili giây quan sát được.

Kết quả localhost không chứng minh game sẽ mượt qua Internet.

---

# Chặng 6 — tự nhận thay đổi nhỏ trong dự án

Chọn việc có phạm vi nhỏ, quan sát được, không đổi balance và không cấu trúc lại dự án.

Ví dụ học tập phù hợp:

- thêm Gizmo hiển thị attack range trong Editor;
- test `ArenaBounds.Clamp`;
- test cùng seed cho cùng dãy `RandomStream`;
- debug overlay cho state/wave/enemy count trong development build.

Trước khi code, viết mini-spec:

```text
Mục tiêu:
Người chơi/lập trình viên nhìn thấy gì:
Không làm:
Input:
Output:
Dữ liệu do ai sở hữu:
Object/event nào cần cleanup:
Cách kiểm tra:
```

Sau khi code:

1. đọc từng phần diff;
2. tự giải thích vì sao từng thay đổi tồn tại;
3. chạy test/build phù hợp;
4. Play Mode đúng luồng;
5. ghi điều đã kiểm tra và điều chưa kiểm tra.

---

## 4. Cách dùng AI như gia sư

### Câu hỏi tốt

```text
Tôi nghĩ đoạn này làm A vì dòng B.
Tôi chưa hiểu tại sao có điều kiện C.
Hãy chỉ ra chỗ suy luận sai, cho một ví dụ nhỏ và 3 câu kiểm tra.
Đừng viết lời giải hoàn chỉnh.
```

```text
Đây là code tôi tự viết và lỗi Console.
Tôi dự đoán _rigidbody bị null vì Inspector chưa nối.
Hãy hướng dẫn tôi kiểm chứng từng bước, chưa sửa code hộ.
```

### Câu hỏi làm tăng phụ thuộc

```text
Viết toàn bộ hệ thống inventory cho tôi.
Fix hết code.
Tối ưu toàn dự án.
```

Không phải lúc nào nhờ AI làm code cũng sai. Nhưng sau khi AI viết, bạn phải:

- đọc diff;
- đoán hành vi trước Play;
- tự chạy kiểm tra;
- giải thích được luồng;
- có khả năng tự sửa một biến thể nhỏ.

### Ba chế độ dùng AI

1. **Gia sư:** hỏi gợi ý, ví dụ nhỏ, câu kiểm tra.
2. **Reviewer:** đưa code mình viết để AI chỉ bug/rủi ro.
3. **Người triển khai:** chỉ dùng khi phạm vi rõ và bạn vẫn review/verify được.

Trong giai đoạn học, ưu tiên 1 rồi 2.

---

## 5. Nhật ký học 5 phút

Cuối buổi ghi:

```text
Hôm nay tôi tự viết:
Tôi dự đoán:
Kết quả thật:
Tôi đã đoán sai ở:
Tôi hiểu được:
Ngày mai tôi sẽ tự làm:
```

Sau vài chục buổi, nhật ký chứng minh năng lực tốt hơn số giờ xem tutorial.

---

## 6. Bảng kiểm “tôi thật sự hiểu”

### Mức 1 — nhận ra

Nhìn lời giải và thấy hợp lý. Mức này chưa đủ.

### Mức 2 — kể lại

Đóng code và kể được luồng bằng lời.

### Mức 3 — dự đoán

Biết đổi một dòng sẽ ảnh hưởng gì trước khi Play.

### Mức 4 — tự viết lại

Tự viết phiên bản nhỏ từ trang trắng.

### Mức 5 — biến đổi

Tự thêm yêu cầu mới, ví dụ Dash có hai lần tích trữ thay vì một cooldown.

Một kiến thức chỉ thực sự thuộc về bạn từ mức 3 trở lên.

---

## 7. Các mốc để đi xin việc game programmer

### Mốc nền tảng

- tự viết movement, health, cooldown, state machine;
- hiểu Vector2, deltaTime và Rigidbody2D;
- đọc Console và tìm NullReference;
- dùng Git để xem diff và giữ thay đổi nhỏ.

### Mốc gameplay junior

- tách dữ liệu khỏi hành vi;
- dùng event, pool, registry có cleanup;
- theo một bug qua nhiều component;
- viết test cho logic thuần;
- giải thích trade-off, không chỉ nói “code chạy”.

### Mốc multiplayer của LẠC

- phân biệt host/client/owned/local;
- giải thích Command, RPC, SyncVar bằng timeline;
- biết host giữ kết quả gameplay;
- test có độ trễ, không chỉ localhost;
- biết vì sao đạn và VFX không phải NetworkObject.

### Portfolio trung thực

Khi trình bày LẠC:

- nói chính xác phần mình tự viết;
- nói phần AI hỗ trợ;
- chỉ một bug mình tự lần ra;
- trình bày một quyết định kiến trúc và đánh đổi;
- đưa bằng chứng test/đo lường;
- không nhận công của công cụ hoặc đồng đội.

Nhà tuyển dụng không cần bạn nhớ toàn API. Họ cần thấy bạn có thể suy nghĩ, đọc code, kiểm chứng và học tiếp.

---

## 8. Việc nên làm ngay bây giờ

Sau khi đọc bài này, đừng mở ngay `PlayerDash.cs` để cố hiểu hết.

1. Làm bài “máy tính máu” ở Chặng 1.
2. Tự gõ, không copy.
3. Ghi một dự đoán trước khi chạy.
4. Khi chạy đúng, thay yêu cầu: “có giáp giảm 1 damage”.
5. Tự sửa mà không xem đáp án.

Khả năng tự sửa yêu cầu nhỏ chính là cây cầu từ “tôi hiểu đoạn code này” sang “tôi có thể tự viết game”.

# 01 — C# và Unity căn bản, giải thích từ gốc

> Học bài này sau `00_START_FROM_ZERO.md`.
>
> Đừng học thuộc. Mục tiêu là nhìn code và kể được: “nó giữ dữ liệu gì, kiểm tra điều gì, rồi làm việc gì”.

## Cách học

Mỗi buổi chỉ học 1–2 mục:

1. Đọc ví dụ rồi tự đoán kết quả.
2. Gõ lại bằng tay trong một scene tập riêng.
3. Đổi một con số và quan sát.
4. Tự giải thích lại mà không nhìn tài liệu.

Chỉ đọc sẽ tạo cảm giác “đã hiểu”. Tự gõ và dự đoán mới biến nó thành kỹ năng.

---

## 1. Một dòng code là một câu

```csharp
int health = 6;
```

Đọc thành: “Tạo chiếc hộp tên `health`, chỉ chứa số nguyên, rồi bỏ số 6 vào.”

- `int`: loại dữ liệu.
- `health`: tên do ta đặt.
- `=`: lấy giá trị bên phải bỏ vào bên trái.
- `6`: giá trị.
- `;`: hết câu lệnh.

```csharp
health = health - 1;
```

Nếu máu đang là 6, máy tính vế phải thành 5 rồi cất 5 trở lại hộp `health`. Dấu `=` trong code là “gán”, không phải “hai vế bằng nhau” như toán.

### Các loại hộp thường gặp

```csharp
int health = 6;             // Số nguyên.
float speed = 5.5f;         // Số có phần lẻ.
bool isAlive = true;        // Đúng hoặc sai.
string playerName = "Tam";  // Chữ.
Vector2 position = new Vector2(2f, 3f); // Hai số x và y.
```

| Loại | Dùng cho | Ví dụ game |
|---|---|---|
| `int` | thứ đếm được | máu, xu, số quái |
| `float` | đại lượng có phần lẻ | tốc độ, giây, khoảng cách |
| `bool` | câu trả lời có/không | đang sống, đang lướt |
| `string` | chữ | tên nhân vật |
| `Vector2` | cặp số `(x, y)` | vị trí, hướng |

`Vector2(1, 0)` là sang phải, `Vector2(0, 1)` là đi lên, `Vector2(-1, 0)` là sang trái.

### Tự kiểm tra

```csharp
int coins = 3;
coins = coins + 2;
coins = coins - 1;
```

Kết quả là 4. Nếu đoán sai, diễn từng dòng bằng đồng xu thật.

---

## 2. Tính toán và đặt câu hỏi

```csharp
int total = 2 + 3;
float distance = 5f * 2f;

health <= 0   // Máu nhỏ hơn hoặc bằng 0?
coins == 10   // Xu có đúng bằng 10?
enemy != null // Biến enemy có đang trỏ tới object thật?
```

- `=`: gán giá trị.
- `==`: hỏi hai giá trị có bằng nhau.
- `!=`: hỏi hai giá trị có khác nhau.
- `&&`: và — cả hai điều kiện phải đúng.
- `||`: hoặc — chỉ cần một điều kiện đúng.
- `!`: không — đảo đúng thành sai và ngược lại.

```csharp
if (isAlive && health > 0)
{
    Move();
}
```

Đọc: “Nếu đang sống và máu lớn hơn 0 thì di chuyển.”

---

## 3. `if`, `else` và `return`

```csharp
if (health <= 0)
{
    Die();
}
else
{
    KeepPlaying();
}
```

Máy chỉ đi vào một nhánh.

```csharp
if (!isOwned) return;
```

Đọc: “Nếu nhân vật này không thuộc quyền điều khiển của máy hiện tại thì dừng hàm.” `return` là cửa thoát; máy không chạy các dòng phía dưới.

```csharp
private void TryMove()
{
    if (!isAlive) return;
    if (speed <= 0f) return;

    Move();
}
```

Khi đọc hàm, đọc các cửa chặn trước rồi mới đọc hành động chính.

---

## 4. Hàm là chiếc máy nhỏ

```csharp
private void Die()
{
    isAlive = false;
    PlayDeathEffect();
}
```

- `private`: chỉ class này được gọi.
- `void`: không gửi kết quả trở lại.
- `Die`: tên hàm.
- `()`: chỗ ghi đầu vào; đang trống.
- `{ ... }`: việc hàm làm.

Hàm có đầu vào:

```csharp
private void TakeDamage(int amount)
{
    health = health - amount;
}

TakeDamage(2);
```

Khi gọi, chiếc hộp tạm `amount` nhận số 2.

Hàm trả kết quả:

```csharp
private bool IsDead()
{
    return health <= 0;
}
```

Đoạn thật gần giống trong `Enemy.cs`:

```csharp
public bool ApplyDamage(int amount)
{
    if (!IsAlive) return false;

    _health -= Mathf.Max(amount, 0);
    return _health <= 0;
}
```

Kể lại:

1. Nhận số sát thương.
2. Quái chết rồi thì không xử lý nữa.
3. Không cho sát thương âm làm quái hồi máu.
4. Trừ máu.
5. Trả lời cú này có giết quái không.

`_health -= value` là cách viết ngắn của `_health = _health - value`.

---

## 5. Biến nhớ lâu và biến giấy nháp

```csharp
public class Player : MonoBehaviour
{
    private int _health = 6; // Sống cùng component.

    private void Move()
    {
        float distance = speed * Time.deltaTime; // Chỉ sống trong hàm.
    }
}
```

`_health` vẫn còn sau khi gọi hàm. `distance` biến mất khi ra khỏi dấu `}` của `Move`.

Quy ước dự án: biến private của class có dấu gạch dưới (`_health`); biến tạm trong hàm không có (`distance`).

---

## 6. Class, object, GameObject và component

Class là bản thiết kế:

```csharp
public class Slime
{
    public int health;
}
```

Object là món đồ làm từ bản thiết kế:

```csharp
Slime first = new Slime();
Slime second = new Slime();
first.health = 3;
second.health = 10;
```

Hai object dùng chung bản thiết kế nhưng giữ máu riêng.

Component là class gắn lên GameObject:

```csharp
public class PlayerMovement : MonoBehaviour
{
}
```

```text
GameObject Player
├── Transform       → đang ở đâu
├── SpriteRenderer  → vẽ hình gì
├── Rigidbody2D     → vật lý
├── PlayerMovement  → luật đi bộ
├── PlayerDash      → luật lướt
└── PlayerHealth    → luật máu
```

GameObject là thân robot; component là từng bộ phận.

```csharp
public sealed class Enemy : MonoBehaviour, IPoolable
```

Đọc: “Enemy là component Unity, không cho class khác kế thừa tiếp, và cam kết có đủ các hàm của `IPoolable`.”

---

## 7. `private`, `public`, property và Inspector

```csharp
private int _health;
public int Health => _health;
```

`_health` là ngăn máy nội bộ. Code khác được đọc qua cửa `Health` nhưng không thể tự gán `Health = 999`.

```csharp
[SerializeField] private float _moveSpeed = 5f;
[SerializeField] private Rigidbody2D _rigidbody;
```

`[SerializeField]` giữ biến là private nhưng cho chỉnh hoặc kéo object vào Inspector. Dòng thứ hai không tự tạo Rigidbody2D; nếu chưa nối, `_rigidbody` có thể là `null`.

### `null` là ô nối đang trống

```csharp
if (_rigidbody == null) return;
```

`NullReferenceException` thường nghĩa là code yêu cầu object làm việc nhưng biến trỏ tới object đó đang trống.

1. Mở đúng dòng Console chỉ tới.
2. Tìm biến đứng trước dấu chấm.
3. Kiểm tra nó được nối ở Inspector hay gán trong code.

---

## 8. Unity gọi code lúc nào?

| Hàm | Hình dung | Dùng cho |
|---|---|---|
| `Awake` | object vừa được dựng | lấy component, chuẩn bị |
| `OnEnable` | object vừa được bật | đăng ký event, đặt Instance |
| `Start` | trước frame đầu | khởi tạo |
| `Update` | mỗi khung hình | input, timer, VFX |
| `FixedUpdate` | mỗi nhịp vật lý | Rigidbody2D |
| `LateUpdate` | sau Update | camera bám vị trí mới |
| `OnDisable` | object vừa tắt | gỡ event, dọn trạng thái |
| `OnDestroy` | object bị huỷ | dọn lần cuối |

Ta không tự gọi các hàm này; Unity gọi đúng thời điểm. Input cần bắt theo frame nên đọc ở `Update`. Vật lý cần nhịp đều nên chạy ở `FixedUpdate`.

---

## 9. Thời gian, hướng và tốc độ

Sai:

```csharp
transform.position += Vector3.right * 5f;
```

Nó đi 5 đơn vị mỗi frame nên máy nhiều FPS chạy nhanh hơn.

Đúng:

```csharp
transform.position += Vector3.right * 5f * Time.deltaTime;
```

`5f` là tốc độ mỗi giây; `deltaTime` là phần giây đã trôi.

```csharp
Vector2 direction = new Vector2(1f, 0f);
float speed = 5f;
Vector2 velocity = direction * speed;
```

- direction: đi về đâu;
- speed: nhanh bao nhiêu;
- velocity: gồm cả hướng và tốc độ.

`.normalized` biến vector thành hướng dài 1, giúp đi chéo không nhanh hơn đi ngang.

---

## 10. Mảng, List, Dictionary và vòng lặp

```csharp
int[] damages = { 1, 2, 5 };
List<Enemy> enemies = new List<Enemy>();
Dictionary<int, Enemy> byId = new Dictionary<int, Enemy>();
```

- Mảng là dãy ô; `damages[0]` là 1 vì máy đếm từ 0.
- List là danh sách thêm bớt được; `EnemyRegistry` dùng để duyệt mọi quái.
- Dictionary là tủ tra cứu, ví dụ `ID 17 → quái A`.

```csharp
for (int i = 0; i < enemies.Count; i++)
{
    Enemy enemy = enemies[i];
    enemy.Move();
}
```

Vòng lặp bắt đầu ở ô 0, xử lý từng ô, rồi tăng `i` thêm 1.

---

## 11. `enum` và `switch`: quái đang làm gì?

```csharp
public enum EnemyState
{
    Spawning,
    Chasing,
    Attacking,
    Dead
}
```

`enum` là danh sách lựa chọn có tên. Một quái chỉ ở một trạng thái tại một lúc.

```csharp
switch (_state)
{
    case EnemyState.Spawning: WaitToAppear(); break;
    case EnemyState.Chasing:  Chase(); break;
    case EnemyState.Attacking: Attack(); break;
}
```

`switch` nhìn thẻ trạng thái rồi chuyển quái tới đúng hành vi.

---

## 12. Interface là bản cam kết

```csharp
public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}
```

Interface không viết cách làm. Nó yêu cầu object tham gia pool có hai nút: lúc lấy ra và lúc cất lại. Enemy, đạn và số sát thương làm khác nhau, nhưng kho đồ đều gọi được hai nút chung.

---

## 13. Event là loa thông báo

```text
Enemy phát: “Tôi vừa chạm player!”
├── DamageSystem nghe → xét trừ máu
├── HitFeedback nghe → rung/nháy
└── hệ thống khác có thể nghe thêm
```

```csharp
private void OnEnable()
{
    GameEvents.EnemyDied += OnEnemyDied;
}

private void OnDisable()
{
    GameEvents.EnemyDied -= OnEnemyDied;
}
```

`+=` là đăng ký nghe, `-=` là ngừng nghe. Quên gỡ có thể khiến object đã tắt vẫn bị gọi.

---

## 14. Scene, prefab và ScriptableObject

```text
Enemy.cs      = luật quái hoạt động ra sao
Enemy.prefab  = các component được lắp thế nào
CoHon.asset   = Cô Hồn có máu/tốc độ/sprite bao nhiêu
Arena.unity   = sân khấu chứa những object nào
```

- Scene là sân khấu.
- Prefab là mẫu vật thể lắp sẵn.
- ScriptableObject là tờ thông số tạo thành file `.asset`.

Nhờ tách dữ liệu khỏi luật, ta tạo nhiều loại quái mà không chép lại `Enemy.cs`.

---

## 15. Object pool là kho đạo cụ

Đạn và quái xuất hiện liên tục. Tạo rồi huỷ hàng trăm object khiến máy dọn rác và có thể giật.

```text
Get()     → lấy đạo cụ ra dùng
Release() → cất lại
Get()     → tái sử dụng lần sau
```

Vì object được dùng lại, `OnSpawned` phải đặt trạng thái mới và `OnDespawned` phải xoá dữ liệu cũ.

---

## 16. Hai máy online là hai bản game

```text
Máy Host                         Máy Client
Player, quái, máu, vị trí        Player, quái, máu, vị trí
```

Nếu hai máy cùng tự quyết máu, chúng có thể ra hai đáp án khác nhau. LẠC chọn host làm trọng tài gameplay.

```csharp
[Command]   // Client gửi yêu cầu lên host.
[ClientRpc] // Host báo sự kiện xuống client.
[SyncVar]   // Host đổi giá trị, Mirror chuyển giá trị mới xuống client.
```

Ví dụ Dash:

```text
Bấm Dash
→ máy người chơi lướt ngay để không thấy trễ
→ gửi CmdDash lên host
→ host kiểm tra hồi chiêu và mở bất tử
→ host gửi RpcDashStarted để máy khác vẽ vệt lướt
```

Chưa cần học Mirror sâu. Chỉ cần luôn hỏi: “Đoạn này chạy trên máy nào?”

---

## 17. Cách đọc một method thật

```csharp
private void Update()
{
    if (IsDashing) EmitAfterimage();

    if (!isOwned || _input == null) return;
    if (!_input.DashPressedThisFrame) return;
    if (Time.time < _readyAt || IsDashing) return;

    BeginLocalDash();
    CmdDash();
}
```

Đọc thành:

1. Đang lướt thì thử tạo ảnh mờ.
2. Không phải nhân vật mình hoặc thiếu input thì dừng.
3. Frame này không bấm Dash thì dừng.
4. Chưa hồi xong hoặc đang lướt thì dừng.
5. Qua hết cửa: lướt tại máy mình và báo host.

Mẫu ghi chú:

```text
Ai gọi hàm?
Đầu vào?
Cửa chặn?
Thay đổi dữ liệu nào?
Gọi tiếp hàm nào?
Trả kết quả gì?
```

---

## 18. Debug là tìm dòng đầu tiên sai với dự đoán

```csharp
Debug.Log($"Health before: {_health}");
_health -= damage;
Debug.Log($"Health after: {_health}");
```

1. Ghi điều mong đợi.
2. Ghi điều thực tế.
3. Tìm dòng đầu tiên hai điều khác nhau.
4. Kiểm tra dữ liệu đúng chỗ đó.
5. Chỉ sửa sau khi hiểu nguyên nhân.

Đừng đổi năm thứ một lúc; nếu hết lỗi, bạn sẽ không biết cái gì đã chữa nó.

---

## 19. Bài thực hành trước bài 02

Làm trong scene tập riêng:

### A — Máu

- `_health = 3`;
- Space trừ 1;
- chỉ in `Dead` đúng một lần;
- không cho máu xuống dưới 0.

### B — Di chuyển

- WASD tạo `Vector2`;
- chuẩn hoá hướng;
- nhân tốc độ và `Time.deltaTime`;
- in hướng ra Console.

### C — Hồi chiêu

- Space in `Dash`;
- chờ 1 giây mới được in lần nữa;
- dùng `Time.time` và `_readyAt`.

### D — Tự nói lại

Phân biệt biến/hàm, class/object, GameObject/component, `Update`/`FixedUpdate`, prefab/ScriptableObject và `=`/`==`.

Hiểu khoảng 70% là đủ sang bài 02; các ý sẽ còn lặp lại.

## Tờ phao

| Code | Đọc đơn giản |
|---|---|
| `private int _health;` | hộp số nguyên dùng nội bộ |
| `public int Health => _health;` | cửa cho bên ngoài đọc máu |
| `if (...) return;` | không đủ điều kiện thì dừng |
| `void Move()` | máy Move không trả kết quả |
| `bool IsDead()` | máy trả lời đúng/sai |
| `thing.Method()` | nhờ object làm một việc |
| `thing.Value` | đọc dữ liệu của object |
| `== null` | chưa nối object |
| `+=` / `-=` event | nghe / ngừng nghe |
| `[SerializeField]` | chỉnh private trong Inspector |
| `[Command]` | client xin host |
| `[ClientRpc]` | host báo client |

Đích không phải thuộc bảng. Đích là tháo một khối code thành nhiều câu nhỏ có nghĩa.

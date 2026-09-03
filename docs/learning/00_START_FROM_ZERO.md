# 00 — Học code game từ con số 0

## Bài 1 — Code là gì?

Code là danh sách chỉ dẫn cực kỳ chi tiết cho máy tính.

Giả sử bạn bảo một người: “hãy pha mì”. Người đó tự hiểu phải lấy tô, đun nước và mở gói mì. Máy tính thì không tự hiểu. Ta phải viết gần như sau:

```text
1. Lấy một cái tô.
2. Đặt mì vào tô.
3. Nếu chưa có nước nóng thì đun nước.
4. Đổ nước vào tô.
5. Chờ 3 phút.
```

Game cũng chỉ là rất nhiều chỉ dẫn nhỏ:

```text
Nếu người chơi giữ phím D
    thì di chuyển sang phải.

Nếu viên đạn chạm quái
    thì trừ máu quái.

Nếu máu quái bằng 0
    thì cho quái chết.
```

Máy tính thực hiện đúng những gì ta viết. Nó không đoán ý định và không tự sửa một quy tắc thiếu.

### Điều cần nhớ duy nhất

Code không phải phép thuật. Code là các bước và các quy tắc.

---

## Bài 2 — Biến là một chiếc hộp có nhãn

Trong game, máy tính phải nhớ rất nhiều thứ:

- người chơi còn bao nhiêu máu;
- quái đang ở đâu;
- đã hết thời gian hồi dash chưa;
- ván đang ở đợt thứ mấy.

Một **biến** là chiếc hộp có tên dùng để giữ một giá trị.

```csharp
int health = 6;
```

Hãy đọc câu này từ phải sang trái:

1. `6` là giá trị được cất.
2. `health` là nhãn dán trên hộp.
3. `int` nói hộp này chỉ chứa số nguyên như `6`, `5`, `0`, không chứa `6.5`.

Dấu `=` ở đây có nghĩa là “đặt giá trị bên phải vào hộp bên trái”.

```csharp
health = health - 1;
```

Nếu trước đó `health` là 6, máy tính làm như sau:

1. đọc giá trị hiện tại của `health`: 6;
2. tính `6 - 1`: được 5;
3. cất 5 trở lại hộp `health`.

### Một số loại hộp thường gặp

```csharp
int health = 6;              // Số nguyên
float speed = 5.5f;          // Số có phần thập phân
bool isAlive = true;         // Chỉ có đúng hoặc sai
string playerName = "Tấm";  // Chữ
```

Phần sau `//` là lời ghi chú cho con người. Máy tính bỏ qua nó.

### Tự kiểm tra

```csharp
int coins = 10;
coins = coins + 3;
coins = coins - 2;
```

Đừng xem đáp án ngay. Hãy lấy giấy làm ba chiếc hộp liên tiếp.

Đáp án: cuối cùng `coins` bằng 11.

---

## Bài 3 — `if` là một cánh cửa có điều kiện

Game phải đưa ra quyết định. Ta dùng `if`, nghĩa là “nếu”.

```csharp
if (health <= 0)
{
    isAlive = false;
}
```

Đọc thành tiếng:

> Nếu máu nhỏ hơn hoặc bằng 0, đặt trạng thái sống thành sai.

Cặp `{ }` bao quanh những lệnh chỉ được chạy khi điều kiện đúng.

Một ví dụ khác:

```csharp
if (spaceKeyPressed)
{
    Dash();
}
```

> Nếu phím Space vừa được bấm, thực hiện hành động Dash.

Các phép so sánh thường gặp:

| Code | Cách đọc |
|---|---|
| `health == 0` | máu có bằng 0 không? |
| `health != 0` | máu có khác 0 không? |
| `health > 0` | máu có lớn hơn 0 không? |
| `health <= 0` | máu có nhỏ hơn hoặc bằng 0 không? |

Chú ý: `=` là đặt giá trị; `==` là hỏi hai giá trị có bằng nhau không.

### Tự kiểm tra

```csharp
int health = 2;

if (health <= 0)
{
    isAlive = false;
}
```

Khối lệnh có chạy không? Không, vì `2 <= 0` là sai.

---

## Bài 4 — Hàm là một chiếc máy nhỏ

Nếu phải viết lại cùng một nhóm bước nhiều lần, code sẽ rất dài. Ta gom các bước thành một **hàm**.

```csharp
void TakeDamage()
{
    health = health - 1;
}
```

Hình dung `TakeDamage` là một chiếc máy có nút bấm:

```text
Bấm nút TakeDamage
        |
        v
lấy health hiện tại - 1
        |
        v
cất kết quả lại vào health
```

Muốn chạy chiếc máy, ta gọi nó:

```csharp
TakeDamage();
```

Dấu `()` có thể chứa dữ liệu đưa vào máy. Dữ liệu đó gọi là **tham số**.

```csharp
void TakeDamage(int amount)
{
    health = health - amount;
}
```

Bây giờ ta có thể gọi:

```csharp
TakeDamage(1); // Mất 1 máu
TakeDamage(3); // Mất 3 máu
```

`void` nghĩa là hàm chỉ làm việc, không gửi một kết quả trở lại nơi gọi.

Một hàm có gửi kết quả:

```csharp
int Add(int a, int b)
{
    return a + b;
}
```

`Add(2, 3)` gửi trở lại số 5. `return` nghĩa là “kết thúc hàm và trả kết quả này về”.

### Điều cần nhớ

- Biến là hộp giữ dữ liệu.
- `if` là cánh cửa quyết định.
- Hàm là chiếc máy gom nhiều bước thành một hành động có tên.

Ba thứ này đã tạo nên phần lớn logic game.

---

## Bài 5 — Class là bản thiết kế

Trong game có nhiều loại đồ vật: Player, Enemy, Projectile. Mỗi loại cần dữ liệu và hành động riêng.

```csharp
class Enemy
{
    int health = 10;

    void TakeDamage(int amount)
    {
        health = health - amount;
    }
}
```

`Enemy` là **bản thiết kế**, giống bản vẽ một căn nhà. Bản vẽ nói rằng mỗi Enemy có:

- một hộp `health`, ban đầu là 10;
- một hành động `TakeDamage`.

Khi game có 20 con quái, ta có 20 **đối tượng** được tạo từ cùng bản thiết kế. Mỗi con giữ hộp health riêng:

```text
Enemy số 1: health = 10
Enemy số 2: health = 4
Enemy số 3: health = 0
```

Đánh Enemy số 2 không tự làm Enemy số 1 mất máu.

### Bản thiết kế và đồ vật thật

```text
Class Enemy.cs        = bản thiết kế quái
Enemy.prefab          = mẫu quái đã lắp hình ảnh, collider và code
Enemy trong lúc Play  = một con quái thật đang tồn tại
```

---

## Bài 6 — Unity nhìn một vật thể như bộ đồ chơi lắp ghép

Trong Unity, một vật thể gọi là **GameObject**. GameObject rỗng gần như không biết làm gì. Ta lắp các **Component** lên nó.

Ví dụ Player:

```text
GameObject "Player"
├── Transform          nhớ vị trí, góc xoay, kích thước
├── SpriteRenderer     vẽ hình nhân vật
├── Rigidbody2D        giúp nhân vật di chuyển theo vật lý
├── PlayerMovement     code đi bộ
├── PlayerDash         code lướt
└── PlayerHealth       code máu
```

Hãy tưởng tượng GameObject là thân robot, Component là các bộ phận:

- mắt giúp nhìn;
- chân giúp đi;
- pin giữ năng lượng.

Mỗi script C# kế thừa `MonoBehaviour` có thể trở thành một component để gắn lên GameObject.

```csharp
public class SimpleMover : MonoBehaviour
{
}
```

Bạn chưa cần hiểu `public` hay kế thừa ngay. Lúc này chỉ cần biết:

> `MonoBehaviour` cho Unity biết class này là một component Unity có thể gắn lên GameObject.

---

## Bài 7 — Unity tự bấm một số nút cho ta

Ta không tự gọi mọi hàm. Unity có những hàm đặc biệt và tự gọi đúng lúc.

```csharp
void Start()
{
    // Unity gọi một lần khi object bắt đầu hoạt động.
}

void Update()
{
    // Unity gọi lại mỗi khung hình.
}
```

Nếu game chạy 60 FPS, `Update()` được gọi xấp xỉ 60 lần trong một giây.

Ví dụ:

```csharp
public class SimpleMover : MonoBehaviour
{
    float speed = 3f;

    void Update()
    {
        transform.position = transform.position + Vector3.right * speed * Time.deltaTime;
    }
}
```

Đừng hoảng vì dòng dài. Tách nó ra:

```text
transform.position        vị trí hiện tại
Vector3.right             hướng sang phải
speed                     đi nhanh bao nhiêu
Time.deltaTime            thời gian đã trôi qua từ frame trước
```

Câu chuyện của mỗi frame:

1. đọc vị trí hiện tại;
2. tính một bước nhỏ sang phải;
3. đặt vị trí mới;
4. frame sau lặp lại.

Nhiều bước nhỏ liên tiếp tạo cảm giác chuyển động.

---

## Bài 8 — Tại sao cần `Time.deltaTime`?

Giả sử ta cộng thẳng 1 đơn vị mỗi frame:

```csharp
position = position + 1;
```

- Máy 30 FPS đi 30 đơn vị trong một giây.
- Máy 120 FPS đi 120 đơn vị trong một giây.

Máy mạnh sẽ làm nhân vật chạy nhanh hơn. Đây là lỗi.

Nếu tốc độ là 3 đơn vị mỗi giây:

```csharp
distanceThisFrame = 3 * Time.deltaTime;
```

- Ở 30 FPS, mỗi bước lớn hơn nhưng có ít bước.
- Ở 120 FPS, mỗi bước nhỏ hơn nhưng có nhiều bước.
- Sau một giây, cả hai đều đi xấp xỉ 3 đơn vị.

`Time.deltaTime` biến “mỗi frame” thành “mỗi giây”.

---

## Bài 9 — Đọc script đầu tiên trong LẠC: `ArenaBounds.cs`

Đừng cố đọc cả file một lượt. Ta chia thành những miếng rất nhỏ.

### Miếng 1: thư viện cần dùng

```csharp
using UnityEngine;
```

Câu này giống như nói:

> Tôi muốn dùng hộp dụng cụ UnityEngine trong file này.

Nhờ đó code dùng được `MonoBehaviour`, `Vector2`, `Rect`, `Gizmos`.

### Miếng 2: ngăn chứa tên

```csharp
namespace LAC.Core
{
}
```

`namespace` giống họ và địa chỉ của class. Có thể nhiều thư viện cùng đặt class là `Enemy`; tên đầy đủ `LAC.Enemies.Enemy` giúp máy phân biệt.

### Miếng 3: bản thiết kế component

```csharp
public sealed class ArenaBounds : MonoBehaviour
```

Đọc đơn giản:

> Tạo một loại component tên ArenaBounds.

- `public`: code khác được phép biết tới nó.
- `sealed`: không cho tạo một class con kế thừa ArenaBounds.
- `: MonoBehaviour`: component này gắn được lên GameObject trong Unity.

Bạn chưa cần học thuộc ba từ này. Chỉ cần nhận ra đây là dòng bắt đầu class.

### Miếng 4: kích thước sân

```csharp
[SerializeField] private Vector2 _size = new Vector2(36f, 20f);
```

Đây là một chiếc hộp tên `_size`.

- `Vector2` chứa hai số: x và y.
- x = 36 là chiều rộng.
- y = 20 là chiều cao.
- `[SerializeField]` làm chiếc hộp hiện trong Inspector.
- `private` không cho class khác gán thẳng tuỳ ý.

Hình dung:

```text
_size
├── x = 36  chiều rộng
└── y = 20  chiều cao
```

Khi bạn chọn object `Bounds` trong scene, Inspector hiển thị hai số này.

### Miếng 5: câu trả lời “hình chữ nhật sân ở đâu?”

```csharp
public Rect Rect => new Rect((Vector2)transform.position - _size * 0.5f, _size);
```

Dòng này khó vì viết tắt nhiều bước. Viết dài ra để dễ hình dung:

```csharp
public Rect Rect
{
    get
    {
        Vector2 center = transform.position;
        Vector2 halfSize = _size * 0.5f;
        Vector2 bottomLeft = center - halfSize;
        Rect result = new Rect(bottomLeft, _size);
        return result;
    }
}
```

Câu chuyện:

1. lấy tâm sân từ vị trí GameObject;
2. tính nửa kích thước;
3. đi từ tâm sang góc trái dưới;
4. tạo hình chữ nhật từ góc đó với kích thước `_size`;
5. trả hình chữ nhật cho nơi hỏi.

Nếu tâm là `(0, 0)` và size là `(36, 20)`:

```text
nửa size = (18, 10)
góc trái dưới = (0, 0) - (18, 10) = (-18, -10)

        y = 10
(-18,10) +------------------+ (18,10)
         |        tâm       |
         |       (0,0)      |
(-18,-10)+------------------+ (18,-10)
        y = -10
```

### Miếng 6: `Clamp` là kéo một điểm vào trong sân

Giả sử player chạy ra ngoài mép phải. `Clamp` kéo tọa độ của player về vị trí hợp lệ gần nhất.

```text
Sân từ x = -18 tới x = 18

điểm x = 5    -> vẫn là 5
điểm x = 30   -> kéo về 18
điểm x = -25  -> kéo về -18
```

`margin` là khoảng chừa thêm. Nếu player rộng 1 đơn vị, ta không muốn tâm player chạm đúng tường làm nửa người ló ra ngoài.

Trong code:

```csharp
float halfX = Mathf.Max(r.width * 0.5f - margin.x, 0f);
```

Đọc theo các hộp trung gian:

```text
nửa chiều rộng sân
- lề cần chừa
= khoảng tâm object được phép di chuyển
```

`Mathf.Max(..., 0)` bảo đảm kết quả không bị âm nếu margin lớn bất thường.

Sau đó:

```csharp
Mathf.Clamp(point.x, center.x - halfX, center.x + halfX)
```

nghĩa là:

> Giữ x của điểm nằm giữa mép trái hợp lệ và mép phải hợp lệ.

Phần y làm y hệt theo chiều dọc.

### Miếng 7: `Contains`

```csharp
public bool Contains(Vector2 point) => Rect.Contains(point);
```

Đây là một câu hỏi đúng/sai:

> Điểm này có nằm trong hình chữ nhật sân không?

Projectile dùng câu trả lời này. Nếu đạn không còn trong sân, nó được trả về pool.

### Miếng 8: đường viền chỉ thấy trong Scene view

```csharp
private void OnDrawGizmos()
{
    Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
    Gizmos.DrawWireCube(transform.position, _size);
}
```

Unity tự gọi `OnDrawGizmos` để vẽ đường hỗ trợ trong Editor:

1. chọn màu xanh nhạt;
2. vẽ khung hộp tại tâm object với kích thước `_size`.

Đường này giúp lập trình viên nhìn sân, không phải vật thể gameplay thật.

### Bạn cần hiểu gì sau file này?

Không cần thuộc `Rect` hay `Mathf.Clamp`. Chỉ cần kể được:

> ArenaBounds nhớ kích thước sân, tính ra hình chữ nhật của sân, trả lời một điểm có nằm trong sân không, và kéo một điểm vượt biên về bên trong.

Nếu kể được câu đó và chỉ ra `_size`, `Contains`, `Clamp`, bạn đã đọc hiểu file đầu tiên.

---

## Bài 10 — Bài thực hành đầu tiên, không sửa code

Thực hiện trong Unity:

1. Mở scene `Arena`.
2. Chọn GameObject `Bounds`.
3. Tìm component `ArenaBounds` trong Inspector.
4. Ghi lại Size X và Size Y hiện tại.
5. Đổi X từ 36 xuống 20.
6. Quan sát đường gizmo thay đổi trong Scene view.
7. Đổi lại 36, không lưu thay đổi thử nghiệm nếu project đang có người khác làm scene.

Trước khi đổi, hãy dự đoán: chiều ngang hay chiều dọc co lại?

Mục tiêu bài này là nối ba thứ trong đầu:

```text
field `_size` trong code
        =
ô Size trong Inspector
        =
đường biên nhìn thấy ở Scene view
```

Đó chính là cách code điều khiển Unity.

---

## Bài 11 — Đoạn code game đầu tiên bạn nên tự viết

Đừng thêm đoạn này vào LẠC ngay. Hãy tạo một project Unity thử nghiệm hoặc một scene học riêng để không ảnh hưởng dự án nhóm.

```csharp
using UnityEngine;

public class MoveRight : MonoBehaviour
{
    [SerializeField] private float _speed = 3f;

    private void Update()
    {
        transform.position += Vector3.right * _speed * Time.deltaTime;
    }
}
```

Thực hiện:

1. Tạo một GameObject hình vuông.
2. Tạo script `MoveRight`.
3. Gắn script lên hình vuông.
4. Bấm Play.
5. Đổi Speed trong Inspector và dự đoán kết quả.

Sau đó tự sửa để nó đi sang trái. Gợi ý: `Vector3.left`.

Tiếp theo thêm chiếc hộp:

```csharp
[SerializeField] private bool _canMove = true;
```

và cánh cửa:

```csharp
if (_canMove)
{
    transform.position += Vector3.right * _speed * Time.deltaTime;
}
```

Tắt/bật Can Move trong Inspector. Bạn vừa dùng:

- biến;
- điều kiện;
- hàm Update;
- component;
- Inspector;
- chuyển động theo thời gian.

Đó đã là code game thật.

---

## Bài 12 — Lộ trình từ số 0 tới tự viết game

Không học tất cả cùng lúc. Đi theo tầng:

### Tầng 1 — C# tối thiểu

- biến và kiểu dữ liệu;
- phép tính;
- `if/else`;
- vòng lặp `for`;
- hàm và tham số;
- class và object;
- List.

Sản phẩm nhỏ: trò đoán số chạy trong Console.

### Tầng 2 — Unity một người chơi

- GameObject và Component;
- Transform;
- `Start/Update/FixedUpdate`;
- input;
- Rigidbody2D và Collider2D;
- prefab;
- scene;
- UI cơ bản.

Sản phẩm nhỏ: nhân vật hình vuông đi lại, nhặt đồng xu và có điểm.

### Tầng 3 — Gameplay có cấu trúc

- health/damage;
- state machine;
- ScriptableObject;
- event;
- object pool;
- tách input, logic và hình ảnh.

Sản phẩm nhỏ: arena một người chơi có một loại quái và một loại đạn.

### Tầng 4 — Đọc lại LẠC

Lúc này mới đọc lần lượt:

```text
ArenaBounds
-> PlayerInputReader
-> PlayerMovement
-> PlayerHealth
-> DamageSystem
-> Enemy
-> WeaponAuto
-> ObjectPool
-> RunManager
```

### Tầng 5 — Multiplayer

Chỉ học sau khi game một người và C# căn bản đã rõ:

- server/client/host;
- authority;
- SyncVar;
- Command;
- RPC;
- prediction và latency.

Nếu học Mirror ngay từ ngày đầu, bạn phải hiểu C#, Unity và mạng cùng lúc—đó chính là cảm giác “đọc gì cũng không hiểu”.

---

## Bài 13 — Cách biết mình thật sự hiểu

Sau mỗi bài, đừng hỏi “mình có nhớ cú pháp không?”. Hãy hỏi:

1. Tôi có kể lại được bằng lời đời thường không?
2. Tôi có dự đoán được nếu đổi một giá trị không?
3. Tôi có tự viết lại phiên bản nhỏ hơn mà không copy không?
4. Khi kết quả sai, tôi có kiểm tra từng bước thay vì hỏi AI làm lại toàn bộ không?

Ví dụ với `ArenaBounds`:

- kể được nhiệm vụ của class;
- dự đoán đổi Size X làm gì;
- tự viết được hàm kiểm tra một số có nằm giữa min/max;
- dùng Debug.Log để xem giá trị thay vì đoán.

Đó là hiểu. Không cần thuộc từng dấu chấm phẩy.

---

## Bài 14 — Việc cần làm ngay sau khi đọc

Chưa đọc tiếp tài liệu số 01. Hãy làm đúng ba việc:

1. Viết ra giấy: biến, `if`, hàm, class là gì bằng lời của bạn.
2. Thực hiện bài quan sát `ArenaBounds` trong Unity.
3. Tự viết `MoveRight` trong môi trường thử nghiệm và sửa nó đi sang trái.

Nếu chưa làm được một bước, quay lại đúng bài đó. Học lập trình giống học bơi: đọc giải thích giúp định hướng, nhưng tay bạn phải tự gõ và mắt bạn phải tự quan sát kết quả.

Khi ba việc trên đã làm được, sang tài liệu số 01 và chỉ đọc mục 1–3. Không cần đọc hết trong một buổi.

# Asset bên thứ ba — dùng để thử nghiệm

**Vị trí:** `Assets/ThirdParty/` · **Trạng thái:** tạm thời, không phát hành

Hai gói đang có:

| Gói | Dung lượng | Nội dung | Kết luận |
|---|---|---|---|
| [TinyRPG](#tinyrpg) | 730 KB · 34 PNG | 2 nhân vật + mũi tên, ~20px | **Dùng chính cho nhân vật và quái** |
| [TinySwords](#tinyswords) | 8.6 MB · 410 PNG | quân, nhà, địa hình, giao diện, hiệu ứng, ~90px | **Chỉ dùng phần giao diện** |

> **CẢNH BÁO — cả hai gói KHÔNG được phát hành cùng sản phẩm.**
> Đây là mỹ thuật trung cổ châu Âu: hiệp sĩ, orc, lâu đài. LẠC dựng trên thần thoại
> Việt Nam. Hai gói này chỉ để **kiểm thử cơ chế** khi mỹ thuật thật chưa có — va chạm,
> hoạt ảnh, tilemap, thứ tự vẽ, ngân sách hiệu năng. Chúng **không thay thế** T-17
> (chốt bảng màu Đông Hồ) và T-18 (sprite thật). Khi T-18 xong, xoá cả `Assets/ThirdParty/`.

Vì lý do trên, hai gói nằm **ngoài** `Assets/_LAC/` — theo mục 4 của CLAUDE.md,
`_LAC/` chỉ chứa tài nguyên do nhóm tạo ra. Đặt ở `ThirdParty/` cho phép xoá sạch
bằng một thao tác mà không có nguy cơ xoá nhầm mỹ thuật của nhóm.

---

## Nên dùng gói nào

**Dùng TinyRPG làm nhân vật, quái và đạn. Giữ TinySwords chỉ cho giao diện.**

Bốn lý do, xếp theo mức quyết định:

### 1. TinySwords không có hoạt ảnh chết — TinyRPG có

Vòng lặp cốt lõi của LẠC là giết quái liên tục: 16 đợt, mỗi đợt hàng chục con,
mỗi con chết thì rơi Hồn. Cả 5 loại quân của TinySwords (Warrior, Archer, Lancer,
Monk, Pawn) **không có một khung hoạt ảnh chết hay trúng đòn nào**. Chúng là asset
cho game chiến thuật, nơi lính biến mất chứ không ngã xuống.

TinyRPG có đủ cho cả Soldier lẫn Orc:

| Hoạt ảnh | Soldier | Orc |
|---|---|---|
| Idle | 6 khung | 6 khung |
| Walk | 8 | 8 |
| Attack01 / 02 | 6 / 6 | 6 / 6 |
| Attack03 (bắn cung, có mũi tên bay) | 9 | — |
| **Hurt** | **4** | **4** |
| **Death** | **4** | **4** |

Đây là khác biệt quyết định. Không có Death thì T-14 (quái chết), T-15 (phản hồi
khi đánh trúng) và cơ chế Hồn đều không kiểm thử được đúng cảm giác.

### 2. Kích cỡ sprite — TinySwords quá to cho thể loại

Đo phần thân thật của nhân vật (bỏ vùng trong suốt):

| | Kích thước thật | Khung chứa |
|---|---|---|
| TinyRPG Soldier | **17 × 21 px** | 100 × 100 |
| TinyRPG Orc | **22 × 15 px** | 100 × 100 |
| TinySwords Warrior | **79 × 89 px** | 192 × 192 |
| TinySwords Lancer | **69 × 150 px** | 320 × 320 |

Ngân sách của LẠC là **40 quái và 200 đạn cùng lúc** (mục 5). Với sprite cao 90px
thì camera phải kéo xa tới mức mất chi tiết, hoặc 40 con quái phủ kín màn hình
không còn chỗ di chuyển. Vampire Survivors và Brotato đều dùng sprite nhỏ, và đó
không phải ngẫu nhiên — thể loại này cần **nhiều thân thể trên màn hình**, không
cần thân thể đẹp.

Đây cũng là vấn đề của mục 2.1: sóng âm Đông Sơn phủ kín màn hình về cuối ván.
Bóng đen đơn giản 20px vẫn đọc được dưới nhiều lớp vòng sáng chồng nhau; sprite
90px nhiều chi tiết sẽ tranh chấp thị giác với chính hiệu ứng của người chơi.

### 3. TinyRPG trùng mật độ pixel với sprite tạm của nhóm

Sprite tạm trong `Assets/_LAC/Art/Placeholder/` là 16×16 px. TinyRPG nhập ở
**PPU 16** cho ra pixel đúng bằng cỡ đó — đặt cạnh nhau không thấy lệch. TinySwords
vẽ ở 64px một ô, nhập ở PPU 64; kích thước trong thế giới thì tương đương, nhưng
**cỡ pixel nhỏ hơn 4 lần**, đặt chung một màn hình sẽ thấy rõ hai phong cách.

### 4. Đổi lại, TinySwords rộng hơn nhiều

TinyRPG chỉ có 2 nhân vật, 1 mũi tên, **không có tileset, không có giao diện,
không có hiệu ứng**, và chỉ có tư thế nhìn nghiêng.

Nên phân vai:

| Hạng mục | Lấy từ |
|---|---|
| Nhân vật người chơi tạm | **TinyRPG Soldier** (có cả đánh gần lẫn bắn cung) |
| Quái Cô Hồn tạm (T-14) | **TinyRPG Orc** |
| Đạn (T-12) | **TinyRPG** `Arrow01(32x32).png` |
| Sàn đấu trường (T-10B) | **Placeholder 16px của nhóm** — đúng mật độ pixel |
| Giao diện: nút, thanh máu, khung thẻ (T-15B, T-22, T-23) | **TinySwords** `UI Elements/` |
| Hiệu ứng | **Không lấy từ gói nào** — `SoundWave` của T-16 đã tự vẽ |

Giao diện nằm ở không gian màn hình, không chịu ràng buộc mật độ pixel với thế
giới game, nên trộn được. Đó là phần duy nhất TinySwords hơn hẳn và TinyRPG
hoàn toàn không có.

### Hai điểm cần biết trước khi dùng

- **Cả hai gói chỉ có tư thế nhìn nghiêng**, lật ngang theo hướng đi. LẠC là góc
  nhìn từ trên xuống. Ngoại lệ duy nhất là TinySwords Lancer — loại duy nhất có
  sprite 4 hướng. Nghĩa là **không gói nào đánh giá được hoạt ảnh xoay hướng thật**;
  T-18 vẫn phải quyết định LẠC dùng 1 hướng lật ngang hay 4/8 hướng.
- **`Orc_Hurt` và `Soldier_Hurt` có sẵn một khung nhuộm đỏ toàn thân.** Đây là hiệu
  ứng chớp trúng đòn nướng sẵn vào sprite, sẽ **chồng lên** `SpriteFlash` trong
  `HitFeedback` của T-15. Khi thử, dùng một trong hai, không dùng cả hai.
- Thư mục `... with shadows/` có sẵn bóng đổ hình elip dưới chân — hợp góc nhìn từ
  trên xuống hơn bản không bóng. Dùng bản có bóng.

---

<a id="tinyrpg"></a>
## TinyRPG — `Assets/ThirdParty/TinyRPG/`

**Tác giả:** Free Game Assets · **Ngày đưa vào:** 03/09/2026 · 730 KB

```
Arrow(Projectile)/              mũi tên 32x32 và 100x100
Aseprite file/                  file nguồn .aseprite (sửa được nếu cần)
Characters(100x100 split)/
  Orc/Orc/                      7 tệp, không bóng
  Orc/Orc with shadows/         7 tệp, có bóng  ← dùng bản này
  Soldier/Soldier/              8 tệp, không bóng
  Soldier/Soldier with shadows/ 8 tệp, có bóng  ← dùng bản này
```

`Orc.png` (800×600) và `Soldier.png` (900×700) là atlas gộp toàn bộ hoạt ảnh
thành lưới — mỗi hàng một hoạt ảnh. Dùng atlas này thay vì 7 tệp rời sẽ giảm
số draw call khi thử ngân sách 40 quái.

**Cấu hình import đã sửa:** Point · Uncompressed · mipmap off · **PPU 16** ·
cắt lưới 100×100 pivot giữa. Kết quả: 4 sprite đơn, 30 sheet, **376 khung**.

---

<a id="tinyswords"></a>
## TinySwords — `Assets/ThirdParty/TinySwords/`

**Tác giả:** Pixel Frog · **Ngày đưa vào:** 03/09/2026 · 8.6 MB

---
### Nội dung — 410 PNG, 18 .aseprite

| Nhóm | Số lượng | Kích thước khung |
|---|---|---|
| `Units/` | 5 loại × 5 phe màu (Black, Blue, Purple, Red, Yellow) | 192×192 · Lancer 320×320 |
| `Buildings/` | 8 công trình × 5 phe màu | ảnh đơn |
| `Terrain/Tileset/` | 5 biến thể màu + Shadow + Water Foam | ô 64×64 |
| `Terrain/Decorations/` | bụi cây, mây, đá, đá dưới nước, vịt cao su | hỗn hợp |
| `Terrain/Resources/` | vàng, thịt, cừu, cây, gốc cây, dụng cụ | hỗn hợp |
| `Particle FX/` | bụi ×2, nổ ×2, lửa ×3, nước bắn | 64×64 và 192×192 |
| `UI Elements/` | nút, thanh, khung, biểu tượng, con trỏ, 25 chân dung | ảnh đơn |

Năm loại quân, mỗi loại đủ bộ hoạt ảnh — **nhưng không loại nào có Hurt hay Death**:

| Quân | Hoạt ảnh có sẵn |
|---|---|
| Warrior | Idle 8 · Run 6 · Attack1 4 · Attack2 4 · Guard 6 |
| Archer | Idle 6 · Run 4 · Shoot 8 · kèm sprite mũi tên |
| Lancer | Idle 12 · Run 6 · Attack và Defence theo 4 hướng |
| Monk | Idle 6 · Run 4 · Heal 11 · Heal_Effect 11 (hiệu ứng tách riêng) |
| Pawn | Idle · Run · Interact — mỗi trạng thái có 7 biến thể cầm đồ vật |

### Cấu hình import đã sửa

Gói này khi kéo vào mang thiết lập mặc định của Unity, sai toàn bộ với pixel art.
Đã sửa hàng loạt cho cả 410 tệp:

| Thiết lập | Trước | Sau | Lý do |
|---|---|---|---|
| Filter Mode | Bilinear | **Point** | Bilinear làm nhoè viền pixel, hỏng hoàn toàn phong cách |
| Compression | Normal Quality | **Uncompressed** | Nén DXT tạo nhiễu quanh viền alpha ở ảnh pixel |
| Pixels Per Unit | 100 | **64** | Một ô tilemap = 64px = 1 đơn vị Unity |
| Max Texture Size | 2048 | **4096** | `Lancer_Idle.png` rộng 3840px — giới hạn 2048 sẽ thu nhỏ và phá nát lưới pixel |
| Mipmap | bật | **tắt** | Không có ích cho sprite 2D chính diện, chỉ tốn bộ nhớ |
| Cắt sprite | tự động, pivot góc dưới trái | **lưới, pivot giữa** | Cắt tự động cho khung lệch nhau từng pixel → nhân vật giật khi chạy hoạt ảnh |

Kết quả: 152 sprite đơn, 258 sheet, tổng **2049 khung**.

Quy tắc cắt lưới: ảnh trải ngang có `width % height == 0` thì cắt ô vuông bằng
chiều cao; `Terrain/Tileset/` cắt ô 64; `UI Elements/`, `Buildings/` và `Clouds/`
giữ nguyên ảnh đơn — `BigBar_Base.png` là 320×64 nhưng là **một** thanh máu liền,
không phải 5 khung.

### Phần nên dùng — chỉ giao diện

| Asset | Dùng cho |
|---|---|
| `UI Elements/UI Elements/Bars/` | Thanh máu (T-15B) — có Base + Fill tách riêng, chuẩn cho thanh kiểu fill |
| `UI Elements/UI Elements/Buttons/` | Nút, đủ 2 trạng thái Regular / Pressed |
| `UI Elements/UI Elements/Papers/` + `Banners/` | Nền thẻ nâng cấp và khung chọn thẻ (T-22, T-23) |
| `UI Elements/UI Elements/Icons/` | Biểu tượng thẻ tạm |
| `UI Elements/UI Elements/Human Avatars/` | Chân dung chọn nhân vật, khung người chơi co-op |

### Phần không dùng, và vì sao

- **`Units/`** — không có Death, sprite 90px quá to cho 40 quái cùng lúc.
  Thay bằng TinyRPG. Điều đáng tiếc duy nhất là **5 phe màu**: cùng một quân có
  5 màu, rất tiện để phân biệt loại quái hoặc phân biệt hai người chơi co-op.
  TinyRPG không có. Nếu sau này cần thử phân biệt bằng màu, nhuộm sprite TinyRPG
  bằng `SpriteRenderer.color` là đủ cho mục đích thử nghiệm.
- **`Terrain/Tileset/`** — 64px một ô, lệch mật độ pixel với nhân vật TinyRPG 16px.
  Dùng `Tile_Floor_*.png` 16px của nhóm cho T-10B.
- **`Particle FX/`** — khung 192px, to gấp 9 lần nhân vật TinyRPG. Không dùng.
- **`Buildings/`, `Terrain/Resources/`, `Terrain/Decorations/`** — asset cho game
  chiến thuật xây dựng, không có cơ chế tương ứng trong LẠC.

---

## Ràng buộc chung khi dùng cả hai gói

1. **Không tham chiếu asset ở đây từ prefab hoặc ScriptableObject sẽ phát hành.**
   Chỉ gắn vào prefab thử nghiệm, hoặc gắn tạm rồi gỡ. Nếu một `CharacterData`
   trỏ vào sprite ở đây, đến lúc xoá thư mục sẽ vỡ tham chiếu hàng loạt.

   **Có hai ngoại lệ đã được ghi nhận:** T-18B gán
   `Assets/ThirdParty/TinyRPG/Soldier_TEST.asset` vào trường `_animationSet` của cả ba
   tài sản trong `Data/Characters/`, và T-18C gán `Orc_TEST.asset` vào
   `Data/Enemies/CoHon.asset`. Cả hai an toàn vì trường đó chịu được null —
   gỡ gói ra thì `SpriteAnimator` lặng lẽ nhường lại sprite tĩnh trong `BodySprite`.
   Xem thêm ở [Thủ tục gỡ gói](#go-goi) bên dưới.
2. **Không rút kết luận thị giác cho mục 2.1 từ hai gói này.** Bảng màu của chúng
   không phải bảng Đông Hồ; mọi phép đo tương phản giữa đòn địch và hiệu ứng
   người chơi làm trên chúng đều **không có giá trị**. T-17 vẫn là hạng mục chặn.
3. **Không đo cân bằng game bằng chúng.** Kích thước nhân vật khác thì tầm đánh
   cảm nhận cũng khác.
4. Vẫn phải đi qua `ObjectPool` như mọi đối tượng khác (mục 5).

## Câu hỏi còn mở cho T-17 / T-18

**LẠC vẽ nhân vật ở mật độ pixel nào?** Sprite tạm của nhóm đang là 16px — rất
thấp, khó vẽ hoa văn trống đồng Đông Sơn cho ra hình. TinyRPG ở 20px cho thấy
mức đó vẫn đọc được silhouette nhưng gần như không tả được hoa văn. Đề xuất
cân nhắc **32px cho nhân vật**, đủ chỗ cho chi tiết mà vẫn giữ được 40 quái
trên màn hình.

**Nhân vật có mấy hướng?** Cả hai gói gần như chỉ có tư thế nhìn nghiêng (ngoại lệ
duy nhất là TinySwords Lancer). Với góc nhìn từ trên xuống, T-18 phải chốt: 1 hướng
lật ngang là rẻ nhất, 4 hướng đắt gấp ba nhưng đọc hướng rõ hơn nhiều khi 40 quái
vây quanh.

---

<a id="go-goi"></a>
## Thủ tục gỡ gói khi T-18 xong

Làm đúng bốn bước, theo thứ tự:

1. Xoá trường `_animationSet` trên ba tài sản trong `Assets/_LAC/Data/Characters/`
   (`ThachSanh.asset`, `Giong.asset`, `Tam.asset`) và trên
   `Assets/_LAC/Data/Enemies/CoHon.asset` — gán về **None**.
2. Xoá trường `_bodySprite` nếu nó còn trỏ vào `ThirdParty/`, thay bằng sprite thật.
3. Sắp lại `CharacterRegistry.asset` về thứ tự thật nếu muốn — T-18C đã đưa Gióng
   lên đầu để hợp với sprite Soldier cận chiến, đây là lựa chọn tạm.
4. Xoá cả thư mục `Assets/ThirdParty/`.

Làm ngược thứ tự cũng không vỡ — `SpriteAnimator` và `PlayerCharacter` đều chịu được
tham chiếu null — nhưng Unity sẽ ghi một loạt cảnh báo tham chiếu thiếu cho tới khi
dọn xong bước 1 và 2.

Tính tới 03/09/2026, đây là toàn bộ chỗ mà mã nguồn và dữ liệu của nhóm chạm vào
`ThirdParty/`.

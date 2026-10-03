# Mỹ thuật thẻ — T-33A

Ngày: 01/10/2026. Bộ 20 icon tạo bằng OpenAI imagegen: 12 thẻ nền + 8 tiến hoá.
Không thay đổi chỉ số, luật bốc thẻ hay đồng bộ mạng. Đây là ảnh AI hỗ trợ sản xuất;
không phải tranh Đông Hồ truyền thống hoặc bản phục dựng hiện vật lịch sử.

## Hướng mỹ thuật

Pixel-art minh hoạ, nền than tối, đồng cổ vàng và ngọc xanh; ánh sáng góc trên trái.
Cường Công là ảnh neo phong cách; 19 ảnh còn lại dùng chính PNG đó làm style reference.
Không nhúng chữ vào ảnh. Bảng màu trong prompt bám PALETTE.md, tránh nhóm đỏ của địch;
ảnh sinh có sắc độ trung gian, không khẳng định đã lượng tử hoá đúng 24 màu.

## Tệp và nhập Unity

- `Assets/_LAC/Art/Sprites/UI/Cards/DongHo_2026/CardIcon_{ID}.png`: PNG gốc không chỉnh sửa.
- Bộ cũ `AI_Demo/` vẫn được giữ nguyên.
- Menu `LAC/Art/Apply Unified Card Icons`: nhập Sprite Single, Point, Clamp,
  không mipmap, không nén, Full Rect, max 512; gán 12 card + 8 recipe + 8 bonus.
- Menu `LAC/Art/Validate Unified Card Icons`: kiểm tra 20 đường dẫn khác nhau,
  đúng ID, bonus trùng recipe và thiết lập import.
- Không sửa ScriptableObject trong Play Mode. Không cần đổi scene/prefab.

## Prompt chung

```text
Use case: stylized-concept. Asset type: one square production UI card icon for LẠC, a Vietnamese mythology pixel-art arena-survival game. Art direction: premium handcrafted 16-bit pixel illustration, clearly visible crisp square pixel clusters as if authored at 128x128 and enlarged with nearest-neighbor; restrained detail, strong readable silhouette at 80px. Centered single emblem, object fills 68% of the square, 16% safe empty margins; no frame or border. Flat nearly black charcoal background #15130F with only a faint dark-teal circular halo behind the object. Materials: aged Đông Sơn bronze, simple geometric sun/avian engravings, subtle jade patina. Uniform light from upper left, bright pale gold edges, dark lower-right shadows. Restricted palette ONLY #15130F #2B2724 #47403A #112E3E #1C4B5C #2F7480 #4FA694 #9CCFC0 #8A5F14 #C08D20 #EDBB3E #FBDD82 #F4EADA. Blue-green magic is solid stepped bands, no blur. No red, orange, pink, purple, blood, photo realism, smooth gradients, painterly brush strokes, letters, text, numbers, labels, watermark, surrounding card frame, scenery, UI mockup or multiple panels. Exactly one icon.
```

Ảnh neo thêm `Deliver a square 1024x1024 PNG. This icon is the style anchor for a cohesive 20-icon set.`

Các ảnh sau thêm phần điều khiển tham chiếu:

```text
Input image: STYLE REFERENCE ONLY, match its pixel clusters, bronze/jade palette, dark background, light direction and visual density. Make a different icon; do not repeat its axe. No colored edge fringing. Keep all details within 12% margins.
Subject: {subject below}
Square 1024x1024 PNG.
```

## Nội dung từng ảnh

### 1. Cường Công — `CuongCong`

A broad Đông Sơn bronze battle axe angled from lower left to upper right, blade facing left, compact bright golden impact wedge at its foot and two teal force marks. Symbolizes increased attack damage.

### 2. Liên Kích — `LienKich`

Three successive curved bronze strike marks cascading diagonally, with three parallel jade speed trails behind them. Distinct sequential rhythm, symbolizing faster attacks. No axe.

### 3. Sinh Lực — `SinhLuc`

A single plump heart-shaped carved jade amulet in a thick bronze rim, a small sun-shaped gold clasp at its top. Symbolizes increased maximum health. The heart is teal, never red.

### 4. Bộ Pháp — `BoPhap`

A bronze-armored Vietnamese cloth boot pointing to upper right, a tight circular jade wind loop curling around its heel like a recharge motion. Symbolizes faster dash recharge.

### 5. Song Tiễn — `SongTien`

Exactly two parallel ornate bronze-tipped arrows flying diagonally toward upper right with matching separated jade motion trails. Both arrows fully visible, equal size, clearly countable. Symbolizes double shot.

### 6. Xuyên Tâm — `XuyenTam`

One long bright bronze arrow piercing through exactly three aligned small jade rings, angled toward upper right. Rings show a clear central opening. Symbolizes piercing through multiple targets.

### 7. Bộc Phá — `BocPha`

A small bronze projectile at the center of a compact eight-point golden impact burst, surrounded by four chunky jade shards. Symbolizes explosion on impact. Explosion uses gold and teal only.

### 8. Khinh Thân — `KhinhThan`

One sweeping jade Lạc-bird feather with a bronze central quill, three short trailing jade wind marks, drifting diagonally upward. Symbolizes lightness and increased movement speed.

### 9. Âm Vang — `AmVang`

A small front-facing bronze Đông Sơn drum with a gold central sun, sending out exactly three widening teal concentric sound rings. Rings remain inside safe margins. Symbolizes increased attack range.

### 10. Hồi Xuân — `HoiXuan`

A fresh jade lotus bud with two open leaves cradling one luminous pale-gold droplet, bronze curled stem below. Compact upright life-restoring emblem. Symbolizes healing between waves.

### 11. Thiết Bích — `ThietBich`

A heavy upright bronze round shield with a central embossed sun and three broad dark-teal metal plates, a short protective jade crescent at its left. Symbolizes protection after taking a hit.

### 12. Cuồng Nộ — `CuongNo`

A compact fierce stylized Vietnamese bronze tiger mask with pale-gold eyes and angular jade energy horns, heavy broad lower silhouette. Symbolizes stronger but slower attacks. No red or orange.

### 13. Nỏ Thần — `NoThan`

A legendary ornate Đông Sơn bronze crossbow, shown from above in a clear wide T-shaped silhouette, a carved turtle-inspired central clasp and one brilliant teal bolt pointing upward. Evolved piercing weapon, richer bronze detail than base icons.

### 14. Lửa Thiêng — `LuaThien`

One sacred jade flame with pale-gold inner core rising from a low bronze lotus brazier, a broad stepped teal shockwave ring at its base. Evolved explosive power. Flame strictly teal and pale gold, never orange or red.

### 15. Trăm Trứng — `TramTrung`

A compact bronze nest cradling four luminous jade eggs, from which exactly four pale-gold arrow rays fan upward. Vietnamese hundred-eggs myth evoked by egg cluster, representing four projectiles; no literal hundred tiny objects.

### 16. Thánh Gióng — `ThanhGiong`

A strong bronze iron-horse head in profile with an angular sun crest and upright iron rod behind its neck, a jade ribbon of energy below. Vietnamese Thánh Gióng inspired heroic relic, no rider portrait or full scene.

### 17. Tiếng Đàn Thần — `TiengDan`

A Vietnamese đàn bầu monochord: long gently tapered bronze-inlaid wooden soundbox, exactly one taut visible string and a curved flexible vertical rod with small gourd at one end, diagonal across the square, jade sound wave arcs around it. Not a harp, lute or guitar.

### 18. Lạc Phong — `LacPhong`

A stylized Đông Sơn Lạc bird soaring upward, long bronze beak and broad jade wings in a sweeping spiral of jade wind, compact central silhouette. Evolved mobility, stronger sweeping motion than the single feather icon.

### 19. Bất Tử — `BatTu`

An open luminous jade lotus enclosing the same bronze-rimmed jade heart motif as a vitality amulet, two gold leaves forming a protective cradle. Evolved health and renewal, no skull, resurrection, cross or infinity symbol.

### 20. Kim Cang — `KimCang`

A large faceted jade diamond inset into a thick circular Đông Sơn bronze shield, four restrained pale-gold corner glints and a solid jade protective arc. Evolved endurance, geometrically distinct from the ordinary round shield.

## Kiểm chứng — 01/10/2026

- `CardArtSetup.Apply/Validate`: PASS, 20 icon khác nhau; 12 card, 8 recipe và
  8 bonus có đúng tham chiếu, Sprite Single / Point / không mipmap.
- Play Mode trong Arena, Mirror host hoạt động với 1 người chơi.
- Đã nhìn ảnh gallery ở kích thước khoảng 84 px; toàn bộ dùng chung chất liệu,
  nền và hướng sáng, không bị cắt biểu tượng.
- Màn chọn Âm Vang / Cuồng Nộ / Hồi Xuân hiển thị ảnh riêng, thay ký hiệu chữ.
- Chạy `ShowEvolution` với từng recipe: 8/8 `Image.sprite` trùng `recipe.Icon`;
  đã nhìn thông báo Nỏ Thần trong Game View.
- Console sau lượt kiểm tra: 0 errors, 0 warnings. Khi nhập lại asset lần đầu,
  Unity có cảnh báo tên bonus khác tên file (đã tồn tại trong dữ liệu).
- Các ảnh chụp là kiểm tra trình bày bằng lời gọi view trong ván host đang chạy;
  kiểm thử lựa chọn/co-op đầy đủ của T-25/T-26 nằm ở `EVOLUTION_TESTS.md`.

![Toàn bộ 20 icon trong Game View](screenshots/card-art-gallery.png)

![Màn chọn thẻ](screenshots/card-art-selection.png)

![Thông báo Nỏ Thần](screenshots/card-art-evolution.png)

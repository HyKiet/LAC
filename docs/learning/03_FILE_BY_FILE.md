# 03 — Bản đồ từng file code trong LẠC

> Đây là bản đồ tra cứu, không phải cuốn sách cần đọc một mạch.
>
> Mỗi lần chỉ mở **một file thật** cạnh mục tương ứng. Đọc tên biến và method trước; khi gặp cú pháp lạ mới quay lại bài 01.

## Cách dùng mỗi mục

Với từng file, tự điền:

```text
File này là ai trong game?
Nó giữ dữ liệu gì?
Unity hoặc class nào gọi nó?
Nó gọi/báo cho ai tiếp?
Nếu bỏ file này, người chơi thấy gì hỏng?
```

Ký hiệu độ khó:

- 🟢: nên đọc ngay;
- 🟡: đọc sau khi hiểu bài 01;
- 🔴: có mạng, pool hoặc thuật toán; đọc từng method, không đọc cả file một lượt.

---

# A. Core — luật và công cụ chung

## 🟢 `RunState.cs` — tên các giai đoạn của trận

**Là ai?** Một danh sách trạng thái: chưa chạy, đang đánh, chọn thẻ, thắng hoặc thua.

**Code làm gì?** Chỉ đặt tên lựa chọn bằng `enum`, không tự chuyển trạng thái.

**Học gì?** `enum` là cách thay số 0, 1, 2 khó hiểu bằng tên có nghĩa.

## 🟢 `IPoolable.cs` — cam kết của đồ tái sử dụng

**Là ai?** Tờ nội quy cho object nằm trong pool.

**Code làm gì?** Yêu cầu có `OnSpawned()` và `OnDespawned()`.

**Học gì?** Interface nói “phải có nút nào”, class thật quyết định nút đó làm gì.

## 🟢 `ArenaBounds.cs` — hàng rào đấu trường

**Giữ gì?** `_size` là chiều rộng và cao.

**Làm gì?**

- `Rect` tính hình chữ nhật từ tâm và kích thước;
- `Clamp` kéo một điểm ngoài sân vào trong;
- `Contains` hỏi điểm có trong sân không;
- `OnDrawGizmos` vẽ khung hỗ trợ trong Editor.

**Ai dùng?** Camera, quái và các hệ thống cần giới hạn vị trí.

**Học gì?** Một nguồn dữ liệu chung tránh mỗi nơi giữ một kích thước sân khác nhau.

## 🟡 `GameEvents.cs` — loa phát thanh

**Là ai?** Nơi khai báo các thông báo gameplay.

**Luồng:** một hệ thống gọi hàm `Raise...` → event phát → các hệ thống đã `+=` nghe sẽ chạy.

**Cẩn thận:** nơi đăng ký nghe phải có nơi `-=` để gỡ.

## 🟡 `RunManager.cs` — quản lý cả ván

**Giữ gì?** Trạng thái run hiện tại và seed dùng cho random.

**Làm gì?** Host bắt đầu/kết thúc run, chuyển trạng thái, khởi tạo random, báo event.

**Cách đọc:** tìm các method đổi `RunState`; chưa cần đọc chi tiết Mirror lần đầu.

## 🟡 `WaveManager.cs` — đồng hồ từng đợt

**Giữ gì?** Số wave, thời điểm kết thúc và khoảng nghỉ.

**Làm gì?** Host đếm thời gian, bắt đầu wave mới, kết thúc wave và báo cho hệ thống khác.

**Phân biệt:** RunManager quản lý cả trận; WaveManager quản lý từng hiệp nhỏ.

## 🟡 `RandomStream.cs` — một máy rút số

**Nhận gì?** Một seed.

**Trả gì?** Dãy số giả ngẫu nhiên nhưng có thể lặp lại nếu seed và thứ tự gọi giống nhau.

**Học gì?** “Ngẫu nhiên có seed” không thật sự hỗn loạn; nó là một dãy khó đoán nhưng tái tạo được.

## 🟡 `RunRandom.cs` — quầy phát các luồng random

**Là ai?** Cửa chung để gameplay xin random.

**Làm gì?** Từ seed của run, tạo các kênh riêng cho từng mục đích để một hệ thống không làm lệch dãy của hệ thống khác.

**Luật dự án:** gameplay dùng file này, không gọi `UnityEngine.Random`.

## 🔴 `ObjectPool.cs` — kho tái sử dụng object

**Giữ gì?** Prefab mẫu, các object đang rảnh và giới hạn kho.

**Luồng:** `Get` lấy object → bật và gọi `OnSpawned` → dùng → `Release` gọi `OnDespawned` và cất lại.

**Cửa bảo vệ:** tránh trả một object hai lần hoặc lấy nhầm object không thuộc kho.

**Cách học:** chỉ theo một viên đạn từ `Get` tới `Release`.

## 🔴 `PoolRegistry.cs` — danh bạ các kho

**Là ai?** Nơi bảo đảm mỗi prefab dùng chung đúng pool tương ứng.

**Làm gì?** Khi được hỏi pool của một prefab, trả pool cũ hoặc tạo một lần nếu chưa có.

**Học gì?** Registry tìm “kho nào”; ObjectPool quản lý “đồ trong kho”.

---

# B. Player — người chơi

## 🟢 `CharacterData.cs` — tờ chỉ số nhân vật

**Giữ gì?** Tên, sprite, máu, tốc độ, Dash và thông số vũ khí.

**Không làm gì?** Không đọc input hay di chuyển.

**Học gì?** ScriptableObject tách con số thiết kế khỏi code hành vi.

## 🟢 `CharacterRegistry.cs` — danh sách nhân vật có thể chọn

**Giữ gì?** Các `CharacterData`.

**Làm gì?** Nhận index/ID và trả tờ dữ liệu tương ứng, có kiểm tra phạm vi.

## 🟢 `PlayerInputReader.cs` — tai nghe bàn phím/gamepad

**Giữ gì?** Hướng Move và việc Dash vừa được bấm.

**Làm gì?** Bật/tắt action map đúng vòng đời, chỉ đọc input cho player local.

**Không làm gì?** Không tự di chuyển. Nó chỉ ghi ý định.

## 🟢 `PlayerRegistry.cs` — danh bạ player đang sống

**Làm gì?** Đăng ký, gỡ và tìm player gần một vị trí.

**Ai dùng?** Quái tìm mục tiêu, camera tìm local player, vũ khí/hệ thống khác tra cứu.

## 🟡 `PlayerCharacter.cs` — nối player với tờ nhân vật

**Nhận gì?** ID nhân vật được mạng truyền tới.

**Làm gì?** Lấy `CharacterData` trong registry rồi áp sprite/thông số cho các component.

**Học gì?** Prefab player có thể dùng chung; dữ liệu quyết định nó là Thạch Sanh, Gióng hay Tấm.

## 🟡 `PlayerMovement.cs` — đôi chân

**Đọc gì?** Hướng từ `PlayerInputReader`.

**Làm gì?** Chuẩn hoá hướng, nhớ hướng mặt, đặt velocity cho Rigidbody2D, giữ player trong `ArenaBounds`.

**Cửa quan trọng:** chỉ object `isOwned` mới tự chạy movement.

## 🟡 `PlayerHealth.cs` — sổ máu thật

**Giữ gì?** Máu hiện tại/tối đa bằng dữ liệu do host quản lý.

**Luồng:** host trừ máu → Mirror đồng bộ → hook phát `HealthChanged` → HUD và VFX cập nhật.

**Cửa quan trọng:** client không được tự quyết mất máu.

## 🔴 `PlayerDash.cs` — lướt, hồi chiêu và bất tử

Đọc theo bốn phần, không từ đầu tới cuối:

1. **Các property:** Duration, Cooldown, Speed lấy từ CharacterData hoặc fallback.
2. **`Update`:** kiểm tra input/hồi chiêu rồi bắt đầu Dash.
3. **`FixedUpdate`:** tiêu quãng đường lướt theo từng nhịp vật lý.
4. **`CmdDash` và `RpcDashStarted`:** host mở bất tử, máy khác nhận biểu diễn.

**Hai đồng hồ:** client giữ cảm giác lướt; host giữ cửa sổ bất tử thật.

**VFX:** `EmitAfterimage` lấy ảnh mờ từ pool, không tạo/huỷ liên tục.

---

# C. Combat — đánh nhau

## 🟢 `WeaponShape.cs` — tên hình dạng đòn đánh

**Là ai?** `enum` mô tả kiểu vòng tròn, cung hoặc tia.

**Không làm gì?** Không tự bắn; chỉ là dữ liệu lựa chọn.

## 🟡 `WeaponAuto.cs` — bộ bắn tự động

**Giữ gì?** Thời điểm được bắn tiếp và pool projectile/sóng âm.

**Luồng:** hết hồi chiêu → tìm mục tiêu gần → tính hướng → lấy projectile từ pool → phát hiệu ứng.

**Học gì?** Cooldown thường lưu bằng “thời điểm sẵn sàng tiếp theo”, không cần tự trừ biến mỗi frame.

## 🟡 `Projectile.cs` — một viên đạn cục bộ

**Nhận gì khi Play?** Hướng, tốc độ, damage, thời gian sống và pool sở hữu nó.

**Làm gì?** Bay mỗi frame, phát hiện chạm quái, phát event hit, rồi tự trả pool khi trúng/hết hạn.

**Cửa quan trọng:** một viên chỉ báo hit một lần; dữ liệu phải reset khi tái sử dụng.

## 🔴 `DamageSystem.cs` — trọng tài sát thương

**Là ai?** Người nghe event va chạm và quyết định kết quả thật.

**Quái bị bắn:** chỉ host yêu cầu EnemySpawner trừ máu.

**Player bị chạm:** chỉ host kiểm tra Dash bất tử rồi gọi PlayerHealth.

**Học gì?** Va chạm là thông tin; thay đổi HP là quyết định có thẩm quyền.

---

# D. Enemies — quái

## 🟢 `EnemyState.cs` — thẻ trạng thái quái

Các lựa chọn: xuất hiện, đuổi, đánh và chết. File chỉ đặt tên, không chứa AI.

## 🟢 `EnemyData.cs` — tờ chỉ số một loại quái

Giữ máu, tốc độ, sát thương chạm, tầm đánh, nhịp đánh, thời gian xuất hiện, sprite và màu.

`Enemy.cs` là luật chung; mỗi file `.asset` là một loại quái cụ thể.

## 🟡 `EnemyRegistry.cs` — danh bạ quái

**List:** tiện duyệt mọi quái để tìm gần nhất hoặc tính giãn cách.

**Dictionary:** tiện tìm đúng quái theo ID mạng.

**Luật:** Register/Unregister phải cập nhật cả hai; Clear dùng khi reset run.

## 🔴 `Enemy.cs` — một con quái

Đây là file dài. Đọc theo nhóm:

### 1. Sinh ra

`Initialize` nhận ID, data và vị trí; đặt máu, sprite, trạng thái rồi đăng ký vào Registry.

### 2. Máy trạng thái

`FixedUpdate` nhìn `_state`:

- Spawning: chờ hết báo trước;
- Chasing: gọi `Chase`;
- Attacking: gọi `Attack`;
- Dead: không làm gì.

### 3. Đuổi

`Chase` tìm player gần nhất, kiểm tra tầm, cộng hướng đuổi với lực giãn cách rồi `Step`.

### 4. Đánh

`Attack` kiểm tra mục tiêu còn hợp lệ và còn trong tầm; đủ thời gian thì phát event “chạm player”.

### 5. Trúng đòn/chết

`ApplyDamage` trừ HP và trả lời có chết không. `Kill` chỉ chạy một lần, tắt collider, phát event và rời Registry.

### 6. Sửa sai mạng

`ApplySnapshot` kéo mềm vị trí client về vị trí host thay vì giật thẳng.

### 7. Giãn cách

`Separation` đi qua các quái gần để tạo lực đẩy. Đây là toán vector; lần đọc đầu chỉ cần hiểu mục tiêu “không cho 40 quái chồng thành một chấm”.

## 🔴 `EnemySpawner.cs` — cổng sinh và đồng bộ quái

**Sinh:** lấy quái từ pool, cấp ID tăng dần và gọi Initialize.

**Damage/kill:** host tìm quái theo ID, quyết định chết và gửi sự kiện chết xuống client.

**Snapshot:** host định kỳ gửi cặp ID/vị trí; client tìm đúng quái và gọi ApplySnapshot.

**Reset:** trả mọi quái về pool, xoá Registry, đặt lại bộ đếm ID.

**Ý chính:** quái không phải NetworkObject riêng; Spawner là đầu mối mạng cho cả đàn.

---

# E. Net — cửa vào multiplayer

## 🔴 `NetworkManagerLAC.cs` — lễ tân kết nối

**Là ai?** Class mở rộng NetworkManager của Mirror.

**Khi người chơi vào:** chọn CharacterData, tạo player prefab, gán dữ liệu cần thiết rồi thêm player vào mạng.

**Khi rời/reset:** dọn registry và trạng thái chung để lần chơi sau không giữ dữ liệu cũ.

**Cách đọc:** phân biệt method chạy server với method chạy client. Đừng cố học toàn API Mirror trong một buổi.

---

# F. UI và camera

## 🟡 `PlayerHud.cs` — màn hình máu

**Làm gì?** Tìm local player, nghe `HealthChanged`, tạo đủ ô máu và bật/tắt chúng theo HP.

**Không làm gì?** HUD không sửa HP; nó chỉ nhìn dữ liệu rồi vẽ.

**Cleanup:** khi bị huỷ phải ngừng nghe event của PlayerHealth.

## 🟡 `CameraFollow.cs` — camera bám người chơi

**Tìm ai?** Local player trong PlayerRegistry.

**Làm gì?** `LateUpdate` làm camera đi mềm tới mục tiêu, kẹp trong ArenaBounds và cộng rung cục bộ.

**Vì sao LateUpdate?** Chờ player di chuyển xong trong frame rồi camera mới theo.

**Random rung hợp lệ:** nó chỉ đổi hình ảnh, không đổi kết quả gameplay.

---

# G. VFX — cảm giác khi chơi

## 🟢 `SpriteFlash.cs` — nháy màu khi trúng

`Flash` nhớ màu gốc và đổi sang màu hit. `Update` hết giờ thì trả màu. `OnDisable` cũng trả màu để object tái sử dụng không bị nhuộm mãi.

## 🟢 `DashAfterimage.cs` — bóng mờ sau Dash

`Play` chép sprite/màu/hướng lật từ player. `Update` làm alpha mờ dần. Hết đời thì trả chính nó về pool.

## 🟡 `DamageNumber.cs` — số damage bay lên

`Play` đặt số, màu, vị trí và vận tốc. `Update` cho số bay/chậm dần/mờ dần. Hết thời gian thì Release.

Random ở đây chỉ làm lệch hình trang trí nên không cần RunRandom.

## 🟡 `PixelNumber.cs` — ghép chữ số từ sprite

**Giữ gì?** Sprite 0–9 và các ô hiển thị.

**Làm gì?** Tách một số thành từng chữ số bằng chia và lấy dư, đặt sprite đúng rồi căn giữa.

Ví dụ 123 được tách từ phải sang trái: 3, 2, 1; sau đó hiển thị lại đúng thứ tự.

## 🟡 `HitStop.cs` — dừng hình cực ngắn

`Request` yêu cầu dừng tối đa vài mili giây. `Tick` dùng thời gian không bị ảnh hưởng bởi `timeScale` để biết lúc phục hồi.

**Học gì?** Utility static không tự nhận Update; cần một MonoBehaviour gọi Tick mỗi frame.

## 🟡 `HitFeedback.cs` — đạo diễn phản hồi đòn đánh

**Nghe gì?** Quái bị thương, quái chết, player bị thương.

**Làm gì?** Điều phối flash, knockback, số damage, hit-stop và camera shake.

**Lý do tách:** Enemy và DamageSystem không cần biết hiệu ứng phải mạnh/yếu ra sao.

## 🟡 `SoundWave.cs` — các vòng sóng âm

`Play` nhận bán kính và màu. `Update` tính tuổi của từng vòng, thay scale/alpha/rotation rồi trả pool khi vòng cuối kết thúc.

Đây là phần nhìn cục bộ; nó không gây damage.

---

# H. Shader và dữ liệu không phải C#

## 🔴 `SpriteAdditive.shader` — cách pixel phát sáng

C# quyết định lúc nào tạo sóng, màu và kích thước. Shader quyết định mỗi pixel hòa với nền ra sao.

`Blend SrcAlpha One` cộng màu vào nền để tạo cảm giác sáng. Lần học đầu chỉ cần hiểu luồng:

```text
vertex nhận điểm của sprite
→ đổi sang tọa độ màn hình
→ fragment đọc texture/màu
→ phép blend hòa pixel vào nền
```

Shader là một ngôn ngữ khác C#; chưa cần tự viết trước khi vững gameplay.

## `LACControls.inputactions` — bản đồ nút bấm

Đặt tên hai hành động `Move` và `Dash`, rồi nối chúng với WASD, phím mũi tên và gamepad. `PlayerInputReader` đọc tên hành động, không tự kiểm tra từng phím cứng.

## `Data/Characters/*.asset`

Các tờ CharacterData thật cho Thạch Sanh, Gióng và Tấm. Giá trị Inspector được Unity lưu vào file.

## `Data/Enemies/CoHon.asset`

Tờ EnemyData cho Cô Hồn. Sửa con số ở đây đổi chỉ số, không cần sửa thuật toán Enemy.

## `Scenes/Arena.unity` và các prefab

Chúng lưu việc component nào nối với object nào. Khi code báo thiếu prefab/renderer/registry, kiểm tra Inspector wiring trước khi sửa C#.

Không sửa YAML scene/prefab bằng tay nếu chỉ muốn thay một giá trị Inspector.

---

# Thứ tự đọc đề nghị

## Vòng 1 — lấy tự tin

1. `RunState.cs`
2. `EnemyState.cs`
3. `WeaponShape.cs`
4. `IPoolable.cs`
5. `CharacterData.cs`
6. `EnemyData.cs`
7. `ArenaBounds.cs`

## Vòng 2 — một object biết hành động

1. `PlayerInputReader.cs`
2. `PlayerMovement.cs`
3. `SpriteFlash.cs`
4. `DashAfterimage.cs`
5. `CameraFollow.cs`

## Vòng 3 — các object phối hợp

1. `PlayerRegistry.cs`
2. `EnemyRegistry.cs`
3. `GameEvents.cs`
4. `PlayerHealth.cs`
5. `PlayerHud.cs`
6. `DamageSystem.cs`

## Vòng 4 — luồng gameplay hoàn chỉnh

1. `WeaponAuto.cs`
2. `Projectile.cs`
3. `Enemy.cs` — mỗi buổi một nhóm method
4. `EnemySpawner.cs`
5. `PlayerDash.cs`

## Vòng 5 — hạ tầng khó

1. `ObjectPool.cs`
2. `PoolRegistry.cs`
3. `RandomStream.cs`
4. `RunRandom.cs`
5. `RunManager.cs`
6. `WaveManager.cs`
7. `NetworkManagerLAC.cs`
8. `SpriteAdditive.shader`

---

# Tiêu chuẩn “đã hiểu một file”

Bạn không cần thuộc từng dòng. Bạn đã hiểu khi có thể:

1. nói một câu file chịu trách nhiệm gì;
2. kể dữ liệu đi vào và kết quả đi ra;
3. chỉ được hai method quan trọng nhất;
4. dự đoán một thay đổi nhỏ sẽ ảnh hưởng gì;
5. đặt breakpoint hoặc log để kiểm tra dự đoán;
6. nói file nào gọi nó và nó gọi ai tiếp.

Nếu chỉ mô tả được cú pháp nhưng không kể được vai trò trong game, hãy quay lại bài 02 và lần theo một luồng. Nếu kể được vai trò nhưng không hiểu một dòng, quay lại đúng mục cú pháp ở bài 01.

Tài liệu này bao phủ toàn bộ 37 file `.cs` và 1 file `.shader` first-party hiện có trong `Assets/_LAC`. Nó là bản đồ để học; code thật vẫn là nguồn chính xác cuối cùng.

# NETCODE — Luật đồng bộ

> **Đọc file này trước khi động vào bất cứ thứ gì liên quan tới spawn, sát thương, hoặc trạng thái chia sẻ.**
> Đây là phần rủi ro nhất của dự án. Sai kiến trúc ở đây thì tuần 12 không cứu được.

---

## 1. Mô hình

**Host-authoritative.** Một người chơi là host (vừa chạy server vừa chơi). Host là chân lý duy nhất về máu, sát thương, cái chết, và cooldown Trống Đồng. Client mô phỏng phần hình ảnh và dự đoán nhân vật của chính mình.

```
Chơi đơn = Mirror host mode, 1 client
Chơi đôi = Mirror host mode, 2 client
         --> MỘT code path duy nhất, không có nhánh riêng cho chơi đơn
```

Thư viện: **Mirror** + **FizzySteamworks** transport + **Steamworks.NET**.
Ngưỡng chịu trễ mục tiêu: **150ms**.

---

## 2. Bảng đồng bộ — tra cứu nhanh

| Đối tượng | Đồng bộ? | Cơ chế |
|---|---|---|
| Vị trí người chơi | ✅ | `NetworkTransform`, client dự đoán nhân vật mình |
| Máu người chơi | ✅ | `SyncVar` do host ghi. Client **không bao giờ** tự trừ |
| Nhân vật đã chọn | ✅ | Đồng bộ một lần khi vào ván |
| Seed của ván | ✅ | Đồng bộ một lần khi vào ván |
| Số hiệu đợt hiện tại | ✅ | `SyncVar`, host điều khiển |
| Đặc tả đợt (`WaveSpec`) | ✅ | Host gửi, hai máy tự spawn từ seed |
| Vị trí quái | ⚠️ | Hai máy tự mô phỏng. Host gửi **snapshot 2 lần/giây** để sửa trôi |
| Quái chết | ✅ | Host phát RPC. Client không tự quyết |
| **Đạn** | ❌ | **Spawn cục bộ.** Đạn phía client thuần trang trí, không gây sát thương |
| Sát thương | ✅ | Host-only, qua `DamageSystem.Apply()` |
| Thẻ đã chọn | ✅ | Đồng bộ **id thẻ**, hai máy tự áp hiệu ứng |
| Cooldown Trống Đồng | ✅ | `SyncVar`, **dùng chung cho cả hai người** |
| Hồn (soul) | ⚠️ | Spawn cục bộ khi quái chết. Host xác nhận việc nhặt |
| VFX, hit-stop, camera shake, âm thanh | ❌ | Hoàn toàn cục bộ |

**Một câu để nhớ:** *host mô phỏng sự thật, client mô phỏng hình ảnh, chỉ đối chiếu thứ ảnh hưởng thắng thua.*

---

## 3. Ba sai lầm sẽ giết dự án

### ❌ Sai lầm 1 — gắn NetworkIdentity lên đạn

Ván cuối có ~200 viên đạn. 200 `NetworkIdentity` = vỡ băng thông, giật, desync. Đây là thứ AI sẽ tự động làm nếu bạn chỉ nói "thêm multiplayer".

**Đúng:** đạn spawn cục bộ từ trạng thái vũ khí đã đồng bộ. Vũ khí bắn theo chu kỳ cố định + `RunRandom` có seed → hai máy sinh ra cùng bộ đạn. Host là bên duy nhất tính va chạm gây sát thương; đạn bên client chỉ để nhìn.

### ❌ Sai lầm 2 — client tự trừ máu

Nếu client tự trừ máu mình, hai máy sẽ lệch trong vài giây và người chơi thấy mình chết trong khi máy kia thấy còn sống.

**Đúng:** mọi sát thương đi qua `DamageSystem.Apply()` (host-only). Client nhận `SyncVar` máu + RPC hiệu ứng bị đánh.

### ❌ Sai lầm 3 — chỉ test trên localhost

Localhost có độ trễ 0ms. Mọi thứ chạy hoàn hảo. Rồi tuần 15 bạn test với bạn bè ở tỉnh khác và game vỡ vụn — không còn thời gian sửa.

**Đúng:** bật `LatencySimulation` transport của Mirror **ngay từ tuần 1**, mặc định **100ms + 2% mất gói** cho mọi lần chạy trong Editor. Bạn sẽ khó chịu ngay từ đầu — đó chính là mục đích.

---

## 4. Mẫu code chuẩn

### 4.1 Client xin làm gì đó → host quyết → phát về

```csharp
// Client: dash chạm trống
[Command]
void CmdTryActivateDrum()
{
    if (!_isReady) return;          // host kiểm tra, không phải client
    _isReady = false;               // SyncVar -> tự lan về client
    _cooldownEnd = NetworkTime.time + CooldownSeconds;

    ApplyShockwave();               // host thi hành: xoá đạn, đẩy, choáng
    RpcPlayShockwaveVfx();          // client chỉ nhận phần hình ảnh
}

[ClientRpc]
void RpcPlayShockwaveVfx() { /* VFX + âm thanh, không có logic gameplay */ }
```

**Nguyên tắc:** `Command` = "tôi muốn", không phải "tôi đã". Client không bao giờ tự cho phép mình.

### 4.2 Ngẫu nhiên có seed

```csharp
// ĐÚNG — hai máy cho cùng kết quả
var card = RunRandom.Pick(_availableCards);

// SAI — desync
var card = _availableCards[UnityEngine.Random.Range(0, _availableCards.Count)];
```

Ngoại lệ duy nhất: ngẫu nhiên thuần trang trí (hướng bắn tia lửa, cao độ SFX). Nếu nghi ngờ, dùng `RunRandom`.

### 4.3 Sát thương

```csharp
// Mọi nơi đều gọi qua đây. Không class nào tự trừ máu.
public static void Apply(ITargetable target, float amount, DamageSource src)
{
    if (!NetworkServer.active) return;   // chỉ host
    ...
}
```

---

## 5. Trống Đồng trong co-op

Chi tiết vì đây là chỗ dễ sai nhất.

| | |
|---|---|
| Trạng thái | **Một** cooldown dùng chung, `SyncVar` trên host |
| Kích hoạt | Bất kỳ người chơi nào dash chạm trống |
| Tranh chấp | Hai người dash cùng lúc → host xử lý tuần tự, người thứ hai bị từ chối. **Không** kích hai lần |
| Hiển thị | Cả hai client thấy cùng một vòng nạp |
| Hiệu ứng | Host thi hành, phát RPC cho VFX |

Việc dùng chung là **chủ đích thiết kế**, không phải để tiết kiệm code — nó tạo khoảnh khắc phối hợp. Đừng "sửa" thành mỗi người một cái.

---

## 6. Trường hợp biên bắt buộc xử lý

- [ ] Client rớt giữa ván → host chơi tiếp một mình, không treo
- [ ] Host thoát → client về menu êm, có thông báo
- [ ] Client vào giữa ván → **cấm.** Chỉ ghép ở lobby
- [ ] Một người chết → hạ gục, người kia đứng cạnh 3 giây để hồi sinh
- [ ] Cả hai chết → thua ván
- [ ] Một người chưa chọn thẻ → đợt sau không bắt đầu; hết 10 giây thì tự chọn ngẫu nhiên
- [ ] Máu trùm nhân theo số người chơi

---

## 7. AI Đạo Diễn không chạy trong co-op

**Quyết định đã chốt:** `AIDirector` chỉ hoạt động ở chế độ chơi đơn. Co-op dùng `FixedWaveTable`.

Lý do:
1. Véc-tơ ngữ cảnh (`healthRatio`, `buildType`) định nghĩa cho **một** người chơi. Hai người khác build, khác máu → không có câu trả lời đúng.
2. Thực nghiệm khoá luận (15 vs 15) chạy ở chế độ chơi đơn. Thêm biến "số người chơi" làm nhiễu kết quả.
3. `FixedWaveTable` **vốn đã phải làm** — nó là nhóm đối chứng. Dùng lại cho co-op, không tốn thêm ngày nào.

Viết vào chương Hạn chế của khoá luận: *"Mở rộng đạo diễn cho nhiều người chơi đòi hỏi hàm mục tiêu đa tác nhân — nằm ngoài phạm vi nghiên cứu này."*

Nếu hội đồng yêu cầu chạy trong co-op: ngữ cảnh dùng `mean(healthRatio)` cho chính sách học, nhưng lớp an toàn dùng `min(healthRatio)` — luôn bảo vệ người yếu hơn.

---

## 8. Danh sách kiểm tra trước khi merge code có mạng

- [ ] Đã test với giả lập trễ 100ms, không phải localhost 0ms
- [ ] Đã test cả hai vai: làm host, và làm client
- [ ] Không có `NetworkIdentity` mới trên đạn hay VFX
- [ ] Không có `UnityEngine.Random` trong đường đi gameplay
- [ ] Client không tự thay đổi máu hay cooldown
- [ ] Đã thử rớt mạng giữa chừng, không treo

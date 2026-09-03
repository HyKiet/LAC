# LẠC — Children of the Dragon

**Tài liệu chủ đạo của dự án.** Mọi thành viên và mọi công cụ AI tham gia phát triển phải đọc tài liệu này trước khi thực hiện bất kỳ thay đổi nào lên mã nguồn.

Bộ tài liệu dự án gồm ba văn bản:

| Tài liệu | Nội dung |
|---|---|
| **CLAUDE.md** *(tài liệu này)* | Tổng quan sản phẩm, ràng buộc kiến trúc, quy ước lập trình, quy trình phát triển |
| [docs/TASKS.md](docs/TASKS.md) | Kế hoạch công việc, phân công, nhật ký hoàn thành |
| [docs/GDD.md](docs/GDD.md) | Đặc tả thiết kế chi tiết — chỉ số, nội dung, cân bằng |

Tài liệu tham chiếu bổ sung: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — sơ đồ hệ thống và quan hệ giữa các module.

---

## 1. Tổng quan sản phẩm

LẠC là một **arena survival roguelite 2D góc nhìn từ trên xuống**, đồ hoạ pixel art, xây dựng trên nền thần thoại Việt Nam. Sản phẩm thuộc dòng Vampire Survivors / Brotato.

| Hạng mục | Đặc tả |
|---|---|
| Nền tảng phát hành | PC — Steam |
| Giá bán | **$2.99** |
| Số người chơi | 1–2, **co-op trực tuyến** *(yêu cầu bắt buộc từ giảng viên hướng dẫn)* |
| Thời lượng một ván | 15 phút — 16 đợt quái |
| Engine | **Unity 6000.5.6f1**, URP 2D, C# |
| Kiến trúc mạng | Mirror + Steamworks.NET, mô hình **host-authoritative** |

> **Nền tảng đã chốt: PC — Steam. Không phát triển bản mobile.**
> Cơ sở: (a) co-op trực tuyến trên Steam sử dụng hạ tầng lobby và NAT traversal miễn phí của Steamworks — trên mobile phải thuê dịch vụ relay và xây dựng hệ thống tài khoản riêng; (b) mobile không có hạ tầng tương đương — hệ thống tài khoản, danh sách bạn bè và lời mời chơi cùng đều phải tự xây dựng, ước tính 10–15 ngày công cộng chi phí vận hành định kỳ; (c) ngân sách hiệu năng 40 quái và 200 đạn ở 60 FPS không đạt được trên thiết bị di động tầm trung; (d) sóng âm Đông Sơn phủ kín màn hình mất khả năng đọc hiểu trên màn hình 6 inch có ngón tay che khuất; (e) thị trường game trả phí một lần trên mobile gần như không còn khả năng tiếp cận người dùng mới.
> Nếu sản phẩm đạt doanh số trên Steam, bản mobile có thể xem xét ở giai đoạn sau — mã nguồn 2D URP không cản trở việc này.

### 1.1 Vòng lặp cốt lõi

```
Đợt quái (30–50 giây) → Chọn 1 trong 3 thẻ nâng cấp (10 giây)
→ Lặp lại 15 lần → Trùm Chằn Tinh ở đợt 16
```

Người chơi chỉ có **hai thao tác: di chuyển và lướt (dash)**. Vũ khí khai hoả tự động. Người chơi kiểm soát vị trí và thời điểm, không kiểm soát hành vi bắn.

### 1.2 Nhân vật

| Nhân vật | Vũ khí | Máu | Tốc độ | Tầm đánh | Chu kỳ |
|---|---|---|---|---|---|
| Thạch Sanh | Đàn bầu | 6 | 5 | 4 — vòng tròn | 0.9 s |
| Gióng | Roi sắt | 10 | 3 | 2.5 — hình cung | 0.6 s |
| Tấm | Sáo trúc | 4 | 8 | 7 — tia thẳng | 0.12 s |

Vũ khí gắn cố định với nhân vật và không thể thay thế trong ván. Việc đổi nhân vật tương đương đổi lối chơi.

---

## 2. Bốn cơ chế định vị

Thể loại survivors hiện đã bão hoà trên Steam. Bốn cơ chế dưới đây là yếu tố phân biệt sản phẩm và **không được cắt giảm trong bất kỳ trường hợp nào**.

### 2.1 Sóng âm Đông Sơn

Toàn bộ vũ khí trong game đều là nhạc cụ. Đòn đánh được biểu diễn dưới dạng **các vòng tròn đồng tâm lan toả** mang hoa văn trống đồng Đông Sơn. Càng về cuối ván, màn hình càng phủ kín sóng âm do chính người chơi tạo ra. Đây là hình ảnh chủ đạo dùng cho trailer và nội dung quảng bá.

> **Ràng buộc bắt buộc về khả năng đọc hiểu thị giác**
> Dành riêng **một màu trong bảng 24 màu Đông Hồ cho đòn tấn công của kẻ địch**. Hiệu ứng của người chơi tuyệt đối không sử dụng màu này, được vẽ ở alpha thấp với chế độ additive và nằm ở sorting layer thấp hơn. Đòn địch vẽ đặc, luôn ở sorting layer trên cùng.
> Không tuân thủ ràng buộc này, người chơi sẽ mất khả năng quan sát đạn địch từ khoảng đợt 10 trở đi.

### 2.2 Trống Đồng

Một trống đồng đặt cố định tại tâm đấu trường. Người chơi lướt vào trống để kích hoạt sóng xung kích: xoá toàn bộ đạn địch trên màn, đẩy lùi và gây choáng 1 giây. Thời gian hồi khoảng 20 giây.

> **Trong chế độ co-op, hai người chơi dùng chung một thời gian hồi.**
> Đây là quyết định thiết kế có chủ đích nhằm tạo ra tình huống phối hợp và thương lượng giữa hai người chơi. Trạng thái do host quản lý tập trung, không nhân bản trên từng client.

Tham chiếu thiết kế: cơ chế Blank của *Enter the Gungeon*, Teleporter của *Risk of Rain 2*, thùng tiếp tế của *Deep Rock Galactic*.

### 2.3 Tiến hoá thẻ — 8 công thức

Khi tích luỹ đủ các thẻ nền theo công thức định sẵn, hệ thống tự động hợp nhất thành một thẻ tiến hoá có sức mạnh vượt trội. Đây là **động lực chơi lại chính** của sản phẩm; số lượng công thức không được giảm xuống dưới 8.

| Nguyên liệu | Kết quả |
|---|---|
| Xuyên thấu ×3 + Nảy tường ×3 | Nỏ Thần |
| Nổ ×3 + Vệt cháy ×3 | Lửa Thiêng |
| +2 đạn ×3 + Tách đạn ×3 | Trăm Trứng |
| *5 công thức còn lại* | *Chốt tại tuần 4* |

### 2.4 Hồn

Quái vật khi bị tiêu diệt sẽ rơi ra "hồn", tự động hút về phía người chơi kèm hiệu ứng âm thanh tăng dần cao độ theo chuỗi nhặt liên tiếp. Hồn là nguồn nạp năng lượng cho Trống Đồng.

Cơ chế này cung cấp vòng phản hồi tích cực chu kỳ 2 giây — một yêu cầu bắt buộc của thể loại.

---

## 3. Ba ràng buộc kiến trúc bắt buộc

Ba ràng buộc dưới đây đã được xác định là các điểm thất bại nghiêm trọng nhất của dự án. Mọi thay đổi lên mã nguồn phải tuân thủ.

### 3.1 Không tồn tại nhánh mã riêng cho chế độ chơi đơn

```
Chơi đơn  = Mirror host mode, 1 client
Chơi đôi  = Mirror host mode, 2 client
          → Một luồng thực thi duy nhất
```

Kể cả khi kiểm thử một mình, game vẫn khởi chạy qua host mode. **Nghiêm cấm mọi câu lệnh rẽ nhánh dạng `if (isSinglePlayer)`.**

*Cơ sở:* việc tích hợp mạng vào một codebase đã hoàn thiện theo hướng chơi đơn là nguyên nhân đổ vỡ phổ biến nhất ở các dự án Unity quy mô sinh viên. Xây dựng kiến trúc mạng từ tuần đầu tiêu tốn khoảng 6 ngày công; tích hợp bổ sung ở tuần 10 tiêu tốn khoảng 15 ngày công và mang rủi ro không hoàn thành.

### 3.2 Đồng bộ sự kiện, không đồng bộ trạng thái

| Đối tượng | Đồng bộ | Cơ chế |
|---|---|---|
| Vị trí người chơi | Có | `NetworkTransform`; client dự đoán cục bộ nhân vật của mình |
| Máu, sát thương, cái chết | Có | **Host là thẩm quyền duy nhất.** Phát RPC theo sự kiện, không đồng bộ liên tục |
| Quái vật | Một phần | Đồng bộ seed và đặc tả đợt; hai máy tự sinh. Host gửi snapshot vị trí 2 lần/giây để hiệu chỉnh sai lệch |
| Lựa chọn thẻ | Có | Đồng bộ định danh thẻ; hai máy tự áp dụng hiệu ứng |
| Thời gian hồi Trống Đồng | Có | `SyncVar` do host quản lý, dùng chung |
| **Đạn** | **Không** | Sinh cục bộ. Đạn phía client chỉ mang tính biểu diễn |
| Hiệu ứng hình ảnh, hit-stop, rung màn, âm thanh | Không | Xử lý hoàn toàn cục bộ |

Nguyên tắc tóm lược: **host mô phỏng trạng thái thật, client mô phỏng biểu diễn.**

**Ba lỗi triển khai phải tránh:**

1. **Gắn `NetworkIdentity` lên đạn.** Ở giai đoạn cuối ván có khoảng 200 viên đạn đồng thời; đồng bộ toàn bộ sẽ làm quá tải băng thông và gây sai lệch trạng thái. *Đây là phương án mà công cụ AI sẽ tự động chọn nếu yêu cầu được diễn đạt chung chung như "thêm multiplayer".*
2. **Client tự trừ máu của chính mình.** Hai máy sẽ phân kỳ chỉ sau vài giây.
3. **Chỉ kiểm thử trên localhost.** Độ trễ 0 ms che giấu toàn bộ lỗi đồng bộ. **Bật giả lập độ trễ 100 ms làm cấu hình mặc định ngay từ tuần 1.**

**Mẫu triển khai chuẩn** — client yêu cầu, host thẩm định, host phát kết quả:

```csharp
[Command]
void CmdTryActivateDrum()
{
    if (!_isReady) return;      // Thẩm quyền kiểm tra thuộc về host
    _isReady = false;           // SyncVar tự lan truyền xuống client
    ApplyShockwave();           // Host thi hành logic
    RpcPlayVfx();               // Client chỉ nhận phần biểu diễn
}
```

### 3.3 Cấm sử dụng `UnityEngine.Random` trong luồng gameplay

Mọi phép ngẫu nhiên có ảnh hưởng đến gameplay phải đi qua `LAC.Core.RunRandom` — bộ sinh số có seed đồng bộ giữa các máy. Một lời gọi `Random.Range` nằm ngoài quy định sẽ gây phân kỳ trạng thái giữa host và client.

*Ngoại lệ duy nhất:* hiệu ứng hình ảnh và âm thanh thuần tuý trang trí.

---

## 4. Tổ chức mã nguồn

Toàn bộ mã nguồn và tài nguyên do nhóm phát triển nằm trong `Assets/_LAC/`. Các thư mục khác trong `Assets/` thuộc về package bên thứ ba và **không được chỉnh sửa**.

Asset bên thứ ba tải về đặt tại `Assets/ThirdParty/<tên gói>/`, không trộn vào `_LAC/`, để có thể xoá nguyên gói khi không cần nữa. Xem [docs/ASSETS_ThirdParty.md](docs/ASSETS_ThirdParty.md) cho các gói asset thử nghiệm hiện có.

```
Assets/_LAC/
├── Scripts/
│   ├── Core/      Vòng đời ván, quản lý đợt, object pool, RunRandom
│   ├── Player/    Di chuyển, dash, hệ thống máu
│   ├── Enemies/   Máy trạng thái quái vật, spawner
│   ├── Combat/    Sát thương, đạn, chọn mục tiêu
│   ├── Cards/     Bể thẻ, hiệu ứng, tiến hoá, giao diện chọn thẻ
│   ├── Director/  AI Đạo Diễn, thu thập telemetry
│   ├── Net/       Mirror, Steamworks, lobby
│   ├── Drum/      Trống Đồng
│   ├── UI/  VFX/  Audio/  Utils/
├── Data/          ScriptableObject — Cards, Characters, Enemies, Waves
├── Prefabs/  Art/  Audio/  Scenes/
```

Tệp mới phải được đặt đúng thư mục chức năng. Không tạo script tại thư mục gốc `Assets/`.

---

## 5. Quy ước lập trình

**Định danh**

| Thành phần | Quy ước | Ví dụ |
|---|---|---|
| Class, method, property | PascalCase | `PlayerDash`, `TryDash()` |
| Trường private | `_camelCase` | `_cooldownTimer` |
| Interface | Tiền tố `I` | `ITargetable` |
| ScriptableObject | Hậu tố `Data` | `CardData`, `EnemyData` |
| Namespace | Theo cấu trúc thư mục | `LAC.Core`, `LAC.Net` |

**Yêu cầu bắt buộc**

- Sử dụng `[SerializeField] private`; không khai báo trường `public`.
- Toàn bộ nội dung game — thẻ, quái, nhân vật, đợt — phải là ScriptableObject trong `Data/`. Không hard-code giá trị trong C#.
- Mọi đối tượng được sinh lặp lại (đạn, quái, hiệu ứng) phải đi qua `ObjectPool`. Không gọi `Instantiate` hoặc `Destroy` trong vòng lặp gameplay.
- Không gọi `GameObject.Find` hoặc `FindObjectOfType` trong vòng lặp gameplay.
- **Ngân sách hiệu năng: 60 FPS với 40 quái và 200 đạn hoạt động đồng thời.**
- Định danh trong mã dùng tiếng Anh; chú thích và văn bản hiển thị dùng tiếng Việt.
- Chú thích chỉ giải thích lý do của quyết định kỹ thuật, không diễn giải lại nội dung mã.

---

## 6. Quy trình phát triển

### 6.1 Quản lý phiên bản

```
main   Nhánh ổn định. Chỉ hợp nhất từ dev tại mỗi cổng nghiệm thu.
dev    Nhánh phát triển. Toàn bộ công việc hàng ngày diễn ra tại đây.
```

Đồng bộ nhánh `dev` vào đầu mỗi phiên làm việc (`git pull origin dev`). Đẩy mã lên remote ngay khi hoàn thành một hạng mục; không giữ thay đổi cục bộ quá một ngày làm việc.

Định dạng thông điệp commit:

```
<loại>(<mã công việc>): <mô tả ngắn gọn>

Ví dụ:  feat(T-11): dash có i-frame và thời gian hồi
Loại:   feat · fix · art · docs · balance · chore
```

### 6.2 Quy định về scene — bắt buộc

Tệp `.unity` và `.prefab` được lưu ở định dạng YAML và không hợp nhất tự động được bằng Git. Đây là nguyên nhân xung đột nghiêm trọng nhất trong các dự án Unity nhiều người.

1. **Tại một thời điểm chỉ một thành viên được chỉnh sửa scene.** Thông báo trên kênh trao đổi của nhóm trước khi bắt đầu và sau khi hoàn tất.
2. **Thực hiện mọi thay đổi trong prefab thay vì trong scene.** Scene chỉ chứa điểm sinh, camera, tilemap và các manager.
3. **Cài đặt UnityYAMLMerge một lần trên mỗi máy** — xem [README.md](README.md).

Khi xảy ra xung đột scene: không chỉnh sửa thủ công tệp YAML. Lấy phiên bản trên remote (`git checkout --theirs`) và thực hiện lại thay đổi trong Unity.

### 6.3 Thủ tục hoàn thành một hạng mục — bắt buộc

Ba thao tác sau phải nằm trong **cùng một commit** với mã nguồn:

1. Đánh dấu hoàn thành trong [docs/TASKS.md](docs/TASKS.md) (`Alt+C`), ghi tên người thực hiện và ngày.
2. Bổ sung **một dòng trích dẫn `>` ngay bên dưới**, mô tả chức năng và vị trí tệp.
3. Commit theo định dạng tại mục 6.1.

> **Áp dụng cho công cụ AI:** hai thao tác đầu là một phần của hạng mục, không phải bước tuỳ chọn. Không được báo cáo hoàn thành khi chưa thực hiện.

### 6.4 Hướng dẫn sử dụng công cụ AI

Nhóm phát triển chủ yếu bằng phương pháp AI-assisted. Ba quy định nhằm bảo toàn kiến trúc:

**a. Chỉ định tài liệu trước khi giao việc**

> *"Đọc CLAUDE.md. Sau đó thực hiện hạng mục T-11 trong docs/TASKS.md."*

**b. Cung cấp kiến trúc, không để công cụ tự quyết định**

| | |
|---|---|
| Không đạt | *"Thêm multiplayer vào game này."* → Công cụ sẽ gắn `NetworkIdentity` lên toàn bộ đối tượng |
| Đạt | *"Theo bảng đồng bộ tại mục 3.2, triển khai Trống Đồng với thời gian hồi dùng chung do host quản lý."* |

**c. Thẩm định trước khi hợp nhất**

1. Chức năng này đã tồn tại trong [docs/TASKS.md](docs/TASKS.md) chưa?
2. Có vi phạm ràng buộc nào tại mục 3 không?
3. Có lời gọi `Instantiate` trong `Update()` không?

---

## 7. Sai lệch so với GDD

[docs/GDD.md](docs/GDD.md) là bản đặc tả gốc và chứa một số nội dung đã lỗi thời. **Khi có mâu thuẫn, tài liệu này là căn cứ.**

| GDD | Quyết định hiện hành | Lý do |
|---|---|---|
| Unity 6.3 LTS · $6.99 | **6000.5.6f1** · **$2.99** | Phiên bản engine thực tế; định giá theo mặt bằng thể loại |
| 48 thẻ · 4 cấp độ khó | **32 thẻ nền + 8 tiến hoá** · **1 cấp** | Cân đối phạm vi theo nguồn lực 16 tuần. Cấp độ khó thứ hai chuyển sang giai đoạn sau bảo vệ để lấy quỹ thời gian cho đạo diễn co-op |
| Không có vật phẩm rơi ra | **Bổ sung cơ chế Hồn** | Thể loại yêu cầu vòng phản hồi chu kỳ ngắn |
| Không có Trống Đồng và tiến hoá thẻ | **Bổ sung — xem mục 2** | Yếu tố định vị sản phẩm |
| AI Đạo Diễn chạy ở mọi chế độ | **Hoạt động ở cả hai chế độ.** Co-op dùng véc-tơ ngữ cảnh hợp thành, tầng an toàn theo người yếu nhất và số hạng công bằng | Yêu cầu bắt buộc từ giảng viên hướng dẫn. Đặc tả đầy đủ tại [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) mục 2.7 |
| — | **Thực nghiệm khoá luận vẫn chạy ở chế độ chơi đơn** (15/15). Co-op thu thập mẫu quan sát bổ sung | Hai người chơi khác build và khác lượng máu tạo ra biến số không kiểm soát được với cỡ mẫu 30. Chế độ hoạt động và thiết kế thí nghiệm là hai vấn đề tách rời |
| Đạo diễn giữ tổn thất máu trong 15–25% ở mọi thời điểm | **Điều tiết bất đối xứng:** được phép giảm áp lực khi người chơi gặp khó; khi người chơi mạnh thì thay đổi **thành phần và hướng sinh quái**, không tăng số lượng hoặc lượng máu | Siết tổn thất máu ở cả hai chiều là trừng phạt người chơi vì xây dựng build hiệu quả, làm triệt tiêu cảm giác tưởng thưởng |
| Triển khai co-op ở tuần 10–12 | **Kiến trúc mạng từ tuần 1** | Xem mục 3.1 |
| Tấm: nhân đôi sát thương trong 1 giây sau dash | **Áp dụng cho phát bắn kế tiếp** | Thời gian hồi dash 0.4 s ngắn hơn cửa sổ 1 giây, khiến hiệu ứng duy trì vĩnh viễn |

---

## 8. Môi trường phát triển

- **Unity 6000.5.6f1.** Toàn nhóm bắt buộc dùng đúng phiên bản này; phiên bản khác sẽ nâng cấp tệp project và gây xung đột cho các thành viên còn lại.
- **Unity-MCP** đã được cài đặt và cấu hình (88 công cụ khả dụng). Khi mất kết nối: mở Unity, đưa cửa sổ về foreground và chờ hoàn tất domain reload.

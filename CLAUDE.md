# LẠC — Children of the Dragon

**Tài liệu chủ đạo của dự án.** Mọi thành viên và mọi công cụ AI tham gia phát triển phải đọc tài liệu này trước khi thực hiện bất kỳ thay đổi nào lên mã nguồn.

Bộ tài liệu dự án gồm ba văn bản:

| Tài liệu | Nội dung |
|---|---|
| **CLAUDE.md** *(tài liệu này)* | Tổng quan sản phẩm, ràng buộc kiến trúc, quy ước lập trình, quy trình phát triển |
| [docs/TASKS.md](docs/TASKS.md) | Kế hoạch công việc, phân công, nhật ký hoàn thành |
| [docs/GDD.md](docs/GDD.md) | Đặc tả thiết kế chi tiết — chỉ số, nội dung, cân bằng |

Tài liệu tham chiếu bổ sung: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — sơ đồ hệ thống và quan hệ giữa các module.

Công cụ AI khác Claude Code đọc [AGENTS.md](AGENTS.md) làm điểm vào — file đó tóm tắt ràng buộc, trạng thái hiện tại và các bẫy đã từng mất nhiều giờ.

---

## 1. Tổng quan đồ án

LẠC là một **arena survival roguelite 2D góc nhìn từ trên xuống**, đồ hoạ pixel art, xây dựng trên nền thần thoại Việt Nam. Gameplay tham chiếu dòng Vampire Survivors / Brotato; chỉ bàn giao đồ án, không phát hành thương mại.

| Hạng mục | Đặc tả |
|---|---|
| Nền tảng bàn giao | PC Windows — bản chạy độc lập cho đồ án |
| Phát hành / giá bán | **Không phát hành thương mại, không định giá** |
| Số người chơi | 1–2, **co-op trực tuyến** *(yêu cầu bắt buộc từ giảng viên hướng dẫn)* |
| Thời lượng một ván | Mục tiêu 8–10 phút — 10 đợt quái |
| Engine | **Unity 6000.5.6f1**, URP 2D, C# |
| Kiến trúc mạng | Mirror + KCP, mô hình **host-authoritative** |

> **Quyết định 11/10/2026: chỉ làm đồ án tốt nghiệp, không phát hành game.** Yêu cầu giảng viên đã biết: online multiplayer, có AI, có yếu tố mới lạ. Người dùng xác nhận không bắt buộc học máy/LinUCB hoặc 30 người thực nghiệm. Không yêu cầu Steamworks, Steam Direct, giá bán hoặc mục tiêu thị trường. Chi tiết đánh giá, phạm vi và tiêu chí bàn giao tại [docs/THESIS_SCOPE.md](docs/THESIS_SCOPE.md).

> AI gameplay dùng **đạo diễn dựa trên luật + FSM ba trạng thái**, hoạt động với một/hai người trên host. T-45/T-45B/T-46 đã có mã: luật trong ScriptableObject, snapshot đội trước thẻ/hồi máu, nguồn WaveSpec trên host, HUD/F3 và CSV schema 2. Giữ bảng cố định để đối chứng/fallback; bằng chứng nghiệm thu tại [docs/DIRECTOR_TESTS.md](docs/DIRECTOR_TESTS.md). Điểm riêng chính: Trống Đồng dùng chung được Hồn nạp và dash kích hoạt; không bắt buộc tám tiến hoá hoặc nhiều map.

> **Vai trò theo quyết định 11/10/2026: AI đạo diễn chính là Chằn Tinh, chủ dungeon.** Hắn điều binh trong các wave đầu, ra trận ở wave 10; HUD thể hiện ý đồ/lời thoại từ kết quả host, F3 vẫn giải thích luật thật. Không đổi ngưỡng/an toàn hoặc FSM chiến đấu boss chỉ để diễn lời thoại. Giải thích cho nhóm tại [docs/AI_CHAN_TINH.md](docs/AI_CHAN_TINH.md).

### 1.1 Vòng lặp cốt lõi

```
Sinh tồn (20–60 giây) → Chọn 1 trong 3 thẻ nâng cấp (tối đa 10 giây)
→ 9 đợt thường / 9 lượt nâng cấp → Trùm Chằn Tinh ở đợt 10
```

Người chơi chỉ có **hai thao tác: di chuyển và lướt (dash)**. Vũ khí khai hoả tự động. Người chơi kiểm soát vị trí và thời điểm, không kiểm soát hành vi bắn.

Đợt thường kết thúc theo đồng hồ mạng của host, quái tiếp tục vào sân đến hết giờ. Dọn quái/đạn khi nghỉ, giữ Hồn đã rơi. Boss phải bị hạ để thắng. Nhịp sinh và thời lượng nằm trong WaveTable; thay đổi 11/10/2026 theo hướng Brotato, giữ vũ khí cố định/thẻ/Trống Đồng. Cửa hàng và nhiều ô vũ khí chưa thuộc triển khai này.

### 1.2 Nhân vật

| Nhân vật | Vũ khí | Máu | Tốc độ | Tầm đánh | Chu kỳ |
|---|---|---|---|---|---|
| Thánh Gióng (Warrior) | Tre ngà | 10 | 4 | 2.2 — cung 110° | 0.65 s |
| Archer | Cung | 6 | 5.5 | 8 — tên bay thẳng | 0.7 s |
| Lancer | Thương | 8 | 4.5 | 3.2 — dải đâm rộng 0.8 | 0.85 s |

Vũ khí gắn cố định với nhân vật và không thể thay thế trong ván. Việc đổi nhân vật tương đương đổi lối chơi.

Ngày 18/10/2026: Thánh Gióng từ ảnh AI đã chuẩn hóa thay mỹ thuật Warrior, giữ ID mạng, chỉ số và vùng quét; Archer/Lancer không đổi. Nguồn/pipeline tại [ASSETS_ThirdParty.md](docs/ASSETS_ThirdParty.md).

Quyết định 07/10/2026: dùng ba lớp TinySwords thay nhân vật truyền thuyết để giảm công mỹ thuật. Ba lớp mở sẵn; chọn trước khi khởi động host/client trong `UI/CharacterSelection.cs`. ID được host thẩm định trước khi sinh player. Solo vẫn Mirror host, co-op cho phép chọn trùng lớp. Archer nhân đôi tên kế tiếp sau dash; Warrior/Lancer khác nhau ở hình dạng sát thương. Chi tiết hiện hành: GDD mục 4 và 10.

---

## 2. Cơ chế giữ trong đồ án

Giữ các cơ chế đã chạy để chứng minh gameplay/co-op; phần tương lai được thu gọn theo [THESIS_SCOPE.md](docs/THESIS_SCOPE.md). Không dùng mục tiêu cạnh tranh Steam để bắt nhóm làm thêm nội dung. Trống Đồng/Hồn là điểm riêng, đạo diễn thích ứng là phần AI bổ sung; tám tiến hoá không còn bắt buộc.

### 2.1 Sóng âm Đông Sơn

Theo thay đổi phạm vi 07/10/2026, vũ khí người chơi là kiếm/cung/thương, biểu diễn đúng cung chém, mũi tên hoặc dải đâm. Sóng Đông Sơn vẫn dùng cho Trống Đồng và hiệu ứng hiện có; không bắt buộc vẽ thêm vòng sóng nhạc cụ trên ba đòn vật lý. Ưu tiên đọc đúng vùng sát thương và giữ tín hiệu đòn địch rõ.

> **Ràng buộc bắt buộc về khả năng đọc hiểu thị giác**
> Dành riêng **một màu trong bảng 24 màu Đông Hồ cho đòn tấn công của kẻ địch**. Hiệu ứng của người chơi tuyệt đối không sử dụng màu này, được vẽ ở alpha thấp với chế độ additive và nằm ở sorting layer thấp hơn. Đòn địch vẽ đặc, luôn ở sorting layer trên cùng.
> Không tuân thủ ràng buộc này, người chơi sẽ mất khả năng quan sát đạn địch từ khoảng đợt 10 trở đi.

### 2.2 Trống Đồng

Một trống đồng đặt cố định tại tâm đấu trường. Người chơi lướt vào trống để kích hoạt sóng xung kích: xoá toàn bộ đạn địch trên màn, đẩy lùi và gây choáng 1 giây. Thời gian hồi khoảng 20 giây.

> **Trong chế độ co-op, hai người chơi dùng chung một thời gian hồi.**
> Đây là quyết định thiết kế có chủ đích nhằm tạo ra tình huống phối hợp và thương lượng giữa hai người chơi. Trạng thái do host quản lý tập trung, không nhân bản trên từng client.

Tham chiếu thiết kế: cơ chế Blank của *Enter the Gungeon*, Teleporter của *Risk of Rain 2*, thùng tiếp tế của *Deep Rock Galactic*.

### 2.3 Thẻ nâng cấp — giữ 12 nền

Mười hai thẻ nền đã có, dùng lại giao diện/đồng bộ hiện hành. T-25/T-26 chuyển dự phòng: tối đa 1–2 tiến hoá dùng hiệu ứng sẵn nếu còn thời gian sau nghiệm thu AI và online. Không có yêu cầu tối thiểu tám công thức. Không thêm shop, nhiều ô vũ khí, tiền tệ hay bể thẻ mở khoá trong đồ án.

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
| Quái vật | Một phần | Wave tính giờ: host phát sự kiện sinh (ID/loại/vị trí); boss tự sinh từ seed. Snapshot roster/vị trí 2 lần/giây cho hiệu chỉnh và vào muộn |
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
│   ├── Net/       Mirror + KCP, luồng tạo/vào phòng
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

## 7. Quyết định phạm vi hiện hành

GDD đã viết lại theo phạm vi đồ án ngày 11/10/2026. Báo cáo kiểm thử cũ giữ nguyên cấu hình/ngày đã nghiệm thu; không dùng chúng để chứng minh tính năng mới.

| Thiết kế cũ | Quyết định hiện hành |
|---|---|
| PC Steam, $2.99, sản phẩm thương mại | Windows executable cho đồ án, không phát hành/định giá/Steam |
| LinUCB, fairness reward, bắt buộc 15/15 người | AI dựa trên luật + FSM ba trạng thái, ngữ cảnh đội theo người yếu nhất; khảo sát nhỏ và kiểm thử có kiểm soát |
| 12 thẻ + tối thiểu 8 tiến hoá | Giữ 12 nền, tiến hoá dự phòng tối đa 1–2 |
| Ba tileset/bốn bản nhạc/Ngọc/mở khoá | Một arena hiện có, âm thanh thiết yếu, mở sẵn ba lớp, không tiến trình vĩnh viễn |
| Co-op phải qua Steam; fallback local | Hai máy qua Internet bằng Mirror/KCP; không hạ online thành co-op cục bộ |
| `clearTime`, ép tổn thất 15–25% | Wave tính giờ; dùng HP thấp nhất, tổn thất thực, lần gục; không trừng phạt build mạnh |
| Bộ ba truyền thuyết | Warrior/Archer/Lancer TinySwords; ghi rõ nguồn asset |

Ba ràng buộc kiến trúc ở mục 3, ngân sách hiệu năng, gameplay 10 wave, thẻ/Hồn/Trống Đồng/hồi sinh và telemetry giữ nguyên. Mã đạo diễn theo phạm vi mới được mô tả tại GDD mục 7 và [DIRECTOR_TESTS.md](docs/DIRECTOR_TESTS.md); việc thu gọn không thay thế nghiệm thu Internet hoặc executable mới.

## 8. Môi trường phát triển

- **Unity 6000.5.6f1.** Toàn nhóm bắt buộc dùng đúng phiên bản này; phiên bản khác sẽ nâng cấp tệp project và gây xung đột cho các thành viên còn lại.
- **Unity-MCP** đã được cài đặt và cấu hình (88 công cụ khả dụng). Khi mất kết nối: mở Unity, đưa cửa sổ về foreground và chờ hoàn tất domain reload.

# Kế hoạch công việc — LẠC

Tài liệu này là căn cứ duy nhất về tình trạng công việc của dự án: phân công, tiến độ và mô tả các chức năng đã hoàn thành.

## Quy trình sử dụng

**Bước 1 — Nhận hạng mục.** Thay `Chưa phân công` bằng tên thành viên và đẩy lên remote ngay, tránh trùng lặp công việc.

**Bước 2 — Ghi nhận hoàn thành.** Chuyển `[ ]` thành `[x]` (đặt con trỏ tại dòng, nhấn `Alt+C`), bổ sung tên và ngày, sau đó viết **một dòng trích dẫn `>` ngay bên dưới** mô tả chức năng và vị trí tệp.

**Bước 3 — Commit.** Định dạng: `feat(T-05): dash có i-frame và thời gian hồi`

```markdown
Trước khi thực hiện:
- [ ] **T-05** Dash — i-frame, thời gian hồi, vệt mờ — Chưa phân công

Sau khi hoàn thành:
- [x] **T-05** Dash — i-frame, thời gian hồi, vệt mờ — @Kiet · 27/08
  > Dash 6 đơn vị trong 0.15 s, bất tử toàn bộ thời gian dash, hồi 0.4 s.
  > `Player/PlayerDash.cs`. Gọi `TryDash()` từ `PlayerController`.
```

Dòng trích dẫn là cơ sở để thành viên khác và công cụ AI xác định chức năng đã tồn tại, tránh triển khai trùng lặp. **Hạng mục thiếu dòng này không được tính là hoàn thành.**

**Thành viên:** `@Kiet` · `@Hung` · `@Kang` · `@artist`

---

# Cổng 1 — Tuần 1 đến 3

**Tiêu chí nghiệm thu:** hai người chơi cùng hoàn tất một ván qua mạng; phản hồi chiến đấu đạt yêu cầu về độ đã tay.

### Chuẩn bị môi trường

- [ ] **T-01** Cả ba máy clone repository và mở dự án bằng Unity 6000.5.6f1 không phát sinh lỗi — Chưa phân công
- [x] **T-02** Cài đặt Mirror (Asset Store, miễn phí) và dùng `KcpTransport` — @Kiet · 29/08
  > Mirror 96.0.1 nhập vào `Assets/Mirror/`. Thư viện commit thẳng vào repository thay vì để mỗi thành viên tự tải từ Asset Store, bảo đảm ba máy dùng đúng một phiên bản — lệch phiên bản thư viện mạng gây lỗi rất khó chẩn đoán.
  > `Transports/KCP/KcpTransport.cs` đáp ứng truyền tải; `Transports/Latency/LatencySimulation.cs` dành cho T-08.
  > Mirror tự thêm các định nghĩa tiền xử lý `MIRROR`, `MIRROR_96_OR_NEWER` vào ProjectSettings.

> **Steamworks.NET và FizzySteamworks đã dời sang T-40 ở tuần 10.** Mirror kèm `KcpTransport` đáp ứng đầy đủ Cổng 1 và Cổng 2, bao gồm giả lập độ trễ. Cài Steamworks sớm buộc mỗi lần chạy thử phải mở Steam và cần App ID — thứ chỉ có sau khi hoàn tất T-46B.

### Hạ tầng nền tảng

- [x] **T-03** `RunRandom` — bộ sinh số ngẫu nhiên có seed, nguồn ngẫu nhiên duy nhất của gameplay — @Kiet · 28/08
  > Bộ sinh Xorshift128 tự cài đặt, **không dùng `System.Random`** vì thuật toán nội bộ của nó thay đổi giữa các phiên bản .NET và không bảo đảm giống nhau trên mọi máy.
  > Ngẫu nhiên chia thành bốn kênh độc lập — `Enemies`, `Cards`, `Loot`, `Director` — để một hệ thống rút thêm hoặc thiếu một lần không làm lệch pha các hệ thống còn lại và gây phân kỳ host/client.
  > `Core/RunRandom.cs` (mặt tiền tĩnh, `Initialize(seed)` · `CreateSeed()` · `Reset()`), `Core/RandomStream.cs` (`NextUInt` · `NextFloat` · `Range` · `Chance` · `Pick` · `Shuffle`).
  > Đã kiểm chứng: cùng seed cho cùng dãy số, hai kênh khác nhau cho dãy khác nhau, 20.000 mẫu có trung bình 0.5021, `Shuffle` bảo toàn tập phần tử.
- [x] **T-04** `ObjectPool` — pool dùng chung cho đạn, quái và hiệu ứng — @Kiet · 28/08
  > `ObjectPool<T>` với `Get` · `Release` · `ReleaseAll` · `Clear`, hỗ trợ prewarm và cảnh báo khi phải cấp phát thêm giữa trận. Phát hiện và chặn lỗi trả về pool hai lần.
  > `PoolRegistry` tra cứu pool theo prefab qua bảng băm, loại bỏ nhu cầu gọi `FindObjectOfType` trong vòng lặp gameplay.
  > `IPoolable` với `OnSpawned` và `OnDespawned` — bắt buộc cho mọi đối tượng có trạng thái riêng theo lần sử dụng, vì đối tượng lấy từ pool giữ nguyên giá trị của lần trước.
  > `Core/ObjectPool.cs`, `Core/PoolRegistry.cs`, `Core/IPoolable.cs`.
- [x] **T-05** `RunManager` — vòng đời một ván: khởi tạo, 16 đợt, điều kiện thắng thua — @Kiet · 29/08
  > `NetworkBehaviour` do host giữ thẩm quyền. Ba `SyncVar`: seed, số đợt hiện tại, trạng thái ván (`Idle` · `WaveActive` · `CardSelection` · `Victory` · `Defeat`).
  > Client nhận seed qua hook và tự gọi `RunRandom.Initialize` — hai máy dùng chung một nguồn ngẫu nhiên mà không cần đồng bộ từng lần rút.
  > Không tự sinh quái và không tự đếm quái. Các hệ thống khác báo cáo vào qua `ReportWaveCleared` · `ReportCardSelectionComplete` · `ReportPlayerDown` · `ReportPlayerRevived`; sự kiện `WaveStarted` · `WaveCleared` · `RunEnded` dành cho phần biểu diễn cục bộ.
  > Chỉ hook của `_state` phát `WaveStarted`, không phải hook của `_currentWave` — hai `SyncVar` cùng đổi trong một lần cập nhật, nếu cả hai cùng phát thì đợt quái sẽ được sinh gấp đôi.
  > `Core/RunManager.cs`, `Core/RunState.cs`. Đặt component vào scene ở T-07.
- [x] **T-06** `CharacterData` (ScriptableObject) kèm asset cấu hình Thạch Sanh — @Kiet · 29/08
  > Chỉ số cơ bản, chỉ số vũ khí và chỉ số lướt nằm chung một tài sản: vũ khí gắn cố định với nhân vật và không thay thế được trong ván, tách ra thành tệp riêng chỉ thêm một lần trỏ mà không thêm khả năng phối hợp nào.
  > `WeaponShape` (`Circle` · `Arc` · `Line`) là thứ phân biệt ba nhân vật về lối chơi chứ không chỉ về con số.
  > `Player/CharacterData.cs`, `Combat/WeaponShape.cs`, tài sản `Data/Characters/ThachSanh.asset` — 6 máu, tốc độ 5, tầm 4, chu kỳ 0.9 s, lướt 6 đơn vị trong 0.15 s, hồi 0.4 s.

### Kiến trúc mạng — triển khai tại tuần 1

- [x] **T-07** `NetworkManagerLAC` vận hành ở host mode kể cả khi chơi đơn — @Kiet · 29/08
  > Kế thừa `NetworkManager`, tự khởi động host khi vào scene. Không có nhánh mã nào phân biệt chơi đơn với chơi đôi.
  > Scene `Scenes/Arena.unity` chứa `NetworkManagerLAC` + `KcpTransport` + `LatencySimulation` + `NetworkManagerHUD`, và đối tượng `RunManager` kèm `NetworkIdentity`.
  > `autoCreatePlayer` tạm tắt vì chưa có prefab nhân vật — bật lại ở T-09.
  > `Net/NetworkManagerLAC.cs`. Đã kiểm chứng khi chạy: server và client đều hoạt động, `RunManager.isServer` đúng, 16 đợt chạy hết và cho `Victory`, hết người chơi cho `Defeat`.
- [x] **T-08** Kích hoạt giả lập độ trễ 100 ms làm cấu hình mặc định khi phát triển — @Kiet · 29/08
  > `LatencySimulation` bọc quanh `KcpTransport`, độ trễ 100 ms, jitter 0.02, thất thoát gói 2% ở kênh unreliable.
  > Việc chọn truyền tải nằm trong mã (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`) chứ không phụ thuộc vào người sửa tay trong Inspector trước khi build — lớp giả lập lọt vào bản phát hành sẽ khiến mọi người chơi thật chịu thêm 100 ms.
  > Không tắt component giả lập bằng cách bỏ tick `enabled`: `OnDisable` của nó tắt luôn truyền tải bên dưới.
- [x] **T-09** Sinh 1–2 nhân vật; hai máy cùng vào được một ván — @Kiet · 30/08
  > `Player/PlayerCharacter.cs` · `Player/CharacterRegistry.cs` · `Player/PlayerRegistry.cs` · `Prefabs/Player/Player.prefab`.
  > Một prefab người chơi duy nhất cho cả ba nhân vật. Mạng chỉ truyền `CharacterId` qua `SyncVar`; mỗi máy tự tra `CharacterRegistry` và tự áp dụng chỉ số — mẫu "đồng bộ định danh" ở CLAUDE.md mục 3.2. Nhờ vậy cân bằng lại chỉ số chỉ là sửa ScriptableObject, không đụng tới đường truyền.
  > `NetworkManagerLAC.OnServerAddPlayer` phân nhân vật theo thứ tự nối vào và sinh tại `NetworkStartPosition`. Định danh được gán **trước** `AddPlayerForConnection` để nó nằm trong gói trạng thái ban đầu; gán sau sẽ tạo ra khoảng thời gian nhân vật đã hiện nhưng chưa có chỉ số.
  > `RunManager.RegisterPlayer` khởi động ván khi người đầu tiên vào; người thứ hai vào sau chỉ tăng số người còn sống, không khởi động lại ván.
  > `PlayerRegistry` là danh sách tĩnh cục bộ, thay cho `FindObjectOfType` bị cấm ở mục 5. Quái vật ở T-14 tra mục tiêu qua đây.
  > `NetworkTransformReliable` đặt `ClientToServer` để client dự đoán cục bộ nhân vật của mình.
  > Đã kiểm chứng khi chạy: nhân vật sinh tại điểm sinh, chỉ số áp đúng (Thạch Sanh 6 máu, Gióng 10 máu), ván tự vào đợt 1, người thứ hai vào không kéo ván về đợt 1, một người gục thì ván tiếp tục và cả hai gục mới `Defeat`.
  > Sprite trong `Art/Placeholder/` là hình khối tạm, **không** thuộc bảng 24 màu Đông Hồ — thay ở T-17.

### Cơ chế chiến đấu

- [x] **T-10** Di chuyển 8 hướng, hỗ trợ bàn phím và tay cầm — @Kiet · 30/08
  > `Player/PlayerInputReader.cs` · `Player/PlayerMovement.cs` · `Utils/CameraFollow.cs` · tài sản `Data/Input/LACControls.inputactions`.
  > Bản đồ thao tác `Gameplay` với `Move` (WASD, phím mũi tên, cần trái, D-pad) và `Dash` (Space, Shift trái, nút Đông, R1) — `Dash` khai báo sẵn cho T-11.
  > Đọc thiết bị tách khỏi di chuyển: sơ đồ phím thay đổi khi thêm thiết bị, cách di chuyển thay đổi khi cân bằng lối chơi. Gộp lại thì mỗi lần thêm một nút là một lần đụng vào mã vật lý.
  > Vùng chết cần analog áp theo độ dài véc-tơ, không theo từng trục — áp theo trục sẽ cắt vuông góc và làm lệch hướng chéo khi cần gạt nghiêng nhẹ.
  > Client tự chạy vật lý cho nhân vật của mình và đẩy vị trí lên qua `NetworkTransformReliable` chiều `ClientToServer`. Bắt client hỏi host rồi mới được đi thì mỗi bước chân chờ trọn một vòng mạng — ở 100 ms là hỏng hoàn toàn cảm giác điều khiển. Host không thẩm định vị trí: LẠC hợp tác, không đối kháng, nên gian lận vị trí chỉ ảnh hưởng chính ván của người đó. Máu và sát thương thì ngược lại, xem T-13.
  > Nhân vật của người khác chuyển `Rigidbody2D` sang `Kinematic` — để vật lý động chạy trên nó thì vật lý và mạng tranh nhau ghi transform, hình ảnh giật liên tục.
  > `CameraFollow` bám nhân vật cục bộ, mỗi máy một camera. Camera chung sẽ buộc hai người chơi luôn ở gần nhau, mâu thuẫn với lối chơi chạy vòng tránh đám quái.
  > Đã kiểm chứng khi chạy bằng tay cầm ảo: cần gạt (0.6, 0.8) cho tốc độ đo 4.90 so với chuẩn 5 và hướng khớp tuyệt đối; D-pad chéo và D-pad thẳng cho tỉ lệ tốc độ 1.000 — đi chéo không nhanh hơn đi thẳng; thả nút thì vận tốc về 0 và hướng nhìn được giữ nguyên; camera bám lệch 0.000.
  > Đường bàn phím đã được @Kiet bấm thử tay và xác nhận chạy. Khung kiểm thử tự động không bơm phím giả vào được: Unity Editor không định tuyến sự kiện bàn phím vào play mode khi cửa sổ Game mất focus.
- [x] **T-10B** Đấu trường — nền lát gạch, biên va chạm, `ArenaBounds` — @Kiet · 30/08
  > Hạng mục **bổ sung ngoài kế hoạch gốc**: kế hoạch không có mục nào dựng đấu trường, mà mọi hạng mục sau đều cần. T-11 không nhìn thấy vệt dash trên nền trống, T-14 không biết sinh quái ở đâu, và T-10 không tự kiểm chứng được camera vì không có gì cố định làm mốc.
  > `Core/ArenaBounds.cs` — nguồn duy nhất cho câu hỏi "bên trong sân là ở đâu". Camera, bộ sinh quái và đạn nảy tường đều cần cùng con số; để mỗi hệ thống giữ một bản sao thì chỗ quên sửa sẽ biểu hiện thành quái sinh ngoài tường, trông như lỗi AI chứ không như lỗi cấu hình. Kích thước là dữ liệu của scene nên không đồng bộ qua mạng.
  > Sân 36×20 đơn vị, tâm tại gốc toạ độ. Nền lát bằng `Tilemap` xen kẽ hai ô sáng tối để nhìn ra chuyển động; viền tường dày 1 ô ngoài vùng chơi.
  > Va chạm dùng **bốn `BoxCollider2D`** thay cho `TilemapCollider2D` — bốn hộp là bốn hình, còn tilemap collider sinh hàng trăm hình và phải dựng lại mỗi khi đổi một ô.
  > `CameraFollow` kẹp khung hình theo nửa khung hình thật (phụ thuộc tỉ lệ màn hình) chứ không theo lề cứng — màn hình siêu rộng sẽ nhìn xuyên qua tường nếu dùng lề cứng. Nới thêm 1.5 đơn vị để vòng tường lọt vào khung: kẹp khít vào vùng chơi thì mép màn hình rơi đúng lên biên và người chơi bị chặn bởi một bức tường họ không nhìn thấy.
  > Đã kiểm chứng khi chạy: nhân vật đâm vào tường phải dừng ở `x=17.60` (biên 18 trừ bán kính 0.4) với vận tốc 0; camera kẹp đúng `±2.90` ngang và `±3.00` dọc; ở góc trên-phải mép khung hình đạt `19.50` / `11.50`, thấy rõ vòng tường.
  > Ô lát và màu nền là tạm, **không** thuộc bảng 24 màu Đông Hồ — thay ở T-17. Kích thước sân là con số đầu tiên, cần chỉnh lại khi có quái để đá thử.
- [x] **T-11** Dash — i-frame, thời gian hồi, vệt mờ — @Kiet · 30/08
  > `Player/PlayerDash.cs` · `VFX/DashAfterimage.cs` · prefab `Prefabs/VFX/DashAfterimage.prefab`.
  > Lướt 6 đơn vị trong 0.15 s, hồi 0.4 s, theo hướng đang nhìn. `PlayerMovement` nhường quyền đặt vận tốc trong pha lướt — không nhường thì hai thành phần cùng ghi vào một `Rigidbody2D` trong cùng một bước vật lý và cú lướt bị kéo ngược thành tốc độ đi bộ.
  > **Chạy theo quãng đường còn lại, không theo đồng hồ.** Bản chạy theo thời gian đo được 5.46 và 5.54 trên quãng đường đặt là 6, và hai lần lướt ra hai số khác nhau vì bước vật lý cuối bị cắt dở. Sai số đó thay đổi theo tốc độ khung hình, nên người chơi không bao giờ học được tầm lướt của mình — với một công cụ né đòn thì đó là hỏng. Bản chạy theo quãng đường đo đúng 6.000 ở cả bốn hướng.
  > Có hạn giờ thoát hiểm bằng hai lần thời gian lướt: khi tường chặn giữa chừng, quãng đường không bao giờ tiêu hết vì nhân vật không đi được, không có hạn giờ thì pha lướt kẹt vĩnh viễn.
  > **i-frame đi qua host.** Chuyển động lướt do client tự chạy như mọi chuyển động khác ở T-10, nhưng sát thương thuộc thẩm quyền host (mục 3.2) — chỉ client biết mình bất tử thì host vẫn trừ máu. Client lướt cục bộ ngay lập tức rồi gửi `CmdDash`; host mở cửa sổ bất tử riêng, có bù nửa RTT ở phần đuôi. `IsInvulnerable` trả về cửa sổ của host khi chạy trên host — đây là con số `DamageSystem` ở T-13 phải hỏi.
  > Hồi chiêu cũng được nới đúng bằng phần bù, nếu không thì độ trễ tự nó biến thành hình phạt: người chơi mạng kém sẽ lướt thưa hơn người chơi mạng tốt.
  > `RpcDashStarted` cho các máy còn lại vẽ vệt mờ của đồng đội — một gói tin cho một lần lướt, là đồng bộ sự kiện chứ không phải trạng thái. Cần thiết vì biết bạn mình vừa lướt là biết bạn mình sắp không lướt được trong 0.4 giây, thông tin quyết định khi hai người dùng chung Trống Đồng ở T-19.
  > Vệt mờ đi qua `ObjectPool`, vẽ ở alpha 0.45 và sorting order thấp hơn nhân vật theo ràng buộc đọc hiểu thị giác ở mục 2.1.
  > Đã kiểm chứng khi chạy: quãng đường 6.000 ở bốn hướng; bấm khi đang hồi không ăn; lướt lại được ngay khi hết hồi; lướt đâm tường thoát sau 0.157 s và dừng đúng `y=9.60`; ảnh mờ trả về pool hết (0 đang dùng, 8 nhàn rỗi).
  > **Chưa kiểm được độ lệch pha i-frame thật.** Đo trên host cho khe hở đầu 21 ms và dư đuôi 17 ms, nhưng host nói chuyện với chính mình qua kết nối cục bộ nên **không đi qua lớp giả lập độ trễ** — con số này không đại diện cho client thật. Phải đo lại khi chạy hai tiến trình ở T-12. Nếu chơi thử thấy rõ hiện tượng "đã né mà vẫn dính", phương án thay thế là để client tự quyết i-frame của mình, cùng lập luận đã dùng cho vị trí ở T-10.
- [x] **T-12** Vũ khí khai hoả tự động — chu kỳ bắn, chọn mục tiêu gần nhất — @Kiet · 30/08
  > `Combat/WeaponAuto.cs` · `Combat/Projectile.cs` · `VFX/PulseEffect.cs` · prefab `Prefabs/Projectiles/Projectile.prefab` và `Prefabs/VFX/PulseEffect.prefab`.
  > Ba hình dạng vũ khí đều chạy: `Circle` vòng tròn quanh người chơi (đàn bầu, tầm 4, chu kỳ 0.9), `Arc` hình cung nửa góc 60° (roi sắt, tầm 2.5, chu kỳ 0.6), `Line` bắn đạn về mục tiêu gần nhất (sáo trúc, tầm 7, chu kỳ 0.12).
  > **Đạn không mang `NetworkIdentity` và không đồng bộ.** Mỗi máy tự sinh đạn của mình; đạn phía client thuần tuý là hình ảnh, sát thương chỉ host quyết qua `DamageSystem`. Không có một dòng rẽ nhánh nào cho việc đó — `DamageSystem` tự bỏ qua khi không phải host, nên cùng một đoạn mã chạy đúng ở cả hai vai.
  > Va chạm của đạn tự kiểm bằng khoảng cách thay vì dùng trigger vật lý: 200 viên × 40 quái là 8000 phép so sánh mỗi bước, rẻ hơn nhiều so với 200 collider động sinh ra 200 lời gọi ngược mỗi khung hình.
  > Chỉ khai hoả khi có mục tiêu trong tầm — bắn vào chỗ trống làm mất ý nghĩa của tiếng động, người chơi phải nghe ra được rằng mình vừa chạm tới đám quái.
  > **Ngân sách hiệu năng đạt với biên độ lớn:** 200 đạn và 40 quái cùng lúc cho **3.56 ms/khung** trung bình, khung tệ nhất **5.50 ms**, so với ngưỡng 16.67 ms của 60 FPS.
  > **Một lỗi thiết kế phát hiện khi đo:** bản đầu cho hình cung nhắm theo **hướng di chuyển**. Gióng đứng yên đánh thì hướng nhìn kẹt ở giá trị cũ — đo được có mục tiêu trong tầm suốt ba giây mà không giết được con nào, vì quái đứng ở sườn còn cung vẫn chĩa xuống. Đã đổi thành nhắm theo mục tiêu gần nhất: vũ khí khai hoả tự động thì việc ngắm cũng phải tự động, người chơi chỉ kiểm soát vị trí. Kiểm lại: Gióng đứng yên với hướng nhìn chĩa xuống vẫn giết được quái đặt bên phải.
  > `PulseEffect` là **chỗ giữ chỗ cho sóng âm Đông Sơn ở T-16** — vòng trơn, alpha thấp, sorting order dưới nhân vật theo ràng buộc đọc hiểu ở mục 2.1.
  > **Ghi nhận về cân bằng, chưa sửa:** với Thạch Sanh 6 máu, đợt 1 sáu con Cô Hồn giết được một người chơi **đứng yên** trong khoảng 5–6 giây. Người chơi biết chạy vòng thì sống được, nhưng biên độ này rất hẹp cho đợt đầu tiên. Thuộc T-51.
- [x] **T-13** `DamageSystem` — điểm vào duy nhất cho mọi sát thương, thẩm quyền thuộc host — @Kiet · 30/08
  > `Combat/DamageSystem.cs` · `Player/PlayerHealth.cs`, đặt cùng đối tượng với `RunManager`.
  > **Vì sao phải gom về một chỗ:** sát thương đến từ quái chạm người, đạn trúng quái, sóng xung kích Trống Đồng, vệt cháy của thẻ tiến hoá. Nếu mỗi nguồn tự trừ máu thì mỗi quy tắc bất tử phải được nhớ lại ở từng nơi, và chỉ cần một nguồn quên kiểm tra i-frame là cú lướt né đòn mất tác dụng đúng trong tình huống đó.
  > Toàn bộ lớp chỉ chạy trên host. Lời gọi trên client bị bỏ qua **lặng lẽ chứ không báo lỗi**: client vẫn chạy cùng mã gameplay — đạn vẫn bay, quái vẫn đuổi — nên nó gọi vào đây rất thường xuyên, và đó là hành vi đúng.
  > `PlayerHealth` không có phương thức công khai nào trừ máu được; chỉ `DamageSystem` gọi được qua `internal`. Máu là `SyncVar` do host giữ, client chỉ đọc để hiển thị.
  > **Cửa sổ bất tử 0.6 giây sau khi trúng đòn là bắt buộc, không phải ưu ái:** cuối ván có 40 con vây quanh, mỗi con 1 sát thương, nên không có cửa sổ này thì Thạch Sanh 6 máu chết trong đúng một khung hình. Đo được: 40 đòn trong một khung chỉ mất 1 máu.
  > `IsInvulnerable` gộp hai nguồn: đang lướt (T-11) và vừa trúng đòn. Đây là chỗ khép lại mối nối để hở ở T-11.
  > Việc tắt điều khiển khi chết do mỗi máy tự làm cho nhân vật của mình — chờ host gửi lệnh tắt thì trong một vòng mạng người chơi vẫn điều khiển được một xác chết.
  > Đã kiểm chứng khi chạy: đánh quái 4 còn 6/10, đánh thêm 6 thì chết, đánh con đã chết không có tác dụng; ba đòn liên tiếp lên người chơi chỉ đòn đầu ăn (6→5); 40 đòn trong một khung chỉ mất 1 máu (5→4); **đánh trong lúc lướt không ăn (4→4) với `batTuTheoHost=True`**; hết máu thì ván chuyển `Defeat` và đàn quái được giữ lại trên sân.
  > **Chưa làm:** hồi sinh giữa ván, và phản hồi hình ảnh khi trúng đòn (T-15).
- [x] **T-14** Quái vật đầu tiên (Cô Hồn) — hành vi truy đuổi và trạng thái chết — @Kiet · 30/08
  > `Enemies/Enemy.cs` · `Enemies/EnemyData.cs` · `Enemies/EnemyState.cs` · `Enemies/EnemyRegistry.cs` · `Enemies/EnemySpawner.cs` · `Core/GameEvents.cs` · tài sản `Data/Enemies/CoHon.asset` · prefab `Prefabs/Enemies/Enemy.prefab`.
  > Cô Hồn: 10 máu, tốc độ 3, truy đuổi trực tiếp — theo docs/GDD.md mục 6.1. FSM ba trạng thái `Spawning → Chasing → Attacking` cộng `Dead`. Pha báo trước 0.35 s đứng yên và không gây sát thương: quái hiện ra rồi lao vào ngay là không đọc kịp.
  > **Quái không mang `NetworkIdentity`.** Cuối ván có 40 con cùng lúc; đồng bộ từng con như đối tượng mạng sẽ ngốn hết băng thông. `EnemySpawner` là đối tượng mạng **duy nhất** cho toàn bộ đàn quái và giữ ba việc: hai máy cùng gọi `Spawn` theo cùng thứ tự nên định danh (bộ đếm tăng dần) tự khớp mà không cần gửi cho nhau; host gửi snapshot vị trí 2 lần/giây để kéo lại sai lệch tích luỹ; cái chết chỉ host quyết rồi phát RPC theo định danh. Chi phí snapshot: 40 con × 12 byte × 2/giây ≈ 1 KB/s.
  > Host thi hành cái chết ngay tại chỗ chứ không chờ RPC của chính mình quay về — RPC gửi từ host vẫn qua hàng đợi kết nối cục bộ nên tới muộn một lần cập nhật mạng, và trong khoảng đó host có thể đánh thêm một lần lên con quái đã chết. Đúng lỗi đã gặp ở `RunManager` tại T-05.
  > Di chuyển bằng `Rigidbody2D` Kinematic, **quái không xô đẩy nhau bằng vật lý**: kết quả giải va chạm giữa 40 vật thể phụ thuộc thứ tự xử lý của engine và phân kỳ rất nhanh giữa hai máy.
  > **Giãn cách tự viết** thay cho va chạm engine — là hàm thuần tuý của vị trí nên hai máy cho cùng một kết quả. O(n²), 1600 phép so sánh mỗi bước vật lý với 40 con.
  > Quái không tự trừ máu người chơi. Nó phát `GameEvents.EnemyTouchedPlayer` — một sự thật cục bộ; việc trừ máu là thẩm quyền host và do `DamageSystem` ở T-13 thi hành. **Sát thương chạm vì vậy chưa có hiệu lực cho tới khi T-13 xong.**
  > Đã kiểm chứng khi chạy: pha báo trước đứng yên đúng 0.35 s; tốc độ đuổi đo được 2.99 so với chuẩn 3; áp sát ở 0.68 với tầm 0.7 và chạm 3 lần trong 1.7 s đúng chu kỳ 0.8; đánh 4 rồi 6 thì chết, trả về pool, sự kiện chết phát đúng một lần và đánh tiếp con đã chết không phát thêm; 40 con chạy ở 2.90 ms/khung (345 FPS), còn xa ngân sách 16.6 ms.
  > **Hai lỗi phát hiện nhờ chụp màn hình chứ không nhờ đọc mã.** (a) Giãn cách chỉ chạy khi truy đuổi, nên vào tầm đánh là cả đàn dồn thành một khối — đo được 40 con trong bán kính 0.076, nhìn như một con duy nhất. (b) Sau khi cho giãn cách chạy cả lúc đứng đánh vẫn còn dồn, vì lực đẩy bị chuẩn hoá nên độ lớn bị vứt bỏ: dù chen chúc đến mấy nó vẫn là hằng số 0.9 trong khi lực hút là 1.0, hút luôn thắng. Sửa thành cộng cả độ lớn, mỗi hàng xóm góp tối đa 1 đơn vị giảm tuyến tính theo khoảng cách. Kết quả: 0 con chồng khít, khoảng cách trung bình 0.396, cả đàn trong bán kính 1.54.
  > **Chưa làm:** `EnemyRegistry.Clear` và `GameEvents.Clear` chưa được `RunManager` gọi khi kết thúc ván. Sprite là hình tròn tạm, không thuộc bảng Đông Hồ — thay ở T-18.
- [x] **T-14B** `WaveManager` — sinh quái theo đợt và kết thúc đợt — @Kiet · 30/08
  > Hạng mục **bổ sung ngoài kế hoạch gốc**: Cổng 1 lấy tiêu chí nghiệm thu là "hai người chơi cùng hoàn tất một ván", nhưng không có hạng mục nào nối `RunManager` với `EnemySpawner`. Hệ quả trực tiếp: bấm Play thì sân trống, vì `Spawn` chỉ được gọi từ khung kiểm thử.
  > `Core/WaveManager.cs`, đặt cùng đối tượng với `RunManager`.
  > **Thành phần đợt do cả hai máy tự tính, không ai gửi cho ai.** `RunManager` đã đồng bộ seed và số đợt; từ hai con số đó mỗi máy rút ra cùng một đặc tả và gọi `Spawn` theo cùng thứ tự. Gửi danh sách quái qua mạng là tốn băng thông cho thứ hai bên đều tự suy ra được.
  > **Mỗi đợt dùng một luồng ngẫu nhiên riêng**, gieo từ seed của ván cộng số hiệu đợt, chứ không rút tiếp từ `RunRandom.Enemies`. Lý do: người thứ hai vào giữa ván không có lịch sử rút của các đợt trước, nên với luồng nối tiếp họ sẽ tính ra một đợt hoàn toàn khác. Luồng theo đợt làm kết quả chỉ phụ thuộc số hiệu đợt.
  > Quái sinh sát biên sân chứ không quanh người chơi — hiện ra ngay cạnh người chơi là đòn không né được.
  > Số lượng: 6 con ở đợt 1, cộng 2 mỗi đợt, trần 40. Đây là con số giữ chỗ; bảng đợt thật ở T-44 và AI Đạo Diễn ở T-45.
  > Chỉ host quyết thời điểm đợt kết thúc, vì chỉ host biết chắc con nào đã chết.
  > **Có một chỗ giữ tạm phải gỡ:** cờ `_autoAdvanceCardSelection` tự chuyển sang đợt kế tiếp sau 1.5 giây mà không cần chọn thẻ. Tắt cờ này khi T-22 và T-23 xong.
  > Đã kiểm chứng khi chạy: bấm Play là 6 con hiện ra ở biên và tiến vào; giết sạch thì sang đợt 2 có 8 con, rồi đợt 3 có 10 con, trạng thái ván trở lại `WaveActive` đúng.
  > **Lưu ý khi chơi thử:** chưa có vũ khí (T-12) nên không giết được quái, và chưa có `DamageSystem` (T-13) nên quái cũng không trừ máu được. Ván sẽ đứng ở đợt 1 với đàn quái vây quanh — đó là trạng thái đúng của lúc này.
- [x] **T-15** Phản hồi khi đánh trúng — hit-stop, nháy trắng, đẩy lùi, số sát thương, rung màn — @Kiet · 30/08
  > `VFX/HitFeedback.cs` · `VFX/SpriteFlash.cs` · `VFX/HitStop.cs` · `VFX/DamageNumber.cs` · `VFX/PixelNumber.cs` · prefab `Prefabs/VFX/DamageNumber.prefab`.
  > Toàn bộ là biểu diễn cục bộ, không đồng bộ — xem bảng ở mục 3.2. Gom về `HitFeedback` thay vì rải vào từng hệ thống vì phản hồi cần **điều tiết theo mức độ**: đánh trúng thường chỉ nháy sáng, quái chết mới được dừng hình và rung màn, người chơi trúng đòn là sự kiện nặng nhất. Nếu mỗi hệ thống tự quyết thì cuối ván mọi thứ cùng kêu to và không còn gì nổi bật.
  > **Hit-stop giữ dưới 30 ms và chỉ dùng cho sự kiện thưa.** Nó tác động lên `Time.timeScale`, tức là trên host thì trạng thái có thẩm quyền cũng chậm theo và client sẽ thấy đàn quái khựng. Giữ dưới một nhịp gửi mạng (33 ms ở sendRate 30) thì độ lệch tan trước snapshot kế tiếp nên không tích luỹ.
  > Nháy sáng và rung màn dùng đồng hồ **không co giãn**: nếu dùng đồng hồ thường thì chính hit-stop sẽ kéo dài chúng ra, và cú đánh mạnh lại nháy lâu hơn cú đánh nhẹ.
  > Rung màn lấy giá trị lớn nhất chứ không cộng dồn — cộng dồn thì cuối ván màn hình rung đến mức không nhìn được, đúng lúc cần nhìn rõ nhất.
  > Đẩy lùi vừa là phản hồi vừa là cơ chế chơi được: nó tạo khoảng hở giữa người chơi và đám quái, nên một cú đánh mạnh vừa là sát thương vừa là không gian thở.
  > Sự kiện sát thương được mở rộng để **mang theo vị trí nguồn** — hướng đẩy lùi phải là hướng ra xa thứ đã đánh trúng; đoán bằng "ra xa người chơi gần nhất" sẽ sai ngay khi có đạn nảy tường hoặc hai người chơi đứng hai phía.
  > **Không dùng TextMeshPro.** Phông vector bị làm mờ khi thu về cỡ pixel art và phá vỡ lưới điểm ảnh; ngoài ra TMP đòi nhập bộ tài sản riêng, thêm một bước cài đặt cho ba máy. Chữ số 3×5 điểm ảnh tự vẽ, dùng lại được cho cả số sát thương lẫn giao diện.
  > Đã kiểm chứng khi chạy: `timeScale` chạm 0.00 đúng lúc hit-stop; số sát thương hiện trong 313 khung liên tiếp của một nhịp thử; **đẩy lùi đo bằng phép so sánh có đối chứng — con bị đẩy lệch đúng 0.350 đơn vị so với con đối chứng cùng khoảng cách**, khớp giá trị đặt.
- [x] **T-16** **Sóng âm Đông Sơn** — vòng tròn đồng tâm lan toả mang hoa văn trống đồng — @Kiet · 30/08
  > `VFX/SoundWave.cs` · shader `Art/Shaders/SpriteAdditive.shader` · vật liệu `Art/Materials/SpriteAdditive.mat` · prefab `Prefabs/VFX/SoundWave.prefab`. Thay hẳn `PulseEffect` giữ chỗ của T-12, đã xoá.
  > Ba vòng đồng tâm lệch pha nhau, mang 24 vạch nan hoa theo nhịp hoa văn trống đồng, xoay chậm. Lệch pha để nghe như một tiếng đàn ngân ra chứ không phải một cú nổ; vòng sau mờ hơn vòng dẫn nên đuôi sóng mảnh dần đúng như âm thanh tắt.
  > Shader additive tự viết cho URP: shader additive dựng sẵn của pipeline cũ sẽ hiện màu hồng. Không khai báo `_MainTex_ST` vì 2D SRP Batcher tắt batching cho mọi vật liệu có thuộc tính `_ST` hoặc `_TexelSize`.
  > **Một lỗi đọc hiểu phát hiện bằng ảnh chụp, không phải bằng đọc mã.** Bản đầu để độ mờ đỉnh 0.55 và sóng phủ trắng gần hết vùng chơi — đúng kiểu hỏng mà mục 2.1 mô tả. Với additive thì các lớp sóng **cộng dồn** độ sáng, nên con số an toàn thấp hơn nhiều so với cảm giác khi nhìn một vòng đơn lẻ. Đã hạ xuống 0.11, khoá trần Inspector ở 0.4, vẽ lại vòng mỏng hơn và có viền mềm.
  > **Cân chỉnh cuối cùng phải chờ T-17.** "Đủ mờ để đọc được đòn địch" chỉ định lượng được khi đã có màu dành riêng cho đòn địch. Con số hiện tại là an toàn tạm thời, không phải kết luận.
- [x] **T-15B** HUD máu người chơi — @Kiet · 30/08
  > Hạng mục **bổ sung ngoài kế hoạch gốc** theo yêu cầu: kế hoạch chỉ có giao diện chọn thẻ (T-22) và vòng nạp Trống Đồng (T-38), không có chỗ nào cho máu.
  > `UI/PlayerHud.cs`, canvas `HUD` trong `Arena.unity`.
  > Hiển thị bằng **ô rời chứ không bằng thanh liền**. Ba nhân vật có 4, 6 và 10 máu — những con số nhỏ, và mỗi điểm máu là một quyết định. Thanh liền biến chúng thành một tỉ lệ phần trăm mờ nhạt; ô rời cho người chơi *đếm* được mình còn chịu được mấy đòn nữa, thứ họ thực sự cần biết khi đang bị vây.
  > Số ô dựng lúc chạy theo `MaxHealth`, nên đổi nhân vật hay cân bằng lại máu không phải sửa giao diện.
  > Chỉ theo dõi nhân vật cục bộ, và chỉ bám khi `MaxHealth` đã về qua `SyncVar` — bám sớm hơn thì thanh máu được dựng với số ô sai.
  > `CanvasScaler` khớp theo chiều cao: màn hình siêu rộng không làm HUD phình ra.
  > Đã kiểm chứng khi chạy: 6 ô đầy lúc vào ván, 3 đầy 3 rỗng khi còn 3 máu, 6 ô rỗng khi gục.

### Mỹ thuật

- [ ] **T-17** Chốt bảng 24 màu Đông Hồ, **dành riêng một màu cho đòn tấn công của địch** — Chưa phân công
- [ ] **T-18** Sprite Thạch Sanh, sprite Cô Hồn, tileset Sân Đình — Chưa phân công
- [x] **T-18B** Hệ thống hoạt ảnh nhân vật, gắn sprite thử nghiệm vào người chơi — @Kiet · 03/09
  > Hạng mục **bổ sung ngoài kế hoạch gốc**, khoảng trống thứ tư sau T-10B, T-14B, T-15B. T-18, T-33 và T-47 đều là hạng mục *vẽ* sprite; không có hạng mục nào cho việc *chạy* sprite. Trước hạng mục này, nhân vật là một hình tĩnh trượt trên sân.
  > `VFX/SpriteAnimationSet.cs` · `VFX/SpriteAnimator.cs` · `Player/PlayerAnimatorDriver.cs` · tài sản `Assets/ThirdParty/TinyRPG/Soldier_TEST.asset`.
  > **Cố ý không dùng Animator của Unity.** `.controller` và `.anim` là tệp YAML chỉ sửa được trong cửa sổ Animator và không ai đọc được diff — đúng loại tệp mà mục 6.2 gọi là nguồn xung đột nghiêm trọng nhất. Mục 5 lại bắt buộc mọi nội dung phải là ScriptableObject trong `Data/`, mà `.controller` thì không phải. Và nhu cầu thật chỉ là năm chuỗi khung, không blend, không transition có điều kiện — mỗi `Animator` dựng một playable graph riêng là cái giá không đáng trả khi ngân sách là 40 quái ở 60 FPS.
  > **Ba thành phần cùng ghi lên một `SpriteRenderer`, mỗi thành phần một thuộc tính.** `SpriteAnimator` chỉ ghi `sprite`, `SpriteFlash` chỉ ghi `color`, `PlayerMovement` chỉ ghi `flipX`. Chia như vậy thì không cần quy định thứ tự thực thi giữa chúng.
  > **Dùng đồng hồ có tỉ lệ**, ngược với T-15. Hit-stop hạ `Time.timeScale` để đóng băng khoảnh khắc chạm, và nhân vật đứng hình trong khoảnh khắc đó chính là tác dụng cần có. Chớp sáng và rung màn thì phải chạy tiếp nên vẫn dùng đồng hồ không co giãn.
  > **Hoạt ảnh đánh bị chặn trần theo chu kỳ khai hoả.** Nếu clip dài hơn thì đòn sau cắt ngang đòn trước và động tác đứng nguyên ở khung đầu. Tấm bắn 0.12 giây một phát, không chặn trần là hỏng ngay. Vượt trần thì clip tự chạy nhanh lên và ghi cảnh báo một lần.
  > **Sửa một lỗi có sẵn:** `PlayerMovement.Update` thoát sớm khi `!isOwned`, nên `IsMoving` và `flipX` không bao giờ cập nhật cho nhân vật của người kia. Sprite tĩnh nên không ai thấy; có hoạt ảnh rồi thì nhân vật đồng đội sẽ trượt ngang màn hình trong tư thế đứng yên, luôn quay mặt một phía. Nay suy ra từ độ dời vị trí, có ngưỡng chết để nhiễu mạng không làm nhân vật rung qua rung lại.
  > **Bật sắp xếp thứ tự vẽ theo trục Y** trong `GraphicsSettings` (`CustomAxis` 0,1,0). Không bật thì 40 con quái chồng nhau vẽ theo thứ tự tuỳ tiện và nhấp nháy khi di chuyển. Làm bây giờ khi mới có ba loại đối tượng, rẻ hơn nhiều so với ở tuần 12.
  > **Pivot đặt ở bàn chân, không ở tâm khung.** Nhân vật cao 21px nằm giữa ô 100×100, để pivot ở tâm thì bàn chân lơ lửng cách gốc toạ độ 0.6 đơn vị. Đo được đường chân của mọi khung, mọi hoạt ảnh, cả Soldier lẫn Orc đều nằm đúng tại một hàng — pivot (0.5, 0.40). Gốc toạ độ ở bàn chân cũng là thứ mà sắp xếp theo trục Y cần.
  > Đã kiểm chứng khi chạy: Idle chạy vòng đúng nhịp; khai hoả → `Attack` khung 0 rồi về `Idle` sau **0.50 giây**, đúng 6 khung ở 12 fps, lặp lại mỗi **0.91 giây** khớp chu kỳ 0.9 s của Thạch Sanh; hướng nhìn đổi theo mục tiêu; hết máu → `Death` và **giữ nguyên khung cuối**.
  > **Chưa kiểm chứng:** hoạt ảnh đi bộ và lật hình cần bàn phím thật; hoạt ảnh nhân vật của người kia cần chạy hai tiến trình. Cũng chưa đo lại hiệu năng với 40 quái — nhưng hạng mục này chỉ thêm thành phần vào prefab người chơi, quái chưa có `SpriteAnimator` nào, nên con số 40 quái của T-14 không đổi.
  > **Sprite là của gói thử nghiệm `Assets/ThirdParty/TinyRPG/`, không phát hành.** Trường `_animationSet` trong `CharacterData` chịu được null: gỡ gói ra thì nhân vật quay về sprite tĩnh, không vỡ. Xem `docs/ASSETS_ThirdParty.md`.
- [x] **T-18C** Gắn hoạt ảnh vào quái, đổi nhân vật thử nghiệm sang Gióng — @Kiet · 03/09
  > Dùng lại nguyên hệ thống của T-18B, **không thêm một dòng mã hoạt ảnh nào** — chỉ nối dây: `EnemyData` thêm trường `_animationSet`, `Enemy` nhận một `SpriteAnimator`, tài sản `Assets/ThirdParty/TinyRPG/Orc_TEST.asset`. Đây là bằng chứng cho quyết định kiến trúc ở T-18B: một trình chạy dùng chung cho cả người chơi lẫn quái.
  > **Nhân vật thử nghiệm đổi sang Gióng.** Sprite Soldier là nhân vật cận chiến cầm kiếm và khiên; Thạch Sanh đánh vòng tròn bán kính 4 quanh người, đọc ra sai hoàn toàn. Gióng vung roi theo hình cung tầm 2.5 — khớp với động tác trong sprite. Đổi bằng cách sắp lại thứ tự trong `CharacterRegistry.asset`; `NetworkManagerLAC` phân nhân vật theo `GetByIndex(numPlayers)` nên phần tử đầu là người chơi thứ nhất. Không sửa mã.
  > **Sóng âm Đông Sơn giữ nguyên, không gỡ.** Nó là một trong bốn cơ chế định vị ở mục 2 và không được cắt giảm. Đổi sang Gióng đã bỏ vòng tròn lớn quanh người; đòn cung của Gióng vẫn phát một vòng nhỏ đặt lệch về phía trước, và đó chính là cách đọc ra hướng vung roi.
  > **Nhịp đánh của Gióng buộc phải nâng tốc độ hoạt ảnh.** Chu kỳ 0.6 giây cho trần 0.48 giây, trong khi clip 6 khung ở 12 fps dài 0.50 giây — vượt trần và sẽ bị cơ chế chặn của T-18B ép chạy nhanh kèm cảnh báo. Nâng thẳng lên 15 fps thành 0.40 giây; động tác vung roi dứt khoát hơn cũng hợp hơn.
  > **`Unlock` đổi thành dọn sạch vô điều kiện** vì quái đi qua object pool. Con vừa chết đang khoá ở khung cuối hoạt ảnh chết, và chính đối tượng đó sẽ được dùng lại cho con tiếp theo; chỉ gỡ khoá thôi thì một lượt đánh dở dang còn sót vẫn chặn trạng thái nền và con mới hiện ra giữa chừng một cú vung.
  > `Enemy.Step` lật hình theo hướng đi — biểu diễn cục bộ, không đồng bộ.
  > Đứng đánh thì trạng thái nền vẫn là `Idle`, mỗi nhịp chạm chồng một lượt `Attack` lên trên. Để nền là `Attack` thì giữa hai nhịp con quái vung roi vào không khí.
  > Đã kiểm chứng khi chạy: quái `FSM=Attacking` → `anim=Attack` (`Orc_Attack01_5`) → về `anim=Idle`; lật hình đúng hướng tiến; HUD hiện 10 ô đúng máu của Gióng.
  > **Đo hiệu năng với 40 quái cùng lúc: trung bình 3.72 ms, p99 7.00 ms, ngưỡng 16.67 ms** — 76 `SpriteAnimator` chạy đồng thời. Mốc T-15 là 3.56 ms; hoạt ảnh cho toàn bộ đàn quái tốn thêm khoảng 0.16 ms.
  > **Một vấn đề đọc hiểu phát hiện bằng ảnh chụp:** sprite quái rộng 1.4 đơn vị trong khi bán kính giãn cách là 0.85, nên 40 con dồn lại thành một khối xanh liền và **người chơi biến mất hẳn bên trong khối đó**. Sprite tạm 16px trước đây nhỏ hơn nên không lộ ra. Đây là dữ kiện cho T-17/T-18: hoặc bán kính giãn cách phải lớn hơn bề ngang sprite, hoặc sprite quái phải nhỏ hơn đáng kể so với nhân vật.

### Quảng bá — khởi động tại tuần 2, không phát sinh chi phí

- [ ] **T-19** Chuẩn bị bộ tài sản trang cửa hàng: mô tả sản phẩm, ảnh chụp màn hình, ảnh bìa, GIF sóng âm — lưu tại `docs/store/` — Chưa phân công
- [ ] **T-20** Thiết lập kênh TikTok, xuất bản devlog số 1, lập lịch đăng hàng tuần — Chưa phân công

> **Đăng ký Steam Direct đã dời sang tuần 10 — hạng mục T-46B.** Khoản phí $100 chỉ chi khi sản phẩm đã có bản chạy được, nhằm loại bỏ rủi ro tài chính ở giai đoạn đầu. Toàn bộ hoạt động quảng bá không phát sinh chi phí vẫn khởi động từ tuần 2 vì lượng wishlist tích luỹ theo thời gian.

---

# Cổng 2 — Tuần 4 đến 7

**Tiêu chí nghiệm thu:** ba nhân vật cho ba trải nghiệm phân biệt rõ ràng, và sản phẩm đã đủ điều kiện quay một clip 15 giây phục vụ quảng bá.

- [ ] **T-21** Hệ thống thẻ nâng cấp — `CardData` và cơ chế áp hiệu ứng lên chỉ số — Chưa phân công
- [ ] **T-22** Giao diện chọn 1 trong 3 thẻ — giới hạn 10 giây, 2 lượt đổi thẻ — Chưa phân công
- [ ] **T-23** **Đồng bộ lựa chọn thẻ:** đợt kế tiếp chỉ khởi động khi cả hai người chơi đã chọn xong — Chưa phân công
- [ ] **T-24** Biên soạn 32 thẻ nền — Chưa phân công
- [ ] **T-25** **Hệ thống tiến hoá thẻ** — bộ kiểm tra công thức và giao diện thông báo — Chưa phân công
- [ ] **T-26** Chốt và triển khai 8 công thức tiến hoá — Chưa phân công
- [ ] **T-27** **Cơ chế Hồn** — rơi ra khi quái chết, tự hút về, âm thanh tăng dần cao độ — Chưa phân công
- [ ] **T-28** Nhân vật Gióng — roi sắt, đòn đánh hình cung — Chưa phân công
- [ ] **T-29** Nhân vật Tấm — sáo trúc, đòn tia. **Hiệu ứng tăng sát thương áp cho phát bắn kế tiếp**, không theo cửa sổ thời gian — Chưa phân công
- [ ] **T-30** Màn chọn nhân vật và đồng bộ lựa chọn qua mạng — Chưa phân công
- [ ] **T-31** Sóng âm riêng biệt cho từng nhạc cụ — Chưa phân công
- [ ] **T-32** Kiểm thử hiệu năng và đọc hiểu: 60 FPS với 40 quái và 200 đạn, đòn địch vẫn phân biệt được — Chưa phân công
- [ ] **T-33** Sprite Gióng, sprite Tấm, 40 icon thẻ — Chưa phân công

---

# Cổng 3 — Tuần 8 đến 12

**Tiêu chí nghiệm thu:** co-op qua Steam hoạt động với người chơi ngoài mạng LAN, hoàn tất 16 đợt không phát sinh sai lệch trạng thái.

- [ ] **T-34** Bốn quái vật còn lại — Ma Trơi, Bù Nhìn, Ma Da, Quỷ Nhỏ — Chưa phân công
- [ ] **T-35** Snapshot vị trí quái 2 lần/giây để hiệu chỉnh sai lệch — Chưa phân công
- [ ] **T-36** **Trống Đồng** — kích hoạt bằng dash, xoá đạn, đẩy lùi, gây choáng 1 giây — Chưa phân công
- [ ] **T-37** **Thời gian hồi Trống Đồng dùng chung**, trạng thái do host quản lý — Chưa phân công
- [ ] **T-38** Hồn nạp năng lượng cho Trống Đồng, hiển thị vòng nạp trên HUD — Chưa phân công
- [ ] **T-39** Trùm Chằn Tinh — hai pha, lượng máu tỉ lệ theo số người chơi — Chưa phân công
- [ ] **T-40** Mời bạn qua Steam overlay, chuyển transport sang FizzySteamworks — Chưa phân công
- [ ] **T-41** Cơ chế hạ gục và hồi sinh — đồng đội đứng cạnh 3 giây — Chưa phân công
- [ ] **T-42** Xử lý mất kết nối: client rớt mạng và host thoát ván, không treo ứng dụng — Chưa phân công
- [ ] **T-43** Thu thập telemetry ra CSV phục vụ phần đánh giá của khoá luận — Chưa phân công
- [ ] **T-44** Bảng đợt cố định — dùng cho nhóm đối chứng và làm phương án dự phòng — Chưa phân công
- [ ] **T-45** **AI Đạo Diễn** (LinUCB) — lõi thuật toán, `ContextVector`, `WaveSpec` — Chưa phân công
- [ ] **T-45B** **Đạo diễn hoạt động trong co-op** — `ContextAggregator` hợp thành ngữ cảnh N người chơi, số hạng công bằng trong hàm thưởng, tầng an toàn đánh giá theo người yếu nhất — Chưa phân công
- [ ] **T-46** Điều tiết bất đối xứng, đòn bẩy *chia cắt* và *dồn ép* theo hướng sinh quái, **hiển thị hoạt động của đạo diễn trên HUD** — Chưa phân công
- [ ] **T-46B** Đăng ký Steam Direct ($100), hoàn tất tax interview và xác minh tài khoản, dựng trang cửa hàng — **thực hiện tại tuần 10** — Chưa phân công
- [ ] **T-47** Sprite bốn quái, sprite Chằn Tinh, sprite trống đồng, hai tileset còn lại — Chưa phân công

---

# Cổng 4 — Tuần 13 đến 16

**Tiêu chí nghiệm thu:** demo miễn phí đã phát hành trên Steam; hoàn tất bảo vệ khoá luận.

- [ ] **T-48** Hệ thống tiền tệ Ngọc, lưu tiến trình, bảng mở khoá — Chưa phân công
- [ ] **T-49** Màn thống kê sau ván — *cấp độ khó thứ hai chuyển sang giai đoạn sau bảo vệ, lấy quỹ thời gian cho T-45B* — Chưa phân công
- [ ] **T-50** **Cân bằng: xác định sát thương gốc cho cả ba vũ khí** — *GDD hiện chưa có thông số này* — Chưa phân công
- [ ] **T-51** Cân bằng đường cong độ khó qua 16 đợt — Chưa phân công
- [ ] **T-52** Nhạc nền và toàn bộ hiệu ứng âm thanh — Chưa phân công
- [ ] **T-53** **Thực nghiệm đánh giá: 15 người dùng AI Đạo Diễn, 15 người dùng bảng đợt cố định**, chế độ chơi đơn. Thu thập thêm mẫu quan sát định tính ở chế độ co-op — Chưa phân công
- [ ] **T-54** Phân tích số liệu và biên soạn chương đánh giá — Chưa phân công
- [ ] **T-55** Đóng gói demo miễn phí — 8 đợt đầu, một nhân vật — Chưa phân công
- [ ] **T-56** Dựng trailer, sử dụng sóng âm Đông Sơn cho 5 giây mở đầu — Chưa phân công
- [ ] **T-57** Phát hành demo và đăng ký Steam Next Fest — Chưa phân công
- [ ] **T-58** Thiết lập giá: $2.99, khu vực Việt Nam 29.000–39.000₫ — *ghi đè mức Steam đề xuất* — Chưa phân công
- [ ] **T-59** Chuẩn bị slide và bản demo phục vụ buổi bảo vệ — Chưa phân công

> **Không mở bán tại tuần 16.** Phạm vi của mốc này là phát hành demo và hoàn tất bảo vệ. Ngày phát hành chính thức chỉ được ấn định khi lượng wishlist đạt khoảng 8.000 — thời điểm ra mắt không thể thực hiện lại lần thứ hai.

---

## Hạng mục dự phòng — triển khai nếu còn quỹ thời gian

Thử thách hằng ngày kèm bảng xếp hạng Steam · nhân vật thứ tư · trùm thứ hai · cấp độ khó thứ hai · thực nghiệm định lượng cho chế độ co-op

## Hạng mục ngoài phạm vi — đã chốt không triển khai

Co-op 4 người · matchmaking · cửa hàng vật phẩm trang trí · bản mobile · cắt cảnh · nhánh rẽ Núi/Biển

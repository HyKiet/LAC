# T-45 / T-45B / T-46 — đạo diễn luật và FSM

Phạm vi 11/10/2026: [THESIS_SCOPE.md](THESIS_SCOPE.md), một thuật toán chạy trên host cho một/hai người. Chọn Fixed hoặc Adaptive trước ván; wave 1 và boss 10 giữ bảng gốc. Không học máy hoặc phần thưởng LinUCB. Kiểm tra có fixture không thay thế chơi thật hoặc nghiệm thu Internet.

## CSV phiên bản 2

`TelemetrySession.SchemaVersion = 2`. Host vẫn tạo thư mục GUID riêng tại `Application.persistentDataPath/Telemetry/<run_id>/`; client không tạo phiên. Giữ UTF-8 BOM, số thập phân theo invariant culture, escape dấu nháy/phẩy/newline, flush định kỳ 5 giây và khi ghi quyết định/mở/đóng wave. Lỗi I/O ngừng ghi và cảnh báo một lần, gameplay tiếp tục. Cách đổi đường dẫn/tắt ghi giữ như [T-43](TELEMETRY_TESTS.md#sử-dụng).

| Tệp | Đơn vị một dòng |
|---|---|
| `events.csv` | Sự kiện host; bổ sung `director_decision` và nguồn thực trong `wave_spec.detail` |
| `directors.csv` | Một quyết định trước mỗi wave, gồm wave mở đầu/boss giữ baseline hoặc fallback |
| `waves.csv` | Một người tham gia một wave, kèm quyết định đã áp dụng cho wave đó |
| `runs.csv` | Một người tham gia ván, nguồn tổng hợp và số quyết định; không gán context một wave thành context cả ván |

Ba mươi cột đầu của tổng kết giữ thứ tự và ý nghĩa cũ. Các cột mới được thêm sau `dashes`:

- `mode`: Fixed/Adaptive đã chốt khi khởi tạo ván. `director_state`: Balanced/Relief/Variety của quyết định.
- `director_decisions`, `adaptive_decisions`, `fallback_decisions`: số quyết định tích luỹ đến dòng đó. Quyết định baseline mở đầu/boss không tính là quyết định thích ứng.
- `decision_reason`, `fallback_reason`: lý do thật từ đạo diễn; fallback không được giả ghi thành AI thích ứng.
- `context_valid`, `health_min`, `damage_ratio_max_raw`, `damage_ratio_max`, `context_downs`, `connected_players`, `participants`, `late_joiners`, `missing_context`: snapshot đầu vào. Tổn thất thô có thể lớn hơn 1; bản kẹp [0,1] dùng ra quyết định. Context không hợp lệ vẫn giữ số hữu hạn đã thu để chẩn đoán, nhưng `context_valid=0`; phải xét cờ này trước phân tích. Giá trị NaN/Infinity để trống.
- Hai nhóm `baseline_*` và `applied_*`: `interval`, `burst`, `duration`, `cycle_count`, `mix`, `directions`. `cycle_count` là thành phần một chu kỳ lặp, không phải tổng số quái đã sinh trong cả wave tính giờ. Thành phần ghi `asset_name:count|`; hướng là vị trí trên chu vi sân.

`directors.csv` có các cột mở đầu `schema,run_id,elapsed_s,wave,source,table`, sau đó cùng nhóm cột mới. `source` phân biệt:

| Giá trị | Ý nghĩa |
|---|---|
| `fixed_table` | Fixed hoặc wave mở đầu/boss được bảo vệ |
| `adaptive_rules` | Luật thực sự chọn đặc tả, kể cả Balanced giữ chỉ số baseline |
| `fixed_fallback` | Không đủ dữ liệu/cấu hình hợp lệ cho luật, dùng baseline và ghi lý do |
| `unrecorded` | Không có quyết định được ghi; không dùng làm bằng chứng AI |

Nguồn trong `runs.csv` là tập nguồn thật đã xuất hiện, sắp xếp và nối bằng `|`, ví dụ `adaptive_rules|fixed_table`. Ván chỉ baseline/fallback không bị gắn nhãn có thích ứng chỉ vì chọn Adaptive. Các cột context/trạng thái/đặc tả trong tổng kết ván để trống; lấy chúng từ dòng wave/quyết định tương ứng. Bộ đếm đội lặp trên từng dòng người như schema 1: không cộng các dòng co-op để tính tổng đội.

`run_started.detail` ghi mode và `source=pending_wave_decision`, vì lúc mở phiên chưa có quyết định đầu tiên. Mỗi quyết định ghi sau `Telemetry.StartWave` và trước lịch sinh của `WaveManager`. Gọi lặp cùng wave hoặc nhầm wave không tạo thêm dòng. `wave_spec.detail` ghi đặc tả thực thi và nguồn quyết định đã ghi, giúp đối chiếu đầu ra với lịch sinh.

## Kiểm tra độc lập đã chạy

Ngày 11/10/2026, .NET SDK **10.0.401**, `TelemetryIsolatedChecks.cs` dùng mã ghi thật `TelemetrySession.cs`/`TelemetryCsv.cs` với test doubles Unity/Mirror/đạo diễn: **129 assertions đạt**. Giữ kiểm tra cũ về locale `fr-FR`, BOM/escape, host/client, counters, lethal hit, reset, join/leave, flush và lỗi I/O. Bổ sung schema 2, quyết định trùng/nhầm wave, raw damage 1.5 với clamp 1, baseline/applied khác nhau, missing context/fallback, nguồn tổng hợp và counters. Hai cảnh báo I/O là tình huống cố ý tạo trong bài kiểm tra.

Artefact cục bộ của lần chạy: `C:\Users\LEGION\AppData\Local\Temp\lac-t43-harness-a6c0a8bb3988416897bdbfcbff1b81d2`. Đây là kiểm tra CSV và session, **không chứng minh Unity lifecycle, mạng, hiệu ứng hoặc thuật toán đạo diễn thật**.

Chạy lại bằng project mẫu ở [T-43](TELEMETRY_TESTS.md#kiểm-chứng-đã-chạy), gồm ba file nguồn như trước:

```powershell
dotnet run --project Logs/T43Harness/T43Harness.csproj
```

Không bật `LAC_TELEMETRY_ISOLATED` trong Player Settings; ký hiệu này chỉ dành cho harness ngoài Unity.

## Kiểm tra tích hợp CSV

Sau khi phiên đã flush hoặc kết thúc, gọi từ công cụ kiểm tra trong Editor:

```csharp
LAC.Director.Editor.TelemetryChecks.ValidateDirectorCsv(
    directory, LAC.Director.DirectorMode.Adaptive, expectedDecisions: 10);
```

Hàm đọc CSV thật, không tạo quyết định giả. Nó kiểm tra schema/mode, một quyết định mỗi wave, sự kiện khớp dòng, baseline/applied giữ Duration, wave 1/boss giữ bảng, raw/clamp, lý do fallback, tổng kết wave khớp quyết định, nguồn lịch sinh và tổng kết ván khớp lịch sử. Có thể bỏ `expectedDecisions` để kiểm tra một phiên chưa kết thúc. Nếu đọc khi đang chơi, chờ flush định kỳ hoặc flush bằng fixture trước khi đọc.

Runner Play Mode T-43 cũ gọi `ReportWaveCleared` trực tiếp mà chưa hết hạn wave tính giờ; sau T-44D lời gọi đó bị từ chối đúng luật. Không dùng runner cũ làm nghiệm thu mới nếu chưa cập nhật fixture deadline. Hàm `ValidateDirectorCsv` độc lập với runner đó; dùng fixture tích hợp đạo diễn để đi qua luật wave hiện hành.

Nghiệm thu gameplay cần dựng riêng các tình huống: áp lực tăng vào Relief; hồi máu trong wave không xoá tổn thất; hysteresis; Variety chỉ dùng loại đã giới thiệu; teammate yếu/gục không bị teammate mạnh che; late join/rời mạng; thiếu dữ liệu fallback; trần suất quái và khoảng cách điểm sinh; Fixed/Adaptive đổi giữa hai ván; boss/thắng/thua/restart; client chỉ nhận biểu diễn; HUD trùng lý do/đặc tả CSV. Tắt fixture và thoát Play trước sửa mã.

## Nghiệm thu Unity

Đã chạy ngày **11/10/2026**, Unity 6000.5.6f1, Arena, Mirror host. MCP xác nhận đúng `LAC-main`, thoát Play trước mọi lần sửa mã. Luật và nội dung nằm tại `Data/Director/Resources/Director/DirectorRules.asset`; host áp dụng `CurrentDirectorDecision.Applied` qua `RunManager.GetHostWaveSpec` trong WaveManager, client không quyết AI.

| Kiểm tra | Kết quả và bằng chứng |
|---|---|
| Luật/ngữ cảnh trong Editor | **863 assertions PASS**: ngưỡng, hysteresis, seed, wave đầu/boss, Fixed, thiếu dữ liệu, NaN/overflow, loại chưa giới thiệu, quái con, snapshot HP/MaxHP, vào/rời đội, asset không đổi. [Kết quả](validation/T45/rules.txt) |
| Host một người | **75 assertions PASS**: actual HP/heal, i-frame, Relief → giữ Relief → Variety → Balanced, thiếu rules fallback, WaveManager dùng lịch Applied, 10 wave/9 lượt thẻ, boss/Victory, CSV, đòn chí tử và Restart. [Kết quả](validation/T45/solo.txt), [quyết định](validation/T45/solo-csv/directors.csv) |
| Hai Editor, latency 100ms | **76 assertions PASS** cùng luồng tích hợp, CSV hai người, boss/Victory/Defeat/Restart. [Kết quả](validation/T45/coop.txt), [CSV](validation/T45/coop-csv/directors.csv) |
| Ngữ cảnh đội có kiểm soát | **29 assertions PASS**: Warrior đầy HP, Archer còn 1/6 trước hồi máu → Relief; hai lần gục/hồi sinh thật 3s; raw damage >100%; rời lúc chọn thẻ loại HP người yếu; vào giữa wave/vào lúc chọn thẻ dùng fallback; một wave đầy đủ khôi phục ngữ cảnh/Variety. [Kết quả](validation/T45/team.txt), [CSV](validation/T45/team-csv/directors.csv), [fixture](validation/T45/CoopFixture.cs.txt) |
| Fixed và đổi phiên | **15 assertions PASS**: nhận 8/10 HP vẫn giữ interval wave 2 =1.4s; HUD/CSV Fixed, từ chối đổi giữa trận, Restart giữ mode và reset; ngắt kết nối rồi chọn Adaptive khởi động đúng phiên mới. [Kết quả](validation/T45/fixed.txt), [CSV](validation/T45/fixed-csv/directors.csv), [fixture](validation/T45/FixedFixture.cs.txt) |
| HUD/network | Đã xem [màn chọn](validation/T45/selection.png) và [HUD Hỗ trợ/F3](validation/T45/relief.png); thanh máu vẫn rõ. [Client probe](validation/T45/client.txt) khớp mode/state/source/reason/HP/damage, không có telemetry phía client và từ chối đổi mode |
| Console cuối Editor chính | **0 lỗi, 0 cảnh báo**, compilationFailed false. [Kết quả](validation/T45/console.txt) |

Fixture tích hợp dùng deadline đẩy nhanh và bất tử; fixture đội tắt WaveManager để cô lập sát thương/hồi sinh và kéo dài hạn thẻ khi kiểm tra mạng. Không dùng chúng để chứng nhận cân bằng hoặc thời lượng ván tự nhiên. Trong các lần probe thủ công trước fixture, quái còn hoạt động gây thêm gục và timeout tự chuyển wave; kết quả đó không dùng làm nghiệm thu. Fixture có kiểm soát đã chạy riêng và đạt. Không thay thế nghiệm thu hai máy/hai mạng Internet T-40.

CSV thực ghi 10 quyết định trong phiên tích hợp (wave 1/boss là baseline; có một wave cố ý thiếu rules), sáu quyết định trong fixture đội (hai fallback do vào muộn). Không tính quyết định baseline/fallback là AI thích ứng. Các bộ CSV đầy đủ nằm trong các thư mục `solo-csv`, `coop-csv`, `team-csv`, `fixed-csv` cùng bằng chứng trên.

### Tài nguyên chữ được bàn giao

HUD mới dùng TMP và font Liberation Sans có OFL, được sao chép nguyên font vào `_LAC/Art/Fonts/Resources/UI/DirectorFont.ttf`; shader Mobile SDF/Properties và giấy phép uGUI vào `_LAC/Art/Shaders`. Hai bảng ngắt dòng và TMP Settings ở `Data/UI/Resources/DirectorUI`. Không sửa hoặc yêu cầu commit thư mục TMP Essentials cục bộ của người dùng.

TMP không có API đặt singleton cấu hình riêng: `DirectorHud.CreateFontAsset` gắn **bản sao runtime** của settings vào `s_Instance`, kiểm tra trường tồn tại, và dùng shader/font Resources đã bàn giao. Không sửa asset khi chạy. Việc đổi phiên bản TMP cần kiểm tra lại điểm nối này. Text ẩn được gán shared material trước đo kích thước để tránh đọc material chưa khởi tạo.

Bản sao TEMP đã chạy với **`Assets/TextMesh Pro` được đưa ra ngoài Assets**, vẫn hiển thị đúng HUD và nhận quyết định từ host, không có lỗi tải font/NullReference trong phiên đạt. Các lần đầu dùng LegacyRuntime cho TMP không tải được font; đã thay bằng font TTF có dữ liệu nhúng và chạy lại. Client bật cờ `--burst-disable-compilation` riêng cho fixture để không bị DLL JIT do Windows chặn; vẫn có lỗi startup Unity Search indexing ở clone, nên không tuyên bố toàn bộ Console client sạch lỗi môi trường. Không thay đổi chính sách Windows.

### Hiệu năng sau tích hợp

Fixture Game View **1070×616**, i9-12900H/RTX 3060 Laptop, AI mode Adaptive/Variety và HUD/F3 bật: 40 quái, 200 đạn, warmup 10s + đo 30s, **7156 samples**, mean **4.193ms**, P95 **4.752ms**, main thread P95 **4.752ms**, **PASS** ngưỡng 16.67ms. [JSON](validation/T45/performance.json).

Fixture `PerformanceScenario` tắt WaveManager/vũ khí và duy trì tải VFX; đo cả HUD/telemetry hiện hành nhưng không đo đầy đủ scheduler timed hoặc chi phí quyết định tại ranh giới wave. Đạo diễn chỉ quyết tại ranh giới, không chạy luật mỗi frame. Chưa chứng nhận executable, mọi cấu hình máy hoặc cân bằng tự nhiên; T-55/build và T-50/T-51 còn việc.

### Chạy lại

1. Edit Mode, mở Arena: `LAC > Content > Apply Rule Based Director`, rồi `LAC > Validation > Director Rules`.
2. Play, chọn Warrior + Thích ứng, tạo host: `LAC > Tests > Check Director Integration (Play Mode)`. Đọc `DirectorIntegrationChecks.LastResult` và `CsvDirectory`.
3. Co-op dùng bản sao TEMP, `-executeMethod LAC.Director.Editor.DirectorIntegrationChecks.StartClientEditor --lac-telemetry-client <command-file> --lac-class Archer`; bật cùng LatencySimulation 100ms. Fixture đội/Fixed được lưu ở liên kết trên và thực thi qua MCP script-execute; chỉ dành cho kiểm thử, không đưa fixture điều khiển vào ván trình diễn.
4. Chọn Cố định/Thích ứng trước kết nối, F3 xem giải thích khi chơi; thoát Play sau kiểm tra. Đổi mode qua phiên mới sau ngắt kết nối; Restart trong cùng phòng giữ mode đã chọn.

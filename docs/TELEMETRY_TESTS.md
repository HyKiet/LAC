# T-43 — Telemetry CSV

Triển khai trong phạm vi @Kiet, kiểm thử Unity ngày 04/10/2026. MCP đã kết nối lại, Unity biên dịch được và Play Mode một/hai người đã đạt. Lỗi Safe Mode trước đó được giữ trong [nhật ký môi trường](UNITY_BUILD_BLOCKER.md).

## Sử dụng

Host tự tạo một thư mục GUID mới cho mỗi ván tại `Application.persistentDataPath/Telemetry/<run_id>/`. Console ghi đường dẫn bằng tiền tố `[Telemetry] CSV:`. Chơi lại, kể cả cùng seed, tạo thư mục mới. Client không tạo phiên ghi. Không thu tên người chơi, Steam ID hoặc địa chỉ mạng; `player_id` chỉ là số thứ tự tham gia trong ván.

Tham số dòng lệnh tùy chọn:

```text
--lac-telemetry-dir "D:\LAC-experiment"
--lac-telemetry-off
```

Thư mục được tạo tự động. CSV dùng UTF-8 BOM, dấu phẩy phân cột, dấu chấm thập phân và escape dấu nháy/newline. Tệp mở cho phép đọc trong khi ghi; flush mỗi 5 giây và khi bắt đầu/kết thúc wave. Lỗi ghi chỉ cảnh báo một lần và ngừng telemetry của ván đó, không dừng gameplay. Thoát bình thường đóng tệp; tắt cưỡng bức/mất điện có thể mất phần chưa flush và không có tổng kết ván. Không xem ván thiếu tổng kết là thất bại gameplay.

## Ba tệp

| Tệp | Mỗi dòng biểu diễn |
|---|---|
| `events.csv` | Bắt đầu/kết thúc ván và wave, đặc tả wave, vào/rời mạng, sát thương, hạ gục, hồi sinh, dash được host chấp nhận, quái chết |
| `waves.csv` | Một người đã tham gia một wave; chỉ xuất khi wave kết thúc hoặc bị ngắt |
| `runs.csv` | Một người đã tham gia ván, kể cả người rời mạng; xuất khi kết thúc ván |

`schema=1` là phiên bản định dạng. `run_id` nối ba tệp; `seq` là thứ tự sự kiện trong ván, không phụ thuộc đồng hồ hệ thống. `elapsed_s` dùng `Time.unscaledTimeAsDouble`; `started_utc` dùng UTC ISO 8601. `environment` phân biệt editor/development/release; `build` là `Application.version`.

Các cột thống kê:

- `wave`: 1–16 trong `waves.csv`; 0 trong `runs.csv`.
- `source`: hiện là `fixed_table`. T-45 phải cập nhật nguồn và bổ sung quyết định/ngữ cảnh/phần thưởng khi AI Đạo Diễn được triển khai. T-43 không giả lập dữ liệu AI chưa có.
- `result`: `cleared` cho wave sạch; `victory`/`defeat` cho ván; `aborted` khi dừng host/thoát; `replaced` khi gọi bắt đầu ván khác lúc ván cũ chưa kết thúc; `interrupted` nếu bắt đầu wave khác trước khi đóng wave hiện tại.
- `duration_s`: thời gian của wave hoặc ván. Wave không gồm thời gian chọn thẻ; ván có gồm thời gian chọn thẻ. Không bị hit-stop làm ngắn thời gian đo. Khi Editor pause, đồng hồ Unity dừng.
- `exposure_s`: thời gian người đó có mặt trong wave/ván, gồm thời gian đang gục; người vào muộn/rời sớm có thời gian riêng.
- `run_players_start`, `run_players_peak`: số người đầu ván và lớn nhất tính đến dòng đó; `players_end`: số người còn kết nối tại thời điểm tổng kết.
- `health_start`, `max_health_start`, `health_end`, `max_health_end`: ảnh chụp chỉ số theo từng người. Wave bắt đầu sau hồi máu đầu wave; người đã rời giữ ảnh chụp lúc rời.
- `hits_taken`, `health_lost`: số đòn được chấp nhận và **máu thực sự mất**, không tính overkill hoặc đòn bị i-frame từ chối. Hồi máu không trừ ngược bộ đếm; sau hồi sinh tổng máu mất có thể lớn hơn máu tối đa. Ghi trước khi đòn chí mạng kết thúc ván.
- `downs`, `revives`, `dashes`: hạ gục, được hồi sinh, dash được host chấp nhận. Khôi phục máu khi chơi lại không tính là hồi sinh.
- `team_kills`, `team_souls`, `team_drum_uses`: số của **cả đội**, lặp trên mỗi dòng người chơi để dễ nối dữ liệu. Lấy một giá trị cho mỗi wave/ván, **không cộng các dòng người chơi**. Các cột này là số trong wave ở `waves.csv`, số cả ván ở `runs.csv`.
- `waves_cleared`: tổng số wave đã sạch tính đến dòng đó, gồm wave cuối khi thắng.

Các sự kiện sát thương/dash/quái chết chỉ tính trong wave đang mở. `events.csv.value` là seed với `run_started`, máu mất với `player_damaged`, máu được hồi với `player_revived`, ID quái với `enemy_killed`, tổng quái theo đặc tả với `wave_spec`. `run_started.detail` giữ UTC, build và nguồn ngay đầu ván để vẫn có metadata nếu ván không đóng bình thường. `wave_spec.detail` lưu tên bảng, thành phần quái, hướng theo chu vi, nhịp sinh và burst thực tế từ `WaveManager`.

## Điểm tích hợp

- `Director/TelemetrySession.cs`, `TelemetryCsv.cs`: thống kê và ghi tệp; không dùng RNG gameplay, không thêm gói tin mạng.
- `Director/RunTelemetry.cs` (partial `RunManager`), `Core/RunManager.cs`: vòng đời và flush. Ghi tại điểm quyết định host, không nghe hook SyncVar nên tránh ghi kép host/client.
- `Core/WaveManager.cs`: ghi đặc tả sau khi dựng lịch.
- `PlayerHealth.cs`, `PlayerRevive.cs`, `PlayerDash.cs`: các sự kiện đã được host chấp nhận.
- `Net/NetworkManagerLAC.cs`: vào/rời mạng và dừng host. Không sửa scene, prefab, thẻ hoặc menu.
- `Director/TelemetryTestClient.cs`: công cụ kiểm tra chỉ tồn tại trong Editor/Development, chỉ cài khi có `--lac-telemetry-client <command-file>`. Không chạy trong bản phát hành.

## Kiểm chứng đã chạy

1. Trong lúc Unity bị chặn, phiên 03/10 biên dịch C# riêng đạt 0 lỗi; lần chạy lại đầu ngày 04/10 thiếu DLL phụ thuộc trong Library nên thất bại. Các kết quả này chỉ là lịch sử chẩn đoán. **Kết quả mới nhất là Unity Development build thành công**, có Mirror Weaver/ILPP; xem phần kết quả bên dưới. `Logs/T43-source-build.txt` vẫn giữ lỗi của lần biên dịch riêng cũ.
2. Kiểm tra độc lập dùng **mã thật** `TelemetrySession.cs`/`TelemetryCsv.cs` với các đối tượng thay thế Unity/Mirror: **99 assertions đạt** ngày 04/10/2026. Bao gồm locale `fr-FR`, UTF-8 BOM, dấu nháy/phẩy/newline, cổng host/client, sát thương cuối, đếm hạ gục/hồi sinh, vào muộn/rời sớm, reset giữa ván cùng seed, wave cuối, flush và đường dẫn không ghi được. Bổ sung bắt lỗi ngay từ bước ghép đường dẫn để lỗi cấu hình telemetry không làm gián đoạn khởi tạo ván. Cảnh báo ghi tệp trong bài này là lỗi chủ động tạo để kiểm tra. Log cục bộ: `Logs/T43-isolated-tests.txt`.
3. Trước khi môi trường được khôi phục, MCP `editor-application-set-state` từng từ chối Play do lỗi biên dịch Unity. Sau đó đã kiểm chứng các điểm nối gameplay, hai tiến trình và hiệu năng; xem kết quả Unity bên dưới.

Mã kiểm tra độc lập ở `Director/Editor/TelemetryIsolatedChecks.cs`, chỉ biên dịch khi định nghĩa `LAC_TELEMETRY_ISOLATED`; không đặt ký hiệu này trong Player Settings. Có thể chạy lại bằng PowerShell tại gốc repo với .NET SDK 10:

```powershell
New-Item -ItemType Directory -Force Logs/T43Harness | Out-Null
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <DefineConstants>LAC_TELEMETRY_ISOLATED</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../../Assets/_LAC/Scripts/Director/Editor/TelemetryIsolatedChecks.cs" />
    <Compile Include="../../Assets/_LAC/Scripts/Director/TelemetrySession.cs" />
    <Compile Include="../../Assets/_LAC/Scripts/Director/TelemetryCsv.cs" />
  </ItemGroup>
</Project>
'@ | Set-Content Logs/T43Harness/T43Harness.csproj
dotnet run --project Logs/T43Harness/T43Harness.csproj
```

## Chạy lại nghiệm thu

1. Vào Play trong Arena, chạy `LAC > Tests > Check T43 Telemetry (Play Mode)`. Bài này dừng wave tự động, tắt vũ khí/di chuyển để cô lập dữ liệu, thử damage/i-frame, chuyển wave, Defeat, Restart, Victory 16 wave và StopHost. Khi kết thúc, thoát Play để bỏ toàn bộ fixture.
2. Lặp lại với host và một Windows Development client độc lập qua LatencySimulation 100 ms. Bài kiểm tra có thêm hồi sinh thật sau 3 giây khi đủ hai người. Client dùng thư mục telemetry riêng; xác nhận nó không tạo CSV.
3. Cho client vào/rời giữa wave, đối chiếu `player_joined`, `player_left`, thời gian tham gia và tổng kết người rời. Kiểm tra dash host/client chỉ tăng một lần khi host chấp nhận.
4. Chạy ván bình thường qua lựa chọn thẻ và boss; đối chiếu wave cuối, máu, Hồn và Trống với CSV. Kiểm tra Console không lỗi đỏ và tình huống 40 quái/200 đạn.
5. Thoát Play và đóng tiến trình client sau kiểm thử để bỏ cấu hình thử nghiệm.

## Kết quả Unity ngày 04/10/2026

- MCP `ping`: `pong`; Editor `scriptCompilationFailed=false`. Windows Development build thành công, 0 lỗi. Hai cảnh báo do assembly definition không có script của plugin MCP tilemap/inputsystem.
- `TelemetryChecks`: **ALL PASSED (1 players)** và **ALL PASSED (2 players)**, host Editor + client Windows độc lập, LatencySimulation **100 ms**. Kiểm tra sát thương được chấp nhận, i-frame bị từ chối, overkill, Defeat, hồi sinh thật, reset, Victory đủ 16 wave, Finish lặp và StopHost. Console sau lần co-op đạt: **0 lỗi, 0 cảnh báo**.
- Kiểm tra mạng bổ sung: host bắt đầu một mình, client vào giữa wave rồi rời. CSV giữ người rời, thời gian tham gia riêng, không đưa người rời vào wave sau. Cả host và client gửi hai yêu cầu dash sát nhau; CSV chỉ ghi **1 dash/người**. Client luôn `telemetry=False`, thư mục CSV riêng không được tạo.
- Chạy luồng wave thật với lịch sinh tăng tốc và sát thương thử nghiệm: đủ **16 `wave_spec`**, 16 tổng kết wave, hết hạn chọn thẻ đầu tiên tự sang wave kế, hạ Chằn Tinh rồi Victory. Tổng Hồn, 1 lần Trống Đồng và máu cuối khớp RunManager/PlayerHealth. Đây là kiểm tra tích hợp có điều khiển, không phải playtest cân bằng một ván 15 phút.
- Sửa hai lỗi fixture tìm được trong Play Mode: đọc tệp đang được ghi cần `FileShare.ReadWrite`; vị trí hồi sinh phải tránh collider Trống Đồng tại tâm sân. Không thay đổi luật hồi sinh.
- Hiệu năng với telemetry đang ghi: Editor Game View 1309×616, i9-12900H/RTX 3060 Laptop, 40 quái/200 đạn xuyên suốt; 7.135 mẫu, 7.134 khung render, trung bình **4,21 ms**, P95 **4,78 ms** (<16,67 ms), **PASS**. [Số đo](validation/T43/performance.json). Lần standalone chạy ẩn trước đó có 0 khung render nên bị loại, không dùng để kết luận hiệu năng. Chưa đo mọi độ phân giải/cấu hình máy.

Bằng chứng: [CSV một người](validation/T43/solo/runs.csv), [CSV co-op](validation/T43/coop/runs.csv), [vào/rời và dash](validation/T43/network/runs.csv), [16 wave thật](validation/T43/flow/waves.csv), [log client](validation/T43/client.txt). Mỗi thư mục CSV có đủ `events.csv`, `waves.csv`, `runs.csv` để đối chiếu.

Client kiểm tra mới được build từ Arena dưới `Builds/TelemetryTests/LAC.exe`. Ví dụ chạy trong thư mục gốc dự án:

```text
Builds/TelemetryTests/LAC.exe --lac-telemetry-client Logs/T43-command.txt --lac-telemetry-dir Logs/T43-client-csv -logFile Logs/T43-client.log
```

Ghi một lệnh vào tệp điều khiển: `dash 1`, `leave 1`, `join 1`, `quit 1`. Số cuối phân biệt các lần thử; `dash` gửi hai yêu cầu để kiểm tra host từ chối yêu cầu trùng khi hồi chiêu. Client này tắt di chuyển/vũ khí nhằm cô lập thống kê.

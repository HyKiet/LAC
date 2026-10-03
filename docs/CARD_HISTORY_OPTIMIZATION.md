# Tối ưu lịch sử Cards và nút đổi thẻ — 02/10/2026

Phạm vi: mã Cards và bộ kiểm thử Cards. Không đổi tham số cân bằng trong lượt này;
giữ bộ 12 thẻ nền, tám tiến hoá và mức phạt tốc đánh Cuồng Nộ −8% đã chốt ở
[lượt trước](CARD_OPTIMIZATION.md). Không sửa nhân vật, đợt quái, VFX chiến đấu,
scene hoặc mô hình host-authoritative. T-32/T-50/T-51 vẫn chưa hoàn tất.

## Thay đổi

- `Cards/CardSelectionNetwork.cs`: cache lịch sử theo người chơi, chỉ dựng lại
  khi `SyncList` thay đổi hoặc đổi ván. Khi nghỉ không quét toàn bộ grant hoặc
  dựng lại danh sách; vẫn duyệt danh sách người chơi để nhận nhân vật/dữ liệu
  tới muộn. So sánh cả thứ tự thẻ, component nâng cấp và dữ liệu nhân vật, không
  chỉ số lượng grant: thay thẻ giữ nguyên độ dài và thay component cùng ID đều
  được nhận ra. Lịch sử giữ nguyên thứ tự để bảo toàn kết quả tiến hoá/chỉ số.
- `OnStartClient` luôn làm mới cache vì Mirror deserialize snapshot đầy đủ không
  phát callback `SyncList.OnChange`; `OnStopClient` bỏ đăng ký và xoá cache. Các
  callback ADD/INSERT/SET/REMOVE/CLEAR chỉ đánh dấu dirty, xử lý ở lần update sau.
  Grant tới trước nhân vật được giữ; chưa có `CharacterData` thì chưa replay để
  tránh xét tiến hoá bằng loại vũ khí dự phòng.
- `Cards/PlayerUpgradeState.cs`: `ReplayCards` reset và áp lịch sử theo đúng luồng
  cũ nhưng chỉ phát một `Changed` sau trạng thái cuối. `Apply`/`ResetRun` thông
  thường vẫn phát sự kiện như trước. Replay không áp máu và không sửa asset.
- `Cards/CardSelectionView.cs`: Image nút đổi dùng màu trắng để không nhân tint
  hai lần với `ColorBlock`; bật lại nhóm nút không bật nút đổi khi còn 0 lượt.
  Không thêm chuyển động, blur hoặc thay cỡ chữ/bố cục thẻ.
- `Cards/Editor/CardNetworkPlayModeChecks.cs`: fixture chờ lịch sinh quái T-44
  tự chạy hết trước khi dọn đợt, thay cho dọn quái đang sống khi còn lịch chờ.
  Đây là thay đổi fixture, không sửa gameplay quái.

## Bằng chứng Edit Mode đã xác nhận

- `CardHistoryReplayChecks`: 58 lịch sử dùng danh mục thật, ba nhân vật và tám
  công thức; mỗi lịch sử replay ba lần, tổng 174 thông báo chỉ thấy trạng thái
  cuối. Chữ ký số thực, cấp thẻ và thứ tự tiến hoá khớp reset + Apply tuần tự.
  Có thứ tự đảo, vượt cấp, null/ID lỗi, lịch sử rỗng, tiến hoá đồng thời, notification
  thông thường và phục hồi suppression sau exception. 31 asset giữ nguyên.
- `CardHistoryCacheChecks`: ADD/INSERT/SET/REMOVE/CLEAR, chỉ replay người bị ảnh
  hưởng, 10.000 idle tick không dựng lại lịch sử hoặc phát thêm `Changed`, grant
  tới trước player/dữ liệu, component mới cùng ID và hai thứ tự đổi ván/clear đều
  đạt. Mô phỏng snapshot reconnect cùng độ dài nhưng khác thẻ đạt; callback không
  đăng ký trùng.
- Hồi quy dữ liệu: `CardBalanceChecks` đạt 45.000 lượt bốc và 1.152 tổ hợp marginal
  Cuồng Nộ; `CardEvolutionCatalogChecks` đạt tám công thức × ba nhân vật. Các kiểm
  này xác nhận không đổi kết quả Cards, không phải cân bằng toàn bộ ván chơi.

## Lợi ích và giới hạn kết luận

Với một người nhận 15 grant riêng biệt, cách replay cũ phát `1 + n` thông báo ở
grant thứ `n` (reset rồi Apply từng thẻ): tổng `15 + 120 = 135`. Cách mới phát 15,
giảm 88,9% thông báo UI trong kịch bản này. Đây là phép tính từ luồng mã, **không
phải benchmark thời gian**; không tính snapshot rỗng ban đầu, reset ván hoặc reconnect.
Replay vẫn áp lịch sử tuần tự, không đổi sang cộng hiệu ứng tăng dần.

10.000 idle tick là kiểm chứng không replay/dựng lại lịch sử khi đầu vào không
đổi, không chứng minh FPS hoặc GC=0 toàn khung. Không dùng kết quả này để đóng
ngân sách 40 quái/200 đạn của T-32.

“Atomic replay” trong tên bài kiểm là **thông báo trạng thái cuối**, không phải
transaction rollback: nếu lịch sử ném exception giữa chừng, trạng thái có thể
dở dang, exception được truyền tiếp, suppression được phục hồi và không phát
thông báo thành công. Caller chưa ghi cache thành công nên có thể replay lại.

Bài cache Edit Mode mô phỏng snapshot bằng backing list và hook start/stop;
không thay thế kiểm reconnect thật qua transport. Chữ ký nâng cấp client khớp
host cũng không chứng minh quái/đạn đồng bộ. Fixture tiến hoá fast-forward lịch
host vẫn có giới hạn riêng; xem [CARD_TESTS.md](CARD_TESTS.md).

## Chạy lại

Thoát Play Mode trước khi sửa mã hoặc chạy các kiểm dữ liệu. Trong menu `LAC/Tests`:

1. `Validate Atomic Card History Replay (Edit Mode)`.
2. `Validate Client Card History Cache (Edit Mode)` — cần registry không có player runtime.
3. `Validate 12 Card Balance (Edit Mode)`.
4. `Validate Eight Evolutions (Edit Mode)`.

Sau đó kiểm Play host, typography/nút đổi ở 720p/1080p và hai tiến trình với
LatencySimulation 100 ms theo [CARD_TESTS.md](CARD_TESTS.md). Đọc Console, nhìn
ảnh Game View và đối chiếu log client trước khi ghi nhận hoàn tất.

## Kết quả Play, hình ảnh và co-op

- Mirror host: 12/12 thẻ, pause/input/vật lý, nhấp đôi, bộ đếm và huỷ lựa chọn
  khi chơi lại PASS (`CardResume ALL PASSED`).
- Typography/UI: 12 thẻ + tám tiến hoá PASS ở 1280×720 và 1920×1080; mô tả
  22/33 px. Kiểm hết lượt đổi vẫn khoá sau disable/enable và Image trắng PASS.
  Đã nhìn ảnh Game View nền nút thường và hover; thẻ không dao động/nhòe chữ.
- Hai tiến trình Editor host + development client, LatencySimulation 100 ms:
  `CardNet ALL PASSED`; timeout thực đo 11,30 s gồm feedback. Đạt ID/token sai,
  nhấp đôi, chờ đồng đội, lượt đổi độc lập, đổi trùng/quá giới hạn, timeout,
  ngắt client giải phóng barrier và reset host. Cấp thẻ và toàn bộ chữ ký chỉ số
  của hai người ở đợt 2 và 3 khớp client/host (4/4). Không thử lại tám UI tiến hoá
  bằng fixture fast-forward hai tiến trình trong lượt này.
- Kiểm snapshot thật khi client vào muộn: host đã có Xuyên Tâm ×1 + Âm Vang ×3
  trước khi client kết nối, trên Tấm. Client mới dựng đúng Nỏ Thần: tầm ×1,5,
  giới hạn trúng 5, một tiến hoá và bốn grant; chữ ký khớp host. Nhân vật mới
  chưa có thẻ vẫn giữ chỉ số gốc. Đây là phiên client mới, không phải reconnect
  của cùng một instance; kiểm cùng-instance start/stop thuộc fixture Edit Mode.
- Console cuối Play rồi Stop: 0 lỗi đỏ, scene không dirty; Game View trả về tỷ lệ
  16:9 ban đầu. Có ba cảnh báo được giữ nguyên: gọi CardResume trước host sẵn
  sàng (đã chạy lại đạt), animation Attack của Tấm 0,13 s vượt 0,10 s được tự
  tăng tốc 1,30×, và MCP `BufferedFileLogStorage` dispose lúc Stop. Client đầu
  không có exception/cảnh báo; client snapshot có cảnh báo Attack nói trên,
  không có exception. Không sửa VFX để xoá cảnh báo ngoài phạm vi.
- Lỗi ban đầu nằm ở khởi tạo identity/SyncList của fixture Edit Mode và đã sửa
  trước lượt Edit PASS. Build client thành công; công cụ timeout/retry build,
  nên xác nhận cả log `BUILD Succeeded` và assembly client mới trước khi dùng.
  Build/log cục bộ không commit; thay đổi MCP/config có sẵn không đưa vào commit.

### Lặp lại kiểm snapshot vào muộn

Trên Arena host một người, dùng `script-execute` với
[`CardHistorySnapshotProbe.cs`](tools/CardHistorySnapshotProbe.cs), method `Run`.
Probe bắt đầu lại ván thử, chọn Tấm và áp bốn grant thật trên host, không sửa
asset. Sau đó mở development client theo hướng dẫn `CARD_TESTS.md`; ở log wave=1,
đối chiếu `[EvolutionClient]` của người host với `[HistorySnapshotHost]`. Bắt đầu
client ngay sau probe. Kiểm này chỉ nghiệm thu lịch sử nâng cấp, không nghiệm thu
quái/đạn khi vào muộn hoặc khôi phục phiên mạng.

![Nút đổi ở 720p](screenshots/card-reroll-720p.png)

![Nút đổi ở 1080p](screenshots/card-reroll-1080p.png)

![Nút đổi hover ở 1080p](screenshots/card-reroll-hover-1080p.png)

HUD demo và nút Stop Host/Client còn chồng góc trên ảnh; đây là overlay dev ngoài
Cards, chưa sửa trong lượt này.

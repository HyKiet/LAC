# T-32 — Đo tải và kiểm tra đọc hiểu, lượt 01/10/2026

**Trạng thái: T-32 chưa hoàn tất.** Đã có fixture lặp lại được và baseline Editor (T-32A).
P95 có dư ngân sách, nhưng còn hitch, cảnh báo pool số sát thương và vấn đề đọc hiểu.
Không sửa mã lõi, VFX, prefab, scene hoặc chỉ số game trong lượt này.

## Môi trường và cách đo

Unity 6000.5.6f1, Windows, Mirror host một người, transport LatencySimulation mặc định.
Intel i7-12650H · RTX 3050 Laptop GPU · Direct3D12. Game View cố định 1280×720 / 1920×1080,
vSync tắt, targetFrameRate không giới hạn. FixedUpdate 0,02 s (50 Hz).

Harness: `docs/tools/T32LoadProbe.cs`, nằm ngoài `Assets`, không đưa vào game.
Dùng Unity-MCP `script-execute`, full code = nội dung file, `className=T32LoadProbe`:

1. Mở Arena và Play host. Đặt độ phân giải Game View trước khi chạy.
2. `methodName=Run` cho đạn bay tiếp tuyến; `RunImpacts` cho đạn hướng vào đàn quái.
3. Setup restart ván thử, tạm ngừng lịch sinh và vũ khí tự bắn; giữ logic quái/đạn/vật lý,
   snapshot, hoạt ảnh, renderer và hit feedback thật. Quái dùng CoHon.asset với sprite Orc
   thử nghiệm; người chơi đổi sang Thạch Sanh trong runtime.
4. Duy trì 40 quái sống, 200 đạn ở LateUpdate bằng số active thật của pool; thêm 10 instance
   sóng âm, mỗi instance ba vòng. Vòng có thể tắt ở pha cuối animation, không phải 30 vòng
   luôn cùng hiện. Không gắn NetworkIdentity lên đạn hoặc hiệu ứng.
5. Đặt máu **component quái** 1.000.000 và bảo vệ người chơi để không hụt tải; không sửa
   EnemyData/CharacterData. Đạn flight gây 0 sát thương, impacts gây 0,05 và hit feedback thật.
   Đây là fixture tổng hợp, không phải lối chơi/build hợp lệ của một nhân vật.
6. Warmup 3 s, lấy mẫu 30 s. Không gọi Unity tools trong cửa sổ đo. Harness thu hồi/khôi phục
   trạng thái thử khi bị hủy; sau đo giữ timeScale=0 để chụp ảnh. Thoát Play giữa các lượt.

ProfilerRecorder lấy PlayerLoop và GC Allocated In Frame. FrameTimingManager lấy CPU main
không tính present wait và GPU; thời gian toàn khung lấy unscaledDeltaTime. JSON lưu đầy đủ
mean/p50/p95/p99/max, số mẫu và số khung vượt ngân sách tại `docs/measurements/`.
GC có cả chi phí Editor/harness, **không dùng để kết luận GC của bản phát hành**.

Lượt đầu có alias tham chiếu instance sau tái dùng pool khiến hụt tải; đã loại khỏi baseline,
sửa fixture duy trì theo CountActive và đo lại. Bốn JSON dưới đây đều có minEnemies=40,
minProjectiles=200, minWaves=10. Lượt không hợp lệ chỉ giữ trong `.codex/` cục bộ.

## Số liệu

Ngân sách một khung 60 FPS: 16,67 ms. P95 nghĩa là 95% mẫu không vượt con số đó.

| Tình huống | Độ phân giải | Số mẫu | Toàn khung P95 | PlayerLoop P95 | GPU P95 | Khung >16,67 ms |
|---|---|---:|---:|---:|---:|---:|
| Đạn bay | 1280×720 | 11.774 | 4,43 ms | 2,83 ms | 0,44 ms | 4 |
| Đạn bay | 1920×1080 | 12.855 | 3,90 ms | 2,55 ms | 0,62 ms | 6 |
| Trúng đòn | 1280×720 | 8.384 | 6,54 ms | 4,27 ms | 0,66 ms | 12 |
| Trúng đòn | 1920×1080 | 7.193 | 7,45 ms | 4,92 ms | 0,91 ms | 13 |

P99 toàn khung ở lượt trúng đòn 1080p là 10,47 ms; max 39,01 ms. Max các lượt nằm trong
22,99–42,95 ms. **Không kết luận ổn định 60 FPS ở mọi khung**, không suy ra nâng độ phân giải
làm game nhanh hơn từ hai lượt flight: nhiễu Editor/lịch chạy/hardware chưa kiểm soát.
Không xác định nguyên nhân hitch chỉ từ dữ liệu tổng hợp này.

Console cuối lượt impacts 720p: **0 lỗi đỏ, 1 cảnh báo**:
`[ObjectPool] Vượt ngưỡng 128 đối tượng: DamageNumber. Tăng giá trị prewarm để tránh cấp phát giữa trận.`
Cả hai lượt impacts đều ghi cảnh báo này. Cần đo peak/pool growth của số sát thương trong
bản build rồi chọn prewarm hoặc gom số theo tick; chưa thay đổi pool trong lượt này.

## Đọc hiểu — chưa đạt ở fixture dày

Đã nhìn ảnh Game View thật bên dưới, không kết luận chỉ bằng mã.

- 200 sprite đạn thử nghiệm sáng, khá lớn, che đáng kể vùng quanh nhân vật khi dồn vào giữa.
- Số sát thương chồng nhau dày trong impacts; khó đọc từng số và ước lượng đám quái.
- Nhân vật đứng trong vòng 40 Orc khó tách khỏi đàn. Đây là sprite thử nghiệm, không phải
  đánh giá chất lượng mỹ thuật cuối cùng của T-33/T-47.
- Khi nhiều sóng âm cùng hiện, dải additive trắng rất sáng. Prefab có `_peakAlpha=0.07`
  đúng PALETTE.md, nhưng trần đó dựa trên khoảng ba lớp chồng, không bảo đảm 10 sóng × 3 vòng.
  Cần kiểm tổng sáng khi chồng nhiều hiệu ứng, không chỉ alpha riêng của một vòng.
- Chưa có quái bắn đạn địch (T-34), nên chưa kiểm chứng đọc màu son dưới tải thật.

Ảnh flight 1080p giữ pha `_age=0,20…0,29` của mười instance sóng **sau khi đo**, để nhìn rõ
chồng sáng. Không sửa pixel ảnh hoặc asset; không dùng pha giữ này để thay số liệu benchmark.
Các ảnh khác là trạng thái fixture được giữ ngay sau đo.

![Đạn bay 720p](screenshots/t32-flight-720p.png)

![Sóng âm chồng sáng 1080p](screenshots/t32-flight-1080p-waves.png)

![Trúng đòn 720p](screenshots/t32-impacts-720p.png)

![Trúng đòn 1080p](screenshots/t32-impacts-1080p.png)

## Việc tiếp theo để đóng T-32

1. @Kiet: đo allocation/pool growth và hitch bằng Profiler trace trên standalone, gồm cả
   chết quái/Hồn, vũ khí tự bắn, hai người chơi và các loại quái thực tế.
2. @Kiet + artist: giảm diện tích/độ sáng biểu diễn đạn, điều tiết số sát thương, bảo vệ silhouette
   nhân vật và kiểm tổng sáng additive. Không đổi damage/collider chỉ để làm hình đẹp.
3. Khi T-34 có đạn địch: chụp và đo tương phản nhóm son ở tải dày; chạy lại trên build phát hành.
4. Chỉ đánh dấu T-32 [x] sau khi cả hiệu năng và đọc hiểu đạt. T-32A chỉ xác nhận fixture/baseline.

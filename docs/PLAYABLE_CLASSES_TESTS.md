# Warrior, Archer, Lancer — kiểm chứng 07/10/2026

## Dùng thử

Mở `Assets/_LAC/Scenes/Arena.unity`, vào Play. Chọn một trong ba lớp rồi bấm **Vào trận / Tạo host**. Chơi đơn cũng dùng host Mirror. Máy thứ hai chọn lớp, nhập IP máy host và bấm **Vào host**; dùng `localhost` khi hai tiến trình chạy cùng máy. Có thể chọn trùng lớp. Restart giữ lớp; màn sảnh cùng Sẵn sàng/Steam invite chưa thuộc màn chọn thử nghiệm này.

| Lớp | Đòn đánh | Máu / tốc độ | Tầm / chu kỳ / sát thương |
|---|---|---|---|
| Warrior | Chém cung 110° hướng tới quái gần nhất | 10 / 4 | 2.2 / 0.65s / 4 |
| Archer | Tên trúng mục tiêu đầu tiên; tên kế tiếp sau dash ×2 | 6 / 5.5 | 8 / 0.7s / 4 |
| Lancer | Đâm xuyên trong dải rộng 0.8, có hoạt ảnh nhiều hướng | 8 / 4.5 | 3.2 / 0.85s / 5 |

Thẻ yêu cầu đạn chỉ áp dụng Archer; nâng cấp chỉ số chung áp dụng cả ba. Dữ liệu gốc không bị sửa trong ván. Không còn bụi cỏ trong bất kỳ nhóm trang trí nào.

## Kiểm thử đã chạy

- `LAC > Tests > Check Playable Classes (Play Mode)` từ màn chọn: **ALL PASSED**. Xác nhận wave 0/chưa có player trước trận, callback chọn/vào trận, đúng lớp và máu cho cả ba, hide menu sau spawn, host dừng và mở lại; chỉ một EventSystem.
- Kiểm bổ sung sau biên dịch cuối: EventSystem raycast trúng nút, sự kiện pointer-click chọn Archer/vào host, input bindings của InputSystemUIInputModule có sẵn; spawn 6 HP và menu đóng. Chạy tiếp gameplay Archer sang wave 2; Console cuối **0 lỗi/0 cảnh báo**, compilationFailed false. [UI](validation/T44C/ui.txt), [Console cuối](validation/T44C/final-console.txt), [Game View Archer](validation/T44C/archer.png).
- Đòn đánh thật qua `WeaponAuto.Update` và EnemySpawner: Warrior/Lancer đánh hai mục tiêu đúng vùng, không trúng bên hông/phía sau/ngoài tầm. Archer chỉ gây sát thương sau tên bay, không tự có splash/pierce; tên sau dash nhận thưởng. Kiểm biên vùng đâm và chọn clip lên/xuống/chéo. Arrow không có NetworkIdentity.
- Probe renderer: Idle/Walk/Attack của cả ba và các hướng đâm Lancer đổi frame; Death một khung giữ tĩnh, Unlock trở lại hoạt ảnh. [Kết quả](validation/T44C/animation.txt).
- `Check 15 Wave Content`: **ALL PASSED**, host Lancer đi qua 15 wave thật, 14 pha chọn thẻ, Chằn Tinh, Victory, ba nhóm trang trí và Restart. Fixture tăng tốc lịch sinh/giết quái; đây không phải playtest cân bằng 15 phút. [Kết quả](validation/T44C/run.txt), [CSV wave](validation/T44C/waves.csv).
- Co-op hai Editor: host Lancer **8 HP**, client Archer **6 HP**, cùng wave 1/15 và bộ hoạt ảnh tương ứng; LatencySimulation 100ms. Yêu cầu chọn lần hai/ID99 sau spawn không thay lớp hoặc thêm player. Guard ID99 trước spawn cũng được gọi trực tiếp và từ chối. Restart trên host giữ bộ đôi/lớp/máu. [Log client](validation/T44C/client.txt).
- Đã xem Game View màn chọn, Warrior, Lancer và bộ đôi Lancer/Archer: chữ/nút/portrait đọc được, không còn HUD mạng IMGUI, đá rìa sân giữ tương phản. [Màn chọn](validation/T44C/selection.png), [co-op](validation/T44C/coop.png).

Client co-op dùng bản sao dự án trong TEMP, cùng gameplay/assets, bỏ package MCP và plugin phụ khỏi bản sao để không kết nối nhầm Editor. Client chạy `Unity.exe -batchmode -nographics -executeMethod CloneRun.Start --lac-telemetry-client <command-file> --lac-class Archer`. Có lỗi Unity Search lúc khởi tạo chỉ mục của Editor không giao diện; log gameplay vẫn xác nhận kết nối/lớp. Không dùng lần này để chứng nhận bản executable Windows hoặc toàn bộ 15 wave co-op.

Hiệu năng Game View 1309×616 trên i9-12900H/RTX 3060 Laptop: 40 quái và 200 đạn, P95 **4.895 ms**, mean 4.186 ms, 7166 frame render trong phép đo. [JSON](validation/T44C/performance.json). Fixture giữ người chơi Lancer và tải VFX chung; đây không phải chứng nhận mọi độ phân giải/phần cứng. Console gameplay 0 lỗi; fixture giết nhanh 283 quái của phép thử 15 wave có một warning vượt pool 256 Hồn vì Hồn chưa kịp bay về. [Console](validation/T44C/console.txt).

## Build Windows đang bị chặn bởi môi trường

Development build: **Failed, 1 error, 8 warnings**. Lỗi cụ thể:

```
Unity.Linker.Api.dll
An Application Control policy has blocked this file. (0x800711C7)
```

Nhật ký Code Integrity của Windows xác nhận chặn DLL Unity BuildPipeline; DLL không có Zone.Identifier để gỡ đánh dấu tải về. Không thay chính sách bảo mật máy. Mã runtime đã compile và Play được, nhưng chưa có executable mới được kiểm chứng; cần cho phép công cụ Unity trong chính sách Windows rồi build lại. [Báo cáo build](validation/T44C/build.txt).

Tám warning còn lại từ package/fixture API cũ (MCP asmdef trống, profiler render callback và FindObjectsByType), không phải lỗi C# mới. Console gameplay được kiểm riêng sau lần build bị chặn.

## Phạm vi đồ án

Ba lớp và màn chọn giảm công nội dung. Co-op trực tuyến, LinUCB/đạo diễn thích ứng, telemetry và thực nghiệm vẫn là trọng tâm kỹ thuật. T-45/T-45B/T-46, Steam ngoài LAN và đánh giá người dùng chưa được báo hoàn thành bởi thay đổi này. Sprite bên thứ ba được ghi đúng nguồn trong báo cáo, không nhận là mỹ thuật tự vẽ.

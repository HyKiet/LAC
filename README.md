# LẠC — Children of the Dragon

Arena survival roguelite 2D trên nền thần thoại Việt Nam. Ba lớp Warrior/Archer/Lancer, 10 wave, mục tiêu 8–10 phút, co-op hai người. Chỉ làm đồ án tốt nghiệp, không phát hành thương mại.

> Đồ án tốt nghiệp · Unity 6000.5.6f1 · URP 2D · Mirror + KCP

Yêu cầu: **multiplayer online, có AI, có điểm riêng**. Đạo diễn dựa trên luật/FSM, chọn Fixed/Adaptive trước trận, HUD/F3 và CSV đã có; xem [kiểm chứng AI](docs/DIRECTOR_TESTS.md). Trống Đồng/Hồn dùng chung đã có. Kiểm thử hai tiến trình hiện có chưa chứng nhận chơi qua Internet. Xem [phạm vi và đánh giá](docs/THESIS_SCOPE.md); bản Windows 10 wave mới còn cần nghiệm thu sau khi xử lý điểm chặn build.

---

## Thiết lập môi trường

Thực hiện một lần trên mỗi máy.

```bash
git lfs install
git clone https://github.com/HyKiet/LAC.git
```

Cấu hình công cụ hợp nhất của Unity. **Bước này bắt buộc** — thiếu nó, xung đột trên tệp scene sẽ phải xử lý thủ công:

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver \
  '"D:/Unity/Hub/Editor/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

Đường dẫn trên tương ứng với vị trí cài Unity của nhóm. Nếu máy của bạn cài ở nơi khác, sửa lại cho đúng — xác nhận tệp `UnityYAMLMerge.exe` tồn tại tại vị trí đó trước khi chạy lệnh.

Mở dự án qua Unity Hub với **đúng phiên bản 6000.5.6f1**.

Mở thư mục dự án bằng VS Code và chấp nhận danh sách extension được đề xuất. Sau khi cài, phím `Alt+C` dùng để đánh dấu hoàn thành hạng mục trong kế hoạch công việc.

**Sau khi clone, chạy một lần:**

```powershell
powershell -ExecutionPolicy Bypass -File tools\setup-git.ps1
```

Script cài công cụ merge cho file scene của Unity và bật Git LFS. Không chạy thì xung đột scene sẽ không tự gỡ được — xem [docs/WORKFLOW.md](docs/WORKFLOW.md).

---

## Quy trình làm việc

1. Đọc [CLAUDE.md](CLAUDE.md) — tài liệu chủ đạo, cung cấp đủ thông tin để bắt đầu.
2. Mở [docs/TASKS.md](docs/TASKS.md) và chọn một hạng mục còn ở trạng thái *Chưa phân công*.
3. Điền tên vào hạng mục và đẩy lên remote ngay để tránh trùng lặp công việc.
4. Sau khi hoàn thành: đánh dấu `[x]`, bổ sung dòng trích dẫn mô tả chức năng và vị trí tệp.
5. Commit theo định dạng `feat(T-11): dash có i-frame` và đẩy lên nhánh `dev`.

---

## Bộ tài liệu

| Tài liệu | Nội dung |
|---|---|
| [CLAUDE.md](CLAUDE.md) | Tài liệu chủ đạo — tổng quan sản phẩm, ràng buộc kiến trúc, quy trình |
| [docs/TASKS.md](docs/TASKS.md) | Kế hoạch công việc, phân công, nhật ký hoàn thành |
| [docs/GDD.md](docs/GDD.md) | Thiết kế bản đồ án 10 wave, không thương mại |
| [docs/THESIS_SCOPE.md](docs/THESIS_SCOPE.md) | Đánh giá hiện trạng, phạm vi thu gọn và tiêu chí bảo vệ |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Sơ đồ hệ thống và quan hệ giữa các module |
| [docs/WORKFLOW.md](docs/WORKFLOW.md) | **Quy trình làm việc nhóm — đọc trước khi commit lần đầu** |
| [docs/PALETTE.md](docs/PALETTE.md) | Bảng 24 màu Đông Hồ đã chốt, và ràng buộc màu cho đòn địch |
| [docs/ASSETS_ThirdParty.md](docs/ASSETS_ThirdParty.md) | Gói asset thử nghiệm: dùng phần nào, gỡ ra thế nào |
| [docs/learning/README.md](docs/learning/README.md) | Lộ trình đọc code dành cho người mới, giải thích từng file first-party |

---

## Ràng buộc kiến trúc

1. **Không tồn tại nhánh mã riêng cho chế độ chơi đơn.** Chơi đơn là Mirror host mode với một client.
2. **Đồng bộ sự kiện, không đồng bộ trạng thái.** Không gắn `NetworkIdentity` lên đạn.
3. **Cấm `UnityEngine.Random` trong luồng gameplay.** Sử dụng `RunRandom` có seed.

Diễn giải đầy đủ tại [CLAUDE.md](CLAUDE.md) mục 3.

---

## Nhóm phát triển

| Thành viên | Vai trò |
|---|---|
| `@Kiet` | Gameplay · đồng bộ trong ván · AI · telemetry và đánh giá |
| `@Hung` | Lập trình |
| `@Kang` | Lập trình |
| `@artist` | Pixel art · hiệu ứng · âm thanh |

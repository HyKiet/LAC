# LẠC — Children of the Dragon

Arena survival roguelite 2D trên nền thần thoại Việt Nam. Ba nhân vật với ba lối chơi phân biệt, mỗi ván 15 phút, hỗ trợ co-op hai người qua Steam.

> Đồ án tốt nghiệp · Unity 6000.5.6f1 · URP 2D · Mirror + Steamworks.NET

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
| [docs/GDD.md](docs/GDD.md) | Đặc tả thiết kế gốc — một số nội dung đã lỗi thời, xem mục 7 của CLAUDE.md |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Sơ đồ hệ thống và quan hệ giữa các module |

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
| `@Kiet` | Lập trình · quản lý sản phẩm · quảng bá |
| `@Hung` | Lập trình |
| `@Kang` | Lập trình |
| `@artist` | Pixel art · hiệu ứng · âm thanh |

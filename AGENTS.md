# AGENTS.md

Dự án sử dụng [CLAUDE.md](CLAUDE.md) làm tài liệu chủ đạo cho toàn bộ công cụ AI tham gia phát triển.

**Đọc [CLAUDE.md](CLAUDE.md) trước khi thực hiện bất kỳ thay đổi nào lên mã nguồn.**

Ba ràng buộc kiến trúc bắt buộc — diễn giải đầy đủ tại mục 3 của tài liệu chủ đạo:

1. **Không tồn tại nhánh mã riêng cho chế độ chơi đơn.** Chơi đơn là Mirror host mode với một client. Nghiêm cấm câu lệnh dạng `if (isSinglePlayer)`.
2. **Đồng bộ sự kiện, không đồng bộ trạng thái.** Không gắn `NetworkIdentity` lên đạn hoặc hiệu ứng hình ảnh.
3. **Cấm `UnityEngine.Random` trong luồng gameplay.** Sử dụng `LAC.Core.RunRandom`.

Khi hoàn thành một hạng mục, việc cập nhật [docs/TASKS.md](docs/TASKS.md) — đánh dấu `[x]` và bổ sung dòng trích dẫn mô tả chức năng — là một phần của hạng mục đó, không phải bước tuỳ chọn. Không báo cáo hoàn thành khi chưa thực hiện.

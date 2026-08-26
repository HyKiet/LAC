# AGENTS.md

Dự án này dùng [CLAUDE.md](CLAUDE.md) làm tài liệu hướng dẫn chính cho mọi công cụ AI.

**Đọc [CLAUDE.md](CLAUDE.md) trước tiên**, sau đó theo mục 7 của file đó để biết đọc tiếp gì.

Tóm tắt ba ràng buộc tuyệt đối (chi tiết ở CLAUDE.md mục 3):

1. **Không có "chế độ chơi đơn" riêng.** Chơi đơn = Mirror host mode với 1 client. Không viết `if (isSinglePlayer)`.
2. **Đồng bộ sự kiện, không đồng bộ trạng thái.** Không gắn `NetworkIdentity` lên đạn hay VFX. Xem [docs/NETCODE.md](docs/NETCODE.md).
3. **Không `UnityEngine.Random` trong gameplay.** Dùng `LAC.Core.RunRandom` có seed.

Sau khi hoàn thành một task, **bắt buộc** cập nhật [docs/TASKS.md](docs/TASKS.md) (tick ô) và [docs/PROGRESS.md](docs/PROGRESS.md) (ghi mục theo mẫu) trong cùng commit trước khi báo xong.

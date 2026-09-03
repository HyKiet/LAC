# Hướng dẫn cho GitHub Copilot

Đọc [AGENTS.md](../AGENTS.md) trước, rồi [CLAUDE.md](../CLAUDE.md).

Ba ràng buộc không được vi phạm:

1. Không có nhánh mã riêng cho chơi đơn — chơi đơn là Mirror host mode với một client. Cấm `if (isSinglePlayer)`.
2. Đồng bộ sự kiện, không đồng bộ trạng thái. Không gắn `NetworkIdentity` lên đạn hay hiệu ứng.
3. Cấm `UnityEngine.Random` trong luồng gameplay — dùng `LAC.Core.RunRandom`.

Định danh viết bằng tiếng Anh; chú thích và văn bản hiển thị viết bằng tiếng Việt.

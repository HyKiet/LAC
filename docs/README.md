# Tài liệu LẠC

Phạm vi từ 11/10/2026: **đồ án tốt nghiệp, không phát hành thương mại**. Giảng viên yêu cầu online multiplayer, có AI và điểm riêng; không bắt buộc học máy/LinUCB hoặc mẫu 30 người.

| Đọc theo thứ tự | Nội dung |
|---|---|
| [THESIS_SCOPE.md](THESIS_SCOPE.md) | Đánh giá game, phần giữ/bỏ, đạo diễn đơn giản, tiêu chí online/build và kịch bản bảo vệ |
| [CLAUDE.md](../CLAUDE.md) | Ràng buộc kiến trúc và quy ước; tài liệu chủ đạo |
| [TASKS.md](TASKS.md) | Ai làm gì, chức năng đã có và mục còn thiếu; dự phòng/ngoài phạm vi được ghi rõ |
| [GDD.md](GDD.md) | Thiết kế hiện hành: 10 wave, một arena, ba lớp, 12 thẻ, Hồn/Trống Đồng |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Hệ thống hiện có và điểm tích hợp đạo diễn dự kiến |
| [WORKFLOW.md](WORKFLOW.md) | Phân công thư mục, Git và phối hợp nhóm |
| [CARDS.md](CARDS.md) | Chỉ số 12 thẻ; tiến hoá chỉ là dự phòng |
| [PALETTE.md](PALETTE.md), [ASSETS_ThirdParty.md](ASSETS_ThirdParty.md) | Chuẩn đọc hình ảnh và nguồn tài nguyên |

Bằng chứng hiện hành của cấu hình 10 wave nằm ở [TEN_WAVE_TESTS.md](TEN_WAVE_TESTS.md) và [validation/T44D](validation/T44D). Các báo cáo `*_TESTS.md` khác có ngày/cấu hình riêng (kể cả 15/16 wave); giữ nguyên để đối chiếu lịch sử. Không ghép chúng thành kết luận “mọi tính năng đã nghiệm thu”.

[UNITY_BUILD_BLOCKER.md](UNITY_BUILD_BLOCKER.md) ghi lịch sử lỗi môi trường; mốc mới hơn trong PLAYABLE_CLASSES_TESTS/TEN_WAVE_TESTS vẫn chưa chứng nhận executable 10 wave. AI thích ứng và chơi Internet giữa hai mạng chưa hoàn thành.

[learning](learning/README.md) là tài liệu học theo mốc đầu dự án; dùng để hiểu nguyên lý, đối chiếu code/TASKS khi tìm trạng thái hiện tại. `tools/` chứa trình sinh mỹ thuật/chữ cũ, không là kế hoạch tính năng. `validation/` lưu ảnh/log/CSV kiểm thử, không là dữ liệu khảo sát người dùng.

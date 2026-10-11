# Tài liệu LẠC

Phạm vi từ 11/10/2026: **đồ án tốt nghiệp, không phát hành thương mại**. Giảng viên yêu cầu online multiplayer, có AI và điểm riêng; không bắt buộc học máy/LinUCB hoặc mẫu 30 người.

| Đọc theo thứ tự | Nội dung |
|---|---|
| [THESIS_SCOPE.md](THESIS_SCOPE.md) | Đánh giá game, phần giữ/bỏ, đạo diễn đơn giản, tiêu chí online/build và kịch bản bảo vệ |
| [CLAUDE.md](../CLAUDE.md) | Ràng buộc kiến trúc và quy ước; tài liệu chủ đạo |
| [TASKS.md](TASKS.md) | Ai làm gì, chức năng đã có và mục còn thiếu; dự phòng/ngoài phạm vi được ghi rõ |
| [GDD.md](GDD.md) | Thiết kế hiện hành: 10 wave, một arena, ba lớp, 12 thẻ, Hồn/Trống Đồng |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Hệ thống hiện có, đạo diễn luật/FSM, ngữ cảnh đội và nguồn WaveSpec trên host |
| [AI_CHAN_TINH.md](AI_CHAN_TINH.md) | Chằn Tinh là chủ dungeon: ý đồ, luật, ví dụ co-op, FSM boss và cách giải thích khi báo cáo |
| [WORKFLOW.md](WORKFLOW.md) | Phân công thư mục, Git và phối hợp nhóm |
| [CARDS.md](CARDS.md) | Chỉ số 12 thẻ; tiến hoá chỉ là dự phòng |
| [PALETTE.md](PALETTE.md), [ASSETS_ThirdParty.md](ASSETS_ThirdParty.md) | Chuẩn đọc hình ảnh và nguồn tài nguyên |

Bằng chứng hiện hành của cấu hình 10 wave nằm ở [TEN_WAVE_TESTS.md](TEN_WAVE_TESTS.md) và [validation/T44D](validation/T44D). Các báo cáo `*_TESTS.md` khác có ngày/cấu hình riêng (kể cả 15/16 wave); giữ nguyên để đối chiếu lịch sử. Không ghép chúng thành kết luận “mọi tính năng đã nghiệm thu”.

T-45/T-45B/T-46 đã có mã đạo diễn ba trạng thái, luật ScriptableObject, snapshot đội trước thẻ/hồi máu, chọn Cố định/Thích ứng trước kết nối, HUD/F3 và CSV schema 2 (`directors.csv`). [DIRECTOR_TESTS.md](DIRECTOR_TESTS.md) ghi kiểm chứng và giới hạn của phần tích hợp này; fixture có kiểm soát không thay thế playtest cân bằng hoặc nghiệm thu Internet.

[UNITY_BUILD_BLOCKER.md](UNITY_BUILD_BLOCKER.md) ghi lịch sử lỗi môi trường; mốc mới hơn trong PLAYABLE_CLASSES_TESTS/TEN_WAVE_TESTS vẫn chưa chứng nhận executable 10 wave. Chơi Internet giữa hai mạng vẫn chưa nghiệm thu; mã AI mới không giải quyết thay điểm chặn build hoặc yêu cầu online này.

[learning](learning/README.md) là tài liệu học theo mốc đầu dự án; dùng để hiểu nguyên lý, đối chiếu code/TASKS khi tìm trạng thái hiện tại. `tools/` chứa trình sinh mỹ thuật/chữ cũ, không là kế hoạch tính năng. `validation/` lưu ảnh/log/CSV kiểm thử, không là dữ liệu khảo sát người dùng.

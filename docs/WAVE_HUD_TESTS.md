# HUD đợt — kiểm tra 29/09/2026

> Phạm vi hiện hành 11/10/2026: [THESIS_SCOPE.md](THESIS_SCOPE.md), 10 wave, đồ án không thương mại, AI luật/FSM và Internet qua KCP. Kết quả bên dưới giữ nguyên ngày/cấu hình lịch sử; không chứng nhận AI thích ứng, Internet hoặc build mới.

Unity 6000.5.6f1, Arena, Mirror host dùng LatencySimulation mặc định. Prefab riêng được tạo qua Unity Editor API, không sửa scene. HUD chỉ đọc RunManager, không thay đổi thời điểm sinh quái hay điều kiện chọn thẻ.

## Thiết kế

- Góc neo giữa phía trên: `ĐỢT 01 / 16`, tổng đợt lấy từ bảng của RunManager.
- Thông báo bên dưới: trượt vào 0.28 giây, giữ 1.15 giây, trượt ra và mờ dần 0.32 giây. Số đợt cố định phóng nhẹ khi chuyển đợt.
- Dùng nhóm than, điệp, hoè và chàm; không dùng nhóm son. Các graphic không nhận raycast; CanvasGroup không chặn điều khiển.
- Chuyển động dùng thời gian không bị ảnh hưởng bởi hit-stop. Chọn thẻ hoặc kết thúc ván sẽ đóng thông báo ngay.
- Dùng phông dựng sẵn như HUD máu và giao diện thẻ, không phụ thuộc thư mục TMP chưa được quản lý trong Git.

## Đã kiểm tra

| Tình huống | Kết quả |
|---|---|
| Đang chơi | Hiển thị đúng số đợt từ RunManager |
| Host báo sạch đợt | Trạng thái `ĐÃ VƯỢT QUA · CHỌN THẺ`, đóng thông báo |
| Host cho phép đợt tiếp theo | Số đợt tăng, thông báo phát một lần |
| Lặp Update không đổi trạng thái | Không phát lại chuyển động |
| Chuyển động tại 0.14 / 0.5 / 1.6 / 2 giây | Alpha 0.5 / 1 / 0.46875 / đóng hoàn toàn |
| Time.timeScale = 0 | Thông báo vẫn kết thúc theo unscaledTime |
| Đợt cuối | `ĐỢT 16 / 16`, nhãn `ĐỢT CUỐI` |
| Thua hoặc thắng | Đóng thông báo; nhãn kết thúc tương ứng |
| Chơi lại | `ĐỢT 01 / 16`, phát thông báo đợt đầu |
| Game View 1311 × 623 | Đã xem ảnh: chữ tiếng Việt rõ, không chồng HUD máu và khu vực nhân vật ở giữa |
| Console cuối lượt kiểm tra | 0 lỗi đỏ, 0 cảnh báo |

Các chuyển trạng thái được kích hoạt trực tiếp trên host bằng Unity-MCP để kiểm tra nhanh. Khi chụp ảnh, thời gian giữ bảng được tăng tạm trong Play mode rồi trả về; prefab giữ nguyên 1.15 giây. Đã thoát Play mode sau kiểm tra. Chưa kiểm tra bằng hai tiến trình co-op hoặc nhiều tỉ lệ màn hình.

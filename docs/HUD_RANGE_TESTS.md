# Kiểm tra HUD và tầm đánh — 29/09/2026

> Phạm vi hiện hành 11/10/2026: [THESIS_SCOPE.md](THESIS_SCOPE.md), 10 wave, đồ án không thương mại, AI luật/FSM và Internet qua KCP. Kết quả bên dưới giữ nguyên ngày/cấu hình lịch sử; không chứng nhận AI thích ứng, Internet hoặc build mới.

Kiểm tra trực tiếp bằng Unity-MCP trong Arena, Unity 6000.5.6f1, Mirror host với LatencySimulation mặc định. Các tình huống biên được dựng tạm trong Play mode bằng reflection; không ghi vào CharacterData hay scene. Sau kiểm tra đã thoát và chạy lại để loại bỏ trạng thái thử nghiệm.

## Thay đổi

- HUD chuyển từ ô rời sang thanh ngang, nền tối và màu chàm trong bảng Đông Hồ, có số hiện tại/tối đa. Giữ tham chiếu scene bằng `FormerlySerializedAs`; không cần sửa YAML. Dùng phông dựng sẵn giống giao diện thẻ hiện có.
- Giữ tầm gốc: Gióng 3.2, Thạch Sanh 4, Tấm 7. Tầm chọn mục tiêu và sát thương vòng/cung đo tới tâm quái; đạn bay đúng quãng đường khai báo, va chạm tính thêm bán kính đạn và collider quái. Nổ vẫn là sát thương vùng riêng của thẻ.
- Vệt cung lấy bán kính/góc từ cùng dữ liệu với đòn đánh, hiện ngay và mờ dần; không còn xoay một sprite có góc cố định ra ngoài cung sát thương.

## Kết quả

| Tình huống | Kết quả |
|---|---|
| HUD mất máu, hồi máu | 5/10 → 2/10 → 3/10 |
| Chỉ tăng máu tối đa | 3/14, phần đầy 0.2142857 |
| Chết, chơi lại | 0/14 → 10/10 |
| Mục tiêu đúng mép/ngoài mép 0.01 | Đúng cả ba nhân vật, 6 kiểm tra |
| Gióng trong/ngoài góc, ngoài bán kính, quái chồng vị trí | Đạt 4 kiểm tra |
| Thạch Sanh đúng mép/ngoài vòng tròn | Đạt 2 kiểm tra |
| Đạn tốc độ 500 đi xuyên vị trí quái trong một bước | Vẫn trúng |
| Đạn tốc độ 12, tầm 7 | Dừng ở 7, sai số dưới 0.001 |
| Quái tại 8, ngoài cả quãng bay và hitbox | Không trúng |
| Hitbox chạm đoạn cuối bị rút ngắn | Vẫn trúng trước khi thu hồi đạn |
| Hai quái đăng ký theo thứ tự xa rồi gần | Đạn thường trúng con gần trước |
| Đạn xuyên hai quái trong một bước | Cả hai nhận sát thương |
| Thẻ Âm Vang và chơi lại | Gióng 3.2 → 3.52 → 3.2 |
| Ảnh Game View | Đã xem thanh đầy, thanh vơi, chữ và cung 110° bán kính 3.2 |
| Lượt chạy mới sau kiểm tra | Console 0 lỗi đỏ, 0 cảnh báo |

Ảnh cung được giữ lâu tạm thời trong Play mode để nhìn rõ hình học; thời lượng trong prefab vẫn 0.16 giây. Không lưu thay đổi thử nghiệm này.

Chưa chạy lại hai tiến trình co-op hoặc bài đo tải 40 quái/200 đạn; không đánh dấu T-32 hoàn tất. Các phép kiểm tra trên không thay thế nghiệm thu hiệu năng hay mạng LAN.

`git pull origin dev` không thực hiện được vì remote không có nhánh `dev`; phiên làm việc hiện ở `main`.

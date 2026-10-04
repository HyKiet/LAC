# Unity Safe Mode — chẩn đoán ngày 04/10/2026

**Cập nhật cùng ngày: môi trường đã hoạt động trở lại sau khi người dùng mở lại Unity.** MCP trả `pong`, `scriptCompilationFailed=false`, Play Mode chạy được và Windows Development build thành công (0 lỗi, 2 cảnh báo). Không có thao tác thay đổi chính sách Windows từ tác vụ này; chưa xác định thay đổi bên ngoài nào đã gỡ chặn. Nội dung dưới đây giữ lại lịch sử chẩn đoán lúc Safe Mode.

## Bằng chứng tại máy

- Unity 6000.5.6f1 đang ở Safe Mode. `Logs/Editor.log`, dòng 287–303: `BuildProgram exited with code -532462766`, bên trong là `FileLoadException` / `0x800711C7`.
- Thư viện bị chặn: `C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Data\Tools\BuildPipeline\BeeBuildProgramCommon.dll`. Phiên trước chặn `Bee.Tools.dll` trong cùng thư mục.
- Windows Event Log `Microsoft-Windows-CodeIntegrity/Operational`: sự kiện 3077 và 3033 lúc 10:26 ngày 04/10/2026 ghi nhận `netcorerun.exe` bị từ chối nạp thư viện trên.
- `CiTool.exe -lp -json`: policy `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` là `VerifiedAndReputableDesktop`, đang enforced. Đây là Smart App Control; registry `VerifiedAndReputablePolicyState=1`.
- `Get-AuthenticodeSignature`: `NotSigned`. File không có stream `Zone.Identifier`.
- SHA-256 của DLL: `5907514BC94224C3C4FF782D21DCB3592410E09A9CFBDE5A9B644DD4B0B5E148`. Hash chỉ để đối chiếu, chưa so sánh với bản phân phối chính thức.
- MCP `ping` và `editor-application-get-state` đều thất bại sau 10 retries. Chưa xác nhận được kết nối hoạt động trong Safe Mode.

## Hướng xử lý

1. Gửi đường dẫn DLL, hash và thông tin sự kiện ở trên cho Unity Support hoặc người quản trị máy để kiểm tra bản phân phối Unity **6000.5.6f1**. Cần thư viện được Windows tin cậy; chưa có bằng chứng cài lại cùng bộ cài sẽ hết lỗi. Không đổi phiên bản engine của dự án trong bước này.
2. Microsoft giải thích Smart App Control có thể chặn mã chưa được xác nhận tin cậy và không có chữ ký hợp lệ; hiện không có nút cho phép riêng từng ứng dụng. Hướng ưu tiên là nhà phát hành ký mã hợp lệ. Xem [Microsoft Smart App Control FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions) và [hướng dẫn ký mã](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control). Việc thay đổi bảo vệ toàn máy cần chủ máy/quản trị viên quyết định; tác vụ này không sửa chính sách Windows.
3. Khi nguyên nhân chặn đã được xử lý, mở lại dự án bằng Unity Hub với quyền người dùng thông thường và chờ biên dịch/import hoàn tất. Cảnh báo chạy Administrator trong ảnh là cảnh báo riêng; chạy Administrator hiện không vượt được lỗi Code Integrity.
4. Kiểm tra lại MCP bằng hai lệnh dưới. Chỉ khi trả thành công và Editor không biên dịch/cập nhật mới tiếp tục [nghiệm thu T-43](TELEMETRY_TESTS.md#nghiệm-thu-còn-lại-sau-khi-unity-biên-dịch-được).

```powershell
unity-mcp-cli.cmd run-system-tool ping --input '{}' --raw
unity-mcp-cli.cmd run-tool editor-application-get-state --input '{}' --raw
```

Kết quả nghiệm thu T-43 mới nhất nằm trong [TELEMETRY_TESTS.md](TELEMETRY_TESTS.md); lịch sử lỗi môi trường này không thay thế kết quả kiểm thử.

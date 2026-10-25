# Tiến độ QA và môi trường riêng

349/349 ca runner đạt; {"PASS":451,"N/A":7,"NOT RUN":47,"BLOCKED":2} dòng checklist (gồm truy vết tái sử dụng, không phải số lần chạy độc lập).

Đã hoàn thành: cấu hình QA riêng; init/migration/seed idempotent; backend cổng riêng; credential ngẫu nhiên cục bộ; guard server/DB/HTTP identity cho test cũ, Chrome và WinForms; kiểm tra âm cho guard; chạy lại 2 nhóm rủi ro và toàn bộ runner; đặt đủ hồ sơ trong docs/qa. Xem WORKSPACE_BASELINE.md để tách công việc có sẵn và thay đổi lượt này.

Chưa hoàn tất nghiệm thu toàn bộ: các dòng NOT RUN/BLOCKED vẫn được liệt kê trong TEST_REPORT.md; chưa xác minh mọi nhánh UI, profiling N+1, máy sạch, in vật lý; backup PS bị policy chặn. Cleanup destructive chưa chạy; không tự dọn DB bằng wildcard.

Bước tiếp theo: thực hiện lần lượt các dòng NOT RUN, ưu tiên nhánh UI và đồng nhất trạng thái/lỗi; thử máy sạch và in khi có thiết bị; chỉ gọi cleanup khi muốn dọn môi trường tương tác. Không tự chuyển sang PhoneStore nếu thiếu quyền/dịch vụ.

## WinApp Windows nhấp đúp

Đã publish Admin/backend win-x64 self-contained, tạo shortcut Desktop và cấu hình LocalAppData trỏ đúng QA hiện có. Packaging10 ca, UI shortcut6 ca và UI HTTP500 đạt; hồi quy toàn hệ thống 349/349 đạt. Xem ADMIN_PACKAGING_REPORT.md. Chưa thử đóng VS Code và máy sạch không SDK; không tự đóng công việc người dùng. Các NOT RUN/BLOCKED QA tổng thể ở trên vẫn còn hiệu lực.

## Website khách hàng cập nhật

Đã triển khai Home/Catalog/Search, gợi ý SQL và lọc cùng biến thể, bán chạy chung quy tắc báo cáo, gallery HTTPS/migration002 và QR disabled. 21/21 tiêu chí,349/349 ca runner,22 Chrome con và12 ca live chỉ đọc đạt. Đã cập nhật website tại http://localhost:65377 và publish Admin có trường ảnh bổ sung. Ảnh chưa xác minh giữ fallback; không ghi đè custom URL. Xem WEB_DISCOVERY_REPORT.md. QR ngân hàng/webhook/đối soát chưa triển khai theo yêu cầu; QA tổng thể vẫn còn NOT RUN/BLOCKED.

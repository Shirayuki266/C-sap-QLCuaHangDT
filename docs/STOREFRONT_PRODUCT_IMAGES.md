# Ảnh đại diện sản phẩm trên Web

Theo yêu cầu chỉnh giao diện, Web dùng một ảnh sản phẩm được chọn riêng cho mỗi model trong danh mục demo hiện tại: nền sáng, máy rõ và không có dòng chú thích nguồn bên trong ảnh. Nguồn chủ yếu là hãng; một số ảnh lấy từ trang bán lẻ khi ảnh hãng không phù hợp hoặc trang cũ không còn hoạt động.

Danh sách đầy đủ 50 model, URL trang nguồn/ảnh, SHA256 của dữ liệu tải về, kích thước và SHA256 của SVG được lưu tại [sources.json](../src/PhoneStore.Web/wwwroot/images/storefront/sources.json). Ảnh thuộc chủ sở hữu nguồn; tệp này không khẳng định giấy phép Creative Commons hoặc sự cho phép độc lập cho bộ ảnh mới.

`StorefrontProductImages.Resolve` đối chiếu đồng thời tên model và đường dẫn ảnh demo trước khi dùng ảnh Web. Ảnh tải lên riêng, model khác và ảnh bị thiếu giữ nguyên cách hiển thị/fallback hiện có. Không thay ảnh trong CSDL, API, Admin hoặc công cụ seed.

Trang chủ, danh sách và chi tiết cùng dùng ảnh đã chọn. Ba đường dẫn ảnh demo của một model quy về một ảnh đại diện duy nhất và được loại trùng trên trang chi tiết; không giả làm ba góc chụp khác nhau. Bản raster tải về được giữ nguyên trong SVG, chỉ căn vị trí/kích thước và bỏ khoảng trắng viền bằng hình học SVG. Không tự vẽ hoặc tạo lại chi tiết thiết bị.

Bộ ảnh LC licensed và sổ nguồn demo cũ vẫn giữ nguyên cho dữ liệu demo. Bộ ảnh Web này được kiểm tra riêng, không dùng kết quả kiểm tra giấy phép của bộ LC để mô tả quyền sử dụng ảnh mới. Các tag/release đã phát hành không thay đổi.

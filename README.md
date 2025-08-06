B1: Clone project hoặc giải nén file có sẵn
B2: Vào file appsetting.json -> tìm chuỗi SQLServerIdentityConnection, thay Server= tên server database trên máy
B3: Mở Package Manager Console, gõ lệnh: update-database để tự động tạo database vào máy
B4: Run project lần đầu để seeding data vào database
//Các tài khoản đăng nhập xem trong file SeedData.cs (nằm trong thư mục Models). Trong database không xem được mật khẩu vì đã mã hóa
***Các chức năng:
1. Đăng ký: lỗi popup trên giao diện không tắt được, chưa bật ràng buộc điều kiện các thuộc tính
2. Đăng nhập: lỗi popup như đăng ký
3. Đặt phòng: chọn ngày giờ để hiện ra các phòng trống -> đặt cọc qua Momo để hoàn tất
4. Nhận phòng: yêu cầu đăng nhập vào dashboard với tài khoản có role admin hoặc receptionist -> chọn xem danh sách đặt phòng -> bấm lập phiếu nhận phòng
5. Trả phòng: vào danh sách phiếu nhận phòng -> chọn phiếu -> bấm trả phòng
6. Thanh toán: chọn hình thức thanh toán -> Tiền mặt hoặc Momo
7. Thêm phòng

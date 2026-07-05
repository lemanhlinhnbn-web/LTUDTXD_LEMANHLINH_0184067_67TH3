# Phạm vi tính toán kỹ thuật

Phần mềm kiểm tra dầm thép chữ I theo các nhóm điều kiện:

- bền chịu uốn;
- bền chịu cắt;
- tương tác uốn và cắt;
- ổn định tổng thể uốn xoắn ngang;
- ổn định cục bộ bản cánh nén;
- ổn định cục bộ bản bụng.

## Quy ước

- Kích thước tiết diện dùng `mm`.
- Cường độ và mô đun đàn hồi dùng `MPa`.
- Mô men ETABS dùng `kN.m`.
- Lực cắt ETABS dùng `kN`.
- Nội lực dầm đọc theo `M3` và `V2` trong hệ trục địa phương của phần tử Frame.
- Kết quả ETABS được bao từ các tổ hợp tải; nếu mô hình không có tổ hợp, chương trình xét các load case có kết quả.

## Hệ số ổn định tổng thể φb

`φb` phụ thuộc sơ đồ tải, biểu đồ mô men, chiều dài không giằng, liên kết và cấu tạo giữ cánh nén. Vì việc xác định đầy đủ cần các bảng/công thức tương ứng trong TCVN 5575:2012, phiên bản hiện tại yêu cầu người thiết kế nhập `φb` đã xác định cho trường hợp đang xét. Phần mềm kiểm tra:

`M / (φb · Ry · Wx · γc) ≤ 1`.

Không sử dụng giá trị mặc định cho hồ sơ thiết kế thực tế nếu chưa đối chiếu tiêu chuẩn.

## Giới hạn

Phiên bản hiện tại áp dụng cho tiết diện chữ I đối xứng, không xét lỗ giảm yếu, thay đổi tiết diện, dầm không đối xứng hoặc làm việc uốn hai phương. Kết quả là công cụ hỗ trợ và phải được kỹ sư chịu trách nhiệm kiểm tra trước khi sử dụng.

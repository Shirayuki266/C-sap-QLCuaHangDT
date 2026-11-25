namespace PhoneStore.Core.Commerce;
// [UC09.01] DTO chứng từ hóa đơn bán hàng nội bộ sinh từ OrderView đã chốt.
public sealed record SalesInvoice(string Title,string Notice,OrderView Order);

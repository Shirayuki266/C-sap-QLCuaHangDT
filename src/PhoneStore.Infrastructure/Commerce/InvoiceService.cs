using PhoneStore.Core.Commerce;
namespace PhoneStore.Infrastructure.Commerce;
public sealed class InvoiceService(BusinessKernel kernel)
{
    public async Task<SalesInvoice> Read(Actor actor,long id)
    {
        var order=await new OrderQueryService(kernel).Read(actor,id,true);
        if(order.Status is not ("DA_XAC_NHAN" or "DANG_GIAO" or "HOAN_TAT"))throw Rules.Error("INVALID_STATE","Chỉ lập chứng từ bán hàng cho đơn đã xác nhận, đang giao hoặc hoàn tất.");
        return new("HÓA ĐƠN BÁN HÀNG","Chứng từ bán hàng nội bộ; không phải hóa đơn điện tử thuế.",order);
    }
}

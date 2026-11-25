using System.Text.Json.Serialization;
namespace PhoneStore.Core.Commerce;
// [UC02.01, UC06.04] DTO hãng điện thoại (HANG) và danh mục sản phẩm (DANH_MUC).
public sealed record LookupInput(string? Name, [property: JsonRequired] bool Active = true);
public sealed record LookupView(int Id, string Name, bool Active)
{
    public LookupView() : this(0, "", false) { }
}

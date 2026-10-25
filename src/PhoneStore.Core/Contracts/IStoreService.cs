namespace PhoneStore.Core.Contracts;
public interface IStoreService
{
    Task<Page<CustomerDto>> Customers(long actor,string? query=null,int page=1);
    Task<bool> SaveCustomer(long actor,long id,CustomerInput input);
    Task<List<SupplierDto>> Suppliers(long actor,string? query=null);
    Task<long> SaveSupplier(long actor,long id,SupplierInput input);
    Task<List<StockDto>> Stock(long actor,string? query=null,int? brandId=null,bool low=false);
    Task<bool> AdjustStock(long actor,long variantId,AdjustmentInput input);
    Task<Page<MovementDto>> Movements(long actor,long? variantId=null,int page=1);
    Task<Page<PurchaseDto>> Purchases(long actor,string? query=null,string? status=null,int page=1);
    Task<long> SavePurchase(long actor,long id,PurchaseInput input);
    Task<bool> ConfirmPurchase(long actor,long id,TransitionRequest input,bool cancel=false);
    Task<Page<ProductDto>> Catalog(CatalogFilter f,bool internalView=false,long actor=0);
    Task<List<ProductDto>> BestSellers();
    Task<List<SearchSuggestion>> Suggestions(string? query);
    Task<List<string>> Colors();
    Task<ProductDto> ProductDetail(long id,bool internalView=false,long actor=0);
    Task<List<LookupDto>> Lookups(string kind,bool all=false,long actor=0);
    Task<int> SaveLookup(long actor,string kind,int id,LookupRequest input);
    Task<long> SaveProduct(long actor,long id,ProductInput input);
    Task<bool> SetCart(long customer,CartInput input,bool add=false);
    Task<bool> MergeCart(long customer,List<CartInput> inputs);
    Task<CartDto> PreviewCart(List<CartInput> inputs);
    Task<OrderDto> GetOrder(long actor,long id,bool admin=false);
    Task<Page<OrderDto>> GetOrders(long actor,bool admin=false,string? query=null,string? status=null,DateOnly? from=null,DateOnly? to=null,long? customerId=null,int page=1);
    Task<long> PlaceOrder(long actor,CheckoutRequest input);
    Task<bool> Transition(long actor,long id,string action,TransitionRequest input,bool admin);
    Task<ReportResult> Report(long actor,string kind,DateOnly? from,DateOnly? to);
    Task<UserDto> GetUser(long id);
    Task<bool> Logout(long id,Guid session);
    Task<bool> UpdateProfile(long id,ProfileRequest input);
    Task<CartDto> GetCart(long customer);
    Task<PurchaseDto> GetPurchase(long actor,long id);
    Task<long> Register(RegisterRequest input);
    Task<(UserDto User,Guid Session)> Login(LoginRequest input,bool admin=false);
    Task<bool> ChangePassword(long id,PasswordRequest input);
}


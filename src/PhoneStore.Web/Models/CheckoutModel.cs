using System.ComponentModel.DataAnnotations;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Web.Models;
public sealed class CheckoutModel
{
    public Guid RequestKey {get;set;} [Required(ErrorMessage="Vui lòng nhập người nhận.")] public string Recipient {get;set;}=""; [Required(ErrorMessage="Vui lòng nhập số điện thoại.")] public string Phone {get;set;}=""; [Required(ErrorMessage="Vui lòng nhập địa chỉ.")] public string Address {get;set;}="";
    public List<PriceConfirmation> Lines {get;set;}=[]; public CartDto Cart {get;set;}=new([]);
    public string PaymentMethod {get;set;}="COD";
}


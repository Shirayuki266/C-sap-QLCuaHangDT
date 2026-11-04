using Microsoft.AspNetCore.Mvc.ModelBinding;
using PhoneStore.Core;

namespace PhoneStore.Web.Storefront;

public static class FormErrors
{
    // Current services do not return a field-error contract. Never infer one from message text.
    public static void AddSummary(ModelStateDictionary state, BusinessException error) => state.AddModelError(string.Empty, error.Message);
}

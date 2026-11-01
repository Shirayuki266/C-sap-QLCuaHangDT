## verdict

1. Resolved — the sole product-copy finding from finish-review.md. Reopened all eleven synthetic captures and five real-QA captures at their original paths; each remains valid and shows the named intact 1280×800 native form. forms/UI11-customer-profile-saved.png and real-forms/UI11-real-qa-profile.png now say the profile was saved and old-order recipient information is preserved, with no “GET”. forms/customer-orders.png explains that the detail window only displays information and cannot change order state. forms/customer-order-readonly-detail.png explains read-only information and recipient/price preservation at order time. Both supplier-picker captures explain that selection does not create or save a receipt. Shipping message strings in CustomerForms.cs and ContactLists.cs corroborate the visible wording; W3 strings remaining inside QA fixture names/codes are test data, not product planning copy. The revised messages fit without clipping and keep the intended limitations clear.

No visible regression attributable to the copy batch in the recaptures. This verdict scores the single copy finding only; it is not a new whole-surface review or independent API/SQL/session verification. Screenshot success text does not itself prove stored-data or session effects. Existing W4/W5 and W6/accessibility scope limits remain unchanged.

## remaining

Clear for the scored finding.

disposition: ship

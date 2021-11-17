using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace BytexDigital.Blazor.Components.CookieConsent
{
    public partial class CookieConsentCheck : IDisposable
    {
        [Parameter]
        public RenderFragment<CookieConsentCheck> NotAllowed { get; set; }

        [Parameter]
        public RenderFragment Allowed { get; set; }

        [Inject]
        public CookieConsentService CookieConsentService { get; set; }

        [Inject]
        public IOptions<CookieConsentOptions> Options { get; set; }

        [Parameter]
        public string RequiredCategory { get; set; }

        [Parameter]
        public string RequiredService { get; set; }

        public CookieCategory Category { get; private set; }
        public CookieCategoryService Service { get; private set; }
        public bool IsAllowed { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            if (Allowed == null) throw new InvalidOperationException($"The '{nameof(Allowed)}' parameter has to be specified.");
            if (RequiredCategory == null && RequiredService == null) throw new InvalidOperationException($"Either '{nameof(RequiredCategory)}' or '{nameof(RequiredService)}' has to be specified.");

            CookieConsentService.CookiePreferencesChanged += CookieConsentService_CookiePreferencesChanged;

            if (RequiredService != null)
            {
                Category = Options.Value.Categories.FirstOrDefault(x => x.Services.Any(x => x.Identifier == RequiredService));
                Service = Category?.Services.First(x => x.Identifier == RequiredService);
            }
            else if (RequiredCategory != null)
            {
                Category = Options.Value.Categories.FirstOrDefault(x => x.Identifier == RequiredCategory);
            }

            if (Category == null) throw new Exception($"The required service or category '{RequiredService ?? RequiredCategory}' was not configured.");

            await EvaluateStateAsync();
        }

        public async Task AcceptRequiredAsync()
        {
            await CookieConsentService.AllowCategoryAsync(Category.Identifier);
        }

        private async void CookieConsentService_CookiePreferencesChanged(object sender, CookiePreferences e)
        {
            await InvokeAsync(async () => await EvaluateStateAsync(e));
        }

        private async Task EvaluateStateAsync(CookiePreferences preferences = null)
        {
            preferences ??= await CookieConsentService.GetPreferencesAsync();

            IsAllowed = preferences.AllowedCategories.Contains(RequiredCategory);

            StateHasChanged();
        }

        public void Dispose()
        {
            CookieConsentService.CookiePreferencesChanged -= CookieConsentService_CookiePreferencesChanged;
        }
    }
}
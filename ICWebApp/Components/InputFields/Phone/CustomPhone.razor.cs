using ICWebApp.Application.Interface.Provider;
using IntlTelInputBlazor;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.InputFields.Phone
{
    public partial class CustomPhone
    {
        [Inject] IMETAProvider MetaProvider { get; set; }
        [Parameter] public string Value { get; set; }
        [Parameter] public EventCallback<string> ValueChanged { get; set; }
        private IntlTel _phoneTel = new IntlTel();
        private List<string> PreferredCountries = new List<string>();
        private List<string> OnlyCountries = new List<string>();
        private Dictionary<String, string> LocalizedCountries = new Dictionary<string, string>();
        private IntlTel Phone
        {
            get
            {
                if(Value == null)
                {
                    Value = "";
                }

                _phoneTel.Number = Value.Trim();

                return _phoneTel;
            }
            set
            {
                _phoneTel = value;
                Value = _phoneTel.Number.Trim();
                ValueChanged.InvokeAsync(_phoneTel.Number.Trim());
            }
        }
        protected override async Task OnInitializedAsync()
        {
            var MetaList = await MetaProvider.GetPhonePrefixes();

            foreach(var meta in MetaList.OrderBy(p => p.Code))
            {
                LocalizedCountries.Add(meta.Code, meta.Name);

                if (meta.Preferred == true)
                {
                    PreferredCountries.Add(meta.Code);
                }
                
                OnlyCountries.Add(meta.Code);                
            }

            StateHasChanged();
            await base.OnInitializedAsync();
        }
    }
}

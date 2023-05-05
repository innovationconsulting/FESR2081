using ICWebApp.Application.Interface.Provider;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.FormRenderer.Components
{
    public partial class ListComponent
    {
        [Inject] public ITEXTProvider TextProvider { get; set; }
        [Parameter] public FORM_Application_Field_Data Field { get; set; }
        [Parameter] public EventCallback<FORM_Application_Field_Data> OnValueChanged {get;set;}
        [Parameter] public bool ReadOnly { get; set; } = false;
        
        protected override void OnInitialized()
        {
            if(Field.FORM_Application_Field_SubData == null || Field.FORM_Application_Field_SubData.Count() == 0)
            {
                Field.FORM_Application_Field_SubData = new List<FORM_Application_Field_SubData>();
                Field.FORM_Application_Field_SubData.Add(
                new FORM_Application_Field_SubData()
                {
                    ID = Guid.NewGuid(),
                    FORM_Application_Field_Data_ID = Field.ID,
                    Value = "0"
                });
            }

            base.OnInitialized();
        }
        private void OnRemove(FORM_Application_Field_SubData item)
        {
            Field.FORM_Application_Field_SubData.Remove(item);
            ValueChanged();
            StateHasChanged();
        }
        private void OnAdd()
        {
            var item = new FORM_Application_Field_SubData();
            
            item.ID = Guid.NewGuid();
            item.FORM_Application_Field_Data_ID = Field.ID;
            item.Value = "0";

            Field.FORM_Application_Field_SubData.Add(item);

            StateHasChanged();
        }

        private async void ValueChanged()
        {
            decimal sum = 0;

            foreach(var subItem in Field.FORM_Application_Field_SubData)
            {
                if(!string.IsNullOrEmpty(subItem.Value))
                {
                    sum += decimal.Parse(subItem.Value);
                }
            }

            Field.Value = sum.ToString();
            await OnValueChanged.InvokeAsync(Field);
            StateHasChanged();
        }
    }
}

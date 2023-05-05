using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Tasks.Bucket
{
    public partial class Edit
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }

        [Parameter] public TASK_Bucket Bucket { get; set; }
        [Parameter] public List<TASK_Bucket_Extended> BucketExtended { get; set; }
        [Parameter] public EventCallback<(TASK_Bucket, List<TASK_Bucket_Extended>, List<V_TASK_Context>)> Saved { get; set; }
        [Parameter] public EventCallback Cancelled { get; set; }
        [Parameter] public bool IsEdit { get; set; }
        [Parameter] public List<V_TASK_Context> ContextList { get; set; }
        private List<V_TASK_Context> _additionalContexts = new List<V_TASK_Context>();
        private List<LANG_Languages>? Languages { get; set; }
        private Guid? CurrentLanguage { get; set; }
        private bool Italian
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == Guid.Parse("e450421a-baff-493e-a390-71b49be6485f"))
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = Guid.Parse("e450421a-baff-493e-a390-71b49be6485f");
                    StateHasChanged();
                }
            }
        }
        private bool German
        {
            get
            {
                if (CurrentLanguage != null && CurrentLanguage.Value == Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075"))
                {
                    return true;
                }

                return false;
            }
            set
            {
                if (CurrentLanguage != null && value == true)
                {
                    CurrentLanguage = Guid.Parse("b97f0849-fa25-4cd0-8c7b-43f90fbe4075");
                    StateHasChanged();
                }
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            Languages = await LangProvider.GetAll();

            if (Languages != null)
            {
                CurrentLanguage = Languages.FirstOrDefault().ID;
            }

            StateHasChanged();
            await base.OnParametersSetAsync();
        }
        private async void ReturnToPreviousPage()
        {
            await Cancelled.InvokeAsync();
            StateHasChanged();
        }
        private async void SaveForm()
        {
            await TaskService.SetBucket(Bucket, BucketExtended);

            await Saved.InvokeAsync((Bucket, BucketExtended, _additionalContexts));
            StateHasChanged();
        }
        private void ContextCheckBoxValueChanged(V_TASK_Context context, bool value)
        {
            if (value && _additionalContexts.All(e => e.ID != context.ID))
            {
                _additionalContexts.Add(context);
            }
            else
            {
                _additionalContexts = _additionalContexts.Where(e => e.ID != context.ID).ToList();
            }
        }
    }
}

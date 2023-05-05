using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Tasks.Eskalation
{
    public partial class Control
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }

        [Parameter] public Guid TaskID { get; set; }
        [Parameter] public List<TASK_Task_Eskalation> List { get; set; }
        [Parameter] public EventCallback<TASK_Task_Eskalation> ItemAdded { get; set; }
        [Parameter] public EventCallback<TASK_Task_Eskalation> ItemRemoved { get; set; }
        private bool EditEskalationVisibility { get; set; } = false;
        private TASK_Task_Eskalation? Eskalation;

        protected override void OnInitialized()
        {
            StateHasChanged();
            base.OnInitialized();
        }
        private void AddEskalation()
        {
            Eskalation = new TASK_Task_Eskalation()
            {
                ID = Guid.NewGuid(),
                TASK_Task_ID = TaskID,
                TASK_Task_Eskalation_Responsible = new List<TASK_Task_Eskalation_Responsible>()
            };

            EditEskalationVisibility = true;

            StateHasChanged();
        }
        private async Task<bool> SaveEskalation()
        {
            if (Eskalation != null)
            {
                if(List.Select(p => p.ID).Contains(Eskalation.ID))
                {
                    var ListItem = List.FirstOrDefault(p => p.ID == Eskalation.ID);

                    if(ListItem != null)
                    {
                        ListItem = Eskalation;
                    }
                }
                else
                {
                    List.Add(Eskalation);
                }
                EditEskalationVisibility = false;
                Eskalation = null;
                StateHasChanged();

                await ItemAdded.InvokeAsync();
            }
            else
            {
                HideEskalation();
            }

            return true;
        }
        private void EditEskalation(TASK_Task_Eskalation ItemToEdit)
        {
            Eskalation = ItemToEdit;

            EditEskalationVisibility = true;

            StateHasChanged();
        }
        private void HideEskalation()
        {
            Eskalation = null;
            EditEskalationVisibility = false;
            StateHasChanged();
        }
        private async Task<bool> ResponsibleRemovedEvent(TASK_Task_Eskalation_Responsible Item)
        {
            if (Eskalation != null)
            {
                if (Eskalation.TASK_Task_Eskalation_Responsible == null)
                {
                    Eskalation.TASK_Task_Eskalation_Responsible = new List<TASK_Task_Eskalation_Responsible>();
                }

                Eskalation.TASK_Task_Eskalation_Responsible.Remove(Item);

                await TaskProvider.RemoveTaskEskalationResponsible(Item.ID);
            }

            StateHasChanged();

            return true;
        }
        private async Task<bool> ResponsibleAddEvent(TASK_Task_Eskalation_Responsible Item)
        {
            if (Eskalation != null)
            {
                if (Eskalation.TASK_Task_Eskalation_Responsible == null)
                {
                    Eskalation.TASK_Task_Eskalation_Responsible = new List<TASK_Task_Eskalation_Responsible>();
                }

                Eskalation.TASK_Task_Eskalation_Responsible.Add(Item);
            }
            StateHasChanged();

            return true;
        }
        private async void RemoveItem(TASK_Task_Eskalation Item)
        {
            var tagToRemove = List.FirstOrDefault(p => p.ID == Item.ID);

            if (tagToRemove != null)
            {
                List.Remove(tagToRemove);
            }

            await ItemRemoved.InvokeAsync(tagToRemove);
            StateHasChanged();
        }
    }
}

using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using ICWebApp.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Components.Tasks.Comments
{
    public partial class Control
    {
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TASKProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IAUTHProvider AUTHProvider { get; set; }

        [Parameter] public Guid TaskID { get; set; }
        [Parameter] public List<TASK_Task_Comment> List { get; set; }
        [Parameter] public EventCallback<TASK_Task_Comment> ItemAdded { get; set; }
        private MessageInput Input = new MessageInput();

        protected override void OnInitialized()
        {
            StateHasChanged();
            base.OnInitialized();
        }
        private async void SendMessage()
        {
            if (!string.IsNullOrEmpty(Input.Text))
            {
                var item = new TASK_Task_Comment()
                {
                    ID = Guid.NewGuid(),
                    TASK_Task_ID = TaskID,
                    AUTH_Users_ID = SessionWrapper.CurrentUser.ID,
                    SendAt = DateTime.Now,
                    Message = Input.Text
                };

                Input.Text = null;

                List.Add(item);

                await ItemAdded.InvokeAsync(item);
                StateHasChanged();
            }
        }
    }
}

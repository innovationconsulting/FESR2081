using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;

namespace ICWebApp.Pages.Tasks.Admin
{
    public partial class Dashboard
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITASKService TaskService { get; set; }
        [Inject] ITASKProvider TaskProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        [Parameter] public string? ContextID { get; set; }

        private List<V_TASK_Context?> ContextList { get; set; }
        private V_TASK_Context? CurrentContext { get; set; }
        private List<V_TASK_Status?> StatusList { get; set; }
        private List<V_TASK_Tag?> TagList { get; set; }
        private List<V_TASK_Priority?> PriorityList { get; set; }
        private List<V_TASK_Bucket?> BucketList { get; set; }
        private int CurrentTab { get; set; } = 0;
        private bool IsBusy = true;
        private bool ShowStatusWindow = false;
        private bool StatusWindowEdit = false;
        private TASK_Status? Status { get; set; }
        private List<TASK_Status_Extended>? StatusExtended {get; set; }
        private bool ShowPriorityWindow = false;
        private bool PrioWindowEdit = false;
        private TASK_Priority? Priority { get; set; }
        private List<TASK_Priority_Extended>? PriorityExtended { get; set; }
        private bool ShowBucketWindow = false;
        private bool BucketWindowEdit = false;
        private TASK_Bucket? Bucket { get; set; }
        private List<TASK_Bucket_Extended>? BucketExtended { get; set; }
        private bool ShowTagWindow = false;
        private bool TagWindowEdit = false;
        private TASK_Tag? Tag { get; set; }
        private List<TASK_Tag_Extended>? TagExtended { get; set; }
        private bool RefreshIconsVisible = true;

        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.Get( "MAINMENU_BUCKETS_TASK_SETTINGS_TITLE");

            if (SessionWrapper.AUTH_Municipality_ID != null)
            {
                ContextList = (await TaskProvider.GetTaskContextList(LangProvider.GetCurrentLanguageID(), SessionWrapper.AUTH_Municipality_ID.Value)).ToList();
            }
            if (ContextList != null)
            {
                if (ContextID != null)
                {
                    CurrentContext = ContextList.FirstOrDefault(p => p.ID == long.Parse(ContextID));
                }
                else
                {
                    CurrentContext = ContextList.FirstOrDefault();
                }
                await ContextChanged();
            }

            IsBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private async void OnContextChange(V_TASK_Context Context)
        {
            CurrentContext = Context;
            await ContextChanged();
        }
        private async Task<bool> ContextChanged()
        {
            IsBusy = true;
            StateHasChanged();

            if (CurrentContext != null)
            {
                TaskService.TASK_Context_ID = CurrentContext.ID;

                StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);
                TagList = await TaskService.GetTagList(TaskService.TASK_Context_ID, false);
                PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, false);
                BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, false);
            }

            IsBusy = false;
            StateHasChanged();

            return true;
        }
        private void OnStepChanged()
        {
            StateHasChanged();
        }
        private async void AddStatus()
        {
            if (CurrentContext != null)
            {
                Status = new TASK_Status();
                Status.ID = Guid.NewGuid();
                Status.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Status.SortOrder = StatusList.Count() + 1;
                Status.CompleteTask = false;
                Status.Default = false;
                Status.DefaultCompleteTask = false;
                Status.Enabled = true;
                Status.TASK_Context_ID = CurrentContext.ID;

                StatusExtended = new List<TASK_Status_Extended>();

                var Languages = await LangProvider.GetAll();

                foreach (var lang in Languages)
                {
                    StatusExtended.Add(new TASK_Status_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Status_ID = Status.ID,
                        LANG_Language_ID = lang.ID
                    });
                }

                StatusWindowEdit = false;
                ShowStatusWindow = true;
                StateHasChanged();
            }
        }
        private async void EditStatus(V_TASK_Status Item)
        {
            Status = await TaskProvider.GetStatus(Item.ID);

            var extended = await TaskProvider.GetStatusExtendedList(Item.ID);

            if (extended != null)
            {
                StatusExtended = extended.ToList();
            }

            StatusWindowEdit = true;
            ShowStatusWindow = true;
            StateHasChanged();
        }
        private async void SaveStatus((TASK_Status status, List<TASK_Status_Extended> statusExtendeds,
            List<V_TASK_Context> additionalContexts) parameters)
        {
            BusyIndicatorService.IsBusy = true;
            ShowStatusWindow = false;
            StateHasChanged();

            var status = parameters.status;
            var statusExtendeds = parameters.statusExtendeds;
            var contexts = parameters.additionalContexts;
            foreach (var context in contexts)
            {
                Status = new TASK_Status();
                Status.ID = Guid.NewGuid();
                Status.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Status.SortOrder = (await TaskService.GetStatusList(context.ID, false)).Count + 1;
                Status.CompleteTask = status.CompleteTask;
                Status.Default = status.Default;
                Status.DefaultCompleteTask = status.DefaultCompleteTask;
                Status.Enabled = status.Enabled;
                Status.TASK_Context_ID = context.ID;
                Status.Icon = status.Icon;

                StatusExtended = new List<TASK_Status_Extended>();

                foreach (var statusExtended in statusExtendeds)
                {
                    StatusExtended.Add(new TASK_Status_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Status_ID = Status.ID,
                        LANG_Language_ID = statusExtended.LANG_Language_ID,
                        Description = statusExtended.Description
                    });
                }

                await TaskService.SetStatus(Status, StatusExtended);
            }

            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            Status = null;
            StatusExtended = null;

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
        private void CloseStatus()
        {
            ShowStatusWindow = false;
            StateHasChanged();

            Status = null;
            StatusExtended = null;

            StateHasChanged();
        }
        private async void DeleteStatus(V_TASK_Status Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                IsBusy = true;
                StateHasChanged();

                await TaskProvider.RemoveStatus(Item.ID);

                StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);
                RefreshIcons();

                IsBusy = false;
                StateHasChanged();
            }
        }
        private async void MoveUpStatus(V_TASK_Status Item)
        {
            if (StatusList != null && StatusList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();

                await ReOrderStatus();

                var startField = await TaskProvider.GetStatus(Item.ID);

                if (startField != null)
                {
                    var newPos = StatusList.FirstOrDefault(p => p.SortOrder == startField.SortOrder - 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetStatus(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder - 1;
                            newPosDB.SortOrder = newPosDB.SortOrder + 1;

                            await TaskProvider.SetStatus(startField);
                            await TaskProvider.SetStatus(newPosDB);
                        }
                    }
                }
            }

            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            IsBusy = false;
            StateHasChanged();
        }
        private async void MoveDownStatus(V_TASK_Status Item)
        {
            if (StatusList != null && StatusList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();
                await ReOrderStatus();

                var startField = await TaskProvider.GetStatus(Item.ID);

                if (startField != null)
                {

                    var newPos = StatusList.FirstOrDefault(p => p.SortOrder == startField.SortOrder + 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetStatus(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder + 1;
                            newPosDB.SortOrder = newPosDB.SortOrder - 1;
                            await TaskProvider.SetStatus(startField);
                            await TaskProvider.SetStatus(newPosDB);
                        }
                    }
                }
            }

            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            IsBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ReOrderStatus()
        {
            int count = 1;

            foreach (var d in StatusList.OrderBy(p => p.SortOrder))
            {
                if (d != null)
                {
                    var field = await TaskProvider.GetStatus(d.ID);

                    if (field != null)
                    {
                        field.SortOrder = count;

                        await TaskProvider.SetStatus(field);
                    }
                }
                count++;
            }

            return true;
        }
        private async void StatusEnabledChanged(V_TASK_Status Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetStatus(Item.ID);

            if(item != null)
            {
                item.Enabled = Item.Enabled;

                await TaskProvider.SetStatus(item);
            }

            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void StatusCompleteTaskChanged(V_TASK_Status Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetStatus(Item.ID);

            if (item != null)
            {
                item.CompleteTask = Item.CompleteTask;

                await TaskProvider.SetStatus(item);
            }

            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void StatusDefaultChanged(V_TASK_Status Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetStatus(Item.ID);

            foreach(var itr in StatusList.ToList())
            {
                if (itr != null)
                {
                    var dbItem = await TaskProvider.GetStatus(itr.ID);

                    if (dbItem != null)
                    {
                        dbItem.Default = false;

                        await TaskProvider.SetStatus(dbItem);
                    }
                }
            }

            if (item != null)
            {
                item.Default = Item.Default;

                await TaskProvider.SetStatus(item);
            }

            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void StatusDefaultCompleteTaskChanged(V_TASK_Status Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetStatus(Item.ID);

            foreach (var itr in StatusList.ToList())
            {
                if (itr != null)
                {
                    var dbItem = await TaskProvider.GetStatus(itr.ID);

                    if (dbItem != null)
                    {
                        dbItem.DefaultCompleteTask = false;

                        await TaskProvider.SetStatus(dbItem);
                    }
                }
            }

            if (item != null)
            {
                item.DefaultCompleteTask = Item.DefaultCompleteTask;

                await TaskProvider.SetStatus(item);
            }

            StatusList = await TaskService.GetStatusList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void AddPriority()
        {
            if (CurrentContext != null)
            {
                Priority = new TASK_Priority();
                Priority.ID = Guid.NewGuid();
                Priority.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Priority.SortOrder = PriorityList.Count() + 1;
                Priority.Default = false;
                Priority.Enabled = true;
                Priority.TASK_Context_ID = CurrentContext.ID;

                PriorityExtended = new List<TASK_Priority_Extended>();

                var Languages = await LangProvider.GetAll();

                foreach (var lang in Languages)
                {
                    PriorityExtended.Add(new TASK_Priority_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Priority_ID = Priority.ID,
                        LANG_Language_ID = lang.ID
                    });
                }

                PrioWindowEdit = false;
                ShowPriorityWindow = true;
                StateHasChanged();
            }
        }
        private async void EditPriority(V_TASK_Priority Item)
        {
            Priority = await TaskProvider.GetPriority(Item.ID);

            var extended = await TaskProvider.GetPriorityExtendedList(Item.ID);

            if (extended != null)
            {
                PriorityExtended = extended.ToList();
            }

            PrioWindowEdit = true;
            ShowPriorityWindow = true;
            StateHasChanged();
        }
        private async void SavePriority((TASK_Priority prio, List<TASK_Priority_Extended> prioExtendeds,
            List<V_TASK_Context> additionalContexts) parameters)
        {
            BusyIndicatorService.IsBusy = true;
            ShowPriorityWindow = false;
            StateHasChanged();

            var prio = parameters.prio;
            var prioExtendeds = parameters.prioExtendeds;
            var contexts = parameters.additionalContexts;
            foreach (var context in contexts)
            {
                Priority = new TASK_Priority();
                Priority.ID = Guid.NewGuid();
                Priority.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Priority.SortOrder = (await TaskService.GetPriorityList(context.ID, false)).Count + 1;
                Priority.Default = prio.Default;
                Priority.Enabled = prio.Enabled;
                Priority.TASK_Context_ID = context.ID;
                Priority.Icon = prio.Icon;

                PriorityExtended = new List<TASK_Priority_Extended>();

                foreach (var prioExtended in prioExtendeds)
                {
                    PriorityExtended.Add(new TASK_Priority_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Priority_ID = Priority.ID,
                        LANG_Language_ID = prioExtended.LANG_Language_ID,
                        Description = prioExtended.Description
                    });
                }

                await TaskService.SetPriority(Priority, PriorityExtended);
            }

            PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            Priority = null;
            PriorityExtended = null;

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
        private void ClosePriority()
        {
            ShowPriorityWindow = false;
            StateHasChanged();

            Priority = null;
            PriorityExtended = null;

            StateHasChanged();
        }
        private async void DeletePriority(V_TASK_Priority Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                IsBusy = true;
                StateHasChanged();

                await TaskProvider.RemovePriority(Item.ID);

                PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, false);
                RefreshIcons();

                IsBusy = false;
                StateHasChanged();
            }
        }
        private async void MoveUpPriority(V_TASK_Priority Item)
        {
            if (PriorityList != null && PriorityList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();

                await ReOrderPriority();

                var startField = await TaskProvider.GetPriority(Item.ID);

                if (startField != null)
                {
                    var newPos = PriorityList.FirstOrDefault(p => p.SortOrder == startField.SortOrder - 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetPriority(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder - 1;
                            newPosDB.SortOrder = newPosDB.SortOrder + 1;

                            await TaskProvider.SetPriority(startField);
                            await TaskProvider.SetPriority(newPosDB);
                        }
                    }
                }
            }

            PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            IsBusy = false;
            StateHasChanged();
        }
        private async void MoveDownPriority(V_TASK_Priority Item)
        {
            if (PriorityList != null && PriorityList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();
                await ReOrderPriority();

                var startField = await TaskProvider.GetPriority(Item.ID);

                if (startField != null)
                {

                    var newPos = PriorityList.FirstOrDefault(p => p.SortOrder == startField.SortOrder + 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetPriority(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder + 1;
                            newPosDB.SortOrder = newPosDB.SortOrder - 1;
                            await TaskProvider.SetPriority(startField);
                            await TaskProvider.SetPriority(newPosDB);
                        }
                    }
                }
            }

            PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            IsBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ReOrderPriority()
        {
            int count = 1;

            foreach (var d in PriorityList.OrderBy(p => p.SortOrder))
            {
                if (d != null)
                {
                    var field = await TaskProvider.GetPriority(d.ID);

                    if (field != null)
                    {
                        field.SortOrder = count;

                        await TaskProvider.SetPriority(field);
                    }
                }
                count++;
            }

            return true;
        }
        private async void PriorityEnabledChanged(V_TASK_Priority Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetPriority(Item.ID);

            if (item != null)
            {
                item.Enabled = Item.Enabled;

                await TaskProvider.SetPriority(item);
            }

            PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void PriorityDefaultChanged(V_TASK_Priority Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetPriority(Item.ID);

            foreach (var itr in PriorityList.ToList())
            {
                if (itr != null)
                {
                    var dbItem = await TaskProvider.GetPriority(itr.ID);

                    if (dbItem != null)
                    {
                        dbItem.Default = false;

                        await TaskProvider.SetPriority(dbItem);
                    }
                }
            }

            if (item != null)
            {
                item.Default = true;

                await TaskProvider.SetPriority(item);
            }

            PriorityList = await TaskService.GetPriorityList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void AddBucket()
        {
            if (CurrentContext != null)
            {
                Bucket = new TASK_Bucket();
                Bucket.ID = Guid.NewGuid();
                Bucket.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Bucket.SortOrder = BucketList.Count() + 1;
                Bucket.Default = false;
                Bucket.Enabled = true;
                Bucket.TASK_Context_ID = CurrentContext.ID;

                BucketExtended = new List<TASK_Bucket_Extended>();

                var Languages = await LangProvider.GetAll();

                foreach (var lang in Languages)
                {
                    BucketExtended.Add(new TASK_Bucket_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Bucket_ID = Bucket.ID,
                        LANG_Language_ID = lang.ID
                    });
                }

                BucketWindowEdit = false;
                ShowBucketWindow = true;
                StateHasChanged();
            }
        }
        private async void EditBucket(V_TASK_Bucket Item)
        {
            Bucket = await TaskProvider.GetBucket(Item.ID);

            var extended = await TaskProvider.GetBucketExtendedList(Item.ID);

            if (extended != null)
            {
                BucketExtended = extended.ToList();
            }

            BucketWindowEdit = true;
            ShowBucketWindow = true;
            StateHasChanged();
        }
        private async void SaveBucket((TASK_Bucket bucket, List<TASK_Bucket_Extended> bucketExtendeds,
            List<V_TASK_Context> additionalContexts) parameters)
        {
            BusyIndicatorService.IsBusy = true;
            ShowBucketWindow = false;
            StateHasChanged();


            var bucket = parameters.bucket;
            var bucketExtendeds = parameters.bucketExtendeds;
            var contexts = parameters.additionalContexts;
            foreach (var context in contexts)
            {
                Bucket = new TASK_Bucket();
                Bucket.ID = Guid.NewGuid();
                Bucket.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Bucket.SortOrder = (await TaskService.GetBucketList(context.ID, false)).Count + 1;
                Bucket.Default = bucket.Default;
                Bucket.Enabled = bucket.Enabled;
                Bucket.TASK_Context_ID = context.ID;
                Bucket.Icon = bucket.Icon;

                BucketExtended = new List<TASK_Bucket_Extended>();

                foreach (var bucketExtended in bucketExtendeds)
                {
                    BucketExtended.Add(new TASK_Bucket_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Bucket_ID = Bucket.ID,
                        LANG_Language_ID = bucketExtended.LANG_Language_ID,
                        Description = bucketExtended.Description
                    });
                }

                await TaskService.SetBucket(Bucket, BucketExtended);

            }

            BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            Bucket = null;
            BucketExtended = null;

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
        private void CloseBucket()
        {
            ShowBucketWindow = false;
            StateHasChanged();

            Bucket = null;
            BucketExtended = null;

            StateHasChanged();
        }
        private async void DeleteBucket(V_TASK_Bucket Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                IsBusy = true;
                StateHasChanged();

                await TaskProvider.RemoveBucket(Item.ID);

                BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, false);
                RefreshIcons();

                IsBusy = false;
                StateHasChanged();
            }
        }
        private async void MoveUpBucket(V_TASK_Bucket Item)
        {
            if (BucketList != null && BucketList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();

                await ReOrderBucket();

                var startField = await TaskProvider.GetBucket(Item.ID);

                if (startField != null)
                {
                    var newPos = BucketList.FirstOrDefault(p => p.SortOrder == startField.SortOrder - 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetBucket(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder - 1;
                            newPosDB.SortOrder = newPosDB.SortOrder + 1;

                            await TaskProvider.SetBucket(startField);
                            await TaskProvider.SetBucket(newPosDB);
                        }
                    }
                }
            }

            BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            IsBusy = false;
            StateHasChanged();
        }
        private async void MoveDownBucket(V_TASK_Bucket Item)
        {
            if (BucketList != null && BucketList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();
                await ReOrderBucket();

                var startField = await TaskProvider.GetBucket(Item.ID);

                if (startField != null)
                {

                    var newPos = BucketList.FirstOrDefault(p => p.SortOrder == startField.SortOrder + 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetBucket(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder + 1;
                            newPosDB.SortOrder = newPosDB.SortOrder - 1;
                            await TaskProvider.SetBucket(startField);
                            await TaskProvider.SetBucket(newPosDB);
                        }
                    }
                }
            }

            BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            IsBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ReOrderBucket()
        {
            int count = 1;

            foreach (var d in BucketList.OrderBy(p => p.SortOrder))
            {
                if (d != null)
                {
                    var field = await TaskProvider.GetBucket(d.ID);

                    if (field != null)
                    {
                        field.SortOrder = count;

                        await TaskProvider.SetBucket(field);
                    }
                }
                count++;
            }

            return true;
        }
        private async void BucketEnabledChanged(V_TASK_Bucket Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetBucket(Item.ID);

            if (item != null)
            {
                item.Enabled = Item.Enabled;

                await TaskProvider.SetBucket(item);
            }

            BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void BucketDefaultChanged(V_TASK_Bucket Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetBucket(Item.ID);

            foreach (var itr in BucketList.ToList())
            {
                if (itr != null)
                {
                    var dbItem = await TaskProvider.GetBucket(itr.ID);

                    if (dbItem != null)
                    {
                        dbItem.Default = false;

                        await TaskProvider.SetBucket(dbItem);
                    }
                }
            }

            if (item != null)
            {
                item.Default = true;

                await TaskProvider.SetBucket(item);
            }

            BucketList = await TaskService.GetBucketList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void AddTag()
        {
            if (CurrentContext != null)
            {
                Tag = new TASK_Tag();
                Tag.ID = Guid.NewGuid();
                Tag.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Tag.SortOrder = TagList.Count() + 1;
                Tag.Enabled = true;
                Tag.TASK_Context_ID = CurrentContext.ID;

                TagExtended = new List<TASK_Tag_Extended>();

                var Languages = await LangProvider.GetAll();

                foreach (var lang in Languages)
                {
                    TagExtended.Add(new TASK_Tag_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Tag_ID = Tag.ID,
                        LANG_Language_ID = lang.ID
                    });
                }
                TagWindowEdit = false;
                ShowTagWindow = true;
                StateHasChanged();
            }
        }
        private async void EditTag(V_TASK_Tag Item)
        {
            Tag = await TaskProvider.GetTag(Item.ID);

            var extended = await TaskProvider.GetTagExtendedList(Item.ID);

            if (extended != null)
            {
                TagExtended = extended.ToList();
            }

            TagWindowEdit = true;
            ShowTagWindow = true;
            StateHasChanged();
        }
        private async void SaveTag((TASK_Tag tag, List<TASK_Tag_Extended> tagExtendeds, List<V_TASK_Context> additionalContexts) parameters)
        {
            BusyIndicatorService.IsBusy = true;
            ShowTagWindow = false;
            StateHasChanged();

            var tag = parameters.tag;
            var tagExtendeds = parameters.tagExtendeds;
            var contexts = parameters.additionalContexts;
            foreach (var context in contexts)
            {
                Tag = new TASK_Tag();
                Tag.ID = Guid.NewGuid();
                Tag.AUTH_Municipality_ID = SessionWrapper.AUTH_Municipality_ID.Value;
                Tag.SortOrder = (await TaskService.GetTagList(context.ID, false)).Count + 1;
                Tag.Enabled = tag.Enabled;
                Tag.TASK_Context_ID = context.ID;
                Tag.Color = tag.Color;

                TagExtended = new List<TASK_Tag_Extended>();
                
                foreach (var tagExtended in tagExtendeds)
                {
                    TagExtended.Add(new TASK_Tag_Extended()
                    {
                        ID = Guid.NewGuid(),
                        TASK_Tag_ID = Tag.ID,
                        LANG_Language_ID = tagExtended.LANG_Language_ID,
                        Description = tagExtended.Description
                    });
                }
                await TaskService.SetTag(Tag, TagExtended); 
            }

            TagList = await TaskService.GetTagList(TaskService.TASK_Context_ID, false);

            Tag = null;
            TagExtended = null;

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();
        }
        private void CloseTag()
        {
            ShowTagWindow = false;
            StateHasChanged();

            Tag = null;
            TagExtended = null;

            StateHasChanged();
        }
        private async void DeleteTag(V_TASK_Tag Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                IsBusy = true;
                StateHasChanged();

                await TaskProvider.RemoveTag(Item.ID);

                TagList = await TaskService.GetTagList(TaskService.TASK_Context_ID, false);

                IsBusy = false;
                StateHasChanged();
            }
        }
        private async void MoveUpTag(V_TASK_Tag Item)
        {
            if (TagList != null && TagList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();

                await ReOrderTag();

                var startField = await TaskProvider.GetTag(Item.ID);

                if (startField != null)
                {
                    var newPos = TagList.FirstOrDefault(p => p.SortOrder == startField.SortOrder - 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetTag(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder - 1;
                            newPosDB.SortOrder = newPosDB.SortOrder + 1;

                            await TaskProvider.SetTag(startField);
                            await TaskProvider.SetTag(newPosDB);
                        }
                    }
                }
            }

            TagList = await TaskService.GetTagList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void MoveDownTag(V_TASK_Tag Item)
        {
            if (TagList != null && TagList.Count() > 0)
            {
                IsBusy = true;
                StateHasChanged();
                await ReOrderTag();

                var startField = await TaskProvider.GetTag(Item.ID);

                if (startField != null)
                {

                    var newPos = TagList.FirstOrDefault(p => p.SortOrder == startField.SortOrder + 1);

                    if (newPos != null)
                    {
                        var newPosDB = await TaskProvider.GetTag(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder + 1;
                            newPosDB.SortOrder = newPosDB.SortOrder - 1;
                            await TaskProvider.SetTag(startField);
                            await TaskProvider.SetTag(newPosDB);
                        }
                    }
                }
            }

            TagList = await TaskService.GetTagList(TaskService.TASK_Context_ID, false);
            RefreshIcons();

            IsBusy = false;
            StateHasChanged();
        }
        private async Task<bool> ReOrderTag()
        {
            int count = 1;

            foreach (var d in TagList.OrderBy(p => p.SortOrder))
            {
                if (d != null)
                {
                    var field = await TaskProvider.GetTag(d.ID);

                    if (field != null)
                    {
                        field.SortOrder = count;

                        await TaskProvider.SetTag(field);
                    }
                }
                count++;
            }

            return true;
        }
        private async void TagEnabledChanged(V_TASK_Tag Item)
        {
            IsBusy = true;
            StateHasChanged();

            var item = await TaskProvider.GetTag(Item.ID);

            if (item != null)
            {
                item.Enabled = Item.Enabled;

                await TaskProvider.SetTag(item);
            }

            TagList = await TaskService.GetTagList(TaskService.TASK_Context_ID, false);

            IsBusy = false;
            StateHasChanged();
        }
        private async void RefreshIcons()
        {
            RefreshIconsVisible = false;
            StateHasChanged();

            await Task.Delay(1);

            RefreshIconsVisible = true;
            StateHasChanged();
        }
    }
}

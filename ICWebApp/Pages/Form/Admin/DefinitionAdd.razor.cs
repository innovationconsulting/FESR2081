using ICWebApp.Application.Helper;
using ICWebApp.Application.Interface.Helper;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Components.Formbuilder;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.Blazor.Components.Editor;

namespace ICWebApp.Pages.Form.Admin
{
    public partial class DefinitionAdd
    {
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] IFORMDefinitionProvider FormDefinitionProvider { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] ILANGProvider LangProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] private IPAYProvider PayProvider { get; set; }
        [Inject] IFormBuilderHelper FormBuilderHelper { get; set; }
        [Parameter] public string ID { get; set; }
        [Parameter] public string AktiveIndex { get; set; }
        [Parameter] public string WizardIndex { get; set; }
        [CascadingParameter] public DialogFactory Dialogs { get; set; }
        private FORM_Definition? Data { get; set; }
        private List<AUTH_Authority> DataAemter { get; set; }
        private List<LANG_Languages>? Languages { get; set; }
        private List<FORM_Definition_Property> Properties { get; set; }
        private List<FORM_Definition_Event> Events { get; set; }
        private List<FORM_Definition_Tasks> Tasks { get; set; }
        private List<FORM_Definition_Deadlines> Deadlines { get; set; }
        private List<FORM_Definition_Upload> Uploads { get; set; }
        private List<FORM_Definition_Ressources> Ressources { get; set; }
        private List<FORM_Definition_Additional_FORM> AdditionalForms { get; set; }
        private List<FORM_Definition_Reminder> Reminder { get; set; }
        private List<FORM_Definition_Signings> Signings { get; set; }
        private List<FORM_Definition_Payment_Position> PaymentPositions {get;set;}
        private List<V_FORM_Definition_Municipal_Field> MunFields { get; set; }
        private List<PAY_PagoPa_Identifier> PagoPaIdentifiers { get; set; }
        private PAY_PagoPa_Identifier? SelectedIdent { get; set; } = null;
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
        private Guid? CurrentLanguage { get; set; }
        private int AktiveTabIndex { get; set; } = 0;
        private int AktiveFormTabIndex { get; set; } = 0;
        private int AktiveMunicipalTasksTabIndex { get; set; } = 0;
        private bool IsTabBusy { get; set; } = true;
        private List<IEditorTool> Tools { get; set; } =
        new List<IEditorTool>()
        {
            new EditorButtonGroup(new Bold(), new Italic(), new Underline()),
            new EditorButtonGroup(new AlignLeft(), new AlignCenter(), new AlignRight()),
            new UnorderedList(),
            new EditorButtonGroup(new CreateLink(), new Unlink()),
            new InsertTable(),
            new DeleteTable(),
            new EditorButtonGroup(new AddRowBefore(), new AddRowAfter(), new DeleteRow(), new MergeCells(), new SplitCell())
        };
        public List<string> RemoveAttributes { get; set; } = new List<string>() { "data-id" };
        public List<string> StripTags { get; set; } = new List<string>() { "font", "h1", "h2", "h2", "h3", "h4", "h5", "h6", "img", "pre", "p", "span", "b", "strong", "ins", "del", "i","em", "small", "mark", "sup", "div" };
        private int CurrentTab { get; set; } = 0;
        private bool ShowFormEditorPreview = false;
        private Container? FormBuilderContainer;
        private FORM_Application? PreviewApplication;
        private bool IsFormRendererBusy = false;
        private List<V_FORM_Definition_Template>? Templates { get; set; }
        private bool SaveChangesDialogVisibility = false;

        protected override async Task OnInitializedAsync()
        {
            Languages = await LangProvider.GetAll();

            if (ID == "New")
            {
                Data = new FORM_Definition();
                Data.ID = Guid.NewGuid();

                Data.HasTasks = true;
                Data.HasChat = true;
                Data.CreatedAt = DateTime.Now;
                Data.AUTH_Municipality_ID = SessionWrapper.CurrentUser.AUTH_Municipality_ID;

                Data.Bollo_Amount = 1;
                Data.FORM_Defintion_Bollo_Type_ID = Guid.Parse("52f95993-5204-400c-af9a-09d36b4ffb53");
                
                if (NavManager.Uri.Contains("Application"))
                {
                    Data.FORM_Definition_Category_ID = Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5");
                    Data.ShowPrivacy = true;
                }
                else if (NavManager.Uri.Contains("Mantainance"))
                {
                    Data.FORM_Definition_Category_ID = FORMCategories.Maintenance;
                }

                if (Data != null && Data.FORM_Definition_Category_ID == Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5"))  //APPLICATION
                {
                    SessionWrapper.PageTitle = TextProvider.Get("BACKEND_FORM_DEFINITION_TITLE_ADD");
                    Data.ShowPrivacy = false;
                }
                else
                {
                    SessionWrapper.PageTitle = TextProvider.Get("BACKEND_FORM_DEFINITION_MANTAINANCE_TITLE_ADD");
                }

                await GetData();

                if (DataAemter != null && DataAemter.Count() > 0)
                {
                    Data.AUTH_Authority_ID = DataAemter.FirstOrDefault().ID;
                    AuthorityChanged();
                }
                
                await FormDefinitionProvider.SetDefinition(Data);

                if (Languages != null)
                {
                    foreach (var l in Languages)
                    {
                        if (Data.FORM_Definition_Extended == null)
                        {
                            Data.FORM_Definition_Extended = new List<FORM_Definition_Extended>();
                        }

                        if (Data.FORM_Definition_Extended.FirstOrDefault(p => p.LANG_Language_ID == l.ID) == null)
                        {
                            var dataE = new FORM_Definition_Extended()
                            {
                                ID = Guid.NewGuid(),
                                FORM_Definition_ID = Data.ID,
                                LANG_Language_ID = l.ID
                            };

                            await FormDefinitionProvider.SetDefinitionExtended(dataE);
                            Data.FORM_Definition_Extended.Add(dataE);
                        }
                    }
                }

                Data = await FormDefinitionProvider.GetDefinition(Data.ID);

                Properties = new List<FORM_Definition_Property>();
                Events = new List<FORM_Definition_Event>();
                Tasks = new List<FORM_Definition_Tasks>();
                Deadlines = new List<FORM_Definition_Deadlines>();
                Uploads = new List<FORM_Definition_Upload>();
                Ressources = new List<FORM_Definition_Ressources>();
                AdditionalForms = new List<FORM_Definition_Additional_FORM>();
                Reminder = new List<FORM_Definition_Reminder>();
                Signings = new List<FORM_Definition_Signings>();
                PaymentPositions = new List<FORM_Definition_Payment_Position>();
                MunFields = new List<V_FORM_Definition_Municipal_Field>();
            }
            else
            {
                Data = await FormDefinitionProvider.GetDefinition(Guid.Parse(ID));

                if (Data == null)
                {
                    ReturnToPreviousPage();
                }

                if (Data != null && Data.FORM_Definition_Category_ID == Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5"))  //APPLICATION
                {
                    SessionWrapper.PageTitle = TextProvider.Get("BACKEND_FORM_DEFINITION_TITLE_EDIT");
                }
                else
                {
                    SessionWrapper.PageTitle = TextProvider.Get("BACKEND_FORM_DEFINITION_MANTAINANCE_TITLE_EDIT");
                }

                Properties = await FormDefinitionProvider.GetDefinitionPropertyList(Data.ID);
                Events = await FormDefinitionProvider.GetDefinitionEventsList(Data.ID);
                Tasks = await FormDefinitionProvider.GetDefinitionTaskList(Data.ID);
                Deadlines = await FormDefinitionProvider.GetDefinitionDeadlinesList(Data.ID);
                Uploads = await FormDefinitionProvider.GetDefinitionUploadList(Data.ID);
                Ressources = await FormDefinitionProvider.GetDefinitionRessourceList(Data.ID);
                AdditionalForms = await FormDefinitionProvider.GetDefinitionAdditionalFORMList(Data.ID);
                Reminder = await FormDefinitionProvider.GetDefinitionReminderList(Data.ID);
                Signings = await FormDefinitionProvider.GetDefinitionSigningList(Data.ID);
                PaymentPositions = await FormDefinitionProvider.GetDefinitionPaymentList(Data.ID);
                MunFields = await FormDefinitionProvider.GetDefinitionMunFieldList(Data.ID, LangProvider.GetCurrentLanguageID());

                await GetData();
            }

            if(WizardIndex != null)
            {
                CurrentTab = int.Parse(WizardIndex);
                
                if (AktiveIndex != null)
                {
                    if(CurrentTab == 1)
                    {
                        AktiveTabIndex = int.Parse(AktiveIndex);
                    }
                    else if (CurrentTab == 2)
                    {
                        AktiveFormTabIndex = int.Parse(AktiveIndex);
                    }
                    else if (CurrentTab == 3)
                    {
                        AktiveMunicipalTasksTabIndex = int.Parse(AktiveIndex);
                    }
                }
            }

            if (Languages != null)
            {
                CurrentLanguage = Languages.FirstOrDefault().ID;
            }

            if (Data != null && Data.FORM_Name != null && Data.FORM_Definition_Extended.Count() > 0 && CurrentLanguage != null)
            {
                var item = Data.FORM_Definition_Extended.FirstOrDefault(p => p.LANG_Language_ID == CurrentLanguage);

                if (item != null) 
                {
                    SessionWrapper.PageSubTitle = item.Name;
                }
            }

            if (Data != null)
            {
                PreviewApplication = new FORM_Application() { FORM_Definition_ID = Data.ID };
            }

            if (CurrentLanguage != null)
            {
                Templates = await FormDefinitionProvider.GetDefinitionTemplates(CurrentLanguage.Value);
            }

            if(Data.FORM_Definition_Category_ID == FORMCategories.Applications)
            {
                FormBuilderHelper.OnCurrentLanguageChangedSingleNotification += FormBuilderHelper_OnCurrentLanguageChangedSingleNotification;
            }

            PagoPaIdentifiers = await PayProvider.GetAllPagoPaApplicaitonsIdentifiers(SessionWrapper.AUTH_Municipality_ID);
            
            IsTabBusy = false;
            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        private void FormBuilderHelper_OnCurrentLanguageChangedSingleNotification()
        {
            CurrentLanguage = FormBuilderHelper.CurrentLanguage;
            StateHasChanged();
        }
        private async Task<bool> GetData()
        {
            if (SessionWrapper.CurrentUser != null && SessionWrapper.CurrentUser.AUTH_Municipality_ID != null)
            {
                if (Data != null && Data.FORM_Definition_Category_ID == Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5"))
                {
                    DataAemter = await AuthProvider.GetAuthorityList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, true);
                }
                else
                {
                    DataAemter = await AuthProvider.GetAuthorityList(SessionWrapper.CurrentUser.AUTH_Municipality_ID.Value, false);
                }
            }

            return true;
        }
        private void AuthorityChanged()
        {
            //Change when cache create cache implemented
            if (Data != null && Data.AUTH_Authority_ID != null)
            {
                var authority = DataAemter.Where(e => e.ID == Data.AUTH_Authority_ID).FirstOrDefault();

                if (authority != null && authority.IdentificationLetters != null && authority.NextFormIndex != null)
                {
                    Data.FormCode = authority.IdentificationLetters + authority.NextFormIndex.Value.ToString("D2");
                }
                else
                {
                    Data.FormCode = null;
                }

                if(authority != null)
                {
                    Data.AUTH_Authority = authority;
                }
            } 
            else if (Data != null)
            {
                Data.FormCode = null;
            }
        }        
        private void SignChanged(string Button)
        {
            if (Data != null)
            {
                if (Data.HasSigning && Button == "Single")
                {
                    Data.HasMultiSigning = false;
                }
                else if (Data.HasMultiSigning && Button == "Multi")
                {
                    Data.HasSigning = false;
                }
                StateHasChanged();
            }
        }
        private void FeeChanged(string Button)
        {
            if (Data != null)
            {
                if (Data.HasPayment && Button == "Fixed")
                {
                    Data.HasFlexiblePrice = false;
                }
                else if (Data.HasFlexiblePrice && Button == "Flexible")
                {
                    Data.HasPayment = false;
                }
                StateHasChanged();
            }
        }
        private void OnFormStepChanged()
        {
        }
        private async void ReturnToPreviousPage()
        {
            if (ID == "New" && Data != null)
            {
                await FormDefinitionProvider.RemoveDefinition(Data.ID, true);
            }

            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            
            if(Data != null && Data.FORM_Definition_Category_ID == Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5"))  //APPLICATION
            {
                NavManager.NavigateTo("/Form/Definition/Application/1");
            }
            else
            {
                NavManager.NavigateTo("/Form/Definition/Mantainance/2");
            }
        }
        private async Task SaveForm(bool ReturnBack = true)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();
            
            foreach (var e in Data.FORM_Definition_Extended)
            {
                await FormDefinitionProvider.SetDefinitionExtended(e);
            }

            if (ID == "New")
            {
                await FormDefinitionProvider.SetDefinition(Data, true);
            }
            else
            {
                await FormDefinitionProvider.SetDefinition(Data, false);
            }

            if (ReturnBack)
            {
                if (Data != null && Data.FORM_Definition_Category_ID == Guid.Parse("93efca6b-c191-473d-b49a-4d6e4d2117e5"))  //APPLICATION
                {
                    NavManager.NavigateTo("/Form/Definition/Application/1");
                }
                else
                {
                    NavManager.NavigateTo("/Form/Definition/Mantainance/2");
                }
            }
        }
        private async void AddProperty()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Property/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void EditProperty(FORM_Definition_Property Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Property/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void DeleteProperty(FORM_Definition_Property Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionProperty(Item.ID);
                Properties = await FormDefinitionProvider.GetDefinitionPropertyList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddEvent()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Event/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void EditEvent(FORM_Definition_Event Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Event/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void DeleteEvent(FORM_Definition_Event Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionEvents(Item.ID);
                Events = await FormDefinitionProvider.GetDefinitionEventsList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddTask()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Task/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void EditTask(FORM_Definition_Tasks Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Task/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void DeleteTask(FORM_Definition_Tasks Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionTask(Item.ID);
                Tasks = await FormDefinitionProvider.GetDefinitionTaskList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddDeadline()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Deadline/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void EditDeadline(FORM_Definition_Deadlines Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Deadline/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void DeleteDeadline(FORM_Definition_Deadlines Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionDeadlines(Item.ID);
                Deadlines = await FormDefinitionProvider.GetDefinitionDeadlinesList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddUpload()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Upload/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveFormTabIndex);
        }
        private async void EditUpload(FORM_Definition_Upload Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Upload/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveFormTabIndex);
        }
        private async void DeleteUpload(FORM_Definition_Upload Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionUpload(Item.ID);
                Uploads = await FormDefinitionProvider.GetDefinitionUploadList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddRessource()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Ressource/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void EditRessource(FORM_Definition_Ressources Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Ressource/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void DeleteRessource(FORM_Definition_Ressources Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionRessource(Item.ID);
                Ressources = await FormDefinitionProvider.GetDefinitionRessourceList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddAdditionalForm()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Additional/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void EditAdditionalForm(FORM_Definition_Additional_FORM Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Additional/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void DeleteAdditionalForm(FORM_Definition_Additional_FORM Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionAdditionalFORM(Item.ID);
                AdditionalForms = await FormDefinitionProvider.GetDefinitionAdditionalFORMList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddReminder()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Reminder/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void EditReminder(FORM_Definition_Reminder Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Reminder/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void DeleteReminder(FORM_Definition_Reminder Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionReminder(Item.ID);
                Reminder = await FormDefinitionProvider.GetDefinitionReminderList(Data.ID);

                StateHasChanged();
            }
        }
        private async void AddSigning()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Sginings/Add/" + Data.ID + "/New/" + CurrentTab);
        }
        private async void EditSigning(FORM_Definition_Signings Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Sginings/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab);
        }
        private async void DeleteSigning(FORM_Definition_Signings Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionSigning(Item.ID);
                Signings = await FormDefinitionProvider.GetDefinitionSigningList(Data.ID);

                StateHasChanged();
            }
        }
        private void OnStepChanged()
        {
            StateHasChanged();
        }
        private async void MoveUpProperty(FORM_Definition_Property opt)
        {
            if (Properties != null && Properties.Count() > 0)
            {
                await ReOrderProperties();
                var newPos = Properties.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await FormDefinitionProvider.SetDefinitionProperty(opt);
                    await FormDefinitionProvider.SetDefinitionProperty(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownProperty(FORM_Definition_Property opt)
        {
            if (Properties != null && Properties.Count() > 0)
            {
                await ReOrderProperties();
                var newPos = Properties.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await FormDefinitionProvider.SetDefinitionProperty(opt);
                    await FormDefinitionProvider.SetDefinitionProperty(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderProperties()
        {
            int count = 1;

            foreach (var d in Properties.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await FormDefinitionProvider.SetDefinitionProperty(d);

                count++;
            }

            return true;
        }
        private async void MoveUpEvent(FORM_Definition_Event opt)
        {
            if (Events != null && Events.Count() > 0)
            {
                await ReOrderEvent();
                var newPos = Events.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await FormDefinitionProvider.SetDefinitionEvents(opt);
                    await FormDefinitionProvider.SetDefinitionEvents(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownEvent(FORM_Definition_Event opt)
        {
            if (Events != null && Events.Count() > 0)
            {
                await ReOrderEvent();
                var newPos = Events.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await FormDefinitionProvider.SetDefinitionEvents(opt);
                    await FormDefinitionProvider.SetDefinitionEvents(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderEvent()
        {
            int count = 1;

            foreach (var d in Events.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await FormDefinitionProvider.SetDefinitionEvents(d);

                count++;
            }

            return true;
        }
        private async void MoveUpRessources(FORM_Definition_Ressources opt)
        {
            if (Ressources != null && Ressources.Count() > 0)
            {
                await ReOrderRessources();
                var newPos = Ressources.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await FormDefinitionProvider.SetDefinitionRessource(opt);
                    await FormDefinitionProvider.SetDefinitionRessource(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownRessources(FORM_Definition_Ressources opt)
        {
            if (Ressources != null && Ressources.Count() > 0)
            {
                await ReOrderRessources();
                var newPos = Ressources.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await FormDefinitionProvider.SetDefinitionRessource(opt);
                    await FormDefinitionProvider.SetDefinitionRessource(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderRessources()
        {
            int count = 1;

            foreach (var d in Ressources.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await FormDefinitionProvider.SetDefinitionRessource(d);

                count++;
            }

            return true;
        }
        private async void MoveUpUpload(FORM_Definition_Upload opt)
        {
            if (Uploads != null && Uploads.Count() > 0)
            {
                await ReOrderUpload();
                var newPos = Uploads.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await FormDefinitionProvider.SetDefinitionUpload(opt);
                    await FormDefinitionProvider.SetDefinitionUpload(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownUpload(FORM_Definition_Upload opt)
        {
            if (Uploads != null && Uploads.Count() > 0)
            {
                await ReOrderUpload();
                var newPos = Uploads.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await FormDefinitionProvider.SetDefinitionUpload(opt);
                    await FormDefinitionProvider.SetDefinitionUpload(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderUpload()
        {
            int count = 1;

            foreach (var d in Uploads.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await FormDefinitionProvider.SetDefinitionUpload(d);

                count++;
            }

            return true;
        }
        private void NameChanged()
        {
            if (Data != null && Data.FORM_Name != null && Data.FORM_Definition_Extended.Count() > 0 && CurrentLanguage != null)
            {
                var item = Data.FORM_Definition_Extended.FirstOrDefault(p => p.LANG_Language_ID == CurrentLanguage);

                if (item != null)
                {
                    SessionWrapper.PageSubTitle = item.Name;
                }
            }
            StateHasChanged();
        }
        private async void MoveUpSignings(FORM_Definition_Signings opt)
        {
            if (Signings != null && Signings.Count() > 0)
            {
                await ReOrderSignings();
                var newPos = Signings.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await FormDefinitionProvider.SetDefinitionSigning(opt);
                    await FormDefinitionProvider.SetDefinitionSigning(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownSignings(FORM_Definition_Signings opt)
        {
            if (Signings != null && Signings.Count() > 0)
            {
                await ReOrderSignings();
                var newPos = Signings.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await FormDefinitionProvider.SetDefinitionSigning(opt);
                    await FormDefinitionProvider.SetDefinitionSigning(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderSignings()
        {
            int count = 1;

            foreach (var d in Signings.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await FormDefinitionProvider.SetDefinitionSigning(d);

                count++;
            }

            return true;
        }
        private async void AddPayment()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Payment/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void EditPayment(FORM_Definition_Payment_Position Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/Payment/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveTabIndex);
        }
        private async void DeletePayment(FORM_Definition_Payment_Position Item)
        {
            if (Item != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionPayment(Item.ID);
                PaymentPositions = await FormDefinitionProvider.GetDefinitionPaymentList(Data.ID);

                StateHasChanged();
            }
        }
        private async void MoveUpPayment(FORM_Definition_Payment_Position opt)
        {
            if (PaymentPositions != null && PaymentPositions.Count() > 0)
            {
                await ReOrderPayment();
                var newPos = PaymentPositions.FirstOrDefault(p => p.SortOrder == opt.SortOrder - 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder - 1;
                    newPos.SortOrder = newPos.SortOrder + 1;
                    await FormDefinitionProvider.SetDefinitionPayment(opt);
                    await FormDefinitionProvider.SetDefinitionPayment(newPos);
                }
            }

            StateHasChanged();
        }
        private async void MoveDownPayment(FORM_Definition_Payment_Position opt)
        {
            if (PaymentPositions != null && PaymentPositions.Count() > 0)
            {
                await ReOrderPayment();
                var newPos = PaymentPositions.FirstOrDefault(p => p.SortOrder == opt.SortOrder + 1);

                if (newPos != null)
                {
                    opt.SortOrder = opt.SortOrder + 1;
                    newPos.SortOrder = newPos.SortOrder - 1;
                    await FormDefinitionProvider.SetDefinitionPayment(opt);
                    await FormDefinitionProvider.SetDefinitionPayment(newPos);
                }
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderPayment()
        {
            int count = 1;

            foreach (var d in PaymentPositions.OrderBy(p => p.SortOrder))
            {
                d.SortOrder = count;

                await FormDefinitionProvider.SetDefinitionPayment(d);

                count++;
            }

            return true;
        }
        private async void AddMunField()
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/MunicipalField/Add/" + Data.ID + "/New/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void EditMunField(V_FORM_Definition_Municipal_Field Item)
        {
            BusyIndicatorService.IsBusy = true;
            StateHasChanged();

            await SaveForm(false);

            NavManager.NavigateTo("/Form/Definition/MunicipalField/Add/" + Data.ID + "/" + Item.ID + "/" + CurrentTab + "/" + AktiveMunicipalTasksTabIndex);
        }
        private async void DeleteMunField(V_FORM_Definition_Municipal_Field Item)
        {
            if (Item != null && Item.ID != null)
            {
                if (!await Dialogs.ConfirmAsync(TextProvider.Get("DELETE_ARE_YOU_SURE"), TextProvider.Get("WARNING")))
                    return;

                await FormDefinitionProvider.RemoveDefinitionMunField(Item.ID);

                if (Data != null)
                {
                    MunFields = await FormDefinitionProvider.GetDefinitionMunFieldList(Data.ID, LangProvider.GetCurrentLanguageID());
                }

                StateHasChanged();
            }
        }
        private async void MoveUpMunField(V_FORM_Definition_Municipal_Field opt)
        {
            if (MunFields != null && MunFields.Count() > 0 && opt.ID != null)
            {
                await ReOrderMunField();

                var startField = await FormDefinitionProvider.GetDefinitionMunField(opt.ID);

                if(startField != null) 
                { 

                    var newPos = MunFields.FirstOrDefault(p => p.SortOrder == startField.SortOrder - 1);

                    if (newPos != null && newPos.ID != null)
                    {
                        var newPosDB = await FormDefinitionProvider.GetDefinitionMunField(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder - 1;
                            newPosDB.SortOrder = newPosDB.SortOrder + 1;
                            await FormDefinitionProvider.SetDefinitionMunField(startField);
                            await FormDefinitionProvider.SetDefinitionMunField(newPosDB);
                        }
                    }
                }
            }

            if (Data != null)
            {
                MunFields = await FormDefinitionProvider.GetDefinitionMunFieldList(Data.ID, LangProvider.GetCurrentLanguageID());
            }

            StateHasChanged();
        }
        private async void MoveDownMunField(V_FORM_Definition_Municipal_Field opt)
        {
            if (MunFields != null && MunFields.Count() > 0 && opt.ID != null)
            {
                await ReOrderMunField();

                var startField = await FormDefinitionProvider.GetDefinitionMunField(opt.ID);

                if (startField != null)
                {

                    var newPos = MunFields.FirstOrDefault(p => p.SortOrder == startField.SortOrder + 1);

                    if (newPos != null && newPos.ID != null)
                    {
                        var newPosDB = await FormDefinitionProvider.GetDefinitionMunField(newPos.ID);

                        if (newPosDB != null)
                        {
                            startField.SortOrder = startField.SortOrder + 1;
                            newPosDB.SortOrder = newPosDB.SortOrder - 1;
                            await FormDefinitionProvider.SetDefinitionMunField(startField);
                            await FormDefinitionProvider.SetDefinitionMunField(newPosDB);
                        }
                    }
                }
            }

            if (Data != null)
            {
                MunFields = await FormDefinitionProvider.GetDefinitionMunFieldList(Data.ID, LangProvider.GetCurrentLanguageID());
            }

            StateHasChanged();
        }
        private async Task<bool> ReOrderMunField()
        {
            int count = 1;

            foreach (var d in MunFields.OrderBy(p => p.SortOrder))
            {
                if (d != null && d.ID != null)
                {
                    var field = await FormDefinitionProvider.GetDefinitionMunField(d.ID);

                    if (field != null)
                    {
                        field.SortOrder = count;

                        await FormDefinitionProvider.SetDefinitionMunField(field);
                    }
                }
                count++;
            }

            return true;
        }
        private async void ShowFormEditor()
        {
            IsFormRendererBusy = true;
            StateHasChanged();

            await SaveForm(false);
            await Task.Delay(1);

            ShowFormEditorPreview = true;
            StateHasChanged();
        }
        private async void HideFormEditor()
        {
            if (FormBuilderContainer != null)
            {
                if(FormBuilderContainer.HasChanges == true)
                {
                    SaveChangesDialogVisibility = true;
                    StateHasChanged();
                    return;
                }
            }

            if (FormBuilderContainer != null && Data != null)
            {
                Data.FORM_Definition_Field = await FormBuilderContainer.GetFields();
            }

            SaveChangesDialogVisibility = false;
            IsFormRendererBusy = false;
            ShowFormEditorPreview = false;
            StateHasChanged();
        }
        private async void SaveChanges()
        {
            if (FormBuilderContainer != null)
            {
                await FormBuilderContainer.SaveForm();

                if (Data != null)
                {
                    PreviewApplication = new FORM_Application() { FORM_Definition_ID = Data.ID };
                }
            }

            if (FormBuilderContainer != null && Data != null)
            {
                Data.FORM_Definition_Field = await FormBuilderContainer.GetFields();
            }

            SaveChangesDialogVisibility = false;
            IsFormRendererBusy = false;
            ShowFormEditorPreview = false;
            StateHasChanged();
        }
        private async void DiscardChanges()
        {
            if (FormBuilderContainer != null && Data != null)
            {
                Data.FORM_Definition_Field = await FormBuilderContainer.GetFields();
            }

            SaveChangesDialogVisibility = false;
            IsFormRendererBusy = false;
            ShowFormEditorPreview = false;
            StateHasChanged();
        }
    }
}

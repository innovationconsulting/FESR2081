﻿using ICWebApp.Application.Interface.Cache;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Application.Settings;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Components;

namespace ICWebApp.Pages.Translation.Developer
{
    public partial class Translation
    {
        [Inject] IAUTHProvider AuthProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IBusyIndicatorService BusyIndicatorService { get; set; }
        [Inject] ITEXTProvider TEXTProvider { get; set; }
        [Inject] ISessionWrapper SessionWrapper { get; set; }
        [Inject] ITEXTProvider TextProvider { get; set; }
        [Inject] ITEXTProviderCache TextProviderCache { get; set; }
        [Inject] IBreadCrumbService BreadCrumbService { get; set; }

        private string? LoginUrl { get; set; }
        private List<V_Translations> Data { get; set; } = new List<V_Translations>();
        private bool WindowVisible { get; set; } = false;
        private TEXT_SystemTexts? CurrentTranslationDE = null;
        private TEXT_SystemTexts? CurrentTranslationIT = null;
        protected override async Task OnInitializedAsync()
        {
            SessionWrapper.PageTitle = TextProvider.GetOrCreate("DEV_TRANSLATIONS");
            BreadCrumbService.ClearBreadCrumb();
            BreadCrumbService.AddBreadCrumb("/Translations", "BREADCRUMB_TRANSLATIONS",null);
            if (!AuthProvider.HasUserRole(AuthRoles.Developer))
            {
                NavManager.NavigateTo("/");
                StateHasChanged();
                return;
            }

            Data = await TEXTProvider.GetTexts();

            BusyIndicatorService.IsBusy = false;
            StateHasChanged();

            await base.OnInitializedAsync();
        }
        public async void EditTranslation(V_Translations Item)
        {
            CurrentTranslationDE = TEXTProvider.Get(Item.DEID);

            var itemIT = TEXTProvider.Get(Item.ITID);

            if (itemIT == null)
            {
                itemIT = new TEXT_SystemTexts();
                itemIT.ID = Guid.NewGuid();
                itemIT.Code = Item.Code;
                itemIT.LANG_LanguagesID = Guid.Parse("e450421a-baff-493e-a390-71b49be6485f");
                itemIT.AutoGenerated = false;

                CurrentTranslationIT = itemIT;
            }
            else
            {
                CurrentTranslationIT = itemIT;
            }

            WindowVisible = true;
            StateHasChanged();
        }
        private async Task<bool> SaveTranslation()
        {
            if (CurrentTranslationDE != null && CurrentTranslationIT != null)
            {
                await TEXTProvider.Set(CurrentTranslationDE);
                await TEXTProvider.Set(CurrentTranslationIT);

                TextProviderCache.Clear();
                StateHasChanged();
            }

            Data = await TEXTProvider.GetTexts();
            WindowClose();
            return true;
        }
        private void WindowClose()
        {
            WindowVisible = false;
            StateHasChanged();
        }
    }
}

using Telerik.Blazor.Services;
using ICWebApp.Classes.Telerik.Resources.TelerikMessages;
using ICWebApp.Application.Interface.Services;
using System.Globalization;
using Syncfusion.Blazor;
using ICWebApp.Classes.Syncfusion.Resources;

namespace ICWebApp.Classes.Syncfusion
{
    public class SyncfusionLocalizer : ISyncfusionStringLocalizer
    {
        public string GetText(string key)
        {
            return this.ResourceManager.GetString(key);
        }

        public System.Resources.ResourceManager ResourceManager
        {
            get
            {
                return SfResources.ResourceManager;
            }
        }
    }
}

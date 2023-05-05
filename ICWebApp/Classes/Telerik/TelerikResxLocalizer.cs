using Telerik.Blazor.Services;
using ICWebApp.Classes.Telerik.Resources.TelerikMessages;
using ICWebApp.Application.Interface.Services;
using System.Globalization;

namespace ICWebApp.Classes.Telerik
{
    public class TelerikResxLocalizer : ITelerikStringLocalizer
    {
        private readonly ISessionWrapper _sessionWrapper;
        public TelerikResxLocalizer(ISessionWrapper sessionWrapper)
        {
            this._sessionWrapper = sessionWrapper;
        }
        // this is the indexer you must implement
        public string this[string name]
        {
            get
            {
                return GetStringFromResource(name);
            }
        }

        // sample implementation - uses .resx files in the ~/Resources folder named TelerikMessages.<culture-locale>.resx
        public string GetStringFromResource(string key)
        {
            TelerikMessages.Culture = CultureInfo.CurrentCulture;
            var result = TelerikMessages.ResourceManager.GetString(key, TelerikMessages.Culture);

            return result;
        }
    }
}

using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;

namespace ICWebApp.Controller
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
    public class NewsController : ControllerBase
    {
        private INEWSService _NEWSService;
        private IAUTHProvider _AuthProvider;
        public NewsController(INEWSService _NEWSService, IAUTHProvider authProvider)
        {
            this._NEWSService = _NEWSService;
            this._AuthProvider = authProvider;
        }

        public async Task<IActionResult> Fetch()
        {
            try
            {
                var municipalities = await _AuthProvider.GetMunicipalityList();

                foreach (var mun in municipalities)
                {
                    await _NEWSService.ReadRSSFeed(mun.ID);
                }
            }
            catch(Exception ex)
            {
                return Content(ex.ToString());
            }

            return Content("Success");
        }
    }
}



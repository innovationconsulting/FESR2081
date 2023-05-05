using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Interface.Services;
using ICWebApp.Domain.DBModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ICWebApp.Controller
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
    public class VeriffController : ControllerBase
    {
        private readonly IAUTHProvider _authProvider;
        private readonly ISessionWrapper _sessionWrapper;
        public VeriffController(IAUTHProvider _authProvider, ISessionWrapper _sessionWrapper)
        {
            this._authProvider = _authProvider;
            this._sessionWrapper = _sessionWrapper;
        }
        public async Task<IActionResult> ResultHook()
        {
            StreamReader reader = new StreamReader(Request.Body); 

            string data = await reader.ReadToEndAsync();

            dynamic result = JsonConvert.DeserializeObject(data);

            if (result != null)
            {
                AUTH_VeriffResponse response = new AUTH_VeriffResponse();

                response.ResponseJson = data;

                response.ID = Guid.NewGuid();
                response.Code = (string)(result.verification.code);
                response.Status = (string)(result.verification.status);
                response.Firstname = (string)(result.verification.person.firstName);
                response.Lastname = (string)(result.verification.person.lastName);

                if (result.verification.person.dateOfBirth != null)
                {
                    response.DateOfBirth = (DateTime)(result.verification.person.dateOfBirth);
                }
                response.Gender = (string)(result.verification.person.gender);
                response.Nationality = (string)(result.verification.person.nationality);
                response.PlaceOfBirth = (string)(result.verification.person.placeOfBirth);
                response.IDCode = (string)(result.verification.person.idCode);
                

                try
                {
                    response.AUTH_Users_ID = Guid.Parse((string)(result.verification.vendorData));

                    var authUser = await _authProvider.GetAnagraficByUserID(response.AUTH_Users_ID.Value);

                    if (authUser != null && response != null) 
                    {
                        if (response.DateOfBirth != null && authUser.DateOfBirth != null 
                            && response.DateOfBirth.Value.Year == authUser.DateOfBirth.Value.Year 
                            && response.DateOfBirth.Value.Month == authUser.DateOfBirth.Value.Month
                            && response.DateOfBirth.Value.Day == authUser.DateOfBirth.Value.Day
                            && response.Firstname != null && authUser.FirstName != null && response.Firstname.Trim().ToLower() == authUser.FirstName.Trim().ToLower()
                            && response.Lastname != null && authUser.LastName != null && response.Lastname.Trim().ToLower() == authUser.LastName.Trim().ToLower()
                            && response.AUTH_Users_ID != null && response.Status == "approved")
                        {
                            var dbUser = await _authProvider.GetUserWithoutMunicipality(response.AUTH_Users_ID.Value);

                            if (dbUser != null)
                            {
                                dbUser.VeriffConfirmed = true;

                                await _authProvider.UpdateUser(dbUser);
                            }
                        } 
                        else
                        {
                            response.Status = "WrongData";
                        }
                    }
                }
                catch {
                    response.Status = "GeneralError";
                }

                response.CreationDate = DateTime.Now;

                await _authProvider.SetVeriffResponse(response);
            }

            return LocalRedirect("/Veriff/Result");
        }
    }
}

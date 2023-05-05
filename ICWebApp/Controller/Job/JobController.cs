using Microsoft.AspNetCore.Mvc;

namespace ICWebApp.Controller.Job
{
    public class JobController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            ClearDownloadFolder();

            return Content("Ok");
        }
        private bool ClearDownloadFolder()
        {
            var path = "D:/Comunix/Cache/";

            Directory.Delete(path, true);

            return true;
        }
    }
}

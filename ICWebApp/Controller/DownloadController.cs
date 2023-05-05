using ICWebApp.Application.Interface.Provider;
using Microsoft.AspNetCore.Mvc;

namespace ICWebApp.Controller
{
    public class DownloadController : ControllerBase
    {
        private IFILEProvider FileProvider;
        public DownloadController(IFILEProvider FileProvider)
        {
            this.FileProvider = FileProvider;
        }

        [HttpGet("Download")]
        public IActionResult Download([FromQuery] string link, string filename, string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                var dbToken = FileProvider.GetDownloadLogByToken(Guid.Parse(token));

                if (dbToken != null)
                {
                    dbToken.DownloadDate = DateTime.Now;
                    dbToken.DownloadExpirationDate = DateTime.Now;

                    FileProvider.SetDownloadLog(dbToken);

                    var net = new System.Net.WebClient();

                    var data = net.DownloadData(link);

                    var content = new System.IO.MemoryStream(data);

                    var contentType = "APPLICATION/octet-stream";

                    return File(content, contentType, filename);
                }
            }

            return Content("Error");
        }
    }
}

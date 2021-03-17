using AspNetCore.Security.JwsDetached.Example.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.Security.JwsDetached.Example.Controllers
{
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpPost("/test")]
        public ActionResult Test(Data data)
        {
            return base.Ok(data);
        }
    }
}

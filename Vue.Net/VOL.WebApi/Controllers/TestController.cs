using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.Enums;
using VOL.Core.Filters;
using VOL.Entity.DomainModels;
using VOL.System.IServices;
using VOL.WebApi.Manager;

namespace VOL.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpPost]
        [HttpPost, Route("SpiderStart"), AllowAnonymous]
        public async Task<IActionResult> SpiderStart(string url)
        {
            //var url = "https://item.jd.hk/40682842338.html";//定义爬虫入口URL


            //var data = await SpiderHelper.Start(url);
            //var data = await SpiderHelper.StartByAgility(url);


            string id = url.Replace(".html", "").Split("/").ToList().LastOrDefault();
            var data = await SpiderHelper.HttpGet(url,id);

            return Content(data);
        }

    }
}
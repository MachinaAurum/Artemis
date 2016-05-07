using System.Web.Http;
using System.Net.Http;
using WebApiContrib.Formatting.Html;
using System.Threading.Tasks;
using com.espertech.esper.client;

namespace MachinaAurum.Artemis.Controllers
{
    public class HomeController : ApiController
    {
        EPServiceProvider Provider;

        public HomeController(EPServiceProvider provider)
        {
            Provider = provider;
        }

        [HttpGet, Route("~/ui")]
        public IHttpActionResult Index()
        {
            return new ViewResult(Request, "Index", null);
        }

        [HttpPost, Route("~/ui")]
        public async Task<object> Save()
        {
            var form = await Request.Content.ReadAsFormDataAsync();
            var routing = form["routing"];

            var elp1 = Provider.EPAdministrator.CreateEPL(routing);            
            var instance = Provider.EPRuntime.DataFlowRuntime.Instantiate("HelloWorldDataFlow");            
            instance.Start();

            return new { };
        }
    }
}

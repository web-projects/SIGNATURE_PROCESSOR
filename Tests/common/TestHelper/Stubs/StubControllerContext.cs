using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TestHelper
{
    public class StubControllerContext : ControllerContext
    {
        public StubControllerContext(HttpRequest request, HttpResponse response)
        {
            HttpContext = new StubHttpContext(request, response);
        }
    }
}

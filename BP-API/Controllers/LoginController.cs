using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BP_API.Controllers
{
    public class LoginController : ApiController
    {
        private Dictionary<string, string> users = new Dictionary<string, string>()
        {
            { "borrower1", "b123" },
            { "borrower2", "b234" },
            { "arbour", "a123" },
            { "pme",  "p123" },
            { "trustee", "t123" }
        };

        [HttpPost]
        [Route("api/login")]
        public HttpResponseMessage Login([FromBody] Models.LoginRequest loginRequest)
        {
            if (loginRequest == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request body");
            }

            if (users.ContainsKey(loginRequest.Username) && users[loginRequest.Username] == loginRequest.Password)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new { message = "Login successful" });
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { message = "Invalid credentials" });
            }
        }
    }
}

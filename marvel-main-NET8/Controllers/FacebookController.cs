using marvel_main_NET8.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace marvel_main_NET8.Controllers
{
    //[Route("api/[controller]")]
    [Route("api")]
    [ApiController]
    public class FacebookController : ControllerBase
    {
        private readonly ScrmDbContext _scrme;

        public FacebookController(ScrmDbContext context)
        {
            _scrme = context;
        }


        // Check Ticket Id
        [Route("CheckTicketId")]
        [HttpPost]
        public IActionResult CheckTicketId([FromBody] JsonObject data)
        {
            string token = (data[AppInp.InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[AppInp.InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (ValidateClass.Authenticated(token, tk_agentId))
                {
                    bool postExists = TicketId_Exists(data);
                    return Ok(new { result = postExists });
                }
                else
                {
                    return Ok(new { result = AppOutp.OutputResult_FAIL, details = AppOutp.Not_Auth_Desc });
                }

            }
            catch (Exception err)
            {
                return Ok(new { result = AppOutp.OutputResult_FAIL, details = err.Message });
            }
        }

        private bool TicketId_Exists(JsonObject data)
        {
            int ticketId = Convert.ToInt32((data["Ticket_Id"] ?? "-1").ToString());

            bool exists = true;

            // obtain the row of data with the given ticket id
            facebook_post? _post = (from _f in _scrme.facebook_posts
                                             where _f.Ticket_Id == ticketId
                                             select _f).SingleOrDefault();

            // if there is at least 1 post
            if (_post == null)
            {
                exists = false;
            }

            return exists;
        }





    }
}

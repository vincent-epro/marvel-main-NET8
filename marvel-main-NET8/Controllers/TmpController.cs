using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using marvel_main_NET8.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Nodes;
using System.Reflection;

namespace marvel_main_NET8.Controllers
{

    //[Route("api/[controller]")]
    [Route("api")]
    [ApiController]
    public class TmpController : ControllerBase
    {
        private readonly ScrmDbContext _scrme;

        public TmpController(ScrmDbContext context)
        {
            _scrme = context;
        }

        // Check Agent Id
        [Route("CheckAgentId")]
        [HttpPost]
        public IActionResult CheckAgentId([FromBody] JsonObject data)
        {

            try
            {
                bool isExists = userExists(data);
                return Ok(new { result = isExists });
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private bool userExists(JsonObject data)
        {

            int agentId = Convert.ToInt32((data["AgentID"] ?? "-1").ToString());
            bool exists = true;

            agentinfo? _agent = (from _a in _scrme.agentinfos
                                 where _a.AgentID == agentId
                                 select _a).SingleOrDefault<agentinfo>();

            if (_agent == null)
            {
                exists = false;
            }

            return exists;
        }


        // Check Seller Id
        [Route("CheckSellerId")]
        [HttpPost]
        public IActionResult CheckSellerId([FromBody] JsonObject data)
        {

            try
            {
                bool isExists = sellerIdExists(data);
                return Ok(new { result = isExists });
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private bool sellerIdExists(JsonObject data)
        {

            string sellerId = (data["SellerID"] ?? "").ToString();
            bool exists = true;

            agentinfo? _agent = (from _a in _scrme.agentinfos
                                 where _a.SellerID == sellerId
                                 select _a).SingleOrDefault<agentinfo>();

            if (_agent == null)
            {
                exists = false;
            }

            return exists;
        }



    }

}
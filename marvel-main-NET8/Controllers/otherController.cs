using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using marvel_main_NET8.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Nodes;
using System.Reflection;

//[Route("api/[controller]")]
[Route("api")]
[ApiController]
public class otherController : ControllerBase
{
    private readonly ScrmDbContext _scrme;

    public otherController(ScrmDbContext context)
    {
        _scrme = context;
    }

    // Check Agent API
    [Route("CheckAgentId")]
    [HttpPost]
    public IActionResult CheckAgentId([FromBody] JsonObject data)
    {
       // string tk_agentId = (data.Agent_Id == null) ? string.Empty : Convert.ToString(data.Agent_Id.Value);

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
        // int agentId = data.TryGetProperty("AgentID", out JsonElement element) && element.ValueKind == JsonValueKind.Number ? element.GetInt32() : -1;
        int agentId = Convert.ToInt32((data["AgentID"] ?? "-1").ToString());
        bool exists = true;

        Agentinfo? _agent = (from _a in _scrme.Agentinfos
                             where _a.AgentId == agentId
                                       select _a).SingleOrDefault<Agentinfo>();

        if (_agent == null)
        {
            exists = false;
        }

        return exists;
    }


}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using marvel_main_NET8.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Nodes;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace marvel_main_NET8.Controllers
{

    //[Route("api/[controller]")]
    [Route("api")]
    [ApiController]
    public class OtherController : ControllerBase
    {
        private readonly ScrmDbContext _scrme;

        public OtherController(ScrmDbContext context)
        {
            _scrme = context;
        }


        // JWT
        private static readonly string Secret = Environment.GetEnvironmentVariable("JWT_Secret") ?? "";

        public static string GenerateToken(string P_Username)
        {
            byte[] _non_base64_secret = Convert.FromBase64String(Secret);
            SymmetricSecurityKey _symmetric_security_key = new SymmetricSecurityKey(_non_base64_secret);

            ClaimsIdentity _claims_identity = new ClaimsIdentity();
            _claims_identity.AddClaim(new Claim(ClaimTypes.Name, P_Username));

            SecurityTokenDescriptor _security_token_descriptor = new SecurityTokenDescriptor
            {
                Subject = _claims_identity,
                Expires = DateTime.UtcNow.AddMinutes(60 * 24),
                SigningCredentials = new SigningCredentials(_symmetric_security_key, SecurityAlgorithms.HmacSha256Signature)
            };

            JwtSecurityTokenHandler _jwt_security_token_handler = new JwtSecurityTokenHandler();
            JwtSecurityToken _jwt_security_token = _jwt_security_token_handler.CreateJwtSecurityToken(_security_token_descriptor);
            return _jwt_security_token_handler.WriteToken(_jwt_security_token);
        }

        public static string? ValidateToken(string P_Token)
        {
            ClaimsIdentity? _claims_identity;

            ClaimsPrincipal? _claims_principal = GetClaimsPrincipal(P_Token);
            if (_claims_principal == null) return null;


            _claims_identity = (ClaimsIdentity?)_claims_principal.Identity;


            Claim? _claim_name = _claims_identity?.FindFirst(ClaimTypes.Name);
            return _claim_name?.Value; // username
        }

        public static ClaimsPrincipal? GetClaimsPrincipal(string P_Token)
        {
            try
            {
                JwtSecurityTokenHandler _jwt_security_token_handler = new JwtSecurityTokenHandler();
                JwtSecurityToken _jwt_security_token = (JwtSecurityToken)_jwt_security_token_handler.ReadToken(P_Token);

                if (_jwt_security_token == null) return null;

                byte[] _non_base64_secret = Convert.FromBase64String(Secret);

                TokenValidationParameters _token_validation_parameters = new TokenValidationParameters()
                {
                    RequireExpirationTime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(_non_base64_secret)
                };

                SecurityToken _security_token;
                ClaimsPrincipal _claims_principal = _jwt_security_token_handler.ValidateToken(P_Token, _token_validation_parameters, out _security_token);

                return _claims_principal;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static bool Authenticated(string token, string P_Username)
        {


            if (string.IsNullOrEmpty(token) ||
                string.IsNullOrEmpty(P_Username))
            {
                return false;
            }

            return ValidateToken(token) == P_Username;

        }






        // Check Agent Id
        [Route("CheckAgentId")]
        [HttpPost]
        public IActionResult CheckAgentId([FromBody] JsonObject data)
        {
            string token = (data["Token"] ?? "").ToString();
            string tk_agentId = (data["Agent_Id"] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    bool isExists = UserExists(data);
                    return Ok(new { result = isExists });
                }
                else
                {
                    return Ok(new { result = "fail", details = "Not Auth." });
                }

            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private bool UserExists(JsonObject data)
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
            string token = (data["Token"] ?? "").ToString();
            string tk_agentId = (data["Agent_Id"] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    bool isExists = SellerIdExists(data);
                    return Ok(new { result = isExists });
                }
                else
                {
                    return Ok(new { result = "fail", details = "Not Auth." });
                }
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private bool SellerIdExists(JsonObject data)
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


        // Retrieve user roles
        [Route("GetRoles")]
        [HttpPost]
        public IActionResult GetRoles([FromBody] JsonObject data)
        {
            string status = (data["RoleStatus"] ?? "").ToString();
            string token = (data["Token"] ?? "").ToString();
            string tk_agentId = (data["Agent_Id"] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    return Content(GetRoleinfo(status).ToString(), "application/json; charset=utf-8", Encoding.UTF8);
                }
                else
                {
                    return Ok(new { result = "fail", details = "Not Auth." });
                }

            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });

            }
        }

        private JObject GetRoleinfo(string status)
        {
            List<JObject> roleListJason = new List<JObject>(); // declare overall json list

           // JObject rolesJson = new JObject(); // declare json object

            IQueryable<user_role> _linq_user_roles = from _r in _scrme.user_roles
                                                     select _r; // declare user role data

            if (status == "Active")
            {
                _linq_user_roles = _linq_user_roles.Where(_r => _r.RoleStatus == "A");
            }

            foreach (user_role _role_item in _linq_user_roles)
            {
                // declare a temp json object to store each column of data
                JObject tempJson = new JObject();
                tempJson.RemoveAll(); // clear the temp object

                // iterate through each column 
                foreach (PropertyInfo property in _role_item.GetType().GetProperties())
                {
                    tempJson.Add(new JProperty(property.Name, property.GetValue(_role_item)));

                }
                roleListJason.Add(tempJson);
            }

            JObject rolesJson = new JObject() // add to overall json object
                    {
                        new JProperty("result", "success"),
                        new JProperty("details", roleListJason)
                    };

            return rolesJson;
        }


        // Retrieve agent list of a role
        [Route("GetAgentsOfRole")]
        [HttpPost]
        public IActionResult GetAgentsOfRole([FromBody] JsonObject data)
        {
            string token = (data["Token"] ?? "").ToString();
            string tk_agentId = (data["Agent_Id"] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    string agentsOfRole = GetAgentlistByRole(data);
                    return Ok(new { result = "success", details = agentsOfRole });
                }
                else
                {
                    return Ok(new { result = "fail", details = "Not Auth." });
                }
            }
            catch (Exception err)
            {               
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private string GetAgentlistByRole(JsonObject data)
        {
            int levelId = Convert.ToInt32((data["LevelID"] ?? "-1").ToString());

            // obtain user records based on the level id
            IQueryable<agentinfo> _agents = (from _a in _scrme.agentinfos
                                                        where _a.LevelID == levelId
                                                        select _a);

            string agentsOfRole = string.Empty;

            if (_agents.Count() > 0)
            {
                agentsOfRole = "The role is used by: ";
                // iterate through each row of data in agentInfo
                foreach (agentinfo _agent_item in _agents)
                {
                    // append agent to the string
                    agentsOfRole = agentsOfRole + _agent_item.AgentName + "(ID: " + _agent_item.AgentID + ")\n";
                }
            }

            return agentsOfRole;
        }


        // Retrieve login details
        [Route("GetLogin")]
        [HttpPost]
        public IActionResult GetLogin([FromBody] JsonObject data)
        {
            string token = (data["Token"] ?? "").ToString();
            string tk_agentId = (data["Agent_Id"] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    return Content(GetLoginInfo().ToString(), "application/json; charset=utf-8", Encoding.UTF8);
                }
                else
                {
                    return Ok(new { result = "fail", details = "Not Auth." });
                }

            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });

            }
        }


        private JObject GetLoginInfo()
        {
            // obtain linq results by left joining 2 tables: agentinto and user_role
            var _agent_users = from _agent in _scrme.agentinfos
                               join _role in _scrme.user_roles
                                    on _agent.LevelID equals _role.RoleID
                                    into joined
                               from _roles in joined.DefaultIfEmpty()
                               orderby _agent.SellerID
                               //where _agent.Account_status == "Active"
                               select new
                               {
                                   _agent,
                                   _roles.RoleName
                               };

            // declare a list of json objects containing the each row of data
            List<JObject> _agent_list = new List<JObject>();


            // JObject allJsonResults = new JObject(); // declare a json object to contain all rows of data

            // iterate through each row of data in agentInfo
            foreach (var _agent_item in _agent_users)
            {
                // declare a temp json object to store each column of data
                JObject tempJson = new JObject();

                tempJson.RemoveAll(); // clear the temp object

                // iterate through each column of the _agent_item
                foreach (PropertyInfo property in _agent_item._agent.GetType().GetProperties())
                {
                    // add all column names and values to temp, except "Password"
                    switch (property.Name)
                    {

                        case "Password":
                            {
                                break;
                            }

                        default:
                            //add the column name and value to temp
                            tempJson.Add(new JProperty(property.Name, property.GetValue(_agent_item._agent)));
                            break;
                    }
                }

                tempJson.Add(new JProperty("RoleName", _agent_item.RoleName));

                _agent_list.Add(tempJson);
            }



            // set up _all_results json data
            JObject allJsonResults = new JObject()
            {
                new JProperty("result", "success"),
                new JProperty("details", _agent_list)
            };

            // return all results in json format
            return allJsonResults;
        }





    }

    }
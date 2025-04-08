﻿using Microsoft.AspNetCore.Http;
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

    public class ConfigDetails
    {
        // Public properties for external access
        public int P_Id { get; private set; }
        public string? P_Name { get; private set; }
        public string? P_Value { get; private set; }

        public ConfigDetails(int id, string? name, string? value)
        {
            P_Id = id;
            P_Name = name;
            P_Value = value;
        }
    }

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

        private const string OutputResult_SUCC = "success";

        private const string Not_Auth_Desc = "Not Auth.";

        private const string InputAuth_Agent_Id = "Agent_Id";
        private const string InputAuth_Token = "Token";


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
            catch (Exception )
            {
                return null;
            }
        }

        public static bool Authenticated(string token, string P_Username)
        {
            if (string.IsNullOrEmpty(token) ||
                string.IsNullOrEmpty(P_Username))
            {
                return false;
            }

            return ValidateToken(token) == P_Username;

        }




        // Login
        [Route("Login")]
        [HttpPost]
        public IActionResult Login([FromBody] JsonObject data)
        {

            try
            {
                return Content(Logincrm(data).ToString(), "application/json; charset=utf-8", Encoding.UTF8);
            }
            catch (Exception )
            {
                return Ok(new { result = "fail", details = "Invalid Parameters" });

            }
        }

        private JObject Logincrm(JsonObject data)
        {
            // obtain form body values                        
            string sellerId = (data["SellerID"] ?? "").ToString();
            string password = (data["Password"] ?? "").ToString();


            bool canLogin = false;
            string details = string.Empty;

            // obtain all data from table agentInfo
            IQueryable<agentinfo> _agent_details = (from _a in _scrme.agentinfos
                                                    select _a);

            // declare an agent and temp agent record
            agentinfo? _agent = null;
            agentinfo? _agent_temp = null;

            int existing_agentid = 0;

            // use seller id to log in
            if (sellerId != string.Empty)
            {
                // obtain the single record with the seller id and password
                _agent = _agent_details.Where(_a => _a.SellerID == sellerId && _a.Password == password).SingleOrDefault<agentinfo>();

                // obtain the single record with the seller id
                _agent_temp = _agent_details.Where(_a => _a.SellerID == sellerId).SingleOrDefault<agentinfo>();
            }


            // declare a json object to contain all rows of data
            JObject allJsonResults = new JObject();

            //both id and password are correct
            if (_agent != null)
            {
                // the account status is also active and the login attempt is <= 3
                if (_agent.Account_status == "Active" && _agent.Counter < 3 && _agent.ExpiryDate > DateTime.Today && _agent.LastLoginDate != null)
                {
                    canLogin = true; //can login
                    _agent.Counter = 0; // reset counter
                    _agent.LastLoginDate = DateTime.Now; // update LastLoginDate

                    // update status and save changes in db
                    _scrme.SaveChanges();

                    // declare a temp json object to store each agent item
                    JObject agentObj = new JObject();
                    agentObj.RemoveAll(); // clear the object

                    // iterate each column of the _agent_item
                    foreach (PropertyInfo property in _agent.GetType().GetProperties())
                    {
                        // add all column names and values to temp, except "Password"
                        switch (property.Name)
                        {
                            case "Password":
                            case "Photo":
                            case "Photo_Type":
                                {
                                    break;
                                }
                            default:
                                // add the column name and value to temp
                                agentObj.Add(new JProperty(property.Name, property.GetValue(_agent)));
                                break;
                        }
                    }

                    int? roleId = _agent.LevelID; // obtain the role id from agent_info

                    // use the role id to find the corresponding role name and companies
                    var _role = (from _r in _scrme.user_roles
                                 where _r.RoleID == roleId
                                 select new
                                 {
                                     _r.RoleName,
                                     _r.Companies,
                                     _r.Categories,
                                     _r.Functions
                                 }).SingleOrDefault();

                    if (_role != null)
                    {
                        // add role name and companies to _agentObj
                        agentObj.Add(new JProperty("RoleName", _role.RoleName));
                        agentObj.Add(new JProperty("Companies", _role.Companies));
                        agentObj.Add(new JProperty("Categories", _role.Categories));
                        agentObj.Add(new JProperty("Functions", _role.Functions));
                    }

                    agentObj.Add(new JProperty(InputAuth_Token, GenerateToken(Convert.ToString(_agent.AgentID))));


                    // obtain all data from table config
                    IQueryable<config> _config_details = (from _conf in _scrme.configs
                                                          select _conf);

                    // declare new FieldDetails class object as List
                    List<ConfigDetails> _list_config_details = new List<ConfigDetails>();

                    // for existing config
                    if (_config_details.Count() > 0)
                    {
                        // iterate through the rows from that field category
                        foreach (config _config_item in _config_details)
                        {
                            // declare ConfigDetails class object
                            // ConfigDetails _cd = new ConfigDetails(); // old

                            // assign the id, name and details to the ConfigDetails object
                            //  _cd.P_Id = _config_item.P_Id; // old
                            //  _cd.P_Name = _config_item.P_Name; // old
                            //  _cd.P_Value = _config_item.P_Value; // old

                            // create ConfigDetails object using the constructor
                            ConfigDetails _cd = new ConfigDetails(_config_item.P_Id, _config_item.P_Name, _config_item.P_Value);


                            // apend the object to the list
                            _list_config_details.Add(_cd);
                        }

                        // for non-existing field category
                    }
                    else
                    {
                     //   _list_config_details = null; //old
                    }

                    JArray configJson = (JArray)JToken.FromObject(_list_config_details);

                    // add config to _agentObj
                    agentObj.Add(new JProperty("config", configJson));


                    allJsonResults = new JObject()
                    {
                        new JProperty("result", OutputResult_SUCC),
                        new JProperty("details", agentObj)
                    };

                }
                else
                {
                    // set the alert messages and login status to false
                    if (_agent.Account_status != "Active")
                    {
                        details = "Account is inactive.";
                    }
                    else if (_agent.LastLoginDate == null)
                    {
                        details = "Initial Login.";
                    }
                    else if (_agent.ExpiryDate <= DateTime.Today)
                    {
                        details = "Account has expired.";
                    }
                    else if (_agent.Counter == 3)
                    {
                        details = "Account is locked.";
                    }

                    //      _agent.Counter = 0; // reset counter

                    canLogin = false;
                    existing_agentid = _agent.AgentID;
                }
            }
            else
            // either the id or password is incorrect
            {

                canLogin = false; // cannot login

                // wrong password
                if (_agent_temp != null)
                {
                    // only add counter when it's < 3
                    if (_agent_temp.Counter < 3)
                    {
                        // iterate the counter
                        _agent_temp.Counter = _agent_temp.Counter + 1;
                    }
                    else
                    {
                        details = "Account is locked.";
                    }

                    // update status and save changes in db
                    _scrme.SaveChanges();
                }
            }

            if (!canLogin)
            {
                if (details == "Initial Login.")
                {
                    allJsonResults = new JObject()
                    {
                        new JProperty("result", "fail"),
                        new JProperty("details", details),
                        new JProperty("AgentID", existing_agentid),
                        new JProperty(InputAuth_Token, GenerateToken(Convert.ToString(existing_agentid)))
                    };
                }
                else if (details == "Account has expired.")
                {
                    allJsonResults = new JObject()
                    {
                        new JProperty("result", "fail"),
                        new JProperty("details", details),
                        new JProperty("AgentID", existing_agentid),
                        new JProperty(InputAuth_Token, GenerateToken(Convert.ToString(existing_agentid)))
                    };
                }
                else
                {

                    details = (details == "") ? "Invalid login." : details;

                    allJsonResults = new JObject()
                    {
                        new JProperty("result", "fail"),
                        new JProperty("details", details)
                    };
                }
            }


            // return all results in json format
            return allJsonResults;
        }



        // Create User
        [Route("CreateUser")]
        [HttpPost]
        public IActionResult CreateUser([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    return Ok(new { result = OutputResult_SUCC, details = CreateCRMUser(data) });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }

            }
            catch (Exception )
            {
                return Ok(new { result = "fail", details = "cannot create user." });

            }
        }

        private List<agentinfo> CreateCRMUser(JsonObject data)
        {
            // declare db table items
            agentinfo _agent_item = new agentinfo();

            // obtain form body values
            int agentId = Convert.ToInt32((data["AgentID"] ?? "-1").ToString());
            string sellerID = (data["SellerID"] ?? "").ToString();
            string agentName = (data["AgentName"] ?? "").ToString();
            string email = (data["Email"] ?? "").ToString();
            string password = (data["Password"] ?? "").ToString();

            string role = (data["Role"] ?? "").ToString();
            int levelId = GetLevelId(role); // get level Id using the role name

            string accountStatus = (data["Account_status"] ?? "").ToString();
            string photoRemoved = (data["Photo_Removed"] ?? "").ToString();

            // assign new agent record
            _agent_item.AgentID = agentId;
            _agent_item.SellerID = sellerID;
            _agent_item.Counter = 0;
            _agent_item.AgentName = agentName;
            _agent_item.Email = email;
            _agent_item.Password = password;
            _agent_item.LevelID = levelId;
            _agent_item.Account_status = accountStatus;
            
            int passwordChangeFrequency = GetPasswordChangeFrequency();
            _agent_item.ExpiryDate = DateTime.Today.AddDays(passwordChangeFrequency);

            if (photoRemoved == "Y")
            {
                _agent_item.Photo = null;
                _agent_item.Photo_Type = null;
            }

            // add new user role record
            _scrme.agentinfos.Add(_agent_item);

            // save db changes
            _scrme.SaveChanges();


            // obtain the new agent from table "agentinfo"
            List<agentinfo> _new_agent = (from _a in _scrme.agentinfos
                                                     where _a.ColId == _agent_item.ColId
                                                     select _a).ToList();


            return _new_agent;
        }


        private int GetLevelId(string roleName)
        {
            int levelId = 0;

            user_role? _linq_user_role = (from _r in _scrme.user_roles
                                                   where _r.RoleName == roleName
                                                   select _r).SingleOrDefault();

            if (_linq_user_role != null)
            {
                levelId = _linq_user_role.RoleID;
            }

            return levelId;
        }

        private int GetPasswordChangeFrequency()
        {
            config? _config = (from _c in _scrme.configs
                                        where _c.P_Name == "PasswordChangeFrequency"
                                        select _c).SingleOrDefault<config>();

            if (_config == null)
            {
                return 90;
            }
            else
            {
                return Convert.ToInt32(_config.P_Value);
            }
        }


        // Update User
        [Route("UpdateUser")]
        [HttpPut]
        public IActionResult UpdateUser([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    UpdateCRMUser(data);
                    return Ok(new { result = OutputResult_SUCC, details = "updated user" });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private void UpdateCRMUser(JsonObject data)
        {
            // obtain form body values
            int colId = Convert.ToInt32((data["ColId"] ?? "-1").ToString());

            int agentId = Convert.ToInt32((data["AgentID"] ?? "-1").ToString());
            string sellerID = (data["SellerID"] ?? "").ToString();
            string agentName = (data["AgentName"] ?? "").ToString();
            string email = (data["Email"] ?? "").ToString();
            string password = (data["Password"] ?? "").ToString();

            string role = (data["Role"] ?? "").ToString();
            int levelId = GetLevelId(role); // get level Id using the role name

            string accountStatus = (data["Account_status"] ?? "").ToString();
            string photoRemoved = (data["Photo_Removed"] ?? "").ToString();

            int counter = Convert.ToInt32((data["Counter"] ?? "-1").ToString());


            // obtain single user record based on the agent id
            agentinfo? _agent = (from _a in _scrme.agentinfos
                                           where _a.ColId == colId
                                           select _a).SingleOrDefault<agentinfo>();

            // if there is at least 1 role
            if (_agent != null)
            {
                // decide if it's update with agent id or update w/o agent id
                if (_agent.AgentID == agentId && _agent.SellerID == sellerID)
                {
                    // assign the updated values to the row

                    _agent.AgentName = agentName;
                    _agent.Email = email;
                    if (password != string.Empty)
                    {
                        _agent.Password = password;
                        int passwordChangeFrequency = GetPasswordChangeFrequency();
                        _agent.ExpiryDate = DateTime.Today.AddDays(passwordChangeFrequency); // only extend expiry date if password is changed
                    }
                    _agent.LevelID = levelId;
                    _agent.Account_status = accountStatus;
                    _agent.Counter = counter;

                    if (photoRemoved == "Y")
                    {
                        _agent.Photo = null;
                        _agent.Photo_Type = null;
                    }

                }
                else
                {
                    // add a new row using the data above
                    // declare db table items
                    agentinfo _new_agent_item = new agentinfo();

                    // assign new agent record
                    _new_agent_item.AgentID = agentId;
                    _new_agent_item.AgentName = agentName;
                    _new_agent_item.Password = _agent.Password;
                    _new_agent_item.LevelID = _agent.LevelID;
                    _new_agent_item.SellerID = sellerID;
                    _new_agent_item.Counter = _agent.Counter;
                    int passwordChangeFrequency = GetPasswordChangeFrequency();
                    
                    _new_agent_item.ExpiryDate = DateTime.Today.AddDays(passwordChangeFrequency);
                    _new_agent_item.LastLoginDate = _agent.LastLoginDate;
                    _new_agent_item.Account_status = _agent.Account_status;
                    _new_agent_item.Email = _agent.Email;

                    if (photoRemoved == "Y")
                    {
                        _new_agent_item.Photo = null;
                        _new_agent_item.Photo_Type = null;
                    }

                    // delete the old row
                    _scrme.agentinfos.Remove(_agent);

                    // add the new row
                    _scrme.agentinfos.Add(_new_agent_item);
                }
                _scrme.SaveChanges(); // save changes to db
            }
        }


        // Check Agent Id
        [Route("CheckAgentId")]
        [HttpPost]
        public IActionResult CheckAgentId([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    bool isExists = UserExists(data);
                    return Ok(new { result = isExists });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
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
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    bool isExists = SellerIdExists(data);
                    return Ok(new { result = isExists });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
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
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    return Content(GetRoleinfo(status).ToString(), "application/json; charset=utf-8", Encoding.UTF8);
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
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
                        new JProperty("result", OutputResult_SUCC),
                        new JProperty("details", roleListJason)
                    };

            return rolesJson;
        }


        // Retrieve agent list of a role
        [Route("GetAgentsOfRole")]
        [HttpPost]
        public IActionResult GetAgentsOfRole([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    string agentsOfRole = GetAgentlistByRole(data);
                    return Ok(new { result = OutputResult_SUCC, details = agentsOfRole });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
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
                StringBuilder sb = new StringBuilder();
                sb.Append("The role is used by: ");

                foreach (agentinfo _agent_item in _agents)
                {
                    sb.Append($"{_agent_item.AgentName}(ID: {_agent_item.AgentID})\n");
                }

                agentsOfRole = sb.ToString(); // Convert the StringBuilder content to a string
            }

            return agentsOfRole;
        }


        // Create Role
        [Route("CreateRole")]
        [HttpPost]
        public IActionResult CreateRole([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    return Ok(new { result = OutputResult_SUCC, details = CreateCRMRole(data) });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }

            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });

            }
        }

        private List<user_role> CreateCRMRole(JsonObject data)
        {
            // declare db table items
            user_role _user_role_item = new user_role();

            string roleName = (data["RoleName"] ?? "").ToString();
            string companies = (data["Companies"] ?? "").ToString();
            string categories = (data["Categories"] ?? "").ToString();
            string functions = (data["Functions"] ?? "").ToString();
            string roleStatus = (data["RoleStatus"] ?? "").ToString();


            // assign new user role record
            _user_role_item.RoleName = roleName;
            _user_role_item.Companies = companies;
            _user_role_item.Categories = categories;
            _user_role_item.Functions = functions;
            _user_role_item.RoleStatus = roleStatus;

            // add new user role record
            _scrme.user_roles.Add(_user_role_item);

            // save db changes
            _scrme.SaveChanges();

            int roleID = _user_role_item.RoleID;

            // obtain the new user role from table "user_role"
            List<user_role> _linq_user_role = (from _r in _scrme.user_roles
                                                         where _r.RoleID == roleID
                                                         select _r).ToList();

            return _linq_user_role;
        }


        // Update Role
        [Route("UpdateRole")]
        [HttpPut]
        public IActionResult UpdateRole([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    UpdateCRMRole(data);
                    return Ok(new { result = OutputResult_SUCC, details = "updated user role" });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }


        private void UpdateCRMRole(JsonObject data)
        {
            int roleID = Convert.ToInt32((data["RoleID"] ?? "-1").ToString());

            // declare a dictionary object where key = fieldName, value = fieldValue
            Dictionary<string, string> fieldsToBeUpdatedDict = new Dictionary<string, string>();

            // iterate through data and add the field names and field values to the dictionary
            foreach (var item in data)
            {
                // obtain form parameters to local variables
                string fieldName = item.Key;
                string fieldValue = item.Value?.ToString() ?? string.Empty;

                if (fieldName != "RoleID" && fieldName != "Agent_Id" && fieldName != "Token")
                {
                    fieldsToBeUpdatedDict.Add(fieldName, fieldValue); // add non-RoleID fields to the Dictionary
                }
            }

            // obtain single role record based on the role id
            user_role? _role = (from _r in _scrme.user_roles
                                         where _r.RoleID == roleID
                                         select _r).SingleOrDefault<user_role>();

            // if there is at least 1 role
            if (_role != null)
            {
                // iterate through the dictionary
                foreach (KeyValuePair<string, string> fields in fieldsToBeUpdatedDict)
                {
                    // find the column name that matches with the field name in dictionary
                    PropertyInfo? properInfo = _role.GetType().GetProperty(fields.Key);

                    // set the field value
                    properInfo?.SetValue(_role, (fields.Value == null ? string.Empty : fields.Value));
                }

                _scrme.SaveChanges();
            }

        }


        // Retrieve login details
        [Route("GetLogin")]
        [HttpPost]
        public IActionResult GetLogin([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    return Content(GetLoginInfo().ToString(), "application/json; charset=utf-8", Encoding.UTF8);
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
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
                new JProperty("result", OutputResult_SUCC),
                new JProperty("details", _agent_list)
            };

            // return all results in json format
            return allJsonResults;
        }


        // Change Password
        [Route("ChangePassword")]
        [HttpPut]
        public IActionResult ChangePassword([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    string changeResult = ChangeUserPassword(data);
                    return Ok(new { result = OutputResult_SUCC, details = changeResult });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }
            }
            catch (Exception)
            {
                return Ok(new { result = "fail", details = "cannot change password" });
            }
        }

        private string ChangeUserPassword(JsonObject data)
        {
            string sellerId = (data["SellerID"] ?? "").ToString();
            string oldPassword = (data["Old_Password"] ?? "").ToString();
            string password = (data["Password"] ?? "").ToString();

            // obtain single user record based on the agent id
            agentinfo? _agent = (from _a in _scrme.agentinfos
                                           where _a.SellerID == sellerId
                                           select _a).SingleOrDefault<agentinfo>();

            // agent exists
            if (_agent != null)
            {
                if (_agent.Password == oldPassword)
                {
                    int pwd_reuse_times = GetPasswordReUseTimes();

                    IQueryable<String> _h;

                    _h = (from _a in _scrme.password_logs
                          where _a.AgentID == _agent.AgentID
                          orderby _a.Created_Time descending
                          select _a.Password).Take(pwd_reuse_times);

                    List<String> _h_list = _h.ToList<String>();

                    bool isDup;

                    isDup = _h_list.Contains(password);

                    if (isDup)
                    {
                        return "The new password has been used before.";
                    }
                    else
                    {
                        // assign the updated values to the row
                        _agent.Password = password;
                        int passwordChangeFrequency = GetPasswordChangeFrequency();
                        _agent.ExpiryDate = DateTime.Now.AddDays(passwordChangeFrequency); // extend the account expiry date

                        _agent.LastLoginDate = DateTime.Now;


                        _scrme.SaveChanges(); // save changes to db

                        // Insert password log
                        password_log _new_pw_item = new password_log();

                        _new_pw_item.Password = password;
                        _new_pw_item.AgentID = _agent.AgentID;
                        _new_pw_item.Created_Time = DateTime.Now;

                        _scrme.password_logs.Add(_new_pw_item);

                        _scrme.SaveChanges();


                        return "password is changed.";
                    }

                }
                else
                {
                    return "The existing password does not match with our record.";
                }
            }
            else
            {
                return "Invalid seller ID.";
            }
        }

        private int GetPasswordReUseTimes()
        {
            config? _config = (from _c in _scrme.configs
                               where _c.P_Name == "PasswordReUseTimes"
                               select _c).SingleOrDefault<config>();

            if (_config == null)
            {
                return 5;
            }
            else
            {
                return Convert.ToInt32(_config.P_Value);
            }
        }



        // Get Floor Plan
        [Route("GetFloorPlan")]
        [HttpPost]
        public IActionResult GetFloorPlan([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            string ftype = (data["F_Type"] ?? "").ToString();
            int fid = Convert.ToInt32((data["F_Id"] ?? "-1").ToString());

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    return Content(GetCRM_FloorPlan(ftype, fid).ToString(), "application/json; charset=utf-8", Encoding.UTF8);
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }

            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });

            }
        }

        private JObject GetCRM_FloorPlan(string ftype, int fid)
        {
            // Default to an empty enumerable to avoid null assignment
            IEnumerable<dynamic> _info = Enumerable.Empty<dynamic>();

            if (ftype == "full")
            {
                _info = (from _m in _scrme.floor_plans
                         where _m.F_Id == fid
                         select _m);
            }
            else if (ftype == "simple")
            {
                _info = (from _m in _scrme.floor_plans
                         select new
                         {
                             F_Id = _m.F_Id,
                             Name = _m.Name,
                             Ordering = _m.Ordering,
                             Status = _m.Status
                         }).Take(500);
            }

            // declare a json object to contain all rows of data
            //  JObject allJsonResults = new JObject(); //old
            JObject allJsonResults;

            // declare a list of json objects containing the each row of data
            List<JObject> jsonList = new List<JObject>();

            // return results in list 
            if (_info.Any())
            {
                // iterate through 
                foreach (var _item in _info)
                {
                    // declare a temp json object to store each 
                    JObject tempJson = new JObject();
                    tempJson.RemoveAll(); // clear the object

                    // iterate each column 
                    foreach (PropertyInfo property in _item.GetType().GetProperties())
                    {

                        tempJson.Add(new JProperty(property.Name, property.GetValue(_item)));
                    }

                    jsonList.Add(tempJson); // add the temp result to the list
                }

                // add the log list to jobject
                allJsonResults = new JObject()
                {
                     new JProperty("result", OutputResult_SUCC),
                     new JProperty("details", jsonList)
                };

            }
            else
            {
                allJsonResults = new JObject()
                {
                     new JProperty("result", OutputResult_SUCC),
                     new JProperty("details", jsonList)
                };
            }

            return allJsonResults;
        }


        // Add Floor Plan
        [Route("AddFloorPlan")]
        [HttpPost]
        public IActionResult AddFloorPlan([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    addCRM_FloorPlan(data);
                    return Ok(new { result = OutputResult_SUCC, details = "inserted" });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private void addCRM_FloorPlan(JsonObject data)
        {
            int agentId = Convert.ToInt32((data["Agent_Id"] ?? "-1").ToString());

            floor_plan _new_fp_item = new floor_plan();

            _new_fp_item.Name = (data["Name"] ?? "").ToString();
            _new_fp_item.Ordering = Convert.ToInt32((data["Ordering"] ?? "-1").ToString());
            _new_fp_item.Value = (data["Value"] ?? "").ToString();
            _new_fp_item.Background = (data["Background"] ?? "").ToString();
            _new_fp_item.Style = (data["Style"] ?? "").ToString();
            _new_fp_item.Remarks = (data["Remarks"] ?? "").ToString();
            _new_fp_item.Status = "Active";

            _new_fp_item.Created_By = agentId;
            _new_fp_item.Created_Time = DateTime.Now;
            _new_fp_item.Updated_By = agentId;
            _new_fp_item.Updated_Time = DateTime.Now;

            _scrme.floor_plans.Add(_new_fp_item);

            _scrme.SaveChanges();

        }


        // Update Floor Plan
        [Route("UpdateFloorPlan")]
        [HttpPut]
        public IActionResult UpdateFloorPlan([FromBody] JsonObject data)
        {
            string token = (data[InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (Authenticated(token, tk_agentId))
                {
                    updateCRM_FloorPlan(data);
                    return Ok(new { result = OutputResult_SUCC, details = "updated" });
                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private void updateCRM_FloorPlan([FromBody] dynamic data)
        {
            int fID = Convert.ToInt32((data["F_Id"] ?? "-1").ToString());
            int agentId = Convert.ToInt32((data["Agent_Id"] ?? "-1").ToString());

            var _ss = (from _c in _scrme.floor_plans
                       where _c.F_Id == fID
                       select _c).SingleOrDefault<floor_plan>();

            // exists in table
            if (_ss != null)
            {
                _ss.Name = (data["Name"] ?? "").ToString();
                _ss.Ordering = Convert.ToInt32((data["Ordering"] ?? "-1").ToString());
                _ss.Value = (data["Value"] ?? "").ToString();
                _ss.Background = (data["Background"] ?? "").ToString();
                _ss.Style = (data["Style"] ?? "").ToString();
                _ss.Remarks = (data["Remarks"] ?? "").ToString();
                _ss.Status = (data["Status"] ?? "").ToString();

                _ss.Updated_By = agentId;
                _ss.Updated_Time = DateTime.Now;

                _scrme.SaveChanges();

            }

        }


        // Upload Photo
        [Route("UploadPhoto")]
        [HttpPost]
        public async Task<IActionResult> UploadPhoto()
        {
            try
            {
                string token = string.Empty;
                string tk_agentId = string.Empty;

                // Access the HTTP context directly from the ControllerBase
                var file = Request.Form.Files[0];

                int agentId = 0;

                foreach (var key in Request.Form.Keys)
                {
                    if (key == "To_Change_Id")
                    {
                        agentId = Convert.ToInt32(Request.Form[key]);
                    }
                    else if (key == "Agent_Id")
                    {
                        tk_agentId = Convert.ToString(Request.Form[key]);
                    }
                    else if (key == "Token")
                    {
                        token = Request.Form[key];
                    }
                }



                if (Authenticated(token, tk_agentId))
                {

                    if (agentId == 0) // cannot obtain agent id
                    {
                        return Ok(new { result = "fail", details = "Invalid Parameters." });
                    }
                    else
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream); // Read file asynchronously
                            byte[] photo = memoryStream.ToArray();
                            string photoType = file.ContentType;

                            // Save the photo and obtain the save status
                            string saveStatus = SaveCRM_AgentPhoto(agentId, photo, photoType);

                            if (saveStatus == "success")
                            {
                                return Ok(new { result = OutputResult_SUCC, details = "" });
                            }
                            else
                            {
                                return Ok(new { result = "fail", details = "No such record." });
                            }
                        }
                    }

                }
                else
                {
                    return Ok(new { result = "fail", details = Not_Auth_Desc });
                }
            }
            catch (Exception err)
            {
                return Ok(new { result = "fail", details = err.Message });
            }
        }

        private string SaveCRM_AgentPhoto(int agentId, byte[] photo, string fileType)
        {
            // obtain the row of data with the given agent id
            agentinfo? _agent = (from _a in _scrme.agentinfos
                                           where _a.AgentID == agentId
                                           select _a).SingleOrDefault();
            if (_agent == null)
            {
                return "fail";
            }
            else // contact exists
            {
                _agent.Photo = photo; // assign the file to Photo column in table
                _agent.Photo_Type = fileType; // assign the file type to Photo_Type column
                _scrme.SaveChanges(); // save to database

                return "success";
            }
        }





    }

}
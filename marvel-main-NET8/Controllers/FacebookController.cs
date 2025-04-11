using marvel_main_NET8.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

namespace marvel_main_NET8.Controllers
{
    //[Route("api/[controller]")]
    [Route("api")]
    [ApiController]
    public class FacebookController : ControllerBase
    {
        private readonly ScrmDbContext _scrme;

        private readonly string rootPath;

        public FacebookController(IConfiguration iConfig, ScrmDbContext context)
        {
            rootPath = iConfig.GetValue<string>("Facebook_Media_path") ?? "";

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


        // Get FaceBookPost Content
        [Route("GetFaceBookPostContent")]
        [HttpPost]
        public IActionResult GetFaceBookPostContent([FromBody] JsonObject data)
        {
            string token = (data[AppInp.InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[AppInp.InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (ValidateClass.Authenticated(token, tk_agentId))
                {

                    int ticketId = Convert.ToInt32((data["Ticket_Id"] ?? "-1").ToString());


                    return Content(GetCRM_FacebookPostContent(ticketId).ToString(), "application/json; charset=utf-8", Encoding.UTF8);
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

        private JObject GetCRM_FacebookPostContent(int ticketId)
        {
            // declare a json object to contain the details of the data
            List<JObject> detailsJson = new List<JObject>();

            IQueryable<facebook_post> _posts = (from _p in _scrme.facebook_posts select _p);

            if (ticketId != -1)
            {
                _posts = _posts.Where(_p => _p.Ticket_Id == ticketId);
            }

            if (_posts.Count() > 0)
            {
                // iterate through each row of data
                foreach (facebook_post _post_item in _posts)
                {
                    // declare a temp json object to store each column of data
                    JObject tempJson = new JObject();

                  //  tempJson.RemoveAll(); // clear the temp object

                    // iterate each column of _post_content
                    foreach (PropertyInfo property in _post_item.GetType().GetProperties())
                    {
                        switch (property.Name)
                        {
                            case "Fb_Id":
                            case "Ticket_Id":
                            case "Details":
                            case "Media_Type":
                            case "Media_Link":
                                {
                                    tempJson.Add(new JProperty(property.Name, property.GetValue(_post_item)));
                                }
                                break;
                            default:
                                // For others, skip
                                break;
                        }
                    }
                    detailsJson.Add(tempJson); // add the temp result to the list
                }


            }


            // include the list in the JSON object of the final result
            JObject jsonResults = new JObject()
            {
                new JProperty("result", AppOutp.OutputResult_SUCC),
                new JProperty("details", detailsJson)
            };

            // return all results in json format
            return jsonResults;

        }


        // Create FacebookPost Content
        [Route("CreateFacebookPostContent")]
        [HttpPost]
        public IActionResult CreateFacebookPostContent([FromBody] JsonObject data)
        {
            string token = (data[AppInp.InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[AppInp.InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (ValidateClass.Authenticated(token, tk_agentId))
                {
                    List<facebook_post> _new_post = CreateCRM_FacebookPostContent(data);

                    return Ok(new { result = AppOutp.OutputResult_SUCC, details = _new_post });
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

        private List<facebook_post> CreateCRM_FacebookPostContent(JsonObject data)
        {
            // declare db table items
            facebook_post _post_item = new facebook_post();

            int ticketId = Convert.ToInt32((data["Ticket_Id"] ?? "-1").ToString());
            int agentId = Convert.ToInt32((data["Agent_Id"] ?? "-1").ToString());
            string details = (data["Details"] ?? "").ToString();

            // assign new record
            _post_item.Ticket_Id = ticketId;
            _post_item.Created_By = agentId;
            _post_item.Created_Time = DateTime.Now;
            _post_item.Updated_By = agentId;
            _post_item.Updated_Time = DateTime.Now;
            _post_item.Details = details;

            // add new record
            _scrme.facebook_posts.Add(_post_item);

            // save db changes
            _scrme.SaveChanges();


            // obtain the new from table "facebook_post"
            List<facebook_post> _new_post = (from _f in _scrme.facebook_posts
                                                       where _f.Fb_Id == _post_item.Fb_Id
                                                       select _f).ToList();


            return _new_post;
        }


        // Update FacebookPost Content
        [Route("UpdateFacebookPostContent")]
        [HttpPut]
        public IActionResult UpdateFacebookPostContent([FromBody] JsonObject data)
        {
            string token = (data[AppInp.InputAuth_Token] ?? "").ToString();
            string tk_agentId = (data[AppInp.InputAuth_Agent_Id] ?? "").ToString();

            try
            {
                if (ValidateClass.Authenticated(token, tk_agentId))
                {
                    UpdateCRM_FacebookPostContent(data);
                    return Ok(new { result = AppOutp.OutputResult_SUCC, details = "updated facebook post" });
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

        private void UpdateCRM_FacebookPostContent(JsonObject data)
        {
            int fbId = Convert.ToInt32((data["Fb_Id"] ?? "-1").ToString());
            int ticketId = Convert.ToInt32((data["Ticket_Id"] ?? "-1").ToString());
            int agentId = Convert.ToInt32((data["Agent_Id"] ?? "-1").ToString());

            string details = (data["Details"] ?? "").ToString();
            string mediaRemoved = (data["Media_Removed"] ?? "N").ToString();


            // obtain single post record based on the fb id
            facebook_post? _post_item = (from _f in _scrme.facebook_posts
                                                  where _f.Fb_Id == fbId
                                                  select _f).SingleOrDefault<facebook_post>();

            // if there is at least 1 post
            if (_post_item != null)
            {
                // decide if it's update with ticket id or update w/o ticket id
                if (_post_item.Ticket_Id == ticketId)
                {
                    // assign the updated values to the row
                    _post_item.Updated_By = agentId;
                    _post_item.Updated_Time = DateTime.Now;
                    _post_item.Details = details;

                    if (mediaRemoved == "Y")
                    {
                        _post_item.Media_Type = string.Empty;
                        _post_item.Media_Link = string.Empty;
                    }

                }
                else
                {
                    // add a new row using the data above
                    // declare db table items
                    facebook_post _new_post_item = new facebook_post();

                    // assign new post record
                    _new_post_item.Ticket_Id = ticketId;
                    _new_post_item.Created_By = _post_item.Created_By;
                    _new_post_item.Created_Time = _post_item.Created_Time;
                    _new_post_item.Updated_By = agentId;
                    _new_post_item.Updated_Time = DateTime.Now;
                    _new_post_item.Details = details;
                    _new_post_item.Media_Link = _post_item.Media_Link;
                    _new_post_item.Media_Type = _post_item.Media_Type;

                    if (mediaRemoved == "Y")
                    {
                        _new_post_item.Media_Link = string.Empty;
                        _new_post_item.Media_Type = string.Empty;
                    }

                    // delete the old row
                    _scrme.facebook_posts.Remove(_post_item);

                    // add the new row
                    _scrme.facebook_posts.Add(_new_post_item);
                }
                _scrme.SaveChanges(); // save changes to db
            }
        }


        // Upload Facebook Media
        [Route("UploadFacebookMedia")]
        [HttpPost]
        public IActionResult UploadFacebookMedia()
        {
            try
            {
                string token = string.Empty;
                string tk_agentId = string.Empty;

                int ticketId = 0;
                int agentId = 0;

                if (Request.Form.Files.Count == 0)
                {
                    return Ok(new { result = AppOutp.OutputResult_FAIL, details = "No file was uploaded." });
                }
                var file = Request.Form.Files[0];

                foreach (var key in Request.Form.Keys)
                {
                    if (key == "Ticket_Id")
                    {
                        ticketId = Convert.ToInt32(Request.Form[key]);
                    }
                    else if (key == "Agent_Id")
                    {
                        agentId = Convert.ToInt32(Request.Form[key]);

                        tk_agentId = Convert.ToString(Request.Form[key]);
                    }
                    else if (key == "Token")
                    {
                        // token = Request.Form[key]; //old
                        token = Convert.ToString(Request.Form[key]) ?? string.Empty;

                    }
                }


                if (ValidateClass.Authenticated(token, tk_agentId))
                {
                    if (ticketId == 0) // cannot obtain ticket id
                    {
                        return Ok(new { result = AppOutp.OutputResult_FAIL, details = "Invalid Parameters." });
                    }
                    else
                    {

                        string mediaType = file.ContentType;

                        // extract only the filename
                        string fileName = Path.GetFileName(file.FileName);

                        // decide the whole file path
                        string filePath = Path.Combine(rootPath, fileName);
                        
                        // Save the file to the server
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        string mediaLink = ".\\fb_media\\" + fileName;

                        SaveCRM_FbMedia(ticketId, agentId, mediaType, mediaLink);

                        return Ok(new
                        {
                            result = AppOutp.OutputResult_SUCC,
                            details = new
                            {
                                Media_Type = mediaType,
                                Media_Link = mediaLink
                            }
                        });

                    }

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

        private void SaveCRM_FbMedia(int ticketId, int agentId, string mediaType, string mediaLink)
        {
            // obtain the row of data with the given ticket id
            facebook_post? _post = (from _f in _scrme.facebook_posts
                                             where _f.Ticket_Id == ticketId
                                             select _f).SingleOrDefault();

            if (_post != null)
            {
                // assign the parameters to db columns
                _post.Media_Type = mediaType;
                _post.Media_Link = mediaLink;
                _post.Updated_By = agentId;
                _post.Updated_Time = DateTime.Now;
                _scrme.SaveChanges(); // save to database
            }
        }



    }
}

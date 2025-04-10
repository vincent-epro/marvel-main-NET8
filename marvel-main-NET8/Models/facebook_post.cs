using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class facebook_post
{
    public int Fb_Id { get; set; }

    public int Ticket_Id { get; set; }

    public int? Created_By { get; set; }

    public DateTime? Created_Time { get; set; }

    public int? Updated_By { get; set; }

    public DateTime? Updated_Time { get; set; }

    public string? Details { get; set; }

    public string? Media_Type { get; set; }

    public string? Media_Link { get; set; }
}

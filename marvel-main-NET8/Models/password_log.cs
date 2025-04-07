using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class password_log
{
    public int P_Id { get; set; }

    public int? AgentID { get; set; }

    public string? Password { get; set; }

    public DateTime? Created_Time { get; set; }
}

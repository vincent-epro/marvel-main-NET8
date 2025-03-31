using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class agentinfo
{
    public int ColId { get; set; }

    public int AgentID { get; set; }

    public string? AgentName { get; set; }

    public string? Password { get; set; }

    public int? LevelID { get; set; }

    public string SellerID { get; set; } = null!;

    public int? Counter { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public string? Account_status { get; set; }

    public byte[]? Photo { get; set; }

    public string? Photo_Type { get; set; }

    public string? Email { get; set; }
}

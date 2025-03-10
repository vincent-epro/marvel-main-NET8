using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class Agentinfo
{
    public int ColId { get; set; }

    public int AgentId { get; set; }

    public string? AgentName { get; set; }

    public string? Password { get; set; }

    public int? LevelId { get; set; }

    public string SellerId { get; set; } = null!;

    public int? Counter { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public string? AccountStatus { get; set; }

    public byte[]? Photo { get; set; }

    public string? PhotoType { get; set; }

    public string? Email { get; set; }
}

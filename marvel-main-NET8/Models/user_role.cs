using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class user_role
{
    public int RoleID { get; set; }

    public string? RoleName { get; set; }

    public string? Companies { get; set; }

    public string? Categories { get; set; }

    public string? Functions { get; set; }

    public string? RoleStatus { get; set; }
}

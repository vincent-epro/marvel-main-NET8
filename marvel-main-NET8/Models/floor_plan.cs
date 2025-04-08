using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class floor_plan
{
    public int F_Id { get; set; }

    public string? Name { get; set; }

    public int? Ordering { get; set; }

    public string? Value { get; set; }

    public string? Background { get; set; }

    public string? Style { get; set; }

    public string? Remarks { get; set; }

    public string? Status { get; set; }

    public int? Created_By { get; set; }

    public DateTime? Created_Time { get; set; }

    public int? Updated_By { get; set; }

    public DateTime? Updated_Time { get; set; }
}

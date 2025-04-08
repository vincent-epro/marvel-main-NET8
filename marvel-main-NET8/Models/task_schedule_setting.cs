using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class task_schedule_setting
{
    public int S_Id { get; set; }

    public string? Service { get; set; }

    public string? Display_Message { get; set; }

    public string? Schedule_Type { get; set; }

    public DateTime? Schedule_Time { get; set; }

    public string? Status { get; set; }

    public int? Created_By { get; set; }

    public DateTime? Created_Time { get; set; }

    public int? Updated_By { get; set; }

    public DateTime? Updated_Time { get; set; }
}

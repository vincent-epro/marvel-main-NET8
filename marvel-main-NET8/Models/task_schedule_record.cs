using System;
using System.Collections.Generic;

namespace marvel_main_NET8.Models;

public partial class task_schedule_record
{
    public int R_Id { get; set; }

    public string? Service { get; set; }

    public string? Display_Message { get; set; }

    public string? Schedule_Type { get; set; }

    public int? S_Id { get; set; }

    public DateTime? Alert_Time { get; set; }

    public string? Comment { get; set; }

    public int? Handle_By { get; set; }

    public DateTime? Handle_Time { get; set; }
}

using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class LoaiRuiRo
{
    public int MaLoaiRuiRo { get; set; }

    public string TenLoaiRuiRo { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<DuBaoRuiRoAI> DuBaoRuiRoAIs { get; set; } = new List<DuBaoRuiRoAI>();

    public virtual ICollection<RuiRoDuAn> RuiRoDuAns { get; set; } = new List<RuiRoDuAn>();
}

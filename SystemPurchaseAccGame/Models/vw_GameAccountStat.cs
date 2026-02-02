using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SystemPurchaseAccGame.Models;

[Keyless]
public partial class vw_GameAccountStat
{
    public int GameId { get; set; }

    public int? TongNick { get; set; }

    public int? TongNickHienTai { get; set; }

    public int? DaBan { get; set; }
}

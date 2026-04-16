using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace TransportLogistic.Models;

public partial class Order
{
    public int Id { get; set; }

    public string User { get; set; } = null!;

    public int Trip { get; set; }

    public int SeatNumber { get; set; }

    public decimal Price { get; set; }

    public string Stasus { get; set; } = null!;

    public virtual Trip TripNavigation { get; set; } = null!;

    public virtual IdentityUser UserNavigation { get; set; } = null!;
}

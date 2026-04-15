using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using YourProject.Domain.Enums;

namespace TransportLogistic.Models;

public partial class Order
{
    public int Id { get; set; }

    public string User { get; set; } = null!;

    public int Trip { get; set; }

    public int SeatNumber { get; set; }

    public decimal Price { get; set; }

    public OrderStatus Stasus { get; set; } 

    public virtual IdentityUser UserNavigation { get; set; } = null!;
}

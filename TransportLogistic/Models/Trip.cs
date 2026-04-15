using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace TransportLogistic.Models;

public partial class Trip
{
    public int Id { get; set; }

    public int Route { get; set; }

    public DateTime DepatureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int Transport { get; set; }

    public string Driver { get; set; } = null!;

    public string? Conductor { get; set; }

    public virtual IdentityUser? ConductorNavigation { get; set; }

    public virtual IdentityUser DriverNavigation { get; set; } = null!;

    public virtual Route RouteNavigation { get; set; } = null!;

    public virtual Transport TransportNavigation { get; set; } = null!;
}

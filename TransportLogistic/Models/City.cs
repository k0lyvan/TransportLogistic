using System;
using System.Collections.Generic;

namespace TransportLogistic.Models;

public partial class City
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Region { get; set; } = null!;

    public virtual ICollection<Route> RouteStartNavigations { get; set; } = new List<Route>();

    public virtual ICollection<Route> RouteStopNavigations { get; set; } = new List<Route>();
}

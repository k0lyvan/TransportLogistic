using System;
using System.Collections.Generic;
using YourProject.Domain.Enums;

namespace TransportLogistic.Models;

public partial class Route
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public Citys Start { get; set; } 

    public Citys Stop { get; set; } 

    public int? Distance { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

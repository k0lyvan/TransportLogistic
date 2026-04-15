using System;
using System.Collections.Generic;

namespace TransportLogistic.Models;

public partial class Transport
{
    public int Id { get; set; }

    public string Model { get; set; } = null!;

    public int Capacity { get; set; }

    public string CarNumber { get; set; } = null!;

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

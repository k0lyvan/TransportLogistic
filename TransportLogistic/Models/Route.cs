using System;
using System.Collections.Generic;

namespace TransportLogistic.Models;

public partial class Route
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Start { get; set; }

    public int Stop { get; set; }

    public int? Distance { get; set; }

    public virtual City StartNavigation { get; set; } = null!;

    public virtual City StopNavigation { get; set; } = null!;

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

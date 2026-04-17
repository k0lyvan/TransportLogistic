namespace TransportLogistic.Models
{
    public class TicketViewModel
    {
        public int OrderId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string StartCity { get; set; } = string.Empty;
        public string StopCity { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int SeatNumber { get; set; }
        public decimal Price { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string TransportModel { get; set; } = string.Empty;
        public string CarNumber { get; set; } = string.Empty;
    }
}

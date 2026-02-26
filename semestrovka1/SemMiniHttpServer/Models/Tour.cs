using System;

namespace MiniHttpServer.Models
{
    public class Tour
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string Country { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DepartureDate { get; set; }
        public int Nights { get; set; }
        public int Rating { get; set; }
        public string Location { get; set; }
    }
}

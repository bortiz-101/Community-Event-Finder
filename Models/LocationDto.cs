namespace Community_Event_Finder.Models
{
    public class LocationDto
    {
        public int LocationId { get; set; }
        public string VenueName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Zip { get; set; } = "";
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public static LocationDto FromLocation(Location location)
        {
            return new LocationDto
            {
                LocationId = location.LocationId,
                VenueName = location.VenueName,
                Address = location.Address,
                City = location.City,
                State = location.State,
                Zip = location.Zip,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };
        }
    }
}

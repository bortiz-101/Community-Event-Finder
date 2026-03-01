using Microsoft.AspNetCore.Mvc;

namespace CommunityEventsApp.Controllers
{
    [ApiController]
    [Route("api/geo")]
    public class GeoController : ControllerBase
    {
        private const double EarthRadiusMiles = 3958.8;

        [HttpGet("distance")]
        public IActionResult Distance(
            double lat1, double lon1,
            double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) *
                Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Ok(EarthRadiusMiles * c);
        }
    }
}

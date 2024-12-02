using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using RoteWiseProject.Dto;
using RoteWiseProject.Services;
namespace RoteWiseProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HotelsController:ControllerBase
    {
        private readonly HotelService _hotelService;
        private readonly ILogger<HotelsController> _logger;

        public HotelsController(HotelService hotelService, ILogger<HotelsController> logger)
        {
            _hotelService = hotelService;
            _logger = logger;
        }

        [HttpGet("search-hotels")]
        public async Task<IActionResult> SearchHotels(string city)
        {
            try
            {
                var hotelPrices = await _hotelService.GetAllHotelsAndPricesAsync(city);
                return Ok(hotelPrices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching hotels");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
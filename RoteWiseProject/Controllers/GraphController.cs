using RoteWiseProject.Dto;
using Microsoft.AspNetCore.Mvc;
using RoteWiseProject.Controllers.Models;
using RoteWiseProject.Services;
using System.Threading.Tasks;
namespace RoteWiseProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GraphController : ControllerBase
    {
        private readonly Neo4jService _neo4jService;
        private readonly FlightService _flightService;
        private readonly HotelService _hotelService;

        public GraphController(Neo4jService neo4jService, FlightService flightService, HotelService hotelService)
        {
            _neo4jService = neo4jService;
            _flightService = flightService;
            _hotelService = hotelService;
        }

        [HttpPost("create-graph")]
        public async Task<IActionResult> CreateGraph([FromBody] GraphRequestModel request)
        {
            try
            {
                var flightsJson = await _flightService.GetFlightsToDestinationAsync(request.Origin, request.Destination, request.DateStart, request.adult);
                var flights = await _flightService.ParseFlightsAsync(flightsJson, request.Destination);
                var flightsEndJson = await _flightService.GetFlightsToDestinationAsync(request.Destination, request.Origin, request.DateEnd, request.adult);
                var flightsEnd = await _flightService.ParseFlightsAsync(flightsEndJson, request.Origin);
                var hotels = await _hotelService.GetAllHotelsAndPricesAsync(request.Destination);
                await _neo4jService.CreateGraphAsync(request, flights, flightsEnd, hotels);
                return Ok("Graph created successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //[HttpGet("get-graph")]
        //public async Task<IActionResult> GetGraphData(string city)
        //{
        //    var nodes=await _neo4jService.GetGraphDataAsync(city);
        //    return Ok(nodes);
        //}
        
    }
}


//public async Task<(List<Flight>, List<Hotel>, List<Relationship>)> GetGraphDataAsync(string country)

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RoteWiseProject.Services;
using RoteWiseProject.Dto;
using RoteWiseProject.Controllers.Models;
namespace RoteWiseProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DijkstraController : ControllerBase
    {
        private readonly DijkstraAlgorithm _dijkstraAlgorithmService;

        public DijkstraController(Neo4jService neo4jService, DijkstraAlgorithm dijkstraAlgorithmService)
        {
            _dijkstraAlgorithmService = dijkstraAlgorithmService;
        }

        [HttpGet("shortest-path")]
        public async Task<IActionResult> GetShortestPath([FromQuery] GraphRequestModel request) 
        {
            //Func<Hotel, bool> hotelConstraint = hotel => hotel.Price <= 150;
            //Func<Flight, bool> flightConstraint = flight => flight.Price <= 300;

            Func<Flight, bool> flightConstraint = flight => true; 
            Func<Hotel, bool> hotelConstraint = hotel => true; 

            var path = await _dijkstraAlgorithmService.FindShortestPath(request, flightConstraint, hotelConstraint);
            if (path == null || path.Count == 0)
            {
                return NotFound("No path found matching the criteria.");
            }
            return Ok(path);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using RoteWiseProject.Dto;
using RoteWiseProject.Services;
using System.Diagnostics.Metrics;
using RoteWiseProject.Controllers.Models;
using Microsoft.Extensions.Logging;


namespace RoteWiseProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TravelController : ControllerBase
    {
        private readonly FlightService _flightService;
        private readonly Neo4jService _neo4jService;

        public TravelController(FlightService flightService, Neo4jService neo4jService)
        {
            _flightService = flightService;
            _neo4jService = neo4jService;
        }

        public int adult = 2;// { get; private set; }

        //[HttpGet("search-destinations")]
        //public async Task<IActionResult> SearchDestinations(string origin, string date)
        //{
        //    var destinationsJson = await _flightService.GetFlightsAsync(origin, date, adult);
        //    var destinationsObject = JObject.Parse(destinationsJson);

        //    var destinationArray = destinationsObject["data"] as JArray;
        //    if (destinationArray == null)
        //    {
        //        return StatusCode(500, "Invalid response format: 'data' array not found.");
        //    }
        //    var destinationList=new List<Flight>();

        //    return Ok(destinationList);
        //}


        [HttpGet("search-destinations")]
        public async Task<IActionResult> SearchDestinations(string origin)
        {
            var destinationsJson = await _flightService.GetFlightsAsync(origin);
            var destinationsObject = JObject.Parse(destinationsJson);

            var destinationArray = destinationsObject["data"] as JArray;
            if (destinationArray == null)
            {
                return StatusCode(500, "Invalid response format: 'data' array not found.");
            }

            var destinationList = new List<string>();

            //על הקריאה שנותנת 4 יעדים אופטימלים AMADOUS לבדוק את האפשרות  
            foreach (var flight in destinationArray)
            {
                var dest = flight["destination"];


                if (dest != null)
                    destinationList.Add((string)dest);

            }
            return Ok(destinationList);
        }




        [HttpGet("search-flights")]
        public async Task<IActionResult> SearchFlights(string origin, string destination, string date, int adult)
       // public async Task<IActionResult> SearchFlights([FromBody]FlightModel f)

        {
            // var flightsJson = await _flightService.GetFlightsToDestinationAsync(f.Origin,f.Destination,f.Date, adult);
            try 
            {
                var flightsJson = await _flightService.GetFlightsToDestinationAsync(origin,destination,date, adult);
                var flights = _flightService.ParseFlightsAsync(flightsJson, destination);

                if (flights == null )
                {
                    Console.WriteLine("No flights found for the given search criteria.");
                    return NotFound("No flights found.");
                }
               // return Ok(new { RawJson = flightsJson, ParsedFlights = flights });

                  return Ok(flights);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchFlights: {ex.Message}");
                return StatusCode(500, "Internal server error");
                //Console.WriteLine($"Exception in SearchFlights: {ex.Message}");
                //return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
            //var flightsObject = JObject.Parse(flightsJson);

            //var flightsArray = flightsObject["data"] as JArray;
            //if (flightsArray == null)
            //{
            //    return StatusCode(500, "Invalid response format: 'data' array not found.");
            //}

            //var flightList = new List<Flight>();
            //foreach (var flight in flightsArray)
            //{
            //    var id = flight["id"].ToString();
            //    var itinerary = flight["itineraries"][0];
            //    var segment = itinerary["segments"][0];
            //    var departure = segment["departure"];
            //    var arrival = segment["arrival"];
            //    var price = flight["price"]["grandTotal"].ToString();

            //    if (arrival["iataCode"].ToString() == destination)
            //    {
            //        flightList.Add(new Flight
            //        {
            //            Id = id,
            //            Origin = departure["iataCode"].ToString(),
            //            Destination = arrival["iataCode"].ToString(),
            //            Date = departure["at"].ToString(),
            //            Price = (double)decimal.Parse(price)
            //        });
            //    }

            //}




            //{
            //    flightList.Add(new Flight
            //    {
            //        Id = flight["id"].ToString(),
            //        Origin = origin,
            //        Destination = destination,
            //        Date = date,
            //        Price = double.Parse(flight["price"]["total"].ToString())
            //    });
            //}

            //i didnt check it yet
            //string country = "OSF";
            // await _neo4jService.CreateGraphAsync(country,flightList);
            //  await _neo4jService.CreateGraphAsync(origin,flightList);

       

        //[HttpGet("test-neo4j-connection")]
        //public async Task<IActionResult> TestNeo4jConnection()
        //{
        //    try
        //    {
        //        var success = await _neo4jService.TestConnectionAsync();
        //        return Ok(success ? "Connection successful!" : "Connection failed.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Connection failed: {ex.Message}");
        //    }
        //}
    }
}

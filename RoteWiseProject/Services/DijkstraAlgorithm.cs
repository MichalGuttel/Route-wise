using System;
using System.Collections.Generic;
using System.Linq;
using RoteWiseProject.Controllers.Models;
using RoteWiseProject.Dto;

namespace RoteWiseProject.Services
{
    public class DijkstraAlgorithm
    {
        private readonly Neo4jService _neo4jService;
        public DijkstraAlgorithm(Neo4jService neo4JService) 
        {
            _neo4jService = neo4JService;
        }





        public async Task<List<object>> FindShortestPath(GraphRequestModel request, Func<Flight, bool> flightConstraint, Func<Hotel, bool> hotelConstraint)
        {
            var (flights, hotels, relationships, returnFlights) = _neo4jService.GetGraphDataAsync(request.Origin).Result;

            var graph = new Dictionary<string, Dictionary<string, double>>();
            var nodes = new HashSet<string>();

            // Calculate number of nights
            var startDate = DateTime.Parse(request.DateStart);
            var endDate = DateTime.Parse(request.DateEnd);
            int numberOfNights = (endDate - startDate).Days;

            // Build the graph
            foreach (var relationship in relationships)
            {
                if (!graph.ContainsKey(relationship.FromFlight))
                    graph[relationship.FromFlight] = new Dictionary<string, double>();

                var hotel = hotels.FirstOrDefault(h => h.Id == relationship.ToHotel);
                if (hotel != null)
                {
                    double hotelCost = hotel.Price * numberOfNights;
                    graph[relationship.FromFlight][relationship.ToHotel] = hotelCost;
                }

                nodes.Add(relationship.FromFlight);
                nodes.Add(relationship.ToHotel);
            }

            // Add return flights to the graph
            foreach (var hotel in hotels)
            {
                if (!graph.ContainsKey(hotel.Id))
                    graph[hotel.Id] = new Dictionary<string, double>();

                foreach (var returnFlight in returnFlights.Where(rf => rf.Origin == hotel.CityCode && rf.Destination == request.Origin))
                {
                    graph[hotel.Id][returnFlight.Id] = returnFlight.Price;
                    nodes.Add(returnFlight.Id);
                }
            }

            var distances = new Dictionary<string, double>();
            var previous = new Dictionary<string, string>();
            var pq = new SortedDictionary<double, HashSet<string>>();

            foreach (var node in nodes)
            {
                distances[node] = double.PositiveInfinity;
                previous[node] = null;
            }

            // Start from the origin flight
            var startFlight = flights.First(f => f.Origin == request.Origin && f.Destination == request.Destination && flightConstraint(f));
            distances[startFlight.Id] = startFlight.Price;
            pq[startFlight.Price] = new HashSet<string> { startFlight.Id };

            while (pq.Count > 0)
            {
                var currentDistance = pq.Keys.First();
                var currentNode = pq[currentDistance].First();
                pq[currentDistance].Remove(currentNode);
                if (!pq[currentDistance].Any()) pq.Remove(currentDistance);

                if (graph.ContainsKey(currentNode))
                {
                    foreach (var neighbor in graph[currentNode])
                    {
                        var alt = distances[currentNode] + neighbor.Value;
                        if (alt < distances[neighbor.Key])
                        {
                            distances[neighbor.Key] = alt;
                            previous[neighbor.Key] = currentNode;

                            if (!pq.ContainsKey(alt))
                                pq[alt] = new HashSet<string>();
                            pq[alt].Add(neighbor.Key);
                        }
                    }
                }
            }

            // Find the cheapest valid path
            var cheapestPath = new List<object>();
            double cheapestTotalCost = double.PositiveInfinity;

            foreach (var returnFlight in returnFlights.Where(rf => rf.Destination == request.Origin && flightConstraint(rf)))
            {
                if (!distances.ContainsKey(returnFlight.Id)) continue;

                var totalCost = distances[returnFlight.Id];
                if (totalCost <= request.MaxPrice && totalCost < cheapestTotalCost)
                {
                    cheapestTotalCost = totalCost;
                    cheapestPath.Clear();

                    string current = returnFlight.Id;
                    while (current != null)
                    {
                        var node = flights.FirstOrDefault(f => f.Id == current) ??
                                   (object)hotels.FirstOrDefault(h => h.Id == current) ??
                                   returnFlights.FirstOrDefault(rf => rf.Id == current);

                        if (node == null) break;

                        if ((node is Flight f && flightConstraint(f)) ||
                            (node is Hotel h && hotelConstraint(h)))
                        {
                            cheapestPath.Insert(0, node);
                        }

                        current = previous[current];
                    }
                }
            }

            return cheapestPath;
        }



    }
}

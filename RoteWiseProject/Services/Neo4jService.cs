using System;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using RoteWiseProject.Services;
using Neo4j.Driver;
using RoteWiseProject.Dto;
using RoteWiseProject.Settings;
using RoteWiseProject.Controllers.Models;
namespace RoteWiseProject.Services
{
    public class Neo4jService : IDisposable
    {
        private readonly IDriver _driver;

        public Neo4jService(IConnectionSettings settings)
        {
            _driver = GraphDatabase.Driver(settings.Uri, settings.AuthToken);
        }
        public IAsyncSession GetSession()
        {
            return _driver.AsyncSession();
        }


        public async Task CreateGraphAsync(GraphRequestModel request, List<Flight> flights, List<Flight> returnFlights, List<Hotel> hotels)
        {
            using var session = _driver.AsyncSession();

            await session.WriteTransactionAsync(async tx =>
            {
                var sourceQuery = @"MERGE (s:Source {country: $country}) SET s.name = 'SOURCE' RETURN s";
                await tx.RunAsync(sourceQuery, new { country = request.Origin });

                foreach (var flight in flights)
                {
                    var flightQuery = @"
                    MERGE (f:Flight {id: $id})
                    SET f.origin = $origin, f.destination = $destination, f.date = $date, f.price = $price";
                    await tx.RunAsync(flightQuery, new
                    {
                        id = flight.Id,
                        origin = flight.Origin,
                        destination = flight.Destination,
                        date = flight.Date,
                        price = flight.Price
                    });

                    //MERGE (s)-[r:FLIGHT_PRICE {price: $price}]->(f)
                    var relationshipQuery = @"
                    MATCH (s:Source {country: $country}), (f:Flight {id: $id})
                    MERGE (s)-[r:FLIGHT_PRICE]->(f)
                    ON CREATE SET r.price = $price
                    RETURN r";
                    await tx.RunAsync(relationshipQuery, new
                    {
                        country = request.Origin,
                        id = flight.Id,
                        price = flight.Price
                    });

                    foreach (var hotel in hotels)
                    {
                        if (IsWithinRadius(hotel, flight.DestinationLatitude, flight.DestinationLongitude, request.Radius))
                        {
                            var hotelQuery = @"
                            MERGE (h:Hotel {id: $id})
                            SET h.name = $name, h.price = $price, h.cityCode = $cityCode, h.latitude = $latitude, h.longitude = $longitude";
                            await tx.RunAsync(hotelQuery, new
                            {
                                id = hotel.Id,
                                name = hotel.Name,
                                price = hotel.Price,
                                cityCode = hotel.CityCode,
                                latitude = hotel.Latitude,
                                longitude = hotel.Longitude
                            });

                            //MERGE (f)-[r:HOTEL_PRICE {price: $price}]->(h)
                            var hotelPriceQuery = @"
                            MATCH (f:Flight {id: $flightId}), (h:Hotel {id: $hotelId})
                            MERGE (f)-[r:HOTEL_PRICE]->(h)
                            ON CREATE SET r.price = $price
                            RETURN r";
                            await tx.RunAsync(hotelPriceQuery, new
                            {
                                flightId = flight.Id,
                                hotelId = hotel.Id,
                                price = hotel.Price
                            });


                            // Add return flights related to this hotel
                            foreach (var returnFlight in returnFlights)
                            {
                                if (returnFlight.Origin == request.Destination && returnFlight.Destination == request.Origin)  // Ensure only return flights with the correct origin and destination
                                {
                                    if (IsWithinRadius(hotel, returnFlight.OrigionLatitude, returnFlight.OrigionLongitude, request.Radius))
                                    {
                                        var returnFlightQuery = @"
                                        MERGE (rf:Flight {id: $id})
                                        SET rf.origin = $origin, rf.destination = $destination, rf.date = $date, rf.price = $price";
                                        await tx.RunAsync(returnFlightQuery, new
                                        {
                                            id = returnFlight.Id,
                                            origin = returnFlight.Origin,
                                            destination = returnFlight.Destination,
                                            date = returnFlight.Date,
                                            price = returnFlight.Price
                                        });

                                        //MERGE (h)-[r:RETURN_FLIGHT_PRICE {price: $price}]->(rf)
                                        var hotelReturnPriceQuery = @"
                                        MATCH (rf:Flight {id: $returnFlightId}), (h:Hotel {id: $hotelId})
                                        MERGE (h)-[r:RETURN_FLIGHT_PRICE]->(rf)
                                        ON CREATE SET r.price = $price
                                        RETURN r";
                                        await tx.RunAsync(hotelReturnPriceQuery, new
                                        {
                                            returnFlightId = returnFlight.Id,
                                            hotelId = hotel.Id,
                                            price = returnFlight.Price
                                        });
                                    }
                                }
                            }

                        }
                    }
                }
            });
        }




        public async Task<List<Flight>> GetFilteredFlightsAsync(string country)
        {
            using var session = _driver.AsyncSession();

            var query = @"
        MATCH (s:Source {country: $country})-[:FLIGHT_PRICE]->(f:Flight)
        RETURN f.id AS id, f.origin AS origin, f.destination AS destination, f.date AS date, f.price AS price";

            var result = await session.RunAsync(query, new { country });

            var flights = new List<Flight>();
            await result.ForEachAsync(record =>
            {
                flights.Add(new Flight
                {
                    Id = record["id"].As<string>(),
                    Origin = record["origin"].As<string>(),
                    Destination = record["destination"].As<string>(),
                    Date = record["date"].As<string>(),
                    Price = record["price"].As<double>()
                });
            });

            return flights;
        }



        public async Task UpdateGraphAsync(GraphRequestModel request, List<Flight> flights, List<Flight> returnFlights, List<Hotel> hotels)
        {
            using var session = _driver.AsyncSession();

            await session.WriteTransactionAsync(async tx =>
            {
                // עדכון  צומת המקור
                var sourceQuery = @"
                MERGE (s:Source {country: $country})
                SET s.lastUpdated = datetime()
                RETURN s";
                await tx.RunAsync(sourceQuery, new { country = request.Origin });

                // עדכון  טיסות
                foreach (var flight in flights)
                {
                    var flightQuery = @"
                    MERGE (f:Flight {id: $id})
                    SET f.origin = $origin, f.destination = $destination, f.date = $date, f.price = $price
                    ON CREATE SET f.created = datetime()
                    ON MATCH SET f.lastUpdated = datetime()";
                    await tx.RunAsync(flightQuery, new
                    {
                        id = flight.Id,
                        origin = flight.Origin,
                        destination = flight.Destination,
                        date = flight.Date,
                        price = flight.Price
                    });

                    // עדכון קשר בין הטיסות עם המקור
                    var relationshipQuery = @"
                    MATCH (s:Source {country: $country}), (f:Flight {id: $id})
                    MERGE (s)-[r:FLIGHT_PRICE]->(f)
                    SET r.price = $price
                    RETURN r";
                    await tx.RunAsync(relationshipQuery, new
                    {
                        country = request.Origin,
                        id = flight.Id,
                        price = flight.Price
                    });
                }

                // עדכון או יצירת מלונות
                foreach (var hotel in hotels)
                {
                    var hotelQuery = @"
                    MERGE (h:Hotel {id: $id})
                    SET h.name = $name, h.price = $price, h.cityCode = $cityCode, h.latitude = $latitude, h.longitude = $longitude
                    ON CREATE SET h.created = datetime()
                    ON MATCH SET h.lastUpdated = datetime()";
                    await tx.RunAsync(hotelQuery, new
                    {
                        id = hotel.Id,
                        name = hotel.Name,
                        price = hotel.Price,
                        cityCode = hotel.CityCode,
                        latitude = hotel.Latitude,
                        longitude = hotel.Longitude
                    });

                    // עדכון או יצירת קשרים בין טיסות לבין מלונות
                    foreach (var flight in flights)
                    {
                        if (IsWithinRadiusHotel(hotel, flight.DestinationLatitude, flight.DestinationLongitude, request.Radius))
                        {
                            var hotelPriceQuery = @"
                            MATCH (f:Flight {id: $flightId}), (h:Hotel {id: $hotelId})
                            MERGE (f)-[r:HOTEL_PRICE]->(h)
                            SET r.price = $price";
                            await tx.RunAsync(hotelPriceQuery, new
                            {
                                flightId = flight.Id,
                                hotelId = hotel.Id,
                                price = hotel.Price
                            });
                        }
                    }
                }
            });
        }



        private bool IsWithinRadius(Hotel hotel, double destinationLatitude, double destinationLongitude, int radius)
        {
            // מחשב אם המלון נמצא בתוך הרדיוס מהיעד של הטיסה
            var distance = GetDistance(hotel.Latitude, hotel.Longitude, destinationLatitude, destinationLongitude);
            return distance <= radius;
        }

        private bool IsWithinRadiusHotel(Hotel hotel, double destinationLatitude, double destinationLongitude, int radius)
        {
            // מחשב אם הטיסה נמצאת בתוך הרדיוס של המלון
            var distance = GetDistance(destinationLatitude, destinationLongitude, hotel.Latitude, hotel.Longitude);
            return distance <= radius;
        }

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // מחשב את המרחק בין שתי נקודות גאוגרפיות
            var R = 6371; // רדיוס כדור הארץ בק"מ
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a =
              Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +// מחשבת את סינוס מחצית מהפרש קווי הרוחב
              Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *//מחשב את קוסינוס קווי הרוחב
              Math.Sin(dLon / 2) * Math.Sin(dLon / 2);//מחשב את סינוס מחצית הפרש קווי הרוחב
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));//המחזירה את קשת הטנגס של שני המספרים Atan2 מחשבת את הזווית הרדיון עי 
            var distance = R * c; // המרחק בק"מ
            return distance;
        }

        private double ToRadians(double deg)
        {
            // ממיר מעלות לרדיאנים
            return deg * (Math.PI / 180);// יחס ההמרה בין מעלות לרדיונים-פאי רדיונים שווים ל180 מעלות
        }


        public async Task<(List<Flight>, List<Hotel>, List<Relationship>, List<Flight>)> GetGraphDataAsync(string country)
        {
            using var session = _driver.AsyncSession();

            var query = @"
            MATCH (s:Source {country: $country})-[:FLIGHT_PRICE]->(f:Flight)
            OPTIONAL MATCH (f)-[:HOTEL_PRICE]->(h:Hotel)
            OPTIONAL MATCH (h)-[:RETURN_FLIGHT_PRICE]->(rf:Flight)
            RETURN f.id AS flightId, f.origin AS origin, f.destination AS destination, f.date AS date, f.price AS flightPrice,
            h.id AS hotelId, h.name AS hotelName, h.price AS hotelPrice, h.cityCode AS hotelCityCode,
            h.latitude AS hotelLatitude, h.longitude AS hotelLongitude,
            rf.id AS returnFlightId, rf.origin AS returnOrigin, rf.destination AS returnDestination,
            rf.date AS returnDate, rf.price AS returnPrice";


            var result = await session.RunAsync(query, new { country });

            var flights = new List<Flight>();
            var hotels = new List<Hotel>();
            var relationships = new HashSet<Relationship>();
            //var relationships = new List<Relationship>();
            var returnFlights = new List<Flight>();

            await result.ForEachAsync(record =>
            {
                var flightId = record["flightId"].As<string>();
                var hotelId = record["hotelId"].As<string>();
                var returnFlightId = record["returnFlightId"].As<string>();

                var flight = new Flight
                {
                    Id = flightId,
                    Origin = record["origin"].As<string>(),
                    Destination = record["destination"].As<string>(),
                    Date = record["date"].As<string>(),
                    Price = record["flightPrice"].As<double>()
                };

                if (!flights.Any(f => f.Id == flightId))
                {
                    flights.Add(flight);
                }

                if (hotelId != null)
                {
                    var hotel = new Hotel
                    {
                        Id = hotelId,
                        Name = record["hotelName"].As<string>(),
                        Price = record["hotelPrice"].As<double>(),
                        CityCode = record["hotelCityCode"].As<string>(),
                        Latitude = record["hotelLatitude"].As<double>(),
                        Longitude = record["hotelLongitude"].As<double>()
                    };

                    if (!hotels.Any(h => h.Id == hotelId))
                    {
                        hotels.Add(hotel);
                    }

                    relationships.Add(new Relationship
                    {
                        FromFlight = flightId,
                        ToHotel = hotelId,
                        Price = record["hotelPrice"].As<double>()
                    });

                    if (returnFlightId != null)
                    {
                        var returnFlight = new Flight
                        {
                            Id = returnFlightId,
                            Origin = record["returnOrigin"].As<string>(),
                            Destination = record["returnDestination"].As<string>(),
                            Date = record["returnDate"].As<string>(),
                            Price = record["returnPrice"].As<double>()
                        };

                        if (!returnFlights.Any(rf => rf.Id == returnFlightId))
                        {
                            returnFlights.Add(returnFlight);
                        }

                        relationships.Add(new Relationship
                        {
                            FromFlight = hotelId,
                            ToHotel = returnFlightId,
                            Price = record["returnPrice"].As<double>()
                        });
                    }
                }
            });

            return (flights, hotels, relationships.ToList(), returnFlights);
        }



        public void Dispose()
        {
            _driver.Dispose();
        }
    }
}



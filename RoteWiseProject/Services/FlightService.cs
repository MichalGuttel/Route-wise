using CsvHelper;
using Newtonsoft.Json.Linq;
using RoteWiseProject.Dto;
using System.Formats.Asn1;
using System.Globalization;
using System.Net.Http.Headers;

namespace RoteWiseProject.Services
{
    public class FlightService
    {
        // משתנים עבור שירותי אימות ו-HTTP
        private readonly AuthenticationService _authenticationService;
        private readonly HttpClient _httpClient;

        // בנאי עבור שירות הטיסות
        public FlightService(AuthenticationService authenticationService, HttpClient httpClient)
        {
            _authenticationService = authenticationService;
            _httpClient = httpClient;
        }

        // פונקציה לקבלת טיסות לפי עיר מוצא 
        public async Task<string> GetFlightsAsync(string origin)
        {
            // קבלת אסימון גישה משירות האימות
            var accessToken = await _authenticationService.GetAccessTokenAsync();

            // יצירת בקשת HTTP FET עם כותרת Authorization
            //var request = new HttpRequestMessage(HttpMethod.Get, $"https://test.api.amadeus.com/v1/reference-data/recommended-locations?cityCodes={origin}&type=AIRPORT&max={maxDestinations}");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://test.api.amadeus.com/v1/travel/analytics/air-traffic/traveled?originCityCode={origin}&period=2017-01&max=5");
            Console.WriteLine(request);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // שליחת הבקשה וקבלת התשובה
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {responseContent}");

            return responseContent;

            //// stringS החזרת התוכן של התשובה כ-
            //return await response.Content.ReadAsStringAsync();
        }
 

        // פונקציה לקבלת טיסות ליעד לפי עיר מוצא, יעד, תאריך ומספר מבוגרים
        public async Task<string> GetFlightsToDestinationAsync(string origin, string destination, string date, int adult)
        {
            try
            {
                // קבלת אסימון גישה משירות האימות
                var accessToken = await _authenticationService.GetAccessTokenAsync();

                // יצירת בקשת HTTP עם כותרת Authorization
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://test.api.amadeus.com/v2/shopping/flight-offers?originLocationCode={origin}&destinationLocationCode={destination}&departureDate={date}&adults={adult}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // שליחת הבקשה וקבלת התשובה
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                // בדיקה אם התשובה לא מוצלחת, במקרה כזה יש לזרוק שגיאה
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {responseContent}");
                }

                // החזרת התוכן של התשובה כ-string
                return responseContent;
            }
            catch (Exception ex)
            {
                // טיפול בשגיאות וזריקת שגיאה כללית במקרה של בעיה
                throw new Exception($"Exception in GetFlightsToDestinationAsync: {ex.Message}");
            }
        }

        // פונקציה לפענוח נתוני טיסות מ-JSON לרשימת אובייקטים של טיסות
        public async Task<List<Flight>> ParseFlightsAsync(string flightsJson, string Destination)
        {
            // פענוח ה-JSON לאובייקט
            var flightsObject = JObject.Parse(flightsJson);
            var flightsArray = flightsObject["data"] as JArray;

            // אם אין נתונים, להחזיר רשימה ריקה
            if (flightsArray == null || flightsArray.Count == 0)
            {
                return new List<Flight>();
            }
            var flightList = new List<Flight>();
            // עבור כל טיסה במערך, יצירת אובייקט טיסה והוספתו לרשימה
            foreach (var flight in flightsArray)
            {
                var id = flight["id"].ToString();
                var itinerary = flight["itineraries"][0];
                var segment = itinerary["segments"][0];
                var departure = segment["departure"];
                var arrival = segment["arrival"];
                var price = flight["price"]["grandTotal"].ToString();
                var LatitudeDestination = GetLatitudeByIATA(arrival["iataCode"].ToString());
                var LongitudeDestination = GetLongitudeByIATA(arrival["iataCode"].ToString());

                var LatitudeOrigion = GetLatitudeByIATA(departure["iataCode"].ToString());
                var LongitudeOrigion = GetLongitudeByIATA(departure["iataCode"].ToString());


                if (arrival["iataCode"].ToString() == Destination)
                {
                    flightList.Add(new Flight
                    {
                        Id = id,
                        Origin = departure["iataCode"].ToString(),
                        Destination = arrival["iataCode"].ToString(),
                        Date = departure["at"].ToString(),
                        Price = (double)decimal.Parse(price),
                        DestinationLatitude = LatitudeDestination,
                        DestinationLongitude = LongitudeDestination,

                        OrigionLatitude = LatitudeOrigion,
                        OrigionLongitude=LongitudeOrigion
                    }); ;

                }
            }
            return flightList;
        }


        private double GetLatitudeByIATA(string iataCode)
        {
            // Define the path to the CSV file. Update it according to your project's structure.
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iata-icao.csv");
            filePath = "iata-icao.csv";
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Read all records from the CSV file
                var records = csv.GetRecords<dynamic>().ToList();

                // Find the first record that matches the given IATA code
                var airportRecord = records.FirstOrDefault(r => r.iata == iataCode);

                if (airportRecord != null)
                {
                    // If a matching record is found, return its latitude
                    Console.WriteLine($"Found IATA code {iataCode}: Latitude = {airportRecord.latitude}");
                    return double.Parse(airportRecord.latitude, CultureInfo.InvariantCulture);
                }
                else
                {
                    // If no matching record is found, return 0.0
                    Console.WriteLine($"IATA code {iataCode} not found");
                    return 0.0;
                }
            }
        }

        private double GetLongitudeByIATA(string iataCode)
        {
            // Define the path to the CSV file. Update it according to your project's structure.
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iata-icao.csv");
            filePath = "iata-icao.csv";
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Read all records from the CSV file
                var records = csv.GetRecords<dynamic>().ToList();

                // Find the first record that matches the given IATA code
                var airportRecord = records.FirstOrDefault(r => r.iata == iataCode);

                if (airportRecord != null)
                {
                    // If a matching record is found, return its longitude
                    Console.WriteLine($"Found IATA code {iataCode}: Longitude = {airportRecord.longitude}");
                    return double.Parse(airportRecord.longitude, CultureInfo.InvariantCulture);
                }
                else
                {
                    // If no matching record is found, return 0.0
                    Console.WriteLine($"IATA code {iataCode} not found");
                    return 0.0;
                }
            }
        }

    }
}

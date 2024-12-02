using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using RoteWiseProject.Dto;
using System.Net.Http.Headers;

namespace RoteWiseProject.Services
{
    public class HotelService
    {
        // משתנים עבור שירותי אימות ו-HTTP
        private readonly AuthenticationService _authenticationService;
        private readonly HttpClient _httpClient;

        // משתנה עבור רדיוס חיפוש המלונות
        public int radius = 10;

        // בנאי עבור שירות המלונות
        public HotelService(AuthenticationService authenticationService, HttpClient httpClient)
        {
            _authenticationService = authenticationService;
            _httpClient = httpClient;
        }

        // פונקציה לקבלת מלונות לפי קוד עיר
        public async Task<string> GetHotelsByCityAsync(string cityCode, string accessToken)
        {
            // אם הרדיוס קטן או שווה לאפס, יש לזרוק שגיאה
            if (radius <= 0)
            {
                throw new ArgumentException("Radius must be a positive integer.");
            }
            Console.WriteLine($"Radius: {radius}");

            // יצירת בקשת HTTP עם כותרת Authorization
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://test.api.amadeus.com/v1/reference-data/locations/hotels/by-city?cityCode={cityCode}&radius={radius}&radiusUnit=KM");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // הדפסת כותרות הבקשה ל-console
            Console.WriteLine("Request Headers:");
            foreach (var header in request.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            // שליחת הבקשה וקבלת התשובה
            var response = await _httpClient.SendAsync(request);
            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine("Response Headers:");
            foreach (var header in response.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            // בדיקה אם התשובה לא מוצלחת, במקרה כזה יש לזרוק שגיאה
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error Content: {errorContent}");
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {errorContent}");
            }

            // החזרת התוכן של התשובה כ-string
            return await response.Content.ReadAsStringAsync();
        }

        // פונקציה לקבלת הצעות מלון לפי מזהה מלון
        public async Task<string> GetHotelOffersAsync(string hotelId, string accessToken, int adult)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                int maxRetries = 3;
                int initialDelay = 2000; // 2 שניות

                for (int retry = 0; retry < maxRetries; retry++)
                {
                    HttpResponseMessage response = null;
                    try
                    {
                        // שליחת בקשת HTTP לקבלת הצעות עבור המלון &checkInDate=2024-07-22&checkOutDate=2024-07-28
                        response = await client.GetAsync($"https://test.api.amadeus.com/v3/shopping/hotel-offers?hotelIds={hotelId}&adults={adult}");
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync();
                        return content;
                    }
                    catch (HttpRequestException e)
                    {
                        // אם יש יותר מדי בקשות, לנסות שוב עם המתנה אקספוננציאלית
                        if (response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            if (retry == maxRetries - 1)
                            {
                                // רישום הכישלון הסופי
                                Console.WriteLine($"Too many requests for hotel ID {hotelId}, final retry failed.");
                                return null;
                            }

                            await Task.Delay(initialDelay);
                            initialDelay *= 2; // אקספוננציאל backoff
                            continue; // לנסות שוב את הבקשה
                        }
                        // אם יש קוד שגיאה רע, להדפיס את השגיאה ולהמשיך
                        if (response?.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Invalid property code for hotel ID {hotelId}: {errorContent}");
                            return null; // לדלג על המלון הנוכחי
                        }
                        // להדפיס את השגיאה הכללית ולהמשיך
                        Console.WriteLine($"Request error for hotel ID {hotelId}: {e.Message}");
                        return null; // לדלג על המלון הנוכחי
                    }
                }
                return null;
            }
        }

        // פונקציה לקבלת כל המלונות והמחירים בעיר נתונה
        public async Task<List<Hotel>> GetAllHotelsAndPricesAsync(string cityCode)
        {
            // מקבל אסימון גישה משירות האימות
            var accessToken = await _authenticationService.GetAccessTokenAsync();

            // מקבל רשימת מלונות לפי קוד העיר
            var hotelsResponse = await GetHotelsByCityAsync(cityCode, accessToken);
            var hotelsJson = JObject.Parse(hotelsResponse);

            var hotels = new List<Hotel>();

            // בודק אם יש נתונים במלונות שנמצאו
            var hotelsArray = hotelsJson["data"] as JArray;
            if (hotelsArray == null)
            {
                Console.WriteLine("No hotels found in the response.");
                return hotels;
            }

            // עבור כל מלון ברשימה, מקבל את ההצעות והמחירים שלו
            foreach (var hotel in hotelsArray)
            {
                var hotelId = hotel["hotelId"].ToString();
                var hotelName = hotel["name"].ToString();
                var latitude = (double)hotel["geoCode"]["latitude"];
                var longitude = (double)hotel["geoCode"]["longitude"];

                Console.WriteLine($"Processing hotel ID: {hotelId}, Name: {hotelName}");
                int adult = 1;
                var offersResponse = await GetHotelOffersAsync(hotelId, accessToken, adult);
                if (offersResponse == null)
                {
                    Console.WriteLine($"No offers found for hotel ID: {hotelId}");
                    continue;
                }
                var offersJson = JObject.Parse(offersResponse);
                var price = (double)offersJson["data"][0]["offers"][0]["price"]["total"];

                // מוסיף את המלון לרשימת המלונות עם המחיר שלו
                hotels.Add(new Hotel
                {
                    Id = hotelId,
                    Name = hotelName,
                    Price = price,
                    CityCode = cityCode,
                    Latitude = latitude,
                    Longitude = longitude
                });
            }
            return hotels;
        }
    }
}

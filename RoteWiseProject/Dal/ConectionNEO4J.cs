using RoteWiseProject.Services;
using RoteWiseProject.Settings;
using System.Threading.Tasks;
using System.Net.Http;
namespace RoteWiseProject.Dal
{
    public class ConectionNEO4J
    {
        public static async Task Main(string[] args)
        {
           

            var settings = ConnectionSettings.CreateBasicAuth("bolt://localhost:7687/db/RouteGraph", "neo4j", "12345678");
            using (var httpClient = new HttpClient())
            {
                var authenticationService = new AuthenticationService(httpClient);
                var hotelService = new HotelService(authenticationService, httpClient);
                using (var neo4jService = new Neo4jService(settings)) ;
            }   
        }
    }
}
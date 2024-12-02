namespace RoteWiseProject.Dto
{
    public class Flight
    {
        public string Id { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Date { get; set; }
        public double Price { get; set; }
        public double DestinationLatitude { get; set; }
        public double DestinationLongitude { get; set; }

        public double OrigionLatitude {  get; set; }
        public double OrigionLongitude { get; set; }
    }
}

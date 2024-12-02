using System.Collections.Generic;
using RoteWiseProject.Dto;

namespace RoteWiseProject.Controllers.Models
{
    public class GraphRequestModel
    {
        public string CityCode { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string DateStart { get; set; }
        public string DateEnd { get; set; }
        public  int adult { get; set; }
        public int Radius { get; set; } 
        public int MaxPrice { get; set; } 
       
    }
}

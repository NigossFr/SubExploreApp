using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SubExplore.Models.Domain
{
    public class LocationCoordinates
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public double Accuracy { get; set; } // Précision en mètres
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    class MathHelper
    {
        public static double StandardDeviation(IEnumerable<double> _Values)
        {
            double ReturnValue = 0;
            int Count = _Values.Count();
            if (Count > 1)
            {
                double Average = _Values.Average();

                double Sum = _Values.Sum(d => (d - Average) * (d - Average));

                ReturnValue = Math.Sqrt(Sum / Count);
            }
            return ReturnValue;
        }
    }
}

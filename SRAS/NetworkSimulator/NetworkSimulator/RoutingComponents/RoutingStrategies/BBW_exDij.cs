using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.CommonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.SimulatorComponents;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class BBW_exDij : RoutingStrategy
    {
        private static readonly double MaxValue = 10000;

        public BBW_exDij(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

        public override List<Link> GetPath(Request request)
        {
            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            Dictionary<Link, double> w1 = new Dictionary<Link, double>();
            Dictionary<Link, int> w2 = new Dictionary<Link, int>();
            foreach (var link in _Topology.Links)
            {
                if (link.UsingBandwidth == 0)
                    w1[link] = 1d / (link.Capacity);
                else
                    w1[link] = link.UsingBandwidth / (link.ResidualBandwidth);

                w2[link] = (int)link.Delay;

                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link);
            }

            EDSP edsp = new EDSP(_Topology);
            var path = edsp.FindFeasiblePath(request.SourceId, request.DestinationId, eliminatedLinks, w1, w2, (int)request.Delay);

            CalculateWeightPath(w1, path);
            //Console.WriteLine();
            return path;
        }
    }
}

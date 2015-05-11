using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.SimulatorComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class HRABDC : RoutingStrategy
    {
        

        public HRABDC(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }
                                
        public override List<Link> GetPath(Request request)
        {
            HashSet<string> eliminatedLinks = new HashSet<string>();
            Dictionary<string, double> w1 = new Dictionary<string, double>(); // link.Key, link_weight
            Dictionary<string, double> w2 = new Dictionary<string, double>();// link.Key, link_delay            

            foreach (var link in _Topology.Links)
            {
                if (link.UsingBandwidth == 0)                    
                    w1[link.Key] = 1d / (link.Capacity);
                else
                    w1[link.Key] = link.UsingBandwidth / (link.ResidualBandwidth);

                w2[link.Key] = link.Delay;
                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link.Key);
            }

            /*
            // cath example
            w1["0|1"] = 1;
            w1["0|2"] = 1;
            w1["1|3"] = 1;
            //eliminatedLinks.Add("1|4"); //w1["1|4"] = 1;
            w1["2|3"] = 2;
            //w1["2|4"] = 4;
            w1["3|4"] = 1;
            w1["3|5"] = 5;
            w1["4|5"] = 1;
            */

            HeuristicDijkstra heDi = new HeuristicDijkstra(_Topology);
            var path = heDi.FindOptimalPath(
                _Topology, eliminatedLinks, request.SourceId, request.DestinationId, w1, w2, request.Delay);    
                    

            return path;
        }
    }
}

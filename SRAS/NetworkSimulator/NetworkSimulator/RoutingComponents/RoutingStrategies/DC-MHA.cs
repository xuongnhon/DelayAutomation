using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class DC_MHA : RoutingStrategy
    {
        public DC_MHA(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            Dictionary<Link, double> weight = new Dictionary<Link, double>();
            Dictionary<Link, int> delay = new Dictionary<Link,int>();

            foreach (var link in _Topology.Links)
            {
                weight[link] = 1;
                delay[link] = (int)link.Delay;
                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link);
            }

            EDSP edsp = new EDSP(_Topology);
            var path = edsp.FindFeasiblePath(
                request.SourceId, request.DestinationId, eliminatedLinks, weight, delay, (int)request.Delay);

            return path;
        }
    }
}

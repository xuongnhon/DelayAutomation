using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class DC_MIRA : RoutingStrategy
    {
        private int _Alpha;

        public int Alpha
        {
            get { return _Alpha; }
            set { _Alpha = value; }
        }

        public DC_MIRA(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            Dictionary<Link, int> delay = new Dictionary<Link,int>();
            Dictionary<Link, double> weight = new Dictionary<Link, double>();
            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            FordFulkerson fordFulkerson = new FordFulkerson(_Topology);
            EDSP edsp = new EDSP(_Topology);

            foreach (var link in _Topology.Links)
            {
                // Initialize for link weight and delay
                weight[link] = 1;
                delay[link] = (int)link.Delay;
                // Eliminate which link have residual bandwidth less than bandwidth demand
                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link);
            }
            // Compute link weight
            var ieList = _Topology.IEPairs
                .Where(p => p.Ingress.Key != request.SourceId || p.Egress.Key != request.DestinationId).ToList();
            foreach (var ie in ieList)
            {
                var criticalLinks = fordFulkerson.FindMinCutSet(ie.Ingress, ie.Egress);
                foreach (var link in criticalLinks)
                    weight[link] += _Alpha;
            }
            // Find feasible path
            var path = edsp.FindFeasiblePath(
                request.SourceId, request.DestinationId, eliminatedLinks, weight, delay, (int)request.Delay);

            return path;
        }
    }
}

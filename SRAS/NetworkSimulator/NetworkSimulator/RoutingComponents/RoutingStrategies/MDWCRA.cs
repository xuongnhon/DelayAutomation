using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.CommonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class MDWCRA : RoutingStrategy
    {
        private static readonly double MaxValue = 10000;
        private Dictionary<IEPair, List<Link>> _Cie;

        public MDWCRA(Topology topology)
            : base(topology)
        {
            _Topology = topology;
            Initialize();
        }

        private void Initialize()
        {
            _Cie = new Dictionary<IEPair, List<Link>>();
        }

        private List<Link> FindLeastDelayPath(int s, int d, HashSet<Link> E)
        {
            // Initialize
            int nv = _Topology.Nodes.Count;
            double[] dist = new double[nv];
            int[] prev = new int[nv];

            for (int v = 0; v < nv; v++)
            {
                dist[v] = MaxValue;
                prev[v] = -1;
            }
            dist[s] = 0;

            var Q = new List<Node>(_Topology.Nodes);

            while (Q.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]:z within T
                var u = Q.First();
                foreach (var node in Q)
                    if (dist[node.Key] < dist[u.Key])
                        u = node;

                Q.Remove(u);

                // Browse all adjacent node that not contain in C to update distance from s.
                foreach (var link in u.Links.Where(l => !E.Contains(l)))
                {
                    var v = link.Destination;
                    if (dist[v.Key] > dist[u.Key] + link.Delay)
                    {
                        dist[v.Key] = dist[u.Key] + link.Delay;
                        prev[v.Key] = u.Key;
                    }
                }
            }
            return ConstructPath(prev, d);
        }

        private List<Link> ConstructPath(int[] prev, int d)
        {
            int v = d;
            List<Link> path = new List<Link>();

            while (prev[v] != -1)
            {
                int u = prev[v];
                var link = _Topology.GetLink(u, v);
                path.Add(link);
                v = u;
            }
            path.Reverse();

            return path;
        }
            
        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            // Initialize all link weights to zero.
            Dictionary<Link, double> weight = new Dictionary<Link, double>();
            Dictionary<Link, int> delay = new Dictionary<Link, int>();
            foreach (var link in _Topology.Links)
            {
                weight[link] = 0;
                delay[link] = (int)link.Delay;
            }

            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            foreach (var ie in _Topology.IEPairs)
            {
                eliminatedLinks.Clear();
                _Cie[ie] = new List<Link>();
                List<Link> lDP = FindLeastDelayPath(ie.Ingress.Key, ie.Egress.Key, eliminatedLinks);
   
                while (lDP.Count > 0)
                {
                    var B = lDP.Min(l => l.ResidualBandwidth);
                    var D = lDP.Sum(l => l.Delay);
                    // Label bottleneck links of LDP as critical, and add them to Cst
                    var bottleneckLinks = lDP.Where(l => l.ResidualBandwidth == B);
                    foreach (var link in bottleneckLinks)
                        _Cie[ie].Add(link);
                    // For each critical links identified, update link weight
                    foreach (var link in bottleneckLinks)
                        weight[link] = weight[link] + 1d / (B * D);
                    // Delete all link beloning to LDP
                    foreach (var link in lDP)
                        eliminatedLinks.Add(link);
                    // Find the next LDP
                    lDP = FindLeastDelayPath(ie.Ingress.Key, ie.Egress.Key, eliminatedLinks);
                }
            }

            eliminatedLinks.Clear();

            // Eliminate all links that have residual bandwidth less then bandwidth demand
            foreach (var link in _Topology.Links)
                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link);

            EDSP edsp = new EDSP(_Topology);
            var path = edsp.FindFeasiblePath(
                request.SourceId, request.DestinationId, eliminatedLinks, weight, delay, (int)request.Delay);

            if (path.Sum(l => l.Delay) > request.Delay)
                throw new Exception("Not feasible path");

            if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
                throw new Exception("Residual bandwidth less than 0");

            return path;
        }
    }
}

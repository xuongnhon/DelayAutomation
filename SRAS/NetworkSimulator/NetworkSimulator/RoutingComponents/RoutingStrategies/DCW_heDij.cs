using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.CommonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class DWC_heDij : RoutingStrategy
    {
        private static readonly double MaxValue = 10000;
        private Dictionary<IEPair, List<Link>> _Cie;

        public DWC_heDij(Topology topology)
            : base(topology)
        {
            _Topology = topology;
            Initialize();
        }

        private void Initialize()
        {
            _Cie = new Dictionary<IEPair, List<Link>>();
        }
       
        #region
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
        #endregion

       
            
        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            // Initialize all link weights to zero.
            Dictionary<string, double> weight = new Dictionary<string, double>();
            Dictionary<string, double> delay = new Dictionary<string, double>();
            foreach (var link in _Topology.Links)
            {
                weight[link.Key] = 0;
                delay[link.Key] = link.Delay;
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
                    //Label bottleneck links of LDP as critical, and add them to Cst
                    var bottleneckLinks = lDP.Where(l => l.ResidualBandwidth == B);
                    foreach (var link in bottleneckLinks)
                        _Cie[ie].Add(link);
                    // For each critical links identified, update link weight
                    foreach (var link in bottleneckLinks)
                        weight[link.Key] = weight[link.Key] + 1d / (B * D);
                    // Delete all bottleneck links
                    foreach (var link in bottleneckLinks)
                        eliminatedLinks.Add(link);
                    // Find the next LDP
                    lDP = FindLeastDelayPath(ie.Ingress.Key, ie.Egress.Key, eliminatedLinks);
                }
            }

            eliminatedLinks.Clear();
            HashSet<String> eliminatedLinks_tmp = new HashSet<String>();

            // Eliminate all links that have residual bandwidth less then bandwidth demand
            foreach (var link in _Topology.Links)
                if (link.ResidualBandwidth < request.Demand)
                {
                    eliminatedLinks.Add(link);
                    eliminatedLinks_tmp.Add(link.Key);
                }

            /*EDSP edsp = new EDSP(_Topology);
            var path = edsp.FindFeasiblePath(
                request.SourceId, request.DestinationId, eliminatedLinks, weight, delay, (int)request.Delay);
            */

            eHRABDC ehrabdc = new eHRABDC(_Topology);

            // 14/07/21 caoth: ehe
            //var path = ehrabdc.FindOptimalPath(_Topology, eliminatedLinks_tmp, request.SourceId, request.DestinationId, weight, delay, request.Delay);

            var tempPath = ehrabdc.FindOptimalPath(
                _Topology, eliminatedLinks_tmp, request.SourceId, request.DestinationId, weight, delay, request.Delay);
            var path = tempPath;
            while (tempPath.Count > 0)
            {
                //foreach (var link in tempPath)
                //    Console.Write(link.Key + "-");
                //Console.WriteLine(tempPath.Sum(l => w1[l.Key]));
                if (tempPath.Sum(l => weight[l.Key]) < path.Sum(l => weight[l.Key]))
                    path = tempPath;
                //if (tempPath.Sum(l => w1[l.Key]) * tempPath.Count < path.Sum(l => w1[l.Key]) * path.Count)
                //    path = tempPath;
                double maxWeight = tempPath.Max(l => weight[l.Key]);
                var cl = tempPath.Where(l => weight[l.Key] == maxWeight).ToList();
                foreach (var link in cl)
                    eliminatedLinks_tmp.Add(link.Key);
                tempPath = ehrabdc.FindOptimalPath(
                _Topology, eliminatedLinks_tmp, request.SourceId, request.DestinationId, weight, delay, request.Delay);
            }

            if (path.Sum(l => l.Delay) > request.Delay)
                throw new Exception("Not feasible path");

            if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
                throw new Exception("Residual bandwidth less than 0");

            return path;
        }
    }
}

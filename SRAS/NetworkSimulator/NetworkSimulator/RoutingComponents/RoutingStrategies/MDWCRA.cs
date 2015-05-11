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
        //private static readonly double MaxValue = 10000;
        //private Dictionary<IEPair, List<Link>> _Cie;

        LDP _ldp;// = new LDP(_Topology);

        public MDWCRA(Topology topology)
            : base(topology)
        {
            _Topology = topology;
            Initialize();            
        }

        private void Initialize()
        {
            //_Cie = new Dictionary<IEPair, List<Link>>();

            _ldp = new LDP(_Topology);
        }

        //caoth
        /*private List<Link> FindLeastDelayPath(int s, int d, HashSet<Link> E)
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
        }*/
            
        // caoth
        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            // Initialize all link weights to zero.
            Dictionary<Link, double> weights = new Dictionary<Link, double>();
            Dictionary<Link, int> delays = new Dictionary<Link, int>();
            HashSet<Link> eliminatedBwLinks = new HashSet<Link>();
            foreach (var link in _Topology.Links)
            {
                weights[link] = 0;
                delays[link] = (int)link.Delay;

                if (link.ResidualBandwidth < request.Demand)
                    eliminatedBwLinks.Add(link);
            }

            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            foreach (var ie in _Topology.IEPairs)
            {
                eliminatedLinks.Clear();
                //_Cie[ie] = new List<Link>();
                List<Link> lDP = _ldp.FindLeastDelayPath(ie.Ingress.Key, ie.Egress.Key, eliminatedLinks);
   
                while (lDP.Count > 0)
                {
                    var B = lDP.Min(l => l.ResidualBandwidth);
                    //var D = lDP.Sum(l => l.Delay);
                    // Label bottleneck links of LDP as critical, and add them to Cst
                    // For each critical links identified, update link weight
                    var bottleneckLinks = lDP.Where(l => l.ResidualBandwidth == B);
                    foreach (var link in bottleneckLinks)
                    {
                        //_Cie[ie].Add(link);
                        //weight[link] = weight[link] + 1d / (B * D); // lamda
                        weights[link] += 1; // lamda, paper 2003 p.6
                    }                                          
                        
                    // Delete all link belonging to LDP
                    foreach (var link in lDP) // MDWCRA
                        eliminatedLinks.Add(link);

                    // Find the next LDP
                    lDP = _ldp.FindLeastDelayPath(ie.Ingress.Key, ie.Egress.Key, eliminatedLinks);
                }
            }

            //eliminatedLinks.Clear();

            // Eliminate all links that have residual bandwidth less then bandwidth demand
            //foreach (var link in _Topology.Links)
                //if (link.ResidualBandwidth < request.Demand)
                    //eliminatedLinks.Add(link);

            /*
            // cath example
            weights[_Topology.Links.Where(l=>l.Key == "0|1").FirstOrDefault()] = 1;
            weights[_Topology.Links.Where(l => l.Key == "0|2").FirstOrDefault()] = 2;
            weights[_Topology.Links.Where(l => l.Key == "1|3").FirstOrDefault()] = 3;
            //eliminatedLinks.Add("1|4"); //weights["1|4"] = 1;
            weights[_Topology.Links.Where(l => l.Key == "2|3").FirstOrDefault()] = 1;
            weights[_Topology.Links.Where(l => l.Key == "2|4").FirstOrDefault()] = 4;
            weights[_Topology.Links.Where(l => l.Key == "3|5").FirstOrDefault()] = 3;
            weights[_Topology.Links.Where(l => l.Key == "4|5").FirstOrDefault()] = 2;
             */ 

            EDSP edsp = new EDSP(_Topology);
            var path = edsp.FindFeasiblePath(
                request.SourceId, request.DestinationId, eliminatedBwLinks, weights, delays, (int)request.Delay);
            // mang delay so nguyen dung lam chi so trong extended Dijkstra

            //if (path.Sum(l => l.Delay) > request.Delay)
            //    throw new Exception("Not feasible path");

            //if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
            //    throw new Exception("Residual bandwidth less than 0");

            return path;
        }
    }
}

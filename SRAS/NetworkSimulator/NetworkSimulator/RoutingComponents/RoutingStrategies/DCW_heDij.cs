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
        LDP _ldp;

        public DWC_heDij(Topology topology)
            : base(topology)
        {
            _Topology = topology;
            Initialize();
        }

        private void Initialize()
        {
            _Cie = new Dictionary<IEPair, List<Link>>();

            _ldp = new LDP(_Topology);
        }
       
       
       
            
        public override List<Link> GetPath(SimulatorComponents.Request request)
        {                        
            HashSet<string> eliminatedBwLinks = new HashSet<string>();
            Dictionary<string, double> w1 = new Dictionary<string, double>();
            Dictionary<string, double> w2 = new Dictionary<string, double>();

            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            //Dictionary<Link, double> weights = new Dictionary<Link, double>();
            //Dictionary<Link, int> delays = new Dictionary<Link, int>();
            
            foreach (var link in _Topology.Links)
            {
                //weights[link] = 0;
                //delays[link] = (int)link.Delay;

                w1[link.Key] = 0;
                w2[link.Key] = link.Delay;

                if (link.ResidualBandwidth < request.Demand)
                    eliminatedBwLinks.Add(link.Key);
            }

            

            foreach (var ie in _Topology.IEPairs)
            {
                // caoth 2015 : same as MDWCRA

                eliminatedLinks.Clear();

                List<Link> lDP = _ldp.FindLeastDelayPath(ie.Ingress.Key, ie.Egress.Key, eliminatedLinks);

                while (lDP.Count > 0)
                {
                    var B = lDP.Min(l => l.ResidualBandwidth);
                    var bottleneckLinks = lDP.Where(l => l.ResidualBandwidth == B);
                    foreach (var link in bottleneckLinks)
                    {
                        w1[link.Key] += 1; // lamda, paper 2003 p.6

                        eliminatedLinks.Add(link); // M-MDWDRA
                    }

                    // Find the next LDP
                    lDP = _ldp.FindLeastDelayPath(ie.Ingress.Key, ie.Egress.Key, eliminatedLinks);
                }
            }

            HeuristicDijkstra heDi = new HeuristicDijkstra(_Topology);
            var path = heDi.FindOptimalPath(
                _Topology, eliminatedBwLinks, request.SourceId, request.DestinationId, w1, w2, request.Delay);

            //Nhon
            CalculateWeightPath(w1, path);

            return path;
        }
    }
}

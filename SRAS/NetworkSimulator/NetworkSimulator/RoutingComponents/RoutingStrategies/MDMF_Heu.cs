using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.CommonObjects;
using NetworkSimulator.SimulatorComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class MDMF_Heu : RoutingStrategy
    {
        public double Mi { get; set; }
        public double Upsilon { get; set; }

        public MDMF_Heu(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

        private List<Link> FindCDSet(Node s, Node d)
        {
            LDP ldp = new LDP(_Topology);
            List<Link> LP = null;
            List<Link> CD = new List<Link>();
            HashSet<Link> E = new HashSet<Link>();

            while (true)
            {
                LP = ldp.FindLeastDelayPath(s.Key, d.Key, E);
                if (LP.Count == 0)
                    break;

                double minRB = LP.Min(l => l.ResidualBandwidth);
                //Link cd = LP.FirstOrDefault(l => l.ResidualBandwidth == minRB);
                var bottleneckLinks = LP.Where(l => l.ResidualBandwidth == minRB);
                foreach (var link in bottleneckLinks)
                {
                    CD.Add(link);
                    E.Add(link);
                }
            }

            return CD;
        }

        public override List<Link> GetPath(Request request)
        {            
            //Dictionary<Link, double> weights = new Dictionary<Link, double>();
            
            FordFulkerson fordFulkerson = new FordFulkerson(_Topology);
            Dijkstra dijkstra = new Dijkstra(_Topology);
            Dictionary<Link, double> oldResidualBandwidths = new Dictionary<Link, double>();

            Dictionary<Link, double> sumCM = new Dictionary<Link, double>();
            Dictionary<Link, double> sumCD = new Dictionary<Link, double>();

            HashSet<string> eliminatedBwLinks = new HashSet<string>();
            Dictionary<string, double> w1 = new Dictionary<string, double>();
            Dictionary<string, double> w2 = new Dictionary<string, double>();

            // initialize
            foreach (Link link in _Topology.Links)
            {
                //weights[link] = 0;
                sumCD[link] = 0;
                sumCM[link] = 0;

                w1[link.Key] = 0;
                w2[link.Key] = link.Delay;

                if (link.ResidualBandwidth < request.Demand)
                    eliminatedBwLinks.Add(link.Key);
            }

            foreach (IEPair ie in _Topology.IEPairs)
            {
                List<Link> CM = fordFulkerson.FindMinCutSet(ie.Ingress, ie.Egress);
                List<Link> CD = FindCDSet(ie.Ingress, ie.Egress);

                foreach (Link link in CM)
                {
                    sumCM[link] += 1; // gamma
                }

                foreach (Link link in CD)
                {
                    sumCD[link] += 1; // lamda
                }
            }

            foreach (Link link in _Topology.Links)
            {
                //weights[link] = (1 + Mi * sumCM[link] + Upsilon * sumCD[link]) / link.ResidualBandwidth;
                w1[link.Key] = (1 + Mi * sumCM[link] + Upsilon * sumCD[link]) / link.ResidualBandwidth;
            }

            HeuristicDijkstra heDi = new HeuristicDijkstra(_Topology);
            var path = heDi.FindOptimalPath(
                _Topology, eliminatedBwLinks, request.SourceId, request.DestinationId, w1, w2, request.Delay);

            CalculateWeightPath(w1, path);

            return path;            
        }
    }
}

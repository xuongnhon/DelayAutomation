using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.SimulatorComponents;
using NetworkSimulator.RoutingComponents.CommonObjects;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class MDMF_exDij : RoutingStrategy
    {
        public double Mi { get; set; }
        public double Upsilon { get; set; }

        public MDMF_exDij(Topology topology)
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
            FordFulkerson fordFulkerson = new FordFulkerson(_Topology);
            Dijkstra dijkstra = new Dijkstra(_Topology);
            Dictionary<Link, double> oldResidualBandwidths = new Dictionary<Link, double>();

            Dictionary<Link, double> sumCM = new Dictionary<Link, double>();
            Dictionary<Link, double> sumCD = new Dictionary<Link, double>();

            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            Dictionary<Link, double> w1 = new Dictionary<Link, double>();
            Dictionary<Link, int> w2 = new Dictionary<Link, int>();

            // initialize
            foreach (Link link in _Topology.Links)
            {
                //weights[link] = 0;
                sumCD[link] = 0;
                sumCM[link] = 0;

                w1[link] = 0;
                w2[link] = (int)link.Delay;

                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link);
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
                w1[link] = (1 + Mi * sumCM[link] + Upsilon * sumCD[link]) / link.ResidualBandwidth;
            }

            EDSP edsp = new EDSP(_Topology);
            var path = edsp.FindFeasiblePath(request.SourceId, request.DestinationId, eliminatedLinks, w1, w2, (int)request.Delay);

            //Nhon
            CalculateWeightPath(w1, path);

            return path;
        }
    }
}

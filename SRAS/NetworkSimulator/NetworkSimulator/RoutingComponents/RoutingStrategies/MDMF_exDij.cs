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

        //private List<Link> FindCDSet(Node s, Node d, HashSet<Link> eliminatedLinks)
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
                
                //double minRB = LP.Min(l => l.ResidualBandwidth);
                //Link cd = LP.FirstOrDefault(l => l.ResidualBandwidth == minRB);

                //CD.Add(cd);
                //E.Add(cd);

                // caoth 14/07/15
                var B = LP.Min(l => l.ResidualBandwidth);
                var bottleneckLinks = LP.Where(l => l.ResidualBandwidth == B);
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
            List<Link> path = null;
            Dictionary<Link, double> weight = new Dictionary<Link, double>();
            Dictionary<Link, int> delay = new Dictionary<Link, int>();
            HashSet<Link> eliminatedBwLinks = new HashSet<Link>();
            FordFulkerson fordFulkerson = new FordFulkerson(_Topology);
            //Dijkstra dijkstra = new Dijkstra(_Topology);
            Dictionary<Link, double> oldResidualBandwidth = new Dictionary<Link, double>();

            Dictionary<Link, double> sumCM = new Dictionary<Link, double>();
            Dictionary<Link, double> sumCD = new Dictionary<Link, double>();

            EDSP edsp = new EDSP(_Topology);

            // initialize
            foreach (Link link in _Topology.Links)
            {
                weight[link] = 0;
                sumCD[link] = 0;
                sumCM[link] = 0;
                delay[link] = (int)link.Delay;

                if (link.ResidualBandwidth < request.Demand)
                {
                    eliminatedBwLinks.Add(link);
                    oldResidualBandwidth[link] = link.ResidualBandwidth;
                    link.ResidualBandwidth = 0;
                }
            }

            foreach (IEPair ie in _Topology.IEPairs)
            {
                List<Link> CM = fordFulkerson.FindMinCutSet(ie.Ingress, ie.Egress);
                //List<Link> CD = FindCDSet(ie.Ingress, ie.Egress, eliminatedLinks);
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
                weight[link] = (1 + Mi * sumCM[link] + Upsilon * sumCD[link]) / link.ResidualBandwidth;
            }

            path = edsp.FindFeasiblePath(request.SourceId, request.DestinationId, eliminatedBwLinks, weight, delay, (int)request.Delay);

            foreach (var item in oldResidualBandwidth) // because fordFulkerson implementation bases on bandwidth to eliminate link
            {
                item.Key.ResidualBandwidth = item.Value;
            }

            return path;
        }
    }
}

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
    public class MDMF : RoutingStrategy
    {
        public double Mi { get; set; }
        public double Upsilon { get; set; }

        public MDMF(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

        private List<Link> FindCDSet(Node s, Node d, HashSet<Link> eliminatedLinks)
        {
            LDP ldp = new LDP(_Topology);
            List<Link> LP = null;
            List<Link> CD = new List<Link>();
            HashSet<Link> E = new HashSet<Link>(eliminatedLinks);

            while (true)
            {
                LP = ldp.FindLeastDelayPath(s.Key, d.Key, E);
                if (LP.Count == 0)
                    break;

                double minRB = LP.Min(l => l.ResidualBandwidth);
                Link cd = LP.FirstOrDefault(l => l.ResidualBandwidth == minRB);

                CD.Add(cd);
                E.Add(cd);
            }

            return CD;
        }

        public override List<Link> GetPath(Request request)
        {
            List<Link> path = null;
            Dictionary<Link, double> weight = new Dictionary<Link, double>();
            Dictionary<Link, int> delay = new Dictionary<Link, int>();
            HashSet<Link> eliminatedLinks = new HashSet<Link>();
            FordFulkerson fordFulkerson = new FordFulkerson(_Topology);
            Dijkstra dijkstra = new Dijkstra(_Topology);
            Dictionary<Link, double> oldResidualBandwidth = new Dictionary<Link, double>();

            Dictionary<Link, double> sumCM = new Dictionary<Link, double>();
            Dictionary<Link, double> sumCD = new Dictionary<Link, double>();

            // initialize
            foreach (Link link in _Topology.Links)
            {
                weight[link] = 0;
                sumCD[link] = 0;
                sumCM[link] = 0;
                delay[link] = (int)link.Delay;

                if (link.ResidualBandwidth < request.Demand)
                {
                    eliminatedLinks.Add(link);
                    oldResidualBandwidth[link] = link.ResidualBandwidth;
                    link.ResidualBandwidth = 0;
                }
            }

            foreach (IEPair ie in _Topology.IEPairs)
            {
                List<Link> CM = fordFulkerson.FindMinCutSet(ie.Ingress, ie.Egress);
                List<Link> CD = FindCDSet(ie.Ingress, ie.Egress, eliminatedLinks);

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

            while (true)
            {
                path = dijkstra.GetShortestPath(request.SourceId, request.DestinationId, weight);

                // if no path is found
                if (path.Count == 0)
                {
                    break; // break to reject the request.
                }
                else if (path.Sum(l => l.Delay) <= request.Delay)
                    break; // break to return the feasible path.
                else
                {
                    // if the found path is not feasible
                    double minCP = path.Min(l => l.Capacity);
                    Link rmLink = path.FirstOrDefault(l => l.Capacity == minCP);

                    oldResidualBandwidth[rmLink] = rmLink.ResidualBandwidth;
                    rmLink.ResidualBandwidth = 0;
                    eliminatedLinks.Add(rmLink);
                }
            }

            foreach (var item in oldResidualBandwidth)
            {
                item.Key.ResidualBandwidth = item.Value;
            }

            return path;
        }
    }
}

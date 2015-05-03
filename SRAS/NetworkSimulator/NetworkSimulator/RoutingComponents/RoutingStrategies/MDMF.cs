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

        //private List<Link> FindCDSet(Node s, Node d, HashSet<Link> eliminatedLinks)
        private List<Link> FindCDSet(Node s, Node d)
        {
            LDP ldp = new LDP(_Topology);
            List<Link> LP = null;
            List<Link> CD = new List<Link>();
            //HashSet<Link> E = new HashSet<Link>(eliminatedLinks);
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

        // caoth
        public override List<Link> GetPath(Request request)
        {
            List<Link> path = new List<Link>();

            Dictionary<Link, double> weights = new Dictionary<Link, double>();            
            //HashSet<Link> eliminatedLinks = new HashSet<Link>();
            FordFulkerson fordFulkerson = new FordFulkerson(_Topology);
            
            Dictionary<Link, double> sumCM = new Dictionary<Link, double>();
            Dictionary<Link, double> sumCD = new Dictionary<Link, double>();

            // initialize
            foreach (Link link in _Topology.Links)
            {
                weights[link] = 0;
                sumCD[link] = 0;
                sumCM[link] = 0;                
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
                weights[link] = (1 + Mi * sumCM[link] + Upsilon * sumCD[link]) / link.ResidualBandwidth;
            }

            //caoth 2015
            Dijkstra dijkstra = new Dijkstra(_Topology);
            Dictionary<Link, double> oldResidualBandwidths = new Dictionary<Link, double>();
            
            // link residual
            foreach (Link link in _Topology.Links)
            {
                if (link.ResidualBandwidth < request.Demand)
                {
                    oldResidualBandwidths[link] = link.ResidualBandwidth;
                    link.ResidualBandwidth = 0;
                }
            }

            while (true)
            {                
                path = dijkstra.GetShortestPath(request.SourceId, request.DestinationId, weights); // Dijkstra không xét link có residual bw == 0
                if (path.Count > 0)
                {
                    var pathDelay = path.Sum(l => l.Delay); // delay here is the min delay, dont need to apply latency-rate server formulate
                    if (pathDelay <= request.Delay)
                        break; // return path
                    else // remove bottle neck link then continue finding
                    {
                        double minRB = path.Min(l => l.ResidualBandwidth);
                        Link bottleNeckLink = path.FirstOrDefault(l => l.ResidualBandwidth == minRB);
                        oldResidualBandwidths[bottleNeckLink] = bottleNeckLink.ResidualBandwidth;
                        bottleNeckLink.ResidualBandwidth = 0; // chú ý!
                    }
                }
                else // no path
                {
                    break;
                }
            }

            // restore
            foreach (var item in oldResidualBandwidths)
            {
                item.Key.ResidualBandwidth = item.Value;
            }                                    

            return path;
        }
    }
}

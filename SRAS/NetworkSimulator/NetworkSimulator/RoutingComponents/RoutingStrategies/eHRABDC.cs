using NetworkSimulator.NetworkComponents;
using NetworkSimulator.SimulatorComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class eHRABDC : RoutingStrategy
    {
        HeuristicDijkstra _heDi;

        public eHRABDC(Topology topology)
            : base(topology)
        {
            _Topology = topology;

            _heDi = new HeuristicDijkstra(_Topology);
        }

        

        public override List<Link> GetPath(Request request)
        {
            HashSet<string> eliminatedLinks = new HashSet<string>();
            Dictionary<string, double> weights = new Dictionary<string, double>();
            Dictionary<string, double> delays = new Dictionary<string, double>();

            foreach (var link in _Topology.Links)
            {
                if (link.UsingBandwidth == 0)
                    weights[link.Key] = 1d / (link.Capacity);
                else
                    weights[link.Key] = link.UsingBandwidth / (link.ResidualBandwidth);

                delays[link.Key] = link.Delay;
                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link.Key);
            }

            // enhanced heuristic

            var tempPath = _heDi.FindOptimalPath(
                _Topology, eliminatedLinks, request.SourceId, request.DestinationId, weights, delays, request.Delay);

            var path = tempPath;
            
            while (tempPath.Count > 0)
            {
                //foreach (var link in tempPath)
                //    Console.Write(link.Key + "-");
                //Console.WriteLine(tempPath.Sum(l => w1[l.Key]));
                if (tempPath.Sum(l => weights[l.Key]) < path.Sum(l => weights[l.Key]))
                    path = tempPath;
                //if (tempPath.Sum(l => w1[l.Key]) * tempPath.Count < path.Sum(l => w1[l.Key]) * path.Count)
                //    path = tempPath;

                double maxWeight = tempPath.Max(l => weights[l.Key]);
                var cl = tempPath.Where(l => weights[l.Key] == maxWeight).ToList();
                foreach (var link in cl)
                    eliminatedLinks.Add(link.Key);

                tempPath = _heDi.FindOptimalPath(
                    _Topology, eliminatedLinks, request.SourceId, request.DestinationId, weights, delays, request.Delay);
            }

            if (path.Sum(l => l.Delay) > request.Delay)
                throw new Exception("Not feasible path");

            if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
                throw new Exception("Residual bandwidth less than 0");

            return path;
        }
    }
}

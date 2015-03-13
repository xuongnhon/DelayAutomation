using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class HBDRA : RoutingStrategy
    {
        public static double epsilon = 0.01;

        public HBDRA(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

        private void Normalize(Dictionary<Link, double> data, int a, int b)
        {
            double A = data.Values.Min();
            double B = data.Values.Max();
            List<Link> links = data.Keys.ToList();
            foreach (var link in links)
            {
                double x = data[link];
                if (A - B != 0)
                    data[link] = a + (x - A) * (b - a) / (B - A);
                else
                    data[link] = b;
            }
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            Dictionary<Link, double> weight = new Dictionary<Link, double>();
            Dictionary<Link, double> delay = new Dictionary<Link, double>();
            foreach (var link in _Topology.Links)
            {
                weight[link] = link.UsingBandwidth / link.ResidualBandwidth;
                delay[link] = link.Delay;
            }

            Normalize(weight, 1, 1000);
            Normalize(delay, 1, 1000);

            EliminateAllLinksNotSatisfy(request.Demand);

            Dictionary<Link, double> LW = new Dictionary<Link, double>();
            //double epsilon = 0.01;

            Dijkstra d = new Dijkstra(_Topology);
            List<Link> bestPath = new List<Link>();

            for (double alpha = 0; alpha <= 1; alpha += epsilon)
            {
                foreach (var link in _Topology.Links)
                    LW[link] = alpha * weight[link] + delay[link];

                var path = d.GetShortestPath(request.SourceId, request.DestinationId, LW);
                if (path.Sum(l => l.Delay) > request.Delay)
                    break;
                bestPath = path;
            }

            RestoreTopology();

            if (bestPath.Sum(l => l.Delay) > request.Delay)
                throw new Exception("Not feasible path");

            if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
                throw new Exception("Residual bandwidth less than 0");

            return bestPath;
        }
    }
}

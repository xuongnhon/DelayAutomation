using NetworkSimulator.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class BDCRA : RoutingStrategy
    {
        private static readonly double MaxValue = double.MaxValue;

        public BDCRA(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

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

                // Browse all adjacent node that not contain in E to update distance from s.
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

        int debug = 0;

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            Dictionary<Link, double> teml = new Dictionary<Link, double>();
            List<List<Link>> ldpSet = new List<List<Link>>();
            HashSet<Link> criticalLinks = new HashSet<Link>();
            HashSet<Link> eliminatedLinks = new HashSet<Link>();

            foreach (var link in _Topology.Links)
            {
                // caoth + Dung khung
                //teml[link] = (link.UsingBandwidth * Math.Pow(10, 8)) / link.Capacity;
                //teml[link] = (link.UsingBandwidth * Math.Pow(10, 8)) / link.ResidualBandwidth;

                if (link.ResidualBandwidth < request.Demand)
                {
                    eliminatedLinks.Add(link);
                    continue;
                }

                // Fixed link weight (Liep Nguyen)
                double load = link.UsingBandwidth / link.ResidualBandwidth;
                double cost = Math.Pow(10, 8) / link.Capacity;
                teml[link] = load * cost;
            }

            var ldp = FindLeastDelayPath(request.SourceId, request.DestinationId, eliminatedLinks);
            while (ldp.Count > 0 && ldp.Sum(l => l.Delay) <= request.Delay)
            {
                ldpSet.Add(ldp);

                // Find highest teml value in ldp path
                double maxTeml = teml.Where(e => ldp.Contains(e.Key)).Max(e => e.Value);
                var cl = ldp.Where(l => teml[l] == maxTeml).ToList();

                foreach (var link in cl)
                    eliminatedLinks.Add(link);

                ldp = FindLeastDelayPath(request.SourceId, request.DestinationId, eliminatedLinks);
            }

            //if (debug++ == 5)
            //{ }

            var ldlcPath = ldpSet.FirstOrDefault();
            double minSumTeml = MaxValue;
            foreach (var p in ldpSet)
            {
                double sumTeml = teml.Where(e => p.Contains(e.Key)).Sum(e => e.Value);
                if (sumTeml < minSumTeml)
                {
                    ldlcPath = p;
                    minSumTeml = sumTeml;
                }
            }

            ldlcPath = ldlcPath ?? new List<Link>();

            if (ldlcPath.Sum(l => l.Delay) > request.Delay)
                throw new Exception("Not feasible path");

            if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
                throw new Exception("Residual bandwidth less than 0");

            return ldlcPath;
        }
    }
}

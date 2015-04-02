using NetworkSimulator.NetworkComponents;
using NetworkSimulator.SimulatorComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class New_eHRABDC : RoutingStrategy
    {
        private static readonly double MaxValue = 10000;

        public New_eHRABDC(Topology topology)
            : base(topology)
        {
            _Topology = topology;
        }

        // Calculate least delay for each node to destination node.
        private double[] ComputeLeastWeight(Topology topology, HashSet<string> E, int d, Dictionary<string, double> w)
        {
            // Copy topology and reverse all 
            Topology copyTopology = new Topology(topology);
            ReverseLinkDirection(copyTopology);
            Dictionary<string, double> wc = new Dictionary<string, double>();
            foreach (var item in w)
            {
                string[] str = item.Key.Split('|');
                string rKey = str[1] + '|' + str[0];
                wc[rKey] = item.Value;
            }
            HashSet<string> Ec = new HashSet<string>();
            foreach (var item in E)
            {
                string[] str = item.Split('|');
                string rKey = str[1] + '|' + str[0];
                Ec.Add(rKey);
            }

            // Initialize
            int nv = topology.Nodes.Count;
            double[] dist = new double[nv];

            for (int v = 0; v < nv; v++)
                dist[v] = MaxValue;
            dist[d] = 0;

            var Q = new List<Node>(copyTopology.Nodes);

            while (Q.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]:z within T
                var u = Q.First();
                foreach (var node in Q)
                    if (dist[node.Key] < dist[u.Key])
                        u = node;

                Q.Remove(u);

                // Browse all adjacent node to update distance from s.
                var links = u.Links.Where(l => !Ec.Contains(l.Key)).ToList();
                foreach (var link in links)
                {
                    var v = link.Destination;
                    if (dist[v.Key] > dist[u.Key] + wc[link.Key])
                        dist[v.Key] = dist[u.Key] + wc[link.Key];
                }
            }

            return dist;
        }

        // Reserver direction of all links in topology
        private void ReverseLinkDirection(Topology topology)
        {
            List<Link> links = topology.Links;
            foreach (var link in links)
            {
                link.Source.Links.Remove(link);
                Link rLink = new Link(link.Destination, link.Source, link.Capacity, link.UsingBandwidth, link.Delay);
                rLink.Source.Links.Add(rLink);
            }

            // Update link set
            topology.Links = null;
        }

        public List<Link> FindOptimalPath(
            Topology t, HashSet<string> E, int s, int d, Dictionary<string, double> w1, Dictionary<string, double> w2, double c2)
        {
            int nv = t.Nodes.Count;
            double[] dist = new double[nv];
            int[] prev = new int[nv];

            // Delay so far of each node from source
            double[] w2SoFar = new double[nv];

            // Least delay from each node to destination node
            double[] leastW2ToDestination = ComputeLeastWeight(t, E, d, w2);

            for (int v = 0; v < nv; v++)
            {
                w2SoFar[v] = 0;
                dist[v] = MaxValue;
                prev[v] = -1;
            }
            dist[s] = 0;

            var Q = new List<Node>(t.Nodes);

            while (Q.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]:z within T (Extract-Min if we use priority heap)
                var u = Q.First();
                foreach (var node in Q)
                    if (dist[node.Key] < dist[u.Key])
                        u = node;

                Q.Remove(u);

                // Browse all adjacent node that can satisfy delay constraint to update total link weight from s.
                List<Link> links = u.Links
                    .Where(l => (w2SoFar[u.Key] + w2[l.Key] + leastW2ToDestination[l.Destination.Key]) <= c2
                        && !E.Contains(l.Key)).ToList();

                foreach (var link in links)
                {
                    var v = link.Destination;
                    // Relax
                    if (dist[v.Key] > dist[u.Key] + w1[link.Key])
                    {
                        dist[v.Key] = dist[u.Key] + w1[link.Key];
                        w2SoFar[v.Key] = w2SoFar[u.Key] + w2[link.Key];
                        prev[v.Key] = u.Key;
                    }
                }
            }

            return ConstructPath(t, prev, d);
        }

        private List<Link> ConstructPath(Topology t, int[] prev, int d)
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

        public override List<Link> GetPath(Request request)
        {
            HashSet<string> eliminatedLinks = new HashSet<string>();
            Dictionary<string, double> w1 = new Dictionary<string, double>();
            Dictionary<string, double> w2 = new Dictionary<string, double>();
            foreach (var link in _Topology.Links)
            {
                if (link.UsingBandwidth == 0)
                    w1[link.Key] = 1d / (link.Capacity);
                else
                    w1[link.Key] = link.UsingBandwidth / (link.ResidualBandwidth);
                w2[link.Key] = link.Delay;
                if (link.ResidualBandwidth < request.Demand)
                    eliminatedLinks.Add(link.Key);
            }

            var tempPath = FindOptimalPath(
                _Topology, eliminatedLinks, request.SourceId, request.DestinationId, w1, w2, request.Delay);

            var path = tempPath;

            while (tempPath.Count > 0)
            {
                //foreach (var link in tempPath)
                //    Console.Write(link.Key + "-");
                //Console.WriteLine(tempPath.Sum(l => w1[l.Key]));
                if (tempPath.Sum(l => w1[l.Key]) < path.Sum(l => w1[l.Key]))
                    path = tempPath;
                //if (tempPath.Sum(l => w1[l.Key]) * tempPath.Count < path.Sum(l => w1[l.Key]) * path.Count)
                //    path = tempPath;

                double maxWeight = tempPath.Max(l => w1[l.Key]);
                var cl = tempPath.Where(l => w1[l.Key] == maxWeight).ToList();
                foreach (var link in cl)
                    eliminatedLinks.Add(link.Key);

                tempPath = FindOptimalPath(
                    _Topology, eliminatedLinks, request.SourceId, request.DestinationId, w1, w2, request.Delay);
            }

            if (path.Sum(l => l.Delay) > request.Delay)
                throw new Exception("Not feasible path");

            if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
                throw new Exception("Residual bandwidth less than 0");

            return path;
        }
    }
}

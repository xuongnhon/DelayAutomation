using NetworkSimulator.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    // caoth // extended Dijkstra
    public class HeuristicDijkstra
    {
        private static readonly double MaxValue = 10000;
        private Topology _Topology;

        public HeuristicDijkstra(Topology topology)
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
            int numberOfNodes = topology.Nodes.Count;
            double[] dist = new double[numberOfNodes];

            for (int v = 0; v < numberOfNodes; v++)
                dist[v] = MaxValue;
            dist[d] = 0;

            var Q = new List<Node>(copyTopology.Nodes);

            while (Q.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]} z within T
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

        // Reverse direction of all links in topology
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
             Topology t, HashSet<string> eliminatedLinks, int s, int d, Dictionary<string, double> weights, Dictionary<string, double> delays, double delta)
        {
            int nv = t.Nodes.Count;
            double[] dist = new double[nv]; // index array is key node !
            int[] prev = new int[nv];

            // Delay so far of each node from source
            double[] delaySoFar = new double[nv];

            // Least delay from each node to destination node
            double[] leastW2ToDestination = ComputeLeastWeight(t, eliminatedLinks, d, delays);

            for (int v = 0; v < nv; v++)
            {
                delaySoFar[v] = 0;
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
                    .Where(l => (delaySoFar[u.Key] + delays[l.Key] + leastW2ToDestination[l.Destination.Key]) <= delta
                        && !eliminatedLinks.Contains(l.Key)).ToList();

                foreach (var link in links)
                {
                    var v = link.Destination;
                    // Relax
                    if (dist[v.Key] > dist[u.Key] + weights[link.Key])
                    {
                        dist[v.Key] = dist[u.Key] + weights[link.Key];
                        delaySoFar[v.Key] = delaySoFar[u.Key] + delays[link.Key];
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
    }
}

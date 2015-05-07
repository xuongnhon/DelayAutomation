using NetworkSimulator.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    // caoth // extended Dijkstra
    public class EDSP
    {
        private Topology _Topology;

        public EDSP(Topology topology)
        {
            _Topology = topology;    
        }

        private static readonly double MaxValue = 10000;

        public List<Link> FindFeasiblePath(
            int s, int d, HashSet<Link> E, Dictionary<Link, double> weights, Dictionary<Link, int> delays, int delta)
        {
            int numberOfNodes = _Topology.Nodes.Count;
            double[,] dist = new double[numberOfNodes, delta + 1];
            int[,] prev = new int[numberOfNodes, delta + 1];

            for (int v = 0; v < numberOfNodes; v++)
                for (int i = 0; i <= delta; i++)
                {
                    dist[v, i] = MaxValue;
                    prev[v, i] = -1;
                }

            // distance at source node
            for (int i = 0; i <= delta; i++)
            {
                dist[s, i] = 0;
            }

            List<int[]> Q = new List<int[]>();
            for (int v = 0; v < numberOfNodes; v++)
                for (int i = 0; i <= delta; i++)
                    Q.Add(new int[] {v, i});

            while (Q.Count > 0)
            {
                // Extract min u-k pair in Q, u is NodeKey, k is the k_th entry (k is also the delay
                int[] minUK = Q.First();
                foreach (var uk in Q)
                {
                    if (dist[uk[0], uk[1]] < dist[minUK[0], minUK[1]])
                    {
                        minUK = uk;
                    }
                }
                
                Q.Remove(minUK);

                int u = minUK[0], k = minUK[1];

                // For each outgoing edge of u
                List<Link> outGoingLink = _Topology.Nodes[u].Links.Where(l => !E.Contains(l)).ToList();
                foreach (var link in outGoingLink)
                {
                    int v = link.Destination.Key;
                    // Relax
                    int kc = k + delays[link];
                    if (kc <= delta)
                        if (dist[v, kc] > dist[u, k] + weights[link])
                        {
                            dist[v, kc] = dist[u, k] + weights[link];
                            prev[v, kc] = u;
                        }
                }
            }

            return ConstructPath(prev, dist, d, delays, delta);
        }

        private List<Link> ConstructPath(int[,] prev, double[,] dist, int d, Dictionary<Link, int> w2, int x)
        {
            int v = d, k = 0;
            double minC = MaxValue;
            for (int i = 0; i <= x; i++)
                if (dist[d, i] < minC)
                {
                    minC = dist[d, i];
                    k = i;
                }

            List<Link> path = new List<Link>();
            while (prev[v, k] != -1)
            {
                int u = prev[v, k];
                var link = _Topology.GetLink(u, v);
                path.Add(link);
                v = u;
                k -= w2[link];
            }
            path.Reverse();

            return path;
        }
    }
}

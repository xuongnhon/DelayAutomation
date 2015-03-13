using NetworkSimulator.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    public class EDSP
    {
        private Topology _Topology;

        public EDSP(Topology topology)
        {
            _Topology = topology;    
        }

        private static readonly double MaxValue = 10000;

        public List<Link> FindFeasiblePath(
            int s, int d, HashSet<Link> E, Dictionary<Link, double> w1, Dictionary<Link, int> w2, int x)
        {
            int nv = _Topology.Nodes.Count;
            double[,] dist = new double[nv, x + 1];
            int[,] prev = new int[nv, x + 1];

            for (int v = 0; v < nv; v++)
                for (int i = 0; i <= x; i++)
                {
                    dist[v, i] = MaxValue;
                    prev[v, i] = -1;
                }

            for (int i = 0; i <= x; i++)
            {
                dist[s, i] = 0;
            }

            List<int[]> Q = new List<int[]>();
            for (int v = 0; v < nv; v++)
                for (int i = 0; i <= x; i++)
                    Q.Add(new int[] {v, i});

            while (Q.Count > 0)
            {
                // Extract min u-k pair in Q
                int[] minUK = Q.First();
                foreach (var uk in Q)
                    if (dist[uk[0], uk[1]] < dist[minUK[0], minUK[1]])
                        minUK = uk;
                Q.Remove(minUK);

                int u = minUK[0], k = minUK[1];

                // For each outgoing edge of u
                List<Link> outGoingLink = _Topology.Nodes[u].Links.Where(l => !E.Contains(l)).ToList();
                foreach (var link in outGoingLink)
                {
                    int v = link.Destination.Key;
                    // Relax
                    int kc = k + w2[link];
                    if (kc <= x)
                        if (dist[v, kc] > dist[u, k] + w1[link])
                        {
                            dist[v, kc] = dist[u, k] + w1[link];
                            prev[v, kc] = u;
                        }
                }
            }

            return ConstructPath(prev, dist, d, w2, x);
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

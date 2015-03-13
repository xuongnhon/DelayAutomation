using NetworkSimulator.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    public class LDP
    {
        private Topology _Topology;

        public LDP(Topology topology)
        {
            this._Topology = topology;
        }

        public List<Link> FindLeastDelayPath(int s, int d, HashSet<Link> E)
        {
            // Initialize
            int nv = _Topology.Nodes.Count;
            double[] dist = new double[nv];
            int[] prev = new int[nv];

            for (int v = 0; v < nv; v++)
            {
                dist[v] = 999999;
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
    }
}

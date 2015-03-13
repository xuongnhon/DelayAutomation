using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    class ShortestPathsSet
    {
        private Topology _Topology;


        public ShortestPathsSet(Topology topology)
        {
            _Topology = topology;
        }

        List<int> _onepath = new List<int>();
        List<List<int>> _result = new List<List<int>>();
        private void Trace(List<int>[] pre, int des, int source)
        {
            _onepath.Add(des);
            if (source == des)
            {
                _result.Add(new List<int>(_onepath));
                _onepath.Remove(des);
            }   
            else
            {
                foreach (var item in pre[des])
                {
                    Trace(pre, item, source);
                }
                _onepath.Remove(des);
            }
        }

        /// <summary>
        /// Find a set of paths having the same length
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="linkcost"></param>
        /// <returns></returns>
        public List<List<Link>> GetShortestPathSet(Node source, Node destination, Dictionary<Link, double> linkcost)
        {
            int n = _Topology.Nodes.Count;
            double[] d = new double[n];
            List<int>[] pre = new List<int>[n];

            for (int v = 0; v < n; v++)
            {
                d[v] = double.MaxValue;
                pre[v] = new List<int>();
            }

            d[source.Key] = 0;
            List<Node> T = new List<Node>(_Topology.Nodes);

            while (T.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]:z within T set}
                int u = T[0].Key;
                foreach (Node node in T)
                    if (d[node.Key] < d[u])
                        u = node.Key;

                T.Remove(_Topology.Nodes[u]);

                // Browse all adjacent node to update distance from s.
                foreach (Link link in _Topology.Nodes[u].Links)
                {
                    if (link.ResidualBandwidth > 0)
                    {
                        int v = link.Destination.Key;
                        if (d[v] > d[u] + linkcost[link])
                        {
                            d[v] = d[u] + linkcost[link];
                            pre[v].Clear();
                            pre[v].Add(u);
                        }
                        else if (d[v] == d[u] + linkcost[link])
                        {
                            d[v] = d[u] + linkcost[link];
                            pre[v].Add(u);
                        }
                    }
                }
            }

            Trace(pre, destination.Key, source.Key);

            List<List<Link>> result = new List<List<Link>>();
            foreach (var path in _result)
            {
                List<Link> p = new List<Link>();
                for (int i = path.Count - 1; i > 0;  i--)
                {

                    Link l = _Topology.GetLink(_Topology.Nodes[path[i]],_Topology.Nodes[path[i - 1]]);
                   // Link l = new Link(new Node(path[i]), new Node(path[i - 1]),0);
                    p.Add(l);
                }
                result.Add(p);
            }
            return result;
        }

    }
}

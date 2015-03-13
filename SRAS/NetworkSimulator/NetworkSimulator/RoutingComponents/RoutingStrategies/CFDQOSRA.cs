using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class CFDQOSRA
    {
        private FloydWarshall _Floyd;

        private Topology _Topology;

        private double[,] _MinimunDelay;

        public CFDQOSRA(Topology topology)
        {
            _Topology = topology;
            _Floyd = new FloydWarshall();
            Initialize();
        }

        private void Initialize()
        {
            Dictionary<string, double> delay = new Dictionary<string, double>();
            foreach (var link in _Topology.Links)
                delay[link.Key] = link.Delay;
            //_MinimunDelay = _Floyd.GetMinimumDistances(_Topology, delay);
        }

        int debug = 0;

        public void GetPath(int s, int d, double bandwidth, double delayLimited, Dictionary<string, double> cost)
        {
            Dictionary<Node, double> distance = new Dictionary<Node, double>();
            Dictionary<Node, Node> previous = new Dictionary<Node, Node>();
            Dictionary<Node, double> delay = new Dictionary<Node, double>();

            foreach (var node in _Topology.Nodes)
            {
                delay[node] = 0;
                distance[node] = double.MaxValue;
                previous[node] = null;
            }
            distance[_Topology.Nodes[s]] = 0;

            var T = new List<Node>(_Topology.Nodes);

            while (T.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]:z within T
                var u = T.First();
                foreach (var node in T)
                {
                    if (distance[node] < distance[u])
                        u = node;
                }

                T.Remove(u);

                //if (++debug == 2)
                //{ }

                // Browse all adjacent node to update distance from s.
                List<Link> links = u.Links
                    .Where(l => (delay[u] + l.Delay + _MinimunDelay[l.Destination.Key, d]) <= delayLimited).ToList();
                foreach (var link in links)
                {
                    var v = link.Destination;
                    if (distance[v] > distance[u] + cost[link.Key])
                    {
                        distance[v] = distance[u] + cost[link.Key];
                        previous[v] = u;
                        delay[v] = delay[u] + link.Delay;
                    }
                }
            }

            Node cur = _Topology.Nodes[d];
            List<Link> path = new List<Link>();

            while (previous[cur] != null)
            {
                var link = _Topology.GetLink(previous[cur], cur);
                path.Add(link);
                cur = previous[cur];
            }
            path.Reverse();
            foreach (var link in path)
            {
                Console.WriteLine(link);
            }
            Console.WriteLine("------------------------------- Distance: " + distance[_Topology.Nodes[d]] + " Delay: " + path.Sum(l => l.Delay));
        }
    }
}

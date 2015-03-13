using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.MulticastSimulatorComponents;

namespace NetworkSimulator.RoutingComponents.MulticastRoutingStrategies
{
    public class SPT : MulticastRoutingStrategy
    {
        protected MulticastDijkstra _MD;

        public SPT(Topology topology) 
            : base(topology)
        {
            Initialize();
        }

        private void Initialize()
        {
            _MD = new MulticastDijkstra(_Topology);
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            throw new NotImplementedException("for Unicast");
            //return temp;
        }

        public override Tree GetTree(MulticastRequest request)
        {
            //List<Link> temp = new List<Link>();
            //Console.WriteLine("Hello");

            List<Node> des = new List<Node>();
            foreach (int id in request.Destinations)
                des.Add(_Topology.Nodes[id]);

            EliminateAllLinksNotSatisfy(request.Demand);
            Tree tree = _MD.GetShortestTree(_Topology.Nodes[request.SourceId], des);
            RestoreTopology();
            return tree;

            //return temp;

            // caoth
            // node.Children = new List<Node>();
            // new Node, keep Link
            // hien nay, chi can List<List<Link>>
            // van lam thu cau truc tree day du, co root
        }

    }

    public class MulticastDijkstra
    {
        private Topology _Topology;

        internal Topology Topology
        {
            get { return _Topology; }
        }

        private Dictionary<Node, Node> _Previous;

        private Dictionary<Node, double> _Distance;

        private const double MaxValue = double.MaxValue;

        public MulticastDijkstra(Topology topology)
        {
            _Topology = topology;
            Initialize();
        }

        private void Initialize()
        {
            _Distance = new Dictionary<Node, double>();
            _Previous = new Dictionary<Node, Node>();
        }

        //14.08.27 HTT: 2. Tim duong di ngan nhat tu nut src den moi nut des
		public Tree GetShortestTree(Node source, List<Node> destination, Dictionary<Link, double> cost)
        {
            // Initialize
            _Distance.Clear();
            _Previous.Clear();
            foreach (var node in _Topology.Nodes)
            {
                _Distance[node] = MaxValue;
                _Previous[node] = null;
            }
            _Distance[source] = 0;

            var T = new List<Node>(_Topology.Nodes);
            var DT = new List<Node>(destination); // DT: destination temp
            while (T.Count > 0 && DT.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]:z within T
                var u = T.First();
                foreach (var node in T)
                {
                    if (_Distance[node] < _Distance[u])
                        u = node;
                }

                T.Remove(u);
                DT.Remove(u);
                // Browse all adjacent node to update distance from s.
                foreach (var link in u.Links.Where(l => l.ResidualBandwidth > 0))
                {
                    var v = link.Destination;
                    if (_Distance[v] > _Distance[u] + cost[link])
                    {
                        _Distance[v] = _Distance[u] + cost[link];
                        _Previous[v] = u;
                    }
                }
            }

            return GetTree(destination);
        }
        
		//14.08.27 HTT: 1. Gan trong so = 1 cho tat ca link
		public Tree GetShortestTree(Node source, List<Node> destination)
        {
            Dictionary<Link, double> cost = new Dictionary<Link, double>();
            
			foreach (var link in _Topology.Links)
            {
                cost.Add(link, 1);
            }

            return GetShortestTree(source, destination, cost);
        }

        //14.08.27 HTT: 3. Tap hop cac duong di ngan nhat thanh multicast tree
		private Tree GetTree(List<Node> destination)
        {
            
            Tree t = new Tree();
            foreach (var node in destination)
            {
                var current = node;
                var path = new List<Link>();
                while (_Previous[current] != null)
                {
                    var link = _Topology.GetLink(_Previous[current], current);
                    path.Add(link);
                    current = _Previous[current];
                }
                path.Reverse();
                t.Paths.Add(path);
            }
            return t;
        }
    }
}

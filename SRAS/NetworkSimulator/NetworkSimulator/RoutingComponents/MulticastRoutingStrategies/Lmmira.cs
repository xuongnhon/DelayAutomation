using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.MulticastSimulatorComponents;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using System.Threading;
using NetworkSimulator.RoutingComponents.MulticastRoutingStrategies;

namespace NetworkSimulator.RoutingComponents.MulticastCommonAlgorithms
{
    public class Lmmira : MulticastRoutingStrategy
    {
        private LmmiraCore _LCore;

        protected MulticastDijkstra _MD;

        public Dictionary<Link, double> cost = new Dictionary<Link, double>();

        public Lmmira(Topology topology): base(topology)
        {            
            Initialize();
        }

        private void Initialize()
        {
            cost = new Dictionary<Link, double>();
            _MD = new MulticastDijkstra(_Topology);

            _LCore = new LmmiraCore(_Topology, 1, 0, 15*1000, this);
            _LCore.Start();            
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            throw new NotImplementedException("for Unicast");
            //return temp;
        }

        public override Tree GetTree(MulticastRequest request)
        {
            List<Node> des = new List<Node>();
            foreach (int id in request.Destinations)
                des.Add(_Topology.Nodes[id]);

            EliminateAllLinksNotSatisfy(request.Demand);
            Tree tree = new Tree();
            lock (cost)
            {
                tree = _MD.GetShortestTree(_Topology.Nodes[request.SourceId], des, cost);
            }            
            RestoreTopology();
            return tree;
        }
       
    }
}

using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class LDPRA : RoutingStrategy
    {
        private Dijkstra _Dijkstra;

        private Dictionary<Link, double> _CostLink;

        public LDPRA(Topology topology)
            : base(topology)
        {
            Initialize();
        }

        private void Initialize()
        {
            _Dijkstra = new Dijkstra(_Topology);

            _CostLink = new Dictionary<Link, double>();
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            List<Link> resultPath = new List<Link>();

            EliminateAllLinksNotSatisfy(request.Demand);

            foreach (var link in _Topology.Links)
            {
                _CostLink[link] = link.Delay;
            }

            resultPath = _Dijkstra.GetShortestPath(_Topology.Nodes[request.SourceId], _Topology.Nodes[request.DestinationId], _CostLink);

            double totalDelay = 0;
            foreach (var link in resultPath)
            {
                totalDelay += link.Delay;
            }

            if (totalDelay > request.Delay)
            {
                resultPath = new List<Link>();
            }

            RestoreTopology();

            return resultPath;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    class FordFulkerson
    {
        private Topology _Topology;

        private BreadthFirstSearch _BFS;

        private const double MaxValue = double.MaxValue;

        private Dictionary<Link, double> _backupResidualBandwidthCopy;

        public FordFulkerson(Topology topology)
        {
            _Topology = topology;
            Initialize();
        }

        private Dictionary<Link, double> _UsingBandwidthCopy;

        private void Initialize()
        {
            _BFS = new BreadthFirstSearch(_Topology);
            _UsingBandwidthCopy = new Dictionary<Link, double>();

            _backupResidualBandwidthCopy = new Dictionary<Link, double>();
        }

        private void BackupTopology()
        {
            _UsingBandwidthCopy.Clear();
            _backupResidualBandwidthCopy.Clear();
            foreach (var link in _Topology.Links)
            {
                _UsingBandwidthCopy[link] = link.UsingBandwidth;
            }
        }

        private void RestoreTopology()
        {
            foreach (var link in _Topology.Links)
            {
                link.UsingBandwidth = _UsingBandwidthCopy[link];
            }
        }

        public double ComputeMaxFlow(Node source, Node destination)
        {
            BackupTopology();
            var maxFlow = MaxFlow(source, destination);
            RestoreTopology();

            return maxFlow;
        }

        private double MaxFlow(Node nodeSource, Node nodeTerminal)
        {
            var flow = 0d;

            var path = _BFS.FindPath(nodeSource, nodeTerminal);

            while (path.Count > 0)
            {
                var minCapacity = MaxValue;
                foreach (var link in path)
                {
                    if (link.ResidualBandwidth < minCapacity)
                        minCapacity = link.ResidualBandwidth;
                }

                if (minCapacity == MaxValue || minCapacity < 0)
                    throw new Exception("minCapacity " + minCapacity);

                AugmentPath(path, minCapacity);

                flow += minCapacity;

                path = _BFS.FindPath(nodeSource, nodeTerminal);
            }

            return flow;
        }
 
        private void AugmentPath(IEnumerable<Link> path, double minCapacity)
        {
            foreach (var link in path)
            {
                link.UsingBandwidth += minCapacity;
            }
        }

        public List<Link> FindMinCutSet(Node source, Node destination) // caoth redo
        //public List<Link> FindMinCutSetOld(Node source, Node destination)
        {
            BackupTopology();
            double flow = MaxFlow(source, destination);

            var sCut = new List<Node>();
            var tCut = new List<Node>();
            var minCutSet = new List<Link>();

            // find sCut
            _BFS.FindPathMarkNode(source, source, sCut);

            // find tCut
            _Topology.Invert();
            _BFS.FindPathMarkNode(destination, destination, tCut);
            _Topology.Invert();

            // find mincut sets
            foreach (Link link in _Topology.Links)
            {
                if (link.ResidualBandwidth == 0 && _backupResidualBandwidthCopy[link] > 0)
                    if (!sCut.Contains(link.Destination) && !tCut.Contains(link.Source))
                        if (_BFS.FindPath(link.Source, link.Destination).Count == 0)
                            minCutSet.Add(link);
            }

            //foreach (var node in _Topology.Nodes)
            //{
            //    if (_BFS.FindPath(node, destination).Count == 0 && node != destination)
            //        sCut.Add(node);
            //    else
            //        tCut.Add(node);

            //}

            //foreach (var sNode in sCut)
            //{
            //    foreach (var link in sNode.Links)
            //    {
            //        if (link.ResidualBandwidth <= 0 && tCut.Contains(link.Destination))
            //            minCutSet.Add(link);
            //    }
            //}

            RestoreTopology();

            return minCutSet;
        }

        public double SubFlow(Node source, Node destination, Link subLink)
        {
            double subflow;
            BackupTopology();
            MaxFlow(source, destination);
            subflow = _Topology.GetLink(subLink.Source, subLink.Destination).UsingBandwidth;
            RestoreTopology();
            return subflow;
        }
    }
}

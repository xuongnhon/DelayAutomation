using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.SimulatorComponents;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public abstract class RoutingStrategy
    {
        protected Topology _Topology;
        protected Dictionary<Link, double> _ResidualBandwidthCopy;

        //Nhon
        public double weightPath { get; protected set; }

        public RoutingStrategy(Topology topology)
        {
            _Topology = topology;
            _ResidualBandwidthCopy = new Dictionary<Link, double>();
            //Nhon
            weightPath = 0;
        }

        // caoth: review :((
        public abstract List<Link> GetPath(Request request); //--> MUST DO

        //public abstract List<Link> GetPath(int sourceId, int destinationId, double bandwidth);


        //public abstract List<Link> GetPath(int sourceId, int destinationId, double bandwidth, long incomingTime, long responseTime, long releasingTime);

        //public abstract void Reroute();

        /// <summary>
        /// Eliminate all links which have residual bandwidth less than demand and from a reduced network.
        /// </summary>
        /// <param name="demand">Bandwidth demand</param>
        protected void EliminateAllLinksNotSatisfy(double demand)
        {
            foreach (Link link in _Topology.Links)
            {
                _ResidualBandwidthCopy[link] = link.ResidualBandwidth;
            }

            foreach (Link link in _Topology.Links)
            {
                if (link.ResidualBandwidth < demand)
                    link.ResidualBandwidth = 0;
            }
        }

        /// <summary>
        /// Restore topology 
        /// </summary>
        protected void RestoreTopology()
        {
            foreach (Link link in _Topology.Links)
            {
                link.ResidualBandwidth = _ResidualBandwidthCopy[link];
            }
        }

        protected void CalculateWeightPath(Dictionary<Link, double> _Weight, List<Link> _Path)
        {
            if (_Path.Count > 0)
            {
                var sumWeight = (from _WeightItem in _Weight
                                 from _Link in _Path
                                 where Object.Equals(_WeightItem.Key, _Link)
                                 select _WeightItem.Value).Sum();
                weightPath = sumWeight;
            }
            else weightPath = 0;
        }

        protected void CalculateWeightPath(Dictionary<string, double> _Weight, List<Link> _Path)
        {
            if (_Path.Count > 0)
            {
                var sumWeight = (from _WeightItem in _Weight
                                 from _Link in _Path
                                 where Object.Equals(_WeightItem.Key, _Link.Key)
                                 select _WeightItem.Value).Sum();
                weightPath = sumWeight;
            }
            else weightPath = 0;
        }
    }
}

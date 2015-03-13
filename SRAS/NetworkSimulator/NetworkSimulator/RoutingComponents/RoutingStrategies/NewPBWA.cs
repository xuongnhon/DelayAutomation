using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class NewPBWA : RoutingStrategy
    {
        long _WindowSize;

        Random r_troj;
        Dijkstra _Dijkstra;

        static Dictionary<Link, List<long>> _LinkReleaseTime;
        static Dictionary<Link, List<double>> _LinkReleaseBandwidth;

        static List<long> _RequestICT;
        static List<long> _RequestHDT;
        static List<double> _RequestBandwidth;


        Dictionary<Link, double> _LinkCost;

        public NewPBWA(Topology topology)
            : base(topology)
        {
            r_troj = new Random();
            _LinkReleaseTime = new Dictionary<Link, List<long>>();
            _LinkReleaseBandwidth = new Dictionary<Link, List<double>>();
            _RequestICT = new List<long>();
            _RequestHDT = new List<long>();
            _RequestBandwidth = new List<double>();
            _LinkCost = new Dictionary<Link, double>();
            _Dijkstra = new Dijkstra(topology);
            Initialize();
        }

        private void Initialize()
        {
            foreach (var link in _Topology.Links)
            {
                _LinkReleaseTime[link] = new List<long>();
                _LinkReleaseBandwidth[link] = new List<double>();
            }
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            List<Link> path = new List<Link>();
            EliminateAllLinksNotSatisfy(request.Demand);

            _RequestICT.Add(request.IncomingTime);
            _RequestBandwidth.Add(request.Demand);
            
            long minreleasetime = request.IncomingTime + request.HoldingTime;

            #region Remove value of released requests
            foreach (var link in _Topology.Links)
            {
                for (int i = 0; i < _LinkReleaseTime[link].Count; i++)
                {
                    if (_LinkReleaseTime[link][i] <= request.IncomingTime)
                    {
                        _LinkReleaseTime[link].RemoveAt(i);
                        _LinkReleaseBandwidth[link].RemoveAt(i);
                    }
                    else
                    {
                        if (_LinkReleaseTime[link][i] < minreleasetime)
                        {
                            minreleasetime = _LinkReleaseTime[link][i];
                        }
                    }
                }
            }
            #endregion
         
            _WindowSize = minreleasetime;

            Console.WriteLine("_WindowSize = " + _WindowSize);

            foreach (var link in _Topology.Links)
            {
                double totalBw = 0;
                for (int i = 0; i < _LinkReleaseTime[link].Count; i++)
                {
                    if (_LinkReleaseTime[link][i] <= request.IncomingTime + _WindowSize && request.HoldingTime != long.MaxValue)
                    {
                        totalBw += _LinkReleaseBandwidth[link][i];
                    }
                }
                _LinkCost[link] = 1 / (totalBw + link.ResidualBandwidth);
            }

            path = _Dijkstra.GetShortestPath(request.SourceId, request.DestinationId, _LinkCost);

            //Save info new path
            foreach (var link in path)
            {
                if (request.HoldingTime == long.MaxValue)
                {
                    _LinkReleaseTime[link].Add(request.HoldingTime);
                    _LinkReleaseBandwidth[link].Add(request.Demand);
                }
                else
                {
                    _LinkReleaseTime[link].Add(request.IncomingTime + request.HoldingTime);
                    _LinkReleaseBandwidth[link].Add(request.Demand);
                }
            }


            RestoreTopology();
            return path;
        }
    }
}

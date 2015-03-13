using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.CommonObjects;
using System.Diagnostics;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class NewBLGC : RoutingStrategy
    {
        #region Fields
        private Dijkstra _Dijkstra;

        private AllSimplePaths _ASP;

        // private List<IEPair> _IEList = new List<IEPair>();

        private static Dictionary<Link, double> _CostLink;

        //private Dictionary<Link, double> _CIe;

        private static Dictionary<Link, double> _CReq; // = _Probability * _CIeLink

        private static Dictionary<Link, double> _Load;
        private static Dictionary<Link, double> _CLink;

        private static Dictionary<Link, double> _UsedLinkCount;

        //private static List<Request> _PastData;

        private static Dictionary<IEPair, double> _Probability;
        private static Dictionary<IEPair, Dictionary<Link, double>> _CIeLink; // count link criticality per IE

        private static Dictionary<IEPair, double> _IECount;
        private static double _TotalRequest = 0;

        private static double K1, K2, K3;

        private const double MAX = 1000;

        private static double _TotalPath = 0;    

        #endregion

        public NewBLGC(Topology topology)
            : base(topology)
        {
            Initialize();
        }

        private void Initialize()
        {
            _Dijkstra = new Dijkstra(_Topology);

            _ASP = new AllSimplePaths(_Topology);

            //_IEList = _Topology.IEPairs;

            _CostLink = new Dictionary<Link, double>();

            //_CIe = new Dictionary<Link, double>();

            _CReq = new Dictionary<Link, double>();

            _Load = new Dictionary<Link, double>();

            _CLink = new Dictionary<Link, double>();

            _UsedLinkCount = new Dictionary<Link, double>();

            

            // for caculator Support Ie
            //_PastData = new List<Request>();

            _Probability = new Dictionary<IEPair, double>();
            _CIeLink = new Dictionary<IEPair, Dictionary<Link, double>>();
            _IECount = new Dictionary<IEPair, double>();

            

            Offline();

            K1 = 0.0;
            K2 = 5.0;
            K3 = 5.0;
        }

        public void Offline()
        {            
            foreach (var link in _Topology.Links)
            {
                _CostLink[link] = 1;

                //_CIe[link] = 0;

                // gan o ham GetPath
                //_CReq[link] = 0;
                //_Load[link] = 0;

                _UsedLinkCount[link] = 0;
            }

            Stopwatch _Stopwatch = new Stopwatch();
            _Stopwatch.Restart();

            foreach (var ie in _Topology.IEPairs)
            {
                _Probability[ie] = 0.25;
                _IECount[ie] = 0;

                // haiz: bad for reroute
                _CIeLink[ie] = new Dictionary<Link, double>();                
                foreach (var link in _Topology.Links)
                {
                    _CIeLink[ie][link] = 0;
                }
                List<List<Link>> paths = _ASP.GetPaths(ie.Ingress, ie.Egress);                
                foreach (var path in paths)
                {
                    foreach (var link in path)
                    {
                        _CIeLink[ie][link] += 1;
                    }                                         
                }

                // chia _CIeLink cho tong so paths cua cap ie, de dam bao ti le nhu 2 so con lai
                foreach (Link link in _Topology.Links)
                {
                    _CIeLink[ie][link] = _CIeLink[ie][link] / paths.Count;
                }                
            }
           
            _Stopwatch.Stop();
            Console.WriteLine("Count  _CIeLink[ie][link]: " + _Stopwatch.ElapsedMilliseconds);
            // ~ 25ms for MIRA topo 
            // :( too long comparing to ?
        }

        //public override void Reroute()
        //{
        //    // Do reroute process like offline phase...
        //}

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {                  
            // Caculator cost for link
            foreach (var link in _Topology.Links)
            {
                _Load[link] = link.UsingBandwidth / link.Capacity;

                _CReq[link] = 0;
                foreach (var ie in _Topology.IEPairs)
                {
                    _CReq[link] += _Probability[ie] * _CIeLink[ie][link];
                }

                if (_TotalPath == 0)
                    _CLink[link] = 0;
                else
                    _CLink[link] = _UsedLinkCount[link] / _TotalPath;                             
                
                // use k1 k2 k3
                _CostLink[link] = (K1 * _CReq[link]) + (K2 * _Load[link]) + (K3 * _CLink[link]);
            }

            // Normalized
            //foreach (var link in _Topology.Links)
            //{
            //    _CostLink[link] = NormalizedCReq(_CReq[link]) + NormalizedLoad(_Load[link]) + NormalizedCLnk(_CLink[link]);
            //}
            
            // Use dijsktra to get path
            EliminateAllLinksNotSatisfy(request.Demand);
            var resultPath = _Dijkstra.GetShortestPath(_Topology.Nodes[request.SourceId], _Topology.Nodes[request.DestinationId], _CostLink);
            RestoreTopology();

            // Caculator at Requesting time            
            _TotalRequest += 1;
            IEPair iepair = (from ie in _Topology.IEPairs
                             where ie.Ingress.Key == request.SourceId && ie.Egress.Key == request.DestinationId
                             select ie).First();
            _IECount[iepair] += 1;

            foreach (var ie in _Topology.IEPairs)
            {
                _Probability[ie] = _IECount[ie] / _TotalRequest;
            }

            // Caculator when finish findpath
            if (resultPath.Count > 0)// neu tim dc dg 
            {
                _TotalPath += 1;
                foreach (var l in resultPath)
                {
                    _UsedLinkCount[l] += 1;
                }
            }                        
            
            return resultPath;
        }

        private double NormalizedCReq(double value)
        {
            double max = _CReq.Values.Max();
            double min = _CReq.Values.Min();
            double delta = max - min;

            if (delta != 0)
                return (value - min) * MAX / delta;
            else
                return MAX;
        }

        private double NormalizedCLnk(double value)
        {
            double max = _CLink.Values.Max();
            double min = _CLink.Values.Min();
            double delta = max - min;

            if (delta != 0)
                   return (value - min) * MAX / delta;
            else
                return MAX;
        }

        private double NormalizedLoad(double value)
        {
            double max = _Load.Values.Max();
            double min = _Load.Values.Min();
            double delta = max - min;
            if (delta != 0)
                return (value - min) * MAX / delta;
            else
                return MAX;
        }
    }
}
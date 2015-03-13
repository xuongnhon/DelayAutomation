using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.CommonObjects;
using NetworkSimulator.SimulatorComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class P_LCBR : RoutingStrategy
    {
        private YenAlgorithm _YenAlgorithm;
        private AllSimplePaths _ASP;

        private static int _ReqCount;
        private static double _CostG = 0;

        private double _Alpha;

        public double Alpha
        {
            get { return _Alpha; }
            set { _Alpha = value; }
        }

        private int _K;

        public int K
        {
            get { return _K; }
            set { _K = value; }
        }
        // For Offline Phase
        private static Dictionary<IEPair, List<List<Link>>> _AllKPaths;
        private static Dictionary<IEPair, List<List<Link>>> _AllKPathsCopy;
        private static Dictionary<IEPair, List<List<Link>>> _AllSimplePaths;

        private static Dictionary<Link, double> _CostY;// cost for get K ShortestPath
        private static Dictionary<IEPair, Dictionary<Link, double>> _CIeLink;
        private static Dictionary<Link, double> _UsedLinkCount = new Dictionary<Link, double>();// use for count num of use link each link
        private static Dictionary<IEPair, double> _BIe;
        private static Dictionary<Link, double> _ExpectedLoad;

        // Input For Online Phase
        private static Dictionary<Link, double> _LinkResidualCopy;
        private static Dictionary<Link, double> _CostLink;


        public P_LCBR(Topology topology)
            : base(topology)
        {
            Initialize();
        }

        private void Initialize()
        {
            _ASP = new AllSimplePaths(_Topology);
            _AllKPaths = new Dictionary<IEPair, List<List<Link>>>();
            _AllSimplePaths = new Dictionary<IEPair, List<List<Link>>>();
            _ReqCount = 0;

            // For Offline Phase
            _CostY = new Dictionary<Link, double>();
            _CIeLink = new Dictionary<IEPair, Dictionary<Link, double>>();
            _BIe = new Dictionary<IEPair, double>();
            _ExpectedLoad = new Dictionary<Link, double>();
            _CostLink = new Dictionary<Link, double>();

            foreach (var ie in _Topology.IEPairs)
            {
                _BIe[ie] = 0;
                _CIeLink[ie] = new Dictionary<Link, double>();

                foreach (var link in _Topology.Links)
                {
                    _CIeLink[ie][link] = 0;
                }
            }

            foreach (var link in _Topology.Links)
            {
                _CostY[link] = 1;

                _UsedLinkCount[link] = 0;

                _ExpectedLoad[link] = 0;
            }

            # region Load Request

            List<Request> _RequestList = new List<Request>();

            //Load request 
            Assembly assembly = Assembly.GetExecutingAssembly();
            string path = System.IO.Path.GetDirectoryName(assembly.Location) + "\\" + Configuration.GetInstance().RequestFilePath;//"\\Request\\Mira\\dynamic_MIRA_bw30-35-45-50_400_2000_40_10.txt";


            FileStream file = new FileStream(path, FileMode.Open);
            StreamReader reader = new StreamReader(file);

            _RequestList = new List<Request>();

            while (!reader.EndOfStream)
            {
                string[] value = reader.ReadLine().Split('\t');
                Request package = MakeRequest(value);
                _RequestList.Add(package);
                _ReqCount++;
            }
            reader.Close();
            _RequestList = _RequestList.OrderBy(o => o.IncomingTime).ToList();

            #endregion

            Offline(_RequestList);
        }

        private Request MakeRequest(string[] value)
        {
            int id = int.Parse(value[0]);
            int source = int.Parse(value[1]);
            int destination = int.Parse(value[2]);
            double banwidth = double.Parse(value[3]);
            long incomingTime = long.Parse(value[4]);
            long holdingTime = long.Parse(value[5]);

            double delay = double.Parse(value[6]);

            return new Request(id, source, destination, banwidth, incomingTime, holdingTime, delay);
        }

        /// <summary>
        /// Offline phase
        /// </summary>
        /// <param name="_RequestList"></param>
        private void Offline(List<Request> _RequestList)
        {
            foreach (var req in _RequestList)
            {
                foreach (var ie in _Topology.IEPairs)
                {
                    if (ie.Ingress.Key == req.SourceId && ie.Egress.Key == req.DestinationId)
                    {
                        _BIe[ie] += req.Demand;
                    }
                }
            }

            foreach (var ie in _Topology.IEPairs)
            {
                _YenAlgorithm = new YenAlgorithm(_Topology, _CostY, _K);

                // Get K ShortestPath for Input Online
                _AllKPaths[ie] = _YenAlgorithm.GetKShortestPath(ie.Ingress, ie.Egress);

                // Find all posible path between source-dest
                _AllSimplePaths[ie] = _ASP.GetPaths(ie.Ingress, ie.Egress);

                foreach (var path in _AllSimplePaths[ie])
                {
                    foreach (var link in path)
                    {
                        _UsedLinkCount[link] += 1;

                        _CIeLink[ie][link] += _UsedLinkCount[link] / _AllSimplePaths[ie].Count;
                    }
                }

                // Reset _UsedLinkCount
                foreach (var link in _Topology.Links)
                {
                    _UsedLinkCount[link] = 0;
                }
            }

            foreach (var link in _Topology.Links)
            {
                foreach (var ie in _Topology.IEPairs)
                {
                    //_ExpectedLoad[link] += (_CIeLink[ie][link] * _BIe[ie]);

                    _ExpectedLoad[link] += (_CIeLink[ie][link] / _BIe[ie]);
                }
            }
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            EliminateAllLinksNotSatisfy(request.Demand);

            // Copy All K Paths
            _AllKPathsCopy = new Dictionary<IEPair, List<List<Link>>>();
            foreach (var iepair in _Topology.IEPairs)
                _AllKPathsCopy[iepair] = new List<List<Link>>(_AllKPaths[iepair]);

            // Set Result path
            var resultPath = new List<Link>();

            // Set costMin = ∞;
            double costMin = double.MaxValue;

            // Calculate _CostLink & Copy Link Residual bw
            _LinkResidualCopy = new Dictionary<Link, double>();

            foreach (var link in _Topology.Links)
            {
                if (link.ResidualBandwidth <= 0)
                    _CostLink[link] = 0;

                else
                    _CostLink[link] = _ExpectedLoad[link] / link.ResidualBandwidth;

                _LinkResidualCopy[link] = link.ResidualBandwidth;
            }

            IEPair ie = (from i in _Topology.IEPairs
                         where i.Ingress.Key == request.SourceId && i.Egress.Key == request.DestinationId
                         select i).First();

            //remove r∈S that not satisfies (DN, ρN)
            foreach (var path in _AllKPathsCopy[ie].ToList())
            {
                double totalDelay = 0;
                foreach (var link in path)
                {
                    //Console.WriteLine(link.ResidualBandwidth);
                    if (link.ResidualBandwidth < request.Demand || (link.ResidualBandwidth - request.Demand) < 0)
                    {
                        _AllKPathsCopy[ie].Remove(path);
                    }
                    totalDelay += link.Delay;
                }

                if (totalDelay > request.Delay)
                {
                    _AllKPathsCopy[ie].Remove(path);
                }
            }

            // Recompute resulting residual link capacities R¢l
            // Recompute the link costs cost(l) = φl/R¢l
            // Recompute the network-wide cost metric cost(G) 
            foreach (var path in _AllKPathsCopy[ie])
            {
                foreach (var link in path)
                {
                    _LinkResidualCopy[link] -= request.Demand;

                    if (_LinkResidualCopy[link] <= 0)
                        _CostLink[link] = 0;

                    else
                        _CostLink[link] = _ExpectedLoad[link] / _LinkResidualCopy[link];
                }

                foreach (var link in _Topology.Links)
                {
                    _CostG += Math.Pow((_CostLink[link] - (_ExpectedLoad[link] / link.Capacity)), 2);
                }

                if (_CostG < costMin)
                {
                    costMin = _CostG;
                    resultPath = path;
                }
            }

            //if (costMin > _Alpha)
            //{
            //    resultPath = new List<Link>();
            //}

            RestoreTopology();

            //Console.WriteLine(costMin);

            return resultPath;
        }
    }
}

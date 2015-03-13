using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.MulticastSimulatorComponents;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using System.Threading;

namespace NetworkSimulator.RoutingComponents.MulticastCommonAlgorithms
{
    public class LmmiraCore
    {
        private Topology _Topology;
        //private List<MulticastRequest> _MulticastRequests;
        private int _K;
        private double _T0;
        private double _Tn;

        private Dijkstra _Dijkstra;

        private Dictionary<Link, double> _LinkWeight;
        private Dictionary<Link, double> _FLinkWeight;

        private Thread _Thread;
        Lmmira _Lmmira;

     // private bool flag = true;
        public Dictionary<Link, double> FLinkWeight
        {
            get { return _FLinkWeight; }
            // set { _FLinkWeight = value; }
        }
        private List<Link> _SCP;

        public List<Link> SCP
        {
            get { return _SCP; }
            //   set { _SCP = value; }
        }

        private List<double> _PeriodTime; // thoi gian moi chu trinh
        private List<double> _PeriodWeight; // tong trong so cac critical link moi chu trinh

        public LmmiraCore(Topology topology, int k, double t0, double t1, Lmmira lmmira)
        {
            _Topology = topology;
            _Lmmira = lmmira;

            //   _MulticastRequests = multicastRequests;

            _K = k;
            _Tn = t1;
            _T0 = t0;            

            this.Initialize();
        }

        private void Initialize()
        {
            _LinkWeight = new Dictionary<Link, double>();
            _FLinkWeight = new Dictionary<Link, double>();


            _PeriodTime = new List<double>();
            _PeriodWeight = new List<double>();

            // time theo ms
            _PeriodTime.Add(_T0);
            //_PeriodTime.Add(15 * 1000); // quan trọng, vì thread sẽ chạy xung quanh giá trị này
            _PeriodTime.Add(_Tn); // quan trọng, vì thread sẽ chạy xung quanh giá trị này
            

            //_PeriodWeight.Add(1);
        }

        private double FindMaxFreeBandwidth(Topology topogoly)
        {
            double max = 0;
            foreach (Link link in topogoly.Links)
            {
                if (link.ResidualBandwidth > max)
                    max = link.ResidualBandwidth;
            }
            return max;
        }

        private void InitWeight(Topology topogoly)
        {
            _LinkWeight.Clear();
            double maxFreeBandwidth = FindMaxFreeBandwidth(topogoly);
            double wl;

            foreach (var link in topogoly.Links)
            {
                //wl = link.ResidualBandwidth / maxFreeBandwidth; // nguoc roi
                wl = maxFreeBandwidth / link.ResidualBandwidth;
                _LinkWeight.Add(link, wl);
            }
        }

        private double SetWeight()
        {
            _FLinkWeight.Clear();

            double totalWeight = 0;

            lock(_Lmmira.cost)
            {                
                foreach (Link link in _Topology.Links)
                {
                    _FLinkWeight.Add(link, 0);
                    foreach (Link cl in _SCP)
                    {
                        if (link.Key == cl.Key) // link trong topo và link trong _SCP (topo tmp) là 2 đtg
                        {
                            _FLinkWeight[link] += 1;                            
                        }
                    }

                    totalWeight += _FLinkWeight[link];

                    _Lmmira.cost[link] = _FLinkWeight[link];
                }       
            }

            return totalWeight;
               
        }        

        // tinh toan khoang thoi gian se thuc hien ke tiep
        public double CalculatePeriod()
        {
            int maxIndex = _PeriodWeight.Count() - 1;
            double tmp = 1 + (_PeriodWeight[maxIndex - 1] - _PeriodWeight[maxIndex]) / _PeriodWeight[maxIndex];
            return _PeriodTime[maxIndex] * tmp;
           
        }

        private List<MulticastRequest> GenerateRequest()
        {
            MulticastRequest a = new MulticastRequest(0, 0, new List<int>() { 14, 15 }, 10, 10, 1000);
            MulticastRequest b = new MulticastRequest(1, 1, new List<int>() { 14, 15 }, 1, 10, 1000);
            MulticastRequest c = new MulticastRequest(2, 2, new List<int>() { 14, 15 }, 1, 10, 1000);
            List<MulticastRequest> mr = new List<MulticastRequest>() { a, b, c };
            return mr;
        }

        public void Start()
        {
            //_Thread = new Thread(FindCritical);
            //_Thread.Start();

            // chay lan dau
            List<MulticastRequest> multicastRequests = this.GenerateRequest();
            FindCritical2(multicastRequests);

            _Thread = new System.Threading.Thread(new System.Threading.ThreadStart(this.DoCalculate));
            _Thread.Start();            
        }

        private void DoCalculate()
        {
            while (_Thread.IsAlive)
            {
                int Tn = (int)_PeriodTime.Last();
                Thread.Sleep(Tn);

                List<MulticastRequest> multicastRequests = this.GenerateRequest();
                FindCritical2(multicastRequests);                
            }            
        }

        public void Stop()
        {
            //_Thread = new Thread(FindCritical);
            //_Thread.Start();

            _Thread.Abort();
        }

        public void FindCritical2(List<MulticastRequest> multicastRequests)
        {
            List<Link> path;
            Topology tmp = new Topology(_Topology);
            _SCP = new List<Link>();// nhung critical link trong 1 period 

            InitWeight(tmp);            

            _Dijkstra = new Dijkstra(tmp);
            foreach (var mRequest in multicastRequests)
            {
                HashSet<Link> _CP = new HashSet<Link>();// nhung critical link trong 1 request 
                List<Link> paths = new List<Link>(); // nhung path trong 1 request

                foreach (var des in mRequest.Destinations)
                {
                    int i = 0;
                    while (i < _K)
                    {
                        path = _Dijkstra.GetShortestPath(tmp.Nodes[mRequest.SourceId], tmp.Nodes[des], _LinkWeight, mRequest.Demand);

                        if (path.Count == 0) // khong tim dc dg di 
                            break;

                        double max = 0;
                        Link critical = null;
                        foreach (var link in path)
                        {
                            if (_LinkWeight[link] > max)
                            {
                                max = _LinkWeight[link];
                                critical = link;
                            }
                        }

                        if (critical != null)
                        {
                            _CP.Add(critical);

                            critical.ResidualBandwidth = -critical.ResidualBandwidth; // remove --> ok

                            paths.AddRange(path); // hong co trong paper?
                        }
                        
                        i++;
                    }
                }

                // giảm nw topo tg ứng với request đã làm
                foreach (var link in _CP)
                    link.ResidualBandwidth = -link.ResidualBandwidth;

                List<Link> distinct = paths.Distinct().ToList(); // lấy trong các paths đã tìm ra 1 link duy nhất, vì chỉ trừ 1 lần

                foreach (var link in distinct)
                    link.UsingBandwidth += mRequest.Demand;

                _SCP.AddRange(_CP);// _SCP chứa link trùng nhau
            }

            if (_SCP.Count > 0)
            {


                double totalWeight = SetWeight(); // paper: C_n-1
                //foreach (var link in _SCP.Distinct().ToList()) // cẩn thận
                //{
                //    totalWeight += _FLinkWeight[link];
                //    //totalWeight += _LinkWeight[link];
                //}

                _PeriodWeight.Add(totalWeight); // dùng biến là đc rồi, dùng mảng cũng chuối

                if (_PeriodWeight.Count() > 1)
                {
                    double Tn = this.CalculatePeriod();
                    if (Tn == 0)
                        Tn = _PeriodTime.Last();

                    _PeriodTime.Add(_Tn);
                }                                
            }
            else
            {
                Console.WriteLine("Tính hổng đc _FLinkWeight!!!");
            }

        }
    }
}

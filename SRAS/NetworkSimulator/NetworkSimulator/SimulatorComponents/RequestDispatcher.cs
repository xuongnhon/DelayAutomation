using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using System.IO;
using NetworkSimulator.StatisticsComponents;
using System.Diagnostics;
using NetworkSimulator.RoutingComponents.RoutingStrategies;

namespace NetworkSimulator.SimulatorComponents
{
    public class RequestDispatcher : ITickerListener
    {
        protected List<Request> _RequestList;

        protected List<Router> _Routers;

        protected Queue<Request> _RequestQueue;

        protected Object _TopologyLockingObject;

        protected Stopwatch _Stopwatch;

        protected ResponseManager _ResponseManager;

        public ResponseManager ResponseManager
        {
            get { return _ResponseManager; }
            set { _ResponseManager = value; }
        }

        protected RoutingStrategy _RoutingStrategy;

        public RoutingStrategy RoutingStrategy
        {
            get { return _RoutingStrategy; }
            set { _RoutingStrategy = value; }
        }

        protected int _ReqCount;

        public int ReqCount
        {
            get { return _ReqCount; }
            set { _ReqCount = value; }
        }

        public List<Router> Routers
        {
            set { _Routers = value; }
        }

        protected void Initialize()
        {
            _ReqCount = 0;
            _Routers = new List<Router>();

            _Stopwatch = new Stopwatch();
        }

        // 02/07/13 caoth
        public RequestDispatcher()
        {
            Initialize();
        }

        public RequestDispatcher(string requestFilePath, Object topologyLockingObject)
            : this()
        {
            //Initialize();
            LoadRequestData(requestFilePath);
            _TopologyLockingObject = topologyLockingObject;
        }

        //public RequestDispatcher(string requestFilePath, List<Router> routers)
        //    : this(requestFilePath)
        //{
        //    //Initialize();
        //    //LoadRequestData(requestFilePath);
        //    _Routers = routers;
        //}

        private void LoadRequestData(string requestFilePath)
        {
            FileStream file = new FileStream(requestFilePath, FileMode.Open);
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

            // 16/7/2013 ngoctoan
            //Statistics._NumOfRequest = _RequestList.Count;

            if (_RequestList.First<Request>().HoldingTime == int.MaxValue && _RequestList.Last<Request>().HoldingTime == int.MaxValue)
            {
                Statistics._RequestTypeName = "static";
            }
            if (_RequestList.First<Request>().HoldingTime != int.MaxValue && _RequestList.Last<Request>().HoldingTime != int.MaxValue)
            {
                Statistics._RequestTypeName = "dynamic";
            }
            if (_RequestList.First<Request>().HoldingTime == int.MaxValue && _RequestList.Last<Request>().HoldingTime != int.MaxValue)
            {
                Statistics._RequestTypeName = "mix";
            }
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

        public virtual void OnTickerTick(long elapsedTime)
        {
            if (_RequestList.Count > 0)// && _Routers.Count > 0)
            {
                Request request = _RequestList[0];
                while (request.IncomingTime <= elapsedTime)
                {
                    //_Routers[request.SourceId].RecieveRequest(request);
                    lock (_TopologyLockingObject)
                    {
                        //Request request = _RequestQueue.Dequeue();
                        _Stopwatch.Restart();

                        // 12/7 ngoctoan
                        List<Link> path;// = new List<Link>();

                        path = _RoutingStrategy.GetPath(request);

                        _Stopwatch.Stop();
                        //Response response = new Response(request, path, _Stopwatch.Elapsed.TotalMilliseconds);
                        //Nhon
                        Response response = new Response(request, path, _Stopwatch.Elapsed.TotalMilliseconds, _RoutingStrategy.weightPath);
                        _ResponseManager.ReceiveResponse(response);

                        ((Topology)_TopologyLockingObject).CalculatePercentOfBandwidthUsedPerLink(response);
                    }

                    _RequestList.RemoveAt(0);
                    if (_RequestList.Count == 0) break;
                    request = _RequestList[0];
                }
            }
        }

    }
}

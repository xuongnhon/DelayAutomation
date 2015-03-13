using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.SimulatorComponents;

namespace NetworkSimulator.MulticastSimulatorComponents
{
    public class MulticastResponseManager : ResponseManager
    {
        // caoth
        public MulticastResponseManager(RequestDispatcher requestDispatcher, Ticker ticker, Topology topology)
            : base(requestDispatcher, ticker, topology)
        {
        }

        protected override void ReserveBandwidth(Response response)
        {
            MulticastResponse multicastReponse = (MulticastResponse)response;
            lock (_TopologyLockingObject)
            {
                List<Link> links = new List<Link>();

                foreach (List<Link> path in multicastReponse.Tree.Paths)
                {
                    links.AddRange(path);
                }

                List<Link> distinct = links.Distinct().ToList();
                foreach (Link link in distinct)
                {
                    link.UsingBandwidth += response.Request.Demand;
                }
            }                       
        }

        protected override void ReleaseBandwidth(Response response)
        {
            MulticastResponse multicastReponse = (MulticastResponse)response;
            lock (_TopologyLockingObject)
            {
                foreach (List<Link> path in multicastReponse.Tree.Paths)
                {
                    foreach (Link link in path)
                    {
                        link.UsingBandwidth -= response.Request.Demand;
                    }
                }
            }
                                  
            Console.WriteLine("REL-" + response);
        }

        public override void OnTickerTick(long elapsedTime)
        {
            _ElapsedTime = elapsedTime;
            DoRelease(elapsedTime);

            if (_ResponsesForStatistics.Count == _RequestDispatcher.ReqCount) // lam xong het cac request
            {
                // code for stoping timer and statistics here
                _Ticker.Stop();
                
                // caoth: ToDo evaluation for multicast routing
                int accepted = _ResponsesForStatistics.Count;
                
                Console.WriteLine("/////////////////////////////////");
                Console.WriteLine("// Accepted: " + accepted + " Requests !");
                Console.WriteLine("/////////////////////////////////");

                //for log
                // Log.WriteLine(_ResponsesForStatistics);
            }

           
        }
        
    }
}

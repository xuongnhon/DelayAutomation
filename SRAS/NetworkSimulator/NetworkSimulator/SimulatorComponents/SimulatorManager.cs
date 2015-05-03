using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.MulticastSimulatorComponents;
using NetworkSimulator.RoutingComponents.RoutingStrategies;
using System.Threading;
using NetworkSimulator.RoutingComponents.MulticastRoutingStrategies;
using NetworkSimulator.RoutingComponents.MulticastCommonAlgorithms;

namespace NetworkSimulator.SimulatorComponents
{
    public class SimulatorManager
    {
        private static SimulatorManager instance = null;

        public static SimulatorManager getInstance()
        {
            if (instance == null)
                instance = new SimulatorManager();
            return instance;
        }

        private Configuration _Config;

        private List<Router> _Routers;

        private Ticker _Ticker;

        private RequestDispatcher _RequestDispatcher;

        private ResponseManager _ResponseManager;

        private Topology _Topology;

        private LmmiraCore _LCore;

        public SimulatorManager()
        {
            // caoth
            //Initialize();
        }

        public void StopSimulate()
        {
            if (_Routers != null)
                foreach (var r in _Routers)
                {
                    r.Stop();
                }
        }

        private void Initialize()
        {
            // Load configuration from /bin/config.xml
            _Config = Configuration.GetInstance();

            _Topology = new Topology(_Config.TopologyFilePath, _Config.IEListFilePath);

            _Ticker = new Ticker(_Config.TimerInterval * 1000); // Ticker counts by micro seconds

            _RequestDispatcher = new RequestDispatcher(_Config.RequestFilePath, _Topology);
            _ResponseManager = new ResponseManager(_RequestDispatcher, _Ticker, _Topology);

            // Turn on all routers
            _Routers = new List<Router>();
            for (int i = 0; i < _Topology.Nodes.Count; i++)
                _Routers.Add(new Router(i, _Topology, _ResponseManager));
            //_Routers.Add(new Router(i, _Topology.LockingObject, _ResponseManager));

            _RequestDispatcher.Routers = _Routers;
            _RequestDispatcher.ResponseManager = _ResponseManager;

            _Ticker.AddListener(_ResponseManager);
            _Ticker.AddListener(_RequestDispatcher);

        }

        public void Start()
        {
            // 02/07 caoth
            Initialize();
            
            var ca = _Config.UnicastRoutingAlgorithms.SingleOrDefault(a => a.Selected == true);
            if (ca == null)
                throw new Exception("Routing strategy not set in the configuration correctly, please check your configuration file.");

            // Set routing strategy for all routers
            //for (int i = 0; i < _Topology.Nodes.Count; i++)
            {
                RoutingStrategy rs = null;
                switch (ca.Name)
                {
                    //case "P-LCBR":
                    //    rs = new P_LCBR(_Topology) 
                    //    { 
                    //        K = ca.GetParam<int>("K"), 
                    //        Alpha = ca.GetParam<double>("Alpha") 
                    //    };
                    //    break;

                    case "LDPRA":
                        rs = new LDPRA(_Topology);
                        break;
                       
                    //case "DC-MHA":
                    //    rs = new DC_MHA(_Topology);
                    //    break;

                    //case "DC-MIRA":
                    //    rs = new DC_MIRA(_Topology) { Alpha = ca.GetParam<int>("Alpha") };
                    //    break;

                    case "MDWCRA":
                        rs = new MDWCRA(_Topology);
                        break;

                    case "M-MDWCRA":
                        rs = new M_MDWCRA(_Topology);
                        break;

                    case "HRABDC":
                        rs = new HRABDC(_Topology);
                        break;

                    case "eHRABDC":
                        rs = new eHRABDC(_Topology);
                        break;

                    //case "ACSBDRA":
                    //    rs = new ACSBDRA(_Topology);
                    //    break;

                    //case "HBDRA":
                    //    rs = new HBDRA(_Topology);
                    //    break;

                    //case "BDCRA":
                    //    rs = new BDCRA(_Topology);
                    //    break;
                    //case "BDCRA-Heu":
                    //    rs = new BDCRA_Heu(_Topology);
                    //    break;

                    case "DCW_heDij":
                        rs = new DWC_heDij(_Topology);
                        break;

                    case "BBW_exDij":
                        rs = new BBW_exDij(_Topology);
                        break;

                    case "MDMF":
                        rs = new MDMF(_Topology) { Mi = ca.GetParam<double>("Mi"), Upsilon = ca.GetParam<double>("Upsilon") };
                        break;

                    case "MDMF_exDij":
                        rs = new MDMF_exDij(_Topology) { Mi = ca.GetParam<double>("Mi"), Upsilon = ca.GetParam<double>("Upsilon") };
                        break;

                    case "MDMF_Heu":
                        rs = new MDMF_Heu(_Topology) { Mi = ca.GetParam<double>("Mi"), Upsilon = ca.GetParam<double>("Upsilon") };
                        break;

                    /////////////////////////////////////////////////////

                    //case "PBWA":
                    //    rs = new PBWA(_Topology);
                    //    break;

                    //case "NewPBWA":
                    //    rs = new NewPBWA(_Topology);
                    //    break;

                    //case "NewBLGC":
                    //    rs = new NewBLGC(_Topology);
                    //    break;

                    //case "MHA":
                    //    rs = new MHA(_Topology);
                    //    break;
                    //case "WSP":
                    //    rs = new WSP(_Topology);
                    //    break;
                    //case "BCRA":
                    //    rs = new BCRA(_Topology);
                    //    break;
                    //case "MIRA":
                    //    rs = new MIRA(_Topology) { Alpha = ca.GetParam<int>("Alpha") };
                    //    break;
                    //case "DORA":
                    //    rs = new DORA(_Topology) { BWP = ca.GetParam<double>("BWP") };
                    //    break;
                    //case "RRATE":
                    //    rs = new RRATE(_Topology)
                    //    {
                    //        K = ca.GetParam<int>("K"),
                    //        N = ca.GetParam<int>("N"),
                    //        K1 = ca.GetParam<double>("K1"),
                    //        K2 = ca.GetParam<double>("K2")
                    //    };
                    //    break;
                    //case "POOA":
                    //    rs = new POOA(_Topology) { K = ca.GetParam<int>("K") };
                    //    break;
                    default:
                        throw new Exception("Routing strategy not found, please check your configuration");
                }
                //_Routers[i].RoutingStrategy = rs;
                _RequestDispatcher.RoutingStrategy = rs;
            }

            //Thread.Sleep(100);
            // vu an 1st request :(            

            _Ticker.Start();
        }

        // 02/07 caoth
        private void InitializeMulticast()
        {
            // Load configuration from /bin/config.xml
            _Config = Configuration.GetInstance();

            _Topology = new Topology(_Config.TopologyFilePath);

            _Ticker = new Ticker(_Config.TimerInterval * 1000);

            _RequestDispatcher = new MulticastRequestDispatcher(_Config.RequestFilePath, _Topology);
            _ResponseManager = new MulticastResponseManager(_RequestDispatcher, _Ticker, _Topology);

            // Turn on all routers
            _Routers = new List<Router>();
            for (int i = 0; i < _Topology.Nodes.Count; i++)
                _Routers.Add(new MulticastRouter(i, _Topology, _ResponseManager));
            //_Routers.Add(new MulticastRouter(i, _Topology.LockingObject, _ResponseManager));

            _RequestDispatcher.Routers = _Routers;
            _RequestDispatcher.ResponseManager = _ResponseManager;

            _Ticker.AddListener(_RequestDispatcher);
            _Ticker.AddListener(_ResponseManager);
        }

        public void StartMulticast()
        {
            InitializeMulticast();

            //_LCore = new LmmiraCore(_Topology, 3, 0, 1);
            //_LCore.Start();

            var ca = _Config.MulticastRoutingAlgorithms.SingleOrDefault(a => a.Selected == true);
            if (ca == null)
                throw new Exception("Routing strategy not set in the configuration correctly, please check your configuration file.");

            // Set routing strategy for all routers
            //for (int i = 0; i < _Topology.Nodes.Count; i++)
            {
                RoutingStrategy rs = null;
                switch (ca.Name)
                {
                    case "SPT":
                        rs = new SPT(_Topology);
                        break;
                    case "LMMIRA":
                        rs = new Lmmira(_Topology);
                        break;

                    default:
                        throw new Exception("Routing strategy not found, please check your configuration");
                }
                //_Routers[i].RoutingStrategy = rs;
                _RequestDispatcher.RoutingStrategy = rs;
            }

            //Thread.Sleep(100);

            _Ticker.Start();
        }

    }
}

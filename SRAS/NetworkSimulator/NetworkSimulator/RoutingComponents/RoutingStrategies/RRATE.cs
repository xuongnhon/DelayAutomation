﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.CommonObjects;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    class RRATE : RoutingStrategy
    {
        #region Fields
        private int _K;

        private List<IEPair> _IEList;

        private Dictionary<IEPair, List<List<Link>>> _AlKPaths;

        private Dictionary<List<Link>, int> _XValues;

        private Dictionary<Link, double> _LinkCost;
        
        private Dictionary<List<Link>, double> _PathCost;

        private YenAlgorithm _YenAlgorithm;

        private FordFulkerson _FordFulkerson;

        private static Dictionary<IEPair,bool> _IsInLearningStage;
        #endregion

        #region Properties
        public int K
        {
            get { return _K; }
            set { _K = value; }
        }

        private int _N;

        public int N
        {
            get { return _N; }
            set { _N = value; }
        }

        private double _K1, _K2;

        public double K2
        {
            get { return _K2; }
            set { _K2 = value; }
        }

        public double K1
        {
            get { return _K1; }
            set { _K1 = value; }
        }
        #endregion

        public RRATE(Topology topology)
            : base(topology)
        {
            Initialize();
        }

        public void Initialize()
        {
            _K1 = _K2 = 0.5;
            _N = 10;
            _K = 25;
            _LinkCost = new Dictionary<Link, double>();
            _IEList = new List<IEPair>();
            _AlKPaths = new Dictionary<IEPair, List<List<Link>>>();
            _XValues = new Dictionary<List<Link>, int>();
            _FordFulkerson = new FordFulkerson(_Topology);
            _IsInLearningStage = new Dictionary<IEPair,bool>();
            _PathCost = new Dictionary<List<Link>, double>();

            foreach (Link link in _Topology.Links)
            {
                _LinkCost[link] = 1;
            }

            // Ingress Egress list
            _IEList = _Topology.IEPairs;

            foreach (var iepair in _IEList)
            {
                _IsInLearningStage[iepair] = true;
            }

            //OfflinePhase
            OfflinePhase();
        }

        /// <summary>
        /// Find k shortest paths and initialize x value foreach IE pair
        /// </summary>
        public void OfflinePhase()
        {
            
            foreach (var iepair in _IEList)
            {
                _YenAlgorithm = new YenAlgorithm(_Topology, _LinkCost, _K);
                _AlKPaths[iepair] = _YenAlgorithm.GetKShortestPath(iepair.Ingress,iepair.Egress);
                foreach (var path in _AlKPaths[iepair])
                {
                    _XValues[path] = 0;
                    _PathCost[path] = double.MaxValue;
                }
            }
            

        }

        private double GetBandwidthOfPath(List<Link> Path)
        {
            double Bandwidth = double.MaxValue;
            foreach (var link in Path)
                if (link.ResidualBandwidth < Bandwidth)
                {
                    Bandwidth = _Topology.GetLink(link.Source,link.Destination).ResidualBandwidth;
                }
            return Bandwidth;
        }

        public List<Link> LearningStage(IEPair iepair, double Bandwidth)
        {
            List<Link> minCutSet = new List<Link>();
            minCutSet = _FordFulkerson.FindMinCutSet(_Topology.Nodes[iepair.Ingress.Key], _Topology.Nodes[iepair.Egress.Key]);
            
            #region Compute cost foreach path
            foreach (var path in _AlKPaths[iepair])
            {
                List<Link> commonLink = new List<Link>();
                double maxResidualBw = 0;
                foreach (var link in path)
                {
                    if (minCutSet.Contains(link))
                    {
                        commonLink.Add(link);
                    }
                    if (maxResidualBw < link.ResidualBandwidth)
                    {
                        maxResidualBw = link.ResidualBandwidth;
                    }
                }

                if (maxResidualBw > 0)
                {
                    _PathCost[path] = _K1 * commonLink.Count + _K2 / maxResidualBw;
                }
            }
            #endregion

            
            //Find the path having min cost satify demand
            double minCost = double.MaxValue;
            List<Link> resultPath = new List<Link>();
            
            foreach (var path in _AlKPaths[iepair])
                if (_PathCost[path] < minCost && GetBandwidthOfPath(path) >= Bandwidth)
                {
                    minCost = _PathCost[path];
                    resultPath = path;
                }

            if (resultPath.Count > 0)
            {
                _XValues[resultPath]++;    
            }
            
            return resultPath;
        }

        public List<Link> PostLearningStage(IEPair iepair, double Bandwidth)
        {
            int maxX = -1;
            List<Link> resultPath = new List<Link>();
            foreach (var path in _AlKPaths[iepair])
            {
                if (_XValues[path] > maxX && GetBandwidthOfPath(path) >= Bandwidth)
                {
                    maxX = _XValues[path];
                    resultPath = path;
                }
            }

            return resultPath;
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            
            List<Link> resultPath = new List<Link>();
            IEPair iepair = (from ie in _IEList
                                where ie.Ingress.Key == request.SourceId && ie.Egress.Key == request.DestinationId
                                select ie).First();
           
            if (_IsInLearningStage[iepair])
            {
                //If have one path X value reach N threshold
                foreach (var path in _AlKPaths[iepair])
                    if (_XValues[path] == _N)
                    {
                        _IsInLearningStage[iepair] = false;
                    }
                resultPath = LearningStage(iepair, request.Demand);
            }
            else
            {
                resultPath = PostLearningStage(iepair,request.Demand);
            }
            
            return resultPath;
        }        

    }
}

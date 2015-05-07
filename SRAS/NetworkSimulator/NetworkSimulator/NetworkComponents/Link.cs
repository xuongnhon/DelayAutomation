using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NetworkSimulator.SimulatorComponents;

namespace NetworkSimulator.NetworkComponents
{
    public class Link
    {
        #region Fields
        private string _Key;

        private double _Capacity;

        private double _UsingBandwidth;

        private Node _Source;

        private Node _Destination;

        private double _Delay;

        private Dictionary<NetworkSimulator.SimulatorComponents.Response, double> _PercentOfBandwidthUsed;

        //17.11.2014 ThaoHT: Dynamic Delay omit
        //private double _DDelay;

        #endregion

        #region Properties
        public string Key
        {
            get { return _Key; }
        }

        public double Capacity
        {
            get { return _Capacity; }
            set { _Capacity = value; }
        }

        public double ResidualBandwidth
        {
            get { return _Capacity - _UsingBandwidth; }
            set { _UsingBandwidth = _Capacity - value; }
        }

        public double UsingBandwidth
        {
            get { return _UsingBandwidth; }
            set { _UsingBandwidth = value; }
        }

        public Node Source
        {
            get { return _Source; }
            set { _Source = value; }
        }

        public Node Destination
        {
            get { return _Destination; }
            set { _Destination = value; }
        }

        //17.11.2014 ThaoHT: ???
        public double Delay
        {
            get { return _Delay; }
            set { _Delay = value; }
        }

        /*
        public double DDelay
        {
            get { return _Delay + 1; }
            set { _DDelay = value; }
        }*/

        public Dictionary<NetworkSimulator.SimulatorComponents.Response, double> PercentOfBandwidthUsed
        {
            get { return _PercentOfBandwidthUsed; }
        }
        #endregion

        public Link(Node source, Node destination, double capacity, double usingBandwidth, double delay)
        {
            _Key = source.Key + "|" + destination.Key;
            this._Source = source;
            this._Destination = destination;
            this._Capacity = capacity;
            this._Delay = delay;
            this._UsingBandwidth = usingBandwidth;
            _PercentOfBandwidthUsed = new Dictionary<Response, double>();
        }

        public override string ToString()
        {
            string[] v = _Key.Split('|');
            return "LNK-(" + v[0] + "-" + v[1] + ") CAP=" + _Capacity + " RSD=" + this.ResidualBandwidth + " DELAY=" + this.Delay;
        }

        public void AddPercentOfBandwidthUsed(NetworkSimulator.SimulatorComponents.Response _Response, double _Value)
        {
            this._PercentOfBandwidthUsed.Add(_Response, _Value);
        }
    }
}

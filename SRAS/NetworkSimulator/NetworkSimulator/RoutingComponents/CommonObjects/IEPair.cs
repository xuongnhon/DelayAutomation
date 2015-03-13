using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;

namespace NetworkSimulator.RoutingComponents.CommonObjects
{
    public class IEPair
    {
        #region Fields
        private Node _Ingress;

        private Node _Egress;
        #endregion

        #region Properties
        public Node Ingress
        {
            get { return _Ingress; }
            set { _Ingress = value; }
        }

        public Node Egress
        {
            get { return _Egress; }
            set { _Egress = value; }
        }
        #endregion

        public IEPair(Node ingress, Node egress)
        {
            _Ingress = ingress;
            _Egress = egress;
        }

        public override string ToString()
        {
            return "Ingress: " + _Ingress.Key + " Egress: " + _Egress.Key;
        }
    }
}

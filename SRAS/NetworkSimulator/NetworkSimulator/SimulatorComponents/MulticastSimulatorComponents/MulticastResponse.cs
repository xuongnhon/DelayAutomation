using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.SimulatorComponents;

namespace NetworkSimulator.MulticastSimulatorComponents
{
    public class MulticastResponse : Response
    {
        #region Fields        

        private Tree _tree;
                
        #endregion

        #region Properties
        
        public Tree Tree
        {
            get { return _tree; }
            set { _tree = value; }
        }
                
        #endregion

        public MulticastResponse(MulticastRequest multicastrequest, Tree tree, double computingTime)
        {
            _Request = multicastrequest;
            _tree = tree;
            _ComputingTime = computingTime;
        }

        public override bool HasPath()
        {
            return Tree.Paths.Count > 0;
        }

        public override string ToString()
        {
            string str = (MulticastRequest)Request + " RPT=" + _ResponseTime + " RLT=" + ReleasingTime + " COT=" + _ComputingTime + "ms" + "\n";
            
            str += "==================== PATH FROM SOURCE TO DESTINATION ===================\n";
            // caoth: todo

            if (_tree.Paths[0].Count() > 0)
            {
                foreach (List<Link> path in _tree.Paths)
                {
                    foreach (Link link in path)
                    {
                        str += "\t" + link + "\n";
                    }
                    str += "\n\n";
                }
            }
            else
            {
                str += "\n\t\t\t NETWORK OVERLOAD \n";
            }
            str += "\n\n";
            str += "=============================================\n";

            return str;
        }
    }
}

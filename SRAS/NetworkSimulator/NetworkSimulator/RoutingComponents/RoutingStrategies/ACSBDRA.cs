using NetworkSimulator.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.RoutingStrategies
{
    public class ACSBDRA : RoutingStrategy
    {
        private static readonly double MaxValue = 10000;
        private HashSet<string> _EliminateLinks;
        private double[] _LeastW2ToDestination;
        private Random _Rand;

        private int _Popsize; // number of trails to construct of once
        private double _Q; // probability of selecting components in an elitist way
        private Dictionary<string, double> _P; // pheromones of the components
        private double _Lamda; // initial value for pheromones
        private int _MaxTime;
        private double _Alpha; // elitist learning rate
        private double _Beta; // evaporation rate
        private double _Delta; // tuning parameter for heuristic in component selection
        private double _Epsilon; // tuning parameter for pheromones in component selection

        public ACSBDRA(Topology topology)
            : base(topology)
        {
            _Topology = topology;
            Initialize();
        }

        private void Initialize()
        {
            _Rand = new Random();
            _P = new Dictionary<string, double>();
            _EliminateLinks = new HashSet<string>();

            _Popsize = 10;
            _Q = 0.5;
            _Lamda = 1;
            _MaxTime = 200;
            _Alpha = 0.05;
            _Beta = 0.02;
            _Delta = 1;
            _Epsilon = 2;
        }

        private double[] ComputeLeastWeight(Topology topology, HashSet<string> E, int d, Dictionary<string, double> w)
        {
            // Copy topology and reverse all 
            Topology copyTopology = new Topology(topology);
            ReverseLinkDirection(copyTopology);
            Dictionary<string, double> wc = new Dictionary<string, double>();
            foreach (var item in w)
            {
                string[] str = item.Key.Split('|');
                string rKey = str[1] + '|' + str[0];
                wc[rKey] = item.Value;
            }
            HashSet<string> Ec = new HashSet<string>();
            foreach (var item in E)
            {
                string[] str = item.Split('|');
                string rKey = str[1] + '|' + str[0];
                Ec.Add(rKey);
            }

            // Initialize
            int nv = topology.Nodes.Count;
            double[] dist = new double[nv];

            for (int v = 0; v < nv; v++)
                dist[v] = MaxValue;
            dist[d] = 0;

            var Q = new List<Node>(copyTopology.Nodes);

            while (Q.Count > 0)
            {
                // Find node u within T set, where d[u] = min{d[z]:z within T
                var u = Q.First();
                foreach (var node in Q)
                    if (dist[node.Key] < dist[u.Key])
                        u = node;

                Q.Remove(u);

                // Browse all adjacent node to update distance from s.
                var links = u.Links.Where(l => !Ec.Contains(l.Key)).ToList();
                foreach (var link in links)
                {
                    var v = link.Destination;
                    if (dist[v.Key] > dist[u.Key] + wc[link.Key])
                        dist[v.Key] = dist[u.Key] + wc[link.Key];
                }
            }

            return dist;
        }

        // Reserver direction of all links in topology
        private void ReverseLinkDirection(Topology topology)
        {
            List<Link> links = topology.Links;
            foreach (var link in links)
            {
                link.Source.Links.Remove(link);
                Link rLink = new Link(link.Destination, link.Source, link.Capacity, link.UsingBandwidth, link.Delay);
                rLink.Source.Links.Add(rLink);
            }

            // Update link set
            topology.Links = null;
        }

        private void InitPheromones()
        {
            foreach (var l in _Topology.Links)
                _P[l.Key] = _Lamda;
        }

        private Link SelectNextLink(List<Link> links, Dictionary<string, double> w1)
        {
            double q = _Rand.NextDouble();
            Dictionary<string, double> desirability = new Dictionary<string, double>();
            foreach (var link in links)
                desirability[link.Key] = Math.Pow(_P[link.Key], _Delta) + Math.Pow(1 / w1[link.Key], _Epsilon); //_Delta * _P[link.Key] + (1 - _Delta) * (1 / w1[link.Key]);

            if (q <= _Q)
            {
                Link selectedLink = links.First();
                foreach (var link in links)
                    if (desirability[link.Key] > desirability[selectedLink.Key])
                        selectedLink = link;
                return selectedLink;
            }

            Dictionary<string, double> probs = new Dictionary<string, double>();
            double sumValue = desirability.Sum(v => v.Value);
            foreach (var link in links)
                probs[link.Key] = desirability[link.Key] / sumValue;

            double[] cumul = new double[probs.Count + 1];
            for (int i = 0; i < probs.Count; i++)
                cumul[i + 1] = cumul[i] + probs.ElementAt(i).Value;

            var p = _Rand.NextDouble();

            for (int i = 0; i < cumul.Length - 1; i++)
                if (p >= cumul[i] && p < cumul[i + 1])
                    return links[i];

            throw new Exception("Failure to return the next link");
        }

        private List<Link> FindPath(int s, int d, Dictionary<string, double> w1, Dictionary<string, double> w2, double c2)
        {
            int nv = _Topology.Nodes.Count;
            HashSet<int> visited = new HashSet<int>();
            int[] prev = new int[nv];
            int u = s;
            double w2SoFar = 0;
            visited.Add(u);
            for (int v = 0; v < nv; v++)
                prev[v] = -1;
	        
            while (u != d)
            {
                var links = _Topology.Nodes[u].Links.Where(l => 
                    !_EliminateLinks.Contains(l.Key)
                    && !visited.Contains(l.Destination.Key)
                    && (w2SoFar + w2[l.Key] + _LeastW2ToDestination[l.Destination.Key]) <= c2).ToList();

                if (links.Count == 0)
                    break;

                var nextLink = SelectNextLink(links, w1);
                int v = nextLink.Destination.Key;
                visited.Add(v);
                prev[v] = u;
                w2SoFar += w2[nextLink.Key];
                u = v;
            }
            var path = ConstructPath(_Topology, prev, d);
            return path;
        }

        private List<Link> ConstructPath(Topology t, int[] prev, int d)
        {
            int v = d;
            List<Link> path = new List<Link>();

            while (prev[v] != -1)
            {
                int u = prev[v];
                var link = _Topology.GetLink(u, v);
                path.Add(link);
                v = u;
            }
            path.Reverse();

            return path;
        }

        private double Fitness(List<Link> path)
        {
            if (path.Count == 0)
                return 0;
            return path.Min(l => l.ResidualBandwidth) / path.Count;
        }

        private List<Link> FindOptimalPath(int s, int d, Dictionary<string, double> w1, Dictionary<string, double> w2, double c2)
        {
            var bestPath = new List<Link>();
            for (int t = 0; t < _MaxTime; t++)
            {
                for (int i = 0; i < _Popsize; i++)
                {
                    var path = FindPath(s, d, w1, w2, c2);
                    if (bestPath.Count == 0 || Fitness(path) > Fitness(bestPath))
                        bestPath = path;
                }
                foreach (var link in _Topology.Links)
                    _P[link.Key] = (1 - _Beta) * _P[link.Key] + _Beta * _Lamda;

                foreach (var link in bestPath)
                    _P[link.Key] = (1 - _Alpha) * _P[link.Key] + _Alpha * Fitness(bestPath);
            }
            return bestPath;
        }

        public override List<Link> GetPath(SimulatorComponents.Request request)
        {
            Dictionary<string, double> w1 = new Dictionary<string, double>();
            Dictionary<string, double> w2 = new Dictionary<string, double>();

            _EliminateLinks.Clear();
            foreach (var link in _Topology.Links)
            {
                w1[link.Key] = 1 / link.ResidualBandwidth;
                w2[link.Key] = link.Delay;
                if (link.ResidualBandwidth < request.Demand)
                    _EliminateLinks.Add(link.Key);
            }

            _LeastW2ToDestination = ComputeLeastWeight(_Topology, _EliminateLinks, request.DestinationId, w2);

            InitPheromones();

            var path = FindOptimalPath(request.SourceId, request.DestinationId, w1, w2, request.Delay);

            if (path.Sum(l => l.Delay) > request.Delay)
                throw new Exception("Not feasible path");

            if (_Topology.Links.Min(l => l.ResidualBandwidth) < 0)
                throw new Exception("Residual bandwidth less than 0");

            return path;
        }
    }
}

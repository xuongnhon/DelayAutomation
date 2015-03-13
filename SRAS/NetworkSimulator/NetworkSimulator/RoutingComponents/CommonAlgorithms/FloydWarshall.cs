using NetworkSimulator.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    public class FloydWarshall
    {
        public double[,] GetMinimumDistances(Topology topology, Dictionary<string, double> cost)
        {
            // Number of vertices in topology (|V|)
            int n = topology.Nodes.Count;
            double[,] dist = new double[n, n];

            // let dist be a |V| × |V| array of minimum distances initialized to ∞ (infinity)
            for (int u = 0; u < n; u++)
                for (int v = 0; v < n; v++)
                    dist[u, v] = double.MaxValue;

            foreach (var node in topology.Nodes)
                dist[node.Key, node.Key] = 0;

            foreach (var link in topology.Links)
                dist[link.Source.Key, link.Destination.Key] = cost[link.Key];

            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (dist[i, k] + dist[k, j] < dist[i, j])
                            dist[i, j] = dist[i, k] + dist[k, j];


            //for (int i = 0; i < n; i++)
            //{
            //    for (int j = 0; j < n; j++)
            //        Console.Write(dist[i, j] + " ");
            //    Console.WriteLine();
            //}

            return dist;
        }
    }
}

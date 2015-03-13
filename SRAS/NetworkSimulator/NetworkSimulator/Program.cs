﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkSimulator.SimulatorComponents;
using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonAlgorithms;
using NetworkSimulator.RoutingComponents.RoutingStrategies;
using NetworkSimulator;
using System.ComponentModel;
using System.IO;

namespace NetworkSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
           // Topology t = new Topology("Map/NET-50Node.dat", "Map/IE_NET1.txt");
            //Topology t = new Topology("Map/cesnet_map.dat", "Map/cesnet_ie_list.dat");
           // Dictionary<Link, double> cost = new Dictionary<Link, double>();
           // List<double> DelayOfPath = new List<double>();

           // foreach (var link in t.Links)
           // {
           //     cost[link] = 1;
           // }

           // FileInfo myfile = new FileInfo("Net1DelayInfo.txt");
           // StreamWriter tex = myfile.CreateText();

            //YenAlgorithm yen = new YenAlgorithm(t, cost, 10);

            //int count = 0;

            //foreach (var ie in t.IEPairs)
            //{
            //    //tex.Write(ie.ToString());
            //    YenAlgorithm yen = new YenAlgorithm(t, cost, 10);
            //    List<List<Link>> result = new List<List<Link>>();
            //    result = yen.GetKShortestPath(ie.Ingress, ie.Egress);
            //    foreach (var path in result)
            //    {

            //        double delaypath = 0;
            //        foreach (var link in path)
            //        {
            //                delaypath += link.Delay;
            //           // tex.Write(link.);
            //        }
            //        DelayOfPath.Add(delaypath);
            //        //tex.Write("\t" + delaypath);
            //    }
            //    //tex.WriteLine();
            //}
            //double ttvariance = DelayOfPath.Variance();
            //double ttstandardDeviation = DelayOfPath.StandardDeviation();
            //double ttmean = DelayOfPath.Mean();
            //double ttmax = DelayOfPath.Max();
            //double ttmin = DelayOfPath.Min();
            //double ttave = DelayOfPath.Average();
            //tex.WriteLine(ttave + "\t" + ttmin + "\t" + ttmax + "\t" + ttmean + "\t" + ttstandardDeviation + "\t" + ttvariance);
            //tex.Close();
            
            //Random r = new Random();

            //foreach (var link in t.Links)
            //{
            //    cost[link] = 1;
            //}

            //OBDCRA aa = new OBDCRA(t);
            //aa.GetPath(null);
            
            //ra.DetermineLeastCost(t, t.Nodes[0], cost);

            //CFDQOSRA cfds = new CFDQOSRA(t);
            //System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
            //st.Start();
            //CFDQOSRA cfd = new CFDQOSRA(t);

            ////cfd.GetPath(0, 10, 50, 2, cost);
            //st.Stop();
            //Console.WriteLine(st.ElapsedMilliseconds);
            

            //FloydWarshall f = new FloydWarshall();
            //f.GetMinimumDistances(t, cost);

            //ShortestPaths sp = new ShortestPaths(t);

            //var paths = sp.GetShortestPath(t.Nodes[0], t.Nodes[9]);

            //foreach (var path in paths)
            //{
            //    foreach (var link in path)
            //    {
            //        Console.WriteLine(link);
            //    }
            //    Console.WriteLine("----------------------------------------");
            //}
            
            //STRA stra = new STRA(t);
            //stra.GetPath(0, 12, 50);

            //SRRA ssra = new SRRA(t);

            //foreach (var link in ssra.GetPath(0, 12, 50))
            //{
            //    Console.WriteLine(link);
            //}

            //int[] arr = new int[] { 3, 2, 2, 3, 4, 4, 6, 7, 8, 3, 2, 4, 6, 7, 5, 3, 4, 5, 6, 7, 8, 6, 5, 4, 4 };

            //double a = Math.Sqrt((arr.Sum(o => Math.Pow(o, 2) / (double)arr.Length) - Math.Pow(arr.Sum() / arr.Length, 2)));

            //Console.WriteLine(a);

            //double a = Math.Sqrt(t.Links.Sum(o => Math.Pow(o.ResidualBandwidth, 2))

            //Ticker a = new Ticker();
            //a.Start();
            //a.Stop();

            //NNRA aaa = new NNRA(t);

            //aaa.GetPath(0, 5, 10);

            // caoth
            SimulatorManager sm = SimulatorManager.getInstance();
            try
            {
                sm.Start();
                //sm.StartMulticast();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //GetDelayInfo a = new GetDelayInfo(t);
            //a.WriteInfo();
            //Console.ReadKey();
        }
    }
}

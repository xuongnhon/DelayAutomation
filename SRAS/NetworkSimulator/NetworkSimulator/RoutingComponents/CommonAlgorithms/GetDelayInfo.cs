using NetworkSimulator.NetworkComponents;
using NetworkSimulator.RoutingComponents.CommonObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetworkSimulator.RoutingComponents.CommonAlgorithms
{
    public static class MyListExtensions
    {
        public static double Mean(this List<double> values)
        {
            return values.Count == 0 ? 0 : values.Mean(0, values.Count);
        }

        public static double Mean(this List<double> values, int start, int end)
        {
            double s = 0;

            for (int i = start; i < end; i++)
            {
                s += values[i];
            }

            return s / (end - start);
        }

        public static double Variance(this List<double> values)
        {
            return values.Variance(values.Mean(), 0, values.Count);
        }

        public static double Variance(this List<double> values, double mean)
        {
            return values.Variance(mean, 0, values.Count);
        }

        public static double Variance(this List<double> values, double mean, int start, int end)
        {
            double variance = 0;

            for (int i = start; i < end; i++)
            {
                variance += Math.Pow((values[i] - mean), 2);
            }

            int n = end - start;
            if (start > 0) n -= 1;

            return variance / (n);
        }

        public static double StandardDeviation(this List<double> values)
        {
            return values.Count == 0 ? 0 : values.StandardDeviation(0, values.Count);
        }

        public static double StandardDeviation(this List<double> values, int start, int end)
        {
            double mean = values.Mean(start, end);
            double variance = values.Variance(mean, start, end);

            return Math.Sqrt(variance);
        }
    }
    public class GetDelayInfo
    {
        private AllSimplePaths _AllPaths;
        private Topology _Topology;
        
        public GetDelayInfo(Topology t)
        {

            _Topology = new Topology(t);
            _Topology.IEPairs = t.IEPairs;
            _AllPaths = new AllSimplePaths(_Topology);
        }



        public void WriteInfo()
        {
            List<List<Link>> Result;
            Console.WriteLine(_Topology.IEPairs.Count);

            List<double> DelayOfPath = new List<double>();

            FileInfo myfile = new FileInfo("./CESNET_delay.txt");
            StreamWriter tex = myfile.CreateText();
            tex.WriteLine("AVERAGE \t MIN \t MAX \t MEAN \t SD \t VARIANCE");

            //List<IEPair> listIE = new List<IEPair>();
            //listIE.Add(_Topology.IEPairs[16]);
            /*listIE.Add(_Topology.IEPairs[102]);
            listIE.Add(_Topology.IEPairs[284]);
            listIE.Add(_Topology.IEPairs[310]);
            listIE.Add(_Topology.IEPairs[463]);*/
            //int countfile = 0;
            Dictionary<Link, double> _LinkCost = new Dictionary<Link,double>();

            foreach (var link in _Topology.Links)
            {
                _LinkCost[link] = 1;
            }
            foreach (var ie in _Topology.IEPairs)
            //foreach (var ie in listIE)
	        {
                //Console.WriteLine(ie.ToString());

                Result = _AllPaths.GetPaths(ie.Ingress, ie.Egress);
               

                //DelayOfPath = new List<double>();
                List<double> DelayOfIE = new List<double>();
                foreach (var path in Result)
                {
                    double t = 0;
                    //string strpath = "";
                    foreach (var link in path)
                    {
                        t += link.Delay;
                        //strpath += link.Source.Key + " -> ";
                    }
                    DelayOfIE.Add(t);

                }
                DelayOfPath.AddRange(DelayOfIE);
                double variance = DelayOfIE.Variance();
                double standardDeviation = DelayOfIE.StandardDeviation();
                double mean = DelayOfIE.Mean();
                double max = DelayOfIE.Max();
                double min = DelayOfIE.Min();
                double ave = DelayOfIE.Average();

              //  tex.WriteLine(ie.ToString() + "\t" + ave + "\t" + min + "\t" + max + "\t" + mean + "\t" + standardDeviation + "\t" + variance);
	        }

            double ttvariance = DelayOfPath.Variance();
            double ttstandardDeviation = DelayOfPath.StandardDeviation();
            double ttmean = DelayOfPath.Mean();
            double ttmax = DelayOfPath.Max();
            double ttmin = DelayOfPath.Min();
            double ttave = DelayOfPath.Average();
            tex.WriteLine(ttave + "\t" + ttmin + "\t" + ttmax + "\t" + ttmean + "\t" + ttstandardDeviation + "\t" + ttvariance);
           // tex.WriteLine();

            tex.Close();
    
        }
    }
}

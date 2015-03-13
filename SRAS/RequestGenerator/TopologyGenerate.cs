using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Troschuetz.Random;
using System.Reflection;

namespace RequestGenerator
{
    class TopologyGenerate
    {
        public void Generate(string filename, string sourcePath, int numOfLink, int[] DL)
        {
            ReadMap._FilePath = sourcePath;
            List<Link> _ListLink = ReadMap.LoadMap();


            FileStream file = new FileStream(filename, FileMode.Create);
            StreamWriter wr = new StreamWriter(file);

            DiscreteUniformDistribution randomForDL =
                          new DiscreteUniformDistribution(new StandardGenerator(Guid.NewGuid().GetHashCode()));
            randomForDL.Beta = DL[1];
            randomForDL.Alpha = DL[0];

            int dl;
            string _SpaceTab = "\t";

            wr.WriteLine(numOfLink);

            for (int i = 0; i < ReadMap._NumOfLink; i++)
            {
                dl = randomForDL.Next();
                _ListLink[i].Delay = dl;

                wr.WriteLine(_ListLink[i].Ingress + _SpaceTab + _ListLink[i].Egress + _SpaceTab + _ListLink[i].Type + _SpaceTab + _ListLink[i].Capacity + _SpaceTab + dl);
                Console.WriteLine(_ListLink[i].Ingress + _SpaceTab + _ListLink[i].Egress + _SpaceTab + _ListLink[i].Type + _SpaceTab + _ListLink[i].Capacity + _SpaceTab + dl);
            }
            wr.Close();
            file.Close();
        }
    }

    public class Link
    {
        #region Fields

        public int Ingress { get; set; }

        public int Egress { get; set; }

        public string Type { get; set; }

        public double Capacity { get; set; }

        public int Delay { get; set; }

        #endregion

        #region Properties
        
        #endregion

        public Link(int ingress, int egress, string type, double capacity, int delay)
        {
            this.Ingress = ingress;
            this.Egress = egress;
            this.Type = type;
            this.Capacity = capacity;
            this.Delay = delay;
        }
    }

    public static class ReadMap
    {
        public static int _NumOfLink { get; set; }
        public static string _FilePath { get; set; }

        public static List<Link> LoadMap()
        {
            List<Link> _LinkList = new List<Link>();

            //Load request 
            Assembly assembly = Assembly.GetExecutingAssembly();
            string path = System.IO.Path.GetDirectoryName(assembly.Location) + _FilePath;


            FileStream file = new FileStream(path, FileMode.Open);
            StreamReader reader = new StreamReader(file);

            _LinkList = new List<Link>();
            _NumOfLink = 0;
            while (!reader.EndOfStream)
            {
                string[] value = reader.ReadLine().Split('\t');
                Link package = MakeLink(value);
                _LinkList.Add(package);
                _NumOfLink++;
            }
            reader.Close();
            return _LinkList;
        }

        private static Link MakeLink(string[] value)
        {
            int ingress = int.Parse(value[0]);
            int egress = int.Parse(value[1]);
            string type = value[2];
            double capacity = double.Parse(value[3]);       
            int delay = 0;

            return new Link(ingress, egress, type, capacity, delay);
        }
    }
}

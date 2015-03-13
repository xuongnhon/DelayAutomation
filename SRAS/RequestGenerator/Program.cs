using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RequestGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            //int[] DL_TOPO = new int[] { 0, 50 };
            //TopologyGenerate t = new TopologyGenerate();
            //t.Generate("ansnet_map.dat", "\\Map\\ansnet_map.inp", 32, DL_TOPO);
            //t.Generate("mira_map.dat", "\\Map\\mira_map.inp", 15, DL_TOPO);

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            int[,] IE_MIRA = new int[,] { { 0, 12 }, { 4, 8 }, { 3, 1 }, { 4, 14 } };
            int[] P_IE_MIRA = new int[] { 25, 25, 25, 25 };
            int[] BW_MIRA = new int[] { 10, 15, 20 };
            int[] DL_MIRA = new int[] { 35, 60 };


            int[,] IE_ANSNET = new int[,] { { 1, 29 }, { 18, 6 }, { 4, 23 }, { 7, 31 }, { 21, 17 } };
            int[] P_IE_ANSNET = new int[] { 20, 20, 20, 20, 20 };
            int[] BW_ANSNET = new int[] { 10, 20, 30, 40, 50};
            int[] DL_ANSNET = new int[] { 100, 155 };
            
            int[,] IE_NET1 = new int[870, 2];
            int[] P_IE_NET1 = new int[] { 20, 20, 20, 20, 20 };
            int[] BW_NET1 = new int[] { 3, 5, 7, 9 };
            int[] DL_NET1 = new int[] { 26, 32 };

            //DUNG: To write IE pairs of NET1 to file
            //FileInfo myfile = new FileInfo("IE_NET1.txt");
            //StreamWriter tex = myfile.CreateText();

            int count = 0;
            for (int i = 0; i < 30; i++)
            {
                for (int j = 0; j < 30; j++)
                {
                    if (i != j)
                    {
                        IE_NET1[count, 0] = i;
                        IE_NET1[count, 1] = j;
                        count++;
            //            tex.WriteLine(i + "\t" + j);

                    }

                }
            }
            //tex.Close();

            #region CESNET config
            int[,] IE_CESNET = new int[,] { { 0, 18 }, { 1, 11 }, { 3, 16 }, { 4, 7 }, { 5, 13} , { 6, 19}, { 15, 0}, { 19, 8}};
            int[] P_IE_CESNET= new int[] { 25, 25, 25, 25 };
            int[] BW_CESNET = new int[] { 80, 100, 120, 140 };
            int[] DL_CESNET = new int[] { 50, 80 };
            #endregion

            //Run Static
            //Screnario staticScenario = new StaticScenario(IE_MIRA, P_IE_MIRA, BW_MIRA, 1, 700, 5, DL_MIRA);
            //staticScenario.Generate("Thao_37_static_MIRA_bw10-15-20-30_1000_dl[36-70].txt");

            //Screnario staticScenario = new StaticScenario(IE_ANSNET, P_IE_ANSNET, BW_ANSNET, 1, 700, 5, DL_ANSNET);
            //staticScenario.Generate("Thao_17_static_ANSNET_bw10-15-20-25_700_dl[100-155].txt");

            //Screnario staticScenario = new StaticScenario(IE_CESNET, P_IE_CESNET, BW_CESNET, 1, 1000, 5, DL_CESNET);
            //staticScenario.Generate("Thao_2_static_CESNET_bw20-40-60-80_1000_dl[50-80].txt");

            //Screnario staticScenario = new StaticScenario(IE_NET1, P_IE_NET1, BW_NET1, 1, 700, 5, DL_NET1);
            //staticScenario.Generate("11_static_NET1_bw1_700_dl[4-10].txt");

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            //Run Dynamic

            // λ=40 request per (timeUnit * TimerInterval) and μ=10
            // timeUnit = 400 --> runtime = 400 * TimerInterval (lần tick)            

            //DynamicScrenario dynamicScenario = new DynamicScrenario(IE_MIRA, P_IE_MIRA, BW_MIRA, 400, 2000, 40, 10, DL_MIRA);
            //dynamicScenario.Generate("Thao_26_dynamic_MIRA_bw10-15-20_400_2000_40_10_dl[35-60].txt");

            //DynamicScrenario dynamicScenario = new DynamicScrenario(IE_ANSNET, P_IE_ANSNET, BW_ANSNET, 400, 5000, 80, 30, DL_ANSNET);
            //dynamicScenario.Generate("Thao_23_dynamic_ANSNET_bw10-20-30-40-50_400_5000_80_30_dl[100-155].txt");

            DynamicScrenario dynamicScenario = new DynamicScrenario(IE_CESNET, P_IE_CESNET, BW_CESNET, 400, 5000, 40, 10, DL_CESNET);
            dynamicScenario.Generate("Thao_35_dynamic_CESNET_bw80-100-120-140_400_5000_40_10_[50-80].txt");

            //DynamicScrenario dynamicScenario = new DynamicScrenario(IE_NET1, P_IE_NET1, BW_NET1, 400, 2000, 80, 30, DL_NET1);
            //dynamicScenario.Generate("15_dynamic_NET1_bw3-5-7-9_400_2000_80_20_[26-32].txt");           

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            //Run Mix
            // λ=40 and μ=10

            //MixScenario mixScenario = new MixScenario(IE_MIRA, P_IE_MIRA, BW_MIRA, 400, 200, 1800, 40, 10, DL_MIRA);
            //mixScenario.Generate("mix_MIRA_bw10-20-30-40-50_400_200+1800_40_10.txt");

            //MixScenario mixScenario = new MixScenario(IE_ANSNET, P_IE_ANSNET, BW_ANSNET, 400, 200, 1800, 40, 10, DL_ANSNET);
            //mixScenario.Generate("mix_ANSNET_bw10-20-30-40-50_400_200+1800_40_10.txt");           

            ////////////////////////////////////////////////////////////////////////////////////////////////////

            //int[] arr = new int[4];
            //Troschuetz.Random.DiscreteUniformDistribution r = new Troschuetz.Random.DiscreteUniformDistribution();
            //r.Alpha = 0;
            //r.Beta = 99;
            //for (int i = 0; i < 1000; i++)
            //{
            //    arr[GetIEId(r.Next(), P)]++;
            //}

            //foreach (var item in arr)
            //{
            //    Console.WriteLine(item);
            //}

            //TestDistribution();
            Console.WriteLine("Done");
            Console.ReadKey();
        }

        static void TestDistribution()
        {
            FileStream f = new FileStream("Exponential_2000_10.txt", FileMode.Create);
            StreamWriter wr = new StreamWriter(f);

            Troschuetz.Random.ExponentialDistribution r = new Troschuetz.Random.ExponentialDistribution();
            //double mu = 10; // mean
            r.Lambda = 0.15;//1 / mu;
            for (int i = 0; i < 2000; i++) // 2000
            {
                wr.WriteLine(r.NextDouble());
            }

            wr.Close();
        }
    }
}

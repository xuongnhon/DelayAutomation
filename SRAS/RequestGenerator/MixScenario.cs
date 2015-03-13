using System;
using System.IO;
using Troschuetz.Random;

namespace RequestGenerator
{
    class MixScenario : Screnario
    {
        private double lamda;
        private double mu;
        private int numOfDynamicRequest;

        public MixScenario(int[,] D, int[] P, int[] B, int timeUnit, int numOfStaticRequest,int numOfDynamicRequest,  double lamda, double mu, int[] DL)
            : base(D, P, B, timeUnit, numOfStaticRequest, DL)
        {
            this.lamda = lamda;
            this.mu = mu;
            this.numOfDynamicRequest = numOfDynamicRequest;
        }

        public override void Generate(string filename)
        {
            FileStream file = new FileStream(filename, FileMode.Create);
            StreamWriter wr = new StreamWriter(file);

            //StandardGenerator generator = new StandardGenerator();

            DiscreteUniformDistribution randomForB =
                new DiscreteUniformDistribution(new StandardGenerator(Guid.NewGuid().GetHashCode()));
            randomForB.Beta = B.Length - 1;
            randomForB.Alpha = 0;

            DiscreteUniformDistribution randomForD =
                new DiscreteUniformDistribution(new StandardGenerator(Guid.NewGuid().GetHashCode()));
            randomForD.Beta = 99;
            randomForD.Alpha = 0;

            ExponentialDistribution randomForHoldingTime =
                new ExponentialDistribution(new StandardGenerator(Guid.NewGuid().GetHashCode()));
            randomForHoldingTime.Lambda = 1 / mu;

            PoissonDistribution randomForNumberOfReq =
                new PoissonDistribution();
            randomForNumberOfReq.Lambda = lamda;

            DiscreteUniformDistribution randomForDL =
                new DiscreteUniformDistribution(new StandardGenerator(Guid.NewGuid().GetHashCode()));
            randomForDL.Beta = DL[1];
            randomForDL.Alpha = DL[0];


            int d, b, reqCount = 0, numOfReqPerTimeUnit, time = 0, dl;
            double holdingTime, incomingTime;

            // xem numberOfRequest như là số request static
            while (reqCount < numberOfRequest + numOfDynamicRequest)
            {
                numOfReqPerTimeUnit = randomForNumberOfReq.Next();
                if (reqCount + numOfReqPerTimeUnit > (numberOfRequest + numOfDynamicRequest))
                    numOfReqPerTimeUnit = (numberOfRequest + numOfDynamicRequest) - reqCount;

                for (int i = 0; i < numOfReqPerTimeUnit; i++)
                {
                    d = GetDemand(randomForD.Next());
                    b = randomForB.Next();
                    dl = randomForDL.Next();

                    holdingTime = randomForHoldingTime.NextDouble() * timeUnit;
                    incomingTime = time * timeUnit + i * (timeUnit / numOfReqPerTimeUnit);

                    // 7/8/13 ngoctoan
                    if (reqCount < numberOfRequest)
                    {
                        Request req = new Request(reqCount, D[d, 0], D[d, 1], B[b], (long)incomingTime, int.MaxValue, dl);
                        Console.WriteLine(req);
                        wr.WriteLine(req);
                    }
                    else
                    {
                        Request req = new Request(reqCount, D[d, 0], D[d, 1], B[b], (long)incomingTime, (long)holdingTime, dl);
                        Console.WriteLine(req);
                        wr.WriteLine(req);
                    }
                    
                    reqCount++;
                }
                time++;
            }
            wr.Close();
            file.Close();
        }
    }
}
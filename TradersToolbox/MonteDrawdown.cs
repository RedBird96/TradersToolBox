using System;
using System.Collections.Generic;
using System.Linq;
using ZedGraph;

namespace TradersToolbox
{
    class MonteDrawdown
    {
        private readonly List<float> drawdowns;

        //private readonly double average_drawdown;
        //private readonly double stdev_drawdown;
        private readonly double account_size;

        public PointPairList ppl, ppl2;

        class Bin
        {
            public int count;
            public float value;
            public float cdf;
        }

        public MonteDrawdown(IReadOnlyList<float> values, int num, double accountSize)
        {
            ppl = new PointPairList();
            ppl2 = new PointPairList();

            account_size = accountSize;
            double mdd_sum = 0.0;
            double mdd_sumsqr = 0.0;
            drawdowns = new List<float>();

            Random r = new Random();
            for (int i = 0; i < num; i++)
            {
                values = values.OrderBy(x => (r.Next())).ToList();
                
                float sum = 0, maxx = 0, dd = 0, mdd = 0;
                for (int j = 0; j < values.Count; j++)
                {
                    sum += values[j];
                    if (sum > maxx) { maxx = sum; }
                    dd = maxx - sum;
                    if (dd > mdd) { mdd = dd; }
                }
                float thisdd = (float)(mdd / account_size) * 100;
                drawdowns.Add(thisdd);
               // System.Diagnostics.Debug.WriteLine(thisdd);
                mdd_sum += mdd;
                mdd_sumsqr += mdd * mdd;
            }

            // in points or dollar terms
            //average_drawdown = mdd_sum / num;
            //stdev_drawdown = Math.Sqrt(mdd_sumsqr / num - average_drawdown * average_drawdown);
            //System.out.printf("95%% Confidence: %.2f to %.2f\n", (average_drawdown - 2*stdev_drawdown),(average_drawdown + 2*stdev_drawdown));

            //   PMF - store drawdowns in bins
            // locate max and min %
            float max = 0, min = 1000000;
            for (int i = 0; i < drawdowns.Count; i++)
            {
                if (drawdowns[i] > max) { max = drawdowns[i]; }
                if (drawdowns[i] < min) { min = drawdowns[i]; }
            }

            // create bins
            List<Bin> Bins = new List<Bin>();
            for (float j = min; j <= max; j += .5f)
            {
                Bin bObj = new Bin
                {
                    count = 0,
                    value = j
                };
                Bins.Add(bObj);
            }

            // count bin occurrences
            for (int k = 0; k < drawdowns.Count; k++)
            {
                for (int l = 1; l < Bins.Count; l++)
                {
                    if ((drawdowns[k] <= Bins[l].value && drawdowns[k] > Bins[l - 1].value)
                        || (l == 1 && drawdowns[k] <= Bins[l-1].value)
                        || (l == Bins.Count - 1 && drawdowns[k] > Bins[l].value))
                    {
                        Bins[l - 1].count++;
                    }
                }
            }

            // Cumulative Distribution Function
            int counter = 0;
            for (int i = 0; i < Bins.Count; i++)
            {
                counter += Bins[i].count;
                Bins[i].cdf = counter * 100.0f / num;
              //  System.Diagnostics.Debug.WriteLine(Bins[i].value + " " + Bins[i].count + " " + Bins[i].cdf);
            }
            // cumulative

            Bins[0].count = 0;  //test code

            // cunstruct graph info
            ppl2.Add(0, 0);
            for (int i = 0; i < Bins.Count; i++)
            {
                if (Bins.Count <= 50)
                    ppl.Add(i, Bins[i].count);
                ppl2.Add(i, Bins[i].cdf);
                //ppl2.Add(i, Bins[i].count);  //test code
            }
            if (ppl.Count == 0)
            {
                int z = (int)(0.02 * Bins.Count);
                for (int i = 0; i <= Bins.Count / z; i++)
                {
                    int sum = 0;
                    for (int j = i * z; j < (i + 1) * z && j < Bins.Count; j++)
                        sum += Bins[j].count;
                    ppl.Add(i * z, sum);
                }
            }
        }
    }
}
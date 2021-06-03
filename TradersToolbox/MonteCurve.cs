using System;
using System.Collections.Generic;
using ZedGraph;

namespace TradersToolbox
{
    class ECBands
    {
        public PointPairList ppl, ppl2;

        public ECBands(IReadOnlyList<float> results, int to_pick, int iterations, double lower_band, double upper_band)
        {
            ppl = new PointPairList();
            ppl2 = new PointPairList();

            Random random = new Random();
            // house keeping
            int max = results.Count;
            List<List<float>> All = new List<List<float>>();

            for (int i = 0; i < iterations; i++)
            {
                List<float> finalCurve = new List<float>();
                List<float> tradesCopy = new List<float>(results);
                
                for (int j = 0; j < to_pick; j++)
                    tradesCopy.Add(results[random.Next(max)]);

                // make new equity curve 
                float running_sum = 0;
                for (int k = 0; k < tradesCopy.Count; k++)
                {
                    running_sum += tradesCopy[k];
                    finalCurve.Add(running_sum);
                }

                // store equity curve
                All.Add(finalCurve);
            }

            // sort All by each one's last element
          //  for (int j = 0; j < All[0].Count; j++)
          //      System.Diagnostics.Debug.WriteLine(String.Format("{0} {1} {2}", All[0][j], All[1][j], All[2][j]));

            //  SORT
            int COLUMN = All[0].Count - 1;
            All.Sort((o1, o2) => o1[COLUMN].CompareTo(o2[COLUMN]));

            //  PRINT and Store Percentiles
            List<float> lowerBounds = new List<float>();
            List<float> upperBounds = new List<float>();

            int lower = (int)((lower_band / 100.00) * iterations);
            if (lower < 1) { lower = 1; }
            int upper = (int)((upper_band / 100.00) * iterations);

        //    System.Diagnostics.Debug.WriteLine("LOWER: " + lower);
        //    System.Diagnostics.Debug.WriteLine("UPPER: " + upper);

            for (int j = 0; j < All[0].Count; j++)
            {
                lowerBounds.Add(All[lower][j]);
                upperBounds.Add(All[upper][j]);

          //      System.Diagnostics.Debug.WriteLine(String.Format("{0} {1}", All[lower][j], All[upper][j]));
            }
            // cunstruct graph info
            for (int i = 0; i < lowerBounds.Count; i++)
                ppl.Add(i, lowerBounds[i]);
            for (int i = 0; i < upperBounds.Count; i++)
                ppl2.Add(i, upperBounds[i]);
        }
    }
}
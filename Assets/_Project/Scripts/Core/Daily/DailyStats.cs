using System;

namespace Meowdoku.Core.Daily
{
    public static class DailyStats
    {
        public static float BeatPercent(int elapsedSeconds, int rank, int size = 12)
        {
            if (elapsedSeconds <= 0) return 99f;

            double mu;
            double sigma;
            if (size == 10 && rank == 3)
            {
                mu = 6.6884;
                sigma = 1.6383;
            }
            else if (size == 10 && rank == 4)
            {
                mu = 6.7747;
                sigma = 1.4422;
            }
            else if (size == 10 && rank == 5)
            {
                mu = 6.7783;
                sigma = 1.3357;
            }
            else if (size == 12 && rank == 3)
            {
                mu = 6.7747;
                sigma = 1.4422;
            }
            else if (size == 12 && rank == 4)
            {
                mu = 6.7783;
                sigma = 1.3357;
            }
            else if (size == 12 && rank == 5)
            {
                mu = 7.1134;
                sigma = 1.3881;
            }
            else
            {
                mu = 6.7747;
                sigma = 1.4422;
            }

            double z = (Math.Log(elapsedSeconds) - mu) / sigma;
            double percent = Math.Round(
                (1d - NormalCdf(z)) * 1000d,
                MidpointRounding.AwayFromZero) / 10d;
            return (float)Math.Max(49d, Math.Min(99d, percent));
        }

        private static double NormalCdf(double value)
        {
            double t = 1d / (1d + 0.2316419d * Math.Abs(value));
            double polynomial = t * (
                0.31938153d +
                t * (-0.356563782d +
                t * (1.781477937d +
                t * (-1.821255978d + t * 1.330274429d))));
            double cdf = 1d -
                         1d / Math.Sqrt(2d * Math.PI) *
                         Math.Exp(-value * value / 2d) *
                         polynomial;
            return value >= 0d ? cdf : 1d - cdf;
        }
    }
}

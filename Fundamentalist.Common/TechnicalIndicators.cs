namespace Fundamentalist.Common
{
	public static class TechnicalIndicators
	{
		public static float GetSimpleMovingAverage(int days, float[] prices)
		{
			float simpleMovingAverage = prices.TakeLast(days).Sum() / days;
			return simpleMovingAverage;
		}

		public static float GetExponentialMovingAverage(int days, float[] prices)
		{
			var exponentialMovingAverage = new float[prices.Length];
			exponentialMovingAverage[days - 1] = GetSimpleMovingAverage(days, prices);
			float weight = 2.0f / (days + 1);
			for (int i = days; i < prices.Length; i++)
				exponentialMovingAverage[i] = weight * prices[i] + (1 - weight) * exponentialMovingAverage[i - 1];
			return exponentialMovingAverage[prices.Length - 1];
		}

		public static float GetRelativeStrengthIndex(int days, float[] prices)
		{
			float gains = 0.0f;
			float losses = 0.0f;
			for (int i = prices.Length - days; i < prices.Length; i++)
			{
				float change = prices[i] / prices[i - 1] - 1.0f;
				if (change >= 0)
					gains += change;
				else
					losses -= change;
			}
			float relativeStrengthIndex = 100.0f - 100.0f / (1.0f + gains / losses);
			return relativeStrengthIndex;
		}

		public static List<float> GetFeatures(decimal? currentPrice, float[] pastPrices)
		{
			float sma10 = GetSimpleMovingAverage(10, pastPrices);
			float sma20 = GetSimpleMovingAverage(20, pastPrices);
			float sma50 = GetSimpleMovingAverage(50, pastPrices);
			float sma200 = GetSimpleMovingAverage(200, pastPrices);
			float ema12 = GetExponentialMovingAverage(12, pastPrices);
			float ema26 = GetExponentialMovingAverage(26, pastPrices);
			float ema50 = GetExponentialMovingAverage(50, pastPrices);
			float ema200 = GetExponentialMovingAverage(200, pastPrices);
			float macd = ema12 - ema26;
			float rsi = GetRelativeStrengthIndex(14, pastPrices);
			var priceDataFeatures = new List<float>
				{
					(float)currentPrice.Value,
					sma10,
					sma20,
					sma50,
					sma200,
					ema12,
					ema26,
					ema50,
					ema200,
					macd,
					rsi
				};
			return priceDataFeatures;
		}
	}
}

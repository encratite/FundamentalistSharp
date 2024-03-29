﻿namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class CashFlow : DatedStatement
	{
		public Financing Financing { get; set; }
		public Investing Investing { get; set; }
		public Operating Operating { get; set; }
		public string Currency { get; set; }
		public int FiscalYearEndMonth { get; set; }

		public float[] GetFeatures()
		{
			var financingFeatures = Financing.GetFeatures();
			var investingFeatures = Investing.GetFeatures();
			var operatingFeatures = Operating.GetFeatures();
			var features = Features.Merge(
				financingFeatures,
				investingFeatures,
				operatingFeatures
			);
			return features;
		}

		public static FeatureName[] GetFeatureNames(CashFlow @this)
		{
			var financingFeatures = Financing.GetFeatureNames(@this?.Financing);
			var investingFeatures = Investing.GetFeatureNames(@this?.Investing);
			var operatingFeatures = Operating.GetFeatureNames(@this?.Operating);
			var features = Features.MergeNames(
				financingFeatures,
				investingFeatures,
				operatingFeatures
			);
			return features;
		}
	}
}

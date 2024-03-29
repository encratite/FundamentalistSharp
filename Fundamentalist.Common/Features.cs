namespace Fundamentalist.Common
{
	internal static class Features
	{
		public static float[] Aggregate(params object[] features)
		{
			if (features.Any(f => f == null))
				return null;
			return features.Select(f => Convert.ToSingle(f)).ToArray();
		}

		public static float[] Merge(params float[][] features)
		{
			if (features.Any(f => f == null))
				return null;
			IEnumerable<float> output = new float[] { };
			foreach (var f in features)
				output = output.Concat(f);
			return output.ToArray();
		}

		public static FeatureName[] AggregateNames(params FeatureName[] names)
		{
			return names;
		}

		public static FeatureName[] MergeNames(params FeatureName[][] names)
		{
			IEnumerable<FeatureName> output = new FeatureName[] { };
			foreach (var n in names)
				output = output.Concat(n);
			return output.ToArray();
		}
	}
}

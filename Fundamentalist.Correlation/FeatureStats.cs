using System.Collections.Concurrent;

namespace Fundamentalist.Correlation
{
	internal class FeatureStats
	{
		public string Name { get; set; }
		public ConcurrentBag<Observation> Observations { get; set; } = new ConcurrentBag<Observation>();
		public ConcurrentBag<float> PresenceGains { get; set; } = new ConcurrentBag<float>();
		public ConcurrentBag<float> AppearanceGains { get; set; } = new ConcurrentBag<float>();
		public ConcurrentBag<float> DisappearanceGains { get; set; } = new ConcurrentBag<float>();

		public float? MeanPresenceGain { get; set; }
		public float? MeanAppearanceGain { get; set; }
		public float? MeanDisappearanceGain { get; set; }

		public FeatureStats(string name)
		{
			Name = name;
		}

		public void SetGains()
		{
			MeanPresenceGain = GetMean(PresenceGains);
			MeanAppearanceGain = GetMean(AppearanceGains);
			MeanDisappearanceGain = GetMean(DisappearanceGains);
		}

		private float? GetMean(ConcurrentBag<float> gains)
		{
			if (gains.Count == 0)
				return null;
			return gains.Sum() / gains.Count;
		}

		public override string ToString()
		{
			return $"{Name} ({MeanPresenceGain}, {MeanAppearanceGain}, {MeanDisappearanceGain})";
		}
	}
}

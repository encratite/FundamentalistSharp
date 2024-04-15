using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Gam : IAlgorithm
	{
		int? _rank;
		private int _numberOfIterations;
		private int _maximumBinCountPerFeature;

		public string Name => $"Generalized Additive Model ({_rank?.ToString() ?? "-"}, {_numberOfIterations}, {_maximumBinCountPerFeature})";

		public Gam(int? rank, int numberOfIterations, int maximumBinCountPerFeature)
		{
			_rank = rank;
			_numberOfIterations = numberOfIterations;
			_maximumBinCountPerFeature = maximumBinCountPerFeature;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator;
			var trainer = mlContext.Regression.Trainers.Gam(numberOfIterations: _numberOfIterations, maximumBinCountPerFeature: _maximumBinCountPerFeature);
			if (_rank.HasValue)
			{
				estimator =
					mlContext.Transforms.ProjectToPrincipalComponents("Features", rank: _rank.Value)
					.Append(trainer);
			}
			else
				estimator = trainer;

			return estimator;
		}
	}
}

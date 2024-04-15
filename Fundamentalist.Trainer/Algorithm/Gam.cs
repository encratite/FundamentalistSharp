using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Gam : IAlgorithm
	{
		private int _numberOfIterations;
		private int _maximumBinCountPerFeature;

		public string Name => $"Generalized Additive Model ({_numberOfIterations}, {_maximumBinCountPerFeature})";

		public Gam(int numberOfIterations, int maximumBinCountPerFeature)
		{
			_numberOfIterations = numberOfIterations;
			_maximumBinCountPerFeature = maximumBinCountPerFeature;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.Gam(numberOfIterations: _numberOfIterations, maximumBinCountPerFeature: _maximumBinCountPerFeature);
			return estimator;
		}
	}
}

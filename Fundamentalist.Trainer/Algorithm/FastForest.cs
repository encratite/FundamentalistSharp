using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastForest : IAlgorithm
	{
		public string Name => "Fast Forest";

		public bool IsStochastic => false;

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.FastForest();
			return estimator;
		}
	}
}

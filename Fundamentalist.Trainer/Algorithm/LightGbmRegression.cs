using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class LightGbmRegression : IAlgorithm
	{
		public string Name => "Light Gradient Boosted Machine";

		public bool IsStochastic => false;

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.LightGbm(numberOfIterations: 100);
			return estimator;
		}
	}
}

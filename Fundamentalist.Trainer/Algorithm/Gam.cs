using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Gam : IAlgorithm
	{
		public string Name => "Generalized Additive Model";

		public bool IsStochastic => false;

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.Gam(numberOfIterations: 100);
			return estimator;
		}
	}
}

using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastTreeTweedie : IAlgorithm
	{
		public string Name => "Fast Tree Tweedie";

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.FastTreeTweedie();
			return estimator;
		}
	}
}

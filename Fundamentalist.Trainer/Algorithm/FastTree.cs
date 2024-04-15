using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastTree : IAlgorithm
	{
		public string Name => "Fast Tree";

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.FastTree();
			return estimator;
		}
	}
}

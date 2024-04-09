using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class OnlineGradientDescent : IAlgorithm
	{
		public string Name => "Online Gradient Descent";

		public bool IsStochastic => false;

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.Regression.Trainers.OnlineGradientDescent(numberOfIterations: 100));
			return estimator;
		}
	}
}

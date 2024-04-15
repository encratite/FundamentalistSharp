using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class OnlineGradientDescent : IAlgorithm
	{
		private int _numberOfIterations;
		private float _learningRate;
		private float _l2Regularization;

		public string Name => $"Online Gradient Descent ({_numberOfIterations}, {_learningRate}, {_l2Regularization})";

		public OnlineGradientDescent(int numberOfIterations, float learningRate, float l2Regularization)
		{
			_numberOfIterations = numberOfIterations;
			_learningRate = learningRate;
			_l2Regularization = l2Regularization;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.Regression.Trainers.OnlineGradientDescent(numberOfIterations: _numberOfIterations, learningRate: _learningRate, l2Regularization: _l2Regularization));
			return estimator;
		}
	}
}

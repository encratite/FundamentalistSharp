using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class LightGbmRegression : IAlgorithm
	{
		private int _iterations;
		private double? _learningRate;
		private int? _numberOfLeaves;

		public string Name => $"Light Gradient Boosted Machine ({_iterations}, {_learningRate?.ToString() ?? "-"}, {_numberOfLeaves?.ToString() ?? "-"})";

		public LightGbmRegression(int iterations, double? learningRate = null, int? numberOfLeaves = null)
		{
			_iterations = iterations;
			_learningRate = learningRate;
			_numberOfLeaves = numberOfLeaves;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.LightGbm(numberOfIterations: _iterations, learningRate: _learningRate, numberOfLeaves: _numberOfLeaves);
			return estimator;
		}
	}
}

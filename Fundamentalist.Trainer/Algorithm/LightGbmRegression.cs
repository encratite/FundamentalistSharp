using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class LightGbmRegression : IAlgorithm
	{
		private int? _rank;
		private int _iterations;
		private double? _learningRate;
		private int? _numberOfLeaves;

		public string Name => $"Light Gradient Boosted Machine ({_rank?.ToString() ?? "-"}, {_iterations}, {_learningRate?.ToString() ?? "-"}, {_numberOfLeaves?.ToString() ?? "-"})";

		public LightGbmRegression(int? rank, int iterations, double? learningRate = null, int? numberOfLeaves = null)
		{
			_rank = rank;
			_iterations = iterations;
			_learningRate = learningRate;
			_numberOfLeaves = numberOfLeaves;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator;
			var trainer = mlContext.Regression.Trainers.LightGbm(numberOfIterations: _iterations, learningRate: _learningRate, numberOfLeaves: _numberOfLeaves);
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

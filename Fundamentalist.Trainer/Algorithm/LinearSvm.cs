using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class LinearSvm : IAlgorithm
	{
		private int _numberOfIterations;

		public string Name => $"LinearSvm ({_numberOfIterations})";

		public bool Calibrated => false;

		public LinearSvm(int maximumNumberOfIterations)
		{
			_numberOfIterations = maximumNumberOfIterations;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.BinaryClassification.Trainers.LinearSvm(numberOfIterations: _numberOfIterations));
			return estimator;
		}
	}
}

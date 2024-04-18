using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Sdca : IAlgorithm
	{
		private int _maximumNumberOfIterations;
		private float? _l1Regularization;
		private float? _l2Regularization;

		public string Name => $"Sdca ({_maximumNumberOfIterations}, {_l1Regularization?.ToString() ?? "null"}, {_l2Regularization?.ToString() ?? "null"})";

		public bool Calibrated => true;

		public Sdca(int maximumNumberOfIterations, float? l1Regularization, float? l2Regularization)
		{
			_maximumNumberOfIterations = maximumNumberOfIterations;
			_l1Regularization = l1Regularization;
			_l2Regularization = l2Regularization;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(maximumNumberOfIterations: _maximumNumberOfIterations, l1Regularization: _l1Regularization, l2Regularization: _l2Regularization));
			return estimator;
		}
	}
}

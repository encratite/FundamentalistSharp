using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Lbfgs : IAlgorithm
	{
		private float _optimizationTolerance;
		private float _l1Regularization;
		private float _l2Regularization;

		public string Name => $"Lbfgs ({_optimizationTolerance}, {_l1Regularization}, {_l2Regularization})";

		public bool Calibrated => true;

		public Lbfgs(float optimizationTolerance, float l1Regularization, float l2Regularization)
		{
			_optimizationTolerance = optimizationTolerance;
			_l1Regularization = l1Regularization;
			_l2Regularization = l2Regularization;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(optimizationTolerance: _optimizationTolerance, l1Regularization: _l1Regularization, l2Regularization: _l2Regularization));
			return estimator;
		}
	}
}

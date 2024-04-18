using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Sgd : IAlgorithm
	{
		private int _numberOfIterations;
		private double _learningRate;
		private float _l2Regularization;

		public string Name => $"Sgd ({_numberOfIterations}, {_learningRate}, {_l2Regularization})";

		public bool Calibrated => true;

		public Sgd(int numberOfIterations, double learningRate, float l2Regularization)
		{
			_numberOfIterations = numberOfIterations;
			_learningRate = learningRate;
			_l2Regularization = l2Regularization;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.BinaryClassification.Trainers.SgdCalibrated(numberOfIterations: _numberOfIterations, learningRate: _learningRate, l2Regularization: _l2Regularization));
			return estimator;
		}
	}
}

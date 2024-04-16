using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class LbfgsPoisson : IAlgorithm
	{
		private float _l1Regularization;
		private float _l2Regularization;
		private int _historySize;

		public string Name => $"L-BFGS Poisson ({_l1Regularization}, {_l2Regularization}, {_historySize})";

		public LbfgsPoisson(float l1Regularization, float l2Regularization, int historySize)
		{
			_l1Regularization = l1Regularization;
			_l2Regularization = l2Regularization;
			_historySize = historySize;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.Regression.Trainers.LbfgsPoissonRegression(l1Regularization: _l1Regularization, l2Regularization: _l2Regularization, historySize: _historySize));
			return estimator;
		}
	}
}

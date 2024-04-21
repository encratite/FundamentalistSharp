using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Sgd : IAlgorithm
	{
		private bool _pairwise;
		private int _numberOfIterations;
		private double _learningRate;
		private float _l2Regularization;

		public string Name => $"Sgd ({_pairwise}, {_numberOfIterations}, {_learningRate}, {_l2Regularization})";

		public bool Calibrated => true;

		public Sgd(bool pairwise, int numberOfIterations = 20, double learningRate = 0.01, float l2Regularization = 1e-6f)
		{
			_pairwise = pairwise;
			_numberOfIterations = numberOfIterations;
			_learningRate = learningRate;
			_l2Regularization = l2Regularization;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			var trainer = mlContext.BinaryClassification.Trainers.SgdCalibrated(numberOfIterations: _numberOfIterations, learningRate: _learningRate, l2Regularization: _l2Regularization);
			var trainers = mlContext.MulticlassClassification.Trainers;
			var normalize = mlContext.Transforms.NormalizeMinMax("Features");
			IEstimator<ITransformer> multiclass = _pairwise ? trainers.OneVersusAll(trainer) : trainers.PairwiseCoupling(trainer);
			var estimator = normalize.Append(multiclass);
			return estimator;
		}
	}
}

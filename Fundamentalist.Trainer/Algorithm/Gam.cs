using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Gam : IAlgorithm
	{
		int _numberOfIterations;
		int _maximumBinCountPerFeature;
		double _learningRate;

		public string Name => $"Gam ({_numberOfIterations}, {_maximumBinCountPerFeature}, {_learningRate})";

		public bool Calibrated => true;

		public Gam(int numberOfIterations, int maximumBinCountPerFeature, double learningRate)
		{
			_numberOfIterations = numberOfIterations;
			_maximumBinCountPerFeature = maximumBinCountPerFeature;
			_learningRate = learningRate;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.BinaryClassification.Trainers.Gam(numberOfIterations: _numberOfIterations, maximumBinCountPerFeature: _maximumBinCountPerFeature, learningRate: _learningRate);
			return estimator;
		}
	}
}

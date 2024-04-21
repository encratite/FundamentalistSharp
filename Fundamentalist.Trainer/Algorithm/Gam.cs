using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Gam : IAlgorithm
	{
		bool _pairwise;
		int _numberOfIterations;
		int _maximumBinCountPerFeature;
		double _learningRate;

		public string Name => $"Gam ({_pairwise}, {_numberOfIterations}, {_maximumBinCountPerFeature}, {_learningRate})";

		public bool Calibrated => true;

		public Gam(bool pairwise, int numberOfIterations, int maximumBinCountPerFeature = 255, double learningRate = 0.002)
		{
			_pairwise = pairwise;
			_numberOfIterations = numberOfIterations;
			_maximumBinCountPerFeature = maximumBinCountPerFeature;
			_learningRate = learningRate;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			var trainer = mlContext.BinaryClassification.Trainers.Gam(numberOfIterations: _numberOfIterations, maximumBinCountPerFeature: _maximumBinCountPerFeature, learningRate: _learningRate);
			var trainers = mlContext.MulticlassClassification.Trainers;
			var normalize = mlContext.Transforms.NormalizeMinMax("Features");
			IEstimator<ITransformer> multiclass = _pairwise ? trainers.OneVersusAll(trainer) : trainers.PairwiseCoupling(trainer);
			var estimator = normalize.Append(multiclass);
			return estimator;
		}
	}
}

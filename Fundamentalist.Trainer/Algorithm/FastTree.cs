using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastTree : IAlgorithm
	{
		bool _pairwise;
		int _numberOfLeaves;
		int _numberOfTrees;
		int _minimumExampleCountPerLeaf;
		double _learningRate;

		public string Name => $"FastTree ({_pairwise}, {_numberOfLeaves}, {_numberOfTrees}, {_minimumExampleCountPerLeaf}, {_learningRate})";

		public bool Calibrated => true;

		public FastTree(bool pairwise, int numberOfLeaves, int numberOfTrees, int minimumExampleCountPerLeaf, double learningRate)
		{
			_pairwise = pairwise;
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
			_minimumExampleCountPerLeaf = minimumExampleCountPerLeaf;
			_learningRate = learningRate;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			var trainer = mlContext.BinaryClassification.Trainers.FastTree(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf, learningRate: _learningRate);
			var trainers = mlContext.MulticlassClassification.Trainers;
			var normalize = mlContext.Transforms.NormalizeMinMax("Features");
			IEstimator<ITransformer> multiclass = _pairwise ? trainers.OneVersusAll(trainer) : trainers.PairwiseCoupling(trainer);
			var estimator = normalize.Append(multiclass);
			return estimator;
		}
	}
}

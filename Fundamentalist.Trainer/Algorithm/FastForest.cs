using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastForest : IAlgorithm
	{
		bool _pairwise;
		int _numberOfLeaves;
		int _numberOfTrees;
		int _minimumExampleCountPerLeaf;

		public string Name => $"FastForest ({_pairwise}, {_numberOfLeaves}, {_numberOfTrees}, {_minimumExampleCountPerLeaf})";

		public bool Calibrated => false;

		public FastForest(bool pairwise, int numberOfLeaves, int numberOfTrees, int minimumExampleCountPerLeaf)
		{
			_pairwise = pairwise;
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
			_minimumExampleCountPerLeaf = minimumExampleCountPerLeaf;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			var trainer = mlContext.BinaryClassification.Trainers.FastForest(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf);
			var trainers = mlContext.MulticlassClassification.Trainers;
			var normalize = mlContext.Transforms.NormalizeMinMax("Features");
			IEstimator<ITransformer> multiclass = _pairwise ? trainers.OneVersusAll(trainer) : trainers.PairwiseCoupling(trainer);
			var estimator = normalize.Append(multiclass);
			return estimator;
		}
	}
}

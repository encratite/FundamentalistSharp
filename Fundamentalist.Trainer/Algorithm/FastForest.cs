using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastForest : IAlgorithm
	{
		int _numberOfLeaves;
		int _numberOfTrees;
		int _minimumExampleCountPerLeaf;

		public string Name => $"FastForest ({_numberOfLeaves}, {_numberOfTrees}, {_minimumExampleCountPerLeaf})";

		public bool Calibrated => false;

		public FastForest(int numberOfLeaves, int numberOfTrees, int minimumExampleCountPerLeaf)
		{
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
			_minimumExampleCountPerLeaf = minimumExampleCountPerLeaf;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.BinaryClassification.Trainers.FastForest(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf);
			return estimator;
		}
	}
}

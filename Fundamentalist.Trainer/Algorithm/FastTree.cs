using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastTree : IAlgorithm
	{
		int _numberOfLeaves;
		int _numberOfTrees;
		int _minimumExampleCountPerLeaf;
		double _learningRate;

		public string Name => $"FastTree ({_numberOfLeaves}, {_numberOfTrees}, {_minimumExampleCountPerLeaf}, {_learningRate})";

		public bool Calibrated => true;

		public FastTree(int numberOfLeaves, int numberOfTrees, int minimumExampleCountPerLeaf, double learningRate)
		{
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
			_minimumExampleCountPerLeaf = minimumExampleCountPerLeaf;
			_learningRate = learningRate;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.BinaryClassification.Trainers.FastTree(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf, learningRate: _learningRate);
			return estimator;
		}
	}
}

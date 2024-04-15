using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastTreeTweedie : IAlgorithm
	{
		int _numberOfLeaves;
		int _numberOfTrees;
		int _minimumExampleCountPerLeaf;

		public string Name => $"Fast Tree Tweedie ({_numberOfLeaves}, {_numberOfTrees}, {_minimumExampleCountPerLeaf})";

		public FastTreeTweedie(int numberOfLeaves, int numberOfTrees, int minimumExampleCountPerLeaf)
		{
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
			_minimumExampleCountPerLeaf = minimumExampleCountPerLeaf;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{

			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.FastTreeTweedie(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf);
			return estimator;
		}
	}
}

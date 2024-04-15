using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastTree : IAlgorithm
	{
		int _numberOfLeaves;
		int _numberOfTrees;

		public string Name => $"Fast Tree ({_numberOfLeaves}, {_numberOfTrees})";

		public FastTree(int numberOfLeaves, int numberOfTrees)
		{
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{

			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.FastTree(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees);
			return estimator;
		}
	}
}

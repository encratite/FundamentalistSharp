using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastForest : IAlgorithm
	{
		int? _rank;
		int _numberOfLeaves;
		int _numberOfTrees;
		int _minimumExampleCountPerLeaf;

		public string Name => $"FastForest ({_rank?.ToString() ?? "-"}, {_numberOfLeaves}, {_numberOfTrees}, {_minimumExampleCountPerLeaf})";

		public FastForest(int? rank, int numberOfLeaves, int numberOfTrees, int minimumExampleCountPerLeaf)
		{
			_rank = rank;
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
			_minimumExampleCountPerLeaf = minimumExampleCountPerLeaf;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{

			IEstimator<ITransformer> estimator;
			var trainer = mlContext.Regression.Trainers.FastForest(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf);
			if (_rank.HasValue)
			{
				estimator =
					mlContext.Transforms.ProjectToPrincipalComponents("Features", rank: _rank.Value)
					.Append(trainer);
			}
			else
				estimator = trainer;

			return estimator;
		}
	}
}

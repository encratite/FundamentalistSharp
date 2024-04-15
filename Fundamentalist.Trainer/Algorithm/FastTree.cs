using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FastTree : IAlgorithm
	{
		int? _rank;
		int _numberOfLeaves;
		int _numberOfTrees;

		public string Name => $"Fast Tree ({_rank?.ToString() ?? "-"}, {_numberOfLeaves}, {_numberOfTrees})";

		public FastTree(int? rank, int numberOfLeaves, int numberOfTrees)
		{
			_rank = rank;
			_numberOfLeaves = numberOfLeaves;
			_numberOfTrees = numberOfTrees;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{

			IEstimator<ITransformer> estimator;
			var trainer = mlContext.Regression.Trainers.FastTree(numberOfLeaves: _numberOfLeaves, numberOfTrees: _numberOfTrees);
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

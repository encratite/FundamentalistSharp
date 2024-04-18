using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class LightLgbm : IAlgorithm
	{
		private int _numberOfIterations;
		private double? _learningRate;
		private int? _numberOfLeaves;
		private int? _minimumExampleCountPerLeaf;

		public string Name => $"LightLgbm ({_numberOfIterations}, {_learningRate?.ToString() ?? "null"}, {_numberOfLeaves?.ToString() ?? "null"}, {_minimumExampleCountPerLeaf?.ToString() ?? "null"})";

		public bool Calibrated => true;

		public LightLgbm(int numberOfIterations, double? learningRate, int? numberOfLeaves, int? minimumExampleCountPerLeaf)
		{
			_numberOfIterations = numberOfIterations;
			_learningRate = learningRate;
			_numberOfLeaves = numberOfLeaves;
			_minimumExampleCountPerLeaf = minimumExampleCountPerLeaf;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.BinaryClassification.Trainers.LightGbm(numberOfIterations: _numberOfIterations, learningRate: _learningRate, numberOfLeaves: _numberOfLeaves, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf);
			return estimator;
		}
	}
}

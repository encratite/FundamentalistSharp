using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class LdSvm : IAlgorithm
	{
		private int _numberOfIterations;
		private int _treeDepth;

		public string Name => $"LdSvm ({_numberOfIterations})";

		public bool Calibrated => false;

		public LdSvm(int maximumNumberOfIterations, int treeDepth)
		{
			_numberOfIterations = maximumNumberOfIterations;
			_treeDepth = treeDepth;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.BinaryClassification.Trainers.LdSvm(numberOfIterations: _numberOfIterations, treeDepth: _treeDepth));
			return estimator;
		}
	}
}

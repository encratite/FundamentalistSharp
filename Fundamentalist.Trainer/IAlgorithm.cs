using Microsoft.ML;

namespace Fundamentalist.Trainer
{
	internal interface IAlgorithm
	{
		public string Name { get; }
		public bool IsStochastic { get; }
		public IEstimator<ITransformer> GetEstimator(MLContext mlContext);
	}
}

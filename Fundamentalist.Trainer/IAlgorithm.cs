using Microsoft.ML;

namespace Fundamentalist.Trainer
{
	internal interface IAlgorithm
	{
		public string Name { get; }
		public bool Calibrated { get; }
		public IEstimator<ITransformer> GetEstimator(MLContext mlContext);
	}
}

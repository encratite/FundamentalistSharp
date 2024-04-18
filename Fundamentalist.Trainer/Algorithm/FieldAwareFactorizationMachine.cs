using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class FieldAwareFactorizationMachine : IAlgorithm
	{
		int _numberOfIterations;
		float _learningRate;
		int _latentDimension;

		public string Name => $"FieldAwareFactorizationMachine ({_numberOfIterations}, {_learningRate}, {_latentDimension})";

		public bool Calibrated => true;

		public FieldAwareFactorizationMachine(int numberOfIterations, float learningRate, int latentDimension)
		{
			_numberOfIterations = numberOfIterations;
			_learningRate = learningRate;
			_latentDimension = latentDimension;
		}

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			var options = new FieldAwareFactorizationMachineTrainer.Options()
			{
				NumberOfIterations = _numberOfIterations,
				LearningRate = _learningRate,
				LatentDimension = _latentDimension
			};
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine(options));
			return estimator;
		}
	}
}

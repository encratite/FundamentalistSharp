using Microsoft.ML;

namespace Fundamentalist.Trainer
{
	internal class Predictions
	{
		public IDataView TrainingPredictions { get; set; }
		public IDataView TestPredictions { get; set; }

		public Predictions(IDataView trainingPredictions, IDataView testPredictions)
		{
			TrainingPredictions = trainingPredictions;
			TestPredictions = testPredictions;
		}
	}
}

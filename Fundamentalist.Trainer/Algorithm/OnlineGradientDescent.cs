﻿using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class OnlineGradientDescent : IAlgorithm
	{
		public string Name => "Online Gradient Descent";

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator = mlContext.Regression.Trainers.OnlineGradientDescent(numberOfIterations: 100);
			return estimator;
		}
	}
}

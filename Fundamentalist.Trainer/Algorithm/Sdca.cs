﻿using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Sdca : IAlgorithm
	{
		public string Name => "Stochastic Dual Coordinated Ascent";

		public bool IsStochastic => true;

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.Regression.Trainers.Sdca(maximumNumberOfIterations: 100));
			return estimator;
		}
	}
}

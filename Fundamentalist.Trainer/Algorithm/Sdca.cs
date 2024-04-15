﻿using Microsoft.ML;

namespace Fundamentalist.Trainer.Algorithm
{
	internal class Sdca : IAlgorithm
	{
		public string Name => "Stochastic Dual Coordinated Ascent";

		public IEstimator<ITransformer> GetEstimator(MLContext mlContext)
		{
			IEstimator<ITransformer> estimator =
				mlContext.Transforms.ProjectToPrincipalComponents("Features", rank: 20)
				.Append(mlContext.Regression.Trainers.Sdca(maximumNumberOfIterations: 100));
			return estimator;
		}
	}
}

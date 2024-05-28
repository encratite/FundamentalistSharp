using Fundamentalist.Common;

namespace Fundamentalist.Backtest
{
	internal class Backtest
	{
		private Configuration _configuration;

		public void Run(Configuration configuration)
		{
			_configuration = configuration;
			var database = Utility.GetMongoDatabase(_configuration.ConnectionString);
		}
	}
}

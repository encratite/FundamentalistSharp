using System.Reflection;

namespace Fundamentalist.SqlAnalysis
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 7)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <from> <to> <upper> <lower> <limit> <form> <SQL Server connection string>");
				return;
			}
			int offset = 0;
			var getArgument = () => arguments[offset++];
			DateTime from = DateTime.Parse(getArgument());
			DateTime to = DateTime.Parse(getArgument());
			decimal upper = decimal.Parse(getArgument());
			decimal lower = decimal.Parse(getArgument());
			int limit = int.Parse(getArgument());
			string form = getArgument();
			string connectionString = getArgument();
			var sqlAnalysis = new SqlAnalysis();
			sqlAnalysis.Run(from, to, upper, lower, limit, form, connectionString);
		}
	}
}

using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace Fundamentalist.Common
{
	public static class Utility
	{
		public static T Deserialize<T>(string json)
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var output = JsonSerializer.Deserialize<T>(json, options);
			return output;
		}

		public static void WriteError(string message)
		{
			var previousColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ForegroundColor = previousColor;
		}

		public static IMongoDatabase GetMongoDatabase(string connectionString)
		{
			var pack = new ConventionPack
			{
				new CamelCaseElementNameConvention()
			};
			ConventionRegistry.Register(nameof(CamelCaseElementNameConvention), pack, _ => true);
			BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
			BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
			var client = new MongoClient(connectionString);
			var database = client.GetDatabase("fundamentalist");
			return database;
		}
	}
}

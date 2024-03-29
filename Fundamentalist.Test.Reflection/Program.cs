namespace Fundamentalist.Test.Reflection
{
	internal static class Program
	{
		private static void Main(string[] arguments)
		{
			var test = new BaseClass
			{
				Object1 = new DerivedClass()
				{
					Decimal1 = 1.23m
				},
				Object2 = new DerivedClass()
				{
					Decimal2 = 2.34m
				},
				String1 = "Unused",
				Int1 = 11
			};
			var features = GetFeatures(test, test.GetType());
		}

		private static List<float> GetFeatures(object obj, Type type)
		{
			var properties = type.GetProperties();
			var features = new List<float>();
			foreach (var property in properties)
			{
				var propertyType = property.PropertyType;
				if (
					obj != null &&
					propertyType.IsClass &&
					!propertyType.FullName.StartsWith("System."))
				{
					var userDefinedValue = property.GetValue(obj);
					var propertyFeatures = GetFeatures(userDefinedValue, propertyType);
					features.AddRange(propertyFeatures);
				}
				if (
					propertyType.IsGenericType &&
					propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
				)
				{
					var nullableType = propertyType.GetGenericArguments()[0];
					if (
						nullableType == typeof(decimal) ||
						nullableType == typeof(int)
					)
					{
						float value = 0.0f;
						if (obj != null)
						{
							var numericValue = property.GetValue(obj);
							if (numericValue != null)
								value = Convert.ToSingle(numericValue);
						}
						features.Add(value);
					}
				}
			}
			return features;
		}
	}
}
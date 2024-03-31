using System.Collections;

namespace Fundamentalist.Common
{
	public static class Features
	{
		public static List<float> GetFeatures(object obj)
		{
			return GetFeatures(obj, obj.GetType());
		}

		private static List<float> GetFeatures(object obj, Type type)
		{
			return
				GetFeaturesFromEnumerable(obj, type) ??
				GetFeaturesFromProperties(obj, type);
		}

		private static List<float> GetFeaturesFromEnumerable(object obj, Type type)
		{
			if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				var elementType = type.GetElementType() ?? type.GetGenericArguments()[0];
				var enumerable = (IEnumerable)obj;
				var enumerator = enumerable.GetEnumerator();
				var features = new List<float>();
				while (enumerator.MoveNext())
				{
					var currentFeatures = GetFeatures(enumerator.Current, elementType);
					features.AddRange(currentFeatures);
				}
				return features;
			}
			else
				return null;
		}

		private static List<float> GetFeaturesFromProperties(object @object, Type type)
		{
			var properties = type.GetProperties();
			var features = new List<float>();
			foreach (var property in properties)
			{
				var propertyType = property.PropertyType;
				if (
					@object != null &&
					propertyType.IsClass &&
					!propertyType.FullName.StartsWith("System.")
				)
				{
					var userDefinedValue = property.GetValue(@object);
					var propertyFeatures = GetFeatures(userDefinedValue, propertyType);
					features.AddRange(propertyFeatures);
				}
				var addValue = () =>
				{
					float value = 0.0f;
					if (@object != null)
					{
						var propertyValue = property.GetValue(@object);
						if (propertyValue != null)
							value = Convert.ToSingle(propertyValue);
					}
					features.Add(value);
				};
				var addDateTimeValue = () =>
				{
					float[] values = new float[]
					{
						0.0f,
						0.0f,
						0.0f
					};
					if (@object != null)
					{
						var propertyValue = property.GetValue(@object) as DateTime?;
						if (propertyValue != null)
						{
							var dateTime = propertyValue.Value;
							values[0] = dateTime.Year;
							values[1] = dateTime.Month;
							values[2] = dateTime.Day;
						}
					}
					features.AddRange(values);
				};
				bool hasFeatureAttribute = propertyType.CustomAttributes.Any(a => a.AttributeType == typeof(FeatureAttribute));
				if (
					propertyType.IsGenericType &&
					propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
				)
				{
					var nullableType = propertyType.GetGenericArguments()[0];
					if (nullableType == typeof(decimal))
						addValue();
					else if (hasFeatureAttribute && nullableType == typeof(DateTime))
						addDateTimeValue();
				}
				else if (hasFeatureAttribute)
					addValue();
			}
			return features;
		}
	}
}

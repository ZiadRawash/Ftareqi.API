using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ftareqi.Application.Common.Helpers
{
	public static class NotificationDataHelper
	{
		private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.Auto,
			Converters = new System.Collections.Generic.List<JsonConverter>
			{
				new StringEnumConverter()  
			}
		};

		/// <summary>
		/// Deserializes the JSON Data string to an object
		/// </summary>
		public static object DeserializeData(string dataJson)
		{
			if (string.IsNullOrEmpty(dataJson))
				return null!;

			return JsonConvert.DeserializeObject(dataJson, JsonSettings);
		}

		/// <summary>
		/// Serializes metadata object to JSON string
		/// </summary>
		public static string SerializeData<T>(T data)
		{
			return JsonConvert.SerializeObject(data, JsonSettings);
		}
	}
}
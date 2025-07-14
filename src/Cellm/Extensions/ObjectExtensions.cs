using System.Text.Json;

namespace Cellm.Extensions
{
    public static class ObjectExtensions
    {
        public static T Clone<T>(this T source)
        {
            if (source == null)
            {
                return default(T);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonString = JsonSerializer.Serialize(source, options);
            return JsonSerializer.Deserialize<T>(jsonString, options);
        }
    }
}
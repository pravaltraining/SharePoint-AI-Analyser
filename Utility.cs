using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Reflection;

namespace SharePointAnalyserDemo
{
    public class Utility
    {
        private readonly string _connectionString;

        public Utility(string connectionString)
        {
            _connectionString = connectionString;
        }

        public T GetDataFromSqlLite<T>(string sql, Dictionary<string, object>? parameters = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var result = command.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return default!;

            return (T)Convert.ChangeType(result, typeof(T));
        }

        public string GetEnumDescription<TEnum>(int value) where TEnum : Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
                return $"Unknown ({value})";

            var enumValue = (TEnum)(object)value;
            var memberInfo = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();

            if (memberInfo != null)
            {
                var descriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttribute != null)
                {
                    return descriptionAttribute.Description;
                }
            }

            return enumValue.ToString();
        }

        public TEnum GetEnumValue<TEnum>(int value) where TEnum : struct, Enum
        {
            if (Enum.IsDefined(typeof(TEnum), value))
            {
                return (TEnum)(object)value;
            }

            throw new ArgumentException($"Value '{value}' is not defined in enum {typeof(TEnum).Name}");
        }

    }
}

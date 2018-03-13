using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ContextClassForEntity
{
    class Context
    {
        public void InsertToTable<T>(T item, string tabelname)
        {
            List<SqlParameter> paramcollection = new List<SqlParameter>();
            Type itemtype = item.GetType();

            foreach (PropertyInfo property in itemtype.GetProperties())
            {
                Console.WriteLine(property.GetValue(item));
                int size;

                if (property.PropertyType.IsValueType)
                {
                    size = Marshal.SizeOf(property.PropertyType);
                }
                else
                {
                    size = property.GetValue(item).ToString().Length;
                }

                paramcollection.Add(new SqlParameter()
                {
                    ParameterName = property.Name,
                    SqlDbType = DBConvertNetType(property.PropertyType),
                    Value = property.GetValue(item),
                    Size = size
                });

            }
            StringBuilder command1 = new StringBuilder($"Insert into {tabelname}(");
            StringBuilder command2 = new StringBuilder(" values(");

            for (int i = 0; i < paramcollection.Count; i++)
            {
                if (i + 1 < paramcollection.Count)
                {
                    command1.Append($"{paramcollection[i].ParameterName},");
                    command2.Append($"@{paramcollection[i].ParameterName},");
                    paramcollection[i].ParameterName = $"@{paramcollection[i].ParameterName}";
                }
                else
                {
                    command1.Append($"{paramcollection[i].ParameterName})");
                    command2.Append($"@{paramcollection[i].ParameterName})");
                    paramcollection[i].ParameterName = $"@{paramcollection[i].ParameterName}";
                }

            }

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DataSource"].ConnectionString))
            {
                string commandstring = command1.Append(command2).ToString();
                Console.WriteLine(commandstring);
                connection.Open();
                SqlCommand command = new SqlCommand(commandstring, connection);
                foreach (var it in paramcollection)
                {
                    command.Parameters.Add(it);
                    Console.WriteLine(it.Value);
                }
                Console.WriteLine(command.ExecuteNonQuery());
            }

        }

        static IEnumerable<T> LoadFromDB<T>(string tablename)
        {
            using (SqlConnection connection = new SqlConnection
                (ConfigurationManager.ConnectionStrings["DataSource"].ConnectionString))
            {
                connection.Open();
                List<PropertyInfo> propertyList = typeof(T).GetProperties().ToList<PropertyInfo>();
                int count = 0;
                SqlCommand command = new SqlCommand($"Select * from {tablename}", connection);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    T obj = Activator.CreateInstance<T>();
                    while (count < propertyList.Count)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (propertyList[count].Name.Equals(reader.GetName(i)))
                            {
                                Type t = typeof(Convert);
                                string str = propertyList[count].PropertyType.ToString().Split('.')[1];
                                propertyList[count]
                                    .SetValue(obj, t.GetMethod("To" + str, new Type[] { typeof(object) })
                                    .Invoke(null, new object[] { reader[i] }));
                                count++;
                            }
                        }
                    }
                    count = 0;
                    yield return obj;
                }
            }
        }


        private SqlDbType DBConvertNetType(Type t)
        {
            Dictionary<Type, SqlDbType> typeMap = new Dictionary<Type, SqlDbType>();
            typeMap[typeof(string)] = SqlDbType.NVarChar;
            typeMap[typeof(char[])] = SqlDbType.NVarChar;
            typeMap[typeof(int)] = SqlDbType.Int;
            typeMap[typeof(Int32)] = SqlDbType.Int;
            typeMap[typeof(Int16)] = SqlDbType.SmallInt;
            typeMap[typeof(Int64)] = SqlDbType.BigInt;
            typeMap[typeof(Byte[])] = SqlDbType.VarBinary;
            typeMap[typeof(Boolean)] = SqlDbType.Bit;
            typeMap[typeof(DateTime)] = SqlDbType.DateTime2;
            typeMap[typeof(DateTimeOffset)] = SqlDbType.DateTimeOffset;
            typeMap[typeof(Decimal)] = SqlDbType.Decimal;
            typeMap[typeof(Double)] = SqlDbType.Float;
            typeMap[typeof(Decimal)] = SqlDbType.Money;
            typeMap[typeof(Byte)] = SqlDbType.TinyInt;
            typeMap[typeof(TimeSpan)] = SqlDbType.Time;

            return typeMap[t];

        }
    }

}

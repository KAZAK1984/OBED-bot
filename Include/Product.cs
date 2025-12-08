using Microsoft.Data.Sqlite;

namespace OBED.Include
{
    public enum ProductType
    {
        MainDish,
        SideDish,
        Drink,
        Appetizer
    }

    class Product(long placeid, string name, ProductType type, (float value, bool perGram) price)
    {
        private static readonly string dbConnectionString = "Data Source=OBED_DB.db";
        public long Place_id { get; private set; } = placeid;
        public string Name { get; init; } = name;
        public (float value, bool perGram) Price { get; private set; } = price;
        public ProductType Type { get; init; } = type;

        // TODO: SetPrice с процеркой на тип учётки

        //public static Product? Load()
        //{
        //    string dbConnectionString = "Data Source=OBED_DB.db";
        //    using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
        //    {
        //        connection.Open();
        //        SqliteCommand command = new SqliteCommand();
        //        command.Connection = connection;
        //    }
        //}

        public static bool Save(Product product)
        {
            using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    CreateTableProducts(command);
                    if (IfProductExists(product))
                    {
                        return false;
                    }
                    int pg = 0;
                    if (product.Price.perGram) { pg = 1; }

                    command.CommandText = $@"INSERT INTO Products(Place_id,Name,Value,perGram,Type) VALUES (@placeid,@name,@value,@pg,@type)";
                    command.Parameters.Add(new SqliteParameter("@placeid", product.Place_id));
                    command.Parameters.Add(new SqliteParameter("@name", product.Name));
                    command.Parameters.Add(new SqliteParameter("@value", product.Price.value));
                    command.Parameters.Add(new SqliteParameter("@pg", pg));
                    command.Parameters.Add(new SqliteParameter("@type", (int)product.Type));
                    int number = command.ExecuteNonQuery();
                    Console.WriteLine($"Добавлено элементов: {number}");
                    return true;
                }
            }
        }

        public static void CreateTableProducts(SqliteCommand command)
        {
			command.CommandText = $@"CREATE TABLE IF NOT EXISTS ""Products"" (
                                        	""Product_id""	INTEGER,
                                        	""Place_id""	INTEGER,
                                        	""Name""	TEXT NOT NULL DEFAULT 'Unkown',
                                        	""Value""	REAL NOT NULL DEFAULT -1.0,
                                        	""perGram""	INTEGER NOT NULL DEFAULT 0,
                                        	""Type""	INTEGER NOT NULL DEFAULT 0,
                                        	PRIMARY KEY(""Product_id"" AUTOINCREMENT),
                                        	FOREIGN KEY(""Place_id"") REFERENCES ""Places""(""Place_id"") ON UPDATE CASCADE
                                        );";
			command.ExecuteNonQuery();
		}

        public static List<Product> LoadAllProducts(long placeid)
        {
            List<Product> list = [];
            using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $@"SELECT * FROM Products WHERE Place_id = @placeid";
                    command.Parameters.Add(new SqliteParameter("@placeid", placeid));
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.GetString(2);
                            float value = reader.GetFloat(3);
                            bool perGram = reader.GetInt32(4) != 0;
                            ProductType type = (ProductType)reader.GetInt32(5);
                            list.Add(new Product(placeid, name, type, (value, perGram)));

                        }
                    }
                }
            }
            return list;
        }

        public static bool IfProductExists(Product product)
        {
            using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $@"SELECT 1 FROM Products WHERE Name = @name AND Place_id = @placeid";
                    command.Parameters.Add(new SqliteParameter("@name", product.Name));
                    command.Parameters.Add(new SqliteParameter("@placeid", product.Place_id));
                    bool t = command.ExecuteScalar() != null;
                    connection.Close();
                    return t;
                }
            }
        }
    }
}

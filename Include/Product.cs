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

    class Product(int placeid, string name, ProductType type, (float value, bool perGram) price)
    {
        public int Place_id { get; private set; } = placeid;
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
            string dbConnectionString = "Data Source=OBED_DB.db";
            using (SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = $@"CREATE TABLE IF NOT EXISTS ""Products"" (
                                        	""Product_id""	INTEGER,
                                        	""Place_id""	INTEGER,
                                        	""Name""	TEXT NOT NULL DEFAULT 'Unkown',
                                        	""Value""	INTEGER NOT NULL DEFAULT -1,
                                        	""perGram""	INTEGER NOT NULL DEFAULT 0,
                                        	""Type""	INTEGER NOT NULL DEFAULT 0,
                                        	PRIMARY KEY(""Product_id"" AUTOINCREMENT),
                                        	FOREIGN KEY(""Place_id"") REFERENCES ""Places""(""Place_id"") ON UPDATE CASCADE
                                        );";
                command.ExecuteNonQuery();
                if (IfProductExists(product))
                {
                    return false;
                }
                int pg = 0;
                if (product.Price.perGram) { pg = 1; }
                
                command.CommandText = $@"INSERT INTO Products(Place_id,Name,Value,perGram,Type) VALUES ({product.Place_id},{product.Name},{product.Price.value},{pg},{(int) product.Type})";
                int number = command.ExecuteNonQuery();
                Console.WriteLine($"Добавлено элементов: {number}");
                return true;
            }
        }

        public static List<Product> LoadAllProducts(int placeid)
        {
            string dbConnectionString = "Data Source=OBED_DB.db";
            List<Product> list = [];
            using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                var command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = $@"SELECT * FROM Products WHERE Place_id LIKE {placeid}";
                command.ExecuteNonQuery();
                using(SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while(reader.Read())
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
            string dbConnectionString = "Data Source=OBED_DB.db";
            using(SqliteConnection connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = $@"SELECT 1 FROM Products WHERE Name LIKE '{product.Name}' AND Place_id LIKE {product.Place_id}";
                return command.ExecuteScalar() != null;
            }
        }
    }
}

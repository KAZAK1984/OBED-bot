namespace OBED.Include
{
    class Grocery : BasePlace
    {
#pragma warning disable IDE0290 // Использовать основной конструктор
		public Grocery(string name, string? description, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
#pragma warning restore IDE0290 // Использовать основной конструктор
		{
            //ObjectLists.Groceries.Add(this);
        }
    }
}

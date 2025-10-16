namespace OBED.Include
{
    class Grocery : BasePlace
    {
        public Grocery(string name, string? description, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
        {
            ObjectLists.Groceries.Add(this);
        }
    }
}

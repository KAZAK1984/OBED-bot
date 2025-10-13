namespace OBED.Include
{
    class Grocery : BasePlace
    {
        public static List<Grocery> All { get; private set; } = [];

        public Grocery(string name, string? description, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
        {
            All.Add(this);
        }
    }
}

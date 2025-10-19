namespace OBED.Include
{
    class Grocery(string name, string? description, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : BasePlace(name, description, reviews, menu, tegs)
    {
	}
}

namespace OBED.Include
{
    class Grocery(long placeid,string name, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : BasePlace(placeid,name, description, reviews, menu, tegs)
    {
	}
}

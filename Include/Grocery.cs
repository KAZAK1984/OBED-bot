namespace OBED.Include
{
    class Grocery : BasePlace
    {
        /// <summary>
        /// Initializes a new Grocery with the specified values and registers it in ObjectLists.Groceries.
        /// </summary>
        /// <param name="name">The display name of the grocery.</param>
        /// <param name="description">An optional description of the grocery.</param>
        /// <param name="reviews">An optional list of reviews for the grocery.</param>
        /// <param name="menu">An optional list of products available at the grocery.</param>
        /// <param name="tegs">An optional list of tags associated with the grocery.</param>
        public Grocery(string name, string? description, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
        {
            ObjectLists.Groceries.Add(this);
        }
    }
}
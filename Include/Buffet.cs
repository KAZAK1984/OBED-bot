namespace OBED.Include
{
    class Buffet : BasePlace, ILocatedUni
    {
        public int BuildingNumber { get; private set; }
        public int Floor { get; private set; }
        
        /// <summary>
        /// Initializes a new Buffet with the given identity and optional cataloging information.
        /// </summary>
        /// <param name="name">The display name of the buffet.</param>
        /// <param name="buildingNumber">The building number where the buffet is located.</param>
        /// <param name="floor">The floor number where the buffet is located.</param>
        /// <param name="description">An optional human-readable description of the buffet.</param>
        /// <param name="reviews">An optional list of initial reviews for the buffet.</param>
        /// <param name="menu">An optional list of products offered by the buffet.</param>
        /// <param name="tegs">An optional list of tags associated with the buffet.</param>
        /// <remarks>The new instance is added to ObjectLists.Buffets.</remarks>
        public Buffet(string name, int buildingNumber, int floor, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null) : base(name, description, reviews, menu, tegs)
        {
            BuildingNumber = buildingNumber;
            Floor = floor;

            ObjectLists.Buffets.Add(this);
        }
    }
}
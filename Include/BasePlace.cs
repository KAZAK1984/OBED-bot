namespace OBED.Include
{
    public enum ReviewSort
    {
        Upper,
        Lower,
        OldDate,
        NewDate
    }

    class Review(long userID, int rating, string? comment = null)
    {
        public long UserID { get; init; } = userID;
        public int Rating { get; private set; } = rating;
        public string? Comment { get; private set; } = comment;
        public DateTime Date { get; private set; } = DateTime.Now;

        // TODO: ChangeReview()
    }

    abstract class BasePlace(string name, string? description = null, List<Review>? reviews = null, List<Product>? menu = null, List<string>? tegs = null)
    {
        public string Name { get; private set; } = name;
        public string? Description { get; private set; } = description;

        public List<Review> Reviews { get; private set; } = reviews ?? [];
        public List<Product> Menu { get; private set; } = menu ?? [];
        public List<string> Tegs { get; private set; } = tegs ?? []; // TODO: Возможное изменение типа на enum
        // TODO: public List<T> photos []

        // TODO: Загрузка с бд/файла
        //abstract public void Load(string file);
        //abstract public void Save(string file);
        public virtual bool AddReview(Review review)
        {
            if (!Reviews.Where(x => x.UserID == review.UserID).Any())
            {
                Reviews.Add(review);
                return true;
            }
            return false;
        }
        public virtual bool AddReview(long userID, int rating, string? comment)
        {
            if (!Reviews.Where(x => x.UserID == userID).Any())
            {
                Reviews.Add(new Review(userID, rating, comment));
                return true;
            }
            return false;
        }
        public virtual bool DeleteReview(long userID)
        {
            var removeCheck = Reviews.Where(x => x.UserID == userID);
            if (removeCheck.Any())
            {
                Reviews.Remove(removeCheck.First());
                return true;
            }
            return false;
        }
        public virtual Review? GetReview(long userID) => Reviews.Where(x => x.UserID == userID).FirstOrDefault();
    }
}

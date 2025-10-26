namespace OBED.Include
{
    class Report(long userID, string comment, List<string> tegs, List<object>? screenshots = null)
    {
        public long UserID { get; init; } = userID;
        public string Comment { get; init; } = comment;
        public List<string> Tegs { get; init; } = tegs; // for bugs / outdated info / violation type -> enum?
        public List<object> Screenshots { get; init; } = screenshots ?? [];
        public DateTime Date { get; init; } = DateTime.Now;
    }

    class Complaint(long userID, string comment, List<string> tegs, Review review, List<object>? screenshots = null) : Report(userID, comment, tegs, screenshots)
    {
        public Review Review { get; init; } = review;
    }
}

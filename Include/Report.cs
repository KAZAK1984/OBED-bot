namespace OBED.Include
{
    class Report(long userID, string comment, List<string> tegs)
    {
        public long UserID { get; init; } = userID;
        public string Comment { get; init; } = comment;
        public List<string> Tegs { get; init; } = tegs; // for bugs / outdated info / violation type -> enum?
        public DateTime Date { get; init; } = DateTime.Now;
    }

    class BugReport(long userID, string comment, List<string> tegs, List<object>? screenshots) : Report(userID, comment, tegs)
    {
        public List<object> Screenshots { get; init; } = screenshots ?? [];
    }

    class Complaint(long userID, string comment, List<string> tegs, Review review) : Report(userID, comment, tegs)
    {
        public Review Review { get; init; } = review;
    }
}

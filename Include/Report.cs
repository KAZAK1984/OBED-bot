namespace OBED.Include
{
    class FeedbackReport(long userID, string comment, List<string> tegs, string[]? screenshots = null)
    {
        public long UserID { get; init; } = userID;
        public string Comment { get; init; } = comment;
        public string? Answer { get; init; } = null;
        public List<string> Tegs { get; init; } = tegs; // for bugs / outdated info / violation type -> enum?
        public string[] Screenshots { get; init; } = screenshots ?? [];
        public DateTime Date { get; init; } = DateTime.Now;
    }

    class ComplaintReport(long userID, string comment, List<string> tegs, long subjectID, string[]? screenshots = null) : FeedbackReport(userID, comment, tegs, screenshots)
    {
        public long SubjectID { get; init; } = subjectID;
    }
}

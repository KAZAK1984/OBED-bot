namespace OBED.Include
{
    enum ReportTeg // Будет пополнятся новыми тегами
    {
        Bug,
        OutdatedInfo,
        WrongInfo,
        Suggestion
    }
    
    class FeedbackReport(long userID, string comment, List<ReportTeg> tegs, string[]? screenshots = null)
    {
        public long UserID { get; init; } = userID;
        public string Comment { get; private set; } = comment;
        public string? Answer { get; private set; } = null;
        public List<ReportTeg> Tegs { get; private set; } = tegs;
        public string[] Screenshots { get; private set; } = screenshots ?? [];
        public DateTime Date { get; private set; } = DateTime.Now;
    }

    class ComplaintReport(long userID, string comment, List<ReportTeg> tegs, long subjectID, string[]? screenshots = null) : FeedbackReport(userID, comment, tegs, screenshots)
    {
        public long SubjectID { get; init; } = subjectID;
    }
}

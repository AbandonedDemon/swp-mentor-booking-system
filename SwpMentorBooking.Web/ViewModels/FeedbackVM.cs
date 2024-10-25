using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class FeedbackVM
{
    public int BookingId { get; set; }
    public int GivenBy { get; set; }
    public int GivenTo { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsMentorFeedback { get; set; } = false;
    [ValidateNever]
    public string GroupName { get; set; }
    [ValidateNever]
    public string MentorName { get; set; }
}

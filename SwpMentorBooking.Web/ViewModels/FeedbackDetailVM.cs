using SwpMentorBooking.Domain.Entities;

namespace SwpMentorBooking.Web.ViewModels
{
    public class FeedbackDetailVM
    {
        public int BookingId { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime Date { get; set; }
        public bool IsGivenByCurrentUser { get; set; }
    }
}

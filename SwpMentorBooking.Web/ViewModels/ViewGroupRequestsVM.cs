using SwpMentorBooking.Domain.Entities;

namespace SwpMentorBooking.Web.ViewModels
{
    public class ViewGroupRequestsVM
    {
        public List<Request> Requests { get; set; } = new List<Request>();

        public bool IsLeader { get; set; } = false;
    }
}

using SwpMentorBooking.Domain.Entities;

namespace SwpMentorBooking.Web.ViewModels
{
    public class SearchMentorVM
    {
        public string? SearchTemp { get; set; }

        public List<Skill> SearchSkill { get; set; }

        public string? Skill { get; set; }

        public int? minBookingScore { get; set; }

        public int? maxBookingScore { get; set; }

        public List<MentorDetailVM> SearchResult { get; set; }
    }
}

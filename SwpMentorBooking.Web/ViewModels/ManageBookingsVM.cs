using Microsoft.AspNetCore.Mvc.Rendering;
using SwpMentorBooking.Web.ViewModels;

public class ManageBookingsVM
{
    public List<BookingDetailVM> Bookings { get; set; }
    public IEnumerable<SelectListItem> Groups { get; set; }
    public IEnumerable<SelectListItem> Mentors { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string Status { get; set; }
    public int? SelectedGroupId { get; set; }
    public int? SelectedMentorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
} 
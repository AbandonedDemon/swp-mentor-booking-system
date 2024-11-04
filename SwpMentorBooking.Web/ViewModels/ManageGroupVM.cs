using SwpMentorBooking.Web.ViewModels;

public class ManageGroupVM
{
    public List<ManageGroupDetailVM> Groups { get; set; }
}

public class ManageGroupDetailVM
{
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string TopicName { get; set; }
    public int WalletBalance { get; set; }
    public List<StudentDetailVM> Members { get; set; } = new List<StudentDetailVM>();
}

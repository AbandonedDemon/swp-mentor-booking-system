using SwpMentorBooking.Domain.Entities;

namespace SwpMentorBooking.Web.ViewModels
{
    public class WalletTransactionsVM
    {
        public int WalletBalance { get; set; }
        public List<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
} 
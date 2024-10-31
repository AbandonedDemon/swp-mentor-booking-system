using SwpMentorBooking.Domain.Entities;

namespace SwpMentorBooking.Application.Common.Interfaces
{
    public interface IWalletTransactionRepository : IRepository<WalletTransaction>
    {
        WalletTransaction Update(WalletTransaction entity);
    }
} 
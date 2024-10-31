using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Infrastructure.Data;

namespace SwpMentorBooking.Infrastructure.Repository
{
    public class WalletTransactionRepository : Repository<WalletTransaction>, IWalletTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public WalletTransactionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public WalletTransaction Update(WalletTransaction entity)
        {
            _context.Update(entity);
            return entity;
        }
    }
} 
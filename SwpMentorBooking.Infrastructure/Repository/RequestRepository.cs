using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Infrastructure.Data;

namespace SwpMentorBooking.Infrastructure.Repository
{
    public class RequestRepository : Repository<Request>, IRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public RequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Request Update(Request entity) 
        {
            _context.Update(entity);
            return entity;
        }
    }
}

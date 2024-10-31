using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwpMentorBooking.Infrastructure.Repository
{
    public class MentorRepository : Repository<MentorDetail>, IMentorRepository
    {
        private readonly ApplicationDbContext _context;

        public MentorRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public MentorDetail Update(MentorDetail entity)
        {
            _context.Update(entity);
            return entity;
        }
    }
}

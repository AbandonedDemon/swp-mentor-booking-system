using SwpMentorBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwpMentorBooking.Application.Common.Interfaces
{
    public interface IRequestRepository : IRepository<Request>
    {
        Request Update(Request entity);
    }
}

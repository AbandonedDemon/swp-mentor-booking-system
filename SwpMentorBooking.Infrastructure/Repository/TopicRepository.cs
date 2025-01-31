﻿using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwpMentorBooking.Infrastructure.Repository
{
    public class TopicRepository : Repository<Topic>, ITopicRepository
    {
        private readonly ApplicationDbContext _context;
        public TopicRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Topic Update(Topic entity)
        {
            _context.Update(entity);
            return entity;
        }
    }
}

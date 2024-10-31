using Microsoft.EntityFrameworkCore.Storage;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Infrastructure.Data;
using SwpMentorBooking.Infrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwpMentorBooking.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public IUserRepository User { get; private set; }
        public IAdminRepository Admin  { get; private set; }
        public IStudentRepository Student { get; private set; }
        public IMentorRepository Mentor { get; private set; }
        public IMentorScheduleRepository MentorSchedule { get; private set; }
        public IStudentGroupRepository StudentGroup { get; private set; }
        public ISkillRepository Skill { get; private set; }
        public ISlotRepository Slot { get; private set; }
        public ISpecializationRepository Specialization { get; private set; }
        public ITopicRepository Topic { get; private set; }
        public IWalletRepository Wallet { get; private set; }
        public IBookingRepository Booking { get; private set; }
        public IFeedbackRepository Feedback { get; private set; }
        public IRequestRepository Request {  get; private set; }
        public IResponseRepository Response {  get; private set; }
        public IWalletTransactionRepository WalletTransaction { get; private set; }
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            User = new UserRepository(_context);
            Admin = new AdminRepository(_context);
            Student = new StudentRepository(_context);
            Mentor = new MentorRepository(_context);
            MentorSchedule = new MentorScheduleRepository(_context);
            StudentGroup = new StudentGroupRepository(_context);
            Skill = new SkillRepository(_context);
            Slot = new SlotRepository(_context);
            Specialization = new SpecializationRepository(_context);
            Topic = new TopicRepository(_context);
            Wallet = new WalletRepository(_context);
            Booking = new BookingRepository(_context);
            Feedback = new FeedbackRepository(_context);
            Request = new RequestRepository(_context);
            Response = new ResponseRepository(_context);
            WalletTransaction = new WalletTransactionRepository(_context);
        }


        public IDbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}

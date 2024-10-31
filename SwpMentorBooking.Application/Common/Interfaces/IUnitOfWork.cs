using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwpMentorBooking.Application.Common.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository User { get; }
        IAdminRepository Admin {  get; }
        IStudentRepository Student { get; }
        IMentorRepository Mentor { get; }
        IMentorScheduleRepository MentorSchedule { get; }
        IStudentGroupRepository StudentGroup { get; }
        ISkillRepository Skill { get; }
        ISlotRepository Slot { get; }
        ISpecializationRepository Specialization { get; }
        ITopicRepository Topic { get; }
        IWalletRepository Wallet { get; }
        IBookingRepository Booking {  get; }
        IFeedbackRepository Feedback { get; }
        IRequestRepository Request {  get; }
        IResponseRepository Response {  get; }
        IWalletTransactionRepository WalletTransaction { get; }
        IDbContextTransaction BeginTransaction();
        void Save();

    }
}

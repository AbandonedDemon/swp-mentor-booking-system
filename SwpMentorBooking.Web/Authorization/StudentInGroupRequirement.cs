using Microsoft.AspNetCore.Authorization;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using System.Security.Claims;

namespace SwpMentorBooking.Web.Authorization
{
    public class StudentInGroupRequirement : IAuthorizationRequirement { }

    public class StudentInGroupHandler : AuthorizationHandler<StudentInGroupRequirement>
    {
        private readonly IUnitOfWork _unitOfWork;

        public StudentInGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, StudentInGroupRequirement requirement)
        {
            var userEmail = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(StudentDetail));

            if (user != null && user.StudentDetail != null && user.StudentDetail.GroupId != null)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
} 
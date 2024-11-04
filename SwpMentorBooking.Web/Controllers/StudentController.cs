using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Application.Common.Utilities;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Infrastructure.Utils;
using SwpMentorBooking.Web.ViewModels;
using System.Linq;
using System.Security.Claims;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize]
    [Route("student")]
    public class StudentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilService _utilService;
        public StudentController(IUnitOfWork unitOfWork, IUtilService utilService)
        {
            _unitOfWork = unitOfWork;
            _utilService = utilService;
        }

        [HttpGet("home")]
        public IActionResult Index()
        {
            var mentorList = _unitOfWork.User.GetAll(filter: u => u.MentorDetail.UserId == u.Id,
                                                    includeProperties: nameof(MentorDetail)).OrderByDescending(u => u.MentorDetail.BookingScore);
            ManageUserVM manageUserVM = new ManageUserVM
            {
                Mentors = mentorList,
            };
            return View(manageUserVM);
        }

        [HttpGet("profile")]
        public IActionResult MyProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(StudentDetail));

            if (user is null)
            {
                return NotFound();
            }
            // Ensure the user is only accessing their own profile
            if (user.Role != "Student")
            {
                return Forbid();
            }

            var studentProfileVM = new StudentDetailVM
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Gender = user.Gender,
                StudentCode = user.StudentDetail?.StudentCode
            };

            return View(studentProfileVM);
        }

        [HttpGet("update")]
        public IActionResult UpdateProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(StudentDetail));
            var studentProfileVM = new StudentDetailVM
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Gender = user.Gender,
                StudentCode = user.StudentDetail.StudentCode
            };
            return View(studentProfileVM);
        }

        [HttpPost("update")]
        public IActionResult UpdateProfile(StudentDetailVM updatedModel)
        {
            if (ModelState.IsValid)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = _unitOfWork.User.Get(u => u.Email == userEmail);
                if (user != null)
                {
                    user.Phone = updatedModel.Phone;
                    _unitOfWork.User.Update(user);
                    _unitOfWork.Save();

                }
                TempData["ValidateMessage"] = "Update successful!";
            }
            return RedirectToAction("MyProfile");
        }

        [HttpGet("group")]
        public IActionResult MyGroup()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(StudentDetail));

            if (user is null)
            {
                return NotFound();
            }

            StudentDetail studentDetail = user.StudentDetail;
            // Get the Student group info
            StudentGroup studentGroup = _unitOfWork.StudentGroup.Get(g => g.Id == studentDetail.GroupId,
                        includeProperties: $"{nameof(Topic)},{nameof(Wallet)},StudentDetails.User");

            List<StudentDetail>? groupMembers = new List<StudentDetail>();
            // Handle the case where the student does not belong to any group
            if (studentGroup is not null)
            {
                groupMembers = studentGroup.StudentDetails?.ToList();
            }

            StudentGroupDetailVM studentGroupDetailVM = new StudentGroupDetailVM
            {
                Student = studentDetail,
                StudentGroup = studentGroup,
                GroupMembers = groupMembers
            };

            return View(studentGroupDetailVM);
        }

        [HttpGet("mentors")]
        public IActionResult ViewMentors()
        {
            IEnumerable<MentorDetail> mentorList = _unitOfWork.Mentor.GetAll(includeProperties: nameof(User));
            return View(mentorList);
        }

        [HttpGet("mentorProfile/{mentorId}")]
        public IActionResult ViewMentorProfile(int mentorId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = _unitOfWork.Student.Get(s => s.User.Email == userEmail, includeProperties: nameof(User));

            if (student is null)
            {
                return NotFound();
            }

            MentorDetail mentor = _unitOfWork.Mentor.Get(m => m.UserId == mentorId, includeProperties: nameof(User));
            if (mentor is null)
            {
                return NotFound();
            }

            var mentorProfileVM = new MentorProfileVM
            {
                UserId = mentor.UserId,
                FullName = mentor.User.FullName,
                Gender = mentor.User.Gender,
                MainProgrammingLanguage = _utilService.StringManipulation.SplitProperty(mentor.MainProgrammingLanguage, "||"),
                AltProgrammingLanguage = _utilService.StringManipulation.SplitProperty(mentor.AltProgrammingLanguage, "||"),
                Framework = _utilService.StringManipulation.SplitProperty(mentor.Framework, "||"),
                Education = _utilService.StringManipulation.SplitProperty(mentor.Education, "||"),
                BookingScore = mentor.BookingScore ?? 0,
                Description = mentor.Description,

                IsStudentGroupLeader = student.IsLeader,
            };

            return View(mentorProfileVM);
        }

        [HttpGet("mentorProfile/{mentorId}/schedule")]
        [HttpGet("mentorProfile/{mentorId}/schedule/{startDate:datetime}")]
        [AllowAnonymous]
        public IActionResult ViewMentorSchedule(int mentorId, DateTime? startDate)
        {
            MentorDetail mentor = _unitOfWork.Mentor.Get(m => m.UserId == mentorId,
                                                includeProperties: nameof(User));
            if (mentor is null)
            {
                return NotFound();
            }

            DateTime now = startDate ?? DateTime.Now;
            DateOnly currentMonday = DateOnly.FromDateTime(now.AddDays(-(int)now.DayOfWeek + 1));
            IEnumerable<Slot> slots = _unitOfWork.Slot.GetAll();
            IEnumerable<MentorSchedule> mentorSchedules = _unitOfWork.MentorSchedule
                .GetAll(ms => ms.MentorDetailId == mentor.UserId && ms.Status != "unavailable", includeProperties: nameof(Slot))
                .OrderBy(ms => ms.Date);

            var mentorScheduleVM = mentorSchedules.Select(s => new MentorScheduleVM
            {
                Id = s.Id,
                MentorDetailId = s.MentorDetailId,
                SlotId = s.SlotId,
                Date = s.Date.ToDateTime(TimeOnly.MinValue),
                Status = s.Status,
                SlotStartTime = s.Slot.StartTime.ToString(@"HH\:mm"),
                SlotEndTime = s.Slot.EndTime.ToString(@"HH\:mm")
            }).ToList();

            var mentorScheduleWeekVM = new MentorScheduleWeekVM
            {
                MentorId = mentor.UserId,
                MentorFullName = mentor.User.FullName,
                WeekStartDate = currentMonday.ToDateTime(TimeOnly.MinValue),
                Slots = slots.ToList(),
                DailySchedules = Enumerable.Range(0, 7).Select(i => new DailySchedule
                {
                    Date = currentMonday.AddDays(i).ToDateTime(TimeOnly.MinValue),
                    MentorSchedules = mentorScheduleVM
                        .Where(s => s.Date.Date == currentMonday.AddDays(i).ToDateTime(TimeOnly.MinValue).Date)
                        .ToList(),
                    IsPastDay = currentMonday.AddDays(i) < DateOnly.FromDateTime(DateTime.Now)

                }).ToList()
            };

            return View(mentorScheduleWeekVM);
        }

    }
}

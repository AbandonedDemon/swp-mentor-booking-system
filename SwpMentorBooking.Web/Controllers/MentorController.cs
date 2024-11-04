using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Web.ViewModels;
using System.Security.Claims;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize(Roles = "Mentor")]
    [Route("mentor")]
    public class MentorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilService _utilService;
        public MentorController(IUnitOfWork unitOfWork, IUtilService utilService)
        {
            _unitOfWork = unitOfWork;
            _utilService = utilService;
        }

        [HttpGet("home")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("profile")]
        public IActionResult MyProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(MentorDetail));

            if (user is null)
            {
                return NotFound();
            }
            // At this point, the user does exist and it's our job to display the data correctly.

            var mentorProfileVM = new MentorDetailVM
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Gender = user.Gender,
                // Split the properties by the delimiter
                MainProgrammingLanguage = _utilService.StringManipulation.SplitProperty(user.MentorDetail?.MainProgrammingLanguage, "||"),
                AltProgrammingLanguage = _utilService.StringManipulation.SplitProperty(user.MentorDetail?.AltProgrammingLanguage, "||"),
                Framework = _utilService.StringManipulation.SplitProperty(user.MentorDetail?.Framework, "||"),
                Education = _utilService.StringManipulation.SplitProperty(user.MentorDetail?.Education, "||"),
                AdditionalContactInfo = _utilService.StringManipulation.SplitProperty(user.MentorDetail?.AdditionalContactInfo, "||"),
                BookingScore = user.MentorDetail?.BookingScore,
                Description = user.MentorDetail?.Description,
            };

            return View(mentorProfileVM);
        }

        [HttpGet("profile/update")]
        public IActionResult UpdateProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var mentor = _unitOfWork.Mentor.Get(u => u.User.Email == userEmail, includeProperties: $"{nameof(User)},Skills,Specs");

            if (mentor is null)
            {
                return NotFound();
            }
            // Ensure the user is only accessing their own profile
            if (mentor.User.Role != "Mentor")
            {
                return Forbid();
            }

            var mentorDetailVM = new MentorDetailVM
            {
                UserId = mentor.UserId,
                Email = mentor.User.Email,
                FullName = mentor.User.FullName,
                Phone = mentor.User.Phone,
                Gender = mentor.User.Gender,
                // Split the properties by the delimiter
                MainProgrammingLanguage = _utilService.StringManipulation.SplitProperty(mentor?.MainProgrammingLanguage, ","),
                AltProgrammingLanguage = _utilService.StringManipulation.SplitProperty(mentor?.AltProgrammingLanguage, ","),
                Framework = _utilService.StringManipulation.SplitProperty(mentor?.Framework, "||"),
                Education = _utilService.StringManipulation.SplitProperty(mentor?.Education, ","),
                AdditionalContactInfo = _utilService.StringManipulation.SplitProperty(mentor?.AdditionalContactInfo, ","),
                BookingScore = mentor?.BookingScore,
                Description = mentor?.Description,
                Skills = mentor.Skills.ToList(),
                Specializations = mentor.Specs.ToList(),
            };
            return View(mentorDetailVM);
        }

        [HttpPost("profile/update")]
        public IActionResult UpdateProfile(MentorDetailVM updatedModel)
        {
            if (ModelState.IsValid)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(MentorDetail));
                if (user != null)
                {
                    user.MentorDetail.AdditionalContactInfo = string.Join(", ", updatedModel.AdditionalContactInfo);
                    user.Gender = updatedModel.Gender;
                    user.Phone = updatedModel.Phone;
                    _unitOfWork.Save();

                }
                TempData["ValidateMessage"] = "Update successful!";
            }
            return RedirectToAction("MyProfile");
        }

        [HttpGet("schedule")]
        public IActionResult ViewSchedule(DateTime? startDate)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var mentor = _unitOfWork.Mentor.Get(m => m.User.Email == userEmail, includeProperties: nameof(User));

            if (mentor is null)
            {
                return NotFound();
            }


            DateTime now = startDate ?? DateTime.Now;
            DateOnly currentMonday = DateOnly.FromDateTime(now.AddDays(-(int)now.DayOfWeek + 1)); // Ensure Monday is first day
            IEnumerable<Slot> slots = _unitOfWork.Slot.GetAll();
            // Get the list of mentor schedule
            IEnumerable<MentorSchedule> mentorSchedules = _unitOfWork.MentorSchedule
                .GetAll(ms => ms.MentorDetailId == mentor.UserId && ms.Status != "unavailable", includeProperties: nameof(Slot))
                .OrderBy(ms => ms.Date);
            //var schedules = _unitOfWork.Mentor.MentorSchedules
            //    .Include(ms => ms.Slot)
            //    .Where(ms => ms.MentorDetailId == mentorId && ms.Status != "unavailable")
            //    .OrderBy(ms => ms.Date)
            //    .ToList();

            // Map from MentorSchedule to MentorScheduleVM
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

            // Populate the mentorScheduleWeekVM for weekly display
            var mentorScheduleWeekVM = new MentorScheduleWeekVM
            {
                MentorFullName = mentor.User.FullName,
                WeekStartDate = currentMonday.ToDateTime(TimeOnly.MinValue),
                Slots = slots.ToList(),
                DailySchedules = Enumerable.Range(0, 7).Select(i => new DailySchedule
                {
                    Date = currentMonday.AddDays(i).ToDateTime(TimeOnly.MinValue),
                    MentorSchedules = mentorScheduleVM
                        .Where(s => s.Date.Date == currentMonday.AddDays(i).ToDateTime(TimeOnly.MinValue).Date)
                        .ToList()
                }).ToList()
            };

            return View(mentorScheduleWeekVM);
        }

        [HttpGet("schedule/details/{scheduleId}")]
        public IActionResult ViewSchedulePreview(int scheduleId)
        {
            var mentorSchedule = _unitOfWork.MentorSchedule.Get(ms => ms.Id == scheduleId,
                includeProperties: "Slot,MentorDetail.User");

            if (mentorSchedule == null)
            {
                return NotFound();
            }

            Booking? booking = _unitOfWork.Booking.Get(b => b.MentorScheduleId == scheduleId,
                               includeProperties: "Leader.Group.Topic");

            var bookingDetailVM = new BookingDetailVM
            {
                ScheduleDate = mentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotStartTime = mentorSchedule.Slot.StartTime,
                SlotEndTime = mentorSchedule.Slot.EndTime,
                Status = mentorSchedule.Status,
                GroupName = booking?.Leader?.Group?.GroupName,
                TopicName = booking?.Leader.Group?.Topic?.Name,
                Note = booking?.Note
            };

            return PartialView("_SchedulePreview", bookingDetailVM);
        }

        // Action for setting the schedule (all slots are shown, including unavailable)
        [HttpGet("schedule/edit")]
        public IActionResult SetSchedule(DateTime? startDate)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var mentor = _unitOfWork.Mentor.Get(m => m.User.Email == userEmail, includeProperties: nameof(User));

            if (mentor is null)
            {
                return NotFound();
            }
            // Get current date of click
            DateTime now = startDate ?? DateTime.Now;
            DateOnly currentMonday = DateOnly.FromDateTime(now.AddDays(-(int)now.DayOfWeek + 1)); // Ensure Monday is first day
            IEnumerable<Slot> slots = _unitOfWork.Slot.GetAll();
            // Get a list of the mentor's schedule
            IEnumerable<MentorSchedule> mentorSchedules = _unitOfWork.MentorSchedule
                .GetAll(ms => ms.MentorDetailId == mentor.UserId, includeProperties: nameof(Slot))
                .OrderBy(ms => ms.Date);

            // Populate the mentorScheduleVM
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

            // Populate the mentorScheduleWeekVM (for weekly display)
            var mentorScheduleWeekVM = new MentorScheduleWeekVM
            {
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

        [HttpPost("schedule/toggle-availability")]
        public IActionResult ToggleSlotAvailability(int mentorScheduleId, int slotId, DateOnly date)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var mentor = _unitOfWork.Mentor.Get(m => m.User.Email == userEmail,
                                                includeProperties: (nameof(User)));

            if (mentor == null)
            {
                return NotFound();
            }
            // Get currently selected mentor schedule
            var mentorSchedule = _unitOfWork.MentorSchedule.Get(ms => ms.Id == mentorScheduleId);

            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    if (mentorSchedule is null) // Schedule slot is not created yet
                    {
                        // Create a new MentorSchedule if it doesn't exist
                        mentorSchedule = new MentorSchedule
                        {
                            MentorDetailId = mentor.UserId,
                            SlotId = slotId,
                            Date = date,
                            Status = "available"
                        };
                        _unitOfWork.MentorSchedule.Add(mentorSchedule);
                    }
                    else
                    {
                        // Toggle the status
                        mentorSchedule.Status = mentorSchedule.Status == "available" ? "unavailable" : "available";
                        _unitOfWork.MentorSchedule.Update(mentorSchedule);
                    }
                    _unitOfWork.Save();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["error"] = "An error has occurred. Please try again.";
                    return RedirectToAction(nameof(SetSchedule), new { startDate = date.ToDateTime(TimeOnly.MinValue) });
                }
            }
            // Toggle action is successful
            TempData["success"] = "Schedule updated successfully.";
            return RedirectToAction(nameof(SetSchedule), new { startDate = date.ToDateTime(TimeOnly.MinValue) });
        }

    }
}

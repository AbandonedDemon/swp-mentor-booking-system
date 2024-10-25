using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Web.ViewModels;
using System.Security.Claims;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize(Roles = "Student, Mentor")]
    [Route("feedback")]
    public class FeedbackController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public FeedbackController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("send-student/{bookingId}")]
        public IActionResult SendFeedbackStudent(int bookingId)
        {
            // Retrieve current user's info
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User user = _unitOfWork.User.Get(u => u.Email == userEmail,
                        includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");

            if (user is null)
            {
                return NotFound();
            }
            // Retrieve current booking info
            Booking booking = _unitOfWork.Booking.Get(b => b.Id == bookingId && b.Status == "completed",
                includeProperties: "MentorSchedule.MentorDetail.User,Leader.User,Leader.Group");

            if (booking is null)
            {
                return NotFound();
            }

            // Check if the user has already given feedback for this booking
            var existingFeedback = _unitOfWork.Feedback.Get(f => f.BookingId == bookingId && f.GivenBy == user.Id);
            if (existingFeedback is not null)
            {
                TempData["error"] = "You have already submitted feedback for this booking.";
                return RedirectToAction("ViewStudentBookings", "Booking");
            }

            // Populate the View model for display
            var feedbackVM = new FeedbackVM
            {
                BookingId = bookingId,
                GivenBy = user.Id,
                GivenTo = booking.MentorSchedule.MentorDetail.UserId,
                GroupName = booking.Leader.Group.GroupName,
                MentorName = booking.MentorSchedule.MentorDetail.User.FullName,
                IsMentorFeedback = user.MentorDetail is not null,
            };

            return View(nameof(SendFeedback), feedbackVM);
        }

        [HttpGet("send-mentor/{bookingId}")]
        public IActionResult SendFeedbackMentor(int bookingId)
        {
            // Retrieve current user's info
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User user = _unitOfWork.User.Get(u => u.Email == userEmail,
                        includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");

            if (user is null)
            {
                return NotFound();
            }
            // Retrieve current booking info
            Booking booking = _unitOfWork.Booking.Get(b => b.Id == bookingId && b.Status == "completed",
                includeProperties: "MentorSchedule.MentorDetail.User,Leader.User,Leader.Group");

            if (booking is null)
            {
                return NotFound();
            }

            // Check if the user is the mentor for this booking
            if (booking.MentorSchedule.MentorDetail.UserId != user.Id)
            {
                return Forbid();
            }

            // Check if the user has already given feedback for this booking
            var existingFeedback = _unitOfWork.Feedback.Get(f => f.BookingId == bookingId && f.GivenBy == user.Id);
            if (existingFeedback is not null)
            {
                TempData["error"] = "You have already submitted feedback for this booking.";
                return RedirectToAction("ViewMentorBookings", "Booking");
            }

            // Populate the View model for display
            var feedbackVM = new FeedbackVM
            {
                BookingId = bookingId,
                GivenBy = user.Id,
                GivenTo = booking.LeaderId,
                GroupName = booking.Leader.Group.GroupName,
                MentorName = booking.MentorSchedule.MentorDetail.User.FullName,
                IsMentorFeedback = user.MentorDetail is not null,

            };

            return View(nameof(SendFeedback), feedbackVM);
        }

        [HttpPost("send")]
        public IActionResult SendFeedback(FeedbackVM feedbackVM)
        {
            if (!ModelState.IsValid)
            {
                return View(feedbackVM);
            }

            var feedback = new Feedback
            {
                BookingId = feedbackVM.BookingId,
                GivenBy = feedbackVM.GivenBy,
                GivenTo = feedbackVM.GivenTo,
                Rating = feedbackVM.Rating,
                Comment = feedbackVM.Comment,
                Date = DateTime.Now
            };
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    _unitOfWork.Feedback.Add(feedback);
                    _unitOfWork.Save();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    TempData["error"] = "An error occurred while sending feedback. Please try again.";
                    return View();
                }
            }
            TempData["success"] = "Feedback submitted successfully!";
            return RedirectToAction("ViewBookingDetail", nameof(Booking), new { bookingId = feedbackVM.BookingId });
        }

        [HttpGet("view")]
        public IActionResult ViewFeedbacks()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User user = _unitOfWork.User.Get(u => u.Email == userEmail,
                        includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");

            var feedbacks = _unitOfWork.Feedback.GetAll(f => f.GivenTo == user.Id || f.GivenBy == user.Id,
                includeProperties: "Booking.MentorSchedule.MentorDetail.User," +
                                   "Booking.Leader.User,Booking," +
                                   "GivenByNavigation," +
                                   "GivenToNavigation");

            var feedbackDetailList = feedbacks.Select(f => new FeedbackDetailVM
            {
                BookingId = f.BookingId,
                SenderName = f.GivenByNavigation.FullName,
                ReceiverName = f.GivenToNavigation.FullName,
                Rating = f.Rating,
                Comment = f.Comment,
                Date = f.Date,
                IsGivenByCurrentUser = f.GivenBy == user.Id
            }).ToList();

            return View(feedbackDetailList);
        }
    }
}

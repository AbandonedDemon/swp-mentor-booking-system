using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Application.Common.Utilities;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Web.ViewModels;
using SwpMentorBooking.Web.Helpers;
using System.Security.Claims;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize]
    [Route("booking")]
    public class BookingController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private const int _bookingCost = 10;
        public BookingController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // -- Booking functionalities for Student's group leader -- //

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpGet("{mentorId}/schedule")]
        public IActionResult BookSchedule(int mentorId, DateTime? startDate)
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
            // Get the selected mentor's schedules
            IEnumerable<MentorSchedule> mentorSchedules = _unitOfWork.MentorSchedule
                .GetAll(ms => ms.MentorDetailId == mentor.UserId && ms.Status != Constants.MentorScheduleStatus.Unavailable, includeProperties: nameof(Slot))
                .OrderBy(ms => ms.Date);

            var mentorScheduleVM = mentorSchedules.Select(s => new MentorScheduleVM
            {
                Id = s.Id,
                MentorDetailId = s.MentorDetailId,
                SlotId = s.SlotId,
                Date = s.Date.ToDateTime(TimeOnly.MinValue),
                Status = s.Status,
                SlotStartTime = s.Slot.StartTime.ToString(@"HH\:mm"),
                SlotEndTime = s.Slot.EndTime.ToString(@"HH\:mm"),

                IsPastSlot = s.Date < DateOnly.FromDateTime(DateTime.Now) ||
                // Or the time is current date and the slot's time has passed
                 (s.Date == DateOnly.FromDateTime(DateTime.Now) &&
                  (s.Slot.StartTime < TimeOnly.FromDateTime(DateTime.Now)))
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
                    IsPastDay = currentMonday.AddDays(i) < DateOnly.FromDateTime(DateTime.Now),
                }).ToList()
            };

            var bookingScheduleVM = new BookingScheduleVM
            {
                MentorSchedule = mentorScheduleWeekVM,
                SelectedSlot = null
            };

            return View(bookingScheduleVM);
        }

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpGet("proceed")]
        public IActionResult ProceedToBooking(int scheduleId)
        {
            var mentorSchedule = _unitOfWork.MentorSchedule.Get(ms => ms.Id == scheduleId,
                includeProperties: $"{nameof(Slot)},MentorDetail.User");
            if (mentorSchedule is null || mentorSchedule.Status != Constants.MentorScheduleStatus.Available)
            {
                return NotFound();
            }

            // Get student detail and check if that student is a leader
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = _unitOfWork.Student.Get(s => s.User.Email == userEmail,
                includeProperties: $"{nameof(User)},Group.Topic");
            if (student is null || student.Group is null || !student.IsLeader)
            {
                return NotFound();
            }

            var bookingDetailsVM = new BookingScheduleDetailsVM
            {
                GroupId = student.Group.Id,
                LeaderId = student.UserId,
                MentorScheduleId = scheduleId,
                GroupName = student.Group.GroupName,
                GroupTopic = student.Group.Topic.Name,
                MentorId = mentorSchedule.MentorDetail.UserId,
                MentorName = mentorSchedule.MentorDetail.User.FullName,
                SlotId = mentorSchedule.SlotId,
                ScheduleDate = mentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotStartTime = mentorSchedule.Slot.StartTime,
                SlotEndTime = mentorSchedule.Slot.EndTime
            };

            return View(bookingDetailsVM);
        }

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpPost("proceed")]
        public IActionResult ProceedToBooking(BookingScheduleDetailsVM bookingDetailsVM)
        {
            // Get the schedule
            var mentorSchedule = _unitOfWork.MentorSchedule.Get(ms => ms.Id == bookingDetailsVM.MentorScheduleId,
                includeProperties: $"{nameof(Slot)},MentorDetail.User");
            if (mentorSchedule is null || mentorSchedule.Status != Constants.MentorScheduleStatus.Available)
            {
                return NotFound();
            }
            // Get the current student 
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = _unitOfWork.Student.Get(s => s.User.Email == userEmail,
                includeProperties: $"{nameof(User)},Group.Topic,Group.Wallet");
            if (student is null || student.Group is null || !student.IsLeader)
            {
                return NotFound();
            }
            // Get current wallet balance of the Student's group
            Wallet groupWallet = _unitOfWork.Wallet.Get(w => w.Id == student.Group.WalletId);
            if (groupWallet is null)
            {
                TempData["error"] = "An error has occurred. Please try again.";
                return RedirectToAction(nameof(BookSchedule), mentorSchedule.MentorDetail.UserId);
            }
            // Re-populate the BookingScheduleDetailsVM, this time with the Wallet's balance
            var confirmBookingVM = new BookingScheduleDetailsVM
            {
                GroupId = student.Group.Id,
                LeaderId = student.UserId,
                GroupName = student.Group.GroupName,
                GroupTopic = student.Group.Topic.Name,
                MentorId = mentorSchedule.MentorDetail.UserId,
                MentorName = mentorSchedule.MentorDetail.User.FullName,
                MentorScheduleId = bookingDetailsVM.MentorScheduleId,
                SlotId = mentorSchedule.SlotId,
                ScheduleDate = mentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotStartTime = mentorSchedule.Slot.StartTime,
                SlotEndTime = mentorSchedule.Slot.EndTime,
                WalletBalance = groupWallet.Balance,
                Note = bookingDetailsVM.Note,
                BookingCost = 10,
            };

            return RedirectToAction(nameof(ConfirmBooking), confirmBookingVM);
        }

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpGet("confirmation")]
        public IActionResult ConfirmBooking(BookingScheduleDetailsVM confirmBookingVM)
        {
            if (confirmBookingVM is null)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            }

            return View(confirmBookingVM);
        }

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpPost("finalize")]
        // Finalize booking => Create a new Booking to the database
        public IActionResult FinalizeBooking(BookingScheduleDetailsVM bookingDetailsVM)
        {
            if (bookingDetailsVM is null || !ModelState.IsValid)
            {
                TempData["error"] = "An error has occurred processing booking details. Please try again.";
                return RedirectToAction(nameof(ConfirmBooking), bookingDetailsVM);
            }
            // Double-check the wallet balance
            if (bookingDetailsVM.BalanceAfter <= 0)
            {
                TempData["ErrorMessage"] = "Insufficient balance to proceed with booking.";
                return RedirectToAction(nameof(ConfirmBooking), bookingDetailsVM);
            }
            // Retrieve necessary information for updates
            Wallet wallet = _unitOfWork.Wallet.Get(w => w.StudentGroup.Id == bookingDetailsVM.GroupId);
            MentorSchedule mentorSchedule = _unitOfWork.MentorSchedule.Get(ms => ms.Id == bookingDetailsVM.MentorScheduleId);

            if (wallet is null || mentorSchedule is null)
            {   // Display error
                TempData["error"] = "An error has occurred processing booking details. Please try again.";
                return RedirectToAction(nameof(BookSchedule), bookingDetailsVM.MentorId);
            }
            // Implement the logic to create the booking in the database
            Booking booking = new Booking
            {
                LeaderId = bookingDetailsVM.LeaderId,
                MentorScheduleId = bookingDetailsVM.MentorScheduleId,
                Timestamp = DateTime.Now,
                Note = bookingDetailsVM.Note,
                Status = Constants.BookingStatus.Pending,
            };
            // Initiate transaction
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    _unitOfWork.Booking.Add(booking);

                    // Update the wallet balance
                    wallet.Balance = (wallet.Balance - _bookingCost);
                    _unitOfWork.Wallet.Update(wallet);

                    // Add to transaction history
                    WalletTransaction walletTransaction = new WalletTransaction
                    {
                        Date = DateTime.Now,
                        Amount = _bookingCost,
                        Type = Constants.WalletDefaults.TransactionTypeDeduction,
                        Description = Constants.TransactionDescriptions.BookingPayment,
                        WalletId = wallet.Id,
                    };
                    _unitOfWork.WalletTransaction.Add(walletTransaction);

                    // Update the mentor schedule status
                    mentorSchedule.Status = Constants.MentorScheduleStatus.Booked;
                    _unitOfWork.MentorSchedule.Update(mentorSchedule);
                    // Save changes
                    _unitOfWork.Save();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["error"] = "An error has occurred processing booking details. Please try again.";
                    return RedirectToAction(nameof(BookSchedule), bookingDetailsVM.MentorId);
                }
            }
            TempData["SuccessMessage"] = "Booking confirmed successfully!";
            return RedirectToAction(nameof(BookingSuccess), new { bookingId = booking.Id });
        }

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpGet("success")]
        // Indicates successful booking => Shows booked schedule's details (invoice)
        public IActionResult BookingSuccess(int bookingId)
        {
            // Retrieve the booked schedule info
            Booking booking = _unitOfWork.Booking.Get(b => b.Id == bookingId,
                includeProperties: "MentorSchedule.MentorDetail.User,MentorSchedule.Slot,Leader.User,Leader.Group");

            if (booking is null)
            {
                return NotFound();
            }

            var bookingSuccessVM = new BookingSuccessVM
            {
                BookingId = booking.Id,
                GroupName = booking.Leader.Group.GroupName,
                MentorName = booking.MentorSchedule.MentorDetail.User.FullName,
                ScheduleDate = booking.MentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotId = booking.MentorSchedule.SlotId,
                SlotStartTime = booking.MentorSchedule.Slot.StartTime,
                SlotEndTime = booking.MentorSchedule.Slot.EndTime,
                Note = booking.Note,
                BookingCost = _bookingCost,
                Timestamp = booking.Timestamp,

                GroupId = booking.Leader.GroupId,
                MentorId = booking.MentorSchedule.MentorDetail.UserId
            };

            return View(bookingSuccessVM);
        }

        // End of booking functionalities for Student's group leader //
        // --------------------------------------------------------- //
        [Authorize(Roles = "Student")]
        [HttpGet("all")]
        public IActionResult ViewStudentBookings(string status, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            // Get Student and their group info
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = _unitOfWork.Student.Get(u => u.User.Email == userEmail,
                            includeProperties: nameof(User));

            if (student is null)
            {
                return NotFound();
            }

            // Get the Student's group info
            StudentGroup studentGroup = _unitOfWork.StudentGroup.Get(g => g.Id == student.GroupId,
                        includeProperties: $"StudentDetails.User");

            if (studentGroup is null) // The student does not belong to any group
            {
                return View();
            }

            // Get the list of bookings made by the group leader
            var query = _unitOfWork.Booking.GetAll(b => b.Leader.GroupId == studentGroup.Id,
                includeProperties: "MentorSchedule.MentorDetail.User,MentorSchedule.Slot,Leader.User,Leader.Group");

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(b => b.MentorSchedule.Date >= DateOnly.FromDateTime(startDate.Value));
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.MentorSchedule.Date <= DateOnly.FromDateTime(endDate.Value));
            }

            // Order the results
            query = query.OrderByDescending(b => b.Timestamp);

            // Pagination
            int pageSize = 10;
            int skip = (page - 1) * pageSize;
            var bookings = query.Skip(skip).Take(pageSize).ToList();

            // Populate the view model with necessary information
            var bookingDetailsVM = bookings.Select(b => new BookingDetailVM
            {
                BookingId = b.Id,
                MentorName = b.MentorSchedule.MentorDetail.User.FullName,
                ScheduleDate = b.MentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotStartTime = b.MentorSchedule.Slot.StartTime,
                SlotEndTime = b.MentorSchedule.Slot.EndTime,
                Timestamp = b.Timestamp,
                Status = b.Status,
                IsCompletable = BookingScheduleHelper.IsBookingCompletable(b)
                && student.IsLeader,
                IsFeedbackable = BookingScheduleHelper.IsBookingFeedbackable(b, student.User, _unitOfWork)
            }).ToList();

            var studentBookingsVM = new MentorBookingsVM
            {
                Bookings = bookingDetailsVM,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(query.Count() / (double)pageSize),
                Status = status,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(studentBookingsVM);
        }

        /*
        [Authorize(Roles = "Mentor")]
        [HttpGet("bookings")]
        public IActionResult ViewMentorBookings()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var mentor = _unitOfWork.Mentor.Get(m => m.User.Email == userEmail, includeProperties: nameof(User));

            if (mentor is null)
            {
                return NotFound();
            }

            // Get all bookings for this mentor
            IEnumerable<Booking> bookings = _unitOfWork.Booking.GetAll(
                b => b.MentorSchedule.MentorDetailId == mentor.UserId,
                includeProperties: "MentorSchedule.Slot,Leader.User,Leader.Group"
            ).OrderByDescending(b => b.Timestamp);

            // Populate the view model with necessary information
            IEnumerable<BookingDetailVM> bookingDetailsVM = bookings.Select(b => new BookingDetailVM
            {
                BookingId = b.Id,
                GroupName = b.Leader.Group.GroupName,
                ScheduleDate = b.MentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotId = b.MentorSchedule.SlotId,
                SlotStartTime = b.MentorSchedule.Slot.StartTime,
                SlotEndTime = b.MentorSchedule.Slot.EndTime,
                Timestamp = b.Timestamp,
                Status = b.Status,
                Note = b.Note
            }).ToList();

            return View(bookingDetailsVM);
        }
        */

        [Authorize(Roles = "Mentor")]
        [HttpGet("bookings")]
        public IActionResult ViewMentorBookings(string status, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var mentor = _unitOfWork.Mentor.Get(m => m.User.Email == userEmail, includeProperties: nameof(User));

            if (mentor is null)
            {
                return NotFound();
            }

            // Get all bookings for this mentor
            var bookingsQuery = _unitOfWork.Booking.GetAll(
                b => b.MentorSchedule.MentorDetailId == mentor.UserId,
                includeProperties: "MentorSchedule.Slot,Leader.User,Leader.Group"
            );

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == status);
            }

            if (startDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.MentorSchedule.Date >= DateOnly.FromDateTime(startDate.Value));
            }

            if (endDate.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.MentorSchedule.Date <= DateOnly.FromDateTime(endDate.Value));
            }

            // Order by date
            bookingsQuery = bookingsQuery.OrderByDescending(b => b.MentorSchedule.Date);

            // Apply pagination
            var totalItems = bookingsQuery.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var bookings = bookingsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Populate the view model with necessary information
            var bookingDetailsVM = bookings.Select(b => new BookingDetailVM
            {
                BookingId = b.Id,
                GroupName = b.Leader.Group.GroupName,
                ScheduleDate = b.MentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotId = b.MentorSchedule.SlotId,
                SlotStartTime = b.MentorSchedule.Slot.StartTime,
                SlotEndTime = b.MentorSchedule.Slot.EndTime,
                Timestamp = b.Timestamp,
                Status = b.Status,
                IsPastBooking = BookingScheduleHelper.IsBookingInPast(b.MentorSchedule),
                IsApprovable = BookingScheduleHelper.IsBookingApprovable(b),
                IsFeedbackable = BookingScheduleHelper.IsBookingFeedbackable(b, mentor.User, _unitOfWork),
                Note = b.Note
            }).ToList();

            var mentorBookingsVM = new MentorBookingsVM
            {
                Bookings = bookingDetailsVM,
                CurrentPage = page,
                TotalPages = totalPages,
                Status = status,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(mentorBookingsVM);
        }

        [Authorize(Roles = "Mentor, Student")]
        [HttpGet("detail/{bookingId}")]
        // View booking detail based on bookingId
        public IActionResult ViewBookingDetail(int bookingId)
        {
            // Get user info
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail,
                includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");

            // Retrieve the booking with all necessary related entities
            Booking booking = _unitOfWork.Booking.Get(b => b.Id == bookingId,
                includeProperties: "MentorSchedule.MentorDetail.User,MentorSchedule.Slot,Leader.User,Leader.Group");

            if (booking is null)
            {
                return NotFound();
            }

            // Create a view model for the booking details
            var bookingDetailVM = new BookingDetailVM
            {
                BookingId = booking.Id,
                GroupName = booking.Leader.Group.GroupName,
                MentorName = booking.MentorSchedule.MentorDetail.User.FullName,
                ScheduleDate = booking.MentorSchedule.Date.ToDateTime(TimeOnly.MinValue),
                SlotStartTime = booking.MentorSchedule.Slot.StartTime,
                SlotEndTime = booking.MentorSchedule.Slot.EndTime,
                Note = booking.Note,
                BookingCost = _bookingCost,
                Timestamp = booking.Timestamp,
                Status = booking.Status,

                IsPastBooking = BookingScheduleHelper.IsBookingInPast(booking.MentorSchedule),
                IsApprovable = User.IsInRole("Mentor")
                && BookingScheduleHelper.IsBookingApprovable(booking),

                IsCompletable = booking.LeaderId == user.Id
                && BookingScheduleHelper.IsBookingCompletable(booking),

                IsFeedbackable = BookingScheduleHelper.IsBookingFeedbackable(booking, user, _unitOfWork)


            };

            return View(bookingDetailVM);
        }

        [Authorize(Roles = "Mentor")]
        [HttpPost("approve")]
        // Confirm / Approve the booking to notify the student group
        public IActionResult ApproveBooking(int bookingId)
        {
            // Get the booking info
            Booking booking = _unitOfWork.Booking.Get(b => b.Id == bookingId,
                includeProperties: $"Leader.Group,{nameof(MentorSchedule)}");

            if (booking is null)
            {
                return NotFound();
            }
            // Handles the case that the booking is past current time
            if (!BookingScheduleHelper.IsBookingApprovable(booking))
            {
                TempData["error"] = "This booking cannot be confirmed.";
                return RedirectToAction(nameof(ViewMentorBookings));
            }

            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    booking.Status = Constants.BookingStatus.Confirmed;
                    _unitOfWork.Booking.Update(booking);

                    _unitOfWork.Save();
                    transaction.Commit();
                    TempData["success"] = "Booking confirmed successfully.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["error"] = "An error occurred while confirming the booking.";
                }
            }

            return RedirectToAction(nameof(ViewMentorBookings));
        }

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpPost("complete")]
        public IActionResult CompleteBooking(int bookingId)
        {
            var booking = _unitOfWork.Booking.Get(b => b.Id == bookingId,
                          includeProperties: "MentorSchedule.MentorDetail");
            if (booking == null)
            {
                return NotFound();
            }
            if (booking.Status != Constants.BookingStatus.Confirmed)
            {
                TempData["error"] = "Only confirmed bookings can be marked as completed.";
                return RedirectToAction(nameof(ViewStudentBookings));
            }
            // Get mentor's info
            MentorDetail mentor = _unitOfWork.Mentor.Get(m =>
                                  m.UserId == booking.MentorSchedule.MentorDetailId);

            if (mentor is null)
            {
                TempData["error"] = "An error has occurred. Please try again";
                return RedirectToAction(nameof(ViewStudentBookings));
            }
            // Proceed to update booking status & add points to Mentor's booking score
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    // Update status
                    booking.Status = Constants.BookingStatus.Completed;
                    _unitOfWork.Booking.Update(booking);

                    // Update score
                    mentor.BookingScore += _bookingCost;
                    _unitOfWork.Mentor.Update(mentor);
                    _unitOfWork.Save();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["error"] = "An error has occurred completing the meeting. Please try again.";
                    return View(nameof(ViewStudentBookings));

                }
            }

            TempData["success"] = "Booking marked as completed successfully.";
            return RedirectToAction(nameof(ViewStudentBookings));
        }


    }
}

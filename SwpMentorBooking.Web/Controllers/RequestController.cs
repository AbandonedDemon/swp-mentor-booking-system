using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Web.ViewModels;
using System.Security.Claims;

namespace SwpMentorBooking.Web.Controllers
{
    [Route("request")]
    public class RequestController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilService _utilService;

        public RequestController(IUnitOfWork unitOfWork, IUtilService utilService)
        {
            _unitOfWork = unitOfWork;
            _utilService = utilService;
        }
        // ADMIN actions
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public IActionResult ManageRequests()
        {
            // Retrieve all requests
            List<Request> requests = _unitOfWork.Request.GetAll(
                                includeProperties: $"Leader.User,Leader.Group.Wallet,Responses")
                                    .ToList();
            return View(requests);
        }
        //
        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpGet("send")]
        public IActionResult SendRequest()
        {
            // Retrieve current user info and validate their credentials
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            StudentDetail student = _unitOfWork.Student.Get(s => s.User.Email == userEmail,
                                    includeProperties: $"{nameof(User)},Group");

            if (student is null)
            {
                return NotFound();
            }
            // Populate the View model
            RequestVM requestVM = new RequestVM
            {
                LeaderId = student.UserId,
                GroupName = student.Group?.GroupName,
                Request = new Request()
            };
            // Take the Student to the Send Request page
            return View(requestVM);
        }
        [Authorize(Roles = "Student")]
        [HttpGet("students")]
        public IActionResult ViewGroupRequests()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            StudentDetail student = _unitOfWork.Student.Get(s => s.User.Email == userEmail,
                                     includeProperties: $"{nameof(User)},Group,Requests");
            if (student is null)
            {
                return NotFound();
            }
            // Get the group's leader info
            StudentDetail studentLeader = _unitOfWork.Student.Get(
                        l => student.GroupId == l.GroupId && l.IsLeader,
                        includeProperties: $"{nameof(User)},Requests");

            // Get the list of requests
            List<Request> requestList = _unitOfWork.Request.GetAll(r => r.Leader == studentLeader,
                                                                includeProperties: "Leader,Responses")
                                                           .ToList();

            return View(requestList);
        }

        [Authorize(Roles = "Student, Admin")]
        [HttpGet("details/{id}")]
        public IActionResult ViewDetails(int id)
        {
            Request request = _unitOfWork.Request.Get(r => r.Id == id,
                               includeProperties: "Responses");

            if (request is null)
            {
                return NotFound();
            }

            return View(request); // Ensure that the Details.cshtml view exists
        }

        [Authorize(Roles = "Student", Policy = "GroupLeaderOnly")]
        [HttpPost("send")]
        public async Task<IActionResult> SendRequest(RequestVM requestVM)
        {
            // Retrieve info
            StudentDetail student = _unitOfWork.Student.Get(s => s.UserId == requestVM.LeaderId,
                                     includeProperties: $"Group,Requests");

            if (student is null)
            {
                return NotFound();
            }

            Request newRequest = new Request
            {
                Title = requestVM.Request.Title,
                Content = requestVM.Request.Content,
                LeaderId = student.UserId,
                Timestamp = DateTime.Now,
                
            };

            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    _unitOfWork.Request.Add(newRequest);
                    _unitOfWork.Save();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["error"] = "An error has occurred creating a new request.";
                    return View(nameof(ViewGroupRequests));
                }
            }
            // Request has been created successfully
            var admin = _unitOfWork.Admin.Get(includeProperties: nameof(User));
            // Create mail content
            string subject = $"New Request from {requestVM.GroupName}";
            string body = $"<h1>Dear {admin.User.FullName},</h1>" +
            $"<p>A new request has been submitted by '{requestVM.GroupName}' with the title '{requestVM.Request.Title}' at {DateTime.Now}.</p>";
            //
            await _utilService.Email.SendEmail(admin.User.Email, subject, body);
            TempData["success"] = "Request has been created successfully!";
            return RedirectToAction(nameof(ViewGroupRequests));
        }
    }
}

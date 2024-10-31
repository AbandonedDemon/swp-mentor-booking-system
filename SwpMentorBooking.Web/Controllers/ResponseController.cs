using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ResponseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ResponseController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [HttpPost("send")]
        public IActionResult SendResponse(int requestId, string status, string content)
        {
            // Get the current request
            Request request = _unitOfWork.Request.Get(r => r.Id == requestId,
                               includeProperties: "Leader");

            if (request is null)
            {
                TempData["error"] = "Request not found. Please try again.";
                return RedirectToAction("ManageRequests", "Request");
            }

            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    // Add new response to Database
                    Response response = new Response
                    {
                        RequestId = request.Id,
                        Content = content,
                        Timestamp = DateTime.Now
                    };
                    _unitOfWork.Response.Add(response);

                    // Update request status 
                    request.Status = status;
                    _unitOfWork.Request.Update(request);

                    

                    _unitOfWork.Save();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["error"] = $"An error has occurred.\n{ex.Message}";
                    return RedirectToAction("ManageRequests", "Request");
                }
            }
            // Response operation is successful
            TempData["success"] = "Response sent successfully!";
            return RedirectToAction("ManageRequests", "Request");
        }
    }
}

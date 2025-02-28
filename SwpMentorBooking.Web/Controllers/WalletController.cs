using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Web.ViewModels;
using System.Security.Claims;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize(Roles = "Student, Admin")]
    [Route("wallet")]
    public class WalletController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public WalletController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("transactions")]
        public IActionResult ViewTransactions()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = _unitOfWork.Student.Get(s => s.User.Email == userEmail,
                includeProperties: $"{nameof(User)},Group.Wallet");

            if (student?.Group?.Wallet == null)
            {
                return NotFound();
            }

            var transactions = _unitOfWork.WalletTransaction
                .GetAll(w => w.WalletId == student.Group.WalletId)
                .OrderByDescending(t => t.Date)
                .ToList();

            var walletTransactionsVM = new WalletTransactionsVM
            {
                WalletBalance = student.Group.Wallet.Balance,
                Transactions = transactions
            };

            return View(walletTransactionsVM);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("transactions/{groupId}")]
        public IActionResult ManageTransactions(int groupId)
        {
            var group = _unitOfWork.StudentGroup.Get(g => g.Id == groupId,
                includeProperties: $"{nameof(Wallet)}");

            if (group?.Wallet == null)
            {
                return NotFound();
            }

            var transactions = _unitOfWork.WalletTransaction
                .GetAll(w => w.WalletId == group.WalletId)
                .OrderByDescending(t => t.Date)
                .ToList();

            var walletTransactionsVM = new WalletTransactionsVM
            {
                WalletBalance = group.Wallet.Balance,
                Transactions = transactions
            };

            return View(walletTransactionsVM);
        }
    }
} 
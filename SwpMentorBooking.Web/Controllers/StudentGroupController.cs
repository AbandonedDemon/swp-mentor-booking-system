using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Application.Common.Utilities;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Web.ViewModels;
using System.Security.Claims;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize(Roles = $"{Constants.UserRoles.Student}, {Constants.UserRoles.Admin}")]
    [Route("student-group")]
    public class StudentGroupController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public StudentGroupController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("")]
        [Authorize(Roles = "Student")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /student-group/create
        [HttpGet("create")]
        [Authorize(Roles = "Student")]
        public IActionResult Create()
        {
            StudentGroupVM studentGroupVM = new StudentGroupVM
            {
                TopicList = _unitOfWork.Topic.GetAll().Select(t => new SelectListItem
                {
                    Text = t.Name,
                    Value = t.Id.ToString(),
                })
            };
            return View(studentGroupVM);
        }

        // POST: /student-group/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public IActionResult Create(StudentGroupVM studentGroupVM)
        {

            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(StudentDetail));
            try
            {
                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    // Create a new wallet
                    Wallet wallet = new Wallet();
                    _unitOfWork.Wallet.Add(wallet);
                    _unitOfWork.Save();

                    // Assign the wallet ID to the group
                    studentGroupVM.StudentGroup.WalletId = wallet.Id;

                    // Add the new group
                    _unitOfWork.StudentGroup.Add(studentGroupVM.StudentGroup);
                    _unitOfWork.Save();

                    user.StudentDetail.GroupId = studentGroupVM.StudentGroup.Id; // Set the GroupId
                    user.StudentDetail.IsLeader = true; // Set the user as the leader
                    _unitOfWork.User.Update(user); // Update the user
                    _unitOfWork.Save(); // Save changes

                    transaction.Commit();
                }
                TempData["success"] = $"Group \"{studentGroupVM.StudentGroup.GroupName}\" created successfully.";
                return RedirectToAction(actionName: "MyGroup", controllerName: "Student");
            }
            catch (Exception ex)
            {   // Logging
                TempData["error"] = "An error occurred when creating new group.";
                return RedirectToAction(actionName: "MyGroup", controllerName: "Student");
            }
        }

        // GET: /student-group/add-member
        [HttpGet("add-member")]
        [Authorize(Policy = "GroupLeaderOnly")]
        [Authorize(Roles = "Student")]
        public IActionResult AddMember()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.User.Get(u => u.Email == userEmail, includeProperties: nameof(StudentDetail));

            if (!user.StudentDetail.IsLeader)
            {
                return Forbid();
            }

            SearchGroupMemberVM searchGroupMemberVM = new SearchGroupMemberVM();
            return View(searchGroupMemberVM);
        }

        // GET: /student-group/add-member/search
        [HttpGet("add-member/search")]
        [Authorize(Policy = "GroupLeaderOnly")]
        [Authorize(Roles = "Student")]
        public IActionResult AddMember(SearchGroupMemberVM searchGroupMemberVM)
        {

            // From the Students, get all those...
            IEnumerable<StudentDetail> students = _unitOfWork.Student.GetAll(u =>
                                // Student Code match search query...
                                ((u.StudentCode.Contains(searchGroupMemberVM.StudentCode))
                                 // and do not belong to any group.
                                 && (u.GroupId == null)), includeProperties: $"{nameof(User)}");

            searchGroupMemberVM = new SearchGroupMemberVM
            {
                SearchResults = students.Select(s =>
                                        new StudentDetailVM
                                        {
                                            UserId = s.UserId,
                                            FullName = s.User.FullName,
                                            StudentCode = s.StudentCode,
                                            Email = s.User.Email
                                        })
                                        .ToList(),
            };
            return View(nameof(AddMember), searchGroupMemberVM);
        }

        // POST: /student-group/add-selected-members
        [HttpPost("add-selected-members")]
        [Authorize(Policy = "GroupLeaderOnly")]
        [Authorize(Roles = "Student")]
        public IActionResult AddSelectedMembers(List<int> memberIds)
        {
            if (memberIds.IsNullOrEmpty())
            {
                return View(nameof(AddMember));
            }
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var studentLeader = _unitOfWork.Student.Get(s => s.User.Email == userEmail,
                                                        includeProperties: nameof(User));

            if (!studentLeader.IsLeader)
            {
                return Forbid();
            }
            // Get the students to add into the group
            IEnumerable<User> studentsToAdd = _unitOfWork.User.GetAll(s => memberIds.Contains(s.Id),
                                                            includeProperties: $"{nameof(StudentDetail)}");
            // Retrieved student(s) not found => Show error
            if (studentsToAdd.IsNullOrEmpty())
            {
                TempData["error"] = "An error has occurred. Please try again.";
                return RedirectToAction(nameof(AddMember));
            }
            // Proceed to adding the students into the group
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    // Adding the students into the leader's group
                    foreach (var student in studentsToAdd)
                    {   // Assign the leader's group to the students
                        student.StudentDetail.GroupId = studentLeader.GroupId;
                        _unitOfWork.User.Update(student);
                        _unitOfWork.Save();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Handle exception
                    transaction.Rollback();
                    return View(nameof(Index));
                }
            }

            TempData["success"] = "Group member(s) added successfully.";
            return RedirectToAction(actionName: "MyGroup", controllerName: "Student");
        }

        [Authorize(Roles = Constants.UserRoles.Admin)]
        [HttpGet("manage")]
        public IActionResult ManageGroup()
        {
            // Get all groups with their related data
            var groups = _unitOfWork.StudentGroup.GetAll(
                includeProperties: $"{nameof(Topic)},{nameof(Wallet)},StudentDetails.User"
            ).ToList();

            // Map to view model
            var manageGroupsVM = new ManageGroupVM
            {
                Groups = groups.Select(g => new ManageGroupDetailVM
                {
                    GroupId = g.Id,
                    GroupName = g.GroupName,
                    TopicName = g.Topic.Name,
                    WalletBalance = g.Wallet.Balance,
                    Members = g.StudentDetails.Select(s => new StudentDetailVM
                    {
                        UserId = s.UserId,
                        FullName = s.User.FullName,
                        Email = s.User.Email,
                        StudentCode = s.StudentCode,
                    }).ToList()
                }).ToList()
            };

            return View(manageGroupsVM);
        }

    }
}

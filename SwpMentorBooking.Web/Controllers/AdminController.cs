using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Infrastructure.DTOs.ImportedUser;
using SwpMentorBooking.Infrastructure.Utils;
using SwpMentorBooking.Web.ViewModels;

namespace SwpMentorBooking.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilService _utilService;
        public AdminController(IUnitOfWork unitOfWork, IUtilService utilService)
        {
            _unitOfWork = unitOfWork;
            _utilService = utilService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("manage-users")]
        public IActionResult ManageUser()
        {
            // Get Student & Mentor lists
            var studentList = _unitOfWork.User.GetAll(u => u.StudentDetail.UserId == u.Id,
                                                      includeProperties: nameof(StudentDetail));
            var mentorList = _unitOfWork.User.GetAll(u => u.MentorDetail.UserId == u.Id,
                                                     includeProperties: nameof(MentorDetail));
            // Populate the View model to display
            ManageUserVM manageUserVM = new ManageUserVM
            {
                Students = studentList,
                Mentors = mentorList
            };

            return View(manageUserVM);
        }

        [HttpGet("import-users")]
        public IActionResult Import()
        {
            ImportUserTypeVM importUserTypeVM = new ImportUserTypeVM()
            {
                UserType = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "Student", Value = "Student" },
                    new SelectListItem() { Text = "Mentor", Value = "Mentor" }
                }
            };
            return View(importUserTypeVM);
        }

        [HttpPost("import-users")]
        public IActionResult Import(ImportUserTypeVM userTypeVM)
        {
            // Get selected user type & CSV file input
            string userType = userTypeVM.SelectedUserType;
            var inputFile = userTypeVM.InputFile;

            // Uploaded file is empty
            if (inputFile is null || inputFile.Length == 0)
            {
                return View();
            }
            // Try to read CSV file
            try
            {
                // Save the file temporarily
                var filePath = Path.GetTempFileName();
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    inputFile.CopyTo(stream);
                }

                // Dictionary to map user types to their corresponding DTO types
                var userTypeToDtoType = new Dictionary<string, Type>
                {
                    { "Student", typeof(CSVStudentDTO) },
                    { "Mentor", typeof(CSVMentorDTO) }
                };

                // Get the corresponding DTO type for the selected user type
                if (!userTypeToDtoType.TryGetValue(userType, out var dtoType))
                {
                    throw new InvalidOperationException("Invalid user type selected.");
                }

                // Use reflection to call the ReadCSVFile method with the appropriate type
                var method = typeof(CSVFileService).GetMethod("ReadCSVFile").MakeGenericMethod(dtoType);
                var importedData = method.Invoke(_utilService.CSV, new object[] { filePath });

                // Validate the imported data
                var validationResults = _utilService.CSV.ValidateCSVData((dynamic)importedData);

                // Delete the temporary file
                System.IO.File.Delete(filePath);

                /// importResult
                // Use ImportUserPreviewVM to pass the uploaded DTOs & validation errors to the View
                dynamic importResult;

                if (userType == "Student")
                {
                    importResult = new ImportUserPreviewVM<CSVStudentDTO>
                    {
                        Results = validationResults
                    };
                }
                else
                {
                    importResult = new ImportUserPreviewVM<CSVMentorDTO>
                    {
                        Results = validationResults
                    };
                }

                TempData["ImportResult"] = JsonConvert.SerializeObject(importResult);
                TempData["SelectedUserType"] = userType;
                return RedirectToAction(nameof(ImportConfirm));
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        [HttpGet("import-users/confirm")]
        public IActionResult ImportConfirm()
        {
            var importResult = TempData["ImportResult"] as string;
            var selectedUserType = TempData["SelectedUserType"] as string;
            var importErrors = TempData["ImportErrors"] as string;

            if (string.IsNullOrEmpty(importResult) || string.IsNullOrEmpty(selectedUserType))
            {
                return RedirectToAction(nameof(Import));
            }

            if (selectedUserType == "Student")
            {
                var studentResults = JsonConvert.DeserializeObject<ImportUserPreviewVM<CSVStudentDTO>>(importResult);
                if (!string.IsNullOrEmpty(importErrors))
                {   // Add error messages to Student import
                    studentResults.ImportErrors = JsonConvert.DeserializeObject<List<string>>(importErrors);
                }
                return View("ImportConfirmStudent", studentResults);
            }
            if (selectedUserType == "Mentor")
            {
                var mentorResults = JsonConvert.DeserializeObject<ImportUserPreviewVM<CSVMentorDTO>>(importResult);
                // Add error messages to Mentor import
                if (!string.IsNullOrEmpty(importErrors))
                {
                    mentorResults.ImportErrors = JsonConvert.DeserializeObject<List<string>>(importErrors);
                }
                // Split Skills into a list for each mentor
                foreach (var result in mentorResults.Results)
                {
                    // Split MainProgrammingLanguage
                    result.Record.MainProgrammingLanguageList = _utilService.StringManipulation.SplitProperty(result.Record.MainProgrammingLanguage, "||");

                    // Split AltProgrammingLanguage
                    result.Record.AltProgrammingLanguageList = _utilService.StringManipulation.SplitProperty(result.Record.AltProgrammingLanguage, "||");

                    // Split Frameworks
                    result.Record.FrameworkList = _utilService.StringManipulation.SplitProperty(result.Record.Framework, "||");
                }

                return View("ImportConfirmMentor", mentorResults);
            }

            return View();
        }

        [HttpPost("import-users/confirm")]
        public IActionResult ImportConfirm(string DTOJson, string SelectedUserType)
        {
            if (string.IsNullOrEmpty(DTOJson) || string.IsNullOrEmpty(SelectedUserType))
            {
                return RedirectToAction(nameof(Import));
            }

            try
            {
                List<string> errorMessages = new List<string>();
                if (SelectedUserType == "Student")
                {
                    var dtos = JsonConvert.DeserializeObject<List<CSVStudentDTO>>(DTOJson);
                    errorMessages = ImportUsers(dtos, "Student");

                    // Handle error messages & reload page
                    if (errorMessages.Any())
                    {
                        var studentResults = new ImportUserPreviewVM<CSVStudentDTO>
                        {
                            Results = dtos.Select(d => (d, new List<string>())).ToList(),
                            ImportErrors = errorMessages
                        };
                        return View("ImportConfirmStudent", studentResults);
                    }
                }
                else if (SelectedUserType == "Mentor")
                {
                    var dtos = JsonConvert.DeserializeObject<List<CSVMentorDTO>>(DTOJson);
                    errorMessages = ImportUsers(dtos, "Mentor");

                    // Handle error messages & reload page
                    if (errorMessages.Any())
                    {
                        var mentorResults = new ImportUserPreviewVM<CSVMentorDTO>
                        {
                            Results = dtos.Select(d => (d, new List<string>())).ToList(),
                            ImportErrors = errorMessages
                        };
                        return View("ImportConfirmMentor", mentorResults);
                    }
                }
                else
                {
                    return RedirectToAction(nameof(Import));
                }

                // If successful, redirect to the index page
                TempData["success"] = "Users imported successfully.";
                return RedirectToAction(nameof(ManageUser));
            }
            catch (Exception ex)    // Problem happened when importing Users
            {
                TempData["error"] = "An error occurred during the import process. Please try again.";
                return RedirectToAction(nameof(Import));
            }
        }

        [HttpGet("update/{userId}")]
        public IActionResult Update(int userId)
        {   // Retrieve user from Database
            User? userToUpdate = _unitOfWork.User.Get(u => u.Id == userId,
                                           includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");
            if (userToUpdate is null)
            {   // An error occurred when clicking Update button
                TempData["error"] = "An error has occurred. Please try again.";
                return RedirectToAction(nameof(ManageUser));
            }

            return View(userToUpdate);
        }

        [HttpPost("update/{userId}")]
        public IActionResult Update(User user)
        {
            if (ModelState.IsValid)
            {
                // Update the retrieved user from DB
                using (var transaction = _unitOfWork.BeginTransaction())
                {
                    try
                    {   // Get existing user in Database
                        var existingUser = _unitOfWork.User.Get(u => u.Id == user.Id,
                            includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");

                        if (existingUser == null)
                        {   // An error occurred in the process
                            TempData["error"] = "An error occurred. User not found.";
                            return RedirectToAction(nameof(ManageUser));
                        }

                        // Update User with new details
                        var updatedUser = UpdateUserProperties(existingUser, user);

                        // Commit updates into Database
                        _unitOfWork.User.Update(updatedUser);
                        _unitOfWork.Save();
                        transaction.Commit();

                        TempData["success"] = $"User #{user.Id} has been updated successfully.";
                        return RedirectToAction(nameof(ManageUser));
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["error"] = "An error occurred during user update.";
                        // Log the exception details here
                    }
                }
                // => Delete operation is successful
                TempData["success"] = $"User #{user.Id} has been updated successfully.";
                return RedirectToAction(nameof(ManageUser));
            }
            return View();
        }

        [HttpGet("delete/{userId}")]
        public IActionResult Delete(int userId)
        {
            User? userToDelete = _unitOfWork.User.Get(u => u.Id == userId,
                                           includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");

            if (userToDelete is null) // Error occurred during clicking Delete button / User not found 
            {
                TempData["error"] = "An error occurred when getting User info. Please try again.";
                return RedirectToAction(nameof(Index), nameof(AdminController));
            }
            return View(userToDelete);
        }

        [HttpPost("delete/{userId}")]
        public IActionResult Delete(User user)
        {
            // Get the user to delete
            User? userToDelete = _unitOfWork.User.Get(u => u.Id == user.Id,
                                            includeProperties: $"{nameof(StudentDetail)},{nameof(MentorDetail)}");
            if (userToDelete is not null)   // User to delete is found
            {   // Process profile image logic
            }

            if (userToDelete.StudentDetail is not null)
            {
                var bookingList = _unitOfWork.Booking.GetAll(b => b.LeaderId == userToDelete.Id);
                foreach (var booking in bookingList)
                {
                    _unitOfWork.Booking.Delete(booking);
                }
                _unitOfWork.Save();
            }

            // Delete the retrieved user from DB
            User deletedUser;
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                deletedUser = _unitOfWork.User.Delete(userToDelete);
                if (deletedUser is null)
                {
                    TempData["error"] = "An error occurred during user deletion.";
                    return RedirectToAction(nameof(ManageUser));
                }
                _unitOfWork.Save();
                transaction.Commit();
            }

            // => Delete operation is successful
            TempData["success"] = $"User #{deletedUser.Id} has been deleted successfully.";
            return RedirectToAction(nameof(ManageUser));
        }

        [HttpGet("import-confirm-student")]
        public IActionResult ImportConfirmStudent()
        {
            return View();
        }

        [HttpGet("import-confirm-mentor")]
        public IActionResult ImportConfirmMentor()
        {
            return View();
        }

        [HttpGet("email-account-info")]
        public IActionResult SendEmailAccountInfo()
        {
            // Retrieve the list of users that haven't logged in yet
            IEnumerable<User> userFirstLoginList = _unitOfWork.User.GetAll(u => u.IsFirstLogin && u.IsActive);

            List<UserFirstLoginVM> userFirstLoginVMs = userFirstLoginList.Select(u => new UserFirstLoginVM
            {
                UserId = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
            }).ToList();

            return View(userFirstLoginVMs);
        }

        [HttpPost("email-account-info")]
        public async Task<IActionResult> SendEmailAccountInfo(List<UserFirstLoginVM> userFirstLoginVMs, List<int> selectedUsers)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "An error has occurred. Please try again.";
                return RedirectToAction(nameof(ManageUser));
            }

            List<User> usersToEmail = new List<User>();

            foreach (var userVM in userFirstLoginVMs)
            {
                if (selectedUsers.Contains(userVM.UserId))
                {
                    var user = _unitOfWork.User.Get(u => u.Id == userVM.UserId);
                    if (user != null && user.IsFirstLogin && user.IsActive)
                    {
                        usersToEmail.Add(user);
                    }
                }
            }

            if (usersToEmail.Count == 0)
            {
                TempData["error"] = "No eligible users selected to send emails.";
                return RedirectToAction(nameof(ManageUser));
            }

            string subject = "Your Account Information for First-Time Login";

            foreach (var user in usersToEmail)
            {
                string body = $"<h1>Dear {user.FullName},</h1>" +
                    $"<h3>Welcome to SWP Mentor Booking System!</h3>" +
                    $"<p>We are excited to have you on board. Below are your account details for your first-time login:</p>" +
                    $"<br />" +
                    $"<p><strong>Email:</strong> {user.Email}</p>" +
                    $"<p><strong>Password:</strong> {user.Password}</p>" +
                    $"<br/>" +
                    $"<p>Thank you for joining us!</p>" +
                    $"<p>Best regards,<br/>" +
                    $"SWP Mentor Booking System</p>";

                await _utilService.Email.SendEmail(user.Email, subject, body);
            }

            TempData["success"] = $"Account information emails sent to {usersToEmail.Count} users.";
            return RedirectToAction(nameof(ManageUser));
        }
        // ** Instant utilities ** //
        private List<string> ImportUsers<T>(List<T> dtos, string userType)
        {
            List<string> errorMessages = new List<string>();
            IEnumerable<User> users = _utilService.AutoMapper.MapAll<T, User>(dtos);

            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    foreach (var user in users)
                    {
                        user.Password = _utilService.PasswordGenerator.GeneratePassword();
                        user.Role = userType;
                        _unitOfWork.User.Add(user);
                    }
                    _unitOfWork.Save();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    errorMessages.Add("Error importing users: Users already exist. Please make changes and try again.");
                }
            }
            //foreach (var user in users)
            //{
            //    try
            //    {
            //        user.Password = _utilService.PasswordGenerator.GeneratePassword();
            //        user.Role = userType;
            //        _unitOfWork.User.Add(user);
            //        _unitOfWork.Save();
            //    }
            //    catch (Exception ex)
            //    {
            //        // Handle specific exceptions (duplicate email)
            //        if (ex.InnerException?.Message.Contains("duplicate key") == true)
            //        {
            //            errorMessages.Add($"User with email {user.Email} already exists.");
            //        }
            //        else
            //        {
            //            errorMessages.Add($"Error adding user {user.Email}: {ex.Message}");
            //        }
            //    }
            //}
            return errorMessages;
        }
        //    Split property by a certain delimiter
        private List<string> SplitProperty(string property)
        {
            if (!string.IsNullOrEmpty(property))
            {
                return property.Split("||").Select(s => s.Trim()).ToList();
            }
            else
            {
                return new List<string>();
            }
        }
        //    Update user properties regardless of Role
        private User UpdateUserProperties(User existingUser, User updatedUser)
        {
            existingUser.FullName = updatedUser.FullName;
            existingUser.Phone = updatedUser.Phone;
            existingUser.ProfilePhoto = updatedUser.ProfilePhoto;

            if (existingUser.StudentDetail != null && updatedUser.StudentDetail != null)
            {
                existingUser.StudentDetail.StudentCode = updatedUser.StudentDetail.StudentCode;
            }
            else if (existingUser.MentorDetail != null && updatedUser.MentorDetail != null)
            {
                existingUser.MentorDetail.MainProgrammingLanguage = updatedUser.MentorDetail.MainProgrammingLanguage;
                existingUser.MentorDetail.AltProgrammingLanguage = updatedUser.MentorDetail.AltProgrammingLanguage;
                existingUser.MentorDetail.Framework = updatedUser.MentorDetail.Framework;
                existingUser.MentorDetail.Education = updatedUser.MentorDetail.Education;
                existingUser.MentorDetail.AdditionalContactInfo = updatedUser.MentorDetail.AdditionalContactInfo;
                existingUser.MentorDetail.Description = updatedUser.MentorDetail.Description;
            }

            return existingUser;
        }

        private async Task SendEmailToImportedUser()
        {

        }
    }
}

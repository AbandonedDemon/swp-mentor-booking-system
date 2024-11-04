using Microsoft.AspNetCore.Mvc;
using SwpMentorBooking.Web.ViewModels;
using System.Linq;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Domain.Entities;
using SwpMentorBooking.Infrastructure.Utils;
using MailKit;
using MailKit.Search;

namespace SwpMentorBooking.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilService _utilService;
        public SearchController(IUnitOfWork unitOfWork, IUtilService utilService)
        {
            _unitOfWork = unitOfWork;
            _utilService = utilService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("search")]
        public IActionResult Search(SearchMentorVM model)
        {
            IEnumerable<MentorDetail> mentors = _unitOfWork.Mentor.GetAll(includeProperties: $"{nameof(User)}");
            IEnumerable<Skill> skills = _unitOfWork.Skill.GetAll();

            // Filter by BookingScore range if specified
            if (model.minBookingScore.HasValue || model.maxBookingScore.HasValue)
            {
                mentors = mentors.Where(m =>
                    (!model.minBookingScore.HasValue || m.BookingScore >= model.minBookingScore) &&
                    (!model.maxBookingScore.HasValue || m.BookingScore <= model.maxBookingScore)).ToList();
            }

            // Filter by ProgrammingLanguages if specified
            if (model.Skill != null && model.Skill.Any())
            {
                //mentors = mentors.Where(m => 
                //    model.Skill.Contains(m.MainProgrammingLanguage) || model.Skill.Contains(m.Framework) ).ToList();

                mentors = mentors.Where(m => m.MainProgrammingLanguage
                                             .Split(", ")
                                             .Contains(model.Skill, StringComparer.OrdinalIgnoreCase) ||
                                              m.MainProgrammingLanguage
                                             .Split(", ")
                                             .Contains(model.Skill, StringComparer.OrdinalIgnoreCase) ||
                                             m.Skills
                                             .Any(ms => ms.Name.Equals(model.Skill, StringComparison.OrdinalIgnoreCase))
                                             ).ToList();
            }


            // Filter by Name if specified
            if (!string.IsNullOrWhiteSpace(model.SearchTemp))
            {
                mentors = mentors.Where(m =>
                    m.User.FullName.Contains(model.SearchTemp, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            SearchMentorVM result = new SearchMentorVM()
            {
                SearchTemp = model.SearchTemp,
                SearchSkill = skills.ToList(),
                Skill = model.Skill,
                minBookingScore = model.minBookingScore,
                maxBookingScore = model.maxBookingScore,
                SearchResult = mentors.Select(m => new MentorDetailVM()
                {
                    UserId = m.UserId,
                    Email = m.User.Email,
                    FullName = m.User.FullName,
                    Phone = m.User.Phone,
                    Gender = m.User.Gender,
                    // Split the properties by the delimiter
                    MainProgrammingLanguage = _utilService.StringManipulation.SplitProperty(m?.MainProgrammingLanguage, ","),
                    AltProgrammingLanguage = _utilService.StringManipulation.SplitProperty(m?.AltProgrammingLanguage, ","),
                    Framework = _utilService.StringManipulation.SplitProperty(m?.Framework, "||"),
                    Education = _utilService.StringManipulation.SplitProperty(m?.Education, ","),
                    AdditionalContactInfo = _utilService.StringManipulation.SplitProperty(m?.AdditionalContactInfo, ","),
                    BookingScore = m?.BookingScore,
                    Description = m?.Description,
                    Skills = m.Skills.ToList(),
                    Specializations = m.Specs.ToList(),
                }).ToList(),
            };
            return View(result); // Return the filtered list to the view
        }
    }
}
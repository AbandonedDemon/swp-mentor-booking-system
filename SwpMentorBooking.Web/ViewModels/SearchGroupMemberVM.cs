using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SwpMentorBooking.Web.ViewModels
{
    public class SearchGroupMemberVM
    {
        [RegularExpression(@"^[A-Za-z]{2}\d{6}$",
            ErrorMessage = "Student Code format is invalid.")]
        [Display(Name = "Student Code")]
        public string StudentCode { get; set; }
        public List<StudentDetailVM> SearchResults { get; set; } = new List<StudentDetailVM>();

    }
}

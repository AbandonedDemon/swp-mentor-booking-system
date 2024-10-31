namespace SwpMentorBooking.Web.Helpers
{
    public static class StringHelper
    {
        public static string CapitalizeFirstLetter(string input)
        {
            return input.Substring(0, 1).ToUpper() + input.Substring(1);
        }   
    }
}

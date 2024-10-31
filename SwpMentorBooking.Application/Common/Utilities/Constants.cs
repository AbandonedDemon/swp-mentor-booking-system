namespace SwpMentorBooking.Application.Common.Utilities
{
    public static class Constants
    {

        public static class UserRoles
        {
            public const string Admin = "admin";
            public const string Mentor = "mentor";
            public const string Student = "student";
        }
        
        public static class WalletDefaults
        {
            public const int InitialBalance = 200;
            public const string TransactionTypeAddition = "addition";
            public const string TransactionTypeDeduction = "deduction";
        }

        public static class TransactionDescriptions
        {
            public const string InitialDeposit = "Initial wallet balance";
            public const string BookingPayment = "Payment for booking session";
            public const string RefundPayment = "Refund from cancelled booking";
            public const string AdminDeposit = "Admin deposit to wallet";
        }

        public static class BookingStatus
        {
            public const string Pending = "pending";
            public const string Confirmed = "confirmed";
            public const string Cancelled = "cancelled";
            public const string Completed = "completed";
        }

        public static class MentorScheduleStatus
        {
            public const string Available = "available";
            public const string Booked = "booked";
            public const string Unavailable = "unavailable";
        }
    }
} 
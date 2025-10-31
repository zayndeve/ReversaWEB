using System;

namespace WebApplication1.Exceptions
{
    // === Custom HTTP Codes === //
    public static class HttpCode
    {
        public const int OK = 200;
        public const int Created = 201;
        public const int NotModified = 304;
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int InternalServerError = 500;
    }

    // === Custom Error Messages === //
    public static class ErrorMessage
    {
        public const string ExistingMemberNick = "Member exists in the server";
        public const string WrongPassword = "Wrong password";
        public const string SomethingWentWrong = "Something went wrong";
        public const string NoDataFound = "No data is found!";
        public const string CreateFailed = "Create failed!";
        public const string UpdateFailed = "Update failed!";
        public const string NoMemberFound = "No member is registered with this nickname!";
        public const string UsedNickPhone = "This number is already registered!";
        public const string NotAuthenticated = "Sorry, but you are not authenticated — sign up first!";
        public const string MissingMemberNickPhoneEmail = "Missing member nickname, phone, or email";
        public const string InvalidOrExpiredToken = "Token has expired or is invalid";
        public const string ResetLinkSent = "✅ Reset link sent to your email!";
        public const string PasswordChanged = "✅ Your password has been changed successfully!";
        public const string BlockedUser = "You have been blocked by the owner of the website!";
        public const string InvalidInput = "Invalid input data";
        public const string TokenCreationFailed = "Token creation failed";
        public const string OrderItemsMissing = "Order items missing";
    }

    // === Custom Exception Class === //
    public class AppException : Exception
    {
        public int Code { get; set; }
        public string Description { get; set; }

        public static readonly AppException Standard =
     new AppException(HttpCode.InternalServerError, ErrorMessage.SomethingWentWrong);


        public AppException(int code, string description) : base(description)
        {
            Code = code;
            Description = description;
        }
    }
}

using System;

namespace ReversaWEB.Core.Types
{
    public class MemberInput
    {
        public string? MemberType { get; set; }
        public string? MemberStatus { get; set; }
        public string MemberNick { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string MemberPassword { get; set; } = string.Empty;

        public string? MemberAddress { get; set; }
        public string? MemberDesc { get; set; }
        public string MemberEmail { get; set; } = string.Empty;
        public string? MemberImage { get; set; }
        public int? MemberPoints { get; set; }
        public string? AuthProvider { get; set; }
    }

    public class LoginInput
    {
        public string? MemberNick { get; set; }
        public string? MemberPhone { get; set; }
        public string? MemberEmail { get; set; }
        public string? MemberPassword { get; set; }
        public string? AuthProvider { get; set; }
    }

    public class PasswordResetRequestInput
    {
        public string? MemberEmail { get; set; }
        public string? MemberPhone { get; set; }
        public string? MemberNick { get; set; }
    }

    public class PasswordResetInput
    {
        public string? MemberNick { get; set; }
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
    }

    public class MemberUpdateInput
    {
        public string? Id { get; set; }
        public string? MemberStatus { get; set; }
        public string? MemberNick { get; set; }
        public string? MemberPhone { get; set; }
        public string? MemberPassword { get; set; }
        public string? MemberAddress { get; set; }
        public string? MemberDesc { get; set; }
        public string? MemberImage { get; set; }
        public string? MemberEmail { get; set; }
    }

    public class MemberTokenPayload
    {
        public string? Id { get; set; }
        public string? MemberNick { get; set; }
        public string? MemberEmail { get; set; }
        public string? MemberStatus { get; set; }
        public string? MemberType { get; set; }
        public string? MemberImage { get; set; }
        public string? MemberAddress { get; set; }
        public string? MemberPhone { get; set; }
        public int? MemberPoints { get; set; }
    }

    public static class MemberFilter
    {
        public static MemberTokenPayload FilterTokenPayload(ReversaWEB.Models.Member member)
        {
            return new MemberTokenPayload
            {
                Id = member.Id,
                MemberNick = member.MemberNick,
                MemberEmail = member.MemberEmail,
                MemberStatus = member.MemberStatus,
                MemberType = member.MemberType,
                MemberImage = member.MemberImage,
                MemberAddress = member.MemberAddress,
                MemberPhone = member.MemberPhone,
                MemberPoints = member.MemberPoints
            };
        }
    }
}

using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WebApplication1.Enums;

namespace WebApplication1.Models
{
    // ====== Member Entity ====== //
    public class Member
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public MemberType MemberType { get; set; } = MemberType.User;

        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public MemberStatus MemberStatus { get; set; } = MemberStatus.Active;

        public string MemberNick { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string MemberPassword { get; set; } = string.Empty;
        public string? MemberAddress { get; set; }
        public string? MemberDesc { get; set; }
        public string? MemberImage { get; set; }
        public int MemberPoints { get; set; } = 0;
        public string? MemberEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 🔐 Password reset fields
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpires { get; set; }
    }

    // ====== DTOs (used for requests) ====== //

    public class MemberInput
    {
        public MemberType? MemberType { get; set; }
        public MemberStatus? MemberStatus { get; set; }

        public string MemberNick { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string MemberPassword { get; set; } = string.Empty;

        public string? MemberAddress { get; set; }
        public string? MemberDesc { get; set; }
        public string? MemberEmail { get; set; }
        public string? MemberImage { get; set; }
        public int? MemberPoints { get; set; }
    }

    public class LoginInput
    {
        public string MemberNick { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string MemberPassword { get; set; } = string.Empty;
    }

    public class PasswordResetRequestInput
    {
        public string? MemberEmail { get; set; }
        public string? MemberPhone { get; set; }
        public string? MemberNick { get; set; }
    }

    public class PasswordResetInput
    {
        public string MemberNick { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class MemberUpdateInput
    {
        public string Id { get; set; } = string.Empty; // ✅ now a string (Mongo ObjectId)
        public MemberStatus? MemberStatus { get; set; }

        public string? MemberNick { get; set; }
        public string? MemberPhone { get; set; }
        public string? MemberPassword { get; set; }
        public string? MemberAddress { get; set; }
        public string? MemberDesc { get; set; }
        public string? MemberImage { get; set; }
        public string? MemberEmail { get; set; }
    }

    // ====== Token Payload (used if you add JWT later) ====== //
    public class MemberTokenPayload
    {
        public string Id { get; set; } = string.Empty;
        public string MemberNick { get; set; } = string.Empty;
        public string? MemberEmail { get; set; }
        public MemberStatus MemberStatus { get; set; }
        public MemberType MemberType { get; set; }
        public string? MemberImage { get; set; }
        public string? MemberAddress { get; set; }
        public string? MemberPhone { get; set; }
        public int? MemberPoints { get; set; }
    }
}

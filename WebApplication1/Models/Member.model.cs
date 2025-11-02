using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using WebApplication1.Enums;

namespace WebApplication1.Models
{
    [BsonIgnoreExtraElements]
    public class Member
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("memberType")]
        [BsonRepresentation(BsonType.String)]
        public MemberType MemberType { get; set; } = MemberType.User;

        [BsonElement("memberStatus")]
        [BsonRepresentation(BsonType.String)]
        public MemberStatus MemberStatus { get; set; } = MemberStatus.Active;

        [BsonElement("memberNick")]
        [BsonRequired]
        public string MemberNick { get; set; } = string.Empty;

        [BsonElement("memberPhone")]
        [BsonRequired]
        public string MemberPhone { get; set; } = string.Empty;

        [BsonElement("memberPassword")]
        [BsonRequired]
        public string MemberPassword { get; set; } = string.Empty;


        [BsonElement("memberAddress")]
        public string MemberAddress { get; set; } = string.Empty;

        [BsonElement("memberDesc")]
        public string? MemberDesc { get; set; }

        [BsonElement("memberEmail")]
        [BsonRequired]
        public string MemberEmail { get; set; } = string.Empty;

        [BsonElement("memberImage")]
        public string? MemberImage { get; set; }

        [BsonElement("memberPoints")]
        public int MemberPoints { get; set; } = 0;

        [BsonElement("passwordResetToken")]
        public string? PasswordResetToken { get; set; }

        [BsonElement("passwordResetExpires")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PasswordResetExpires { get; set; }


        [BsonElement("authProvider")]
        public string AuthProvider { get; set; } = "LOCAL";

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

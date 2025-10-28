using MongoDB.Driver;
using ReversaWEB.Models;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using System;
using System.Threading.Tasks;
using WebApplication1.Services;
using ReversaWEB.Core.Types;



namespace ReversaWEB.Services
{
    public class MemberService
    {
        private readonly IMongoCollection<Member> _memberCollection;

        public MemberService(MongoDBService db)
        {
            _memberCollection = db.GetCollection<Member>("members");
        }

        //====== SPA ======//

        public async Task<Member> Signup(MemberInput input)
        {
            try
            {
                // Hash password
                input.MemberPassword = BCrypt.Net.BCrypt.HashPassword(input.MemberPassword);

                var newMember = new Member
                {
                    MemberNick = input.MemberNick,
                    MemberPhone = input.MemberPhone,
                    MemberPassword = input.MemberPassword,
                    MemberAddress = input.MemberAddress,
                    MemberDesc = input.MemberDesc,
                    MemberEmail = input.MemberEmail ?? string.Empty,
                    MemberImage = input.MemberImage,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _memberCollection.InsertOneAsync(newMember);
                newMember.MemberPassword = "";
                return newMember;
            }
            catch (MongoWriteException)
            {
                throw new Exception("Used Nickname or Phone already exists");
            }
        }

        public async Task<Member?> Login(LoginInput input)
        {
            var filter = Builders<Member>.Filter.And(
                Builders<Member>.Filter.Or(
                    Builders<Member>.Filter.Eq(m => m.MemberNick, input.MemberNick),
                    Builders<Member>.Filter.Eq(m => m.MemberPhone, input.MemberPhone),
                    Builders<Member>.Filter.Eq(m => m.MemberEmail, input.MemberEmail)
                ),
                Builders<Member>.Filter.Ne(m => m.MemberStatus, "DELETED")
            );

            var member = await _memberCollection.Find(filter).FirstOrDefaultAsync();
            if (member == null)
                throw new Exception("No member found");

            if (member.MemberStatus == "BLOCKED")
                throw new Exception("Blocked user");

            bool isMatch = BCrypt.Net.BCrypt.Verify(input.MemberPassword, member.MemberPassword);
            if (!isMatch)
                throw new Exception("Invalid credentials");

            return member;
        }

        public async Task<Member?> UpdateSelf(string memberId, MemberUpdateInput input)
        {
            var filter = Builders<Member>.Filter.Eq(m => m.Id, memberId);
            var update = Builders<Member>.Update
                .Set(m => m.MemberNick, input.MemberNick)
                .Set(m => m.MemberPhone, input.MemberPhone)
                .Set(m => m.MemberAddress, input.MemberAddress)
                .Set(m => m.MemberDesc, input.MemberDesc)
                .Set(m => m.MemberEmail, input.MemberEmail)
                .Set(m => m.MemberImage, input.MemberImage)
                .Set(m => m.UpdatedAt, DateTime.Now);

            var options = new FindOneAndUpdateOptions<Member>
            {
                ReturnDocument = ReturnDocument.After
            };

            var result = await _memberCollection.FindOneAndUpdateAsync(filter, update, options);
            if (result == null)
                throw new Exception("Update failed");
            return result;
        }

        public async Task<Member?> AddUserPoint(string memberId, int point)
        {
            var filter = Builders<Member>.Filter.And(
                Builders<Member>.Filter.Eq(m => m.Id, memberId),
                Builders<Member>.Filter.Eq(m => m.MemberStatus, "ACTIVE")
            );

            var update = Builders<Member>.Update.Inc(m => m.MemberPoints, point);
            var options = new FindOneAndUpdateOptions<Member>
            {
                ReturnDocument = ReturnDocument.After
            };

            return await _memberCollection.FindOneAndUpdateAsync(filter, update, options);
        }

        //====== PASSWORD RESET ======//
        public async Task<object> RequestPassword(PasswordResetRequestInput input)
        {
            var filter = Builders<Member>.Filter.Eq(m => m.MemberNick, input.MemberNick);
            var member = await _memberCollection.Find(filter).FirstOrDefaultAsync();
            if (member == null)
                throw new Exception("No member found");

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = BitConverter.ToString(tokenBytes).Replace("-", "").ToLower();

            var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLower();
            var expires = DateTimeOffset.Now.AddMinutes(30).ToUnixTimeMilliseconds();

            var update = Builders<Member>.Update
                .Set(m => m.PasswordResetToken, hashedToken)
                .Set(m => m.PasswordResetExpires, expires);

            await _memberCollection.UpdateOneAsync(filter, update);

            // You can implement sendResetPasswordEmail() later here
            Console.WriteLine($"[Email] Reset link for {member.MemberEmail} token={token}");

            return new { message = "Reset link sent" };
        }

        public async Task ResetPassword(string token, string newPassword)
        {
            var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLower();

            var filter = Builders<Member>.Filter.And(
                Builders<Member>.Filter.Eq(m => m.PasswordResetToken, hashedToken),
                Builders<Member>.Filter.Gt(m => m.PasswordResetExpires, DateTimeOffset.Now.ToUnixTimeMilliseconds())
            );

            var member = await _memberCollection.Find(filter).FirstOrDefaultAsync();
            if (member == null)
                throw new Exception("Invalid or expired token");

            member.MemberPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            member.PasswordResetToken = null;
            member.PasswordResetExpires = null;

            var update = Builders<Member>.Update
                .Set(m => m.MemberPassword, member.MemberPassword)
                .Set(m => m.PasswordResetToken, null)
                .Set(m => m.PasswordResetExpires, null);

            await _memberCollection.UpdateOneAsync(filter, update);
        }

        public async Task<Member?> GetById(string id)
        {
            var member = await _memberCollection.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (member == null)
                throw new Exception("No member found");
            return member;
        }
    }
}

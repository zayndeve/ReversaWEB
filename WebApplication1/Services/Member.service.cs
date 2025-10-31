using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using WebApplication1.Models;
using WebApplication1.Enums;
using WebApplication1.Exceptions;
using BCrypt.Net; // BCrypt.Net-Next package
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Services
{
    public class MemberService
    {
        private readonly IMongoCollection<Member> _members;

        public MemberService(MongoDBService mongo)
        {
            _members = mongo.Database.GetCollection<Member>("members");
        }

        // ====== SPA ====== //

        public async Task<Member> SignupAsync(MemberInput input)
        {
            try
            {
                // Hash password
                string salt = BCrypt.Net.BCrypt.GenerateSalt();
                input.MemberPassword = BCrypt.Net.BCrypt.HashPassword(input.MemberPassword, salt);

                var member = new Member
                {
                    MemberNick = input.MemberNick,
                    MemberEmail = input.MemberEmail,
                    MemberPhone = input.MemberPhone,
                    MemberPassword = input.MemberPassword,
                    MemberType = input.MemberType ?? MemberType.User,
                    MemberImage = input.MemberImage,
                    MemberStatus = MemberStatus.Active,
                    MemberPoints = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _members.InsertOneAsync(member);
                member.MemberPassword = string.Empty;
                return member;
            }
            catch (MongoWriteException)
            {
                throw new AppException(HttpCode.BadRequest, ErrorMessage.UsedNickPhone);
            }
            catch (Exception ex)
            {
                throw new AppException(HttpCode.BadRequest, ErrorMessage.CreateFailed + $" ({ex.Message})");
            }
        }

        public async Task<Member> LoginAsync(LoginInput input)
        {
            var filter = Builders<Member>.Filter.And(
                Builders<Member>.Filter.Ne(m => m.MemberStatus, MemberStatus.Deleted),
                Builders<Member>.Filter.Or(
                    Builders<Member>.Filter.Eq(m => m.MemberNick, input.MemberNick),
                    Builders<Member>.Filter.Eq(m => m.MemberPhone, input.MemberPhone),
                    Builders<Member>.Filter.Eq(m => m.MemberEmail, input.MemberEmail)
                )
            );

            var member = await _members.Find(filter).FirstOrDefaultAsync();

            if (member == null)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoMemberFound);

            if (member.MemberStatus == MemberStatus.Blocked)
                throw new AppException(HttpCode.Forbidden, ErrorMessage.BlockedUser);

            bool isMatch = BCrypt.Net.BCrypt.Verify(input.MemberPassword, member.MemberPassword);
            if (!isMatch)
                throw new AppException(HttpCode.Unauthorized, ErrorMessage.WrongPassword);

            return member;
        }

        public async Task<Member> UpdateSelfAsync(string memberId, MemberUpdateInput input)
        {
            var filter = Builders<Member>.Filter.Eq("_id", ObjectId.Parse(memberId));
            var update = Builders<Member>.Update
                .Set(m => m.MemberNick, input.MemberNick)
                .Set(m => m.MemberEmail, input.MemberEmail)
                .Set(m => m.MemberPhone, input.MemberPhone)
                .Set(m => m.MemberImage, input.MemberImage)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _members.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                throw new AppException(HttpCode.NotModified, ErrorMessage.UpdateFailed);

            return await _members.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<Member> AddUserPointAsync(string memberId, int point)
        {
            var filter = Builders<Member>.Filter.And(
                Builders<Member>.Filter.Eq("_id", ObjectId.Parse(memberId)),
                Builders<Member>.Filter.Eq(m => m.MemberStatus, MemberStatus.Active)
            );

            var update = Builders<Member>.Update.Inc(m => m.MemberPoints, point);
            var result = await _members.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Member>
            {
                ReturnDocument = ReturnDocument.After
            });

            if (result == null)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoMemberFound);

            return result;
        }

        // ====== SSR ====== //

        // === Authentication === //
        public async Task<Member> ProcessSignupAsync(MemberInput input)
        {
            var existingAdmin = await _members.Find(m => m.MemberType == MemberType.Admin).FirstOrDefaultAsync();
            if (existingAdmin != null)
                throw new AppException(HttpCode.BadRequest, ErrorMessage.ExistingMemberNick);

            try
            {
                string salt = BCrypt.Net.BCrypt.GenerateSalt();
                input.MemberPassword = BCrypt.Net.BCrypt.HashPassword(input.MemberPassword, salt);

                var member = new Member
                {
                    MemberNick = input.MemberNick,
                    MemberEmail = input.MemberEmail,
                    MemberPhone = input.MemberPhone,
                    MemberPassword = input.MemberPassword,
                    MemberType = MemberType.Admin,
                    MemberImage = input.MemberImage,
                    MemberStatus = MemberStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                await _members.InsertOneAsync(member);
                member.MemberPassword = string.Empty;
                return member;
            }
            catch (Exception ex)
            {
                throw new AppException(HttpCode.BadRequest, ErrorMessage.CreateFailed + $" ({ex.Message})");
            }
        }

        public async Task<Member> ProcessLoginAsync(LoginInput input)
        {
            var filter = Builders<Member>.Filter.Or(
                Builders<Member>.Filter.Eq(m => m.MemberNick, input.MemberNick),
                Builders<Member>.Filter.Eq(m => m.MemberPhone, input.MemberPhone),
                Builders<Member>.Filter.Eq(m => m.MemberEmail, input.MemberEmail)
            );

            var member = await _members.Find(filter).FirstOrDefaultAsync();

            if (member == null)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoMemberFound);

            bool isMatch = BCrypt.Net.BCrypt.Verify(input.MemberPassword, member.MemberPassword);
            if (!isMatch)
                throw new AppException(HttpCode.Unauthorized, ErrorMessage.WrongPassword);

            if (member.MemberType != MemberType.Admin)
                throw new AppException(HttpCode.Unauthorized, ErrorMessage.NotAuthenticated);

            return member;
        }


        // ====== Password Reset ====== //

        // ====== Password Reset ====== //
        public async Task<string> RequestPasswordAsync(PasswordResetRequestInput input)
        {
            var member = await _members
                .Find(m => m.MemberNick == input.MemberNick)
                .FirstOrDefaultAsync();

            if (member == null)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoMemberFound);

            // Generate token
            var tokenBytes = new byte[32];
            RandomNumberGenerator.Fill(tokenBytes);
            var token = Convert.ToHexString(tokenBytes);

            using var sha = SHA256.Create();
            var hashedToken = Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes(token))
            );
            var expires = DateTime.UtcNow.AddMinutes(30);

            var update = Builders<Member>.Update
                .Set(m => m.PasswordResetToken, hashedToken)
                .Set(m => m.PasswordResetExpires, expires);

            await _members.UpdateOneAsync(
                Builders<Member>.Filter.Eq("_id", member.Id),
                update
            );

            // TODO: integrate email sender (placeholder)
            Console.WriteLine($"📧 Reset link token for {member.MemberNick}: {token}");

            return ErrorMessage.ResetLinkSent;
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            using var sha = SHA256.Create();
            var hashedToken = Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes(token))
            );

            var filter = Builders<Member>.Filter.And(
                Builders<Member>.Filter.Eq(m => m.PasswordResetToken, hashedToken),
                Builders<Member>.Filter.Gt(m => m.PasswordResetExpires, DateTime.UtcNow)
            );

            var member = await _members.Find(filter).FirstOrDefaultAsync();

            if (member == null)
                throw new AppException(HttpCode.BadRequest, ErrorMessage.InvalidOrExpiredToken);

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            var update = Builders<Member>.Update
                .Set(m => m.MemberPassword, hashedPassword)
                .Set(m => m.PasswordResetToken, null)
                .Set(m => m.PasswordResetExpires, null);

            await _members.UpdateOneAsync(
                Builders<Member>.Filter.Eq("_id", member.Id),
                update
            );
        }

        // ====== Utility ====== //
        public async Task<Member> GetByIdAsync(string id)
        {
            var filter = Builders<Member>.Filter.Eq("_id", ObjectId.Parse(id));
            var member = await _members.Find(filter).FirstOrDefaultAsync();

            if (member == null)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoMemberFound);

            return member;
        }

        // ====== Admin Panel ====== //
        public async Task<Member> UpdateAdminDataAsync(string memberId, MemberUpdateInput input)
        {
            var filter = Builders<Member>.Filter.Eq("_id", ObjectId.Parse(memberId));
            var found = await _members.Find(filter).FirstOrDefaultAsync();

            if (found == null)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoMemberFound);

            var update = Builders<Member>.Update
                .Set(m => m.MemberNick, input.MemberNick ?? found.MemberNick)
                .Set(m => m.MemberEmail, input.MemberEmail ?? found.MemberEmail)
                .Set(m => m.MemberPhone, input.MemberPhone ?? found.MemberPhone)
                .Set(m => m.MemberImage, input.MemberImage ?? found.MemberImage);

            var result = await _members.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
                throw new AppException(HttpCode.NotModified, ErrorMessage.UpdateFailed);

            return await _members.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<Member> UpdateChosenMemberAsync(MemberUpdateInput input)
        {
            var filter = Builders<Member>.Filter.Eq("_id", ObjectId.Parse(input.Id));
            var found = await _members.Find(filter).FirstOrDefaultAsync();

            if (found == null)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoMemberFound);

            if (input.MemberStatus == MemberStatus.Deleted)
            {
                await _members.DeleteOneAsync(filter);
                return new Member { MemberNick = "Deleted Member" };
            }

            var update = Builders<Member>.Update
                .Set(m => m.MemberNick, input.MemberNick ?? found.MemberNick)
                .Set(m => m.MemberEmail, input.MemberEmail ?? found.MemberEmail)
                .Set(m => m.MemberPhone, input.MemberPhone ?? found.MemberPhone)
                .Set(m => m.MemberStatus, input.MemberStatus ?? found.MemberStatus);

            await _members.UpdateOneAsync(filter, update);

            return await _members.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<Member>> GetUsersAsync()
        {
            var filter = Builders<Member>.Filter.Eq(m => m.MemberType, MemberType.User);
            var result = await _members.Find(filter).ToListAsync();

            if (result == null || result.Count == 0)
                throw new AppException(HttpCode.NotFound, ErrorMessage.NoDataFound);

            return result;
        }
    }
}

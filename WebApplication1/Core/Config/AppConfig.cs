using System;

namespace ReversaWEB.Core.Config
{
    public static class AppConfig
    {
        // 🔐 Secret key for JWT — can be set via environment variable or fallback
        public static string SecretToken =>
            Environment.GetEnvironmentVariable("SECRET_TOKEN") ?? "super_secret_fallback_key";

        // ⏱ Authentication duration in hours
        public static int AuthTimer =>
            int.TryParse(Environment.GetEnvironmentVariable("AUTH_TIMER"), out var hours)
                ? hours
                : 12; // default 12h

        // 💾 Database connection (optional central place)
        public static string MongoHost =>
            Environment.GetEnvironmentVariable("MONGO_HOST") ?? "webp.flykorea.kr:27017";

        public static string MongoUser =>
            Environment.GetEnvironmentVariable("MONGO_USER") ?? "w136138";

        public static string MongoPassword =>
            Environment.GetEnvironmentVariable("MONGO_PASSWORD") ?? "wp@aV5n$8rJ";

        public static string MongoDatabase =>
            Environment.GetEnvironmentVariable("MONGO_DB") ?? "w136138DB";
    }
}

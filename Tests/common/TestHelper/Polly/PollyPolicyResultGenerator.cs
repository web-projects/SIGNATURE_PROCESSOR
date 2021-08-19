using System;
using Polly;

namespace TestHelper.Polly
{
    public static class PollyPolicyResultGenerator
    {
        public static PolicyResult<T> GetSuccessfulPolicy<T>(T defaultValue = default(T)) => PolicyResult<T>.Successful(defaultValue, new Context(Guid.NewGuid().ToString()));

        public static PolicyResult<T> GetFailurePolicy<T>(Exception exception = null) => PolicyResult<T>.Failure(
            exception ?? new Exception("Unable to execute your policy successfully!"),
            ExceptionType.Unhandled,
            new Context(Guid.NewGuid().ToString()));
    }
}

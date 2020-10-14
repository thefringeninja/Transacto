using System;

namespace SomeCompany {
    /// <summary>
    /// The result of a test specification verification.
    /// </summary>
    public struct VerificationResult : IEquatable<VerificationResult> {
        public bool Equals(VerificationResult other) => _state == other._state && Message == other.Message;
        public override bool Equals(object obj) => obj is VerificationResult other && Equals(other);
        public static bool operator ==(VerificationResult left, VerificationResult right) => left.Equals(right);
        public static bool operator !=(VerificationResult left, VerificationResult right) => !left.Equals(right);

        public override int GetHashCode() {
            unchecked {
                return ((int)_state * 397) ^ (Message != null ? Message.GetHashCode() : 0);
            }
        }


        private readonly VerificationResultState _state;

        private VerificationResult(VerificationResultState state, string message) {
            _state = state;
            Message = message;
        }

        /// <summary>
        /// Returns <c>true</c> if the test specification verification passed.
        /// </summary>
        public bool Passed => _state == VerificationResultState.Passed;

        /// <summary>
        /// Returns <c>false</c> if the test specification verification failed.
        /// </summary>
        public bool Failed => _state == VerificationResultState.Failed;

        /// <summary>
        /// Returns a message stating why the test specification verification passed or failed, or an empty if it wasn't specified.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Indicates that the test specification verification passed.
        /// </summary>
        /// <param name="message">An optional message stating why test specification verification passed.</param>
        /// <returns>A passed <see cref="VerificationResult"/>.</returns>
        public static VerificationResult Pass(string message = "") {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return new VerificationResult(VerificationResultState.Passed, message);
        }

        /// <summary>
        /// Indicates that the test specification verification failed.
        /// </summary>
        /// <param name="message">An optional message stating why test specification verification failed.</param>
        /// <returns>A failed <see cref="VerificationResult"/>.</returns>
        public static VerificationResult Fail(string message = "") {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return new VerificationResult(VerificationResultState.Failed, message);
        }
    }
}

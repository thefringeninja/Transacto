using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Projac.Sql;

namespace SomeCompany {
    /// <summary>
    /// Represent a scenario that tests a set of <see cref="SqlProjectionHandler"/>s.
    /// </summary>
    public class NpgsqlProjectionScenario {
        public SqlProjectionHandlerResolver Resolver { get; }
        public object[] Messages { get; }
        public Table[] Tables { get; }
        public IEnumerable<string> Schemas => Tables.Select(x => x.Schema).Distinct();

        public NpgsqlProjectionScenario(SqlProjection projection) :
            this(Resolve.WhenEqualToHandlerMessageType(projection)) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlProjectionScenario"/> class.
        /// </summary>
        /// <param name="resolver">The projection handler resolver.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="resolver"/> is <c>null</c>.</exception>
        public NpgsqlProjectionScenario(SqlProjectionHandlerResolver resolver) {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            Resolver = resolver;
            Messages = Array.Empty<object>();
            Tables = Array.Empty<Table>();
        }

        private NpgsqlProjectionScenario(SqlProjectionHandlerResolver resolver, object[] messages, Table[] tables) {
            Resolver = resolver;
            Messages = messages;
            Tables = tables;
        }

        /// <summary>
        /// Given the following specified messages to project.
        /// </summary>
        /// <param name="messages">The messages to project.</param>
        /// <returns>A new <see cref="NpgsqlProjectionScenario"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="messages"/> is <c>null</c>.</exception>
        public NpgsqlProjectionScenario Given(params object[] messages) {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            return new NpgsqlProjectionScenario(
                Resolver,
                Messages.Concat(messages).ToArray(),
                Tables);
        }

        /// <summary>
        /// Given the following specified messages to project.
        /// </summary>
        /// <param name="messages">The messages to project.</param>
        /// <returns>A new <see cref="NpgsqlProjectionScenario"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="messages"/> is <c>null</c>.</exception>
        public NpgsqlProjectionScenario Given(IEnumerable<object> messages) {
            return new NpgsqlProjectionScenario(
                Resolver,
                Messages.Concat(messages).ToArray(),
                Tables);
        }

        public NpgsqlProjectionScenario Then<T>(string schema, string tableName, params T[] documents) where T: class =>
            new NpgsqlProjectionScenario(Resolver, Messages,
                Tables.Concat(new[] {new Table<T>(schema, tableName, documents)}).ToArray());

        /// <summary>
        /// Builds a test specification using the specified verification method.
        /// </summary>
        /// <param name="verification">The verification method.</param>
        /// <returns>A <see cref="ProjectionTestSpecification"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="verification"/> is <c>null</c>.</exception>
        public ProjectionTestSpecification
            Verify(Func<NpgsqlConnection, Task<VerificationResult>> verification) {
            if (verification == null)
                throw new ArgumentNullException(nameof(verification));
            return new ProjectionTestSpecification(
                Resolver,
                Messages,
                Tables,
                (connection, token) => verification(connection));
        }

        /// <summary>
        /// Builds a test specification using the specified verification method that takes a cancellation token.
        /// </summary>
        /// <param name="verification">The verification method.</param>
        /// <returns>A <see cref="ProjectionTestSpecification"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="verification"/> is <c>null</c>.</exception>
        public ProjectionTestSpecification Verify(
            Func<NpgsqlConnection, CancellationToken, Task<VerificationResult>> verification) {
            if (verification == null)
                throw new ArgumentNullException(nameof(verification));
            return new ProjectionTestSpecification(
                Resolver,
                Messages,
                Tables,
                verification);
        }
    }
}

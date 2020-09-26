using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Projac.Sql;

namespace SomeCompany {
    /// <summary>
    /// Represent a <see cref="SqlProjection"/> test specification.
    /// </summary>
    public class ProjectionTestSpecification {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionTestSpecification"/> class.
        /// </summary>
        /// <param name="resolver">The projection handler resolver.</param>
        /// <param name="messages">The messages to project.</param>
        /// <param name="verification">The verification method.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when
        /// <paramref name="resolver"/>
        /// or
        /// <paramref name="messages"/>
        /// or
        /// <paramref name="verification"/> is null.
        /// </exception>
        public ProjectionTestSpecification(SqlProjectionHandlerResolver resolver, object[] messages,
            Table[] tables,
            Func<NpgsqlConnection, CancellationToken, Task<VerificationResult>> verification) {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            if (verification == null) throw new ArgumentNullException(nameof(verification));
            Resolver = resolver;
            Messages = messages;
            Tables = tables;
            Verification = verification;
        }

        /// <summary>
        /// Gets the projection handler resolver.
        /// </summary>
        /// <value>
        /// The projection handler resolver.
        /// </value>
        public SqlProjectionHandlerResolver Resolver { get; }

        /// <summary>
        /// Gets the messages to project.
        /// </summary>
        /// <value>
        /// The messages to project.
        /// </value>
        public object[] Messages { get; }

        public Table[] Tables { get; }

        /// <summary>
        /// Gets the verification method.
        /// </summary>
        /// <value>
        /// The verification method.
        /// </value>
        public Func<NpgsqlConnection, CancellationToken, Task<VerificationResult>> Verification { get; }
    }
}

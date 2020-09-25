using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using Transacto.Testing.Comparers;
using Xunit.Sdk;

namespace Transacto.Testing {
    internal static class XUnitExtensions {
        static IExceptionComparer CreateExceptionComparer() =>
            new CompareNetObjectsBasedExceptionComparer(new CompareLogic(new ComparisonConfig {
                MembersToIgnore = {nameof(Exception.Source), nameof(Exception.StackTrace), nameof(Exception.TargetSite)}
            }));

        public static Task Assert(this IEventCentricTestSpecificationBuilder builder, object handler,
            IFactRecorder factRecorder) =>
            builder.Assert(new CompareNetObjectsBasedFactComparer(new CompareLogic()), handler, factRecorder);

        public static Task Assert(this IExceptionCentricTestSpecificationBuilder builder, object handler,
            IFactRecorder factRecorder) =>
            builder.Assert(CreateExceptionComparer(), handler, factRecorder);

        /// <summary>
        /// Asserts that the specification is met.
        /// </summary>
        /// <param name="builder">The specification builder.</param>
        /// <param name="comparer">The event comparer.</param>
        public static async Task Assert(this IEventCentricTestSpecificationBuilder builder,
            IFactComparer comparer, object handler, IFactRecorder factRecorder) {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (factRecorder == null) throw new ArgumentNullException(nameof(factRecorder));
            var specification = builder.Build();
            var runner = new EventCentricTestSpecificationRunner(comparer, handler, factRecorder);
            var result = await runner.Run(specification);

            if (!result.Failed) return;
            if (result.ButException.HasValue) {
                await using var writer = new StringWriter();
                writer.WriteLine("  Expected: {0} event(s),", result.Specification.Thens.Length);
                writer.WriteLine("  But was:  {0}", result.ButException.Value);

                throw new XunitException(writer.ToString());
            }

            if (!result.ButEvents.HasValue) return;

            if (result.ButEvents.Value.Length != result.Specification.Thens.Length) {
                await using var writer = new StringWriter();
                writer.WriteLine("  Expected: {0} event(s) ({1}),",
                    result.Specification.Thens.Length,
                    string.Join(",", result.Specification.Thens.Select(_ => _.Event.GetType().Name).ToArray()));
                writer.WriteLine("  But was:  {0} event(s) ({1})",
                    result.ButEvents.Value.Length,
                    string.Join(",", result.ButEvents.Value.Select(_ => _.Event.GetType().Name).ToArray()));

                throw new XunitException(writer.ToString());
            }

            await using (var writer = new StringWriter()) {
                writer.WriteLine("  Expected: {0} event(s) ({1}),",
                    result.Specification.Thens.Length,
                    string.Join(",", result.Specification.Thens.Select(_ => _.Event.GetType().Name).ToArray()));
                writer.WriteLine("  But found the following differences:");
                foreach (var difference in
                    result.Specification.Thens.Zip(result.ButEvents.Value,
                            (expected, actual) => new Tuple<Fact, Fact>(expected, actual))
                        .SelectMany(_ => comparer.Compare(_.Item1, _.Item2))) {
                    writer.WriteLine("    {0}", difference.Message);
                }

                throw new XunitException(writer.ToString());
            }
        }

        public static async Task Assert(this IExceptionCentricTestSpecificationBuilder builder,
            IExceptionComparer comparer, object handler, IFactRecorder factRecorder) {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (factRecorder == null) throw new ArgumentNullException(nameof(factRecorder));
            var specification = builder.Build();
            var runner = new ExceptionCentricTestSpecificationRunner(comparer, handler, factRecorder);
            var result = await runner.Run(specification);

            if (!result.Failed) return;
            if (result.ButException.HasValue) {
                await using var writer = new StringWriter();
                writer.WriteLine("  Expected: {0},", result.Specification.Throws);
                writer.WriteLine("  But was:  {0}", result.ButException.Value);

                throw new XunitException(writer.ToString());
            }

            if (!result.ButEvents.HasValue) return;

            await using (var writer = new StringWriter()) {
                writer.WriteLine("  Expected: {0},", result.Specification.Throws);
                writer.WriteLine("  But was:  {0} event(s) ({1})",
                    result.ButEvents.Value.Length,
                    string.Join(",", result.ButEvents.Value.Select(_ => _.GetType().Name).ToArray()));

                throw new XunitException(writer.ToString());
            }
        }
    }
}

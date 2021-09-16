using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ntbs_service.DataAccess;
using ntbs_service.Jobs;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using Xunit;

namespace ntbs_service_unit_tests.Jobs
{
    public class ReportingDataRefreshJobTest
    {
        private readonly Mock<IExternalStoredProcedureRepository> _externalStoredProcedureRepository;
        private readonly ReportingDataRefreshJob _reportingDataRefreshJob;

        public ReportingDataRefreshJobTest()
        {
            _externalStoredProcedureRepository = new Mock<IExternalStoredProcedureRepository>();
            _reportingDataRefreshJob = new ReportingDataRefreshJob(_externalStoredProcedureRepository.Object);
        }

        [Fact]
        public async Task Run_CallsPopulateStoredProcedures()
        {
            // Arrange
            SetupRepositoryMethods();

            // Act
            await _reportingDataRefreshJob.Run(null);

            // Assert
            AssertRepositoryMethodsCalled();
        }

        [Fact]
        public async Task Run_LogsSuccess()
        {
            // Arrange
            SetupRepositoryMethods();

            Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
            using (TestCorrelator.CreateContext())
            {
                // Act
                await _reportingDataRefreshJob.Run(null);

                // Assert
                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

                Assert.DoesNotContain(logEvents, logEvent => logEvent.Level > LogEventLevel.Information);
                AssertContainsInfoMessages(logEvents, new[]
                {
                    "Starting reporting data refresh job",
                    "Starting specimen-matching uspGenerate",
                    "Starting reporting uspGenerate",
                    "Starting migration uspGenerate",
                    "Finishing reporting data refresh job"
                });
                AssertDoesNotContainInfoMessages(logEvents, new[] {"Error occured during reporting data refresh job"});
            }
        }

        [Fact]
        public async Task Run_ThrowsOnSpecimenMatchingStoredProcedureMessage()
        {
            // Arrange
            List<dynamic> sqlResult = new List<object> { new { ErrorMessage = "Divide by zero error encountered." } };
            const string serialisedResult = "[{\"ErrorMessage\":\"Divide by zero error encountered.\"}]";

            SetupRepositoryMethods(specimenMatchingReturnResult: sqlResult.AsEnumerable());

            Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
            using (TestCorrelator.CreateContext())
            {
                // Act
                var exception = await Assert.ThrowsAsync<ApplicationException>(async () =>
                    await _reportingDataRefreshJob.Run(null));

                // Assert
                Assert.Equal("Stored procedure did not execute successfully as result has messages, check the logs",
                    exception.Message);
                AssertRepositoryMethodsCalled(reportingCalled: false, migrationCalled: false);

                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

                AssertContainsInfoMessages(logEvents, new[]
                {
                    "Starting specimen-matching uspGenerate",
                    $"Result: {serialisedResult}"
                });
                Assert.Contains(logEvents,
                    logEvent => logEvent.Level == LogEventLevel.Error
                                && logEvent.RenderMessage() == "Error occured during reporting data refresh job");
                AssertDoesNotContainInfoMessages(logEvents, new[]
                {
                    "Starting reporting uspGenerate", "Starting migration uspGenerate",
                    "Finishing reporting data refresh job"
                });
            }
        }

        [Fact]
        public async Task Run_ThrowsOnReportingStoredProcedureMessage()
        {
            // Arrange
            List<dynamic> sqlResult = new List<object> { new { ErrorMessage = "Divide by zero error encountered." } };
            const string serialisedResult = "[{\"ErrorMessage\":\"Divide by zero error encountered.\"}]";

            SetupRepositoryMethods(reportingReturnResult: sqlResult.AsEnumerable());

            Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
            using (TestCorrelator.CreateContext())
            {
                // Act
                var exception = await Assert.ThrowsAsync<ApplicationException>(async () =>
                    await _reportingDataRefreshJob.Run(null));

                // Assert
                Assert.Equal("Stored procedure did not execute successfully as result has messages, check the logs",
                    exception.Message);
                AssertRepositoryMethodsCalled();

                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

                AssertContainsInfoMessages(logEvents, new[]
                {
                    "Starting specimen-matching uspGenerate", "Starting reporting uspGenerate",
                    $"Result: {serialisedResult}"
                });
                Assert.Contains(logEvents,
                    logEvent => logEvent.Level == LogEventLevel.Error
                                && logEvent.RenderMessage() == "Error occured during reporting data refresh job");
                AssertDoesNotContainInfoMessages(logEvents, new[]
                {
                    "Finishing reporting data refresh job"
                });
            }
        }

        [Fact]
        public async Task Run_ThrowsOnMigrationStoredProcedureMessage()
        {
            // Arrange
            List<dynamic> sqlResult = new List<object> { new { ErrorMessage = "Divide by zero error encountered." } };
            const string serialisedResult = "[{\"ErrorMessage\":\"Divide by zero error encountered.\"}]";

            SetupRepositoryMethods(migrationReturnResult: sqlResult.AsEnumerable());

            Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
            using (TestCorrelator.CreateContext())
            {
                // Act
                var exception = await Assert.ThrowsAsync<ApplicationException>(async () =>
                    await _reportingDataRefreshJob.Run(null));

                // Assert
                Assert.Equal("Stored procedure did not execute successfully as result has messages, check the logs",
                    exception.Message);
                AssertRepositoryMethodsCalled(reportingCalled: false);

                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

                AssertContainsInfoMessages(logEvents, new[]
                {
                    "Starting specimen-matching uspGenerate",
                    "Starting migration uspGenerate",
                    $"Result: {serialisedResult}"
                });
                Assert.Contains(logEvents,
                    logEvent => logEvent.Level == LogEventLevel.Error
                                && logEvent.RenderMessage() == "Error occured during reporting data refresh job");
                AssertDoesNotContainInfoMessages(logEvents, new[] { "Starting reporting uspGenerate", "Finishing reporting data refresh job" });
            }
        }

        [Fact]
        public async Task Run_ThrowsOnSpecimenMatchingRepositoryException()
        {
            // Arrange
            var expectedException = new ApplicationException("Connection was unsuccessful");
            _externalStoredProcedureRepository
                .Setup(r => r.ExecuteSpecimenMatchingGenerateStoredProcedure())
                .Throws(expectedException);

            Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
            using (TestCorrelator.CreateContext())
            {
                // Act
                var actualException = await Assert.ThrowsAsync<ApplicationException>(async () =>
                    await _reportingDataRefreshJob.Run(null));

                // Assert
                Assert.Equal(expectedException, actualException);
                AssertRepositoryMethodsCalled(reportingCalled: false, migrationCalled: false);

                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

                AssertContainsInfoMessages(logEvents, new[]
                {
                    "Starting specimen-matching uspGenerate"
                });
                Assert.Contains(logEvents,
                    logEvent => logEvent.Level == LogEventLevel.Error
                                && logEvent.RenderMessage() == "Error occured during reporting data refresh job"
                                && logEvent.Exception == expectedException);
                AssertDoesNotContainInfoMessages(logEvents, new[]
                {
                    "Starting reporting uspGenerate", "Starting migration uspGenerate",
                    "Finishing reporting data refresh job"
                });
            }
        }

        [Fact]
        public async Task Run_ThrowsOnReportingRepositoryException()
        {
            // Arrange
            SetupRepositoryMethods();

            var expectedException = new ApplicationException("Connection was unsuccessful");
            // Override migration with an exception
            _externalStoredProcedureRepository
                .Setup(r => r.ExecuteReportingGenerateStoredProcedure())
                .Throws(expectedException);

            Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
            using (TestCorrelator.CreateContext())
            {
                // Act
                var actualException = await Assert.ThrowsAsync<ApplicationException>(async () =>
                    await _reportingDataRefreshJob.Run(null));

                // Assert
                Assert.Equal(expectedException, actualException);
                AssertRepositoryMethodsCalled();

                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

                AssertContainsInfoMessages(logEvents, new[]
                {
                    "Starting specimen-matching uspGenerate", "Starting migration uspGenerate", "Starting reporting uspGenerate"
                });
                Assert.Contains(logEvents,
                    logEvent => logEvent.Level == LogEventLevel.Error
                                && logEvent.RenderMessage() == "Error occured during reporting data refresh job"
                                && logEvent.Exception == expectedException);
                AssertDoesNotContainInfoMessages(logEvents, new[]
                {
                    "Finishing reporting data refresh job"
                });
            }
        }

        [Fact]
        public async Task Run_ThrowsOnMigrationRepositoryException()
        {
            // Arrange
            SetupRepositoryMethods();

            var expectedException = new ApplicationException("Connection was unsuccessful");
            // Override migration with an exception
            _externalStoredProcedureRepository
                .Setup(r => r.ExecuteMigrationGenerateStoredProcedure())
                .Throws(expectedException);

            Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
            using (TestCorrelator.CreateContext())
            {
                // Act
                var actualException = await Assert.ThrowsAsync<ApplicationException>(async () =>
                    await _reportingDataRefreshJob.Run(null));

                // Assert
                Assert.Equal(expectedException, actualException);
                AssertRepositoryMethodsCalled(reportingCalled: false);

                var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();

                AssertContainsInfoMessages(logEvents, new[]
                {
                    "Starting specimen-matching uspGenerate",
                    "Starting migration uspGenerate"
                });
                Assert.Contains(logEvents,
                    logEvent => logEvent.Level == LogEventLevel.Error
                                && logEvent.RenderMessage() == "Error occured during reporting data refresh job"
                                && logEvent.Exception == expectedException);
                AssertDoesNotContainInfoMessages(logEvents, new[] { "Starting reporting uspGenerate", "Finishing reporting data refresh job" });
            }
        }

        private void SetupRepositoryMethods(IEnumerable<dynamic> specimenMatchingReturnResult = null,
            IEnumerable<dynamic> reportingReturnResult = null,
            IEnumerable<dynamic> migrationReturnResult = null)
        {
            var successfulResult = Enumerable.Empty<dynamic>();

            _externalStoredProcedureRepository
                .Setup(r => r.ExecuteSpecimenMatchingGenerateStoredProcedure())
                .Returns(Task.FromResult(specimenMatchingReturnResult ?? successfulResult));
            _externalStoredProcedureRepository
                .Setup(r => r.ExecuteReportingGenerateStoredProcedure())
                .Returns(Task.FromResult(reportingReturnResult ?? successfulResult));
            _externalStoredProcedureRepository
                .Setup(r => r.ExecuteMigrationGenerateStoredProcedure())
                .Returns(Task.FromResult(migrationReturnResult ?? successfulResult));
        }

        private void AssertRepositoryMethodsCalled(bool specimenMatchingCalled = true,
            bool reportingCalled = true,
            bool migrationCalled = true)
        {
            var timesSpecimenMatchingCalled = specimenMatchingCalled ? Times.Once() : Times.Never();
            _externalStoredProcedureRepository
                .Verify(r => r.ExecuteSpecimenMatchingGenerateStoredProcedure(), timesSpecimenMatchingCalled);


            var timesReportingCalled = reportingCalled ? Times.Once() : Times.Never();
            _externalStoredProcedureRepository
                .Verify(r => r.ExecuteReportingGenerateStoredProcedure(), timesReportingCalled);


            var timesMigrationCalled = migrationCalled ? Times.Once() : Times.Never();
            _externalStoredProcedureRepository
                .Verify(r => r.ExecuteMigrationGenerateStoredProcedure(), timesMigrationCalled);

            _externalStoredProcedureRepository.VerifyNoOtherCalls();
        }

        private static void AssertContainsInfoMessages(IReadOnlyCollection<LogEvent> logEvents,
            IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                Assert.Contains(logEvents, logEvent => logEvent.Level == LogEventLevel.Information
                                                       && logEvent.RenderMessage() == message);
            }
        }

        private static void AssertDoesNotContainInfoMessages(IReadOnlyCollection<LogEvent> logEvents,
            IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                Assert.DoesNotContain(logEvents, logEvent => logEvent.Level == LogEventLevel.Information
                                                             && logEvent.RenderMessage() == message);
            }
        }
    }
}

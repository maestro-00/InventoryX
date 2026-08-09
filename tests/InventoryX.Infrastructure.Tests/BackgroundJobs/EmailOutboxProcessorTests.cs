using FluentAssertions;
using InventoryX.Application.Services.IServices;
using InventoryX.Common.Tests;
using InventoryX.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InventoryX.Infrastructure.Tests.BackgroundJobs;

public sealed class EmailOutboxProcessorTests : IDisposable
{
    private sealed class RecordingEmailSender : IEmailSender
    {
        public int Attempts { get; private set; }
        public bool FailNext { get; set; }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Attempts++;
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("Temporary delivery failure.");
            }
            return Task.CompletedTask;
        }
    }

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly TestDb _db;

    public EmailOutboxProcessorTests()
    {
        _db = new TestDb(_tenantId, "worker");
    }

    [Fact]
    public async Task Failed_send_retries_after_bounded_backoff_and_success_is_not_resent()
    {
        var now = new DateTime(2026, 8, 9, 8, 0, 0, DateTimeKind.Utc);
        await using var context = _db.CreateContext();
        context.OutboxMessages.Add(EmailOutbox.Html(
            _tenantId, "digest:one", "owner@example.com", "Digest", "<p>Body</p>", now));
        await context.SaveChangesAsync();
        var email = new RecordingEmailSender { FailNext = true };
        var processor = Processor(context, email);

        (await processor.ProcessBatchAsync("instance-a", now, CancellationToken.None)).Should().Be(1);
        var failed = await context.OutboxMessages.SingleAsync();
        failed.AttemptCount.Should().Be(1);
        failed.ProcessedAt.Should().BeNull();
        failed.LastError.Should().Contain("Temporary delivery failure");
        failed.AvailableAt.Should().Be(now.AddMinutes(2));

        (await processor.ProcessBatchAsync("instance-b", now.AddMinutes(1), CancellationToken.None)).Should().Be(0);
        (await processor.ProcessBatchAsync("instance-b", now.AddMinutes(2), CancellationToken.None)).Should().Be(1);
        (await processor.ProcessBatchAsync("instance-a", now.AddHours(1), CancellationToken.None)).Should().Be(0);

        email.Attempts.Should().Be(2);
        (await context.OutboxMessages.SingleAsync()).ProcessedAt.Should().Be(now.AddMinutes(2));
        EmailOutboxProcessor.Backoff(100).Should().Be(TimeSpan.FromMinutes(60));
    }

    [Fact]
    public async Task Claim_lease_prevents_competing_instance_and_expiry_recovers_crashed_claim()
    {
        var now = new DateTime(2026, 8, 9, 8, 0, 0, DateTimeKind.Utc);
        await using (var seed = _db.CreateContext())
        {
            seed.OutboxMessages.Add(EmailOutbox.Html(
                _tenantId, "digest:lease", "owner@example.com", "Digest", "<p>Body</p>", now));
            await seed.SaveChangesAsync();
        }
        await using var contextA = _db.CreateContext();
        await using var contextB = _db.CreateContext();
        var processorA = Processor(contextA, new RecordingEmailSender());
        var processorB = Processor(contextB, new RecordingEmailSender());

        var claimedA = await processorA.ClaimNextAsync("instance-a", now, CancellationToken.None);
        var claimedB = await processorB.ClaimNextAsync("instance-b", now, CancellationToken.None);

        claimedA.Should().NotBeNull();
        claimedA!.ClaimedBy.Should().Be("instance-a");
        claimedB.Should().BeNull();

        var recovered = await processorB.ClaimNextAsync("instance-b", now.AddMinutes(5), CancellationToken.None);
        recovered.Should().NotBeNull();
        recovered!.Id.Should().Be(claimedA.Id);
        recovered.ClaimedBy.Should().Be("instance-b");
    }

    [Fact]
    public async Task Duplicate_idempotency_key_is_rejected_per_tenant()
    {
        var now = DateTime.UtcNow;
        await using var context = _db.CreateContext();
        context.OutboxMessages.Add(EmailOutbox.Html(
            _tenantId, "digest:duplicate", "one@example.com", "Digest", "<p>One</p>", now));
        context.OutboxMessages.Add(EmailOutbox.Html(
            _tenantId, "digest:duplicate", "two@example.com", "Digest", "<p>Two</p>", now));

        var action = () => context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    private static EmailOutboxProcessor Processor(
        InventoryX.Infrastructure.Data.AppDbContext context,
        IEmailSender emailSender) => new(
        context,
        emailSender,
        Mock.Of<IAttachmentEmailSender>(),
        NullLogger<EmailOutboxProcessor>.Instance);

    public void Dispose() => _db.Dispose();
}

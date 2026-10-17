using SupportDashboard.Core.Interfaces;
using SupportDashboard.Core.Models;

namespace SupportDashboard.Infrastructure.InMemory;

public abstract class InMemoryRunRepository<TRun>(IEnumerable<TRun> seed) : IRunRepository<TRun>
    where TRun : class
{
    private readonly IReadOnlyList<TRun> _runs = seed.ToList();

    public Task<IReadOnlyList<TRun>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(_runs);
}

public sealed class InMemoryFunctionExecutionRepository(TimeProvider time)
    : InMemoryRunRepository<FunctionExecution>(Seed(time.GetUtcNow()))
{
    private static IEnumerable<FunctionExecution> Seed(DateTimeOffset now)
    {
        FunctionExecution Make(string fn, string app, RunStatus status, string trigger, TimeSpan ago, TimeSpan? duration, string? error = null) =>
            new() { FunctionName = fn, AppName = app, Status = status, Trigger = trigger, StartUtc = now - ago, Duration = duration, ErrorMessage = error };

        return
        [
            Make("ValidatePaymentWebhook", "fn-payments-prod", RunStatus.Running, "HTTP", TimeSpan.FromMinutes(1), null),
            Make("ProcessOrderQueue", "fn-orders-prod", RunStatus.Succeeded, "Queue", TimeSpan.FromMinutes(2), TimeSpan.FromMilliseconds(420)),
            Make("SyncInventoryLevels", "fn-inventory-prod", RunStatus.Failed, "Timer", TimeSpan.FromMinutes(14), TimeSpan.FromSeconds(3.2), "SQL timeout connecting to InventoryDB"),
            Make("SendInvoiceEmail", "fn-notify-prod", RunStatus.Succeeded, "HTTP", TimeSpan.FromMinutes(18), TimeSpan.FromMilliseconds(610)),
            Make("GenerateDailyReport", "fn-reporting-prod", RunStatus.Succeeded, "Timer", TimeSpan.FromMinutes(32), TimeSpan.FromSeconds(5.6)),
            Make("ArchiveOldLogs", "fn-ops-prod", RunStatus.Succeeded, "Timer", TimeSpan.FromMinutes(46), TimeSpan.FromSeconds(2.1)),
            Make("ResizeProductImages", "fn-media-prod", RunStatus.Failed, "Blob", TimeSpan.FromMinutes(64), TimeSpan.FromMilliseconds(890), "Unsupported image format"),
            Make("NotifyLowStock", "fn-inventory-prod", RunStatus.Succeeded, "Queue", TimeSpan.FromMinutes(71), TimeSpan.FromMilliseconds(340)),
        ];
    }
}

public sealed class InMemoryPipelineRunRepository(TimeProvider time)
    : InMemoryRunRepository<PipelineRun>(Seed(time.GetUtcNow()))
{
    private static IEnumerable<PipelineRun> Seed(DateTimeOffset now)
    {
        PipelineRun Make(string name, string runId, RunStatus status, string trigger, TimeSpan ago, TimeSpan? duration, string? error = null) =>
            new() { PipelineName = name, RunId = runId, Status = status, TriggerType = trigger, StartUtc = now - ago, Duration = duration, ErrorMessage = error };

        return
        [
            Make("pl_inventory_sync", "e21a-4f3c-9b10", RunStatus.Running, "Tumbling window", TimeSpan.FromMinutes(6), null),
            Make("pl_ingest_sales_daily", "a93f-21c7-44de", RunStatus.Failed, "Scheduled", TimeSpan.FromMinutes(14), new TimeSpan(0, 4, 12), "Copy activity failed: source file not found"),
            Make("pl_customer_etl", "7bd0-88a2-0c51", RunStatus.Succeeded, "Scheduled", TimeSpan.FromMinutes(62), new TimeSpan(0, 12, 3)),
            Make("pl_vendor_feed_import", "5c71-0d29-7e8a", RunStatus.Succeeded, "Event-based", TimeSpan.FromMinutes(128), new TimeSpan(0, 3, 51)),
            Make("pl_finance_gl_export", "9f02-7a14-d3b6", RunStatus.Failed, "Scheduled", TimeSpan.FromMinutes(185), new TimeSpan(0, 1, 9), "Sink write denied: storage account firewall"),
            Make("pl_marketing_leads_sync", "31bd-66e0-2fa9", RunStatus.Succeeded, "Scheduled", TimeSpan.FromMinutes(242), new TimeSpan(0, 6, 22)),
            Make("pl_hr_employee_feed", "b8a4-19f5-61c0", RunStatus.Succeeded, "Scheduled", TimeSpan.FromMinutes(305), new TimeSpan(0, 2, 47)),
            Make("pl_warehouse_stock_adjust", "02cd-5b83-ae47", RunStatus.Failed, "Event-based", TimeSpan.FromMinutes(366), new TimeSpan(0, 0, 58), "Lookup activity returned no rows"),
        ];
    }
}

public sealed class InMemoryWorkflowRunRepository(TimeProvider time)
    : InMemoryRunRepository<WorkflowRun>(Seed(time.GetUtcNow()))
{
    private static IEnumerable<WorkflowRun> Seed(DateTimeOffset now)
    {
        WorkflowRun Make(string name, RunStatus status, string trigger, TimeSpan ago, TimeSpan? duration, string? failedAction = null) =>
            new() { WorkflowName = name, Status = status, Trigger = trigger, StartUtc = now - ago, Duration = duration, FailedAction = failedAction };

        return
        [
            Make("la-invoice-approval", RunStatus.Running, "HTTP request", TimeSpan.FromMinutes(2), null),
            Make("la-ticket-escalation", RunStatus.Succeeded, "Recurrence", TimeSpan.FromMinutes(18), TimeSpan.FromSeconds(8)),
            Make("la-po-approval-flow", RunStatus.Failed, "HTTP request", TimeSpan.FromMinutes(34), TimeSpan.FromSeconds(14), "Send approval email"),
            Make("la-employee-onboarding", RunStatus.Succeeded, "SharePoint", TimeSpan.FromMinutes(61), TimeSpan.FromSeconds(42)),
            Make("la-expense-notification", RunStatus.Succeeded, "Recurrence", TimeSpan.FromMinutes(75), TimeSpan.FromSeconds(5)),
            Make("la-vendor-sync", RunStatus.Failed, "Recurrence", TimeSpan.FromMinutes(121), TimeSpan.FromSeconds(22), "Call vendor API"),
            Make("la-sla-breach-alert", RunStatus.Succeeded, "Service Bus", TimeSpan.FromMinutes(183), TimeSpan.FromSeconds(3)),
            Make("la-customer-survey", RunStatus.Succeeded, "Recurrence", TimeSpan.FromMinutes(246), TimeSpan.FromSeconds(11)),
        ];
    }
}

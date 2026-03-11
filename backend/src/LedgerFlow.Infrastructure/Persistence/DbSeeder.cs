using LedgerFlow.Application.Dashboard;
using LedgerFlow.Application.Jobs;
using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Operations;
using LedgerFlow.Domain.Payments;
using LedgerFlow.Domain.Vendors;
using LedgerFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LedgerFlow.Infrastructure.Persistence;

/// <summary>
/// Creates demo users and a year of realistic AP activity. Every invoice is driven through the real domain
/// workflow (submit, approve, pay), so seeded data obeys the same rules as data entered in the app.
/// </summary>
public static partial class DbSeeder
{
    public const string DemoPassword = "LedgerFlow!2026";

    public static readonly IReadOnlyList<(string Email, string Name, string Role)> DemoUsers =
    [
        ("clerk@ledgerflow.dev", "Priya Shah", Roles.Clerk),
        ("approver@ledgerflow.dev", "Marcus Lee", Roles.Approver),
        ("controller@ledgerflow.dev", "Elena Ruiz", Roles.Controller),
    ];

    private static readonly (string Name, string Category, int Terms)[] VendorSeed =
    [
        ("Northwind Cloud Services", "Cloud", 30),
        ("Contoso Software", "Software", 30),
        ("Fabrikam Facilities", "Facilities", 45),
        ("Adatum Legal LLP", "Professional Services", 30),
        ("Tailspin Logistics", "Logistics", 15),
        ("Litware Hardware", "Hardware", 45),
        ("Proseware Marketing", "Marketing", 30),
        ("Wingtip Utilities", "Utilities", 15),
        ("Blue Yonder Consulting", "Professional Services", 60),
        ("Coho Data Systems", "Cloud", 30),
        ("Alpine Office Supply", "Facilities", 30),
        ("Trey Research Group", "Professional Services", 45),
        ("Lucerne Security", "Software", 30),
        ("Margie's Freight", "Logistics", 30),
        ("Woodgrove Print Co", "Marketing", 15),
        ("Humongous Devices", "Hardware", 60),
        ("City Power & Light", "Utilities", 15),
        ("Relecloud Analytics", "Software", 45),
    ];

    private static readonly Dictionary<string, (string Gl, string[] Items, decimal Min, decimal Max)> Catalog = new()
    {
        ["Cloud"] = ("6110", ["Compute reserved instances", "Object storage", "Managed SQL", "Egress bandwidth"], 400m, 9_000m),
        ["Software"] = ("6100", ["Annual license", "Seat subscription", "Support renewal", "API usage tier"], 300m, 14_000m),
        ["Facilities"] = ("6400", ["Office lease", "Cleaning services", "HVAC maintenance", "Furniture"], 250m, 12_000m),
        ["Professional Services"] = ("6300", ["Advisory hours", "Contract review", "Audit fieldwork", "Implementation services"], 900m, 16_000m),
        ["Logistics"] = ("6500", ["Freight LTL", "Courier", "Warehousing", "Customs brokerage"], 150m, 5_000m),
        ["Hardware"] = ("1510", ["Laptops", "Network switches", "Monitors", "Docking stations"], 600m, 18_000m),
        ["Marketing"] = ("6200", ["Paid search", "Event sponsorship", "Print collateral", "Agency retainer"], 300m, 11_000m),
        ["Utilities"] = ("6600", ["Electricity", "Water", "Internet circuit", "Gas"], 120m, 3_500m),
    };

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        var db = services.GetRequiredService<LedgerFlowDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

        await SeedUsersAsync(services);
        if (await db.Vendors.AnyAsync(ct))
        {
            return;
        }

        var time = services.GetRequiredService<TimeProvider>();
        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var rng = new Random(2026);
        var (clerk, approver, controller) = (DemoUsers[0].Email, DemoUsers[1].Email, DemoUsers[2].Email);

        var vendors = VendorSeed
            .Select((v, i) => new Vendor(
                v.Name,
                $"{10 + i:D2}-{rng.Next(1_000_000, 9_999_999):D7}",
                $"ap@{Slug().Replace(v.Name.ToLowerInvariant(), string.Empty)}.example",
                v.Category,
                v.Terms))
            .ToList();
        foreach (var vendor in vendors)
        {
            vendor.CreatedAt = now.AddDays(-400);
            vendor.CreatedBy = clerk;
        }

        db.Vendors.AddRange(vendors);
        await db.SaveChangesAsync(ct);

        var paidOn = new Dictionary<Invoice, DateOnly>();
        var invoices = new List<Invoice>();
        for (var n = 0; n < 120; n++)
        {
            var vendor = vendors[rng.Next(vendors.Count)];
            var age = n < 12 ? rng.Next(0, 6) : rng.Next(6, 330);
            var invoiceDate = today.AddDays(-age);
            var (gl, items, min, max) = Catalog[vendor.Category];
            var lines = Enumerable.Range(0, rng.Next(1, 4))
                .Select(_ => new InvoiceLineInput(
                    items[rng.Next(items.Length)],
                    gl,
                    rng.Next(1, 4),
                    decimal.Round(min + ((max - min) * (decimal)Math.Pow(rng.NextDouble(), 2.2)) / 2, 2)))
                .ToList();
            var tax = vendor.Category is "Hardware" or "Facilities" ? decimal.Round(lines.Sum(l => l.Quantity * l.UnitPrice) * 0.0725m, 2) : 0m;
            var enteredAt = At(invoiceDate.AddDays(1), 14);
            var invoice = new Invoice(vendor, $"{Prefix(vendor.Name)}-{invoiceDate:yyMM}-{1000 + n}", invoiceDate, tax, lines, clerk, enteredAt);
            invoices.Add(invoice);

            var roll = rng.NextDouble();
            if (age < 6)
            {
                if (roll < 0.5)
                {
                    continue;
                }

                invoice.Submit(clerk, enteredAt.AddHours(1));
                continue;
            }

            invoice.Submit(clerk, enteredAt.AddHours(1));
            if (roll < 0.05)
            {
                invoice.Reject(approver, "Amount does not match the purchase order. Please confirm with the vendor.", enteredAt.AddHours(20));
                continue;
            }

            var pastDue = today.DayNumber - invoice.DueDate.DayNumber;
            var leaveSubmitted = pastDue > 5 ? roll >= 0.94 : roll > 0.65;
            if (leaveSubmitted)
            {
                continue;
            }

            Approve(invoice, approver, controller, enteredAt.AddHours(4 + rng.Next(0, 70)));
            if (pastDue > 5 && roll < 0.88)
            {
                var payDate = Friday(invoice.DueDate.AddDays(-rng.Next(2, 7)));
                paidOn[invoice] = payDate > today.AddDays(-1) ? today.AddDays(-1) : payDate;
            }
        }

        db.Invoices.AddRange(invoices);
        await db.SaveChangesAsync(ct);

        var controllerRoles = new[] { Roles.Controller };
        foreach (var batch in paidOn.GroupBy(p => p.Value).OrderBy(g => g.Key))
        {
            var paymentDate = batch.Key;
            var run = new PaymentRun($"Weekly run {paymentDate:yyyy-MM-dd}", paymentDate)
            {
                CreatedAt = At(paymentDate.AddDays(-2), 9),
                CreatedBy = controller,
            };
            db.PaymentRuns.Add(run);
            await db.SaveChangesAsync(ct);
            foreach (var invoice in batch.Select(p => p.Key))
            {
                run.AddInvoice(invoice, controller, At(paymentDate.AddDays(-2), 9));
            }

            run.Release(controller, controllerRoles, At(paymentDate, 10));
        }

        var pending = invoices.Where(i => i.Status == InvoiceStatus.Approved).OrderBy(i => i.DueDate).Take(5).ToList();
        if (pending.Count > 0)
        {
            var draft = new PaymentRun($"Weekly run {Friday(today.AddDays(1)):yyyy-MM-dd}", Friday(today.AddDays(1))) { CreatedBy = controller };
            db.PaymentRuns.Add(draft);
            await db.SaveChangesAsync(ct);
            foreach (var invoice in pending)
            {
                draft.AddInvoice(invoice, controller, now.AddHours(-3));
            }
        }

        await db.SaveChangesAsync(ct);
        SeedAgingHistory(db, invoices, paidOn, today);
        db.Notifications.Add(new Notification(Roles.Clerk, "Welcome to LedgerFlow", "Demo data was generated: 18 vendors and a year of invoices.", "/dashboard", now));
        await db.SaveChangesAsync(ct);

        await services.GetRequiredService<OverdueInvoiceJob>().RunAsync(ct);
        await services.GetRequiredService<PaymentReminderJob>().RunAsync(ct);
        await services.GetRequiredService<AgingSnapshotJob>().RunAsync(ct);
        await services.GetRequiredService<WeeklyAgingReportJob>().RunAsync(ct);

        LogSeeded(logger, vendors.Count, invoices.Count);
    }

    private static async Task SeedUsersAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        foreach (var role in new[] { Roles.Clerk, Roles.Approver, Roles.Controller })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                Check(await roleManager.CreateAsync(new IdentityRole(role)));
            }
        }

        foreach (var (email, name, role) in DemoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = name };
            Check(await userManager.CreateAsync(user, DemoPassword));
            Check(await userManager.AddToRoleAsync(user, role));
        }
    }

    /// <summary>Back-fills 30 days of aging snapshots from the invoice history so the trend chart has real data.</summary>
    private static void SeedAgingHistory(LedgerFlowDbContext db, List<Invoice> invoices, Dictionary<Invoice, DateOnly> paidOn, DateOnly today)
    {
        for (var day = today.AddDays(-30); day < today; day = day.AddDays(1))
        {
            var asOf = day;
            var open = invoices
                .Where(i => i.Status is not (InvoiceStatus.Draft or InvoiceStatus.Rejected))
                .Where(i => i.InvoiceDate < asOf && (!paidOn.TryGetValue(i, out var paid) || paid > asOf))
                .Select(i => new { i.Total, DaysPastDue = asOf.DayNumber - i.DueDate.DayNumber })
                .ToList();
            foreach (var (bucket, order) in DashboardService.Buckets)
            {
                var rows = open.Where(o => BucketOrder(o.DaysPastDue) == order).ToList();
                db.AgingSnapshots.Add(new AgingSnapshot(asOf, bucket, order, rows.Count, rows.Sum(r => r.Total)));
            }
        }
    }

    private static int BucketOrder(int daysPastDue) => daysPastDue switch
    {
        <= 0 => 0,
        <= 30 => 1,
        <= 60 => 2,
        <= 90 => 3,
        _ => 4,
    };

    private static void Approve(Invoice invoice, string approver, string controller, DateTimeOffset at)
    {
        if (invoice.Total > ApprovalPolicy.ApproverLimit)
        {
            invoice.Approve(controller, [Roles.Controller], "Within budget for the quarter.", at);
        }
        else
        {
            invoice.Approve(approver, [Roles.Approver], null, at);
        }
    }

    private static DateOnly Friday(DateOnly date) => date.AddDays(((int)DayOfWeek.Friday - (int)date.DayOfWeek + 7) % 7);

    private static DateTimeOffset At(DateOnly date, int hour) => new(date.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);

    private static string Prefix(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex("[^a-z0-9]")]
    private static partial System.Text.RegularExpressions.Regex Slug();

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {VendorCount} vendors and {InvoiceCount} invoices")]
    private static partial void LogSeeded(ILogger logger, int vendorCount, int invoiceCount);
}

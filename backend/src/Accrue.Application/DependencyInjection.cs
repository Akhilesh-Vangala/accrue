using FluentValidation;
using Accrue.Application.Dashboard;
using Accrue.Application.Extraction;
using Accrue.Application.Invoices;
using Accrue.Application.Jobs;
using Accrue.Application.Notifications;
using Accrue.Application.Payments;
using Accrue.Application.Reporting;
using Accrue.Application.Vendors;
using Microsoft.Extensions.DependencyInjection;

namespace Accrue.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddScoped<VendorService>();
        services.AddScoped<InvoiceService>();
        services.AddScoped<PaymentRunService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<ReportService>();
        services.AddScoped<CsvReportRenderer>();
        services.AddScoped<InvoiceExtractionService>();

        services.AddScoped<OverdueInvoiceJob>();
        services.AddScoped<PaymentReminderJob>();
        services.AddScoped<AgingSnapshotJob>();
        services.AddScoped<AutoPaymentRunJob>();
        services.AddScoped<WeeklyAgingReportJob>();
        services.AddScoped<InvoiceDecisionNotificationJob>();
        return services;
    }
}

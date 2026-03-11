using FluentValidation;
using LedgerFlow.Application.Dashboard;
using LedgerFlow.Application.Invoices;
using LedgerFlow.Application.Jobs;
using LedgerFlow.Application.Notifications;
using LedgerFlow.Application.Payments;
using LedgerFlow.Application.Reporting;
using LedgerFlow.Application.Vendors;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerFlow.Application;

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

        services.AddScoped<OverdueInvoiceJob>();
        services.AddScoped<PaymentReminderJob>();
        services.AddScoped<AgingSnapshotJob>();
        services.AddScoped<AutoPaymentRunJob>();
        services.AddScoped<WeeklyAgingReportJob>();
        services.AddScoped<InvoiceDecisionNotificationJob>();
        return services;
    }
}

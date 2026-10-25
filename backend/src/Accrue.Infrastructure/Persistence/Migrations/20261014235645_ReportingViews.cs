using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accrue.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Reporting views shared by EF Core (dashboard, CSV export, aging snapshot job) and the Telerik .trdx reports,
    /// so every surface computes aging and spend the same way.
    /// </summary>
    public partial class ReportingViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW dbo.vw_ApAging AS
                SELECT
                    i.Id AS InvoiceId,
                    i.InvoiceNumber,
                    i.VendorId,
                    v.Name AS VendorName,
                    i.InvoiceDate,
                    i.DueDate,
                    i.Status,
                    CAST(i.Total - i.AmountPaid AS decimal(18, 2)) AS Outstanding,
                    d.DaysPastDue,
                    CASE
                        WHEN d.DaysPastDue <= 0 THEN 'Current'
                        WHEN d.DaysPastDue <= 30 THEN '1-30'
                        WHEN d.DaysPastDue <= 60 THEN '31-60'
                        WHEN d.DaysPastDue <= 90 THEN '61-90'
                        ELSE '90+'
                    END AS Bucket,
                    CASE
                        WHEN d.DaysPastDue <= 0 THEN 0
                        WHEN d.DaysPastDue <= 30 THEN 1
                        WHEN d.DaysPastDue <= 60 THEN 2
                        WHEN d.DaysPastDue <= 90 THEN 3
                        ELSE 4
                    END AS BucketOrder
                FROM dbo.Invoices AS i
                INNER JOIN dbo.Vendors AS v ON v.Id = i.VendorId
                CROSS APPLY (SELECT DATEDIFF(day, i.DueDate, CAST(SYSUTCDATETIME() AS date)) AS DaysPastDue) AS d
                WHERE i.Status IN (N'Submitted', N'Approved', N'Scheduled')
                  AND i.Total > i.AmountPaid;
                """);

            migrationBuilder.Sql("""
                CREATE VIEW dbo.vw_VendorSpend AS
                SELECT
                    v.Id AS VendorId,
                    v.Name AS VendorName,
                    v.Category,
                    DATEFROMPARTS(YEAR(i.InvoiceDate), MONTH(i.InvoiceDate), 1) AS PeriodStart,
                    CAST(COUNT(*) AS int) AS InvoiceCount,
                    CAST(SUM(i.Total) AS decimal(18, 2)) AS TotalAmount
                FROM dbo.Invoices AS i
                INNER JOIN dbo.Vendors AS v ON v.Id = i.VendorId
                WHERE i.Status NOT IN (N'Draft', N'Rejected')
                GROUP BY v.Id, v.Name, v.Category, DATEFROMPARTS(YEAR(i.InvoiceDate), MONTH(i.InvoiceDate), 1);
                """);

            migrationBuilder.Sql("""
                CREATE VIEW dbo.vw_PaymentRemittance AS
                SELECT
                    p.Id AS PaymentId,
                    r.Id AS PaymentRunId,
                    r.Name AS RunName,
                    r.PaymentDate,
                    r.Status AS RunStatus,
                    v.Name AS VendorName,
                    v.Email AS VendorEmail,
                    i.InvoiceNumber,
                    i.InvoiceDate,
                    p.Amount,
                    p.Reference
                FROM dbo.Payments AS p
                INNER JOIN dbo.PaymentRuns AS r ON r.Id = p.PaymentRunId
                INNER JOIN dbo.Invoices AS i ON i.Id = p.InvoiceId
                INNER JOIN dbo.Vendors AS v ON v.Id = i.VendorId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_PaymentRemittance;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_VendorSpend;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_ApAging;");
        }
    }
}

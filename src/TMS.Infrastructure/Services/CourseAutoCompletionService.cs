using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TMS.Core.Entities.Courses;
using TMS.Core.Enums;
using TMS.Infrastructure.Data;

namespace TMS.Infrastructure.Services;

public class CourseAutoCompletionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CourseAutoCompletionService> _logger;

    public CourseAutoCompletionService(IServiceScopeFactory scopeFactory, ILogger<CourseAutoCompletionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndCompleteAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in course auto-completion check.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task CheckAndCompleteAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var expiringCourses = await db.Courses
            .Include(c => c.Schedules)
            .Where(c => c.IsActive && c.Status == CourseStatus.InProgress)
            .Where(c => c.Schedules.Any(s => s.EndDate < now && s.Status != ScheduleStatus.Cancelled))
            .ToListAsync(ct);

        foreach (var course in expiringCourses)
        {
            var allEnded = course.Schedules
                .Where(s => s.Status != ScheduleStatus.Cancelled)
                .All(s => s.EndDate < now);

            if (!allEnded) continue;

            course.Status = CourseStatus.Completed;
            course.ModifiedAt = now;

            foreach (var schedule in course.Schedules.Where(s => s.Status != ScheduleStatus.Cancelled))
                schedule.Status = ScheduleStatus.Completed;

            _logger.LogInformation("Course {CourseId} auto-completed on end date.", course.Id);
        }

        if (expiringCourses.Any())
            await db.SaveChangesAsync(ct);
    }
}
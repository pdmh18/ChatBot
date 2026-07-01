using System;

namespace Application.Common.Utils
{
    public static class TaskRiskCalculator
    {
        private const string PriorityMedium = "Trung binh";
        private const string PriorityHigh = "Cao";
        private const string PriorityUrgent = "Khan cap";

        private const string StatusTodo = "Can lam";
        private const string StatusDoing = "Dang lam";
        private const string StatusReview = "Cho kiem tra";
        private const string StatusDone = "Hoan thanh";
        private const string StatusBlocked = "Bi chan";
        private const string StatusWaitingApproval = "Cho duyet";
        private const string StatusApproved = "Da duyet";
        private const string StatusRejected = "Bi tu choi";
        private const string StatusCanceled = "Da huy";

        public static int Calculate(
            DateOnly? startDate,
            DateOnly? deadline,
            string? status,
            string? priority,
            int? progress,
            decimal? estimatedHours,
            int? assigneeId)
        {
            if (IsInactiveStatus(status))
            {
                return 0;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var safeProgress = Math.Clamp(progress ?? 0, 0, 100);

            var score =
                CalculateDeadlineScore(deadline, today)
                + CalculateScheduleProgressScore(startDate, deadline, today, safeProgress)
                + CalculatePriorityScore(priority)
                + CalculateStatusScore(status, startDate, today, safeProgress)
                + CalculateWorkSizeScore(estimatedHours)
                + CalculateAssignmentScore(assigneeId, startDate, deadline, today);

            return Math.Clamp(score, 0, 100);
        }

        public static string GetEnglishRiskLevel(int riskPercent)
        {
            if (riskPercent >= 70)
            {
                return "High";
            }

            if (riskPercent >= 31)
            {
                return "Medium";
            }

            return "Low";
        }

        public static string GetVietnameseRiskLevel(int riskPercent)
        {
            if (riskPercent >= 70)
            {
                return "Cao";
            }

            if (riskPercent >= 31)
            {
                return "Trung binh";
            }

            return "Thap";
        }

        public static int? CalculateExpectedProgress(
            DateOnly? startDate,
            DateOnly? deadline,
            DateOnly today)
        {
            if (!startDate.HasValue || !deadline.HasValue || deadline.Value <= startDate.Value)
            {
                return null;
            }

            if (today < startDate.Value)
            {
                return 0;
            }

            var totalDays = deadline.Value.DayNumber - startDate.Value.DayNumber;
            var elapsedDays = Math.Clamp(
                today.DayNumber - startDate.Value.DayNumber,
                0,
                totalDays);

            return Math.Clamp((int)Math.Round((elapsedDays * 100m) / totalDays), 0, 100);
        }

        private static int CalculateDeadlineScore(DateOnly? deadline, DateOnly today)
        {
            if (!deadline.HasValue)
            {
                return 0;
            }

            var daysLeft = deadline.Value.DayNumber - today.DayNumber;

            if (daysLeft < -14)
            {
                return 45;
            }

            if (daysLeft < -7)
            {
                return 42;
            }

            if (daysLeft < 0)
            {
                return 38;
            }

            if (daysLeft == 0)
            {
                return 30;
            }

            if (daysLeft <= 2)
            {
                return 24;
            }

            if (daysLeft <= 5)
            {
                return 16;
            }

            if (daysLeft <= 10)
            {
                return 8;
            }

            return 0;
        }

        private static int CalculateScheduleProgressScore(
            DateOnly? startDate,
            DateOnly? deadline,
            DateOnly today,
            int progress)
        {
            var expectedProgress = CalculateExpectedProgress(startDate, deadline, today);

            if (expectedProgress.HasValue)
            {
                var progressGap = expectedProgress.Value - progress;

                if (progressGap >= 50)
                {
                    return 25;
                }

                if (progressGap >= 35)
                {
                    return 20;
                }

                if (progressGap >= 20)
                {
                    return 14;
                }

                if (progressGap >= 10)
                {
                    return 8;
                }

                if (progressGap >= 5)
                {
                    return 4;
                }

                return 0;
            }

            if (startDate.HasValue && today < startDate.Value)
            {
                return 0;
            }

            if (!startDate.HasValue && !deadline.HasValue)
            {
                return progress < 20 ? 6 : 0;
            }

            if (progress < 20)
            {
                return 18;
            }

            if (progress < 40)
            {
                return 12;
            }

            if (progress < 60)
            {
                return 6;
            }

            return 0;
        }

        private static int CalculatePriorityScore(string? priority)
        {
            if (IsSame(priority, PriorityUrgent))
            {
                return 16;
            }

            if (IsSame(priority, PriorityHigh))
            {
                return 11;
            }

            if (IsSame(priority, PriorityMedium))
            {
                return 5;
            }

            return 0;
        }

        private static int CalculateStatusScore(
            string? status,
            DateOnly? startDate,
            DateOnly today,
            int progress)
        {
            if (IsSame(status, StatusBlocked))
            {
                return 28;
            }

            if (IsSame(status, StatusRejected))
            {
                return 18;
            }

            if (IsSame(status, StatusTodo))
            {
                if (startDate.HasValue && today > startDate.Value)
                {
                    return progress <= 0 ? 12 : 8;
                }

                if (startDate.HasValue && today == startDate.Value)
                {
                    return 6;
                }

                return progress <= 0 ? 4 : 0;
            }

            if (IsSame(status, StatusDoing))
            {
                return 4;
            }

            if (IsSame(status, StatusReview) || IsSame(status, StatusWaitingApproval))
            {
                return 2;
            }

            if (IsSame(status, StatusApproved))
            {
                return 0;
            }

            return 0;
        }

        private static int CalculateWorkSizeScore(decimal? estimatedHours)
        {
            if (!estimatedHours.HasValue || estimatedHours.Value <= 0m)
            {
                return 0;
            }

            if (estimatedHours.Value >= 40m)
            {
                return 8;
            }

            if (estimatedHours.Value >= 24m)
            {
                return 6;
            }

            if (estimatedHours.Value >= 16m)
            {
                return 4;
            }

            if (estimatedHours.Value >= 8m)
            {
                return 2;
            }

            return 0;
        }

        private static int CalculateAssignmentScore(
            int? assigneeId,
            DateOnly? startDate,
            DateOnly? deadline,
            DateOnly today)
        {
            if (assigneeId.HasValue)
            {
                return 0;
            }

            if (deadline.HasValue)
            {
                var daysLeft = deadline.Value.DayNumber - today.DayNumber;

                if (daysLeft <= 3)
                {
                    return 14;
                }

                if (daysLeft <= 7)
                {
                    return 10;
                }
            }

            if (startDate.HasValue && today >= startDate.Value)
            {
                return 10;
            }

            return 6;
        }

        private static bool IsInactiveStatus(string? status)
        {
            return IsSame(status, StatusDone) || IsSame(status, StatusCanceled);
        }

        private static bool IsSame(string? value, string expected)
        {
            return string.Equals(
                value?.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}

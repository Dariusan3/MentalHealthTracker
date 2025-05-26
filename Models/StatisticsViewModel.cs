using System;
using System.Collections.Generic;

namespace MentalHealthTracker.Models
{
    public class StatisticsViewModel
    {
        public double AverageMood { get; set; }
        public int EntriesCount { get; set; }
        public List<MoodTrendItem> MoodTrend { get; set; } = new();
        public List<string> MostCommonActivities { get; set; } = new();
        public List<string> MostCommonTriggers { get; set; } = new();
        public double AverageSleepHours { get; set; }
    }
    
    public class MoodTrendItem
    {
        public required string Date { get; set; }
        public double AverageMood { get; set; }
    }
} 
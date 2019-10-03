using ApiReviewList.Reports;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace create_past_reviews
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var netFoundationStart = new DateTimeOffset(2018, 2, 27, 0, 0, 0, TimeSpan.Zero);
            var playlist = "PL1rZQsJPBU2S49OQPjupSJF-qeIEz9_ju";

            Console.WriteLine("Getting dates...");
            var dates = (await ApiReviewVideo.GetAllAsync(playlist)).Select(v => (DateTimeOffset) v.StartDateTime.Date).ToList();
            dates.RemoveAll(d => d < netFoundationStart);

            Console.WriteLine("Getting summaries...");
            foreach (var summary in await ApiReviewSummary.GetAsync(dates))
            {
                var path = summary.GetPath();

                if (File.Exists(path))
                    continue;

                Console.WriteLine(summary.Date);

                if (summary.Items.Any())
                {
                    Console.WriteLine("   Saving summaries...");
                    await summary.StoreAsync();
                    Console.WriteLine("   Updating comments...");
                    await summary.UpdateCommentsAsync();
                    try
                    {
                        Console.WriteLine("   Updating YouTube...");
                        await summary.UpdateVideoDescriptionAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   Updating YouTube failed {ex}");
                    }
                }
            }
        }
    }
}

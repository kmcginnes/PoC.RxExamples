using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace PoC.RxExamples
{
    public class WebsiteFetching
    {
        public void Execute()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var websites = new[]
            {
                "http://www.google.com",
                "http://www.bing.com",
                "http://www.yahoo.com",
                "http://www.duckduckgo.com",
                "http://www.github.com",
                "http://www.microsoft.com",
                "http://www.stackoverflow.com",
            };

            //websites
            //    .Select(FetchWebsiteSync)
            //    .ForEach(
            //        x => Console.WriteLine(x.Substring(0, 10)));
            //stopwatch.StopAndPrint();

            //websites
            //    .ToObservable()
            //    .SelectMany(FetchWebsite)
            //    .Subscribe(
            //        x => Console.WriteLine(x.Substring(0, 10)),
            //        ex => Console.WriteLine(ex.Message),
            //        () => stopwatch.StopAndPrint());

            //websites
            //    .ToObservable()
            //    .SelectMany(x => FetchWebsite(x)
            //        .Timeout(TimeSpan.FromMilliseconds(5))
            //        .Catch(Observable.Return(x + " didn't work" + Environment.NewLine)))
            //    .Subscribe(
            //        x => Console.WriteLine(x.Truncate(100) + Environment.NewLine),
            //        ex => Console.WriteLine(ex.Message + Environment.NewLine),
            //        () => stopwatch.StopAndPrint());

            //websites
            //    .ToObservable()
            //    .SelectMany(x => 
            //        Observable.Defer(() =>
            //            {
            //                Console.WriteLine("Attempting to fetch {0}...", x); 
            //                return FetchWebsite(x);
            //            })
            //            .Timeout(TimeSpan.FromMilliseconds(5))
            //            .Retry(2)
            //            .Catch(Observable.Return(x + " didn't work" + Environment.NewLine)))
            //    .Subscribe(
            //        x => Console.WriteLine(x.Truncate(100) + Environment.NewLine),
            //        ex => Console.WriteLine(ex.Message + Environment.NewLine),
            //        () => stopwatch.StopAndPrint());

            websites
                .ToObservable()
                .Select(x =>
                    Observable.Defer(() =>
                    {
                        Console.WriteLine("Attempting to fetch {0}...", x);
                        return FetchWebsite(x);
                    })
                        .Timeout(TimeSpan.FromMilliseconds(50))
                        .Retry(2)
                        .Catch(Observable.Return(x + " didn't work" + Environment.NewLine)))
                .Merge(2)
                .Subscribe(
                    x => Console.WriteLine(x.Truncate(100) + Environment.NewLine),
                    ex => Console.WriteLine(ex.Message + Environment.NewLine),
                    () => stopwatch.StopAndPrint());
        }

        private IObservable<string> FetchWebsite(string url)
        {
            return Observable.Start<string>(() => FetchWebsiteSync(url), Scheduler.Default);
        }

        private string FetchWebsiteSync(string url)
        {
            var webRequest = HttpWebRequest.Create(url);

            var webResponse = webRequest.GetResponse();

            string htmlContent = String.Empty;
            using (var reader = new StreamReader(webResponse.GetResponseStream()))
            {
                htmlContent = reader.ReadToEnd();
            }
            return htmlContent;
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PoC.RxExamples.Logging;

namespace PoC.RxExamples
{
    // Pulled from GitHub decompiled
    public static class ObservableEx
    {
        private static readonly ILog log = typeof(ObservableEx).Log();
        private static readonly ConcurrentDictionary<string, object> currentObservables = new ConcurrentDictionary<string, object>();
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Func<int, TimeSpan> ExpontentialBackoff = n => TimeSpan.FromSeconds(Math.Pow(n, 2.0));

        public static IObservable<IReadOnlyList<T>> ToReadOnlyList<T>(this IObservable<T> source)
        {
            return source.ToList().Select(list => new ReadOnlyCollection<T>(list));
        }

        public static IObservable<List<T>> ToConcreteList<T>(this IObservable<T> source)
        {
            return source.Aggregate(new List<T>(), (list, item) =>
            {
                list.Add(item);
                return list;
            });
        }

        //public static IObservable<TSource> CatchNonCritical<TSource>(this IObservable<TSource> first, Func<Exception, IObservable<TSource>> second)
        //{
        //    return first.Catch((Func<Exception, IObservable<TSource>>)(e =>
        //    {
        //        if (!ExceptionExtensions.IsCriticalException(e))
        //            return second(e);
        //        ModeDetector.InUnitTestRunner();
        //        return Observable.Throw<TSource>(e);
        //    }));
        //}

        //public static IObservable<TSource> CatchNonCritical<TSource>(this IObservable<TSource> first, IObservable<TSource> second)
        //{
        //    return first.CatchNonCritical(e => second);
        //}

        public static IObservable<T> LogErrors<T>(this IObservable<T> observable, string message = null)
        {
            return Observable.Create((Func<IObserver<T>, IDisposable>)(subj => observable.Subscribe(subj.OnNext, ex =>
            {
                string str = message ?? "0x" + observable.GetHashCode().ToString("x", CultureInfo.InvariantCulture);
                log.Info(string.Format(CultureInfo.InvariantCulture, "{0} failed.", (object) str), ex);
                subj.OnError(ex);
            }, subj.OnCompleted)));
        }

        public static IObservable<T> RateLimit<T>(this IObservable<T> observable, TimeSpan interval, IScheduler scheduler)
        {
            //Ensure.ArgumentNotNull((object)scheduler, "scheduler");
            return observable.RateLimitImpl(interval, scheduler);
        }

        public static IObservable<T> RateLimit<T>(this IObservable<T> observable, TimeSpan interval)
        {
            return observable.RateLimitImpl(interval, null);
        }

        private static IObservable<T> RateLimitImpl<T>(this IObservable<T> observable, TimeSpan interval, IScheduler scheduler)
        {
            //Ensure.ArgumentNotNull((object)observable, "observable");
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("The interval must not be negative", "interval");
            DateTimeOffset lastSeen = DateTimeOffset.MinValue;
            return (scheduler == null ? observable.Timestamp() : observable.Timestamp(scheduler)).Where(item =>
            {
                if (!(lastSeen == DateTimeOffset.MinValue))
                    return item.Timestamp.Ticks > lastSeen.Ticks + interval.Ticks;
                return true;
            }).Do(item => lastSeen = item.Timestamp).Select(item => item.Value);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Rx has your back.")]
        public static IObservable<T> PublishAsync<T>(this IObservable<T> observable)
        {
            //Ensure.ArgumentNotNull((object)observable, "observable");
            IConnectableObservable<T> connectableObservable = observable.Multicast(new AsyncSubject<T>());
            connectableObservable.Connect();
            return connectableObservable;
        }

        //public static IObservable<T> StartWithRetry<T>(Func<IObservable<T>> block, int retries)
        //{
        //    return StartWithRetry(block, retries, RxApp.TaskpoolScheduler);
        //}

        //public static IObservable<T> StartWithRetry<T>(Func<IObservable<T>> block, int retries, IScheduler scheduler)
        //{
        //    //Ensure.ArgumentNotNull((object)block, "block");
        //    //Ensure.ArgumentNonNegative(retries, "retries");
        //    //Ensure.ArgumentNotNull((object)scheduler, "scheduler");
        //    return Observable.Defer(() => Observable.Start(block, scheduler).Merge().Select(item => new Tuple<bool, T, Exception>(true, item, null)).Catch((Func<Exception, IObservable<Tuple<bool, T, Exception>>>)(e =>
        //    {
        //        if (!ExceptionExtensions.CanRetry(e))
        //            return Observable.Return(new Tuple<bool, T, Exception>(false, default(T), e));
        //        return Observable.Throw<Tuple<bool, T, Exception>>(e);
        //    }))).Retry(retries).SelectMany(t =>
        //    {
        //        if (!t.Item1)
        //            return Observable.Throw<T>(t.Item3);
        //        return Observable.Return(t.Item2);
        //    }).PublishAsync();
        //}

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Automatically disposed when the observable completes.")]
        public static IObservable<T> CurrentOrCreate<T>(Func<IObservable<T>> sourceThunk, string key = null)
        {
            if (sourceThunk == null)
                throw new ArgumentNullException("sourceThunk");
            key = key ?? sourceThunk.Method.DeclaringType.FullName + "." + sourceThunk.Method.Name;
            ReplaySubject<T> replaySubject1 = new ReplaySubject<T>();
            ReplaySubject<T> replaySubject2 = (ReplaySubject<T>)currentObservables.GetOrAdd(key, replaySubject1);
            if (replaySubject2 != replaySubject1)
                return replaySubject2;
            object removed;
            Action finallyAction = () => currentObservables.TryRemove(key, out removed);
            IObservable<T> source;
            try
            {
                source = sourceThunk();
                if (source == null)
                {
                    finallyAction();
                    throw new InvalidOperationException("The observable returned by the lambda expression is null.");
                }
            }
            catch (Exception ex)
            {
                finallyAction();
                throw;
            }
            source.Do(_ => { }, ex => finallyAction(), finallyAction).Multicast(replaySubject1).Connect();
            return replaySubject1;
        }

        public static IObservable<Unit> AsCompletion<T>(this IObservable<T> observable)
        {
            return observable.Aggregate(Unit.Default, (unit, _) => unit);
        }

        public static IObservable<T> WhereNotNull<T>(this IObservable<T> observable) where T : class
        {
            return observable.Where(item => (object)item != null);
        }

        public static IObservable<Unit> SelectUnit<T>(this IObservable<T> observable)
        {
            return observable.Select(_ => Unit.Default);
        }

        public static IObservable<TRet> ContinueAfter<T, TRet>(this IObservable<T> observable, Func<IObservable<TRet>> selector)
        {
            return observable.AsCompletion().SelectMany(_ => selector());
        }

        public static IObservable<TRet> ContinueAfter<T, TRet>(this IObservable<T> observable, Func<IObservable<TRet>> selector, IScheduler scheduler)
        {
            return observable.AsCompletion().ObserveOn(scheduler).SelectMany(_ => selector());
        }

        //[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        //public static IObservable<T> RetryWithBackoffStrategy<T>(this IObservable<T> source, int retryCount = 3, Func<int, TimeSpan> strategy = null, Func<Exception, bool> retryOnError = null, IScheduler scheduler = null)
        //{
        //    strategy = strategy ?? ExpontentialBackoff;
        //    scheduler = scheduler ?? RxApp.TaskpoolScheduler;
        //    if (retryOnError == null)
        //        retryOnError = e => ExceptionExtensions.CanRetry(e);
        //    int attempt = 0;
        //    return Observable.Defer(() => (++attempt == 1 ? source : source.DelaySubscription(strategy(attempt - 1), scheduler)).Select(item => new Tuple<bool, T, Exception>(true, item, null)).Catch((Func<Exception, IObservable<Tuple<bool, T, Exception>>>)(e =>
        //    {
        //        if (!retryOnError(e))
        //            return Observable.Return(new Tuple<bool, T, Exception>(false, default(T), e));
        //        return Observable.Throw<Tuple<bool, T, Exception>>(e);
        //    }))).Retry(retryCount).SelectMany(t =>
        //    {
        //        if (!t.Item1)
        //            return Observable.Throw<T>(t.Item3);
        //        return Observable.Return(t.Item2);
        //    });
        //}

        public static IObservable<T> Using<TResource, T>(Func<IObservable<TResource>> resourceFactory, Func<TResource, IObservable<T>> observableFactory) where TResource : IDisposable
        {
            return Observable.Create((Func<IObserver<T>, IDisposable>)(obs =>
            {
                var disposable = new SingleAssignmentDisposable();
                Observable.Materialize(resourceFactory()).Take(1).Subscribe(notification =>
                {
                    if (notification.Kind == NotificationKind.OnCompleted)
                    {
                        obs.OnCompleted();
                    }
                    else if (notification.Kind == NotificationKind.OnError)
                    {
                        obs.OnError(notification.Exception);
                    }
                    else if (disposable.IsDisposed)
                    {
                        if (notification.Value == null)
                            return;
                        notification.Value.Dispose();
                    }
                    else
                    {
                        disposable.Disposable = Observable.Using(() => notification.Value, observableFactory).Subscribe(obs);
                    }
                });
                return disposable;
            }));
        }

        public static IObservable<T> ErrorIfEmpty<T>(this IObservable<T> source, Exception exc)
        {
            return source.Materialize().Scan(Tuple.Create(false, (Notification<T>)null), (prev, cur) => Tuple.Create(prev.Item1 || cur.Kind == NotificationKind.OnNext, cur)).SelectMany(x =>
            {
                if (x.Item1 || x.Item2.Kind != NotificationKind.OnCompleted)
                    return Observable.Return(x.Item2);
                return Observable.Throw<Notification<T>>(exc);
            }).Dematerialize();
        }

        public static IObservable<TRet> Select<T1, T2, TRet>(this IObservable<Tuple<T1, T2>> source, Func<T1, T2, TRet> lambda)
        {
            return source.Select(x => lambda(x.Item1, x.Item2));
        }

        public static IObservable<TRet> Select<T1, T2, T3, TRet>(this IObservable<Tuple<T1, T2, T3>> source, Func<T1, T2, T3, TRet> lambda)
        {
            return source.Select(x => lambda(x.Item1, x.Item2, x.Item3));
        }

        public static IObservable<TRet> Select<T1, T2, T3, T4, T5, T6, T7, TRet>(this IObservable<Tuple<T1, T2, T3, T4, T5, T6, T7>> source, Func<T1, T2, T3, T4, T5, T6, T7, TRet> lambda)
        {
            return source.Select(x => lambda(x.Item1, x.Item2, x.Item3, x.Item4, x.Item5, x.Item6, x.Item7));
        }

        //public static IObservable<T> AddErrorData<T>(this IObservable<T> source, string key, object value)
        //{
        //    //Ensure.ArgumentNotNull((object)source, "source");
        //    return source.Do(_ => { }, ex => ExceptionExtensions.AddData<Exception>(ex, key, value));
        //}

        //public static IObservable<T> AddErrorData<T>(this IObservable<T> source, object values)
        //{
        //    //Ensure.ArgumentNotNull((object)source, "source");
        //    return source.Do(_ => { }, ex => ExceptionExtensions.AddData<Exception>(ex, values));
        //}

        //public static IObservable<TRet> Benchmark<TRet>(this IObservable<TRet> observable, string action, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        //{
        //    return observable.Benchmark(log, action, callerFilePath, callerLineNumber);
        //}

        //public static IObservable<TRet> Benchmark<TRet>(this IObservable<TRet> observable, ILog log, string formatString, params object[] args)
        //{
        //    return observable.Benchmark(log, string.Format(CultureInfo.InvariantCulture, formatString, args), null, 0);
        //}

        //private static IObservable<TRet> Benchmark<TRet>(this IObservable<TRet> observable, ILog log, string action, string callerFilePath, int callerLineNumber)
        //{
        //    return Observable.Defer(() =>
        //    {
        //        Stopwatch stopwatch = Stopwatch.StartNew();
        //        return observable.Do(_ => { }, () =>
        //        {
        //            stopwatch.Stop();
        //            if (callerFilePath != null)
        //                log.Info("{0}: Took {1}ms to {1}", DebugUtils.FormatSourcePath(callerFilePath, callerLineNumber), stopwatch.ElapsedMilliseconds, action);
        //            else
        //                log.Info("Took {0}ms to {1}", stopwatch.ElapsedMilliseconds, action);
        //        });
        //    });
        //}
    }
}
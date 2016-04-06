using System;
using System.Diagnostics;
using System.Globalization;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using PoC.RxExamples.Logging;

namespace PoC.RxExamples
{
    // Pulled from GitHub decompiled
    public sealed class AsyncReaderWriterLock
    {
        private readonly ConcurrentExclusiveSchedulerPair pair;
        private readonly TaskFactory concurrentFactory;
        private readonly TaskFactory exclusiveFactory;
        private static int operationCounter;

        public AsyncReaderWriterLock()
        {
            pair = new ConcurrentExclusiveSchedulerPair();
            concurrentFactory = new TaskFactory(pair.ConcurrentScheduler);
            exclusiveFactory = new TaskFactory(pair.ExclusiveScheduler);
        }

        public IObservable<T> AddConcurrentOperation<T>(IObservable<T> work, string operationName)
        {
            return AddOperationOnScheduler(concurrentFactory, work, operationName);
        }

        public IObservable<T> AddExclusiveOperation<T>(IObservable<T> work, string operationName)
        {
            return AddOperationOnScheduler(exclusiveFactory, work, operationName);
        }

        private IObservable<T> AddOperationOnScheduler<T>(TaskFactory scheduler, IObservable<T> work, string operationName)
        {
            return Observable.Create((Func<IObserver<T>, IDisposable>)(subscriber =>
            {
                var @lock = new AsyncSubject<IDisposable>();
                var gate = new AsyncSubject<Unit>();
                var cancellation = new CompositeDisposable();
                var operationId = Interlocked.Increment(ref operationCounter);
                var schedulerType = scheduler == exclusiveFactory ? "exclusive" : "concurrent";
                try
                {
                    this.Log().Info("Acquiring {0} scheduler for operation {1} at {2}", schedulerType, operationId, operationName);
                    var stopwatch = Stopwatch.StartNew();
                    IDisposable inFlightSpan;
                    scheduler.StartNew(() =>
                    {
                        //Ensure.NotOnUIThread();
                        var totalSeconds1 = stopwatch.Elapsed.TotalSeconds;
                        this.Log().Info("Acquired {0} scheduler for operation: {1} at {2} after waiting {3}s", schedulerType, operationId, operationName, totalSeconds1);
                        inFlightSpan = Profiler.EnterSpan(string.Format(CultureInfo.InvariantCulture, "Locked for {0} at {1} operation", operationId, operationName), Importance.Normal);
                        stopwatch.Reset();
                        stopwatch.Start();
                        var disposable = Disposable.Create(() =>
                        {
                            gate.OnNext(Unit.Default);
                            gate.OnCompleted();
                            inFlightSpan.Dispose();
                            // ISSUE: reference to a compiler-generated field
                            // ISSUE: reference to a compiler-generated field
                            var totalSeconds2 = stopwatch.Elapsed.TotalSeconds;
                            this.Log().Info("Released {0} scheduler for operation: {1} at {2} after blocking {3}s", schedulerType, operationId, operationName, totalSeconds2);
                        });
                        cancellation.Add(disposable);
                        @lock.OnNext(disposable);
                        @lock.OnCompleted();
                        gate.Wait();
                    });
                    cancellation.Add(ObservableEx.Using(() => @lock, _ => work).SubscribeOn(TaskPoolScheduler.Default).Subscribe(subscriber));
                }
                catch (TaskSchedulerException ex)
                {
                    var message = string.Format("Exception on {0} scheduler queuing operation: {1} at {2}", schedulerType, operationId, operationName);
                    this.Log().Warn(message, (Exception)ex);
                }
                return cancellation;
            }));
        }

        public IObservable<Unit> Shutdown()
        {
            return Observable.Defer(() =>
            {
                pair.Complete();
                return Observable.FromAsync(() => pair.Completion);
            }).SubscribeOn(TaskPoolScheduler.Default);
        }
    }
}

using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace PoC.RxExamples
{
    public class TimelineController
    {
        readonly BehaviorSubject<bool> _pause;
        readonly BehaviorSubject<int> _volume;
        public IObservable<TimelineState> StateStream { get; }
        
        public TimelineController()
        {
            _pause = new BehaviorSubject<bool>(true);
            _volume = new BehaviorSubject<int>(0);

            var startTime = DateTime.Now;

            var refreshInterval = TimeSpan.FromMilliseconds(500);
            var timeChange = PausableInterval(refreshInterval, _pause);

            StateStream = 
                Observable.CombineLatest(
                    timeChange, _volume,
                    (step, volume) => new TimelineState
                    {
                        Time = startTime.AddMilliseconds(refreshInterval.TotalMilliseconds * step),
                        Volume = volume
                    });
        }

        IObservable<long> PausableInterval(TimeSpan refreshRate, IObservable<bool> pauser)
        {
            return Observable.Create<long>(obs =>
            {
                var disposables = new CompositeDisposable();
                var currentTime = 0L;
                var timer = new Timer
                {
                    Interval = refreshRate.TotalMilliseconds
                };

                disposables.Add(
                    Observable.FromEventPattern<ElapsedEventHandler, ElapsedEventArgs>(
                        h => timer.Elapsed += h,
                        h => timer.Elapsed -= h)
                        .Subscribe(args =>
                        {
                            currentTime = currentTime + 1;
                            obs.OnNext(currentTime);
                        }));

                disposables.Add(pauser.Subscribe(isPaused => timer.Enabled = !isPaused));

                return disposables;
            });
        }

        public void NotifyPlay()
        {
            _pause.OnNext(false);
        }

        public void NotifyPause()
        {
            _pause.OnNext(true);
        }

        public void SetVolume(int volume)
        {
            _volume.OnNext(volume);
        }
    }

    public class TimelineState
    {
        public DateTime Time { get; set; }
        public int Volume { get; set; }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            //WaitOnLoaded();
            //PlayPauseInterval();

            var timelineController = new TimelineController();

            timelineController.StateStream.Subscribe(x => Console.WriteLine($"Time: {x.Time}, Volume: {x.Volume}"));

            timelineController.NotifyPlay();

            var exit = false;
            var playing = true;
            while (!exit)
            {
                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.Escape)
                {
                    exit = true;
                    continue;
                }

                if (key == ConsoleKey.Spacebar)
                {
                    if (playing)
                    {
                        timelineController.NotifyPause();
                    }
                    else
                    {
                        timelineController.NotifyPlay();
                    }
                    playing = !playing;
                }
            }
        }

        static void PlayPauseInterval()
        {
            var period = TimeSpan.FromSeconds(1);
            var observable = Observable.Interval(period).Publish();
            observable.Subscribe(i => Console.WriteLine("subscription : {0}", i));
            var exit = false;
            var playing = false;
            while (!exit)
            {
                Console.WriteLine("Press space to play, esc to exit.");
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Spacebar)
                {
                    var connection = observable.Connect(); //--Connects here--
                    playing = true;
                    Console.WriteLine("Press space to pause.");
                    key = Console.ReadKey();
                    if(key.Key == ConsoleKey.Spacebar)
                    {
                        connection.Dispose(); //--Disconnects here--
                        playing = false;
                    }
                }
                if (key.Key == ConsoleKey.Escape)
                {
                    exit = true;
                }
            }
        }

        static void WaitOnLoaded()
        {
            var loaded = new ReplaySubject<Unit>();

            loaded.Subscribe(
                x => Console.WriteLine("Loaded: OnNext"),
                ex => Console.WriteLine("Loaded: OnError"),
                () => Console.WriteLine("Loaded: OnComplete"));

            var play = new Subject<Unit>();

            play.Subscribe(
                x => Console.WriteLine("Play: OnNext"),
                ex => Console.WriteLine("Play: OnError"),
                () => Console.WriteLine("Play: OnComplete"));

            Console.WriteLine("Finished loading. Letting subscribers know...");
            loaded.OnNext(Unit.Default);
            loaded.OnCompleted();

            Observable.Start(() =>
            {
                Console.WriteLine("Waiting on loaded...");
                loaded.Wait();
                //loaded.Finally(() =>
                //{
                Console.WriteLine("Loaded completed. Calling play...");
                play.OnNext(Unit.Default);
                //});
            });

            //Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(x =>
            //{
            //    Console.WriteLine("Finished loading. Letting subscribers know...");
            //    loaded.OnNext(Unit.Default);
            //    loaded.OnCompleted();
            //});

            //new WindowOfValues().Execute();
            //new WebsiteFetching().Execute();
        }
    }
}

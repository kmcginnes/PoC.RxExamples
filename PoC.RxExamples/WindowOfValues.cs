using System;
using System.Reactive.Linq;

namespace PoC.RxExamples
{
    public class WindowOfValues
    {
        public void Execute()
        {
            var dragStarted = Observable.Timer(TimeSpan.FromSeconds(1)).Select(_ => true)
                .Do(_ => Console.WriteLine("Drag Started"));

            var dragStoped = Observable.Timer(TimeSpan.FromSeconds(3)).Select(_ => false)
                .Do(_ => Console.WriteLine("Drag Stoped"));

            var isDragging = dragStarted.Merge(dragStoped)
                .DistinctUntilChanged()
                .StartWith(false);

            Observable.Interval(TimeSpan.FromMilliseconds(300))
                .WhenFalse(isDragging)
                .Take(10)
                .Subscribe(x => Console.WriteLine("Tick: " + x));
        }
    }
}
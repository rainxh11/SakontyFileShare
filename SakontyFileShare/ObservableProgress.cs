using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace SakontyFileShare
{
    public static class ObservableProgress
    {
        class DelegateProgress<T> : IProgress<T>
        {
            private readonly Action<T> _report;
            public DelegateProgress(Action<T> report)
            {
                if (report == null) throw new ArgumentNullException();
                _report = report;
            }
            public void Report(T value)
            {
                _report(value);
            }
        }
        public static IObservable<T> CreateAsync<T>(Func<IProgress<T>, Task> action)
        {
            return Observable.Create<T>(async observer =>
            {
                try
                {
                    await action(new DelegateProgress<T>(observer.OnNext));
                }
                catch(Exception ex)
                {
                    observer.OnError(ex);
                }
                observer.OnCompleted();
                return Disposable.Empty;
            });
        }
    }

}


using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace HelloSwitcher.Helper;

/// <summary>
/// Rx Sample like operator
/// </summary>
public class Sample
{
	private readonly System.Timers.Timer _timer;
	private readonly Func<string, CancellationToken, Task> _action;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public Sample(TimeSpan dueTime, Func<string, CancellationToken, Task> action)
	{
		if (dueTime <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(dueTime), dueTime, "The duration must be positive.");

		_timer = new System.Timers.Timer(dueTime.TotalMilliseconds);
		_timer.Elapsed += OnElapsed;

		this._action = action ?? throw new ArgumentNullException(nameof(action));
	}

	private string _actionName;
	private CancellationToken _cancellationToken;

	public void Push(string actionName, CancellationToken cancellationToken = default)
	{
		if (_semaphore.Wait(0, cancellationToken))
		{
			_actionName = actionName;
			_cancellationToken = cancellationToken;
			_timer.Start();
		}
	}

	private async void OnElapsed(object sender, ElapsedEventArgs e)
	{
		_timer.Stop();
		await _action.Invoke(_actionName, _cancellationToken);
		_semaphore.Release();
	}
}
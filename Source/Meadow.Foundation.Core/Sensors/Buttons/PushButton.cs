using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Buttons;
using System;

namespace Meadow.Foundation.Sensors.Buttons
{
	/// <summary>
	/// A simple push button. 
	/// </summary>
	public class PushButton : IButton
	{
		#region Properties
		/// <summary>
		/// This duration controls the debounce filter. It also has the effect
		/// of rate limiting clicks. Decrease this time to allow users to click
		/// more quickly.
		/// </summary>
		public TimeSpan DebounceDuration
		{
			get => (DigitalIn != null) ? new TimeSpan(0, 0, 0, 0, DigitalIn.DebounceDuration) : TimeSpan.MinValue;
			set
			{
				DigitalIn.DebounceDuration = (int)value.TotalMilliseconds;
			}
		}

		/// <summary>
		/// Returns if this is connected to a pull up resistor, or a pull down resistor.
		/// </summary>
		public ResistorMode Resistor { get; private set; }

		/// <summary>
		/// Returns the current raw state of the switch. If the switch 
		/// is pressed (connected), returns true, otherwise false.
		/// </summary>
		public bool State => (DigitalIn != null) ? !DigitalIn.State : false;

		/// <summary>
		/// The minimum duration for a long press.
		/// </summary>
		public TimeSpan LongPressThreshold { get; set; } = new TimeSpan(0, 0, 0, 0, 500);

		/// <summary>
		/// Returns digital input port.
		/// </summary>
		public IDigitalInputPort DigitalIn { get; private set; }

		/// <summary>
		/// Raised when a press starts (the button is pushed down; circuit is closed).
		/// </summary>
		public event EventHandler PressStarted = delegate { };

		/// <summary>
		/// Raised when a press ends (the button is released; circuit is opened).
		/// </summary>
		public event EventHandler PressEnded = delegate { };

		/// <summary>
		/// Raised when the button circuit is re-opened after it has been closed (at the end of a �press�.
		/// </summary>
		public event EventHandler Clicked = delegate { };

		/// <summary>
		/// Raised when the button circuit is pressed for at least 500ms.
		/// </summary>
		public event EventHandler LongPressClicked = delegate { };
		#endregion

		#region Member variables / fields
		/// <summary>
		/// Minimum DateTime value when the button was pushed
		/// </summary>
		protected DateTime _lastClicked = DateTime.MinValue;

		/// <summary>
		/// Maximum DateTime value when the button was just pushed
		/// </summary>
		protected DateTime _buttonPressStart = DateTime.MaxValue;
		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor is private to prevent it being called.
		/// </summary>
		private PushButton() { }

		/// <summary>
		/// Creates a PushButton on a digital input port connected on a IIOdevice, specifying Interrupt Mode, Circuit Type and optionally Debounce filter duration.
		/// </summary>
		/// <param name="device">The IODevice to connect the button to.</param>
		/// <param name="inputPin">The input pin to bind this button to.</param>
		/// <param name="resistorMode">Determines if this is a pull up or pull down resistor configuration.</param>
		/// <param name="debounceDuration">the duration in miliseconds to debounce the button for</param>
		public PushButton(IIODevice device, IPin inputPin, ResistorMode resistorMode, uint debounceDuration = 20)
		{
			DigitalIn = device.CreateDigitalInputPort(inputPin, InterruptMode.EdgeBoth, resistorMode, (int)debounceDuration);
			DigitalIn.Changed += DigitalInChanged;
		}

		/// <summary>
		/// Creates a PushButton on a digital input port specifying Interrupt Mode, Circuit Type and optionally Debounce filter duration.
		/// </summary>
		/// <param name="interruptPort"></param>
		/// <param name="resistorMode">Determines if this is a pull up or pull down resistor configuration.</param>
		/// <param name="debounceDuration"></param>
		public PushButton(IDigitalInputPort interruptPort, ResistorMode resistorMode, uint debounceDuration = 20)
		{
			DigitalIn = interruptPort;
			DigitalIn.Resistor = resistorMode;
			DebounceDuration = new TimeSpan(0, 0, 0, 0, (int)debounceDuration);
			DigitalIn.Changed += DigitalInChanged;
		}
		#endregion

		#region Methods
		private void DigitalInChanged(object sender, DigitalInputPortEventArgs e)
		{
			bool pressed = DigitalIn.Resistor == ResistorMode.PullDown ? false : true;
			bool released = DigitalIn.Resistor == ResistorMode.PullDown ? true : false;
			DateTime pressStartAt = default;

			if (State == pressed)
			{
				// save our press start time (for long press event)
				pressStartAt = DateTime.Now;
				// raise our event in an inheritance friendly way
				RaisePressStarted();
			}
			else if (State == released)
			{
				// calculate the press duration
				TimeSpan pressDuration = DateTime.Now - pressStartAt;

				// if it's a long press, raise our long press event
				if (pressDuration > LongPressThreshold) this.RaiseLongPress();

				// raise the other events
				RaisePressEnded();
				RaiseClicked();
			}
		}

		/// <summary>
		/// Raised when the button circuit is re-opened after it has been closed (at the end of a �press�).
		/// </summary>
		protected virtual void RaiseClicked()
		{
			Clicked(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raised when a press starts (the button is pushed down; circuit is closed).
		/// </summary>
		protected virtual void RaisePressStarted()
		{
			// raise the press started event
			PressStarted(this, new EventArgs());
		}

		/// <summary>
		/// Raised when a press ends (the button is released; circuit is opened).
		/// </summary>
		protected virtual void RaisePressEnded()
		{
			PressEnded(this, new EventArgs());
		}

		/// <summary>
		/// Raised when the button circuit is pressed for at least 500ms.
		/// </summary>
		protected virtual void RaiseLongPress()
		{
			LongPressClicked(this, new EventArgs());
		}
		#endregion
	}
}
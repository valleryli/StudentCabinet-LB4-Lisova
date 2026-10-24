using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StudentCabinet_LB1.Messages
{
	public enum AppLifecycleEvent { Created, Activated, Deactivated, Stopped, Resumed, Destroying }

	// App надсилає це повідомлення, ViewModel підписується на нього
	public class AppLifecycleMessage : ValueChangedMessage<AppLifecycleEvent>
	{
		public AppLifecycleMessage(AppLifecycleEvent value) : base(value) { }
	}
}
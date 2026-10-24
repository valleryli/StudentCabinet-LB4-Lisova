using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StudentCabinet_LB1.Messages
{
    // Сповіщає ViewModel'і, що змінилась кількість записів у черзі синхронізації
    public class SyncStateMessage : ValueChangedMessage<int>
    {
        public SyncStateMessage(int pendingCount) : base(pendingCount) { }
    }
}
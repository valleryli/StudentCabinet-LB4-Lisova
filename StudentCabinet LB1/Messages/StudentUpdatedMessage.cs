using CommunityToolkit.Mvvm.Messaging.Messages;
using StudentCabinet_LB1.Models;

namespace StudentCabinet_LB1.Messages
{
    public class StudentUpdatedMessage : ValueChangedMessage<Student>
    {
        public StudentUpdatedMessage(Student student) : base(student)
        {
        }
    }
}
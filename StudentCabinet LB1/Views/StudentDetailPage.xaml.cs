using StudentCabinet_LB1.ViewModels;

namespace StudentCabinet_LB1.Views
{
    public partial class StudentDetailPage : ContentPage
    {
        public StudentDetailPage(StudentDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
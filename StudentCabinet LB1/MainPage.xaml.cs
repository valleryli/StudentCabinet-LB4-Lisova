using StudentCabinet_LB1.ViewModels;

namespace StudentCabinet_LB1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new StudentViewModel();
        }
    }
}